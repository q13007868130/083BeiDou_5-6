using System.Collections.Generic;

public static class GrowthDao
{
	public static GrowthState Load(DbStore db, int characterId)
	{
		GrowthState growthState = new GrowthState();
		DbRow dbRow = db.Table("character_growth").Find(DbStore.Eq("characterid", (long)characterId));
		if (dbRow == null)
		{
			growthState.soulMax = 1000;
			return growthState;
		}
		growthState.honorExp = DbStore.Long(dbRow, "honorExp", 0L);
		growthState.hyperPreset = Clamp(DbStore.Int(dbRow, "hyperPreset"));
		growthState.abilityPreset = Clamp(DbStore.Int(dbRow, "abilityPreset"));
		growthState.abilityLock = DbStore.Str(dbRow, "abilityLock") ?? "";
		growthState.abilityChoice = DbStore.Str(dbRow, "abilityChoice") ?? "";
		growthState.soulMp = DbStore.Int(dbRow, "soulMp");
		growthState.soulMax = DbStore.Int(dbRow, "soulMax");
		if (growthState.soulMax <= 0)
		{
			growthState.soulMax = 1000;
		}
		int num = DbStore.Int(dbRow, "quiverMode");
		growthState.quiverMode = ((num >= 0 && num <= 2) ? num : 0);
		return growthState;
	}

	public static void Save(DbStore db, int characterId, GrowthState state)
	{
		if (state != null)
		{
			DbRow dbRow = new DbRow
			{
				["honorExp"] = state.honorExp,
				["hyperPreset"] = (long)state.hyperPreset,
				["abilityPreset"] = (long)state.abilityPreset,
				["abilityLock"] = state.abilityLock ?? "",
				["abilityChoice"] = state.abilityChoice ?? "",
				["soulMp"] = (long)state.soulMp,
				["soulMax"] = (long)state.soulMax,
				["quiverMode"] = (long)state.quiverMode
			};
			DbTable dbTable = db.Table("character_growth");
			if (dbTable.Update(DbStore.Eq("characterid", (long)characterId), dbRow) == 0)
			{
				dbRow["characterid"] = (long)characterId;
				dbTable.Insert(dbRow);
			}
		}
	}

	public static List<HyperStatEntry> HyperStats(DbStore db, int characterId)
	{
		List<HyperStatEntry> list = new List<HyperStatEntry>();
		foreach (DbRow item in db.Table("hyperstats").Select(DbStore.Eq("characterid", (long)characterId)))
		{
			list.Add(new HyperStatEntry
			{
				preset = Clamp(DbStore.Int(item, "preset")),
				skillId = DbStore.Int(item, "skillid"),
				slv = DbStore.Int(item, "slv")
			});
		}
		list.Sort((HyperStatEntry a, HyperStatEntry b) => (a.preset == b.preset) ? a.skillId.CompareTo(b.skillId) : a.preset.CompareTo(b.preset));
		return list;
	}

	public static void SaveHyperStats(DbStore db, int characterId, int preset, IEnumerable<HyperStatEntry> rows)
	{
		DbTable dbTable = db.Table("hyperstats");
		dbTable.Delete((DbRow r) => SkillsDao.Eq64(r, "characterid", characterId) && SkillsDao.Eq64(r, "preset", preset));
		if (rows == null)
		{
			return;
		}
		foreach (HyperStatEntry row in rows)
		{
			if (row != null && row.skillId > 0 && row.slv > 0)
			{
				dbTable.Insert(new DbRow
				{
					["characterid"] = (long)characterId,
					["preset"] = (long)preset,
					["skillid"] = (long)row.skillId,
					["slv"] = (long)row.slv
				});
			}
		}
	}

	public static List<PotentialEntry> Potentials(DbStore db, int characterId)
	{
		List<PotentialEntry> list = new List<PotentialEntry>();
		foreach (DbRow item in db.Table("characterpotentials").Select(DbStore.Eq("characterid", (long)characterId)))
		{
			list.Add(new PotentialEntry
			{
				preset = Clamp(DbStore.Int(item, "preset")),
				potKey = DbStore.Int(item, "potkey"),
				skillId = DbStore.Int(item, "skillid"),
				slv = DbStore.Int(item, "slv"),
				grade = DbStore.Int(item, "grade")
			});
		}
		list.Sort((PotentialEntry a, PotentialEntry b) => (a.preset == b.preset) ? a.potKey.CompareTo(b.potKey) : a.preset.CompareTo(b.preset));
		return list;
	}

	public static void SavePotentials(DbStore db, int characterId, int preset, IEnumerable<PotentialEntry> rows)
	{
		DbTable dbTable = db.Table("characterpotentials");
		dbTable.Delete((DbRow r) => SkillsDao.Eq64(r, "characterid", characterId) && SkillsDao.Eq64(r, "preset", preset));
		if (rows == null)
		{
			return;
		}
		foreach (PotentialEntry row in rows)
		{
			if (row != null && row.potKey >= 0 && row.potKey <= 2)
			{
				dbTable.Insert(new DbRow
				{
					["characterid"] = (long)characterId,
					["preset"] = (long)preset,
					["potkey"] = (long)row.potKey,
					["skillid"] = (long)row.skillId,
					["slv"] = (long)row.slv,
					["grade"] = (long)row.grade
				});
			}
		}
	}

	private static int Clamp(int preset)
	{
		if (preset >= 0)
		{
			if (preset <= 2)
			{
				return preset;
			}
			return 2;
		}
		return 0;
	}
}
