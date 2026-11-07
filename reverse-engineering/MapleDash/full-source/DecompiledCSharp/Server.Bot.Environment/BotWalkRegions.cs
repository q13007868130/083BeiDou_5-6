using System;
using System.Collections.Generic;
using Server.Field;

namespace Server.Bot.Environment;

public sealed class BotWalkRegions
{
	public readonly struct Sample(BotNavigationGraph.Point point, FootholdSegment foothold)
	{
		public readonly BotNavigationGraph.Point point = point;

		public readonly FootholdSegment foothold = foothold;
	}

	public readonly List<BotNavigationGraph.Region> regions = new List<BotNavigationGraph.Region>();

	public readonly Dictionary<int, BotNavigationGraph.Region> byId = new Dictionary<int, BotNavigationGraph.Region>();

	public readonly Dictionary<int, int> byFoothold = new Dictionary<int, int>();

	public readonly Dictionary<int, FootholdSegment> footholds = new Dictionary<int, FootholdSegment>();

	public readonly HashSet<int> fromBelow;

	public readonly HashSet<int> walls;

	private readonly Dictionary<int, int> parent = new Dictionary<int, int>();

	private readonly Dictionary<int, int> rank = new Dictionary<int, int>();

	public BotWalkRegions(MapRuntime map)
	{
		fromBelow = BotFootholdCollision.FromBelow(map.footholds);
		walls = BotFootholdCollision.Walls(map.footholds);
		foreach (FootholdSegment foothold in map.footholds)
		{
			footholds[foothold.id] = foothold;
			if (!foothold.Wall)
			{
				parent[foothold.id] = foothold.id;
				rank[foothold.id] = 0;
			}
		}
		foreach (FootholdSegment foothold2 in map.footholds)
		{
			if (!foothold2.Wall)
			{
				if (footholds.TryGetValue(foothold2.previous, out var value))
				{
					Union(foothold2, value);
				}
				if (footholds.TryGetValue(foothold2.next, out var value2))
				{
					Union(foothold2, value2);
				}
			}
		}
		Dictionary<int, List<FootholdSegment>> dictionary = new Dictionary<int, List<FootholdSegment>>();
		foreach (FootholdSegment foothold3 in map.footholds)
		{
			if (!foothold3.Wall)
			{
				int key = Find(foothold3.id);
				if (!dictionary.TryGetValue(key, out var value3))
				{
					value3 = (dictionary[key] = new List<FootholdSegment>());
				}
				value3.Add(foothold3);
			}
		}
		List<List<FootholdSegment>> list2 = new List<List<FootholdSegment>>(dictionary.Values);
		bool flag = map.bottom > map.top;
		bool flag2 = map.right > map.left;
		if (flag | flag2)
		{
			List<List<FootholdSegment>> list3 = new List<List<FootholdSegment>>(list2.Count);
			foreach (List<FootholdSegment> item in list2)
			{
				float num = float.MaxValue;
				float num2 = float.MinValue;
				float num3 = float.MaxValue;
				float num4 = float.MinValue;
				foreach (FootholdSegment item2 in item)
				{
					num = Math.Min(num, Math.Min(item2.y1, item2.y2));
					num2 = Math.Max(num2, Math.Max(item2.y1, item2.y2));
					num3 = Math.Min(num3, Math.Min(item2.x1, item2.x2));
					num4 = Math.Max(num4, Math.Max(item2.x1, item2.x2));
				}
				if ((!flag || (!(num > (float)map.bottom) && !(num2 < (float)map.top))) && (!flag2 || (!(num4 < (float)map.left) && !(num3 > (float)map.right))))
				{
					list3.Add(item);
				}
			}
			list2 = list3;
		}
		for (int i = 1; i < list2.Count; i++)
		{
			List<FootholdSegment> list4 = list2[i];
			int num5 = i;
			while (num5 > 0 && CompareGroup(list2[num5 - 1], list4) > 0)
			{
				list2[num5] = list2[num5 - 1];
				num5--;
			}
			list2[num5] = list4;
		}
		int value4 = 1;
		foreach (List<FootholdSegment> item3 in list2)
		{
			item3.Sort((FootholdSegment a, FootholdSegment b) =>
			{
				int num6 = Math.Min(a.x1, a.x2).CompareTo(Math.Min(b.x1, b.x2));
				if (num6 == 0)
				{
					num6 = Math.Min(a.y1, a.y2).CompareTo(Math.Min(b.y1, b.y2));
				}
				return (num6 != 0) ? num6 : a.id.CompareTo(b.id);
			});
			List<BotNavigationGraph.Segment> list5 = new List<BotNavigationGraph.Segment>();
			foreach (FootholdSegment item4 in item3)
			{
				list5.Add(new BotNavigationGraph.Segment(item4, fromBelow.Contains(item4.id)));
				byFoothold[item4.id] = value4;
			}
			Add(new BotNavigationGraph.Region(value4++, list5));
		}
		foreach (MapRope rope in map.ropes)
		{
			Add(new BotNavigationGraph.Region(value4++, rope.x, rope.Top, rope.Bottom, rope.ladder));
		}
	}

	private void Add(BotNavigationGraph.Region region)
	{
		regions.Add(region);
		byId[region.id] = region;
	}

	private int Find(int id)
	{
		int num = id;
		while (parent[num] != num)
		{
			num = parent[num];
		}
		while (parent[id] != id)
		{
			int num2 = parent[id];
			parent[id] = num;
			id = num2;
		}
		return num;
	}

	private void Union(FootholdSegment a, FootholdSegment b)
	{
		if (!BotGroundIndex.CanWalkAcross(a, b))
		{
			return;
		}
		int num = Find(a.id);
		int num2 = Find(b.id);
		if (num != num2)
		{
			if (rank[num] < rank[num2])
			{
				parent[num] = num2;
				return;
			}
			if (rank[num] > rank[num2])
			{
				parent[num2] = num;
				return;
			}
			parent[num2] = num;
			rank[num]++;
		}
	}

	private static int CompareGroup(List<FootholdSegment> a, List<FootholdSegment> b)
	{
		float val = float.MaxValue;
		float val2 = float.MaxValue;
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		foreach (FootholdSegment item in a)
		{
			val = Math.Min(val, Math.Min(item.y1, item.y2));
			val2 = Math.Min(val2, Math.Min(item.x1, item.x2));
		}
		foreach (FootholdSegment item2 in b)
		{
			num = Math.Min(num, Math.Min(item2.y1, item2.y2));
			num2 = Math.Min(num2, Math.Min(item2.x1, item2.x2));
		}
		int num3 = val.CompareTo(num);
		if (num3 != 0)
		{
			return num3;
		}
		return val2.CompareTo(num2);
	}

	public Sample? GroundSample(FootholdSegment footing, int x, int referenceY)
	{
		if (footing == null || !byFoothold.TryGetValue(footing.id, out var value))
		{
			return null;
		}
		BotNavigationGraph.Region region = byId[value];
		bool flag = false;
		foreach (BotNavigationGraph.Segment segment in region.segments)
		{
			if (segment.ContainsX(x))
			{
				flag = true;
				break;
			}
		}
		Sample? result = null;
		int num = int.MaxValue;
		foreach (BotNavigationGraph.Segment segment2 in region.segments)
		{
			int num2 = Math.Max(segment2.minX - x, Math.Max(0, x - segment2.maxX));
			if (!segment2.ContainsX(x) && (flag || num2 > 2))
			{
				continue;
			}
			BotNavigationGraph.Point point = segment2.PointAt(x);
			int num3 = point.y - referenceY;
			if (num3 <= 16 && num3 >= -26)
			{
				int num4 = num2 * 1000 + Math.Abs(num3);
				if (!result.HasValue || num4 < num || (num4 == num && point.y > result.Value.point.y))
				{
					result = new Sample(point, footholds[segment2.footholdId]);
					num = num4;
				}
			}
		}
		return result;
	}
}
