using System;
using System.Collections.Generic;
using System.Globalization;

public static class AdminCatalog
{
	private static DbStore Db
	{
		get
		{
			if (AdminBackend.DbOverride != null)
			{
				return AdminBackend.DbOverride;
			}
			return GameServer.Local?.Db;
		}
	}

	public static void Register(AdminApi api)
	{
		api.Post("/common/v1/informationSearch", (AdminCtx ctx) => AdminCatalogData.InformationSearch(ctx.Body));
		RegisterItemCatalog(api);
		RegisterEquipmentCatalog(api);
		RegisterMobCatalog(api);
		RegisterMobDrops(api);
		RegisterSkillCatalog(api);
		RegisterIcons(api);
	}

	private static int? QueryNullableInt(AdminCtx ctx, string key)
	{
		string text = ctx.Query(key);
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return null;
		}
		return result;
	}

	private static bool? QueryNullableBool(AdminCtx ctx, string key)
	{
		string text = ctx.Query(key);
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		if (text == "1" || string.Equals(text, "true", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (text == "0" || string.Equals(text, "false", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		return null;
	}

	private static void RegisterItemCatalog(AdminApi api)
	{
		api.Get("/setItem/v1/item/catalog", (AdminCtx ctx) => AdminCatalogData.ItemCatalog(ctx.Query("keyword"), ctx.Query("category"), ctx.QueryInt("pageNo", 1), ctx.QueryInt("pageSize", 60)).ToDict());
	}

	private static void RegisterEquipmentCatalog(AdminApi api)
	{
		api.Get("/setItem/v1/equipment/catalog", (AdminCtx ctx) => AdminCatalogData.EquipmentCatalog(ctx.Query("keyword"), ctx.Query("category"), ctx.QueryInt("pageNo", 1), ctx.QueryInt("pageSize", 60), QueryNullableBool(ctx, "cash"), ctx.Query("weaponType"), QueryNullableInt(ctx, "job"), QueryNullableInt(ctx, "minLevel"), QueryNullableInt(ctx, "maxLevel")).ToDict());
	}

	private static void RegisterMobCatalog(AdminApi api)
	{
		api.Get("/mob/v1/catalog", (AdminCtx ctx) => AdminCatalogData.MobCatalog(ctx.Query("keyword"), ctx.QueryInt("pageNo", 1), ctx.QueryInt("pageSize", 60), QueryNullableInt(ctx, "minLevel"), QueryNullableInt(ctx, "maxLevel")).ToDict());
	}

	private static void RegisterSkillCatalog(AdminApi api)
	{
		api.Get("/skill/v1/catalog", (AdminCtx ctx) => AdminCatalogData.SkillCatalog(ctx.Query("keyword"), QueryNullableInt(ctx, "jobId"), QueryNullableInt(ctx, "advancement"), ctx.QueryInt("pageNo", 1), ctx.QueryInt("pageSize", 120)).ToDict());
		api.Get("/skill/v1/detail/{skillId}", (AdminCtx ctx) => AdminCatalogData.SkillDetail(ctx.ParamLong("skillId")));
		api.Get("/skill/v1/jobs", (AdminCtx ctx) => AdminCatalogData.SkillJobs());
		api.Post("/skill/v1/reload", (AdminCtx ctx) => AdminCatalogData.SkillReload());
	}

	private static void RegisterIcons(AdminApi api)
	{
		api.Get("/assets/item-icons/{file}", (AdminCtx ctx) => IconResponse(ctx, AdminCatalogData.ItemIconPng), auth: false);
		api.Get("/assets/equipment-icons/{file}", (AdminCtx ctx) => IconResponse(ctx, AdminCatalogData.EquipmentIconPng), auth: false);
		api.Get("/assets/mob-icons/{file}", (AdminCtx ctx) => IconResponse(ctx, AdminCatalogData.MobIconPng), auth: false);
		api.Get("/assets/npc-icons/{file}", (AdminCtx ctx) => IconResponse(ctx, AdminCatalogData.NpcIconPng), auth: false);
		api.Get("/assets/map-icons/{file}", (AdminCtx ctx) => IconResponse(ctx, AdminCatalogData.MapIconPng), auth: false);
		api.Get("/assets/skill-icons/{file}", (AdminCtx ctx) => IconResponse(ctx, AdminCatalogData.SkillIconPng), auth: false);
	}

	private static AdminHttpResponse IconResponse(AdminCtx ctx, Func<long, byte[]> encode)
	{
		string text = ctx.Param("file") ?? "";
		if (text.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring(0, text.Length - 4);
		}
		if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) || result <= 0)
		{
			return NotFound();
		}
		byte[] array = encode(result);
		if (array != null && array.Length != 0)
		{
			return AdminHttpResponse.Bytes(array, "image/png");
		}
		return NotFound();
	}

	private static AdminHttpResponse NotFound()
	{
		return AdminHttpResponse.Json(AdminJson.Fail("not found", 404), 404);
	}

	private static void RegisterMobDrops(AdminApi api)
	{
		api.Get("/mob/v1/{mobId}/drops", (AdminCtx ctx) =>
		{
			long mobId = ctx.ParamLong("mobId");
			return AdminMainThread.Run(() =>
			{
				List<object> list = new List<object>();
				DbStore db = Db;
				if (db == null || !db.HasTable("drop_data"))
				{
					return list;
				}
				List<DbRow> list2 = db.Table("drop_data").Select(DbStore.Eq("dropperid", mobId));
				list2.Sort((DbRow a, DbRow b) => DbStore.Long(a, "id", 0L).CompareTo(DbStore.Long(b, "id", 0L)));
				foreach (DbRow item in list2)
				{
					list.Add(DropDto(item));
				}
				return list;
			});
		});
		api.Put("/mob/v1/{mobId}/drops", (AdminCtx ctx) =>
		{
			long mobId = ctx.ParamLong("mobId");
			Dictionary<string, object> form = UnwrapInner(AdminJson.AsDict(ctx.Body));
			if (form == null)
			{
				throw new AdminApiException("缺少掉落数据");
			}
			long itemId = AdminJson.GetLong(form, "itemId", 0L);
			return AdminMainThread.Run(() =>
			{
				DbStore dbStore = RequireDb();
				if (!dbStore.HasTable("drop_data"))
				{
					throw new AdminApiException("掉落表 drop_data 尚未初始化（请先升级存档）");
				}
				DbTable dbTable = dbStore.Table("drop_data");
				DbRow dbRow = new DbRow
				{
					["dropperid"] = mobId,
					["itemid"] = itemId,
					["minimum_quantity"] = AdminJson.GetLong(form, "minimumQuantity", 0L),
					["maximum_quantity"] = AdminJson.GetLong(form, "maximumQuantity", 0L),
					["questid"] = AdminJson.GetLong(form, "questId", 0L),
					["chance"] = AdminJson.GetLong(form, "chance", 0L)
				};
				if (dbTable.Col("comments") != null)
				{
					dbRow["comments"] = AdminJson.GetString(form, "comments");
				}
				long result = dbTable.Insert(dbRow);
				dbStore.Save();
				DropStore.Invalidate();
				return result;
			});
		});
		api.Delete("/mob/v1/drops/{dropId}", (AdminCtx ctx) =>
		{
			long dropId = ctx.ParamLong("dropId");
			return AdminMainThread.Run(() =>
			{
				DbStore dbStore = RequireDb();
				if (dbStore.HasTable("drop_data"))
				{
					dbStore.Table("drop_data").Delete(DbStore.Eq("id", dropId));
					dbStore.Save();
					DropStore.Invalidate();
				}
				return (object)null;
			});
		});
	}

	private static Dictionary<string, object> UnwrapInner(Dictionary<string, object> body)
	{
		if (body == null)
		{
			return null;
		}
		if (body.TryGetValue("data", out var value) && value is Dictionary<string, object> result)
		{
			return result;
		}
		return body;
	}

	private static Dictionary<string, object> DropDto(DbRow row)
	{
		long num = DbStore.Long(row, "itemid", 0L);
		long num2 = DbStore.Long(row, "questid", 0L);
		return AdminJson.Obj("id", DbStore.Long(row, "id", 0L), "dropperId", DbStore.Long(row, "dropperid", 0L), "dropperName", WzNames.Mob((int)DbStore.Long(row, "dropperid", 0L)), "itemId", num, "itemName", (num > 0) ? WzNames.Item((int)num) : "", "itemCategory", ItemCategoryOf(num), "minimumQuantity", DbStore.Long(row, "minimum_quantity", 0L), "maximumQuantity", DbStore.Long(row, "maximum_quantity", 0L), "questId", num2, "questName", AdminCatalogData.QuestName(num2), "chance", DbStore.Long(row, "chance", 0L), "comments", DbStore.Str(row, "comments"));
	}

	private static string ItemCategoryOf(long itemId)
	{
		long num = itemId / 1000000;
		if ((ulong)num <= 5uL)
		{
			switch ((int)num)
			{
			case 0:
				return "金币";
			case 1:
				return "装备";
			case 2:
				return "消耗";
			case 3:
				return "设置";
			case 4:
				return "其他";
			case 5:
				return "特殊";
			}
		}
		return "未知";
	}

	private static DbStore RequireDb()
	{
		return Db ?? throw new AdminApiException("服务端未启动（请先进入游戏）");
	}
}
