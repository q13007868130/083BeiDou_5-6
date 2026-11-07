using System.Collections.Generic;

public static class HyperStatCatalog
{
	public const int UnlockLevel = 140;

	public const int Presets = 3;

	public const long ResetCost = 10000000L;

	public const long SwitchCost = 2000000L;

	public static readonly int[] PointCost = new int[15]
	{
		1, 2, 4, 8, 10, 15, 20, 25, 30, 35,
		50, 65, 80, 95, 110
	};

	private static readonly int[] CritChance = new int[15]
	{
		1, 2, 3, 4, 5, 7, 9, 11, 13, 15,
		17, 19, 21, 23, 25
	};

	private static readonly int[] Boss = new int[15]
	{
		3, 6, 9, 12, 15, 19, 23, 27, 31, 35,
		39, 43, 47, 51, 55
	};

	private static readonly int[] Resist = new int[15]
	{
		1, 2, 3, 4, 5, 7, 9, 11, 13, 15,
		17, 19, 21, 23, 25
	};

	private static readonly int[] Exp = new int[15]
	{
		50, 100, 150, 200, 250, 300, 350, 400, 450, 500,
		600, 700, 800, 900, 1000
	};

	private static readonly int[] Arc = new int[15]
	{
		5, 10, 15, 20, 25, 30, 35, 40, 45, 50,
		60, 70, 80, 90, 100
	};

	private static readonly int[] Normal = new int[15]
	{
		3, 6, 9, 12, 15, 19, 23, 27, 31, 35,
		39, 43, 47, 51, 55
	};

	private static readonly HyperStatInfo[] all = new HyperStatInfo[17]
	{
		new HyperStatInfo
		{
			skillId = 80000400,
			name = "STR",
			maxLevel = 15,
			key = "STR",
			perLevel = 30
		},
		new HyperStatInfo
		{
			skillId = 80000401,
			name = "DEX",
			maxLevel = 15,
			key = "DEX",
			perLevel = 30
		},
		new HyperStatInfo
		{
			skillId = 80000402,
			name = "INT",
			maxLevel = 15,
			key = "INT",
			perLevel = 30
		},
		new HyperStatInfo
		{
			skillId = 80000403,
			name = "LUK",
			maxLevel = 15,
			key = "LUK",
			perLevel = 30
		},
		new HyperStatInfo
		{
			skillId = 80000404,
			name = "最大HP",
			maxLevel = 15,
			key = "HPR",
			perLevel = 2,
			percent = true
		},
		new HyperStatInfo
		{
			skillId = 80000405,
			name = "最大MP",
			maxLevel = 15,
			key = "MPR",
			perLevel = 2,
			percent = true
		},
		new HyperStatInfo
		{
			skillId = 80000406,
			name = "惡魔力量/時間之力",
			maxLevel = 10,
			key = "",
			perLevel = 10
		},
		new HyperStatInfo
		{
			skillId = 80000409,
			name = "爆擊機率",
			maxLevel = 15,
			key = "CriticalChance",
			levels = CritChance
		},
		new HyperStatInfo
		{
			skillId = 80000410,
			name = "爆擊傷害",
			maxLevel = 15,
			key = "CriticalDamage",
			perLevel = 1,
			percent = true
		},
		new HyperStatInfo
		{
			skillId = 80000412,
			name = "無視防禦率",
			maxLevel = 15,
			key = "IgnoreDefense",
			perLevel = 3,
			percent = true
		},
		new HyperStatInfo
		{
			skillId = 80000413,
			name = "傷害",
			maxLevel = 15,
			key = "Damage",
			perLevel = 3,
			percent = true
		},
		new HyperStatInfo
		{
			skillId = 80000414,
			name = "Boss傷害",
			maxLevel = 15,
			key = "BossDamage",
			levels = Boss,
			percent = true
		},
		new HyperStatInfo
		{
			skillId = 80000416,
			name = "異常狀態耐性",
			maxLevel = 15,
			key = "StatusResist",
			levels = Resist,
			percent = true
		},
		new HyperStatInfo
		{
			skillId = 80000419,
			name = "攻擊力／魔力",
			maxLevel = 15,
			key = "PAD",
			perLevel = 3,
			attack = true
		},
		new HyperStatInfo
		{
			skillId = 80000420,
			name = "獲得經驗值",
			maxLevel = 15,
			key = "ExpRate",
			levels = Exp,
			percent = true
		},
		new HyperStatInfo
		{
			skillId = 80000421,
			name = "神祕力量",
			maxLevel = 15,
			key = "Arcane",
			levels = Arc
		},
		new HyperStatInfo
		{
			skillId = 80000422,
			name = "一般傷害",
			maxLevel = 15,
			key = "NormalDamage",
			levels = Normal,
			percent = true
		}
	};

	private static readonly Dictionary<int, HyperStatInfo> byId = Build();

	public static IReadOnlyList<HyperStatInfo> All => all;

	private static Dictionary<int, HyperStatInfo> Build()
	{
		Dictionary<int, HyperStatInfo> dictionary = new Dictionary<int, HyperStatInfo>();
		HyperStatInfo[] array = all;
		foreach (HyperStatInfo hyperStatInfo in array)
		{
			dictionary[hyperStatInfo.skillId] = hyperStatInfo;
		}
		return dictionary;
	}

	public static HyperStatInfo Of(int skillId)
	{
		if (!byId.TryGetValue(skillId, out var value))
		{
			return null;
		}
		return value;
	}

	public static int SpForLevel(int level)
	{
		if (level >= 140)
		{
			return 3 + (level - 140) / 10;
		}
		return 0;
	}

	public static int TotalSp(int level)
	{
		int num = 0;
		for (int i = 140; i <= level; i++)
		{
			num += SpForLevel(i);
		}
		return num;
	}

	public static int NeededSp(int level)
	{
		if (level < 1 || level > PointCost.Length)
		{
			return 0;
		}
		return PointCost[level - 1];
	}
}
