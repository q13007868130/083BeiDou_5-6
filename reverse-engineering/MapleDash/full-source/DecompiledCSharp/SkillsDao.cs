using System;
using System.Collections.Generic;

public static class SkillsDao
{
	public static List<DbRow> ByCharacter(DbStore db, int characterId)
	{
		return db.Table("skills").Select(DbStore.Eq("characterid", (long)characterId));
	}

	public static void Upsert(DbStore db, int characterId, int skillId, int skillLevel, int masterLevel, long expiration = -1L)
	{
		DbTable dbTable = db.Table("skills");
		if (dbTable.Update((DbRow r) => Eq64(r, "characterid", characterId) && Eq64(r, "skillid", skillId), new DbRow
		{
			["skilllevel"] = (long)skillLevel,
			["masterlevel"] = (long)masterLevel,
			["expiration"] = expiration
		}) == 0)
		{
			dbTable.Insert(new DbRow
			{
				["skillid"] = (long)skillId,
				["characterid"] = (long)characterId,
				["skilllevel"] = (long)skillLevel,
				["masterlevel"] = (long)masterLevel,
				["expiration"] = expiration
			});
		}
	}

	internal static bool Eq64(DbRow r, string col, int v)
	{
		if (r.TryGetValue(col, out var value) && value != null)
		{
			return Convert.ToInt64(value) == v;
		}
		return false;
	}
}
