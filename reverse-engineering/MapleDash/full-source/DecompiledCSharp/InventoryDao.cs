using System;
using System.Collections.Generic;
using Server.Bot;

public static class InventoryDao
{
	public const int Equipped = 1;

	public const int Equip = 1;

	public const int Use = 2;

	public const int Setup = 3;

	public const int Etc = 4;

	public const int Cash = 5;

	public static bool IsBagPosition(int position)
	{
		if (position > 0)
		{
			return !NativeAndroidService.IsApSlot(position);
		}
		return false;
	}

	public static bool IsRetiredSoulMaterial(int itemId)
	{
		if (itemId / 1000 != 2590)
		{
			return itemId / 1000 == 2591;
		}
		return true;
	}

	public static int PurgeRetiredSoulMaterials(DbStore db)
	{
		int num = 0;
		string[] array;
		foreach (DbRow item in new List<DbRow>(db.Table("inventoryitems").rows))
		{
			if (!IsRetiredSoulMaterial(DbStore.Int(item, "itemid")))
			{
				continue;
			}
			long num2 = DbStore.Long(item, "inventoryitemid", 0L);
			num += db.Table("inventoryitems").Delete(DbStore.Eq("inventoryitemid", num2));
			db.Table("inventoryequipment").Delete(DbStore.Eq("inventoryitemid", num2));
			array = new string[2] { "dueyitems", "merchantitems" };
			foreach (string name in array)
			{
				if (db.HasTable(name))
				{
					db.Table(name).Delete(DbStore.Eq("inventoryitemid", num2));
				}
			}
		}
		array = new string[4] { "extrastorage", "shopitems", "drop_data", "drop_data_global" };
		foreach (string name2 in array)
		{
			if (db.HasTable(name2))
			{
				num += db.Table(name2).Delete((DbRow row) => IsRetiredSoulMaterial(DbStore.Int(row, "itemid")));
			}
		}
		return num;
	}

	public static long InsertItem(DbStore db, int characterId, int itemId, int inventoryType, int position, int quantity, int petId = -1)
	{
		if (IsRetiredSoulMaterial(itemId))
		{
			throw new InvalidOperationException("旧灵魂附魔石和灵魂宝珠已移除，请使用新版灵魂武器系统");
		}
		if (itemId / 1000 == 5000 && petId < 0)
		{
			petId = (int)db.Table("pets").Insert(new DbRow
			{
				["name"] = BotItemCatalog.Shared.Name(itemId),
				["level"] = 1L,
				["closeness"] = 0L,
				["fullness"] = 100L,
				["summoned"] = 0L,
				["flag"] = 0L
			});
		}
		return db.Table("inventoryitems").Insert(new DbRow
		{
			["type"] = (long)(itemId / 1000000),
			["characterid"] = (long)characterId,
			["itemid"] = (long)itemId,
			["inventorytype"] = (long)inventoryType,
			["position"] = (long)position,
			["quantity"] = (long)quantity,
			["owner"] = "",
			["petid"] = (long)petId,
			["flag"] = 0L,
			["expiration"] = -1L,
			["giftFrom"] = ""
		});
	}

	public static DbRow EnsureEquipment(DbStore db, long inventoryItemId, BotItemCatalog catalog = null)
	{
		DbRow dbRow = EquipmentOf(db, inventoryItemId);
		if (dbRow != null)
		{
			return dbRow;
		}
		int num = DbStore.Int(db.Table("inventoryitems").Find((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == inventoryItemId), "itemid");
		if (num / 1000000 != 1)
		{
			return null;
		}
		if (catalog == null)
		{
			catalog = BotItemCatalog.Shared;
		}
		Dictionary<string, object> info = catalog.Info(num);
		if (info == null)
		{
			return null;
		}
		InsertEquipment(db, inventoryItemId, Read("incSTR"), Read("incDEX"), Read("incINT"), Read("incLUK"), Read("incMHP"), Read("incMMP"), Read("incPAD"), Read("incMAD"), Read("incPDD"), Read("incMDD"), Read("incACC"), Read("incEVA"), Read("incCraft"), Read("incSpeed"), Read("incJump"), Read("tuc"));
		DbRow dbRow2 = EquipmentOf(db, inventoryItemId);
		dbRow2["nativeStats"] = 1L;
		return dbRow2;
		int Read(string key)
		{
			return BotItemCatalog.Int(info, key);
		}
	}

	public static void InsertEquipment(DbStore db, long inventoryItemId, int str = 0, int dex = 0, int intl = 0, int luk = 0, int hp = 0, int mp = 0, int watk = 0, int matk = 0, int wdef = 0, int mdef = 0, int acc = 0, int avoid = 0, int hands = 0, int speed = 0, int jump = 0, int upgradeslots = 0)
	{
		db.Table("inventoryequipment").Insert(new DbRow
		{
			["inventoryitemid"] = inventoryItemId,
			["upgradeslots"] = (long)upgradeslots,
			["str"] = (long)str,
			["dex"] = (long)dex,
			["int"] = (long)intl,
			["luk"] = (long)luk,
			["hp"] = (long)hp,
			["mp"] = (long)mp,
			["watk"] = (long)watk,
			["matk"] = (long)matk,
			["wdef"] = (long)wdef,
			["mdef"] = (long)mdef,
			["acc"] = (long)acc,
			["avoid"] = (long)avoid,
			["hands"] = (long)hands,
			["speed"] = (long)speed,
			["jump"] = (long)jump
		});
	}

	public static List<DbRow> ItemsByCharacter(DbStore db, int characterId)
	{
		return db.Table("inventoryitems").Select(DbStore.Eq("characterid", (long)characterId));
	}

	public static bool ExpireItems(DbStore db, int characterId, long now)
	{
		List<DbRow> list = from x in db.Table("inventoryitems")
			select DbStore.Int(x, "characterid") == characterId && DbStore.Long(x, "expiration", 0L) > 0 && DbStore.Long(x, "expiration", 0L) <= now;
		foreach (DbRow item in list)
		{
			int num = DbStore.Int(item, "flag");
			if ((num & 1) != 0)
			{
				item["flag"] = (long)(num & -2);
				item["expiration"] = -1L;
				continue;
			}
			int petId = DbStore.Int(item, "petid", -1);
			int num2 = DbStore.Int(item, "itemid");
			if (num2 / 1000 == 5000)
			{
				DbRow dbRow = db.Table("pets").Find((DbRow x) => DbStore.Int(x, "petid") == petId);
				if (dbRow != null)
				{
					dbRow["summoned"] = 0L;
				}
				if (num2 != 5000054 && !GameConfigDao.GetBool(db, "use_erase_pet_on_expiration"))
				{
					item["expiration"] = -1L;
					continue;
				}
				db.Table("petignores").Delete((DbRow x) => DbStore.Int(x, "petid") == petId);
				db.Table("pets").Delete((DbRow x) => DbStore.Int(x, "petid") == petId);
			}
			long id = DbStore.Long(item, "inventoryitemid", 0L);
			db.Table("inventoryequipment").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == id);
			db.Table("inventoryitems").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == id);
		}
		return list.Count > 0;
	}

	public static DbRow EquipmentOf(DbStore db, long inventoryItemId)
	{
		return db.Table("inventoryequipment").Find(DbStore.Eq("inventoryitemid", inventoryItemId));
	}

	public static int CountOf(DbStore db, int characterId, int itemId)
	{
		int num = 0;
		foreach (DbRow item in ItemsByCharacter(db, characterId))
		{
			if (DbStore.Long(item, "itemid", 0L) == itemId && IsBagPosition(DbStore.Int(item, "position")))
			{
				num += (int)DbStore.Long(item, "quantity", 0L);
			}
		}
		return num;
	}

	public static int ConsumeItem(DbStore db, int characterId, int itemId, int count)
	{
		if (count <= 0)
		{
			return 0;
		}
		DbTable dbTable = db.Table("inventoryitems");
		int num = count;
		foreach (DbRow item in ItemsByCharacter(db, characterId))
		{
			if (num <= 0)
			{
				break;
			}
			if (DbStore.Long(item, "itemid", 0L) != itemId || !IsBagPosition(DbStore.Int(item, "position")))
			{
				continue;
			}
			int num2 = (int)DbStore.Long(item, "quantity", 0L);
			int num3 = Math.Min(num2, num);
			long id = DbStore.Long(item, "inventoryitemid", 0L);
			if (NativePotentialService.IsReserved(db, id))
			{
				continue;
			}
			if (num2 - num3 <= 0)
			{
				dbTable.Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == id);
				db.Table("inventoryequipment").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == id);
			}
			else
			{
				item["quantity"] = (long)(num2 - num3);
			}
			num -= num3;
		}
		return count - num;
	}
}
