using System;
using System.Collections.Generic;
using UnityEngine;

public static class NativeSkillEnhance
{
	private sealed class Spec
	{
		public int Wrapper;

		public int MaxLevel;

		public int Source;

		public List<int[]> Levels = new List<int[]>();
	}

	private static bool ready;

	private static readonly Dictionary<int, List<Spec>> bySkill = new Dictionary<int, List<Spec>>();

	private static void Ensure()
	{
		if (ready)
		{
			return;
		}
		ready = true;
		TextAsset val = Resources.Load<TextAsset>("NativeSkillEnhance");
		if ((Object)(object)val == (Object)null || !(MiniJson.Parse(val.text) is Dictionary<string, object> dictionary) || !dictionary.TryGetValue("cores", out var value) || !(value is Dictionary<string, object> dictionary2))
		{
			return;
		}
		foreach (KeyValuePair<string, object> item in dictionary2)
		{
			if (!(item.Value is Dictionary<string, object> dictionary3))
			{
				continue;
			}
			Spec spec = new Spec
			{
				Wrapper = Art.Id(dictionary3, "w"),
				MaxLevel = Mathf.Max(1, Art.Id(dictionary3, "max", 60)),
				Source = Art.Id(dictionary3, "s")
			};
			if (!dictionary3.TryGetValue("lv", out var value2) || !(value2 is List<object> list))
			{
				Debug.LogWarning((object)("[SkillEnhance] " + item.Key + " 没有等级数组，跳过"));
				continue;
			}
			foreach (object item2 in list)
			{
				if (!(item2 is List<object> list2))
				{
					spec.Levels.Add(new int[4]);
					continue;
				}
				spec.Levels.Add(new int[4]
				{
					(list2.Count > 0) ? Convert.ToInt32(list2[0]) : 0,
					(list2.Count > 1) ? Convert.ToInt32(list2[1]) : 0,
					(list2.Count > 2) ? Convert.ToInt32(list2[2]) : 0,
					(list2.Count > 3) ? Convert.ToInt32(list2[3]) : 0
				});
			}
			if (!dictionary3.TryGetValue("t", out var value3) || !(value3 is List<object> list3))
			{
				continue;
			}
			foreach (object item3 in list3)
			{
				int num = (int)Art.Long(item3);
				if (num > 0)
				{
					if (!bySkill.TryGetValue(num, out var value4))
					{
						value4 = (bySkill[num] = new List<Spec>());
					}
					value4.Add(spec);
				}
			}
		}
		int num2 = 0;
		foreach (List<Spec> value5 in bySkill.Values)
		{
			num2 += value5.Count;
		}
		if (bySkill.Count == 0)
		{
			Debug.LogWarning((object)"[SkillEnhance] 表里没有被强化技能：强化核心加成不会生效");
		}
		else if (PlayerPrefs.GetInt("debug_hitlog", 0) != 0)
		{
			Debug.Log((object)("[SkillEnhance] 载入 " + dictionary2.Count + " 个核心 / " + bySkill.Count + " 条被强化技能 / " + num2 + " 条连线"));
		}
	}

	private static int LevelOf(int wrapper)
	{
		if (VMatrixLevels.TryGet(wrapper, out var level) && level > 0)
		{
			return level;
		}
		if (Profile.Data == null)
		{
			return 0;
		}
		return Profile.Data.SkillLevelOf(wrapper);
	}

	public static void Bonus(int skillId, out int finalPct, out int targetPlus, out int ignorePdp, out int bossPct)
	{
		finalPct = 0;
		targetPlus = 0;
		ignorePdp = 0;
		bossPct = 0;
		if (skillId <= 0)
		{
			return;
		}
		Ensure();
		if (!bySkill.TryGetValue(skillId, out var value))
		{
			return;
		}
		foreach (Spec item in value)
		{
			int num = LevelOf(item.Wrapper);
			if (num > 0)
			{
				int num2 = Mathf.Clamp(num, 1, Mathf.Min(item.MaxLevel, item.Levels.Count)) - 1;
				if (num2 >= 0 && num2 < item.Levels.Count)
				{
					int[] array = item.Levels[num2];
					finalPct += array[0];
					targetPlus += array[1];
					ignorePdp += array[2];
					bossPct += array[3];
				}
			}
		}
	}

	public static int FinalPct(int skillId)
	{
		Bonus(skillId, out var finalPct, out var _, out var _, out var _);
		return finalPct;
	}

	public static int TargetPlus(int skillId)
	{
		Bonus(skillId, out var _, out var targetPlus, out var _, out var _);
		return targetPlus;
	}

	public static int IgnorePdp(int skillId)
	{
		Bonus(skillId, out var _, out var _, out var ignorePdp, out var _);
		return ignorePdp;
	}

	public static int BossPct(int skillId)
	{
		Bonus(skillId, out var _, out var _, out var _, out var bossPct);
		return bossPct;
	}

	public static int WrapperLevel(int wrapper)
	{
		return LevelOf(wrapper);
	}

	public static List<int> TargetsOf(int wrapper)
	{
		Ensure();
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, List<Spec>> item in bySkill)
		{
			foreach (Spec item2 in item.Value)
			{
				if (item2.Wrapper == wrapper && !list.Contains(item.Key))
				{
					list.Add(item.Key);
				}
			}
		}
		return list;
	}
}
