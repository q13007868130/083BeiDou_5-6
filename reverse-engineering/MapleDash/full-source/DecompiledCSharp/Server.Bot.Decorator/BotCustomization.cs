using System;
using System.Collections.Generic;
using Server.Bot.Social;

namespace Server.Bot.Decorator;

public sealed class BotCustomization
{
	private readonly BotItemCatalog catalog;

	private readonly BotOutput output;

	public BotCustomization(BotItemCatalog catalog, BotOutput output)
	{
		this.catalog = catalog;
		this.output = output;
	}

	public static int EquipSlotType(int id)
	{
		if (id == 1189999)
		{
			return 53;
		}
		int num = id / 10000;
		if (num == 134 || num == 135)
		{
			return 51;
		}
		if (id >= 1302000 && id < 1493000)
		{
			return 11;
		}
		return num switch
		{
			100 => 1, 
			101 => 2, 
			102 => 3, 
			103 => 4, 
			104 => 5, 
			105 => 5, 
			106 => 6, 
			107 => 7, 
			108 => 8, 
			109 => 10, 
			110 => 9, 
			111 => 12, 
			112 => 17, 
			113 => 50, 
			114 => 49, 
			115 => 20, 
			118 => 55, 
			119 => 56, 
			167 => 54, 
			_ => 0, 
		};
	}

	public int DestinationSlot(int id)
	{
		int num = EquipSlotType(id);
		bool flag = BotItemCatalog.Int(catalog.Info(id), "cash") == 1;
		if (flag && id >= 1700000 && id < 1800000)
		{
			return -111;
		}
		if (!flag)
		{
			return -num;
		}
		return -((num == 11) ? 111 : (num + 100));
	}

	public void Equip(BotCharacter bot, int? id)
	{
		if (id.HasValue)
		{
			Equip(bot, id.Value, DestinationSlot(id.Value));
		}
	}

	public void Equip(BotCharacter bot, int id, int slot)
	{
		InventoryEntry inventoryEntry = catalog.CleanItem(id);
		if (!inventoryEntry.isEquip)
		{
			throw new InvalidCastException("Bot equipment must be an equip item");
		}
		inventoryEntry.position = slot;
		bot.equipment[slot] = inventoryEntry;
		bot.equips[slot] = id;
		output.Send(BotOutput.Kind.Appearance, bot);
	}

	public void Clear(BotCharacter bot)
	{
		bot.equipment.Clear();
		bot.equips.Clear();
	}

	public void ClearCash(BotCharacter bot)
	{
		if (bot == null)
		{
			return;
		}
		foreach (int item in new List<int>(bot.equips.Keys))
		{
			if (item > -160 && item <= -100)
			{
				bot.equips.Remove(item);
				bot.equipment.Remove(item);
			}
		}
		output.Send(BotOutput.Kind.Appearance, bot);
	}
}
