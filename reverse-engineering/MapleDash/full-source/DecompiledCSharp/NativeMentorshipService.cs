using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class NativeMentorshipService
{
	private sealed class Wallet
	{
		public long apprenticeCoin;

		public long virtueCoin;

		public long totalPoints;

		public long weeklyPoints;
	}

	private sealed class Relation
	{
		public long id;

		public long masterCid;

		public long apprenticeCid;

		public long guildid;

		public string masterName;

		public string apprenticeName;

		public int status;

		public int rewardStep;

		public long totalPoints;

		public long lastMasterPower;

		public long lastApprenticePower;
	}

	public const int StatusActive = 0;

	public const int StatusGraduated = 1;

	public const int StatusCancelled = 2;

	public const int MasterMinLevel = 200;

	public const int MinDuelStake = 100;

	public const int GpqClearPoints = 100;

	private const int WeeklyUnitPoints = 100;

	private const int WeeklyMasterCoin = 8;

	private const int WeeklyApprenticeCoin = 12;

	private static readonly int[] StageThresholds = new int[5] { 100, 300, 600, 1000, 1500 };

	private static readonly int[] StageMasterVirtue = new int[5] { 10, 20, 35, 55, 80 };

	private static readonly int[] StageApprenticeCoin = new int[5] { 20, 40, 70, 110, 160 };

	public static Func<long> OnlinePowerOfActiveCharacter;

	private static Wallet EnsureWallet(DbStore db, long characterId)
	{
		DbRow dbRow = db.Table("mentorship_wallet").Find(DbStore.Eq("characterid", characterId));
		if (dbRow == null)
		{
			dbRow = new DbRow
			{
				["characterid"] = characterId,
				["apprentice_coin"] = 0L,
				["virtue_coin"] = 0L,
				["total_points"] = 0L,
				["weekly_points"] = 0L
			};
			db.Table("mentorship_wallet").Insert(dbRow);
		}
		return new Wallet
		{
			apprenticeCoin = DbStore.Int(dbRow, "apprentice_coin"),
			virtueCoin = DbStore.Int(dbRow, "virtue_coin"),
			totalPoints = DbStore.Int(dbRow, "total_points"),
			weeklyPoints = DbStore.Int(dbRow, "weekly_points")
		};
	}

	private static void AddWallet(DbStore db, long characterId, long apprenticeDelta, long virtueDelta, long pointsDelta)
	{
		Wallet wallet = EnsureWallet(db, characterId);
		DbRow dbRow = db.Table("mentorship_wallet").Find(DbStore.Eq("characterid", characterId));
		dbRow["apprentice_coin"] = wallet.apprenticeCoin + apprenticeDelta;
		dbRow["virtue_coin"] = wallet.virtueCoin + virtueDelta;
		dbRow["total_points"] = wallet.totalPoints + pointsDelta;
	}

	public static long CoinsOf(DbStore db, long characterId, bool virtue)
	{
		if (!virtue)
		{
			return EnsureWallet(db, characterId).apprenticeCoin;
		}
		return EnsureWallet(db, characterId).virtueCoin;
	}

	public static bool SpendCoins(DbStore db, long characterId, bool virtue, long cost)
	{
		Wallet wallet = EnsureWallet(db, characterId);
		long num = (virtue ? wallet.virtueCoin : wallet.apprenticeCoin);
		if (num < cost)
		{
			return false;
		}
		db.Table("mentorship_wallet").Find(DbStore.Eq("characterid", characterId))[virtue ? "virtue_coin" : "apprentice_coin"] = num - cost;
		return true;
	}

	private static List<Relation> RelationsOf(DbStore db, long characterId)
	{
		List<Relation> list = new List<Relation>();
		foreach (DbRow row in db.Table("mentorship_relation").rows)
		{
			if (DbStore.Int(row, "master_cid") == characterId || DbStore.Int(row, "apprentice_cid") == characterId)
			{
				int num = DbStore.Int(row, "status");
				if (num != 2)
				{
					list.Add(new Relation
					{
						id = DbStore.Int(row, "id"),
						masterCid = DbStore.Int(row, "master_cid"),
						apprenticeCid = DbStore.Int(row, "apprentice_cid"),
						guildid = DbStore.Int(row, "guildid"),
						masterName = DbStore.Str(row, "master_name"),
						apprenticeName = DbStore.Str(row, "apprentice_name"),
						status = num,
						rewardStep = DbStore.Int(row, "reward_step"),
						totalPoints = DbStore.Int(row, "total_points"),
						lastMasterPower = DbStore.Int(row, "last_master_power"),
						lastApprenticePower = DbStore.Int(row, "last_apprentice_power")
					});
				}
			}
		}
		return list;
	}

	private static Relation FindRelation(DbStore db, long characterId, bool activeOnly = true)
	{
		foreach (Relation item in RelationsOf(db, characterId))
		{
			if (!activeOnly || item.status == 0)
			{
				return item;
			}
		}
		return null;
	}

	public static long OfflinePower(DbStore db, long characterId)
	{
		CharRecord charRecord = CharactersDao.FindById(db, (int)characterId);
		if (charRecord == null)
		{
			return 0L;
		}
		long num = CashPower(db, charRecord.accountid);
		return (long)charRecord.level * 1000L + (long)charRecord.str * 10L + (long)charRecord.dex * 10L + (long)charRecord.int_ * 10L + (long)charRecord.luk * 10L + (long)charRecord.maxhp / 2L + (long)charRecord.maxmp / 2L + num / 100;
	}

	private static long CashPower(DbStore db, int accountId)
	{
		DbRow dbRow = db.Table("accounts").Find(DbStore.Eq("id", (long)accountId));
		if (dbRow == null)
		{
			return 0L;
		}
		return Math.Max(0, DbStore.Int(dbRow, "nxCredit")) + Math.Max(0, DbStore.Int(dbRow, "maplePoint")) + Math.Max(0, DbStore.Int(dbRow, "nxPrepaid"));
	}

	public static long CashOfActiveAccount()
	{
		if (!GameSession.InGame)
		{
			return 0L;
		}
		CharRecord charRecord = GameSession.Character?.record;
		if (charRecord != null)
		{
			return CashPower(GameServer.Local.Db, charRecord.accountid);
		}
		return 0L;
	}

	private static void CheckAutoGraduation(DbStore db, long characterId)
	{
		Relation relation = FindRelation(db, characterId);
		if (relation == null)
		{
			return;
		}
		DbRow dbRow = db.Table("mentorship_relation").Find(DbStore.Eq("id", relation.id));
		long num = OfflinePower(db, relation.masterCid);
		long num2 = OfflinePower(db, relation.apprenticeCid);
		if (OnlinePowerOfActiveCharacter != null && GameSession.InGame)
		{
			long num3 = OnlinePowerOfActiveCharacter();
			if (relation.masterCid == GameSession.CharacterId)
			{
				num = num3;
			}
			else if (relation.apprenticeCid == GameSession.CharacterId)
			{
				num2 = num3;
			}
		}
		dbRow["last_master_power"] = num;
		dbRow["last_apprentice_power"] = num2;
		if (num > 0 && num2 > num)
		{
			dbRow["status"] = 1L;
			AddWallet(db, relation.masterCid, 0L, 50L, 0L);
			AddWallet(db, relation.apprenticeCid, 100L, 0L, 0L);
			NativeSystemMessage.Drop(5, "[师徒] 徒弟 " + relation.apprenticeName + " 战力 " + num2 + " 已超过师傅 " + relation.masterName + " 战力 " + num + "，自动出师。");
		}
	}

	public static string Overview(DbStore db, long characterId)
	{
		FinishExpiredDuels(db);
		CheckAutoGraduation(db, characterId);
		Wallet wallet = EnsureWallet(db, characterId);
		List<Relation> list = RelationsOf(db, characterId);
		StringBuilder stringBuilder = new StringBuilder("#e师徒系统#n\r\n\r\n");
		stringBuilder.Append("师徒币：#b").Append(wallet.apprenticeCoin).Append("#k\r\n");
		stringBuilder.Append("师德币：#b").Append(wallet.virtueCoin).Append("#k\r\n");
		stringBuilder.Append("累计积分：#b").Append(wallet.totalPoints).Append("#k，本周积分：#b")
			.Append(wallet.weeklyPoints)
			.Append("#k\r\n\r\n");
		if (list.Count == 0)
		{
			stringBuilder.Append("当前没有进行中的师徒关系。");
		}
		else
		{
			foreach (Relation item in list)
			{
				bool flag = item.masterCid == characterId;
				stringBuilder.Append(flag ? "师傅：" : "徒弟：").Append("#b").Append(flag ? item.apprenticeName : item.masterName)
					.Append("#k  积分：#r")
					.Append(item.totalPoints)
					.Append("#k  阶段：")
					.Append(item.rewardStep)
					.Append("/")
					.Append(StageThresholds.Length)
					.Append("\r\n");
			}
		}
		return stringBuilder.ToString();
	}

	public static string CreateRelation(DbStore db, long actorId, string targetName)
	{
		CharRecord charRecord = CharactersDao.FindById(db, (int)actorId);
		if (charRecord == null)
		{
			return "当前角色数据不存在。";
		}
		DbRow dbRow = db.Table("characters").Find(DbStore.Eq("name", targetName));
		if (dbRow == null)
		{
			return "没有找到角色 #b" + targetName + "#k。";
		}
		long num = DbStore.Int(dbRow, "id");
		if (num == actorId)
		{
			return "不能和自己建立师徒关系。";
		}
		bool flag = charRecord.level >= 200 && DbStore.Int(dbRow, "level") < 200;
		if (!flag && DbStore.Int(dbRow, "level") >= 200 && charRecord.level < 200)
		{
			flag = false;
		}
		long num2 = (flag ? actorId : num);
		long num3 = (flag ? num : actorId);
		CharRecord charRecord2 = CharactersDao.FindById(db, (int)num2);
		CharRecord charRecord3 = CharactersDao.FindById(db, (int)num3);
		if (charRecord2 == null || charRecord3 == null)
		{
			return "角色数据不存在。";
		}
		string text = ValidateCreate(db, charRecord2, charRecord3);
		if (text != null)
		{
			return text;
		}
		return InsertRelation(db, charRecord2, charRecord3);
	}

	private static string ValidateCreate(DbStore db, CharRecord master, CharRecord apprentice)
	{
		if (master.level < 200)
		{
			return "师傅必须达到 200 级以上。点券会参与战力计算，但不能绕过等级条件。";
		}
		if (apprentice.reborns != 0)
		{
			return "徒弟必须是未转生角色。";
		}
		if (master.guildid == 0 || master.guildid != apprentice.guildid)
		{
			return "师傅和徒弟必须在同一个家族。";
		}
		if (FindRelation(db, apprentice.id) != null)
		{
			return "该徒弟已经有进行中的师徒关系。";
		}
		return null;
	}

	private static string InsertRelation(DbStore db, CharRecord master, CharRecord apprentice)
	{
		long num = OfflinePower(db, master.id);
		long num2 = OfflinePower(db, apprentice.id);
		db.Table("mentorship_relation").Insert(new DbRow
		{
			["master_cid"] = master.id,
			["master_accountid"] = (long)master.accountid,
			["master_name"] = master.name,
			["apprentice_cid"] = apprentice.id,
			["apprentice_accountid"] = (long)apprentice.accountid,
			["apprentice_name"] = apprentice.name,
			["guildid"] = (long)master.guildid,
			["status"] = 0L,
			["start_master_power"] = num,
			["start_apprentice_power"] = num2,
			["last_master_power"] = num,
			["last_apprentice_power"] = num2,
			["reward_step"] = 0L,
			["total_points"] = 0L
		});
		EnsureWallet(db, master.id);
		EnsureWallet(db, apprentice.id);
		return "师徒关系建立成功：#b" + master.name + "#k 收 #b" + apprentice.name + "#k 为徒。";
	}

	public static string PartyMasterMenu(DbStore db, long actorId, List<long> onlinePartyMemberIds)
	{
		if (onlinePartyMemberIds == null || onlinePartyMemberIds.Count < 2)
		{
			return "请先和要建立师徒关系的角色组队。";
		}
		StringBuilder stringBuilder = new StringBuilder("#e确认师傅#n\r\n\r\n请选择本队伍中的师傅：\r\n\r\n");
		int num = 0;
		foreach (long onlinePartyMemberId in onlinePartyMemberIds)
		{
			CharRecord charRecord = CharactersDao.FindById(db, (int)onlinePartyMemberId);
			if (charRecord != null && charRecord.level >= 200)
			{
				num++;
				stringBuilder.Append("#L").Append(charRecord.id).Append("##b")
					.Append(charRecord.name)
					.Append("#k  等级 ")
					.Append(charRecord.level)
					.Append("#l\r\n");
			}
		}
		if (num == 0)
		{
			return "队伍中没有达到 200 级的师傅候选。";
		}
		stringBuilder.Append("选择后，队伍中其他符合条件的成员会作为徒弟建立关系。");
		return stringBuilder.ToString();
	}

	public static bool HasPartyMasterCandidate(DbStore db, List<long> onlinePartyMemberIds)
	{
		if (onlinePartyMemberIds == null)
		{
			return false;
		}
		foreach (long onlinePartyMemberId in onlinePartyMemberIds)
		{
			CharRecord charRecord = CharactersDao.FindById(db, (int)onlinePartyMemberId);
			if (charRecord != null && charRecord.level >= 200)
			{
				return true;
			}
		}
		return false;
	}

	public static string CreateFromParty(DbStore db, long actorId, int masterCid, List<long> onlinePartyMemberIds)
	{
		List<long> list = onlinePartyMemberIds ?? new List<long>();
		if (!list.Contains(masterCid))
		{
			return "选择的师傅不在当前队伍中。";
		}
		CharRecord charRecord = CharactersDao.FindById(db, masterCid);
		if (charRecord == null)
		{
			return "选择的Link角色不存在。";
		}
		if (charRecord.level < 200)
		{
			return "师傅必须达到 200 级以上。点券会参与战力计算，但不能绕过等级条件。";
		}
		StringBuilder stringBuilder = new StringBuilder("#e师徒关系建立结果#n\r\n\r\n");
		int num = 0;
		foreach (long item in list)
		{
			if (item != masterCid)
			{
				CharRecord charRecord2 = CharactersDao.FindById(db, (int)item);
				string text = ((charRecord2 == null) ? "角色数据不存在。" : (ValidateCreate(db, charRecord, charRecord2) ?? InsertRelation(db, charRecord, charRecord2)));
				if (text.StartsWith("师徒关系建立成功", StringComparison.Ordinal))
				{
					num++;
				}
				stringBuilder.Append(charRecord2?.name ?? ("角色" + item)).Append("：").Append(text)
					.Append("\r\n");
			}
		}
		if (num == 0)
		{
			stringBuilder.Append("\r\n没有成功建立新的师徒关系。");
		}
		return stringBuilder.ToString();
	}

	public static string Cancel(DbStore db, long characterId)
	{
		Relation relation = FindRelation(db, characterId);
		if (relation == null)
		{
			return "当前没有可解除的师徒关系。";
		}
		db.Table("mentorship_relation").Update(DbStore.Eq("id", relation.id), new DbRow { ["status"] = 2L });
		return "已解除当前进行中的师徒关系。";
	}

	public static string ClaimStages(DbStore db, long characterId)
	{
		CheckAutoGraduation(db, characterId);
		bool flag = false;
		foreach (Relation relation in RelationsOf(db, characterId))
		{
			bool flag2 = relation.masterCid == characterId;
			int num = relation.rewardStep;
			while (num < StageThresholds.Length && relation.totalPoints >= StageThresholds[num])
			{
				string rewardKey = "stage_" + (num + 1);
				DbTable dbTable = db.Table("mentorship_reward_log");
				if (dbTable.Find((DbRow r) => DbStore.Int(r, "relationid") == relation.id && DbStore.Str(r, "reward_key") == rewardKey) != null)
				{
					num++;
					continue;
				}
				dbTable.Insert(new DbRow
				{
					["relationid"] = relation.id,
					["reward_key"] = rewardKey
				});
				if (flag2)
				{
					AddWallet(db, characterId, 0L, StageMasterVirtue[num], 0L);
				}
				else
				{
					AddWallet(db, characterId, StageApprenticeCoin[num], 0L, 0L);
				}
				db.Table("mentorship_relation").Update(DbStore.Eq("id", relation.id), new DbRow { ["reward_step"] = (long)(num + 1) });
				flag = true;
				num++;
			}
		}
		if (!flag)
		{
			return "当前没有可领取的阶段奖励。";
		}
		return "已领取阶段奖励，具体数额见钱包变化。";
	}

	public static string ClaimWeekly(DbStore db, long characterId)
	{
		CheckAutoGraduation(db, characterId);
		string weekKey = WeekKey();
		bool flag = false;
		foreach (Relation relation in RelationsOf(db, characterId))
		{
			if (relation.status == 2)
			{
				continue;
			}
			bool flag2 = relation.masterCid == characterId;
			DbRow dbRow = db.Table("mentorship_weekly_pool").Find((DbRow r) => DbStore.Int(r, "relationid") == relation.id && DbStore.Str(r, "week_key") == weekKey);
			if (dbRow == null || DbStore.Int(dbRow, "points") <= 0)
			{
				continue;
			}
			string text = (flag2 ? "claim_master" : "claim_apprentice");
			if (DbStore.Int(dbRow, text) == 0)
			{
				long num = (long)Math.Max(1, DbStore.Int(dbRow, "points") / 100) * (long)(flag2 ? 8 : 12);
				dbRow[text] = 1L;
				if (flag2)
				{
					AddWallet(db, characterId, 0L, num, 0L);
				}
				else
				{
					AddWallet(db, characterId, num, 0L, 0L);
				}
				flag = true;
			}
		}
		if (!flag)
		{
			return "本周暂无可领取历练池奖励。";
		}
		return "已领取本周历练池奖励，数额见钱包变化。";
	}

	public static string StartDuel(DbStore db, long characterId, long stake)
	{
		if (stake < 100)
		{
			return "对决下注最少需要 100 枚师德币。";
		}
		Relation relation = FindRelation(db, characterId);
		if (relation == null)
		{
			return "需要先建立师徒关系，才能参加对决积分赛。";
		}
		foreach (DbRow row in db.Table("mentorship_duel").rows)
		{
			int num = DbStore.Int(row, "status");
			if ((num == 0 || num == 1) && (DbStore.Int(row, "relation_a") == relation.id || DbStore.Int(row, "relation_b") == relation.id))
			{
				return "你的师徒组合已经有进行中或排队中的对决。";
			}
		}
		if (!SpendCoins(db, characterId, virtue: true, stake))
		{
			return "师德币不足，无法下注。";
		}
		db.Table("mentorship_duel").Insert(new DbRow
		{
			["relation_a"] = relation.id,
			["relation_b"] = 0L,
			["stake"] = stake,
			["points_a"] = 0L,
			["points_b"] = 0L,
			["status"] = 0L,
			["winner_relation"] = 0L,
			["created_at"] = Now(),
			["ends_at"] = 0L,
			["finished_at"] = 0L
		});
		return "已报名 24 小时对决积分赛，等待另一组师徒加入。";
	}

	public static string JoinDuel(DbStore db, long characterId, long stakeLimit)
	{
		Relation relation = FindRelation(db, characterId);
		if (relation == null)
		{
			return "需要先建立师徒关系，才能参加对决积分赛。";
		}
		foreach (DbRow row in db.Table("mentorship_duel").rows)
		{
			if (DbStore.Int(row, "status") != 0 || DbStore.Int(row, "relation_a") == relation.id)
			{
				continue;
			}
			long num = DbStore.Int(row, "stake");
			if (num >= 100 && num <= stakeLimit)
			{
				DbRow dbRow = db.Table("mentorship_relation").Find(DbStore.Eq("id", DbStore.Int(row, "relation_a")));
				if (dbRow != null)
				{
					DbStore.Int(dbRow, "master_cid");
				}
				row["relation_b"] = relation.id;
				row["status"] = 1L;
				row["ends_at"] = Now() + 86400000;
				return "匹配成功，24 小时内获得的师徒积分会计入对决。";
			}
		}
		return "暂时没有可匹配的对决队列。";
	}

	public static string DuelStatus(DbStore db, long characterId)
	{
		Relation relation = FindRelation(db, characterId);
		if (relation == null)
		{
			return "当前没有排队中或进行中的对决。";
		}
		foreach (DbRow row in db.Table("mentorship_duel").rows)
		{
			if (DbStore.Int(row, "relation_a") == relation.id || DbStore.Int(row, "relation_b") == relation.id)
			{
				int num = DbStore.Int(row, "status");
				if (num == 0 || num == 1)
				{
					return "对决状态：" + ((num == 0) ? "排队中" : "进行中") + "\r\nA 组积分：" + DbStore.Int(row, "points_a") + "\r\nB 组积分：" + DbStore.Int(row, "points_b") + "\r\n下注：" + DbStore.Int(row, "stake") + " 师德币";
				}
			}
		}
		return "当前没有排队中或进行中的对决。";
	}

	public static string Ranking(DbStore db, long characterId, string type)
	{
		bool flag = type == "weekly";
		List<(string, long)> list = new List<(string, long)>();
		if (flag)
		{
			string text = WeekKey();
			foreach (DbRow row in db.Table("mentorship_weekly_pool").rows)
			{
				if (DbStore.Str(row, "week_key") != text || DbStore.Int(row, "points") <= 0)
				{
					continue;
				}
				DbRow dbRow = db.Table("mentorship_relation").Find(DbStore.Eq("id", DbStore.Int(row, "relationid")));
				if (dbRow != null)
				{
					int num = DbStore.Int(dbRow, "status");
					if (num == 0 || num == 1)
					{
						list.Add((DbStore.Str(dbRow, "master_name") + " / " + DbStore.Str(dbRow, "apprentice_name"), DbStore.Int(row, "points")));
					}
				}
			}
		}
		else
		{
			foreach (DbRow row2 in db.Table("mentorship_relation").rows)
			{
				int num2 = DbStore.Int(row2, "status");
				if (num2 == 0 || num2 == 1)
				{
					long num3 = DbStore.Int(row2, "total_points");
					if (num3 > 0)
					{
						list.Add((DbStore.Str(row2, "master_name") + " / " + DbStore.Str(row2, "apprentice_name"), num3));
					}
				}
			}
		}
		list.Sort(((string name, long points) a, (string name, long points) b) => b.points.CompareTo(a.points));
		StringBuilder stringBuilder = new StringBuilder(flag ? "#e本周师徒榜#n\r\n\r\n" : "#e累计师徒榜#n\r\n\r\n");
		if (list.Count == 0)
		{
			return stringBuilder.Append("暂无排行数据。").ToString();
		}
		for (int num4 = 0; num4 < list.Count && num4 < 10; num4++)
		{
			stringBuilder.Append(num4 + 1).Append(". #b").Append(list[num4].Item1)
				.Append("#k  ")
				.Append(list[num4].Item2)
				.Append("\r\n");
		}
		return stringBuilder.ToString();
	}

	public static void AddPoints(DbStore db, long relationId, int points, string eventKey)
	{
		DbRow dbRow = db.Table("mentorship_relation").Find(DbStore.Eq("id", relationId));
		if (dbRow == null || DbStore.Int(dbRow, "status") != 0)
		{
			return;
		}
		DbTable dbTable = db.Table("mentorship_event_log");
		if (dbTable.Find((DbRow r) => DbStore.Str(r, "event_key") == eventKey && DbStore.Int(r, "relationid") == relationId) != null)
		{
			return;
		}
		dbTable.Insert(new DbRow
		{
			["event_key"] = eventKey,
			["relationid"] = relationId,
			["points"] = (long)points
		});
		dbRow["total_points"] = DbStore.Int(dbRow, "total_points") + points;
		long characterId = DbStore.Int(dbRow, "master_cid");
		long characterId2 = DbStore.Int(dbRow, "apprentice_cid");
		string weekKey = WeekKey();
		DbRow dbRow2 = db.Table("mentorship_weekly_pool").Find((DbRow r) => DbStore.Int(r, "relationid") == relationId && DbStore.Str(r, "week_key") == weekKey);
		if (dbRow2 == null)
		{
			db.Table("mentorship_weekly_pool").Insert(new DbRow
			{
				["relationid"] = relationId,
				["week_key"] = weekKey,
				["points"] = (long)points,
				["claim_master"] = 0L,
				["claim_apprentice"] = 0L
			});
		}
		else
		{
			dbRow2["points"] = DbStore.Int(dbRow2, "points") + points;
		}
		AddWallet(db, characterId, 0L, points / 10, points);
		AddWallet(db, characterId2, points / 5, 0L, points);
		foreach (DbRow row in db.Table("mentorship_duel").rows)
		{
			if (DbStore.Int(row, "status") == 1)
			{
				if (DbStore.Int(row, "relation_a") == relationId)
				{
					row["points_a"] = DbStore.Int(row, "points_a") + points;
				}
				else if (DbStore.Int(row, "relation_b") == relationId)
				{
					row["points_b"] = DbStore.Int(row, "points_b") + points;
				}
			}
		}
	}

	public static void RecordGpqClear(DbStore db, List<long> participantIds)
	{
		string eventKey = "gpq:" + Now() + ":" + string.Join(",", participantIds ?? new List<long>());
		foreach (long item in participantIds ?? new List<long>())
		{
			foreach (Relation item2 in RelationsOf(db, item))
			{
				if (item2.status == 0)
				{
					AddPoints(db, item2.id, 100, eventKey);
				}
			}
		}
	}

	public static void FinishExpiredDuels(DbStore db)
	{
		long num = Now();
		foreach (DbRow row in db.Table("mentorship_duel").rows)
		{
			if (DbStore.Int(row, "status") == 1 && (DbStore.Int(row, "ends_at") <= num || DbStore.Int(row, "ends_at") == 0))
			{
				long num2 = ((DbStore.Int(row, "points_a") >= DbStore.Int(row, "points_b")) ? DbStore.Int(row, "relation_a") : DbStore.Int(row, "relation_b"));
				long num3 = DbStore.Int(row, "stake");
				DbRow dbRow = db.Table("mentorship_relation").Find(DbStore.Eq("id", num2));
				if (num3 > 0 && dbRow != null)
				{
					AddWallet(db, DbStore.Int(dbRow, "master_cid"), 0L, num3 * 2, 0L);
				}
				row["status"] = 2L;
				row["winner_relation"] = num2;
				row["finished_at"] = num;
			}
		}
	}

	private static string WeekKey()
	{
		DateTime utcNow = DateTime.UtcNow;
		int weekOfYear = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(utcNow, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
		int num = ((utcNow.Month == 1 && weekOfYear >= 52) ? (utcNow.Year - 1) : utcNow.Year);
		return string.Format(CultureInfo.InvariantCulture, "{0:D4}W{1:D2}", num, weekOfYear);
	}

	private static long Now()
	{
		return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
	}
}
