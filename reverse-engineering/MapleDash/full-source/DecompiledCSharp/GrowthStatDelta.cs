using System.Collections.Generic;

public sealed class GrowthStatDelta
{
	public readonly Dictionary<string, int> set = new Dictionary<string, int>();

	public float criticalChance;

	public float criticalBonus;

	public float hexaDamagePercent;

	public float hexaBossPercent;

	public float hexaIgnorePercent;

	public int arcaneForce;

	public int authenticForce;

	public readonly List<string> unsettled = new List<string>();

	public void Add(string key, int value)
	{
		if (!string.IsNullOrEmpty(key) && value != 0)
		{
			set[key] = (set.TryGetValue(key, out var value2) ? value2 : 0) + value;
		}
	}

	public int Get(string key)
	{
		if (!set.TryGetValue(key, out var value))
		{
			return 0;
		}
		return value;
	}
}
