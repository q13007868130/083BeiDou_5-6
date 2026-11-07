using System;
using System.Collections.Generic;

namespace Server.Bot;

public static class BotHelpers
{
	private static readonly string[] ChineseNamePrefixes = new string[20]
	{
		"小", "阿", "云", "星", "枫", "南", "北", "青", "白", "洛",
		"夏", "秋", "雨", "风", "月", "川", "竹", "桃", "一", "七"
	};

	private static readonly string[] ChineseNameSuffixes = new string[20]
	{
		"枫", "糖", "鱼", "鹿", "橙", "眠", "茶", "雪", "竹", "舟",
		"安", "宁", "澈", "遥", "然", "梨", "月", "星", "豆", "夏"
	};

	private static readonly Random rng = new Random();

	public static bool IsBot(BotActor c)
	{
		if (c != null)
		{
			return IsBot(c.id);
		}
		return false;
	}

	public static bool IsBot(int id)
	{
		if (id <= 20000)
		{
			return id == 999;
		}
		return true;
	}

	public static string GetRandomChineseBotName()
	{
		return GetRandomChineseBotName(rng);
	}

	public static string GetRandomChineseBotName(Random random)
	{
		string text = ChineseNamePrefixes[random.Next(ChineseNamePrefixes.Length)];
		string text2 = ChineseNameSuffixes[random.Next(ChineseNameSuffixes.Length)];
		if (random.Next(100) < 35)
		{
			string text3 = ChineseNameSuffixes[random.Next(ChineseNameSuffixes.Length)];
			return text + text2 + text3;
		}
		return text + text2;
	}

	public static (int x0, int y0, int x1, int y1) CreateRectangle(int px, int py, int w, int h)
	{
		return (x0: px - w / 2, y0: py - h / 2, x1: px + w / 2, y1: py + h / 2);
	}

	public static string ConvertItemIdToName(int itemId)
	{
		return BotItemCatalog.Shared.Name(itemId) ?? "NULL";
	}

	public static bool CheckSecondListInsideFirstList<T>(List<T> inner, List<T> outer)
	{
		foreach (T item in inner)
		{
			if (!outer.Contains(item))
			{
				return false;
			}
		}
		return true;
	}

	public static double Distance(int x1, int y1, int x2, int y2)
	{
		int num = x1 - x2;
		int num2 = y1 - y2;
		return Math.Sqrt(num * num + num2 * num2);
	}

	public static (int x, int y) AdjustCenterPositionXAxis(int centerX, int centerY, int currIndex, int initialIncrement, int subsequentIncrement, int offset)
	{
		int num = centerX;
		if (currIndex < initialIncrement)
		{
			num += offset;
		}
		else
		{
			int num2 = currIndex - initialIncrement;
			int num3 = num2 / subsequentIncrement % 2;
			if (num2 % subsequentIncrement < subsequentIncrement)
			{
				num += ((num3 == 0) ? (-offset) : offset);
			}
		}
		return (x: num, y: centerY);
	}
}
