public static class SavedLocationsDao
{
	public static void Save(DbStore db, int characterId, string locationType, int map, int portal)
	{
		DbTable dbTable = db.Table("savedlocations");
		if (dbTable.Update((DbRow r) => SkillsDao.Eq64(r, "characterid", characterId) && (string)r["locationtype"] == locationType, new DbRow
		{
			["map"] = (long)map,
			["portal"] = (long)portal
		}) == 0)
		{
			dbTable.Insert(new DbRow
			{
				["characterid"] = (long)characterId,
				["locationtype"] = locationType,
				["map"] = (long)map,
				["portal"] = (long)portal
			});
		}
	}

	public static DbRow Find(DbStore db, int characterId, string locationType)
	{
		return db.Table("savedlocations").Find((DbRow r) => SkillsDao.Eq64(r, "characterid", characterId) && (string)r["locationtype"] == locationType);
	}
}
