using System;
using System.Collections.Generic;

public static class NativeSymbolService
{
	private static SymbolResponse Reject(string error)
	{
		return new SymbolResponse
		{
			ok = false,
			error = error
		};
	}

	public static SymbolResponse Handle(DbStore db, SymbolRequest request)
	{
		if (db == null || request == null)
		{
			return Reject("无效徽章请求");
		}
		lock (db)
		{
			CharRecord charRecord = CharactersDao.FindById(db, request.characterId);
			if (charRecord == null)
			{
				return Reject("角色不存在");
			}
			DbRow dbRow = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == request.characterId && DbStore.Long(x, "inventoryitemid", 0L) == request.inventoryItemId);
			if (dbRow == null)
			{
				return Reject("没有持有这件徽章");
			}
			int itemId = DbStore.Int(dbRow, "itemid");
			SymbolInfo symbolInfo = SymbolCatalog.Of(itemId);
			if (symbolInfo == null)
			{
				return Reject("这不是徽章");
			}
			if (!SymbolCatalog.IsMergeable(itemId))
			{
				return Reject("通用祕法符文不能成长");
			}
			DbStore.Int(dbRow, "inventorytype");
			int num = DbStore.Int(dbRow, "position");
			if (NativeAndroidService.IsApSlot(num))
			{
				return Reject("请先取下机器人换装部件");
			}
			bool flag = num < 0;
			if (request.operation == SymbolOperation.LevelUp && !flag)
			{
				return Reject("请先穿上这件徽章再升级");
			}
			long num2 = DbStore.Long(dbRow, "expiration", -1L);
			if (num2 > 0 && num2 <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
			{
				return Reject("徽章已到期");
			}
			if (DbStore.Int(dbRow, "flag") != 0)
			{
				return Reject("徽章已锁定");
			}
			DbRow dbRow2 = InventoryDao.EquipmentOf(db, request.inventoryItemId);
			if (dbRow2 == null)
			{
				dbRow2 = InventoryDao.EnsureEquipment(db, request.inventoryItemId);
				if (dbRow2 == null)
				{
					return Reject("徽章模板缺失，无法恢复实例");
				}
				db.Save();
			}
			if (DbStore.Int(dbRow2, "symbolLevel") == 0)
			{
				Normalize(dbRow2, itemId, charRecord.job);
			}
			SymbolResponse symbolResponse = Snapshot(db, charRecord, dbRow2, symbolInfo, flag);
			if (request.operation == SymbolOperation.Preview)
			{
				return symbolResponse;
			}
			if (request.operation == SymbolOperation.MergeOne || request.operation == SymbolOperation.MergeAll)
			{
				return Merge(db, request, dbRow, dbRow2, symbolInfo, symbolResponse);
			}
			return LevelUp(db, request, charRecord, dbRow, dbRow2, symbolInfo, symbolResponse);
		}
	}

	private static SymbolResponse Merge(DbStore db, SymbolRequest request, DbRow item, DbRow equipment, SymbolInfo info, SymbolResponse response)
	{
		int num = DbStore.Int(equipment, "symbolLevel");
		int num2 = DbStore.Int(equipment, "symbolExp");
		if (num < 1 || num >= info.maxLevel)
		{
			return Reject("徽章已满级");
		}
		int num3 = info.GrowthCost(num);
		if (num2 >= num3)
		{
			return Reject("经验已满，请先升级");
		}
		List<DbRow> list = new List<DbRow>();
		if (request.operation == SymbolOperation.MergeOne)
		{
			if (request.materialInventoryItemId == request.inventoryItemId)
			{
				return Reject("不能把徽章喂给自己");
			}
			DbRow dbRow = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == request.characterId && DbStore.Long(x, "inventoryitemid", 0L) == request.materialInventoryItemId);
			if (dbRow == null)
			{
				return Reject("材料徽章不在背包里");
			}
			if (!IsMaterial(db, request.characterId, dbRow, DbStore.Int(item, "itemid")))
			{
				return Reject("材料必须是同款、1 级、未使用的徽章");
			}
			list.Add(dbRow);
		}
		else
		{
			int num4 = num3 - num2;
			foreach (DbRow item2 in from x in db.Table("inventoryitems")
				select DbStore.Int(x, "characterid") == request.characterId)
			{
				if (list.Count >= num4)
				{
					break;
				}
				if (DbStore.Long(item2, "inventoryitemid", 0L) != request.inventoryItemId && IsMaterial(db, request.characterId, item2, DbStore.Int(item, "itemid")))
				{
					list.Add(item2);
				}
			}
			if (list.Count == 0)
			{
				return Reject("背包里没有可用的同款材料徽章");
			}
		}
		if (!request.commit)
		{
			int num5 = (response.mergedCount = Math.Min(list.Count, num3 - num2));
			response.growthCost = num3;
			response.canLevelUp = info.CanLevelUp(num, num2 + num5);
			return response;
		}
		db.BeginTransaction();
		try
		{
			foreach (DbRow item3 in list)
			{
				CharService.RemoveItem(db, DbStore.Long(item3, "inventoryitemid", 0L));
			}
			int num6 = Math.Min(list.Count, num3 - num2);
			equipment["symbolExp"] = (long)(num2 + num6);
			db.Save();
			db.Commit();
			response.exp = num2 + num6;
			response.mergedCount = num6;
		}
		catch
		{
			db.Rollback();
			throw;
		}
		response.growthCost = num3;
		response.canLevelUp = info.CanLevelUp(num, response.exp);
		response.inventory = CharService.Load(db, request.characterId).inventory;
		return response;
	}

	private static SymbolResponse LevelUp(DbStore db, SymbolRequest request, CharRecord character, DbRow item, DbRow equipment, SymbolInfo info, SymbolResponse response)
	{
		int num = DbStore.Int(equipment, "symbolLevel");
		int num2 = DbStore.Int(equipment, "symbolExp");
		int num3 = info.GrowthCost(num);
		if (!info.CanLevelUp(num, num2))
		{
			return Reject("经验不足，无法升级");
		}
		long num4 = SymbolCatalog.MesoCost(info.itemId, num);
		if (num4 <= 0)
		{
			return Reject("这件徽章没有可用的升级费用表");
		}
		long meso = character.meso;
		response.mesoCost = num4;
		response.meso = meso;
		if (!request.commit)
		{
			return response;
		}
		if (meso < num4)
		{
			return Reject("枫币不足");
		}
		int num5 = num + 1;
		db.BeginTransaction();
		try
		{
			equipment["symbolLevel"] = (long)num5;
			equipment["symbolExp"] = (long)Math.Max(0, num2 - num3);
			equipment["symbolInc"] = (long)info.IncAt(num5);
			ApplyMainStat(equipment, info, character.job);
			character.meso = meso - num4;
			CharactersDao.Save(db, character);
			db.Save();
			db.Commit();
		}
		catch
		{
			db.Rollback();
			throw;
		}
		response.level = num5;
		response.exp = Math.Max(0, num2 - num3);
		response.inc = info.IncAt(num5);
		response.meso = meso - num4;
		response.growthCost = info.GrowthCost(num5);
		response.canLevelUp = info.CanLevelUp(num5, response.exp);
		response.inventory = CharService.Load(db, request.characterId).inventory;
		return response;
	}

	private static SymbolResponse Snapshot(DbStore db, CharRecord character, DbRow equipment, SymbolInfo info, bool equipped)
	{
		int level = DbStore.Int(equipment, "symbolLevel");
		int exp = DbStore.Int(equipment, "symbolExp");
		return new SymbolResponse
		{
			itemId = info.itemId,
			level = level,
			exp = exp,
			maxLevel = info.maxLevel,
			growthCost = info.GrowthCost(level),
			mesoCost = SymbolCatalog.MesoCost(info.itemId, level),
			inc = DbStore.Int(equipment, "symbolInc"),
			meso = character.meso,
			canLevelUp = info.CanLevelUp(level, exp),
			equipped = equipped
		};
	}

	private static bool IsMaterial(DbStore db, int characterId, DbRow row, int itemId)
	{
		if (DbStore.Int(row, "itemid") != itemId)
		{
			return false;
		}
		int num = DbStore.Int(row, "position");
		if (num <= 0 || NativeAndroidService.IsApSlot(num))
		{
			return false;
		}
		if (DbStore.Long(row, "quantity", 0L) <= 0 || DbStore.Int(row, "flag") != 0)
		{
			return false;
		}
		long num2 = DbStore.Long(row, "expiration", -1L);
		if (num2 > 0 && num2 <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
		{
			return false;
		}
		DbRow dbRow = InventoryDao.EquipmentOf(db, DbStore.Long(row, "inventoryitemid", 0L));
		if (dbRow == null)
		{
			return true;
		}
		return SymbolCatalog.IsMergeMaterial(DbStore.Int(dbRow, "symbolLevel"), DbStore.Int(dbRow, "symbolExp"));
	}

	public static void Normalize(DbRow equipment, int itemId, int job)
	{
		SymbolInfo symbolInfo = SymbolCatalog.Of(itemId);
		if (symbolInfo != null)
		{
			bool isArcane = symbolInfo.IsArcane;
			equipment["symbolLevel"] = 1L;
			if (DbStore.Int(equipment, "symbolExp") < 0)
			{
				equipment["symbolExp"] = 0L;
			}
			equipment["symbolInc"] = (long)symbolInfo.initialInc;
			if (IsDemonAvenger(job))
			{
				equipment["hp"] = (long)((!isArcane) ? 10500 : ((itemId == 1712000) ? 4200 : 5250));
			}
			else if (IsXenon(job))
			{
				long num = (isArcane ? 117 : 240);
				equipment["str"] = num;
				equipment["dex"] = num;
				equipment["luk"] = num;
			}
			else
			{
				equipment[MainStatColumn(job)] = (long)(isArcane ? 300 : 500);
			}
		}
	}

	private static void ApplyMainStat(DbRow equipment, SymbolInfo info, int job)
	{
		if (IsDemonAvenger(job))
		{
			equipment["hp"] = DbStore.Long(equipment, "hp", 0L) + info.demonAvengerHpPerLevel;
		}
		else if (IsXenon(job))
		{
			long num = info.xenonPerLevel;
			equipment["str"] = DbStore.Long(equipment, "str", 0L) + num;
			equipment["dex"] = DbStore.Long(equipment, "dex", 0L) + num;
			equipment["luk"] = DbStore.Long(equipment, "luk", 0L) + num;
		}
		else
		{
			string text = MainStatColumn(job);
			equipment[text] = DbStore.Long(equipment, text, 0L) + info.mainStatPerLevel;
		}
	}

	public static bool IsDemonAvenger(int job)
	{
		if (job != 3101)
		{
			return job == 3102;
		}
		return true;
	}

	public static bool IsXenon(int job)
	{
		if (job >= 3600)
		{
			return job <= 3612;
		}
		return false;
	}

	public static string MainStatColumn(int job)
	{
		return (job / 100 % 10) switch
		{
			2 => "int", 
			3 => "dex", 
			4 => "luk", 
			_ => "str", 
		};
	}
}
