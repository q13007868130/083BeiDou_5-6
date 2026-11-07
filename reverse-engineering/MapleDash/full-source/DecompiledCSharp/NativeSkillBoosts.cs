using System;
using System.Collections.Generic;
using UnityEngine;

public static class NativeSkillBoosts
{
	private sealed class Entry
	{
		public int Source;

		public int[] Pct;
	}

	private static Dictionary<int, List<Entry>> byTarget;

	private static bool tried;

	private static void Ensure()
	{
		if (tried)
		{
			return;
		}
		tried = true;
		byTarget = new Dictionary<int, List<Entry>>();
		TextAsset val = Resources.Load<TextAsset>("NativeSkillBoosts");
		if ((Object)(object)val == (Object)null || !(MiniJson.Parse(val.text) is Dictionary<string, object> dictionary) || !dictionary.TryGetValue("boosts", out var value) || !(value is Dictionary<string, object> dictionary2))
		{
			return;
		}
		foreach (KeyValuePair<string, object> item in dictionary2)
		{
			if (!(item.Value is List<object> list))
			{
				continue;
			}
			foreach (object item2 in list)
			{
				if (!(item2 is Dictionary<string, object> dictionary3))
				{
					continue;
				}
				int num = (int)Art.Num(dictionary3, "target");
				if (num <= 0 || !dictionary3.TryGetValue("pct", out var value2) || !(value2 is List<object> list2))
				{
					continue;
				}
				int[] array = new int[list2.Count];
				for (int i = 0; i < list2.Count; i++)
				{
					array[i] = Convert.ToInt32(list2[i]);
				}
				if (array.Length != 0)
				{
					if (!byTarget.TryGetValue(num, out var value3))
					{
						value3 = (byTarget[num] = new List<Entry>());
					}
					value3.Add(new Entry
					{
						Source = int.Parse(item.Key),
						Pct = array
					});
				}
			}
		}
	}

	public static int Pct(int targetId)
	{
		Ensure();
		if (byTarget == null || !byTarget.TryGetValue(targetId, out var value))
		{
			return 0;
		}
		int num = 0;
		foreach (Entry item in value)
		{
			int num2 = SkillProgress.LevelOf(item.Source);
			if (num2 > 0)
			{
				num += item.Pct[Mathf.Clamp(num2 - 1, 0, item.Pct.Length - 1)];
			}
		}
		return num;
	}
}
