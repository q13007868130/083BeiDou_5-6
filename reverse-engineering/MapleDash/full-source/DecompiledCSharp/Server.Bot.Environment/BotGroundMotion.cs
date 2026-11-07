using System;
using Server.Field;

namespace Server.Bot.Environment;

public sealed class BotGroundMotion
{
	public readonly struct Result(BotNavigationGraph.Point point, FootholdSegment foothold, BotGroundPhysics.Travel travel, int stepX, int velocityX, bool lostGround)
	{
		public readonly BotNavigationGraph.Point point = point;

		public readonly FootholdSegment foothold = foothold;

		public readonly BotGroundPhysics.Travel travel = travel;

		public readonly int stepX = stepX;

		public readonly int velocityX = velocityX;

		public readonly bool lostGround = lostGround;
	}

	private readonly MapRuntime map;

	private readonly BotGroundIndex ground;

	private readonly BotWalkRegions regions;

	private readonly BotAirCollision collision;

	private readonly BotGroundPhysics physics;

	public BotGroundMotion(MapRuntime map, BotGroundIndex ground, BotWalkRegions regions, BotAirCollision collision, BotGroundPhysics physics)
	{
		this.map = map;
		this.ground = ground;
		this.regions = regions;
		this.collision = collision;
		this.physics = physics;
	}

	public Result Step(BotNavigationGraph.Point position, FootholdSegment foothold, int direction, BotGroundPhysics.Travel travel, BotMovementProfile profile)
	{
		if (foothold == null)
		{
			return new Result(position, null, travel, 0, 0, lostGround: true);
		}
		BotGroundPhysics.Travel displaced = physics.Advance(travel, direction, foothold.Wall ? 0.0 : foothold.Slope, map.footholdSpeed, profile);
		return Preview(position, foothold, travel, displaced);
	}

	public bool CanStep(BotNavigationGraph.Point position, int stepX)
	{
		FootholdSegment footholdSegment = ground.Ground(position.x, position.y);
		if (footholdSegment == null)
		{
			return false;
		}
		Result result = Preview(position, footholdSegment, new BotGroundPhysics.Travel(position.x), new BotGroundPhysics.Travel(position.x + stepX));
		if (!result.lostGround)
		{
			return result.point.x == position.x + stepX;
		}
		return false;
	}

	private Result Preview(BotNavigationGraph.Point position, FootholdSegment foothold, BotGroundPhysics.Travel travel, BotGroundPhysics.Travel displaced)
	{
		int num = BotGroundPhysics.JavaRound(displaced.X);
		int stepX = num - position.x;
		bool flag = regions != null && regions.byFoothold.ContainsKey(foothold.id);
		BotNavigationGraph.Point? point = ((!flag) ? ((BotNavigationGraph.Point?)null) : regions.GroundSample(foothold, position.x, position.y)?.point);
		if (!point.HasValue)
		{
			point = ground.GroundPoint(position.x, position.y);
		}
		int num2 = ((point.HasValue && Math.Abs(point.Value.y - position.y) <= 26) ? point.Value.y : position.y);
		if (collision.Wall(position, new BotNavigationGraph.Point(num, num2), allowWalkableEndpoint: true).kind == BotAirCollision.Kind.Wall)
		{
			return new Result(position, foothold, new BotGroundPhysics.Travel(position.x), 0, 0, lostGround: false);
		}
		BotNavigationGraph.Point? point2;
		FootholdSegment footholdSegment;
		if (flag)
		{
			BotWalkRegions.Sample? sample = regions.GroundSample(foothold, num, num2);
			point2 = sample?.point;
			footholdSegment = sample?.foothold;
		}
		else
		{
			point2 = ground.GroundPoint(num, Math.Max(position.y, num2 + 1));
			footholdSegment = (point2.HasValue ? ground.Below(num, point2.Value.y + 1) : null);
		}
		int velocityX = BotGroundPhysics.JavaRound((displaced.X - (double)position.x) * 1000.0 / (double)physics.TickMs);
		bool flag2 = !point2.HasValue || point2.Value.y > num2 + 16;
		return new Result(flag2 ? new BotNavigationGraph.Point(num, num2) : point2.Value, flag2 ? foothold : (footholdSegment ?? foothold), displaced, stepX, velocityX, flag2);
	}

	public int SlipperyStopDirection(BotNavigationGraph.Point position, FootholdSegment foothold, BotGroundPhysics.Travel travel, BotMovementProfile profile)
	{
		int num = physics.CounterStrafe(map.footholdSpeed, profile, travel.Speed);
		if (num == 0)
		{
			return 0;
		}
		for (int i = 0; i < 240; i++)
		{
			if (!(Math.Abs(travel.Speed) > 0.0))
			{
				break;
			}
			Result result = Step(position, foothold, 0, travel, profile);
			if (result.lostGround)
			{
				return num;
			}
			position = result.point;
			foothold = result.foothold;
			travel = result.travel;
		}
		return 0;
	}
}
