using System;

public static class FallDamage
{
	public const float ThresholdPx = 890f;

	private const double Saturation = 28.0;

	private const double KneeSharpness = 0.013000000268220901;

	private const double PerPixelTail = 0.002400000113993883;

	public static int FromDistance(double distancePx)
	{
		if (distancePx <= 890.0)
		{
			return 0;
		}
		double num = distancePx - 890.0;
		double num2 = 28.0 * (1.0 - Math.Exp(-0.013000000268220901 * num)) + 0.002400000113993883 * num;
		return (int)Math.Max(1L, (long)Math.Floor(num2 + 0.5));
	}
}
