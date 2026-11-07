using System;
using System.Collections.Generic;

public static class NativeHexaMatrixService
{
	private sealed class HexaState
	{
		public long solErda;

		public readonly Dictionary<int, int> skills = new Dictionary<int, int>();

		public readonly Dictionary<int, HexaStatView> stats = new Dictionary<int, HexaStatView>();
	}

	public sealed class ErdaConversion
	{
		public bool ok = true;

		public string error = "";

		public long solErda;
	}

	public static readonly int[] FragmentItems = new int[3] { 4009548, 4009547, 4009550 };

	public const int FaintSolErda = 2638879;

	public const int HexaBooster = 2638603;

	private static readonly int[] ActivateSolErda = new int[4] { 3, 4, 5, 7 };

	private static readonly int[] ActivateFragments = new int[4] { 50, 75, 100, 125 };

	private static readonly int[] MasterySolErda = new int[29]
	{
		1, 1, 1, 1, 1, 1, 2, 2, 5, 2,
		2, 2, 2, 2, 2, 2, 2, 3, 8, 3,
		3, 3, 3, 3, 3, 3, 3, 4, 10
	};

	private static readonly int[] MasteryFragments = new int[29]
	{
		15, 18, 20, 23, 25, 28, 30, 33, 100, 40,
		45, 50, 55, 60, 65, 70, 75, 80, 175, 85,
		90, 95, 100, 105, 110, 115, 120, 125, 250
	};

	private static readonly int[] EnforceSolErda = new int[29]
	{
		1, 1, 1, 2, 2, 2, 3, 3, 8, 3,
		3, 3, 3, 3, 3, 3, 3, 4, 12, 4,
		4, 4, 4, 4, 5, 5, 5, 6, 15
	};

	private static readonly int[] EnforceFragments = new int[29]
	{
		23, 27, 30, 34, 38, 42, 45, 49, 150, 60,
		68, 75, 83, 90, 98, 105, 113, 120, 263, 128,
		135, 143, 150, 158, 165, 173, 180, 188, 375
	};

	private static readonly int[] SkillSolErda = new int[29]
	{
		1, 1, 1, 2, 2, 2, 3, 3, 10, 3,
		3, 4, 4, 4, 4, 4, 4, 5, 15, 5,
		5, 5, 5, 5, 6, 6, 6, 7, 20
	};

	private static readonly int[] SkillFragments = new int[29]
	{
		30, 35, 40, 45, 50, 55, 60, 65, 200, 80,
		90, 100, 110, 120, 130, 140, 150, 160, 350, 170,
		180, 190, 200, 210, 220, 230, 240, 250, 500
	};

	private static readonly int[] CommonSolErda = new int[29]
	{
		2, 2, 2, 3, 3, 3, 5, 5, 14, 5,
		5, 6, 6, 6, 6, 6, 6, 7, 17, 7,
		7, 7, 7, 7, 9, 9, 9, 10, 20
	};

	private static readonly int[] CommonFragments = new int[29]
	{
		38, 44, 50, 57, 63, 69, 75, 82, 300, 110,
		124, 138, 152, 165, 179, 193, 207, 220, 525, 234,
		248, 262, 275, 289, 303, 317, 330, 344, 750
	};

	private static readonly int[] StatFragments = new int[11]
	{
		10, 10, 10, 20, 20, 20, 20, 30, 40, 50,
		50
	};

	public const long StatResetMeso = 10000000L;

	public const long StatChangeMeso = 1000000L;

	public static bool ValidStats(int a, int b, int c)
	{
		if (a >= 0 && a <= 6 && b >= 0 && b <= 6 && c >= 0 && c <= 6 && a != b && a != c && b != c)
		{
			if ((a != 4 || (b != 5 && c != 5)) && (b != 4 || (a != 5 && c != 5)))
			{
				if (c == 4)
				{
					if (a != 5)
					{
						return b != 5;
					}
					return false;
				}
				return true;
			}
			return false;
		}
		return false;
	}

	public static int RollStat(HexaStatView view, Random random)
	{
		double[] array = new double[10] { 0.35, 0.35, 0.35, 0.2, 0.2, 0.2, 0.2, 0.15, 0.1, 0.05 };
		double num = ((view.level1 >= 10) ? 0.0 : array[view.level1]);
		double num2 = random.NextDouble();
		if (num2 < num)
		{
			return 0;
		}
		if (view.level2 >= 10)
		{
			return 2;
		}
		if (view.level3 >= 10)
		{
			return 1;
		}
		if (!(num2 < num + (1.0 - num) / 2.0))
		{
			return 2;
		}
		return 1;
	}

	private static HexaResponse Reject(string error)
	{
		return new HexaResponse
		{
			ok = false,
			error = error
		};
	}

	public static HexaResponse Handle(DbStore db, HexaRequest request, Random random = null)
	{
		if (db == null || request == null)
		{
			return Reject("无效 HEXA 矩阵请求");
		}
		HexaCatalog.Ensure();
		lock (db)
		{
			CharRecord charRecord = CharactersDao.FindById(db, request.characterId);
			if (charRecord == null)
			{
				return Reject("角色不存在");
			}
			int level = charRecord.level;
			int job = charRecord.job;
			HexaState hexaState = Load(db, request.characterId);
			bool flag = HexaCatalog.JobAllowed(job) && (charRecord.gm > 0 || (level >= 260 && QuestStatusDao.StatusOf(db, request.characterId, 38625) == 2));
			HexaResponse hexaResponse = Snapshot(db, charRecord, hexaState, flag);
			if (request.operation == HexaOperation.Snapshot)
			{
				if (!flag)
				{
					hexaResponse.error = LockText(db, request.characterId, level);
				}
			}
			else if (!flag)
			{
				return Reject(LockText(db, request.characterId, level));
			}
			if ((request.operation == HexaOperation.SetStat || request.operation == HexaOperation.EnforceStat || request.operation == HexaOperation.Reset) && request.preset != 0)
			{
				return Reject("保存中的核心只供套用，不能直接修改。");
			}
			switch (request.operation)
			{
			case HexaOperation.Snapshot:
				return hexaResponse;
			case HexaOperation.Activate:
			{
				HexaCore hexaCore2 = HexaCatalog.Core(request.coreId);
				if (hexaCore2 == null)
				{
					return Reject("未知错误。");
				}
				HexaGroup hexaGroup2 = HexaCatalog.Group(job);
				if (hexaGroup2 == null || !hexaGroup2.Contains(hexaCore2.id))
				{
					return Reject("你的职业无法解锁该技能：" + hexaCore2.name + "。");
				}
				Dictionary<int, int> dictionary = NativeMatrixSkills.Levels(db, request.characterId);
				foreach (DbRow item in SkillsDao.ByCharacter(db, request.characterId))
				{
					int key = DbStore.Int(item, "skillid");
					if (!dictionary.ContainsKey(key))
					{
						dictionary[key] = DbStore.Int(item, "skilllevel");
					}
				}
				foreach (KeyValuePair<int, int> requiredSkill in hexaCore2.requiredSkills)
				{
					if (!dictionary.TryGetValue(requiredSkill.Key, out var value4) || value4 < requiredSkill.Value)
					{
						return Reject("前置技能 " + requiredSkill.Key + " 需要 " + requiredSkill.Value + " 级。");
					}
				}
				int num7 = GroupOf(hexaGroup2, hexaCore2.id);
				int num8 = ActivateSolErda[num7];
				int num9 = ActivateFragments[num7];
				long num10 = Fragments(db, request.characterId);
				if (hexaState.solErda < num8)
				{
					return Reject("灵魂艾尔达不足，无法解锁该技能。（需要：" + hexaState.solErda + "/" + num8 + "）");
				}
				if (num10 < num9)
				{
					return Reject("灵魂艾尔达碎片不足，无法解锁该技能。（需要：" + num10 + "/" + num9 + "）");
				}
				if (hexaState.skills.ContainsKey(hexaCore2.id) && hexaState.skills[hexaCore2.id] > 0)
				{
					return Reject("该核心已激活。");
				}
				if (!request.commit)
				{
					return hexaResponse;
				}
				db.BeginTransaction();
				try
				{
					hexaState.solErda -= num8;
					ConsumeFragments(db, request.characterId, num9);
					hexaState.skills[hexaCore2.id] = 1;
					Save(db, request.characterId, hexaState);
					db.Save();
					db.Commit();
				}
				catch
				{
					db.Rollback();
					throw;
				}
				return Snapshot(db, charRecord, Load(db, request.characterId), IsEligible(db, charRecord));
			}
			case HexaOperation.Upgrade:
			{
				HexaCore hexaCore = HexaCatalog.Core(request.coreId);
				if (hexaCore == null)
				{
					return Reject("未知错误。");
				}
				HexaGroup hexaGroup = HexaCatalog.Group(job);
				if (hexaGroup == null || !hexaGroup.Contains(hexaCore.id))
				{
					return Reject("你的职业无法强化该核心。");
				}
				int num = (hexaState.skills.TryGetValue(hexaCore.id, out var value3) ? value3 : 0);
				if (num <= 0)
				{
					return Reject("该核心尚未激活。");
				}
				int num2 = ((request.targetLevel > 0) ? request.targetLevel : (num + 1));
				if (num2 <= num)
				{
					return Reject("目标等级不正确。");
				}
				if (num2 > hexaCore.maxLevel)
				{
					return Reject((num2 >= hexaCore.maxLevel) ? "已经是最高等级。" : "目标等级不正确。");
				}
				int num3 = GroupOf(hexaGroup, hexaCore.id);
				int num4 = 0;
				int num5 = 0;
				for (int i = num; i < num2; i++)
				{
					num4 += SolErdaUpgrade(num3, i);
					num5 += FragmentUpgrade(num3, i);
				}
				long num6 = Fragments(db, request.characterId);
				if (hexaState.solErda < num4)
				{
					return Reject("灵魂艾尔达不足。（需要：" + hexaState.solErda + "/" + num4 + "）");
				}
				if (num6 < num5)
				{
					return Reject("灵魂艾尔达碎片不足。（需要：" + num6 + "/" + num5 + "）");
				}
				if (!request.commit)
				{
					hexaResponse.nextSolErda = num4;
					hexaResponse.nextFragments = num5;
					return hexaResponse;
				}
				db.BeginTransaction();
				try
				{
					hexaState.solErda -= num4;
					ConsumeFragments(db, request.characterId, num5);
					hexaState.skills[hexaCore.id] = num2;
					Save(db, request.characterId, hexaState);
					db.Save();
					db.Commit();
				}
				catch
				{
					db.Rollback();
					throw;
				}
				return Snapshot(db, charRecord, Load(db, request.characterId), IsEligible(db, charRecord));
			}
			case HexaOperation.SetStat:
			case HexaOperation.ChangeStat:
			{
				HexaStatCore hexaStatCore = HexaCatalog.Stat(request.coreId);
				if (hexaStatCore == null)
				{
					return Reject("没有这个属性核心。");
				}
				if (level < hexaStatCore.reqLevel)
				{
					return Reject(hexaStatCore.reqLevel + " 级才能启用这个属性核心。");
				}
				if (hexaStatCore.reqCore != 0 && !HasStat(hexaState, hexaStatCore.reqCore))
				{
					return Reject("需要先激活前置属性核心。");
				}
				if (!ValidStats(request.stat1, request.stat2, request.stat3))
				{
					return Reject("请选择三条不同的有效能力值。");
				}
				if (request.preset < 0 || request.preset > 1)
				{
					return Reject("预设号不正确。");
				}
				bool flag2 = hexaState.stats.TryGetValue(StatKey(hexaStatCore.id, request.preset), out var value6);
				bool flag3 = request.operation == HexaOperation.ChangeStat;
				if (flag3 && !flag2)
				{
					return Reject("该属性核心尚未激活。");
				}
				if (!flag3 & flag2)
				{
					return Reject("该属性核心已激活，请使用变更能力值。");
				}
				if (flag3 && charRecord.meso < 1000000)
				{
					return Reject("变更能力值需要100万枫币。");
				}
				if (!flag3 && (hexaState.solErda < 5 || Fragments(db, request.characterId) < 10))
				{
					return Reject("启用属性核心需要5气息和10碎片。");
				}
				if (!request.commit)
				{
					return hexaResponse;
				}
				db.BeginTransaction();
				try
				{
					if (flag3)
					{
						db.Table("characters").Update(DbStore.Eq("id", (long)request.characterId), new DbRow { ["meso"] = charRecord.meso - 1000000 });
					}
					else
					{
						hexaState.solErda -= 5L;
						ConsumeFragments(db, request.characterId, 10);
					}
					value6 = StatView(hexaState, hexaStatCore.id, request.preset);
					value6.stat1 = request.stat1;
					value6.stat2 = request.stat2;
					value6.stat3 = request.stat3;
					if (!flag3)
					{
						value6.applied = true;
					}
					Save(db, request.characterId, hexaState);
					SaveStats(db, request.characterId, hexaState);
					db.Save();
					db.Commit();
				}
				catch
				{
					db.Rollback();
					throw;
				}
				return Snapshot(db, charRecord, Load(db, request.characterId), IsEligible(db, charRecord));
			}
			case HexaOperation.EnforceStat:
			{
				HexaStatCore hexaStatCore2 = HexaCatalog.Stat(request.coreId);
				if (hexaStatCore2 == null)
				{
					return Reject("没有这个属性核心。");
				}
				if (!HasStat(hexaState, hexaStatCore2.id))
				{
					return Reject("该属性核心尚未激活。");
				}
				HexaStatView hexaStatView2 = StatView(hexaState, hexaStatCore2.id, request.preset);
				if (hexaStatView2 == null)
				{
					return Reject("预设号不正确。");
				}
				if (hexaStatView2.level != hexaStatView2.level1 + hexaStatView2.level2 + hexaStatView2.level3)
				{
					return Reject("旧版核心缺少三条等级记录，请先初始化后强化。");
				}
				if (hexaStatView2.level >= hexaStatCore2.maxLevel)
				{
					return Reject("强化完成。初始化后可再次强化。");
				}
				int num11 = StatFragmentCost(hexaStatView2.level1);
				long num12 = Fragments(db, request.characterId);
				if (num12 < num11)
				{
					return Reject("强化材料不足。（需要：" + num12 + "/" + num11 + "）");
				}
				if (!request.commit)
				{
					hexaResponse.nextFragments = num11;
					return hexaResponse;
				}
				db.BeginTransaction();
				try
				{
					ConsumeFragments(db, request.characterId, num11);
					switch (RollStat(hexaStatView2, random ?? new Random()))
					{
					case 0:
						hexaStatView2.level1++;
						break;
					case 1:
						hexaStatView2.level2++;
						break;
					default:
						hexaStatView2.level3++;
						break;
					}
					hexaStatView2.level = hexaStatView2.level1 + hexaStatView2.level2 + hexaStatView2.level3;
					SaveStats(db, request.characterId, hexaState);
					db.Save();
					db.Commit();
				}
				catch
				{
					db.Rollback();
					throw;
				}
				return Snapshot(db, charRecord, Load(db, request.characterId), IsEligible(db, charRecord));
			}
			case HexaOperation.Preset:
			{
				if (request.preset < 0 || request.preset > 1)
				{
					return Reject("预设号不正确。");
				}
				if (!HasStat(hexaState, request.coreId))
				{
					return Reject("该属性核心尚未激活。");
				}
				if (!request.commit)
				{
					return hexaResponse;
				}
				foreach (HexaStatView value8 in hexaState.stats.Values)
				{
					if (value8.coreId == request.coreId)
					{
						value8.applied = false;
					}
				}
				if (!hexaState.stats.TryGetValue(StatKey(request.coreId, request.preset), out var value5))
				{
					return Reject("该预设尚未保存。");
				}
				value5.applied = true;
				db.BeginTransaction();
				try
				{
					SaveStats(db, request.characterId, hexaState);
					db.Save();
					db.Commit();
				}
				catch
				{
					db.Rollback();
					throw;
				}
				return Snapshot(db, charRecord, Load(db, request.characterId), IsEligible(db, charRecord));
			}
			case HexaOperation.Reset:
			{
				if (!hexaState.stats.TryGetValue(StatKey(request.coreId, request.preset), out var value7))
				{
					return Reject("该属性核心尚未激活。");
				}
				if (value7.level <= 0)
				{
					return Reject("已经是初始状态。");
				}
				bool flag4 = value7.level != value7.level1 + value7.level2 + value7.level3;
				if (!flag4 && value7.level < 10)
				{
					return Reject("核心等级10以上才能初始化。");
				}
				if (!flag4 && charRecord.meso < 10000000)
				{
					return Reject("初始化需要1000万枫币。");
				}
				if (!request.commit)
				{
					return hexaResponse;
				}
				db.BeginTransaction();
				try
				{
					db.Table("characters").Update(DbStore.Eq("id", (long)request.characterId), new DbRow { ["meso"] = charRecord.meso - (flag4 ? 0 : 10000000) });
					value7.level1 = (value7.level2 = (value7.level3 = 0));
					value7.level = 0;
					value7.weight = 0;
					SaveStats(db, request.characterId, hexaState);
					db.Save();
					db.Commit();
				}
				catch
				{
					db.Rollback();
					throw;
				}
				return Snapshot(db, charRecord, Load(db, request.characterId), IsEligible(db, charRecord));
			}
			case HexaOperation.SaveStat:
			{
				if (!hexaState.stats.TryGetValue(StatKey(request.coreId, 0), out var value) || value.level != 20 || value.level != value.level1 + value.level2 + value.level3)
				{
					return Reject("属性核心20级才能保存。");
				}
				if (!request.commit)
				{
					return hexaResponse;
				}
				db.BeginTransaction();
				try
				{
					bool applied = hexaState.stats.TryGetValue(StatKey(request.coreId, 1), out var value2) && value2.applied;
					HexaStatView hexaStatView = StatView(hexaState, request.coreId, 1);
					hexaStatView.stat1 = value.stat1;
					hexaStatView.stat2 = value.stat2;
					hexaStatView.stat3 = value.stat3;
					hexaStatView.level1 = value.level1;
					hexaStatView.level2 = value.level2;
					hexaStatView.level3 = value.level3;
					hexaStatView.level = value.level;
					hexaStatView.weight = value.weight;
					hexaStatView.applied = applied;
					SaveStats(db, request.characterId, hexaState);
					db.Save();
					db.Commit();
				}
				catch
				{
					db.Rollback();
					throw;
				}
				return Snapshot(db, charRecord, Load(db, request.characterId), IsEligible(db, charRecord));
			}
			case HexaOperation.Rename:
				return Reject("原端操作8为二级密码，不是核心重命名。");
			default:
				return Reject("不支援的 HEXA 矩阵操作。");
			}
		}
	}

	public static bool IsEligible(DbStore db, CharRecord character)
	{
		if (character != null && HexaCatalog.JobAllowed(character.job))
		{
			if (character.gm <= 0)
			{
				if (character.level >= 260)
				{
					return QuestStatusDao.StatusOf(db, (int)character.id, 38625) == 2;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private static string LockText(DbStore db, int characterId, int level)
	{
		if (level >= 260)
		{
			if (QuestStatusDao.StatusOf(db, characterId, 38625) == 2)
			{
				return "当前职业无法使用 HEXA 矩阵";
			}
			return "尚未完成 [6转] HEXA 矩阵 任务";
		}
		return "260 级才能使用 HEXA 矩阵";
	}

	private static int GroupOf(HexaGroup group, int coreId)
	{
		if (Array.IndexOf(group.mastery, coreId) >= 0)
		{
			return 0;
		}
		if (Array.IndexOf(group.enforce, coreId) >= 0)
		{
			return 1;
		}
		if (Array.IndexOf(group.skill, coreId) >= 0)
		{
			return 2;
		}
		return 3;
	}

	private static int SolErdaUpgrade(int group, int level)
	{
		int[] array = group switch
		{
			2 => SkillSolErda, 
			1 => EnforceSolErda, 
			0 => MasterySolErda, 
			_ => CommonSolErda, 
		};
		if (level < 1 || level > array.Length)
		{
			return 20;
		}
		return array[level - 1];
	}

	private static int FragmentUpgrade(int group, int level)
	{
		int[] array = group switch
		{
			2 => SkillFragments, 
			1 => EnforceFragments, 
			0 => MasteryFragments, 
			_ => CommonFragments, 
		};
		if (level < 1 || level > array.Length)
		{
			return 20;
		}
		return array[level - 1];
	}

	public static void ActivateCost(int group, out int solErda, out int fragments)
	{
		int num = ((group >= 0) ? ((group > 3) ? 3 : group) : 0);
		solErda = ActivateSolErda[num];
		fragments = ActivateFragments[num];
	}

	public static void UpgradeCost(int group, int level, out int solErda, out int fragments)
	{
		if (level < 1 || level > 29)
		{
			solErda = 0;
			fragments = 0;
		}
		else
		{
			solErda = SolErdaUpgrade(group, level);
			fragments = FragmentUpgrade(group, level);
		}
	}

	public static int StatFragmentCost(int level)
	{
		if (level < 0 || level >= StatFragments.Length)
		{
			return 0;
		}
		return StatFragments[level];
	}

	public static long Fragments(DbStore db, int characterId)
	{
		long num = 0L;
		int[] fragmentItems = FragmentItems;
		foreach (int itemId in fragmentItems)
		{
			num += InventoryDao.CountOf(db, characterId, itemId);
		}
		return num;
	}

	private static void ConsumeFragments(DbStore db, int characterId, int need)
	{
		int[] fragmentItems = FragmentItems;
		foreach (int itemId in fragmentItems)
		{
			if (need > 0)
			{
				int num = Math.Min(InventoryDao.CountOf(db, characterId, itemId), need);
				if (num > 0)
				{
					InventoryDao.ConsumeItem(db, characterId, itemId, num);
				}
				need -= num;
				continue;
			}
			break;
		}
	}

	private static HexaState Load(DbStore db, int characterId)
	{
		HexaState hexaState = new HexaState();
		DbRow dbRow = db.Table("hexa_state").Find(DbStore.Eq("characterid", (long)characterId));
		if (dbRow != null)
		{
			hexaState.solErda = DbStore.Long(dbRow, "solerda", 0L);
		}
		foreach (DbRow item in db.Table("hexa_skills").Select(DbStore.Eq("characterid", (long)characterId)))
		{
			hexaState.skills[DbStore.Int(item, "coreid")] = DbStore.Int(item, "level");
		}
		foreach (DbRow item2 in db.Table("hexa_stats").Select(DbStore.Eq("characterid", (long)characterId)))
		{
			int coreId = DbStore.Int(item2, "coreid");
			int preset = DbStore.Int(item2, "preset");
			hexaState.stats[StatKey(coreId, preset)] = new HexaStatView
			{
				coreId = coreId,
				preset = preset,
				level = DbStore.Int(item2, "level"),
				stat1 = DbStore.Int(item2, "stat1"),
				stat2 = DbStore.Int(item2, "stat2"),
				stat3 = DbStore.Int(item2, "stat3"),
				weight = DbStore.Int(item2, "weight"),
				level1 = DbStore.Int(item2, "level1"),
				level2 = DbStore.Int(item2, "level2"),
				level3 = DbStore.Int(item2, "level3"),
				applied = (DbStore.Int(item2, "applied") != 0)
			};
		}
		return hexaState;
	}

	private static int StatKey(int coreId, int preset)
	{
		return coreId * 4 + (preset & 3);
	}

	private static bool HasStat(HexaState state, int coreId)
	{
		for (int i = 0; i < 4; i++)
		{
			if (state.stats.ContainsKey(StatKey(coreId, i)))
			{
				return true;
			}
		}
		return false;
	}

	private static HexaStatView StatView(HexaState state, int coreId, int preset)
	{
		int key = StatKey(coreId, preset);
		if (!state.stats.TryGetValue(key, out var value))
		{
			value = new HexaStatView
			{
				coreId = coreId,
				preset = preset
			};
			state.stats[key] = value;
		}
		return value;
	}

	private static void Save(DbStore db, int characterId, HexaState state)
	{
		DbTable dbTable = db.Table("hexa_state");
		DbRow dbRow = new DbRow { ["solerda"] = state.solErda };
		if (dbTable.Update(DbStore.Eq("characterid", (long)characterId), dbRow) == 0)
		{
			dbRow["characterid"] = (long)characterId;
			dbTable.Insert(dbRow);
		}
		DbTable dbTable2 = db.Table("hexa_skills");
		dbTable2.Delete(DbStore.Eq("characterid", (long)characterId));
		foreach (KeyValuePair<int, int> skill in state.skills)
		{
			if (skill.Value > 0)
			{
				dbTable2.Insert(new DbRow
				{
					["characterid"] = (long)characterId,
					["coreid"] = (long)skill.Key,
					["level"] = (long)skill.Value
				});
			}
		}
		NativeMatrixSkills.Sync(db, characterId);
	}

	private static void SaveStats(DbStore db, int characterId, HexaState state)
	{
		DbTable dbTable = db.Table("hexa_stats");
		dbTable.Delete(DbStore.Eq("characterid", (long)characterId));
		foreach (HexaStatView value in state.stats.Values)
		{
			dbTable.Insert(new DbRow
			{
				["characterid"] = (long)characterId,
				["coreid"] = (long)value.coreId,
				["preset"] = (long)value.preset,
				["level"] = (long)value.level,
				["stat1"] = (long)value.stat1,
				["stat2"] = (long)value.stat2,
				["stat3"] = (long)value.stat3,
				["weight"] = (long)value.weight,
				["level1"] = (long)value.level1,
				["level2"] = (long)value.level2,
				["level3"] = (long)value.level3,
				["applied"] = (long)(value.applied ? 1 : 0)
			});
		}
	}

	public static List<HexaStatView> Stats(DbStore db, int characterId)
	{
		return new List<HexaStatView>(Load(db, characterId).stats.Values);
	}

	private static HexaResponse Snapshot(DbStore db, CharRecord character, HexaState state, bool eligible)
	{
		int job = character.job;
		HexaResponse hexaResponse = new HexaResponse
		{
			level = character.level,
			job = job,
			eligible = eligible,
			solErda = state.solErda,
			fragments = Fragments(db, (int)character.id),
			solErdaGaugeMax = HexaCatalog.GaugeMax,
			solErdaGauge = Math.Min(HexaCatalog.GaugeMax, state.solErda)
		};
		HexaGroup hexaGroup = HexaCatalog.Group(job);
		if (hexaGroup != null)
		{
			foreach (int item in hexaGroup.All)
			{
				HexaCore hexaCore = HexaCatalog.Core(item);
				if (hexaCore != null)
				{
					int num = GroupOf(hexaGroup, item);
					int num2 = (state.skills.TryGetValue(item, out var value) ? value : 0);
					hexaResponse.cores.Add(new HexaCoreView
					{
						coreId = item,
						type = hexaCore.type,
						level = num2,
						maxLevel = hexaCore.maxLevel,
						activated = (num2 > 0),
						name = hexaCore.name,
						desc = hexaCore.desc,
						skills = hexaCore.skills,
						group = num
					});
					if (num2 > 0 && num2 < hexaCore.maxLevel)
					{
						hexaResponse.nextSolErda = SolErdaUpgrade(num, num2);
						hexaResponse.nextFragments = FragmentUpgrade(num, num2);
					}
				}
			}
		}
		foreach (HexaStatView value2 in state.stats.Values)
		{
			HexaStatCore hexaStatCore = HexaCatalog.Stat(value2.coreId);
			hexaResponse.stats.Add(new HexaStatView
			{
				coreId = value2.coreId,
				level = value2.level,
				maxLevel = (hexaStatCore?.maxLevel ?? 0),
				reqLevel = (hexaStatCore?.reqLevel ?? 0),
				preset = value2.preset,
				stat1 = value2.stat1,
				stat2 = value2.stat2,
				stat3 = value2.stat3,
				weight = value2.weight,
				level1 = value2.level1,
				level2 = value2.level2,
				level3 = value2.level3,
				applied = value2.applied
			});
		}
		return hexaResponse;
	}

	public static ErdaConversion Convert(DbStore db, int characterId, int itemId, int count)
	{
		ErdaConversion erdaConversion = new ErdaConversion();
		if (db == null)
		{
			erdaConversion.ok = false;
			erdaConversion.error = "无效转换请求";
			return erdaConversion;
		}
		lock (db)
		{
			int num = itemId switch
			{
				2638603 => 1, 
				2638879 => 30, 
				_ => 0, 
			};
			if (num == 0)
			{
				erdaConversion.ok = false;
				erdaConversion.error = "没有这种兑换。";
				return erdaConversion;
			}
			if (count <= 0)
			{
				erdaConversion.ok = false;
				erdaConversion.error = "转换数量不正确。";
				return erdaConversion;
			}
			DbRow dbRow = db.Table("hexa_state").Find(DbStore.Eq("characterid", (long)characterId));
			long num2 = ((dbRow == null) ? 0 : DbStore.Long(dbRow, "solerda", 0L));
			if (num2 < count)
			{
				erdaConversion.ok = false;
				erdaConversion.error = "持有的灵魂艾尔达不足，无法进行转换。";
				return erdaConversion;
			}
			CharRecord charRecord = CharactersDao.FindById(db, characterId);
			if (charRecord == null)
			{
				erdaConversion.ok = false;
				erdaConversion.error = "角色不存在";
				return erdaConversion;
			}
			db.BeginTransaction();
			try
			{
				DbTable dbTable = db.Table("hexa_state");
				DbRow dbRow2 = new DbRow { ["solerda"] = num2 - count };
				if (dbTable.Update(DbStore.Eq("characterid", (long)characterId), dbRow2) == 0)
				{
					dbRow2["characterid"] = (long)characterId;
					dbTable.Insert(dbRow2);
				}
				NativeInventoryService.AddItems(db, charRecord, itemId, num * count, -1L);
				db.Save();
				db.Commit();
			}
			catch
			{
				db.Rollback();
				throw;
			}
			erdaConversion.solErda = num2 - count;
			return erdaConversion;
		}
	}
}
