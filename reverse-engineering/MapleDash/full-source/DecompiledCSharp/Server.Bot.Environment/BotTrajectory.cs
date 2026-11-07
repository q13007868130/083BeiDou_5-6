using System;
using Server.Field;

namespace Server.Bot.Environment;

public sealed class BotTrajectory
{
	public sealed class Landing
	{
		public readonly BotNavigationGraph.Point point;

		public readonly FootholdSegment foothold;

		public readonly int deltaX;

		public readonly int deltaY;

		public readonly int ticks;

		public int TimeMs => ticks * 50;

		public Landing(BotNavigationGraph.Point point, FootholdSegment foothold, int dx, int dy, int ticks)
		{
			this.point = point;
			this.foothold = foothold;
			deltaX = dx;
			deltaY = dy;
			this.ticks = ticks;
		}
	}

	public sealed class RopeGrab
	{
		public readonly BotNavigationGraph.Point point;

		public readonly int ticks;

		public int TimeMs => ticks * 50;

		public RopeGrab(BotNavigationGraph.Point point, int ticks)
		{
			this.point = point;
			this.ticks = ticks;
		}
	}

	public sealed class StableLanding
	{
		public readonly Landing landing;

		public readonly BotNavigationGraph.Point point;

		public readonly FootholdSegment foothold;

		public readonly bool lostGround;

		public StableLanding(Landing landing, BotNavigationGraph.Point point, FootholdSegment foothold, bool lost)
		{
			this.landing = landing;
			this.point = point;
			this.foothold = foothold;
			lostGround = lost;
		}
	}

	private readonly MapRuntime map;

	private readonly BotAirCollision collision;

	private readonly BotMovementProfile profile;

	public BotTrajectory(MapRuntime map, BotAirCollision collision, BotMovementProfile profile)
	{
		this.map = map;
		this.collision = collision;
		this.profile = profile ?? BotMovementProfile.Base;
	}

	public Landing Jump(BotNavigationGraph.Point start, int stepX)
	{
		RopeGrab grab;
		return Simulate(start, (0f - profile.JumpSpeed(555f)) * 0.05f, stepX, 0, null, out grab);
	}

	public Landing Fall(BotNavigationGraph.Point start, int stepX)
	{
		RopeGrab grab;
		return Simulate(start, 0f, stepX, 0, null, out grab);
	}

	public Landing DownJump(BotNavigationGraph.Point start)
	{
		RopeGrab grab;
		return Simulate(start, -9.8f, 0, 350, null, out grab);
	}

	public Landing RopeJump(BotNavigationGraph.Point start, int stepX)
	{
		RopeGrab grab;
		return Simulate(start, (0f - profile.JumpSpeed(375f)) * 0.05f, stepX, 0, null, out grab);
	}

	public StableLanding PostLanding(Landing landing, int direction, BotGroundMotion motion, BotGroundPhysics physics, int ticks = 3)
	{
		if (landing == null)
		{
			return null;
		}
		BotNavigationGraph.Point point = landing.point;
		FootholdSegment foothold = landing.foothold;
		double speed = BotAirMotion.LandingSpeed(foothold, landing.deltaX, landing.deltaY, physics, profile);
		BotGroundPhysics.Travel travel = new BotGroundPhysics.Travel(point.x, speed);
		bool flag = physics.SlipScale(map.footholdSpeed, profile) < 1.0;
		for (int i = 0; i < (flag ? 240 : ticks); i++)
		{
			int num = (flag ? motion.SlipperyStopDirection(point, foothold, travel, profile) : direction);
			if (flag && num == 0)
			{
				return new StableLanding(landing, point, foothold, lost: false);
			}
			BotGroundMotion.Result result = motion.Step(point, foothold, num, travel, profile);
			if (result.lostGround)
			{
				return new StableLanding(landing, result.point, result.foothold, lost: true);
			}
			point = result.point;
			foothold = result.foothold;
			travel = result.travel;
		}
		return new StableLanding(landing, point, foothold, flag);
	}

	public RopeGrab Grab(BotNavigationGraph.Point start, int stepX, MapRope rope, bool ropeJump = false, bool downJump = false)
	{
		Simulate(start, downJump ? (-9.8f) : ((0f - profile.JumpSpeed(ropeJump ? 375 : 555)) * 0.05f), stepX, downJump ? 350 : 0, rope, out var grab);
		return grab;
	}

	private Landing Simulate(BotNavigationGraph.Point start, float velocityY, int stepX, int graceMs, MapRope rope, out RopeGrab grab)
	{
		grab = null;
		double num = start.x;
		double num2 = start.y;
		int y = start.y;
		float num3 = 5f;
		float val = 33.5f;
		int num4 = ((map.bottom > map.top) ? (map.bottom + 600) : int.MaxValue);
		for (int i = 0; i < 2000; i++)
		{
			if (rope != null)
			{
				BotNavigationGraph.Point position = new BotNavigationGraph.Point(BotGroundPhysics.JavaRound(num), BotGroundPhysics.JavaRound(num2));
				if (BotRopeMotion.CanGrab(position, rope))
				{
					grab = new RopeGrab(new BotNavigationGraph.Point(rope.x, position.y), i);
					return null;
				}
			}
			if (graceMs > 0)
			{
				graceMs = Math.Max(0, graceMs - 50);
			}
			num += (double)stepX;
			num2 += (double)(velocityY + 0.5f * num3);
			velocityY = Math.Min(velocityY + num3, val);
			BotNavigationGraph.Point point = new BotNavigationGraph.Point(BotGroundPhysics.JavaRound(num - (double)stepX), y);
			BotNavigationGraph.Point to = new BotNavigationGraph.Point(BotGroundPhysics.JavaRound(num), BotGroundPhysics.JavaRound(num2));
			BotAirCollision.Hit hit = collision.Resolve(point, to);
			if (hit.kind == BotAirCollision.Kind.Wall)
			{
				num = hit.point.x;
				num2 = hit.point.y;
				stepX = 0;
				y = hit.point.y;
				continue;
			}
			if (hit.kind == BotAirCollision.Kind.Ceiling)
			{
				num = hit.point.x;
				num2 = hit.point.y;
				velocityY = 0f;
				y = hit.point.y;
				continue;
			}
			if (hit.kind == BotAirCollision.Kind.Land && (graceMs == 0 || hit.foothold.forbidFallDown))
			{
				if (rope == null)
				{
					return new Landing(hit.point, hit.foothold, to.x - point.x, to.y - point.y, i + 1);
				}
				return null;
			}
			if (to.y > num4)
			{
				return null;
			}
			y = to.y;
		}
		return null;
	}
}
