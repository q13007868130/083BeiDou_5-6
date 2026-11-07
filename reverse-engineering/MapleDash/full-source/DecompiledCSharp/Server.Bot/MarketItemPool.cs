using System;
using System.Collections.Generic;
using System.Globalization;

namespace Server.Bot;

public sealed class MarketItemPool
{
	private const string Directory = "soloMapling/itemPool/itemConfig/";

	private static readonly string[] databasePools = new string[6] { "thief.yaml", "common.yaml", "etc.yaml", "scrolls.yaml", "darkscrolls.yaml", "useables.yaml" };

	private static readonly string[] scrolls = new string[9] { "Earring", "Overall", "Gloves", "Claw", "Dagger", "Shoes", "Gloves", "Cape", "ETC" };

	private static readonly string[] darkScrolls = new string[11]
	{
		"Earring", "Overall", "Shoes", "Gloves", "Cape", "Hat", "Top", "Bottom", "Shield", "ETC",
		"Special"
	};

	private readonly BotResources resources;

	private readonly Random random;

	private readonly Dictionary<string, Dictionary<string, object>> pools = new Dictionary<string, Dictionary<string, object>>();

	private readonly Dictionary<int, Dictionary<string, object>> database = new Dictionary<int, Dictionary<string, object>>();

	private bool databaseLoaded;

	public int Version = 55;

	public MarketItemPool(BotResources resources, Random random = null)
	{
		this.resources = resources;
		this.random = random ?? new Random();
	}

	private static int Number(object value)
	{
		return int.Parse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
	}

	private static Dictionary<string, object> Map(object value)
	{
		return value as Dictionary<string, object>;
	}

	private static object Get(Dictionary<string, object> map, string key)
	{
		if (map == null || !map.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	private static List<object> List(object value)
	{
		return value as List<object>;
	}

	public static object ValueForVersion(Dictionary<string, object> values, int version)
	{
		object result = null;
		int num = -1;
		if (values != null)
		{
			foreach (KeyValuePair<string, object> value in values)
			{
				int num2 = Number(value.Key);
				if (num2 <= version && num2 > num)
				{
					num = num2;
					result = value.Value;
				}
			}
		}
		return result;
	}

	private Dictionary<string, object> Load(string file)
	{
		file = file.Replace('\\', '/');
		file = file.Substring(file.LastIndexOf('/') + 1);
		if (pools.TryGetValue(file, out var value))
		{
			return value;
		}
		try
		{
			value = Map(resources.ReadData("soloMapling/itemPool/itemConfig/" + file));
		}
		catch (Exception)
		{
			value = null;
		}
		pools[file] = value;
		return value;
	}

	public Dictionary<string, object> Pick(string file, string type, string tier)
	{
		List<object> list = List(Get(Load(file), type));
		if (list == null)
		{
			return null;
		}
		List<Dictionary<string, object>> list2 = new List<Dictionary<string, object>>();
		foreach (object item in list)
		{
			Dictionary<string, object> dictionary = Map(item);
			if (Convert.ToString(ValueForVersion(Map(Get(dictionary, "tier")), Version), CultureInfo.InvariantCulture) == tier)
			{
				list2.Add(dictionary);
			}
		}
		if (list2.Count != 0)
		{
			return list2[random.Next(list2.Count)];
		}
		return null;
	}

	public int? Variant(Dictionary<string, object> item)
	{
		List<object> list = List(Get(item, "variant_id"));
		if (list != null && list.Count != 0)
		{
			return Number(list[random.Next(list.Count)]);
		}
		return null;
	}

	public int CurrentPrice(Dictionary<string, object> item)
	{
		object obj = ValueForVersion(Map(Get(item, "price")), Version);
		if (obj != null)
		{
			return Number(obj);
		}
		return -1;
	}

	public Dictionary<string, object> Item(int id)
	{
		if (!databaseLoaded)
		{
			databaseLoaded = true;
			string[] array = databasePools;
			foreach (string file in array)
			{
				Dictionary<string, object> dictionary = Load(file);
				if (dictionary == null)
				{
					break;
				}
				foreach (object value2 in dictionary.Values)
				{
					if (!(value2 is List<object> list))
					{
						continue;
					}
					foreach (object item in list)
					{
						Dictionary<string, object> dictionary2 = Map(item);
						List<object> list2 = List(Get(dictionary2, "variant_id"));
						if (list2 == null)
						{
							continue;
						}
						foreach (object item2 in list2)
						{
							database[Number(item2)] = dictionary2;
						}
					}
				}
			}
		}
		if (!database.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public int? Price(int id)
	{
		object obj = ValueForVersion(Map(Get(Item(id), "price")), Version);
		if (obj != null)
		{
			return Number(obj);
		}
		return null;
	}

	public string Tier(int id)
	{
		return Convert.ToString(ValueForVersion(Map(Get(Item(id), "tier")), Version), CultureInfo.InvariantCulture);
	}

	public bool Exists(int id)
	{
		Dictionary<string, object> dictionary = Map(Get(Item(id), "tier"));
		if (dictionary == null)
		{
			return false;
		}
		foreach (string key in dictionary.Keys)
		{
			if (Version >= Number(key))
			{
				return true;
			}
		}
		return false;
	}

	public int Quantity(string type, string tier)
	{
		Dictionary<string, object> dictionary = Map(Get(Map(Get(Map(Get(Map(Get(Load("itemQuantities.yaml"), "itemQuantities")), type)), "tiers")), tier));
		if (dictionary == null)
		{
			return 5;
		}
		int num = Number(Get(dictionary, "min"));
		int num2 = Number(Get(dictionary, "max"));
		return random.Next(num2 - num + 1) + num;
	}

	public string DistributedTier(string tier)
	{
		int num = random.Next(100);
		switch (tier)
		{
		case "S":
			if (num >= 60)
			{
				if (num >= 98)
				{
					return "B";
				}
				return "A";
			}
			return "S";
		case "A":
			if (num >= 70)
			{
				if (num >= 80)
				{
					return "B";
				}
				return "S";
			}
			return "A";
		case "B":
			if (num >= 68)
			{
				if (num >= 98)
				{
					return "S";
				}
				return "A";
			}
			return "B";
		default:
			if (num >= 20)
			{
				if (num >= 60)
				{
					return "B";
				}
				return "A";
			}
			return "S";
		}
	}

	public List<MarketItem> Scrolls(string tier, bool dark = false)
	{
		if (!dark || Version >= 39)
		{
			return Generate(dark ? "darkscrolls.yaml" : "scrolls.yaml", dark ? darkScrolls : scrolls, "Scroll", tier, 4);
		}
		return new List<MarketItem>();
	}

	public List<MarketItem> Potions(string tier)
	{
		return Generate("useables.yaml", new string[1] { "Potions" }, "Potion", tier, 3);
	}

	public List<MarketItem> Etc(string tier)
	{
		return Generate("etc.yaml", new string[9] { "CrystalRefined", "CrystalOre", "MineralRefined", "MineralOre", "JewelRefined", "JewelOre", "SkillRocks", "BossItems", "RandomETC" }, "ETC", tier, 3);
	}

	public List<MarketItem> MasteryBooks(string tier)
	{
		return Generate("masteryBook.yaml", new string[1] { "MasteryBooks" }, "Mastery", tier, 3);
	}

	public List<MarketItem> Chairs(string tier)
	{
		return Generate("chair.yaml", new string[1] { "Chairs" }, null, tier, 1);
	}

	public List<MarketItem> Stars(string tier)
	{
		List<MarketItem> list = new List<MarketItem>();
		for (int i = 0; i < 2; i++)
		{
			Dictionary<string, object> item = Pick("thief.yaml", "Stars", DistributedTier(tier));
			int? num = Variant(item);
			if (num.HasValue)
			{
				list.Add(new MarketItem(num.Value, CurrentPrice(item)));
			}
		}
		return list;
	}

	private List<MarketItem> Generate(string file, string[] categories, string quantityType, string tier, int count)
	{
		List<MarketItem> list = new List<MarketItem>();
		HashSet<int> hashSet = new HashSet<int>();
		for (int i = 0; i < count; i++)
		{
			string type = categories[random.Next(categories.Length)];
			tier = DistributedTier(tier);
			Dictionary<string, object> dictionary = Pick(file, type, tier);
			if (dictionary == null)
			{
				continue;
			}
			int? num = Variant(dictionary);
			if (num.HasValue)
			{
				int price = CurrentPrice(dictionary);
				int quantity = ((quantityType == null) ? 1 : Quantity(quantityType, tier));
				if (hashSet.Add(num.Value))
				{
					list.Add(new MarketItem(num.Value, price, quantity));
				}
			}
		}
		return list;
	}
}
