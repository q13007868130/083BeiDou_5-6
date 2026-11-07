using System;
using System.Collections.Generic;

namespace Server.Bot;

public sealed class MarketShop
{
	public int ownerId;

	public int mapId;

	public int world;

	public int channel;

	public int permitId;

	public int x;

	public int y;

	public long merchantMeso;

	public int purchasedStocks;

	public string ownerName;

	public string description;

	public string tier;

	public bool open;

	public bool playerStore;

	public Func<int, int, long> playerSell;

	public MarketShopType primary;

	public MarketShopType secondary;

	public MarketShopType? tertiary;

	public readonly List<MarketShopStock> items = new List<MarketShopStock>();

	public readonly List<MarketSoldItem> sold = new List<MarketSoldItem>();

	public readonly List<string> chat = new List<string>();
}
