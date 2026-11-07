using System;
using System.Collections.Generic;
using System.Text;

public static class NativeLinkService
{
	public sealed class Bonus
	{
		public int activeLinks;

		public int allStatPercent;

		public int finalDamagePercent;

		public int bossDamagePercent;

		public int expPercent;

		public int hp;

		public int mp;
	}

	private sealed class Slot
	{
		public long id;

		public string name;

		public int level;

		public string createdate;
	}

	public const int FirstStageLevel = 150;

	private static List<Slot> AccountCharacters(DbStore db, int accountId, int world)
	{
		List<Slot> list = new List<Slot>();
		foreach (DbRow item in db.Table("characters").Select(DbStore.Eq("accountid", (long)accountId)))
		{
			if (DbStore.Int(item, "world") == world)
			{
				list.Add(new Slot
				{
					id = DbStore.Int(item, "id"),
					name = DbStore.Str(item, "name"),
					level = DbStore.Int(item, "level"),
					createdate = DbStore.Str(item, "createdate")
				});
			}
		}
		list.Sort((Slot a, Slot b) =>
		{
			int num = string.CompareOrdinal(a.createdate, b.createdate);
			return (num == 0) ? a.id.CompareTo(b.id) : num;
		});
		return list;
	}

	private static HashSet<long> LinkedSources(DbStore db, long targetCid)
	{
		HashSet<long> hashSet = new HashSet<long>();
		foreach (DbRow item in db.Table("character_link").Select(DbStore.Eq("target_cid", targetCid)))
		{
			hashSet.Add(DbStore.Int(item, "source_cid"));
		}
		return hashSet;
	}

	private static int CharacterSlots(DbStore db, int accountId)
	{
		DbRow dbRow = db.Table("accounts").Find(DbStore.Eq("id", (long)accountId));
		if (dbRow == null)
		{
			return 3;
		}
		return DbStore.Int(dbRow, "characterslots");
	}

	public static string Overview(DbStore db, long targetCid)
	{
		CharRecord current = CharactersDao.FindById(db, (int)targetCid);
		if (current == null)
		{
			return "当前角色数据不存在，请重新登录后再试。";
		}
		List<Slot> list = AccountCharacters(db, current.accountid, current.world);
		HashSet<long> hashSet = LinkedSources(db, current.id);
		int num = Math.Max(CharacterSlots(db, current.accountid), list.Count);
		int num2 = list.FindIndex((Slot c) => c.id == current.id);
		Bonus bonus = LoadBonus(db, current.id);
		StringBuilder stringBuilder = new StringBuilder("#b#eLink系统#n#k\r\n");
		stringBuilder.Append("当前角色：#b").Append(current.name).Append("#k Lv.")
			.Append(current.level)
			.Append("  槽位 ")
			.Append(num2 + 1)
			.Append("/")
			.Append(num)
			.Append("\r\n");
		stringBuilder.Append("已连接：#r").Append(hashSet.Count).Append("/")
			.Append(Math.Max(0, num2))
			.Append("#k  已生效：#r")
			.Append(bonus.activeLinks)
			.Append("#k\r\n\r\n");
		stringBuilder.Append("#e当前总加成#n：全属性 +").Append(bonus.allStatPercent).Append("%  最终伤害 +")
			.Append(bonus.finalDamagePercent)
			.Append("%\r\n")
			.Append("Boss伤害 +")
			.Append(bonus.bossDamagePercent)
			.Append("%  经验 +")
			.Append(bonus.expPercent)
			.Append("%  HP/MP +")
			.Append(bonus.hp)
			.Append("\r\n\r\n");
		stringBuilder.Append("阶段：Lv.150（3% / 800）  Lv.200（7% / 1500）\r\n").Append("\u3000\u3000\u3000Lv.255（10% / 2000）\r\n\r\n").Append("#eLink槽位#n（只能添加当前角色之前创建的角色）\r\n");
		for (int num3 = 0; num3 < num; num3++)
		{
			stringBuilder.Append("#d").Append(num3 + 1).Append(".#k ");
			if (num3 >= list.Count)
			{
				stringBuilder.Append("#b+ 空槽#k\r\n");
				continue;
			}
			Slot slot = list[num3];
			if (slot.id == current.id)
			{
				stringBuilder.Append("#r◆ ").Append(slot.name).Append(" Lv.")
					.Append(slot.level)
					.Append("（当前角色）#k\r\n");
				continue;
			}
			if (num3 > num2)
			{
				stringBuilder.Append(slot.name).Append(" Lv.").Append(slot.level)
					.Append("（后序角色）\r\n");
				continue;
			}
			int num4 = StagePercent(slot.level);
			int value = StageHpMp(slot.level);
			if (hashSet.Contains(slot.id))
			{
				if (num4 > 0)
				{
					stringBuilder.Append("#g◆ ").Append(slot.name).Append(" Lv.")
						.Append(slot.level)
						.Append(" 已生效（+")
						.Append(num4)
						.Append("% / +")
						.Append(value)
						.Append("）#k\r\n");
				}
				else
				{
					stringBuilder.Append("#k◆ ").Append(slot.name).Append(" Lv.")
						.Append(slot.level)
						.Append(" 已Link，达到Lv.150后生效\r\n");
				}
			}
			else if (num4 > 0)
			{
				stringBuilder.Append(slot.name).Append(" Lv.").Append(slot.level)
					.Append("  #L")
					.Append(slot.id)
					.Append("##b+ 添加Link#k#l\r\n");
			}
			else
			{
				stringBuilder.Append(slot.name).Append(" Lv.").Append(slot.level)
					.Append("（达到Lv.150后可Link）\r\n");
			}
		}
		stringBuilder.Append("\r\n#L-1##b关闭#k#l");
		return stringBuilder.ToString();
	}

	public static string AddLink(DbStore db, long targetCid, int sourceCid)
	{
		if (sourceCid <= 0)
		{
			return "Link角色不存在。";
		}
		CharRecord target = CharactersDao.FindById(db, (int)targetCid);
		CharRecord source = CharactersDao.FindById(db, sourceCid);
		if (target == null)
		{
			return "当前角色数据不存在，请重新登录后再试。";
		}
		if (source == null)
		{
			return "选择的Link角色不存在。";
		}
		if (target.id == source.id)
		{
			return "不能Link当前角色自己。";
		}
		if (target.accountid != source.accountid || target.world != source.world)
		{
			return "只能Link同一账号、同一世界的角色。";
		}
		if (!IsEarlier(source.createdate, source.id, target.createdate, target.id))
		{
			return "只能添加比当前角色更早创建的角色。";
		}
		if (source.level < 150)
		{
			return "该角色需要达到150级后才能添加Link。";
		}
		DbTable dbTable = db.Table("character_link");
		if (dbTable.Find((DbRow r) => DbStore.Int(r, "target_cid") == target.id && DbStore.Int(r, "source_cid") == source.id) != null)
		{
			return "该角色已经添加过Link。";
		}
		dbTable.Insert(new DbRow
		{
			["target_cid"] = target.id,
			["source_cid"] = source.id,
			["created_at"] = DbStore.NowText()
		});
		Bonus bonus = LoadBonus(db, target.id);
		return "已将 #b" + source.name + "#k 添加到Link。\r\n当前总加成：全属性/最终伤害/Boss伤害/经验 +" + bonus.allStatPercent + "% ，HP/MP +" + bonus.hp + "。";
	}

	public static Bonus LoadBonus(DbStore db, long targetCid)
	{
		CharRecord charRecord = CharactersDao.FindById(db, (int)targetCid);
		if (charRecord == null)
		{
			return new Bonus();
		}
		List<int> list = new List<int>();
		foreach (DbRow item in db.Table("character_link").Select(DbStore.Eq("target_cid", targetCid)))
		{
			CharRecord charRecord2 = CharactersDao.FindById(db, DbStore.Int(item, "source_cid"));
			if (charRecord2 != null && charRecord2.accountid == charRecord.accountid && charRecord2.world == charRecord.world && IsEarlier(charRecord2.createdate, charRecord2.id, charRecord.createdate, charRecord.id))
			{
				list.Add(charRecord2.level);
			}
		}
		Bonus bonus = new Bonus();
		foreach (int item2 in list)
		{
			int num = StagePercent(item2);
			if (num != 0)
			{
				bonus.activeLinks++;
				bonus.allStatPercent += num;
				bonus.finalDamagePercent += num;
				bonus.bossDamagePercent += num;
				bonus.expPercent += num;
				bonus.hp += StageHpMp(item2);
				bonus.mp += StageHpMp(item2);
			}
		}
		return bonus;
	}

	public static int StagePercent(int level)
	{
		if (level >= 255)
		{
			return 10;
		}
		if (level >= 200)
		{
			return 7;
		}
		if (level < 150)
		{
			return 0;
		}
		return 3;
	}

	public static int StageHpMp(int level)
	{
		if (level >= 255)
		{
			return 2000;
		}
		if (level >= 200)
		{
			return 1500;
		}
		if (level < 150)
		{
			return 0;
		}
		return 800;
	}

	private static bool IsEarlier(string sourceCreated, long sourceId, string targetCreated, long targetId)
	{
		int num = string.CompareOrdinal(sourceCreated ?? "", targetCreated ?? "");
		if (num >= 0)
		{
			if (num == 0)
			{
				return sourceId < targetId;
			}
			return false;
		}
		return true;
	}
}
