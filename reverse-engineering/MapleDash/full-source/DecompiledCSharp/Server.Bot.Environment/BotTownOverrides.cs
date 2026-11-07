using System;
using System.Collections.Generic;

namespace Server.Bot.Environment;

public sealed class BotTownOverrides
{
	public readonly struct Zone(int x1, int y1, int x2, int y2, double multiplier)
	{
		public readonly int x1 = x1;

		public readonly int y1 = y1;

		public readonly int x2 = x2;

		public readonly int y2 = y2;

		public readonly double multiplier = multiplier;

		public bool Contains(int x, int y)
		{
			if (x >= Math.Min(x1, x2) && x <= Math.Max(x1, x2) && y >= Math.Min(y1, y2))
			{
				return y <= Math.Max(y1, y2);
			}
			return false;
		}
	}

	public readonly List<Zone> ban = new List<Zone>();

	public readonly List<Zone> boost = new List<Zone>();

	public readonly List<BotNavigationGraph.Point> pins = new List<BotNavigationGraph.Point>();

	public bool IsBanned(int x, int y)
	{
		foreach (Zone item in ban)
		{
			if (item.Contains(x, y))
			{
				return true;
			}
		}
		return false;
	}

	public double Boost(int x, int y)
	{
		double num = 1.0;
		foreach (Zone item in boost)
		{
			if (item.Contains(x, y))
			{
				num *= item.multiplier;
			}
		}
		return num;
	}
}
