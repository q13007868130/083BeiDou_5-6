using System;

public static class NativeDropMotion
{
	public static int MesoTier(int amount)
	{
		if (amount >= 50)
		{
			if (amount >= 100)
			{
				if (amount >= 1000)
				{
					return 3;
				}
				return 2;
			}
			return 1;
		}
		return 0;
	}

	public static int MesoFrame(int amount, int elapsedMs)
	{
		int num = MesoTier(amount);
		if (num < 2)
		{
			return elapsedMs / 80 % 4;
		}
		if (num == 2)
		{
			return elapsedMs / 200 % 4;
		}
		int num2 = elapsedMs % 4360;
		if (num2 >= 4000)
		{
			return 1 + (num2 - 4000) / 120;
		}
		return 0;
	}

	public static int FlightDuration(int sourceY, int targetY)
	{
		if (sourceY <= targetY)
		{
			return 1000;
		}
		double num = Math.Sqrt(((double)targetY + 100.0 - (double)sourceY) * 2.5);
		if (!double.IsNaN(num))
		{
			return Math.Min(1000, ((int)num + 1) * 30 + 500);
		}
		return 530;
	}

	private static int Round(double value)
	{
		return (int)((value >= 0.0) ? (value + 0.5) : (value - 0.499999999));
	}

	public static void Flight(int sourceX, int sourceY, int targetX, int targetY, int elapsedMs, out int x, out int y, out bool landed)
	{
		int num = FlightDuration(sourceY, targetY);
		int num2 = Math.Max(0, elapsedMs / 30);
		int num3 = Math.Min(num2 * 30, (num + 29) / 30 * 30);
		double num4 = (double)num3 * 0.001;
		double num5 = Math.Min((double)num3 * 0.002, 1.0);
		double num6 = ((num3 <= 500) ? 0.0 : Math.Min(((double)num3 - 500.0) / (double)(num - 500), 1.0));
		x = Round((double)sourceX + (double)(targetX - sourceX) * 0.5 * (num5 + num6));
		y = Round((double)sourceY - 400.0 * num4 + 400.0 * num4 * num4);
		landed = num3 >= num && sourceY >= targetY;
		if (num3 >= num && sourceY < targetY)
		{
			int num7 = Math.Max(0, num2 * 30 - num3);
			y = Round((double)sourceY + (double)num7 * 0.4);
			landed = y >= targetY;
			x = targetX;
		}
		if (landed)
		{
			x = targetX;
			y = targetY;
		}
	}

	public static double Bob(int ticks)
	{
		return Math.Sin((double)ticks * 0.0942477) * 3.0;
	}

	public static int LandingTick(int sourceY, int targetY)
	{
		return (FlightDuration(sourceY, targetY) + 29) / 30 + ((sourceY < targetY) ? ((targetY - sourceY + 11) / 12) : 0);
	}

	public static bool InPickupRange(int feetX, int feetY, int dropX, int dropY)
	{
		if (dropX >= feetX - 25 && dropX < feetX + 25 && dropY >= feetY - 50)
		{
			return dropY < feetY + 10;
		}
		return false;
	}

	public static bool Pickup(int sourceX, int sourceY, int targetX, int targetY, int elapsedMs, out int x, out int y, out int alpha)
	{
		int num = Math.Max(0, Math.Min(elapsedMs, 700));
		x = ((700 - num) * sourceX + num * targetX) / 700;
		y = ((700 - num) * sourceY + num * targetY) / 700 + 160 * (num - 350) * (num - 350) / 490000 - 40;
		alpha = ((num <= 420) ? 255 : (255 + (420 - num) * 192 / 280));
		return elapsedMs < 700;
	}
}
