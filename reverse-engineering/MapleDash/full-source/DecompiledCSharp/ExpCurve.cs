using System;

public static class ExpCurve
{
	public const int CapLevel = 300;

	public const int GrowthFromLevel = 200;

	public const int GrowthPercent = 20;

	public static long[] Build(long[] source)
	{
		long[] array = new long[301];
		int num = ((source == null || source.Length == 0) ? (-1) : (source.Length - 1));
		for (int i = 0; i <= 300; i++)
		{
			if (i < 200)
			{
				array[i] = ((num < 0) ? 15 : source[Math.Min(i, num)]);
				continue;
			}
			long num2 = Math.Max(1L, array[i - 1]);
			array[i] = num2 + num2 / 100 * 20 + num2 % 100 * 20 / 100;
		}
		return array;
	}

	public static long Needed(long[] table, int level)
	{
		if (table == null || table.Length == 0)
		{
			return 15L;
		}
		return table[Math.Max(0, Math.Min(level, table.Length - 1))];
	}
}
