using System.Collections.Generic;
using UnityEngine;

public static class JobDefs
{
	private static readonly Dictionary<int, JobDef> byId = new Dictionary<int, JobDef>();

	private static readonly Dictionary<string, int> byCls = new Dictionary<string, int>();

	private static readonly Dictionary<int, string> names = new Dictionary<int, string>();

	private static readonly Dictionary<string, int> LegacyCls = new Dictionary<string, int>
	{
		{ "beginner", 0 },
		{ "knight", 100 },
		{ "warrior", 110 },
		{ "mage", 200 },
		{ "archer", 300 },
		{ "priest", 230 }
	};

	private static readonly Dictionary<int, string> byClsRev = new Dictionary<int, string>();

	private static readonly List<int> Empty = new List<int>();

	public static bool Ready { get; private set; }

	public static int Count
	{
		get
		{
			Ensure();
			return byId.Count;
		}
	}

	public static IEnumerable<JobDef> All
	{
		get
		{
			Ensure();
			return byId.Values;
		}
	}

	public static void Ensure()
	{
		if (Ready)
		{
			return;
		}
		Ready = true;
		TextAsset val = Resources.Load<TextAsset>("UI/JobTree");
		if ((Object)(object)val == (Object)null)
		{
			Debug.LogError((object)"[JobDefs] 缺少 UI/JobTree.json");
			return;
		}
		if (!(MiniJson.Parse(val.text) is Dictionary<string, object> dictionary))
		{
			Debug.LogError((object)"[JobDefs] JobTree.json 解析失败");
			return;
		}
		if (dictionary.TryGetValue("jobNames", out var value) && value is Dictionary<string, object> dictionary2)
		{
			foreach (KeyValuePair<string, object> item in dictionary2)
			{
				if (int.TryParse(item.Key, out var result) && item.Value is string value2)
				{
					names[result] = value2;
				}
			}
		}
		if (dictionary.TryGetValue("jobs", out var value3) && value3 is List<object> list)
		{
			foreach (object item2 in list)
			{
				if (item2 is Dictionary<string, object> dictionary3)
				{
					JobDef jobDef = new JobDef
					{
						id = Art.Int(dictionary3, "id"),
						name = (dictionary3.TryGetValue("name", out var value4) ? (value4 as string) : ""),
						book = (dictionary3.TryGetValue("book", out var value5) ? (value5 as string) : ""),
						family = (dictionary3.TryGetValue("family", out var value6) ? (value6 as string) : ""),
						tier = Art.Int(dictionary3, "tier"),
						parent = Art.Int(dictionary3, "parent", -1)
					};
					jobDef.next = IntList(dictionary3, "next");
					jobDef.skills = IntList(dictionary3, "skills");
					jobDef.allSkills = IntList(dictionary3, "allSkills");
					if (dictionary3.TryGetValue("advance", out var value7) && value7 is Dictionary<string, object> d)
					{
						jobDef.advance = ParseAdvance(d);
					}
					byId[jobDef.id] = jobDef;
				}
			}
		}
		foreach (KeyValuePair<string, int> legacyCl in LegacyCls)
		{
			byCls[legacyCl.Key] = legacyCl.Value;
		}
		byClsRev[0] = "beginner";
		byClsRev[100] = "knight";
		byClsRev[110] = "warrior";
		byClsRev[120] = "warrior";
		byClsRev[130] = "warrior";
		byClsRev[200] = "mage";
		byClsRev[210] = "mage";
		byClsRev[220] = "mage";
		byClsRev[230] = "mage";
		byClsRev[300] = "archer";
		byClsRev[310] = "archer";
		byClsRev[320] = "archer";
		byClsRev[400] = "archer";
		byClsRev[410] = "archer";
		byClsRev[420] = "archer";
		byClsRev[500] = "warrior";
		byClsRev[510] = "warrior";
		byClsRev[520] = "warrior";
	}

	private static JobAdvance ParseAdvance(Dictionary<string, object> d)
	{
		JobAdvance jobAdvance = new JobAdvance
		{
			level = Art.Int(d, "level"),
			quest = Art.Int(d, "quest"),
			requireItem = Art.Int(d, "requireItem"),
			consumeItem = Art.Int(d, "consumeItem"),
			resetStats = (Art.Int(d, "resetStats") != 0),
			sp = Art.Int(d, "sp"),
			ap = Art.Int(d, "ap"),
			derived = (Art.Int(d, "derived") != 0)
		};
		jobAdvance.grant = IntList(d, "grant");
		jobAdvance.teach = IntList(d, "teach");
		if (d.TryGetValue("stat", out var value) && value is Dictionary<string, object> dictionary)
		{
			foreach (KeyValuePair<string, object> item in dictionary)
			{
				if (int.TryParse(item.Key, out var _) && item.Value != null)
				{
					jobAdvance.stat.Add(new KeyValuePair<string, int>(item.Key, Art.Int(dictionary, item.Key)));
				}
			}
		}
		return jobAdvance;
	}

	private static List<int> IntList(Dictionary<string, object> d, string key)
	{
		List<int> list = new List<int>();
		if (d != null && d.TryGetValue(key, out var value) && value is List<object> list2)
		{
			foreach (object item in list2)
			{
				if (item != null && int.TryParse(item.ToString(), out var result))
				{
					list.Add(result);
				}
			}
		}
		return list;
	}

	public static JobDef Get(int jobId)
	{
		Ensure();
		if (!byId.TryGetValue(jobId, out var value))
		{
			return null;
		}
		return value;
	}

	public static bool IsValid(int jobId)
	{
		Ensure();
		return byId.ContainsKey(jobId);
	}

	public static string NameOf(int jobId)
	{
		Ensure();
		if (byId.TryGetValue(jobId, out var value) && !string.IsNullOrEmpty(value.name))
		{
			return value.name;
		}
		if (!names.TryGetValue(jobId, out var value2))
		{
			return jobId.ToString();
		}
		return value2;
	}

	public static int TierOf(int jobId)
	{
		Ensure();
		if (!byId.TryGetValue(jobId, out var value))
		{
			return 0;
		}
		return value.tier;
	}

	public static int SkillBookOf(int jobId)
	{
		return JobBook.Of(jobId);
	}

	public static string FamilyOf(int jobId)
	{
		Ensure();
		if (!byId.TryGetValue(jobId, out var value))
		{
			return "beginner";
		}
		return value.family;
	}

	public static int ParentOf(int jobId)
	{
		Ensure();
		if (!byId.TryGetValue(jobId, out var value))
		{
			return -1;
		}
		return value.parent;
	}

	public static List<int> NextOf(int jobId)
	{
		Ensure();
		if (!byId.TryGetValue(jobId, out var value))
		{
			return Empty;
		}
		return value.next;
	}

	public static int AdvancedOf(int jobId)
	{
		Ensure();
		foreach (int item in NextOf(jobId))
		{
			if (byId.TryGetValue(item, out var value) && value.IsAdvanced)
			{
				return item;
			}
		}
		return 0;
	}

	public static int BaseOf(int jobId)
	{
		Ensure();
		if (!byId.TryGetValue(jobId, out var value) || !value.IsAdvanced)
		{
			return jobId;
		}
		return value.parent;
	}

	public static int JobBit(int jobId)
	{
		if (jobId <= 0)
		{
			return 0;
		}
		int num = jobId / 100 % 10;
		if (num < 1 || num > 5)
		{
			return 0;
		}
		return 1 << num - 1;
	}

	public static bool IsNext(int from, int to)
	{
		Ensure();
		if (byId.TryGetValue(from, out var value))
		{
			return value.next.Contains(to);
		}
		return false;
	}

	public static int JobIdOf(string cls)
	{
		Ensure();
		if (string.IsNullOrEmpty(cls))
		{
			return 0;
		}
		if (!byCls.TryGetValue(cls, out var value))
		{
			return 0;
		}
		return value;
	}

	public static string ClsOf(int jobId)
	{
		Ensure();
		if (byClsRev.TryGetValue(jobId, out var value))
		{
			return value;
		}
		if (!byId.ContainsKey(jobId))
		{
			return "beginner";
		}
		int key = jobId;
		for (int i = 0; i < 8; i++)
		{
			if (!byId.TryGetValue(key, out var value2))
			{
				break;
			}
			if (value2.parent < 0)
			{
				break;
			}
			key = value2.parent;
			if (byClsRev.TryGetValue(key, out var value3))
			{
				byClsRev[jobId] = value3;
				return value3;
			}
		}
		string text;
		switch (FamilyOf(jobId))
		{
		case "warrior":
		case "cygnus-dawnwarrior":
		case "cygnus-thunderbreaker":
		case "pirate":
			text = "warrior";
			break;
		case "magician":
		case "cygnus-blazewizard":
			text = "mage";
			break;
		case "bowman":
		case "thief":
		case "cygnus-windarcher":
		case "cygnus-nightwalker":
			text = "archer";
			break;
		default:
			text = "beginner";
			break;
		}
		byClsRev[jobId] = text;
		return text;
	}
}
