using System;

namespace Server.Bot.Decorator;

public sealed class QuickEquip
{
	public bool Enabled = true;

	private readonly GenericEquipPool pool;

	private readonly BotCustomization customization;

	private readonly Random random;

	public QuickEquip(GenericEquipPool pool, BotCustomization customization, Random random = null)
	{
		this.pool = pool;
		this.customization = customization;
		this.random = random ?? new Random();
	}

	public void Apply(BotCharacter bot)
	{
		if (!Enabled)
		{
			return;
		}
		double num = bot.tier switch
		{
			BotTier.S => 0.9, 
			BotTier.A => 0.75, 
			BotTier.B => 0.55, 
			BotTier.C => 0.35, 
			BotTier.D => 0.15, 
			_ => 0.3, 
		};
		if (random.NextDouble() < num)
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
		(string, double)[] array = new (string, double)[5]
		{
			("weapons", 0.9),
			("caps", 0.6),
			("shoes", 0.5),
			("capes", 0.3),
			("gloves", 0.3)
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, double) tuple = array[i];
			if (random.NextDouble() < num * tuple.Item2)
			{
				customization.Equip(bot, Pick(bot, tuple.Item1));
			}
		}
	}

	private int? Pick(BotCharacter bot, string category)
	{
		return pool.Pick(category, bot.level, bot.gender);
	}

	private bool Overall(BotCharacter bot)
	{
		int? id = Pick(bot, "overalls");
		if (!id.HasValue)
		{
			return false;
		}
		customization.Equip(bot, id);
		return true;
	}

	private bool TopBottom(BotCharacter bot)
	{
		int? id = Pick(bot, "tops");
		int? id2 = Pick(bot, "bottoms");
		if (!id.HasValue || !id2.HasValue)
		{
			return false;
		}
		customization.Equip(bot, id);
		customization.Equip(bot, id2);
		return true;
	}
}
