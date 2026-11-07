using System.Collections.Generic;

public static class ItemNames
{
	private static readonly Dictionary<int, string> cache = new Dictionary<int, string>();

	public static string Of(int id)
	{
		if (cache.Count == 0)
		{
			Load();
		}
		if (!cache.TryGetValue(id, out var value))
		{
			return id.ToString();
		}
		return value;
	}

	private static void Load()
	{
		cache[4031008] = "一转教官的介绍信";
		cache[4031009] = "一转教官的介绍信";
		cache[4031010] = "一转教官的介绍信";
		cache[4031011] = "一转教官的介绍信";
		cache[4031012] = "英雄的证明";
		cache[4031013] = "黑珠";
		cache[4031057] = "三转试炼证明";
		cache[4031058] = "智慧项链";
		cache[4031059] = "智慧项链";
		cache[2280003] = "技能书";
	}
}
