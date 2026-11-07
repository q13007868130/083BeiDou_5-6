using System;

public static class ExpeditionBossLog
{
	public struct Entry(string name, int entries, bool week = false)
	{
		public readonly string Name = name;

		public readonly int Entries = entries;

		public readonly bool Week = week;
	}

	public static readonly Entry[] Entries = new Entry[10]
	{
		new Entry("ZAKUM", 3),
		new Entry("CHAOS_ZAKUM", 5),
		new Entry("HORNTAIL", 3),
		new Entry("CHAOS_HORNTAIL", 5),
		new Entry("CYGNUS", 1),
		new Entry("AKAYRUM", 1),
		new Entry("LUCID", 1),
		new Entry("PINKBEAN", 3),
		new Entry("SCARGA", 1),
		new Entry("PAPULATUS", 2)
	};

	public static Func<int, string, int> CountEntries;

	public static Action<int, string> InsertEntry;

	private static readonly int[] BossMobs = new int[61]
	{
		8800000, 8800001, 8800002, 8800003, 8800004, 8800005, 8800006, 8800007, 8800008, 8800009,
		8800010, 8800100, 8800101, 8800102, 8800103, 8800104, 8800105, 8800106, 8800107, 8800108,
		8800109, 8800110, 8810000, 8810001, 8810002, 8810003, 8810004, 8810005, 8810006, 8810007,
		8810008, 8810009, 8810100, 8810101, 8810102, 8810103, 8810104, 8810105, 8810106, 8810107,
		8810108, 8810109, 8810118, 8810122, 8850000, 8850001, 8850002, 8850003, 8850004, 8850011,
		8880140, 8880141, 8880152, 9420546, 9420547, 9420548, 9420549, 9420541, 9420542, 9420543,
		9420544
	};

	private const int CygnusBossMin = 8850000;

	private const int CygnusBossMax = 8850013;

	public static bool AttemptBoss(int characterId, ExpeditionType type, bool log)
	{
		if (Expedition.ConfigBool == null)
		{
			return true;
		}
		if (!Expedition.ConfigBool("use_enable_daily_expeditions"))
		{
			return true;
		}
		Entry entry = default;
		bool flag = false;
		Entry[] entries = Entries;
		for (int i = 0; i < entries.Length; i++)
		{
			Entry entry2 = entries[i];
			if (entry2.Name == type.ToString())
			{
				entry = entry2;
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return true;
		}
		if (CountEntries != null && CountEntries(characterId, entry.Name) >= entry.Entries)
		{
			return false;
		}
		if (log)
		{
			InsertEntry?.Invoke(characterId, entry.Name);
		}
		return true;
	}

	public static bool IsExpeditionBoss(int mobId)
	{
		if (mobId >= 8850000 && mobId <= 8850013)
		{
			return true;
		}
		int[] bossMobs = BossMobs;
		for (int i = 0; i < bossMobs.Length; i++)
		{
			if (bossMobs[i] == mobId)
			{
				return true;
			}
		}
		return false;
	}
}
