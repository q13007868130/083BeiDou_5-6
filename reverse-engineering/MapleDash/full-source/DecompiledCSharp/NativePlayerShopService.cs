using System;
using System.Collections.Generic;
using Server.Bot;
using Server.Bot.Trade;
using Server.Field;

public static class NativePlayerShopService
{
	private sealed class Slot
	{
		public InventoryEntry item;

		public int bundles;

		public int price;

		public bool exists = true;
	}

	private sealed class Shop
	{
		public int ownerId;

		public int mapId;

		public int permitId;

		public string ownerName = "";

		public string description = "";

		public bool open;

		public int boughtNumber;

		public readonly List<Slot> items = new List<Slot>();

		public readonly List<PlayerShopSoldData> sold = new List<PlayerShopSoldData>();

		public readonly List<string> banned = new List<string>();

		public readonly List<string> chat = new List<string>();
	}

	public const int MaxSlots = 16;

	public const int MaxTotalPerSlot = 2000;

	public const int MaxVisitors = 3;

	public const int MaxChatLog = 25;

	public const byte ErrorNotAlive = 4;

	public const byte ErrorInEvent = 5;

	public const byte ErrorNoPermit = 6;

	public const byte ErrorNearPortal = 10;

	public const byte ErrorBlocked = 13;

	public const byte ErrorNotFreeMarket = 15;

	private const int FlagUntradeable = 8;

	private const int FlagKarmaUse = 2;

	private const int FlagKarmaEqp = 16;

	private static readonly Dictionary<int, Shop> shops = new Dictionary<int, Shop>();

	public static Func<MarketShopService> MarketProvider;

	public static bool IsPlayerShopPermit(int itemId)
	{
		return itemId / 10000 == 514;
	}

	private static bool FreeMarket(int mapId)
	{
		if (mapId / 1000000 == 910)
		{
			return mapId > 910000000;
		}
		return false;
	}

	public static void Reset()
	{
		shops.Clear();
	}

	public static bool Has(int characterId)
	{
		return shops.ContainsKey(characterId);
	}

	public static PlayerShopResponse Handle(DbStore db, PlayerShopRequest r)
	{
		CharRecord charRecord = CharactersDao.FindById(db, r.characterId);
		if (charRecord == null)
		{
			return new PlayerShopResponse
			{
				ok = false,
				error = "角色不存在"
			};
		}
		if (shops.TryGetValue((int)charRecord.id, out var value) && value.mapId != charRecord.map)
		{
			if (ReturnItems(db, charRecord, value) == 0)
			{
				shops.Remove((int)charRecord.id);
			}
			else
			{
				value.open = false;
				value.mapId = charRecord.map;
			}
			db.Save();
		}
		PlayerShopResponse result = Dispatch(db, charRecord, r);
		PlayerShopOperation operation = r.operation;
		if ((uint)(operation - 1) <= 5u)
		{
			SyncMarket(db, (int)charRecord.id);
		}
		return result;
	}

	private static PlayerShopResponse Dispatch(DbStore db, CharRecord c, PlayerShopRequest r)
	{
		return r.operation switch
		{
			PlayerShopOperation.Create => Create(db, c, r), 
			PlayerShopOperation.AddItem => AddItem(db, c, r), 
			PlayerShopOperation.TakeItemBack => TakeItemBack(db, c, r), 
			PlayerShopOperation.Open => Open(db, c), 
			PlayerShopOperation.Close => CloseRequest(db, c), 
			PlayerShopOperation.Ban => Ban(db, c, r.name), 
			PlayerShopOperation.Chat => Chat(db, c, r.text), 
			_ => Snapshot(db, c), 
		};
	}

	private static PlayerShopResponse Create(DbStore db, CharRecord c, PlayerShopRequest r)
	{
		if (!FreeMarket(c.map))
		{
			return Fail(15, "只能在自由市场里开个人商店");
		}
		if (shops.ContainsKey((int)c.id))
		{
			return Fail(13, "你已经有店铺了");
		}
		if (!IsPlayerShopPermit(r.permitId))
		{
			return Fail(6, "需要营业执照才能开店");
		}
		if (InventoryDao.CountOf(db, (int)c.id, r.permitId) < 1)
		{
			return Fail(6, "背包里没有这个营业执照");
		}
		byte b = CanPlaceStore(db, c, r.x, r.y);
		if (b != 0)
		{
			return Fail(b, (b == 10) ? "离传送门太近，换个地方摆摊" : "这里已经有店铺了");
		}
		Shop shop = new Shop
		{
			ownerId = (int)c.id,
			mapId = c.map,
			permitId = r.permitId,
			ownerName = c.name,
			description = Trim(r.description, 40)
		};
		shops[shop.ownerId] = shop;
		return Snapshot(db, c);
	}

	private static byte CanPlaceStore(DbStore db, CharRecord c, int x, int y)
	{
		foreach (Shop value in shops.Values)
		{
			if (value.ownerId != (int)c.id && value.mapId == c.map)
			{
				return 13;
			}
		}
		foreach (DbRow row in db.Table("hiredmerchants").rows)
		{
			if (DbStore.Int(row, "mapid") == c.map)
			{
				return 13;
			}
		}
		MapRuntime mapRuntime = MapService.Get(c.map);
		if (mapRuntime == null)
		{
			return 0;
		}
		double num = double.MaxValue;
		int num2 = 0;
		int num3 = 0;
		foreach (MapPortal portal in mapRuntime.portals)
		{
			if (portal.type == 1 && portal.targetMap > 0)
			{
				double num4 = portal.x - x;
				double num5 = portal.y - y;
				double num6 = num4 * num4 + num5 * num5;
				if (num6 < num)
				{
					num = num6;
					num2 = portal.x;
					num3 = portal.y;
				}
			}
		}
		if (num == double.MaxValue)
		{
			return 0;
		}
		double num7 = num2 - x;
		double num8 = num3 - y;
		if (!(Math.Sqrt(num7 * num7 + num8 * num8) < 120.0))
		{
			return 0;
		}
		return 10;
	}

	private static PlayerShopResponse Open(DbStore db, CharRecord c)
	{
		Shop shop = ShopOf(c);
		if (shop == null)
		{
			return Fail(13, "还没有开店");
		}
		if (GameConfigDao.GetBool(db, "use_erase_permit_on_open_shop", fallback: true))
		{
			InventoryDao.ConsumeItem(db, (int)c.id, shop.permitId, 1);
		}
		shop.open = true;
		db.Save();
		return Snapshot(db, c);
	}

	private static PlayerShopResponse AddItem(DbStore db, CharRecord c, PlayerShopRequest r)
	{
		Shop shop = ShopOf(c);
		if (shop == null)
		{
			return Fail(13, "还没有开店");
		}
		if (shop.open)
		{
			return Fail(0, "营业中不能上架道具");
		}
		if (shop.items.Count >= 16)
		{
			return Fail(0, "货架已满（最多 16 格）");
		}
		int cid = (int)c.id;
		DbRow dbRow = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == cid && DbStore.Int(x, "inventorytype") == r.inventoryType && DbStore.Int(x, "position") == r.itemPosition && InventoryDao.IsBagPosition(DbStore.Int(x, "position")));
		if (dbRow == null)
		{
			return Fail(0, "背包里没有这件道具");
		}
		InventoryEntry inventoryEntry = CharService.ToInventoryEntry(db, dbRow);
		if (Untradeable(db, inventoryEntry))
		{
			return Fail(0, "这件道具无法交易");
		}
		if (Unmerchable(db, inventoryEntry.itemId))
		{
			return Fail(0, (inventoryEntry.itemId / 1000 == 5000) ? "宠物不能摆摊" : "这件现金道具不能摆摊");
		}
		bool flag = TradeItemRules.Rechargeable(inventoryEntry.itemId);
		int num = r.perBundle;
		int num2 = r.bundles;
		if (flag)
		{
			num = 1;
			num2 = 1;
		}
		else if (inventoryEntry.quantity < num * num2)
		{
			return Fail(0, "数量不足");
		}
		if (num <= 0 || num * num2 > 2000 || num2 <= 0 || r.price <= 0)
		{
			return Fail(0, "上架参数不合法（每格最多 2000 个）");
		}
		InventoryEntry inventoryEntry2 = inventoryEntry.Copy();
		inventoryEntry2.quantity = (flag ? inventoryEntry.quantity : num);
		shop.items.Add(new Slot
		{
			item = inventoryEntry2,
			bundles = num2,
			price = r.price
		});
		int take = (flag ? inventoryEntry.quantity : (num * num2));
		NativeMerchantService.ConsumeFromInventory(db, dbRow, inventoryEntry, take);
		db.Save();
		return Snapshot(db, c);
	}

	private static PlayerShopResponse TakeItemBack(DbStore db, CharRecord c, PlayerShopRequest r)
	{
		Shop shop = ShopOf(c);
		if (shop == null)
		{
			return Fail(13, "还没有开店");
		}
		if (shop.open)
		{
			return Fail(0, "营业中不能取回道具");
		}
		if (r.slot < 0 || r.slot >= shop.items.Count)
		{
			return Fail(0, "这个格子是空的");
		}
		Slot slot = shop.items[r.slot];
		if (slot.exists && slot.bundles > 0 && slot.item != null)
		{
			InventoryEntry inventoryEntry = slot.item.Copy();
			inventoryEntry.quantity = slot.item.quantity * slot.bundles;
			if (!Give(db, c, inventoryEntry))
			{
				return Fail(0, "背包没有空位放回这件道具");
			}
		}
		shop.items.RemoveAt(r.slot);
		db.Save();
		return Snapshot(db, c);
	}

	private static PlayerShopResponse CloseRequest(DbStore db, CharRecord c)
	{
		Shop shop = ShopOf(c);
		if (shop == null)
		{
			return Fail(13, "还没有开店");
		}
		int num = ReturnItems(db, c, shop);
		if (num > 0)
		{
			return Fail(0, "背包没有空位，还有 " + num + " 件留在店里，清出空间后再关店");
		}
		shops.Remove(shop.ownerId);
		db.Save();
		return new PlayerShopResponse
		{
			ok = true,
			inventory = CharService.Load(db, (int)c.id).inventory,
			meso = c.meso,
			shop = null
		};
	}

	private static int ReturnItems(DbStore db, CharRecord c, Shop shop)
	{
		int num = 0;
		for (int num2 = shop.items.Count - 1; num2 >= 0; num2--)
		{
			Slot slot = shop.items[num2];
			if (slot.bundles < 2 && !slot.exists)
			{
				shop.items.RemoveAt(num2);
			}
			else
			{
				InventoryEntry inventoryEntry = slot.item.Copy();
				inventoryEntry.quantity = ((slot.bundles >= 2) ? (slot.item.quantity * slot.bundles) : slot.item.quantity);
				if (!Give(db, c, inventoryEntry))
				{
					num++;
				}
				else
				{
					shop.items.RemoveAt(num2);
				}
			}
		}
		return num;
	}

	public static void Logout(DbStore db, int characterId)
	{
		if (!shops.TryGetValue(characterId, out var value))
		{
			return;
		}
		CharRecord charRecord = CharactersDao.FindById(db, characterId);
		if (charRecord == null)
		{
			shops.Remove(characterId);
			return;
		}
		if (ReturnItems(db, charRecord, value) == 0)
		{
			shops.Remove(characterId);
		}
		db.Save();
	}

	private static PlayerShopResponse Ban(DbStore db, CharRecord c, string name)
	{
		Shop shop = ShopOf(c);
		if (shop == null)
		{
			return Fail(13, "还没有开店");
		}
		name = (name ?? "").Trim();
		if (name.Length == 0)
		{
			return Fail(0, "名字不能为空");
		}
		if (!shop.banned.Contains(name))
		{
			shop.banned.Add(name);
		}
		shop.chat.Add(shop.ownerName + " 把 " + name + " 请出了店铺");
		TrimChat(shop);
		return Snapshot(db, c);
	}

	private static PlayerShopResponse Chat(DbStore db, CharRecord c, string text)
	{
		Shop shop = ShopOf(c);
		if (shop == null)
		{
			return Fail(13, "还没有开店");
		}
		text = (text ?? "").Trim();
		if (text.Length == 0)
		{
			return Fail(0, "内容不能为空");
		}
		shop.chat.Add(shop.ownerName + " : " + Trim(text, 80));
		TrimChat(shop);
		return Snapshot(db, c);
	}

	private static void TrimChat(Shop shop)
	{
		while (shop.chat.Count > 25)
		{
			shop.chat.RemoveAt(0);
		}
	}

	public static long BotBuy(DbStore db, int ownerId, int slot, int quantity, string buyer)
	{
		if (!shops.TryGetValue(ownerId, out var value) || !value.open)
		{
			return 0L;
		}
		if (slot < 0 || slot >= value.items.Count)
		{
			return 0L;
		}
		Slot slot2 = value.items[slot];
		if (quantity < 1 || !slot2.exists || slot2.bundles < quantity)
		{
			return 0L;
		}
		CharRecord charRecord = CharactersDao.FindById(db, ownerId);
		if (charRecord == null)
		{
			return 0L;
		}
		int num = (int)Math.Min((float)slot2.price * (float)quantity, 2.1474836E+09f);
		if (charRecord.meso + num > int.MaxValue)
		{
			return 0L;
		}
		long num2 = num - BotTradeService.Fee(num);
		charRecord.meso += num2;
		CharactersDao.Save(db, charRecord);
		value.sold.Add(new PlayerShopSoldData
		{
			buyer = (buyer ?? ""),
			itemId = slot2.item.itemId,
			quantity = slot2.item.quantity * quantity,
			meso = num2
		});
		slot2.bundles -= quantity;
		if (slot2.bundles < 1)
		{
			slot2.exists = false;
			if (++value.boughtNumber >= value.items.Count)
			{
				value.open = false;
				shops.Remove(ownerId);
			}
		}
		db.Save();
		return num2;
	}

	private static PlayerShopResponse Snapshot(DbStore db, CharRecord c)
	{
		Shop shop = ShopOf(c);
		PlayerShopResponse playerShopResponse = new PlayerShopResponse
		{
			inventory = CharService.Load(db, (int)c.id).inventory,
			meso = c.meso
		};
		if (shop == null)
		{
			return playerShopResponse;
		}
		PlayerShopInfo playerShopInfo = new PlayerShopInfo
		{
			ownerId = shop.ownerId,
			mapId = shop.mapId,
			permitId = shop.permitId,
			ownerName = shop.ownerName,
			description = shop.description,
			open = shop.open,
			boughtNumber = shop.boughtNumber
		};
		for (int i = 0; i < shop.items.Count; i++)
		{
			Slot slot = shop.items[i];
			playerShopInfo.items.Add(new PlayerShopSlotData
			{
				slot = i,
				bundles = slot.bundles,
				price = slot.price,
				exists = slot.exists,
				item = ((slot.item == null) ? null : slot.item.Copy())
			});
		}
		foreach (PlayerShopSoldData item in shop.sold)
		{
			playerShopInfo.sold.Add(item);
		}
		foreach (string item2 in shop.banned)
		{
			playerShopInfo.banned.Add(item2);
		}
		foreach (string item3 in shop.chat)
		{
			playerShopInfo.chat.Add(item3);
		}
		playerShopResponse.shop = playerShopInfo;
		return playerShopResponse;
	}

	private static void SyncMarket(DbStore db, int ownerId)
	{
		MarketShopService marketShopService = MarketProvider?.Invoke();
		if (marketShopService == null)
		{
			return;
		}
		if (!shops.TryGetValue(ownerId, out var value) || !value.open || value.items.Count == 0)
		{
			marketShopService.RemovePlayerStore(ownerId);
			return;
		}
		BotClient botClient = BotClient.GetBotClient();
		MarketShop marketShop = new MarketShop
		{
			ownerId = ownerId,
			ownerName = value.ownerName,
			description = value.description,
			mapId = value.mapId,
			world = botClient.world,
			channel = botClient.channel,
			permitId = value.permitId,
			open = true,
			playerStore = true
		};
		for (int i = 0; i < value.items.Count; i++)
		{
			Slot slot = value.items[i];
			if (slot.item != null && slot.bundles >= 1 && slot.exists)
			{
				marketShop.items.Add(new MarketShopStock
				{
					item = slot.item.Copy(),
					bundles = slot.bundles,
					price = slot.price,
					exists = true,
					merchantSlot = i
				});
			}
		}
		if (marketShop.items.Count == 0)
		{
			marketShopService.RemovePlayerStore(ownerId);
			return;
		}
		marketShop.playerSell = (int slot2, int quantity) => BotBuy(db, ownerId, slot2, quantity, "假人");
		marketShopService.UpsertPlayerStore(marketShop);
	}

	private static Shop ShopOf(CharRecord c)
	{
		if (!shops.TryGetValue((int)c.id, out var value))
		{
			return null;
		}
		return value;
	}

	private static PlayerShopResponse Fail(byte code, string error)
	{
		return new PlayerShopResponse
		{
			ok = false,
			code = code,
			error = error
		};
	}

	private static string Trim(string value, int max)
	{
		value = (value ?? "").Trim();
		if (value.Length > max)
		{
			return value.Substring(0, max);
		}
		return value;
	}

	private static bool Untradeable(DbStore db, InventoryEntry item)
	{
		if (NativePotentialService.BlocksTransfer(db, item))
		{
			return true;
		}
		int num = (item.isEquip ? 16 : 2);
		if ((item.flag & 8) == 8)
		{
			return true;
		}
		Dictionary<string, object> info = BotItemCatalog.Shared.Info(item.itemId);
		if (BotItemCatalog.Int(info, "tradeBlock") == 1 || BotItemCatalog.Int(info, "accountSharable") == 1 || BotItemCatalog.Int(info, "quest") == 1)
		{
			return (item.flag & num) != num;
		}
		return false;
	}

	private static bool Unmerchable(DbStore db, int itemId)
	{
		bool flag = BotItemCatalog.Int(BotItemCatalog.Shared.Info(itemId), "cash") == 1;
		bool flag2 = itemId / 1000 == 5000;
		if (!(GameConfigDao.GetBool(db, "use_enforce_unmerchable_cash") & flag))
		{
			return GameConfigDao.GetBool(db, "use_enforce_unmerchable_pet") & flag2;
		}
		return true;
	}

	private static bool Give(DbStore db, CharRecord c, InventoryEntry item)
	{
		if (item == null || item.quantity <= 0)
		{
			return true;
		}
		FullCharacter fullCharacter = CharService.Load(db, (int)c.id);
		int num = TradeItemRules.FreeSlot(new BotActor
		{
			id = (int)c.id,
			job = c.job,
			character = fullCharacter
		}, fullCharacter.inventory, item.inventoryType);
		if (num <= 0)
		{
			return false;
		}
		NativeMerchantService.WriteEntry(db, (int)c.id, item, num, item.quantity);
		fullCharacter.inventory.Add(new InventoryEntry
		{
			itemId = item.itemId,
			inventoryType = item.inventoryType,
			position = num,
			quantity = item.quantity
		});
		return true;
	}
}
