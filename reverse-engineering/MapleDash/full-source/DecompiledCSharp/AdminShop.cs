using System;
using System.Collections.Generic;

public static class AdminShop
{
	public static void Register(AdminApi api)
	{
		api.Post("/shop/v1/getShopList", GetShopList);
		api.Post("/shop/v1/getShopItemList", GetShopItemList);
		api.Get("/shop/v1/getShopItem/{id}", (AdminCtx ctx) =>
		{
			long id = ctx.ParamLong("id");
			return AdminMainThread.Run(() =>
			{
				DbStore db = AdminBackend.Db;
				DbRow dbRow = ((db != null && db.HasTable("shopitems")) ? db.Table("shopitems").Find(DbStore.Eq("shopitemid", id)) : null);
				return (dbRow != null) ? ItemDto(dbRow) : null;
			});
		});
		api.Put("/shop/v1/addShopItem", (AdminCtx ctx) => SaveItem(ctx, isAdd: true));
		api.Post("/shop/v1/updateShopItem", (AdminCtx ctx) => SaveItem(ctx, isAdd: false));
		api.Delete("/shop/v1/deleteShopItem/{id}", (AdminCtx ctx) =>
		{
			long id = ctx.ParamLong("id");
			return AdminMainThread.Run(() =>
			{
				DbStore dbStore = AdminBackend.RequireDb();
				if (dbStore.HasTable("shopitems"))
				{
					dbStore.Table("shopitems").Delete(DbStore.Eq("shopitemid", id));
					dbStore.Save();
				}
				return (object)null;
			});
		});
	}

	private static object GetShopList(AdminCtx ctx)
	{
		long shopId = ctx.BodyLong("shopId", -1L);
		long npcId = ctx.BodyLong("npcId", -1L);
		string npcName = ctx.BodyString("npcName");
		long itemId = ctx.BodyLong("itemId", -1L);
		string itemName = ctx.BodyString("itemName");
		int pageNo = ctx.BodyInt("pageNo", 1);
		int pageSize = ctx.BodyInt("pageSize", 20);
		bool onlyTotal = ctx.BodyBool("onlyTotal");
		bool notPage = ctx.BodyBool("notPage");
		return AdminMainThread.Run(() =>
		{
			DbStore db = AdminBackend.Db;
			Dictionary<long, long> dictionary = new Dictionary<long, long>();
			if (db != null && db.HasTable("shops"))
			{
				foreach (DbRow row in db.Table("shops").rows)
				{
					dictionary[DbStore.Long(row, "shopid", 0L)] = DbStore.Long(row, "npcid", 0L);
				}
			}
			if (itemId >= 0 || !string.IsNullOrEmpty(itemName))
			{
				HashSet<long> hashSet = new HashSet<long>();
				if (db != null && db.HasTable("shopitems"))
				{
					foreach (DbRow row2 in db.Table("shopitems").rows)
					{
						long num = DbStore.Long(row2, "itemid", 0L);
						if ((itemId < 0 || num == itemId) && (string.IsNullOrEmpty(itemName) || AdminNames.ItemName((int)num).IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0))
						{
							hashSet.Add(DbStore.Long(row2, "shopid", 0L));
						}
					}
				}
				Dictionary<long, long> dictionary2 = new Dictionary<long, long>();
				foreach (KeyValuePair<long, long> item in dictionary)
				{
					if (hashSet.Contains(item.Key))
					{
						dictionary2[item.Key] = item.Value;
					}
				}
				dictionary = dictionary2;
			}
			List<KeyValuePair<long, long>> list = new List<KeyValuePair<long, long>>();
			foreach (KeyValuePair<long, long> item2 in dictionary)
			{
				if ((shopId < 0 || item2.Key == shopId) && (npcId < 0 || item2.Value == npcId) && (string.IsNullOrEmpty(npcName) || AdminNames.NpcName((int)item2.Value).IndexOf(npcName, StringComparison.OrdinalIgnoreCase) >= 0))
				{
					list.Add(item2);
				}
			}
			list.Sort((KeyValuePair<long, long> a, KeyValuePair<long, long> b) => a.Key.CompareTo(b.Key));
			return AdminDbUtil.Page(list, pageNo, pageSize, onlyTotal, notPage, (KeyValuePair<long, long> kv) => AdminJson.Obj("shopId", kv.Key, "npcId", kv.Value, "npcName", AdminNames.NpcName((int)kv.Value)));
		});
	}

	private static object GetShopItemList(AdminCtx ctx)
	{
		long shopId = ctx.BodyLong("shopId", -1L);
		int pageNo = ctx.BodyInt("pageNo", 1);
		int pageSize = ctx.BodyInt("pageSize", 20);
		bool onlyTotal = ctx.BodyBool("onlyTotal");
		bool notPage = ctx.BodyBool("notPage");
		return AdminMainThread.Run(() =>
		{
			DbStore db = AdminBackend.Db;
			List<DbRow> list = new List<DbRow>();
			if (db != null && db.HasTable("shopitems"))
			{
				foreach (DbRow row in db.Table("shopitems").rows)
				{
					if (shopId < 0 || DbStore.Long(row, "shopid", 0L) == shopId)
					{
						list.Add(row);
					}
				}
			}
			list.Sort((DbRow a, DbRow b) =>
			{
				int num = DbStore.Long(a, "shopid", 0L).CompareTo(DbStore.Long(b, "shopid", 0L));
				return (num == 0) ? DbStore.Long(a, "position", 0L).CompareTo(DbStore.Long(b, "position", 0L)) : num;
			});
			return AdminDbUtil.Page(list, pageNo, pageSize, onlyTotal, notPage, ItemDto);
		});
	}

	private static Dictionary<string, object> ItemDto(DbRow row)
	{
		long num = DbStore.Long(row, "itemid", 0L);
		return AdminJson.Obj("id", DbStore.Long(row, "shopitemid", 0L), "shopId", DbStore.Long(row, "shopid", 0L), "itemId", num, "price", DbStore.Long(row, "price", 0L), "pitch", DbStore.Long(row, "pitch", 0L), "position", DbStore.Long(row, "position", 0L), "itemName", AdminNames.ItemName((int)num), "itemDesc", AdminNames.ItemDescription((int)num));
	}

	private static object SaveItem(AdminCtx ctx, bool isAdd)
	{
		Dictionary<string, object> form = AdminJson.AsDict(ctx.Body);
		if (form == null)
		{
			throw new AdminApiException("缺少商品数据");
		}
		long id = AdminJson.GetLong(form, "id", 0L);
		return AdminMainThread.Run((Func<object>)(() =>
		{
			DbStore dbStore = AdminBackend.RequireDb();
			if (!dbStore.HasTable("shopitems"))
			{
				throw new AdminApiException("商店表尚未初始化（请先升级存档）");
			}
			DbTable dbTable = dbStore.Table("shopitems");
			long num = AdminJson.GetLong(form, "shopId", 0L);
			if (num <= 0)
			{
				throw new AdminApiException("商店 ID 不能为空");
			}
			EnsureShop(dbStore, num, AdminJson.GetLong(form, "npcId", 0L));
			DbRow dbRow = new DbRow();
			AdminDbUtil.Set(dbRow, "shopid", num);
			AdminDbUtil.Set(dbRow, "itemid", AdminJson.GetLong(form, "itemId", 0L));
			AdminDbUtil.Set(dbRow, "price", AdminJson.GetLong(form, "price", 0L));
			AdminDbUtil.Set(dbRow, "pitch", AdminJson.GetLong(form, "pitch", 0L));
			AdminDbUtil.Set(dbRow, "position", AdminJson.GetLong(form, "position", 0L));
			long num2;
			if (isAdd || id <= 0)
			{
				num2 = dbTable.Insert(dbRow);
			}
			else
			{
				if (dbTable.Find(DbStore.Eq("shopitemid", id)) == null)
				{
					throw new AdminApiException("商品不存在: " + id);
				}
				dbTable.Update(DbStore.Eq("shopitemid", id), dbRow);
				num2 = id;
			}
			dbStore.Save();
			return num2;
		}), 8000);
	}

	private static void EnsureShop(DbStore db, long shopId, long npcId)
	{
		if (db.HasTable("shops"))
		{
			DbTable dbTable = db.Table("shops");
			if (dbTable.Find(DbStore.Eq("shopid", shopId)) == null)
			{
				dbTable.Insert(new DbRow
				{
					["shopid"] = shopId,
					["npcid"] = ((npcId > 0) ? npcId : 0)
				});
			}
		}
	}
}
