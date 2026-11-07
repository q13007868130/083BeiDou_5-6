using System;
using System.Collections.Generic;

public static class KeymapDao
{
	public static readonly int[] DefaultKeys = new int[40]
	{
		18, 65, 2, 23, 3, 4, 5, 6, 16, 17,
		19, 25, 26, 27, 31, 34, 35, 37, 38, 40,
		43, 44, 45, 46, 50, 56, 59, 60, 61, 62,
		63, 64, 57, 48, 29, 7, 24, 33, 41, 39
	};

	public static readonly int[] DefaultTypes = new int[40]
	{
		4, 6, 4, 4, 4, 4, 4, 4, 4, 4,
		4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
		4, 5, 5, 4, 4, 5, 6, 6, 6, 6,
		6, 6, 5, 4, 5, 4, 4, 4, 4, 4
	};

	public static readonly int[] DefaultActions = new int[40]
	{
		0, 106, 10, 1, 12, 13, 18, 24, 8, 5,
		4, 19, 14, 15, 2, 17, 11, 3, 20, 16,
		9, 50, 51, 6, 7, 53, 100, 101, 102, 103,
		104, 105, 54, 22, 52, 21, 25, 26, 23, 27
	};

	public static List<DbRow> ByCharacter(DbStore db, int characterId)
	{
		List<DbRow> list = db.Table("keymap").Select(DbStore.Eq("characterid", (long)characterId));
		if (list.Count > 0 && list.TrueForAll((DbRow r) => !r.ContainsKey("key") || Convert.ToInt64(r["key"]) == 0))
		{
			db.Table("keymap").Delete(DbStore.Eq("characterid", (long)characterId));
			SeedDefaults(db, characterId);
			list = db.Table("keymap").Select(DbStore.Eq("characterid", (long)characterId));
		}
		return list;
	}

	public static void Save(DbStore db, int characterId, List<KeymapEntry> entries)
	{
		DbTable dbTable = db.Table("keymap");
		dbTable.Delete(DbStore.Eq("characterid", (long)characterId));
		foreach (KeymapEntry entry in entries)
		{
			dbTable.Insert(new DbRow
			{
				["characterid"] = (long)characterId,
				["key"] = (long)entry.key,
				["type"] = (long)entry.type,
				["action"] = (long)entry.action
			});
		}
	}

	public static void SeedDefaults(DbStore db, int characterId)
	{
		DbTable dbTable = db.Table("keymap");
		int num = Math.Min(Math.Min(DefaultKeys.Length, DefaultTypes.Length), DefaultActions.Length);
		for (int i = 0; i < num; i++)
		{
			dbTable.Insert(new DbRow
			{
				["characterid"] = (long)characterId,
				["key"] = (long)DefaultKeys[i],
				["type"] = (long)DefaultTypes[i],
				["action"] = (long)DefaultActions[i]
			});
		}
	}
}
