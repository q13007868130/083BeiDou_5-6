using System;
using System.Collections.Generic;

public static class SymbolCatalog
{
	public const int ArcSlotBase = -201;

	public const int AutSlotBase = -211;

	public const int GrandSlotBase = -221;

	private static readonly SymbolInfo[] all = new SymbolInfo[15]
	{
		new SymbolInfo
		{
			itemId = 1712000,
			name = "祕法符文",
			kind = SymbolKind.Arcane,
			maxLevel = 20,
			initialInc = 30,
			mainStatPerLevel = 100,
			demonAvengerHpPerLevel = 2100,
			xenonPerLevel = 48
		},
		new SymbolInfo
		{
			itemId = 1712001,
			name = "祕法符文：消逝的旅途",
			kind = SymbolKind.Arcane,
			maxLevel = 20,
			initialInc = 30,
			mainStatPerLevel = 100,
			demonAvengerHpPerLevel = 2100,
			xenonPerLevel = 48
		},
		new SymbolInfo
		{
			itemId = 1712002,
			name = "祕法符文：啾啾艾爾蘭",
			kind = SymbolKind.Arcane,
			maxLevel = 20,
			initialInc = 30,
			mainStatPerLevel = 100,
			demonAvengerHpPerLevel = 2100,
			xenonPerLevel = 48
		},
		new SymbolInfo
		{
			itemId = 1712003,
			name = "祕法符文：拉契爾恩",
			kind = SymbolKind.Arcane,
			maxLevel = 20,
			initialInc = 30,
			mainStatPerLevel = 100,
			demonAvengerHpPerLevel = 2100,
			xenonPerLevel = 48
		},
		new SymbolInfo
		{
			itemId = 1712004,
			name = "祕法符文：阿爾卡娜",
			kind = SymbolKind.Arcane,
			maxLevel = 20,
			initialInc = 30,
			mainStatPerLevel = 100,
			demonAvengerHpPerLevel = 2100,
			xenonPerLevel = 48
		},
		new SymbolInfo
		{
			itemId = 1712005,
			name = "祕法符文：魔菈斯",
			kind = SymbolKind.Arcane,
			maxLevel = 20,
			initialInc = 30,
			mainStatPerLevel = 100,
			demonAvengerHpPerLevel = 2100,
			xenonPerLevel = 48
		},
		new SymbolInfo
		{
			itemId = 1712006,
			name = "祕法符文：艾斯佩拉",
			kind = SymbolKind.Arcane,
			maxLevel = 20,
			initialInc = 30,
			mainStatPerLevel = 100,
			demonAvengerHpPerLevel = 2100,
			xenonPerLevel = 48
		},
		new SymbolInfo
		{
			itemId = 1713000,
			name = "真實符文：賽爾尼溫",
			kind = SymbolKind.Authentic,
			maxLevel = 11,
			initialInc = 10,
			mainStatPerLevel = 200,
			demonAvengerHpPerLevel = 4200,
			xenonPerLevel = 96
		},
		new SymbolInfo
		{
			itemId = 1713001,
			name = "真實符文：阿爾克斯",
			kind = SymbolKind.Authentic,
			maxLevel = 11,
			initialInc = 10,
			mainStatPerLevel = 200,
			demonAvengerHpPerLevel = 4200,
			xenonPerLevel = 96
		},
		new SymbolInfo
		{
			itemId = 1713002,
			name = "真實符文：奧迪溫",
			kind = SymbolKind.Authentic,
			maxLevel = 11,
			initialInc = 10,
			mainStatPerLevel = 200,
			demonAvengerHpPerLevel = 4200,
			xenonPerLevel = 96
		},
		new SymbolInfo
		{
			itemId = 1713003,
			name = "真實符文：桃源境",
			kind = SymbolKind.Authentic,
			maxLevel = 11,
			initialInc = 10,
			mainStatPerLevel = 200,
			demonAvengerHpPerLevel = 4200,
			xenonPerLevel = 96
		},
		new SymbolInfo
		{
			itemId = 1713004,
			name = "真實符文：阿爾特利亞",
			kind = SymbolKind.Authentic,
			maxLevel = 11,
			initialInc = 10,
			mainStatPerLevel = 200,
			demonAvengerHpPerLevel = 4200,
			xenonPerLevel = 96
		},
		new SymbolInfo
		{
			itemId = 1713005,
			name = "真實符文：卡爾西溫",
			kind = SymbolKind.Authentic,
			maxLevel = 11,
			initialInc = 10,
			mainStatPerLevel = 200,
			demonAvengerHpPerLevel = 4200,
			xenonPerLevel = 96
		},
		new SymbolInfo
		{
			itemId = 1714000,
			name = "豪華真實符文：塔拉哈特",
			kind = SymbolKind.Grand,
			maxLevel = 11,
			initialInc = 10,
			mainStatPerLevel = 200,
			demonAvengerHpPerLevel = 4200,
			xenonPerLevel = 96
		},
		new SymbolInfo
		{
			itemId = 1714001,
			name = "豪華真實符文：基爾德拉",
			kind = SymbolKind.Grand,
			maxLevel = 11,
			initialInc = 10,
			mainStatPerLevel = 200,
			demonAvengerHpPerLevel = 4200,
			xenonPerLevel = 96
		}
	};

	private static readonly Dictionary<int, SymbolInfo> byId = Build();

	public static IReadOnlyList<SymbolInfo> All => all;

	private static Dictionary<int, SymbolInfo> Build()
	{
		Dictionary<int, SymbolInfo> dictionary = new Dictionary<int, SymbolInfo>();
		SymbolInfo[] array = all;
		foreach (SymbolInfo symbolInfo in array)
		{
			dictionary[symbolInfo.itemId] = symbolInfo;
		}
		return dictionary;
	}

	public static SymbolInfo Of(int itemId)
	{
		if (!byId.TryGetValue(itemId, out var value))
		{
			return null;
		}
		return value;
	}

	public static bool IsSymbol(int itemId)
	{
		if (itemId / 1000 != 1712 && itemId / 1000 != 1713)
		{
			return itemId / 1000 == 1714;
		}
		return true;
	}

	public static bool IsArcaneSymbol(int itemId)
	{
		return itemId / 1000 == 1712;
	}

	public static bool IsSacredSymbol(int itemId)
	{
		if (itemId / 1000 != 1713)
		{
			return itemId / 1000 == 1714;
		}
		return true;
	}

	public static bool IsMergeable(int itemId)
	{
		if (IsSymbol(itemId))
		{
			return itemId != 1712000;
		}
		return false;
	}

	public static bool IsMergeMaterial(int level, int exp)
	{
		if (level == 1)
		{
			if (exp != 0)
			{
				return exp == 1;
			}
			return true;
		}
		return false;
	}

	public static int SlotOf(int itemId)
	{
		SymbolInfo symbolInfo = Of(itemId);
		if (symbolInfo == null)
		{
			return 0;
		}
		return symbolInfo.kind switch
		{
			SymbolKind.Arcane => -201 - (itemId - 1712000), 
			SymbolKind.Authentic => -211 - (itemId - 1713000), 
			_ => -221 - (itemId - 1714000), 
		};
	}

	public static int GrandBonus(int itemId, int level, string stat)
	{
		SymbolInfo symbolInfo = Of(itemId);
		if (symbolInfo == null || symbolInfo.kind != SymbolKind.Grand || level < 1 || level > symbolInfo.maxLevel)
		{
			return 0;
		}
		switch (stat)
		{
		case "ExpRate":
			return 10 + (level - 1) * 4;
		case "MesoRate":
		case "DropRate":
			return 5 + level - 1;
		default:
			return 0;
		}
	}

	public static bool IsSymbolSlot(int slot)
	{
		if ((slot > -201 || slot <= -217) && (slot > -211 || slot <= -227))
		{
			if (slot <= -221)
			{
				return slot > -237;
			}
			return false;
		}
		return true;
	}

	public static long MesoCost(int itemId, int level)
	{
		long num;
		long num2;
		if (itemId >= 1712001 && itemId <= 1712006)
		{
			num = 80000 + (long)(itemId - 1712001) * 20000L;
			num2 = 1000L;
		}
		else if (itemId >= 1713000 && itemId <= 1713005)
		{
			num = 1320000 + (long)(itemId - 1713000) * 180000L;
			num2 = -60000L;
		}
		else
		{
			if (itemId != 1714000 && itemId != 1714001)
			{
				return 0L;
			}
			num = ((itemId == 1714000) ? 3980000 : 4880000);
			num2 = -60000L;
		}
		bool flag = itemId / 1000 == 1712;
		long num3 = (flag ? ((long)level * (long)level + 11) : (9L * (long)level * level + 20L * (long)level));
		long num4 = (flag ? 10000 : 100000);
		long num5 = num3 * (num + level * num2);
		return Math.Max(0L, num5 / num4 * num4);
	}
}
