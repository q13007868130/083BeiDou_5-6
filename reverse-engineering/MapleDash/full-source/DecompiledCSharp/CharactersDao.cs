using System;
using System.Collections.Generic;
using System.Globalization;

public static class CharactersDao
{
	public static List<CharRecord> ByAccount(DbStore db, int accountId, int world = 0)
	{
		List<CharRecord> list = new List<CharRecord>();
		foreach (DbRow item in db.Table("characters").Select(DbStore.Eq("accountid", (long)accountId)))
		{
			CharRecord charRecord = CharRecord.FromRow(item);
			charRecord.gm = Math.Max(charRecord.gm, AccountsDao.DefaultGmLevel(db, charRecord.accountid));
			if (world == 0 || charRecord.world == world)
			{
				list.Add(charRecord);
			}
		}
		list.Sort((CharRecord a, CharRecord b) => a.id.CompareTo(b.id));
		return list;
	}

	public static CharRecord FindById(DbStore db, int id)
	{
		DbRow dbRow = db.Table("characters").Find(DbStore.Eq("id", (long)id));
		if (dbRow == null)
		{
			return null;
		}
		CharRecord charRecord = CharRecord.FromRow(dbRow);
		charRecord.gm = Math.Max(charRecord.gm, AccountsDao.DefaultGmLevel(db, charRecord.accountid));
		return charRecord;
	}

	public static bool NameTaken(DbStore db, string name)
	{
		return db.Table("characters").Find(DbStore.Eq("name", name)) != null;
	}

	public static long Insert(DbStore db, CharRecord c)
	{
		return db.Table("characters").Insert(c.ToInsertRow());
	}

	public static void Save(DbStore db, CharRecord c)
	{
		db.Table("characters").Update(DbStore.Eq("id", c.id), c.ToUpdateRow());
	}

	public static void Delete(DbStore db, int characterId)
	{
		new NativeCommunityService(db).DeleteCharacter(characterId);
		new NativeBossService(db).DeleteCharacter(characterId);
		db.Table("characters").Delete(DbStore.Eq("id", (long)characterId));
		db.Table("skills").Delete(DbStore.Eq("characterid", (long)characterId));
		db.Table("keymap").Delete(DbStore.Eq("characterid", (long)characterId));
		db.Table("inventoryitems").Delete(DbStore.Eq("characterid", (long)characterId));
		db.Table("queststatus").Delete(DbStore.Eq("characterid", (long)characterId));
		db.Table("questprogress").Delete(DbStore.Eq("characterid", (long)characterId));
		db.Table("buddies").Delete((DbRow r) => DbStore.Int(r, "characterid") == characterId || DbStore.Int(r, "buddyid") == characterId);
		db.Table("monsterbook").Delete(DbStore.Eq("charid", (long)characterId));
		db.Table("savedlocations").Delete(DbStore.Eq("characterid", (long)characterId));
		db.Table("extend_value").Delete((DbRow r) => DbStore.Str(r, "extend_id") == characterId.ToString(CultureInfo.InvariantCulture) && DbStore.Int(r, "extend_type") >= 21 && DbStore.Int(r, "extend_type") <= 23);
	}

	public static bool CanCreateChar(string name)
	{
		return CharNameRules.Valid(name);
	}
}
