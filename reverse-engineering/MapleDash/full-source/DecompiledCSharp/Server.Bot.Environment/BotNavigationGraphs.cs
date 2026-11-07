using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Server.Field;

namespace Server.Bot.Environment;

public sealed class BotNavigationGraphs
{
	private readonly Dictionary<(int, int, int, bool), BotNavigationGraph> graphs = new Dictionary<(int, int, int, bool), BotNavigationGraph>();

	private readonly LinkedList<(int, int, int, bool)> lru = new LinkedList<(int, int, int, bool)>();

	private readonly Dictionary<(int, int, int, bool), LinkedListNode<(int, int, int, bool)>> nodes = new Dictionary<(int, int, int, bool), LinkedListNode<(int, int, int, bool)>>();

	private readonly Dictionary<(int, int, int, bool), Task<BotNavigationGraph>> pending = new Dictionary<(int, int, int, bool), Task<BotNavigationGraph>>();

	public int Count => graphs.Count;

	public bool Prepare(MapRuntime map, BotMovementProfile profile = null)
	{
		if (map == null)
		{
			return false;
		}
		(int, int, int, bool) key = Key(map, profile);
		if (graphs.ContainsKey(key))
		{
			Touch(key);
			return true;
		}
		if (!pending.TryGetValue(key, out var value))
		{
			MapGeometry geometry = MapGeometryCache.Get(map);
			BotMovementProfile canonical = Canonical(map, profile);
			pending[key] = Task.Run(() => new BotNavigationGraphBuilder(map, canonical, geometry).Build());
			return false;
		}
		if (!value.IsCompleted)
		{
			return false;
		}
		pending.Remove(key);
		Cache(key, value.GetAwaiter().GetResult());
		return true;
	}

	private static BotMovementProfile Canonical(MapRuntime map, BotMovementProfile profile)
	{
		profile = profile ?? BotMovementProfile.Base;
		if (!profile.snowShoes || (map.footholdSpeed > 0f && map.footholdSpeed < 1f))
		{
			return profile;
		}
		return new BotMovementProfile(profile.speed, profile.jump);
	}

	private static (int, int, int, bool) Key(MapRuntime map, BotMovementProfile profile)
	{
		BotMovementProfile botMovementProfile = Canonical(map, profile);
		return (map.mapId, botMovementProfile.speed, botMovementProfile.jump, botMovementProfile.snowShoes);
	}

	private void Touch((int, int, int, bool) key)
	{
		if (nodes.TryGetValue(key, out var value))
		{
			if (lru.Last != value)
			{
				lru.Remove(value);
				lru.AddLast(value);
			}
		}
		else
		{
			nodes[key] = lru.AddLast(key);
		}
	}

	private void Cache((int, int, int, bool) key, BotNavigationGraph graph)
	{
		graphs[key] = graph;
		Touch(key);
		while (graphs.Count > 512)
		{
			LinkedListNode<(int, int, int, bool)> first = lru.First;
			if (first == null)
			{
				break;
			}
			lru.RemoveFirst();
			nodes.Remove(first.Value);
			graphs.Remove(first.Value);
		}
	}

	public BotNavigationGraph Get(MapRuntime map, BotMovementProfile profile = null)
	{
		if (map == null)
		{
			return null;
		}
		(int, int, int, bool) key = Key(map, profile);
		if (graphs.TryGetValue(key, out var value))
		{
			Touch(key);
			return value;
		}
		using (BotPerf.Timer("BotNavigation.BuildSync"))
		{
			BotNavigationGraph botNavigationGraph = new BotNavigationGraphBuilder(map, Canonical(map, profile)).Build();
			Cache(key, botNavigationGraph);
			return botNavigationGraph;
		}
	}

	public BotNavigationGraph Rebuild(MapRuntime map, BotMovementProfile profile = null)
	{
		BotNavigationGraph botNavigationGraph = new BotNavigationGraphBuilder(map, profile).Build();
		BotMovementProfile botMovementProfile = profile ?? BotMovementProfile.Base;
		Cache((map.mapId, botMovementProfile.speed, botMovementProfile.jump, botMovementProfile.snowShoes), botNavigationGraph);
		return botNavigationGraph;
	}

	public BotNavigationGraph Peek(MapRuntime map, BotMovementProfile profile)
	{
		if (map == null)
		{
			return null;
		}
		(int, int, int, bool) key = Key(map, profile);
		if (!graphs.TryGetValue(key, out var value))
		{
			return null;
		}
		Touch(key);
		return value;
	}

	public BotNavigationGraph PeekAny(MapRuntime map)
	{
		if (map == null)
		{
			return null;
		}
		foreach (KeyValuePair<(int, int, int, bool), BotNavigationGraph> graph in graphs)
		{
			if (graph.Key.Item1 == map.mapId)
			{
				Touch(graph.Key);
				return graph.Value;
			}
		}
		return null;
	}

	public BotNavigationGraph PeekClosest(MapRuntime map, BotMovementProfile profile = null)
	{
		if (map == null)
		{
			return null;
		}
		BotMovementProfile botMovementProfile = Canonical(map, profile);
		BotNavigationGraph botNavigationGraph = null;
		int num = int.MaxValue;
		(int, int, int, bool) key = default;
		foreach (KeyValuePair<(int, int, int, bool), BotNavigationGraph> graph in graphs)
		{
			if (graph.Key.Item1 == map.mapId)
			{
				int num2 = Math.Abs(graph.Key.Item2 - botMovementProfile.speed) + Math.Abs(graph.Key.Item3 - botMovementProfile.jump) + ((graph.Key.Item4 != botMovementProfile.snowShoes) ? 1000 : 0);
				if (botNavigationGraph == null || num2 < num)
				{
					botNavigationGraph = graph.Value;
					key = graph.Key;
					num = num2;
				}
			}
		}
		if (botNavigationGraph != null)
		{
			Touch(key);
		}
		return botNavigationGraph;
	}

	public BotNavigationGraph PeekBest(MapRuntime map, BotMovementProfile profile = null)
	{
		return Peek(map, profile) ?? PeekClosest(map, profile);
	}
}
