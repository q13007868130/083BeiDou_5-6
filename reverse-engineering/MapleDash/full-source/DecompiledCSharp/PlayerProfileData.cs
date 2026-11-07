using System;
using System.Collections.Generic;

[Serializable]
public sealed class PlayerProfileData
{
	public int version = 1;

	public CharacterAttributes attributes = new CharacterAttributes();

	public List<string> migratedLegacyWeapons = new List<string>();

	public long gold;

	public long experience;

	public int level = 1;

	public int baseMaxHp;

	public int baseMaxMp;

	public bool noviceApMigrated;

	public int ap;

	public string playerName = "";

	public int equipmentSlots = 24;

	public string cls = "beginner";

	public int jobId;

	public int sp;

	public string equipWeapon = "1302000";

	public int equipHat;

	public int equipTop;

	public int equipBottom;

	public int equipShoes;

	public int equipGlove;

	public int equipCape;

	public int equipShield;

	public List<int> accessorySlots = new List<int>();

	public List<int> accessoryItems = new List<int>();

	public string hair = "h0";

	public string outfit = "none";

	public List<string> owned = new List<string> { "1302000" };

	public int nameplateTitleSeries;

	public List<int> skillIds = new List<int>();

	public List<int> skillLevels = new List<int>();

	public List<int> itemIds = new List<int>();

	public List<int> itemCounts = new List<int>();

	public List<int> questStarted = new List<int>();

	public List<int> questCompleted = new List<int>();

	public List<long> questCompletedAt = new List<long>();

	public List<string> questProgress = new List<string>();

	public const int QuestNotStarted = 0;

	public const int QuestStartedState = 1;

	public const int QuestCompletedState = 2;

	public int SkillLevelOf(int id)
	{
		if (skillIds == null)
		{
			return 0;
		}
		int num = skillIds.IndexOf(id);
		if (num < 0 || num >= skillLevels.Count)
		{
			return 0;
		}
		return skillLevels[num];
	}

	public void SetSkillLevel(int id, int lv)
	{
		if (skillIds == null)
		{
			skillIds = new List<int>();
		}
		if (skillLevels == null)
		{
			skillLevels = new List<int>();
		}
		int num = skillIds.IndexOf(id);
		if (num < 0)
		{
			skillIds.Add(id);
			skillLevels.Add(Math.Max(0, lv));
		}
		else if (num < skillLevels.Count)
		{
			skillLevels[num] = Math.Max(0, lv);
		}
	}

	public void ClearSkills()
	{
		skillIds.Clear();
		skillLevels.Clear();
	}

	public int QuestStatusOf(int id)
	{
		if (questCompleted != null && questCompleted.Contains(id))
		{
			return 2;
		}
		if (questStarted != null && questStarted.Contains(id))
		{
			return 1;
		}
		return 0;
	}

	public void SetQuestStarted(int id)
	{
		if (questStarted == null)
		{
			questStarted = new List<int>();
		}
		if (!questStarted.Contains(id))
		{
			questStarted.Add(id);
		}
	}

	public void SetQuestCompleted(int id)
	{
		if (questStarted == null)
		{
			questStarted = new List<int>();
		}
		if (questCompleted == null)
		{
			questCompleted = new List<int>();
		}
		if (questCompletedAt == null)
		{
			questCompletedAt = new List<long>();
		}
		questStarted.Remove(id);
		int num = questCompleted.IndexOf(id);
		if (num < 0)
		{
			questCompleted.Add(id);
			questCompletedAt.Add(0L);
			num = questCompleted.Count - 1;
		}
		questCompletedAt[num] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
	}

	public void ResetQuest(int id)
	{
		questStarted?.Remove(id);
		int num = questCompleted?.IndexOf(id) ?? (-1);
		if (num >= 0)
		{
			questCompleted.RemoveAt(num);
			if (questCompletedAt != null && num < questCompletedAt.Count)
			{
				questCompletedAt.RemoveAt(num);
			}
		}
		if (questCompletedAt != null)
		{
			while (questCompletedAt.Count < (questCompleted?.Count ?? 0))
			{
				questCompletedAt.Add(0L);
			}
		}
		if (questProgress != null)
		{
			questProgress.RemoveAll((string p) => p.StartsWith(id + "|", StringComparison.Ordinal));
		}
	}

	public long QuestCompletedTime(int id)
	{
		if (questCompleted == null || questCompletedAt == null)
		{
			return 0L;
		}
		int num = questCompleted.IndexOf(id);
		if (num < 0 || num >= questCompletedAt.Count)
		{
			return 0L;
		}
		return questCompletedAt[num];
	}

	public string GetQuestProgress(int id, string key, string fallback = "0")
	{
		if (questProgress == null)
		{
			return fallback;
		}
		string text = id + "|" + key + "|";
		for (int num = questProgress.Count - 1; num >= 0; num--)
		{
			if (questProgress[num].StartsWith(text, StringComparison.Ordinal))
			{
				return questProgress[num].Substring(text.Length);
			}
		}
		return fallback;
	}

	public void SetQuestProgress(int id, string key, string value)
	{
		if (questProgress == null)
		{
			questProgress = new List<string>();
		}
		string text = id + "|" + key + "|";
		for (int num = questProgress.Count - 1; num >= 0; num--)
		{
			if (questProgress[num].StartsWith(text, StringComparison.Ordinal))
			{
				questProgress[num] = text + value;
				return;
			}
		}
		questProgress.Add(text + value);
	}

	public bool RemoveItem(int id, int count)
	{
		if (itemIds == null || itemCounts == null || count <= 0)
		{
			return false;
		}
		int num = itemIds.IndexOf(id);
		if (num < 0 || itemCounts[num] < count)
		{
			return false;
		}
		itemCounts[num] -= count;
		if (itemCounts[num] <= 0)
		{
			itemIds.RemoveAt(num);
			itemCounts.RemoveAt(num);
		}
		return true;
	}

	public int ItemCount(int id)
	{
		if (itemIds == null || itemCounts == null)
		{
			return 0;
		}
		int num = itemIds.IndexOf(id);
		if (num < 0 || num >= itemCounts.Count)
		{
			return 0;
		}
		return itemCounts[num];
	}

	public bool AddItem(int id, int count)
	{
		if (id <= 0 || count <= 0)
		{
			return false;
		}
		if (itemIds == null)
		{
			itemIds = new List<int>();
		}
		if (itemCounts == null)
		{
			itemCounts = new List<int>();
		}
		int num = itemIds.IndexOf(id);
		if (num < 0)
		{
			itemIds.Add(id);
			itemCounts.Add(count);
		}
		else
		{
			itemCounts[num] = (int)Math.Min(2147483647L, (long)itemCounts[num] + (long)count);
		}
		return true;
	}

	public void AddGold(long amount)
	{
		gold = Math.Max(0L, gold + amount);
	}

	public bool TrySpend(long amount)
	{
		if (amount < 0 || gold < amount)
		{
			return false;
		}
		gold -= amount;
		return true;
	}

	public bool TryBuy(string id, long catalogPrice)
	{
		if (string.IsNullOrEmpty(id) || catalogPrice < 0 || owned.Count - 1 >= equipmentSlots)
		{
			return false;
		}
		if (!TrySpend(catalogPrice))
		{
			return false;
		}
		owned.Add(id);
		return true;
	}

	public bool TryEquip(string id)
	{
		if (!owned.Contains(id))
		{
			return false;
		}
		equipWeapon = id;
		return true;
	}

	public int EquipOfSlot(int slot)
	{
		switch (slot)
		{
		case -1:
			return equipHat;
		case -5:
			return equipTop;
		case -6:
			return equipBottom;
		case -7:
			return equipShoes;
		case -8:
			return equipGlove;
		case -9:
			return equipCape;
		case -10:
			return equipShield;
		case -11:
		{
			if (!int.TryParse(equipWeapon, out var result))
			{
				return 0;
			}
			return result;
		}
		default:
		{
			int num = ((accessorySlots == null) ? (-1) : accessorySlots.IndexOf(slot));
			if (num < 0 || accessoryItems == null || num >= accessoryItems.Count)
			{
				return 0;
			}
			return accessoryItems[num];
		}
		}
	}

	public void SetEquipSlot(int slot, int itemId)
	{
		switch (slot)
		{
		case -1:
			equipHat = itemId;
			return;
		case -5:
			equipTop = itemId;
			return;
		case -6:
			equipBottom = itemId;
			return;
		case -7:
			equipShoes = itemId;
			return;
		case -8:
			equipGlove = itemId;
			return;
		case -9:
			equipCape = itemId;
			return;
		case -10:
			equipShield = itemId;
			return;
		case -11:
			equipWeapon = itemId.ToString();
			return;
		}
		if (accessorySlots == null)
		{
			accessorySlots = new List<int>();
		}
		if (accessoryItems == null)
		{
			accessoryItems = new List<int>();
		}
		int num = accessorySlots.IndexOf(slot);
		if (num < 0)
		{
			accessorySlots.Add(slot);
			accessoryItems.Add(itemId);
		}
		else
		{
			accessoryItems[num] = itemId;
		}
	}

	public void SetJob(int id)
	{
		jobId = id;
		cls = JobDefs.ClsOf(id);
	}

	public void AddSp(int delta)
	{
		sp = Math.Max(0, sp + delta);
	}

	public void AddAp(int delta)
	{
		ap = Math.Max(0, ap + delta);
	}

	public void AddEquipSlots(int delta)
	{
		equipmentSlots = Math.Max(4, equipmentSlots + delta);
	}

	public void SetBaseStats(int str, int dex, int intl, int luk, int remainingAp, int remainingSp)
	{
		attributes.str = str;
		attributes.dex = dex;
		attributes.intelligence = intl;
		attributes.luk = luk;
		ap = remainingAp;
		sp = remainingSp;
	}

	public void AddBaseHpMp(int addHp, int addMp)
	{
		if (addHp > 0)
		{
			baseMaxHp = Math.Max(1, baseMaxHp + addHp);
		}
		if (addMp > 0)
		{
			baseMaxMp = Math.Max(1, baseMaxMp + addMp);
		}
		attributes.maxHp = baseMaxHp;
		attributes.maxMp = baseMaxMp;
	}
}
