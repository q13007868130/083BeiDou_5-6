using UnityEngine;

public static class NativeMount
{
	public static int Saddle => Profile.EquipOfSlot(VisibleSlot(-19));

	public static bool IsSkill(int id)
	{
		if (id != 1004 && id != 10001004)
		{
			return id == 20001004;
		}
		return true;
	}

	public static int VisibleSlot(int slot)
	{
		if ((slot != -18 && slot != -19) || Profile.EquipOfSlot(slot - 100) <= 0)
		{
			return slot;
		}
		return slot - 100;
	}

	public static int Item(PlayerCtrl player)
	{
		if (!IsSkill(((Component)player).GetComponent<NativeBuffs>()?.Value("MONSTER_RIDING") ?? 0))
		{
			return 0;
		}
		return Profile.EquipOfSlot(VisibleSlot(-18));
	}

	private static bool Fits(int mount, int saddle)
	{
		return NativeItemSource.Node(saddle)?.Get(mount.ToString()) != null;
	}

	public static string WearError(int item, int slot)
	{
		if (slot != -18 && slot != -19 && slot != -118 && slot != -119)
		{
			return null;
		}
		bool flag = slot < -100;
		int num = (flag ? (slot + 100) : slot);
		if (flag && Profile.EquipOfSlot(num) == 0)
		{
			if (num != -18)
			{
				return "请先装备普通鞍具，再装备现金鞍具";
			}
			return "请先装备普通骑兽，再装备现金骑兽";
		}
		int slot2;
		if (num != -18)
		{
			slot2 = (flag ? (-118) : (-18));
		}
		else
		{
			slot2 = (flag ? (-119) : (-19));
		}
		int num2 = Profile.EquipOfSlot(slot2);
		if (num2 > 0 && !((num == -18) ? Fits(item, num2) : Fits(num2, item)))
		{
			if (!flag)
			{
				return "鞍具与坐骑不匹配";
			}
			return "现金骑兽与现金鞍具不匹配";
		}
		return null;
	}

	public static string Toggle(PlayerCtrl player, int skill, int level)
	{
		NativeBuffs nativeBuffs = NativeBuffs.For(player);
		if (nativeBuffs.HasSource(skill))
		{
			nativeBuffs.Cancel(skill);
			player.RefreshFieldAppearance();
			return null;
		}
		if (player.hp <= 0f || player.IsClimbing || player.Sitting)
		{
			return "当前状态无法骑乘";
		}
		int num = Profile.EquipOfSlot(-18);
		int num2 = Profile.EquipOfSlot(-19);
		if (num / 10000 != 190 || num2 / 10000 != 191)
		{
			return "请先装备普通骑兽和普通鞍具（现金骑宠不能代替）";
		}
		int num3 = Profile.EquipOfSlot(-118);
		int num4 = Profile.EquipOfSlot(-119);
		if (num3 > 0 || num4 > 0)
		{
			if (num3 / 10000 != 190 || num4 / 10000 != 191 || !Fits(num3, num4))
			{
				return "现金骑兽需要装备配套的现金鞍具";
			}
		}
		else if (!Fits(num, num2))
		{
			return "鞍具与坐骑不匹配";
		}
		if (!nativeBuffs.ApplySkill(skill, level, NativeBuffs.Now))
		{
			return "骑兽技能效果数据缺失";
		}
		player.RefreshFieldAppearance();
		return null;
	}
}
