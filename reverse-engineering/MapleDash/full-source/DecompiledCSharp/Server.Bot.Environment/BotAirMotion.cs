using System;
using Server.Field;

namespace Server.Bot.Environment;

public sealed class BotAirMotion
{
	public sealed class State
	{
		public double x;

		public double y;

		public double steerVelocity;

		public double peakY;

		public double groundSpeed;

		public double fallDistance;

		public float velocityY;

		public int velocityX;

		public int direction;

		public int facing = 1;

		public int landingGraceMs;

		public bool fixedArc;

		public bool pendingFlash;

		public bool flashFired;

		public bool airborne = true;

		public float flashScale = 1f;

		public BotNavigationGraph.Point Position => new BotNavigationGraph.Point(BotGroundPhysics.JavaRound(x), BotGroundPhysics.JavaRound(y));
	}

	private readonly MapRuntime map;

	private readonly BotAirCollision collision;

	private readonly BotGroundPhysics groundPhysics;

	private readonly BotMovementProfile profile;

	private const float Tick = 0.05f;

	public BotAirMotion(MapRuntime map, BotAirCollision collision, BotGroundPhysics groundPhysics, BotMovementProfile profile)
	{
		this.map = map;
		this.collision = collision;
		this.groundPhysics = groundPhysics;
		this.profile = profile ?? BotMovementProfile.Base;
	}

	public State Launch(BotNavigationGraph.Point from, int horizontalStep, bool ropeJump = false, bool downJump = false)
	{
		return new State
		{
			x = from.x,
			y = from.y,
			peakY = from.y,
			velocityX = horizontalStep,
			velocityY = (downJump ? (-9.8f) : ((0f - profile.JumpSpeed(ropeJump ? 375 : 555)) * 0.05f)),
			landingGraceMs = (downJump ? 350 : 0)
		};
	}

	public static void AdvanceTimers(State state)
	{
		state.landingGraceMs = Math.Max(0, state.landingGraceMs - 50);
	}

	public BotAirCollision.Kind Step(State state)
	{
		if (!state.airborne)
		{
			return BotAirCollision.Kind.None;
		}
		double num = groundPhysics.SlipScale(map.footholdSpeed, profile);
		double num2 = (double)state.velocityX + state.steerVelocity;
		if (state.direction != 0)
		{
			double num3 = profile.WalkVelocity(125.0) * 0.05000000074505806 * num / 14.0;
			double num4 = num2 * (double)state.direction;
			if (num4 < num3)
			{
				state.steerVelocity = Math.Min(num3, num4 + 200.0 * num * 0.05000000074505806 * 0.05000000074505806) * (double)state.direction - (double)state.velocityX;
			}
			state.facing = ((state.direction > 0) ? 1 : (-1));
		}
		else if (!state.fixedArc && num2 != 0.0)
		{
			double num5 = (double)((!(state.velocityY >= 33.5f)) ? 1 : 100) * num * 0.05000000074505806 * 0.05000000074505806;
			double num6 = ((num2 > 0.0) ? Math.Max(0.0, num2 - num5) : Math.Min(0.0, num2 + num5));
			state.steerVelocity = num6 - (double)state.velocityX;
		}
		if (state.pendingFlash && state.velocityY >= (0f - (1f - state.flashScale)) * profile.JumpSpeed(555f) * 0.05f)
		{
			int num7 = ((state.velocityX != 0) ? Math.Sign(state.velocityX) : ((state.facing >= 0) ? 1 : (-1)));
			state.velocityX = BotGroundPhysics.JavaRound((float)(num7 * 550) * 0.05f * state.flashScale);
			state.steerVelocity = 0.0;
			state.velocityY = -17.5f * state.flashScale;
			state.fixedArc = true;
			state.pendingFlash = false;
			state.flashFired = true;
		}
		BotNavigationGraph.Point position = state.Position;
		state.x += (double)state.velocityX + state.steerVelocity;
		float num8 = 5f;
		state.y += state.velocityY + 0.5f * num8;
		state.velocityY = Math.Min(state.velocityY + num8, 33.5f);
		if (state.y < state.peakY)
		{
			state.peakY = state.y;
		}
		BotNavigationGraph.Point position2 = state.Position;
		BotAirCollision.Hit hit = collision.Resolve(position, position2);
		if (hit.kind == BotAirCollision.Kind.Wall)
		{
			state.velocityX = 0;
			state.steerVelocity = 0.0;
			state.fixedArc = false;
			state.x = hit.point.x;
			state.y = hit.point.y;
		}
		else
		{
			if (hit.kind != BotAirCollision.Kind.Ceiling)
			{
				if (hit.kind == BotAirCollision.Kind.Land && (state.landingGraceMs == 0 || hit.foothold.forbidFallDown))
				{
					state.groundSpeed = LandingSpeed(hit.foothold, position2.x - position.x, position2.y - position.y, groundPhysics, profile);
					state.fallDistance = (double.IsInfinity(state.peakY) ? 0.0 : Math.Max(0.0, (double)hit.point.y - state.peakY));
					state.x = hit.point.x;
					state.y = hit.point.y;
					state.airborne = false;
					state.velocityY = 0f;
					state.velocityX = 0;
					state.steerVelocity = 0.0;
					state.fixedArc = false;
					state.pendingFlash = false;
					state.flashFired = false;
					state.landingGraceMs = 0;
					state.peakY = double.PositiveInfinity;
					return BotAirCollision.Kind.Land;
				}
				return BotAirCollision.Kind.None;
			}
			state.velocityY = 0f;
			state.fixedArc = false;
			state.x = hit.point.x;
			state.y = hit.point.y;
		}
		return hit.kind;
	}

	public static double LandingSpeed(FootholdSegment foothold, double dx, double dy, BotGroundPhysics physics, BotMovementProfile profile)
	{
		if (foothold != null && !foothold.Wall && foothold.Slope != 0.0)
		{
			double num = foothold.x2 - foothold.x1;
			double num2 = foothold.y2 - foothold.y1;
			double num3 = Math.Sqrt(num * num + num2 * num2);
			if (num3 > 0.0)
			{
				num /= num3;
				num2 /= num3;
				dx = num * (dx * num + dy * num2);
			}
		}
		double num4 = Math.Max(1, physics.WalkStep(profile));
		double num5 = Math.Max(0.0 - num4, Math.Min(num4, dx * 0.5));
		double num6 = physics.MaxSpeed(profile);
		return Math.Max(0.0 - num6, Math.Min(num6, num5 / Math.Max(1.0, (double)physics.TickMs / 8.0)));
	}
}
