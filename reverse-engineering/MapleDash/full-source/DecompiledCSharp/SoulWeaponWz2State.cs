using System;
using System.Collections.Generic;

public sealed class SoulWeaponWz2State
{
	public long weapon;

	public long revision;

	public long activeUntil;

	public long cooldownUntil;

	public int level = 1;

	public int grade = 1;

	public int exp;

	public int fragments;

	public int option;

	public bool unlocked;

	public bool converted;

	public string week = "";

	public Dictionary<long, SoulGrowth> souls = new Dictionary<long, SoulGrowth>();

	public Dictionary<string, int> weekly = new Dictionary<string, int>();

	public List<int> cleared = new List<int>();

	public SoulGrowth Active => Of(weapon);

	public SoulGrowth Of(long rowId)
	{
		if (rowId <= 0 || !souls.TryGetValue(rowId, out var value))
		{
			return null;
		}
		return value;
	}

	public bool Has(long rowId)
	{
		if (rowId > 0)
		{
			return souls.ContainsKey(rowId);
		}
		return false;
	}

	public SoulGrowth Ensure(long rowId)
	{
		if (!souls.TryGetValue(rowId, out var value))
		{
			value = (souls[rowId] = new SoulGrowth());
		}
		return value;
	}

	public void Mirror()
	{
		SoulGrowth active = Active;
		if (active != null)
		{
			level = active.level;
			grade = active.grade;
			exp = active.exp;
			option = active.option;
			converted = active.converted;
		}
	}

	public bool Equipped(FullCharacter character)
	{
		if (weapon > 0 && character?.inventory != null)
		{
			return character.inventory.Exists((InventoryEntry i) => i.itemRowId == weapon && i.position == -11 && (i.expiration <= 0 || i.expiration > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
		}
		return false;
	}
}
