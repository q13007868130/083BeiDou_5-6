using UnityEngine;

public static class NativeCombatOrders
{
	public static int ActiveBonus
	{
		get
		{
			PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
			NativeBuffs nativeBuffs = (((Object)(object)playerCtrl != (Object)null) ? ((Component)playerCtrl).GetComponent<NativeBuffs>() : null);
			if (!((Object)(object)nativeBuffs != (Object)null))
			{
				return 0;
			}
			return nativeBuffs.SkillLevelBoost;
		}
	}

	public static int Bonus(int skillId)
	{
		int activeBonus = ActiveBonus;
		if (activeBonus <= 0 || !Applies(skillId))
		{
			return 0;
		}
		return activeBonus;
	}

	public static bool Applies(int id)
	{
		SkillData skillData = SkillBook.Get(id);
		if (skillData == null)
		{
			return false;
		}
		if (skillData.autoLearn)
		{
			return false;
		}
		if (skillData.hexa)
		{
			return false;
		}
		if (id >= 400000000)
		{
			return false;
		}
		int num = id / 10000;
		if (num % 1000 == 0)
		{
			return false;
		}
		return num % 10 <= 2;
	}

	public static int Cap(int id, int bonus)
	{
		SkillData skillData = SkillBook.Get(id);
		if (skillData == null)
		{
			return 0;
		}
		int num = SkillBook.MasterCapOf(id);
		if (!SkillBook.IsFourthJobSkill(id))
		{
			return num;
		}
		return Mathf.Min(skillData.maxLevel, num + bonus);
	}
}
