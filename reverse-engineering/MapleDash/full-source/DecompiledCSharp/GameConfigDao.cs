using System;
using System.Globalization;

public static class GameConfigDao
{
	public static void EnsureLevelCaps(DbStore db)
	{
		string[] array = new string[3] { "mxj_max_level", "qst_max_level", "max_reborn_count" };
		foreach (string code in array)
		{
			if (db.Table("game_config").Find((DbRow r) => DbStore.Str(r, "config_code") == code) == null)
			{
				db.Table("game_config").Insert(new DbRow
				{
					["config_type"] = "server",
					["config_sub_type"] = "Game Mechanics",
					["config_clazz"] = "java.lang.Integer",
					["config_code"] = code,
					["config_value"] = ((code == "max_reborn_count") ? "10" : "255")
				});
			}
		}
		if (db.Table("game_config").Find((DbRow r) => DbStore.Str(r, "config_code") == "use_rebirth_system") == null)
		{
			db.Table("game_config").Insert(new DbRow
			{
				["config_type"] = "server",
				["config_sub_type"] = "Game Mechanics",
				["config_clazz"] = "java.lang.Boolean",
				["config_code"] = "use_rebirth_system",
				["config_value"] = "true"
			});
		}
	}

	public static string Get(DbStore db, string code, string fallback = "")
	{
		DbRow dbRow = db.Table("game_config").Find((DbRow x) => (string)x["config_code"] == code);
		if (dbRow != null)
		{
			return (string)dbRow["config_value"];
		}
		return fallback;
	}

	public static bool GetBool(DbStore db, string code, bool fallback = false)
	{
		DbRow dbRow = db.Table("game_config").Find((DbRow x) => (string)x["config_code"] == code);
		if (dbRow == null)
		{
			return fallback;
		}
		string text = ((string)dbRow["config_value"]).Trim();
		if (!(text == "1"))
		{
			return text.Equals("true", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	public static int GetInt(DbStore db, string code, int fallback = 0)
	{
		DbRow dbRow = db.Table("game_config").Find((DbRow x) => (string)x["config_code"] == code);
		if (dbRow != null)
		{
			if (!int.TryParse((string)dbRow["config_value"], out var result))
			{
				return fallback;
			}
			return result;
		}
		return fallback;
	}

	public static double GetFloat(DbStore db, string code, double fallback = 0.0)
	{
		DbRow dbRow = db.Table("game_config").Find((DbRow x) => (string)x["config_code"] == code);
		if (dbRow != null)
		{
			if (!double.TryParse((string)dbRow["config_value"], NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				return fallback;
			}
			return result;
		}
		return fallback;
	}
}
