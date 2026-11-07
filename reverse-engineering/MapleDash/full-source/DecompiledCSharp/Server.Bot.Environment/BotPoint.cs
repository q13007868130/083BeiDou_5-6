using System;

namespace Server.Bot.Environment;

public readonly struct BotPoint(int x, int y) : IEquatable<BotPoint>
{
	public readonly int x = x;

	public readonly int y = y;

	public bool Equals(BotPoint other)
	{
		if (x == other.x)
		{
			return y == other.y;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is BotPoint other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (x * 397) ^ y;
	}
}
