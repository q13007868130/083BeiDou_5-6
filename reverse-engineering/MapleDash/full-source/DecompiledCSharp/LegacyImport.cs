using System;

public static class LegacyImport
{
	public static int Import(DbStore db, LegacyProfileSnapshot s)
	{
		if (s == null)
		{
			return 0;
		}
		if (AccountsDao.Count(db) > 0)
		{
			return 0;
		}
		string[] array = new string[1] { "characters" };
		foreach (string name in array)
		{
			if (db.Table(name).rows.Count > 0)
			{
				return 0;
			}
		}
		int accountid = AccountsDao.AutoRegister(db, "local", "local");
		CharRecord charRecord = new CharRecord
		{
			accountid = accountid,
			name = (string.IsNullOrEmpty(s.name) ? "Migrated" : s.name),
			level = ((s.level < 1) ? 1 : s.level),
			exp = Math.Max(0L, s.exp),
			str = s.str,
			dex = s.dex,
			int_ = s.int_,
			luk = s.luk,
			hp = 50,
			maxhp = ((s.maxHp > 0) ? s.maxHp : 50),
			mp = 5,
			maxmp = ((s.maxMp > 0) ? s.maxMp : 5),
			job = s.job,
			map = s.map,
			hair = 30000,
			face = 20000,
			ap = s.ap,
			meso = s.meso,
			equipslots = ((s.equipSlots > 0) ? s.equipSlots : 24),
			useslots = 24,
			setupslots = 24,
			etcslots = 24,
			fishLevel = 1
		};
		charRecord.SetSp(JobBook.Of(charRecord.job), s.sp);
		db.BeginTransaction();
		try
		{
			int num = (int)CharactersDao.Insert(db, charRecord);
			KeymapDao.SeedDefaults(db, num);
			foreach (LegacySkillEntry skill in s.skills)
			{
				if (skill.skillLevel > 0)
				{
					SkillsDao.Upsert(db, num, skill.skillId, skill.skillLevel, skill.skillLevel, -1L);
				}
			}
			db.Commit();
			db.Save();
			return num;
		}
		catch
		{
			db.Rollback();
			return 0;
		}
	}
}
