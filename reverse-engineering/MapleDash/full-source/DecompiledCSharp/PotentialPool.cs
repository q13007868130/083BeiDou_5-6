using System;
using System.Collections.Generic;

public sealed class PotentialPool
{
	private readonly PotentialCatalog catalog;

	private static readonly HashSet<int> Removed = new HashSet<int>
	{
		40081, 10222, 10227, 10232, 10237, 10242, 10247, 32661, 42661, 20396,
		40057, 42061, 42062, 22056, 32052, 32054, 32058, 32071, 42052, 42054,
		42058, 42071
	};

	public PotentialPool(PotentialCatalog catalog)
	{
		this.catalog = catalog ?? throw new ArgumentNullException("catalog");
	}

	public int WeightOf(int id)
	{
		Dictionary<string, object> dictionary = catalog.Info(id);
		int num = ((dictionary != null && dictionary.TryGetValue("weight", out var value)) ? Convert.ToInt32(value) : 0);
		if (num != 0)
		{
			return num;
		}
		return 1;
	}

	public static int GradeOf(int id)
	{
		if (id <= 0 || id >= 10000)
		{
			if (id <= 10000 || id >= 20000)
			{
				if (id <= 20000 || id >= 30000)
				{
					if (id <= 30000 || id >= 40000)
					{
						if (id <= 40000 || id >= 50000)
						{
							return -1;
						}
						return 4;
					}
					return 3;
				}
				return 2;
			}
			return 1;
		}
		return 0;
	}

	public static bool IsBonus(int id)
	{
		if (id > 2000)
		{
			return id / 1000 % 10 == 2;
		}
		return false;
	}

	public static bool SourceAllows(int id)
	{
		int num = id % 1000;
		if (id > 0 && num != 14 && num != 54 && (num != 7 || id == 41007) && (num != 47 || id == 12047) && num != 8 && (num != 48 || id == 12048) && num != 202 && num != 207 && num != 801 && num != 802 && id % 10000 != 2056 && !Removed.Contains(id) && (id <= 14 || id >= 900) && (id <= 20000 || id >= 20014) && (id <= 30000 || id >= 30014))
		{
			if (id > 40000)
			{
				return id >= 40014;
			}
			return true;
		}
		return false;
	}

	private static int Value(Dictionary<string, object> row, string key)
	{
		if (!row.TryGetValue(key, out var value))
		{
			return 0;
		}
		return Convert.ToInt32(value);
	}

	public List<int> Legal(int itemId, int level, int grade, bool bonus, bool allowAutoSteal)
	{
		if (level < 0 || grade < 0 || grade > 4)
		{
			throw new ArgumentException("Invalid potential pool request");
		}
		int num = itemId / 10000;
		bool flag = num == 109 || num == 135;
		bool flag2 = (itemId >= 1210000 && itemId < 1600000) || num == 170;
		bool flag3 = num == 119;
		bool flag4 = num == 111 || num == 112 || num == 101 || num == 102 || num == 103;
		bool flag5 = (itemId < 1010000 || itemId >= 1040000) && (itemId < 1122000 || itemId >= 1153000) && (itemId < 1112000 || itemId >= 1113000) && (itemId < 1670000 || itemId >= 1680000) && !flag2 && num != 118 && num != 167;
		List<int> list = new List<int>();
		foreach (int id in catalog.Ids)
		{
			if (!SourceAllows(id) || GradeOf(id) != grade || IsBonus(id) != bonus)
			{
				continue;
			}
			Dictionary<string, object> dictionary = catalog.Info(id);
			if (Value(dictionary, "reqLevel") > level)
			{
				continue;
			}
			int num2 = Value(dictionary, "optionType");
			bool flag6 = num2 == 0;
			if (flag)
			{
				flag6 |= (num2 == 10 || num2 == 20) && id != 42060;
			}
			else if (flag2)
			{
				flag6 |= num2 == 10;
			}
			else if (flag3)
			{
				string text = (dictionary.TryGetValue("string", out var value) ? (value as string) : "");
				flag6 |= num2 == 10 && !(text ?? "").Contains("Boss");
			}
			else
			{
				flag6 |= num2 == 11;
				if (flag4)
				{
					flag6 |= num2 == 40;
				}
				else
				{
					flag6 |= (num2 == 51 && num == 100) || (num2 == 52 && (num == 104 || num == 105)) || (num2 == 53 && num == 106) || (num2 == 55 && num == 107);
					if (num == 108)
					{
						flag6 |= ((grade < 3) ? (num2 == 54) : ((num2 == 54 || num2 == 20) && id != 42060));
					}
					else if (flag5 || num == 115 || num == 113)
					{
						flag6 |= num2 == 20;
					}
				}
			}
			if (flag6 && num == 108 && grade >= 3 && !allowAutoSteal && dictionary.TryGetValue("string", out var value2) && ((string)value2).Contains("Auto Steal"))
			{
				flag6 = false;
			}
			if (flag6)
			{
				list.Add(id);
			}
		}
		return list;
	}
}
