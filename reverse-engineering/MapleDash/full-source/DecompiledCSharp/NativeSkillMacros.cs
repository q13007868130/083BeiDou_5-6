using System.Collections.Generic;
using UnityEngine;

public static class NativeSkillMacros
{
	public const int Slots = 5;

	public const int Chain = 3;

	private static readonly MacroEntry[] slots = new MacroEntry[5];

	private static bool loaded;

	public static int Marker(int index)
	{
		return -1000000 - index;
	}

	public static int MarkerIndex(int marker)
	{
		return -1000000 - marker;
	}

	public static bool IsMacro(int id)
	{
		if (id <= Marker(4))
		{
			return id >= Marker(0);
		}
		return false;
	}

	public static bool HasContent(int index)
	{
		EnsureLoaded();
		MacroEntry macroEntry = slots[index];
		if (macroEntry != null)
		{
			return !macroEntry.IsEmpty;
		}
		return false;
	}

	public static void Reset()
	{
		loaded = false;
		for (int i = 0; i < 5; i++)
		{
			slots[i] = null;
		}
	}

	private static void EnsureLoaded()
	{
		if (loaded)
		{
			return;
		}
		loaded = true;
		List<MacroEntry> list = GameSession.Character?.macros;
		if (list == null)
		{
			return;
		}
		foreach (MacroEntry item in list)
		{
			if (item != null && item.position >= 0 && item.position < 5)
			{
				slots[item.position] = new MacroEntry
				{
					position = item.position,
					name = (item.name ?? ""),
					shout = item.shout,
					skill1 = item.skill1,
					skill2 = item.skill2,
					skill3 = item.skill3
				};
			}
		}
	}

	public static string Name(int index)
	{
		EnsureLoaded();
		return slots[index]?.name ?? "";
	}

	public static bool ShowName(int index)
	{
		EnsureLoaded();
		if (slots[index] != null)
		{
			return slots[index].shout != 0;
		}
		return false;
	}

	public static int Skill(int index, int chain)
	{
		EnsureLoaded();
		MacroEntry macroEntry = slots[index];
		if (macroEntry == null)
		{
			return 0;
		}
		return chain switch
		{
			1 => macroEntry.skill2, 
			0 => macroEntry.skill1, 
			_ => macroEntry.skill3, 
		};
	}

	public static void SetName(int index, string name)
	{
		EnsureLoaded();
		if (slots[index] == null)
		{
			slots[index] = new MacroEntry
			{
				position = index
			};
		}
		slots[index].name = name ?? "";
	}

	public static void SetSkill(int index, int chain, int skillId)
	{
		EnsureLoaded();
		if (slots[index] == null)
		{
			slots[index] = new MacroEntry
			{
				position = index
			};
		}
		switch (chain)
		{
		case 0:
			slots[index].skill1 = skillId;
			break;
		case 1:
			slots[index].skill2 = skillId;
			break;
		default:
			slots[index].skill3 = skillId;
			break;
		}
	}

	public static void ToggleShow(int index)
	{
		EnsureLoaded();
		if (slots[index] == null)
		{
			slots[index] = new MacroEntry
			{
				position = index
			};
		}
		slots[index].shout = ((slots[index].shout == 0) ? 1 : 0);
	}

	public static int FirstSkill(int index)
	{
		for (int i = 0; i < 3; i++)
		{
			int num = Skill(index, i);
			if (num != 0 && SkillProgress.LevelOf(num) > 0)
			{
				return num;
			}
		}
		for (int j = 0; j < 3; j++)
		{
			if (Skill(index, j) != 0)
			{
				return Skill(index, j);
			}
		}
		return 0;
	}

	public static string Commit()
	{
		EnsureLoaded();
		if (GameSession.Character == null)
		{
			return "未进入游戏";
		}
		List<MacroEntry> list = new List<MacroEntry>(5);
		for (int i = 0; i < 5; i++)
		{
			MacroEntry macroEntry = slots[i] ?? new MacroEntry
			{
				position = i
			};
			macroEntry.position = i;
			if (macroEntry.name != null && macroEntry.name.Length > 12)
			{
				return "宏名过长";
			}
			list.Add(macroEntry);
		}
		MacroResponse macroResponse = GameSession.Net.Macro(new MacroRequest
		{
			characterId = GameSession.CharacterId,
			macros = list
		});
		if (!macroResponse.ok)
		{
			return macroResponse.error;
		}
		GameSession.Character.macros = list;
		return null;
	}

	public static void ClearSkillRefs(int skillId)
	{
		EnsureLoaded();
		bool flag = false;
		for (int i = 0; i < 5; i++)
		{
			MacroEntry macroEntry = slots[i];
			if (macroEntry != null)
			{
				if (macroEntry.skill1 == skillId)
				{
					macroEntry.skill1 = 0;
					flag = true;
				}
				if (macroEntry.skill2 == skillId)
				{
					macroEntry.skill2 = 0;
					flag = true;
				}
				if (macroEntry.skill3 == skillId)
				{
					macroEntry.skill3 = 0;
					flag = true;
				}
			}
		}
		if (flag)
		{
			Commit();
		}
	}

	public static void Cast(int index)
	{
		EnsureLoaded();
		if (!HasContent(index))
		{
			return;
		}
		MacroEntry macroEntry = slots[index];
		int num = 0;
		for (int i = 0; i < 3; i++)
		{
			int num2 = i switch
			{
				1 => macroEntry.skill2, 
				0 => macroEntry.skill1, 
				_ => macroEntry.skill3, 
			};
			if (num2 != 0 && SkillProgress.LevelOf(num2) > 0 && SkillCast.TryCast(num2, out var _))
			{
				num++;
			}
		}
		if (macroEntry.shout != 0 && !string.IsNullOrEmpty(macroEntry.name) && (Object)(object)GameLoop.I?.field != (Object)null)
		{
			(((Component)GameLoop.I.field).GetComponent<BotField>() ?? ((Component)GameLoop.I.field).gameObject.AddComponent<BotField>()).ShowLocalBalloon(macroEntry.name);
		}
		if (num == 0)
		{
			BottomBar.Toast("宏「" + macroEntry.name + "」里的技能都还不能施放");
		}
	}
}
