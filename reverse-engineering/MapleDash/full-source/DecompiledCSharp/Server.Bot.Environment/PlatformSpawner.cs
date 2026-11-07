using System;
using System.Collections.Generic;
using System.Linq;
using Server.Field;

namespace Server.Bot.Environment;

public sealed class PlatformSpawner
{
	private readonly Random random;

	public PlatformSpawner(Random random = null)
	{
		this.random = random ?? new Random();
	}

	public static BotPoint SnapToGround(int mapId, BotPoint raw)
	{
		return new BotPoint(raw.x, (int)MapService.SnapToGround(mapId, raw.x, raw.y));
	}

	public BotPoint FindUnoccupiedPoint(Platform platform, List<BotPoint> occupied)
	{
		if (!platform.IsSloped)
		{
			return FindFlat(platform, occupied);
		}
		return FindSloped(platform, occupied);
	}

	public List<BotPoint> FindUnoccupiedPoints(Platform platform, List<BotPoint> occupied, int count)
	{
		List<BotPoint> list = new List<BotPoint>();
		List<BotPoint> list2 = ((occupied == null) ? new List<BotPoint>() : new List<BotPoint>(occupied));
		for (int i = 0; i < count; i++)
		{
			BotPoint item = FindUnoccupiedPoint(platform, list2);
			list.Add(item);
			list2.Add(item);
		}
		return list;
	}

	private BotPoint RandomFlat(Platform p)
	{
		return new BotPoint(p.minX + random.Next(Math.Max(1, p.Width)), p.baseY);
	}

	private BotPoint FindFlat(Platform platform, List<BotPoint> occupied)
	{
		if (occupied == null || occupied.Count == 0)
		{
			return RandomFlat(platform);
		}
		List<BotPoint> list = occupied.OrderBy((BotPoint p) => p.x).ToList();
		List<(int, int)> list2 = new List<(int, int)>();
		if (list[0].x - platform.minX > 30)
		{
			list2.Add((platform.minX, list[0].x));
		}
		for (int num = 0; num < list.Count - 1; num++)
		{
			if (list[num + 1].x - list[num].x > 30)
			{
				list2.Add((list[num].x, list[num + 1].x));
			}
		}
		if (platform.maxX - list[list.Count - 1].x > 30)
		{
			list2.Add((list[list.Count - 1].x, platform.maxX));
		}
		if (list2.Count == 0)
		{
			return RandomFlat(platform);
		}
		(int, int) tuple = list2[0];
		if (list2.Count > 1)
		{
			int num2 = random.Next(list2.Sum(((int start, int end) g) => g.end - g.start));
			int num3 = 0;
			foreach (var item in list2)
			{
				num3 += item.Item2 - item.Item1;
				if (num2 < num3)
				{
					tuple = item;
					break;
				}
			}
		}
		int num4 = tuple.Item2 - tuple.Item1;
		int num5 = (int)((double)(num4 / 2) * 0.3);
		int num6 = ((num5 > 0) ? (random.Next(num5 * 2) - num5) : 0);
		int num7 = Math.Min(15, num4 / 4);
		return new BotPoint(Math.Min(tuple.Item2 - num7, Math.Max(tuple.Item1 + num7, tuple.Item1 + num4 / 2 + num6)), platform.baseY);
	}

	private BotPoint FindSloped(Platform platform, List<BotPoint> occupied)
	{
		List<BotPoint> sortedPoints = platform.sortedPoints;
		if (sortedPoints.Count == 0)
		{
			return new BotPoint(platform.minX, platform.baseY);
		}
		if (occupied == null || occupied.Count == 0)
		{
			return sortedPoints[random.Next(sortedPoints.Count)];
		}
		List<BotPoint> list = sortedPoints.Where((BotPoint p) => !occupied.Any((BotPoint o) => Math.Abs(p.x - o.x) < 30)).ToList();
		if (list.Count == 0)
		{
			return sortedPoints[random.Next(sortedPoints.Count)];
		}
		if (list.Count == 1)
		{
			return list[0];
		}
		List<int> list2 = list.Select((BotPoint p) => (int)Math.Sqrt(occupied.Min((BotPoint o) => Math.Abs(p.x - o.x)))).ToList();
		int num = list2.Sum();
		if (num == 0)
		{
			return list[random.Next(list.Count)];
		}
		int num2 = random.Next(num);
		int num3 = 0;
		for (int num4 = 0; num4 < list.Count; num4++)
		{
			num3 += list2[num4];
			if (num2 < num3)
			{
				return list[num4];
			}
		}
		return list[list.Count - 1];
	}
}
