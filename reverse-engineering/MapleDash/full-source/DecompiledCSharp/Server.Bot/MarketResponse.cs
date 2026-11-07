using System.Collections.Generic;

namespace Server.Bot;

public sealed class MarketResponse
{
	public bool ok = true;

	public string error;

	public MarketPurchaseResult purchase;

	public MarketShopSnapshot visiting;

	public readonly List<MarketShopSnapshot> shops = new List<MarketShopSnapshot>();
}
