using System;
using UnityEngine;

public static class QuickSlots
{
	public const int Count = 26;

	private static readonly int[] ids = new int[26];

	private static bool loaded;

	public static GameAction Action(int index)
	{
		if (index >= 8)
		{
			return (GameAction)(25 + index - 8);
		}
		return (GameAction)(7 + index);
	}

	public static int Index(GameAction action)
	{
		if (action < GameAction.Slot1 || action > GameAction.Slot8)
		{
			if (action < GameAction.Slot9 || action > GameAction.Slot26)
			{
				return -1;
			}
			return (int)(action - 25 + 8);
		}
		return (int)(action - 7);
	}

	public static void Ensure()
	{
		if (!loaded)
		{
			loaded = true;
			for (int i = 0; i < 26; i++)
			{
				ids[i] = PlayerPrefs.GetInt("mxd.qs." + i, 0);
			}
		}
	}

	public static void LoadCharacter(int[] skills)
	{
		loaded = true;
		Array.Clear(ids, 0, ids.Length);
		Array.Copy(skills, ids, Math.Min(skills.Length, ids.Length));
	}

	public static int Id(int slot)
	{
		Ensure();
		if (slot < 0 || slot >= 26)
		{
			return 0;
		}
		return ids[slot];
	}

	public static void Bind(int slot, int skillId)
	{
		Ensure();
		if (slot >= 0 && slot < 26)
		{
			ids[slot] = skillId;
			PlayerPrefs.SetInt("mxd.qs." + slot, skillId);
			PlayerPrefs.Save();
		}
	}

	public static bool IsItem(int id)
	{
		if (id < 0)
		{
			return !NativeSkillMacros.IsMacro(id);
		}
		return false;
	}

	public static bool CanBindToQuickBar(int id, out string why)
	{
		why = null;
		if (id == 0)
		{
			why = "这一格没有可绑定的东西。";
			return false;
		}
		if (IsItem(id) || NativeSkillMacros.IsMacro(id))
		{
			return true;
		}
		SkillData skillData = SkillBook.Get(id);
		if (skillData == null)
		{
			why = "技能数据缺失，不能绑到快捷栏。";
			return false;
		}
		if (skillData.IsPassive)
		{
			why = "「" + skillData.name + "」是被动技能，按不下去，不能绑到快捷栏。";
			return false;
		}
		return true;
	}

	public static Texture2D BindingIcon(int id)
	{
		if (NativeSkillMacros.IsMacro(id))
		{
			int num = NativeSkillMacros.FirstSkill(NativeSkillMacros.MarkerIndex(id));
			SkillData skillData = ((num != 0) ? SkillBook.Get(num) : null);
			if (skillData == null || string.IsNullOrEmpty(skillData.icon))
			{
				return null;
			}
			return Art.Tex(skillData.icon);
		}
		if (IsItem(id))
		{
			return NativeItemSource.Icon(-id);
		}
		SkillData skillData2 = SkillBook.Get(id);
		if (skillData2 == null || string.IsNullOrEmpty(skillData2.icon))
		{
			return null;
		}
		return Art.Tex(skillData2.icon);
	}

	public static void Clear(int slot)
	{
		Bind(slot, 0);
	}

	public static void Cast(int slot)
	{
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		if ((Object)(object)playerCtrl == (Object)null)
		{
			return;
		}
		int num = Id(slot);
		if (NativeSkillMacros.IsMacro(num))
		{
			NativeSkillMacros.Cast(NativeSkillMacros.MarkerIndex(num));
			return;
		}
		if (IsItem(num))
		{
			NativeInventoryActions.DoubleClick(-num);
			return;
		}
		SkillSystem component = ((Component)playerCtrl).GetComponent<SkillSystem>();
		if (!((Object)(object)component == (Object)null))
		{
			if (num == 0)
			{
				component.Report("快捷格 " + (slot + 1) + " 是空的（技能窗里拖技能上去）");
			}
			else if (!component.TryCastBoundSkill(num, Action(slot)))
			{
				component.Report(component.LastFailure);
			}
		}
	}
}
