using System;
using System.Collections.Generic;

[Serializable]
public sealed class CharacterAttributes
{
	public int level = 1;

	public int str = 12;

	public int dex = 5;

	public int intelligence = 4;

	public int luk = 4;

	public int maxHp = 50;

	public int maxMp = 5;

	public int weaponDefense;

	public int magicDefense;

	public int accuracy;

	public int avoidability;

	public int equipmentSpeed;

	public int equipmentJump;

	public float mastery = 0.1f;

	public float criticalChance;

	public float criticalBonus;

	public float overCritBonus;

	public float hexaDamagePercent;

	public float hexaBossPercent;

	public float hexaIgnorePercent;

	public int arcaneForce;

	public int authenticForce;

	public Dictionary<string, int> setItemBonus;

	public CharacterAttributes Copy()
	{
		return (CharacterAttributes)MemberwiseClone();
	}

	public CharacterAttributes WithWeapon(WeaponDef weapon, int attackBuff, out int watk, out int matk)
	{
		return EquipLoadout.Apply(this, weapon, attackBuff, out watk, out matk);
	}
}
