using System;
using System.Collections.Generic;
using UnityEngine;

public static class NativeSkillOverrides
{
	private sealed class Rule
	{
		public int Source;

		public int ScaleTarget;

		public int DurationTarget;

		public string ScaleField;

		public string DurationField;
	}

	private static Dictionary<string, object> table;

	private static List<Rule> rules;

	private static List<Rule> Rules
	{
		get
		{
			if (rules != null)
			{
				return rules;
			}
			rules = new List<Rule>();
			if (table == null)
			{
				TextAsset val = Resources.Load<TextAsset>("NativeSkillOverrides");
				string text = ((val != null) ? val.text : null);
				table = (string.IsNullOrEmpty(text) ? new Dictionary<string, object>() : ((Dictionary<string, object>)MiniJson.Parse(text)));
			}
			if (!table.TryGetValue("overrides", out var value) || !(value is Dictionary<string, object> dictionary))
			{
				return rules;
			}
			foreach (KeyValuePair<string, object> item in dictionary)
			{
				if (item.Value is Dictionary<string, object> row && int.TryParse(item.Key, out var result))
				{
					rules.Add(new Rule
					{
						Source = result,
						ScaleTarget = Int(row, "scaleTarget"),
						ScaleField = Str(row, "scaleField"),
						DurationTarget = Int(row, "durationTarget"),
						DurationField = Str(row, "durationField")
					});
				}
			}
			return rules;
		}
	}

	private static int Int(Dictionary<string, object> row, string key, int fallback = 0)
	{
		if (row == null || !row.TryGetValue(key, out var value))
		{
			return fallback;
		}
		return Convert.ToInt32(value);
	}

	private static string Str(Dictionary<string, object> row, string key)
	{
		if (row == null || !row.TryGetValue(key, out var value))
		{
			return "";
		}
		return Convert.ToString(value);
	}

	public static bool IsDurationTarget(int id)
	{
		foreach (Rule rule in Rules)
		{
			if (rule.DurationTarget == id)
			{
				return true;
			}
		}
		return false;
	}

	private static int Value(Rule rule, string field, int fallback)
	{
		int num = SkillProgress.LevelOf(rule.Source);
		if (num <= 0 || string.IsNullOrEmpty(field))
		{
			return fallback;
		}
		int lv = Mathf.Clamp(num, 1, Mathf.Max(1, SkillBook.MaxLevelOf(rule.Source)));
		int num2 = Art.Int(SkillBook.Get(rule.Source)?.Level(lv), field);
		if (num2 <= 0)
		{
			return fallback;
		}
		return num2;
	}

	public static int Scale(int targetId, int fallback)
	{
		foreach (Rule rule in Rules)
		{
			if (rule.ScaleTarget == targetId)
			{
				return Value(rule, rule.ScaleField, fallback);
			}
		}
		return fallback;
	}

	public static int DurationSeconds(int targetId, int fallback)
	{
		foreach (Rule rule in Rules)
		{
			if (rule.DurationTarget == targetId)
			{
				return Value(rule, rule.DurationField, fallback);
			}
		}
		return fallback;
	}
}
