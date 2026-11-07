using System;
using System.Collections.Generic;
using Server.Bot.Decorator;

namespace Server.Bot;

public static class BotAppearanceTemplates
{
	private const int Seed = 20261027;

	public const int DefaultCount = 32;

	private static readonly List<BotAppearanceTemplate> pool = new List<BotAppearanceTemplate>();

	private static BotDecorate builder;

	private static int builtCount = -1;

	public static IReadOnlyList<BotAppearanceTemplate> All => pool;

	public static bool Enabled => pool.Count > 0;

	public static void Reset()
	{
		pool.Clear();
		builder = null;
		builtCount = -1;
	}

	public static void Ensure(DbStore db)
	{
		int num = Math.Max(0, BotConfig.AppearanceTemplateCount(db));
		if (builtCount == num)
		{
			return;
		}
		pool.Clear();
		builtCount = num;
		if (num != 0)
		{
			if (builder == null)
			{
				builder = new BotDecorate(BotResources.Load(), BotItemCatalog.Shared, BotTypeManager.Output, new Random(20261027));
			}
			for (int i = 0; i < num; i++)
			{
				pool.Add(Build(db));
			}
		}
	}

	public static BotAppearanceTemplate Take(Random random)
	{
		if (pool.Count != 0)
		{
			return pool[random.Next(pool.Count)];
		}
		return null;
	}

	private static BotAppearanceTemplate Build(DbStore db)
	{
		BotCharacter botCharacter = new BotCharacter();
		builder.ApplyAppearance(botCharacter, db);
		BotAppearanceTemplate botAppearanceTemplate = new BotAppearanceTemplate
		{
			gender = botCharacter.gender,
			skinColor = botCharacter.skinColor,
			hair = botCharacter.hair,
			face = botCharacter.face,
			level = botCharacter.level,
			job = botCharacter.job,
			tier = botCharacter.tier
		};
		List<int> list = new List<int>(botCharacter.equips.Count);
		List<int> list2 = new List<int>(botCharacter.equips.Count);
		foreach (KeyValuePair<int, int> equip in botCharacter.equips)
		{
			list.Add(equip.Key);
			list2.Add(equip.Value);
		}
		botAppearanceTemplate.slots = list.ToArray();
		botAppearanceTemplate.items = list2.ToArray();
		botAppearanceTemplate.ids = BotAppearance.CharacterIds(botCharacter);
		botAppearanceTemplate.key = "bot_" + string.Join("_", botAppearanceTemplate.ids);
		return botAppearanceTemplate;
	}
}
