using System;
using System.Collections.Generic;
using System.Globalization;

namespace Server.Bot;

public sealed class BotItemCatalog
{
	private static BotItemCatalog shared;

	private readonly Dictionary<string, object> names;

	private readonly Dictionary<string, object> items;

	private readonly Dictionary<string, object> descriptions;

	public static BotItemCatalog Shared => shared ?? (shared = new BotItemCatalog(BotWarmup.Root("Bot/Items")));

	public int ItemCount => items.Count;

	public IEnumerable<int> ItemIds
	{
		get
		{
			foreach (string key in items.Keys)
			{
				yield return int.Parse(key, CultureInfo.InvariantCulture);
			}
		}
	}

	public BotItemCatalog(string json)
		: this((json == null) ? null : (MiniJsonDb.Parse(json) as Dictionary<string, object>))
	{
	}

	public BotItemCatalog(Dictionary<string, object> root)
	{
		if (root == null)
		{
			throw new InvalidOperationException("Original bot item catalog is missing");
		}
		names = (Dictionary<string, object>)root["names"];
		items = (Dictionary<string, object>)root["items"];
		descriptions = (root.TryGetValue("descriptions", out var value) ? (value as Dictionary<string, object>) : null);
	}

	public string Name(int id)
	{
		if (!names.TryGetValue(id.ToString(CultureInfo.InvariantCulture), out var value))
		{
			return null;
		}
		return value as string;
	}

	public string Description(int id)
	{
		if (descriptions == null || !descriptions.TryGetValue(id.ToString(CultureInfo.InvariantCulture), out var value))
		{
			return null;
		}
		return value as string;
	}

	public Dictionary<string, object> Info(int id)
	{
		if (!items.TryGetValue(id.ToString(CultureInfo.InvariantCulture), out var value) || !(value is Dictionary<string, object> dictionary) || !dictionary.TryGetValue("info", out var value2))
		{
			return null;
		}
		return value2 as Dictionary<string, object>;
	}

	public static int Int(Dictionary<string, object> info, string key, int fallback = 0)
	{
		if (info == null || !info.TryGetValue(key, out var value) || value == null)
		{
			return fallback;
		}
		if (value is long num)
		{
			return (int)num;
		}
		if (value is double num2)
		{
			return (int)num2;
		}
		if (!int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return fallback;
		}
		return result;
	}

	private Dictionary<string, object> Record(int id)
	{
		if (!items.TryGetValue(id.ToString(CultureInfo.InvariantCulture), out var value))
		{
			return null;
		}
		return value as Dictionary<string, object>;
	}

	public int[] RequiredEquips(int id)
	{
		Dictionary<string, object> dictionary = Record(id);
		if (dictionary == null || !dictionary.TryGetValue("req", out var value) || !(value is List<object> list))
		{
			return null;
		}
		int[] array = new int[list.Count];
		for (int i = 0; i < list.Count; i++)
		{
			array[i] = Convert.ToInt32(list[i], CultureInfo.InvariantCulture);
		}
		return array;
	}

	public bool ScopedTo(int id, int itemId)
	{
		Dictionary<string, object> dictionary = Record(id);
		if (dictionary == null || !dictionary.TryGetValue("reqScope", out var value) || !(value is List<object> list))
		{
			return false;
		}
		foreach (object item in list)
		{
			if (item is List<object> { Count: >=2 } list2)
			{
				int num = Convert.ToInt32(list2[0], CultureInfo.InvariantCulture);
				int num2 = Convert.ToInt32(list2[1], CultureInfo.InvariantCulture);
				if (num2 > 0 && itemId / num2 == num)
				{
					return true;
				}
			}
		}
		return false;
	}

	public Dictionary<string, int> EquipStats(int id)
	{
		Dictionary<string, object> dictionary = Info(id);
		if (dictionary == null)
		{
			return null;
		}
		Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
		foreach (KeyValuePair<string, object> item in dictionary)
		{
			if (item.Key.StartsWith("inc", StringComparison.Ordinal))
			{
				dictionary2[item.Key.Substring(3)] = Int(dictionary, item.Key);
			}
		}
		string[] array = new string[12]
		{
			"reqJob", "reqLevel", "reqDEX", "reqSTR", "reqINT", "reqLUK", "reqPOP", "cash", "tuc", "cursed",
			"success", "fs"
		};
		foreach (string key in array)
		{
			dictionary2[key] = Int(dictionary, key);
		}
		return dictionary2;
	}

	public InventoryEntry CleanEquip(int id)
	{
		Dictionary<string, object> info = Info(id);
		return new InventoryEntry
		{
			itemId = id,
			inventoryType = 1,
			quantity = 1,
			isEquip = true,
			str = (short)Int(info, "incSTR"),
			dex = (short)Int(info, "incDEX"),
			int_ = (short)Int(info, "incINT"),
			luk = (short)Int(info, "incLUK"),
			hp = (short)Int(info, "incMHP"),
			mp = (short)Int(info, "incMMP"),
			watk = (short)Int(info, "incPAD"),
			matk = (short)Int(info, "incMAD"),
			wdef = (short)Int(info, "incPDD"),
			mdef = (short)Int(info, "incMDD"),
			acc = (short)Int(info, "incACC"),
			avoid = (short)Int(info, "incEVA"),
			speed = (short)Int(info, "incSpeed"),
			jump = (short)Int(info, "incJump"),
			upgradeslots = (sbyte)Int(info, "tuc"),
			flag = ((Int(info, "tradeBlock") == 1) ? 8 : ((Int(info, "fs") > 0) ? 2 : 0))
		};
	}

	public InventoryEntry CleanItem(int id, int quantity = 1)
	{
		if (id / 1000000 != 1)
		{
			return new InventoryEntry
			{
				itemId = id,
				inventoryType = id / 1000000,
				quantity = (short)quantity
			};
		}
		return CleanEquip(id);
	}
}
