using System.Collections.Generic;
using MapleDash.Wz;
using Server.Bot;

public static class ItemCatalog
{
	private static readonly Dictionary<int, string> nameCache = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> descCache = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> subcatCache = new Dictionary<int, string>();

	public static bool IsEquip(int id)
	{
		return id / 1000000 == 1;
	}

	public static bool IsCash(int id)
	{
		return id / 1000000 == 5;
	}

	private static ImgNode ItemNode(int id)
	{
		if (id / 10000 == 500)
		{
			return WzAssets.Node("String/Pet.img/" + id);
		}
		if (IsEquip(id))
		{
			return WzAssets.Node("String/Eqp.img/Eqp/" + EqpSubcat(id) + "/" + id);
		}
		int num = id / 1000000;
		string text = "String/" + CatFile(id) + ".img";
		return WzAssets.Node((num == 4) ? (text + "/Etc/" + id) : (text + "/" + id));
	}

	public static string Name(int id)
	{
		if (nameCache.TryGetValue(id, out var value))
		{
			return value ?? "";
		}
		ImgNode imgNode = ItemNode(id);
		string text = ((imgNode != null) ? imgNode.Text("name") : "");
		if (string.IsNullOrEmpty(text))
		{
			text = BotItemCatalog.Shared.Name(id);
		}
		nameCache[id] = text;
		return text;
	}

	public static string Desc(int id)
	{
		if (descCache.TryGetValue(id, out var value))
		{
			return value ?? "";
		}
		ImgNode imgNode = ItemNode(id);
		string text = ((imgNode != null) ? imgNode.Text("desc") : "");
		if (string.IsNullOrEmpty(text))
		{
			text = BotItemCatalog.Shared.Description(id);
		}
		descCache[id] = text;
		return text;
	}

	private static string CatFile(int id)
	{
		return (id / 1000000) switch
		{
			1 => "Eqp", 
			2 => "Consume", 
			3 => "Ins", 
			4 => "Etc", 
			5 => "Cash", 
			_ => null, 
		};
	}

	private static string EqpSubcat(int id)
	{
		if (subcatCache.TryGetValue(id, out var value))
		{
			return value;
		}
		int num = id / 10000;
		string text;
		switch (num)
		{
		case 100:
			text = "Cap";
			break;
		case 101:
		case 102:
		case 103:
			text = "Accessory";
			break;
		default:
			switch (num)
			{
			case 104:
				text = "Coat";
				break;
			case 105:
				text = "Longcoat";
				break;
			case 106:
				text = "Pants";
				break;
			case 107:
				text = "Shoes";
				break;
			case 108:
				text = "Glove";
				break;
			case 109:
				text = "Shield";
				break;
			case 110:
				text = "Cape";
				break;
			case 111:
				text = "Ring";
				break;
			case 112:
			case 113:
			case 114:
			case 115:
			case 116:
			case 117:
			case 118:
			case 119:
				text = "Accessory";
				break;
			default:
				if (num >= 180 && num <= 183)
				{
					text = "PetEquip";
					break;
				}
				switch (num)
				{
				case 190:
				case 191:
				case 193:
					text = "Taming";
					break;
				case 194:
				case 195:
				case 196:
				case 197:
					text = "Dragon";
					break;
				default:
					text = ((num != 166 && num != 167) ? "Weapon" : "Android");
					break;
				}
				break;
			}
			break;
		}
		subcatCache[id] = text;
		return text;
	}
}
