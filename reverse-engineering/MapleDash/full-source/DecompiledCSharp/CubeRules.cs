using System;

public static class CubeRules
{
	public sealed class Family
	{
		public readonly bool bonus;

		public readonly bool choice;

		public readonly bool karma;

		public readonly int minimum;

		public readonly int maximum;

		public readonly string policy;

		internal Family(bool bonus, bool choice, int maximum, bool karma, string policy, int minimum = 1)
		{
			this.bonus = bonus;
			this.choice = choice;
			this.maximum = maximum;
			this.karma = karma;
			this.policy = policy;
			this.minimum = minimum;
		}
	}

	public static Family GetFamily(int id)
	{
		switch (id)
		{
		case 2711000:
		case 2711001:
		case 2711009:
		case 2711011:
		case 2711027:
			return new Family(bonus: false, choice: false, 2, karma: false, "MYSTICAL_CUBES_TIER_UP_RATES");
		case 2711003:
		case 2711005:
		case 2711012:
			return new Family(bonus: false, choice: false, 3, karma: false, "HARD_CUBES_TIER_UP_RATES");
		case 2711023:
		case 2711026:
			return new Family(bonus: false, choice: false, 3, karma: true, "HARD_CUBES_TIER_UP_RATES");
		case 2711004:
		case 2711006:
		case 2711013:
			return new Family(bonus: false, choice: false, 4, karma: false, "SOLID_CUBE_TIER_UP_RATES");
		case 2711017:
		case 2711021:
		case 2711028:
			return new Family(bonus: false, choice: false, 4, karma: true, "SOLID_CUBE_TIER_UP_RATES");
		case 2711024:
		case 2711025:
		case 5062032:
			return new Family(bonus: false, choice: true, 4, karma: true, "BRIGHT_CUBES_TIER_UP_RATES");
		case 5062030:
			return new Family(bonus: false, choice: true, 4, karma: false, "BRIGHT_CUBES_TIER_UP_RATES");
		case 2730000:
		case 2730002:
		case 2730004:
		case 2730013:
			return new Family(bonus: true, choice: false, 2, karma: false, "BONUS_MYSTICAL_CUBE_TIER_UP_RATES");
		case 2730006:
		case 2730020:
			return new Family(bonus: true, choice: false, 2, karma: true, "BONUS_MYSTICAL_CUBE_TIER_UP_RATES");
		case 2730011:
		case 2730012:
		case 5062521:
			return new Family(bonus: true, choice: true, 4, karma: true, "BONUS_BRIGHT_CUBE_TIER_UP_RATES");
		case 5062507:
		case 5062508:
			return new Family(bonus: true, choice: false, 4, id == 5062508, "BONUS_POT_CUBE_TIER_UP_RATE", 2);
		case 5062031:
			return new Family(bonus: true, choice: true, 4, karma: false, "BONUS_BRIGHT_CUBE_TIER_UP_RATES", 2);
		default:
			return null;
		}
	}

	public static long AppraisalCost(int equipLevel, int cubeId, int insightLevel = 0)
	{
		Family family = GetFamily(cubeId);
		if (family == null || equipLevel < 0 || equipLevel > 1000 || insightLevel < 0 || insightLevel > 100)
		{
			throw new ArgumentException("Unsupported cube appraisal inputs");
		}
		if (equipLevel <= 30 || (cubeId / 1000000 == 5 && !family.bonus))
		{
			return 0L;
		}
		int num;
		if (insightLevel >= 90)
		{
			num = 120;
		}
		else
		{
			num = ((insightLevel >= 60) ? 70 : ((insightLevel >= 30) ? 30 : 0));
		}
		if (equipLevel <= num)
		{
			return 0L;
		}
		long num2 = (long)equipLevel * (long)equipLevel;
		return ((equipLevel <= 70) ? (num2 / 2) : ((equipLevel <= 120) ? (num2 * 5 / 2) : (num2 * 20))) / 100 * 100;
	}
}
