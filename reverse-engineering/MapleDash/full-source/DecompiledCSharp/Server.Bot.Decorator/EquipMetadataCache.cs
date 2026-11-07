using System;
using System.Collections.Generic;

namespace Server.Bot.Decorator;

public sealed class EquipMetadataCache
{
	public sealed class Entry
	{
		public int Id;

		public int Gender;

		public int RequiredLevel;

		public int RequiredJob;

		public int Price;

		public string Type;

		public string Name;

		public bool Cash;

		public bool Untradeable;

		public bool Quest;
	}

	private readonly List<Entry> all = new List<Entry>();

	public IReadOnlyList<Entry> All => all;

	public EquipMetadataCache(BotItemCatalog catalog, Dictionary<string, object> pools)
	{
		List<object> list = (List<object>)pools["ranges"];
		List<object> list2 = (List<object>)pools["extraRingRanges"];
		foreach (int itemId in catalog.ItemIds)
		{
			string text = null;
			foreach (List<object> item in list2)
			{
				if (itemId >= Convert.ToInt32(item[0]) && itemId <= Convert.ToInt32(item[1]))
				{
					text = "RING";
					break;
				}
			}
			if (text == null)
			{
				foreach (List<object> item2 in list)
				{
					if (itemId >= Convert.ToInt32(item2[1]) && itemId <= Convert.ToInt32(item2[2]))
					{
						text = (string)item2[0];
						break;
					}
				}
			}
			Dictionary<string, object> dictionary = catalog.Info(itemId);
			if (text != null && dictionary != null)
			{
				int num = itemId / 1000 % 10;
				all.Add(new Entry
				{
					Id = itemId,
					Type = text,
					Gender = ((num <= 2) ? num : 2),
					RequiredLevel = BotItemCatalog.Int(dictionary, "reqLevel"),
					RequiredJob = BotItemCatalog.Int(dictionary, "reqJob"),
					Cash = (BotItemCatalog.Int(dictionary, "cash") == 1),
					Untradeable = (BotItemCatalog.Int(dictionary, "tradeBlock") == 1),
					Quest = (BotItemCatalog.Int(dictionary, "quest") == 1),
					Price = BotItemCatalog.Int(dictionary, "price"),
					Name = (catalog.Name(itemId) ?? "?")
				});
			}
		}
		all.Sort((Entry a, Entry b) => a.Id.CompareTo(b.Id));
	}

	public IEnumerable<Entry> ByType(string type, bool cashOnly = false)
	{
		foreach (Entry item in all)
		{
			if (item.Type == type && (!cashOnly || item.Cash))
			{
				yield return item;
			}
		}
	}

	public static Dictionary<string, object> LoadPools()
	{
		return (MiniJsonDb.Parse(PlatformRes.LoadText("Bot/EquipmentPools")) as Dictionary<string, object>) ?? throw new InvalidOperationException("Original equipment pool data is missing");
	}
}
