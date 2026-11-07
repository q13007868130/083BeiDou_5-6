using System;
using System.Collections.Generic;
using Server.Bot;

public static class NativeDueyService
{
	public const int QuickDeliveryTicket = 5330000;

	private const int UnTradeable = 8;

	private const int KarmaUse = 2;

	private const int KarmaEqp = 16;

	public const byte SendEnableActions = 9;

	public const byte NotEnoughMesos = 10;

	public const byte IncorrectRequest = 11;

	public const byte NameDoesNotExist = 12;

	public const byte SameAccError = 13;

	public const byte SuccessfullySent = 18;

	public const byte RecvUnknownError = 19;

	public const byte RecvNoFreeSlots = 21;

	public const byte RecvReceiverWithUnique = 22;

	public static int GetFee(long meso)
	{
		long num = 0L;
		if (meso >= 100000000)
		{
			num = meso * 6 / 100;
		}
		else if (meso >= 25000000)
		{
			num = meso * 5 / 100;
		}
		else if (meso >= 10000000)
		{
			num = meso * 4 / 100;
		}
		else if (meso >= 5000000)
		{
			num = meso * 3 / 100;
		}
		else if (meso >= 1000000)
		{
			num = meso * 18 / 1000;
		}
		else if (meso >= 100000)
		{
			num = meso * 8 / 1000;
		}
		return (int)num;
	}

	private static bool Rechargeable(int itemId)
	{
		return itemId / 10000 == 207;
	}

	private static bool IsUntradeable(DbStore db, InventoryEntry item)
	{
		if (NativePotentialService.BlocksTransfer(db, item))
		{
			return true;
		}
		if ((item.flag & 8) != 0)
		{
			return true;
		}
		int num = (item.isEquip ? 16 : 2);
		if ((item.flag & num) == num)
		{
			return false;
		}
		return IsDropRestricted(item);
	}

	private static bool IsDropRestricted(InventoryEntry item)
	{
		return BotItemCatalog.Int(BotItemCatalog.Shared.Info(item.itemId), "tradeBlock") == 1;
	}

	private static bool IsPickupRestricted(int itemId)
	{
		return BotItemCatalog.Int(BotItemCatalog.Shared.Info(itemId), "only") == 1;
	}

	private static bool IsUnmerchable(DbStore db, int itemId)
	{
		if (GameConfigDao.GetBool(db, "use_enforce_unmerchable_cash", fallback: true) && itemId / 1000000 == 5)
		{
			return true;
		}
		if (GameConfigDao.GetBool(db, "use_enforce_unmerchable_pet", fallback: true))
		{
			return itemId / 1000 == 5000;
		}
		return false;
	}

	private static void ToggleKarmaFlagToUntradeable(InventoryEntry item)
	{
		int num = (item.isEquip ? 16 : 2);
		if ((item.flag & num) == num)
		{
			item.flag = (item.flag ^ num) | 8;
		}
	}

	public static DueyResponse Handle(DbStore db, DueyRequest r)
	{
		if (!GameConfigDao.GetBool(db, "use_duey", fallback: true))
		{
			return new DueyResponse
			{
				ok = false,
				error = "快递系统未开启"
			};
		}
		CharRecord charRecord = CharactersDao.FindById(db, r.characterId);
		if (charRecord == null)
		{
			return new DueyResponse
			{
				ok = false,
				error = "角色不存在"
			};
		}
		return r.operation switch
		{
			DueyOperation.Open => new DueyResponse
			{
				packages = Load(db, (int)charRecord.id)
			}, 
			DueyOperation.Quick => new DueyResponse
			{
				quick = true
			}, 
			DueyOperation.Send => Send(db, charRecord, r), 
			DueyOperation.Claim => Claim(db, charRecord, r), 
			DueyOperation.Remove => Remove(db, charRecord, r), 
			DueyOperation.Notify => new DueyResponse
			{
				pending = TakePendingNotification(db, (int)charRecord.id)
			}, 
			_ => new DueyResponse
			{
				ok = true
			}, 
		};
	}

	private static List<DueyPackageData> Load(DbStore db, int characterId)
	{
		List<DueyPackageData> list = new List<DueyPackageData>();
		foreach (DbRow item2 in from x in db.Table("dueypackages")
			select DbStore.Int(x, "receiverid") == characterId)
		{
			InventoryEntry item = ItemOf(db, DbStore.Int(item2, "packageid"));
			list.Add(new DueyPackageData
			{
				packageId = DbStore.Int(item2, "packageid"),
				mesos = DbStore.Int(item2, "mesos"),
				sender = DbStore.Str(item2, "sendername"),
				message = (item2.TryGetValue("message", out var value) ? (value as string) : null),
				sendTime = DbStore.Long(item2, "sendtime", 0L),
				quick = (DbStore.Int(item2, "type") == 1),
				item = item
			});
		}
		return list;
	}

	private static InventoryEntry ItemOf(DbStore db, int packageId)
	{
		DbRow dbRow = db.Table("dueyitems").Find((DbRow x) => DbStore.Int(x, "packageid") == packageId);
		if (dbRow == null)
		{
			return null;
		}
		long rowId = DbStore.Long(dbRow, "inventoryitemid", 0L);
		DbRow dbRow2 = db.Table("inventoryitems").Find((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == rowId);
		if (dbRow2 != null)
		{
			return CharService.ToInventoryEntry(db, dbRow2);
		}
		return null;
	}

	private static DueyResponse Send(DbStore db, CharRecord c, DueyRequest r)
	{
		int characterId = (int)c.id;
		int num = GameConfigDao.GetInt(db, "minimum_gm_level_to_use_duey", 4);
		if (c.gm > 0 && c.gm < num)
		{
			return new DueyResponse
			{
				ok = false,
				code = 11,
				error = "当前 GM 等级不能使用快递"
			};
		}
		if (r.mesos < 0)
		{
			return new DueyResponse
			{
				ok = false,
				code = 11,
				error = "金额不合法"
			};
		}
		if (!string.IsNullOrEmpty(r.message) && r.message.Length > 100)
		{
			return new DueyResponse
			{
				ok = false,
				code = 11,
				error = "留言过长"
			};
		}
		int num2 = GetFee(r.mesos);
		if (!r.quick)
		{
			num2 += 5000;
		}
		else if (CountOf(db, characterId, 5330000) < 1)
		{
			return new DueyResponse
			{
				ok = false,
				code = 11,
				error = "缺少快速投递券"
			};
		}
		long num3 = (long)r.mesos + (long)num2;
		if (num3 < 0 || num3 > int.MaxValue || (r.itemQuantity < 1 && r.mesos == 0))
		{
			return new DueyResponse
			{
				ok = false,
				code = 11,
				error = "请求不合法"
			};
		}
		if (c.meso < num3)
		{
			return new DueyResponse
			{
				ok = false,
				code = 10,
				error = "金币不足",
				fee = num2
			};
		}
		DbRow dbRow = db.Table("characters").Find((DbRow x) => string.Equals(DbStore.Str(x, "name"), r.recipient, StringComparison.OrdinalIgnoreCase));
		CharRecord charRecord = ((dbRow == null) ? null : CharRecord.FromRow(dbRow));
		if (charRecord == null)
		{
			return new DueyResponse
			{
				ok = false,
				code = 12,
				error = "收件人不存在"
			};
		}
		if (charRecord.accountid == c.accountid)
		{
			return new DueyResponse
			{
				ok = false,
				code = 13,
				error = "不能寄给自己账号"
			};
		}
		if (r.quick)
		{
			InventoryDao.ConsumeItem(db, characterId, 5330000, 1);
		}
		int num4 = (int)db.Table("dueypackages").Insert(new DbRow
		{
			["receiverid"] = charRecord.id,
			["sendername"] = c.name,
			["mesos"] = (long)r.mesos,
			["sendtime"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
			["message"] = r.message ?? "",
			["checked"] = 1L,
			["type"] = (long)(r.quick ? 1 : 0)
		});
		if (num4 <= 0)
		{
			return new DueyResponse
			{
				ok = false,
				code = 9,
				error = "创建包裹失败"
			};
		}
		c.meso -= num3;
		CharactersDao.Save(db, c);
		int num5 = AddPackageItemFromInventory(db, c, num4, r);
		db.Save();
		if (num5 == 0)
		{
			return new DueyResponse
			{
				ok = true,
				code = 18,
				fee = num2,
				meso = c.meso,
				inventory = CharService.Load(db, characterId).inventory
			};
		}
		if (num5 > 0)
		{
			return new DueyResponse
			{
				ok = false,
				code = 9,
				error = "包裹写入失败",
				fee = num2,
				meso = c.meso,
				inventory = CharService.Load(db, characterId).inventory
			};
		}
		return new DueyResponse
		{
			ok = false,
			code = 11,
			error = ((num5 == -1) ? "这件物品不能寄送" : "指定数量不足"),
			fee = num2,
			meso = c.meso,
			inventory = CharService.Load(db, characterId).inventory
		};
	}

	private static int AddPackageItemFromInventory(DbStore db, CharRecord c, int packageId, DueyRequest r)
	{
		if (r.inventoryType <= 0)
		{
			return 0;
		}
		int cid = (int)c.id;
		DbRow dbRow = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == cid && DbStore.Int(x, "inventorytype") == r.inventoryType && DbStore.Int(x, "position") == r.itemPosition && InventoryDao.IsBagPosition(DbStore.Int(x, "position")));
		if (dbRow == null)
		{
			return -2;
		}
		InventoryEntry inventoryEntry = CharService.ToInventoryEntry(db, dbRow);
		if (inventoryEntry.quantity < r.itemQuantity)
		{
			return -2;
		}
		if (IsUntradeable(db, inventoryEntry) || IsUnmerchable(db, inventoryEntry.itemId))
		{
			return -1;
		}
		int num = (Rechargeable(inventoryEntry.itemId) ? inventoryEntry.quantity : r.itemQuantity);
		if (num >= inventoryEntry.quantity)
		{
			long rowId = DbStore.Long(dbRow, "inventoryitemid", 0L);
			db.Table("inventoryitems").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == rowId);
			db.Table("inventoryequipment").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == rowId);
		}
		else
		{
			dbRow["quantity"] = (long)(inventoryEntry.quantity - num);
		}
		InventoryEntry inventoryEntry2 = inventoryEntry.Copy();
		ToggleKarmaFlagToUntradeable(inventoryEntry2);
		inventoryEntry2.quantity = r.itemQuantity;
		inventoryEntry2.position = 1;
		long newRow = InventoryDao.InsertItem(db, -1, inventoryEntry2.itemId, inventoryEntry2.inventoryType, 1, inventoryEntry2.quantity, inventoryEntry2.petId);
		DbRow dbRow2 = db.Table("inventoryitems").Find((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == newRow);
		if (dbRow2 != null)
		{
			dbRow2["owner"] = inventoryEntry2.owner ?? "";
			dbRow2["flag"] = (long)inventoryEntry2.flag;
			dbRow2["expiration"] = inventoryEntry2.expiration;
		}
		if (inventoryEntry2.isEquip)
		{
			InventoryDao.InsertEquipment(db, newRow);
		}
		db.Table("dueyitems").Insert(new DbRow
		{
			["packageid"] = (long)packageId,
			["inventoryitemid"] = newRow
		});
		return 0;
	}

	private static DueyResponse Claim(DbStore db, CharRecord c, DueyRequest r)
	{
		int num = (int)c.id;
		DbRow dbRow = db.Table("dueypackages").Find((DbRow x) => DbStore.Int(x, "packageid") == r.packageId);
		if (dbRow == null)
		{
			return new DueyResponse
			{
				ok = false,
				code = 19,
				error = "包裹不存在"
			};
		}
		if (DbStore.Int(dbRow, "receiverid") != num)
		{
			return new DueyResponse
			{
				ok = false,
				code = 19,
				error = "不是本人的包裹"
			};
		}
		if (DbStore.Long(dbRow, "sendtime", 0L) > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
		{
			return new DueyResponse
			{
				ok = false,
				code = 19,
				error = "包裹还在投递中"
			};
		}
		int num2 = DbStore.Int(dbRow, "mesos");
		InventoryEntry inventoryEntry = ItemOf(db, r.packageId);
		if (inventoryEntry != null)
		{
			if (c.meso > int.MaxValue - num2)
			{
				return new DueyResponse
				{
					ok = false,
					code = 19,
					error = "金币已达上限"
				};
			}
			InventoryChangeRequest inventoryChangeRequest = new InventoryChangeRequest();
			inventoryChangeRequest.checkItems = new int[1] { inventoryEntry.itemId };
			inventoryChangeRequest.checkQuantities = new int[1] { inventoryEntry.quantity };
			InventoryChangeRequest r2 = inventoryChangeRequest;
			if (!NativeInventoryService.CanReceive(db, c, r2))
			{
				bool flag = IsPickupRestricted(inventoryEntry.itemId) && CountOf(db, num, inventoryEntry.itemId) > 0;
				return new DueyResponse
				{
					ok = false,
					code = (byte)(flag ? 22 : 21),
					error = (flag ? "背包已有同一件唯一道具" : "背包空间不足")
				};
			}
			NativeInventoryService.AddItems(db, c, inventoryEntry.itemId, inventoryEntry.quantity, inventoryEntry.expiration, inventoryEntry.petId);
		}
		c.meso += num2;
		CharactersDao.Save(db, c);
		DeletePackage(db, r.packageId);
		db.Save();
		return new DueyResponse
		{
			ok = true,
			meso = c.meso,
			packages = Load(db, num),
			inventory = CharService.Load(db, num).inventory
		};
	}

	private static DueyResponse Remove(DbStore db, CharRecord c, DueyRequest r)
	{
		DbRow dbRow = db.Table("dueypackages").Find((DbRow x) => DbStore.Int(x, "packageid") == r.packageId);
		if (dbRow == null)
		{
			return new DueyResponse
			{
				ok = true,
				packages = Load(db, (int)c.id)
			};
		}
		if (DbStore.Int(dbRow, "receiverid") != (int)c.id)
		{
			return new DueyResponse
			{
				ok = false,
				code = 19,
				error = "不是本人的包裹"
			};
		}
		DeletePackage(db, r.packageId);
		db.Save();
		return new DueyResponse
		{
			ok = true,
			packages = Load(db, (int)c.id)
		};
	}

	private static void DeletePackage(DbStore db, int packageId)
	{
		DbRow dbRow = db.Table("dueyitems").Find((DbRow x) => DbStore.Int(x, "packageid") == packageId);
		if (dbRow != null)
		{
			long rowId = DbStore.Long(dbRow, "inventoryitemid", 0L);
			db.Table("inventoryitems").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == rowId);
			db.Table("inventoryequipment").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == rowId);
			db.Table("dueyitems").Delete((DbRow x) => DbStore.Int(x, "packageid") == packageId);
		}
		db.Table("dueypackages").Delete((DbRow x) => DbStore.Int(x, "packageid") == packageId);
	}

	public static int Expire(DbStore db, long now)
	{
		long cutoff = now - 2592000000u;
		List<DbRow> list = from x in db.Table("dueypackages")
			select DbStore.Long(x, "sendtime", 0L) < cutoff;
		foreach (DbRow item in list)
		{
			DeletePackage(db, DbStore.Int(item, "packageid"));
		}
		if (list.Count > 0)
		{
			db.Save();
		}
		return list.Count;
	}

	public static DueyPackageData TakePendingNotification(DbStore db, int characterId)
	{
		DbRow dbRow = (from x in db.Table("dueypackages")
			select DbStore.Int(x, "receiverid") == characterId && DbStore.Int(x, "checked") == 1).Find((DbRow x) => true);
		if (dbRow == null)
		{
			return null;
		}
		db.Table("dueypackages").Update((DbRow x) => DbStore.Int(x, "receiverid") == characterId, new DbRow { ["checked"] = 0L });
		db.Save();
		return new DueyPackageData
		{
			sender = DbStore.Str(dbRow, "sendername"),
			quick = (DbStore.Int(dbRow, "type") == 1),
			packageId = DbStore.Int(dbRow, "packageid")
		};
	}

	private static int CountOf(DbStore db, int characterId, int itemId)
	{
		return InventoryDao.CountOf(db, characterId, itemId);
	}
}
