using System;

public static class PercentageDefense
{
	public static double Multiplier(double defensePercent, double ignorePercent)
	{
		if (double.IsNaN(defensePercent) || double.IsNaN(ignorePercent))
		{
			throw new ArgumentException("Invalid defense percentage");
		}
		double num = Math.Max(0.0, defensePercent) * (1.0 - Math.Max(0.0, Math.Min(100.0, ignorePercent)) / 100.0);
		return Math.Max(0.0, 1.0 - num / 100.0);
	}
}
