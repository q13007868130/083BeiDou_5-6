using System.Collections.Generic;

namespace Server.Bot.Trade;

public sealed class BotTradeWants
{
	public long mesoWanted = 1L;

	public int mesoOffering;

	public readonly List<ItemQuantity> itemsWanted = new List<ItemQuantity>(9);

	public void AddItemWanted(int id, int quantity = 1)
	{
		itemsWanted.Add(new ItemQuantity(id, quantity));
	}

	public ItemQuantity? GetItemQuantityAt(int index)
	{
		if (index < 0 || index >= itemsWanted.Count)
		{
			return null;
		}
		return itemsWanted[index];
	}

	public List<int> GetListItemsWanted()
	{
		List<int> list = new List<int>(itemsWanted.Count);
		foreach (ItemQuantity item in itemsWanted)
		{
			list.Add(item.itemId);
		}
		return list;
	}

	public void SetWants(long mesos, IEnumerable<int> ids)
	{
		Reset();
		mesoWanted = mesos;
		foreach (int id in ids)
		{
			AddItemWanted(id);
		}
	}

	public void SetWants(long mesos, int id)
	{
		SetWants(mesos, new int[1] { id });
	}

	public void SetWants(int id)
	{
		SetWants(0L, id);
	}

	public void SetWants(IEnumerable<int> ids)
	{
		SetWants(0L, ids);
	}

	public bool VerifyTrade(long mesos, IList<InventoryEntry> offered)
	{
		if (VerifySufficientMeso(mesos))
		{
			return VerifySufficientItems(offered);
		}
		return false;
	}

	public bool VerifySufficientMeso(long mesos)
	{
		if (mesos >= mesoWanted)
		{
			return true;
		}
		double num;
		if (mesoWanted >= 1000000000)
		{
			num = 0.01;
		}
		else if (mesoWanted >= 100000000)
		{
			num = 0.02;
		}
		else if (mesoWanted >= 10000000)
		{
			num = 0.05;
		}
		else if (mesoWanted >= 1000000)
		{
			num = 0.1;
		}
		else if (mesoWanted >= 100000)
		{
			num = 0.15;
		}
		else
		{
			num = ((mesoWanted >= 10000) ? 0.2 : 0.0);
		}
		return mesoWanted - mesos <= (long)((double)mesoWanted * num);
	}

	public bool VerifySufficientItems(IList<InventoryEntry> offered)
	{
		if (itemsWanted.Count == 0)
		{
			return true;
		}
		if (offered == null || offered.Count == 0)
		{
			return false;
		}
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		foreach (InventoryEntry item in offered)
		{
			dictionary.TryGetValue(item.itemId, out var value);
			dictionary[item.itemId] = value + ((item.quantity <= 1) ? 1 : item.quantity);
		}
		foreach (ItemQuantity item2 in itemsWanted)
		{
			if (!dictionary.TryGetValue(item2.itemId, out var value2) || value2 < item2.quantity)
			{
				return false;
			}
		}
		return true;
	}

	public void Reset()
	{
		mesoWanted = 4206969L;
		itemsWanted.Clear();
	}
}
