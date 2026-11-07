using System.Collections.Generic;

public sealed class GrowthState
{
	public long honorExp;

	public int hyperPreset;

	public int abilityPreset;

	public string abilityLock = "";

	public string abilityChoice = "";

	public int soulMp;

	public int soulMax;

	public int quiverMode;

	public HashSet<int> LockedRows()
	{
		HashSet<int> hashSet = new HashSet<int>();
		if (string.IsNullOrEmpty(abilityLock))
		{
			return hashSet;
		}
		string[] array = abilityLock.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			if (int.TryParse(array[i].Trim(), out var result) && result >= 0 && result < 3)
			{
				hashSet.Add(result);
			}
		}
		return hashSet;
	}

	public void SetLockedRows(IEnumerable<int> rows)
	{
		List<int> list = new List<int>();
		foreach (int row in rows)
		{
			if (row >= 0 && row < 3 && !list.Contains(row))
			{
				list.Add(row);
			}
		}
		list.Sort();
		abilityLock = string.Join(",", list);
	}
}
