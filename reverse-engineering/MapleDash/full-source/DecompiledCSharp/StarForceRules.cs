using System;
using System.Collections.Generic;
using System.Globalization;

public sealed class StarForceRules
{
	public sealed class Rate
	{
		public int success;

		public int destroy;

		public bool down;

		public long meso;
	}

	public sealed class Quote
	{
		public long cost;

		public int success;

		public int destroy;

		public bool chanceTime;

		public bool safeguard;
	}

	public enum Outcome
	{
		Success = 1,
		Destroy,
		Maintain
	}

	public sealed class Result
	{
		public Outcome outcome;

		public int stars;

		public int dropStreak;

		public bool vestige;
	}

	private readonly Rate[] normal;

	private readonly Rate[] superior;

	private readonly Dictionary<string, object> itemCaps;

	private readonly int[] levelMinimums;

	private readonly int[] normalCaps;

	private readonly int[] superiorCaps;

	private static StarForceRules shared;

	public static StarForceRules Shared => shared ?? (shared = new StarForceRules(PlatformRes.LoadText("Enhancement/StarForce")));

	public StarForceRules(string json)
	{
		if (!(MiniJsonDb.Parse(json ?? "") is Dictionary<string, object> dictionary) || Number(dictionary, "schemaVersion") != 1 || Number(dictionary, "scale") != 10000)
		{
			throw new InvalidOperationException("282 星力规则数据缺失或版本错误");
		}
		normal = ReadRates(dictionary, "normal", 30);
		superior = ReadRates(dictionary, "superior", 15);
		Dictionary<string, object> dictionary2 = (Dictionary<string, object>)dictionary["clientCaps"];
		itemCaps = (Dictionary<string, object>)dictionary2["itemCaps"];
		levelMinimums = ReadInts(dictionary2, "levelMinimums");
		normalCaps = ReadInts(dictionary2, "normal");
		superiorCaps = ReadInts(dictionary2, "superior");
		if (levelMinimums.Length != 6 || normalCaps.Length != 6 || superiorCaps.Length != 6)
		{
			throw new InvalidOperationException("282 客户端星级上限表不完整");
		}
	}

	private static int Number(Dictionary<string, object> row, string key)
	{
		return Convert.ToInt32(row[key]);
	}

	private static Rate[] ReadRates(Dictionary<string, object> root, string key, int count)
	{
		List<object> list = (List<object>)root[key];
		if (list.Count != count)
		{
			throw new InvalidOperationException("星力概率表不完整");
		}
		Rate[] array = new Rate[count];
		foreach (Dictionary<string, object> item in list)
		{
			int num = Number(item, "star");
			int num2 = Number(item, "success");
			int num3 = Number(item, "destroy");
			long num4 = Convert.ToInt64(item["meso"]);
			if (num < 0 || num >= count || array[num] != null || num2 < 0 || num3 < 0 || num2 + num3 > 10000 || num4 < -1)
			{
				throw new InvalidOperationException("星力概率表无效");
			}
			array[num] = new Rate
			{
				success = num2,
				destroy = num3,
				down = Convert.ToBoolean(item["down"]),
				meso = num4
			};
		}
		return array;
	}

	private static int[] ReadInts(Dictionary<string, object> root, string key)
	{
		return ((List<object>)root[key]).ConvertAll(Convert.ToInt32).ToArray();
	}

	public int Cap(int itemId, int level, bool isSuperior)
	{
		if (itemCaps.TryGetValue(itemId.ToString(CultureInfo.InvariantCulture), out var value))
		{
			return Convert.ToInt32(value);
		}
		int i;
		for (i = 0; i + 1 < levelMinimums.Length && level >= levelMinimums[i + 1]; i++)
		{
		}
		return (isSuperior ? superiorCaps : normalCaps)[i];
	}

	private Rate At(int star, bool isSuperior)
	{
		Rate[] array = (isSuperior ? superior : normal);
		if (star < 0 || star >= array.Length)
		{
			throw new ArgumentOutOfRangeException("star");
		}
		return array[star];
	}

	public bool CanSafeguard(int star, bool isSuperior)
	{
		if (!isSuperior && star >= 15)
		{
			return star <= 17;
		}
		return false;
	}

	public long Cost(int itemId, int level, int star, bool isSuperior)
	{
		Rate rate = At(star, isSuperior);
		if (level < 0 || level > 1000)
		{
			throw new ArgumentOutOfRangeException("level");
		}
		if (itemId == 1122444 || itemId == 1122445)
		{
			return 0L;
		}
		if (rate.meso >= 0)
		{
			return rate.meso;
		}
		double num = Math.Pow(level, 3.0);
		double num2;
		if (isSuperior)
		{
			num2 = 1000.0 + Math.Pow(level, 3.56);
		}
		else if (star < 10)
		{
			num2 = 1000.0 + num * (double)(star + 1) / 25.0;
		}
		else
		{
			double num3 = star switch
			{
				21 => 125, 
				19 => 45, 
				18 => 70, 
				17 => 150, 
				14 => 75, 
				13 => 110, 
				12 => 150, 
				11 => 220, 
				10 => 400, 
				_ => 200, 
			};
			num2 = 1000.0 + num * Math.Pow(star + 1, 2.7) / num3;
		}
		return checked((long)Math.Floor(num2 / 100.0 + 0.5) * 100);
	}

	public Quote Preview(int itemId, int level, int star, int dropStreak, bool isSuperior, bool safeguardUI = false, bool protectionScroll = false, int vipGrade = 0, bool miniGame = false)
	{
		if (star >= Cap(itemId, level, isSuperior))
		{
			throw new ArgumentException("装备已达到最大星级");
		}
		if (dropStreak < 0 || vipGrade < 0 || vipGrade > 20)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (safeguardUI && !CanSafeguard(star, isSuperior))
		{
			throw new ArgumentException("防破坏保护仅适用于普通装备15至17星");
		}
		Rate rate = At(star, isSuperior);
		bool flag = dropStreak >= 2;
		int num = rate.success;
		int num2 = rate.destroy;
		if (vipGrade > 0)
		{
			num = checked((int)Math.Floor((double)num * (1.0 + 0.025 * (double)vipGrade) + 0.5));
		}
		if (miniGame)
		{
			num = (int)Math.Floor((double)num * 1.045 + 0.5);
		}
		if (!isSuperior && (safeguardUI | protectionScroll))
		{
			num2 = 0;
		}
		if (flag)
		{
			num = 10000;
			num2 = 0;
		}
		num = Math.Min(num, 10000 - num2);
		long num3 = checked(Cost(itemId, level, star, isSuperior) * ((!safeguardUI) ? 1 : 3));
		num3 = checked(num3 * (100 - vipGrade * 5)) / 100;
		return new Quote
		{
			cost = num3,
			success = num,
			destroy = num2,
			chanceTime = flag,
			safeguard = safeguardUI
		};
	}

	public Result Roll(int star, int dropStreak, bool isSuperior, Quote quote, int draw)
	{
		Rate rate = At(star, isSuperior);
		if (quote == null || draw < 0 || draw >= 10000)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (draw < quote.success)
		{
			return new Result
			{
				outcome = Outcome.Success,
				stars = star + 1
			};
		}
		if (draw < quote.success + quote.destroy)
		{
			return new Result
			{
				outcome = Outcome.Destroy,
				stars = ((!isSuperior) ? star : 0),
				vestige = true
			};
		}
		bool flag = rate.down && star > 0;
		return new Result
		{
			outcome = Outcome.Maintain,
			stars = (flag ? (star - 1) : star),
			dropStreak = (flag ? Math.Min(2, dropStreak + 1) : 0)
		};
	}
}
