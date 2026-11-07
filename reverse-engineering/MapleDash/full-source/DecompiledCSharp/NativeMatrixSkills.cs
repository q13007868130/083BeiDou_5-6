using System;
using System.Collections.Generic;

public static class NativeMatrixSkills
{
	public static Dictionary<int, int> Levels(DbStore db, int characterId)
	{
		Dictionary<int, int> result = new Dictionary<int, int>();
		CharRecord charRecord = CharactersDao.FindById(db, characterId);
		if (charRecord == null)
		{
			return result;
		}
		bool flag = db.Table("vmatrix_state").Find(DbStore.Eq("characterid", (long)characterId)) != null;
		bool flag2 = db.Table("hexa_state").Find(DbStore.Eq("characterid", (long)characterId)) != null;
		VMatrixGroup vMatrixGroup = VMatrixCatalog.Group(charRecord.job);
		HexaGroup hexaGroup = HexaCatalog.Group(charRecord.job);
		if (flag && vMatrixGroup != null)
		{
			int[] array = VMatrixSlots.Assign(vMatrixGroup);
			foreach (int id in array)
			{
				VMatrixCore vMatrixCore = VMatrixCatalog.Core(id);
				if (vMatrixCore != null)
				{
					DbRow r = db.Table("vmatrix_cores").Find((DbRow r3) => DbStore.Int(r3, "characterid") == characterId && DbStore.Int(r3, "coreid") == id);
					int level = Math.Min(vMatrixCore.maxLevel, Math.Max(0, DbStore.Int(r, "level")));
					Merge(result, vMatrixCore.skills, level);
					Merge(result, vMatrixCore.subSkills, level);
				}
			}
		}
		if (flag2 && hexaGroup != null)
		{
			foreach (int id2 in hexaGroup.All)
			{
				HexaCore hexaCore = HexaCatalog.Core(id2);
				if (hexaCore != null)
				{
					DbRow r2 = db.Table("hexa_skills").Find((DbRow r3) => DbStore.Int(r3, "characterid") == characterId && DbStore.Int(r3, "coreid") == id2);
					int level2 = Math.Min(hexaCore.maxLevel, Math.Max(0, DbStore.Int(r2, "level")));
					Merge(result, hexaCore.skills, level2);
					Merge(result, hexaCore.subSkills, level2);
				}
			}
		}
		return result;
	}

	private static void Merge(Dictionary<int, int> result, int[] skills, int level)
	{
		foreach (int key in skills)
		{
			result[key] = Math.Max(result.TryGetValue(key, out var value) ? value : 0, level);
		}
	}

	public static bool Sync(DbStore db, int characterId)
	{
		bool result = false;
		List<DbRow> list = SkillsDao.ByCharacter(db, characterId);
		foreach (KeyValuePair<int, int> pair in Levels(db, characterId))
		{
			DbRow dbRow = list.Find((DbRow r) => DbStore.Int(r, "skillid") == pair.Key);
			if ((dbRow != null || pair.Value != 0) && (dbRow == null || DbStore.Int(dbRow, "skilllevel") != pair.Value || DbStore.Int(dbRow, "masterlevel") < pair.Value))
			{
				SkillsDao.Upsert(db, characterId, pair.Key, pair.Value, Math.Max(pair.Value, DbStore.Int(dbRow, "masterlevel")), -1L);
				result = true;
			}
		}
		return result;
	}
}
