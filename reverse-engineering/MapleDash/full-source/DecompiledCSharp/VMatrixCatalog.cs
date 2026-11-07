using System;
using System.Collections.Generic;
using System.Globalization;

public static class VMatrixCatalog
{
	public const int MaxPoints = 9999;

	public const int UnlockLevel = 200;

	public const int FifthJobQuest = 1465;

	private static Dictionary<int, VMatrixCore> cores;

	private static Dictionary<int, VMatrixGroup> groups;

	private static Dictionary<int, VMatrixGemstone> gems;

	private static SortedDictionary<int, int> skillCosts;

	private static SortedDictionary<int, int> boostCosts;

	public static int ResetMeso;

	public static int RegisterSpecialCost;

	public static int ExtendSpecialCost;

	public static int TransformCost;

	public static int TransformItem;

	public static int TransformMinutes;

	public static bool Loaded => cores != null;

	public static IEnumerable<VMatrixCore> AllCores
	{
		get
		{
			Ensure();
			return cores.Values;
		}
	}

	public static int CoreCount
	{
		get
		{
			Ensure();
			return cores.Count;
		}
	}

	public static void Invalidate()
	{
		cores = null;
		groups = null;
		gems = null;
		skillCosts = null;
		boostCosts = null;
	}

	public static void Ensure()
	{
		if (cores == null)
		{
			Load(PlatformRes.LoadText("UI/VMatrix"));
		}
	}

	public static void Load(string json)
	{
		cores = new Dictionary<int, VMatrixCore>();
		groups = new Dictionary<int, VMatrixGroup>();
		gems = new Dictionary<int, VMatrixGemstone>();
		skillCosts = new SortedDictionary<int, int>();
		boostCosts = new SortedDictionary<int, int>();
		if (string.IsNullOrEmpty(json) || !(MiniJsonDb.Parse(json) is Dictionary<string, object> dictionary))
		{
			return;
		}
		Dictionary<string, object> node = Obj(dictionary, "info");
		ResetMeso = I(node, "resetMeso");
		RegisterSpecialCost = I(node, "registerSpecialCost");
		ExtendSpecialCost = I(node, "extendSpecialCost");
		TransformCost = I(node, "transformCost");
		TransformItem = I(node, "transformItem");
		TransformMinutes = I(node, "transformMinutes");
		Dictionary<string, object> node2 = Obj(dictionary, "costs");
		ReadCosts(Obj(node2, "skill"), skillCosts);
		ReadCosts(Obj(node2, "enforce"), boostCosts);
		Dictionary<string, object> dictionary2 = Obj(dictionary, "cores");
		if (dictionary2 != null)
		{
			foreach (KeyValuePair<string, object> item in dictionary2)
			{
				if (item.Value is Dictionary<string, object> dictionary3)
				{
					int num = I(dictionary3, "id", ParseInt(item.Key));
					cores[num] = new VMatrixCore
					{
						id = num,
						type = I(dictionary3, "type", -1),
						maxLevel = Math.Max(1, I(dictionary3, "maxLevel", 1)),
						expireDays = I(dictionary3, "expireDays"),
						selfOpen = B(dictionary3, "selfOpen"),
						name = S(dictionary3, "name"),
						desc = S(dictionary3, "desc"),
						skills = Ints(dictionary3, "skills"),
						subSkills = Ints(dictionary3, "subSkills"),
						cooldownSkills = Ints(dictionary3, "cooldownSkills"),
						removalSkills = Ints(dictionary3, "removalSkills"),
						equipRemovalSkills = Ints(dictionary3, "equipRemovalSkills"),
						jobs = Strings(dictionary3, "jobs"),
						special = ((dictionary3.ContainsKey("special") && dictionary3["special"] != null) ? NodeText(dictionary3["special"]) : "")
					};
				}
			}
		}
		Dictionary<string, object> dictionary4 = Obj(dictionary, "groups");
		if (dictionary4 != null)
		{
			foreach (KeyValuePair<string, object> item2 in dictionary4)
			{
				if (item2.Value is Dictionary<string, object> node3)
				{
					groups[ParseInt(item2.Key)] = new VMatrixGroup
					{
						skill = Ints(node3, "skill"),
						enforce = Ints(node3, "enforce"),
						common = Ints(node3, "common")
					};
				}
			}
		}
		if (!dictionary.TryGetValue("gemstones", out var value) || !(value is List<object> list))
		{
			return;
		}
		foreach (object item3 in list)
		{
			if (item3 is Dictionary<string, object> node4)
			{
				VMatrixGemstone vMatrixGemstone = new VMatrixGemstone
				{
					itemIds = Ints(node4, "itemIds"),
					points = I(node4, "points"),
					openCore = I(node4, "openCore"),
					reqQuest = I(node4, "reqQuest"),
					animation = I(node4, "animation"),
					questAlert = S(node4, "questAlert")
				};
				int[] itemIds = vMatrixGemstone.itemIds;
				foreach (int key in itemIds)
				{
					gems[key] = vMatrixGemstone;
				}
			}
		}
	}

	private static void ReadCosts(Dictionary<string, object> node, SortedDictionary<int, int> target)
	{
		if (node == null)
		{
			return;
		}
		foreach (KeyValuePair<string, object> item in node)
		{
			if (int.TryParse(item.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
			{
				target[result] = (int)Num(item.Value, 0L);
			}
		}
	}

	public static VMatrixCore Core(int id)
	{
		Ensure();
		if (!cores.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public static VMatrixGroup Group(int job)
	{
		Ensure();
		if (groups.TryGetValue(job, out var value))
		{
			return value;
		}
		if (!groups.TryGetValue(job - 1, out value))
		{
			return null;
		}
		return value;
	}

	public static bool Allowed(int job, int id)
	{
		VMatrixCore vMatrixCore = Core(id);
		if (vMatrixCore == null)
		{
			return false;
		}
		VMatrixGroup vMatrixGroup = Group(job);
		if (vMatrixGroup != null)
		{
			if (vMatrixCore.type != 2)
			{
				return vMatrixGroup.Contains(id);
			}
			return true;
		}
		return false;
	}

	public static int CostForLevel(int id, int level)
	{
		VMatrixCore vMatrixCore = Core(id);
		if (vMatrixCore == null || vMatrixCore.type > 1 || level < 1 || level > vMatrixCore.maxLevel)
		{
			return -1;
		}
		foreach (KeyValuePair<int, int> item in (vMatrixCore.type == 0) ? skillCosts : boostCosts)
		{
			if (item.Key >= level)
			{
				return (item.Value > 0) ? item.Value : (-1);
			}
		}
		return -1;
	}

	public static long CostBetween(int id, int from, int to)
	{
		if (from < 0 || to <= from)
		{
			return -1L;
		}
		long num = 0L;
		for (int i = from + 1; i <= to; i++)
		{
			int num2 = CostForLevel(id, i);
			if (num2 < 0)
			{
				return -1L;
			}
			num += num2;
		}
		return num;
	}

	public static int ResetFloor(int job, int id)
	{
		VMatrixCore vMatrixCore = Core(id);
		if (vMatrixCore == null)
		{
			return 0;
		}
		VMatrixGroup vMatrixGroup = Group(job);
		if (vMatrixCore.selfOpen && (vMatrixGroup == null || Array.IndexOf(vMatrixGroup.skill, id) < 0))
		{
			return 0;
		}
		return 1;
	}

	public static VMatrixGemstone Gemstone(int itemId)
	{
		Ensure();
		if (!gems.TryGetValue(itemId, out var value))
		{
			return null;
		}
		return value;
	}

	internal static Dictionary<string, object> Obj(Dictionary<string, object> node, string key)
	{
		if (node == null || !node.TryGetValue(key, out var value))
		{
			return null;
		}
		return value as Dictionary<string, object>;
	}

	internal static int I(Dictionary<string, object> node, string key, int def = 0)
	{
		if (node == null || !node.TryGetValue(key, out var value))
		{
			return def;
		}
		return (int)Num(value, def);
	}

	internal static bool B(Dictionary<string, object> node, string key, bool def = false)
	{
		if (node != null && node.TryGetValue(key, out var value) && value is bool)
		{
			return (bool)value;
		}
		return def;
	}

	internal static string S(Dictionary<string, object> node, string key)
	{
		if (node == null || !node.TryGetValue(key, out var value) || !(value is string result))
		{
			return "";
		}
		return result;
	}

	internal static int[] Ints(Dictionary<string, object> node, string key)
	{
		if (node == null || !node.TryGetValue(key, out var value) || !(value is List<object> list))
		{
			return new int[0];
		}
		int[] array = new int[list.Count];
		for (int i = 0; i < list.Count; i++)
		{
			array[i] = (int)Num(list[i], 0L);
		}
		return array;
	}

	internal static string[] Strings(Dictionary<string, object> node, string key)
	{
		if (node == null || !node.TryGetValue(key, out var value) || !(value is List<object> list))
		{
			return new string[0];
		}
		string[] array = new string[list.Count];
		for (int i = 0; i < list.Count; i++)
		{
			array[i] = (list[i] as string) ?? "";
		}
		return array;
	}

	internal static long Num(object value, long def = 0L)
	{
		if (value is long)
		{
			return (long)value;
		}
		if (value is double num)
		{
			return (long)num;
		}
		if (value is int num2)
		{
			return num2;
		}
		return def;
	}

	internal static int ParseInt(string text)
	{
		if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return 0;
		}
		return result;
	}

	internal static string NodeText(object node)
	{
		if (node is Dictionary<string, object> dictionary)
		{
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, object> item in dictionary)
			{
				list.Add("\"" + item.Key + "\":" + NodeText(item.Value));
			}
			return "{" + string.Join(",", list) + "}";
		}
		if (node is List<object> list2)
		{
			List<string> list3 = new List<string>();
			foreach (object item2 in list2)
			{
				list3.Add(NodeText(item2));
			}
			return "[" + string.Join(",", list3) + "]";
		}
		if (node is string text)
		{
			return "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
		}
		if (node is bool)
		{
			if (!(bool)node)
			{
				return "false";
			}
			return "true";
		}
		if (node == null)
		{
			return "null";
		}
		return Convert.ToString(node, CultureInfo.InvariantCulture);
	}
}
