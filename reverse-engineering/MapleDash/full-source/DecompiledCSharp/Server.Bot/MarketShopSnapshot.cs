using System.Collections.Generic;

namespace Server.Bot;

public sealed class MarketShopSnapshot
{
	public int ownerId;

	public int permitId;

	public int x;

	public int y;

	public string ownerName;

	public string description;

	public bool playerStore;

	public readonly List<MarketShopStock> items = new List<MarketShopStock>();

	public readonly List<string> chat = new List<string>();

	public readonly List<MarketVisitorSnapshot> visitors = new List<MarketVisitorSnapshot>();

	public static MarketShopSnapshot Of(MarketShop shop)
	{
		if (shop == null || !shop.open)
		{
			return null;
		}
		MarketShopSnapshot marketShopSnapshot = new MarketShopSnapshot
		{
			ownerId = shop.ownerId,
			permitId = shop.permitId,
			x = shop.x,
			y = shop.y,
			ownerName = shop.ownerName,
			description = shop.description,
			playerStore = shop.playerStore
		};
		foreach (MarketShopStock item in shop.items)
		{
			marketShopSnapshot.items.Add(new MarketShopStock
			{
				item = item.item.Copy(),
				price = item.price,
				bundles = item.bundles,
				exists = item.exists
			});
		}
		marketShopSnapshot.chat.AddRange(shop.chat);
		return marketShopSnapshot;
	}
}
