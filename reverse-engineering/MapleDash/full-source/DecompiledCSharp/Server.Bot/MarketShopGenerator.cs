using System;
using System.Collections.Generic;

namespace Server.Bot;

public sealed class MarketShopGenerator
{
	private static readonly int[] normal = new int[12]
	{
		12, 12, 12, 18, 18, 1, 5, 5, 5, 5,
		3, 4
	};

	private static readonly int[] hot = new int[12]
	{
		18, 18, 18, 20, 16, 0, 3, 2, 2, 1,
		1, 1
	};

	private static readonly MarketShopType[] secondary = new MarketShopType[6]
	{
		MarketShopType.Potion,
		MarketShopType.Scroll,
		MarketShopType.DarkScroll,
		MarketShopType.Mastery,
		MarketShopType.Chair,
		MarketShopType.ETC
	};

	private static readonly int[] secondaryWeights = new int[6] { 40, 25, 15, 5, 2, 13 };

	private readonly MarketItemPool pool;

	private readonly MarketEquipmentPool equipment;

	private readonly MarketEconomy economy;

	private readonly Random random;

	public Func<MarketShop, string> Description;

	public MarketShopGenerator(MarketItemPool pool, MarketEquipmentPool equipment, MarketEconomy economy, Random random = null)
	{
		this.pool = pool;
		this.equipment = equipment;
		this.economy = economy;
		this.random = random ?? new Random();
	}

	private int Weighted(int[] weights)
	{
		int num = 0;
		foreach (int num2 in weights)
		{
			num += num2;
		}
		int num3 = random.Next(num);
		for (int j = 0; j < weights.Length; j++)
		{
			num3 -= weights[j];
			if (num3 < 0)
			{
				return j;
			}
		}
		throw new InvalidOperationException();
	}

	public void Generate(MarketShop shop)
	{
		shop.tier = economy.TierForRoom(shop.mapId);
		bool flag = MarketEconomy.IsHotRoom(shop.mapId);
		shop.primary = (MarketShopType)Weighted(flag ? hot : normal);
		Primary(shop, shop.primary);
		if (flag)
		{
			Primary(shop, (MarketShopType)random.Next(5));
			Secondary(shop);
			Secondary(shop);
			int num = 0;
			while (shop.items.Count < 10 && num < 3)
			{
				Primary(shop, (MarketShopType)random.Next(5));
				num++;
			}
		}
		else
		{
			Secondary(shop);
			if (shop.items.Count < 5)
			{
				shop.tertiary = (MarketShopType)random.Next(5);
				Primary(shop, shop.tertiary.Value);
			}
		}
		shop.description = Description?.Invoke(shop);
		foreach (MarketShopStock item in shop.items)
		{
			if (flag && (item.item.inventoryType == 2 || item.item.inventoryType == 4))
			{
				item.bundles = (short)(item.bundles * random.Next(5, 9));
			}
			if (random.NextDouble() < 0.09)
			{
				item.bundles = 0;
				item.exists = false;
			}
		}
		ApplySale(shop, random.Next(10000));
		shop.description = shop.description + "          <" + (shop.mapId - 910000000) + ">";
		EnsureItems(shop);
	}

	public static void ApplySale(MarketShop shop, int roll)
	{
		if (roll >= 800)
		{
			return;
		}
		if (roll == 0)
		{
			shop.description = "一金币店";
		}
		else
		{
			shop.description += ((roll < 100) ? "清仓" : "低价");
		}
		foreach (MarketShopStock item in shop.items)
		{
			item.price = ((roll == 0) ? 1 : ((int)((double)item.price * ((roll < 100) ? 0.7 : 0.85))));
		}
	}

	public static void EnsureItems(MarketShop shop)
	{
		if (shop.items.Count == 0)
		{
			int[] array = new int[2] { 2000002, 2000001 };
			foreach (int itemId in array)
			{
				shop.items.Add(new MarketShopStock
				{
					item = new InventoryEntry
					{
						itemId = itemId,
						inventoryType = 2,
						position = 1,
						quantity = 100
					},
					bundles = 20,
					price = 1
				});
			}
		}
	}

	private void Add(MarketShop shop, InventoryEntry item, int bundles, int price)
	{
		shop.items.Add(new MarketShopStock
		{
			item = item,
			bundles = (short)MarketEconomy.Quantity(bundles, shop.mapId, shop.x, shop.y),
			price = economy.Price(price, shop.mapId, shop.x, shop.y)
		});
	}

	private void Primary(MarketShop shop, MarketShopType type)
	{
		if (type <= MarketShopType.Pirate)
		{
			int num = type switch
			{
				MarketShopType.Thief => 400, 
				MarketShopType.Bowman => 300, 
				MarketShopType.Mage => 200, 
				MarketShopType.Warrior => 100, 
				_ => 0, 
			};
			foreach (MarketEquipment item in equipment.ForJob(shop.tier, num, MarketEconomy.IsHotRoom(shop.mapId)))
			{
				Add(shop, item.Item, 1, item.Price);
			}
			if (num == 400 && random.NextDouble() < ((shop.tier == "S") ? 0.7 : ((shop.tier == "A") ? 0.4 : 0.1)))
			{
				Items(shop, MarketShopType.Stars);
			}
		}
		else
		{
			for (int i = 0; i < ((type == MarketShopType.Chair) ? 5 : 3); i++)
			{
				Items(shop, type);
			}
		}
	}

	private void Secondary(MarketShop shop)
	{
		int num = ((random.Next(2) == 0) ? 1 : 2);
		HashSet<MarketShopType> hashSet = new HashSet<MarketShopType>();
		while (hashSet.Count < num)
		{
			MarketShopType marketShopType = secondary[Weighted(secondaryWeights)];
			if (hashSet.Add(marketShopType))
			{
				if (hashSet.Count == 1)
				{
					shop.secondary = marketShopType;
				}
				Items(shop, marketShopType);
			}
		}
	}

	private void Items(MarketShop shop, MarketShopType type)
	{
		foreach (MarketItem item in type switch
		{
			MarketShopType.Scroll => pool.Scrolls(shop.tier), 
			MarketShopType.DarkScroll => pool.Scrolls(shop.tier, dark: true), 
			MarketShopType.Potion => pool.Potions(shop.tier), 
			MarketShopType.ETC => pool.Etc(shop.tier), 
			MarketShopType.Mastery => pool.MasteryBooks(shop.tier), 
			MarketShopType.Chair => pool.Chairs(shop.tier), 
			MarketShopType.Stars => pool.Stars(shop.tier), 
			_ => throw new ArgumentOutOfRangeException("type"), 
		})
		{
			Add(shop, new InventoryEntry
			{
				itemId = item.itemId,
				inventoryType = item.itemId / 1000000,
				position = 1,
				quantity = 1
			}, item.quantity, item.price);
		}
	}
}
