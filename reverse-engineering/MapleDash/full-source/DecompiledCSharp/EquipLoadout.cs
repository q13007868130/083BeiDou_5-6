using System;
using System.Collections.Generic;
using UnityEngine;

public static class EquipLoadout
{
	private static int SlotItem(int slot)
	{
		return Profile.EquipOfSlot(slot);
	}

	public static int Get(int slot)
	{
		return SlotItem(slot);
	}

	public static string Equip(int itemId, CharacterAttributes attrs, int inventoryPosition = 0, int preferredSlot = 0)
	{
		if (NativePetItems.IsEquipment(itemId))
		{
			if (!GameSession.InGame)
			{
				return "请先进入游戏";
			}
			int selectedPet = EquipWindow.SelectedPet;
			InventoryEntry pet = GameSession.Net.ChangeInventory(new InventoryChangeRequest
			{
				characterId = GameSession.CharacterId,
				getPet = true,
				petSlot = selectedPet
			}).pet;
			if (pet == null || !NativePetItems.EquipmentFits(itemId, pet.itemId))
			{
				return "请先召唤并选择可以穿戴此装备的宠物";
			}
			int num = NativePetItems.EquipmentSlot(itemId, selectedPet);
			if (num == 0)
			{
				return "此宠物装备的槽位尚未核实";
			}
			return MoveEquipment(itemId, num, unequip: false, inventoryPosition, selectedPet);
		}
		int num2 = EquipSlot.SlotOf(itemId);
		if (num2 == 0)
		{
			return "这不是可装备的物品";
		}
		if (NativeAndroidService.IsApSlot(preferredSlot))
		{
			if (!NativeAndroidService.ApAccepts(preferredSlot, num2))
			{
				return "这件装备不能放进机器人的这个换装槽";
			}
			return MoveEquipment(itemId, preferredSlot, unequip: false, inventoryPosition);
		}
		string text = NativeMount.WearError(itemId, num2);
		if (text != null)
		{
			return text;
		}
		CharacterAttributes a = CheckAttributes(attrs);
		if (num2 == -12 || num2 == -112)
		{
			bool flag = num2 == -112;
			int[] array = ((!flag) ? new int[4] { -12, -13, -15, -16 } : new int[4] { -112, -113, -115, -116 });
			int num3 = ((preferredSlot != 0) ? (flag ? (preferredSlot - 100) : ((preferredSlot <= -100) ? (preferredSlot + 100) : preferredSlot)) : 0);
			if (num3 != 0 && Array.IndexOf(array, num3) >= 0)
			{
				num2 = num3;
			}
			else
			{
				int[] array2 = array;
				foreach (int num4 in array2)
				{
					if (Profile.EquipOfSlot(num4) == 0)
					{
						num2 = num4;
						break;
					}
				}
			}
		}
		switch (num2)
		{
		case -11:
		{
			if (!WeaponDefs.TryGet(itemId.ToString(), out var _))
			{
				return "这件武器的穿戴尚未接入";
			}
			string text3 = EquipCatalog.WhyNot(itemId, a, Profile.JobId);
			if (text3 != null)
			{
				return text3;
			}
			return MoveEquipment(itemId, num2, unequip: false, inventoryPosition);
		}
		case -49:
			NativeMedalWindow.Wear(itemId, unequip: false);
			return null;
		default:
		{
			string text2 = EquipCatalog.WhyNot(itemId, a, Profile.JobId);
			if (text2 != null)
			{
				return text2;
			}
			if (!(GameSession.InGame ? GameSession.Character.inventory.Exists((InventoryEntry x) => x.inventoryType == 1 && InventoryDao.IsBagPosition(x.position) && x.itemId == itemId && x.quantity > 0 && (inventoryPosition == 0 || x.position == inventoryPosition)) : (Profile.ItemCount(itemId) > 0)))
			{
				return "背包里没有这件装备";
			}
			return MoveEquipment(itemId, num2, unequip: false, inventoryPosition);
		}
		}
	}

	private static string MoveEquipment(int itemId, int slot, bool unequip, int inventoryPosition = 0, int petSlot = 0, int destinationPosition = 0)
	{
		if (!GameSession.InGame)
		{
			return "请先进入游戏";
		}
		InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
		{
			characterId = GameSession.CharacterId,
			itemId = itemId,
			equipSlot = slot,
			sourcePosition = inventoryPosition,
			petSlot = petSlot,
			equip = true,
			quantity = ((!unequip) ? 1 : 0),
			moveTo = destinationPosition
		});
		if (NativeAndroidService.IsApSlot(slot))
		{
			Debug.Log((object)("[Android/Drag] request item=" + itemId + " source=" + inventoryPosition + " target=" + slot + " unequip=" + unequip + " ok=" + inventoryChangeResponse.ok + " error=" + inventoryChangeResponse.error));
		}
		if (!inventoryChangeResponse.ok)
		{
			return inventoryChangeResponse.error;
		}
		if (slot == -18 || slot == -19 || slot == -118 || slot == -119)
		{
			PlayerCtrl playerCtrl = GameLoop.I?.player;
			if (playerCtrl != null)
			{
				((Component)playerCtrl).GetComponent<NativeBuffs>()?.CancelStat("MONSTER_RIDING");
			}
		}
		GameSession.Character.inventory = inventoryChangeResponse.inventory;
		GameSession.ApplyInventoryToProfile();
		if (!unequip && (slot == -32 || slot == -54))
		{
			string reason = NativeAndroidService.Resolve(GameSession.CharacterId, Profile.EquipOfSlot).reason;
			if (!string.IsNullOrEmpty(reason))
			{
				BottomBar.Toast(reason);
			}
		}
		if (!unequip && itemId == 1189999 && SkillProgress.LevelOf(1016) == 0)
		{
			SkillProgress.SetLearned(1016, 1);
		}
		Profile.Save();
		return null;
	}

	public static void Unequip(int slot)
	{
		if (slot != -11)
		{
			string text = UnequipToInventory(slot);
			if (text != null)
			{
				NativeUtilPrompt.Notice(text);
			}
		}
	}

	public static string UnequipToInventory(int slot, int destinationPosition = 0)
	{
		if (slot == -11)
		{
			return "普通武器请通过换装替换";
		}
		int num = Profile.EquipOfSlot(slot);
		if (num <= 0)
		{
			return "该格没有装备";
		}
		return MoveEquipment(num, slot, unequip: true, 0, 0, destinationPosition);
	}

	public static Dictionary<string, int> CurrentSetBonus()
	{
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, int> item in WornSlots())
		{
			list.Add(item.Value);
		}
		NativeSetItemSkills.Refresh(list, AdminSetItem.JobIndexOf(Profile.JobId));
		Dictionary<string, int> dictionary = AdminSetItem.ComputeBonus(list, AdminSetItem.JobIndexOf(Profile.JobId));
		Dictionary<string, int> dictionary2 = ((dictionary == null) ? new Dictionary<string, int>() : new Dictionary<string, int>(dictionary));
		AddPotentialBonus(dictionary2, new CharacterAttributes
		{
			level = (GameSession.Character?.record.level ?? 1)
		});
		NativeSkillEffects.AddPassive(dictionary2);
		GrowthStatDelta growthStatDelta = NativeGrowthStats.Accumulate(GameSession.Character, Profile.level, Profile.JobId);
		string[] array = new string[3] { "ExpRate", "MesoRate", "DropRate" };
		foreach (string key in array)
		{
			dictionary2[key] = (dictionary2.TryGetValue(key, out var value) ? value : 0) + growthStatDelta.Get(key);
		}
		return dictionary2;
	}

	private static void AddPotentialBonus(Dictionary<string, int> set, CharacterAttributes result, int swapSlot = 0, InventoryEntry swapEntry = null)
	{
		List<InventoryEntry> list = GameSession.Character?.inventory;
		if (list != null)
		{
			foreach (InventoryEntry item in list)
			{
				if (item.position < 0 && Array.IndexOf(EquipSlot.Worn, item.position) >= 0 && item.equipmentData != null && (swapSlot == 0 || item.position != swapSlot))
				{
					ApplyPotential(item, set, result);
				}
			}
		}
		if (swapEntry != null && swapEntry.equipmentData != null)
		{
			ApplyPotential(swapEntry, set, result);
		}
	}

	private static void ApplyPotential(InventoryEntry item, Dictionary<string, int> set, CharacterAttributes result)
	{
		Dictionary<string, object> equipmentData = item.equipmentData;
		if (equipmentData.TryGetValue("starForceVestige", out var value) && Convert.ToInt32(value) != 0)
		{
			return;
		}
		List<int> list = new List<int>();
		string[] array = new string[2] { "potentialOption", "bonusOption" };
		foreach (string text in array)
		{
			for (int j = 1; j <= 3; j++)
			{
				if (equipmentData.TryGetValue(text + j, out var value2))
				{
					list.Add(Convert.ToInt32(value2));
				}
			}
		}
		if (!list.Exists((int id) => id > 0))
		{
			return;
		}
		int num = EquipCatalog.Info(item.itemId)?.Int("reqLevel") ?? 0;
		if (equipmentData.TryGetValue("iIncReq", out var value3))
		{
			num += Convert.ToInt32(value3);
		}
		foreach (KeyValuePair<string, int> item2 in PotentialStats.Calculate(PotentialCatalog.Shared, num, list, result.level))
		{
			if (item2.Key == "CriticalChance")
			{
				result.criticalChance += (float)item2.Value / 100f;
			}
			else if (item2.Key == "CriticalDamage")
			{
				result.criticalBonus += (float)item2.Value / 100f;
			}
			else if (item2.Key == "Speed")
			{
				result.equipmentSpeed += item2.Value;
			}
			else if (item2.Key == "Jump")
			{
				result.equipmentJump += item2.Value;
			}
			else
			{
				PotentialStats.Add(set, item2.Key, item2.Value);
			}
		}
	}

	private static IEnumerable<KeyValuePair<int, int>> WornSlots(int swapSlot = 0, int swapId = 0)
	{
		int[] worn = EquipSlot.Worn;
		foreach (int num in worn)
		{
			int num2 = ((swapSlot == num && swapId > 0) ? swapId : SlotItem(num));
			if (num2 > 0)
			{
				yield return new KeyValuePair<int, int>(num, num2);
			}
		}
	}

	public static CharacterAttributes Apply(CharacterAttributes baseAttrs, WeaponDef weapon, int attackBuff, out int watk, out int matk)
	{
		return Apply(baseAttrs, weapon, attackBuff, out watk, out matk, 0, 0, null);
	}

	public static CharacterAttributes Apply(CharacterAttributes baseAttrs, WeaponDef weapon, int attackBuff, out int watk, out int matk, int swapSlot, int swapId, InventoryEntry swapEntry)
	{
		int watk2 = attackBuff;
		int matk2 = 0;
		int bossDamage = 0;
		CharacterAttributes characterAttributes = baseAttrs.Copy();
		InventoryEntry overrideInstance = ((swapSlot == -11) ? swapEntry : null);
		if (int.TryParse(weapon.id, out var result))
		{
			EquipCatalog.Apply(result, characterAttributes, ref watk2, ref matk2, -11, ref bossDamage, overrideInstance);
		}
		foreach (KeyValuePair<int, int> item in WornSlots(swapSlot, swapId))
		{
			if (item.Key != -11)
			{
				EquipCatalog.Apply(item.Value, characterAttributes, ref watk2, ref matk2, item.Key, ref bossDamage, (item.Key == swapSlot) ? swapEntry : null);
			}
		}
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, int> item2 in WornSlots(swapSlot, swapId))
		{
			list.Add(item2.Value);
		}
		Dictionary<string, int> dictionary = AdminSetItem.ComputeBonus(list, AdminSetItem.JobIndexOf(Profile.JobId));
		dictionary = ((dictionary == null) ? new Dictionary<string, int>() : new Dictionary<string, int>(dictionary));
		AddPotentialBonus(dictionary, characterAttributes, swapSlot, swapEntry);
		NativeSkillEffects.AddPassive(dictionary);
		GrowthStatDelta growthStatDelta = NativeGrowthStats.Accumulate(GameSession.Character, characterAttributes.level, Profile.JobId, swapSlot, swapEntry);
		foreach (KeyValuePair<string, int> item3 in growthStatDelta.set)
		{
			dictionary[item3.Key] = (dictionary.TryGetValue(item3.Key, out var value) ? value : 0) + item3.Value;
		}
		foreach (KeyValuePair<string, int> item4 in NativeTitleTrials.BonusSet())
		{
			dictionary[item4.Key] = (dictionary.TryGetValue(item4.Key, out var value2) ? value2 : 0) + item4.Value;
		}
		characterAttributes.criticalChance += growthStatDelta.criticalChance;
		characterAttributes.criticalBonus += growthStatDelta.criticalBonus;
		characterAttributes.hexaDamagePercent = growthStatDelta.hexaDamagePercent;
		characterAttributes.hexaBossPercent = growthStatDelta.hexaBossPercent;
		characterAttributes.hexaIgnorePercent = growthStatDelta.hexaIgnorePercent;
		characterAttributes.arcaneForce = growthStatDelta.arcaneForce;
		characterAttributes.authenticForce = growthStatDelta.authenticForce;
		if (bossDamage != 0)
		{
			dictionary = ((dictionary == null) ? new Dictionary<string, int>() : new Dictionary<string, int>(dictionary));
			dictionary["BossDamage"] = (dictionary.TryGetValue("BossDamage", out var value3) ? value3 : 0) + bossDamage;
		}
		characterAttributes.maxHp = AdminSetItem.ApplyStat(dictionary, "HP", characterAttributes.maxHp);
		characterAttributes.maxMp = AdminSetItem.ApplyStat(dictionary, "MP", characterAttributes.maxMp);
		characterAttributes.str = AdminSetItem.ApplyStat(dictionary, "STR", characterAttributes.str);
		characterAttributes.dex = AdminSetItem.ApplyStat(dictionary, "DEX", characterAttributes.dex);
		characterAttributes.luk = AdminSetItem.ApplyStat(dictionary, "LUK", characterAttributes.luk);
		characterAttributes.intelligence = AdminSetItem.ApplyStat(dictionary, "INT", characterAttributes.intelligence);
		characterAttributes.weaponDefense = AdminSetItem.ApplyStat(dictionary, "PDD", characterAttributes.weaponDefense);
		characterAttributes.magicDefense = AdminSetItem.ApplyStat(dictionary, "MDD", characterAttributes.magicDefense);
		characterAttributes.accuracy = AdminSetItem.ApplyStat(dictionary, "ACC", characterAttributes.accuracy);
		characterAttributes.avoidability = AdminSetItem.ApplyStat(dictionary, "EVA", characterAttributes.avoidability);
		watk2 = AdminSetItem.ApplyStat(dictionary, "PAD", watk2);
		matk2 = AdminSetItem.ApplyStat(dictionary, "MAD", characterAttributes.intelligence + matk2) - characterAttributes.intelligence;
		if (dictionary.TryGetValue("CriticalChance", out var value4))
		{
			characterAttributes.criticalChance += (float)value4 / 100f;
		}
		if (dictionary.TryGetValue("CriticalDamage", out var value5))
		{
			characterAttributes.criticalBonus += (float)value5 / 100f;
		}
		characterAttributes.maxHp = Mathf.Max(50, characterAttributes.maxHp);
		characterAttributes.maxMp = Mathf.Max(5, characterAttributes.maxMp);
		characterAttributes.setItemBonus = dictionary;
		watk = watk2;
		matk = Math.Min(2000, characterAttributes.intelligence + matk2);
		return characterAttributes;
	}

	public static CharacterAttributes PanelAttributes()
	{
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		int matk;
		int watk;
		if ((Object)(object)playerCtrl != (Object)null)
		{
			return playerCtrl.ReadCombatAttributes(out matk, out watk);
		}
		return Apply(Profile.Attributes, WeaponDefs.Get(Profile.equipWeapon), 0, out watk, out matk);
	}

	private static CharacterAttributes CheckAttributes(CharacterAttributes baseline)
	{
		CharacterAttributes characterAttributes = PanelAttributes();
		if (baseline == null)
		{
			return characterAttributes;
		}
		CharacterAttributes characterAttributes2 = characterAttributes.Copy();
		characterAttributes2.level = Mathf.Max(characterAttributes2.level, baseline.level);
		characterAttributes2.str = Mathf.Max(characterAttributes2.str, baseline.str);
		characterAttributes2.dex = Mathf.Max(characterAttributes2.dex, baseline.dex);
		characterAttributes2.intelligence = Mathf.Max(characterAttributes2.intelligence, baseline.intelligence);
		characterAttributes2.luk = Mathf.Max(characterAttributes2.luk, baseline.luk);
		return characterAttributes2;
	}
}
