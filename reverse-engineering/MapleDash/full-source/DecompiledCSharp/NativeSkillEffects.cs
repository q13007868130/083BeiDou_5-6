using System;
using System.Collections.Generic;
using UnityEngine;

public static class NativeSkillEffects
{
	private static Dictionary<string, object> table;

	private static Dictionary<string, object> Table
	{
		get
		{
			if (table != null)
			{
				return table;
			}
			TextAsset val = Resources.Load<TextAsset>("NativeSkillEffects");
			string text = ((val != null) ? val.text : null);
			table = (string.IsNullOrEmpty(text) ? new Dictionary<string, object>() : ((Dictionary<string, object>)MiniJson.Parse(text)));
			return table;
		}
	}

	private static Dictionary<string, object> Fields => Section("fields");

	private static Dictionary<string, object> Passives => Section("passive");

	private static Dictionary<string, object> PassiveFieldMap => Section("passiveFields");

	public static bool Ready => Fields != null;

	private static Dictionary<string, object> Aliases => Section("alias");

	private static Dictionary<string, object> Section(string name)
	{
		if (!Table.TryGetValue(name, out var value))
		{
			return null;
		}
		return value as Dictionary<string, object>;
	}

	private static HashSet<string> PassiveFieldNames(int id)
	{
		HashSet<string> hashSet = new HashSet<string>();
		Dictionary<string, object> passiveFieldMap = PassiveFieldMap;
		if (passiveFieldMap == null || !passiveFieldMap.TryGetValue(id.ToString(), out var value))
		{
			return hashSet;
		}
		foreach (object item in (List<object>)value)
		{
			hashSet.Add((string)item);
		}
		return hashSet;
	}

	public static void AddPassive(Dictionary<string, int> set)
	{
		Dictionary<string, object> passives = Passives;
		if (passives == null || set == null)
		{
			return;
		}
		foreach (KeyValuePair<string, object> item in passives)
		{
			if (!int.TryParse(item.Key, out var result))
			{
				continue;
			}
			int num = SkillProgress.LevelOf(result);
			if (num <= 0)
			{
				continue;
			}
			foreach (KeyValuePair<string, object> item2 in (Dictionary<string, object>)item.Value)
			{
				if (item2.Value is List<object> { Count: not 0 } list)
				{
					int index = Mathf.Clamp(num - 1, 0, list.Count - 1);
					int num2 = Convert.ToInt32(list[index]);
					if (num2 != 0)
					{
						string key = item2.Key;
						set[key] = (set.TryGetValue(key, out var value) ? value : 0) + num2;
					}
				}
			}
		}
	}

	public static void AddBuff(Dictionary<string, int> stats, int id, Dictionary<string, object> row)
	{
		if (row == null || stats == null)
		{
			return;
		}
		HashSet<string> hashSet = PassiveFieldNames(id);
		Dictionary<string, object> aliases = Aliases;
		if (aliases != null && aliases.TryGetValue(id.ToString(), out var value))
		{
			foreach (KeyValuePair<string, object> item in (Dictionary<string, object>)value)
			{
				if (!hashSet.Contains(item.Key))
				{
					Add(stats, Art.Int(row, item.Key), item.Value);
				}
			}
		}
		Dictionary<string, object> fields = Fields;
		if (fields == null)
		{
			return;
		}
		foreach (KeyValuePair<string, object> item2 in fields)
		{
			string key = item2.Key;
			if (!hashSet.Contains(key))
			{
				Add(stats, Art.Int(row, key), item2.Value);
			}
		}
	}

	private static void Add(Dictionary<string, int> stats, int value, object keys)
	{
		if (value == 0)
		{
			return;
		}
		foreach (string item in (List<object>)keys)
		{
			stats[item] = (stats.TryGetValue(item, out var value2) ? value2 : 0) + value;
		}
	}
}
