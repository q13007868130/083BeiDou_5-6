using System;
using System.Collections.Generic;
using MapleDash.Wz;
using Server.Bot;

public static class WzNames
{
	private static readonly Dictionary<int, string> npc = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> map = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> item = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> mob = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> npcNamed = new Dictionary<int, string>();

	private static readonly Dictionary<int, bool> named = new Dictionary<int, bool>();

	public static string Npc(int id)
	{
		if (npc.TryGetValue(id, out var value))
		{
			return value;
		}
		value = NpcOrNull(id) ?? ("NPC " + id);
		npc[id] = value;
		return value;
	}

	public static string NpcOrNull(int id)
	{
		if (npcNamed.TryGetValue(id, out var value))
		{
			return value;
		}
		try
		{
			value = WzAssets.FileRoot("String/Npc.img").Get(id.ToString())?.Text("name");
		}
		catch (Exception)
		{
			value = null;
		}
		if (string.IsNullOrEmpty(value))
		{
			value = null;
		}
		npcNamed[id] = value;
		return value;
	}

	public static string Mob(int id)
	{
		if (mob.TryGetValue(id, out var value))
		{
			return value;
		}
		value = WzAssets.FileRoot("String/Mob.img").Get(id.ToString())?.Text("name");
		if (string.IsNullOrEmpty(value))
		{
			value = "怪物 " + id;
		}
		mob[id] = value;
		return value;
	}

	public static string Map(int id)
	{
		if (map.TryGetValue(id, out var value))
		{
			return value;
		}
		value = null;
		foreach (ImgNode child in WzAssets.FileRoot("String/Map.img").Children)
		{
			ImgNode imgNode = child.Get(id.ToString());
			if (imgNode != null)
			{
				value = imgNode.Text("mapName");
				if (!string.IsNullOrEmpty(value))
				{
					break;
				}
			}
		}
		if (string.IsNullOrEmpty(value))
		{
			value = "地图 " + id;
		}
		map[id] = value;
		return value;
	}

	public static string Item(int id)
	{
		if (item.TryGetValue(id, out var value))
		{
			return value;
		}
		value = ItemUncached(id);
		if (string.IsNullOrEmpty(value))
		{
			value = "道具 " + id;
		}
		item[id] = value;
		return value;
	}

	public static bool HasItem(int id)
	{
		if (named.TryGetValue(id, out var value))
		{
			return value;
		}
		value = !string.IsNullOrEmpty(ItemUncached(id));
		named[id] = value;
		return value;
	}

	private static string ItemUncached(int id)
	{
		int num = id / 1000000;
		string text = null;
		try
		{
			switch (num)
			{
			case 0:
				text = ((id / 10000 >= 2 && id / 10000 <= 6) ? EquipItem(id) : null);
				break;
			case 1:
				text = EquipItem(id);
				break;
			case 2:
				text = FlatItem("String/Consume.img", id);
				break;
			case 3:
				text = FlatItem("String/Ins.img", id);
				break;
			case 4:
				text = FlatItem("String/Etc.img", "Etc", id);
				break;
			case 5:
				text = FlatItem("String/Cash.img", id) ?? FlatItem("String/Pet.img", id);
				break;
			case 9:
				text = SpecialItem(id);
				break;
			case 6:
			case 7:
			case 8:
				break;
			}
		}
		catch
		{
			text = null;
		}
		if (string.IsNullOrEmpty(text))
		{
			text = BotItemCatalog.Shared.Name(id);
		}
		return text;
	}

	private static string EquipItem(int id)
	{
		ImgNode imgNode = WzAssets.FileRoot("String/Eqp.img");
		foreach (ImgNode child in (imgNode.Get("Eqp") ?? imgNode).Children)
		{
			string text = child.Get(id.ToString())?.Text("name");
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
		}
		return null;
	}

	private static string FlatItem(string file, int id)
	{
		return FlatItem(file, null, id);
	}

	private static string FlatItem(string file, string inner, int id)
	{
		ImgNode imgNode = WzAssets.FileRoot(file);
		if (inner != null)
		{
			imgNode = imgNode.Get(inner) ?? imgNode;
		}
		string text = imgNode.Get(id.ToString())?.Text("name");
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return null;
	}

	private static string SpecialItem(int id)
	{
		string relative = "Item/Special/" + (id / 10000).ToString("D4") + ".img";
		if (!WzAssets.HasFile(relative))
		{
			return null;
		}
		ImgNode imgNode = WzAssets.FileRoot(relative);
		string[] array = new string[2]
		{
			id.ToString(),
			id.ToString("D8")
		};
		foreach (string path in array)
		{
			string text = imgNode.Get(path)?.Text("name");
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
		}
		return null;
	}
}
