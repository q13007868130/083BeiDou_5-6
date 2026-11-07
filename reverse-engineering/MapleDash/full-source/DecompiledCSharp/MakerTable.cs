using System;
using System.Collections.Generic;
using System.Globalization;

public static class MakerTable
{
	public sealed class MakerStatUp
	{
		public string stat;

		public int value;
	}

	public static Func<string> TextProvider;

	private static Dictionary<int, MakerRecipe> recipes;

	private static Dictionary<int, MakerStatUp> reagents;

	private static Dictionary<int, int> crystals;

	private static List<MakerRecipe> ordered;

	public static bool Loaded => recipes != null;

	public static void Invalidate()
	{
		recipes = null;
		reagents = null;
		crystals = null;
		ordered = null;
	}

	public static void Load(string text)
	{
		Dictionary<int, MakerRecipe> dictionary = new Dictionary<int, MakerRecipe>();
		Dictionary<int, MakerStatUp> dictionary2 = new Dictionary<int, MakerStatUp>();
		Dictionary<int, int> dictionary3 = new Dictionary<int, int>();
		List<MakerRecipe> list = new List<MakerRecipe>();
		if (string.IsNullOrEmpty(text))
		{
			recipes = dictionary;
			reagents = dictionary2;
			crystals = dictionary3;
			ordered = list;
			return;
		}
		string[] array = text.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i].Trim();
			if (text2.Length < 3 || text2[0] == '#')
			{
				continue;
			}
			string[] array2 = text2.Split('|');
			switch (array2[0])
			{
			case "M":
				if (array2.Length >= 12)
				{
					MakerRecipe makerRecipe = new MakerRecipe
					{
						category = I(array2[1]),
						itemId = I(array2[2]),
						reqLevel = I(array2[3]),
						reqSkillLevel = I(array2[4]),
						itemNum = I(array2[5]),
						tuc = I(array2[6]),
						meso = I(array2[7]),
						catalyst = I(array2[8], -1),
						reqItem = I(array2[9]),
						reqQuest = I(array2[10]),
						reqEquip = I(array2[11])
					};
					dictionary[makerRecipe.itemId] = makerRecipe;
					list.Add(makerRecipe);
				}
				break;
			case "R":
			{
				if (array2.Length >= 4 && dictionary.TryGetValue(I(array2[1]), out var value))
				{
					value.reqItems.Add(new MakerReqItem
					{
						itemId = I(array2[2]),
						count = I(array2[3])
					});
				}
				break;
			}
			case "O":
			{
				if (array2.Length >= 5 && dictionary.TryGetValue(I(array2[1]), out var value2))
				{
					value2.rewards.Add(new MakerRewardItem
					{
						itemId = I(array2[2]),
						quantity = I(array2[3]),
						prob = I(array2[4])
					});
				}
				break;
			}
			case "S":
				if (array2.Length >= 4)
				{
					dictionary2[I(array2[1])] = new MakerStatUp
					{
						stat = array2[2],
						value = I(array2[3])
					};
				}
				break;
			case "C":
				if (array2.Length >= 3)
				{
					dictionary3[I(array2[1])] = I(array2[2]);
				}
				break;
			}
		}
		recipes = dictionary;
		reagents = dictionary2;
		crystals = dictionary3;
		ordered = list;
	}

	private static int I(string s, int fallback = 0)
	{
		if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return fallback;
		}
		return result;
	}

	private static void Ensure()
	{
		if (recipes == null)
		{
			Load((TextProvider != null) ? TextProvider() : null);
		}
	}

	public static MakerRecipe Get(int itemId)
	{
		Ensure();
		if (recipes == null || !recipes.TryGetValue(itemId, out var value))
		{
			return null;
		}
		return value;
	}

	public static MakerStatUp Reagent(int itemId)
	{
		Ensure();
		if (reagents == null || !reagents.TryGetValue(itemId, out var value))
		{
			return null;
		}
		return value;
	}

	public static int CrystalFromLeftover(int leftoverId)
	{
		Ensure();
		if (crystals == null || !crystals.TryGetValue(leftoverId, out var value))
		{
			return -1;
		}
		return value;
	}

	public static int CategoryOf(int job)
	{
		if (job <= 0)
		{
			return 0;
		}
		int num = job / 100 % 10;
		if (num < 1 || num > 5)
		{
			return 0;
		}
		return 1 << num - 1;
	}

	public static List<MakerRecipe> All()
	{
		Ensure();
		return ordered ?? new List<MakerRecipe>();
	}

	public static List<MakerRecipe> ForJob(int job)
	{
		Ensure();
		int num = CategoryOf(job);
		List<MakerRecipe> list = new List<MakerRecipe>();
		if (ordered == null)
		{
			return list;
		}
		foreach (MakerRecipe item in ordered)
		{
			if (item.category == 0 || item.category == num)
			{
				list.Add(item);
			}
		}
		return list;
	}
}
