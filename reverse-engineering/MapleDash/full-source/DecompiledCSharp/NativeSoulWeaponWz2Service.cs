using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Server.Bot;

public static class NativeSoulWeaponWz2Service
{
	private const int LedgerType = 32;

	private const string LedgerName = "tms282.soulWeaponWz2";

	private static DbRow Row(DbStore db, int cid)
	{
		return db.Table("extend_value").Find((DbRow r) => DbStore.Int(r, "extend_type") == 32 && DbStore.Str(r, "extend_id") == cid.ToString(CultureInfo.InvariantCulture) && DbStore.Str(r, "extend_name") == "tms282.soulWeaponWz2");
	}

	public static bool IsBound(DbStore db, int cid, long itemId)
	{
		if (itemId > 0)
		{
			return Read(db, cid).weapon == itemId;
		}
		return false;
	}

	public static int SkillSeconds(int grade, string key)
	{
		return SoulWeaponWz2Catalog.Int(SoulWeaponWz2Catalog.Map(SoulWeaponWz2Catalog.Skill(grade), "common"), key);
	}

	private static long Long(Dictionary<string, object> d, string k)
	{
		if (!d.TryGetValue(k, out var value) || !long.TryParse(value?.ToString(), out var result))
		{
			return 0L;
		}
		return result;
	}

	public static SoulWeaponWz2State Read(DbStore db, int cid)
	{
		SoulWeaponWz2State soulWeaponWz2State = new SoulWeaponWz2State();
		DbRow dbRow = Row(db, cid);
		Dictionary<string, object> dictionary = ((dbRow == null) ? null : (MiniJsonDb.Parse(DbStore.Str(dbRow, "extend_value")) as Dictionary<string, object>));
		if (dictionary == null)
		{
			return soulWeaponWz2State;
		}
		soulWeaponWz2State.weapon = Long(dictionary, "weapon");
		soulWeaponWz2State.revision = Long(dictionary, "revision");
		soulWeaponWz2State.activeUntil = Long(dictionary, "activeUntil");
		soulWeaponWz2State.cooldownUntil = Long(dictionary, "cooldownUntil");
		soulWeaponWz2State.level = SoulWeaponWz2Catalog.Int(dictionary, "level", 1);
		soulWeaponWz2State.grade = SoulWeaponWz2Catalog.Int(dictionary, "grade", 1);
		soulWeaponWz2State.exp = SoulWeaponWz2Catalog.Int(dictionary, "exp");
		soulWeaponWz2State.fragments = SoulWeaponWz2Catalog.Int(dictionary, "fragments");
		soulWeaponWz2State.option = SoulWeaponWz2Catalog.Int(dictionary, "option");
		soulWeaponWz2State.unlocked = Long(dictionary, "unlocked") != 0;
		soulWeaponWz2State.converted = Long(dictionary, "converted") != 0;
		soulWeaponWz2State.week = SoulWeaponWz2Catalog.Text(dictionary, "week");
		Dictionary<string, object> dictionary2 = SoulWeaponWz2Catalog.Map(dictionary, "weekly");
		if (dictionary2 != null)
		{
			foreach (KeyValuePair<string, object> item in dictionary2)
			{
				soulWeaponWz2State.weekly[item.Key] = Convert.ToInt32(item.Value);
			}
		}
		if (dictionary.TryGetValue("cleared", out var value) && value is List<object> list)
		{
			foreach (object item2 in list)
			{
				soulWeaponWz2State.cleared.Add(Convert.ToInt32(item2));
			}
		}
		Dictionary<string, object> dictionary3 = SoulWeaponWz2Catalog.Map(dictionary, "souls");
		if (dictionary3 != null)
		{
			foreach (KeyValuePair<string, object> item3 in dictionary3)
			{
				if (long.TryParse(item3.Key, out var result) && result > 0 && item3.Value is Dictionary<string, object> d)
				{
					soulWeaponWz2State.souls[result] = new SoulGrowth
					{
						level = SoulWeaponWz2Catalog.Int(d, "level", 1),
						grade = SoulWeaponWz2Catalog.Int(d, "grade", 1),
						exp = SoulWeaponWz2Catalog.Int(d, "exp"),
						option = SoulWeaponWz2Catalog.Int(d, "option"),
						converted = (Long(d, "converted") != 0)
					};
				}
			}
		}
		if (soulWeaponWz2State.souls.Count == 0 && soulWeaponWz2State.weapon > 0)
		{
			soulWeaponWz2State.souls[soulWeaponWz2State.weapon] = new SoulGrowth
			{
				level = soulWeaponWz2State.level,
				grade = soulWeaponWz2State.grade,
				exp = soulWeaponWz2State.exp,
				option = soulWeaponWz2State.option,
				converted = soulWeaponWz2State.converted
			};
		}
		soulWeaponWz2State.Mirror();
		return soulWeaponWz2State;
	}

	private static void Save(DbStore db, int cid, SoulWeaponWz2State s, long now)
	{
		StringBuilder text = new StringBuilder("{");
		Number("weapon", s.weapon);
		Number("revision", s.revision);
		Number("activeUntil", s.activeUntil);
		Number("cooldownUntil", s.cooldownUntil);
		s.Mirror();
		Number("level", s.level);
		Number("grade", s.grade);
		Number("exp", s.exp);
		Number("fragments", s.fragments);
		Number("option", s.option);
		Number("unlocked", s.unlocked ? 1 : 0);
		Number("converted", s.converted ? 1 : 0);
		text.Append(",\"week\":\"").Append(s.week).Append("\",\"weekly\":{");
		bool flag = true;
		foreach (KeyValuePair<string, int> item in s.weekly)
		{
			if (!flag)
			{
				text.Append(',');
			}
			flag = false;
			text.Append('"').Append(item.Key).Append("\":")
				.Append(item.Value.ToString(CultureInfo.InvariantCulture));
		}
		text.Append("},\"souls\":{");
		bool flag2 = true;
		foreach (KeyValuePair<long, SoulGrowth> soul in s.souls)
		{
			if (soul.Value != null)
			{
				if (!flag2)
				{
					text.Append(',');
				}
				flag2 = false;
				text.Append('"').Append(soul.Key.ToString(CultureInfo.InvariantCulture)).Append("\":{\"level\":")
					.Append(soul.Value.level.ToString(CultureInfo.InvariantCulture))
					.Append(",\"grade\":")
					.Append(soul.Value.grade.ToString(CultureInfo.InvariantCulture))
					.Append(",\"exp\":")
					.Append(soul.Value.exp.ToString(CultureInfo.InvariantCulture))
					.Append(",\"option\":")
					.Append(soul.Value.option.ToString(CultureInfo.InvariantCulture))
					.Append(",\"converted\":")
					.Append(soul.Value.converted ? "1" : "0")
					.Append('}');
			}
		}
		text.Append("},\"cleared\":[").Append(string.Join(",", s.cleared)).Append("]}");
		DbRow dbRow = Row(db, cid);
		if (dbRow == null)
		{
			db.Table("extend_value").Insert(new DbRow
			{
				["extend_type"] = 32L,
				["extend_id"] = cid.ToString(CultureInfo.InvariantCulture),
				["extend_name"] = "tms282.soulWeaponWz2",
				["extend_value"] = text.ToString(),
				["create_time"] = now,
				["update_time"] = now
			});
		}
		else
		{
			dbRow["extend_value"] = text.ToString();
			dbRow["update_time"] = now;
		}
		void Number(string key, long value)
		{
			if (text.Length > 1)
			{
				text.Append(',');
			}
			text.Append('"').Append(key).Append("\":")
				.Append(value.ToString(CultureInfo.InvariantCulture));
		}
	}

	private static SoulResponse Reject(string text, SoulWeaponWz2State s = null)
	{
		return new SoulResponse
		{
			ok = false,
			error = text,
			modern = s
		};
	}

	public static SoulResponse Handle(DbStore db, SoulRequest r, Random random = null, DateTimeOffset? instant = null)
	{
		if (db == null || r == null)
		{
			return Reject("无效灵魂武器请求");
		}
		lock (db)
		{
			CharRecord charRecord = CharactersDao.FindById(db, r.characterId);
			if (charRecord == null)
			{
				return Reject("角色不存在");
			}
			DateTimeOffset now = instant ?? DateTimeOffset.UtcNow;
			long num = now.ToUnixTimeMilliseconds();
			SoulWeaponWz2State s = Read(db, r.characterId);
			if (r.operation == SoulOperation.ReadModern)
			{
				return new SoulResponse
				{
					modern = s
				};
			}
			if (!r.commit)
			{
				return Reject("操作需要确认", s);
			}
			if (r.revision != s.revision)
			{
				return Reject("灵魂武器状态已变化，请重新打开面板", s);
			}
			if (charRecord.level < 200)
			{
				return Reject("角色达到200级后才能解锁灵魂武器", s);
			}
			if (r.operation < SoulOperation.ActivateModern || r.operation > SoulOperation.CastModern)
			{
				return Reject("无效的新版灵魂武器操作", s);
			}
			DbRow dbRow = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == r.characterId && DbStore.Long(x, "inventoryitemid", 0L) == r.inventoryItemId);
			DbRow dbRow2 = ((dbRow == null) ? null : InventoryDao.EquipmentOf(db, r.inventoryItemId));
			SoulGrowth active = s.Active;
			string text = null;
			if (r.operation == SoulOperation.ActivateModern)
			{
				if (s.weapon != 0L)
				{
					text = "已有启用的灵魂武器，请先停用";
				}
				else if (dbRow == null || dbRow2 == null)
				{
					text = "请放入持有的武器";
				}
				else if (!NativeSoulService.IsWeapon(DbStore.Int(dbRow, "itemid")))
				{
					text = "只有武器才可启用";
				}
				else if (DbStore.Int(dbRow2, "locked") != 0 || (DbStore.Int(dbRow, "flag") & 1) != 0)
				{
					text = "已锁定的武器无法启用";
				}
				else if (DbStore.Long(dbRow, "expiration", -1L) > 0 && DbStore.Long(dbRow, "expiration", 0L) <= num)
				{
					text = "武器已到期";
				}
				else if (BotItemCatalog.Int(BotItemCatalog.Shared.Info(DbStore.Int(dbRow, "itemid")), "reqLevel") + DbStore.Int(dbRow2, "iIncReq") < 150)
				{
					text = "只有150级以上的武器才可启用";
				}
				else if (DbStore.Int(dbRow, "position") <= -100 || NativeAndroidService.IsApSlot(DbStore.Int(dbRow, "position")))
				{
					text = "这件武器不能启用";
				}
			}
			else if (!s.unlocked || s.weapon <= 0 || active == null)
			{
				text = "请先启用灵魂武器";
			}
			else if (r.operation == SoulOperation.LevelModern && (r.amount <= 0 || r.amount > s.fragments || r.amount > RemainingExp(active)))
			{
				text = "碎片数量不足，或超过当前阶级经验上限";
			}
			else if (r.operation == SoulOperation.GradeModern)
			{
				int item = SoulWeaponWz2Catalog.Int(SoulWeaponWz2Catalog.Grade(active.grade), "GradeUpReqQuestID");
				if (active.grade >= 10)
				{
					text = "已达到最高阶级";
				}
				else if (active.level < SoulWeaponWz2Catalog.MaxLevel(active.grade) || active.exp < SoulWeaponWz2Catalog.RequiredExp(active.level + 1))
				{
					text = "当前阶级经验尚未蓄满";
				}
				else if (!s.cleared.Contains(item))
				{
					text = "尚未完成本阶级要求的Boss单人击破";
				}
			}
			else if (r.operation == SoulOperation.RollModern && s.fragments < SoulWeaponWz2Catalog.Int(SoulWeaponWz2Catalog.Grade(active.grade), "OptionRollCost"))
			{
				text = "灵魂碎片不足";
			}
			else if (r.operation == SoulOperation.CastModern)
			{
				DbRow dbRow3 = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == r.characterId && DbStore.Long(x, "inventoryitemid", 0L) == s.weapon);
				if (dbRow3 == null || DbStore.Int(dbRow3, "position") != -11)
				{
					text = "请装备已启用的灵魂武器";
				}
				else if (DbStore.Long(dbRow3, "expiration", -1L) > 0 && DbStore.Long(dbRow3, "expiration", 0L) <= num)
				{
					text = "武器已到期";
				}
				else if (s.cooldownUntil > num)
				{
					text = "灵魂技能仍在冷却";
				}
			}
			if (text != null)
			{
				return Reject(text, s);
			}
			db.BeginTransaction();
			try
			{
				if (r.operation == SoulOperation.ActivateModern)
				{
					s.weapon = r.inventoryItemId;
					s.unlocked = true;
					SoulGrowth soulGrowth = s.Ensure(s.weapon);
					if (!soulGrowth.converted)
					{
						ConvertLegacy(dbRow2, soulGrowth, now);
					}
					string[] array = new string[4] { "soulSocketId", "soulOptionId", "soulItemId", "soulOption" };
					foreach (string key in array)
					{
						dbRow2[key] = 0L;
					}
				}
				else if (r.operation == SoulOperation.DeactivateModern)
				{
					s.weapon = 0L;
					s.activeUntil = 0L;
					s.level = 1;
					s.grade = 1;
					s.exp = 0;
					s.option = 0;
					s.converted = false;
				}
				else if (r.operation == SoulOperation.LevelModern)
				{
					SoulGrowth active2 = s.Active;
					s.fragments -= r.amount;
					active2.exp += r.amount;
					NormalizeExp(active2);
				}
				else if (r.operation == SoulOperation.GradeModern)
				{
					SoulGrowth active3 = s.Active;
					active3.grade++;
					NormalizeExp(active3);
				}
				else if (r.operation == SoulOperation.RollModern)
				{
					SoulGrowth active4 = s.Active;
					List<int> list = SoulWeaponWz2Catalog.RollOptions(active4.grade);
					if (list.Count == 0)
					{
						throw new InvalidOperationException("缺少灵魂烙印候选");
					}
					s.fragments -= SoulWeaponWz2Catalog.Int(SoulWeaponWz2Catalog.Grade(active4.grade), "OptionRollCost");
					active4.option = list[(random ?? new Random()).Next(list.Count)];
				}
				else if (r.operation == SoulOperation.CastModern)
				{
					SoulGrowth active5 = s.Active;
					s.activeUntil = num + 1000L * (long)SkillSeconds(active5.grade, "time");
					s.cooldownUntil = num + 1000L * (long)SkillSeconds(active5.grade, "cooltime");
				}
				s.revision++;
				Save(db, r.characterId, s, num);
				db.Save();
				db.Commit();
			}
			catch
			{
				db.Rollback();
				throw;
			}
			NativeGrowthStats.Invalidate();
			return new SoulResponse
			{
				success = true,
				modern = s,
				inventory = CharService.Load(db, r.characterId).inventory
			};
		}
	}

	public static int OnBossClear(DbStore db, int cid, string fieldSetName, bool solo, DateTimeOffset? instant = null)
	{
		if (db == null || !solo || string.IsNullOrEmpty(fieldSetName))
		{
			return 0;
		}
		lock (db)
		{
			CharRecord charRecord = CharactersDao.FindById(db, cid);
			SoulWeaponWz2State soulWeaponWz2State = Read(db, cid);
			if (charRecord == null || charRecord.level < 200 || !soulWeaponWz2State.unlocked)
			{
				return 0;
			}
			Dictionary<string, object> dictionary = null;
			foreach (KeyValuePair<string, object> boss in SoulWeaponWz2Catalog.Bosses)
			{
				if (boss.Value is Dictionary<string, object> dictionary2 && SoulWeaponWz2Catalog.Text(dictionary2, "FieldSetName") == fieldSetName)
				{
					dictionary = dictionary2;
					break;
				}
			}
			if (dictionary == null)
			{
				return 0;
			}
			DateTimeOffset now = instant ?? DateTimeOffset.UtcNow;
			string text = NativeWeeklyGemstoneService.Week(now);
			if (soulWeaponWz2State.week != text)
			{
				soulWeaponWz2State.week = text;
				soulWeaponWz2State.weekly.Clear();
			}
			string key = SoulWeaponWz2Catalog.Int(dictionary, "BossIcon").ToString(CultureInfo.InvariantCulture);
			soulWeaponWz2State.weekly.TryGetValue(key, out var value);
			int num = SoulWeaponWz2Catalog.Int(dictionary, "FragmentAmount");
			int num2 = Math.Max(0, num - value);
			int num3 = SoulWeaponWz2Catalog.Int(dictionary, "QuestID_GradeUp");
			bool flag = num3 > 0 && !soulWeaponWz2State.cleared.Contains(num3);
			if (num2 == 0 && !flag)
			{
				return 0;
			}
			db.BeginTransaction();
			try
			{
				soulWeaponWz2State.weekly[key] = Math.Max(value, num);
				checked
				{
					soulWeaponWz2State.fragments += num2;
					if (flag)
					{
						soulWeaponWz2State.cleared.Add(num3);
					}
				}
				soulWeaponWz2State.revision++;
				Save(db, cid, soulWeaponWz2State, now.ToUnixTimeMilliseconds());
				db.Save();
				db.Commit();
			}
			catch
			{
				db.Rollback();
				throw;
			}
			NativeGrowthStats.Invalidate();
			return num2;
		}
	}

	public static int RemainingExp(SoulWeaponWz2State s)
	{
		return RemainingExp(s?.Active ?? new SoulGrowth());
	}

	public static int RemainingExp(SoulGrowth g)
	{
		if (g == null || g.level >= 100)
		{
			return 0;
		}
		int num = SoulWeaponWz2Catalog.MaxLevel(g.grade);
		int num2 = -g.exp;
		for (int i = g.level + 1; i <= Math.Min(100, num + 1); i++)
		{
			num2 += SoulWeaponWz2Catalog.RequiredExp(i);
		}
		return Math.Max(0, num2);
	}

	private static void NormalizeExp(SoulGrowth g)
	{
		if (g == null)
		{
			return;
		}
		int num = SoulWeaponWz2Catalog.MaxLevel(g.grade);
		while (g.level < num)
		{
			int num2 = SoulWeaponWz2Catalog.RequiredExp(g.level + 1);
			if (num2 > 0 && g.exp >= num2)
			{
				g.exp -= num2;
				g.level++;
				continue;
			}
			break;
		}
	}

	private static void ConvertLegacy(DbRow equipment, SoulGrowth g, DateTimeOffset now)
	{
		if (g == null)
		{
			return;
		}
		g.converted = true;
		Dictionary<string, object> d = SoulWeaponWz2Catalog.Map(SoulWeaponWz2Catalog.Rules, "TransmitInfo");
		string strA = now.ToOffset(TimeSpan.FromHours(8.0)).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
		if (string.CompareOrdinal(strA, SoulWeaponWz2Catalog.Text(d, "StartTime")) < 0 || string.CompareOrdinal(strA, SoulWeaponWz2Catalog.Text(d, "EndTime")) >= 0)
		{
			return;
		}
		int num = DbStore.Int(equipment, "soulItemId");
		int num2 = SoulWeaponWz2Catalog.Int(SoulWeaponWz2Catalog.Map(d, "GradeConversion"), num.ToString());
		if (num2 <= 0)
		{
			return;
		}
		g.grade = Math.Max(g.grade, num2);
		g.level = Math.Max(g.level, (g.grade - 1) * 10 + 1);
		SoulOption soulOption = SoulCatalog.Of(DbStore.Int(equipment, "soulOption"));
		if (soulOption == null)
		{
			return;
		}
		string key;
		if (soulOption.key == "PAD")
		{
			key = "incPAD";
		}
		else if (soulOption.key == "MAD")
		{
			key = "incMAD";
		}
		else if (soulOption.key == "MHP")
		{
			key = "incMHP";
		}
		else
		{
			key = ((soulOption.key == "MMP") ? "incMMP" : ("inc" + soulOption.key));
		}
		foreach (KeyValuePair<string, object> item in SoulWeaponWz2Catalog.Map(SoulWeaponWz2Catalog.Data, "options"))
		{
			if (int.TryParse(item.Key, out var result) && result / 10000 == g.grade && SoulWeaponWz2Catalog.Int(SoulWeaponWz2Catalog.OptionStats(result), key) == soulOption.value)
			{
				g.option = result;
				break;
			}
		}
	}
}
