using System;

public static class QuickslotDao
{
	public static void Save(DbStore db, int accountId, long keymap)
	{
		DbTable dbTable = db.Table("quickslotkeymapped");
		dbTable.Update(DbStore.Eq("accountid", (long)accountId), new DbRow { ["keymap"] = keymap });
		if (dbTable.Find(DbStore.Eq("accountid", (long)accountId)) == null)
		{
			dbTable.Insert(new DbRow
			{
				["accountid"] = (long)accountId,
				["keymap"] = keymap
			});
		}
	}

	public static long Load(DbStore db, int accountId)
	{
		DbRow dbRow = db.Table("quickslotkeymapped").Find(DbStore.Eq("accountid", (long)accountId));
		if (dbRow != null)
		{
			return Convert.ToInt64(dbRow["keymap"]);
		}
		return 0L;
	}
}
