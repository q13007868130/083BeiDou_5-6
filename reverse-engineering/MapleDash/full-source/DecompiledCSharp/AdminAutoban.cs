using System;
using System.Collections.Generic;

public static class AdminAutoban
{
	private sealed class Def
	{
		public string Type;

		public string Name;

		public int DefaultPoints;

		public long DefaultExpireMs;

		public Def(string type, string name, int points, long expireMs)
		{
			Type = type;
			Name = name;
			DefaultPoints = points;
			DefaultExpireMs = expireMs;
		}
	}

	private static readonly Def[] Defs = new Def[20]
	{
		new Def("MOB_COUNT", "怪物数量异常", 1, -1L),
		new Def("GENERAL", "通用检测", 1, -1L),
		new Def("FIX_DAMAGE", "固定伤害", 1, -1L),
		new Def("DAMAGE_HACK", "伤害外挂", 15, 60000L),
		new Def("DISTANCE_HACK", "距离外挂", 10, 120000L),
		new Def("PORTAL_DISTANCE", "传送门距离异常", 5, 30000L),
		new Def("PACKET_EDIT", "数据包篡改", 1, -1L),
		new Def("ACC_HACK", "命中率外挂", 1, -1L),
		new Def("CREATION_GENERATOR", "创建生成器", 1, -1L),
		new Def("HIGH_HP_HEALING", "高血量恢复", 1, -1L),
		new Def("FAST_HP_HEALING", "快速血量恢复", 15, -1L),
		new Def("FAST_MP_HEALING", "快速蓝量恢复", 20, 30000L),
		new Def("GACHA_EXP", "扭蛋经验异常", 1, -1L),
		new Def("TUBI", "快速拾取", 20, 15000L),
		new Def("SHORT_ITEM_VAC", "短距离吸物", 1, -1L),
		new Def("ITEM_VAC", "吸物", 1, -1L),
		new Def("FAST_ITEM_PICKUP", "快速拾取物品", 5, 30000L),
		new Def("FAST_ATTACK", "快速攻击", 10, 30000L),
		new Def("MPCON", "蓝量消耗异常", 25, 30000L),
		new Def("ATTACK_INTERVAL", "攻击间隔异常", 60, 60000L)
	};

	public static void Register(AdminApi api)
	{
		api.Get("/autoban/v1/getConfigList", (AdminCtx ctx) => AdminMainThread.Run((Func<object>)(() =>
		{
			DbStore db = AdminBackend.Db;
			List<object> list = new List<object>();
			Def[] defs = Defs;
			foreach (Def def in defs)
			{
				DbRow dbRow = null;
				if (db != null && db.HasTable("autoban_config"))
				{
					dbRow = db.Table("autoban_config").Find(DbStore.Eq("type", def.Type));
				}
				Dictionary<string, object> dictionary = AdminJson.Obj("id", (dbRow == null) ? 0 : DbStore.Long(dbRow, "id", 0L), "type", def.Type, "name", def.Name, "defaultPoints", (long)def.DefaultPoints, "defaultExpireTimeSeconds", (def.DefaultExpireMs < 0) ? (-1) : (def.DefaultExpireMs / 1000));
				dictionary["disabled"] = dbRow != null && DbStore.Long(dbRow, "disabled", 0L) != 0;
				bool flag = dbRow != null && dbRow.ContainsKey("points") && dbRow["points"] != null;
				bool flag2 = dbRow != null && dbRow.ContainsKey("expire_time") && dbRow["expire_time"] != null;
				dictionary["points"] = (flag ? ((object)DbStore.Long(dbRow, "points", 0L)) : null);
				dictionary["expireTimeSeconds"] = (flag2 ? ((object)((DbStore.Long(dbRow, "expire_time", 0L) < 0) ? (-1) : (DbStore.Long(dbRow, "expire_time", 0L) / 1000))) : null);
				dictionary["description"] = ((dbRow == null) ? "" : DbStore.Str(dbRow, "description"));
				dictionary["changePoints"] = flag;
				dictionary["changeExpireTime"] = flag2;
				list.Add(dictionary);
			}
			return list;
		}), 8000));
		api.Post("/autoban/v1/updateConfig", (AdminCtx ctx) =>
		{
			Dictionary<string, object> dictionary = AdminJson.AsDict(ctx.Body);
			if (dictionary == null)
			{
				throw new AdminApiException("缺少配置数据");
			}
			string type = AdminJson.GetString(dictionary, "type");
			if (string.IsNullOrEmpty(type))
			{
				throw new AdminApiException("封禁类型不能为空");
			}
			Def def = null;
			Def[] defs = Defs;
			foreach (Def def2 in defs)
			{
				if (def2.Type == type)
				{
					def = def2;
					break;
				}
			}
			if (def == null)
			{
				throw new AdminApiException("未知的封禁类型: " + type);
			}
			bool flag = AdminJson.GetBool(dictionary, "changePoints");
			bool flag2 = AdminJson.GetBool(dictionary, "changeExpireTime");
			long? points = ((flag && AdminJson.Get(dictionary, "points") != null) ? new long?(AdminJson.GetLong(dictionary, "points", 0L)) : ((long?)null));
			long? expireMs = null;
			if (flag2 && AdminJson.Get(dictionary, "expireTimeSeconds") != null)
			{
				long num = AdminJson.GetLong(dictionary, "expireTimeSeconds", 0L);
				expireMs = ((num < 0) ? (-1) : (num * 1000));
			}
			bool disabled = AdminJson.GetBool(dictionary, "disabled");
			string description = AdminJson.GetString(dictionary, "description");
			return AdminMainThread.Run(() =>
			{
				DbStore dbStore = AdminBackend.RequireDb();
				if (!dbStore.HasTable("autoban_config"))
				{
					throw new AdminApiException("autoban_config 表尚未初始化（请先升级存档）");
				}
				DbTable dbTable = dbStore.Table("autoban_config");
				DbRow dbRow = dbTable.Find(DbStore.Eq("type", type));
				if (dbRow == null && !points.HasValue && !expireMs.HasValue && !disabled && string.IsNullOrEmpty(description))
				{
					return (object)null;
				}
				DbRow dbRow2 = new DbRow
				{
					["disabled"] = (long)(disabled ? 1 : 0),
					["points"] = points,
					["expire_time"] = expireMs,
					["description"] = description,
					["update_time"] = DbStore.NowText()
				};
				if (dbRow == null)
				{
					dbRow2["type"] = type;
					dbRow2["create_time"] = DbStore.NowText();
					dbTable.Insert(dbRow2);
				}
				else
				{
					dbTable.Update(DbStore.Eq("type", type), dbRow2);
				}
				dbStore.Save();
				return (object)null;
			});
		});
	}
}
