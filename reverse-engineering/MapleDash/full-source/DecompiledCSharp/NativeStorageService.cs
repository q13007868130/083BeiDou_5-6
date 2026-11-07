using System;
using System.Collections.Generic;
using Server.Bot;
using Server.Bot.Trade;

public static class NativeStorageService
{
	public const int MaxSlots = 48;

	public const int InitialSlots = 4;

	public const int MinLevel = 15;

	public const byte ErrorInvFull = 10;

	public const byte ErrorNoMeso = 11;

	public const byte ErrorUnique = 12;

	public const byte ErrorStorageFull = 17;

	private const int StorageMarker = -3;

	public const int StorageMarkerValue = -3;

	public const int DefaultPutFee = 100;

	public const int DefaultTakeFee = 0;

	public static Func<string> FeeProvider;

	private static Dictionary<int, int[]> fees;

	public static void Reset()
	{
		fees = null;
	}

	private static void LoadFees()
	{
		fees = new Dictionary<int, int[]>();
		string text = FeeProvider?.Invoke();
		if (text == null)
		{
			return;
		}
		string[] array = text.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Trim().Split('|');
			if (array2.Length == 4 && !(array2[0] != "F"))
			{
				fees[int.Parse(array2[1])] = new int[2]
				{
					int.Parse(array2[2]),
					int.Parse(array2[3])
				};
			}
		}
	}

	private static int Fee(int npcId, int which, int fallback)
	{
		if (fees == null)
		{
			LoadFees();
		}
		if (!fees.TryGetValue(npcId, out var value))
		{
			return fallback;
		}
		return value[which];
	}

	public static int PutFee(int npcId)
	{
		return Fee(npcId, 0, 100);
	}

	public static int TakeFee(int npcId)
	{
		return Fee(npcId, 1, 0);
	}

	public static StorageResponse Handle(DbStore db, StorageRequest r)
	{
		CharRecord charRecord = CharactersDao.FindById(db, r.characterId);
		if (charRecord == null)
		{
			return new StorageResponse
			{
				ok = false,
				error = "角色不存在"
			};
		}
		if (charRecord.level < 15 && r.operation != StorageOperation.Close)
		{
			return new StorageResponse
			{
				ok = false,
				error = "15级以后才可以使用仓库服务"
			};
		}
		return r.operation switch
		{
			StorageOperation.Open => Open(db, charRecord, r.npcId), 
			StorageOperation.Store => Store(db, charRecord, r), 
			StorageOperation.TakeOut => TakeOut(db, charRecord, r), 
			StorageOperation.Meso => Meso(db, charRecord, r), 
			StorageOperation.Arrange => Arrange(db, charRecord, r.npcId), 
			StorageOperation.Close => new StorageResponse
			{
				ok = true
			}, 
			StorageOperation.IncreaseSlots => IncreaseSlots(db, charRecord, r.slots), 
			_ => new StorageResponse
			{
				ok = false,
				error = "未知操作"
			}, 
		};
	}

	private static DbRow StorageRow(DbStore db, int accountId, bool create)
	{
		DbRow dbRow = db.Table("storages").Find((DbRow x) => DbStore.Int(x, "accountid") == accountId);
		if ((dbRow == null) & create)
		{
			db.Table("storages").Insert(new DbRow
			{
				["accountid"] = (long)accountId,
				["slots"] = 4L,
				["meso"] = 0L
			});
			dbRow = db.Table("storages").Find((DbRow x) => DbStore.Int(x, "accountid") == accountId);
			db.Save();
		}
		return dbRow;
	}

	private static int Slots(DbStore db, int accountId)
	{
		return DbStore.Int(StorageRow(db, accountId, create: true), "slots", 4);
	}

	private static long Meso(DbStore db, int accountId)
	{
		return DbStore.Long(StorageRow(db, accountId, create: true), "meso", 0L);
	}

	public static List<InventoryEntry> LoadItems(DbStore db, int accountId)
	{
		List<InventoryEntry> list = new List<InventoryEntry>();
		foreach (DbRow item in from x in db.Table("inventoryitems")
			select DbStore.Int(x, "characterid") == -3 && DbStore.Int(x, "storageaccountid") == accountId)
		{
			list.Add(CharService.ToInventoryEntry(db, item));
		}
		return list;
	}

	private static void DeleteStorageItem(DbStore db, InventoryEntry entry)
	{
		long id = entry.itemRowId;
		db.Table("inventoryitems").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == id);
		db.Table("inventoryequipment").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == id);
	}

	private static void WriteStorageEntry(DbStore db, int accountId, InventoryEntry entry)
	{
		long id = NativeMerchantService.WriteEntry(db, -3, entry, 0, entry.quantity);
		db.Table("inventoryitems").Find((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == id)["storageaccountid"] = (long)accountId;
	}

	private static StorageResponse Open(DbStore db, CharRecord c, int npcId)
	{
		List<InventoryEntry> list = LoadItems(db, c.accountid);
		list.Sort((InventoryEntry a, InventoryEntry b) => a.inventoryType.CompareTo(b.inventoryType));
		return new StorageResponse
		{
			storage = Snapshot(npcId, db, c, list)
		};
	}

	private static StorageSnapshot Snapshot(int npcId, DbStore db, CharRecord c, List<InventoryEntry> items)
	{
		StorageSnapshot storageSnapshot = new StorageSnapshot
		{
			npcId = npcId,
			slots = Slots(db, c.accountid),
			meso = Meso(db, c.accountid)
		};
		for (int i = 0; i < items.Count; i++)
		{
			storageSnapshot.items.Add(new StorageSlotData
			{
				index = i,
				item = items[i]
			});
		}
		return storageSnapshot;
	}

	private static StorageResponse Store(DbStore db, CharRecord c, StorageRequest r)
	{
		if (r.quantity < 1)
		{
			return new StorageResponse
			{
				ok = true,
				error = ""
			};
		}
		InventoryEntry inventoryEntry = CharService.Load(db, (int)c.id).inventory.Find((InventoryEntry x) => x.inventoryType == r.inventoryType && x.position == r.itemPosition && InventoryDao.IsBagPosition(x.position));
		if (inventoryEntry == null || inventoryEntry.itemId != r.itemId)
		{
			return new StorageResponse
			{
				ok = false,
				error = "背包里没有这个道具"
			};
		}
		if (IsWeddingItem(inventoryEntry.itemId))
		{
			return new StorageResponse
			{
				ok = false,
				error = "这个道具不能存进仓库"
			};
		}
		if (NativePotentialService.BlocksTransfer(db, inventoryEntry))
		{
			return new StorageResponse
			{
				ok = false,
				error = "请先处理装备痕迹或方块候选结果"
			};
		}
		int num = Slots(db, c.accountid);
		if (LoadItems(db, c.accountid).Count >= num)
		{
			return new StorageResponse
			{
				ok = false,
				code = 17,
				error = "仓库已满"
			};
		}
		int num2 = PutFee(r.npcId);
		if (c.meso < num2)
		{
			return new StorageResponse
			{
				ok = false,
				code = 11,
				error = "金币不足"
			};
		}
		int quantity = r.quantity;
		if (TradeItemRules.Rechargeable(inventoryEntry.itemId))
		{
			quantity = inventoryEntry.quantity;
		}
		if (quantity > inventoryEntry.quantity)
		{
			return new StorageResponse
			{
				ok = false,
				error = "数量不合法"
			};
		}
		DbRow row = InventoryRow(db, (int)c.id, inventoryEntry.itemRowId);
		NativeMerchantService.ConsumeFromInventory(db, row, inventoryEntry, quantity);
		c.meso -= num2;
		CharactersDao.Save(db, c);
		InventoryEntry inventoryEntry2 = inventoryEntry.Copy();
		inventoryEntry2.quantity = quantity;
		KarmaToUntradeable(inventoryEntry2);
		WriteStorageEntry(db, c.accountid, inventoryEntry2);
		db.Save();
		return ResponseAfter(db, c, r.npcId, null, num2, stored: true);
	}

	private static StorageResponse TakeOut(DbStore db, CharRecord c, StorageRequest r)
	{
		List<InventoryEntry> list = LoadItems(db, c.accountid).FindAll((InventoryEntry x) => x.inventoryType == r.inventoryType);
		if (r.storageIndex < 0 || r.storageIndex >= list.Count)
		{
			return new StorageResponse
			{
				ok = false,
				error = "仓库里没有这个道具"
			};
		}
		InventoryEntry inventoryEntry = list[r.storageIndex];
		if (BotItemCatalog.Int(BotItemCatalog.Shared.Info(inventoryEntry.itemId), "only") == 1)
		{
			foreach (InventoryEntry item in CharService.Load(db, (int)c.id).inventory)
			{
				if (item.itemId == inventoryEntry.itemId)
				{
					return Fail(12, "身上已有一件这个道具");
				}
			}
		}
		int num = TakeFee(r.npcId);
		if (c.meso < num)
		{
			return Fail(11, "金币不足");
		}
		FullCharacter fullCharacter = CharService.Load(db, (int)c.id);
		BotActor actor = new BotActor
		{
			id = (int)c.id,
			character = fullCharacter
		};
		if (TradeItemRules.FreeSlot(actor, fullCharacter.inventory, r.inventoryType) <= 0)
		{
			return Fail(10, "背包空间不足");
		}
		DeleteStorageItem(db, inventoryEntry);
		KarmaToUntradeable(inventoryEntry);
		int position = TradeItemRules.FreeSlot(actor, fullCharacter.inventory, r.inventoryType);
		NativeMerchantService.WriteEntry(db, (int)c.id, inventoryEntry, position, inventoryEntry.quantity);
		c.meso -= num;
		CharactersDao.Save(db, c);
		db.Save();
		return ResponseAfter(db, c, r.npcId, null, num, stored: false);
	}

	private static StorageResponse Meso(DbStore db, CharRecord c, StorageRequest r)
	{
		long num = Meso(db, c.accountid);
		if ((r.meso > 0 && num >= r.meso) || (r.meso < 0 && c.meso >= -r.meso))
		{
			StorageRow(db, c.accountid, create: true)["meso"] = num - r.meso;
			c.meso += r.meso;
			CharactersDao.Save(db, c);
			db.Save();
			return ResponseAfter(db, c, r.npcId, null, 0L, stored: false);
		}
		return new StorageResponse
		{
			ok = true,
			storage = Snapshot(r.npcId, db, c, LoadItems(db, c.accountid))
		};
	}

	private static StorageResponse Arrange(DbStore db, CharRecord c, int npcId)
	{
		if (!GameConfigDao.GetBool(db, "use_storage_item_sort"))
		{
			return new StorageResponse
			{
				storage = Snapshot(npcId, db, c, LoadItems(db, c.accountid))
			};
		}
		List<InventoryEntry> list = LoadItems(db, c.accountid);
		TradeItemRules tradeItemRules = new TradeItemRules(db, BotItemCatalog.Shared);
		bool byName = GameConfigDao.GetBool(db, "use_item_sort_by_name");
		List<InventoryEntry> list2 = new List<InventoryEntry>();
		foreach (InventoryEntry item in list)
		{
			if (!TryMergeInto(list2, item, tradeItemRules.SlotMax(item.itemId, new BotActor
			{
				id = (int)c.id
			})))
			{
				list2.Add(item);
			}
		}
		list2.Sort((InventoryEntry a, InventoryEntry b) =>
		{
			if (a.inventoryType != b.inventoryType)
			{
				return a.inventoryType.CompareTo(b.inventoryType);
			}
			return (!byName) ? a.itemId.CompareTo(b.itemId) : string.CompareOrdinal(NativeItemName(a.itemId), NativeItemName(b.itemId));
		});
		foreach (InventoryEntry item2 in list)
		{
			DeleteStorageItem(db, item2);
		}
		foreach (InventoryEntry item3 in list2)
		{
			WriteStorageEntry(db, c.accountid, item3);
		}
		db.Save();
		return new StorageResponse
		{
			storage = Snapshot(npcId, db, c, LoadItems(db, c.accountid))
		};
	}

	private static bool TryMergeInto(List<InventoryEntry> merged, InventoryEntry item, int slotMax)
	{
		if (item.isEquip || TradeItemRules.Rechargeable(item.itemId))
		{
			return false;
		}
		if (BotItemCatalog.Int(BotItemCatalog.Shared.Info(item.itemId), "only") == 1)
		{
			return false;
		}
		foreach (InventoryEntry item2 in merged)
		{
			if (!item2.isEquip && item2.itemId == item.itemId && !(item2.owner != item.owner) && !TradeItemRules.Rechargeable(item2.itemId) && item2.quantity < slotMax)
			{
				if (item2.quantity + item.quantity <= slotMax)
				{
					item2.quantity += item.quantity;
					return true;
				}
				int num = slotMax - item2.quantity;
				item2.quantity = slotMax;
				item.quantity -= num;
			}
		}
		return item.quantity <= 0;
	}

	private static string NativeItemName(int id)
	{
		return BotItemCatalog.Shared.Name(id) ?? "";
	}

	private static StorageResponse IncreaseSlots(DbStore db, CharRecord c, int slots)
	{
		int num = Slots(db, c.accountid);
		if (num + slots > 48)
		{
			return new StorageResponse
			{
				ok = false,
				error = "仓库格已达上限（48）"
			};
		}
		StorageRow(db, c.accountid, create: true)["slots"] = (long)(num + slots);
		db.Save();
		return new StorageResponse
		{
			storage = Snapshot(0, db, c, LoadItems(db, c.accountid))
		};
	}

	private static StorageResponse ResponseAfter(DbStore db, CharRecord c, int npcId, List<InventoryEntry> _, long __, bool stored)
	{
		return new StorageResponse
		{
			storage = Snapshot(npcId, db, c, LoadItems(db, c.accountid)),
			inventory = CharService.Load(db, (int)c.id).inventory,
			meso = c.meso
		};
	}

	private static StorageResponse Fail(byte code, string error)
	{
		return new StorageResponse
		{
			ok = false,
			code = code,
			error = error
		};
	}

	private static DbRow InventoryRow(DbStore db, int cid, long itemRowId)
	{
		return db.Table("inventoryitems").Find((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == itemRowId);
	}

	private static void KarmaToUntradeable(InventoryEntry item)
	{
		int num = (item.isEquip ? 16 : 2);
		if ((item.flag & num) == num)
		{
			item.flag = (item.flag & ~num) | 8;
		}
	}

	private static bool IsWeddingItem(int itemId)
	{
		if (itemId != 1112803 && itemId != 1112806 && itemId != 1112807 && itemId != 1112809)
		{
			if (itemId >= 4031357)
			{
				return itemId <= 4031364;
			}
			return false;
		}
		return true;
	}
}
