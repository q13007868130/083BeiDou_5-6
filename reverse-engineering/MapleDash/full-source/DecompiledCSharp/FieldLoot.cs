using System;
using System.Collections.Generic;

public static class FieldLoot
{
	public delegate void DropSink(int itemId, int quantity, int offsetIndex);

	public static float OffsetPx(int d)
	{
		if (d % 2 != 0)
		{
			return -25f * (float)(d / 2);
		}
		return 25f * (float)((d + 1) / 2);
	}

	public static void Roll(int mobId, DropSink sink, Random rng, double dropRate = 1.0, Func<int, int, bool> needQuestItem = null)
	{
		if (sink == null || rng == null)
		{
			return;
		}
		int num = 1;
		List<DropEntry> list = DropStore.ForMob(mobId);
		if (list.Count > 0)
		{
			List<DropEntry> list2 = new List<DropEntry>(list);
			Shuffle(list2, rng);
			foreach (DropEntry item in list2)
			{
				if ((double)rng.Next(0, 999999) >= (double)item.chance * dropRate || (needQuestItem != null && !needQuestItem(item.quest, item.item)))
				{
					continue;
				}
				if (item.item == 0)
				{
					int num2 = ((item.max > item.min) ? rng.Next(item.min, item.max) : item.min);
					if (num2 > 0)
					{
						sink(0, num2, num++);
					}
					continue;
				}
				int num3 = Math.Max(1, item.min);
				if (item.max > item.min)
				{
					num3 += rng.Next(0, item.max - item.min + 1);
				}
				sink(item.item, num3, num++);
			}
		}
		List<DropEntry> list3 = new List<DropEntry>(DropStore.Global());
		if (list3.Count == 0)
		{
			return;
		}
		Shuffle(list3, rng);
		foreach (DropEntry item2 in list3)
		{
			if (!((double)rng.Next(0, 999999) >= (double)item2.chance * dropRate) && item2.item != 0 && (needQuestItem == null || needQuestItem(item2.quest, item2.item)))
			{
				int num4 = Math.Max(1, item2.min);
				int num5 = Math.Max(num4, item2.max);
				int quantity = ((num5 > num4) ? rng.Next(num4, num5 + 1) : num4);
				sink(item2.item, quantity, num++);
			}
		}
	}

	public static void Shuffle<T>(IList<T> list, Random rng)
	{
		for (int num = list.Count - 1; num > 0; num--)
		{
			int index = rng.Next(0, num + 1);
			T value = list[num];
			list[num] = list[index];
			list[index] = value;
		}
	}
}
