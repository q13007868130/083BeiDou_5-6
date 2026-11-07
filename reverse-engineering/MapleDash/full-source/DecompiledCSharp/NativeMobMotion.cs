using System;

public static class NativeMobMotion
{
	public static double Multiplier(int raw, int correction = 0)
	{
		return (double)Math.Max(10L, Math.Min(140L, Math.Max(0L, Math.Min(140L, 100L + (long)raw)) + correction)) * 0.01;
	}

	public static bool AcquisitionRange(int dx, int dy)
	{
		return Math.Abs((long)dx) / 10 + Math.Abs((long)dy) / 3 <= 40;
	}
}
