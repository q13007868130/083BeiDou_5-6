using System;
using System.Collections.Generic;
using System.Globalization;

public static class ServerExpTable
{
	private static long[] exp;

	private static int maxLevel = 300;

	public static int MaxLevel
	{
		get
		{
			Ensure();
			return maxLevel;
		}
	}

	public static void Ensure()
	{
		if (exp != null)
		{
			return;
		}
		string text = PlatformRes.LoadText("Field/ExpTable");
		List<long> list = new List<long>();
		if (!string.IsNullOrEmpty(text))
		{
			Dictionary<string, object> dictionary = MiniJsonDb.Parse(text) as Dictionary<string, object>;
			if (dictionary != null && dictionary.TryGetValue("exp", out var value) && value is List<object> list2)
			{
				foreach (object item in list2)
				{
					list.Add((item == null) ? 0 : Convert.ToInt64(item, CultureInfo.InvariantCulture));
				}
			}
			if (dictionary != null && dictionary.TryGetValue("maxLevel", out var value2) && value2 != null)
			{
				long val = Convert.ToInt64(value2, CultureInfo.InvariantCulture);
				maxLevel = (int)Math.Max(1L, Math.Min(300L, val));
			}
		}
		if (list.Count == 0)
		{
			list.Add(0L);
			list.Add(15L);
			list.Add(34L);
		}
		exp = ExpCurve.Build(list.ToArray());
	}

	public static long GetExpNeededForLevel(int level)
	{
		Ensure();
		return ExpCurve.Needed(exp, level);
	}
}
