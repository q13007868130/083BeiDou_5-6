using System;
using System.Collections.Generic;
using UnityEngine;

public static class NativeCombatData
{
	private static Dictionary<string, object> root;

	public static Dictionary<string, object> Root => root ?? (root = (Dictionary<string, object>)MiniJson.Parse(Resources.Load<TextAsset>("GameplayCombat").text));

	public static Dictionary<string, object> Mob(string key)
	{
		foreach (Dictionary<string, object> item in (List<object>)Root["mobs"])
		{
			if ((string)item["key"] == key)
			{
				Dictionary<string, object> dictionary2 = (Dictionary<string, object>)item["stats"];
				MobDefenseCatalog.Shared.Apply(Int(item, "id"), dictionary2);
				return dictionary2;
			}
		}
		return null;
	}

	public static int Int(Dictionary<string, object> d, string key, int fallback = 0)
	{
		if (d == null || !d.TryGetValue(key, out var value))
		{
			return fallback;
		}
		return Convert.ToInt32(value);
	}

	public static long Long(Dictionary<string, object> d, string key, long fallback = 0L)
	{
		if (d == null || !d.TryGetValue(key, out var value))
		{
			return fallback;
		}
		return Convert.ToInt64(value);
	}
}
