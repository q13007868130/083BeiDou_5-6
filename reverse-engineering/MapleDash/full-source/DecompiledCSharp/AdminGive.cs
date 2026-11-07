using System;
using System.Collections.Generic;

public static class AdminGive
{
	public static void Register(AdminApi api)
	{
		api.Post("/give/v1/resource", (AdminCtx ctx) =>
		{
			Dictionary<string, object> form = AdminJson.AsDict(ctx.Body);
			if (form == null)
			{
				throw new AdminApiException("缺少发放数据");
			}
			long type = AdminJson.GetLong(form, "type", 0L);
			long id = AdminJson.GetLong(form, "id", 0L);
			long quantity = AdminJson.GetLong(form, "quantity", 0L);
			long playerId = AdminJson.GetLong(form, "playerId", 0L);
			return AdminMainThread.Run(() =>
			{
				DbStore dbStore = AdminBackend.RequireDb();
				if (type >= 7 && type <= 10)
				{
					throw new AdminApiException("倍率类（type 7-10）单机版未接入：大区倍率只影响世界列表显示，游戏内经验/掉落走本地倍率");
				}
				long characterId = ResolveCharacter(dbStore, playerId);
				long num = type;
				if ((ulong)num <= 13uL)
				{
					switch ((int)num)
					{
					case 0:
						AddCurrency(dbStore, characterId, "nxCredit", quantity);
						goto IL_0187;
					case 1:
						AddCurrency(dbStore, characterId, "nxPrepaid", quantity);
						goto IL_0187;
					case 2:
						AddCurrency(dbStore, characterId, "maplePoint", quantity);
						goto IL_0187;
					case 3:
						AddMesos(dbStore, characterId, quantity);
						goto IL_0187;
					case 4:
						AddExp(dbStore, characterId, quantity);
						goto IL_0187;
					case 5:
						GiveItem(dbStore, characterId, (int)id, (int)Math.Max(1L, quantity), form);
						goto IL_0187;
					case 6:
						GiveEquip(dbStore, characterId, (int)id, form);
						goto IL_0187;
					case 11:
						SetGm(dbStore, characterId, quantity);
						goto IL_0187;
					case 12:
						SetFame(dbStore, characterId, quantity);
						goto IL_0187;
					case 13:
						{
							Warp(dbStore, characterId, (id > 0) ? id : 100000000);
							goto IL_0187;
						}
						IL_0187:
						dbStore.Save();
						return (object)null;
					}
				}
				throw new AdminApiException("不支持的发放类型: " + type);
			});
		});
	}

	private static long ResolveCharacter(DbStore db, long playerId)
	{
		long num = 0L;
		try
		{
			num = ((AdminBackend.CurrentCharacterId != null) ? AdminBackend.CurrentCharacterId() : 0);
		}
		catch
		{
			num = 0L;
		}
		long num2 = ((playerId == 0L) ? num : playerId);
		if (num2 <= 0)
		{
			throw new AdminApiException("请指定角色（playerId），或先进入游戏再发给当前角色");
		}
		if (db.Table("characters").Find(DbStore.Eq("id", num2)) == null)
		{
			throw new AdminApiException("角色不存在: " + num2);
		}
		return num2;
	}

	private static void AddCurrency(DbStore db, long characterId, string column, long quantity)
	{
		long num = DbStore.Long(db.Table("characters").Find(DbStore.Eq("id", characterId)), "accountid", 0L);
		long num2 = DbStore.Long(db.Table("accounts").Find(DbStore.Eq("id", num)) ?? throw new AdminApiException("角色所属账号不存在: " + num), column, 0L) + quantity;
		if (num2 < 0)
		{
			num2 = 0L;
		}
		if (num2 > int.MaxValue)
		{
			num2 = 2147483647L;
		}
		db.Table("accounts").Update(DbStore.Eq("id", num), new DbRow { [column] = num2 });
	}

	private static void AddMesos(DbStore db, long characterId, long quantity)
	{
		long num = DbStore.Long(db.Table("characters").Find(DbStore.Eq("id", characterId)), "meso", 0L);
		if (GameSession.InGame && GameSession.CharacterId == characterId)
		{
			num = Profile.Data.gold;
		}
		long num2;
		if (quantity > 0 && num > long.MaxValue - quantity)
		{
			num2 = long.MaxValue;
		}
		else
		{
			num2 = ((quantity < 0 && quantity < -num) ? 0 : (num + quantity));
		}
		db.Table("characters").Update(DbStore.Eq("id", characterId), new DbRow { ["meso"] = num2 });
		SyncCharacterField(characterId, "meso", num2);
	}

	private static void AddExp(DbStore db, long characterId, long quantity)
	{
		DbRow r = db.Table("characters").Find(DbStore.Eq("id", characterId));
		long num = Math.Max(0L, DbStore.Long(r, "exp", 0L) + quantity);
		db.Table("characters").Update(DbStore.Eq("id", characterId), new DbRow { ["exp"] = num });
		SyncCharacterField(characterId, "exp", num);
	}

	private static void SetGm(DbStore db, long characterId, long level)
	{
		long num = Math.Max(0L, Math.Min(127L, level));
		db.Table("characters").Update(DbStore.Eq("id", characterId), new DbRow { ["gm"] = num });
		SyncCharacterField(characterId, "gm", num);
	}

	private static void SetFame(DbStore db, long characterId, long fame)
	{
		db.Table("characters").Update(DbStore.Eq("id", characterId), new DbRow { ["fame"] = Math.Max(0L, fame) });
	}

	private static void Warp(DbStore db, long characterId, long mapId)
	{
		db.Table("characters").Update(DbStore.Eq("id", characterId), new DbRow { ["map"] = mapId });
		AdminBackend.LiveWarpTo(characterId, mapId);
	}

	private static void GiveItem(DbStore db, long characterId, int itemId, int quantity, Dictionary<string, object> form)
	{
		if (itemId <= 0)
		{
			throw new AdminApiException("道具 ID 不能为空");
		}
		long num = InsertItem(db, characterId, itemId, quantity, form, out var _);
		if (itemId / 1000000 == 1)
		{
			InventoryDao.EnsureEquipment(db, num);
			if (SymbolCatalog.IsSymbol(itemId))
			{
				DbRow dbRow = db.Table("characters").Find((DbRow x) => DbStore.Long(x, "id", 0L) == characterId);
				NativeSymbolService.Normalize(InventoryDao.EquipmentOf(db, num), itemId, (dbRow != null) ? DbStore.Int(dbRow, "job") : 0);
			}
		}
		AdminBackend.LiveInventory(db, characterId, num, deleted: false);
	}

	private static void GiveEquip(DbStore db, long characterId, int itemId, Dictionary<string, object> form)
	{
		if (itemId <= 0)
		{
			throw new AdminApiException("装备 ID 不能为空");
		}
		long num = InsertItem(db, characterId, itemId, 1, form, out var _);
		InventoryDao.InsertEquipment(db, num, (int)AdminJson.GetLong(form, "str", 0L), (int)AdminJson.GetLong(form, "dex", 0L), (int)AdminJson.GetLong(form, "int", 0L), (int)AdminJson.GetLong(form, "luk", 0L), (int)AdminJson.GetLong(form, "hp", 0L), (int)AdminJson.GetLong(form, "mp", 0L), (int)AdminJson.GetLong(form, "pAtk", 0L), (int)AdminJson.GetLong(form, "mAtk", 0L), (int)AdminJson.GetLong(form, "pDef", 0L), (int)AdminJson.GetLong(form, "mDef", 0L), (int)AdminJson.GetLong(form, "acc", 0L), (int)AdminJson.GetLong(form, "avoid", 0L), (int)AdminJson.GetLong(form, "hands", 0L), (int)AdminJson.GetLong(form, "speed", 0L), (int)AdminJson.GetLong(form, "jump", 0L), (int)AdminJson.GetLong(form, "upgradeSlot", 0L));
		AdminBackend.LiveInventory(db, characterId, num, deleted: false);
	}

	private static long InsertItem(DbStore db, long characterId, int itemId, int quantity, Dictionary<string, object> form, out int invType)
	{
		invType = Math.Max(1, Math.Min(5, itemId / 1000000));
		int position = NextPosition(db, (int)characterId, invType);
		long num = InventoryDao.InsertItem(db, (int)characterId, itemId, invType, position, quantity);
		long num2 = AdminJson.GetLong(form, "expire", -1L);
		if (num2 != -1)
		{
			db.Table("inventoryitems").Update(DbStore.Eq("inventoryitemid", num), new DbRow { ["expiration"] = num2 });
		}
		return num;
	}

	private static int NextPosition(DbStore db, int characterId, int invType)
	{
		int num = NativeInventoryService.Free(db, CharactersDao.FindById(db, characterId), invType);
		if (num == 0)
		{
			throw new AdminApiException("背包空间不足");
		}
		return num;
	}

	private static void SyncCharacterField(long characterId, string field, long value)
	{
		try
		{
			if (GameSession.InGame && GameSession.CharacterId == (int)characterId && GameSession.Character != null && GameSession.Character.record != null)
			{
				if (field == "meso")
				{
					GameSession.Character.record.meso = value;
					Profile.Data.gold = value;
				}
				if (field == "exp")
				{
					GameSession.Character.record.exp = (int)value;
				}
				if (field == "gm")
				{
					GameSession.Character.record.gm = (int)value;
				}
			}
		}
		catch (Exception)
		{
		}
	}
}
