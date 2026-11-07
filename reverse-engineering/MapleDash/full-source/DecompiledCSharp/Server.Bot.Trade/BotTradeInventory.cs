using System.Collections.Generic;

namespace Server.Bot.Trade;

public sealed class BotTradeInventory
{
	public readonly List<InventoryEntry> itemsForSale = new List<InventoryEntry>(9);

	public InventoryEntry MainItem
	{
		get
		{
			if (itemsForSale.Count != 0)
			{
				return itemsForSale[0];
			}
			return null;
		}
	}

	public void SetMain(InventoryEntry item)
	{
		if (itemsForSale.Count == 0)
		{
			itemsForSale.Add(item);
		}
		else
		{
			itemsForSale[0] = item;
		}
	}

	public void Add(InventoryEntry item)
	{
		itemsForSale.Add(item);
	}

	public void Remove(InventoryEntry item)
	{
		itemsForSale.Remove(item);
	}

	public void Reset()
	{
		itemsForSale.Clear();
	}
}
