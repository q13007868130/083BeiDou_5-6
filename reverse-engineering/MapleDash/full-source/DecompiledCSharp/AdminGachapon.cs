using System;
using System.Collections.Generic;

public static class AdminGachapon
{
	public static void Register(AdminApi api)
	{
		api.Post("/gachapon/v1/getPools", GetPools);
		api.Post("/gachapon/v1/updatePool", UpdatePool);
		api.Post("/gachapon/v1/deletePool", (AdminCtx ctx) =>
		{
			long id = AdminJson.GetLong(AdminJson.AsDict(ctx.Body), "id", 0L);
			return AdminMainThread.Run(() =>
			{
				DbStore dbStore = AdminBackend.RequireDb();
				if (dbStore.HasTable("gachapon_reward_pool"))
				{
					dbStore.Table("gachapon_reward_pool").Delete(DbStore.Eq("id", id));
				}
				if (dbStore.HasTable("gachapon_reward"))
				{
					dbStore.Table("gachapon_reward").Delete(DbStore.Eq("pool_id", id));
				}
				dbStore.Save();
				return (object)null;
			});
		});
		api.Post("/gachapon/v1/getRewards", GetRewards);
		api.Post("/gachapon/v1/updateReward", UpdateReward);
		api.Post("/gachapon/v1/deleteReward", (AdminCtx ctx) =>
		{
			long id = AdminJson.GetLong(AdminJson.AsDict(ctx.Body), "id", 0L);
			return AdminMainThread.Run(() =>
			{
				DbStore dbStore = AdminBackend.RequireDb();
				if (dbStore.HasTable("gachapon_reward"))
				{
					dbStore.Table("gachapon_reward").Delete(DbStore.Eq("id", id));
					dbStore.Save();
				}
				return (object)null;
			});
		});
	}

	private static object GetPools(AdminCtx ctx)
	{
		long? gachaponId = ((ctx.BodyLong("gachaponId", long.MinValue) == long.MinValue) ? ((long?)null) : new long?(ctx.BodyLong("gachaponId", 0L)));
		int pageNo = ctx.BodyInt("pageNo", 1);
		int pageSize = ctx.BodyInt("pageSize", 20);
		bool onlyTotal = ctx.BodyBool("onlyTotal");
		bool notPage = ctx.BodyBool("notPage");
		return AdminMainThread.Run(() =>
		{
			DbStore db = AdminBackend.Db;
			List<DbRow> list = new List<DbRow>();
			if (db != null && db.HasTable("gachapon_reward_pool"))
			{
				foreach (DbRow row in db.Table("gachapon_reward_pool").rows)
				{
					if (!gachaponId.HasValue || DbStore.Long(row, "gachapon_id", 0L) == gachaponId.Value || DbStore.Long(row, "is_public", 0L) != 0L)
					{
						list.Add(row);
					}
				}
			}
			list.Sort((DbRow a, DbRow b) =>
			{
				int num = DbStore.Long(b, "is_public", 0L).CompareTo(DbStore.Long(a, "is_public", 0L));
				return (num == 0) ? DbStore.Long(a, "id", 0L).CompareTo(DbStore.Long(b, "id", 0L)) : num;
			});
			List<Dictionary<string, object>> list2 = new List<Dictionary<string, object>>();
			foreach (DbRow item in list)
			{
				list2.Add(PoolDto(item, 0L));
			}
			if (gachaponId.HasValue && gachaponId.Value != -1)
			{
				SetRealProb(list2);
			}
			return AdminDbUtil.Page(list2, pageNo, pageSize, onlyTotal, notPage, (Dictionary<string, object> d) => d);
		});
	}

	private static Dictionary<string, object> PoolDto(DbRow row, long realProb)
	{
		long num = DbStore.Long(row, "gachapon_id", 0L);
		return AdminJson.Obj("id", DbStore.Long(row, "id", 0L), "name", DbStore.Str(row, "name"), "gachaponId", num, "gachaponName", (num > 0) ? AdminNames.NpcName((int)num) : "", "weight", DbStore.Long(row, "weight", 0L), "isPublic", DbStore.Long(row, "is_public", 0L) != 0, "prob", DbStore.Long(row, "prob", 0L), "startTime", DbStore.Str(row, "start_time"), "endTime", DbStore.Str(row, "end_time"), "notification", DbStore.Long(row, "notification", 0L) != 0, "comment", DbStore.Str(row, "comment"), "realProb", realProb);
	}

	private static void SetRealProb(List<Dictionary<string, object>> pools)
	{
		long num = 0L;
		long num2 = 0L;
		foreach (Dictionary<string, object> pool in pools)
		{
			num += AdminJson.GetLong(pool, "prob", 0L);
			num2 += AdminJson.GetLong(pool, "weight", 0L);
		}
		long num3 = 1000000 - 100 * num;
		foreach (Dictionary<string, object> pool2 in pools)
		{
			if (AdminJson.GetBool(pool2, "isPublic"))
			{
				pool2["realProb"] = AdminJson.GetLong(pool2, "prob", 0L) * 100;
			}
			else
			{
				pool2["realProb"] = ((num2 > 0) ? ((long)Math.Round((double)num3 * (double)AdminJson.GetLong(pool2, "weight", 0L) / (double)num2)) : 0);
			}
		}
	}

	private static object UpdatePool(AdminCtx ctx)
	{
		Dictionary<string, object> form = AdminJson.AsDict(ctx.Body);
		if (form == null)
		{
			throw new AdminApiException("缺少奖池数据");
		}
		string startTime = AdminJson.GetString(form, "startTime");
		if (string.IsNullOrEmpty(startTime))
		{
			throw new AdminApiException("生效时间不能为空");
		}
		bool isPublic = AdminJson.GetBool(form, "isPublic");
		long prob = AdminJson.GetLong(form, "prob", 0L);
		long gachaponId = AdminJson.GetLong(form, "gachaponId", 0L);
		long weight = AdminJson.GetLong(form, "weight", 0L);
		if (isPublic)
		{
			if (!form.ContainsKey("prob") || AdminJson.Get(form, "prob") == null)
			{
				throw new AdminApiException("公共奖池中奖率不能为空");
			}
			gachaponId = -1L;
			weight = 0L;
		}
		else
		{
			prob = 0L;
			if (gachaponId == 0L && AdminJson.Get(form, "gachaponId") == null)
			{
				throw new AdminApiException("百宝箱ID不能为空");
			}
			if (AdminJson.Get(form, "weight") == null)
			{
				throw new AdminApiException("奖池权重不能为空");
			}
		}
		long id = AdminJson.GetLong(form, "id", 0L);
		return AdminMainThread.Run((Func<object>)(() =>
		{
			DbStore dbStore = AdminBackend.RequireDb();
			DbTable dbTable = RequireTable(dbStore, "gachapon_reward_pool");
			DbRow dbRow = new DbRow();
			AdminDbUtil.Set(dbRow, "name", AdminJson.GetString(form, "name"));
			AdminDbUtil.Set(dbRow, "gachapon_id", gachaponId);
			AdminDbUtil.Set(dbRow, "weight", weight);
			AdminDbUtil.Set(dbRow, "is_public", (long)(isPublic ? 1 : 0));
			AdminDbUtil.Set(dbRow, "prob", prob);
			AdminDbUtil.Set(dbRow, "start_time", startTime);
			AdminDbUtil.Set(dbRow, "end_time", AdminJson.GetString(form, "endTime"));
			AdminDbUtil.Set(dbRow, "notification", (long)(AdminJson.GetBool(form, "notification") ? 1 : 0));
			AdminDbUtil.Set(dbRow, "comment", AdminJson.GetString(form, "comment"));
			if (id > 0 && dbTable.Find(DbStore.Eq("id", id)) != null)
			{
				dbTable.Update(DbStore.Eq("id", id), dbRow);
			}
			else
			{
				id = dbTable.Insert(dbRow);
			}
			dbStore.Save();
			return id;
		}), 8000);
	}

	private static object GetRewards(AdminCtx ctx)
	{
		long poolId = AdminJson.GetLong(AdminJson.AsDict(ctx.Body), "id", 0L);
		return AdminMainThread.Run((Func<object>)(() =>
		{
			DbStore db = AdminBackend.Db;
			List<object> list = new List<object>();
			if (db == null || !db.HasTable("gachapon_reward"))
			{
				return list;
			}
			List<DbRow> list2 = db.Table("gachapon_reward").Select(DbStore.Eq("pool_id", poolId));
			list2.Sort((DbRow a, DbRow b) => DbStore.Long(a, "item_id", 0L).CompareTo(DbStore.Long(b, "item_id", 0L)));
			foreach (DbRow item in list2)
			{
				long num = DbStore.Long(item, "item_id", 0L);
				list.Add(AdminJson.Obj("id", DbStore.Long(item, "id", 0L), "poolId", DbStore.Long(item, "pool_id", 0L), "itemId", num, "itemName", AdminNames.ItemName((int)num), "quantity", DbStore.Long(item, "quantity", 1L), "createTime", DbStore.Str(item, "create_time"), "comment", DbStore.Str(item, "comment")));
			}
			return list;
		}), 8000);
	}

	private static object UpdateReward(AdminCtx ctx)
	{
		Dictionary<string, object> form = AdminJson.AsDict(ctx.Body);
		if (form == null)
		{
			throw new AdminApiException("缺少奖品数据");
		}
		long itemId = AdminJson.GetLong(form, "itemId", 0L);
		if (itemId <= 0)
		{
			return null;
		}
		long id = AdminJson.GetLong(form, "id", 0L);
		return AdminMainThread.Run(() =>
		{
			DbStore dbStore = AdminBackend.RequireDb();
			DbTable dbTable = RequireTable(dbStore, "gachapon_reward");
			DbRow dbRow = new DbRow();
			AdminDbUtil.Set(dbRow, "pool_id", AdminJson.GetLong(form, "poolId", 0L));
			AdminDbUtil.Set(dbRow, "item_id", itemId);
			AdminDbUtil.Set(dbRow, "quantity", AdminJson.GetLong(form, "quantity", 1L));
			AdminDbUtil.Set(dbRow, "comment", AdminJson.GetString(form, "comment"));
			string text = AdminJson.GetString(form, "createTime");
			AdminDbUtil.Set(dbRow, "create_time", string.IsNullOrEmpty(text) ? DbStore.NowText() : text);
			if (id > 0 && dbTable.Find(DbStore.Eq("id", id)) != null)
			{
				dbTable.Update(DbStore.Eq("id", id), dbRow);
			}
			else
			{
				dbTable.Insert(dbRow);
			}
			dbStore.Save();
			return (object)null;
		});
	}

	private static DbTable RequireTable(DbStore db, string name)
	{
		if (!db.HasTable(name))
		{
			throw new AdminApiException("扭蛋表 " + name + " 尚未初始化（请先升级存档）");
		}
		return db.Table(name);
	}
}
