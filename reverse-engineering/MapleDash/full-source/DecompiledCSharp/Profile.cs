using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Profile
{
	private sealed class SaveBatch : IDisposable
	{
		private bool disposed;

		public void Dispose()
		{
			if (!disposed)
			{
				disposed = true;
				if (--saveBatchDepth == 0 && saveBatchPending)
				{
					saveBatchPending = false;
					Save();
				}
			}
		}
	}

	private static PlayerProfileData data;

	public const string DefaultName = "枫叶旅人";

	private static bool debugOverride;

	private static int saveBatchDepth;

	private static bool saveBatchPending;

	private static readonly List<int> EmptyQuests = new List<int>();

	public static PlayerProfileData Data
	{
		get
		{
			Load();
			return data;
		}
	}

	public static long gold => Data.gold;

	public static CharacterAttributes Attributes => Data.attributes.Copy();

	public static long experience => Data.experience;

	public static int level => Data.level;

	public static int ap => Data.ap;

	public static string playerName => Data.playerName;

	public static long ExpNeeded => ExpTable.Needed(Data.level);

	public static float ExpRatio
	{
		get
		{
			long expNeeded = ExpNeeded;
			if (expNeeded > 0)
			{
				return Mathf.Clamp01((float)((double)Data.experience / (double)expNeeded));
			}
			return 0f;
		}
	}

	public static string cls => Data.cls;

	public static int JobId => Data.jobId;

	public static int Sp => Data.sp;

	public static string equipWeapon => Data.equipWeapon;

	public static string hair => Data.hair;

	public static string outfit => Data.outfit;

	public static IReadOnlyCollection<string> OwnedWeapons => Data.owned.AsReadOnly();

	public static bool LastSaveSucceeded { get; private set; } = true;

	public static IReadOnlyList<int> StartedQuests => Data.questStarted ?? EmptyQuests;

	public static IReadOnlyList<int> CompletedQuests => Data.questCompleted ?? EmptyQuests;

	public static int LastMapId
	{
		get
		{
			return PlayerPrefs.GetInt("mxd.fieldmap", 1000000);
		}
		set
		{
			PlayerPrefs.SetInt("mxd.fieldmap", value);
			PlayerPrefs.Save();
		}
	}

	public static bool AutomaticAp
	{
		get
		{
			if (JobId == 0)
			{
				return level <= 10;
			}
			return false;
		}
	}

	public static event Action<int> LeveledUp;

	public static void Load()
	{
		if (data != null)
		{
			return;
		}
		data = LocalProfileStore.Load();
		data.gold = Math.Max(0L, data.gold);
		data.experience = Math.Max(0L, data.experience);
		if (data.equipmentSlots <= 0)
		{
			data.equipmentSlots = 24;
		}
		if (data.attributes == null)
		{
			data.attributes = new CharacterAttributes();
		}
		if (data.migratedLegacyWeapons == null)
		{
			data.migratedLegacyWeapons = new List<string>();
		}
		for (int i = 0; i < data.owned.Count; i++)
		{
			string text = data.owned[i];
			data.owned[i] = WeaponDefs.Canonical(text);
			if (text != data.owned[i])
			{
				data.migratedLegacyWeapons.Add(text);
			}
		}
		data.equipWeapon = WeaponDefs.Canonical(data.equipWeapon);
		data.owned.RemoveAll((string id) => !WeaponDefs.TryGet(id, out var _));
		if (!data.owned.Contains("1302000"))
		{
			data.owned.Add("1302000");
		}
		if (!data.owned.Contains(data.equipWeapon))
		{
			data.equipWeapon = "1302000";
		}
		if (!HairStyles.Contains(data.hair))
		{
			data.hair = "h0";
		}
		if (ClassDefs.Get(data.cls).id != data.cls)
		{
			data.cls = "beginner";
		}
		MigrateJob(data);
		if (data.outfit == null || !CosmeticFactory.OutfitColors.ContainsKey(data.outfit))
		{
			data.outfit = "none";
		}
		if (!WeaponDefs.CanEquip(WeaponDefs.Get(data.equipWeapon), data.attributes, data.jobId))
		{
			data.equipWeapon = "1302000";
		}
		bool flag = string.IsNullOrEmpty(data.playerName);
		if (flag)
		{
			data.playerName = PlayerPrefs.GetString("mxd.name", "枫叶旅人");
		}
		LevelSystem.EnsureBaseline(data);
		bool flag2 = LevelSystem.MigrateNoviceAp(data);
		string text2 = PlayerPrefs.GetString("debug_cls", "");
		if (!string.IsNullOrEmpty(text2) && ClassDefs.Get(text2).id == text2)
		{
			PlayerPrefs.DeleteKey("debug_cls");
			PlayerPrefs.Save();
			data.cls = text2;
			data.jobId = JobDefs.JobIdOf(text2);
			debugOverride = true;
			Debug.LogWarning((object)("[Profile] 本次运行职业被调试键 debug_cls 覆盖 → " + text2 + "（job " + data.jobId + "），本次不落盘；键已删除，下次启动恢复存档职业"));
		}
		int num = LevelSystem.Reconcile(data);
		if (num > 0)
		{
			Debug.Log((object)("[Profile] 旧经验补算等级 → Lv." + data.level + "（补了 " + num + " 级）"));
		}
		int num2 = PlayerPrefs.GetInt("debug_level", 0);
		if (num2 > 0)
		{
			PlayerPrefs.DeleteKey("debug_level");
			LevelSystem.SetLevel(data, num2);
			data.experience = 0L;
			debugOverride = true;
			Debug.Log((object)("[Profile] 调试等级 → Lv." + data.level + "（HP " + data.baseMaxHp + " / MP " + data.baseMaxMp + "）"));
		}
		if ((num > 0) | flag | flag2)
		{
			Save();
		}
	}

	private static void MigrateJob(PlayerProfileData d)
	{
		if (d.jobId <= 0 || !JobDefs.IsValid(d.jobId))
		{
			d.jobId = JobDefs.JobIdOf(d.cls);
		}
		if (!JobDefs.IsValid(d.jobId))
		{
			d.jobId = 0;
		}
		d.cls = JobDefs.ClsOf(d.jobId);
		if (d.sp <= 0 && d.jobId > 0)
		{
			d.sp = 3 * Mathf.Max(0, d.level - 1);
		}
	}

	public static void SetName(string value)
	{
		value = (value ?? "").Trim();
		if (value.Length != 0 && value.Length <= 12)
		{
			Data.playerName = value;
			PlayerPrefs.SetString("mxd.name", value);
			Save();
		}
	}

	public static IDisposable DeferSaves()
	{
		saveBatchDepth++;
		return new SaveBatch();
	}

	public static void Save()
	{
		if (debugOverride)
		{
			LastSaveSucceeded = true;
		}
		else if (saveBatchDepth > 0)
		{
			saveBatchPending = true;
		}
		else
		{
			LastSaveSucceeded = LocalProfileStore.Save(Data);
		}
	}

	public static void AddGold(long amount)
	{
		Data.AddGold(amount);
		Save();
	}

	public static int ItemCount(int id)
	{
		return Data.ItemCount(id);
	}

	public static void AddItem(int id, int count)
	{
		TryAddItem(id, count);
	}

	public static bool TryAddItem(int id, int count)
	{
		if (GameSession.InGame && !SyncInventory(id, count))
		{
			return false;
		}
		if (!Data.AddItem(id, count))
		{
			return false;
		}
		Save();
		return true;
	}

	public static bool RemoveItem(int id, int count, int sourcePosition = 0)
	{
		if (Data.ItemCount(id) < count)
		{
			return false;
		}
		if (GameSession.InGame && !SyncInventory(id, -count, sourcePosition))
		{
			return false;
		}
		if (!Data.RemoveItem(id, count))
		{
			return false;
		}
		Save();
		return true;
	}

	private static bool SyncInventory(int id, int count, int sourcePosition = 0)
	{
		InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
		{
			characterId = GameSession.CharacterId,
			itemId = id,
			quantity = count,
			sourcePosition = sourcePosition
		});
		if (!inventoryChangeResponse.ok)
		{
			BottomBar.Toast(inventoryChangeResponse.error);
			return false;
		}
		GameSession.Character.inventory = inventoryChangeResponse.inventory;
		return true;
	}

	public static bool ConsumeProjectile(int id, int count, int position)
	{
		if (count == 0)
		{
			return true;
		}
		if (count > 0 && Data.ItemCount(id) < count)
		{
			return false;
		}
		if (GameSession.InGame)
		{
			InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
			{
				characterId = GameSession.CharacterId,
				itemId = id,
				quantity = -count,
				sourcePosition = position,
				projectile = true
			});
			if (!inventoryChangeResponse.ok)
			{
				return false;
			}
			GameSession.Character.inventory = inventoryChangeResponse.inventory;
		}
		if (count > 0)
		{
			Data.RemoveItem(id, count);
		}
		else
		{
			Data.AddItem(id, -count);
		}
		Save();
		return true;
	}

	public static int QuestStatus(int id)
	{
		if (id != 34151 || !GameSession.InGame)
		{
			return Data.QuestStatusOf(id);
		}
		return WeeklyGemstoneQuest.Status();
	}

	public static bool IsQuestStarted(int id)
	{
		return QuestStatus(id) == 1;
	}

	public static bool IsQuestCompleted(int id)
	{
		return QuestStatus(id) == 2;
	}

	public static long QuestCompletedTime(int id)
	{
		return Data.QuestCompletedTime(id);
	}

	public static void StartQuest(int id)
	{
		Data.SetQuestStarted(id);
		Save();
	}

	public static void CompleteQuest(int id)
	{
		Data.SetQuestCompleted(id);
		Save();
	}

	public static void ResetQuest(int id)
	{
		Data.ResetQuest(id);
		Save();
	}

	public static string QuestProgress(int id, string key, string fallback = "0")
	{
		return Data.GetQuestProgress(id, key, fallback);
	}

	public static int QuestProgressInt(int id, string key, int fallback = 0)
	{
		if (!int.TryParse(Data.GetQuestProgress(id, key, fallback.ToString()), out var result))
		{
			return fallback;
		}
		return result;
	}

	public static void SetQuestProgress(int id, string key, string value, bool save = true)
	{
		Data.SetQuestProgress(id, key, value);
		if (save)
		{
			Save();
		}
	}

	public static int GrantExperience(long amount)
	{
		if (GameServer.Local != null && !GameConfigDao.GetBool(GameServer.Local.Db, "exp_enabled", fallback: true))
		{
			return 0;
		}
		amount += NativeEquipmentEffects.PendantBonus(amount);
		amount += NativeEquipmentEffects.RingExpBonus(amount);
		int expPercent = NativeSpecialCore.ExpPercent;
		if (expPercent > 0)
		{
			amount += amount * expPercent / 100;
		}
		GameLoop i = GameLoop.I;
		if (i != null)
		{
			PlayerCtrl player = i.player;
			if (((player == null) ? ((bool?)null) : ((Component)player).GetComponent<NativeMobDebuffs>()?.Has(124)) == true)
			{
				amount /= 2;
			}
		}
		if (amount <= 0)
		{
			return 0;
		}
		int num = Data.level;
		int num2 = LevelSystem.AddExperience(Data, amount, protect: true);
		NativeGainNotice.Experience(amount);
		Save();
		if (num2 > 0 && Data.level > num)
		{
			Action<int> leveledUp = LeveledUp;
			if (leveledUp == null)
			{
				return num2;
			}
			leveledUp(Data.level);
		}
		return num2;
	}

	public static bool Spend(long amount)
	{
		if (!Data.TrySpend(amount))
		{
			return false;
		}
		Save();
		return true;
	}

	public static bool OwnsWeapon(string id)
	{
		return Data.owned.Contains(WeaponDefs.Canonical(id));
	}

	public static bool TryBuyWeapon(string id)
	{
		if (!WeaponDefs.TryGet(id, out var weapon) || weapon.price < 0 || !Data.TryBuy(weapon.id, weapon.price))
		{
			return false;
		}
		Save();
		return true;
	}

	public static void EquipWeapon(string id)
	{
		if (WeaponDefs.TryGet(id, out var weapon) && WeaponDefs.CanEquip(weapon, Attributes, cls) && Data.TryEquip(weapon.id))
		{
			Save();
		}
	}

	public static bool TryBuyWeapon(int shop, int slot, string expectedId)
	{
		NativeWeaponShop nativeWeaponShop = WeaponDefs.Shops.FirstOrDefault((NativeWeaponShop s) => s.id == shop);
		if (nativeWeaponShop == null || slot < 0 || slot >= nativeWeaponShop.items.Count)
		{
			return false;
		}
		WeaponDef weaponDef = nativeWeaponShop.items[slot];
		if (weaponDef.id != expectedId || weaponDef.price < 0 || !Data.TryBuy(weaponDef.id, weaponDef.price))
		{
			return false;
		}
		Save();
		return true;
	}

	public static void SetClass(string id)
	{
		if (!(ClassDefs.Get(id).id != id))
		{
			Data.cls = id;
			Data.jobId = JobDefs.JobIdOf(id);
			if (!WeaponDefs.CanEquip(WeaponDefs.Get(Data.equipWeapon), Data.attributes, id))
			{
				Data.equipWeapon = "1302000";
			}
			Save();
		}
	}

	public static void SetJob(int id)
	{
		if (JobDefs.IsValid(id))
		{
			Data.SetJob(id);
			if (!WeaponDefs.CanEquip(WeaponDefs.Get(Data.equipWeapon), Data.attributes, Data.jobId))
			{
				Data.equipWeapon = "1302000";
			}
			Save();
		}
	}

	public static void AddSp(int delta)
	{
		Data.AddSp(delta);
		Save();
	}

	public static void AddAp(int delta)
	{
		Data.AddAp(delta);
		Save();
	}

	public static void AddBaseHpMp(int addHp, int addMp)
	{
		Data.AddBaseHpMp(addHp, addMp);
		Save();
	}

	public static void AddEquipSlots(int delta)
	{
		Data.AddEquipSlots(delta);
		Save();
	}

	public static void SetBaseStats(int str, int dex, int intl, int luk, int ap, int sp)
	{
		Data.SetBaseStats(str, dex, intl, luk, ap, sp);
		Save();
	}

	public static int EquipOfSlot(int slot)
	{
		if (GameSession.InGame && (NativePetItems.IsPetSlot(slot) || SymbolCatalog.IsSymbolSlot(slot)))
		{
			return GameSession.Character.inventory.Find((InventoryEntry x) => x.inventoryType == 1 && x.position == slot)?.itemId ?? 0;
		}
		return Data.EquipOfSlot(slot);
	}

	public static void SetEquipSlot(int slot, int itemId)
	{
		Data.SetEquipSlot(slot, itemId);
		Save();
	}

	public static bool CanAssignAp(int stat)
	{
		if (AutomaticAp || ap <= 0 || stat < 0 || stat > 3)
		{
			return false;
		}
		CharacterAttributes attributes = Data.attributes;
		return stat switch
		{
			2 => attributes.intelligence, 
			1 => attributes.dex, 
			0 => attributes.str, 
			_ => attributes.luk, 
		} < 32767;
	}

	public static bool AssignAp(int stat)
	{
		if (!CanAssignAp(stat))
		{
			return false;
		}
		switch (stat)
		{
		case 0:
			Data.attributes.str++;
			break;
		case 1:
			Data.attributes.dex++;
			break;
		case 2:
			Data.attributes.intelligence++;
			break;
		default:
			Data.attributes.luk++;
			break;
		}
		Data.ap--;
		Save();
		return true;
	}

	public static int[] AutoApPlan(bool brawler = false)
	{
		if (AutomaticAp || ap <= 0)
		{
			return null;
		}
		return BuildAutoApPlan(JobId, level, ap, Data.attributes, brawler);
	}

	public static int[] BuildAutoApPlan(int jobId, int lv, int points, CharacterAttributes attributes, bool brawler = false)
	{
		if (points <= 0 || attributes == null)
		{
			return null;
		}
		int num = jobId % 1000;
		int num2;
		int num3;
		int num4;
		int num5;
		switch (num / 100)
		{
		case 0:
			if (jobId != 0 && jobId != 2001)
			{
				return null;
			}
			num2 = 1;
			num3 = 0;
			num4 = lv;
			num5 = lv * 4 + 12;
			break;
		case 1:
			num2 = 1;
			num3 = 0;
			num4 = ((lv <= 30) ? (lv * 2) : (lv + 30));
			num5 = lv * 5 + 12 - num4;
			break;
		case 2:
			num2 = 3;
			num3 = 2;
			num4 = lv + 3;
			num5 = lv * 5 + 12 - num4;
			break;
		case 3:
			switch (num)
			{
			case 300:
			case 310:
			case 311:
			case 312:
				num4 = lv + 5;
				break;
			default:
				if (num >= 320 && num <= 322)
				{
					num4 = lv;
					break;
				}
				return null;
			}
			num2 = 0;
			num3 = 1;
			num5 = lv * 5 + 12 - num4;
			break;
		case 4:
			num2 = 1;
			num3 = 3;
			num4 = ((lv <= 40) ? (lv * 2) : (lv + 40));
			num5 = lv * 5 + 12 - num4;
			break;
		case 5:
			if (num / 10 == 51 || jobId == 1500 || ((num == 500) & brawler))
			{
				num2 = 1;
				num3 = 0;
				num4 = ((lv <= 20) ? (lv * 2) : (lv + 20));
				num5 = lv * 5 + 12 - num4;
				break;
			}
			if (num / 10 == 52 || num == 500)
			{
				num2 = 0;
				num3 = 1;
				num4 = lv;
				num5 = lv * 4 + 12;
				break;
			}
			return null;
		default:
			return null;
		}
		int[] array = new int[4] { attributes.str, attributes.dex, attributes.intelligence, attributes.luk };
		int[] array2 = new int[4];
		int num6 = Math.Min(points, Math.Max(0, num4 - array[num2]));
		int num7 = Math.Min(points - num6, Math.Max(0, num5 - array[num3]));
		num7 += points - num6 - num7;
		array2[num2] = Math.Min(num6, Math.Max(0, 32767 - array[num2]));
		array2[num3] = Math.Min(num7, Math.Max(0, 32767 - array[num3]));
		return array2;
	}

	public static bool AssignAutoAp(int[] plan)
	{
		if (plan == null || plan.Length != 4 || AutomaticAp)
		{
			return false;
		}
		CharacterAttributes attributes = Data.attributes;
		int[] array = new int[4] { attributes.str, attributes.dex, attributes.intelligence, attributes.luk };
		long num = 0L;
		for (int i = 0; i < 4; i++)
		{
			if (plan[i] < 0 || (long)array[i] + (long)plan[i] > 32767)
			{
				return false;
			}
			num += plan[i];
		}
		if (num <= 0 || num > ap)
		{
			return false;
		}
		attributes.str += plan[0];
		attributes.dex += plan[1];
		attributes.intelligence += plan[2];
		attributes.luk += plan[3];
		Data.ap -= (int)num;
		Save();
		GameLoop.I?.player?.RefreshBuffAttributes();
		return true;
	}

	public static string ResetAp(int from, int to)
	{
		CharacterAttributes attributes = Data.attributes;
		switch (from)
		{
		case 64:
			if (attributes.str < 5)
			{
				return "你的力量(STR)不足，无法进行重置。";
			}
			break;
		case 128:
			if (attributes.dex < 5)
			{
				return "你的敏捷(DEX)不足，无法进行重置。";
			}
			break;
		case 256:
			if (attributes.intelligence < 5)
			{
				return "你的智力(INT)不足，无法进行重置。";
			}
			break;
		case 512:
			if (attributes.luk < 5)
			{
				return "你的运气(LUK)不足，无法进行重置。";
			}
			break;
		case 2048:
		case 8192:
			return "你没有足够的HP、MP属性点用于AP重置。";
		default:
			return "AP重置操作执行失败。";
		}
		switch (to)
		{
		case 2048:
		case 8192:
			return "你没有足够的HP、MP属性点用于AP重置。";
		default:
			return "AP重置操作执行失败。";
		case 64:
		case 128:
		case 256:
		case 512:
			switch (from)
			{
			case 64:
				attributes.str--;
				break;
			case 128:
				attributes.dex--;
				break;
			case 256:
				attributes.intelligence--;
				break;
			default:
				attributes.luk--;
				break;
			}
			switch (to)
			{
			case 64:
				attributes.str++;
				break;
			case 128:
				attributes.dex++;
				break;
			case 256:
				attributes.intelligence++;
				break;
			default:
				attributes.luk++;
				break;
			}
			Save();
			GameLoop.I?.player?.RefreshBuffAttributes();
			return null;
		}
	}

	public static string ResetSp(int spFromId, int spToId)
	{
		SkillData skillData = SkillBook.Get(spFromId);
		SkillData skillData2 = SkillBook.Get(spToId);
		if (skillData == null || skillData2 == null)
		{
			return "技能数据缺失";
		}
		if (spFromId == spToId)
		{
			return "AP/SP重置操作执行失败。";
		}
		int num = SkillProgress.BaseLevelOf(spFromId);
		int num2 = SkillProgress.BaseLevelOf(spToId);
		int num3 = SkillBook.MasterCapOf(spToId);
		if (num <= 0)
		{
			return "你没有可以恢复的技能点。";
		}
		if (num2 >= num3)
		{
			return "目标技能等级已达到上限。";
		}
		SkillProgress.SetLevel(spFromId, num - 1);
		SkillProgress.SetLevel(spToId, num2 + 1);
		if (num - 1 == 0)
		{
			NativeSkillMacros.ClearSkillRefs(spFromId);
		}
		return null;
	}

	public static int InventoryCapacity(int tab)
	{
		if (tab == 0)
		{
			return Data.equipmentSlots;
		}
		CharRecord charRecord = (GameSession.InGame ? GameSession.Character.record : null);
		if (tab != 4)
		{
			if (charRecord != null)
			{
				return tab switch
				{
					2 => charRecord.setupslots, 
					1 => charRecord.useslots, 
					_ => charRecord.etcslots, 
				};
			}
			return 24;
		}
		return 96;
	}

	public static List<int> InventoryItems(int tab)
	{
		List<int> list = new List<int>();
		if (Data.itemIds != null)
		{
			foreach (int itemId in Data.itemIds)
			{
				if (itemId / 1000000 - 1 == tab && Data.ItemCount(itemId) > 0)
				{
					list.Add(itemId);
				}
			}
		}
		return list;
	}

	public static void SetHair(string id)
	{
		if (HairStyles.Contains(id))
		{
			Data.hair = id;
			Save();
		}
	}

	public static void SetOutfit(string id)
	{
		if (CosmeticFactory.OutfitColors.ContainsKey(id))
		{
			Data.outfit = id;
			Save();
		}
	}
}
