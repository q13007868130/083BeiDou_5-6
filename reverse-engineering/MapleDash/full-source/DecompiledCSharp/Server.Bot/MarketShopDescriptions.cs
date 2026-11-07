using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Bot;

public sealed class MarketShopDescriptions
{
	private readonly BotResources resources;

	private readonly BotItemCatalog catalog;

	private readonly Random random;

	private readonly List<string> clans = new List<string>();

	public MarketShopDescriptions(BotResources resources, BotItemCatalog catalog, Random random = null)
	{
		this.resources = resources;
		this.catalog = catalog;
		this.random = random ?? new Random();
	}

	private string Pick(IReadOnlyList<string> list)
	{
		return list[random.Next(list.Count)];
	}

	private string Normalize(string value)
	{
		if (!string.IsNullOrWhiteSpace(value) && !MarketShopTitles.HasLatin(value))
		{
			return value.Trim();
		}
		return Pick(MarketShopTitles.COMMON);
	}

	private void Shuffle(int[] values)
	{
		for (int num = values.Length - 1; num > 0; num--)
		{
			int num2 = random.Next(num + 1);
			int num3 = values[num];
			values[num] = values[num2];
			values[num2] = num3;
		}
	}

	private string Text(string type)
	{
		string text;
		string[] list;
		switch (type)
		{
		case "thief":
			text = "thiefDesc.txt";
			list = MarketShopTitles.THIEF;
			break;
		case "warrior":
			text = "warriorDesc.txt";
			list = MarketShopTitles.WARRIOR;
			break;
		case "mage":
			text = "mageDesc.txt";
			list = MarketShopTitles.MAGE;
			break;
		case "bowman":
			text = "bowmanDesc.txt";
			list = MarketShopTitles.BOWMAN;
			break;
		case "chair":
			text = "chairDesc.txt";
			list = MarketShopTitles.CHAIR;
			break;
		case "scrolls":
			text = "scrollsDesc.txt";
			list = MarketShopTitles.SCROLLS;
			break;
		case "useable":
			text = "useableDesc.txt";
			list = MarketShopTitles.USEABLE;
			break;
		case "etc":
			text = "etcDesc.txt";
			list = MarketShopTitles.ETC;
			break;
		case "fmclan":
			text = "FMClans.txt";
			list = MarketShopTitles.CLAN;
			break;
		case "shortword":
			text = "shortWordDesc.txt";
			list = MarketShopTitles.SHORT;
			break;
		case "emojis":
			text = "emojiFaces.txt";
			list = MarketShopTitles.EMOJI;
			break;
		default:
			text = "commonDesc.txt";
			list = MarketShopTitles.COMMON;
			break;
		}
		List<string> list2 = new List<string>();
		using (StringReader stringReader = new StringReader(resources.ReadText("soloMapling/FreeMarket/FMNameDesc/" + text) ?? ""))
		{
			string text2;
			while ((text2 = stringReader.ReadLine()) != null)
			{
				if (text2.Length != 0 && !MarketShopTitles.HasLatin(text2))
				{
					list2.Add(text2);
				}
			}
		}
		if (list2.Count != 0)
		{
			return Pick(list2);
		}
		return Pick(list);
	}

	private string Clan()
	{
		if (clans.Count == 0)
		{
			for (int i = 0; i < 7; i++)
			{
				clans.Add(Text("fmclan"));
			}
		}
		return "【" + Pick(clans) + "】";
	}

	private static string TypeName(MarketShopType type)
	{
		switch (type)
		{
		case MarketShopType.Thief:
		case MarketShopType.Stars:
			return "thief";
		case MarketShopType.Warrior:
			return "warrior";
		case MarketShopType.Mage:
			return "mage";
		case MarketShopType.Bowman:
			return "bowman";
		case MarketShopType.Chair:
			return "chair";
		case MarketShopType.Scroll:
		case MarketShopType.DarkScroll:
		case MarketShopType.Mastery:
			return "scrolls";
		case MarketShopType.Potion:
			return "useable";
		case MarketShopType.ETC:
			return "etc";
		default:
			return "common";
		}
	}

	private string TopItem(MarketShop shop)
	{
		int num = 0;
		int id = 0;
		foreach (MarketShopStock item in shop.items)
		{
			if (item.item.inventoryType != 1 && item.price > num)
			{
				num = item.price;
				id = item.item.itemId;
			}
		}
		string text = catalog.Name(id);
		if (text == null)
		{
			return null;
		}
		if (!text.Contains("White Scroll") && !text.Contains("Chaos Scroll"))
		{
			string[] array = new string[7] { "Dark Scroll ", "Dark scroll ", "Scroll ", "for ", "[Mastery Book] ", "Throwing-Knives", "Throwing-Stars" };
			foreach (string oldValue in array)
			{
				text = text.Replace(oldValue, "").Trim();
			}
		}
		if (!MarketShopTitles.HasLatin(text))
		{
			return text;
		}
		return "";
	}

	private string TopEquip(MarketShop shop)
	{
		int num = 0;
		InventoryEntry inventoryEntry = null;
		foreach (MarketShopStock item in shop.items)
		{
			if (item.item.inventoryType == 1 && item.price > num)
			{
				num = item.price;
				inventoryEntry = item.item;
			}
		}
		if (inventoryEntry == null)
		{
			return TopItem(shop);
		}
		bool flag = random.NextDouble() < 0.5;
		string text = catalog.Name(inventoryEntry.itemId);
		if (string.IsNullOrWhiteSpace(text) || MarketShopTitles.HasLatin(text))
		{
			return Pick(MarketShopTitles.COMMON);
		}
		if (BotItemCatalog.Int(inventoryEntry.equipmentData, "level") == 0)
		{
			return "未砸" + text;
		}
		if (!flag)
		{
			return "神砸" + text;
		}
		int bonus;
		string text2;
		int num2;
		switch (MarketScrollPricing.HighestStat(inventoryEntry, catalog.CleanEquip(inventoryEntry.itemId), out bonus))
		{
		case "watt":
			text2 = "物攻";
			num2 = inventoryEntry.watk;
			break;
		case "matt":
			text2 = "魔攻";
			num2 = inventoryEntry.matk;
			break;
		case "str":
			text2 = "力量";
			num2 = inventoryEntry.str;
			break;
		case "dex":
			text2 = "敏捷";
			num2 = inventoryEntry.dex;
			break;
		case "int":
			text2 = "智力";
			num2 = inventoryEntry.int_;
			break;
		default:
			text2 = "幸运";
			num2 = inventoryEntry.luk;
			break;
		}
		return text + text2 + num2;
	}

	private string GenerateTop(int type, MarketShop shop)
	{
		switch (type)
		{
		case 0:
			return Clan();
		case 1:
			return TopItem(shop);
		case 2:
			return TopEquip(shop);
		case 3:
			return Text(TypeName(shop.primary));
		case 4:
		case 5:
			return Text("shortword");
		default:
			if (string.IsNullOrWhiteSpace(shop.ownerName) || MarketShopTitles.HasLatin(shop.ownerName) || shop.ownerName.Length > 6)
			{
				return "欢迎光临";
			}
			return shop.ownerName + "的小店";
		}
	}

	public string Generate(MarketShop shop)
	{
		int[] array = new int[7] { 0, 1, 2, 3, 4, 5, 6 };
		int[] array2 = new int[3] { 0, 1, 2 };
		Shuffle(array);
		Shuffle(array2);
		int num = shop.mapId % 100;
		int num2 = -1;
		string text = null;
		if (MarketEconomy.IsHotRoom(shop.mapId) && random.NextDouble() < 0.9)
		{
			num2 = 0;
			text = Clan();
		}
		if (num2 < 0)
		{
			int[] array3 = array;
			foreach (int num3 in array3)
			{
				bool flag = shop.tier == "S" || shop.tier == "A";
				bool num4;
				if (num3 != 0)
				{
					if (num3 > 2)
					{
						if (num3 != 3)
						{
							if (num3 > 5)
							{
								if (!(shop.tier == "B") || num < 13)
								{
									continue;
								}
								num4 = num <= 22;
							}
							else
							{
								if (num < 3)
								{
									continue;
								}
								num4 = num <= 22;
							}
						}
						else
						{
							if (num < 1)
							{
								continue;
							}
							num4 = num <= 22;
						}
					}
					else
					{
						if (!flag || num < 1)
						{
							continue;
						}
						num4 = num <= 22;
					}
				}
				else
				{
					if (!flag)
					{
						continue;
					}
					if (num == 1 || num == 2)
					{
						goto IL_0131;
					}
					num4 = num == 7;
				}
				if (!num4)
				{
					continue;
				}
				goto IL_0131;
				IL_0131:
				string text2 = GenerateTop(num3, shop);
				if (text2 != null)
				{
					num2 = num3;
					text = text2;
					break;
				}
			}
		}
		if (random.NextDouble() < 0.4)
		{
			int[] array3 = array2;
			foreach (int num5 in array3)
			{
				double num6;
				switch (num2)
				{
				default:
					num6 = 0.0;
					break;
				case 4:
				case 5:
					num6 = ((num5 == 1) ? 0.0 : 0.5);
					break;
				case 3:
					num6 = num5 switch
					{
						1 => 0.3, 
						0 => 0.6, 
						_ => 0.5, 
					};
					break;
				case 1:
				case 2:
					num6 = num5 switch
					{
						1 => 0.3, 
						0 => 0.8, 
						_ => 0.4, 
					};
					break;
				case 0:
					num6 = ((num5 == 0) ? 0.7 : 0.5);
					break;
				}
				double num7 = num6;
				if (num7 != 0.0 && !(random.NextDouble() >= num7))
				{
					text = text + " " + num5 switch
					{
						1 => Pick(MarketShopTitles.CURRENCY), 
						0 => Pick(MarketShopTitles.OFFER), 
						_ => Text("emojis"), 
					};
					break;
				}
			}
		}
		return Normalize(text);
	}
}
