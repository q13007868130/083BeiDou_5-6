using System;
using System.Collections.Generic;
using System.Globalization;

public static class AdminBots
{
	public sealed class PartyTeamInfo
	{
		public long characterId;

		public string name;

		public int mapId;

		public int companions;

		public long remainingSec;
	}

	private static readonly string[] MarketCodes = new string[13]
	{
		"solo_mapling_market_auto_start", "solo_mapling_feature_fm_bots_enabled", "solo_mapling_feature_fm_merchants_enabled", "solo_mapling_feature_fm_region_fill_enabled", "solo_mapling_market_bot_max", "solo_mapling_market_shop_max", "solo_mapling_market_wander_enabled", "solo_mapling_market_hawk_enabled", "solo_mapling_market_browse_shops_enabled", "solo_mapling_market_idle_when_empty",
		"solo_mapling_market_tick_ms", "solo_mapling_market_speech_gap_ms", "solo_mapling_market_pathfind_per_tick"
	};

	private static readonly string[] AppearanceCodes = new string[8] { "solo_mapling_bot_random_chinese_name", "solo_mapling_bot_random_skill_enabled", "solo_mapling_bot_random_chair_enabled", "solo_mapling_bot_random_body_enabled", "solo_mapling_bot_normal_equips_enabled", "solo_mapling_bot_full_equips_enabled", "solo_mapling_bot_nx_equips_enabled", "solo_mapling_bot_deferred_decoration_enabled" };

	private static readonly string[] PartyCodes = new string[5] { "solo_mapling_party_companion_enabled", "solo_mapling_party_companion_require_party", "solo_mapling_party_companion_count", "solo_mapling_party_companion_minutes", "solo_mapling_party_companion_level_jitter" };

	public static Func<List<PartyTeamInfo>> PartyTeams;

	public static Func<long, string> PartySummon;

	public static Func<long, bool> PartyDismiss;

	public static Func<bool> MarketStartupComplete;

	public static Func<bool> MarketStartupRunning;

	public static Action RequestMarketStartup;

	public static Func<int> ActiveBots;

	public static void Register(AdminApi api)
	{
		api.Get("/bot-control/v1/state", (AdminCtx ctx) => State());
		api.Get("/bot-control/v1/config", (AdminCtx ctx) => Config());
		api.Put("/bot-control/v1/config", Update);
		api.Post("/bot-control/v1/market/start", StartMarket);
		api.Get("/bot-control/v1/party", (AdminCtx ctx) => Party());
		api.Put("/bot-control/v1/party", UpdateParty);
		api.Post("/bot-control/v1/party/summon", SummonParty);
		api.Post("/bot-control/v1/party/dismiss", DismissParty);
	}

	private static object StartMarket(AdminCtx ctx)
	{
		if (MarketStartupComplete != null && MarketStartupComplete())
		{
			return "already";
		}
		if (MarketStartupRunning != null && MarketStartupRunning())
		{
			return "running";
		}
		if (RequestMarketStartup == null)
		{
			throw new AdminApiException("服务端未启动（请先进入游戏）");
		}
		AdminMainThread.Run(() =>
		{
			RequestMarketStartup();
			return 1L;
		});
		return "started";
	}

	private static object State()
	{
		long current = 0L;
		try
		{
			current = ((AdminBackend.CurrentCharacterId != null) ? AdminBackend.CurrentCharacterId() : 0);
		}
		catch
		{
		}
		int bots = 0;
		try
		{
			bots = ((ActiveBots != null) ? ActiveBots() : 0);
		}
		catch
		{
		}
		return AdminMainThread.Run(() => AdminJson.Obj("activeBots", (long)Math.Max(0, bots), "marketDirectorBots", 0L, "marketEntranceBots", 0L, "onlinePlayers", (long)((current > 0) ? 1 : 0), "marketReady", false, "marketStarting", false));
	}

	private static object Config()
	{
		return AdminMainThread.Run(() =>
		{
			DbStore dbStore = AdminBackend.RequireDb();
			Dictionary<string, DbRow> dictionary = new Dictionary<string, DbRow>();
			foreach (DbRow row in dbStore.Table("game_config").rows)
			{
				string text = DbStore.Str(row, "config_code");
				if (text.StartsWith("solo_mapling", StringComparison.Ordinal))
				{
					dictionary[text] = row;
				}
			}
			HashSet<string> hashSet = new HashSet<string>();
			List<object> list = Fields(MarketCodes, dictionary, hashSet);
			List<object> list2 = Fields(AppearanceCodes, dictionary, hashSet);
			List<object> list3 = Fields(PartyCodes, dictionary, hashSet);
			List<string> list4 = new List<string>(dictionary.Keys);
			foreach (string item in hashSet)
			{
				list4.Remove(item);
			}
			list4.Sort(StringComparer.Ordinal);
			List<object> list5 = Fields(list4, dictionary, null);
			return AdminJson.Obj("market", list, "appearance", list2, "environment", list5, "party", list3);
		});
	}

	private static List<object> Fields(string[] codes, Dictionary<string, DbRow> rows, HashSet<string> used)
	{
		List<object> list = new List<object>();
		foreach (string text in codes)
		{
			used?.Add(text);
			if (!rows.TryGetValue(text, out var value))
			{
				list.Add(AdminJson.Obj("id", 0L, "code", text, "clazz", ClazzOf(text), "value", DefaultOf(text), "desc", text));
			}
			else
			{
				list.Add(AdminJson.Obj("id", DbStore.Long(value, "id", 0L), "code", text, "clazz", string.IsNullOrEmpty(DbStore.Str(value, "config_clazz")) ? ClazzOf(text) : DbStore.Str(value, "config_clazz"), "value", DbStore.Str(value, "config_value"), "desc", text));
			}
		}
		return list;
	}

	private static List<object> Fields(List<string> codes, Dictionary<string, DbRow> rows, HashSet<string> used)
	{
		return Fields(codes.ToArray(), rows, used);
	}

	private static string ClazzOf(string code)
	{
		if (code.EndsWith("_enabled", StringComparison.Ordinal) || code.EndsWith("_auto_start", StringComparison.Ordinal) || code.EndsWith("_require_party", StringComparison.Ordinal))
		{
			return "java.lang.Boolean";
		}
		if (code.EndsWith("_max", StringComparison.Ordinal) || code.EndsWith("_ms", StringComparison.Ordinal) || code.EndsWith("_per_tick", StringComparison.Ordinal) || code.EndsWith("_min", StringComparison.Ordinal) || code.EndsWith("_count", StringComparison.Ordinal) || code.EndsWith("_minutes", StringComparison.Ordinal) || code.EndsWith("_jitter", StringComparison.Ordinal))
		{
			return "java.lang.Integer";
		}
		return "java.lang.String";
	}

	private static string DefaultOf(string code)
	{
		switch (code)
		{
		case "solo_mapling_market_auto_start":
			return "true";
		case "solo_mapling_market_bot_max":
			return "10";
		case "solo_mapling_market_shop_max":
			return "10";
		case "solo_mapling_market_tick_ms":
			return "250";
		case "solo_mapling_market_speech_gap_ms":
			return "500";
		case "solo_mapling_market_pathfind_per_tick":
			return "3";
		case "solo_mapling_bot_deferred_decoration_enabled":
			return "false";
		case "solo_mapling_party_companion_enabled":
			return "true";
		case "solo_mapling_party_companion_require_party":
			return "true";
		case "solo_mapling_party_companion_count":
			return "5";
		case "solo_mapling_party_companion_minutes":
			return "30";
		case "solo_mapling_party_companion_level_jitter":
			return "3";
		default:
			if (!code.EndsWith("_enabled", StringComparison.Ordinal))
			{
				return "";
			}
			return "true";
		}
	}

	private static object Update(AdminCtx ctx)
	{
		List<object> fields = AdminJson.GetList(ctx.Body, "fields");
		if (fields == null || fields.Count == 0)
		{
			throw new AdminApiException("参数不能为空: fields");
		}
		return AdminMainThread.Run((Func<long>)(() =>
		{
			DbTable dbTable = AdminBackend.RequireDb().Table("game_config");
			int num = 0;
			foreach (object item in fields)
			{
				Dictionary<string, object> o = AdminJson.AsDict(item);
				string code = AdminJson.GetString(o, "code");
				if (!string.IsNullOrEmpty(code) && code.StartsWith("solo_mapling_", StringComparison.Ordinal))
				{
					string value = AdminJson.GetString(o, "value");
					DbRow row = dbTable.Find((DbRow r) => DbStore.Str(r, "config_code") == code);
					if (row != null)
					{
						dbTable.Update((DbRow r) => r == row, new DbRow
						{
							["config_value"] = value,
							["config_clazz"] = ClazzOf(code),
							["update_time"] = Now()
						});
					}
					else
					{
						dbTable.Insert(new DbRow
						{
							["config_type"] = "server",
							["config_sub_type"] = "SoloMapling",
							["config_clazz"] = ClazzOf(code),
							["config_code"] = code,
							["config_value"] = value,
							["config_desc"] = code,
							["update_time"] = Now()
						});
					}
					num++;
				}
			}
			return num;
		}), 8000);
	}

	private static object Party()
	{
		return AdminMainThread.Run(() =>
		{
			DbStore dbStore = AdminBackend.RequireDb();
			Dictionary<string, DbRow> dictionary = new Dictionary<string, DbRow>();
			foreach (DbRow row in dbStore.Table("game_config").rows)
			{
				string text = DbStore.Str(row, "config_code");
				if (text.StartsWith("solo_mapling", StringComparison.Ordinal))
				{
					dictionary[text] = row;
				}
			}
			return AdminJson.Obj("config", Fields(PartyCodes, dictionary, null), "state", PartyState());
		});
	}

	private static object PartyState()
	{
		List<object> list = new List<object>();
		List<PartyTeamInfo> list2 = ((PartyTeams != null) ? PartyTeams() : null);
		if (list2 != null)
		{
			foreach (PartyTeamInfo item in list2)
			{
				list.Add(AdminJson.Obj("characterId", item.characterId, "name", item.name ?? "", "mapId", (long)item.mapId, "companions", (long)item.companions, "remainingSec", Math.Max(0L, item.remainingSec)));
			}
		}
		long num = 0L;
		try
		{
			num = ((AdminBackend.CurrentCharacterId != null) ? AdminBackend.CurrentCharacterId() : 0);
		}
		catch
		{
		}
		return AdminJson.Obj("teams", list, "activeTeams", (long)list.Count, "characterId", num);
	}

	private static object UpdateParty(AdminCtx ctx)
	{
		List<object> fields = AdminJson.GetList(ctx.Body, "fields");
		if (fields == null || fields.Count == 0)
		{
			throw new AdminApiException("参数不能为空: fields");
		}
		return AdminMainThread.Run((Func<long>)(() =>
		{
			DbTable dbTable = AdminBackend.RequireDb().Table("game_config");
			HashSet<string> hashSet = new HashSet<string>(PartyCodes);
			int num = 0;
			foreach (object item in fields)
			{
				Dictionary<string, object> o = AdminJson.AsDict(item);
				string code = AdminJson.GetString(o, "code");
				if (!string.IsNullOrEmpty(code) && hashSet.Contains(code))
				{
					string value = AdminJson.GetString(o, "value");
					DbRow row = dbTable.Find((DbRow r) => DbStore.Str(r, "config_code") == code);
					if (row != null)
					{
						dbTable.Update((DbRow r) => r == row, new DbRow
						{
							["config_value"] = value,
							["config_clazz"] = ClazzOf(code),
							["update_time"] = Now()
						});
					}
					else
					{
						dbTable.Insert(new DbRow
						{
							["config_type"] = "server",
							["config_sub_type"] = "SoloMapling",
							["config_clazz"] = ClazzOf(code),
							["config_code"] = code,
							["config_value"] = value,
							["config_desc"] = code,
							["update_time"] = Now()
						});
					}
					num++;
				}
			}
			return num;
		}), 8000);
	}

	private static object SummonParty(AdminCtx ctx)
	{
		long characterId = PartyTarget(ctx);
		return AdminMainThread.Run(() =>
		{
			if (PartySummon == null)
			{
				throw new AdminApiException("服务端未启动（请先进入游戏）");
			}
			string text = PartySummon(characterId);
			if (!string.IsNullOrEmpty(text))
			{
				throw new AdminApiException(text);
			}
			return PartyState();
		});
	}

	private static object DismissParty(AdminCtx ctx)
	{
		long characterId = PartyTarget(ctx);
		return AdminMainThread.Run(() =>
		{
			if (PartyDismiss == null)
			{
				throw new AdminApiException("服务端未启动（请先进入游戏）");
			}
			PartyDismiss(characterId);
			return PartyState();
		});
	}

	private static long PartyTarget(AdminCtx ctx)
	{
		long num = AdminJson.GetLong(ctx.Body, "characterId", 0L);
		if (num <= 0)
		{
			try
			{
				num = ((AdminBackend.CurrentCharacterId != null) ? AdminBackend.CurrentCharacterId() : 0);
			}
			catch
			{
			}
		}
		if (num <= 0)
		{
			throw new AdminApiException("没有在线角色（请先进入游戏）");
		}
		return num;
	}

	private static string Now()
	{
		return DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
	}
}
