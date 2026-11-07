using System;
using System.Collections.Generic;
using Server.Field;

namespace Server.Bot.Environment;

public sealed class BotNavigationRuntime
{
	private readonly BotMovementRuntime motion;

	private readonly BotNavigationGraph graph;

	private readonly Random random;

	private readonly BotTrajectory trajectory;

	private readonly BotGroundMotion groundMotion;

	private int targetRegion = -1;

	private int selectedLaunchX = int.MinValue;

	private int launchDelay = int.MinValue;

	private BotNavigationGraph.Edge selectedLaunchEdge;

	private int portalReadyTicks = -1;

	private int blockedTicks;

	private int blockedGiveUp;

	private BotNavigationGraph.Point blockedPosition;

	private long portalCooldown;

	private string blockedReason;

	public Func<MapPortal, bool> UsePortal;

	public string LastDecision { get; private set; }

	public BotNavigationRuntime(BotMovementRuntime motion, BotNavigationGraph graph, Random random = null)
	{
		this.motion = motion;
		this.graph = graph;
		this.random = random ?? new Random();
		BotAirCollision collision = motion.collision;
		trajectory = new BotTrajectory(motion.map, collision, motion.profile);
		groundMotion = new BotGroundMotion(motion.map, motion.ground, motion.regions, collision, motion.physics);
	}

	public void Clear()
	{
		motion.ClearNavigation();
		targetRegion = -1;
		selectedLaunchEdge = null;
		selectedLaunchX = (launchDelay = int.MinValue);
		blockedTicks = 0;
	}

	public int CurrentRegion()
	{
		if (motion.Climbing || motion.actor.stance == 14 || motion.actor.stance == 16)
		{
			int num = graph.RopeRegion(motion.Position);
			if (num >= 0)
			{
				return num;
			}
		}
		if (!motion.InAir)
		{
			return graph.FindRegionId(motion.ground, motion.Position);
		}
		return -1;
	}

	public int TargetRegion(BotNavigationGraph.Point point)
	{
		int num = graph.RopeRegion(point);
		BotNavigationGraph.Point? point2 = motion.ground.GroundPoint(point.x, point.y);
		if (num < 0 || (point2.HasValue && point2.Value.y - point.y <= 16))
		{
			return graph.FindRegionId(motion.ground, point);
		}
		return num;
	}

	public bool Tick(BotNavigationGraph.Point? rawTarget, long now, bool runAi = true, bool precise = false)
	{
		BotNavigationGraph.Point? target = Resolve(rawTarget, now, runAi, out var consumed);
		if (!consumed)
		{
			if (precise && motion.navEdge == null)
			{
				motion.preciseTarget = true;
			}
			motion.Tick(target, now, runAi);
			if (!motion.InAir && !motion.Climbing && motion.navEdge != null)
			{
				BotNavigationGraph.Edge edge = Reuse(CurrentRegion(), targetRegion);
				if (edge == null)
				{
					Clear();
				}
				else if (Execute(edge, now, runAi: true))
				{
					LastDecision = "exec";
					motion.Publish(now);
				}
			}
		}
		else
		{
			motion.Publish(now);
		}
		return consumed;
	}

	public BotNavigationGraph.Point? Resolve(BotNavigationGraph.Point? rawTarget, long now, bool runAi, out bool consumed)
	{
		consumed = false;
		if (motion.map.swim || !rawTarget.HasValue)
		{
			Clear();
			return rawTarget;
		}
		int num = CurrentRegion();
		int num2 = TargetRegion(rawTarget.Value);
		BotNavigationGraph.Point target = rawTarget.Value;
		BotNavigationGraph.Region region = graph.GetRegion(num2);
		if (motion.grinding && region != null && !region.isRopeRegion && region.minX + 40 < region.maxX - 40)
		{
			target = region.PointAt(Clamp(target.x, region.minX + 40, region.maxX - 40));
		}
		if (runAi && motion.navEdge != null && blockedTicks > 0 && blockedTicks >= blockedGiveUp)
		{
			Clear();
		}
		BotNavigationGraph.Edge edge = Reuse(num, num2);
		bool flag = edge != null;
		if ((flag & runAi) && num >= 0 && num2 >= 0 && num != num2 && !motion.InAir && (!motion.Climbing || (edge.type == BotNavigationGraph.EdgeType.Climb && edge.launchStepX != 0 && !CanClimbExit(edge))))
		{
			BotNavigationGraph.Edge edge2 = FindNext(num, num2, target);
			bool flag2 = !motion.Climbing && edge2 != null && edge.fromRegionId == edge2.fromRegionId && edge.toRegionId == edge2.toRegionId && edge.type != BotNavigationGraph.EdgeType.Walk && edge2.type != BotNavigationGraph.EdgeType.Walk;
			if (edge2 != null && !Same(edge, edge2) && !flag2)
			{
				edge = edge2;
				motion.navEdge = edge2;
				targetRegion = num2;
			}
		}
		if (((edge == null) & runAi) && num >= 0 && num2 >= 0)
		{
			edge = FindNext(num, num2, target);
			if (edge != null)
			{
				motion.navEdge = edge;
				targetRegion = num2;
			}
		}
		if (edge == null)
		{
			BotNavigationRuntime botNavigationRuntime = this;
			string lastDecision;
			if (runAi)
			{
				if (num < 0 || num2 < 0)
				{
					lastDecision = "no-region";
				}
				else
				{
					lastDecision = ((num == num2) ? "same-region" : "no-path");
				}
			}
			else
			{
				lastDecision = "no-ai";
			}
			botNavigationRuntime.LastDecision = lastDecision;
			Clear();
			return rawTarget;
		}
		if (Execute(edge, now, runAi))
		{
			LastDecision = "exec";
			blockedTicks = 0;
			consumed = true;
			return rawTarget;
		}
		if (!flag || blockedReason == null || !blockedReason.EndsWith("-pos", StringComparison.Ordinal))
		{
			blockedTicks = 0;
		}
		else
		{
			if (blockedTicks == 0 || !Equal(blockedPosition, motion.Position))
			{
				blockedTicks = 0;
				blockedGiveUp = 6 + random.Next(5);
				blockedPosition = motion.Position;
			}
			blockedTicks++;
		}
		motion.preciseTarget = Precise(edge);
		motion.navigationTarget = Waypoint(edge);
		LastDecision = (flag ? "reuse" : "new");
		return motion.navigationTarget;
	}

	private BotNavigationGraph.Edge Reuse(int start, int goal)
	{
		BotNavigationGraph.Edge navEdge = motion.navEdge;
		if (navEdge == null || goal < 0)
		{
			return null;
		}
		int num = targetRegion;
		targetRegion = goal;
		if (navEdge.type == BotNavigationGraph.EdgeType.Portal)
		{
			MapPortal mapPortal = Portal(navEdge.portalId);
			if (mapPortal == null || !mapPortal.enabled)
			{
				return null;
			}
		}
		if (motion.Climbing && navEdge.type == BotNavigationGraph.EdgeType.Climb)
		{
			BotNavigationGraph.Region region = graph.GetRegion(navEdge.fromRegionId);
			if (region != null && !region.isRopeRegion)
			{
				BotNavigationGraph.Region region2 = graph.GetRegion(navEdge.toRegionId);
				if (region2 != null && region2.isRopeRegion)
				{
					return null;
				}
			}
		}
		if (start == navEdge.toRegionId && !motion.InAir && !motion.Climbing && navEdge.fromRegionId != navEdge.toRegionId)
		{
			return null;
		}
		if (!motion.InAir && !motion.Climbing && start >= 0 && start == goal && navEdge.toRegionId != start && num != goal)
		{
			return null;
		}
		if (start == navEdge.fromRegionId)
		{
			if (!motion.InAir && !motion.Climbing && num >= 0 && num != goal && navEdge.toRegionId != goal)
			{
				return null;
			}
			return navEdge;
		}
		if (motion.Climbing && (start < 0 || start != navEdge.toRegionId))
		{
			return navEdge;
		}
		if (motion.InAir && (start < 0 || start == navEdge.toRegionId) && (navEdge.type == BotNavigationGraph.EdgeType.Drop || navEdge.type == BotNavigationGraph.EdgeType.Jump))
		{
			return navEdge;
		}
		if (!motion.InAir || navEdge.type != BotNavigationGraph.EdgeType.Climb || navEdge.launchStepX == 0)
		{
			return null;
		}
		return navEdge;
	}

	private BotNavigationGraph.Edge FindNext(int start, int goal, BotNavigationGraph.Point target)
	{
		List<BotNavigationGraph.Edge> path = BotPathSearch.Run(graph, motion.map, motion.Position, start, goal, target, zeroHeuristic: true, BotPathSearch.RouteSeed(motion.actor.id)).path;
		if (path.Count == 0)
		{
			return null;
		}
		BotNavigationGraph.Edge edge = path[0];
		if (edge.type != BotNavigationGraph.EdgeType.Walk || !NoMovement(edge))
		{
			return edge;
		}
		int num = 0;
		int num2 = 0;
		while (num < path.Count && path[num].type == BotNavigationGraph.EdgeType.Walk && NoMovement(path[num]))
		{
			num2 += path[num++].cost;
		}
		if (num == path.Count)
		{
			return null;
		}
		BotNavigationGraph.Edge edge2 = path[num];
		return new BotNavigationGraph.Edge(edge.fromRegionId, edge2.toRegionId, edge2.type, edge2.startPoint, edge2.endPoint, edge2.launchMinX, edge2.launchMaxX, edge2.launchStepX, edge2.portalId, edge2.ropeX, edge2.ropeTopY, edge2.ropeBottomY, num2 + edge2.cost);
	}

	private bool Execute(BotNavigationGraph.Edge edge, long now, bool runAi)
	{
		if (!runAi)
		{
			return false;
		}
		BotNavigationGraph.Point position = motion.Position;
		switch (edge.type)
		{
		case BotNavigationGraph.EdgeType.Jump:
			if (motion.InAir || motion.Climbing)
			{
				return false;
			}
			if (!CanJump(edge))
			{
				MapRope mapRope3 = Rope(graph.GetRegion(edge.fromRegionId));
				if (edge.startPoint.y > position.y && mapRope3 != null && BotRopeMotion.CanGrab(position, mapRope3))
				{
					motion.Attach(mapRope3, position.y);
					return true;
				}
				blockedReason = "jump-pos";
				return false;
			}
			if (Deepen(edge))
			{
				blockedReason = "jump-delay";
				return false;
			}
			if (edge.launchStepX == 0 && motion.physics.SlipScale(motion.map.footholdSpeed, motion.profile) < 1.0 && BotGroundPhysics.JavaRound(motion.travel.Speed * 50.0 / 8.0) != 0)
			{
				blockedReason = "jump-slide";
				return false;
			}
			SetExecution(edge);
			motion.Jump(edge.launchStepX);
			selectedLaunchEdge = null;
			selectedLaunchX = (launchDelay = int.MinValue);
			return true;
		case BotNavigationGraph.EdgeType.Drop:
			if (motion.InAir || motion.Climbing || motion.downJumpPending || edge.launchStepX != 0)
			{
				return false;
			}
			if (!WithinWindow(edge))
			{
				blockedReason = "drop-pos";
				return false;
			}
			SetExecution(edge);
			motion.QueueDownJump();
			motion.navigationTarget = edge.endPoint;
			return true;
		case BotNavigationGraph.EdgeType.Climb:
		{
			if (motion.InAir || motion.downJumpPending)
			{
				return false;
			}
			if (motion.Climbing)
			{
				if (!CanClimbExit(edge))
				{
					return false;
				}
				BotNavigationGraph.Region region = graph.GetRegion(edge.toRegionId);
				if (region != null && region.isRopeRegion)
				{
					MapRope mapRope = Rope(region);
					if (mapRope == null || SameRope(motion.climb.rope, mapRope))
					{
						return false;
					}
					motion.RopeJump(edge.launchStepX, transfer: true);
					return true;
				}
				if (edge.launchStepX == 0)
				{
					return false;
				}
				MapRope rope = Rope(graph.GetRegion(edge.fromRegionId));
				if (TopJumpReady(rope, position, edge) && position.y != edge.startPoint.y)
				{
					motion.Attach(rope, edge.startPoint.y);
				}
				motion.RopeJump(edge.launchStepX, transfer: false);
				return true;
			}
			MapRope mapRope2 = Rope(graph.GetRegion(edge.toRegionId));
			if (!CanClimbEntry(edge, mapRope2))
			{
				blockedReason = "climb-pos";
				return false;
			}
			if (BotRopeMotion.CanGrab(position, mapRope2))
			{
				motion.Attach(mapRope2, position.y);
			}
			else if (TopAttach(edge, mapRope2))
			{
				motion.Attach(mapRope2, edge.endPoint.y);
			}
			else if (TopGrab(edge, mapRope2))
			{
				motion.QueueRopeEntry(mapRope2, edge.endPoint.y);
			}
			else
			{
				if (!GroundRopeJump(edge) || (Math.Abs(position.x - mapRope2.x) > 10 && CanWalkTowardRope(mapRope2)))
				{
					blockedReason = "climb-reach";
					return false;
				}
				motion.Jump(edge.launchStepX, fixedArc: false, climbIntent: true);
			}
			blockedReason = null;
			return true;
		}
		case BotNavigationGraph.EdgeType.Portal:
		{
			if (motion.InAir || Math.Abs(position.x - edge.startPoint.x) > 14 || Math.Abs(position.y - edge.startPoint.y) > 60)
			{
				portalReadyTicks = -1;
				return false;
			}
			if (portalReadyTicks < 0)
			{
				portalReadyTicks = random.Next(4);
			}
			if (portalReadyTicks > 0)
			{
				portalReadyTicks--;
				return false;
			}
			portalReadyTicks = -1;
			MapPortal mapPortal = Portal(edge.portalId);
			if (now < portalCooldown || mapPortal == null || !mapPortal.enabled || UsePortal == null || !UsePortal(mapPortal))
			{
				return false;
			}
			portalCooldown = now + 250;
			Clear();
			motion.Reset();
			return true;
		}
		default:
			return false;
		}
	}

	private void SetExecution(BotNavigationGraph.Edge edge)
	{
		blockedReason = null;
		motion.preciseTarget = false;
		motion.navigationTarget = edge.endPoint;
	}

	private bool WithinWindow(BotNavigationGraph.Edge edge)
	{
		BotNavigationGraph.Region region = graph.GetRegion(edge.fromRegionId);
		if (edge.ContainsLaunchX(motion.Position.x) && region != null && (edge.type != BotNavigationGraph.EdgeType.Drop || !region.isRopeRegion))
		{
			return Math.Abs(motion.Position.y - region.PointAt(motion.Position.x).y) <= 30;
		}
		return false;
	}

	private bool CanJump(BotNavigationGraph.Edge edge)
	{
		if (WithinWindow(edge))
		{
			return Math.Abs(motion.Position.x - SelectedLaunch(edge)) <= motion.physics.WalkStep(motion.profile);
		}
		return false;
	}

	private int SelectedLaunch(BotNavigationGraph.Edge edge)
	{
		BotNavigationGraph.Region region = graph.GetRegion(edge.fromRegionId);
		if (region == null || region.isRopeRegion)
		{
			return edge.startPoint.x;
		}
		if (Same(selectedLaunchEdge, edge) && edge.ContainsLaunchX(selectedLaunchX))
		{
			return selectedLaunchX;
		}
		int num = Math.Max(edge.launchMinX, region.minX);
		int num2 = Math.Min(edge.launchMaxX, region.maxX);
		if (num > num2)
		{
			num = edge.launchMinX;
			num2 = edge.launchMaxX;
		}
		int num3 = Math.Min((num2 - num) / 2, Math.Max(1, motion.physics.WalkStep(motion.profile) * 2));
		selectedLaunchX = ((num + num3 >= num2 - num3) ? (num + num3) : random.Next(num + num3, num2 - num3 + 1));
		selectedLaunchEdge = edge;
		return selectedLaunchX;
	}

	private bool Deepen(BotNavigationGraph.Edge edge)
	{
		int num = Math.Sign(edge.launchStepX);
		if (selectedLaunchX == int.MinValue || num == 0)
		{
			return false;
		}
		if (launchDelay == int.MinValue)
		{
			launchDelay = random.Next(3);
		}
		if (launchDelay <= 0)
		{
			return false;
		}
		int num2 = selectedLaunchX + num * motion.physics.WalkStep(motion.profile);
		BotNavigationGraph.Region region = graph.GetRegion(edge.fromRegionId);
		if (!edge.ContainsLaunchX(num2) || region == null || region.isRopeRegion || num2 < region.minX || num2 > region.maxX)
		{
			launchDelay = 0;
			return false;
		}
		launchDelay--;
		selectedLaunchX = num2;
		return true;
	}

	private bool Precise(BotNavigationGraph.Edge edge)
	{
		if (motion.InAir)
		{
			return false;
		}
		switch (edge.type)
		{
		case BotNavigationGraph.EdgeType.Jump:
			return !CanJump(edge);
		case BotNavigationGraph.EdgeType.Drop:
			if (edge.launchStepX == 0)
			{
				return !WithinWindow(edge);
			}
			return false;
		case BotNavigationGraph.EdgeType.Climb:
			if (!motion.Climbing)
			{
				return !CanClimbEntry(edge, Rope(graph.GetRegion(edge.toRegionId)));
			}
			if (edge.launchStepX != 0)
			{
				return !CanClimbExit(edge);
			}
			return false;
		case BotNavigationGraph.EdgeType.Portal:
			if (Math.Abs(motion.Position.x - edge.startPoint.x) <= 14 && Math.Abs(motion.Position.y - edge.startPoint.y) <= 60)
			{
				return portalReadyTicks > 0;
			}
			return true;
		default:
			return !NoMovement(edge);
		}
	}

	private BotNavigationGraph.Point Waypoint(BotNavigationGraph.Edge edge)
	{
		BotNavigationGraph.Point position = motion.Position;
		switch (edge.type)
		{
		case BotNavigationGraph.EdgeType.Jump:
		{
			if (motion.InAir)
			{
				return edge.endPoint;
			}
			BotNavigationGraph.Region region = graph.GetRegion(edge.fromRegionId);
			if (region != null && !region.isRopeRegion)
			{
				return region.PointAt(SelectedLaunch(edge));
			}
			return edge.startPoint;
		}
		case BotNavigationGraph.EdgeType.Drop:
		{
			if (motion.InAir)
			{
				return edge.endPoint;
			}
			BotNavigationGraph.Region region2 = graph.GetRegion(edge.fromRegionId);
			if (edge.launchStepX == 0)
			{
				if (region2 != null && !region2.isRopeRegion)
				{
					return region2.PointAt(edge.ContainsLaunchX(position.x) ? position.x : SteerWindow(edge, position.x));
				}
				return edge.startPoint;
			}
			if ((edge.launchStepX > 0) ? (position.x >= edge.startPoint.x) : (position.x <= edge.startPoint.x))
			{
				return edge.endPoint;
			}
			if (region2 == null || region2.isRopeRegion || MatchesWalkOff(edge))
			{
				return edge.endPoint;
			}
			return edge.startPoint;
		}
		case BotNavigationGraph.EdgeType.Climb:
		{
			if (motion.InAir)
			{
				return edge.endPoint;
			}
			if (motion.Climbing && edge.launchStepX != 0)
			{
				if (!CanClimbExit(edge))
				{
					return edge.startPoint;
				}
				return position;
			}
			if (motion.Climbing)
			{
				return new BotNavigationGraph.Point(motion.climb.rope.x, edge.endPoint.y);
			}
			MapRope mapRope = Rope(graph.GetRegion(edge.toRegionId));
			if (mapRope != null && CanWalkTowardRope(mapRope))
			{
				return new BotNavigationGraph.Point(mapRope.x, position.y);
			}
			if (edge.launchStepX == 0 || edge.launchMaxX <= edge.launchMinX)
			{
				return edge.startPoint;
			}
			return new BotNavigationGraph.Point(SteerWindow(edge, position.x), edge.startPoint.y);
		}
		case BotNavigationGraph.EdgeType.Portal:
			return edge.startPoint;
		default:
			return edge.endPoint;
		}
	}

	private bool MatchesWalkOff(BotNavigationGraph.Edge edge)
	{
		BotNavigationGraph.Point position = motion.Position;
		FootholdSegment footholdSegment = motion.ground.Ground(position.x, position.y);
		BotGroundPhysics.Travel travel = motion.travel;
		if (footholdSegment == null)
		{
			return false;
		}
		for (int i = 0; i < 256; i++)
		{
			BotGroundMotion.Result result = groundMotion.Step(position, footholdSegment, Math.Sign(edge.launchStepX), travel, motion.profile);
			if (result.lostGround)
			{
				BotTrajectory.Landing landing = ((result.stepX == 0) ? null : trajectory.Fall(result.point, result.stepX));
				if (landing != null && graph.regionByFoothold.TryGetValue(landing.foothold.id, out var value) && value == edge.toRegionId && Math.Abs(landing.point.x - edge.endPoint.x) <= Math.Max(6, Math.Abs(edge.launchStepX) + 2))
				{
					return Math.Abs(landing.point.y - edge.endPoint.y) <= 60;
				}
				return false;
			}
			position = result.point;
			footholdSegment = result.foothold;
			travel = result.travel;
		}
		return false;
	}

	private bool CanClimbEntry(BotNavigationGraph.Edge edge, MapRope rope)
	{
		if (rope != null)
		{
			if (!BotRopeMotion.CanGrab(motion.Position, rope) && !TopAttach(edge, rope) && !TopGrab(edge, rope))
			{
				return GroundRopeJump(edge);
			}
			return true;
		}
		return false;
	}

	private bool GroundRopeJump(BotNavigationGraph.Edge edge)
	{
		if (edge.ContainsLaunchX(motion.Position.x))
		{
			return Math.Abs(motion.Position.y - edge.startPoint.y) <= 60;
		}
		return false;
	}

	private bool CanWalkTowardRope(MapRope rope)
	{
		if (rope == null)
		{
			return false;
		}
		BotNavigationGraph.Point position = motion.Position;
		if (rope.x - position.x == 0)
		{
			return false;
		}
		FootholdSegment footholdSegment = motion.ground.Ground(position.x, position.y);
		BotGroundPhysics.Travel travel = motion.travel;
		if (footholdSegment == null)
		{
			return false;
		}
		for (int i = 0; i < 256; i++)
		{
			if (Math.Abs(position.x - rope.x) <= 10)
			{
				return BotRopeMotion.CanGrab(position, rope);
			}
			BotGroundMotion.Result result = groundMotion.Step(position, footholdSegment, Math.Sign(rope.x - position.x), travel, motion.profile);
			if (result.lostGround || result.point.x == position.x)
			{
				return false;
			}
			position = result.point;
			footholdSegment = result.foothold;
			travel = result.travel;
		}
		return false;
	}

	private bool TopAttach(BotNavigationGraph.Edge edge, MapRope rope)
	{
		if (Math.Abs(motion.Position.x - rope.x) <= 10 && edge.endPoint.y == BotRopeMotion.FirstClimbableY(rope) && motion.Position.y < rope.Top)
		{
			return rope.Top - motion.Position.y <= 16;
		}
		return false;
	}

	private bool TopGrab(BotNavigationGraph.Edge edge, MapRope rope)
	{
		if (edge.startPoint.y <= rope.Top + 30)
		{
			return Math.Abs(motion.Position.x - rope.x) <= 10;
		}
		return false;
	}

	private bool CanClimbExit(BotNavigationGraph.Edge edge)
	{
		BotNavigationGraph.Point position = motion.Position;
		MapRope mapRope = Rope(graph.GetRegion(edge.fromRegionId));
		if (edge.launchStepX != 0 && position.y != edge.startPoint.y && !TopJumpReady(mapRope, position, edge))
		{
			return false;
		}
		BotNavigationGraph.Region region = graph.GetRegion(edge.toRegionId);
		if ((region != null && region.isRopeRegion) || edge.launchStepX != 0)
		{
			return Math.Abs(position.y - edge.startPoint.y) <= 60;
		}
		if (mapRope != null && edge.startPoint.y == mapRope.Top && Math.Abs(edge.endPoint.y - mapRope.Top) <= 60)
		{
			return position.y <= mapRope.Top + 60;
		}
		return false;
	}

	private static bool TopJumpReady(MapRope rope, BotNavigationGraph.Point pos, BotNavigationGraph.Edge edge)
	{
		if (rope != null && edge.launchStepX != 0 && edge.startPoint.x == rope.x && edge.startPoint.y == BotRopeMotion.FirstClimbableY(rope) && pos.x == rope.x && pos.y >= BotRopeMotion.FirstClimbableY(rope))
		{
			return pos.y <= BotRopeMotion.FirstClimbableY(rope) + 7;
		}
		return false;
	}

	private MapRope Rope(BotNavigationGraph.Region region)
	{
		if (region == null || !region.isRopeRegion)
		{
			return null;
		}
		foreach (MapRope rope in motion.map.ropes)
		{
			if (rope.x == region.minX && rope.Top == region.minY && rope.Bottom == region.maxY && rope.ladder == region.isLadder)
			{
				return rope;
			}
		}
		return null;
	}

	private MapPortal Portal(int id)
	{
		return motion.map.portals.Find((MapPortal p) => p.id == id);
	}

	private static bool SameRope(MapRope a, MapRope b)
	{
		if (a != null && b != null && a.x == b.x && a.Top == b.Top && a.Bottom == b.Bottom)
		{
			return a.ladder == b.ladder;
		}
		return false;
	}

	private static bool Equal(BotNavigationGraph.Point a, BotNavigationGraph.Point b)
	{
		if (a.x == b.x)
		{
			return a.y == b.y;
		}
		return false;
	}

	private static bool NoMovement(BotNavigationGraph.Edge edge)
	{
		if (Math.Abs(edge.endPoint.x - edge.startPoint.x) <= 4)
		{
			return Math.Abs(edge.endPoint.y - edge.startPoint.y) <= 4;
		}
		return false;
	}

	private static int SteerWindow(BotNavigationGraph.Edge edge, int x)
	{
		int num = Math.Min((edge.launchMaxX - edge.launchMinX) / 2, 4);
		return Clamp(x, edge.launchMinX + num, edge.launchMaxX - num);
	}

	private static int Clamp(int x, int min, int max)
	{
		return Math.Max(min, Math.Min(max, x));
	}

	private static bool Same(BotNavigationGraph.Edge a, BotNavigationGraph.Edge b)
	{
		if (a != b)
		{
			if (a != null && b != null && a.fromRegionId == b.fromRegionId && a.toRegionId == b.toRegionId && a.type == b.type && a.launchMinX == b.launchMinX && a.launchMaxX == b.launchMaxX && a.launchStepX == b.launchStepX && a.portalId == b.portalId && a.ropeX == b.ropeX && a.ropeTopY == b.ropeTopY && a.ropeBottomY == b.ropeBottomY && Equal(a.startPoint, b.startPoint))
			{
				return Equal(a.endPoint, b.endPoint);
			}
			return false;
		}
		return true;
	}
}
