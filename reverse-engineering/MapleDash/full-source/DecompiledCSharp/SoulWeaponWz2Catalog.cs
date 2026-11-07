using System;
using System.Collections.Generic;

public static class SoulWeaponWz2Catalog
{
	private static Dictionary<string, object> data;

	public static Dictionary<string, object> Data
	{
		get
		{
			object obj = data;
			if (obj == null)
			{
				obj = (MiniJsonDb.Parse(PlatformRes.LoadText("Enhancement/SoulWeaponWz2")) as Dictionary<string, object>) ?? throw new InvalidOperationException("缺少282灵魂武器数据");
				data = (Dictionary<string, object>)obj;
			}
			return (Dictionary<string, object>)obj;
		}
	}

	public static Dictionary<string, object> Rules => Map(Data, "rules");

	public static Dictionary<string, object> Bosses => Map(Rules, "BossInfo");

	public static Dictionary<string, object> Map(Dictionary<string, object> d, string key)
	{
		if (d == null || !d.TryGetValue(key, out var value))
		{
			return null;
		}
		return value as Dictionary<string, object>;
	}

	public static int Int(Dictionary<string, object> d, string key, int fallback = 0)
	{
		if (d == null || !d.TryGetValue(key, out var value) || !int.TryParse(value?.ToString(), out var result))
		{
			return fallback;
		}
		return result;
	}

	public static string Text(Dictionary<string, object> d, string key, string fallback = "")
	{
		if (d == null || !d.TryGetValue(key, out var value))
		{
			return fallback;
		}
		return value?.ToString() ?? fallback;
	}

	public static Dictionary<string, object> Grade(int grade)
	{
		return Map(Map(Rules, "GradeInfo"), grade.ToString());
	}

	public static int RequiredExp(int nextLevel)
	{
		return Int(Map(Map(Rules, "LevelInfo"), nextLevel.ToString()), "ReqExp");
	}

	public static int MaxLevel(int grade)
	{
		return Int(Grade(grade), "MaxLevel");
	}

	public static int PassiveAttack(int level)
	{
		switch (level)
		{
		case 61:
		case 62:
		case 63:
		case 64:
		case 65:
		case 66:
		case 67:
		case 68:
		case 69:
		case 70:
		case 71:
		case 72:
		case 73:
		case 74:
		case 75:
		case 76:
		case 77:
		case 78:
		case 79:
		case 80:
		case 81:
		case 82:
		case 83:
		case 84:
		case 85:
		case 86:
		case 87:
		case 88:
		case 89:
		case 90:
		case 91:
		case 92:
		case 93:
		case 94:
		case 95:
		case 96:
		case 97:
		case 98:
		case 99:
		case 100:
			return level * 2 - 40;
		case 1:
		case 2:
		case 3:
		case 4:
		case 5:
		case 6:
		case 7:
		case 8:
		case 9:
		case 10:
		case 11:
		case 12:
		case 13:
		case 14:
		case 15:
		case 16:
		case 17:
		case 18:
		case 19:
		case 20:
		case 21:
		case 22:
		case 23:
		case 24:
		case 25:
		case 26:
		case 27:
		case 28:
		case 29:
		case 30:
		case 31:
		case 32:
		case 33:
		case 34:
		case 35:
		case 36:
		case 37:
		case 38:
		case 39:
		case 40:
		case 41:
		case 42:
		case 43:
		case 44:
		case 45:
		case 46:
		case 47:
		case 48:
		case 49:
		case 50:
		case 51:
		case 52:
		case 53:
		case 54:
		case 55:
		case 56:
		case 57:
		case 58:
		case 59:
		case 60:
			return level + 20;
		default:
			return 0;
		}
	}

	public static Dictionary<string, object> Option(int id)
	{
		return Map(Map(Data, "options"), id.ToString("D6"));
	}

	public static Dictionary<string, object> OptionStats(int id)
	{
		return Map(Map(Option(id), "level"), "1");
	}

	public static string DescribeOption(int id)
	{
		Dictionary<string, object> dictionary = OptionStats(id);
		string text = Text(Map(Option(id), "info"), "string", (id == 0) ? "尚未设置灵魂烙印" : "未知灵魂烙印");
		if (dictionary == null)
		{
			return text;
		}
		List<string> list = new List<string>(dictionary.Keys);
		list.Sort((string a, string b) => b.Length.CompareTo(a.Length));
		foreach (string item in list)
		{
			text = text.Replace("#" + item, dictionary[item].ToString());
		}
		return text;
	}

	public static List<int> RollOptions(int grade)
	{
		List<int> list = new List<int>();
		foreach (KeyValuePair<string, object> item in Map(Data, "options"))
		{
			if (int.TryParse(item.Key, out var result) && result / 10000 == grade && result % 10000 < 1000)
			{
				list.Add(result);
			}
		}
		list.Sort();
		return list;
	}

	public static Dictionary<string, object> Skill(int grade)
	{
		return Map(Map(Data, "skills"), Int(Grade(grade), "ActiveSkillID").ToString());
	}

	public static int ActiveAttackPercent(int grade)
	{
		Dictionary<string, object> d = Grade(grade);
		return GrowthFormula.Evaluate(Text(Map(Skill(grade), "common"), "indiePadR", "0"), Int(d, "ActiveSkillLevel"));
	}

	public static string SkillName(int grade)
	{
		return Text(Map(Skill(grade), "string"), "name", "灵魂的斗志");
	}

	public static string BossName(Dictionary<string, object> boss)
	{
		return Text(Map(Map(Map(Data, "quests"), Int(boss, "QuestID_Weekly").ToString()), "QuestInfo"), "name").Replace("[靈魂武器] ", "").Replace("1人擊破", "");
	}
}
