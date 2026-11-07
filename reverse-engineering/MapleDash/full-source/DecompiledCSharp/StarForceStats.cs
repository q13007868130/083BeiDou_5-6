using System;
using System.Collections.Generic;

public static class StarForceStats
{
	private static readonly string[] Keys = new string[14]
	{
		"str", "dex", "int", "luk", "hp", "mp", "watk", "matk", "wdef", "mdef",
		"acc", "avoid", "speed", "jump"
	};

	private static readonly int[,] Attack = new int[15, 5]
	{
		{ 6, 7, 8, 9, 12 },
		{ 7, 8, 9, 9, 13 },
		{ 7, 8, 9, 10, 14 },
		{ 8, 9, 10, 11, 14 },
		{ 9, 10, 11, 12, 15 },
		{ 11, 11, 12, 13, 16 },
		{ 12, 12, 13, 14, 17 },
		{ 17, 17, 18, 19, 21 },
		{ 19, 19, 20, 21, 23 },
		{ 21, 21, 22, 23, 25 },
		{ 23, 23, 24, 25, 27 },
		{ 25, 25, 26, 27, 29 },
		{ 27, 27, 28, 29, 31 },
		{ 29, 29, 30, 31, 33 },
		{ 31, 31, 32, 33, 35 }
	};

	private static int Base(Dictionary<string, int> stats, string key)
	{
		if (!stats.TryGetValue(key, out var value))
		{
			return 0;
		}
		return value;
	}

	public static Dictionary<string, int> Calculate(int itemId, int reqLevel, int stars, bool superior, Dictionary<string, int> baseStats, bool eligible = true)
	{
		if (baseStats == null || stars < 0 || stars > (superior ? 15 : 30) || reqLevel < 0)
		{
			throw new ArgumentException("无效的星力属性参数");
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		if (!eligible)
		{
			return dictionary;
		}
		int num = itemId / 10000;
		bool flag = ((itemId >= 1210000 && itemId < 1600000) || num == 170) && num != 135;
		int num2 = ((reqLevel > 137) ? ((reqLevel <= 149) ? 1 : ((reqLevel <= 159) ? 2 : ((reqLevel <= 199) ? 3 : 4))) : 0);
		string[] keys = Keys;
		foreach (string text in keys)
		{
			int num3 = Base(baseStats, text);
			bool flag2 = text == "watk" || text == "matk";
			bool flag3 = text == "wdef" || text == "mdef";
			if (num3 <= 0 && !flag2 && !flag3)
			{
				continue;
			}
			int num4 = 0;
			for (int j = 0; j < stars; j++)
			{
				int num5 = 0;
				if (flag3)
				{
					num5 = (int)((double)num3 * ((num == 105) ? 0.1 : 0.05));
				}
				else if (superior)
				{
					switch (text)
					{
					case "str":
					case "dex":
					case "int":
					case "luk":
						num5 = SuperiorStat(reqLevel, j);
						break;
					default:
						if (flag2)
						{
							num5 = SuperiorAttack(reqLevel, j);
						}
						break;
					}
				}
				else
				{
					switch (text)
					{
					case "hp":
					case "mp":
						if (j <= 2)
						{
							num5 = 5;
						}
						else if (j <= 4)
						{
							num5 = 10;
						}
						else if (j <= 6)
						{
							num5 = 15;
						}
						else
						{
							num5 = ((j <= 8) ? 20 : ((j <= 14) ? 25 : 0));
						}
						break;
					case "str":
					case "dex":
					case "int":
					case "luk":
						if (j <= 4)
						{
							num5 = 2;
						}
						else
						{
							num5 = ((j <= 14) ? 3 : ((j <= 21) ? (new int[5] { 7, 9, 11, 13, 15 })[num2] : 0));
						}
						break;
					default:
						if (flag2)
						{
							if (j >= 15)
							{
								num5 += Attack[j - 15, num2];
							}
							if (flag)
							{
								if (j <= 14)
								{
									num5 += ((num3 > 0) ? Math.Max(1, (int)((double)num3 * 0.02)) : 0);
								}
								else
								{
									num5 += j switch
									{
										29 => 7, 
										28 => 8, 
										27 => 9, 
										25 => 10, 
										24 => 11, 
										23 => 12, 
										22 => 13, 
										_ => 0, 
									};
									if (reqLevel == 200 && j == 15)
									{
										num5++;
									}
								}
							}
							if (num == 108 && ((j <= 10 && j % 2 == 0) || (j >= 12 && j <= 14)))
							{
								num5++;
							}
						}
						else if (num == 107 && (text == "speed" || text == "jump") && j <= 4)
						{
							num5++;
						}
						break;
					}
				}
				num4 = checked(num4 + num5);
			}
			dictionary[text] = num4;
		}
		return dictionary;
	}

	private static int SuperiorStat(int level, int star)
	{
		switch (star)
		{
		case 0:
			if (level >= 110)
			{
				if (level >= 149)
				{
					return 19;
				}
				return 9;
			}
			return 2;
		case 1:
			if (level >= 110)
			{
				if (level >= 149)
				{
					return 20;
				}
				return 10;
			}
			return 3;
		case 2:
			if (level >= 110)
			{
				if (level >= 149)
				{
					return 22;
				}
				return 12;
			}
			return 5;
		case 3:
			if (level >= 149)
			{
				return 25;
			}
			return 15;
		case 4:
			if (level >= 149)
			{
				return 29;
			}
			return 19;
		default:
			return 0;
		}
	}

	private static int SuperiorAttack(int level, int star)
	{
		switch (star)
		{
		default:
			return 0;
		case 14:
			return 23;
		case 13:
			return 21;
		case 12:
			return 19;
		case 11:
			return 17;
		case 10:
			return 15;
		case 9:
			return 13;
		case 8:
			return 12;
		case 7:
			if (level >= 150)
			{
				return 11;
			}
			return 7;
		case 6:
			if (level >= 150)
			{
				return 10;
			}
			return 6;
		case 5:
			if (level >= 150)
			{
				return 9;
			}
			return 5;
		}
	}
}
