using System;
using System.Collections.Generic;
using Server.Bot.Types.Pq;
using Server.Field;

namespace Server.Bot.Environment;

public static class BotMovementService
{
	private sealed class Entry
	{
		public BotMovementRuntime motion;

		public BotNavigationRuntime navigation;

		public BotTasks.Handle task;

		public BotNavigationGraph.Point? target;

		public Action arrival;

		public long progressAt;

		public long profileAt;

		public long portalDropAt;

		public int bestDistance = int.MaxValue;

		public int aiAccumulator;

		public int stuckMs;

		public int cooldown;

		public int airStuckTicks;

		public BotNavigationGraph.Point? stuckAt;

		public BotNavigationGraph.Point? airStuckAt;

		public bool coarse;

		public List<BotNavigationGraph.Edge> coarseEdges;

		public long coarseStarted;

		public long coarseDuration;
	}

	private static readonly Random random = new Random();

	private static readonly Dictionary<int, Entry> entries = new Dictionary<int, Entry>();

	public static readonly BotNavigationGraphs Graphs = new BotNavigationGraphs();

	public static Func<int, bool> IsObserved;

	public static Action<BotCharacter, byte[]> Broadcast;

	private static readonly Func<bool> sinkProbe = HasSink;

	public static Action<BotCharacter, float> ApplyFallDamage;

	public static Func<BotCharacter, MapPortal, bool> EnterPortal;

	public static Func<BotCharacter, MapRuntime, BotMovementProfile> ResolveProfile;

	public static bool HasSink()
	{
		return Broadcast != null;
	}

	public static bool Observed(int mapId)
	{
		if (IsObserved != null)
		{
			return IsObserved(mapId);
		}
		return false;
	}

	private static BotMovementProfile Profile(BotCharacter actor, MapRuntime map)
	{
		if (ResolveProfile == null)
		{
			return BotMovementProfile.FromCharacter(actor, (map.fieldLimit & 2) != 0, null, HasSnowShoes(actor));
		}
		return ResolveProfile(actor, map);
	}

	internal static bool HasSnowShoes(BotCharacter actor)
	{
		if (actor.equipment.TryGetValue(-7, out var value) && value != null)
		{
			return BotItemCatalog.Int(BotItemCatalog.Shared.Info(value.itemId), "fs") >= 1;
		}
		return false;
	}

	public static BotMovementRuntime Get(BotCharacter actor)
	{
		if (actor == null || !entries.TryGetValue(actor.id, out var value))
		{
			return null;
		}
		return value.motion;
	}

	public static bool IsMoving(BotCharacter actor)
	{
		if (actor != null && entries.TryGetValue(actor.id, out var value))
		{
			if (!value.target.HasValue && !value.motion.InAir && !value.motion.Climbing && value.motion.navEdge == null)
			{
				return value.portalDropAt > 0;
			}
			return true;
		}
		return false;
	}

	public static bool IsMovingToward(BotCharacter actor, int x, int y)
	{
		if (actor != null && entries.TryGetValue(actor.id, out var value) && value.target.HasValue && Math.Abs((long)value.target.Value.x - (long)x) < 16)
		{
			return Math.Abs((long)value.target.Value.y - (long)y) < 16;
		}
		return false;
	}

	public static bool PrepareNavigation(BotCharacter actor)
	{
		MapRuntime mapRuntime = ((actor == null) ? null : MapService.Get(actor.mapId));
		if (mapRuntime == null)
		{
			return false;
		}
		bool flag = Graphs.Prepare(mapRuntime);
		bool flag2 = Graphs.Prepare(mapRuntime, Profile(actor, mapRuntime));
		return flag & flag2;
	}

	public static BotMovementRuntime Enable(BotCharacter actor)
	{
		if (actor == null)
		{
			return null;
		}
		if (entries.TryGetValue(actor.id, out var value))
		{
			return value.motion;
		}
		MapRuntime mapRuntime = MapService.Get(actor.mapId);
		if (mapRuntime == null)
		{
			return null;
		}
		Entry entry = Create(actor, mapRuntime);
		entries[actor.id] = entry;
		BotNavigationGraph.Point? point = entry.motion.ground.GroundPoint((int)actor.x, (int)actor.y - 1);
		if (point.HasValue)
		{
			entry.motion.Teleport(point.Value);
		}
		Schedule(entry, 0L);
		return entry.motion;
	}

	private static Entry Create(BotCharacter actor, MapRuntime map)
	{
		BotMovementRuntime botMovementRuntime = new BotMovementRuntime(actor, map, Profile(actor, map));
		botMovementRuntime.IsObserved = Observed;
		botMovementRuntime.Broadcast = (BotCharacter bot, byte[] bytes) =>
		{
			Broadcast?.Invoke(bot, bytes);
		};
		botMovementRuntime.SinkReady = sinkProbe;
		botMovementRuntime.ApplyFallDamage = (BotCharacter bot, float distance) =>
		{
			ApplyFallDamage?.Invoke(bot, distance);
		};
		BotNavigationRuntime botNavigationRuntime = new BotNavigationRuntime(botMovementRuntime, Graphs.Get(map, botMovementRuntime.profile));
		botNavigationRuntime.UsePortal = (MapPortal portal) => EnterPortal != null && EnterPortal(actor, portal);
		return new Entry
		{
			motion = botMovementRuntime,
			navigation = botNavigationRuntime,
			profileAt = BotScheduler.Now()
		};
	}

	private static void Schedule(Entry e, long delay)
	{
		e.task = BotTasks.Schedule(() =>
		{
			if (!entries.TryGetValue(e.motion.actor.id, out var value) || value != e)
			{
				return;
			}
			try
			{
				Tick(e, BotScheduler.Now());
			}
			finally
			{
				if (entries.TryGetValue(e.motion.actor.id, out value) && value == e)
				{
					Schedule(e, Observed(e.motion.actor.mapId) ? 50 : (IsMoving(e.motion.actor) ? 500 : 2500));
				}
			}
		}, delay, "Schedule", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Environment/BotMovementService.cs");
	}

	public static void SetGrinding(BotCharacter actor, bool grinding)
	{
		BotMovementRuntime botMovementRuntime = Enable(actor);
		if (botMovementRuntime != null)
		{
			botMovementRuntime.grinding = grinding;
		}
	}

	public static void Teleport(BotCharacter actor, BotNavigationGraph.Point point)
	{
		BotMovementRuntime botMovementRuntime = Enable(actor);
		if (botMovementRuntime != null)
		{
			Stop(actor);
			botMovementRuntime.Teleport(point);
		}
	}

	public static void Move(BotCharacter actor, int x, int y, Action arrival = null)
	{
		if (Enable(actor) != null)
		{
			Entry entry = entries[actor.id];
			entry.target = new BotNavigationGraph.Point(x, y);
			entry.arrival = arrival;
			entry.bestDistance = int.MaxValue;
			entry.progressAt = BotScheduler.Now();
			entry.coarseEdges = null;
		}
	}

	public static void Stop(BotCharacter actor)
	{
		if (actor != null && entries.TryGetValue(actor.id, out var value))
		{
			value.target = null;
			value.arrival = null;
			value.coarseEdges = null;
			value.navigation.Clear();
		}
	}

	public static void Disable(BotCharacter actor)
	{
		if (actor != null && entries.TryGetValue(actor.id, out var value))
		{
			value.task?.Cancel();
			entries.Remove(actor.id);
		}
	}

	public static void Shutdown()
	{
		foreach (Entry value in entries.Values)
		{
			value.task?.Cancel();
		}
		entries.Clear();
	}

	private static void Tick(Entry e, long now)
	{
		BotCharacter actor = e.motion.actor;
		if (BotRegistry.Get(actor.id) != actor)
		{
			Disable(actor);
			return;
		}
		if (actor.mapId != e.motion.map.mapId)
		{
			MapRuntime mapRuntime = MapService.Get(actor.mapId);
			if (mapRuntime == null)
			{
				Disable(actor);
			}
			else if (!(BotStore.GetBotById(actor.id) is PqBot) || PrepareNavigation(actor))
			{
				Entry entry = Create(actor, mapRuntime);
				entry.target = e.target;
				entry.arrival = e.arrival;
				BotNavigationGraph.Point? point = entry.motion.ground.GroundPoint((int)actor.x, (int)actor.y - 1);
				if (point.HasValue && Observed(actor.mapId))
				{
					entry.motion.Teleport(new BotNavigationGraph.Point((int)actor.x, Math.Min((int)actor.y, point.Value.y - 60)));
					entry.portalDropAt = now + 1500 + random.Next(601);
				}
				else
				{
					entry.motion.Teleport(point ?? entry.motion.Position);
				}
				entries[actor.id] = entry;
				Schedule(entry, 0L);
			}
			return;
		}
		if (now - e.profileAt >= 20000)
		{
			e.profileAt = now;
			if (!Profile(actor, e.motion.map).Equals(e.motion.profile))
			{
				if (BotStore.GetBotById(actor.id) is PqBot && !PrepareNavigation(actor))
				{
					e.profileAt = now - 20000;
					return;
				}
				Entry entry2 = Create(actor, e.motion.map);
				entry2.target = e.target;
				entry2.arrival = e.arrival;
				entry2.progressAt = e.progressAt;
				entries[actor.id] = entry2;
				Schedule(entry2, 0L);
				return;
			}
		}
		if (actor.chair > 0)
		{
			return;
		}
		bool flag = Observed(actor.mapId);
		if (!flag && !IsMoving(actor))
		{
			return;
		}
		if (flag)
		{
			RecoverOutOfBounds(e);
		}
		if (e.coarse & flag)
		{
			e.coarse = false;
			e.coarseEdges = null;
			BotNavigationGraph.Point? point2 = e.motion.ground.GroundPoint((int)actor.x, (int)actor.y - 1);
			e.motion.Teleport(point2 ?? e.motion.Position);
			e.navigation.Clear();
		}
		if (e.motion.resting)
		{
			if (!e.motion.Climbing)
			{
				e.motion.Idle();
			}
			e.motion.Publish(now);
			return;
		}
		if (e.portalDropAt > 0)
		{
			if (now < e.portalDropAt)
			{
				e.motion.Idle();
				e.motion.Fall(0);
				e.motion.Publish(now);
			}
			else
			{
				e.portalDropAt = 0L;
				e.motion.Fall(0);
				e.motion.Publish(now);
			}
			return;
		}
		if (e.target.HasValue && !e.motion.InAir && !e.motion.Climbing)
		{
			int num = Math.Abs((int)actor.x - e.target.Value.x) + Math.Abs((int)actor.y - e.target.Value.y);
			if (e.progressAt == 0L)
			{
				e.progressAt = now;
			}
			if (num < e.bestDistance - 16)
			{
				e.bestDistance = num;
				e.progressAt = now;
			}
			else if (now - e.progressAt > 8000)
			{
				Stop(actor);
				e.motion.Idle();
				e.motion.Publish(now);
				return;
			}
		}
		if (!e.target.HasValue && !e.motion.InAir && !e.motion.Climbing && e.motion.navEdge == null)
		{
			e.motion.Idle();
			e.motion.Publish(now);
			return;
		}
		if (!flag && e.target.HasValue && !e.motion.InAir && !e.motion.Climbing)
		{
			AdvanceCoarse(e, now);
			return;
		}
		e.aiAccumulator += 50;
		bool flag2 = e.aiAccumulator >= 100;
		if (flag2)
		{
			e.aiAccumulator -= 100;
		}
		e.navigation.Tick(e.target ?? e.motion.Position, now, flag2, precise: true);
		DetectStuck(e, now);
		if (e.target.HasValue && Math.Abs(actor.x - (float)e.target.Value.x) <= 8f && Math.Abs(actor.y - (float)e.target.Value.y) <= 8f)
		{
			Arrive(e);
		}
	}

	private static void Arrive(Entry e)
	{
		e.target = null;
		e.bestDistance = int.MaxValue;
		Action arrival = e.arrival;
		e.arrival = null;
		arrival?.Invoke();
	}

	private static void AdvanceCoarse(Entry e, long now)
	{
		BotNavigationGraph.Point value = e.target.Value;
		BotNavigationGraph.Point position = e.motion.Position;
		if (e.coarseEdges == null && Math.Abs(position.x - value.x) <= 12 && Math.Abs(position.y - value.y) <= 12)
		{
			e.motion.Teleport(value);
			e.navigation.Clear();
			Arrive(e);
			return;
		}
		if (e.coarseEdges == null)
		{
			BotNavigationGraph botNavigationGraph = Graphs.Get(e.motion.map, e.motion.profile);
			int num = botNavigationGraph.FindRegionId(e.motion.ground, position);
			int num2 = botNavigationGraph.FindRegionId(e.motion.ground, value);
			List<BotNavigationGraph.Edge> list = ((num < 0 || num2 < 0) ? null : BotPathSearch.Run(botNavigationGraph, e.motion.map, position, num, num2, value, zeroHeuristic: true, 0L).path);
			if (list == null || list.Count == 0)
			{
				e.motion.Teleport(value);
				e.navigation.Clear();
				Arrive(e);
				return;
			}
			e.coarseEdges = list;
			e.coarseDuration = 0L;
			foreach (BotNavigationGraph.Edge item in list)
			{
				e.coarseDuration += Math.Max(0, item.cost);
			}
			e.coarseStarted = now;
		}
		e.coarse = true;
		long num3 = Math.Max(0L, now - e.coarseStarted);
		if (num3 >= e.coarseDuration)
		{
			e.motion.actor.x = value.x;
			e.motion.actor.y = value.y;
			e.coarseEdges = null;
			e.navigation.Clear();
			Arrive(e);
			return;
		}
		long num4 = 0L;
		foreach (BotNavigationGraph.Edge coarseEdge in e.coarseEdges)
		{
			long num5 = Math.Max(0, coarseEdge.cost);
			if (num3 < num4 + num5)
			{
				double num6 = Math.Min(1.0, (num3 - num4) / Math.Max(1L, num5));
				e.motion.actor.x = BotGroundPhysics.JavaRound((double)coarseEdge.startPoint.x + (double)(coarseEdge.endPoint.x - coarseEdge.startPoint.x) * num6);
				e.motion.actor.y = BotGroundPhysics.JavaRound((double)coarseEdge.startPoint.y + (double)(coarseEdge.endPoint.y - coarseEdge.startPoint.y) * num6);
				break;
			}
			num4 += num5;
		}
	}

	private static void RecoverOutOfBounds(Entry e)
	{
		MapRuntime map = e.motion.map;
		if (map.right > map.left && map.bottom > map.top)
		{
			BotNavigationGraph.Point position = e.motion.Position;
			if (position.x < map.left - 400 || position.x > map.right + 400 || position.y < map.top - 300 || position.y > map.bottom + 16)
			{
				int x = ((map.right > map.left) ? Math.Max(map.left, Math.Min(map.right, position.x)) : position.x);
				Recover(e, new BotNavigationGraph.Point(x, position.y));
				e.target = null;
			}
		}
	}

	private static void DetectStuck(Entry e, long now)
	{
		BotMovementRuntime motion = e.motion;
		BotNavigationGraph.Point position = motion.Position;
		e.cooldown = BotMovementRuntime.TickDown(e.cooldown);
		if (motion.InAir && !motion.Climbing)
		{
			if (!e.airStuckAt.HasValue || position.x != e.airStuckAt.Value.x || position.y != e.airStuckAt.Value.y)
			{
				e.airStuckTicks = 0;
				e.airStuckAt = position;
			}
			else if (++e.airStuckTicks >= 30)
			{
				Recover(e, e.target ?? motion.navigationTarget ?? position);
				e.airStuckTicks = 0;
				e.airStuckAt = null;
			}
		}
		else
		{
			e.airStuckTicks = 0;
			e.airStuckAt = null;
		}
		RecoverOutOfBounds(e);
		if (motion.InAir || motion.Climbing || (motion.navEdge == null && !e.target.HasValue))
		{
			e.stuckMs = 0;
			e.stuckAt = null;
			return;
		}
		if (!e.stuckAt.HasValue)
		{
			e.stuckAt = position;
			return;
		}
		if (Math.Abs(position.x - e.stuckAt.Value.x) > 8 || Math.Abs(position.y - e.stuckAt.Value.y) > 8)
		{
			e.stuckMs = 0;
			e.stuckAt = position;
		}
		else
		{
			e.stuckMs += 50;
		}
		if (e.stuckMs >= 500 && e.cooldown == 0)
		{
			e.stuckMs = 0;
			e.stuckAt = null;
			motion.Jump((random.Next(2) != 0) ? 1 : (-1));
			e.navigation.Clear();
			e.cooldown = 4950;
			motion.Publish(now);
		}
	}

	private static void Recover(Entry e, BotNavigationGraph.Point target)
	{
		MapRuntime map = e.motion.map;
		bool num = map.bottom > map.top && map.right > map.left;
		int num2 = ((map.right > map.left) ? Math.Max(map.left, Math.Min(map.right, target.x)) : target.x);
		BotNavigationGraph.Point? point = null;
		if (num)
		{
			int num3 = Math.Max(map.top, Math.Min(map.bottom, target.y));
			bool flag = false;
			int num4 = num3;
			int num5 = int.MaxValue;
			foreach (FootholdSegment foothold in map.footholds)
			{
				if (foothold.Wall || foothold.x1 > (float)num2 || foothold.x2 < (float)num2)
				{
					continue;
				}
				int num6 = BotGroundIndex.YAt(foothold, num2);
				if (num6 >= map.top && num6 <= map.bottom)
				{
					int num7 = Math.Abs(num6 - num3);
					if (num7 < num5)
					{
						num5 = num7;
						num4 = num6;
						flag = true;
					}
				}
			}
			point = new BotNavigationGraph.Point(num2, flag ? num4 : num3);
		}
		else
		{
			point = e.motion.ground.GroundPoint(target.x, target.y - 1);
		}
		e.motion.Teleport(point ?? target);
		e.navigation.Clear();
	}

	public static HashSet<int> Reachable(BotNavigationGraph graph, BotGroundIndex ground, BotNavigationGraph.Point from)
	{
		HashSet<int> hashSet = new HashSet<int>();
		int num = graph.FindRegionId(ground, from);
		if (num < 0)
		{
			return hashSet;
		}
		Queue<int> queue = new Queue<int>();
		hashSet.Add(num);
		queue.Enqueue(num);
		while (queue.Count > 0)
		{
			foreach (BotNavigationGraph.Edge item in graph.Outgoing(queue.Dequeue()))
			{
				if (hashSet.Add(item.toRegionId))
				{
					queue.Enqueue(item.toRegionId);
				}
			}
		}
		return hashSet;
	}
}
