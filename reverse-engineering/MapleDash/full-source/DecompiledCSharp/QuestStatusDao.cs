using System;
using System.Collections.Generic;

public static class QuestStatusDao
{
	public static List<DbRow> ByCharacter(DbStore db, int characterId)
	{
		return db.Table("queststatus").Select(DbStore.Eq("characterid", (long)characterId));
	}

	public static void Upsert(DbStore db, int characterId, int questId, int status, int completed = 0)
	{
		if (questId != 34151)
		{
			bool num = status == 2 && StatusOf(db, characterId, questId) == 1;
			DbTable dbTable = db.Table("queststatus");
			if (dbTable.Update((DbRow r) => SkillsDao.Eq64(r, "characterid", characterId) && SkillsDao.Eq64(r, "quest", questId), new DbRow
			{
				["status"] = (long)status,
				["completed"] = (long)completed
			}) == 0)
			{
				dbTable.Insert(new DbRow
				{
					["characterid"] = (long)characterId,
					["quest"] = (long)questId,
					["status"] = (long)status,
					["completed"] = (long)completed
				});
			}
			if (num)
			{
				NativeWeeklyGemstoneService.OnCompleted(db, characterId, questId);
			}
		}
	}

	public static int StatusOf(DbStore db, int characterId, int questId)
	{
		DbRow dbRow = db.Table("queststatus").Find((DbRow x) => SkillsDao.Eq64(x, "characterid", characterId) && SkillsDao.Eq64(x, "quest", questId));
		if (dbRow != null)
		{
			return (int)Convert.ToInt64(dbRow["status"]);
		}
		return 0;
	}
}
