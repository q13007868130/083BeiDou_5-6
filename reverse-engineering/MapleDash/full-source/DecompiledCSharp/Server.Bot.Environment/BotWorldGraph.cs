using System;
using System.Collections.Generic;

namespace Server.Bot.Environment;

public sealed class BotWorldGraph
{
	private readonly Dictionary<int, int[]> edges = new Dictionary<int, int[]>();

	private static readonly int[,] taxis = new int[6, 2]
	{
		{ 104000000, 1002007 },
		{ 100000000, 1012000 },
		{ 102000000, 1022001 },
		{ 101000000, 1032000 },
		{ 103000000, 1052016 },
		{ 120000000, 1092014 }
	};

	public int MapCount => edges.Count;

	public BotWorldGraph(string mapIndex)
	{
		Dictionary<string, object> root = MiniJsonDb.Parse(mapIndex) as Dictionary<string, object>;
		Build(root);
	}

	public BotWorldGraph(Dictionary<string, object> root)
	{
		Build(root);
	}

	private void Build(Dictionary<string, object> root)
	{
		foreach (KeyValuePair<string, object> item in (root?["neighbors"] as Dictionary<string, object>) ?? throw new ArgumentException("Original world graph is missing"))
		{
			List<object> list = (List<object>)item.Value;
			int[] array = new int[list.Count];
			for (int i = 0; i < list.Count; i++)
			{
				array[i] = Convert.ToInt32(list[i]);
			}
			edges[int.Parse(item.Key)] = array;
		}
	}

	public static BotWorldGraph Load()
	{
		return new BotWorldGraph(BotWarmup.Root("Bot/Maps"));
	}

	public IReadOnlyList<int> PortalNeighbors(int id)
	{
		if (!edges.TryGetValue(id, out var value))
		{
			return Array.Empty<int>();
		}
		return value;
	}

	public static int TaxiNpc(int from, int to)
	{
		int result = 0;
		bool flag = false;
		for (int i = 0; i < taxis.GetLength(0); i++)
		{
			if (taxis[i, 0] == from)
			{
				result = taxis[i, 1];
			}
			if (taxis[i, 0] == to)
			{
				flag = true;
			}
		}
		if (!((from != to) & flag))
		{
			return 0;
		}
		return result;
	}

	public IEnumerable<int> Neighbors(int id)
	{
		foreach (int item in PortalNeighbors(id))
		{
			yield return item;
		}
		for (int i = 0; i < taxis.GetLength(0); i++)
		{
			if (taxis[i, 0] != id)
			{
				continue;
			}
			for (int j = 0; j < taxis.GetLength(0); j++)
			{
				if (i != j)
				{
					yield return taxis[j, 0];
				}
			}
		}
	}

	public List<int> Route(int from, int to, int maxHops)
	{
		if (from == to)
		{
			return new List<int>();
		}
		if (maxHops <= 0)
		{
			return null;
		}
		Dictionary<int, int> dictionary = new Dictionary<int, int> { [from] = from };
		Queue<int> queue = new Queue<int>();
		queue.Enqueue(from);
		int num = 0;
		while (queue.Count > 0 && num < maxHops)
		{
			num++;
			for (int num2 = queue.Count; num2 > 0; num2--)
			{
				int num3 = queue.Dequeue();
				foreach (int item in Neighbors(num3))
				{
					if (dictionary.ContainsKey(item))
					{
						continue;
					}
					dictionary[item] = num3;
					if (item == to)
					{
						List<int> list = new List<int>();
						for (int num4 = to; num4 != from; num4 = dictionary[num4])
						{
							list.Add(num4);
						}
						list.Reverse();
						return list;
					}
					queue.Enqueue(item);
				}
			}
		}
		return null;
	}
}
