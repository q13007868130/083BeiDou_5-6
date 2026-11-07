using System;
using System.Collections.Generic;
using Server.Bot.Social;

namespace Server.Bot.Decorator;

public sealed class BotDecorate
{
	private readonly Random random;

	private readonly BotCustomization customization;

	private readonly BotCosmeticPool cosmetics;

	private readonly QuickEquip quick;

	private readonly BotDecorateEquips full;

	private readonly BotDecorateNX nx;

	public BotDecorate(BotResources resources, BotItemCatalog catalog, BotOutput output, Random random = null)
	{
		this.random = random ?? new Random();
		Dictionary<string, object> dictionary = EquipMetadataCache.LoadPools();
		EquipMetadataCache cache = new EquipMetadataCache(catalog, dictionary);
		customization = new BotCustomization(catalog, output);
		cosmetics = new BotCosmeticPool(this.random);
		quick = new QuickEquip(new GenericEquipPool(resources, catalog, this.random), customization, this.random);
		full = new BotDecorateEquips(new EquipSelection(cache, this.random), customization, this.random);
		nx = new BotDecorateNX(new NXItemPool(resources, cache, dictionary, this.random), customization, this.random);
	}

	public static int MinimumEquipLevel(int level)
	{
		return level / 10 * 10 - 9;
	}

	public BotTier RandomTier()
	{
		int num = random.Next(100);
		if (num >= 30)
		{
			if (num >= 60)
			{
				if (num >= 80)
				{
					if (num >= 90)
					{
						return BotTier.D;
					}
					return BotTier.C;
				}
				return BotTier.B;
			}
			return BotTier.A;
		}
		return BotTier.S;
	}

	public int GenerateLevel(BotTier tier, int min, int max)
	{
		if (min >= max)
		{
			throw new ArgumentException("minLevel must be less than maxLevel");
		}
		int num = max - min + 1;
		int num2;
		int num3;
		switch (tier)
		{
		case BotTier.S:
			num2 = min + 3 * num / 4;
			num3 = max;
			break;
		case BotTier.A:
			num2 = min + 2 * num / 4;
			num3 = min + 3 * num / 4 - 1;
			break;
		case BotTier.B:
			num2 = min + num / 4;
			num3 = min + 2 * num / 4 - 1;
			break;
		case BotTier.D:
		case BotTier.C:
			num2 = min;
			num3 = min + num / 4 - 1;
			break;
		default:
			throw new ArgumentOutOfRangeException("tier");
		}
		return (int)(random.NextDouble() * (double)(num3 - num2 + 1)) + num2;
	}

	public int SelectJob(int level)
	{
		if (level < 10)
		{
			return 0;
		}
		int num = (int)(random.NextDouble() * 5.0) + 1;
		if (level < 30)
		{
			return num * 100;
		}
		int num2 = ((num <= 2) ? ((int)(random.NextDouble() * 3.0) + 1) : ((random.NextDouble() < 0.5) ? 1 : 2));
		return num * 100 + num2 * 10 + ((level >= 70) ? ((level < 120) ? 1 : 2) : 0);
	}

	public void EquipWeapon(BotCharacter bot)
	{
		full.Weapon(bot);
	}

	public void ApplyAppearance(BotCharacter bot, DbStore db)
	{
		bot.tier = RandomTier();
		bot.level = GenerateLevel(bot.tier, 120, 200);
		bot.job = SelectJob(bot.level);
		bot.gender = random.Next(2);
		if (BotConfig.RandomBodyEnabled(db))
		{
			cosmetics.DecorateBody(bot);
		}
		if (BotConfig.NormalEquipsEnabled(db))
		{
			if (BotConfig.FullEquipsEnabled(db) && random.NextDouble() < 0.5)
			{
				full.Apply(bot);
			}
			else
			{
				quick.Apply(bot);
				if (BotConfig.FullEquipsEnabled(db) && !HasClothing(bot))
				{
					full.Apply(bot);
				}
			}
		}
		nx.Apply(bot, BotConfig.NxEquipsEnabled(db));
		if (BotConfig.NormalEquipsEnabled(db) && !HasClothing(bot))
		{
			customization.Equip(bot, (bot.gender == 1) ? 1041002 : 1040002);
			customization.Equip(bot, (bot.gender == 1) ? 1061002 : 1060002);
		}
		if (BotConfig.AnyOutfitFeatureEnabled(db) && !bot.equips.ContainsKey(-11) && !bot.equips.ContainsKey(-111))
		{
			customization.Equip(bot, 1302000);
		}
	}

	private static bool HasClothing(BotCharacter bot)
	{
		if (!bot.equips.ContainsKey(-5))
		{
			return bot.equips.ContainsKey(-6);
		}
		return true;
	}
}
