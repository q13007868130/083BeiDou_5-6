using System;
using MapleDash.Wz;

public sealed class NativeMovementPhysics
{
	public double WalkForce;

	public double WalkSpeed;

	public double WalkDrag;

	public double SlipForce;

	public double SlipSpeed;

	public double FloatDrag1;

	public double FloatDrag2;

	public double FloatCoefficient;

	public double Gravity;

	public double FallSpeed;

	public double JumpSpeed;

	public double MaxFriction;

	public double MinFriction;

	public double FlyForce;

	public double FlySpeed;

	public const double Mass = 100.0;

	public const double SlipThreshold = 0.9;

	public static NativeMovementPhysics Load()
	{
		ImgNode imgNode = WzAssets.FileRoot("Map/Physics.img");
		return new NativeMovementPhysics
		{
			WalkForce = Convert.ToDouble(imgNode.Get("walkForce").Value),
			WalkSpeed = Convert.ToDouble(imgNode.Get("walkSpeed").Value),
			WalkDrag = Convert.ToDouble(imgNode.Get("walkDrag").Value),
			SlipForce = Convert.ToDouble(imgNode.Get("slipForce").Value),
			SlipSpeed = Convert.ToDouble(imgNode.Get("slipSpeed").Value),
			FloatDrag1 = Convert.ToDouble(imgNode.Get("floatDrag1").Value),
			FloatDrag2 = Convert.ToDouble(imgNode.Get("floatDrag2").Value),
			FloatCoefficient = Convert.ToDouble(imgNode.Get("floatCoefficient").Value),
			Gravity = Convert.ToDouble(imgNode.Get("gravityAcc").Value),
			FallSpeed = Convert.ToDouble(imgNode.Get("fallSpeed").Value),
			JumpSpeed = Convert.ToDouble(imgNode.Get("jumpSpeed").Value),
			MaxFriction = Convert.ToDouble(imgNode.Get("maxFriction").Value),
			MinFriction = Convert.ToDouble(imgNode.Get("minFriction").Value),
			FlyForce = Convert.ToDouble(imgNode.Get("flyForce").Value),
			FlySpeed = Convert.ToDouble(imgNode.Get("flySpeed").Value)
		};
	}

	private static double Approach(double v, double target, double step)
	{
		if (!(v < target))
		{
			return Math.Max(target, v - step);
		}
		return Math.Min(target, v + step);
	}

	private static double Accelerate(double v, double force, double limit, double dt)
	{
		if (force > 0.0 && v < limit)
		{
			return Math.Min(limit, v + force / 100.0 * dt);
		}
		if (force <= 0.0 && v > 0.0 - Math.Abs(limit))
		{
			return Math.Max(0.0 - Math.Abs(limit), v + force / 100.0 * dt);
		}
		return v;
	}

	private static double Restrict(double v, double limit, double step)
	{
		if (!(limit >= 0.0) || !(Math.Abs(v) > limit))
		{
			return v;
		}
		return Approach(v, (double)Math.Sign(v) * limit, step);
	}

	public double Ground(double velocity, int input, double force, double drag, double walk, double slope, double dt)
	{
		double num = WalkForce * walk;
		double num2 = (double)input * num;
		double num3 = WalkSpeed * walk;
		if (force != 0.0)
		{
			double num4 = Math.Abs((int)force);
			double num5;
			if (input != 0)
			{
				num5 = (((double)input * force > 0.0) ? (2.0 * num4) : (0.2 / num4));
			}
			else
			{
				num5 = force;
			}
			num2 = (double)((input == 0) ? 1 : input) * num * num5;
			num3 *= ((input == 0) ? ((double)(int)Math.Abs(force)) : num5);
		}
		double num6 = slope * slope;
		num2 *= ((slope < 0.0) ? (1.0 - num6) : (1.0 + num6));
		double num7 = num3;
		double num8 = Math.Max(MinFriction, Math.Min(MaxFriction, drag));
		if (num8 < 1.0)
		{
			num8 *= 0.5;
		}
		double step = num8 * WalkDrag / 100.0 * dt;
		if (slope == 0.0)
		{
			velocity = Restrict(velocity, num7, step);
			if (input != 0 || force != 0.0)
			{
				return Accelerate(velocity, num2, num7, dt);
			}
			return Approach(velocity, 0.0, step);
		}
		int num9 = ((slope < 0.0) ? 1 : (-1));
		double num10 = Math.Max(0.0, num7);
		double num11 = Math.Max(0.0, (1.0 + num6) * num7);
		velocity = Restrict(velocity, ((double)num9 * velocity > 0.0) ? num10 : num11, step);
		if (Math.Abs(slope) <= 0.9)
		{
			if (input == 0 && force == 0.0)
			{
				return Approach(velocity, 0.0, step);
			}
			return Accelerate(velocity, num2, ((double)num9 * num2 > 0.0) ? num10 : num11, dt);
		}
		double num12 = slope * SlipForce;
		double num13 = Math.Abs(slope) * SlipSpeed;
		if (input * num9 > 0)
		{
			num12 *= 0.5;
			num13 *= 0.5;
		}
		else if (input != 0 || force != 0.0)
		{
			num12 += num2;
			num13 += num11;
		}
		if ((double)num9 * velocity > 0.0)
		{
			velocity = Approach(velocity, 0.0, step);
		}
		return Accelerate(velocity, num12, num13, dt);
	}

	public double Air(double velocity, double downwardVelocity, int input, double dt)
	{
		double num = FloatDrag2 / 100.0 * dt;
		double limit = WalkSpeed / WalkForce * FloatDrag2;
		if (input == 0)
		{
			return Approach(velocity, 0.0, (downwardVelocity < FallSpeed) ? (num * FloatCoefficient) : num);
		}
		return Accelerate(velocity, (double)input * FloatDrag2 * 2.0, limit, dt);
	}

	public void Flight(ref double vx, ref double vy, int horizontal, int vertical, double fly, double dt, double gravity = 1.0)
	{
		gravity = Math.Abs(gravity);
		double num = FlySpeed * fly * gravity;
		double step = FloatDrag1 / 100.0 * dt;
		vx = Restrict(vx, num, step);
		vx = ((horizontal == 0) ? Approach(vx, 0.0, step) : Accelerate(vx, (double)horizontal * FlyForce * gravity, num, dt));
		vy = Restrict(vy, num, step);
		double num2 = FlyForce / 100.0 / gravity;
		if (vertical < 0)
		{
			double num3 = num * 0.3;
			vy = ((vy > num3) ? Math.Max(num3, vy - num2 * dt) : Math.Min(num3, vy + num2 * dt * 0.5));
		}
		else if (vertical > 0)
		{
			vy = num * 1.5;
		}
		else
		{
			vy = Accelerate(vy, num2 * 100.0, num, dt);
		}
	}

	public double Flap(double fly, double gravity = 1.0)
	{
		return (0.0 - FlySpeed) * fly * Math.Abs(gravity);
	}
}
