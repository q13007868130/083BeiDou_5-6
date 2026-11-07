using System;
using System.Collections.Generic;
using Server.Bot.Environment;
using Server.Field;

namespace Server.Bot;

public static class BotPartyGrindFloors
{
	public readonly struct Floor(int centerY, int minX, int maxX)
	{
		public readonly int centerY = centerY;

		public readonly int minX = minX;

		public readonly int maxX = maxX;

		public bool ContainsY(int y)
		{
			return Math.Abs(y - centerY) <= 80;
		}

		public int ClampX(int x)
		{
			return Math.Max(minX, Math.Min(maxX, x));
		}
	}

	public const int Band = 80;

	public static int IndexFor(int slot, int count)
	{
		if (count > 0)
		{
			return (slot % count + count) % count;
		}
		return 0;
	}

	public static List<Floor> Cluster(IEnumerable<Floor> samples)
	{
		List<Floor> list = ((samples == null) ? new List<Floor>() : new List<Floor>(samples));
		for (int i = 1; i < list.Count; i++)
		{
			Floor value = list[i];
			int num = i;
			while (num > 0 && list[num - 1].centerY > value.centerY)
			{
				list[num] = list[num - 1];
				num--;
			}
			list[num] = value;
		}
		List<Floor> list2 = new List<Floor>();
		if (list.Count == 0)
		{
			return list2;
		}
		int num2 = list[0].centerY;
		int num3 = 1;
		int num4 = list[0].minX;
		int num5 = list[0].maxX;
		for (int j = 1; j < list.Count; j++)
		{
			Floor floor = list[j];
			if (Math.Abs(floor.centerY - Mean(num2, num3)) <= 80)
			{
				num2 += floor.centerY;
				num3++;
				num4 = Math.Min(num4, floor.minX);
				num5 = Math.Max(num5, floor.maxX);
			}
			else
			{
				list2.Add(new Floor(Mean(num2, num3), num4, num5));
				num2 = floor.centerY;
				num3 = 1;
				num4 = floor.minX;
				num5 = floor.maxX;
			}
		}
		list2.Add(new Floor(Mean(num2, num3), num4, num5));
		return list2;
	}

	private static int Mean(int sum, int count)
	{
		return (int)Math.Floor((float)sum / (float)count + 0.5f);
	}

	public static List<Floor> FromMap(MapRuntime map, BotNavigationGraph.Point fallback)
	{
		List<Floor> list = new List<Floor>();
		if (map != null)
		{
			foreach (BotNavigationGraph.Region region in BotMovementService.Graphs.Get(map, BotMovementProfile.Base).regions)
			{
				if (!region.isRopeRegion && !region.isLadder && region.Width >= 40)
				{
					list.Add(new Floor(region.Center.y, region.minX, region.maxX));
				}
			}
		}
		List<Floor> list2 = Cluster(list);
		if (list2.Count == 0)
		{
			list2.Add(new Floor(fallback.y, fallback.x - 400, fallback.x + 400));
		}
		return list2;
	}

	public static BotNavigationGraph.Point Station(MapRuntime map, Floor floor, int desiredX)
	{
		int x = floor.ClampX(desiredX);
		if (map != null)
		{
			return MapGeometryCache.Get(map).ground.GroundPoint(x, floor.centerY) ?? new BotNavigationGraph.Point(x, floor.centerY);
		}
		return new BotNavigationGraph.Point(x, floor.centerY);
	}
}
