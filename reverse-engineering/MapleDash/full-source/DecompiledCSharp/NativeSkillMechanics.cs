using System;
using System.Collections.Generic;
using UnityEngine;

public static class NativeSkillMechanics
{
	private static Dictionary<string, object> table;

	private static Dictionary<string, object> Table
	{
		get
		{
			if (table == null)
			{
				TextAsset val = Resources.Load<TextAsset>("NativeSkillMechanics");
				string text = ((val != null) ? val.text : null);
				table = (string.IsNullOrEmpty(text) ? new Dictionary<string, object>() : ((Dictionary<string, object>)MiniJson.Parse(text)));
			}
			return table;
		}
	}

	private static Dictionary<string, object> Mechanics
	{
		get
		{
			if (!Table.TryGetValue("mechanics", out var value))
			{
				return null;
			}
			return value as Dictionary<string, object>;
		}
	}

	public static Dictionary<string, object> Spec(int id)
	{
		Dictionary<string, object> mechanics = Mechanics;
		if (mechanics == null || !mechanics.TryGetValue(id.ToString(), out var value))
		{
			return null;
		}
		return value as Dictionary<string, object>;
	}

	public static int Int(int id, string key, int fallback = 0)
	{
		Dictionary<string, object> dictionary = Spec(id);
		if (dictionary == null || !dictionary.TryGetValue(key, out var value))
		{
			return fallback;
		}
		return Convert.ToInt32(value);
	}

	public static string Str(int id, string key)
	{
		Dictionary<string, object> dictionary = Spec(id);
		if (dictionary == null || !dictionary.TryGetValue(key, out var value))
		{
			return "";
		}
		return Convert.ToString(value);
	}

	public static int[] Ints(int id, string key)
	{
		Dictionary<string, object> dictionary = Spec(id);
		if (dictionary == null || !dictionary.TryGetValue(key, out var value) || !(value is List<object> list))
		{
			return new int[0];
		}
		int[] array = new int[list.Count];
		for (int i = 0; i < list.Count; i++)
		{
			array[i] = Convert.ToInt32(list[i]);
		}
		return array;
	}

	public static List<Dictionary<string, object>> Variants(int id)
	{
		List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
		Dictionary<string, object> dictionary = Spec(id);
		if (dictionary == null || !dictionary.TryGetValue("variants", out var value) || !(value is List<object> list2))
		{
			return list;
		}
		foreach (object item2 in list2)
		{
			if (item2 is Dictionary<string, object> item)
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static bool InList(int id, string key, int value)
	{
		int[] array = Ints(id, key);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == value)
			{
				return true;
			}
		}
		return false;
	}

	public static bool InList(Dictionary<string, object> row, string key, int value)
	{
		if (row == null || !row.TryGetValue(key, out var value2) || !(value2 is List<object> list))
		{
			return false;
		}
		foreach (object item in list)
		{
			if (Convert.ToInt32(item) == value)
			{
				return true;
			}
		}
		return false;
	}

	public static int[] AllIds()
	{
		Dictionary<string, object> mechanics = Mechanics;
		if (mechanics == null)
		{
			return new int[0];
		}
		List<int> list = new List<int>();
		foreach (string key in mechanics.Keys)
		{
			if (int.TryParse(key, out var result))
			{
				list.Add(result);
			}
		}
		return list.ToArray();
	}

	public static void AddTimedEffect(Dictionary<string, int> stats, int stateId)
	{
		if (stats == null)
		{
			return;
		}
		Dictionary<string, object> dictionary = NativeSkillStates.Spec(stateId);
		if (dictionary == null || !dictionary.TryGetValue("effectKey", out var value))
		{
			return;
		}
		int num = NativeSkillStates.Int(stateId, "effectPct");
		int num2 = NativeSkillStates.Int(stateId, "elementAbsorbCap");
		if (num > 0 && num2 > 0)
		{
			string text = Convert.ToString(value);
			if (!string.IsNullOrEmpty(text))
			{
				stats.TryGetValue(text, out var value2);
				stats[text] = Mathf.Max(value2, num);
			}
		}
	}
}
