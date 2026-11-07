using System;
using System.Collections.Generic;
using Server.Bot;
using Server.Bot.Trade;

public static class NativeMerchantService
{
	public const int MaxSlots = 16;

	public const int MaxTotalPerSlot = 2000;

	private static readonly int[] DailyReminders = new int[8] { 2, 5, 10, 15, 30, 60, 90, 2147483647 };

	private const int StorageCharacterId = -2;

	public const byte ErrorNotFreeMarket = 15;

	public const byte ErrorNoPermit = 6;

	public const byte ErrorAlreadyOpen = 13;

	public const byte ErrorClosed = 18;

	public const byte FredrickNoSpace = 32;

	public const byte FredrickNotEnoughMeso = 33;

	public const byte FredrickUnique = 34;

	public const byte FredrickSuccess = 30;

	public const byte FredrickMesoFull = 31;

	public static Func<MarketShopService> MarketProvider;

	private static bool FreeMarket(int mapId)
	{
		if (mapId / 1000000 == 910)
		{
			return mapId > 910000000;
		}
		return false;
	}

	public static bool IsHiredMerchantPermit(int itemId)
	{
		return itemId / 10000 == 503;
	}

	public static MerchantResponse Handle(DbStore db, MerchantRequest r)
	{
		CharRecord charRecord = CharactersDao.FindById(db, r.characterId);
		if (charRecord == null)
		{
			return new MerchantResponse
			{
				ok = false,
				error = "角色不存在"
			};
		}
		MerchantResponse result = Dispatch(db, charRecord, r);
		switch (r.operation)
		{
		case MerchantOperation.Create:
		case MerchantOperation.AddItem:
		case MerchantOperation.TakeItemBack:
		case MerchantOperation.RemoveItem:
		case MerchantOperation.Organize:
		case MerchantOperation.Close:
		case MerchantOperation.MaintenanceOff:
		case MerchantOperation.SetOpen:
		case MerchantOperation.Retrieve:
		case MerchantOperation.Tick:
			SyncMarket(db, (int)charRecord.id);
			break;
		}
		return result;
	}

	private static MerchantResponse Dispatch(DbStore db, CharRecord c, MerchantRequest r)
	{
		switch (r.operation)
		{
		case MerchantOperation.Box:
			return Box(db, c);
		case MerchantOperation.Create:
			return Create(db, c, r);
		case MerchantOperation.AddItem:
			return AddItem(db, c, r);
		case MerchantOperation.TakeItemBack:
		case MerchantOperation.RemoveItem:
			return TakeItemBack(db, c, r);
		case MerchantOperation.Buy:
			return new MerchantResponse
			{
				ok = false,
				error = "单机没有其他真人顾客，店内商品由假人购买"
			};
		case MerchantOperation.WithdrawMesos:
			return WithdrawMesos(db, c);
		case MerchantOperation.Organize:
			return Organize(db, c);
		case MerchantOperation.Close:
			return Close(db, c, expire: false);
		case MerchantOperation.MaintenanceOff:
			return Maintenance(db, c);
		case MerchantOperation.SetOpen:
			return SetOpen(db, c, r.quantity != 0);
		case MerchantOperation.AddBlacklist:
			return Blacklist(db, c, r.blacklistName, add: true);
		case MerchantOperation.RemoveBlacklist:
			return Blacklist(db, c, r.blacklistName, add: false);
		case MerchantOperation.Retrieve:
			return Retrieve(db, c);
		case MerchantOperation.Tick:
			return Tick(db, c);
		default:
			return Snapshot(db, c);
		}
	}

	private static void SyncMarket(DbStore db, int ownerId)
	{
		MarketShopService marketShopService = MarketProvider?.Invoke();
		if (marketShopService == null)
		{
			return;
		}
		MerchantSnapshot merchantSnapshot = Load(db, ownerId);
		if (merchantSnapshot == null || !merchantSnapshot.open)
		{
			marketShopService.RemovePlayerStore(ownerId);
			return;
		}
		BotClient botClient = BotClient.GetBotClient();
		MarketShop marketShop = new MarketShop
		{
			ownerId = ownerId,
			ownerName = merchantSnapshot.ownerName,
			description = merchantSnapshot.description,
			mapId = merchantSnapshot.mapId,
			world = botClient.world,
			channel = botClient.channel,
			permitId = merchantSnapshot.permitId,
			open = true,
			playerStore = true
		};
		foreach (MerchantSlotData item in merchantSnapshot.items)
		{
			if (item.item != null && item.bundles >= 1)
			{
				marketShop.items.Add(new MarketShopStock
				{
					item = item.item.Copy(),
					bundles = item.bundles,
					price = item.price,
					exists = true,
					merchantSlot = item.slot
				});
			}
		}
		if (marketShop.items.Count == 0)
		{
			marketShopService.RemovePlayerStore(ownerId);
			return;
		}
		marketShop.playerSell = (int merchantSlot, int quantity) => (!BotPurchase(db, ownerId, merchantSlot, quantity, "假人", out var price, out var _)) ? 0 : (price - BotTradeService.Fee(price));
		marketShopService.UpsertPlayerStore(marketShop);
	}

	private static MerchantResponse Box(DbStore db, CharRecord c)
	{
		if (!FreeMarket(c.map))
		{
			return new MerchantResponse
			{
				ok = false,
				code = 15,
				error = "不能在这里开雇佣商店"
			};
		}
		if (HasMerchantFlag(db, c))
		{
			return new MerchantResponse
			{
				ok = false,
				error = "你已经有店铺了"
			};
		}
		if (StoredItemRows(db, (int)c.id).Count <= 0 && DbStore.Long(CharactersRow(db, c), "MerchantMesos", 0L) == 0L)
		{
			return new MerchantResponse
			{
				ok = true,
				box = true
			};
		}
		return new MerchantResponse
		{
			ok = true,
			box = false,
			error = "店铺寄存还有东西，请到自由市场入口找弗利德里取回"
		};
	}

	private static MerchantResponse Create(DbStore db, CharRecord c, MerchantRequest r)
	{
		if (!FreeMarket(c.map))
		{
			return new MerchantResponse
			{
				ok = false,
				code = 15,
				error = "不能在这里开雇佣商店"
			};
		}
		if (HasMerchantFlag(db, c) || ShopRow(db, (int)c.id) != null)
		{
			return new MerchantResponse
			{
				ok = false,
				code = 13,
				error = "你已经有店铺了"
			};
		}
		if (!IsHiredMerchantPermit((r.price > 0) ? r.price : r.inventoryType))
		{
			return new MerchantResponse
			{
				ok = false,
				code = 6,
				error = "需要雇佣商人许可"
			};
		}
		int num = ((r.price > 0) ? r.price : r.inventoryType);
		if (InventoryDao.CountOf(db, (int)c.id, num) < 1)
		{
			return new MerchantResponse
			{
				ok = false,
				code = 6,
				error = "背包里没有这个许可"
			};
		}
		db.Table("hiredmerchants").Insert(new DbRow
		{
			["ownerid"] = c.id,
			["mapid"] = (long)c.map,
			["x"] = 0L,
			["y"] = 0L,
			["permitid"] = (long)num,
			["description"] = r.description ?? "",
			["open"] = 0L,
			["published"] = 0L,
			["starttime"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
		});
		SetMerchantFlag(db, c, value: true);
		db.Save();
		return Snapshot(db, c);
	}

	private static MerchantResponse Snapshot(DbStore db, CharRecord c)
	{
		MerchantSnapshot shop = Load(db, (int)c.id);
		return new MerchantResponse
		{
			shop = shop,
			inventory = CharService.Load(db, (int)c.id).inventory,
			meso = c.meso,
			merchantMeso = MerchantMeso(db, c)
		};
	}

	private static MerchantResponse AddItem(DbStore db, CharRecord c, MerchantRequest r)
	{
		DbRow dbRow = ShopRow(db, (int)c.id);
		if (dbRow == null)
		{
			return new MerchantResponse
			{
				ok = false,
				error = "还没有开店"
			};
		}
		int cid = (int)c.id;
		List<DbRow> list = from x in db.Table("merchantitems")
			select DbStore.Int(x, "ownerid") == cid;
		if (DbStore.Int(dbRow, "open") == 1)
		{
			return new MerchantResponse
			{
				ok = false,
				error = "营业中不能上架道具"
			};
		}
		if (DbStore.Int(dbRow, "published") == 1 && r.inventoryType == 5)
		{
			return new MerchantResponse
			{
				ok = false,
				error = "已经开过张的商店不能再上架现金道具"
			};
		}
		if (list.Count >= 16)
		{
			return new MerchantResponse
			{
				ok = false,
				error = "货架已满（最多 16 格）"
			};
		}
		DbRow dbRow2 = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == cid && DbStore.Int(x, "inventorytype") == r.inventoryType && DbStore.Int(x, "position") == r.itemPosition && InventoryDao.IsBagPosition(DbStore.Int(x, "position")));
		if (dbRow2 == null)
		{
			return new MerchantResponse
			{
				ok = false,
				error = "背包里没有这件道具"
			};
		}
		InventoryEntry inventoryEntry = CharService.ToInventoryEntry(db, dbRow2);
		if (NativePotentialService.BlocksTransfer(db, inventoryEntry))
		{
			return new MerchantResponse
			{
				ok = false,
				error = "潜能结果待选装备或装备痕迹不能上架"
			};
		}
		bool flag = TradeItemRules.Rechargeable(inventoryEntry.itemId);
		int num = ((r.bundles > 0) ? r.quantity : r.quantity);
		int num2 = ((r.bundles <= 0) ? 1 : r.bundles);
		if (flag)
		{
			num = 1;
			num2 = 1;
		}
		if (!flag && inventoryEntry.quantity < num * num2)
		{
			return new MerchantResponse
			{
				ok = false,
				error = "数量不足"
			};
		}
		int price = r.price;
		if (num <= 0 || num * num2 > 2000 || num2 <= 0 || price <= 0)
		{
			return new MerchantResponse
			{
				ok = false,
				error = "上架参数不合法"
			};
		}
		int take = (flag ? inventoryEntry.quantity : (num * num2));
		InventoryEntry inventoryEntry2 = inventoryEntry.Copy();
		inventoryEntry2.quantity = (flag ? inventoryEntry.quantity : num);
		int num3 = NextSlot(list);
		long num4 = WriteEntry(db, -2, inventoryEntry2, 1, inventoryEntry2.quantity);
		db.Table("merchantitems").Insert(new DbRow
		{
			["ownerid"] = c.id,
			["slot"] = (long)num3,
			["inventoryitemid"] = num4,
			["bundles"] = (long)num2,
			["price"] = (long)price
		});
		ConsumeFromInventory(db, dbRow2, inventoryEntry, take);
		db.Save();
		return Snapshot(db, c);
	}

	private static MerchantResponse TakeItemBack(DbStore db, CharRecord c, MerchantRequest r)
	{
		DbRow dbRow = ShopRow(db, (int)c.id);
		if (dbRow == null)
		{
			return new MerchantResponse
			{
				ok = false,
				error = "还没有开店"
			};
		}
		if (DbStore.Int(dbRow, "open") == 1)
		{
			return new MerchantResponse
			{
				ok = false,
				error = "营业中不能取回道具"
			};
		}
		int cid = (int)c.id;
		DbRow dbRow2 = db.Table("merchantitems").Find((DbRow x) => DbStore.Int(x, "ownerid") == cid && DbStore.Int(x, "slot") == r.slot);
		if (dbRow2 == null)
		{
			return new MerchantResponse
			{
				ok = false,
				error = "这个格子是空的"
			};
		}
		InventoryEntry inventoryEntry = LoadSlotItem(db, dbRow2);
		int val = DbStore.Int(dbRow2, "bundles");
		int num = ((inventoryEntry != null) ? (inventoryEntry.quantity * Math.Max(0, val)) : 0);
		if (inventoryEntry != null && num > 0)
		{
			InventoryEntry inventoryEntry2 = inventoryEntry.Copy();
			inventoryEntry2.quantity = num;
			FullCharacter fullCharacter = CharService.Load(db, cid);
			int num2 = TradeItemRules.FreeSlot(Actor(c, fullCharacter), fullCharacter.inventory, inventoryEntry2.inventoryType);
			if (num2 <= 0)
			{
				return new MerchantResponse
				{
					ok = false,
					error = "背包没有空位放回这件道具"
				};
			}
			WriteEntry(db, cid, inventoryEntry2, num2, num);
		}
		DeleteSlot(db, dbRow2);
		db.Save();
		return Snapshot(db, c);
	}

	public static bool BotPurchase(DbStore db, int ownerId, int slot, int quantity, string buyer, out int price, out MerchantSlotData sold)
	{
		price = 0;
		sold = null;
		DbRow dbRow = db.Table("merchantitems").Find((DbRow x) => DbStore.Int(x, "ownerid") == ownerId && DbStore.Int(x, "slot") == slot);
		if (dbRow == null || quantity < 1)
		{
			return false;
		}
		DbRow dbRow2 = ShopRow(db, ownerId);
		if (dbRow2 == null || DbStore.Int(dbRow2, "open") == 0)
		{
			return false;
		}
		int num = DbStore.Int(dbRow, "bundles");
		if (num < quantity)
		{
			return false;
		}
		InventoryEntry inventoryEntry = LoadSlotItem(db, dbRow);
		if (inventoryEntry == null)
		{
			return false;
		}
		price = MarketEconomyClamp(Math.Min((float)DbStore.Int(dbRow, "price") * (float)quantity, 2.1474836E+09f));
		long num2 = price - BotTradeService.Fee(price);
		CreditMerchantMeso(db, ownerId, num2);
		EntryAddSold(db, ownerId, buyer, inventoryEntry.itemId, inventoryEntry.quantity, quantity, num2);
		num -= quantity;
		if (num < 1)
		{
			DeleteSlot(db, dbRow);
		}
		else
		{
			dbRow["bundles"] = (long)num;
		}
		db.Save();
		sold = new MerchantSlotData
		{
			slot = slot,
			price = price,
			bundles = quantity,
			item = inventoryEntry
		};
		return true;
	}

	private static void EntryAddSold(DbStore db, int ownerId, string buyer, int itemId, int perBundle, int quantity, long meso)
	{
		db.Table("merchantsold").Insert(new DbRow
		{
			["ownerid"] = (long)ownerId,
			["buyer"] = buyer ?? "",
			["itemid"] = (long)itemId,
			["quantity"] = (long)(perBundle * quantity),
			["meso"] = meso,
			["time"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
		});
	}

	private static int MarketEconomyClamp(double value)
	{
		if (!double.IsNaN(value))
		{
			if (!(value >= 2147483647.0))
			{
				if (!(value <= -2147483648.0))
				{
					return (int)value;
				}
				return int.MinValue;
			}
			return int.MaxValue;
		}
		return 0;
	}

	private static MerchantResponse WithdrawMesos(DbStore db, CharRecord c)
	{
		int num = (int)c.id;
		long num2 = NetMerchantMeso(db, c);
		long num3 = MerchantMeso(db, c);
		if (num2 > 0)
		{
			long num4 = int.MaxValue - c.meso;
			if (num4 > 0)
			{
				long num5 = Math.Min(num4, num2);
				c.meso += num5;
				CharactersDao.Save(db, c);
				SetMerchantMeso(db, num, num3 - num5);
			}
		}
		else if (num3 < 0)
		{
			if (c.meso + num3 < 0)
			{
				SetMerchantMeso(db, num, num3 + c.meso);
				c.meso = 0L;
			}
			else
			{
				c.meso += num3;
				SetMerchantMeso(db, num, 0L);
			}
			CharactersDao.Save(db, c);
		}
		db.Save();
		return new MerchantResponse
		{
			ok = true,
			meso = c.meso,
			merchantMeso = MerchantMeso(db, c),
			shop = Load(db, num)
		};
	}

	private static MerchantResponse Organize(DbStore db, CharRecord c)
	{
		WithdrawMesos(db, c);
		ClearEmptySlots(db, (int)c.id);
		int cid = (int)c.id;
		if ((from x in db.Table("merchantitems")
			select DbStore.Int(x, "ownerid") == cid).Count == 0)
		{
			return Close(db, c, expire: false);
		}
		db.Save();
		return Snapshot(db, c);
	}

	private static MerchantResponse Close(DbStore db, CharRecord c, bool expire)
	{
		int cid = (int)c.id;
		if (ShopRow(db, cid) == null)
		{
			return new MerchantResponse
			{
				ok = false,
				error = "还没有开店"
			};
		}
		List<DbRow> list = from x in db.Table("merchantitems")
			select DbStore.Int(x, "ownerid") == cid;
		if (!expire)
		{
			FullCharacter fullCharacter = CharService.Load(db, cid);
			BotActor actor = Actor(c, fullCharacter);
			HashSet<int> used = new HashSet<int>();
			List<KeyValuePair<DbRow, InventoryEntry>> list2 = new List<KeyValuePair<DbRow, InventoryEntry>>();
			bool flag = true;
			foreach (DbRow item in list)
			{
				InventoryEntry inventoryEntry = LoadSlotItem(db, item);
				if (inventoryEntry != null)
				{
					InventoryEntry inventoryEntry2 = inventoryEntry.Copy();
					inventoryEntry2.quantity = inventoryEntry.quantity * Math.Max(1, DbStore.Int(item, "bundles"));
					if (Reserve(actor, fullCharacter.inventory, used, inventoryEntry2.inventoryType) <= 0)
					{
						flag = false;
						break;
					}
					list2.Add(new KeyValuePair<DbRow, InventoryEntry>(item, inventoryEntry2));
				}
			}
			if (flag)
			{
				foreach (KeyValuePair<DbRow, InventoryEntry> item2 in list2)
				{
					int num = Reserve(actor, fullCharacter.inventory, used, item2.Value.inventoryType);
					if (num > 0)
					{
						WriteEntry(db, cid, item2.Value, num, item2.Value.quantity);
						DeleteSlot(db, item2.Key);
					}
				}
			}
		}
		if ((from x in db.Table("merchantitems")
			select DbStore.Int(x, "ownerid") == cid).Count > 0)
		{
			TouchFredrickLog(db, cid);
		}
		else
		{
			RemoveFredrickLog(db, cid);
		}
		db.Table("hiredmerchants").Delete((DbRow x) => DbStore.Int(x, "ownerid") == cid);
		SetMerchantFlag(db, c, value: false);
		db.Save();
		return new MerchantResponse
		{
			ok = true,
			inventory = CharService.Load(db, cid).inventory,
			meso = c.meso,
			merchantMeso = MerchantMeso(db, c),
			shop = null
		};
	}

	private static MerchantResponse Maintenance(DbStore db, CharRecord c)
	{
		DbRow dbRow = ShopRow(db, (int)c.id);
		if (dbRow == null)
		{
			return new MerchantResponse
			{
				ok = false,
				error = "还没有开店"
			};
		}
		int cid = (int)c.id;
		if ((from x in db.Table("merchantitems")
			select DbStore.Int(x, "ownerid") == cid).Count == 0)
		{
			return Close(db, c, expire: false);
		}
		dbRow["open"] = 1L;
		dbRow["published"] = 1L;
		db.Save();
		return Snapshot(db, c);
	}

	private static MerchantResponse SetOpen(DbStore db, CharRecord c, bool open)
	{
		DbRow dbRow = ShopRow(db, (int)c.id);
		if (dbRow == null)
		{
			return new MerchantResponse
			{
				ok = false,
				error = "还没有开店"
			};
		}
		dbRow["open"] = (long)(open ? 1 : 0);
		if (open)
		{
			dbRow["published"] = 1L;
		}
		db.Save();
		return Snapshot(db, c);
	}

	private static MerchantResponse Blacklist(DbStore db, CharRecord c, string name, bool add)
	{
		if (string.IsNullOrEmpty(name))
		{
			return new MerchantResponse
			{
				ok = false,
				error = "名字不能为空"
			};
		}
		MerchantSnapshot merchantSnapshot = Load(db, (int)c.id);
		if (merchantSnapshot == null)
		{
			return new MerchantResponse
			{
				ok = false,
				error = "还没有开店"
			};
		}
		if (add)
		{
			if (merchantSnapshot.blacklist.Count < 20 && !merchantSnapshot.blacklist.Contains(name))
			{
				merchantSnapshot.blacklist.Add(name);
			}
		}
		else
		{
			merchantSnapshot.blacklist.Remove(name);
		}
		return new MerchantResponse
		{
			ok = true,
			shop = merchantSnapshot,
			inventory = null,
			meso = -1L
		};
	}

	private static MerchantResponse Retrieve(DbStore db, CharRecord c)
	{
		int cid = (int)c.id;
		List<DbRow> list = from x in db.Table("merchantitems")
			select DbStore.Int(x, "ownerid") == cid;
		FullCharacter fullCharacter = CharService.Load(db, cid);
		BotActor actor = Actor(c, fullCharacter);
		List<InventoryEntry> list2 = new List<InventoryEntry>();
		foreach (DbRow item in list)
		{
			InventoryEntry inventoryEntry = LoadSlotItem(db, item);
			if (inventoryEntry == null)
			{
				continue;
			}
			InventoryEntry inventoryEntry2 = inventoryEntry.Copy();
			inventoryEntry2.quantity = inventoryEntry.quantity * Math.Max(1, DbStore.Int(item, "bundles"));
			List<InventoryEntry> list3 = new List<InventoryEntry>();
			foreach (InventoryEntry item2 in fullCharacter.inventory)
			{
				list3.Add(item2.Copy());
			}
			foreach (InventoryEntry item3 in list2)
			{
				list3.Add(new InventoryEntry
				{
					itemId = item3.itemId,
					inventoryType = item3.inventoryType,
					position = 1,
					quantity = 0
				});
			}
			if (TradeItemRules.FreeSlot(actor, list3, inventoryEntry2.inventoryType) <= 0)
			{
				bool flag = BotItemCatalog.Int(BotItemCatalog.Shared.Info(inventoryEntry2.itemId), "only") == 1;
				return new MerchantResponse
				{
					ok = false,
					code = (byte)(flag ? 34 : 32),
					error = (flag ? "背包里已有同一件唯一道具" : "背包空间不足，无法取回")
				};
			}
			list2.Add(inventoryEntry2);
		}
		long num = NetMerchantMeso(db, c);
		if (num > 0 && c.meso + num > int.MaxValue)
		{
			return new MerchantResponse
			{
				ok = false,
				code = 31,
				error = "金币已达上限"
			};
		}
		if (num < 0 && c.meso < -num)
		{
			return new MerchantResponse
			{
				ok = false,
				code = 33,
				error = "金币不足以支付商店欠款"
			};
		}
		foreach (InventoryEntry item4 in list2)
		{
			int num2 = TradeItemRules.FreeSlot(actor, fullCharacter.inventory, item4.inventoryType);
			if (num2 > 0)
			{
				WriteEntry(db, cid, item4, num2, item4.quantity);
				fullCharacter.inventory.Add(new InventoryEntry
				{
					itemId = item4.itemId,
					inventoryType = item4.inventoryType,
					position = num2,
					quantity = item4.quantity
				});
			}
		}
		foreach (DbRow item5 in list)
		{
			DeleteSlot(db, item5);
		}
		WithdrawMesos(db, c);
		RemoveFredrickLog(db, cid);
		db.Save();
		return new MerchantResponse
		{
			ok = true,
			code = 30,
			inventory = CharService.Load(db, cid).inventory,
			meso = c.meso,
			merchantMeso = MerchantMeso(db, c)
		};
	}

	public static int Expire(DbStore db, long now)
	{
		int num = Math.Max(1, GameConfigDao.GetInt(db, "hired_merchants_exist_time", 1));
		long limit = (long)num * 24L * 60 * 60 * 1000;
		List<DbRow> list = from x in db.Table("hiredmerchants")
			select now - DbStore.Long(x, "starttime", 0L) > limit;
		foreach (DbRow row in list)
		{
			CharRecord charRecord = CharactersDao.FindById(db, DbStore.Int(row, "ownerid"));
			if (charRecord != null)
			{
				DbRow dbRow = ShopRow(db, (int)charRecord.id);
				if (dbRow == null || DbStore.Long(dbRow, "starttime", 0L) <= now - limit)
				{
					Close(db, charRecord, expire: true);
				}
			}
			else
			{
				db.Table("hiredmerchants").Delete((DbRow x) => DbStore.Int(x, "ownerid") == DbStore.Int(row, "ownerid"));
				TouchFredrickLog(db, DbStore.Int(row, "ownerid"));
			}
		}
		if (list.Count > 0)
		{
			db.Save();
		}
		return list.Count;
	}

	private static MerchantResponse Tick(DbStore db, CharRecord c)
	{
		Expire(db, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
		return Snapshot(db, c);
	}

	private static DbRow ShopRow(DbStore db, int ownerId)
	{
		return db.Table("hiredmerchants").Find((DbRow x) => DbStore.Int(x, "ownerid") == ownerId);
	}

	private static DbRow CharactersRow(DbStore db, CharRecord c)
	{
		return db.Table("characters").Find((DbRow x) => DbStore.Int(x, "id") == (int)c.id);
	}

	private static bool HasMerchantFlag(DbStore db, CharRecord c)
	{
		DbRow dbRow = CharactersRow(db, c);
		if (dbRow != null)
		{
			return DbStore.Int(dbRow, "HasMerchant") != 0;
		}
		return false;
	}

	private static void SetMerchantFlag(DbStore db, CharRecord c, bool value)
	{
		DbRow dbRow = CharactersRow(db, c);
		if (dbRow != null)
		{
			dbRow["HasMerchant"] = (long)(value ? 1 : 0);
			c.HasMerchant = (value ? 1 : 0);
		}
	}

	private static long MerchantMeso(DbStore db, CharRecord c)
	{
		DbRow dbRow = CharactersRow(db, c);
		if (dbRow != null)
		{
			return DbStore.Long(dbRow, "MerchantMesos", 0L);
		}
		return 0L;
	}

	private static void SetMerchantMeso(DbStore db, int characterId, long value)
	{
		value = Math.Max(-2147483648L, Math.Min(2147483647L, value));
		db.Table("characters").Update((DbRow x) => DbStore.Int(x, "id") == characterId, new DbRow { ["MerchantMesos"] = value });
		CharRecord charRecord = CharactersDao.FindById(db, characterId);
		if (charRecord != null)
		{
			charRecord.MerchantMesos = (int)value;
		}
	}

	private static void CreditMerchantMeso(DbStore db, int ownerId, long add)
	{
		CharRecord charRecord = CharactersDao.FindById(db, ownerId);
		if (charRecord != null)
		{
			SetMerchantMeso(db, ownerId, MerchantMeso(db, charRecord) + add);
		}
	}

	private static long NetMerchantMeso(DbStore db, CharRecord c)
	{
		int num = 0;
		DbRow dbRow = db.Table("fredstorage").Find((DbRow x) => DbStore.Int(x, "cid") == (int)c.id);
		if (dbRow != null)
		{
			num = (int)((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - DbStore.Long(dbRow, "timestamp", 0L)) / 86400000);
		}
		if (num > 100)
		{
			num = 100;
		}
		if (num < 0)
		{
			num = 0;
		}
		return MerchantMeso(db, c) * (100 - num) / 100;
	}

	private static void TouchFredrickLog(DbStore db, int cid)
	{
		RemoveFredrickLog(db, cid);
		db.Table("fredstorage").Insert(new DbRow
		{
			["cid"] = (long)cid,
			["daynotes"] = 0L,
			["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
		});
	}

	public static void RemoveFredrickLog(DbStore db, int cid)
	{
		db.Table("fredstorage").Delete((DbRow x) => DbStore.Int(x, "cid") == cid);
	}

	public static List<int> FredrickDueReminders(DbStore db, long now)
	{
		List<int> list = new List<int>();
		foreach (DbRow item in from x in db.Table("fredstorage")
			select true)
		{
			int cid = DbStore.Int(item, "cid");
			int num = DbStore.Int(item, "daynotes");
			int num2 = (int)((now - DbStore.Long(item, "timestamp", 0L)) / 86400000);
			if (num2 > 100)
			{
				continue;
			}
			int num3 = ((num >= 0 && num < DailyReminders.Length) ? DailyReminders[num] : int.MaxValue);
			if (num2 >= num3)
			{
				do
				{
					num++;
					num3 = ((num < DailyReminders.Length) ? DailyReminders[num] : int.MaxValue);
				}
				while (num2 >= num3);
				item["daynotes"] = (long)num;
				DbRow dbRow = db.Table("characters").Find((DbRow x) => DbStore.Int(x, "id") == cid);
				if (dbRow != null)
				{
					NativeNoteService.SendNormal(db, FredrickReminderMessage(num - 1), "FREDRICK", DbStore.Str(dbRow, "name"));
				}
				list.Add(cid);
			}
		}
		if (list.Count > 0)
		{
			db.Save();
		}
		return list;
	}

	public static string FredrickReminderMessage(int daynotes)
	{
		if (daynotes < 0 || daynotes >= DailyReminders.Length - 1)
		{
			daynotes = 0;
		}
		int num = DailyReminders[daynotes];
		if (daynotes < 4)
		{
			return "您好，亲爱的顾客! 我是弗兰德里, 雇佣商人工会的主席. 您的店铺已经关闭" + num + " 天了. 请到自由市场入口找我取回存储的物品.";
		}
		return "您好，亲爱的顾客! 我是弗兰德里, 雇佣商人工会的主席. 您的店铺已经关闭 " + num + " 天了. 在我们撤走之前，请考虑索回这些物品";
	}

	private static List<DbRow> StoredItemRows(DbStore db, int ownerId)
	{
		return from x in db.Table("merchantitems")
			select DbStore.Int(x, "ownerid") == ownerId;
	}

	private static int NextSlot(List<DbRow> items)
	{
		HashSet<int> hashSet = new HashSet<int>();
		foreach (DbRow item in items)
		{
			hashSet.Add(DbStore.Int(item, "slot"));
		}
		for (int i = 0; i < 16; i++)
		{
			if (!hashSet.Contains(i))
			{
				return i;
			}
		}
		return -1;
	}

	private static void ClearEmptySlots(DbStore db, int ownerId)
	{
		foreach (DbRow item in from x in db.Table("merchantitems")
			select DbStore.Int(x, "ownerid") == ownerId)
		{
			if (DbStore.Int(item, "bundles") < 1)
			{
				DeleteSlot(db, item);
			}
		}
	}

	private static void DeleteSlot(DbStore db, DbRow entry)
	{
		long payload = DbStore.Long(entry, "inventoryitemid", 0L);
		db.Table("inventoryitems").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == payload);
		db.Table("inventoryequipment").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == payload);
		db.Table("merchantitems").Delete((DbRow x) => DbStore.Long(x, "id", -1L) == DbStore.Long(entry, "id", -2L));
	}

	private static InventoryEntry LoadSlotItem(DbStore db, DbRow entry)
	{
		long payload = DbStore.Long(entry, "inventoryitemid", 0L);
		DbRow dbRow = db.Table("inventoryitems").Find((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == payload);
		if (dbRow != null)
		{
			return CharService.ToInventoryEntry(db, dbRow);
		}
		return null;
	}

	internal static long WriteEntry(DbStore db, int characterId, InventoryEntry entry, int position, int quantity)
	{
		DbRow dbRow = new DbRow();
		if (entry.itemData != null)
		{
			foreach (KeyValuePair<string, object> itemDatum in entry.itemData)
			{
				dbRow[itemDatum.Key] = itemDatum.Value;
			}
		}
		dbRow.Remove("inventoryitemid");
		dbRow["characterid"] = (long)characterId;
		dbRow["type"] = (long)(entry.itemId / 1000000);
		dbRow["itemid"] = (long)entry.itemId;
		dbRow["inventorytype"] = (long)entry.inventoryType;
		dbRow["position"] = (long)position;
		dbRow["quantity"] = (long)quantity;
		dbRow["owner"] = entry.owner ?? "";
		dbRow["flag"] = (long)entry.flag;
		dbRow["expiration"] = entry.expiration;
		if (entry.petId > 0)
		{
			dbRow["petid"] = (long)entry.petId;
		}
		long num = db.Table("inventoryitems").Insert(dbRow);
		if (entry.isEquip)
		{
			DbRow dbRow2 = new DbRow();
			if (entry.equipmentData != null)
			{
				foreach (KeyValuePair<string, object> equipmentDatum in entry.equipmentData)
				{
					dbRow2[equipmentDatum.Key] = equipmentDatum.Value;
				}
			}
			dbRow2.Remove("inventoryequipmentid");
			dbRow2["inventoryitemid"] = num;
			db.Table("inventoryequipment").Insert(dbRow2);
		}
		return num;
	}

	internal static void ConsumeFromInventory(DbStore db, DbRow row, InventoryEntry item, int take)
	{
		if (take >= item.quantity)
		{
			long id = DbStore.Long(row, "inventoryitemid", 0L);
			db.Table("inventoryitems").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == id);
			db.Table("inventoryequipment").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == id);
		}
		else
		{
			row["quantity"] = (long)(item.quantity - take);
		}
	}

	private static BotActor Actor(CharRecord c, FullCharacter full)
	{
		return new BotActor
		{
			id = (int)c.id,
			job = c.job,
			character = full
		};
	}

	private static int Reserve(BotActor actor, List<InventoryEntry> inventory, HashSet<int> used, int type)
	{
		int num = TradeItemRules.SlotLimit(actor, type);
		HashSet<int> hashSet = new HashSet<int>();
		foreach (InventoryEntry item in inventory)
		{
			if (item.inventoryType == type && InventoryDao.IsBagPosition(item.position))
			{
				hashSet.Add(item.position);
			}
		}
		for (int i = 1; i <= num; i++)
		{
			if (!hashSet.Contains(i) && used.Add(i))
			{
				return i;
			}
		}
		return -1;
	}

	public static MerchantSnapshot Load(DbStore db, int ownerId)
	{
		DbRow dbRow = ShopRow(db, ownerId);
		if (dbRow == null)
		{
			return null;
		}
		CharRecord charRecord = CharactersDao.FindById(db, ownerId);
		MerchantSnapshot merchantSnapshot = new MerchantSnapshot
		{
			ownerId = ownerId,
			mapId = DbStore.Int(dbRow, "mapid"),
			permitId = DbStore.Int(dbRow, "permitid"),
			ownerName = (charRecord?.name ?? ""),
			description = Str(dbRow, "description"),
			open = (DbStore.Int(dbRow, "open") != 0),
			published = (DbStore.Int(dbRow, "published") != 0),
			merchantMeso = ((charRecord == null) ? 0 : MerchantMeso(db, charRecord)),
			startedAt = DbStore.Long(dbRow, "starttime", 0L)
		};
		foreach (DbRow item2 in StoredItemRows(db, ownerId))
		{
			InventoryEntry item = LoadSlotItem(db, item2);
			merchantSnapshot.items.Add(new MerchantSlotData
			{
				slot = DbStore.Int(item2, "slot"),
				bundles = DbStore.Int(item2, "bundles"),
				price = DbStore.Int(item2, "price"),
				item = item
			});
		}
		merchantSnapshot.items.Sort((MerchantSlotData a, MerchantSlotData b) => a.slot.CompareTo(b.slot));
		return merchantSnapshot;
	}

	private static string Str(DbRow row, string column)
	{
		if (!row.TryGetValue(column, out var value))
		{
			return "";
		}
		return (value as string) ?? "";
	}
}
