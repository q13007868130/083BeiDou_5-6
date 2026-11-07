using System;
using System.Collections.Generic;

public static class HexaCatalog
{
	public sealed class SolErdaTier
	{
		public int exp;

		public int[] itemIds = new int[0];
	}

	public const int UnlockLevel = 260;

	public const int UnlockQuest = 38625;

	public static readonly int[] JobWhitelist = new int[51]
	{
		113, 123, 133, 213, 223, 233, 313, 323, 333, 413,
		423, 435, 513, 523, 533, 1113, 1213, 1313, 1413, 1513,
		2113, 2219, 2313, 2413, 2513, 2713, 3113, 3123, 3213, 3313,
		3513, 3613, 3713, 5113, 6113, 6313, 6413, 6513, 10113, 14213,
		15113, 15213, 15413, 15513, 16113, 16213, 16413, 4113, 4213, 17213,
		17513
	};

	private static Dictionary<int, HexaCore> cores;

	private static Dictionary<int, HexaGroup> groups;

	private static Dictionary<int, HexaStatCore> stats;

	private static List<SolErdaTier> tiers;

	private static Dictionary<string, object> statValues;

	public static int GaugeMax;

	public static int SlotMax;

	public static bool Loaded => cores != null;

	public static List<HexaStatCore> AllStats
	{
		get
		{
			Ensure();
			List<HexaStatCore> list = new List<HexaStatCore>(stats.Values);
			list.Sort((HexaStatCore a, HexaStatCore b) => (a.reqLevel == b.reqLevel) ? a.id.CompareTo(b.id) : a.reqLevel.CompareTo(b.reqLevel));
			return list;
		}
	}

	public static IEnumerable<HexaCore> AllCores
	{
		get
		{
			Ensure();
			return cores.Values;
		}
	}

	public static IReadOnlyList<SolErdaTier> Tiers
	{
		get
		{
			Ensure();
			return tiers;
		}
	}

	public static void Invalidate()
	{
		cores = null;
		groups = null;
		stats = null;
		tiers = null;
	}

	public static void Ensure()
	{
		if (cores == null)
		{
			Load(PlatformRes.LoadText("UI/HexaMatrix"));
		}
	}

	public static void Load(string json)
	{
		cores = new Dictionary<int, HexaCore>();
		groups = new Dictionary<int, HexaGroup>();
		stats = new Dictionary<int, HexaStatCore>();
		tiers = new List<SolErdaTier>();
		if (string.IsNullOrEmpty(json) || !(MiniJsonDb.Parse(json) is Dictionary<string, object> node))
		{
			return;
		}
		statValues = VMatrixCatalog.Obj(node, "statValues");
		Dictionary<string, object> dictionary = VMatrixCatalog.Obj(node, "solErda");
		GaugeMax = VMatrixCatalog.I(dictionary, "gaugeMax", 1000);
		SlotMax = VMatrixCatalog.I(dictionary, "slotMax", 20);
		if (dictionary != null && dictionary.TryGetValue("tiers", out var value) && value is List<object> list)
		{
			foreach (object item in list)
			{
				if (item is Dictionary<string, object> node2)
				{
					tiers.Add(new SolErdaTier
					{
						exp = VMatrixCatalog.I(node2, "exp"),
						itemIds = VMatrixCatalog.Ints(node2, "itemIds")
					});
				}
			}
		}
		Dictionary<string, object> dictionary2 = VMatrixCatalog.Obj(node, "cores");
		if (dictionary2 != null)
		{
			foreach (KeyValuePair<string, object> item2 in dictionary2)
			{
				if (item2.Value is Dictionary<string, object> node3)
				{
					int num = VMatrixCatalog.I(node3, "id", VMatrixCatalog.ParseInt(item2.Key));
					cores[num] = new HexaCore
					{
						id = num,
						type = VMatrixCatalog.I(node3, "type", -1),
						maxLevel = Math.Max(1, VMatrixCatalog.I(node3, "maxLevel", 1)),
						name = VMatrixCatalog.S(node3, "name"),
						desc = VMatrixCatalog.S(node3, "desc"),
						skills = VMatrixCatalog.Ints(node3, "skills"),
						subSkills = VMatrixCatalog.Ints(node3, "subSkills"),
						reqForActivation = VMatrixCatalog.Ints(node3, "reqForActivation"),
						requiredSkills = Requirements(VMatrixCatalog.Obj(node3, "requiredSkills"))
					};
				}
			}
		}
		Dictionary<string, object> dictionary3 = VMatrixCatalog.Obj(node, "groups");
		if (dictionary3 != null)
		{
			foreach (KeyValuePair<string, object> item3 in dictionary3)
			{
				if (item3.Value is Dictionary<string, object> node4)
				{
					groups[VMatrixCatalog.ParseInt(item3.Key)] = new HexaGroup
					{
						mastery = VMatrixCatalog.Ints(node4, "mastery"),
						enforce = VMatrixCatalog.Ints(node4, "enforce"),
						skill = VMatrixCatalog.Ints(node4, "skill"),
						common = VMatrixCatalog.Ints(node4, "common")
					};
				}
			}
		}
		Dictionary<string, object> dictionary4 = VMatrixCatalog.Obj(node, "stats");
		if (dictionary4 == null)
		{
			return;
		}
		foreach (KeyValuePair<string, object> item4 in dictionary4)
		{
			if (item4.Value is Dictionary<string, object> node5)
			{
				int num2 = VMatrixCatalog.I(node5, "id", VMatrixCatalog.ParseInt(item4.Key));
				stats[num2] = new HexaStatCore
				{
					id = num2,
					reqLevel = VMatrixCatalog.I(node5, "reqLevel"),
					maxLevel = Math.Max(1, VMatrixCatalog.I(node5, "maxLevel", 1)),
					reqCore = VMatrixCatalog.I(node5, "reqCore"),
					name = VMatrixCatalog.S(node5, "name"),
					desc = VMatrixCatalog.S(node5, "desc")
				};
			}
		}
	}

	public static HexaCore Core(int id)
	{
		Ensure();
		if (!cores.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	private static Dictionary<int, int> Requirements(Dictionary<string, object> node)
	{
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		if (node != null)
		{
			foreach (KeyValuePair<string, object> item in node)
			{
				if (int.TryParse(item.Key, out var result))
				{
					dictionary[result] = Convert.ToInt32(item.Value);
				}
			}
		}
		return dictionary;
	}

	public static string MainStat(int job)
	{
		if (job / 10 == 312 || job == 3101)
		{
			return "HP";
		}
		switch (job / 100)
		{
		case 2:
		case 12:
		case 22:
		case 27:
		case 32:
		case 42:
		case 112:
		case 142:
		case 152:
		case 162:
		case 172:
		case 182:
			return "INT";
		case 3:
		case 13:
		case 23:
		case 33:
		case 35:
		case 63:
		case 65:
		case 175:
			return "DEX";
		case 4:
		case 14:
		case 24:
		case 64:
		case 154:
		case 164:
			return "LUK";
		case 5:
			if (job / 10 != 52)
			{
				return "STR";
			}
			return "DEX";
		default:
			return "STR";
		}
	}

	public static int MainStatValue(int weight, int job)
	{
		return weight * ((job / 100 == 36) ? 40 : ((MainStat(job) == "HP") ? 630 : 30));
	}

	public static int StatValue(int type, int level, bool main)
	{
		Ensure();
		int[] array = VMatrixCatalog.Ints(VMatrixCatalog.Obj(statValues, main ? "main" : "additional"), type.ToString());
		if (level < 0 || level >= array.Length)
		{
			return 0;
		}
		return array[level];
	}

	public static HexaStatCore Stat(int id)
	{
		Ensure();
		if (!stats.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public static HexaGroup Group(int job)
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

	public static bool JobAllowed(int job)
	{
		if (Array.IndexOf(JobWhitelist, job) < 0)
		{
			return Array.IndexOf(JobWhitelist, job + 1) >= 0;
		}
		return true;
	}

	public static bool Allowed(int job, int coreId)
	{
		if (Core(coreId) == null)
		{
			return false;
		}
		return Group(job)?.Contains(coreId) ?? false;
	}

	public static int SolErdaExp(int itemId)
	{
		Ensure();
		foreach (SolErdaTier tier in tiers)
		{
			if (Array.IndexOf(tier.itemIds, itemId) >= 0)
			{
				return tier.exp;
			}
		}
		return 0;
	}
}
