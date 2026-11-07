using System;

public sealed class HyperStatInfo
{
	public int skillId;

	public string name;

	public int maxLevel;

	public string key;

	public int perLevel;

	public int[] levels;

	public bool percent;

	public bool attack;

	public int Value(int level)
	{
		if (level <= 0)
		{
			return 0;
		}
		if (levels != null && levels.Length != 0)
		{
			return levels[Math.Min(level, levels.Length) - 1];
		}
		return perLevel * level;
	}
}
