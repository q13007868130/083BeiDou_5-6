using System;
using System.Collections.Generic;
using Server.Bot;

public static class NativeGrowthStats
{
	private static FullCharacter cachedOwner;

	private static int cachedFingerprint;

	private static GrowthStatDelta cachedDelta;

	private static readonly List<FlameLine> EmptyFlame = new List<FlameLine>();

	private static InventoryEntry cachedFlameItem;

	private static long cachedFlameCode;

	private static int cachedFlameIncReq;

	private static List<FlameLine> cachedFlameLines;

	public static GrowthStatDelta Accumulate(FullCharacter character, int level, int job)
	{
		if (character == null)
		{
			return new GrowthStatDelta();
		}
		int num = Fingerprint(character, level, job);
		if (character == cachedOwner && num == cachedFingerprint && cachedDelta != null)
		{
			return cachedDelta;
		}
		GrowthStatDelta growthStatDelta = new GrowthStatDelta();
		GrowthState growth = character.growth ?? new GrowthState();
		AccumulateEquipment(character, growthStatDelta);
		AccumulateHyperStats(character, growth, level, growthStatDelta);
		AccumulatePotentials(character, growth, level, growthStatDelta);
		AccumulateHexa(character, job, growthStatDelta);
		AccumulateSoulWeapon(character, growthStatDelta);
		cachedOwner = character;
		cachedFingerprint = num;
		cachedDelta = growthStatDelta;
		return growthStatDelta;
	}

	public static GrowthStatDelta Accumulate(FullCharacter character, int level, int job, int swapSlot, InventoryEntry swapEntry)
	{
		if (character == null)
		{
			return new GrowthStatDelta();
		}
		if (swapSlot == 0 || swapEntry == null)
		{
			return Accumulate(character, level, job);
		}
		GrowthStatDelta growthStatDelta = new GrowthStatDelta();
		GrowthState growth = character.growth ?? new GrowthState();
		AccumulateEquipment(character, growthStatDelta, swapSlot, swapEntry);
		AccumulateHyperStats(character, growth, level, growthStatDelta);
		AccumulatePotentials(character, growth, level, growthStatDelta);
		AccumulateHexa(character, job, growthStatDelta);
		AccumulateSoulWeapon(character, growthStatDelta);
		return growthStatDelta;
	}

	public static void Invalidate()
	{
		cachedOwner = null;
		cachedDelta = null;
	}

	private static int Fingerprint(FullCharacter character, int level, int job)
	{
		int num = (level * 397) ^ job;
		SoulWeaponWz2State soulWeapon = character.soulWeapon;
		if (soulWeapon != null)
		{
			SoulGrowth active = soulWeapon.Active;
			num = num * 31 + (int)soulWeapon.revision;
			num = num * 31 + (active?.level ?? 0);
			num = num * 31 + (active?.option ?? 0);
			num = num * 31 + ((soulWeapon.activeUntil > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) ? 1 : 0);
		}
		GrowthState growth = character.growth;
		if (growth != null)
		{
			num = num * 31 + growth.hyperPreset * 7 + growth.abilityPreset;
		}
		List<InventoryEntry> inventory = character.inventory;
		if (inventory != null)
		{
			num = num * 31 + inventory.Count;
			for (int i = 0; i < inventory.Count; i++)
			{
				InventoryEntry inventoryEntry = inventory[i];
				if (inventoryEntry != null)
				{
					num = num * 31 + (int)inventoryEntry.itemRowId;
					num = num * 31 + inventoryEntry.position;
					num = num * 31 + inventoryEntry.itemId;
					num = num * 31 + inventoryEntry.symbolLevel;
					num = num * 31 + inventoryEntry.symbolInc;
					num = num * 31 + inventoryEntry.str + inventoryEntry.dex * 3 + inventoryEntry.int_ * 5 + inventoryEntry.luk * 7 + inventoryEntry.hp * 11 + inventoryEntry.mp * 13;
					long exGradeOption = inventoryEntry.exGradeOption;
					num = num * 31 + (int)exGradeOption + (int)(exGradeOption >> 32);
					num = num * 31 + inventoryEntry.soulOption;
				}
			}
		}
		List<HyperStatEntry> hyperstats = character.hyperstats;
		if (hyperstats != null)
		{
			num = num * 31 + hyperstats.Count;
			for (int j = 0; j < hyperstats.Count; j++)
			{
				HyperStatEntry hyperStatEntry = hyperstats[j];
				if (hyperStatEntry != null)
				{
					num = num * 31 + hyperStatEntry.preset * 7 + hyperStatEntry.skillId * 3 + hyperStatEntry.slv;
				}
			}
		}
		List<PotentialEntry> potentials = character.potentials;
		if (potentials != null)
		{
			num = num * 31 + potentials.Count;
			for (int k = 0; k < potentials.Count; k++)
			{
				PotentialEntry potentialEntry = potentials[k];
				if (potentialEntry != null)
				{
					num = num * 31 + potentialEntry.preset * 7 + potentialEntry.potKey * 3 + potentialEntry.skillId * 5 + potentialEntry.slv;
				}
			}
		}
		if (character.hexaStats != null)
		{
			foreach (HexaStatView hexaStat in character.hexaStats)
			{
				if (hexaStat != null)
				{
					num = num * 31 + hexaStat.coreId + hexaStat.preset * 3 + (hexaStat.applied ? 7 : 0);
					num = num * 31 + hexaStat.stat1 + hexaStat.stat2 * 7 + hexaStat.stat3 * 49;
					num = num * 31 + hexaStat.level1 + hexaStat.level2 * 11 + hexaStat.level3 * 121;
				}
			}
		}
		return num;
	}

	private static void AccumulateSoulWeapon(FullCharacter character, GrowthStatDelta delta)
	{
		SoulWeaponWz2State soulWeapon = character.soulWeapon;
		if (soulWeapon == null || !soulWeapon.unlocked || !soulWeapon.Equipped(character))
		{
			return;
		}
		SoulGrowth soulGrowth = soulWeapon.Active ?? new SoulGrowth();
		int value = SoulWeaponWz2Catalog.PassiveAttack(soulGrowth.level);
		delta.Add("PAD", value);
		delta.Add("MAD", value);
		foreach (KeyValuePair<string, int> item in PotentialStats.CalculateValues(SoulWeaponWz2Catalog.OptionStats(soulGrowth.option)))
		{
			if (item.Key == "CriticalChance")
			{
				delta.criticalChance += (float)item.Value / 100f;
			}
			else
			{
				delta.Add(item.Key, item.Value);
			}
		}
		if (soulWeapon.activeUntil > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
		{
			int value2 = SoulWeaponWz2Catalog.ActiveAttackPercent(soulGrowth.grade);
			delta.Add("PADPct", value2);
			delta.Add("MADPct", value2);
		}
	}

	private static void AccumulateHexa(FullCharacter character, int job, GrowthStatDelta delta)
	{
		if (character.hexaStats == null)
		{
			return;
		}
		foreach (HexaStatView hexaStat in character.hexaStats)
		{
			if (hexaStat != null && hexaStat.applied && hexaStat.level == hexaStat.level1 + hexaStat.level2 + hexaStat.level3)
			{
				ApplyHexa(hexaStat.stat1, hexaStat.level1, main: true, job, delta);
				ApplyHexa(hexaStat.stat2, hexaStat.level2, main: false, job, delta);
				ApplyHexa(hexaStat.stat3, hexaStat.level3, main: false, job, delta);
			}
		}
	}

	private static void ApplyHexa(int type, int level, bool main, int job, GrowthStatDelta delta)
	{
		int num = HexaCatalog.StatValue(type, level, main);
		switch (type)
		{
		case 0:
			delta.criticalBonus += (float)num / 10000f;
			break;
		case 1:
			delta.hexaBossPercent += (float)num / 100f;
			break;
		case 2:
			delta.hexaIgnorePercent += (float)num / 100f;
			break;
		case 3:
			delta.hexaDamagePercent += (float)num / 100f;
			break;
		case 4:
			delta.Add("PAD", num);
			break;
		case 5:
			delta.Add("MAD", num);
			break;
		case 6:
			if (job / 100 == 36)
			{
				delta.Add("STR", num * 40);
				delta.Add("DEX", num * 40);
				delta.Add("LUK", num * 40);
			}
			else
			{
				delta.Add(HexaCatalog.MainStat(job), HexaCatalog.MainStatValue(num, job));
			}
			break;
		}
	}

	private static void AccumulateEquipment(FullCharacter character, GrowthStatDelta delta, int swapSlot = 0, InventoryEntry swapEntry = null)
	{
		if (character.inventory != null)
		{
			foreach (InventoryEntry item in character.inventory)
			{
				if (item != null && item.position < 0 && item.quantity > 0 && (swapSlot == 0 || item.position != swapSlot))
				{
					AccumulateWornItem(item, character, delta, item.position);
				}
			}
		}
		if (swapEntry != null && swapEntry.quantity > 0)
		{
			AccumulateWornItem(swapEntry, character, delta, swapSlot);
		}
	}

	private static void AccumulateWornItem(InventoryEntry item, FullCharacter character, GrowthStatDelta delta, int position)
	{
		if (position < 0 && SymbolCatalog.IsSymbolSlot(position))
		{
			SymbolInfo symbolInfo = SymbolCatalog.Of(item.itemId);
			if (symbolInfo == null)
			{
				return;
			}
			if (symbolInfo.IsArcane)
			{
				delta.arcaneForce += item.symbolInc;
			}
			else
			{
				delta.authenticForce += item.symbolInc;
			}
			if (position == SymbolCatalog.SlotOf(item.itemId))
			{
				string[] array = new string[3] { "ExpRate", "MesoRate", "DropRate" };
				foreach (string text in array)
				{
					delta.Add(text, SymbolCatalog.GrandBonus(item.itemId, item.symbolLevel, text));
				}
			}
			delta.Add("STR", item.str);
			delta.Add("DEX", item.dex);
			delta.Add("INT", item.int_);
			delta.Add("LUK", item.luk);
			delta.Add("HP", item.hp);
			delta.Add("MP", item.mp);
		}
		else
		{
			if (item.exGradeOption > 0)
			{
				AccumulateFlame(item, delta);
			}
			if (item.soulOption > 0 && (character.soulWeapon == null || !character.soulWeapon.unlocked))
			{
				ApplySoul(item.soulOption, delta);
			}
		}
	}

	private static void AccumulateFlame(InventoryEntry item, GrowthStatDelta delta)
	{
		foreach (FlameLine item2 in DescribeFlame(item))
		{
			if (item2.statVal >= 0 && item2.value > 0)
			{
				ApplyFlameStat(item2.statVal, item2.value, delta);
			}
		}
	}

	public static List<FlameLine> DescribeFlame(InventoryEntry item)
	{
		if (item == null || item.exGradeOption <= 0)
		{
			return EmptyFlame;
		}
		int result = 0;
		if (item.equipmentData != null && item.equipmentData.TryGetValue("iIncReq", out var value))
		{
			int.TryParse(Convert.ToString(value), out result);
		}
		if (item == cachedFlameItem && item.exGradeOption == cachedFlameCode && result == cachedFlameIncReq)
		{
			return cachedFlameLines;
		}
		List<FlameLine> list = new List<FlameLine>();
		Dictionary<string, object> dictionary = BotItemCatalog.Shared.Info(item.itemId);
		int reqLevel = ((dictionary != null) ? BotItemCatalog.Int(dictionary, "reqLevel") : 0);
		bool weapon = NativeSoulService.IsWeapon(item.itemId);
		bool bossWeapon = dictionary != null && BotItemCatalog.Int(dictionary, "bossReward") != 0;
		int baseAttack = ((dictionary != null) ? Math.Max(BotItemCatalog.Int(dictionary, "incPAD"), BotItemCatalog.Int(dictionary, "incMAD")) : 0);
		foreach (FlameLine item2 in NativeFlameService.Unpack(item.exGradeOption))
		{
			item2.value = ((item2.statVal >= 0) ? FlameCatalog.Value(item2.statVal, item2.tier, reqLevel, result, weapon, bossWeapon, baseAttack) : 0);
			list.Add(item2);
		}
		cachedFlameItem = item;
		cachedFlameCode = item.exGradeOption;
		cachedFlameIncReq = result;
		cachedFlameLines = list;
		return list;
	}

	private static void ApplyFlameStat(int statVal, int value, GrowthStatDelta delta)
	{
		switch (statVal)
		{
		case 0:
			delta.Add("STR", value);
			break;
		case 1:
			delta.Add("DEX", value);
			break;
		case 2:
			delta.Add("INT", value);
			break;
		case 3:
			delta.Add("LUK", value);
			break;
		case 4:
			delta.Add("STR", value);
			delta.Add("DEX", value);
			break;
		case 5:
			delta.Add("STR", value);
			delta.Add("INT", value);
			break;
		case 6:
			delta.Add("STR", value);
			delta.Add("LUK", value);
			break;
		case 7:
			delta.Add("DEX", value);
			delta.Add("INT", value);
			break;
		case 8:
			delta.Add("DEX", value);
			delta.Add("LUK", value);
			break;
		case 9:
			delta.Add("INT", value);
			delta.Add("LUK", value);
			break;
		case 10:
			delta.Add("PAD", value);
			break;
		case 11:
			delta.Add("MAD", value);
			break;
		case 12:
			delta.Add("PDD", value);
			break;
		case 13:
			delta.Add("HP", value);
			break;
		case 14:
			delta.Add("MP", value);
			break;
		case 15:
			delta.Add("SPD", value);
			break;
		case 16:
			delta.Add("JMP", value);
			break;
		case 17:
			delta.Add("STRPct", value);
			delta.Add("DEXPct", value);
			delta.Add("INTPct", value);
			delta.Add("LUKPct", value);
			break;
		case 18:
			delta.Add("BossDamage", value);
			break;
		case 19:
			delta.Add("Damage", value);
			break;
		}
	}

	private static void ApplySoul(int optionCode, GrowthStatDelta delta)
	{
		SoulOption soulOption = SoulCatalog.Of(optionCode);
		if (soulOption != null && !string.IsNullOrEmpty(soulOption.key))
		{
			ApplyAbilityKey(soulOption.key, soulOption.value, 0, delta);
		}
	}

	private static void AccumulateHyperStats(FullCharacter character, GrowthState growth, int level, GrowthStatDelta delta)
	{
		if (character.hyperstats == null)
		{
			return;
		}
		foreach (HyperStatEntry hyperstat in character.hyperstats)
		{
			if (hyperstat.preset != growth.hyperPreset || hyperstat.slv <= 0)
			{
				continue;
			}
			HyperStatInfo hyperStatInfo = HyperStatCatalog.Of(hyperstat.skillId);
			if (hyperStatInfo == null || string.IsNullOrEmpty(hyperStatInfo.key))
			{
				continue;
			}
			int num = hyperStatInfo.Value(hyperstat.slv);
			if (num != 0)
			{
				if (hyperStatInfo.key == "Arcane")
				{
					delta.arcaneForce += num;
				}
				else
				{
					ApplyAbilityKey(hyperStatInfo.key, num, level, delta);
				}
			}
		}
	}

	private static void AccumulatePotentials(FullCharacter character, GrowthState growth, int level, GrowthStatDelta delta)
	{
		if (character.potentials == null)
		{
			return;
		}
		int num = NativeInnerAbilityService.UnlockedLines(level);
		foreach (PotentialEntry potential in character.potentials)
		{
			if (potential.preset != growth.abilityPreset || potential.potKey >= num || potential.slv <= 0)
			{
				continue;
			}
			AbilityInfo abilityInfo = AbilityCatalog.Of(potential.skillId);
			if (abilityInfo == null || abilityInfo.keys == null || abilityInfo.formulas == null)
			{
				continue;
			}
			for (int i = 0; i < abilityInfo.keys.Length && i < abilityInfo.formulas.Length; i++)
			{
				int num2 = GrowthFormula.Evaluate(abilityInfo.formulas[i], potential.slv);
				if (num2 != 0)
				{
					ApplyAbilityKey(abilityInfo.keys[i], num2, level, delta);
				}
			}
		}
	}

	private static void ApplyAbilityKey(string key, int value, int level, GrowthStatDelta delta)
	{
		switch (key)
		{
		case "STR":
			delta.Add("STR", value);
			break;
		case "DEX":
			delta.Add("DEX", value);
			break;
		case "INT":
			delta.Add("INT", value);
			break;
		case "LUK":
			delta.Add("LUK", value);
			break;
		case "MHP":
			delta.Add("HP", value);
			break;
		case "MMP":
			delta.Add("MP", value);
			break;
		case "PDD":
			delta.Add("PDD", value);
			break;
		case "MDD":
			delta.Add("MDD", value);
			break;
		case "PAD":
			delta.Add("PAD", value);
			break;
		case "MAD":
			delta.Add("MAD", value);
			break;
		case "ACC":
			delta.Add("ACC", value);
			break;
		case "EVA":
			delta.Add("EVA", value);
			break;
		case "Speed":
			delta.Add("SPD", value);
			break;
		case "Jump":
			delta.Add("JMP", value);
			break;
		case "HPR":
		case "MHPR":
			delta.Add("HPPct", value);
			break;
		case "MPR":
		case "MMPR":
			delta.Add("MPPct", value);
			break;
		case "PDDR":
			delta.Add("PDDPct", value);
			break;
		case "Damage":
			delta.Add("Damage", value);
			break;
		case "BossDamage":
			delta.Add("BossDamage", value);
			break;
		case "NormalDamage":
			delta.Add("NormalDamage", value);
			break;
		case "IgnoreDefense":
			delta.Add("IgnoreDefense", value);
			break;
		case "ExpRate":
			delta.Add("ExpRate", value);
			break;
		case "DropRate":
			delta.Add("DropRate", value);
			break;
		case "MesoRate":
			delta.Add("MesoRate", value);
			break;
		case "CriticalChance":
			delta.criticalChance += (float)value / 100f;
			break;
		case "CriticalDamage":
			delta.criticalBonus += (float)value / 100f;
			break;
		case "LV2PAD":
			if (level > 0 && value > 0)
			{
				delta.Add("PAD", level / value);
			}
			break;
		case "LV2MAD":
			if (level > 0 && value > 0)
			{
				delta.Add("MAD", level / value);
			}
			break;
		case "AllStats":
			delta.Add("STR", value);
			delta.Add("DEX", value);
			delta.Add("INT", value);
			delta.Add("LUK", value);
			break;
		default:
		{
			string item = (string.IsNullOrEmpty(key) ? "?" : key);
			if (!delta.unsettled.Contains(item))
			{
				delta.unsettled.Add(item);
			}
			break;
		}
		}
	}
}
