using System.Collections.Generic;

public sealed class QuestActions
{
	public int exp;

	public int money;

	public int fame;

	public int nextQuest;

	public int petTameness;

	public readonly List<QuestItemReward> items = new List<QuestItemReward>();

	public readonly List<QuestSkillReward> skills = new List<QuestSkillReward>();
}
