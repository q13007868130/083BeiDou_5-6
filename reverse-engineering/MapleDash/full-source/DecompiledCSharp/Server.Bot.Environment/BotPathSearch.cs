using System;
using System.Collections.Generic;
using Server.Field;

namespace Server.Bot.Environment;

public static class BotPathSearch
{
	private readonly struct State : IEquatable<State>
	{
		public readonly int region;

		public readonly int x;

		public readonly int y;

		public readonly bool viaPortal;

		public BotNavigationGraph.Point Point => new BotNavigationGraph.Point(x, y);

		public State(int region, BotNavigationGraph.Point point, bool viaPortal)
		{
			this.region = region;
			x = point.x;
			y = point.y;
			this.viaPortal = viaPortal;
		}

		public bool Equals(State other)
		{
			if (region == other.region && x == other.x && y == other.y)
			{
				return viaPortal == other.viaPortal;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is State other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return ((region * 31 + x) * 31 + y) * 31 + (viaPortal ? 1 : 0);
		}
	}

	private readonly struct Node(State state, int cost, int score)
	{
		public readonly State state = state;

		public readonly int cost = cost;

		public readonly int score = score;
	}

	private sealed class Open
	{
		private readonly List<Node> nodes = new List<Node>();

		public int Count => nodes.Count;

		public void Add(Node node)
		{
			int num = nodes.Count;
			nodes.Add(node);
			while (num > 0)
			{
				int num2 = num - 1 >> 1;
				if (node.score >= nodes[num2].score)
				{
					break;
				}
				nodes[num] = nodes[num2];
				num = num2;
			}
			nodes[num] = node;
		}

		public Node Pop()
		{
			Node result = nodes[0];
			Node value = nodes[nodes.Count - 1];
			nodes.RemoveAt(nodes.Count - 1);
			if (nodes.Count == 0)
			{
				return result;
			}
			int num = 0;
			int num2 = nodes.Count >> 1;
			while (num < num2)
			{
				int num3 = num * 2 + 1;
				int num4 = num3 + 1;
				if (num4 < nodes.Count && nodes[num3].score > nodes[num4].score)
				{
					num3 = num4;
				}
				if (value.score <= nodes[num3].score)
				{
					break;
				}
				nodes[num] = nodes[num3];
				num = num3;
			}
			nodes[num] = value;
			return result;
		}
	}

	public sealed class Outcome
	{
		public readonly List<BotNavigationGraph.Edge> path;

		public readonly int cost;

		public readonly int expanded;

		public readonly bool usesPortal;

		public Outcome(List<BotNavigationGraph.Edge> path, int cost, int expanded)
		{
			this.path = path;
			this.cost = cost;
			this.expanded = expanded;
			foreach (BotNavigationGraph.Edge item in path)
			{
				if (item.type == BotNavigationGraph.EdgeType.Portal)
				{
					usesPortal = true;
				}
			}
		}
	}

	public static Outcome Run(BotNavigationGraph graph, MapRuntime map, BotNavigationGraph.Point start, int startRegion, int targetRegion, BotNavigationGraph.Point target, bool zeroHeuristic = true, long routeSeed = 0L)
	{
		bool flag = routeSeed != 0;
		double epsilon = (flag ? (1.0 + HashFraction(routeSeed, 15290385L) * 0.15) : 0.0);
		Open open = new Open();
		Dictionary<State, int> dictionary = new Dictionary<State, int>();
		Dictionary<State, State> dictionary2 = new Dictionary<State, State>();
		Dictionary<State, BotNavigationGraph.Edge> dictionary3 = new Dictionary<State, BotNavigationGraph.Edge>();
		State state = new State(startRegion, start, viaPortal: false);
		State? state2 = null;
		int num = int.MaxValue;
		int num2 = 0;
		int num3 = 0;
		dictionary[state] = 0;
		open.Add(new Node(state, 0, Heuristic(graph, start, target, zeroHeuristic, flag, epsilon)));
		while (open.Count > 0 && num3 < 200000)
		{
			Node node = open.Pop();
			if (node.cost != dictionary[node.state])
			{
				continue;
			}
			if (state2.HasValue && node.score >= num)
			{
				break;
			}
			num2++;
			if (node.state.region == targetRegion)
			{
				int num4 = node.cost + TravelCost(graph, node.state.region, node.state.Point, target);
				if (num4 < num)
				{
					num = num4;
					state2 = node.state;
				}
			}
			foreach (BotNavigationGraph.Edge item in graph.Outgoing(node.state.region))
			{
				bool flag2 = item.type == BotNavigationGraph.EdgeType.Portal;
				if (flag2 && !PortalEnabled(map, item.portalId))
				{
					continue;
				}
				bool flag3 = node.state.viaPortal && SamePoint(node.state.Point, item.startPoint);
				int num5 = ((flag2 & flag3) ? 250 : item.cost);
				int num6;
				BotNavigationGraph.Point point;
				if (item.type == BotNavigationGraph.EdgeType.Drop)
				{
					num6 = ((item.launchStepX == 0) ? 1 : 0);
					if (num6 != 0)
					{
						point = item.NearestLaunch(node.state.x);
						goto IL_01ea;
					}
				}
				else
				{
					num6 = 0;
				}
				point = item.startPoint;
				goto IL_01ea;
				IL_01ea:
				BotNavigationGraph.Point to = point;
				BotNavigationGraph.Point point2 = ((num6 != 0) ? new BotNavigationGraph.Point(to.x, item.endPoint.y) : item.endPoint);
				int num7 = TravelCost(graph, node.state.region, node.state.Point, to) + num5;
				if (flag)
				{
					num7 += BotGroundPhysics.JavaRound((double)num7 * 0.55 * HashFraction(routeSeed, EdgeKey(item)));
				}
				int num8 = node.cost + num7;
				State state3 = new State(item.toRegionId, point2, flag2);
				if (!dictionary.TryGetValue(state3, out var value) || num8 < value)
				{
					num3++;
					dictionary[state3] = num8;
					dictionary2[state3] = node.state;
					dictionary3[state3] = item;
					int score = num8 + Heuristic(graph, item.endPoint, target, zeroHeuristic, flag, epsilon);
					open.Add(new Node(state3, num8, score));
					if (num3 >= 200000)
					{
						break;
					}
				}
			}
		}
		List<BotNavigationGraph.Edge> list = new List<BotNavigationGraph.Edge>();
		if (state2.HasValue && dictionary3.ContainsKey(state2.Value))
		{
			State key = state2.Value;
			while (!key.Equals(state))
			{
				if (!dictionary3.TryGetValue(key, out var value2) || !dictionary2.TryGetValue(key, out var value3))
				{
					list.Clear();
					break;
				}
				list.Add(value2);
				key = value3;
			}
			list.Reverse();
		}
		return new Outcome(list, num, num2);
	}

	private static bool PortalEnabled(MapRuntime map, int id)
	{
		foreach (MapPortal portal in map.portals)
		{
			if (portal.id == id)
			{
				return portal.enabled;
			}
		}
		return false;
	}

	private static bool SamePoint(BotNavigationGraph.Point a, BotNavigationGraph.Point b)
	{
		if (a.x == b.x)
		{
			return a.y == b.y;
		}
		return false;
	}

	private static int TravelCost(BotNavigationGraph graph, int region, BotNavigationGraph.Point from, BotNavigationGraph.Point to)
	{
		bool flag = graph.GetRegion(region)?.isRopeRegion ?? false;
		return Math.Max(0, BotGroundPhysics.JavaRound((double)Math.Abs(flag ? (to.y - from.y) : (to.x - from.x)) * 1000.0 / Math.Max(1.0, flag ? 100.0 : graph.profile.WalkVelocity(125.0))));
	}

	private static int Heuristic(BotNavigationGraph graph, BotNavigationGraph.Point from, BotNavigationGraph.Point target, bool zero, bool random, double epsilon)
	{
		int num = Math.Max(0, BotGroundPhysics.JavaRound((double)Math.Abs(target.x - from.x) * 1000.0 / Math.Max(1.0, graph.profile.WalkVelocity(125.0))));
		if (!random)
		{
			if (!zero)
			{
				return num;
			}
			return 0;
		}
		return BotGroundPhysics.JavaRound(epsilon * (double)num);
	}

	public static long RouteSeed(int botId)
	{
		return Mix(botId) | 1;
	}

	private static long EdgeKey(BotNavigationGraph.Edge edge)
	{
		return (((long)edge.toRegionId * 31L + edge.startPoint.x) * 31 + edge.startPoint.y) * 31 + (long)edge.type;
	}

	private static double HashFraction(long seed, long key)
	{
		return (double)((ulong)Mix(seed ^ (key * -7046029254386353131L)) >> 11) * 1.1102230246251565E-16;
	}

	private static long Mix(long value)
	{
		long num = (value ^ (value >>> 30)) * -4658895280553007687L;
		long num2 = (num ^ (num >>> 27)) * -7723592293110705685L;
		return num2 ^ (num2 >>> 31);
	}
}
