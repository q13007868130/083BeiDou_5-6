using System;
using System.Collections.Generic;
using Server.Bot.Decorator;
using Server.Bot.Environment;
using Server.Bot.Trade;
using Server.Field;

namespace Server.Bot;

public sealed class MarketShopService : IDisposable
{
	private static readonly int[] permits = new int[5] { 5030000, 5030001, 5030002, 5030004, 5030008 };

	private readonly DbStore db;

	private readonly BotTradeService trades;

	private readonly Func<int, MapRuntime> map;

	private readonly Random random;

	private readonly Dictionary<int, MarketShop> shops = new Dictionary<int, MarketShop>();

	private readonly Dictionary<int, int> counts = new Dictionary<int, int>();

	private readonly Dictionary<int, int> visits = new Dictionary<int, int>();

	private readonly Dictionary<int, int> visitorSlots = new Dictionary<int, int>();

	private readonly List<BotTasks.Handle> pending = new List<BotTasks.Handle>();

	private MarketShopGenerator generator;

	private bool disposed;

	private bool populated;

	private int ownerId = 800001;

	private MarketShopGenerator Generator
	{
		get
		{
			if (generator != null)
			{
				return generator;
			}
			BotResources resources = BotResources.Load();
			BotItemCatalog shared = BotItemCatalog.Shared;
			MarketItemPool pool = new MarketItemPool(resources, random);
			MarketEquipmentPool marketEquipmentPool = new MarketEquipmentPool(pool, shared, new EquipMetadataCache(shared, EquipMetadataCache.LoadPools()), db, random);
			marketEquipmentPool.LoadDesirable(resources);
			MarketShopDescriptions marketShopDescriptions = new MarketShopDescriptions(resources, shared, random);
			return generator = new MarketShopGenerator(pool, marketEquipmentPool, new MarketEconomy(random), random)
			{
				Description = marketShopDescriptions.Generate
			};
		}
	}

	public MarketShopService(DbStore db, BotTradeService trades, Func<int, MapRuntime> map, Random random = null)
	{
		this.db = db;
		this.trades = trades;
		this.map = map;
		this.random = random ?? new Random();
	}

	public List<MarketShop> OnMap(int world, int channel, int mapId)
	{
		List<MarketShop> list = new List<MarketShop>();
		foreach (MarketShop value in shops.Values)
		{
			if (value.open && value.world == world && value.channel == channel && value.mapId == mapId)
			{
				list.Add(value);
			}
		}
		return list;
	}

	public MarketShop Get(int id)
	{
		if (!shops.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public List<MarketShop> AllOpen()
	{
		List<MarketShop> list = new List<MarketShop>();
		foreach (MarketShop value in shops.Values)
		{
			if (value.open)
			{
				list.Add(value);
			}
		}
		return list;
	}

	private bool Reserve(int mapId)
	{
		int num = (counts.TryGetValue(mapId, out var value) ? value : 0);
		if (num >= Math.Max(BotConfig.MarketShopMax(db), 24))
		{
			return false;
		}
		counts[mapId] = num + 1;
		return true;
	}

	public void PopulateRooms()
	{
		if (disposed || populated || !BotConfig.FmRegionFillEnabled(db))
		{
			return;
		}
		populated = true;
		for (int i = 910000001; i <= 910000012; i++)
		{
			int room = i;
			BotPoint[] array = MarketShopSpots.ForRoom(room);
			for (int num = array.Length - 1; num > 0; num--)
			{
				int num2 = random.Next(num + 1);
				BotPoint botPoint = array[num];
				array[num] = array[num2];
				array[num2] = botPoint;
			}
			BotPoint[] array2 = array;
			foreach (BotPoint botPoint2 in array2)
			{
				BotPoint position = botPoint2;
				BotEnvironment.Post(() =>
				{
					Spawn(room, position, playerStore: false);
				}, "PopulateRooms");
			}
			for (int num3 = 0; num3 < 2; num3++)
			{
				BotPoint point = array[random.Next(array.Length)];
				BotEnvironment.Post(() =>
				{
					Spawn(room, point, playerStore: true);
				}, "PopulateRooms");
			}
		}
	}

	private void Spawn(int mapId, BotPoint point, bool playerStore)
	{
		if (disposed || !Reserve(mapId))
		{
			return;
		}
		try
		{
			if (map(mapId) == null)
			{
				counts[mapId]--;
				return;
			}
			BotPoint botPoint = PlatformSpawner.SnapToGround(mapId, point);
			if (playerStore)
			{
				BotCharacter actor = BotGeneration.CreateBotPollReadiness(botPoint.x, botPoint.y, mapId, ignoreEnvironmentLimit: true);
				if (actor == null)
				{
					counts[mapId]--;
					return;
				}
				pending.Add(BotTasks.Schedule(() =>
				{
					if (!disposed)
					{
						Open(actor.id, actor.name, mapId, (int)actor.x, (int)actor.y, BotFurniture.RandomStorePermit(random), playerStore: true);
						if (random.NextDouble() < 0.5)
						{
							actor.facing = -actor.facing;
						}
						if (random.NextDouble() < 0.4)
						{
							pending.Add(BotTasks.Schedule(() =>
							{
								if (!disposed)
								{
									actor.chair = BotFurniture.RandomChair(random);
								}
							}, 500L, "Spawn", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/MarketShopService.cs"));
						}
					}
				}, 1200L, "Spawn", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/MarketShopService.cs"));
			}
			else
			{
				Open(ownerId++, BotGeneration.GetRandomShopOwnerIGN(), mapId, botPoint.x, botPoint.y, permits[random.Next(permits.Length)], playerStore: false);
			}
		}
		catch
		{
			counts[mapId]--;
			throw;
		}
	}

	private void Open(int owner, string name, int mapId, int x, int y, int permit, bool playerStore)
	{
		BotClient botClient = BotClient.GetBotClient();
		MarketShop marketShop = new MarketShop
		{
			ownerId = owner,
			ownerName = name,
			mapId = mapId,
			x = x,
			y = y,
			world = botClient.world,
			channel = botClient.channel,
			permitId = permit,
			playerStore = playerStore
		};
		Generator.Generate(marketShop);
		marketShop.open = true;
		shops.Add(owner, marketShop);
	}

	public MarketShop Visit(BotActor actor, int owner)
	{
		if (actor == null)
		{
			return null;
		}
		Leave(actor.id);
		MarketShop marketShop = Get(owner);
		if (marketShop == null || !Matches(actor, marketShop) || actor.trade != null || actor.id == owner)
		{
			return null;
		}
		int num = 0;
		foreach (int value in visits.Values)
		{
			if (value == owner)
			{
				num++;
			}
		}
		if (num >= 3)
		{
			return null;
		}
		int i;
		for (i = 0; i < 3; i++)
		{
			bool flag = false;
			foreach (KeyValuePair<int, int> visit in visits)
			{
				if (visit.Value == owner && visitorSlots[visit.Key] == i)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				break;
			}
		}
		visits[actor.id] = owner;
		visitorSlots[actor.id] = i;
		return marketShop;
	}

	private static bool Matches(BotActor actor, MarketShop shop)
	{
		if (actor != null && actor.online && shop.open && actor.world == shop.world && actor.channel == shop.channel)
		{
			return actor.mapId == shop.mapId;
		}
		return false;
	}

	public MarketShop Visiting(BotActor actor)
	{
		if (actor == null || !visits.TryGetValue(actor.id, out var value))
		{
			return null;
		}
		MarketShop marketShop = Get(value);
		if (marketShop != null && Matches(actor, marketShop))
		{
			return marketShop;
		}
		Leave(actor.id);
		return null;
	}

	public void Leave(int actor)
	{
		visits.Remove(actor);
		visitorSlots.Remove(actor);
	}

	public List<KeyValuePair<int, int>> Visitors(int owner)
	{
		List<KeyValuePair<int, int>> list = new List<KeyValuePair<int, int>>();
		foreach (KeyValuePair<int, int> visit in visits)
		{
			if (visit.Value == owner)
			{
				list.Add(new KeyValuePair<int, int>(visit.Key, visitorSlots[visit.Key]));
			}
		}
		return list;
	}

	public bool Chat(BotActor actor, string text)
	{
		MarketShop marketShop = Visiting(actor);
		if (marketShop == null || string.IsNullOrWhiteSpace(text) || text.Length > 127)
		{
			return false;
		}
		marketShop.chat.Add(actor.name + " : " + text);
		return true;
	}

	public MarketPurchaseResult Buy(BotActor actor, int owner, int index, int quantity)
	{
		MarketShop marketShop = Get(owner);
		if (marketShop == null || !Matches(actor, marketShop) || !visits.TryGetValue(actor.id, out var value) || value != owner || index < 0 || index >= marketShop.items.Count || actor.trade != null)
		{
			return MarketPurchaseResult.Unavailable;
		}
		MarketShopStock marketShopStock = marketShop.items[index];
		if (quantity < 1 || quantity > 32767 || !marketShopStock.exists || quantity > marketShopStock.bundles)
		{
			return MarketPurchaseResult.InvalidQuantity;
		}
		InventoryEntry inventoryEntry = marketShopStock.item.Copy();
		inventoryEntry.quantity = (short)(inventoryEntry.quantity * quantity);
		if (inventoryEntry.quantity <= 0 || (inventoryEntry.isEquip && inventoryEntry.quantity > 1) || marketShopStock.price < 0)
		{
			return MarketPurchaseResult.InvalidQuantity;
		}
		int num = MarketEconomy.JavaInt(Math.Min((float)marketShopStock.price * (float)quantity, 2.1474836E+09f));
		if (((actor is BotCharacter botCharacter) ? botCharacter.meso : (actor.character?.record.meso ?? 0)) < num)
		{
			return MarketPurchaseResult.InsufficientMeso;
		}
		bool flag = marketShop.playerStore && marketShop.playerSell != null;
		BotCharacter botCharacter2 = ((marketShop.playerStore && !flag) ? BotRegistry.Get(marketShop.ownerId) : null);
		if (marketShop.playerStore && !flag && (botCharacter2 == null || botCharacter2.meso + num < botCharacter2.meso))
		{
			return MarketPurchaseResult.OwnerMesoFull;
		}
		if (flag && !CanBuyPlayerStore(marketShop, marketShopStock, quantity))
		{
			return MarketPurchaseResult.Unavailable;
		}
		TradeItemRules.ApplyKarma(inventoryEntry);
		if (!trades.Purchase(actor, inventoryEntry, num))
		{
			return MarketPurchaseResult.InventoryFull;
		}
		long num2 = num - BotTradeService.Fee(num);
		if (flag)
		{
			long num3 = marketShop.playerSell(marketShopStock.merchantSlot, quantity);
			if (num3 <= 0)
			{
				return MarketPurchaseResult.Unavailable;
			}
			num2 = num3;
		}
		else if (marketShop.playerStore)
		{
			botCharacter2.meso += num2;
		}
		else
		{
			marketShop.merchantMeso += num2;
		}
		marketShop.sold.Add(new MarketSoldItem
		{
			buyer = actor.name,
			itemId = inventoryEntry.itemId,
			quantity = (marketShop.playerStore ? quantity : inventoryEntry.quantity),
			meso = num2
		});
		marketShopStock.bundles -= quantity;
		if (marketShopStock.bundles < 1)
		{
			marketShopStock.exists = false;
			if (marketShop.playerStore && ++marketShop.purchasedStocks == marketShop.items.Count)
			{
				marketShop.open = false;
				List<int> list = new List<int>();
				foreach (KeyValuePair<int, int> visit in visits)
				{
					if (visit.Value == owner)
					{
						list.Add(visit.Key);
					}
				}
				foreach (int item in list)
				{
					Leave(item);
				}
			}
		}
		return MarketPurchaseResult.Success;
	}

	private static bool CanBuyPlayerStore(MarketShop shop, MarketShopStock stock, int quantity)
	{
		if (shop.open && stock.merchantSlot >= 0 && stock.exists && quantity > 0)
		{
			return quantity <= stock.bundles;
		}
		return false;
	}

	public void UpsertPlayerStore(MarketShop shop)
	{
		if (shop != null && !disposed)
		{
			shops[shop.ownerId] = shop;
		}
	}

	public void RemovePlayerStore(int ownerId)
	{
		shops.Remove(ownerId);
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, int> visit in visits)
		{
			if (visit.Value == ownerId)
			{
				list.Add(visit.Key);
			}
		}
		foreach (int item in list)
		{
			Leave(item);
		}
	}

	public void Dispose()
	{
		disposed = true;
		foreach (BotTasks.Handle item in pending)
		{
			item.Cancel();
		}
		pending.Clear();
		foreach (MarketShop value in shops.Values)
		{
			value.open = false;
		}
		shops.Clear();
		counts.Clear();
		visits.Clear();
		visitorSlots.Clear();
	}
}
