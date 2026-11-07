using System;
using System.Collections.Generic;

namespace Server.Bot.Decorator;

public sealed class BotCosmeticPool
{
	private readonly Random random;

	private static readonly Dictionary<BotTier, int[]> HAIR_EYE_WEIGHTS = new Dictionary<BotTier, int[]>
	{
		[BotTier.S] = new int[3] { 55, 35, 10 },
		[BotTier.A] = new int[3] { 40, 45, 15 },
		[BotTier.B] = new int[3] { 20, 55, 25 },
		[BotTier.C] = new int[3] { 10, 50, 40 },
		[BotTier.D] = new int[3] { 5, 40, 55 }
	};

	private static readonly Dictionary<CosmeticTier, int[]> MALE_HAIR = new Dictionary<CosmeticTier, int[]>
	{
		[CosmeticTier.PREMIUM] = new int[12]
		{
			48700, 48710, 48720, 48730, 42220, 48740, 48750, 42230, 42240, 42250,
			48760, 48770
		},
		[CosmeticTier.STANDARD] = new int[4] { 40070, 40080, 42100, 42220 },
		[CosmeticTier.BASIC] = new int[3] { 40070, 40080, 42100 }
	};

	private static readonly Dictionary<CosmeticTier, int[]> FEMALE_HAIR = new Dictionary<CosmeticTier, int[]>
	{
		[CosmeticTier.PREMIUM] = new int[12]
		{
			46540, 46550, 47140, 48670, 48680, 48690, 48700, 48710, 48720, 48730,
			42220, 48740
		},
		[CosmeticTier.STANDARD] = new int[4] { 43280, 44430, 44460, 42220 },
		[CosmeticTier.BASIC] = new int[3] { 43280, 44430, 44460 }
	};

	private static readonly Dictionary<CosmeticTier, int[]> MALE_EYES = new Dictionary<CosmeticTier, int[]>
	{
		[CosmeticTier.PREMIUM] = new int[1] { 20005 },
		[CosmeticTier.STANDARD] = new int[3] { 20000, 20001, 20002 },
		[CosmeticTier.BASIC] = new int[3] { 20000, 20001, 20002 }
	};

	private static readonly Dictionary<CosmeticTier, int[]> FEMALE_EYES = new Dictionary<CosmeticTier, int[]>
	{
		[CosmeticTier.PREMIUM] = new int[2] { 21003, 21004 },
		[CosmeticTier.STANDARD] = new int[3] { 21001, 21000, 21002 },
		[CosmeticTier.BASIC] = new int[3] { 21000, 21001, 21002 }
	};

	private static readonly Dictionary<BotTier, double> HAIR_COLOR_CHANCE = new Dictionary<BotTier, double>
	{
		[BotTier.S] = 0.6,
		[BotTier.A] = 0.45,
		[BotTier.B] = 0.3,
		[BotTier.C] = 0.18,
		[BotTier.D] = 0.1
	};

	private static readonly Dictionary<BotTier, double> EYE_COLOR_CHANCE = new Dictionary<BotTier, double>
	{
		[BotTier.S] = 0.55,
		[BotTier.A] = 0.4,
		[BotTier.B] = 0.25,
		[BotTier.C] = 0.15,
		[BotTier.D] = 0.08
	};

	private static readonly int[] WILD_MALE_HAIR = new int[9] { 46540, 46550, 47140, 48670, 48680, 48690, 42220, 42260, 42270 };

	private static readonly int[] WILD_FEMALE_HAIR = new int[9] { 46540, 46550, 47140, 48670, 48680, 48690, 42220, 42260, 42270 };

	private static readonly int[] WILD_MALE_EYES = new int[1] { 20012 };

	private static readonly int[] WILD_FEMALE_EYES = new int[1] { 21009 };

	public BotCosmeticPool(Random random = null)
	{
		this.random = random ?? new Random();
	}

	private CosmeticTier RollTier(BotTier tier)
	{
		int[] array = HAIR_EYE_WEIGHTS[tier];
		int num = random.Next(100);
		if (num >= array[0])
		{
			if (num >= array[0] + array[1])
			{
				return CosmeticTier.BASIC;
			}
			return CosmeticTier.STANDARD;
		}
		return CosmeticTier.PREMIUM;
	}

	private int Pick(int[] pool)
	{
		return pool[random.Next(pool.Length)];
	}

	public int SelectHair(int gender, BotTier tier)
	{
		int num = ((random.NextDouble() < 0.01) ? Pick((gender == 1) ? WILD_FEMALE_HAIR : WILD_MALE_HAIR) : Pick(((gender == 1) ? FEMALE_HAIR : MALE_HAIR)[RollTier(tier)]));
		if (!(random.NextDouble() < HAIR_COLOR_CHANCE[tier]))
		{
			return num;
		}
		return num + random.Next(7) + 1;
	}

	public int SelectEyes(int gender, BotTier tier)
	{
		int num = ((random.NextDouble() < 0.01) ? Pick((gender == 1) ? WILD_FEMALE_EYES : WILD_MALE_EYES) : Pick(((gender == 1) ? FEMALE_EYES : MALE_EYES)[RollTier(tier)]));
		if (!(random.NextDouble() < EYE_COLOR_CHANCE[tier]))
		{
			return num;
		}
		return num + (random.Next(8) + 1) * 100;
	}

	public void DecorateBody(BotCharacter bot)
	{
		bot.skinColor = ((!(random.NextDouble() < 0.8)) ? 1 : 0);
		bot.face = SelectEyes(bot.gender, bot.tier);
		bot.hair = SelectHair(bot.gender, bot.tier);
	}
}
