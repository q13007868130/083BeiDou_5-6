using System;
using System.Collections.Generic;

namespace Server.Bot;

public sealed class MarketScrollPricing
{
	public sealed class Scroll
	{
		public readonly long Price;

		public readonly int SuccessRate;

		public readonly int StatBonus;

		public Scroll(long price, int successRate, int statBonus)
		{
			Price = price;
			SuccessRate = successRate;
			StatBonus = statBonus;
		}
	}

	public sealed class ScrollInfo
	{
		public readonly int Ten;

		public readonly int Sixty;

		public readonly int Thirty;

		public readonly int Seventy;

		public int First
		{
			get
			{
				if (Ten == 0)
				{
					return Thirty;
				}
				return Ten;
			}
		}

		public int Second
		{
			get
			{
				if (Sixty == 0)
				{
					return Seventy;
				}
				return Sixty;
			}
		}

		public ScrollInfo(int ten, int sixty, int thirty = 0, int seventy = 0)
		{
			Ten = ten;
			Sixty = sixty;
			Thirty = thirty;
			Seventy = seventy;
		}
	}

	private static readonly Dictionary<int, Dictionary<string, ScrollInfo>> scrolls = new Dictionary<int, Dictionary<string, ScrollInfo>>
	{
		{
			147,
			new Dictionary<string, ScrollInfo> { 
			{
				"watt",
				new ScrollInfo(2044702, 2044701)
			} }
		},
		{
			133,
			new Dictionary<string, ScrollInfo> { 
			{
				"watt",
				new ScrollInfo(2043302, 2043301)
			} }
		},
		{
			100,
			new Dictionary<string, ScrollInfo> { 
			{
				"int",
				new ScrollInfo(0, 0, 2040013, 2040012)
			} }
		},
		{
			104,
			new Dictionary<string, ScrollInfo>
			{
				{
					"luk",
					new ScrollInfo(0, 0, 2040411, 2040410)
				},
				{
					"str",
					new ScrollInfo(0, 0, 2040407, 2040406)
				}
			}
		},
		{
			106,
			new Dictionary<string, ScrollInfo> { 
			{
				"dex",
				new ScrollInfo(0, 0, 2040611, 2040610)
			} }
		},
		{
			105,
			new Dictionary<string, ScrollInfo>
			{
				{
					"dex",
					new ScrollInfo(2040502, 2040501, 2040509, 2040508)
				},
				{
					"int",
					new ScrollInfo(2040514, 2040513, 2040519, 2040518)
				},
				{
					"luk",
					new ScrollInfo(0, 0, 2040521, 2040520)
				}
			}
		},
		{
			108,
			new Dictionary<string, ScrollInfo>
			{
				{
					"watt",
					new ScrollInfo(2040805, 2040804, 2040811, 2040810)
				},
				{
					"matt",
					new ScrollInfo(0, 0, 2040815, 2040814)
				},
				{
					"int",
					new ScrollInfo(0, 0, 2040815, 2040814)
				}
			}
		},
		{
			107,
			new Dictionary<string, ScrollInfo> { 
			{
				"dex",
				new ScrollInfo(2040705, 2040704, 2040715, 2040714)
			} }
		},
		{
			110,
			new Dictionary<string, ScrollInfo>
			{
				{
					"str",
					new ScrollInfo(2041014, 2041013, 2041035, 2041034)
				},
				{
					"dex",
					new ScrollInfo(2041020, 2041019, 2041039, 2041038)
				},
				{
					"int",
					new ScrollInfo(2041017, 2041016, 2041037, 2041036)
				},
				{
					"luk",
					new ScrollInfo(2041023, 2041022, 2041041, 2041040)
				}
			}
		},
		{
			103,
			new Dictionary<string, ScrollInfo>
			{
				{
					"int",
					new ScrollInfo(2040302, 2040301, 2040305, 2040304)
				},
				{
					"matt",
					new ScrollInfo(2040302, 2040301, 2040305, 2040304)
				},
				{
					"dex",
					new ScrollInfo(2040307, 2040306, 2040307, 2040306)
				}
			}
		},
		{
			109,
			new Dictionary<string, ScrollInfo>
			{
				{
					"watt",
					new ScrollInfo(0, 0, 2040917, 2040916)
				},
				{
					"matt",
					new ScrollInfo(0, 0, 2040922, 2040921)
				},
				{
					"luk",
					new ScrollInfo(0, 0, 2040907, 2040906)
				}
			}
		}
	};

	private readonly MarketItemPool pool;

	private readonly BotItemCatalog catalog;

	private readonly Random random;

	public MarketScrollPricing(MarketItemPool pool, BotItemCatalog catalog, Random random = null)
	{
		this.pool = pool;
		this.catalog = catalog;
		this.random = random ?? new Random();
	}

	public ScrollInfo Find(int itemId, string stat)
	{
		if (!scrolls.TryGetValue(itemId / 10000, out var value) || !value.TryGetValue(stat, out var value2))
		{
			return null;
		}
		if (!pool.Exists(value2.First))
		{
			return null;
		}
		return value2;
	}

	public ScrollInfo DefaultScrolls(int itemId)
	{
		int num = itemId / 10000;
		if (num == 133 || num == 147)
		{
			return Find(itemId, "watt");
		}
		if (!scrolls.ContainsKey(num))
		{
			return null;
		}
		switch (BotItemCatalog.Int(catalog.Info(itemId), "reqJob"))
		{
		case 0:
		{
			string[] array = new string[6] { "watt", "matt", "str", "luk", "dex", "int" };
			for (int num2 = array.Length - 1; num2 > 0; num2--)
			{
				int num3 = random.Next(num2 + 1);
				string text = array[num2];
				array[num2] = array[num3];
				array[num3] = text;
			}
			string[] array2 = array;
			foreach (string stat in array2)
			{
				ScrollInfo scrollInfo = Find(itemId, stat);
				if (scrollInfo != null)
				{
					return scrollInfo;
				}
			}
			return null;
		}
		case 1:
			return Find(itemId, "str");
		case 2:
			return Find(itemId, (num == 108) ? "matt" : "int");
		case 4:
			return Find(itemId, "dex");
		case 8:
			return Find(itemId, "luk") ?? Find(itemId, "dex");
		default:
			return null;
		}
	}

	public static string HighestStat(InventoryEntry item, InventoryEntry clean, out int bonus)
	{
		int[] array = new int[6]
		{
			item.watk - clean.watk,
			item.matk - clean.matk,
			item.str - clean.str,
			item.dex - clean.dex,
			item.int_ - clean.int_,
			item.luk - clean.luk
		};
		string[] array2 = new string[6] { "watt", "matt", "str", "dex", "int", "luk" };
		bonus = int.MinValue;
		string result = null;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] >= bonus)
			{
				bonus = array[i];
				result = array2[i];
			}
		}
		return result;
	}

	public static int[] Pattern(int slots, int version, Random random)
	{
		int num = random.Next(100) + 1;
		int num2;
		if (version < 40)
		{
			num2 = ((num <= 3) ? 3 : ((num > 38) ? 1 : 2));
		}
		else if (num <= 15)
		{
			num2 = 4;
		}
		else
		{
			num2 = ((num <= 40) ? 3 : ((num > 85) ? 1 : 2));
		}
		int num3 = slots - num2;
		int num4 = 0;
		if (num3 > 0)
		{
			num = random.Next(100) + 1;
			if (num <= 10)
			{
				num4 = 5;
			}
			else if (num <= 40)
			{
				num4 = 4;
			}
			else
			{
				num4 = ((num <= 70) ? 3 : ((version < 40 && num > 90) ? 1 : 2));
			}
			num4 = Math.Min(num4, num3);
		}
		return new int[2] { num2, num4 };
	}

	public static double[,] MinimumCosts(IList<Scroll> options, long baseCost, int slots, int maxBonus, int maxIncrement)
	{
		double[,] array = new double[slots + 1, maxBonus + 1];
		for (int i = 0; i <= slots; i++)
		{
			for (int j = 0; j <= maxBonus; j++)
			{
				array[i, j] = double.PositiveInfinity;
			}
		}
		array[0, 0] = baseCost;
		for (int k = 0; k < slots; k++)
		{
			for (int l = 0; l <= maxBonus - maxIncrement; l++)
			{
				foreach (Scroll option in options)
				{
					if (option.SuccessRate > 0 && l + option.StatBonus <= maxBonus)
					{
						array[k + 1, l + option.StatBonus] = Math.Min(array[k + 1, l + option.StatBonus], (array[k, l] + (double)option.Price) / ((double)option.SuccessRate / 100.0));
					}
				}
			}
		}
		return array;
	}

	private static long JavaLong(double value)
	{
		if (!double.IsNaN(value))
		{
			if (!(value >= 9.223372036854776E+18))
			{
				if (!(value <= -9.223372036854776E+18))
				{
					return (long)value;
				}
				return long.MinValue;
			}
			return long.MaxValue;
		}
		return 0L;
	}

	public static SortedDictionary<int, long> CleanCosts(double[,] costs)
	{
		SortedDictionary<int, long> sortedDictionary = new SortedDictionary<int, long>();
		for (int i = 0; i < costs.GetLength(0); i++)
		{
			for (int j = 0; j < costs.GetLength(1); j++)
			{
				if (costs[i, j] < double.PositiveInfinity)
				{
					long num = JavaLong(costs[i, j]);
					if (!sortedDictionary.TryGetValue(j, out var value) || num < value)
					{
						sortedDictionary[j] = num;
					}
				}
			}
		}
		List<int> list = new List<int>(sortedDictionary.Keys);
		for (int k = 1; k < list.Count - 1; k++)
		{
			if (sortedDictionary[list[k]] > sortedDictionary[list[k + 1]])
			{
				sortedDictionary[list[k]] = JavaLong((double)sortedDictionary[list[k - 1]] + 0.6 * (double)(sortedDictionary[list[k + 1]] - sortedDictionary[list[k - 1]]));
			}
		}
		for (int l = 1; l < list.Count; l++)
		{
			sortedDictionary[list[l]] = JavaLong((double)sortedDictionary[list[l]] * 0.6);
		}
		return sortedDictionary;
	}

	private Scroll ReadScroll(int id)
	{
		Dictionary<string, object> dictionary = pool.Item(id);
		if (dictionary == null || !dictionary.ContainsKey("success_rate"))
		{
			throw new InvalidOperationException("Original scroll pricing node missing: " + id);
		}
		return new Scroll(pool.CurrentPrice(dictionary), BotItemCatalog.Int(dictionary, "success_rate"), BotItemCatalog.Int(dictionary, "stat_bonus"));
	}

	public int EquipValue(InventoryEntry equip)
	{
		InventoryEntry inventoryEntry = catalog.CleanEquip(equip.itemId);
		string stat = HighestStat(equip, inventoryEntry, out var bonus);
		long num = pool.Price(equip.itemId) ?? 100;
		ScrollInfo scrollInfo = Find(equip.itemId, stat);
		if (scrollInfo == null)
		{
			return (int)num;
		}
		List<Scroll> list = new List<Scroll>();
		if (scrollInfo.Ten != 0 && scrollInfo.Sixty != 0)
		{
			list.Add(ReadScroll(scrollInfo.Ten));
			list.Add(ReadScroll(scrollInfo.Sixty));
		}
		if (pool.Version > 40 && scrollInfo.Thirty != 0 && scrollInfo.Seventy != 0)
		{
			list.Add(ReadScroll(scrollInfo.Thirty));
			list.Add(ReadScroll(scrollInfo.Seventy));
		}
		int num2 = int.MinValue;
		foreach (Scroll item in list)
		{
			num2 = Math.Max(num2, item.StatBonus);
		}
		int maxBonus = num2 * inventoryEntry.upgradeslots;
		long num3 = (CleanCosts(MinimumCosts(list, num, inventoryEntry.upgradeslots, maxBonus, num2)).TryGetValue(bonus, out var value) ? value : 1);
		if (num3 <= int.MaxValue)
		{
			return (int)num3;
		}
		return int.MaxValue;
	}

	public bool IsClean(InventoryEntry item)
	{
		if (item.inventoryType == 1 && BotItemCatalog.Int(item.equipmentData, "level") == 0)
		{
			return item.upgradeslots == catalog.CleanEquip(item.itemId).upgradeslots;
		}
		return false;
	}

	public int? ItemValue(InventoryEntry item)
	{
		if (item.inventoryType != 1 || IsClean(item))
		{
			return pool.Price(item.itemId);
		}
		return EquipValue(item);
	}

	private static int Improve(int value, Dictionary<string, int> stats, string key)
	{
		if (!stats.TryGetValue(key, out var value2))
		{
			return value;
		}
		return (short)Math.Min(32767, value + value2);
	}

	public void ImproveStats(InventoryEntry equip, int scrollId)
	{
		Dictionary<string, int> dictionary = catalog.EquipStats(scrollId);
		if (dictionary == null)
		{
			throw new InvalidOperationException("Original scroll IMG missing: " + scrollId);
		}
		equip.str = Improve(equip.str, dictionary, "STR");
		equip.dex = Improve(equip.dex, dictionary, "DEX");
		equip.int_ = Improve(equip.int_, dictionary, "INT");
		equip.luk = Improve(equip.luk, dictionary, "LUK");
		equip.watk = Improve(equip.watk, dictionary, "PAD");
		equip.matk = Improve(equip.matk, dictionary, "MAD");
		equip.wdef = Improve(equip.wdef, dictionary, "PDD");
		equip.mdef = Improve(equip.mdef, dictionary, "MDD");
		equip.acc = Improve(equip.acc, dictionary, "ACC");
		equip.avoid = Improve(equip.avoid, dictionary, "EVA");
		equip.speed = Improve(equip.speed, dictionary, "Speed");
		equip.jump = Improve(equip.jump, dictionary, "Jump");
		equip.hp = Improve(equip.hp, dictionary, "MHP");
		equip.mp = Improve(equip.mp, dictionary, "MMP");
	}

	public InventoryEntry ScrollEquip(InventoryEntry equip, DbStore db, int gameVersion = 55)
	{
		ScrollInfo scrollInfo = DefaultScrolls(equip.itemId);
		if (scrollInfo == null)
		{
			return equip;
		}
		int[] array = Pattern(equip.upgradeslots, gameVersion, random);
		bool flag = GameConfigDao.GetBool(db, "use_perfect_gm_scroll", fallback: true);
		bool flag2 = GameConfigDao.GetBool(db, "use_perfect_scrolling");
		int num = GameConfigDao.GetInt(db, "scroll_chance_rolls", 1);
		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < array[i]; j++)
			{
				int num2 = ((i == 0) ? scrollInfo.First : scrollInfo.Second);
				Dictionary<string, int> dictionary = catalog.EquipStats(num2);
				if (dictionary == null)
				{
					throw new InvalidOperationException("Original scroll IMG missing: " + num2);
				}
				if (equip.upgradeslots <= 0 && !flag)
				{
					continue;
				}
				if (flag || random.NextDouble() >= Math.Pow(1.0 - (double)dictionary["success"] / 100.0, num))
				{
					ImproveStats(equip, num2);
					if (!flag)
					{
						equip.upgradeslots--;
					}
					int num3 = BotItemCatalog.Int(equip.equipmentData, "level");
					if (equip.equipmentData == null)
					{
						equip.equipmentData = new Dictionary<string, object>();
					}
					equip.equipmentData["level"] = (sbyte)(num3 + 1);
				}
				else
				{
					if (!flag2)
					{
						equip.upgradeslots--;
					}
					random.Next(100);
				}
			}
		}
		equip.upgradeslots = 0;
		return equip;
	}
}
