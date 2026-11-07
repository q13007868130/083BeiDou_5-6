using System;
using System.Collections.Generic;
using Server.Field;

namespace Server.Bot.Environment;

public sealed class BotTownPresenceSampler
{
	public readonly struct Anchor(int x, int y, double strength)
	{
		public readonly int x = x;

		public readonly int y = y;

		public readonly double strength = strength;
	}

	private readonly Random random;

	public BotTownPresenceSampler(Random random = null)
	{
		this.random = random ?? new Random();
	}

	public List<BotNavigationGraph.Point> Sample(MapRuntime map, BotNavigationGraph.Point anchor, int count, IEnumerable<BotNavigationGraph.Point> npcs, BotTownOverrides overrides = null)
	{
		List<BotNavigationGraph.Point> list = new List<BotNavigationGraph.Point>();
		if (map == null || count <= 0)
		{
			return list;
		}
		BotTownOverrides botTownOverrides = overrides ?? new BotTownOverrides();
		BotGroundIndex ground = MapGeometryCache.Get(map).ground;
		foreach (BotNavigationGraph.Point pin in botTownOverrides.pins)
		{
			if (list.Count >= count)
			{
				return list;
			}
			list.Add(ground.GroundPoint(pin.x, pin.y) ?? pin);
		}
		BotNavigationGraph botNavigationGraph = BotMovementService.Graphs.Get(map);
		List<BotNavigationGraph.Region> list2 = botNavigationGraph.regions.FindAll((BotNavigationGraph.Region r) => !r.isRopeRegion && !r.isLadder);
		HashSet<int> reachable = BotMovementService.Reachable(botNavigationGraph, ground, anchor);
		List<BotNavigationGraph.Region> list3 = list2.FindAll((BotNavigationGraph.Region r) => reachable.Contains(r.id));
		if (list3.Count == 0)
		{
			list3 = list2;
		}
		if (list3.Count == 0)
		{
			return list;
		}
		List<Anchor> list4 = new List<Anchor>();
		if (npcs != null)
		{
			foreach (BotNavigationGraph.Point npc in npcs)
			{
				list4.Add(new Anchor(npc.x, npc.y, 1.0));
			}
		}
		foreach (MapPortal portal in map.portals)
		{
			list4.Add(new Anchor(portal.x, portal.y, 0.45));
		}
		int num = 1;
		foreach (BotNavigationGraph.Region item in list3)
		{
			num = Math.Max(num, item.Width);
		}
		int num2 = int.MinValue;
		foreach (BotNavigationGraph.Region item2 in list3)
		{
			if (item2.Width >= num / 2)
			{
				num2 = Math.Max(num2, item2.Center.y);
			}
		}
		if (num2 == int.MinValue)
		{
			num2 = list3[0].Center.y;
		}
		double[] array = new double[list3.Count];
		double num3 = 0.0;
		for (int num4 = 0; num4 < list3.Count; num4++)
		{
			BotNavigationGraph.Region region = list3[num4];
			BotNavigationGraph.Point center = region.Center;
			double num5 = 0.0;
			foreach (Anchor item3 in list4)
			{
				num5 += Pull(item3, Math.Max(region.minX, Math.Min(region.maxX, item3.x)), center.y);
			}
			array[num4] = (botTownOverrides.IsBanned(center.x, center.y) ? 0.0 : ((double)Math.Max(1, region.Width) * Math.Exp((double)(-Math.Max(0, num2 - center.y)) / 320.0) * (0.15 + num5) * botTownOverrides.Boost(center.x, center.y)));
			num3 += array[num4];
		}
		Dictionary<int, List<int>> dictionary = new Dictionary<int, List<int>>();
		while (list.Count < count)
		{
			BotNavigationGraph.Region region2;
			if (random.NextDouble() < 0.09 || num3 <= 0.0)
			{
				region2 = list3[random.Next(list3.Count)];
			}
			else
			{
				double num6 = random.NextDouble() * num3;
				double num7 = 0.0;
				region2 = list3[list3.Count - 1];
				for (int num8 = 0; num8 < list3.Count; num8++)
				{
					num7 += array[num8];
					if (num6 < num7)
					{
						region2 = list3[num8];
						break;
					}
				}
			}
			if (!dictionary.TryGetValue(region2.id, out var value))
			{
				value = (dictionary[region2.id] = new List<int>());
			}
			int num9 = region2.minX;
			double num10 = double.MinValue;
			if (region2.maxX > region2.minX)
			{
				for (int num11 = 0; num11 < 7; num11++)
				{
					int num12 = random.Next(region2.minX, region2.maxX + 1);
					if (botTownOverrides.IsBanned(num12, region2.Center.y))
					{
						continue;
					}
					double num13 = 0.0;
					double num14 = 0.0;
					foreach (Anchor item4 in list4)
					{
						num13 += Pull(item4, num12, region2.Center.y);
					}
					foreach (int item5 in value)
					{
						if (Math.Abs(item5 - num12) < 30)
						{
							num14 += (double)(30 - Math.Abs(item5 - num12));
						}
					}
					double num15 = num13 * botTownOverrides.Boost(num12, region2.Center.y) - num14;
					if (num15 > num10)
					{
						num10 = num15;
						num9 = num12;
					}
				}
			}
			value.Add(num9);
			list.Add(region2.PointAt(num9));
		}
		return list;
	}

	private static double Pull(Anchor a, int x, int y)
	{
		double num = (double)a.x - (double)x;
		double num2 = (double)a.y - (double)y;
		return a.strength * Math.Exp((0.0 - num) * num / 135200.0) * Math.Exp((0.0 - num2) * num2 / 33800.0);
	}
}
