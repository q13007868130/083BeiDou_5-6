using System;
using System.Collections.Generic;

public static class SoulCatalog
{
	public const int EnchanterBase = 2590000;

	public const int SoulBase = 2591000;

	public const int MinLevel = 75;

	public const int MaxSoulMp = 1000;

	public static readonly int[] FallbackPotList = new int[16]
	{
		32001, 32002, 32003, 32004, 32005, 32006, 32011, 32012, 32041, 32042,
		32043, 32044, 32045, 32046, 32051, 32052
	};

	private static readonly Dictionary<int, SoulOption> table = Build();

	private static Dictionary<int, int> soulOptions;

	private static Dictionary<int, int> SoulOptions => soulOptions ?? (soulOptions = LoadSoulOptions());

	public static int FallbackOption(int roll)
	{
		return FallbackPotList[Math.Abs(roll) % FallbackPotList.Length];
	}

	private static Dictionary<int, SoulOption> Build()
	{
		Dictionary<int, SoulOption> map = new Dictionary<int, SoulOption>();
		int[] array = new int[9] { 3, 5, 7, 10, 12, 15, 18, 20, 24 };
		int[] array2 = new int[9] { 101, 105, 109, 113, 163, 117, 185, 121, 167 };
		string[] array3 = new string[4] { "STR", "DEX", "INT", "LUK" };
		for (int i = 0; i < array.Length; i++)
		{
			for (int j = 0; j < array3.Length; j++)
			{
				Add(array2[i] + j, array3[j], array[i]);
			}
		}
		Add(131, "MHP", 150);
		Add(132, "MMP", 150);
		Add(133, "MHP", 200);
		Add(134, "MMP", 200);
		Add(135, "MHP", 300);
		Add(136, "MMP", 300);
		Add(137, "MHP", 400);
		Add(175, "MHP", 500);
		Add(138, "MHP", 600);
		Add(189, "MHP", 700);
		Add(139, "MHP", 800);
		Add(177, "MHP", 960);
		Add(201, "AllStats", 2);
		Add(202, "AllStats", 3);
		Add(203, "AllStats", 5);
		Add(204, "AllStats", 7);
		Add(210, "AllStats", 8);
		Add(205, "AllStats", 10);
		Add(40081, "AllStats", 12);
		Add(70057, "AllStats", 15);
		Add(2011, "PAD", 3);
		Add(2012, "MAD", 3);
		Add(153, "PAD", 4);
		Add(154, "MAD", 4);
		Add(155, "PAD", 5);
		Add(156, "MAD", 5);
		Add(171, "PAD", 6);
		Add(172, "MAD", 6);
		Add(42292, "", 0);
		Add(60009, "", 0);
		int[] fallbackPotList = FallbackPotList;
		for (int k = 0; k < fallbackPotList.Length; k++)
		{
			Add(fallbackPotList[k], "", 0);
		}
		return map;
		void Add(int code, string key, int value)
		{
			map[code] = new SoulOption
			{
				code = code,
				key = key,
				value = value
			};
		}
	}

	public static SoulOption Of(int code)
	{
		if (!table.TryGetValue(code, out var value))
		{
			return null;
		}
		return value;
	}

	public static int SoulIdOf(int itemId)
	{
		return 1 + itemId % 2591000;
	}

	private static Dictionary<int, int> LoadSoulOptions()
	{
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		string text = PlatformRes.LoadText("Enhancement/SoulOptions");
		if (string.IsNullOrEmpty(text))
		{
			return dictionary;
		}
		if (!(MiniJsonDb.Parse(text) is Dictionary<string, object> dictionary2))
		{
			return dictionary;
		}
		if (!dictionary2.TryGetValue("options", out var value) || !(value is Dictionary<string, object> dictionary3))
		{
			return dictionary;
		}
		foreach (KeyValuePair<string, object> item in dictionary3)
		{
			if (int.TryParse(item.Key, out var result) && item.Value != null && int.TryParse(item.Value.ToString(), out var result2))
			{
				dictionary[result] = result2;
			}
		}
		return dictionary;
	}

	public static int OptionFor(int soulItemId)
	{
		if (!SoulOptions.TryGetValue(soulItemId, out var value))
		{
			return 0;
		}
		return value;
	}
}
