using System;
using System.Collections.Generic;

namespace Server.Bot.Decorator;

public sealed class BotDecorateNX
{
	public bool Enabled = true;

	private readonly NXItemPool pool;

	private readonly BotCustomization customization;

	private readonly Random random;

	private static readonly string[] categories = new string[9] { "weapons", "caps", "clothing", "shoes", "gloves", "capes", "earrings", "face", "eye" };

	private static readonly int[] weights = new int[9] { 10, 10, 10, 6, 6, 6, 3, 3, 3 };

	public BotDecorateNX(NXItemPool pool, BotCustomization customization, Random random = null)
	{
		this.pool = pool;
		this.customization = customization;
		this.random = random ?? new Random();
	}

	public void Apply(BotCharacter bot, bool configEnabled, bool forced = false)
	{
		if (!forced && (!Enabled || !configEnabled))
		{
			return;
		}
		Equip(bot, "weapons");
		Clothing(bot);
		int minValue = ((bot.tier != BotTier.S) ? 1 : 2);
		int num = bot.tier switch
		{
			BotTier.S => 5, 
			BotTier.A => 3, 
			BotTier.B => 2, 
			BotTier.C => 1, 
			BotTier.D => 1, 
			_ => 2, 
		};
		int num2 = random.Next(minValue, num + 1);
		List<int> list = new List<int>();
		for (int i = 0; i < categories.Length; i++)
		{
			list.Add(i);
		}
		for (int j = 0; j < num2; j++)
		{
			if (list.Count <= 0)
			{
				break;
			}
			int num3 = 0;
			foreach (int item in list)
			{
				num3 += weights[item];
			}
			int num4 = random.Next(num3);
			int num5 = 0;
			for (int k = 0; k < list.Count; k++)
			{
				int num6 = list[k];
				num5 += weights[num6];
				if (num4 < num5)
				{
					list.RemoveAt(k);
					if (categories[num6] == "clothing")
					{
						Clothing(bot);
					}
					else
					{
						Equip(bot, categories[num6]);
					}
					break;
				}
			}
		}
		double num7 = bot.tier switch
		{
			BotTier.S => 0.4, 
			BotTier.A => 0.25, 
			BotTier.B => 0.15, 
			BotTier.C => 0.05, 
			BotTier.D => 0.02, 
			_ => 0.05, 
		};
		if (random.NextDouble() < num7)
		{
			Equip(bot, "rings");
		}
	}

	private void Equip(BotCharacter bot, string category)
	{
		customization.Equip(bot, pool.Pick(category, bot.gender));
	}

	private void Clothing(BotCharacter bot)
	{
		if (random.NextDouble() < 0.75)
		{
			if (!Overall(bot))
			{
				TopBottom(bot);
			}
		}
		else if (!TopBottom(bot))
		{
			Overall(bot);
		}
	}

	private bool Overall(BotCharacter bot)
	{
		int? id = pool.Pick("overalls", bot.gender);
		if (!id.HasValue)
		{
			return false;
		}
		customization.Equip(bot, id);
		return true;
	}

	private bool TopBottom(BotCharacter bot)
	{
		int? id = pool.Pick("tops", bot.gender);
		int? id2 = pool.Pick("bottoms", bot.gender);
		if (!id.HasValue || !id2.HasValue)
		{
			return false;
		}
		customization.Equip(bot, id);
		customization.Equip(bot, id2);
		return true;
	}
}
