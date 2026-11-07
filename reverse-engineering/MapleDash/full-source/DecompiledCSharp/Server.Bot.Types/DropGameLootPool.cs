using System;
using System.Collections.Generic;
using System.Globalization;

namespace Server.Bot.Types;

public sealed class DropGameLootPool
{
	public readonly struct LootEntry(int itemId, int weight, bool isEquip)
	{
		public readonly int itemId = itemId;

		public readonly int weight = weight;

		public readonly bool isEquip = isEquip;
	}

	public const string ResourcePath = "soloMapling/ArtificialPlayer/BotDialoguePack/DropGameLootPool.yaml";

	private const int SpecialChancePercent = 33;

	private static readonly Random sharedRandom = new Random();

	private readonly Random random;

	private readonly List<LootEntry> entries = new List<LootEntry>();

	private readonly List<LootEntry> specialEntries = new List<LootEntry>();

	private int totalWeight;

	private int specialTotalWeight;

	public bool IsEmpty
	{
		get
		{
			if (entries.Count == 0)
			{
				return specialEntries.Count == 0;
			}
			return false;
		}
	}

	public int Size => entries.Count + specialEntries.Count;

	public DropGameLootPool(Random random = null)
	{
		this.random = random ?? sharedRandom;
	}

	public static DropGameLootPool Load(string tier, Random random = null)
	{
		DropGameLootPool dropGameLootPool = new DropGameLootPool(random);
		try
		{
			if (!(BotResources.Load().ReadData("soloMapling/ArtificialPlayer/BotDialoguePack/DropGameLootPool.yaml") is Dictionary<string, object> dictionary) || !dictionary.TryGetValue(tier, out var value) || !(value is Dictionary<string, object> tierNode))
			{
				return dropGameLootPool;
			}
			LoadEntries(tierNode, "items", dropGameLootPool.entries, dropGameLootPool, special: false);
			LoadEntries(tierNode, "special_items", dropGameLootPool.specialEntries, dropGameLootPool, special: true);
		}
		catch (Exception)
		{
		}
		return dropGameLootPool;
	}

	private static void LoadEntries(Dictionary<string, object> tierNode, string key, List<LootEntry> target, DropGameLootPool pool, bool special)
	{
		if (!tierNode.TryGetValue(key, out var value) || !(value is List<object> list))
		{
			return;
		}
		int num = 0;
		bool result = default;
		foreach (object item in list)
		{
			if (item is Dictionary<string, object> dictionary)
			{
				int itemId = ToInt(dictionary, "id");
				int num2 = ToInt(dictionary, "weight");
				bool isEquip = (dictionary.TryGetValue("equip", out var value2) && bool.TryParse(Convert.ToString(value2, CultureInfo.InvariantCulture), out result)) & result;
				target.Add(new LootEntry(itemId, num2, isEquip));
				num += num2;
			}
		}
		if (special)
		{
			pool.specialTotalWeight = num;
		}
		else
		{
			pool.totalWeight = num;
		}
	}

	private static int ToInt(Dictionary<string, object> entry, string key)
	{
		if (!entry.TryGetValue(key, out var value) || value == null)
		{
			return 0;
		}
		if (value is long num)
		{
			return (int)num;
		}
		if (value is int)
		{
			return (int)value;
		}
		if (!int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result))
		{
			return 0;
		}
		return result;
	}

	public LootEntry? RollItem()
	{
		if (entries.Count == 0 && specialEntries.Count == 0)
		{
			return null;
		}
		if (specialEntries.Count > 0 && random.Next(100) < 33)
		{
			return RollFrom(specialEntries, specialTotalWeight);
		}
		if (entries.Count > 0)
		{
			return RollFrom(entries, totalWeight);
		}
		return RollFrom(specialEntries, specialTotalWeight);
	}

	private LootEntry? RollFrom(List<LootEntry> list, int total)
	{
		if (list.Count == 0 || total == 0)
		{
			return null;
		}
		int num = random.Next(total);
		int num2 = 0;
		foreach (LootEntry item in list)
		{
			num2 += item.weight;
			if (num < num2)
			{
				return item;
			}
		}
		return list[list.Count - 1];
	}
}
