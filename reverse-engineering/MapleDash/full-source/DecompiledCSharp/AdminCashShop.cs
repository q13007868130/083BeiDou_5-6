using System;
using System.Collections.Generic;
using System.Globalization;
using MapleDash.Wz;

public static class AdminCashShop
{
	private sealed class Baseline
	{
		public int sn;

		public int itemId;

		public int price;

		public int count = 1;

		public int period = 1;

		public int onSale;

		public int gender = 2;

		public int priority;

		public int bonus;

		public int maplePoint;

		public int meso;

		public int forPremiumUser;

		public int clz;

		public int limit;

		public int pbCash;

		public int pbPoint;

		public int pbGift;

		public int packageSn;
	}

	private static readonly Dictionary<int, string> CategoryNames = new Dictionary<int, string>
	{
		{ 8, "MAIN" },
		{ 1, "EVENT" },
		{ 2, "EQUIP" },
		{ 3, "USE" },
		{ 4, "SET-UP" },
		{ 5, "ETC" },
		{ 6, "PET" },
		{ 7, "PACKAGE" }
	};

	private static Dictionary<int, Baseline> baselineCache;

	public static void Register(AdminApi api)
	{
		api.Get("/cashShop/v1/getAllCategoryList", (AdminCtx ctx) => Categories());
		api.Post("/cashShop/v1/getCommodityByCategory", CommodityPage);
		api.Post("/cashShop/v1/onSale", (AdminCtx ctx) => ChangeOnSale(ctx, 1));
		api.Post("/cashShop/v1/offSale", (AdminCtx ctx) => ChangeOnSale(ctx, 0));
		api.Post("/cashShop/v1/batchOnSale", BatchOnSale);
	}

	private static object Categories()
	{
		List<object> list = new List<object>();
		foreach (ImgNode child in WzAssets.FileRoot("Etc/Category.img").Children)
		{
			int num = child.Int("Category", -1);
			if (num >= 0)
			{
				list.Add(AdminJson.Obj("id", (long)num, "name", CategoryNames.TryGetValue(num, out var value) ? value : "", "subId", (long)child.Int("CategorySub"), "subName", child.Text("Name")));
			}
		}
		return list;
	}

	private static Dictionary<int, Baseline> Baselines()
	{
		if (baselineCache != null)
		{
			return baselineCache;
		}
		Dictionary<int, Baseline> dictionary = new Dictionary<int, Baseline>();
		foreach (ImgNode child in WzAssets.FileRoot("Etc/Commodity.img").Children)
		{
			if (child.Get("SN") != null && child.Get("ItemId") != null)
			{
				int num = child.Int("Period", 1);
				Baseline baseline = new Baseline
				{
					sn = child.Int("SN"),
					itemId = child.Int("ItemId"),
					price = child.Int("Price"),
					count = child.Int("Count", 1),
					period = ((num == 0) ? 90 : num),
					onSale = child.Int("OnSale"),
					gender = child.Int("Gender", 2),
					priority = child.Int("Priority"),
					clz = child.Int("Class"),
					limit = IntOf(child, "Limit"),
					pbCash = child.Int("PbCash"),
					pbPoint = child.Int("PbPoint"),
					pbGift = child.Int("PbGift")
				};
				dictionary[baseline.sn] = baseline;
			}
		}
		baselineCache = dictionary;
		return dictionary;
	}

	private static int IntOf(ImgNode node, string key, int fallback = 0)
	{
		object obj = node.Get(key)?.Value;
		if (obj == null)
		{
			return fallback;
		}
		if (obj is int)
		{
			return (int)obj;
		}
		if (obj is string s && int.TryParse(s, out var result))
		{
			return result;
		}
		try
		{
			return Convert.ToInt32(obj);
		}
		catch
		{
			return fallback;
		}
	}

	private static object CommodityPage(AdminCtx ctx)
	{
		object body = ctx.Body;
		int id = AdminJson.GetInt(body, "id");
		int subId = AdminJson.GetInt(body, "subId");
		if (id <= 0)
		{
			throw new AdminApiException("参数不能为空: id");
		}
		if (subId <= 0 && AdminJson.Get(body, "subId") == null)
		{
			throw new AdminApiException("参数不能为空: subId");
		}
		int pageNo = AdminJson.GetInt(body, "pageNo", 1);
		bool onSaleFilter = AdminJson.Get(body, "onSale") != null && AdminJson.GetBool(body, "onSale");
		long itemIdFilter = AdminJson.GetLong(body, "itemId", 0L);
		string prefix = id + subId.ToString("D2");
		return AdminMainThread.Run(() =>
		{
			DbStore dbStore = AdminBackend.RequireDb();
			Dictionary<int, Baseline> dictionary = Baselines();
			Dictionary<int, DbRow> dictionary2 = new Dictionary<int, DbRow>();
			foreach (DbRow row in dbStore.Table("modified_cash_item").rows)
			{
				dictionary2[DbStore.Int(row, "sn")] = row;
			}
			string categoryName = (CategoryNames.TryGetValue(id, out var value) ? value : "");
			string subName = "";
			foreach (ImgNode child in WzAssets.FileRoot("Etc/Category.img").Children)
			{
				if (child.Int("Category", -1) == id && child.Int("CategorySub", -1) == subId)
				{
					subName = child.Text("Name");
					break;
				}
			}
			List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
			foreach (Baseline value3 in dictionary.Values)
			{
				if (value3.sn.ToString(CultureInfo.InvariantCulture).StartsWith(prefix, StringComparison.Ordinal))
				{
					Dictionary<string, object> dictionary3 = ToDto(value3, dictionary2.TryGetValue(value3.sn, out var value2) ? value2 : null, id, categoryName, subId, subName);
					if ((AdminJson.Get(body, "onSale") == null || (dictionary3["onSale"] is int num && num == 1) == onSaleFilter) && (itemIdFilter <= 0 || (long)dictionary3["itemId"] == itemIdFilter))
					{
						list.Add(dictionary3);
					}
				}
			}
			list.Sort((Dictionary<string, object> a, Dictionary<string, object> b2) =>
			{
				int num4 = ((int)b2["priority"]).CompareTo((int)a["priority"]);
				return (num4 == 0) ? ((long)a["itemId"]).CompareTo((long)b2["itemId"]) : num4;
			});
			List<object> list2 = new List<object>();
			int num2 = (Math.Max(1, pageNo) - 1) * 10;
			for (int num3 = num2; num3 < list.Count && num3 < num2 + 10; num3++)
			{
				list2.Add(list[num3]);
			}
			return AdminJson.Page(list2, pageNo, 10, list.Count);
		});
	}

	private static Dictionary<string, object> ToDto(Baseline b, DbRow row, int categoryId, string categoryName, int subId, string subName)
	{
		return AdminJson.Obj("categoryId", (long)categoryId, "categoryName", categoryName, "subcategoryId", (long)subId, "subcategoryName", subName, "sn", (long)b.sn, "itemId", (long)Ov("item_id", b.itemId), "itemName", AdminNames.ItemName(Ov("item_id", b.itemId)), "price", (long)Ov("price", b.price), "defaultPrice", (long)b.price, "count", (long)Ov("count", b.count), "defaultCount", (long)b.count, "period", (long)Ov("period", b.period), "defaultPeriod", (long)b.period, "priority", (long)Ov("priority", b.priority), "defaultPriority", (long)b.priority, "onSale", (long)Ov("on_sale", b.onSale), "defaultOnSale", (long)b.onSale, "bonus", (long)Ov("bonus", b.bonus), "defaultBonus", (long)b.bonus, "maplePoint", (long)Ov("maple_point", b.maplePoint), "defaultMaplePoint", (long)b.maplePoint, "meso", (long)Ov("meso", b.meso), "defaultMeso", (long)b.meso, "forPremiumUser", (long)Ov("for_premium_user", b.forPremiumUser), "defaultForPremiumUser", (long)b.forPremiumUser, "gender", (long)Ov("commodity_gender", b.gender), "defaultGender", (long)b.gender, "clz", (long)Ov("class", b.clz), "defaultClz", (long)b.clz, "limit", (long)Ov("limit", b.limit), "defaultLimit", (long)b.limit, "pbCash", (long)Ov("pb_cash", b.pbCash), "defaultPBCash", (long)b.pbCash, "pbPoint", (long)Ov("pb_point", b.pbPoint), "defaultPBPoint", (long)b.pbPoint, "pbGift", (long)Ov("pb_gift", b.pbGift), "defaultPBGift", (long)b.pbGift, "packageSn", (long)Ov("package_sn", b.packageSn), "defaultPackageSn", (long)b.packageSn);
		int Ov(string col, int dflt)
		{
			if (row == null || row[col] == null)
			{
				return dflt;
			}
			return DbStore.Int(row, col);
		}
	}

	private static object ChangeOnSale(AdminCtx ctx, int onSale)
	{
		return ChangeOnSaleCore(ctx.Body, onSale);
	}

	private static object ChangeOnSaleCore(object body, int onSale)
	{
		long sn = AdminJson.GetLong(body, "sn", 0L);
		if (sn <= 0)
		{
			throw new AdminApiException("参数不能为空: sn");
		}
		return AdminMainThread.Run(() =>
		{
			DbStore dbStore = AdminBackend.RequireDb();
			if (!Baselines().TryGetValue((int)sn, out var value))
			{
				throw new AdminApiException("未知参数值: sn=" + sn);
			}
			DbTable dbTable = dbStore.Table("modified_cash_item");
			dbTable.Delete((DbRow r) => DbStore.Int(r, "sn") == (int)sn);
			if (onSale != 1)
			{
				if (value.onSale == 1)
				{
					dbTable.Insert(new DbRow
					{
						["sn"] = sn,
						["on_sale"] = 0L
					});
				}
				return AdminJson.Ok();
			}
			DbRow dbRow = new DbRow { ["sn"] = sn };
			PutIfChanged(dbRow, "item_id", AdminJson.Get(body, "itemId"), value.itemId);
			PutIfChanged(dbRow, "price", AdminJson.Get(body, "price"), value.price);
			PutIfChanged(dbRow, "period", AdminJson.Get(body, "period"), value.period);
			PutIfChanged(dbRow, "priority", AdminJson.Get(body, "priority"), value.priority);
			PutIfChanged(dbRow, "count", AdminJson.Get(body, "count"), value.count);
			PutIfChanged(dbRow, "on_sale", AdminJson.Get(body, "onSale"), value.onSale);
			PutIf(dbRow, "bonus", AdminJson.Get(body, "bonus"));
			PutIf(dbRow, "maple_point", AdminJson.Get(body, "maplePoint"));
			PutIf(dbRow, "meso", AdminJson.Get(body, "meso"));
			PutIf(dbRow, "for_premium_user", AdminJson.Get(body, "forPremiumUser"));
			PutIf(dbRow, "commodity_gender", AdminJson.Get(body, "commodityGender"));
			PutIf(dbRow, "class", AdminJson.Get(body, "class"));
			PutIf(dbRow, "limit", AdminJson.Get(body, "limit"));
			PutIf(dbRow, "pb_cash", AdminJson.Get(body, "pbCash"));
			PutIf(dbRow, "pb_point", AdminJson.Get(body, "pbPoint"));
			PutIf(dbRow, "pb_gift", AdminJson.Get(body, "pbGift"));
			PutIf(dbRow, "package_sn", AdminJson.Get(body, "packageSn"));
			dbTable.Insert(dbRow);
			return AdminJson.Ok();
		});
	}

	private static void PutIfChanged(DbRow row, string col, object value, int baseline)
	{
		if (value != null)
		{
			int num = Convert.ToInt32(value, CultureInfo.InvariantCulture);
			if (num != baseline)
			{
				row[col] = (long)num;
			}
		}
	}

	private static void PutIf(DbRow row, string col, object value)
	{
		if (value != null)
		{
			row[col] = (long)Convert.ToInt32(value, CultureInfo.InvariantCulture);
		}
	}

	private static object BatchOnSale(AdminCtx ctx)
	{
		object body = ctx.Body;
		List<object> list = AdminJson.GetList(body, "data");
		string text = AdminJson.GetString(body, "type");
		long num = AdminJson.GetLong(body, "value", 0L);
		if (list == null || list.Count == 0)
		{
			throw new AdminApiException("参数不能为空: data");
		}
		foreach (object item in list)
		{
			Dictionary<string, object> dictionary = AdminJson.AsDict(item);
			if (dictionary != null)
			{
				dictionary["onSale"] = 1;
				switch (text)
				{
				case "价格":
					dictionary["price"] = num;
					break;
				case "数量":
					dictionary["count"] = num;
					break;
				case "有效期":
					dictionary["period"] = num;
					break;
				}
				ChangeOnSaleCore(dictionary, 1);
			}
		}
		return AdminJson.Ok();
	}
}
