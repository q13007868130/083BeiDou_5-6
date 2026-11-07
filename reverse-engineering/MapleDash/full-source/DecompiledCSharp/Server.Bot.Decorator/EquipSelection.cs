using System;
using System.Collections.Generic;

namespace Server.Bot.Decorator;

public sealed class EquipSelection
{
	private readonly EquipMetadataCache cache;

	private readonly Random random;

	public EquipSelection(EquipMetadataCache cache, Random random = null)
	{
		this.cache = cache;
		this.random = random ?? new Random();
	}

	public static int JobStyle(BotCharacter bot)
	{
		switch (bot.job / 100)
		{
		case 1:
		case 11:
		case 21:
			return 100;
		case 2:
		case 12:
		case 22:
			return 200;
		case 3:
		case 13:
			if (bot.job / 10 != 32)
			{
				return 300;
			}
			return 320;
		case 4:
		case 14:
			return 400;
		case 5:
		case 15:
			if (bot.str <= bot.dex)
			{
				return 520;
			}
			return 510;
		default:
			return 0;
		}
	}

	public int? ForWearing(string type, BotCharacter bot)
	{
		int num = JobStyle(bot);
		return Pick(type, bot.level, num switch
		{
			100 => 1, 
			200 => 2, 
			300 => 4, 
			400 => 8, 
			500 => 16, 
			_ => 0, 
		}, bot.gender) ?? Pick(type, bot.level, 0, bot.gender);
	}

	public int? Pick(string type, int maxLevel, int requiredJob, int gender)
	{
		int num = maxLevel - Math.Max((int)((double)maxLevel * 0.25), 10);
		List<int> list = new List<int>();
		foreach (EquipMetadataCache.Entry item in cache.ByType(type))
		{
			if (!item.Cash && (item.Gender == 2 || item.Gender == gender) && item.RequiredLevel >= num && item.RequiredLevel <= maxLevel && item.RequiredJob == requiredJob)
			{
				list.Add(item.Id);
			}
		}
		return Weighted(list);
	}

	public int? Weighted(IReadOnlyList<int> ids, int intervalPercentage = 20)
	{
		if (ids == null || ids.Count == 0)
		{
			return null;
		}
		if (intervalPercentage < 1 || intervalPercentage > 100)
		{
			throw new ArgumentOutOfRangeException("intervalPercentage");
		}
		int num = Math.Max(1, ids.Count * intervalPercentage / 100);
		int num2 = (ids.Count + num - 1) / num;
		double num3 = (double)num2 * (double)(num2 + 1) / 2.0;
		double num4 = random.NextDouble() * num3;
		double num5 = 0.0;
		int num6 = 0;
		for (int i = 0; i < num2; i++)
		{
			num5 += (double)(num2 - i);
			if (num4 <= num5)
			{
				num6 = i;
				break;
			}
		}
		int num7 = num6 * num;
		int num8 = Math.Min(num7 + num, ids.Count);
		return ids[num7 + random.Next(num8 - num7)];
	}
}
