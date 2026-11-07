using System;

namespace Server.Bot.Environment;

public sealed class BotSwimMotion
{
	public sealed class State
	{
		public double x;

		public double y;

		public double horizontalSpeed;

		public double groundSpeed;

		public float verticalSpeed;

		public int direction;

		public int verticalHold;

		public int facing = 1;

		public bool swimming;

		public bool jumpRequested;

		public BotNavigationGraph.Point position;
	}

	private readonly BotAirCollision collision;

	private readonly BotGroundPhysics physics;

	private readonly BotMovementProfile profile;

	public BotSwimMotion(BotAirCollision collision, BotGroundPhysics physics, BotMovementProfile profile)
	{
		this.collision = collision;
		this.physics = physics;
		this.profile = profile ?? BotMovementProfile.Base;
	}

	public bool Step(State state)
	{
		if (!state.swimming || Math.Abs(state.x - (double)state.position.x) > 2.0 || Math.Abs(state.y - (double)state.position.y) > 2.0)
		{
			state.x = state.position.x;
			state.y = state.position.y;
		}
		double num = state.horizontalSpeed;
		double num2 = state.verticalSpeed;
		if (state.jumpRequested)
		{
			num2 = 0f - 1000f * (float)profile.SpeedMultiplier;
			state.jumpRequested = false;
		}
		if (state.direction != 0)
		{
			num += 30.000000447034836 * (double)Math.Sign(state.direction);
		}
		double num3 = Math.Max(0.0, 0.7894999949559569);
		num *= num3;
		num2 = num2 * num3 + 29.500000439584255;
		if (state.verticalHold < 0)
		{
			num2 -= 20.60000030696392;
		}
		else if (state.verticalHold > 0)
		{
			num2 += 14.750000219792128;
		}
		num = Math.Max(-800.0, Math.Min(800.0, num));
		if (state.direction > 0 && num > 140.0)
		{
			num = 140.0;
		}
		if (state.direction < 0 && num < -140.0)
		{
			num = -140.0;
		}
		double val = ((state.verticalHold < 0) ? 42 : ((state.verticalHold > 0) ? 210 : 140));
		num2 = Math.Max(-800.0, Math.Min(val, num2));
		double num4 = state.x + num * 0.05000000074505806;
		double num5 = state.y + num2 * 0.05000000074505806;
		BotNavigationGraph.Point point = new BotNavigationGraph.Point(BotGroundPhysics.JavaRound(state.x), BotGroundPhysics.JavaRound(state.y));
		BotNavigationGraph.Point to = new BotNavigationGraph.Point(BotGroundPhysics.JavaRound(num4), BotGroundPhysics.JavaRound(num5));
		BotAirCollision.Hit hit = collision.Resolve(point, to);
		if (state.direction != 0)
		{
			state.facing = Math.Sign(state.direction);
		}
		if (hit.kind == BotAirCollision.Kind.Land)
		{
			state.x = hit.point.x;
			state.y = hit.point.y;
			state.position = hit.point;
			state.swimming = false;
			state.verticalSpeed = 0f;
			state.groundSpeed = BotAirMotion.LandingSpeed(hit.foothold, hit.point.x - point.x, hit.point.y - point.y, physics, profile);
			state.horizontalSpeed = state.groundSpeed;
			return true;
		}
		if (hit.kind == BotAirCollision.Kind.Wall)
		{
			num4 = hit.point.x;
			num = 0.0;
		}
		state.horizontalSpeed = num;
		state.verticalSpeed = (float)num2;
		state.x = num4;
		state.y = num5;
		state.swimming = true;
		state.position = new BotNavigationGraph.Point(BotGroundPhysics.JavaRound(num4), BotGroundPhysics.JavaRound(num5));
		return false;
	}
}
