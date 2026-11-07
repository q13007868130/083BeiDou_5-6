using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public sealed class NativeCommunityService
{
	private sealed class GuildFormation
	{
		public string name;

		public long expires;

		public HashSet<int> members = new HashSet<int>();

		public HashSet<int> accepted = new HashSet<int>();
	}

	private readonly DbStore db;

	private readonly HashSet<int> online = new HashSet<int>();

	private readonly Dictionary<int, int> guildInvites = new Dictionary<int, int>();

	private readonly Dictionary<int, int> familyInvites = new Dictionary<int, int>();

	private readonly Dictionary<int, long> inviteTimes = new Dictionary<int, long>();

	private readonly Dictionary<int, int> summons = new Dictionary<int, int>();

	private readonly Dictionary<int, string> boards = new Dictionary<int, string>();

	private readonly Dictionary<int, GuildFormation> formations = new Dictionary<int, GuildFormation>();

	public static readonly int[] FamilyCosts = new int[11]
	{
		300, 500, 700, 800, 1000, 1200, 1500, 2000, 2500, 4000,
		5000
	};

	public Func<int, int, bool> SameChannel;

	public Func<int, int, bool> CanFamilyTravel;

	private static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

	public static int IncreaseGuildCost(DbStore db, int capacity)
	{
		int num = GameConfigDao.GetInt(db, "expand_guild_base_cost", 500000) + Math.Max(0, (capacity - 15) / 5) * GameConfigDao.GetInt(db, "expand_guild_tier_cost", 1000000);
		if (capacity > 30)
		{
			return Math.Min(GameConfigDao.GetInt(db, "expand_guild_max_cost", 5000000), Math.Max(num, 5000000));
		}
		return num;
	}

	public NativeCommunityService(DbStore db)
	{
		this.db = db;
	}

	public void BeginSession(int id)
	{
		online.Add(id);
	}

	public void EndSession(int id)
	{
		online.Remove(id);
		boards.Remove(id);
		guildInvites.Remove(id);
		familyInvites.Remove(id);
		formations.Remove(id);
		summons.Remove(id);
		foreach (int item in new List<int>(formations.Keys))
		{
			if (formations[item].members.Contains(id))
			{
				formations.Remove(item);
			}
		}
		db.Table("family_buff").Delete((DbRow r) => DbStore.Int(r, "charid") == id);
	}

	public void DeleteCharacter(int id)
	{
		EndSession(id);
		CharRecord c = Character(id);
		DbRow dbRow = ((c == null) ? null : Guild(c.guildid));
		if (dbRow != null && DbStore.Int(dbRow, "leader") == id)
		{
			foreach (DbRow item in from x in db.Table("characters")
				select DbStore.Int(x, "guildid") == c.guildid)
			{
				Membership(DbStore.Int(item, "id"), 0, 5);
			}
			db.Table("guilds").Delete((DbRow x) => DbStore.Int(x, "guildid") == c.guildid);
		}
		foreach (DbRow item2 in from x in db.Table("family_character")
			select DbStore.Int(x, "seniorid") == id)
		{
			int num = DbStore.Int(item2, "cid");
			item2["seniorid"] = 0L;
			Rehome(num, num);
		}
		db.Table("family_character").Delete((DbRow x) => DbStore.Int(x, "cid") == id);
		db.Table("family_entitlement").Delete((DbRow x) => DbStore.Int(x, "charid") == id);
		db.Table("cash_gifts").Delete((DbRow x) => DbStore.Int(x, "characterid") == id);
		db.Table("cash_wishlist").Delete((DbRow x) => DbStore.Int(x, "characterid") == id);
		db.Table("cash_items").Delete((DbRow x) => DbStore.Int(x, "characterid") == id);
	}

	public bool IsOnline(int id)
	{
		return online.Contains(id);
	}

	private CharRecord Character(int id)
	{
		return CharactersDao.FindById(db, id);
	}

	private CharRecord Find(string name, int world)
	{
		DbRow dbRow = db.Table("characters").Find((DbRow r) => DbStore.Int(r, "world") == world && string.Equals(DbStore.Str(r, "name"), name, StringComparison.OrdinalIgnoreCase));
		if (dbRow != null)
		{
			return Character(DbStore.Int(dbRow, "id"));
		}
		return null;
	}

	private DbRow Guild(int id)
	{
		return db.Table("guilds").Find((DbRow r) => DbStore.Int(r, "guildid") == id);
	}

	private DbRow Family(int id)
	{
		return db.Table("family_character").Find((DbRow r) => DbStore.Int(r, "cid") == id);
	}

	private List<DbRow> FamilyMembers(int id)
	{
		return from r in db.Table("family_character")
			select DbStore.Int(r, "familyid") == id;
	}

	private void SetCharacter(int id, string column, int value)
	{
		db.Table("characters").Update((DbRow r) => DbStore.Int(r, "id") == id, new DbRow { [column] = (long)value });
	}

	private void SetCharacterLong(int id, string column, long value)
	{
		db.Table("characters").Update((DbRow r) => DbStore.Int(r, "id") == id, new DbRow { [column] = value });
	}

	private void Meso(CharRecord c, long amount)
	{
		c.meso = Math.Max(0L, c.meso + amount);
		SetCharacterLong((int)c.id, "meso", c.meso);
	}

	private void Membership(int id, int guild, int rank)
	{
		SetCharacter(id, "guildid", guild);
		SetCharacter(id, "guildrank", rank);
	}

	private bool ResetDaily(DbRow row)
	{
		long midnight = new DateTimeOffset(DateTime.Today).ToUnixTimeMilliseconds();
		if (DbStore.Long(row, "lastresettime", 0L) >= midnight)
		{
			return false;
		}
		row["lastresettime"] = Now;
		row["todaysrep"] = 0L;
		row["reptosenior"] = 0L;
		int cid = DbStore.Int(row, "cid");
		db.Table("family_entitlement").Delete((DbRow r) => DbStore.Int(r, "charid") == cid && DbStore.Long(r, "timestamp", 0L) < midnight);
		return true;
	}

	public CommunityResponse Handle(CommunityRequest r)
	{
		CharRecord charRecord = Character(r.characterId);
		if (charRecord == null)
		{
			return new CommunityResponse
			{
				ok = false,
				error = "角色不存在"
			};
		}
		lock (db)
		{
			if (r.operation == CommunityOperation.ReadFamilyTree)
			{
				CommunityResponse communityResponse = Read(charRecord);
				CharRecord charRecord2 = ((r.targetId > 0) ? Character(r.targetId) : Find(r.name, charRecord.world));
				if (!GameConfigDao.GetBool(db, "use_family_system", fallback: true) || charRecord2 == null || charRecord2.world != charRecord.world || !online.Contains((int)charRecord2.id) || Family((int)charRecord2.id) == null || (SameChannel != null && !SameChannel((int)charRecord.id, (int)charRecord2.id)))
				{
					communityResponse.ok = false;
					communityResponse.error = "目标角色不在线或尚未加入学院";
					return communityResponse;
				}
				communityResponse.treeTarget = (int)charRecord2.id;
				foreach (DbRow item in FamilyMembers(DbStore.Int(Family((int)charRecord2.id), "familyid")))
				{
					CharRecord charRecord3 = Character(DbStore.Int(item, "cid"));
					if (charRecord3 != null)
					{
						CommunityMember communityMember = Member(charRecord3);
						communityMember.senior = DbStore.Int(item, "seniorid");
						communityResponse.familyTree.Add(communityMember);
					}
				}
				return communityResponse;
			}
			DbRow dbRow = Family(r.characterId);
			if (r.operation == CommunityOperation.Read || (r.operation == CommunityOperation.RecordKill && (r.value <= 1 || dbRow == null || Family(DbStore.Int(dbRow, "seniorid")) == null)))
			{
				return Read(charRecord);
			}
			db.BeginTransaction();
			string text;
			try
			{
				text = Apply(charRecord, r);
				if (text == null)
				{
					db.Commit();
				}
				else
				{
					db.Rollback();
				}
			}
			catch
			{
				db.Rollback();
				throw;
			}
			db.Save();
			CommunityResponse communityResponse2 = Read(Character(r.characterId));
			communityResponse2.ok = text == null;
			communityResponse2.error = text ?? "";
			return communityResponse2;
		}
	}

	private string Apply(CharRecord c, CommunityRequest r)
	{
		int cid = (int)c.id;
		DbRow dbRow = Guild(c.guildid);
		DbRow dbRow2 = Family(cid);
		bool flag = dbRow != null && c.guildrank == 1;
		bool flag2 = dbRow != null && c.guildrank <= 2;
		CharRecord charRecord = ((r.targetId > 0) ? Character(r.targetId) : Find(r.name, c.world));
		switch (r.operation)
		{
		case CommunityOperation.Read:
			return null;
		case CommunityOperation.RecordKill:
			if (r.value > 1)
			{
				GiveReputation(cid, GameConfigDao.GetInt(db, (r.targetId == 1) ? "family_rep_per_boss_kill" : "family_rep_per_kill", (r.targetId == 1) ? 20 : 4));
			}
			return null;
		case CommunityOperation.AnswerSummon:
		{
			if (!summons.TryGetValue(cid, out var value4))
			{
				return "召集请求已失效";
			}
			CharRecord charRecord3 = Character(value4);
			summons.Remove(cid);
			if (r.value == 0)
			{
				return null;
			}
			if (charRecord3 == null || !online.Contains(value4) || c.familyId != charRecord3.familyId || CanFamilyTravel == null || !CanFamilyTravel(c.map, charRecord3.map))
			{
				return "召集条件已改变";
			}
			SetCharacter(cid, "map", charRecord3.map);
			return null;
		}
		case CommunityOperation.CreateGuild:
		{
			if (c.guildid > 0)
			{
				return "已经加入家族";
			}
			if (c.meso < GameConfigDao.GetInt(db, "create_guild_cost", 1500000))
			{
				return "创建家族需要150万金币（以服务器配置为准）";
			}
			if (!Regex.IsMatch(r.name ?? "", "^[\\u4e00-\\u9fa5a-zA-Z0-9_]{3,12}$"))
			{
				return "家族名称须为3～12个中文、字母、数字或下划线";
			}
			if (db.Table("guilds").Find((DbRow x) => string.Equals(DbStore.Str(x, "name"), r.name, StringComparison.OrdinalIgnoreCase)) != null)
			{
				return "家族名称已存在";
			}
			if (c.level < 10 && !GameConfigDao.GetBool(db, "use_party_for_starters"))
			{
				return "达到10级才能发起创建家族";
			}
			GuildFormation guildFormation = new GuildFormation
			{
				name = r.name,
				expires = Now + 60000
			};
			guildFormation.members.Add(cid);
			guildFormation.accepted.Add(cid);
			foreach (int item in online)
			{
				CharRecord charRecord2 = Character(item);
				if (charRecord2 != null && charRecord2.world == c.world && charRecord2.map == c.map && charRecord2.guildid == 0 && charRecord2.party == 0)
				{
					guildFormation.members.Add(item);
				}
			}
			if (guildFormation.members.Count < GameConfigDao.GetInt(db, "create_guild_min_partners", 6))
			{
				return "同地图内符合条件的人数不足，源端默认至少6人";
			}
			formations[cid] = guildFormation;
			return FinishFormation(cid);
		}
		case CommunityOperation.ConfirmGuild:
		{
			if (!formations.TryGetValue(r.targetId, out var value2) || value2.expires < Now || !value2.members.Contains(cid))
			{
				return "创建确认已失效";
			}
			if (r.value == 0)
			{
				formations.Remove(r.targetId);
				return null;
			}
			if (c.guildid > 0)
			{
				return "已经加入家族";
			}
			value2.accepted.Add(cid);
			return FinishFormation(r.targetId);
		}
		case CommunityOperation.InviteGuild:
			if (!flag2)
			{
				return "只有族长和副族长可以邀请";
			}
			if (charRecord == null || !online.Contains((int)charRecord.id))
			{
				return "该角色不在线";
			}
			if (charRecord.world != c.world || charRecord.guildid > 0)
			{
				return "对方已加入家族或不在本世界";
			}
			guildInvites[(int)charRecord.id] = c.guildid;
			inviteTimes[(int)charRecord.id] = Now;
			return null;
		case CommunityOperation.AcceptGuild:
		{
			if (!guildInvites.TryGetValue(cid, out var gid) || !inviteTimes.TryGetValue(cid, out var value) || Now - value > 60000)
			{
				return "家族邀请已失效";
			}
			DbRow dbRow3 = Guild(gid);
			if (dbRow3 == null || c.guildid > 0)
			{
				return "无法加入该家族";
			}
			if ((from x in db.Table("characters")
				select DbStore.Int(x, "guildid") == gid).Count >= DbStore.Int(dbRow3, "capacity"))
			{
				return "家族人数已满";
			}
			Membership(cid, gid, 5);
			guildInvites.Remove(cid);
			return null;
		}
		case CommunityOperation.LeaveGuild:
			if (dbRow == null)
			{
				return "尚未加入家族";
			}
			if (flag)
			{
				return "族长需要解散家族";
			}
			Membership(cid, 0, 5);
			return null;
		case CommunityOperation.DisbandGuild:
			if (!flag)
			{
				return "只有族长可以解散";
			}
			foreach (DbRow item2 in from x in db.Table("characters")
				select DbStore.Int(x, "guildid") == c.guildid)
			{
				Membership(DbStore.Int(item2, "id"), 0, 5);
			}
			db.Table("guilds").Delete((DbRow x) => DbStore.Int(x, "guildid") == c.guildid);
			return null;
		case CommunityOperation.ExpelGuild:
			if (!flag2 || charRecord == null || charRecord.guildid != c.guildid || charRecord.guildrank <= c.guildrank)
			{
				return "没有权限驱逐该成员";
			}
			Membership((int)charRecord.id, 0, 5);
			return null;
		case CommunityOperation.GuildRank:
			if (!flag2 || charRecord == null || charRecord.guildid != c.guildid || charRecord.id == c.id || charRecord.guildrank <= c.guildrank || r.value < 2 || r.value > 5 || (r.value == 2 && !flag))
			{
				return "职位或操作权限无效";
			}
			Membership((int)charRecord.id, c.guildid, r.value);
			return null;
		case CommunityOperation.GuildNotice:
			if (!flag2)
			{
				return "没有修改公告的权限";
			}
			if (r.text == null || r.text.Length > 100)
			{
				return "公告最多100个字符";
			}
			dbRow["notice"] = r.text;
			return null;
		case CommunityOperation.GuildTitles:
		{
			if (!flag || r.titles == null || r.titles.Length != 5)
			{
				return "只有族长可以修改五级职位名称";
			}
			for (int num8 = 0; num8 < 5; num8++)
			{
				if (string.IsNullOrEmpty(r.titles[num8]) || r.titles[num8].Length > 45)
				{
					return "职位名称长度无效";
				}
			}
			for (int num9 = 0; num9 < 5; num9++)
			{
				dbRow["rank" + (num9 + 1) + "title"] = r.titles[num9];
			}
			return null;
		}
		case CommunityOperation.GuildEmblem:
		{
			if (!flag || c.map != 200000301)
			{
				return "族长需在家族总部修改徽章";
			}
			int num4 = GameConfigDao.GetInt(db, "change_emblem_cost", 5000000);
			if (c.meso < num4)
			{
				return "金币不足";
			}
			if (r.logo < 0 || r.logo > 65535 || r.background < 0 || r.background > 65535 || r.logoColor < 0 || r.logoColor > 255 || r.backgroundColor < 0 || r.backgroundColor > 255)
			{
				return "徽章参数无效";
			}
			dbRow["logo"] = (long)r.logo;
			dbRow["logoColor"] = (long)r.logoColor;
			dbRow["logoBG"] = (long)r.background;
			dbRow["logoBGColor"] = (long)r.backgroundColor;
			Meso(c, -num4);
			return null;
		}
		case CommunityOperation.ExpandGuild:
		{
			if (!flag)
			{
				return "只有族长可以扩容";
			}
			int num = DbStore.Int(dbRow, "capacity");
			if (num >= 100)
			{
				return "家族容量已达上限";
			}
			int num2 = IncreaseGuildCost(db, num);
			if (c.meso < num2)
			{
				return "金币不足，需要" + num2;
			}
			dbRow["capacity"] = (long)(num + 5);
			Meso(c, -num2);
			return null;
		}
		case CommunityOperation.InviteFamily:
			if (!GameConfigDao.GetBool(db, "use_family_system", fallback: true))
			{
				return "学院系统已关闭";
			}
			if (charRecord == null || !online.Contains((int)charRecord.id))
			{
				return "该角色不在线";
			}
			if (charRecord.id == c.id || charRecord.world != c.world || charRecord.map != c.map)
			{
				return "需要邀请同地图的其他角色";
			}
			if (charRecord.level <= 10)
			{
				return "对方需要超过10级";
			}
			if (Math.Abs(charRecord.level - c.level) > GameConfigDao.GetInt(db, "family_add_add_level_range", 240))
			{
				return "等级差超过学院限制";
			}
			if (dbRow2 != null && FamilyMembers(DbStore.Int(dbRow2, "familyid")).FindAll((DbRow x) => DbStore.Int(x, "seniorid") == cid).Count >= 2)
			{
				return "最多登记两名直属同学";
			}
			if (dbRow2 != null && c.familyId == charRecord.familyId)
			{
				return "已经在同一学院";
			}
			familyInvites[(int)charRecord.id] = cid;
			inviteTimes[-(int)charRecord.id] = Now;
			return null;
		case CommunityOperation.AcceptFamily:
		{
			if (!familyInvites.TryGetValue(cid, out var seniorId) || !inviteTimes.TryGetValue(-cid, out var value3) || Now - value3 > 60000 || !online.Contains(seniorId))
			{
				return "学院邀请已失效";
			}
			if (Character(seniorId) == null)
			{
				return "学长不存在";
			}
			if (dbRow2 != null && DbStore.Int(dbRow2, "seniorid") != 0)
			{
				return "只有学院首领可以合并学院";
			}
			DbRow dbRow4 = Family(seniorId);
			int num3 = ((dbRow4 == null) ? seniorId : DbStore.Int(dbRow4, "familyid"));
			if (num3 == c.familyId && dbRow2 != null)
			{
				return "不能形成循环学院关系";
			}
			if (Depth(seniorId) + Height(cid) > GameConfigDao.GetInt(db, "family_max_generations", 1000))
			{
				return "学院代数超过上限";
			}
			if (dbRow4 == null)
			{
				AddFamily(seniorId, num3, 0);
			}
			if (FamilyMembers(num3).FindAll((DbRow x) => DbStore.Int(x, "seniorid") == seniorId).Count >= 2)
			{
				return "学长已登记两名直属同学";
			}
			if (dbRow2 == null)
			{
				AddFamily(cid, num3, seniorId);
			}
			else
			{
				int id = DbStore.Int(dbRow2, "familyid");
				dbRow2["seniorid"] = (long)seniorId;
				foreach (DbRow item3 in FamilyMembers(id))
				{
					item3["familyid"] = (long)num3;
					SetCharacter(DbStore.Int(item3, "cid"), "familyId", num3);
				}
			}
			familyInvites.Remove(cid);
			return null;
		}
		case CommunityOperation.SeparateFamily:
		{
			if (dbRow2 == null)
			{
				return "尚未加入学院";
			}
			DbRow dbRow5 = ((r.targetId == 0 || r.targetId == cid) ? dbRow2 : Family(r.targetId));
			if (dbRow5 == null || (DbStore.Int(dbRow5, "cid") != cid && DbStore.Int(dbRow5, "seniorid") != cid))
			{
				return "只能解除直属学院关系";
			}
			CharRecord charRecord4 = Character(DbStore.Int(dbRow5, "seniorid"));
			if (charRecord4 == null)
			{
				return "没有可解除的学长关系";
			}
			int num5 = Math.Abs(c.level - charRecord4.level);
			int num6 = 2500 * num5 + num5 * num5;
			if (c.meso < num6)
			{
				return "金币不足，需要" + num6;
			}
			CharRecord charRecord5 = Character(DbStore.Int(dbRow5, "cid"));
			int num7 = (charRecord5.level / 20 + 10) * charRecord5.level * 2;
			GainRep(Family((int)charRecord4.id), -num7, total: false);
			GainRep(Family(DbStore.Int(Family((int)charRecord4.id), "seniorid")), -(num7 / 2), total: false);
			Meso(c, -num6);
			dbRow5["seniorid"] = 0L;
			Rehome(DbStore.Int(dbRow5, "cid"), DbStore.Int(dbRow5, "cid"));
			return null;
		}
		case CommunityOperation.FamilyPrecepts:
			if (dbRow2 == null || DbStore.Int(dbRow2, "seniorid") != 0)
			{
				return "只有学院首领可以修改家训";
			}
			if (r.text == null || r.text.Length > 200)
			{
				return "家训最多200个字符";
			}
			foreach (DbRow item4 in FamilyMembers(DbStore.Int(dbRow2, "familyid")))
			{
				item4["precepts"] = r.text;
			}
			return null;
		case CommunityOperation.UseFamily:
			return UseFamily(c, dbRow2, r.value, charRecord);
		case CommunityOperation.SetBoard:
		{
			if (c.map >= 910000001 && c.map <= 910000022)
			{
				return "自由市场房间内不能使用黑板";
			}
			bool flag3 = false;
			foreach (DbRow item5 in InventoryDao.ItemsByCharacter(db, cid))
			{
				if (DbStore.Int(item5, "itemid") / 10000 == 537 && DbStore.Int(item5, "quantity") > 0)
				{
					flag3 = true;
				}
			}
			if (!flag3)
			{
				return "需要持有黑板类现金道具";
			}
			if (r.text == null || r.text.Length > 255)
			{
				return "黑板内容最多255个字符";
			}
			boards[cid] = r.text;
			return null;
		}
		case CommunityOperation.CloseBoard:
			boards.Remove(cid);
			return null;
		default:
			return "无效操作";
		}
	}

	private string FinishFormation(int owner)
	{
		GuildFormation guildFormation = formations[owner];
		if (guildFormation.accepted.Count != guildFormation.members.Count)
		{
			return null;
		}
		CharRecord charRecord = Character(owner);
		int num = GameConfigDao.GetInt(db, "create_guild_cost", 1500000);
		if (charRecord == null || charRecord.meso < num)
		{
			formations.Remove(owner);
			return "创建条件已改变";
		}
		foreach (int member in guildFormation.members)
		{
			CharRecord charRecord2 = Character(member);
			if (charRecord2 == null || charRecord2.guildid != 0 || charRecord2.map != charRecord.map || !online.Contains(member))
			{
				formations.Remove(owner);
				return "创建成员状态已改变";
			}
		}
		int guild = (int)db.Table("guilds").Insert(new DbRow
		{
			["leader"] = (long)owner,
			["name"] = guildFormation.name
		});
		foreach (int member2 in guildFormation.members)
		{
			Membership(member2, guild, (member2 == owner) ? 1 : 5);
		}
		Meso(charRecord, -num);
		formations.Remove(owner);
		return null;
	}

	private void AddFamily(int cid, int fid, int senior)
	{
		db.Table("family_character").Insert(new DbRow
		{
			["cid"] = (long)cid,
			["familyid"] = (long)fid,
			["seniorid"] = (long)senior
		});
		SetCharacter(cid, "familyId", fid);
	}

	private int Depth(int id)
	{
		int num = 1;
		HashSet<int> hashSet = new HashSet<int>();
		while (hashSet.Add(id))
		{
			DbRow dbRow = Family(id);
			if (dbRow == null || DbStore.Int(dbRow, "seniorid") == 0)
			{
				break;
			}
			num++;
			id = DbStore.Int(dbRow, "seniorid");
		}
		return num;
	}

	private int Height(int id)
	{
		int num = 1;
		foreach (DbRow item in from x in db.Table("family_character")
			select DbStore.Int(x, "seniorid") == id)
		{
			num = Math.Max(num, 1 + Height(DbStore.Int(item, "cid")));
		}
		return num;
	}

	private void Rehome(int id, int fid)
	{
		DbRow dbRow = Family(id);
		if (dbRow == null)
		{
			return;
		}
		dbRow["familyid"] = (long)fid;
		SetCharacter(id, "familyId", fid);
		foreach (DbRow item in from x in db.Table("family_character")
			select DbStore.Int(x, "seniorid") == id)
		{
			Rehome(DbStore.Int(item, "cid"), fid);
		}
	}

	private void GainRep(DbRow row, int value, bool total)
	{
		if (row != null)
		{
			ResetDaily(row);
			row["reputation"] = (long)(DbStore.Int(row, "reputation") + value);
			row["todaysrep"] = (long)(DbStore.Int(row, "todaysrep") + value);
			if (total && value > 0)
			{
				row["totalreputation"] = (long)(DbStore.Int(row, "totalreputation") + value);
			}
		}
	}

	public void GiveReputation(int cid, int amount)
	{
		DbRow dbRow = Family(cid);
		if (dbRow == null)
		{
			return;
		}
		DbRow dbRow2 = Family(DbStore.Int(dbRow, "seniorid"));
		if (dbRow2 == null)
		{
			return;
		}
		CharRecord charRecord = Character(DbStore.Int(dbRow2, "cid"));
		CharRecord charRecord2 = Character(cid);
		if (charRecord != null && charRecord2 != null)
		{
			if (charRecord.level < charRecord2.level && amount > 0)
			{
				amount /= 2;
			}
			GainRep(dbRow2, amount, total: true);
			dbRow["reptosenior"] = (long)(DbStore.Int(dbRow, "reptosenior") + Math.Max(0, amount));
			if (GameConfigDao.GetBool(db, "family_rep_per_level_up_notice", fallback: true))
			{
				GainRep(Family(DbStore.Int(dbRow2, "seniorid")), amount, total: true);
			}
			db.Save();
		}
	}

	private string UseFamily(CharRecord c, DbRow family, int type, CharRecord target)
	{
		if (!GameConfigDao.GetBool(db, "use_family_system", fallback: true) || family == null)
		{
			return "尚未加入学院";
		}
		if (type < 0 || type >= FamilyCosts.Length)
		{
			return "无效特权";
		}
		ResetDaily(family);
		if (DbStore.Int(family, "reputation") < FamilyCosts[type])
		{
			return "学院声望不足";
		}
		if (type >= 2 && db.Table("family_entitlement").Find((DbRow x) => DbStore.Int(x, "charid") == c.id && DbStore.Int(x, "entitlementid") == type) != null)
		{
			return "此特权今天已经使用";
		}
		if (type <= 1)
		{
			if (target == null || target.id == c.id || !online.Contains((int)target.id))
			{
				return "目标同学不在线";
			}
			if (target.familyId != c.familyId)
			{
				return "目标不在同一学院";
			}
			if (CanFamilyTravel == null || !CanFamilyTravel((type == 0) ? c.map : target.map, (type == 0) ? target.map : c.map))
			{
				return "这些地图之间不能使用学院传送";
			}
			if (type == 1)
			{
				if (summons.ContainsKey((int)target.id))
				{
					return "对方已有召集请求";
				}
				summons[(int)target.id] = (int)c.id;
			}
			else
			{
				SetCharacter((int)c.id, "map", target.map);
			}
		}
		else
		{
			if (Rates((int)c.id).buffExpires > Now)
			{
				return "学院增益尚未结束";
			}
			double num = ((type == 3) ? 1.5 : ((double)((type != 6 && type != 8 && type != 10 && type != 4) ? 1 : 2)));
			double num2 = ((type == 2) ? 1.5 : ((double)((type != 5 && type != 7 && type != 9 && type != 4) ? 1 : 2)));
			int num3 = ((type == 2 || type == 3 || type == 5 || type == 6) ? 15 : 30);
			List<int> list = new List<int> { (int)c.id };
			if (type == 4)
			{
				list.Clear();
				foreach (DbRow item in FamilyMembers(c.familyId))
				{
					if (online.Contains(DbStore.Int(item, "cid")))
					{
						list.Add(DbStore.Int(item, "cid"));
					}
				}
			}
			if (type >= 9)
			{
				if (c.party == 0)
				{
					return "组队特权需要加入组队";
				}
				list.Clear();
				foreach (int item2 in online)
				{
					CharRecord charRecord = Character(item2);
					if (charRecord != null && charRecord.party == c.party && charRecord.familyId == c.familyId)
					{
						list.Add(item2);
					}
				}
			}
			foreach (int id in list)
			{
				db.Table("family_buff").Delete((DbRow x) => DbStore.Int(x, "charid") == id);
				db.Table("family_buff").Insert(new DbRow
				{
					["charid"] = (long)id,
					["expires"] = Now + (long)num3 * 60000L,
					["expRate"] = num,
					["dropRate"] = num2
				});
			}
			db.Table("family_entitlement").Insert(new DbRow
			{
				["charid"] = c.id,
				["entitlementid"] = (long)type,
				["timestamp"] = Now
			});
		}
		GainRep(family, -FamilyCosts[type], total: false);
		return null;
	}

	public CommunityResponse Rates(int cid)
	{
		CommunityResponse communityResponse = new CommunityResponse();
		DbRow dbRow = db.Table("family_buff").Find((DbRow x) => DbStore.Int(x, "charid") == cid && DbStore.Long(x, "expires", 0L) > Now);
		if (dbRow != null)
		{
			communityResponse.expRate = DbStore.Dbl(dbRow, "expRate", 1.0);
			communityResponse.dropRate = DbStore.Dbl(dbRow, "dropRate", 1.0);
			communityResponse.familyDropRate = communityResponse.dropRate;
			communityResponse.buffExpires = DbStore.Long(dbRow, "expires", 0L);
		}
		communityResponse.expRate *= GameConfigDao.GetFloat(db, "exp_rate", 1.0);
		communityResponse.dropRate *= GameConfigDao.GetFloat(db, "drop_rate", 1.0);
		communityResponse.mesoRate = GameConfigDao.GetFloat(db, "meso_rate", 1.0);
		NativeCouponRates.Apply(db, cid, communityResponse, DateTime.Now);
		return communityResponse;
	}

	private CommunityResponse Read(CharRecord c)
	{
		CommunityResponse communityResponse = Rates((int)c.id);
		communityResponse.meso = c.meso;
		communityResponse.map = c.map;
		communityResponse.guildId = c.guildid;
		DbRow dbRow = Guild(c.guildid);
		if (dbRow != null)
		{
			communityResponse.guildName = DbStore.Str(dbRow, "name");
			communityResponse.notice = DbStore.Str(dbRow, "notice");
			communityResponse.capacity = DbStore.Int(dbRow, "capacity");
			communityResponse.gp = DbStore.Int(dbRow, "GP");
			for (int i = 0; i < 5; i++)
			{
				communityResponse.titles[i] = DbStore.Str(dbRow, "rank" + (i + 1) + "title");
			}
			foreach (DbRow item in from x in db.Table("characters")
				select DbStore.Int(x, "guildid") == c.guildid)
			{
				communityResponse.guild.Add(Member(Character(DbStore.Int(item, "id"))));
			}
		}
		DbRow dbRow2 = Family((int)c.id);
		if (dbRow2 != null)
		{
			if (ResetDaily(dbRow2))
			{
				db.Save();
			}
			communityResponse.familyId = DbStore.Int(dbRow2, "familyid");
			communityResponse.reputation = DbStore.Int(dbRow2, "reputation");
			communityResponse.todayRep = DbStore.Int(dbRow2, "todaysrep");
			communityResponse.totalRep = DbStore.Int(dbRow2, "totalreputation");
			communityResponse.precepts = DbStore.Str(dbRow2, "precepts");
			foreach (DbRow item2 in FamilyMembers(communityResponse.familyId))
			{
				CharRecord charRecord = Character(DbStore.Int(item2, "cid"));
				if (charRecord != null)
				{
					CommunityMember communityMember = Member(charRecord);
					communityMember.senior = DbStore.Int(item2, "seniorid");
					communityResponse.family.Add(communityMember);
				}
			}
			foreach (DbRow item3 in from x in db.Table("family_entitlement")
				select DbStore.Int(x, "charid") == c.id)
			{
				communityResponse.used.Add(DbStore.Int(item3, "entitlementid"));
			}
		}
		summons.TryGetValue((int)c.id, out communityResponse.summonFrom);
		guildInvites.TryGetValue((int)c.id, out communityResponse.guildInvite);
		familyInvites.TryGetValue((int)c.id, out communityResponse.familyInvite);
		foreach (KeyValuePair<int, GuildFormation> formation in formations)
		{
			if (formation.Value.expires > Now && formation.Value.members.Contains((int)c.id) && !formation.Value.accepted.Contains((int)c.id))
			{
				communityResponse.guildConfirmation = formation.Key;
			}
		}
		if (boards.TryGetValue((int)c.id, out var value))
		{
			communityResponse.board = value;
		}
		return communityResponse;
	}

	private CommunityMember Member(CharRecord c)
	{
		return new CommunityMember
		{
			id = (int)c.id,
			name = c.name,
			level = c.level,
			rank = c.guildrank,
			online = online.Contains((int)c.id),
			job = c.job,
			map = c.map
		};
	}
}
