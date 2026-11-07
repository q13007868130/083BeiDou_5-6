using System.Globalization;

public static class NumberFormat
{
	public const long Wan = 10000L;

	public const long Yi = 100000000L;

	public const long Zhao = 1000000000000L;

	public const long Jing = 10000000000000000L;

	public static string Compact(long value)
	{
		if (value > -10000 && value < 10000)
		{
			return value.ToString("N0", CultureInfo.InvariantCulture);
		}
		bool flag = value < 0;
		long abs;
		if (flag)
		{
			abs = ((value == long.MinValue) ? long.MaxValue : (-value));
		}
		else
		{
			abs = value;
		}
		string text = Units(abs);
		if (!flag)
		{
			return text;
		}
		return "-" + text;
	}

	private static string Units(long abs)
	{
		if (abs < 10000)
		{
			return abs.ToString(CultureInfo.InvariantCulture);
		}
		long num;
		string text;
		if (abs >= 10000000000000000L)
		{
			num = 10000000000000000L;
			text = "京";
		}
		else if (abs >= 1000000000000L)
		{
			num = 1000000000000L;
			text = "兆";
		}
		else if (abs >= 100000000)
		{
			num = 100000000L;
			text = "亿";
		}
		else
		{
			num = 10000L;
			text = "万";
		}
		long num2 = abs / num;
		long num3 = abs % num;
		if (num3 != 0L)
		{
			return num2 + text + " " + Units(num3);
		}
		return num2 + text;
	}
}
