using System;
using System.Collections.Generic;

public static class AbilityCatalog
{
	public const int BaseId = 70000000;

	public const int MaxId = 70000062;

	public const int Presets = 3;

	public const int Lines = 3;

	public const int Line2Level = 30;

	public const int Line3Level = 50;

	public const int LockFirstCost = 3000;

	public const int LockSecondCost = 5000;

	private static readonly AbilityInfo[] all = new AbilityInfo[63]
	{
		Make(70000000, "力量增加", 40, "STR", "x"),
		Make(70000001, "敏捷增加", 40, "DEX", "x"),
		Make(70000002, "智力增加", 40, "INT", "x"),
		Make(70000003, "幸運增加", 40, "LUK", "x"),
		Make(70000004, "跳躍力增加", 30, "Jump", "2*u(x/3)"),
		Make(70000005, "移動速度增加", 30, "Speed", "2*u(x/3)"),
		Make(70000006, "防禦力增加", 40, "PDD", "10*x"),
		Make(70000007, "防禦力增加", 40, "PDD", "10*x"),
		Make(70000008, "最大HP增加", 40, "MHP", "x*15"),
		Make(70000009, "最大MP增加", 40, "MMP", "x*15"),
		Make(70000010, "跳躍力增加", 30, "Jump", "2*u(x/3)"),
		Make(70000011, "移動速度增加", 30, "Speed", "2*u(x/3)"),
		Make(70000012, "攻擊力增加", 30, "PAD", "3*u(x/3)"),
		Make(70000013, "魔力增加", 30, "MAD", "3*u(x/3)"),
		Make(70000014, "爆擊機率增加", 30, "CriticalChance", "x"),
		Make(70000015, "所有能力值增加", 40, "STR", "x", "DEX", "x", "INT", "x", "LUK", "x"),
		Make(70000016, "攻擊速度增加", 10, "AttackSpeed", "-1"),
		Make(70000017, "STR增加", 40, "STR", "x"),
		Make(70000018, "DEX增加", 40, "DEX", "x"),
		Make(70000019, "INT增加", 40, "INT", "x"),
		Make(70000020, "LUK增加", 40, "LUK", "x"),
		Make(70000021, "力量轉換為敏捷", 40, "STR2DEX", "u(x/4)"),
		Make(70000022, "敏捷轉換為力量", 40, "DEX2STR", "u(x/4)"),
		Make(70000023, "智力轉換為幸運", 40, "INT2LUK", "u(x/4)"),
		Make(70000024, "幸運轉換為敏捷", 40, "LUK2DEX", "u(x/4)"),
		Make(70000025, "每一定等級攻擊力增加", 10, "LV2PAD", "20-2*d(x/2)"),
		Make(70000026, "每一定等級魔力增加", 10, "LV2MAD", "20-2*d(x/2)"),
		Make(70000027, "最大HP固定比率增加", 20, "MHPR", "x"),
		Make(70000028, "最大MP固定比率增加", 20, "MMPR", "x"),
		Make(70000029, "防禦力固定比率增加", 20, "PDDR", "x"),
		Make(70000030, "防禦力固定比率增加", 20, "PDDR", "x"),
		Make(70000031, "最大HP增加一定比率", 20, "MHPR", "x"),
		Make(70000032, "最大MP增加一定比率", 20, "MMPR", "x"),
		Make(70000033, "楓幣獲得量增加", 40, "MesoRate", "u(x/2)"),
		Make(70000034, "增加迴避率", 20, "EVA", "u(x/2)"),
		Make(70000035, "攻擊Boss怪物時增加傷害", 20, "BossDamage", "x"),
		Make(70000036, "攻擊一般怪物時增加傷害", 40, "NormalDamage", "u(x/4)"),
		Make(70000037, "攻擊塔時增加傷害", 30, "TowerDamage", "2*u(x/3)"),
		Make(70000038, "補給模式秒殺", 40, "MINION", "u(x/4)"),
		Make(70000039, "狀態異常追加傷害", 40, "STATUSDMG", "u(x/4)"),
		Make(70000040, "每一定等級攻擊力增加", 10, "LV2PAD", "20-2*d(x/2)"),
		Make(70000041, "防禦力轉換為傷害", 20, "PDD2DAM", "x*2+u(x/2)"),
		Make(70000042, "每一定等級魔力增加", 10, "LV2MAD", "20-2*d(x/2)"),
		Make(70000043, "爆擊機率增加", 30, "CriticalChance", "x"),
		Make(70000044, "所有能力值增加", 40, "STR", "x", "DEX", "x", "INT", "x", "LUK", "x"),
		Make(70000045, "一定機率不套用冷卻", 20, "NOCOOL", "x"),
		Make(70000046, "被動技能等級增加", 10, "PASSIVE", "1"),
		Make(70000047, "增加攻擊技能目標", 10, "TARGET", "1"),
		Make(70000048, "增加加持持續時間", 40, "BuffTime", "x+u(x/4)"),
		Make(70000049, "增加道具掉落率", 40, "DropRate", "u(x/2)"),
		Make(70000050, "增加楓幣獲得量", 40, "MesoRate", "u(x/2)"),
		Make(70000051, "增加力量、敏捷", 40, "STR", "x", "DEX", "u(x/2)"),
		Make(70000052, "增加力量、智力", 40, "STR", "x", "INT", "u(x/2)"),
		Make(70000053, "增加力量、幸運", 40, "STR", "x", "LUK", "u(x/2)"),
		Make(70000054, "增加敏捷、智力", 40, "DEX", "x", "INT", "u(x/2)"),
		Make(70000055, "增加敏捷、幸運", 40, "DEX", "x", "LUK", "u(x/2)"),
		Make(70000056, "增加智力、幸運", 40, "INT", "x", "LUK", "u(x/2)"),
		Make(70000057, "增加敏捷、力量", 40, "DEX", "x", "STR", "u(x/2)"),
		Make(70000058, "增加智力、力量", 40, "INT", "x", "STR", "u(x/2)"),
		Make(70000059, "增加幸運、力量", 40, "LUK", "x", "STR", "u(x/2)"),
		Make(70000060, "增加智力、敏捷", 40, "INT", "x", "DEX", "u(x/2)"),
		Make(70000061, "增加幸運、敏捷", 40, "LUK", "x", "DEX", "u(x/2)"),
		Make(70000062, "增加幸運、智力", 40, "LUK", "x", "INT", "u(x/2)")
	};

	private static readonly Dictionary<int, AbilityInfo> byId = Build();

	public static IReadOnlyList<AbilityInfo> All => all;

	public static int ResetCostOf(int grade)
	{
		if (grade < 3)
		{
			return grade switch
			{
				1 => 200, 
				2 => 1500, 
				_ => 100, 
			};
		}
		return 8000;
	}

	public static int CostOf(int grade, int lockedCount)
	{
		int num = ResetCostOf(grade);
		if (lockedCount >= 1)
		{
			num += 3000;
		}
		if (lockedCount >= 2)
		{
			num += 5000;
		}
		return num;
	}

	public static int GradeUpRate(int grade)
	{
		if (grade < 3)
		{
			return grade switch
			{
				1 => 3, 
				2 => 2, 
				_ => 5, 
			};
		}
		return 0;
	}

	public static int GradeDownRate(int grade)
	{
		if (grade < 3)
		{
			return grade switch
			{
				1 => 3, 
				2 => 15, 
				_ => 0, 
			};
		}
		return 50;
	}

	private static AbilityInfo Make(int id, string name, int max, params string[] pairs)
	{
		AbilityInfo abilityInfo = new AbilityInfo
		{
			skillId = id,
			name = name,
			maxLevel = max,
			keys = new string[pairs.Length / 2],
			formulas = new string[pairs.Length / 2]
		};
		for (int i = 0; i < abilityInfo.keys.Length; i++)
		{
			abilityInfo.keys[i] = pairs[i * 2];
			abilityInfo.formulas[i] = pairs[i * 2 + 1];
		}
		return abilityInfo;
	}

	private static Dictionary<int, AbilityInfo> Build()
	{
		Dictionary<int, AbilityInfo> dictionary = new Dictionary<int, AbilityInfo>();
		AbilityInfo[] array = all;
		foreach (AbilityInfo abilityInfo in array)
		{
			dictionary[abilityInfo.skillId] = abilityInfo;
		}
		return dictionary;
	}

	public static AbilityInfo Of(int skillId)
	{
		if (!byId.TryGetValue(skillId, out var value))
		{
			return null;
		}
		return value;
	}

	public static int MinLevelForGrade(int grade)
	{
		return grade * 10 + 1;
	}

	public static int MaxLevelForGrade(int grade)
	{
		return (grade + 1) * 10;
	}

	public static int ClampLevel(AbilityInfo info, int grade, int level)
	{
		return Math.Min(info.maxLevel, Math.Max(MinLevelForGrade(grade), Math.Min(MaxLevelForGrade(grade), level)));
	}

	public static int GradeOfLevel(int level)
	{
		return Math.Max(0, Math.Min(3, (level - 1) / 10));
	}
}
