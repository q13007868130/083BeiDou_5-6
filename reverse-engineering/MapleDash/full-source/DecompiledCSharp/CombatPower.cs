using UnityEngine;

public static class CombatPower
{
	public static long Of(long stats, long attack, long maxHp, long maxMp)
	{
		if (stats < 0)
		{
			stats = 0L;
		}
		if (attack < 1)
		{
			attack = 1L;
		}
		if (maxHp < 0)
		{
			maxHp = 0L;
		}
		if (maxMp < 0)
		{
			maxMp = 0L;
		}
		long num = ((stats != 0L && attack > long.MaxValue / stats) ? long.MaxValue : (stats * attack));
		long num2 = maxHp / 10 + maxMp / 20;
		if (num <= long.MaxValue - num2)
		{
			return num + num2;
		}
		return long.MaxValue;
	}

	public static long Of(CharacterAttributes a, int watk, int matk)
	{
		if (a == null)
		{
			return 0L;
		}
		return Of((long)a.str + (long)a.dex + a.intelligence + a.luk, Mathf.Max(watk, matk), a.maxHp, a.maxMp);
	}

	public static long Current()
	{
		int watk;
		int matk;
		return Of(Panel(out watk, out matk), watk, matk);
	}

	private static CharacterAttributes Panel(out int watk, out int matk)
	{
		return EquipLoadout.Apply(Profile.Attributes, WeaponDefs.Get(Profile.equipWeapon), 0, out watk, out matk);
	}

	public static bool IsEquippable(int itemId)
	{
		if (itemId > 0)
		{
			return EquipSlot.SlotOf(itemId) != 0;
		}
		return false;
	}

	public static bool TryPreview(InventoryEntry entry, out long current, out long after)
	{
		current = (after = 0L);
		if (entry == null || entry.itemId <= 0 || entry.position < 0)
		{
			return false;
		}
		int num = EquipSlot.SlotOf(entry.itemId);
		if (num == 0)
		{
			return false;
		}
		current = Current();
		WeaponDef weapon = WeaponDefs.Get(Profile.equipWeapon);
		if (num == -11 && WeaponDefs.TryGet(entry.itemId.ToString(), out var weapon2))
		{
			weapon = weapon2;
		}
		CharacterAttributes a = EquipLoadout.Apply(Profile.Attributes, weapon, 0, out var watk, out var matk, num, entry.itemId, entry);
		after = Of(a, watk, matk);
		return true;
	}
}
