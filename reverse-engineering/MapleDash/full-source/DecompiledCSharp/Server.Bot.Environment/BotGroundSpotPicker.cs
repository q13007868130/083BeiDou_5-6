using System;
using System.Collections.Generic;
using Server.Field;

namespace Server.Bot.Environment;

public sealed class BotGroundSpotPicker
{
	private sealed class Candidate
	{
		public BotNavigationGraph.Region region;

		public int lo;

		public int hi;

		public int Weight => Math.Max(1, hi - lo + 1);
	}

	private readonly Random random;

	public BotGroundSpotPicker(Random random = null)
	{
		this.random = random ?? new Random();
	}

	private List<Candidate> Eligible(MapRuntime map, BotNavigationGraph.Point from, int x1, int x2)
	{
		List<Candidate> list = new List<Candidate>();
		if (map == null)
		{
			return list;
		}
		BotNavigationGraph botNavigationGraph = BotMovementService.Graphs.Get(map);
		HashSet<int> hashSet = BotMovementService.Reachable(botNavigationGraph, MapGeometryCache.Get(map).ground, from);
		int val = Math.Min(x1, x2);
		int val2 = Math.Max(x1, x2);
		foreach (BotNavigationGraph.Region region in botNavigationGraph.regions)
		{
			if (!region.isRopeRegion && !region.isLadder && (hashSet.Count == 0 || hashSet.Contains(region.id)))
			{
				int num = Math.Max(region.minX, val);
				int num2 = Math.Min(region.maxX, val2);
				if (num2 >= num)
				{
					list.Add(new Candidate
					{
						region = region,
						lo = num,
						hi = num2
					});
				}
			}
		}
		return list;
	}

	private Candidate Weighted(List<Candidate> candidates)
	{
		if (candidates.Count == 1)
		{
			return candidates[0];
		}
		long num = 0L;
		foreach (Candidate candidate in candidates)
		{
			num += candidate.Weight;
		}
		long num2 = (long)(random.NextDouble() * (double)num);
		long num3 = 0L;
		foreach (Candidate candidate2 in candidates)
		{
			num3 += candidate2.Weight;
			if (num2 < num3)
			{
				return candidate2;
			}
		}
		return candidates[candidates.Count - 1];
	}

	private int InSpan(int lo, int hi)
	{
		if (hi > lo)
		{
			return random.Next(lo, hi + 1);
		}
		return lo;
	}

	public BotNavigationGraph.Point? Pick(MapRuntime map, BotNavigationGraph.Point from, int lo = int.MinValue, int hi = int.MaxValue)
	{
		List<Candidate> list = Eligible(map, from, lo, hi);
		if (list.Count == 0)
		{
			return null;
		}
		Candidate candidate = Weighted(list);
		return candidate.region.PointAt(InSpan(candidate.lo, candidate.hi));
	}

	public List<BotNavigationGraph.Point> PickMany(MapRuntime map, BotNavigationGraph.Point from, int count, int lo = int.MinValue, int hi = int.MaxValue)
	{
		List<BotNavigationGraph.Point> list = new List<BotNavigationGraph.Point>();
		List<Candidate> list2 = Eligible(map, from, lo, hi);
		if (list2.Count == 0 || count <= 0)
		{
			return list;
		}
		Dictionary<int, List<int>> dictionary = new Dictionary<int, List<int>>();
		for (int i = 0; i < count; i++)
		{
			Candidate candidate = Weighted(list2);
			if (!dictionary.TryGetValue(candidate.region.id, out var value))
			{
				value = (dictionary[candidate.region.id] = new List<int>());
			}
			int num = candidate.lo;
			for (int j = 0; j < 8; j++)
			{
				num = InSpan(candidate.lo, candidate.hi);
				bool flag = true;
				foreach (int item in value)
				{
					if (Math.Abs(item - num) < 30)
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					break;
				}
			}
			value.Add(num);
			list.Add(candidate.region.PointAt(num));
		}
		return list;
	}
}
