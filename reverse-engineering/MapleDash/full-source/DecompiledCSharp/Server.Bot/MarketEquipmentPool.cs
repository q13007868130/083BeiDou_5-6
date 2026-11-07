using System;
using System.Collections.Generic;
using Server.Bot.Decorator;

namespace Server.Bot;

public sealed class MarketEquipmentPool
{
	private readonly MarketItemPool pool;

	private readonly BotItemCatalog catalog;

	private readonly EquipMetadataCache cache;

	private readonly MarketScrollPricing scrolls;

	private readonly DbStore db;

	private readonly Random random;

	private readonly EquipSelection selection;

	private readonly HashSet<int> desirable = new HashSet<int>();

	public MarketEquipmentPool(MarketItemPool pool, BotItemCatalog catalog, EquipMetadataCache cache, DbStore db, Random random = null)
	{
		this.pool = pool;
		this.catalog = catalog;
		this.cache = cache;
		this.db = db;
		this.random = random ?? new Random();
		selection = new EquipSelection(cache, this.random);
		scrolls = new MarketScrollPricing(pool, catalog, this.random);
	}

	public void LoadDesirable(BotResources resources)
	{
		if (!(resources.ReadData("soloMapling/itemPool/itemConfig/desirableEquips.yaml") is Dictionary<string, object> dictionary))
		{
			return;
		}
		foreach (object value in dictionary.Values)
		{
			if (!(value is List<object> list))
			{
				continue;
			}
			foreach (object item in list)
			{
				if (int.TryParse(Convert.ToString(item).Trim(), out var result))
				{
					desirable.Add(result);
				}
			}
		}
	}

	public static int[] Pattern(string tier, bool hotRoom = false)
	{
		if (!hotRoom || !tier.Equals("S", StringComparison.OrdinalIgnoreCase))
		{
			if (!tier.Equals("A", StringComparison.OrdinalIgnoreCase) && !tier.Equals("B", StringComparison.OrdinalIgnoreCase))
			{
				return new int[3] { 2, 1, 1 };
			}
			return new int[2] { 2, 2 };
		}
		return new int[4] { 2, 2, 1, 1 };
	}

	public List<string> Categories(IReadOnlyList<string> categories, int[] pattern)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < categories.Count; i++)
		{
			list.Add(i);
		}
		for (int num = list.Count - 1; num > 0; num--)
		{
			int index = random.Next(num + 1);
			int value = list[num];
			list[num] = list[index];
			list[index] = value;
		}
		int num2 = Math.Min(pattern.Length, list.Count);
		list = list.GetRange(0, num2);
		list.Sort();
		List<string> list2 = new List<string>();
		for (int j = 0; j < num2; j++)
		{
			for (int k = 0; k < pattern[j]; k++)
			{
				list2.Add(categories[list[j]]);
			}
		}
		return list2;
	}

	public List<MarketEquipment> FromYaml(string file, IReadOnlyList<string> categories, string tier, bool hotRoom = false)
	{
		List<MarketEquipment> list = new List<MarketEquipment>();
		List<string> list2 = Categories(categories, Pattern(tier, hotRoom));
		string tier2 = pool.DistributedTier(tier);
		foreach (string item in list2)
		{
			Dictionary<string, object> dictionary = pool.Pick(file, item, tier2);
			if (dictionary == null)
			{
				continue;
			}
			int? num = pool.Variant(dictionary);
			if (num.HasValue)
			{
				InventoryEntry inventoryEntry = catalog.CleanEquip(num.Value);
				int price = pool.CurrentPrice(dictionary);
				if (scrolls.DefaultScrolls(num.Value) != null)
				{
					scrolls.ScrollEquip(inventoryEntry, db);
					price = scrolls.EquipValue(inventoryEntry);
				}
				list.Add(new MarketEquipment(inventoryEntry, price));
			}
		}
		return list;
	}

	public int Level(string tier)
	{
		return tier switch
		{
			"B" => random.Next(30, 71), 
			"A" => random.Next(60, 81), 
			"S" => random.Next(80, 121), 
			_ => random.Next(0, 41), 
		};
	}

	public int? Cached(string type, int maxLevel, int jobStyle, string tier)
	{
		int num = ((!(random.NextDouble() < 0.5)) ? 1 : 0);
		int num2 = jobStyle switch
		{
			400 => 8, 
			300 => 4, 
			200 => 2, 
			100 => 1, 
			_ => 0, 
		};
		int num3 = ((num2 != 0) ? Math.Max((int)((double)maxLevel * 0.6), 15) : Math.Max((int)((double)maxLevel * 0.25), 10));
		int num4;
		if (tier.Equals("S", StringComparison.OrdinalIgnoreCase))
		{
			num4 = 100000;
		}
		else if (tier.Equals("A", StringComparison.OrdinalIgnoreCase))
		{
			num4 = 50000;
		}
		else
		{
			num4 = (tier.Equals("B", StringComparison.OrdinalIgnoreCase) ? 20000 : 10000);
		}
		List<int> list = new List<int>();
		foreach (EquipMetadataCache.Entry item in cache.ByType(type))
		{
			if (!item.Untradeable && !item.Quest && !item.Cash && (item.RequiredJob == 0 || item.RequiredJob == num2) && (num2 != 0 || item.RequiredJob == 0) && (item.Gender == 2 || item.Gender == num) && (desirable.Contains(item.Id) || (item.RequiredLevel >= num3 && item.RequiredLevel <= maxLevel && item.Price >= num4)))
			{
				list.Add(item.Id);
			}
		}
		list.Sort();
		return selection.Weighted(list);
	}

	private int AboveAverage(int value, int range)
	{
		if (value != 0)
		{
			return (short)((double)value + Math.Floor(random.NextDouble() * (double)(range + 1)));
		}
		return 0;
	}

	public MarketEquipment Process(int id, bool scroll = false)
	{
		Dictionary<string, object> info = catalog.Info(id);
		int jobStyle = BotItemCatalog.Int(info, "reqJob") switch
		{
			16 => 500, 
			8 => 400, 
			4 => 300, 
			2 => 200, 
			1 => 100, 
			_ => 0, 
		};
		int num = BotItemCatalog.Int(info, "price");
		if (num <= 50)
		{
			num = 5000000;
		}
		int num2 = MarketEconomy.WzPriceByJob(num, jobStyle);
		InventoryEntry inventoryEntry = catalog.CleanEquip(id);
		if (random.NextDouble() > 0.5)
		{
			inventoryEntry.str = AboveAverage(inventoryEntry.str, 2);
			inventoryEntry.dex = AboveAverage(inventoryEntry.dex, 2);
			inventoryEntry.int_ = AboveAverage(inventoryEntry.int_, 2);
			inventoryEntry.luk = AboveAverage(inventoryEntry.luk, 2);
			inventoryEntry.matk = AboveAverage(inventoryEntry.matk, 2);
			inventoryEntry.watk = AboveAverage(inventoryEntry.watk, 2);
			inventoryEntry.acc = AboveAverage(inventoryEntry.acc, 2);
			inventoryEntry.avoid = AboveAverage(inventoryEntry.avoid, 2);
			inventoryEntry.jump = AboveAverage(inventoryEntry.jump, 2);
			inventoryEntry.wdef = AboveAverage(inventoryEntry.wdef, 5);
			inventoryEntry.mdef = AboveAverage(inventoryEntry.mdef, 5);
			inventoryEntry.hp = AboveAverage(inventoryEntry.hp, 5);
			inventoryEntry.mp = AboveAverage(inventoryEntry.mp, 5);
			num2 = MarketEconomy.JavaInt((double)num2 * 1.5);
		}
		if ((scrolls.DefaultScrolls(id) != null) & scroll)
		{
			scrolls.ScrollEquip(inventoryEntry, db);
			num2 = scrolls.EquipValue(inventoryEntry);
		}
		return new MarketEquipment(inventoryEntry, num2);
	}

	public List<MarketEquipment> FromCache(string tier, int jobStyle, bool scroll = false)
	{
		List<MarketEquipment> list = new List<MarketEquipment>();
		if (cache == null)
		{
			return list;
		}
		int maxLevel = Level(tier);
		List<string> list2 = new List<string> { "CAP", "COAT", "LONGCOAT", "PANTS", "GLOVES", "SHOES" };
		switch (jobStyle)
		{
		case 0:
			list2.AddRange(new string[4] { "SHIELD", "CAPE", "EARRING", "ACCESSORY" });
			break;
		case 100:
			list2.AddRange(new string[7] { "SHIELD", "SWORD", "SWORD_2H", "AXE", "AXE_2H", "SPEAR", "POLEARM" });
			break;
		case 200:
			list2.AddRange(new string[3] { "SHIELD", "WAND", "STAFF" });
			break;
		case 300:
			list2.AddRange(new string[2] { "BOW", "CROSSBOW" });
			break;
		case 400:
			list2.AddRange(new string[3] { "SHIELD", "CLAW", "DAGGER" });
			break;
		}
		for (int i = 0; i < 15; i++)
		{
			if (list.Count >= 4)
			{
				break;
			}
			int? num = Cached(list2[random.Next(list2.Count)], maxLevel, jobStyle, tier);
			if (num.HasValue)
			{
				list.Add(Process(num.Value, scroll));
			}
		}
		return list;
	}

	public List<MarketEquipment> ForJob(string tier, int jobStyle, bool hotRoom = false)
	{
		string file;
		string[] categories;
		switch (jobStyle)
		{
		case 0:
			file = "common.yaml";
			categories = new string[8] { "Earring", "Overall", "Gloves", "Shoes", "Cape", "Hat", "Bottom", "Top" };
			break;
		case 100:
			file = "warrior.yaml";
			categories = new string[4] { "Weapon", "Hat", "Top", "Pants" };
			break;
		case 200:
			file = "mage.yaml";
			categories = new string[4] { "Weapon", "Hat", "Top", "Shield" };
			break;
		case 300:
			file = "bowman.yaml";
			categories = new string[4] { "Weapon", "Hat", "Top", "Pants" };
			break;
		case 400:
			file = "thief.yaml";
			categories = new string[5] { "Weapon", "Hat", "Top", "Pants", "Shield" };
			break;
		default:
			return new List<MarketEquipment>();
		}
		List<MarketEquipment> list = FromYaml(file, categories, tier, hotRoom);
		list.AddRange(FromCache(tier, jobStyle));
		return list;
	}
}
