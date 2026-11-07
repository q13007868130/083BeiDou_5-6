using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class QuickBarKeys
{
	private static readonly KeyCode[] defaults;

	public static KeyCode[] Defaults()
	{
		KeyCode[] array = new KeyCode[26];
		Array.Copy(defaults, array, defaults.Length);
		return array;
	}

	public static KeyCode Of(int index)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		return NativeKeyLayout.FunctionKey((KeyCode)PlayerPrefs.GetInt("mxd.quickDisplay." + index, (index < defaults.Length) ? ((int)defaults[index]) : 0));
	}

	public static KeyCode[] Copy()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected I4, but got Unknown
		KeyCode[] array = new KeyCode[26];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (KeyCode)(int)Of(i);
		}
		return array;
	}

	public static void Save(KeyCode[] keys)
	{
		for (int i = 0; i < keys.Length; i++)
		{
			PlayerPrefs.SetInt("mxd.quickDisplay." + i, (int)keys[i]);
		}
		PlayerPrefs.Save();
	}

	public static int SkillSlot(KeyCode key)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		if ((int)key == 0)
		{
			return -1;
		}
		for (int i = 0; i < 26; i++)
		{
			if (KeyMap.Of(QuickSlots.Action(i)) == key && QuickSlots.Id(i) != 0)
			{
				return i;
			}
		}
		return -1;
	}

	public static GameAction? Function(KeyCode key)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		if ((int)key == 0)
		{
			return null;
		}
		foreach (KeyValuePair<GameAction, KeyCode> item in KeyMap.All)
		{
			if (item.Value == key && QuickSlots.Index(item.Key) < 0)
			{
				return item.Key;
			}
		}
		return null;
	}

	public static bool BindSkill(int displayIndex, int skillId)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		KeyCode val = Of(displayIndex);
		if ((int)val == 0)
		{
			NativeUtilPrompt.Notice("这一格还没有选择按键，请先在键盘设置里给它指定一个键。");
			return false;
		}
		return BindSkillToKey(val, skillId);
	}

	public static int FreeKeyedSlot()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < 26; i++)
		{
			KeyCode val = Of(i);
			if ((int)val != 0 && KeyMap.Of(QuickSlots.Action(i)) == val && SkillSlot(val) < 0)
			{
				return i;
			}
		}
		return -1;
	}

	public static bool BindSkillToKey(KeyCode key, int skillId)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		if (!QuickSlots.CanBindToQuickBar(skillId, out var why))
		{
			if (!string.IsNullOrEmpty(why))
			{
				NativeUtilPrompt.Notice(why);
			}
			return false;
		}
		int num = SkillSlot(key);
		if (num < 0)
		{
			for (int i = 0; i < 26; i++)
			{
				if (QuickSlots.Id(i) == 0 || (int)KeyMap.Of(QuickSlots.Action(i)) == 0)
				{
					num = i;
					break;
				}
			}
		}
		if (num < 0)
		{
			NativeUtilPrompt.Notice("快捷栏的技能格已经满了，先清掉一格再绑定。");
			return false;
		}
		KeyMap.Bind(QuickSlots.Action(num), key);
		QuickSlots.Bind(num, skillId);
		return true;
	}

	static QuickBarKeys()
	{
		KeyCode[] array = new KeyCode[8];
		RuntimeHelpers.InitializeArray(array, __ldtoken(_003CPrivateImplementationDetails_003E._73362CD7168754F160EB051E8568BCB1706933C25970CB76D64056B441DC6E28));
		defaults = array;
	}
}
