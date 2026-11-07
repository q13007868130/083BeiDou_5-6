using System;
using System.Collections.Generic;

namespace Server.Bot;

public static class BotDirectory
{
	private const string TableName = "bot_profile";

	private const int Seed = 20261027;

	public const int DefaultSize = 300;

	private static readonly int[] BeginnerJobs = new int[1];

	private static readonly int[] FirstJobs = new int[5] { 100, 200, 300, 400, 500 };

	private static readonly int[] SecondJobs = new int[12]
	{
		110, 120, 130, 210, 220, 230, 310, 320, 410, 420,
		510, 520
	};

	private static readonly int[] ThirdJobs = new int[12]
	{
		111, 121, 131, 211, 221, 231, 311, 321, 411, 421,
		511, 521
	};

	private static readonly int[] FourthJobs = new int[12]
	{
		112, 122, 132, 212, 222, 232, 312, 322, 412, 422,
		512, 522
	};

	private static readonly List<BotProfile> profiles = new List<BotProfile>();

	private static readonly HashSet<int> inUse = new HashSet<int>();

	private static readonly Dictionary<int, List<int>> byMap = new Dictionary<int, List<int>>();

	private static int loadedSize = -1;

	public static int Count => profiles.Count;

	public static int InUseCount => inUse.Count;

	public static bool Enabled => profiles.Count > 0;

	public static IReadOnlyList<BotProfile> All => profiles;

	public static int[] JobsForLevel(int level)
	{
		if (level >= 10)
		{
			if (level >= 30)
			{
				if (level >= 70)
				{
					if (level >= 120)
					{
						return FourthJobs;
					}
					return ThirdJobs;
				}
				return SecondJobs;
			}
			return FirstJobs;
		}
		return BeginnerJobs;
	}

	public static int CoverageCount(int maxLevel)
	{
		int num = 0;
		int num2 = Math.Max(1, Math.Min(200, maxLevel));
		for (int i = 1; i <= num2; i++)
		{
			num += JobsForLevel(i).Length;
		}
		return num;
	}

	public static int ExpectedCount(DbStore db)
	{
		int num = BotConfig.DirectorySize(db);
		if (num <= 0)
		{
			return 0;
		}
		if (!BotConfig.DirectoryCoverageEnabled(db))
		{
			return num;
		}
		return CoverageCount(BotConfig.DirectoryMaxLevel(db));
	}

	private static bool GridAt(int index, out int level, out int job)
	{
		level = 0;
		job = 0;
		if (index < 0)
		{
			return false;
		}
		int num = index;
		for (int i = 1; i <= 200; i++)
		{
			int[] array = JobsForLevel(i);
			if (num < array.Length)
			{
				level = i;
				job = array[num];
				return true;
			}
			num -= array.Length;
		}
		return false;
	}

	public static void Reset()
	{
		profiles.Clear();
		inUse.Clear();
		byMap.Clear();
		loadedSize = -1;
	}

	public static int Size(DbStore db)
	{
		return BotConfig.DirectorySize(db);
	}

	public static void Ensure(DbStore db)
	{
		int num = ExpectedCount(db);
		if (loadedSize == num && profiles.Count > 0)
		{
			return;
		}
		if (num <= 0 || db == null)
		{
			loadedSize = 0;
			profiles.Clear();
			inUse.Clear();
			byMap.Clear();
		}
		else
		{
			if (!db.HasTable("bot_profile"))
			{
				return;
			}
			loadedSize = num;
			profiles.Clear();
			inUse.Clear();
			byMap.Clear();
			BotAppearanceTemplates.Ensure(db);
			int count = BotAppearanceTemplates.All.Count;
			DbTable dbTable = db.Table("bot_profile");
			if (dbTable == null)
			{
				loadedSize = -1;
				return;
			}
			List<DbRow> list = dbTable.Select(null);
			bool flag = BotConfig.DirectoryCoverageEnabled(db);
			bool flag2 = false;
			if (flag)
			{
				for (int i = 0; i < list.Count && i < num; i++)
				{
					if (ToLong(list[i], "level", 0L) <= 0 && GridAt(i, out var level, out var job))
					{
						list[i]["level"] = (long)level;
						list[i]["job"] = (long)job;
						flag2 = true;
					}
				}
			}
			if (list.Count < num)
			{
				HashSet<string> hashSet = new HashSet<string>();
				for (int j = 0; j < list.Count; j++)
				{
					string text = ToText(list[j], "name");
					if (text.Length > 0)
					{
						hashSet.Add(text);
					}
				}
				for (int k = list.Count; k < num; k++)
				{
					GridAt(k, out var level2, out var job2);
					DbRow dbRow = new DbRow
					{
						["name"] = (BotConfig.RandomChineseNameEnabled(db) ? UniqueName(k, hashSet) : FixedName(k, hashSet)),
						["template"] = ((count > 0) ? NextTemplate(k, count) : 0),
						["map_id"] = 0,
						["channel"] = 1,
						["bound"] = 0,
						["enabled"] = 1,
						["level"] = (long)level2,
						["job"] = (long)job2
					};
					dbTable.Insert(dbRow);
					list.Add(dbRow);
				}
				flag2 = true;
			}
			if (flag2)
			{
				db.Save();
			}
			for (int l = 0; l < list.Count; l++)
			{
				if (profiles.Count >= num)
				{
					break;
				}
				DbRow row = list[l];
				if (ToLong(row, "enabled", 1L) == 0L)
				{
					continue;
				}
				BotProfile botProfile = new BotProfile
				{
					id = (int)ToLong(row, "id", 0L),
					name = ToText(row, "name"),
					template = (int)ToLong(row, "template", 0L),
					mapId = (int)ToLong(row, "map_id", 0L),
					channel = (int)ToLong(row, "channel", 1L),
					level = (int)ToLong(row, "level", 0L),
					job = (int)ToLong(row, "job", 0L)
				};
				int count2 = profiles.Count;
				profiles.Add(botProfile);
				if (botProfile.mapId > 0)
				{
					if (!byMap.TryGetValue(botProfile.mapId, out var value))
					{
						value = (byMap[botProfile.mapId] = new List<int>());
					}
					value.Add(count2);
				}
			}
		}
	}

	private static int NextTemplate(int index, int templateCount)
	{
		return (int)((uint)(index * 2654435761u) % (uint)templateCount);
	}

	private static string UniqueName(int index, HashSet<string> used)
	{
		for (int i = 0; i < 256; i++)
		{
			string randomChineseBotName = BotHelpers.GetRandomChineseBotName(new Random(628091837 + index * 7919 + i));
			if (used.Add(randomChineseBotName))
			{
				return randomChineseBotName;
			}
		}
		string text = "游客" + (index + 1);
		used.Add(text);
		return text;
	}

	private static string FixedName(int index, HashSet<string> used)
	{
		string text = "游客" + (index + 1);
		while (!used.Add(text))
		{
			text += "_";
		}
		return text;
	}

	public static BotProfile Take(int mapId, Random random)
	{
		if (profiles.Count == 0)
		{
			return null;
		}
		if (byMap.TryGetValue(mapId, out var value))
		{
			for (int i = 0; i < value.Count; i++)
			{
				BotProfile botProfile = profiles[value[i]];
				if (!inUse.Contains(botProfile.id))
				{
					inUse.Add(botProfile.id);
					return botProfile;
				}
			}
		}
		int num = ((random != null && profiles.Count > 0) ? random.Next(profiles.Count) : 0);
		for (int j = 0; j < profiles.Count; j++)
		{
			BotProfile botProfile2 = profiles[(num + j) % profiles.Count];
			if (botProfile2.mapId <= 0 && !inUse.Contains(botProfile2.id))
			{
				inUse.Add(botProfile2.id);
				return botProfile2;
			}
		}
		return null;
	}

	public static void Release(int profileId)
	{
		if (profileId != 0)
		{
			inUse.Remove(profileId);
		}
	}

	public static BotProfile Find(int profileId)
	{
		for (int i = 0; i < profiles.Count; i++)
		{
			if (profiles[i].id == profileId)
			{
				return profiles[i];
			}
		}
		return null;
	}

	private static long ToLong(DbRow row, string key, long fallback)
	{
		if (row == null || !row.TryGetValue(key, out var value) || value == null)
		{
			return fallback;
		}
		try
		{
			return Convert.ToInt64(value);
		}
		catch (Exception)
		{
			return fallback;
		}
	}

	private static string ToText(DbRow row, string key)
	{
		if (row == null || !row.TryGetValue(key, out var value) || value == null)
		{
			return "";
		}
		return value.ToString();
	}
}
