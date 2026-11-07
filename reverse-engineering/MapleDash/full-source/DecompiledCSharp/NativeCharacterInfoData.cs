using System;
using System.Collections.Generic;
using Server.Bot;

public sealed class NativeCharacterInfoData
{
	public string guild = "-";

	public readonly List<InventoryEntry> pets = new List<InventoryEntry>();

	public readonly List<int> wishes = new List<int>();

	public readonly List<int> items = new List<int>();

	public readonly List<int> medalQuests = new List<int>();

	public readonly List<int> mountItems = new List<int>();

	public readonly List<List<int>> petItems = new List<List<int>>
	{
		new List<int>(),
		new List<int>(),
		new List<int>()
	};

	public int mount;

	public int mountLevel;

	public int mountExp;

	public int mountTiredness;

	public int medal;

	public int petClothing;

	public int bookLevel = 1;

	public int bookCover;

	public int normalCards;

	public int specialCards;

	public static NativeCharacterInfoData Read(DbStore db, CharRecord self, BotActor actor)
	{
		NativeCharacterInfoData nativeCharacterInfoData = new NativeCharacterInfoData();
		FullCharacter fullCharacter = ((actor == null) ? CharService.Load(db, (int)self.id) : actor.character);
		CharRecord record = fullCharacter?.record;
		int cid = actor?.id ?? ((int)self.id);
		List<InventoryEntry> list = fullCharacter?.inventory ?? (actor as BotCharacter)?.inventory;
		if (list != null)
		{
			foreach (InventoryEntry item in list)
			{
				if (item.petData != null && Convert.ToInt32(item.petData["summoned"]) == 1 && nativeCharacterInfoData.pets.Count < 3)
				{
					nativeCharacterInfoData.pets.Add(item.Copy());
				}
				if (item.inventoryType != 1 || item.position >= 0)
				{
					continue;
				}
				if (item.position == -18)
				{
					nativeCharacterInfoData.mount = item.itemId;
				}
				if (item.position == -49)
				{
					nativeCharacterInfoData.medal = item.itemId;
				}
				if (item.position == -14 && item.itemId / 10000 == 180)
				{
					nativeCharacterInfoData.petClothing = item.itemId;
				}
				if (item.position == -18 || item.position == -19 || item.position == -20)
				{
					nativeCharacterInfoData.mountItems.Add(item.itemId);
				}
				for (int i = 0; i < 3; i++)
				{
					if (NativePetItems.IsEquipment(item.itemId) && NativePetItems.EquipmentSlot(item.itemId, i) == item.position)
					{
						nativeCharacterInfoData.petItems[i].Add(item.itemId);
					}
				}
			}
		}
		if (record != null)
		{
			DbRow dbRow = db.Table("guilds").Find((DbRow r) => DbStore.Int(r, "guildid") == record.guildid);
			if (dbRow != null)
			{
				nativeCharacterInfoData.guild = DbStore.Str(dbRow, "name");
			}
			nativeCharacterInfoData.mountLevel = record.mountlevel;
			nativeCharacterInfoData.mountExp = record.mountexp;
			nativeCharacterInfoData.mountTiredness = record.mounttiredness;
			nativeCharacterInfoData.bookCover = record.monsterbookcover;
		}
		if (actor is BotCharacter botCharacter && nativeCharacterInfoData.mount == 0 && botCharacter.equips.TryGetValue(-18, out var value))
		{
			nativeCharacterInfoData.mount = value;
		}
		int num = actor?.level ?? record?.level ?? 0;
		if (nativeCharacterInfoData.mount != 0 && BotItemCatalog.Int(BotItemCatalog.Shared.Info(nativeCharacterInfoData.mount), "reqLevel") > num)
		{
			nativeCharacterInfoData.mount = 0;
		}
		SortedDictionary<int, List<InventoryEntry>> sortedDictionary = new SortedDictionary<int, List<InventoryEntry>>();
		if (list != null)
		{
			foreach (InventoryEntry item2 in list)
			{
				if (item2.inventoryType != 1 || item2.position >= 0)
				{
					continue;
				}
				int num2 = -item2.position;
				if (num2 > 100)
				{
					num2 -= 100;
				}
				if (num2 > 0 && num2 != 14 && num2 != 18 && num2 != 19 && num2 != 20 && !NativePetItems.IsEquipment(item2.itemId))
				{
					if (!sortedDictionary.TryGetValue(num2, out var value2))
					{
						value2 = (sortedDictionary[num2] = new List<InventoryEntry>());
					}
					value2.Add(item2);
				}
			}
		}
		if (actor is BotCharacter botCharacter2)
		{
			foreach (KeyValuePair<int, int> pair in botCharacter2.equips)
			{
				int num3 = -pair.Key;
				if (num3 > 100)
				{
					num3 -= 100;
				}
				if (num3 > 0 && num3 != 14 && num3 != 18 && num3 != 19 && num3 != 20 && !NativePetItems.IsEquipment(pair.Value))
				{
					if (!sortedDictionary.TryGetValue(num3, out var value3))
					{
						value3 = (sortedDictionary[num3] = new List<InventoryEntry>());
					}
					if (!value3.Exists((InventoryEntry item) => item.itemId == pair.Value))
					{
						value3.Add(new InventoryEntry
						{
							itemId = pair.Value,
							position = pair.Key
						});
					}
				}
			}
		}
		foreach (List<InventoryEntry> value4 in sortedDictionary.Values)
		{
			value4.Sort((InventoryEntry a, InventoryEntry b) => b.position.CompareTo(a.position));
			foreach (InventoryEntry item3 in value4)
			{
				nativeCharacterInfoData.items.Add(item3.itemId);
			}
		}
		if (actor == null)
		{
			nativeCharacterInfoData.pets.Clear();
			for (int num4 = 0; num4 < 3; num4++)
			{
				InventoryEntry pet = NativePetItems.GetPet(db, cid, num4);
				if (pet != null)
				{
					nativeCharacterInfoData.pets.Add(pet.Copy());
				}
			}
		}
		foreach (DbRow item4 in from r in db.Table("cash_wishlist")
			select DbStore.Int(r, "characterid") == cid)
		{
			nativeCharacterInfoData.wishes.Add(DbStore.Int(item4, "sn"));
		}
		foreach (DbRow item5 in from r in db.Table("monsterbook")
			select DbStore.Int(r, "charid") == cid)
		{
			if (DbStore.Int(item5, "cardid") / 1000 >= 2388)
			{
				nativeCharacterInfoData.specialCards++;
			}
			else
			{
				nativeCharacterInfoData.normalCards++;
			}
		}
		int num5 = 1;
		while (true)
		{
			num5 += nativeCharacterInfoData.bookLevel * 10;
			if (nativeCharacterInfoData.normalCards + nativeCharacterInfoData.specialCards < num5)
			{
				break;
			}
			nativeCharacterInfoData.bookLevel++;
		}
		if (fullCharacter != null)
		{
			foreach (QuestEntry quest in fullCharacter.quests)
			{
				if (quest.status == 2 && quest.quest >= 29000)
				{
					nativeCharacterInfoData.medalQuests.Add(quest.quest);
				}
			}
		}
		nativeCharacterInfoData.medalQuests.Sort();
		return nativeCharacterInfoData;
	}
}
