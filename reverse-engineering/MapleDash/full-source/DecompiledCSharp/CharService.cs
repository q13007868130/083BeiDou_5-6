using System;
using System.Collections.Generic;
using System.Globalization;

public static class CharService
{
	public static FullCharacter Load(DbStore db, int characterId)
	{
		CharRecord charRecord = CharactersDao.FindById(db, characterId);
		if (charRecord == null)
		{
			return null;
		}
		bool flag = false;
		bool flag2 = false;
		foreach (DbRow item in InventoryDao.ItemsByCharacter(db, characterId))
		{
			int num = DbStore.Int(item, "position");
			int num2 = DbStore.Int(item, "itemid");
			long inventoryItemId = DbStore.Long(item, "inventoryitemid", 0L);
			if (num2 / 1000000 == 1 && InventoryDao.EquipmentOf(db, inventoryItemId) == null)
			{
				flag2 |= InventoryDao.EnsureEquipment(db, inventoryItemId) != null;
			}
			if (DbStore.Int(item, "inventorytype") == 1 && NativeAndroidService.IsApSlot(num) && !NativeAndroidService.ApAccepts(num, NativeInventoryService.EquipmentSlotOf(num2)))
			{
				int num3 = NativeInventoryService.Free(db, charRecord, 1);
				if (num3 != 0)
				{
					item["position"] = (long)num3;
					flag = true;
				}
			}
		}
		if (flag | flag2)
		{
			db.Save();
		}
		if (InventoryDao.ExpireItems(db, characterId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()))
		{
			db.Save();
		}
		FullCharacter fullCharacter = new FullCharacter
		{
			record = charRecord,
			soulWeapon = NativeSoulWeaponWz2Service.Read(db, characterId)
		};
		if (NativeMatrixSkills.Sync(db, characterId))
		{
			db.Save();
		}
		foreach (DbRow item2 in SkillsDao.ByCharacter(db, characterId))
		{
			fullCharacter.skills.Add(new SkillEntry
			{
				skillId = (int)Convert(item2, "skillid"),
				skillLevel = (int)Convert(item2, "skilllevel"),
				masterLevel = (int)Convert(item2, "masterlevel"),
				expiration = Convert(item2, "expiration")
			});
		}
		foreach (DbRow item3 in InventoryDao.ItemsByCharacter(db, characterId))
		{
			fullCharacter.inventory.Add(ToInventoryEntry(db, item3));
		}
		foreach (DbRow item4 in KeymapDao.ByCharacter(db, characterId))
		{
			fullCharacter.keymap.Add(new KeymapEntry
			{
				key = (int)Convert(item4, "key"),
				type = (int)Convert(item4, "type"),
				action = (int)Convert(item4, "action")
			});
		}
		List<DbRow> list = new List<DbRow>(db.Table("skillmacros").Select(DbStore.Eq("characterid", characterId)));
		list.Sort((DbRow a, DbRow b) => Convert(a, "position").CompareTo(Convert(b, "position")));
		foreach (DbRow item5 in list)
		{
			fullCharacter.macros.Add(new MacroEntry
			{
				position = (int)Convert(item5, "position"),
				name = (DbStore.Str(item5, "name") ?? ""),
				shout = (int)Convert(item5, "shout"),
				skill1 = (int)Convert(item5, "skill1"),
				skill2 = (int)Convert(item5, "skill2"),
				skill3 = (int)Convert(item5, "skill3")
			});
		}
		foreach (DbRow item6 in QuestStatusDao.ByCharacter(db, characterId))
		{
			fullCharacter.quests.Add(new QuestEntry
			{
				quest = (int)Convert(item6, "quest"),
				status = (int)Convert(item6, "status"),
				completed = (int)Convert(item6, "completed"),
				time = Convert(item6, "time"),
				expires = Convert(item6, "expires")
			});
		}
		fullCharacter.quickslotKeymap = QuickslotDao.Load(db, charRecord.accountid);
		fullCharacter.growth = GrowthDao.Load(db, characterId);
		fullCharacter.hyperstats = GrowthDao.HyperStats(db, characterId);
		fullCharacter.potentials = GrowthDao.Potentials(db, characterId);
		fullCharacter.hexaStats = NativeHexaMatrixService.Stats(db, characterId);
		return fullCharacter;
	}

	public static InventoryEntry ToInventoryEntry(DbStore db, DbRow r)
	{
		InventoryEntry e = new InventoryEntry
		{
			itemRowId = Convert(r, "inventoryitemid"),
			expiration = Convert(r, "expiration"),
			itemId = (int)Convert(r, "itemid"),
			inventoryType = (int)Convert(r, "inventorytype"),
			position = (int)Convert(r, "position"),
			quantity = (int)Convert(r, "quantity"),
			flag = (int)Convert(r, "flag"),
			owner = DbStore.Str(r, "owner"),
			giftFrom = DbStore.Str(r, "giftFrom"),
			petId = DbStore.Int(r, "petid", -1),
			itemData = new Dictionary<string, object>(r)
		};
		if (e.petId >= 0 && db.HasTable("pets"))
		{
			DbRow dbRow = db.Table("pets").Find((DbRow x) => DbStore.Int(x, "petid") == e.petId);
			if (dbRow != null)
			{
				e.petData = new Dictionary<string, object>(dbRow);
			}
		}
		DbRow dbRow2 = InventoryDao.EquipmentOf(db, e.itemRowId);
		if (dbRow2 != null)
		{
			e.isEquip = true;
			e.equipmentData = new Dictionary<string, object>(dbRow2);
			e.str = (int)Convert(dbRow2, "str");
			e.dex = (int)Convert(dbRow2, "dex");
			e.int_ = (int)Convert(dbRow2, "int");
			e.luk = (int)Convert(dbRow2, "luk");
			e.hp = (int)Convert(dbRow2, "hp");
			e.mp = (int)Convert(dbRow2, "mp");
			e.watk = (int)Convert(dbRow2, "watk");
			e.matk = (int)Convert(dbRow2, "matk");
			e.wdef = (int)Convert(dbRow2, "wdef");
			e.mdef = (int)Convert(dbRow2, "mdef");
			e.acc = (int)Convert(dbRow2, "acc");
			e.avoid = (int)Convert(dbRow2, "avoid");
			e.hands = (int)Convert(dbRow2, "hands");
			e.speed = (int)Convert(dbRow2, "speed");
			e.jump = (int)Convert(dbRow2, "jump");
			e.upgradeslots = (int)Convert(dbRow2, "upgradeslots");
			e.equipLevel = (int)Convert(dbRow2, "level");
			e.maxStar = (int)Convert(dbRow2, "maxStar");
			e.starLevel = (int)Convert(dbRow2, "starLevel");
			e.starCount = (int)Convert(dbRow2, "starCount");
			e.symbolLevel = (int)Convert(dbRow2, "symbolLevel");
			e.symbolExp = (int)Convert(dbRow2, "symbolExp");
			e.symbolInc = (int)Convert(dbRow2, "symbolInc");
			e.exGradeOption = Convert(dbRow2, "exGradeOption");
			e.flameChoice = DbStore.Str(dbRow2, "flameChoice") ?? "";
			e.flameRevision = (int)Convert(dbRow2, "flameRevision");
			e.soulSocketId = (int)Convert(dbRow2, "soulSocketId");
			e.soulItemId = (int)Convert(dbRow2, "soulItemId");
			e.soulOptionId = (int)Convert(dbRow2, "soulOptionId");
			e.soulOption = (int)Convert(dbRow2, "soulOption");
		}
		return e;
	}

	public static void Save(DbStore db, FullCharacter full)
	{
		if (full == null || full.record == null)
		{
			return;
		}
		CharactersDao.Save(db, full.record);
		int num = (int)full.record.id;
		KeymapDao.Save(db, num, full.keymap);
		db.Table("skillmacros").Delete(DbStore.Eq("characterid", num));
		foreach (MacroEntry macro in full.macros)
		{
			if (macro != null && !macro.IsEmpty)
			{
				db.Table("skillmacros").Insert(new DbRow
				{
					["characterid"] = num,
					["position"] = macro.position,
					["skill1"] = macro.skill1,
					["skill2"] = macro.skill2,
					["skill3"] = macro.skill3,
					["name"] = macro.name,
					["shout"] = macro.shout
				});
			}
		}
		Dictionary<int, int> dictionary = NativeMatrixSkills.Levels(db, num);
		foreach (SkillEntry skill in full.skills)
		{
			if (!dictionary.ContainsKey(skill.skillId))
			{
				SkillsDao.Upsert(db, num, skill.skillId, skill.skillLevel, skill.masterLevel, skill.expiration);
			}
		}
		foreach (QuestEntry quest in full.quests)
		{
			QuestStatusDao.Upsert(db, num, quest.quest, quest.status, quest.completed);
		}
	}

	public static long AddItem(DbStore db, int characterId, int itemId, int inventoryType, int position, int quantity, bool equipStats = false)
	{
		long num = InventoryDao.InsertItem(db, characterId, itemId, inventoryType, position, quantity);
		if (itemId / 1000000 == 1)
		{
			InventoryDao.EnsureEquipment(db, num);
		}
		else if (equipStats)
		{
			InventoryDao.InsertEquipment(db, num);
		}
		return num;
	}

	public static void RemoveItem(DbStore db, long inventoryItemId)
	{
		db.Table("inventoryitems").Delete(DbStore.Eq("inventoryitemid", inventoryItemId));
		db.Table("inventoryequipment").Delete(DbStore.Eq("inventoryitemid", inventoryItemId));
	}

	public static int CountItem(DbStore db, int characterId, int itemId)
	{
		int num = 0;
		foreach (DbRow item in InventoryDao.ItemsByCharacter(db, characterId))
		{
			if (Convert(item, "itemid") == itemId)
			{
				num += (int)Convert(item, "quantity");
			}
		}
		return num;
	}

	private static long Convert(DbRow r, string k)
	{
		if (r == null || !r.TryGetValue(k, out var value) || value == null)
		{
			return 0L;
		}
		try
		{
			return System.Convert.ToInt64(value, CultureInfo.InvariantCulture);
		}
		catch
		{
			return 0L;
		}
	}
}
