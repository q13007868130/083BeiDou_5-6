using System;
using System.Collections.Generic;

namespace Server.Bot.Decorator;

public sealed class GenericEquipPool
{
	private sealed class PoolItem
	{
		public int id;

		public int level;

		public int gender;
	}

	private readonly Dictionary<string, List<PoolItem>> pools = new Dictionary<string, List<PoolItem>>();

	private readonly Random random;

	public GenericEquipPool(BotResources resources, BotItemCatalog catalog, Random random = null)
	{
		this.random = random ?? new Random();
		foreach (KeyValuePair<string, object> item in (resources.ReadData("soloMapling/ArtificialPlayer/BotDecoratorSystem/GenericEquipPool.yaml") as Dictionary<string, object>) ?? throw new InvalidOperationException("Original generic equipment pool is missing"))
		{
			if (!(item.Value is List<object> list))
			{
				continue;
			}
			List<PoolItem> list2 = new List<PoolItem>();
			foreach (object item2 in list)
			{
				int num = Convert.ToInt32(item2);
				list2.Add(new PoolItem
				{
					id = num,
					level = BotItemCatalog.Int(catalog.Info(num), "reqLevel"),
					gender = num / 1000 % 10
				});
			}
			pools[item.Key] = list2;
		}
	}

	public int? Pick(string category, int level, int gender)
	{
		if (!pools.TryGetValue(category, out var value))
		{
			return null;
		}
		List<PoolItem> list = new List<PoolItem>();
		List<double> list2 = new List<double>();
		double num = 0.0;
		foreach (PoolItem item in value)
		{
			if (item.level <= level && (item.gender == 2 || item.gender == gender))
			{
				double num2 = Math.Max(0.05, 1.0 / (1.0 + (double)(level - item.level) * 0.05));
				list.Add(item);
				list2.Add(num2);
				num += num2;
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		double num3 = random.NextDouble() * num;
		double num4 = 0.0;
		for (int i = 0; i < list.Count; i++)
		{
			num4 += list2[i];
			if (num3 <= num4)
			{
				return list[i].id;
			}
		}
		return list[list.Count - 1].id;
	}
}
