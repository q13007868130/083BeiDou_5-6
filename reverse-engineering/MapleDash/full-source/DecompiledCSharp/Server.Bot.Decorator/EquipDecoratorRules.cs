using System;
using System.Collections.Generic;

namespace Server.Bot.Decorator;

public sealed class EquipDecoratorRules
{
	private readonly Dictionary<string, double> skips = new Dictionary<string, double>();

	private readonly List<string[][]> exclusive = new List<string[][]>();

	private readonly EquipSelection selection;

	private readonly BotCustomization customization;

	private readonly Random random;

	public EquipDecoratorRules(EquipSelection selection, BotCustomization customization, Random random = null)
	{
		this.selection = selection;
		this.customization = customization;
		this.random = random ?? new Random();
	}

	public void Add(string type, double skip)
	{
		skips[type] = skip;
	}

	public void AddExclusive(params string[][] groups)
	{
		exclusive.Add(groups);
	}

	public void Apply(BotCharacter bot)
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (string[][] item2 in exclusive)
		{
			foreach (string[] array in item2)
			{
				foreach (string item in array)
				{
					hashSet.Add(item);
				}
			}
		}
		foreach (KeyValuePair<string, double> skip in skips)
		{
			if (!hashSet.Contains(skip.Key) && random.NextDouble() >= skip.Value)
			{
				try
				{
					customization.Equip(bot, selection.ForWearing(skip.Key, bot));
				}
				catch (Exception)
				{
				}
			}
		}
		foreach (string[][] item3 in exclusive)
		{
			string[] array = item3[random.Next(item3.Length)];
			foreach (string text in array)
			{
				if (random.NextDouble() >= (skips.TryGetValue(text, out var value) ? value : 0.0))
				{
					int value2 = selection.ForWearing(text, bot) ?? throw new InvalidOperationException("Exclusive equip selection returned null");
					customization.Equip(bot, value2);
				}
			}
		}
	}
}
