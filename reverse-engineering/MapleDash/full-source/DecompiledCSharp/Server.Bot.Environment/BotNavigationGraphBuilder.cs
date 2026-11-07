using System;
using System.Collections.Generic;
using Server.Field;

namespace Server.Bot.Environment;

public sealed class BotNavigationGraphBuilder
{
	private readonly MapRuntime map;

	private readonly BotMovementProfile profile;

	private readonly BotWalkRegions regions;

	private readonly BotGroundIndex ground;

	private readonly BotGroundPhysics physics = new BotGroundPhysics();

	private readonly BotAirCollision collision;

	private readonly BotGroundMotion motion;

	private readonly BotTrajectory trajectory;

	private readonly Dictionary<int, List<BotNavigationGraph.Point>> anchors;

	private readonly Dictionary<int, List<BotNavigationGraph.Edge>> outgoing = new Dictionary<int, List<BotNavigationGraph.Edge>>();

	private readonly HashSet<string> edgeKeys = new HashSet<string>();

	private readonly Dictionary<(int, int, int), BotTrajectory.StableLanding> jumps = new Dictionary<(int, int, int), BotTrajectory.StableLanding>();

	private readonly Dictionary<(int, int, int, int, int, int), BotTrajectory.RopeGrab> ropeGrabs = new Dictionary<(int, int, int, int, int, int), BotTrajectory.RopeGrab>();

	public BotNavigationGraphBuilder(MapRuntime map, BotMovementProfile profile = null, MapGeometry geometry = null)
	{
		this.map = map;
		this.profile = profile ?? BotMovementProfile.Base;
		geometry = geometry ?? MapGeometryCache.Get(map);
		regions = geometry.regions;
		ground = geometry.ground;
		collision = geometry.collision;
		motion = new BotGroundMotion(map, ground, regions, collision, physics);
		trajectory = new BotTrajectory(map, collision, this.profile);
		anchors = BotNavigationAnchors.Build(map, regions, ground, physics, this.profile);
	}

	public BotNavigationGraph Build()
	{
		foreach (FootholdSegment foothold in map.footholds)
		{
			if (!foothold.Wall)
			{
				if (regions.footholds.TryGetValue(foothold.previous, out var value))
				{
					Walk(foothold, value);
				}
				if (regions.footholds.TryGetValue(foothold.next, out var value2))
				{
					Walk(foothold, value2);
				}
			}
		}
		foreach (BotNavigationGraph.Region region in regions.regions)
		{
			if (!region.isRopeRegion)
			{
				DirectionalDrop(region, -1);
				DirectionalDrop(region, 1);
				DownJumps(region);
			}
		}
		foreach (BotNavigationGraph.Region region2 in regions.regions)
		{
			if (!region2.isRopeRegion)
			{
				Jumps(region2);
			}
		}
		foreach (BotNavigationGraph.Region region3 in regions.regions)
		{
			if (region3.isRopeRegion)
			{
				RopeEntries(region3);
			}
		}
		foreach (BotNavigationGraph.Region region4 in regions.regions)
		{
			if (region4.isRopeRegion)
			{
				RopeExits(region4);
			}
		}
		foreach (MapPortal portal in map.portals)
		{
			Portal(portal);
		}
		return new BotNavigationGraph(map.mapId, 61, profile, regions.regions, regions.byId, regions.byFoothold, outgoing, regions.walls, regions.fromBelow);
	}

	private void Add(int from, int to, BotNavigationGraph.EdgeType type, BotNavigationGraph.Point start, BotNavigationGraph.Point end, int min, int max, int step, int portal, int cost, int ropeX = 0, int ropeTop = 0, int ropeBottom = 0)
	{
		string item = from + ":" + to + ":" + type.ToString() + ":" + start.x + ":" + start.y + ":" + end.x + ":" + end.y + ":" + step + ":" + portal + ":" + ropeX + ":" + ropeTop + ":" + ropeBottom + ":" + min + ":" + max;
		if (edgeKeys.Add(item))
		{
			if (!outgoing.TryGetValue(from, out var value))
			{
				value = (outgoing[from] = new List<BotNavigationGraph.Edge>());
			}
			value.Add(new BotNavigationGraph.Edge(from, to, type, start, end, min, max, step, portal, ropeX, ropeTop, ropeBottom, cost));
		}
	}

	private int BelowRegion(BotNavigationGraph.Point point)
	{
		FootholdSegment footholdSegment = ground.Below(point.x, point.y);
		if (footholdSegment == null || !regions.byFoothold.TryGetValue(footholdSegment.id, out var value))
		{
			return -1;
		}
		return value;
	}

	private int FootholdRegion(FootholdSegment f)
	{
		if (f == null || !regions.byFoothold.TryGetValue(f.id, out var value))
		{
			return -1;
		}
		return value;
	}

	private int TravelCost(int dx)
	{
		return Math.Max(0, BotGroundPhysics.JavaRound((double)dx * 1000.0 / Math.Max(1.0, profile.WalkVelocity(125.0))));
	}

	private void Walk(FootholdSegment from, FootholdSegment to)
	{
		if (to.Wall)
		{
			return;
		}
		int num = FootholdRegion(from);
		int num2 = FootholdRegion(to);
		if (num < 0 || num2 < 0 || num == num2)
		{
			return;
		}
		int num3 = int.MaxValue;
		BotNavigationGraph.Point point = default;
		BotNavigationGraph.Point point2 = default;
		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < 2; j++)
			{
				BotNavigationGraph.Point point3 = new BotNavigationGraph.Point((int)((i == 0) ? from.x1 : from.x2), (int)((i == 0) ? from.y1 : from.y2));
				BotNavigationGraph.Point point4 = new BotNavigationGraph.Point((int)((j == 0) ? to.x1 : to.x2), (int)((j == 0) ? to.y1 : to.y2));
				int num4 = Math.Abs(point4.x - point3.x) + Math.Abs(point4.y - point3.y);
				if (num4 < num3)
				{
					num3 = num4;
					point = point3;
					point2 = point4;
				}
			}
		}
		if (BotGroundPhysics.WalkableEndpoint(Math.Abs(point2.x - point.x), point2.y - point.y))
		{
			point = regions.byId[num].PointAt(point.x);
			point2 = regions.byId[num2].PointAt(point2.x);
			int cost = TravelCost(Math.Abs(point2.x - point.x));
			Add(num, num2, BotNavigationGraph.EdgeType.Walk, point, point2, point.x, point.x, 0, 0, cost);
			Add(num2, num, BotNavigationGraph.EdgeType.Walk, point2, point, point2.x, point2.x, 0, 0, cost);
		}
	}

	private void Portal(MapPortal portal)
	{
		if (portal.targetMap != map.mapId)
		{
			return;
		}
		MapPortal mapPortal = null;
		foreach (MapPortal portal2 in map.portals)
		{
			if (portal2.name == portal.targetName)
			{
				mapPortal = portal2;
				break;
			}
		}
		if (mapPortal != null && mapPortal.id != portal.id)
		{
			int num = BelowRegion(new BotNavigationGraph.Point(portal.x, portal.y - 16));
			int num2 = BelowRegion(new BotNavigationGraph.Point(mapPortal.x, mapPortal.y - 16));
			if (num >= 0 && num2 >= 0)
			{
				BotNavigationGraph.Point start = regions.byId[num].PointAt(portal.x);
				BotNavigationGraph.Point end = regions.byId[num2].PointAt(mapPortal.x);
				Add(num, num2, BotNavigationGraph.EdgeType.Portal, start, end, start.x, start.x, 0, portal.id, 0);
			}
		}
	}

	private void DirectionalDrop(BotNavigationGraph.Region from, int direction)
	{
		BotNavigationGraph.Point point = ((direction < 0) ? from.Left : from.Right);
		if (from.ForbidFallDownAt(point.x))
		{
			return;
		}
		int num = physics.LaunchRunway(map.footholdSpeed, profile);
		int num2 = ((direction < 0) ? Math.Min(from.maxX, point.x + num) : Math.Max(from.minX, point.x - num));
		int num3 = Math.Abs(num2 - point.x);
		if (num3 < Math.Min(num, 20))
		{
			return;
		}
		BotNavigationGraph.Point point2 = from.PointAt(num2);
		if (collision.Wall(point2, point, allowWalkableEndpoint: true).kind == BotAirCollision.Kind.Wall)
		{
			return;
		}
		int num4 = physics.WalkStep(profile) * direction;
		BotTrajectory.Landing landing = trajectory.Fall(point, num4);
		if (landing != null)
		{
			int num5 = FootholdRegion(landing.foothold);
			if (num5 >= 0 && num5 != from.id && landing.point.y > point.y + 4 && landing.point.y - point.y <= 300)
			{
				Add(from.id, num5, BotNavigationGraph.EdgeType.Drop, point2, landing.point, point2.x, point2.x, num4, 0, landing.TimeMs + TravelCost(num3));
			}
		}
	}

	private int DropStep(BotNavigationGraph.Region from, BotNavigationGraph.Point point)
	{
		BotNavigationGraph.Point left = from.Left;
		BotNavigationGraph.Point right = from.Right;
		if (Math.Abs(point.x - left.x) <= 10 && Math.Abs(point.y - left.y) <= 12)
		{
			return -physics.WalkStep(profile);
		}
		if (Math.Abs(point.x - right.x) <= 10 && Math.Abs(point.y - right.y) <= 12)
		{
			return physics.WalkStep(profile);
		}
		return 0;
	}

	private bool WallBoundary(BotNavigationGraph.Point launch)
	{
		foreach (FootholdSegment foothold in map.footholds)
		{
			if (foothold.Wall && foothold.x1 == (float)launch.x && (float)launch.y > Math.Min(foothold.y1, foothold.y2) && (float)launch.y <= Math.Max(foothold.y1, foothold.y2))
			{
				return true;
			}
		}
		return false;
	}

	private bool Approachable(BotNavigationGraph.Region from, int x)
	{
		if (WallBoundary(from.PointAt(x)))
		{
			return false;
		}
		if (x <= from.minX || !CanWalkTo(from, x - 1, x))
		{
			if (x < from.maxX)
			{
				return CanWalkTo(from, x + 1, x);
			}
			return false;
		}
		return true;
	}

	private bool CanWalkTo(BotNavigationGraph.Region from, int x, int launchX)
	{
		BotNavigationGraph.Point position = from.PointAt(x);
		BotNavigationGraph.Point point = from.PointAt(launchX);
		if (position.x != point.x || position.y != point.y)
		{
			return motion.CanStep(position, point.x - position.x);
		}
		return false;
	}

	private BotTrajectory.Landing ValidDown(BotNavigationGraph.Region from, int x, int requiredTarget = int.MinValue)
	{
		BotNavigationGraph.Point point = from.PointAt(x);
		FootholdSegment footholdSegment = ground.Ground(point.x, point.y);
		if (WallBoundary(point) || footholdSegment == null || footholdSegment.forbidFallDown || from.ForbidFallDownAt(x) || DropStep(from, point) != 0)
		{
			return null;
		}
		BotTrajectory.Landing landing = trajectory.DownJump(point);
		if (landing == null || landing.point.y <= point.y + 4 || landing.point.y - point.y > 200)
		{
			return null;
		}
		int num = FootholdRegion(landing.foothold);
		if (num < 0 || num == from.id || (requiredTarget != int.MinValue && num != requiredTarget))
		{
			return null;
		}
		return landing;
	}

	private static int Boundary(BotNavigationGraph.Region from, int start, bool left, Func<int, bool> valid)
	{
		int num = (left ? from.minX : from.maxX);
		int num2 = start;
		int num3 = start;
		int num4 = 1;
		while (true)
		{
			int num5 = (left ? Math.Max(num, start - num4) : Math.Min(num, start + num4));
			if (num5 == num2)
			{
				break;
			}
			if (!valid(num5))
			{
				num3 = num5;
				break;
			}
			num2 = num5;
			if (num5 == num)
			{
				return num5;
			}
			num4 *= 2;
		}
		while (Math.Abs(num2 - num3) > 1)
		{
			int num6 = (num2 + num3) / 2;
			if (valid(num6))
			{
				num2 = num6;
			}
			else
			{
				num3 = num6;
			}
		}
		return num2;
	}

	private void DownJumps(BotNavigationGraph.Region from)
	{
		foreach (BotNavigationGraph.Point item in anchors[from.id])
		{
			if (DropStep(from, item) != 0)
			{
				continue;
			}
			BotTrajectory.Landing landing = ValidDown(from, item.x);
			if (landing == null)
			{
				continue;
			}
			int target = FootholdRegion(landing.foothold);
			Func<int, bool> valid = (int x) => ValidDown(from, x, target) != null;
			int num = Boundary(from, item.x, left: true, valid);
			int num2 = Boundary(from, item.x, left: false, valid);
			BotNavigationGraph.Point start = from.PointAt((num + num2) / 2);
			landing = ValidDown(from, start.x, target);
			if (landing != null)
			{
				int num3 = BelowRegion(landing.point);
				if (num3 >= 0 && num3 != from.id)
				{
					Add(from.id, num3, BotNavigationGraph.EdgeType.Drop, start, landing.point, num, num2, 0, 0, landing.TimeMs + 2000);
				}
			}
		}
	}

	private BotTrajectory.StableLanding Jump(BotNavigationGraph.Point start, int step)
	{
		(int, int, int) key = (start.x, start.y, step);
		if (!jumps.TryGetValue(key, out var value))
		{
			value = trajectory.PostLanding(trajectory.Jump(start, step), Math.Sign(step), motion, physics);
			jumps[key] = value;
		}
		return value;
	}

	private bool ValidJump(BotNavigationGraph.Region from, int x, int step, int target)
	{
		if (!Approachable(from, x))
		{
			return false;
		}
		BotTrajectory.StableLanding stableLanding = Jump(from.PointAt(x), step);
		if (stableLanding != null && !stableLanding.lostGround)
		{
			return FootholdRegion(stableLanding.foothold) == target;
		}
		return false;
	}

	private void Jumps(BotNavigationGraph.Region from)
	{
		int num = physics.WalkStep(profile);
		foreach (BotNavigationGraph.Point item in anchors[from.id])
		{
			int[] array = new int[3]
			{
				-num,
				0,
				num
			};
			foreach (int step in array)
			{
				BotTrajectory.StableLanding stableLanding = Jump(item, step);
				if (stableLanding == null || stableLanding.lostGround)
				{
					continue;
				}
				int target = FootholdRegion(stableLanding.foothold);
				BotTrajectory.Landing landing = stableLanding.landing;
				if (target >= 0 && target != from.id && (Math.Abs(landing.point.x - item.x) >= 6 || Math.Abs(landing.point.y - item.y) >= 6) && ValidJump(from, item.x, step, target))
				{
					Func<int, bool> valid = (int x) => ValidJump(from, x, step, target);
					int num2 = Boundary(from, item.x, left: true, valid);
					int num3 = Boundary(from, item.x, left: false, valid);
					BotNavigationGraph.Point start = from.PointAt((num2 + num3) / 2);
					stableLanding = Jump(start, step);
					if (stableLanding != null && !stableLanding.lostGround && FootholdRegion(stableLanding.foothold) == target)
					{
						Add(from.id, target, BotNavigationGraph.EdgeType.Jump, start, stableLanding.landing.point, num2, num3, step, 0, stableLanding.landing.TimeMs);
					}
				}
			}
		}
	}

	private MapRope Rope(BotNavigationGraph.Region region)
	{
		foreach (MapRope rope in map.ropes)
		{
			if (rope.x == region.minX && rope.Top == region.minY && rope.Bottom == region.maxY)
			{
				return rope;
			}
		}
		return null;
	}

	private BotTrajectory.RopeGrab Grab(BotNavigationGraph.Point start, int step, MapRope rope)
	{
		(int, int, int, int, int, int) key = (start.x, start.y, step, rope.x, rope.Top, rope.Bottom);
		if (!ropeGrabs.TryGetValue(key, out var value))
		{
			value = trajectory.Grab(start, step, rope);
			ropeGrabs[key] = value;
		}
		return value;
	}

	private bool ReachableRope(BotNavigationGraph.Point from, MapRope rope)
	{
		if (BotRopeMotion.CanGrab(from, rope))
		{
			return true;
		}
		if (rope.Top >= from.y)
		{
			return false;
		}
		float num = profile.JumpSpeed(555f) * 0.05f;
		float num2 = 5f;
		int num3 = (int)Math.Ceiling(num * num / (2f * num2));
		float num4 = num / num2;
		float num5 = num * num / (2f * num2);
		float num6 = (float)Math.Sqrt(2f * (num5 + (float)Math.Max(0, rope.Bottom - from.y)) / num2);
		int num7 = Math.Max(1, (int)Math.Ceiling(num4 + num6));
		if (rope.Bottom >= from.y - num3)
		{
			return Math.Abs(rope.x - from.x) <= physics.WalkStep(profile) * num7;
		}
		return false;
	}

	private void RopeEntries(BotNavigationGraph.Region ropeRegion)
	{
		MapRope rope = Rope(ropeRegion);
		if (rope == null)
		{
			return;
		}
		int num = BotRopeMotion.FirstClimbableY(rope);
		int num2 = physics.WalkStep(profile);
		foreach (BotNavigationGraph.Region from in regions.regions)
		{
			if (from.isRopeRegion)
			{
				continue;
			}
			foreach (BotNavigationGraph.Point item in anchors[from.id])
			{
				bool flag = Math.Abs(item.x - rope.x) <= 8;
				if (flag && item.y >= num && item.y <= rope.Bottom)
				{
					Add(end: new BotNavigationGraph.Point(rope.x, Math.Max(num, Math.Min(item.y, rope.Bottom))), from: from.id, to: ropeRegion.id, type: BotNavigationGraph.EdgeType.Climb, start: item, min: item.x, max: item.x, step: 0, portal: 0, cost: 50);
					continue;
				}
				if (flag && item.y < rope.Top && rope.Top - item.y <= 16)
				{
					Add(from.id, ropeRegion.id, BotNavigationGraph.EdgeType.Climb, item, new BotNavigationGraph.Point(rope.x, num), item.x, item.x, 0, 0, 50);
					continue;
				}
				if (flag && item.y <= rope.Top + 30)
				{
					BotTrajectory.RopeGrab ropeGrab = trajectory.Grab(item, 0, rope, ropeJump: false, downJump: true);
					if (ropeGrab != null)
					{
						Add(from.id, ropeRegion.id, BotNavigationGraph.EdgeType.Climb, item, ropeGrab.point, item.x, item.x, 0, 0, ropeGrab.TimeMs);
					}
				}
				if (!ReachableRope(item, rope))
				{
					continue;
				}
				int[] array = new int[3]
				{
					-num2,
					0,
					num2
				};
				foreach (int step in array)
				{
					Func<int, bool> func = (int x) => Approachable(from, x) && Grab(from.PointAt(x), step, rope) != null;
					if (func(item.x))
					{
						int num3 = Boundary(from, item.x, left: true, func);
						int num4 = Boundary(from, item.x, left: false, func);
						BotNavigationGraph.Point start = from.PointAt((num3 + num4) / 2);
						BotTrajectory.RopeGrab ropeGrab2 = Grab(start, step, rope);
						if (ropeGrab2 != null)
						{
							Add(from.id, ropeRegion.id, BotNavigationGraph.EdgeType.Climb, start, ropeGrab2.point, num3, num4, step, 0, ropeGrab2.TimeMs);
						}
					}
				}
			}
		}
	}

	private void RopeExits(BotNavigationGraph.Region region)
	{
		MapRope mapRope = Rope(region);
		if (mapRope == null)
		{
			return;
		}
		int num = BotRopeMotion.FirstClimbableY(mapRope);
		int num2 = physics.WalkStep(profile);
		BotNavigationGraph.Point? point = new BotRopeMotion(ground).TopExitLanding(mapRope);
		if (point.HasValue)
		{
			int num3 = FootholdRegion(ground.Ground(point.Value.x, point.Value.y));
			if (num3 >= 0 && !regions.byId[num3].isRopeRegion)
			{
				Add(region.id, num3, BotNavigationGraph.EdgeType.Climb, new BotNavigationGraph.Point(mapRope.x, mapRope.Top), point.Value, mapRope.x, mapRope.x, 0, 0, 50);
			}
		}
		List<int> list = new List<int> { num };
		for (int i = mapRope.Top + 30; i < mapRope.Bottom; i += 30)
		{
			if (i > num)
			{
				list.Add(i);
			}
		}
		if (list[list.Count - 1] != mapRope.Bottom)
		{
			list.Add(mapRope.Bottom);
		}
		foreach (int item in list)
		{
			int[] array = new int[3]
			{
				-num2,
				0,
				num2
			};
			foreach (int num4 in array)
			{
				BotNavigationGraph.Point start = new BotNavigationGraph.Point(mapRope.x, item);
				BotTrajectory.Landing landing = trajectory.RopeJump(start, num4);
				if (landing != null)
				{
					int num5 = FootholdRegion(landing.foothold);
					if (num5 >= 0 && !regions.byId[num5].isRopeRegion)
					{
						Add(region.id, num5, BotNavigationGraph.EdgeType.Climb, start, landing.point, start.x, start.x, num4, 0, landing.TimeMs);
					}
				}
			}
		}
		list.Clear();
		for (int k = num; k <= mapRope.Bottom; k += 5)
		{
			list.Add(k);
		}
		if (list.Count == 0 || list[list.Count - 1] != mapRope.Bottom)
		{
			list.Add(mapRope.Bottom);
		}
		foreach (int item2 in list)
		{
			foreach (BotNavigationGraph.Region region2 in regions.regions)
			{
				if (!region2.isRopeRegion || region2.id == region.id)
				{
					continue;
				}
				MapRope mapRope2 = Rope(region2);
				if (mapRope2 != null && Math.Abs(mapRope.x - mapRope2.x) <= num2 * 30)
				{
					int num6 = ((mapRope2.x > mapRope.x) ? num2 : (-num2));
					BotNavigationGraph.Point start2 = new BotNavigationGraph.Point(mapRope.x, item2);
					BotTrajectory.RopeGrab ropeGrab = trajectory.Grab(start2, num6, mapRope2, ropeJump: true);
					if (ropeGrab != null)
					{
						Add(region.id, region2.id, BotNavigationGraph.EdgeType.Climb, start2, ropeGrab.point, start2.x, start2.x, num6, 0, ropeGrab.TimeMs);
					}
				}
			}
		}
	}
}
