using System.Collections.Generic;

public sealed class QuestRequirement
{
	public int level;

	public int maxLevel;

	public int meso;

	public int fame;

	public int npc = -1;

	public int interval;

	public int fieldEnter;

	public int completedQuests;

	public int monsterBookMin;

	public int petTameness;

	public string startDate = "";

	public string endDate = "";

	public readonly List<int> pets = new List<int>();

	public readonly List<int> jobs = new List<int>();

	public readonly List<QuestItemReq> items = new List<QuestItemReq>();

	public readonly List<KeyValuePair<int, int>> quests = new List<KeyValuePair<int, int>>();

	public readonly List<QuestItemReq> mobs = new List<QuestItemReq>();

	public readonly List<string> infoEx = new List<string>();
}
