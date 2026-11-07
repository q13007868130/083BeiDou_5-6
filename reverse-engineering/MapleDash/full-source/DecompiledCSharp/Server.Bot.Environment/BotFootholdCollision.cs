using System;
using System.Collections.Generic;
using Server.Field;

namespace Server.Bot.Environment;

public static class BotFootholdCollision
{
	private sealed class Loop
	{
		public readonly List<FootholdSegment> chain;

		public double minX = double.PositiveInfinity;

		public double maxX = double.NegativeInfinity;

		public double minY = double.PositiveInfinity;

		public double maxY = double.NegativeInfinity;

		public bool solid;

		public double Area => (maxX - minX) * (maxY - minY);

		public Loop(List<FootholdSegment> chain)
		{
			this.chain = chain;
			foreach (FootholdSegment item in chain)
			{
				minX = Math.Min(minX, Math.Min(item.x1, item.x2));
				maxX = Math.Max(maxX, Math.Max(item.x1, item.x2));
				minY = Math.Min(minY, Math.Min(item.y1, item.y2));
				maxY = Math.Max(maxY, Math.Max(item.y1, item.y2));
			}
		}

		public bool Contains(double x, double y)
		{
			bool flag = false;
			int num = 0;
			int index = chain.Count - 1;
			while (num < chain.Count)
			{
				FootholdSegment footholdSegment = chain[num];
				FootholdSegment footholdSegment2 = chain[index];
				if ((double)footholdSegment.y1 > y != (double)footholdSegment2.y1 > y && x < (double)(footholdSegment2.x1 - footholdSegment.x1) * (y - (double)footholdSegment.y1) / (double)(footholdSegment2.y1 - footholdSegment.y1) + (double)footholdSegment.x1)
				{
					flag = !flag;
				}
				index = num++;
			}
			return flag;
		}

		public bool Contains(Loop inner)
		{
			if (minX <= inner.minX && maxX >= inner.maxX && minY <= inner.minY && maxY >= inner.maxY)
			{
				return Contains(inner.chain[0].x1, inner.chain[0].y1);
			}
			return false;
		}

		public bool ThinShell(Loop inner)
		{
			if (inner.minX - minX <= 8.0 && maxX - inner.maxX <= 8.0 && inner.minY - minY <= 8.0)
			{
				return maxY - inner.maxY <= 8.0;
			}
			return false;
		}
	}

	public static HashSet<int> FromBelow(IReadOnlyList<FootholdSegment> footholds)
	{
		Dictionary<int, FootholdSegment> dictionary = new Dictionary<int, FootholdSegment>();
		foreach (FootholdSegment foothold in footholds)
		{
			dictionary[foothold.id] = foothold;
		}
		HashSet<int> hashSet = new HashSet<int>();
		List<Loop> list = new List<Loop>();
		foreach (FootholdSegment foothold2 in footholds)
		{
			if (hashSet.Contains(foothold2.id))
			{
				continue;
			}
			List<FootholdSegment> list2 = new List<FootholdSegment>();
			HashSet<int> hashSet2 = new HashSet<int>();
			FootholdSegment footholdSegment = foothold2;
			bool flag = false;
			while (footholdSegment != null && hashSet2.Add(footholdSegment.id) && list2.Count <= dictionary.Count)
			{
				list2.Add(footholdSegment);
				dictionary.TryGetValue(footholdSegment.next, out var value);
				if (value != null && value.id == foothold2.id)
				{
					flag = true;
					break;
				}
				footholdSegment = value;
			}
			if (!flag || list2.Count < 3)
			{
				continue;
			}
			bool flag2 = true;
			for (int i = 0; i < list2.Count; i++)
			{
				FootholdSegment footholdSegment2 = list2[i];
				FootholdSegment footholdSegment3 = list2[(i + 1) % list2.Count];
				if (footholdSegment2.x2 != footholdSegment3.x1 || footholdSegment2.y2 != footholdSegment3.y1)
				{
					flag2 = false;
					break;
				}
			}
			if (flag2)
			{
				hashSet.UnionWith(hashSet2);
				list.Add(new Loop(list2));
			}
		}
		for (int j = 1; j < list.Count; j++)
		{
			Loop loop = list[j];
			int num = j;
			while (num > 0 && list[num - 1].Area < loop.Area)
			{
				list[num] = list[num - 1];
				num--;
			}
			list[num] = loop;
		}
		HashSet<int> hashSet3 = new HashSet<int>();
		for (int k = 0; k < list.Count; k++)
		{
			Loop loop2 = list[k];
			Loop loop3 = null;
			double num2 = double.PositiveInfinity;
			for (int l = 0; l < k; l++)
			{
				if (list[l].Contains(loop2) && list[l].Area < num2)
				{
					loop3 = list[l];
					num2 = loop3.Area;
				}
			}
			loop2.solid = loop3 == null || (loop3.ThinShell(loop2) ? loop3.solid : (!loop3.solid));
			if (!loop2.solid)
			{
				continue;
			}
			foreach (FootholdSegment item in loop2.chain)
			{
				if (!item.Wall && item.y1 == item.y2)
				{
					double x = (double)(item.x1 + item.x2) / 2.0;
					if (loop2.Contains(x, item.y1 - 1f) && !loop2.Contains(x, item.y1 + 1f))
					{
						hashSet3.Add(item.id);
					}
				}
			}
		}
		return hashSet3;
	}

	public static HashSet<int> Walls(IReadOnlyList<FootholdSegment> footholds)
	{
		HashSet<int> hashSet = new HashSet<int>();
		foreach (FootholdSegment foothold in footholds)
		{
			if (foothold.Wall)
			{
				hashSet.Add(foothold.id);
			}
		}
		return hashSet;
	}
}
