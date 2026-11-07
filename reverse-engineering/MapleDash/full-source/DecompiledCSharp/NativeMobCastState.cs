using System;
using System.Collections.Generic;

public sealed class NativeMobCastState
{
	private readonly Dictionary<string, long> ready = new Dictionary<string, long>();

	public int Mp { get; private set; }

	public static bool HpAllows(long hp, long maximum, int threshold)
	{
		return (int)((float)hp / (float)maximum * 100f) <= ((threshold <= 0) ? 100 : threshold);
	}

	public NativeMobCastState(int mp)
	{
		Mp = Math.Max(0, mp);
	}

	public bool CanUse(string key, long now, int mpCost, bool sealedSkills)
	{
		if (!sealedSkills && Mp >= mpCost)
		{
			if (ready.TryGetValue(key, out var value))
			{
				return now >= value;
			}
			return true;
		}
		return false;
	}

	public bool TryUse(string key, long now, int mpCost, long cooldown, bool sealedSkills)
	{
		if (!CanUse(key, now, mpCost, sealedSkills))
		{
			return false;
		}
		Mp -= mpCost;
		ready[key] = now + Math.Max(0L, cooldown);
		return true;
	}

	public void HealMp(int amount, int maximum)
	{
		Mp = Math.Max(0, Math.Min(maximum, Mp + amount));
	}
}
