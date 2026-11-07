using System;

namespace Server.Bot;

public sealed class BotAppearanceTemplate
{
	public int gender;

	public int skinColor;

	public int hair;

	public int face;

	public int level;

	public int job;

	public BotTier tier = BotTier.S;

	public int[] slots = Array.Empty<int>();

	public int[] items = Array.Empty<int>();

	public string[] ids = Array.Empty<string>();

	public string key = "";

	public void ApplyTo(BotCharacter bot)
	{
		bot.gender = gender;
		bot.skinColor = skinColor;
		bot.hair = hair;
		bot.face = face;
		bot.level = level;
		bot.job = job;
		bot.tier = tier;
		bot.equips.Clear();
		for (int i = 0; i < slots.Length; i++)
		{
			bot.equips[slots[i]] = items[i];
		}
	}
}
