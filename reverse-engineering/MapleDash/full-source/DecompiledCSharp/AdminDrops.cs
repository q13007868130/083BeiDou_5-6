using System;
using System.Collections.Generic;

public static class AdminDrops
{
	private sealed class ViewRow
	{
		public int owner;

		public DropEntry e;

		public bool global;

		public long dbId => e.id;
	}

	public static void Register(AdminApi api)
	{
		api.Post("/drop/v1/getDropList", (AdminCtx ctx) => Query(ctx, global: false));
		api.Post("/drop/v1/getGlobalDropList", (AdminCtx ctx) => Query(ctx, global: true));
		api.Put("/drop/v1/addDropData", (AdminCtx ctx) => Save(ctx, global: false, isAdd: true));
		api.Put("/drop/v1/addGlobalDropData", (AdminCtx ctx) => Save(ctx, global: true, isAdd: true));
		api.Post("/drop/v1/updateDropData", (AdminCtx ctx) => Save(ctx, global: false, isAdd: false));
		api.Post("/drop/v1/updateGlobalDropData", (AdminCtx ctx) => Save(ctx, global: true, isAdd: false));
		api.Delete("/drop/v1/deleteDropData/{id}", (AdminCtx ctx) => Delete(ctx, global: false));
		api.Delete("/drop/v1/deleteGlobalDropData/{id}", (AdminCtx ctx) => Delete(ctx, global: true));
	}

	private static string Table(bool global)
	{
		if (!global)
		{
			return "drop_data";
		}
		return "drop_data_global";
	}

	private static object Query(AdminCtx ctx, bool global)
	{
		long dropperId = ctx.BodyLong("dropperId", 0L);
		string dropperName = ctx.BodyString("dropperName");
		long continent = ctx.BodyLong("continent", -2L);
		long itemId = ctx.BodyLong("itemId", -1L);
		string itemName = ctx.BodyString("itemName");
		long questId = ctx.BodyLong("questId", -1L);
		int pageNo = ctx.BodyInt("pageNo", 1);
		int pageSize = ctx.BodyInt("pageSize", 20);
		bool onlyTotal = ctx.BodyBool("onlyTotal");
		bool notPage = ctx.BodyBool("notPage");
		return AdminMainThread.Run((Func<Dictionary<string, object>>)(() =>
		{
			List<ViewRow> list = View(global);
			Dictionary<int, string> mobNames = new Dictionary<int, string>();
			Dictionary<int, string> itemNames = new Dictionary<int, string>();
			List<ViewRow> list2 = new List<ViewRow>();
			foreach (ViewRow item in list)
			{
				if ((global || dropperId <= 0 || item.owner == dropperId) && (!global || continent == -2 || item.owner == continent) && (itemId < 0 || item.e.item == itemId) && (questId < 0 || item.e.quest == questId) && (string.IsNullOrEmpty(dropperName) || global || MobName(item.owner).IndexOf(dropperName, StringComparison.OrdinalIgnoreCase) >= 0) && (string.IsNullOrEmpty(itemName) || ItemName(item.e.item).IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0))
				{
					list2.Add(item);
				}
			}
			list2.Sort((ViewRow a, ViewRow b) =>
			{
				int num = a.owner.CompareTo(b.owner);
				if (num != 0)
				{
					return num;
				}
				num = a.e.item.CompareTo(b.e.item);
				return (num != 0) ? num : a.dbId.CompareTo(b.dbId);
			});
			return AdminDbUtil.Page(list2, pageNo, pageSize, onlyTotal, notPage, (ViewRow row) => Dto(row, global));
			string ItemName(int id)
			{
				if (!itemNames.TryGetValue(id, out var value))
				{
					value = (itemNames[id] = AdminNames.ItemName(id));
				}
				return value;
			}
			string MobName(int id)
			{
				if (!mobNames.TryGetValue(id, out var value))
				{
					value = (mobNames[id] = AdminNames.MobName(id));
				}
				return value;
			}
		}), 8000);
	}

	private static List<ViewRow> View(bool global)
	{
		List<ViewRow> list = new List<ViewRow>();
		if (global)
		{
			foreach (DropEntry item in DropStore.Global())
			{
				list.Add(new ViewRow
				{
					owner = (int)item.continent,
					e = item,
					global = true
				});
			}
			return list;
		}
		foreach (KeyValuePair<int, List<DropEntry>> item2 in DropStore.All())
		{
			foreach (DropEntry item3 in item2.Value)
			{
				list.Add(new ViewRow
				{
					owner = item2.Key,
					e = item3,
					global = false
				});
			}
		}
		return list;
	}

	private static Dictionary<string, object> Dto(ViewRow v, bool global)
	{
		long num = v.e.item;
		return AdminJson.Obj("id", RowKey(v), "dbId", v.dbId, "source", (v.dbId > 0) ? "override" : "baseline", "dropperId", (long)(global ? 0 : v.owner), "dropperName", global ? "" : AdminNames.MobName(v.owner), "continent", (long)(global ? v.owner : (-1)), "itemId", num, "itemName", AdminNames.ItemName((int)num), "itemCategory", AdminNames.ItemCategory((int)num), "minimumQuantity", (long)v.e.min, "maximumQuantity", (long)v.e.max, "questId", (long)v.e.quest, "questName", AdminNames.QuestName(v.e.quest), "chance", (long)v.e.chance, "comments", v.e.comments ?? "");
	}

	private static long RowKey(ViewRow v)
	{
		if (v.dbId > 0)
		{
			return v.dbId;
		}
		long num = (long)v.owner & 0xFFFFFFL;
		long num2 = (long)v.e.item & 0xFFFFFFL;
		return -(((num << 24) | num2) + 1);
	}

	private static object Save(AdminCtx ctx, bool global, bool isAdd)
	{
		Dictionary<string, object> form = AdminJson.AsDict(ctx.Body);
		if (form == null)
		{
			throw new AdminApiException("缺少掉落数据");
		}
		long id = AdminJson.GetLong(form, "id", 0L);
		long dropperId = (global ? 0 : AdminJson.GetLong(form, "dropperId", 0L));
		long continent = (global ? AdminJson.GetLong(form, "continent", -1L) : 0);
		long itemId = AdminJson.GetLong(form, "itemId", 0L);
		return AdminMainThread.Run((Func<object>)(() =>
		{
			DbStore dbStore = AdminBackend.RequireDb();
			if (!dbStore.HasTable(Table(global)))
			{
				throw new AdminApiException("掉落表 " + Table(global) + " 尚未初始化（请先升级存档）");
			}
			DbTable dbTable = dbStore.Table(Table(global));
			DbRow existing = null;
			if (id > 0)
			{
				existing = dbTable.Find(DbStore.Eq("id", id));
			}
			if (existing == null)
			{
				existing = dbTable.Find(KeyFilter(global, global ? continent : dropperId, itemId));
			}
			DbRow dbRow = new DbRow();
			if (!global)
			{
				AdminDbUtil.Set(dbRow, "dropperid", dropperId);
			}
			if (global)
			{
				AdminDbUtil.Set(dbRow, "continent", continent);
			}
			AdminDbUtil.Set(dbRow, "itemid", itemId);
			AdminDbUtil.Set(dbRow, "minimum_quantity", AdminJson.GetLong(form, "minimumQuantity", 1L));
			AdminDbUtil.Set(dbRow, "maximum_quantity", AdminJson.GetLong(form, "maximumQuantity", 1L));
			AdminDbUtil.Set(dbRow, "questid", AdminJson.GetLong(form, "questId", 0L));
			AdminDbUtil.Set(dbRow, "chance", AdminJson.GetLong(form, "chance", 0L));
			if (dbTable.Col("comments") != null)
			{
				AdminDbUtil.Set(dbRow, "comments", AdminJson.GetString(form, "comments"));
			}
			long num;
			if (existing != null)
			{
				dbTable.Update((DbRow r) => r == existing, dbRow);
				num = DbStore.Long(existing, "id", 0L);
			}
			else
			{
				num = dbTable.Insert(dbRow);
			}
			dbStore.Save();
			DropStore.Invalidate();
			return num;
		}), 8000);
	}

	private static object Delete(AdminCtx ctx, bool global)
	{
		long id = ctx.ParamLong("id");
		long dropperId = ctx.QueryLong("dropperId", -1L);
		long itemId = ctx.QueryLong("itemId", -1L);
		long continent = ctx.QueryLong("continent", -2L);
		return AdminMainThread.Run(() =>
		{
			DbStore dbStore = AdminBackend.RequireDb();
			if (!dbStore.HasTable(Table(global)))
			{
				return (object)null;
			}
			DbTable dbTable = dbStore.Table(Table(global));
			DbRow row = null;
			if (id > 0)
			{
				row = dbTable.Find(DbStore.Eq("id", id));
			}
			if (row == null && itemId >= 0 && (global ? (continent != -2) : (dropperId > 0)))
			{
				row = dbTable.Find(KeyFilter(global, global ? continent : dropperId, itemId));
			}
			long num = ((row == null) ? (global ? continent : dropperId) : (global ? DbStore.Long(row, "continent", -1L) : DbStore.Long(row, "dropperid", 0L)));
			long num2 = ((row != null) ? DbStore.Long(row, "itemid", 0L) : itemId);
			if (num2 < 0)
			{
				return (object)null;
			}
			if (!global && num <= 0)
			{
				return (object)null;
			}
			DropEntry dropEntry = DropStore.BaselineEntry(global, (int)num, (int)num2);
			if (row == null && dropEntry == null)
			{
				return (object)null;
			}
			if (dropEntry == null && row != null)
			{
				dbTable.Delete((DbRow r) => r == row);
			}
			else if (row != null)
			{
				dbTable.Update((DbRow r) => r == row, new DbRow { ["chance"] = 0L });
			}
			else
			{
				DbRow dbRow = new DbRow();
				if (!global)
				{
					AdminDbUtil.Set(dbRow, "dropperid", num);
				}
				if (global)
				{
					AdminDbUtil.Set(dbRow, "continent", num);
				}
				AdminDbUtil.Set(dbRow, "itemid", num2);
				AdminDbUtil.Set(dbRow, "minimum_quantity", (long)dropEntry.min);
				AdminDbUtil.Set(dbRow, "maximum_quantity", (long)dropEntry.max);
				AdminDbUtil.Set(dbRow, "questid", (long)dropEntry.quest);
				AdminDbUtil.Set(dbRow, "chance", 0L);
				if (dbTable.Col("comments") != null)
				{
					AdminDbUtil.Set(dbRow, "comments", "后台删除（墓碑）");
				}
				dbTable.Insert(dbRow);
			}
			dbStore.Save();
			DropStore.Invalidate();
			return (object)null;
		});
	}

	private static Func<DbRow, bool> KeyFilter(bool global, long owner, long item)
	{
		if (!global)
		{
			return (DbRow r) => DbStore.Long(r, "dropperid", 0L) == owner && DbStore.Long(r, "itemid", 0L) == item;
		}
		return (DbRow r) => DbStore.Long(r, "continent", -1L) == owner && DbStore.Long(r, "itemid", 0L) == item;
	}
}
