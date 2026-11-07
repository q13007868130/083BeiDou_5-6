using System;
using System.Collections.Generic;

public static class FlameCatalog
{
	public sealed class Weights
	{
		public readonly int[] keys;

		public readonly int[] cumulative;

		public readonly int total;

		public Weights(string spec)
		{
			List<int> list = new List<int>();
			List<int> list2 = new List<int>();
			int num = 0;
			string[] array = spec.Split(',');
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split(':');
				int item = int.Parse(array2[0]);
				int num2 = int.Parse(array2[1]);
				if (num2 > 0)
				{
					num += num2;
					list.Add(item);
					list2.Add(num);
				}
			}
			keys = list.ToArray();
			cumulative = list2.ToArray();
			total = num;
		}

		public int Pick(int roll)
		{
			for (int i = 0; i < cumulative.Length; i++)
			{
				if (roll < cumulative[i])
				{
					return keys[i];
				}
			}
			return keys[keys.Length - 1];
		}
	}

	public const int BossLines = 4;

	public const int BossTierShift = 2;

	public const int Divider = 40;

	public const int DividerExtended = 20;

	public static readonly double[] WeaponMultiplier = new double[7] { 1.0, 2.2, 3.65, 5.35, 7.3, 8.8, 10.25 };

	public static readonly double[] BossWeaponMultiplier = new double[7] { 1.0, 1.0, 3.0, 4.4, 6.05, 8.0, 10.25 };

	private static readonly int[] FlamePrefixes = new int[52]
	{
		112, 113, 100, 101, 102, 105, 104, 106, 107, 103,
		108, 110, 116, 121, 122, 123, 124, 125, 126, 127,
		128, 129, 130, 131, 132, 133, 134, 136, 137, 138,
		139, 140, 141, 142, 143, 144, 145, 146, 147, 148,
		149, 150, 151, 152, 153, 154, 155, 156, 157, 158,
		159, 160
	};

	private static readonly Dictionary<int, FlameRule> rules = new Dictionary<int, FlameRule>
	{
		{
			6,
			new FlameRule
			{
				createType = 6,
				kind = "POWERFUL",
				lineWeights = "1:40,2:40,3:15,4:5",
				tierWeights = "1:20,2:30,3:36,4:14"
			}
		},
		{
			7,
			new FlameRule
			{
				createType = 7,
				kind = "ETERNAL",
				lineWeights = "1:40,2:40,3:15,4:5",
				tierWeights = "2:29,3:45,4:25"
			}
		},
		{
			12,
			new FlameRule
			{
				createType = 12,
				kind = "ABYSSAL",
				lineWeights = "1:40,2:40,3:15,4:5",
				tierWeights = "3:1,4:1,5:1"
			}
		},
		{
			20,
			new FlameRule
			{
				createType = 20,
				kind = "POWERFUL",
				lineWeights = "1:40,2:40,3:15,4:5",
				tierWeights = "3:300,4:550,5:270,6:140,7:50"
			}
		},
		{
			21,
			new FlameRule
			{
				createType = 21,
				kind = "POWERFUL",
				lineWeights = "1:40,2:40,3:15,4:5",
				tierWeights = "4:550,5:270,6:140,7:50"
			}
		},
		{
			0,
			new FlameRule
			{
				createType = 0,
				kind = "POWERFUL",
				lineWeights = "1:40,2:40,3:15,4:5",
				tierWeights = "1:20,2:30,3:36,4:14"
			}
		},
		{
			8,
			new FlameRule
			{
				createType = 8,
				kind = "POWERFUL",
				lineWeights = "1:40,2:40,3:15,4:5",
				tierWeights = "1:20,2:30,3:36,4:14"
			}
		}
	};

	private static readonly HashSet<int> Black = new HashSet<int> { 2048753, 2048755, 2048766, 2048767, 2048768, 2048799 };

	private static readonly HashSet<int> Abyssal = new HashSet<int> { 2048794, 2048796 };

	private static readonly Dictionary<int, int> createTypes = new Dictionary<int, int>
	{
		{ 2048716, 6 },
		{ 2048724, 6 },
		{ 2048745, 6 },
		{ 2048751, 6 },
		{ 2048752, 6 },
		{ 2048759, 6 },
		{ 2048770, 6 },
		{ 2048780, 6 },
		{ 2048788, 6 },
		{ 2048795, 6 },
		{ 2048717, 7 },
		{ 2048721, 7 },
		{ 2048723, 7 },
		{ 2048746, 7 },
		{ 2048747, 7 },
		{ 2048753, 7 },
		{ 2048755, 7 },
		{ 2048757, 7 },
		{ 2048758, 7 },
		{ 2048766, 7 },
		{ 2048767, 7 },
		{ 2048768, 7 },
		{ 2048769, 7 },
		{ 2048786, 7 },
		{ 2048787, 7 },
		{ 2048799, 7 },
		{ 2048720, 8 },
		{ 2048794, 12 },
		{ 2048796, 12 },
		{ 2048761, 20 },
		{ 2048760, 21 },
		{ 2048779, 21 },
		{ 2048726, 0 },
		{ 2048738, 0 }
	};

	public static bool IsFlameItem(int itemId)
	{
		return createTypes.ContainsKey(itemId);
	}

	public static int CreateTypeOf(int itemId)
	{
		if (!createTypes.TryGetValue(itemId, out var value))
		{
			return -1;
		}
		return value;
	}

	public static FlameRule Rule(int createType)
	{
		if (!rules.TryGetValue(createType, out var value))
		{
			return null;
		}
		return value;
	}

	public static bool IsChoice(int itemId)
	{
		if (!Black.Contains(itemId))
		{
			return Abyssal.Contains(itemId);
		}
		return true;
	}

	public static bool IsAbyssal(int itemId)
	{
		return Abyssal.Contains(itemId);
	}

	public static bool CanFlame(int itemId)
	{
		if (itemId / 1000000 != 1)
		{
			return false;
		}
		int num = itemId / 10000;
		if (num == 109 || num == 135)
		{
			return false;
		}
		return Array.IndexOf(FlamePrefixes, num) >= 0;
	}

	public static int FlameLevel(int reqLevel, int incReq)
	{
		return (int)Math.Ceiling(((double)(reqLevel + incReq) + 1.0) / 40.0);
	}

	public static int FlameLevelExtended(int reqLevel, int incReq)
	{
		return (int)Math.Ceiling(((double)(reqLevel + incReq) + 1.0) / 20.0);
	}

	public static int Value(int statVal, int tier, int reqLevel, int incReq, bool weapon, bool bossWeapon, int baseAttack)
	{
		int result = tier * FlameLevel(reqLevel, incReq);
		int result2 = tier * FlameLevelExtended(reqLevel, incReq);
		switch (statVal)
		{
		case 0:
		case 1:
		case 2:
		case 3:
			return result2;
		case 4:
		case 5:
		case 6:
		case 7:
		case 8:
		case 9:
			return result;
		case 10:
		case 11:
		{
			if (!weapon)
			{
				return tier;
			}
			double[] array = (bossWeapon ? BossWeaponMultiplier : WeaponMultiplier);
			int num = Math.Min(Math.Max(tier, 1), array.Length) - 1;
			return (int)Math.Ceiling((double)baseAttack * (array[num] * (double)FlameLevel(reqLevel, incReq)) / 100.0);
		}
		case 12:
			return result2;
		case 13:
		case 14:
			return (reqLevel + incReq) / 10 * 30 * tier;
		case 15:
		case 16:
		case 17:
		case 19:
			return tier;
		case 18:
			return tier * 2;
		case 20:
			return 5 * tier;
		default:
			return 0;
		}
	}

	public static bool Allowed(int statVal, bool weapon, int reqLevel, int incReq)
	{
		switch (statVal)
		{
		case 20:
			return reqLevel + incReq >= 5;
		case 18:
		case 19:
			return weapon;
		default:
			return true;
		}
	}
}
