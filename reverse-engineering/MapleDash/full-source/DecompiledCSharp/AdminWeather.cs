using System;
using System.Collections.Generic;
using System.Globalization;

public static class AdminWeather
{
	private static readonly string[] Profiles = new string[9] { "clear", "rain", "snow", "overcast", "storm", "blizzard", "leaves", "blossom", "sandstorm" };

	private const string ConfigCode = "admin_weather_config";

	private static NativeWeatherService service;

	private static object configuredFor;

	public static void Register(AdminApi api)
	{
		api.Get("/weather/v1/state", (AdminCtx ctx) => State());
		api.Get("/weather/v1/config", (AdminCtx ctx) => Config());
		api.Put("/weather/v1/config", UpdateConfig);
		api.Get("/weather/v1/regions", (AdminCtx ctx) => Regions());
		api.Put("/weather/v1/regions/{region}", UpdateRegion);
		api.Post("/weather/v1/override", Override);
		api.Delete("/weather/v1/override", (AdminCtx ctx) => AdminMainThread.Run(() =>
		{
			Service().ClearOverride(NativeWeatherHost.NowMs());
			return AdminJson.Ok();
		}));
		api.Post("/weather/v1/broadcast", (AdminCtx ctx) => AdminJson.Obj("count", 1L));
		api.Post("/weather/v1/wind", (AdminCtx ctx) => AdminMainThread.Run(() =>
		{
			Service().RerollWind();
			return AdminJson.Ok();
		}));
	}

	private static NativeWeatherService Service()
	{
		NativeWeatherService s = NativeWeatherHost.Service;
		AdminMainThread.Run(() =>
		{
			ApplyPersisted(s);
			return (object)null;
		});
		return s;
	}

	private static void ApplyPersisted(NativeWeatherService s)
	{
		if (configuredFor == s)
		{
			return;
		}
		configuredFor = s;
		try
		{
			DbStore db = AdminBackend.Db;
			if (db == null)
			{
				return;
			}
			string text = GameConfigDao.Get(db, "admin_weather_config");
			Dictionary<string, object> dictionary = (string.IsNullOrEmpty(text) ? null : AdminJson.AsDict(AdminJson.Parse(text)));
			if (dictionary != null)
			{
				s.Enabled = AdminJson.GetBool(dictionary, "enabled", s.Enabled);
				s.DayLength = AdminJson.GetLong(dictionary, "dayLengthMs", s.DayLength);
				s.ChangeInterval = AdminJson.GetLong(dictionary, "changeIntervalMs", s.ChangeInterval);
				s.OverrideHold = AdminJson.GetLong(dictionary, "overrideHoldMs", s.OverrideHold);
				s.RainbowDuration = AdminJson.GetInt(dictionary, "rainbowDurationSec", s.RainbowDuration);
				s.InjectSky = AdminJson.GetBool(dictionary, "injectSky", s.InjectSky);
				s.SeasonDrift = AdminJson.GetBool(dictionary, "seasonDrift", s.SeasonDrift);
			}
			if (!db.HasTable("game_config"))
			{
				return;
			}
			foreach (DbRow row in db.Table("game_config").rows)
			{
				string text2 = DbStore.Str(row, "config_code");
				if (text2.StartsWith("admin_weather_region_", StringComparison.Ordinal))
				{
					ApplyRegion(s, text2.Substring("admin_weather_region_".Length), AdminJson.AsDict(AdminJson.Parse(DbStore.Str(row, "config_value"))));
				}
			}
		}
		catch
		{
		}
	}

	private static object State()
	{
		NativeWeatherService nativeWeatherService = Service();
		long num = NativeWeatherHost.NowMs();
		bool flag = nativeWeatherService.OverrideSky >= 0 || nativeWeatherService.OverrideMinute >= 0;
		int num2 = ((nativeWeatherService.RegionCount > 0) ? nativeWeatherService.SkyOf(nativeWeatherService.RegionCount - 1) : 0);
		long num3 = 0L;
		try
		{
			num3 = ((AdminBackend.CurrentCharacterId != null) ? AdminBackend.CurrentCharacterId() : 0);
		}
		catch
		{
		}
		return AdminJson.Obj("enabled", nativeWeatherService.Enabled, "minuteOfDay", (long)nativeWeatherService.MinuteOfDay(num), "nightLevel", (double)nativeWeatherService.NightLevel(num), "phase", (num2 >= 0 && num2 < Profiles.Length) ? Profiles[num2] : "clear", "weatherOverridden", flag, "overrideProfile", (nativeWeatherService.OverrideSky >= 0 && nativeWeatherService.OverrideSky < Profiles.Length) ? Profiles[nativeWeatherService.OverrideSky] : null, "timeFrozen", nativeWeatherService.OverrideMinute >= 0, "overrideRemainingSec", Math.Max(0L, (nativeWeatherService.OverrideUntil - num) / 1000), "nextRollInSec", Math.Max(0L, (nativeWeatherService.NextRollAt - num) / 1000), "onlinePlayers", (long)((num3 > 0) ? 1 : 0), "msPerGameMinute", Math.Max(1L, nativeWeatherService.DayLength / 1440));
	}

	private static object Config()
	{
		NativeWeatherService nativeWeatherService = Service();
		return AdminJson.Obj("enabled", nativeWeatherService.Enabled, "dayLengthMs", nativeWeatherService.DayLength, "changeIntervalMs", nativeWeatherService.ChangeInterval, "overrideHoldMs", nativeWeatherService.OverrideHold, "rainbowDurationSec", (long)nativeWeatherService.RainbowDuration, "injectSky", nativeWeatherService.InjectSky, "seasonDrift", nativeWeatherService.SeasonDrift);
	}

	private static object UpdateConfig(AdminCtx ctx)
	{
		object body = ctx.Body;
		return AdminMainThread.Run(() =>
		{
			NativeWeatherService nativeWeatherService = Service();
			if (AdminJson.Get(body, "enabled") != null)
			{
				nativeWeatherService.Enabled = AdminJson.GetBool(body, "enabled", nativeWeatherService.Enabled);
			}
			if (AdminJson.Get(body, "dayLengthMs") != null)
			{
				nativeWeatherService.DayLength = Math.Max(60000L, AdminJson.GetLong(body, "dayLengthMs", nativeWeatherService.DayLength));
			}
			if (AdminJson.Get(body, "changeIntervalMs") != null)
			{
				nativeWeatherService.ChangeInterval = Math.Max(10000L, AdminJson.GetLong(body, "changeIntervalMs", nativeWeatherService.ChangeInterval));
			}
			if (AdminJson.Get(body, "overrideHoldMs") != null)
			{
				nativeWeatherService.OverrideHold = Math.Max(60000L, AdminJson.GetLong(body, "overrideHoldMs", nativeWeatherService.OverrideHold));
			}
			if (AdminJson.Get(body, "rainbowDurationSec") != null)
			{
				nativeWeatherService.RainbowDuration = Math.Max(0, AdminJson.GetInt(body, "rainbowDurationSec", nativeWeatherService.RainbowDuration));
			}
			if (AdminJson.Get(body, "injectSky") != null)
			{
				nativeWeatherService.InjectSky = AdminJson.GetBool(body, "injectSky", nativeWeatherService.InjectSky);
			}
			if (AdminJson.Get(body, "seasonDrift") != null)
			{
				nativeWeatherService.SeasonDrift = AdminJson.GetBool(body, "seasonDrift", nativeWeatherService.SeasonDrift);
			}
			nativeWeatherService.Touch();
			Upsert(AdminBackend.RequireDb(), "admin_weather_config", AdminJson.Write(Config()), "server", "Weather", "weather");
			return 1L;
		});
	}

	private static object Regions()
	{
		NativeWeatherService nativeWeatherService = Service();
		List<object> list = new List<object>();
		for (int i = 0; i < nativeWeatherService.RegionCount; i++)
		{
			NativeWeatherRegion nativeWeatherRegion = nativeWeatherService.Region(i);
			int num = nativeWeatherService.SkyOf(i);
			list.Add(AdminJson.Obj("region", nativeWeatherRegion.Name, "label", nativeWeatherRegion.Name, "currentProfile", (num >= 0 && num < Profiles.Length) ? Profiles[num] : "clear", "forcedProfile", (nativeWeatherRegion.Forced >= 0 && nativeWeatherRegion.Forced < Profiles.Length) ? Profiles[nativeWeatherRegion.Forced] : null, "weights", ToDoubles(nativeWeatherRegion.Weights), "nightTint", (long)nativeWeatherRegion.Tint, "paletteId", (long)nativeWeatherRegion.Palette));
		}
		return list;
	}

	private static object UpdateRegion(AdminCtx ctx)
	{
		string region = ctx.Param("region");
		object body = ctx.Body;
		return AdminMainThread.Run(() =>
		{
			NativeWeatherService nativeWeatherService = Service();
			int num = -1;
			for (int i = 0; i < nativeWeatherService.RegionCount; i++)
			{
				if (string.Equals(nativeWeatherService.Region(i).Name, region, StringComparison.OrdinalIgnoreCase))
				{
					num = i;
					break;
				}
			}
			if (num < 0)
			{
				throw new AdminApiException("天气区域不存在: " + region);
			}
			NativeWeatherRegion nativeWeatherRegion = nativeWeatherService.Region(num);
			int forced = nativeWeatherRegion.Forced;
			int num2 = nativeWeatherService.SkyOf(num);
			if (AdminJson.Get(body, "forcedProfile") != null)
			{
				string text = AdminJson.GetString(body, "forcedProfile");
				nativeWeatherRegion.Forced = (string.IsNullOrEmpty(text) ? (-1) : Array.IndexOf(Profiles, text));
				if (nativeWeatherRegion.Forced < 0 && !string.IsNullOrEmpty(text))
				{
					throw new AdminApiException("未知天气: " + text);
				}
			}
			if (AdminJson.Get(body, "weights") is List<object> list)
			{
				double[] array = new double[list.Count];
				for (int j = 0; j < array.Length; j++)
				{
					array[j] = Convert.ToDouble(list[j], CultureInfo.InvariantCulture);
				}
				nativeWeatherRegion.Weights = array;
			}
			if (AdminJson.Get(body, "nightTint") != null)
			{
				nativeWeatherRegion.Tint = AdminJson.GetInt(body, "nightTint", nativeWeatherRegion.Tint);
			}
			if (AdminJson.Get(body, "paletteId") != null)
			{
				nativeWeatherRegion.Palette = AdminJson.GetInt(body, "paletteId", nativeWeatherRegion.Palette);
			}
			NativeWeatherService nativeWeatherService2 = nativeWeatherService;
			int r = num;
			int previousEffective = ((nativeWeatherService.OverrideSky >= 0) ? nativeWeatherService.OverrideSky : ((forced >= 0) ? forced : num2));
			nativeWeatherService2.ApplyForced(r, previousEffective, NativeWeatherHost.NowMs());
			Upsert(AdminBackend.RequireDb(), "admin_weather_region_" + nativeWeatherRegion.Name, AdminJson.Write(AdminJson.Obj("forced", (long)nativeWeatherRegion.Forced, "weights", ToDoubles(nativeWeatherRegion.Weights), "tint", (long)nativeWeatherRegion.Tint, "paletteId", (long)nativeWeatherRegion.Palette)), "server", "Weather", "weather region");
			return 1L;
		});
	}

	private static void ApplyRegion(NativeWeatherService s, string name, Dictionary<string, object> cfg)
	{
		if (cfg == null)
		{
			return;
		}
		for (int i = 0; i < s.RegionCount; i++)
		{
			if (!string.Equals(s.Region(i).Name, name, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			NativeWeatherRegion nativeWeatherRegion = s.Region(i);
			nativeWeatherRegion.Forced = AdminJson.GetInt(cfg, "forced", nativeWeatherRegion.Forced);
			if (AdminJson.Get(cfg, "weights") is List<object> list)
			{
				double[] array = new double[list.Count];
				for (int j = 0; j < array.Length; j++)
				{
					array[j] = Convert.ToDouble(list[j], CultureInfo.InvariantCulture);
				}
				nativeWeatherRegion.Weights = array;
			}
			nativeWeatherRegion.Tint = AdminJson.GetInt(cfg, "tint", nativeWeatherRegion.Tint);
			nativeWeatherRegion.Palette = AdminJson.GetInt(cfg, "paletteId", nativeWeatherRegion.Palette);
		}
	}

	private static object Override(AdminCtx ctx)
	{
		object body = ctx.Body;
		return AdminMainThread.Run(() =>
		{
			NativeWeatherService nativeWeatherService = Service();
			long now = NativeWeatherHost.NowMs();
			int? sky = null;
			int? minute = null;
			string text = AdminJson.GetString(body, "profile");
			if (!string.IsNullOrEmpty(text))
			{
				int num = Array.IndexOf(Profiles, text);
				if (num < 0)
				{
					throw new AdminApiException("未知天气: " + text);
				}
				sky = num;
			}
			if (AdminJson.Get(body, "minuteOfDay") != null)
			{
				minute = Math.Max(0, Math.Min(1439, AdminJson.GetInt(body, "minuteOfDay", 720)));
			}
			if (!sky.HasValue && !minute.HasValue)
			{
				throw new AdminApiException("天气或时段必填其一");
			}
			long duration = ((AdminJson.Get(body, "durationMinutes") != null) ? (Math.Max(1L, AdminJson.GetLong(body, "durationMinutes", 60L)) * 60000) : nativeWeatherService.OverrideHold);
			nativeWeatherService.Override(sky, minute, duration, now);
			return 1L;
		});
	}

	private static List<object> ToDoubles(double[] w)
	{
		List<object> list = new List<object>();
		foreach (double num in w)
		{
			list.Add(num);
		}
		return list;
	}

	private static void Upsert(DbStore db, string code, string value, string type, string subType, string desc)
	{
		DbTable dbTable = db.Table("game_config");
		DbRow row = dbTable.Find((DbRow r) => DbStore.Str(r, "config_code") == code);
		if (row != null)
		{
			dbTable.Update((DbRow r) => r == row, new DbRow
			{
				["config_value"] = value,
				["update_time"] = Now()
			});
		}
		else
		{
			dbTable.Insert(new DbRow
			{
				["config_type"] = type,
				["config_sub_type"] = subType,
				["config_clazz"] = "java.lang.String",
				["config_code"] = code,
				["config_value"] = value,
				["config_desc"] = desc,
				["update_time"] = Now()
			});
		}
	}

	private static string Now()
	{
		return DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
	}
}
