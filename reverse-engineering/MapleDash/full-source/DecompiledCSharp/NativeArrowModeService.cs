public static class NativeArrowModeService
{
	public const int Off = 0;

	public const int Magic = 1;

	public const int Drain = 2;

	public static int Get(DbStore db, int characterId)
	{
		if (db == null || characterId <= 0)
		{
			return 0;
		}
		int quiverMode = GrowthDao.Load(db, characterId).quiverMode;
		if (quiverMode >= 0 && quiverMode <= 2)
		{
			return quiverMode;
		}
		return 0;
	}

	public static bool Set(DbStore db, int characterId, int mode)
	{
		if (db == null || characterId <= 0)
		{
			return false;
		}
		if (mode < 0 || mode > 2)
		{
			return false;
		}
		lock (db)
		{
			GrowthState growthState = GrowthDao.Load(db, characterId);
			if (growthState.quiverMode == mode)
			{
				return true;
			}
			growthState.quiverMode = mode;
			GrowthDao.Save(db, characterId, growthState);
			return true;
		}
	}
}
