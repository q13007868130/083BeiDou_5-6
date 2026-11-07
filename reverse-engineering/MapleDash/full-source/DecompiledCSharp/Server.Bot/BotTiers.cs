using System;

namespace Server.Bot;

public static class BotTiers
{
	public const BotTier Default = BotTier.S;

	public static string Description(this BotTier tier)
	{
		return tier switch
		{
			BotTier.S => "Elite", 
			BotTier.A => "High", 
			BotTier.B => "Above Average", 
			BotTier.C => "Average", 
			BotTier.D => "Below Average", 
			_ => throw new ArgumentOutOfRangeException("tier"), 
		};
	}

	public static bool IsAtLeast(this BotTier tier, BotTier minimum)
	{
		return tier >= minimum;
	}

	public static bool IsBetterThan(this BotTier tier, BotTier other)
	{
		return tier > other;
	}

	public static bool IsWorseThan(this BotTier tier, BotTier other)
	{
		return tier < other;
	}

	public static BotTier OrDefault(this BotTier tier, BotTier minimum, BotTier fallback)
	{
		if (tier < minimum)
		{
			return fallback;
		}
		return tier;
	}

	public static BotTier BoostIfBelow(this BotTier tier, BotTier threshold, BotTier boostTo)
	{
		return tier.OrDefault(threshold, boostTo);
	}

	public static BotTier NextHigher(this BotTier tier)
	{
		if (tier != BotTier.S)
		{
			return tier + 1;
		}
		return tier;
	}

	public static BotTier NextLower(this BotTier tier)
	{
		if (tier != BotTier.D)
		{
			return tier - 1;
		}
		return tier;
	}

	public static int Difference(this BotTier tier, BotTier other)
	{
		return tier - other;
	}

	public static BotTier? FromString(string value)
	{
		if (value == null)
		{
			return null;
		}
		return value.ToUpperInvariant() switch
		{
			"S" => (BotTier?)BotTier.S, 
			"A" => BotTier.A, 
			"B" => BotTier.B, 
			"C" => BotTier.C, 
			"D" => BotTier.D, 
			_ => null, 
		};
	}

	public static BotTier SafeSet(BotTier? current, BotTier? value)
	{
		return value ?? BotTier.S;
	}

	public static BotTier Effective(BotTier? current)
	{
		return (current ?? BotTier.S).BoostIfBelow(BotTier.C, BotTier.B);
	}

	public static bool CanPerformAction(BotTier? current, BotTier? required)
	{
		if (current.HasValue && required.HasValue)
		{
			return current.Value >= required.Value;
		}
		return false;
	}

	public static BotTier Upgrade(BotTier? current)
	{
		if (!current.HasValue)
		{
			return BotTier.S;
		}
		return current.Value.NextHigher();
	}

	public static BotTier Downgrade(BotTier? current)
	{
		if (!current.HasValue)
		{
			return BotTier.S;
		}
		return current.Value.NextLower();
	}

	public static BotTier Safe(BotTier? tier)
	{
		return tier ?? BotTier.S;
	}
}
