using System;
using System.Collections.Generic;

namespace Server.Bot.Environment;

public sealed class Platform
{
	public enum Type
	{
		FLAT,
		SLOPED
	}

	public readonly int minX;

	public readonly int maxX;

	public readonly int baseY;

	public readonly List<BotPoint> sortedPoints;

	public readonly Type type;

	public int Width => maxX - minX;

	public bool IsFlat => type == Type.FLAT;

	public bool IsSloped => type == Type.SLOPED;

	public Platform(int minX, int maxX, int baseY, List<BotPoint> sortedPoints, Type type)
	{
		this.minX = minX;
		this.maxX = maxX;
		this.baseY = baseY;
		this.sortedPoints = sortedPoints;
		this.type = type;
	}

	public int GetYAtX(int x)
	{
		if (IsFlat || sortedPoints.Count == 0)
		{
			return baseY;
		}
		x = Math.Max(minX, Math.Min(maxX, x));
		BotPoint? botPoint = null;
		BotPoint? botPoint2 = null;
		foreach (BotPoint sortedPoint in sortedPoints)
		{
			if (sortedPoint.x <= x)
			{
				botPoint = sortedPoint;
			}
			if (sortedPoint.x >= x && !botPoint2.HasValue)
			{
				botPoint2 = sortedPoint;
			}
		}
		if (!botPoint.HasValue)
		{
			return sortedPoints[0].y;
		}
		if (!botPoint2.HasValue)
		{
			return sortedPoints[sortedPoints.Count - 1].y;
		}
		if (botPoint.Value.x == botPoint2.Value.x)
		{
			return botPoint.Value.y;
		}
		double num = (double)(x - botPoint.Value.x) / (double)(botPoint2.Value.x - botPoint.Value.x);
		return (int)((double)botPoint.Value.y + num * (double)(botPoint2.Value.y - botPoint.Value.y));
	}
}
