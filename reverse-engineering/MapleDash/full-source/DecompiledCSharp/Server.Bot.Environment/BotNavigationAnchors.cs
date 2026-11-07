using System;
using System.Collections.Generic;
using Server.Field;

namespace Server.Bot.Environment;

public static class BotNavigationAnchors
{
	public static Dictionary<int, List<BotNavigationGraph.Point>> Build(MapRuntime map, BotWalkRegions regions, BotGroundIndex ground, BotGroundPhysics physics, BotMovementProfile profile)
	{
		Dictionary<int, HashSet<int>> dictionary = new Dictionary<int, HashSet<int>>();
		foreach (MapRope rope in map.ropes)
		{
			Feature(dictionary, regions, ground, rope.x, rope.Bottom - 1);
			Feature(dictionary, regions, ground, rope.x, rope.Top - 1);
			Feature(dictionary, regions, ground, rope.x, rope.Top - 60);
		}
		foreach (MapPortal portal in map.portals)
		{
			Feature(dictionary, regions, ground, portal.x, portal.y - 16);
			if (portal.targetMap != map.mapId)
			{
				continue;
			}
			foreach (MapPortal portal2 in map.portals)
			{
				if (portal2.name == portal.targetName)
				{
					Feature(dictionary, regions, ground, portal2.x, portal2.y - 16);
					break;
				}
			}
		}
		foreach (BotNavigationGraph.Region region in regions.regions)
		{
			if (!region.isRopeRegion)
			{
				BotNavigationGraph.Point left = region.Left;
				BotNavigationGraph.Point right = region.Right;
				Feature(dictionary, regions, ground, left.x, left.y + 1);
				Feature(dictionary, regions, ground, right.x, right.y + 1);
				if (region.Width <= 64)
				{
					BotNavigationGraph.Point center = region.Center;
					Feature(dictionary, regions, ground, center.x, center.y + 1);
				}
			}
		}
		Dictionary<int, List<BotNavigationGraph.Point>> dictionary2 = new Dictionary<int, List<BotNavigationGraph.Point>>();
		foreach (BotNavigationGraph.Region region2 in regions.regions)
		{
			if (!region2.isRopeRegion)
			{
				List<int> list = (dictionary.TryGetValue(region2.id, out var value) ? new List<int>(value) : new List<int>());
				list.Sort();
				dictionary2[region2.id] = Points(region2, list, physics, profile);
			}
		}
		return dictionary2;
	}

	private static void Feature(Dictionary<int, HashSet<int>> features, BotWalkRegions regions, BotGroundIndex ground, int x, int y)
	{
		FootholdSegment footholdSegment = ground.Below(x, y);
		if (footholdSegment != null && regions.byFoothold.TryGetValue(footholdSegment.id, out var value))
		{
			if (!features.TryGetValue(value, out var value2))
			{
				value2 = (features[value] = new HashSet<int>());
			}
			value2.Add(x);
		}
	}

	private static List<BotNavigationGraph.Point> Points(BotNavigationGraph.Region region, List<int> features, BotGroundPhysics physics, BotMovementProfile profile)
	{
		List<BotNavigationGraph.Point> list = new List<BotNavigationGraph.Point>();
		Add(list, region.Left);
		int num = Math.Max(8, BotGroundPhysics.JavaRound(profile.WalkVelocity(125.0) * 50.0 / 1000.0));
		if (region.Width > num * 2)
		{
			Add(list, region.PointAt(region.minX + num), 10);
			Add(list, region.PointAt(region.maxX - num), 10);
		}
		int num2 = Math.Max(1, (int)Math.Ceiling(profile.JumpSpeed(555f) * 0.05f / Math.Max(0.001f, 5f)));
		int num3 = Math.Max(num * 3, physics.WalkStep(profile) * num2);
		if (region.Width > num3 * 2)
		{
			Add(list, region.PointAt(region.minX + num3), 10);
			Add(list, region.PointAt(region.maxX - num3), 10);
		}
		if (region.Width > num * 2)
		{
			for (int i = region.minX + num; i <= region.maxX - num; i += Math.Max(physics.WalkStep(profile), 1))
			{
				Add(list, region.PointAt(i));
			}
		}
		foreach (BotNavigationGraph.Segment segment in region.segments)
		{
			Add(list, new BotNavigationGraph.Point(segment.x1, segment.y1), 10);
			Add(list, new BotNavigationGraph.Point(segment.x2, segment.y2), 10);
		}
		if (region.Width >= 160)
		{
			Add(list, region.Center);
		}
		if (region.Width >= 320)
		{
			Add(list, region.PointAt(region.minX + region.Width / 3));
			Add(list, region.PointAt(region.maxX - region.Width / 3));
		}
		Add(list, region.Right);
		foreach (int feature in features)
		{
			if (feature >= region.minX && feature <= region.maxX)
			{
				Add(list, region.PointAt(feature));
			}
		}
		list.Sort((BotNavigationGraph.Point a, BotNavigationGraph.Point b) => (a.x != b.x) ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
		return list;
	}

	private static void Add(List<BotNavigationGraph.Point> points, BotNavigationGraph.Point point, int spacing = 0)
	{
		foreach (BotNavigationGraph.Point point2 in points)
		{
			if ((point2.x == point.x && point2.y == point.y) || (spacing > 0 && Math.Abs(point2.x - point.x) <= spacing && Math.Abs(point2.y - point.y) <= 8))
			{
				return;
			}
		}
		points.Add(point);
	}
}
