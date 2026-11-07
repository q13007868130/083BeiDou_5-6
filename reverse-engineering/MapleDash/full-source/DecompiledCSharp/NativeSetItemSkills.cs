using System.Collections.Generic;

public static class NativeSetItemSkills
{
	private static readonly Dictionary<int, int> granted = new Dictionary<int, int>();

	private static readonly Dictionary<int, string> names = new Dictionary<int, string>();

	private static int lastSignature;

	public static int Count => granted.Count;

	public static bool IsSkill(int id)
	{
		return granted.ContainsKey(id);
	}

	public static int LevelOf(int id)
	{
		if (!granted.TryGetValue(id, out var value))
		{
			return 0;
		}
		return value;
	}

	public static string NameOf(int id)
	{
		if (!names.TryGetValue(id, out var value))
		{
			return "技能 " + id;
		}
		return value;
	}

	public static void Refresh(IEnumerable<int> wornIds, int jobIndex)
	{
		int num = 17;
		foreach (int wornId in wornIds)
		{
			num = num * 31 + wornId;
		}
		num = num * 31 + jobIndex;
		if (num == lastSignature)
		{
			return;
		}
		lastSignature = num;
		granted.Clear();
		names.Clear();
		foreach (AdminSetItem.GrantedSkill item in AdminSetItem.ActiveSkillsFor(wornIds, jobIndex))
		{
			granted[item.Id] = item.Level;
			names[item.Id] = (string.IsNullOrEmpty(item.Name) ? ("技能 " + item.Id) : item.Name);
		}
	}
}
