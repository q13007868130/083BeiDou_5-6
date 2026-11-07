using System;
using System.Collections.Generic;
using Server.Bot;

public static class NativeSoulService
{
	private static SoulResponse Reject(string error)
	{
		return new SoulResponse
		{
			ok = false,
			error = error
		};
	}

	public static SoulResponse Handle(DbStore db, SoulRequest request, Random random = null)
	{
		if (db == null || request == null)
		{
			return Reject("无效灵魂请求");
		}
		if (request.operation >= SoulOperation.ReadModern)
		{
			return NativeSoulWeaponWz2Service.Handle(db, request, random);
		}
		if (NativeSoulWeaponWz2Service.Read(db, request.characterId).unlocked)
		{
			return Reject("已切换为新版灵魂武器，请使用等级、升阶和烙印功能");
		}
		if (random == null)
		{
			random = new Random();
		}
		lock (db)
		{
			DbRow dbRow = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == request.characterId && DbStore.Long(x, "inventoryitemid", 0L) == request.inventoryItemId);
			if (dbRow == null)
			{
				return Reject("没有持有这件装备");
			}
			int itemId = DbStore.Int(dbRow, "itemid");
			if (NativeAndroidService.IsApSlot(DbStore.Int(dbRow, "position")))
			{
				return Reject("请先取下机器人换装部件");
			}
			long num = DbStore.Long(dbRow, "expiration", -1L);
			if (num > 0 && num <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
			{
				return Reject("装备已到期");
			}
			DbRow dbRow2 = InventoryDao.EquipmentOf(db, request.inventoryItemId);
			if (dbRow2 == null)
			{
				return Reject("装备模板缺失");
			}
			if (DbStore.Int(dbRow2, "locked") != 0 || (DbStore.Int(dbRow, "flag") & 1) != 0)
			{
				return Reject("装备已上锁");
			}
			if (!IsWeapon(itemId))
			{
				return Reject("只有武器可以开孔或镶魂");
			}
			GrowthState state = GrowthDao.Load(db, request.characterId);
			SoulResponse soulResponse = Snapshot(dbRow2, state);
			if (request.operation == SoulOperation.Preview)
			{
				return soulResponse;
			}
			DbRow dbRow3 = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == request.characterId && DbStore.Long(x, "inventoryitemid", 0L) == request.soulInventoryItemId);
			if (dbRow3 == null || DbStore.Int(dbRow3, "quantity") < 1 || DbStore.Int(dbRow3, "position") <= 0)
			{
				return Reject("道具不存在");
			}
			int soulItemId = DbStore.Int(dbRow3, "itemid");
			return (request.operation == SoulOperation.Socket) ? Socket(db, request, dbRow2, dbRow3, soulItemId, itemId, soulResponse, random) : Enchant(db, request, dbRow2, dbRow3, soulItemId, ResolveOption(soulItemId, random), soulResponse);
		}
	}

	private static SoulResponse Socket(DbStore db, SoulRequest request, DbRow equipment, DbRow soul, int soulItemId, int itemId, SoulResponse response, Random random)
	{
		if (soulItemId / 1000 != 2590)
		{
			return Reject("这不是灵魂开孔器");
		}
		if (DbStore.Int(equipment, "soulSocketId") != 0)
		{
			return Reject("这件武器已经有灵魂插槽了");
		}
		if (BotItemCatalog.Int(BotItemCatalog.Shared.Info(itemId) ?? new Dictionary<string, object>(), "reqLevel") + DbStore.Int(equipment, "iIncReq") < 75)
		{
			return Reject("武器等级不足 " + 75 + " 级，无法开孔");
		}
		response.success = false;
		if (!request.commit)
		{
			response.ok = true;
			return response;
		}
		int num = ScrollSuccess(soulItemId);
		bool flag = num >= 100 || random.Next(100) < num;
		db.BeginTransaction();
		try
		{
			Consume(db, soul, request.soulInventoryItemId);
			if (flag)
			{
				equipment["soulSocketId"] = (long)(soulItemId % 2590000);
			}
			db.Save();
			db.Commit();
		}
		catch
		{
			db.Rollback();
			throw;
		}
		response.success = flag;
		response.socketId = DbStore.Int(equipment, "soulSocketId");
		response.inventory = CharService.Load(db, request.characterId).inventory;
		return response;
	}

	private static SoulResponse Enchant(DbStore db, SoulRequest request, DbRow equipment, DbRow soul, int soulItemId, int optionOverride, SoulResponse response)
	{
		if (soulItemId / 1000 != 2591)
		{
			return Reject("这不是灵魂石");
		}
		if (DbStore.Int(equipment, "soulSocketId") == 0)
		{
			return Reject("这件武器还没有灵魂插槽");
		}
		if (optionOverride <= 0 || SoulCatalog.Of(optionOverride) == null)
		{
			return Reject("这颗灵魂石还没有属性数据");
		}
		int num = SoulCatalog.SoulIdOf(soulItemId);
		if (!request.commit)
		{
			response.soulId = num;
			response.soulItemId = soulItemId;
			response.optionCode = optionOverride;
			ApplyOptionPreview(response, optionOverride);
			return response;
		}
		db.BeginTransaction();
		try
		{
			Consume(db, soul, request.soulInventoryItemId);
			equipment["soulOptionId"] = (long)num;
			equipment["soulItemId"] = (long)soulItemId;
			equipment["soulOption"] = (long)optionOverride;
			db.Save();
			db.Commit();
		}
		catch
		{
			db.Rollback();
			throw;
		}
		response.soulId = num;
		response.soulItemId = soulItemId;
		response.optionCode = optionOverride;
		ApplyOptionPreview(response, optionOverride);
		response.inventory = CharService.Load(db, request.characterId).inventory;
		return response;
	}

	private static SoulResponse Snapshot(DbRow equipment, GrowthState state)
	{
		int num = DbStore.Int(equipment, "soulOption");
		SoulResponse soulResponse = new SoulResponse
		{
			socketId = DbStore.Int(equipment, "soulSocketId"),
			soulId = DbStore.Int(equipment, "soulOptionId"),
			soulItemId = DbStore.Int(equipment, "soulItemId"),
			optionCode = num,
			soulMp = state.soulMp,
			soulMax = state.soulMax
		};
		ApplyOptionPreview(soulResponse, num);
		return soulResponse;
	}

	private static void ApplyOptionPreview(SoulResponse response, int code)
	{
		SoulOption soulOption = SoulCatalog.Of(code);
		if (soulOption != null)
		{
			response.optionKey = soulOption.key;
			response.optionValue = soulOption.value;
		}
	}

	private static void Consume(DbStore db, DbRow row, long inventoryItemId)
	{
		int num = DbStore.Int(row, "quantity");
		if (num <= 1)
		{
			CharService.RemoveItem(db, inventoryItemId);
		}
		else
		{
			row["quantity"] = (long)num - 1L;
		}
	}

	private static int ResolveOption(int soulItemId, Random random)
	{
		int num = SoulCatalog.OptionFor(soulItemId);
		if (num <= 0)
		{
			return SoulCatalog.FallbackOption(random.Next(SoulCatalog.FallbackPotList.Length));
		}
		return num;
	}

	private static int ScrollSuccess(int soulItemId)
	{
		Dictionary<string, object> dictionary = BotItemCatalog.Shared.Info(soulItemId);
		if (dictionary == null)
		{
			return 100;
		}
		int num = BotItemCatalog.Int(dictionary, "success");
		if (num > 0)
		{
			return num;
		}
		return 100;
	}

	public static bool IsWeapon(int itemId)
	{
		int num = itemId / 10000;
		if (num >= 121)
		{
			return num <= 160;
		}
		return false;
	}
}
