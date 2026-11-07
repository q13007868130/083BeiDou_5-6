using System;
using System.Collections.Generic;

namespace Server.Bot.Decorator;

public sealed class NXItemPool
{
	private sealed class Item
	{
		public int id;

		public int gender;
	}

	private readonly Dictionary<string, List<Item>> pools = new Dictionary<string, List<Item>>();

	private readonly Random random;

	private static readonly HashSet<string> body = new HashSet<string> { "caps", "tops", "bottoms", "overalls", "shoes", "gloves", "capes" };

	private static readonly Dictionary<string, string[]> types = new Dictionary<string, string[]>
	{
		["caps"] = new string[1] { "CAP" },
		["tops"] = new string[1] { "COAT" },
		["bottoms"] = new string[1] { "PANTS" },
		["overalls"] = new string[1] { "LONGCOAT" },
		["shoes"] = new string[1] { "SHOES" },
		["gloves"] = new string[1] { "GLOVES" },
		["capes"] = new string[1] { "CAPE" },
		["earrings"] = new string[1] { "EARRING" },
		["face"] = new string[1] { "FACE" },
		["eye"] = new string[1] { "ACCESSORY" },
		["rings"] = new string[1] { "RING" },
		["weapons"] = new string[16]
		{
			"SWORD", "AXE", "MACE", "DAGGER", "WAND", "STAFF", "SWORD_2H", "AXE_2H", "MACE_2H", "SPEAR",
			"POLEARM", "BOW", "CROSSBOW", "CLAW", "KNUCKLER", "PISTOL"
		}
	};

	public NXItemPool(BotResources resources, EquipMetadataCache cache, Dictionary<string, object> data, Random random = null)
	{
		this.random = random ?? new Random();
		Reload(resources, cache, data);
	}

	public void Reload(BotResources resources, EquipMetadataCache cache, Dictionary<string, object> data)
	{
		pools.Clear();
		if (resources.ReadData("soloMapling/ArtificialPlayer/BotDecoratorSystem/NXItemPool.yaml") is Dictionary<string, object> dictionary)
		{
			foreach (KeyValuePair<string, object> item in dictionary)
			{
				if (!(item.Value is List<object> list))
				{
					continue;
				}
				List<Item> list2 = new List<Item>();
				pools[item.Key] = list2;
				foreach (object item2 in list)
				{
					if (item2 == null)
					{
						continue;
					}
					int? value = null;
					int id;
					if (item2 is Dictionary<string, object> dictionary2)
					{
						id = (dictionary2.TryGetValue("id", out var value2) ? Convert.ToInt32(value2) : 0);
						if (dictionary2.TryGetValue("gender", out var value3))
						{
							value = ParseGender(value3);
						}
					}
					else
					{
						if (!(item2 is string) && !(item2 is long) && !(item2 is int) && !(item2 is double))
						{
							continue;
						}
						id = Convert.ToInt32(item2);
					}
					list2.Add(new Item
					{
						id = id,
						gender = Gender(id, item.Key, value)
					});
				}
			}
		}
		foreach (KeyValuePair<string, object> item3 in (Dictionary<string, object>)data["fashion"])
		{
			List<object> list3 = (List<object>)item3.Value;
			if (list3.Count == 0)
			{
				continue;
			}
			pools[item3.Key] = new List<Item>();
			foreach (object item4 in list3)
			{
				pools[item3.Key].Add(new Item
				{
					id = Convert.ToInt32(item4),
					gender = 2
				});
			}
		}
		foreach (KeyValuePair<string, string[]> type2 in types)
		{
			if (pools.TryGetValue(type2.Key, out var value4) && value4.Count > 0)
			{
				continue;
			}
			value4 = new List<Item>();
			pools[type2.Key] = value4;
			string[] value5 = type2.Value;
			foreach (string type in value5)
			{
				foreach (EquipMetadataCache.Entry item5 in cache.ByType(type, cashOnly: true))
				{
					value4.Add(new Item
					{
						id = item5.Id,
						gender = Gender(item5.Id, type2.Key, null)
					});
				}
			}
		}
		foreach (KeyValuePair<string, object> item6 in (Dictionary<string, object>)data["fallback"])
		{
			if (pools.TryGetValue(item6.Key, out var value6) && value6.Count > 0)
			{
				continue;
			}
			value6 = new List<Item>();
			pools[item6.Key] = value6;
			foreach (object item7 in (List<object>)item6.Value)
			{
				value6.Add(new Item
				{
					id = Convert.ToInt32(item7),
					gender = 2
				});
			}
		}
		if (!pools.TryGetValue("rings", out var value7))
		{
			value7 = (pools["rings"] = new List<Item>());
		}
		HashSet<int> hashSet = new HashSet<int>();
		foreach (Item item8 in value7)
		{
			hashSet.Add(item8.id);
		}
		foreach (List<object> item9 in (List<object>)data["showcaseRingRanges"])
		{
			int j = Convert.ToInt32(item9[0]);
			for (int num = Convert.ToInt32(item9[1]); j <= num; j++)
			{
				if (hashSet.Add(j))
				{
					value7.Add(new Item
					{
						id = j,
						gender = 2
					});
				}
			}
		}
	}

	private static int? ParseGender(object raw)
	{
		return Convert.ToString(raw)?.ToLowerInvariant() switch
		{
			"male" => (int?)0, 
			"0" => 0, 
			"female" => 1, 
			"1" => 1, 
			"unisex" => 2, 
			"2" => 2, 
			_ => null, 
		};
	}

	private static int Gender(int id, string category, int? value)
	{
		int num = id / 1000 % 10;
		int? num2 = value;
		if (!num2.HasValue)
		{
			if (!body.Contains(category) || num > 1)
			{
				return 2;
			}
			return num;
		}
		return num2.GetValueOrDefault();
	}

	public int? Pick(string category, int gender)
	{
		if (!pools.TryGetValue(category, out var value))
		{
			return null;
		}
		List<Item> list = new List<Item>();
		foreach (Item item in value)
		{
			if (item.gender == 2 || item.gender == gender)
			{
				list.Add(item);
			}
		}
		if (list.Count != 0)
		{
			return list[random.Next(list.Count)].id;
		}
		return null;
	}
}
