using System;

namespace Server.Bot.Environment;

public sealed class BotGroundPhysics
{
	public readonly struct Travel(double x, double speed = 0.0, double carryMs = 0.0)
	{
		public readonly double X = x;

		public readonly double Speed = speed;

		public readonly double CarryMs = carryMs;
	}

	public int TickMs = 50;

	public double GroundSlip = 3.0;

	public double Friction = 0.3;

	public double SlopeFactor = 0.1;

	public double BaseHorizontalForce = 16.667;

	public double SlipWalkAcceleration = 1400.0;

	public double SlipGlideDeceleration = 400.0;

	public double SlipScale(float mapFootholdSpeed, BotMovementProfile profile)
	{
		return ((profile ?? BotMovementProfile.Base).snowShoes || mapFootholdSpeed <= 0f || mapFootholdSpeed >= 1f) ? 1f : mapFootholdSpeed;
	}

	private double Force(BotMovementProfile profile)
	{
		return (profile ?? BotMovementProfile.Base).HorizontalForce(BaseHorizontalForce) * 0.008;
	}

	public double MaxSpeed(BotMovementProfile profile)
	{
		return Force(profile) * GroundSlip / (Friction + SlopeFactor);
	}

	public int WalkStep(BotMovementProfile profile)
	{
		return Math.Max(1, JavaRound(MaxSpeed(profile) * (double)TickMs / 8.0));
	}

	public int LaunchRunway(float mapFootholdSpeed, BotMovementProfile profile)
	{
		int num = WalkStep(profile);
		double num2 = SlipScale(mapFootholdSpeed, profile);
		if (num2 >= 1.0)
		{
			return Math.Max(40, num * 6);
		}
		double num3 = MaxSpeed(profile) / 0.008;
		double num4 = num3 * num3 / (2.0 * SlipWalkAcceleration * num2);
		return Math.Max(40, JavaRound((double)(num * 6) + num4));
	}

	public Travel Advance(Travel state, int direction, double slope, float mapFootholdSpeed, BotMovementProfile profile)
	{
		double num = state.CarryMs + (double)TickMs;
		int num2 = (int)(num / 8.0);
		if (num2 == 0)
		{
			return state;
		}
		double num3 = state.X;
		double num4 = state.Speed;
		double slip = SlipScale(mapFootholdSpeed, profile);
		for (int i = 0; i < num2; i++)
		{
			num4 = Step(num4, direction, slope, slip, profile);
			num3 += num4;
		}
		return new Travel(num3, num4, num - (double)(num2 * 8));
	}

	public double Step(double speed, int direction, double slope, double slip, BotMovementProfile profile)
	{
		double num = (double)direction * Force(profile);
		if (num == 0.0 && Math.Abs(speed) < 0.1)
		{
			return 0.0;
		}
		if (slip < 1.0)
		{
			if (direction != 0)
			{
				double num2 = SlipWalkAcceleration * slip * 0.008 * 0.008;
				double num3 = MaxSpeed(profile);
				return Clamp(speed + (double)direction * num2, 0.0 - num3, num3);
			}
			double val = SlipGlideDeceleration * slip * 0.008 * 0.008;
			return speed - (double)Math.Sign(speed) * Math.Min(Math.Abs(speed), val);
		}
		double num4 = speed / GroundSlip;
		double num5 = (Friction + SlopeFactor * (1.0 + Clamp(slope, -0.5, 0.5) * (0.0 - num4))) * num4;
		return speed + (num - num5) * slip;
	}

	public int CounterStrafe(float mapFootholdSpeed, BotMovementProfile profile, double speed)
	{
		double num = SlipScale(mapFootholdSpeed, profile);
		if (num >= 1.0)
		{
			return 0;
		}
		double num2 = SlipWalkAcceleration * num * 0.008 * 0.008 * Math.Max(1.0, (double)TickMs / 8.0);
		if (!(Math.Abs(speed) > num2))
		{
			return 0;
		}
		if (!(speed > 0.0))
		{
			return 1;
		}
		return -1;
	}

	public int Approach(float mapFootholdSpeed, BotMovementProfile profile, double speed, int targetDelta, int overshootSlack = 0)
	{
		int num = Math.Sign(targetDelta);
		double num2 = SlipScale(mapFootholdSpeed, profile);
		if (num == 0 || num2 >= 1.0)
		{
			return num;
		}
		double num3 = SlipWalkAcceleration * num2 * 0.008 * 0.008;
		double num4 = SlipGlideDeceleration * num2 * 0.008 * 0.008;
		int num5 = Math.Max(1, (int)Math.Ceiling((double)TickMs / 8.0));
		double num6 = num3 * (double)num5;
		double num7 = speed * (double)num;
		double num8 = 0.0;
		double val = MaxSpeed(profile);
		for (int i = 0; i < num5; i++)
		{
			num7 = Math.Min(num7 + num3, val);
			num8 += num7;
		}
		while (num7 > num6)
		{
			num7 -= num3;
			num8 += num7;
		}
		while (num7 > 0.0)
		{
			num7 -= num4;
			num8 += Math.Max(num7, 0.0);
		}
		if (!(num8 < (double)(Math.Abs(targetDelta) + Math.Max(0, overshootSlack))))
		{
			return CounterStrafe(mapFootholdSpeed, profile, speed);
		}
		return num;
	}

	public static bool WalkableEndpoint(int dx, int dy)
	{
		if (dx <= 12 && dy <= 16)
		{
			return dy >= -26;
		}
		return false;
	}

	public static int JavaRound(double value)
	{
		return (int)Math.Floor(value + 0.5);
	}

	private static double Clamp(double v, double min, double max)
	{
		return Math.Max(min, Math.Min(max, v));
	}
}
