using System;
using System.Collections.Generic;
using Server.Field;

namespace Server.Bot.Environment;

public sealed class BotGroundIndex
{
	private readonly Dictionary<int, List<FootholdSegment>> buckets = new Dictionary<int, List<FootholdSegment>>();

	private readonly int minX;

	public BotGroundIndex(MapRuntime map)
	{
		minX = int.MaxValue;
		foreach (FootholdSegment foothold in map.footholds)
		{
			if (!foothold.Wall)
			{
				minX = Math.Min(minX, (int)Math.Min(foothold.x1, foothold.x2));
			}
		}
		if (minX == int.MaxValue)
		{
			minX = 0;
			return;
		}
		foreach (FootholdSegment foothold2 in map.footholds)
		{
			if (foothold2.Wall)
			{
				continue;
			}
			int num = (int)Math.Min(foothold2.x1, foothold2.x2) - minX >> 6;
			int num2 = (int)Math.Max(foothold2.x1, foothold2.x2) - minX >> 6;
			for (int i = num; i <= num2; i++)
			{
				if (!buckets.TryGetValue(i, out var value))
				{
					value = (buckets[i] = new List<FootholdSegment>());
				}
				int num3 = value.Count;
				value.Add(foothold2);
				while (num3 > 0 && Compare(value[num3 - 1], foothold2) > 0)
				{
					value[num3] = value[num3 - 1];
					num3--;
				}
				value[num3] = foothold2;
			}
		}
	}

	private static int Compare(FootholdSegment a, FootholdSegment b)
	{
		if (!(a.y2 < b.y1))
		{
			if (!(a.y1 > b.y2))
			{
				return 0;
			}
			return 1;
		}
		return -1;
	}

	public static int YAt(FootholdSegment f, int x)
	{
		if (f.y1 == f.y2)
		{
			return (int)f.y1;
		}
		double num = Math.Abs(f.y2 - f.y1);
		double num2 = Math.Abs(f.x2 - f.x1);
		double num3 = Math.Abs((float)x - f.x1);
		double num4 = Math.Cos(Math.Atan(num2 / num)) * (num3 / Math.Cos(Math.Atan(num / num2)));
		if (!(f.y2 < f.y1))
		{
			return (int)f.y1 + (int)num4;
		}
		return (int)f.y1 - (int)num4;
	}

	public FootholdSegment Below(int x, int y)
	{
		if (buckets.TryGetValue(x - minX >> 6, out var value))
		{
			foreach (FootholdSegment item in value)
			{
				if (item.x1 <= (float)x && item.x2 >= (float)x && YAt(item, x) >= y)
				{
					return item;
				}
			}
		}
		return null;
	}

	public BotNavigationGraph.Point? PointBelow(int x, int y)
	{
		FootholdSegment footholdSegment = Below(x, y);
		if (footholdSegment != null)
		{
			return new BotNavigationGraph.Point(x, YAt(footholdSegment, x));
		}
		return null;
	}

	public BotNavigationGraph.Point? GroundAbove(int x, int y, int maxRise)
	{
		if (buckets.TryGetValue(x - minX >> 6, out var value))
		{
			foreach (FootholdSegment item in value)
			{
				if (!(item.x1 > (float)x) && !(item.x2 < (float)x))
				{
					int num = YAt(item, x);
					if (num < y && num >= y - maxRise)
					{
						return new BotNavigationGraph.Point(x, num);
					}
				}
			}
		}
		return null;
	}

	public FootholdSegment Ground(int x, int y)
	{
		FootholdSegment footholdSegment = Below(x, y);
		FootholdSegment footholdSegment2 = Below(x, y - 26);
		if (footholdSegment == null)
		{
			return footholdSegment2;
		}
		if (footholdSegment2 == null)
		{
			return footholdSegment;
		}
		if (Math.Abs(YAt(footholdSegment2, x) - y) >= Math.Abs(YAt(footholdSegment, x) - y))
		{
			return footholdSegment;
		}
		return footholdSegment2;
	}

	public BotNavigationGraph.Point? GroundPoint(int x, int y)
	{
		FootholdSegment footholdSegment = Ground(x, y);
		if (footholdSegment != null)
		{
			return new BotNavigationGraph.Point(x, YAt(footholdSegment, x));
		}
		return null;
	}

	public static bool CanWalkAcross(FootholdSegment from, FootholdSegment to)
	{
		if (from == null || to == null || from.Wall || to.Wall)
		{
			return false;
		}
		int num = int.MaxValue;
		int dx = 0;
		int dy = 0;
		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < 2; j++)
			{
				int value = (int)(((j == 0) ? to.x1 : to.x2) - ((i == 0) ? from.x1 : from.x2));
				int num2 = (int)(((j == 0) ? to.y1 : to.y2) - ((i == 0) ? from.y1 : from.y2));
				int num3 = Math.Abs(value) + Math.Abs(num2);
				if (num3 < num)
				{
					num = num3;
					dx = Math.Abs(value);
					dy = num2;
				}
			}
		}
		if (num <= 2)
		{
			return BotGroundPhysics.WalkableEndpoint(dx, dy);
		}
		return false;
	}
}
