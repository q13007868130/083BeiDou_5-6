using System;

public static class NativeEquipService
{
	public static EquipPatchResponse Patch(DbStore db, EquipPatchRequest r)
	{
		if (r != null && NativePotentialService.IsReserved(db, r.inventoryItemId))
		{
			return new EquipPatchResponse
			{
				ok = false,
				error = "请先选择方块候选结果"
			};
		}
		if (r != null && r.cashItemId > 0)
		{
			return NativeCashEquipmentService.Apply(db, r);
		}
		if (r != null && r.scrollId > 0)
		{
			return NativeScrollService.Apply(db, r);
		}
		if (r == null || r.inventoryItemId <= 0)
		{
			return new EquipPatchResponse
			{
				ok = false,
				error = "无效装备"
			};
		}
		DbRow dbRow = db.Table("inventoryitems").Find((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == r.inventoryItemId && DbStore.Int(x, "characterid") == r.characterId);
		if (dbRow == null)
		{
			return new EquipPatchResponse
			{
				ok = false,
				error = "找不到这件装备"
			};
		}
		if (r.expiration.HasValue)
		{
			dbRow["expiration"] = r.expiration.Value;
			db.Save();
			return new EquipPatchResponse
			{
				inventory = CharService.Load(db, r.characterId).inventory
			};
		}
		DbRow dbRow2 = db.Table("inventoryequipment").Find((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == r.inventoryItemId);
		if (dbRow2 != null && DbStore.Int(dbRow2, "enhancementVersion") == 282)
		{
			return new EquipPatchResponse
			{
				ok = false,
				error = "282 强化装备不能通过旧属性写入接口修改"
			};
		}
		if (dbRow2 == null)
		{
			InventoryDao.InsertEquipment(db, r.inventoryItemId);
			dbRow2 = db.Table("inventoryequipment").Find((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == r.inventoryItemId);
			if (dbRow2 == null)
			{
				return new EquipPatchResponse
				{
					ok = false,
					error = "装备行写入失败"
				};
			}
		}
		dbRow2["upgradeslots"] = (long)r.upgradeslots;
		dbRow2["nativeStats"] = 1L;
		dbRow2["str"] = (long)r.str;
		dbRow2["dex"] = (long)r.dex;
		dbRow2["int"] = (long)r.int_;
		dbRow2["luk"] = (long)r.luk;
		dbRow2["hp"] = (long)r.hp;
		dbRow2["mp"] = (long)r.mp;
		dbRow2["watk"] = (long)r.watk;
		dbRow2["matk"] = (long)r.matk;
		dbRow2["wdef"] = (long)r.wdef;
		dbRow2["mdef"] = (long)r.mdef;
		dbRow2["acc"] = (long)r.acc;
		dbRow2["avoid"] = (long)r.avoid;
		dbRow2["hands"] = (long)r.hands;
		dbRow2["speed"] = (long)r.speed;
		dbRow2["jump"] = (long)r.jump;
		dbRow2["starLevel"] = (long)Math.Max(0, r.starLevel);
		dbRow2["starCount"] = (long)Math.Max(0, r.starCount);
		dbRow2["maxStar"] = (long)Math.Max(0, r.maxStar);
		if (r.owner != null)
		{
			dbRow["owner"] = r.owner;
		}
		if (r.expandAttribute4 != null)
		{
			dbRow2["expandAttribute4"] = r.expandAttribute4;
		}
		db.Save();
		return new EquipPatchResponse
		{
			inventory = CharService.Load(db, r.characterId).inventory
		};
	}

	public static SlotRemoveResponse RemoveSlot(DbStore db, SlotRemoveRequest r)
	{
		if (r == null)
		{
			return new SlotRemoveResponse
			{
				ok = false,
				error = "无效请求"
			};
		}
		int num = Math.Max(1, r.quantity);
		DbRow dbRow = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == r.characterId && DbStore.Int(x, "inventorytype") == r.inventoryType && DbStore.Int(x, "position") == r.slot);
		if (dbRow == null)
		{
			return new SlotRemoveResponse
			{
				ok = false,
				error = "该格位没有道具"
			};
		}
		if (NativePotentialService.IsReserved(db, DbStore.Long(dbRow, "inventoryitemid", 0L)))
		{
			return new SlotRemoveResponse
			{
				ok = false,
				error = "请先选择方块候选结果"
			};
		}
		long id = DbStore.Long(dbRow, "inventoryitemid", 0L);
		int num2 = DbStore.Int(dbRow, "quantity", 1);
		if (num2 <= num)
		{
			db.Table("inventoryitems").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == id);
			db.Table("inventoryequipment").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == id);
		}
		else
		{
			dbRow["quantity"] = (long)(num2 - num);
		}
		db.Save();
		return new SlotRemoveResponse
		{
			inventory = CharService.Load(db, r.characterId).inventory
		};
	}
}
