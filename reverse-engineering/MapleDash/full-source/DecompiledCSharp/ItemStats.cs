using System;
using System.Collections.Generic;
using MapleDash.Wz;

public struct ItemStats
{
	public static readonly string[] Keys = new string[15]
	{
		"incSTR", "incDEX", "incINT", "incLUK", "incMHP", "incMMP", "incPAD", "incMAD", "incPDD", "incMDD",
		"incACC", "incEVA", "incCraft", "incSpeed", "incJump"
	};

	private static readonly string[] StatKeys = new string[15]
	{
		"str", "dex", "int", "luk", "hp", "mp", "watk", "matk", "wdef", "mdef",
		"acc", "avoid", "hands", "speed", "jump"
	};

	private static readonly int[][] FlameColumns = new int[17][]
	{
		new int[1],
		new int[1] { 1 },
		new int[1] { 2 },
		new int[1] { 3 },
		new int[2] { 0, 1 },
		new int[2] { 0, 2 },
		new int[2] { 0, 3 },
		new int[2] { 1, 2 },
		new int[2] { 1, 3 },
		new int[2] { 2, 3 },
		new int[1] { 6 },
		new int[1] { 7 },
		new int[1] { 8 },
		new int[1] { 4 },
		new int[1] { 5 },
		new int[1] { 13 },
		new int[1] { 14 }
	};

	public int[] baseStats;

	public int[] starStats;

	public int[] flameStats;

	public bool starForceVestige;

	public int Str => Sum(0);

	public int Dex => Sum(1);

	public int Int => Sum(2);

	public int Luk => Sum(3);

	public int Hp => Sum(4);

	public int Mp => Sum(5);

	public int Watk => Sum(6);

	public int Matk => Sum(7);

	private int Sum(int column)
	{
		return ((baseStats != null) ? baseStats[column] : 0) + ((starStats != null) ? starStats[column] : 0) + ((flameStats != null) ? flameStats[column] : 0);
	}

	public static ItemStats Template(int itemId)
	{
		ItemStats result = new ItemStats
		{
			baseStats = new int[Keys.Length],
			starStats = new int[Keys.Length],
			flameStats = new int[Keys.Length]
		};
		ImgNode imgNode = EquipCatalog.Info(itemId);
		if (imgNode == null)
		{
			return result;
		}
		for (int i = 0; i < Keys.Length; i++)
		{
			result.baseStats[i] = imgNode.Int(Keys[i]);
		}
		return result;
	}

	public static ItemStats Of(InventoryEntry entry)
	{
		ItemStats result = new ItemStats
		{
			baseStats = new int[Keys.Length],
			starStats = new int[Keys.Length],
			flameStats = new int[Keys.Length]
		};
		if (entry == null || entry.itemId <= 0)
		{
			return result;
		}
		ImgNode imgNode = EquipCatalog.Info(entry.itemId);
		if (imgNode == null)
		{
			return result;
		}
		bool flag = entry.equipmentData != null && entry.equipmentData.TryGetValue("enhancementVersion", out var value) && Convert.ToInt32(value) == 282;
		result.starForceVestige = entry.equipmentData != null && entry.equipmentData.TryGetValue("starForceVestige", out var value2) && Convert.ToInt32(value2) != 0;
		bool flag2 = NativeScrollService.HasStats(entry);
		int[] array = ((!entry.isEquip) ? null : new int[15]
		{
			entry.str, entry.dex, entry.int_, entry.luk, entry.hp, entry.mp, entry.watk, entry.matk, entry.wdef, entry.mdef,
			entry.acc, entry.avoid, entry.hands, entry.speed, entry.jump
		});
		for (int i = 0; i < Keys.Length; i++)
		{
			if (flag)
			{
				result.baseStats[i] = ((flag2 && array != null) ? array[i] : imgNode.Int(Keys[i]));
			}
			else if (array == null)
			{
				result.baseStats[i] = imgNode.Int(Keys[i]);
			}
			else
			{
				result.baseStats[i] = Math.Max(array[i], imgNode.Int(Keys[i]));
			}
		}
		if (flag && !result.starForceVestige)
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			for (int j = 0; j < Keys.Length; j++)
			{
				dictionary[StatKeys[j]] = result.baseStats[j];
			}
			int num = imgNode.Int("reqLevel");
			if (entry.equipmentData != null && entry.equipmentData.TryGetValue("iIncReq", out var value3))
			{
				num += Convert.ToInt32(value3);
			}
			Dictionary<string, int> dictionary2 = StarForceStats.Calculate(entry.itemId, num, entry.starLevel, imgNode.Int("superiorEqp") != 0, dictionary);
			for (int k = 0; k < Keys.Length; k++)
			{
				if (dictionary2.TryGetValue(StatKeys[k], out var value4))
				{
					result.starStats[k] = value4;
				}
			}
		}
		foreach (FlameLine item in NativeGrowthStats.DescribeFlame(entry))
		{
			if (item.statVal >= 0 && item.statVal < FlameColumns.Length)
			{
				int[] array2 = FlameColumns[item.statVal];
				foreach (int num2 in array2)
				{
					result.flameStats[num2] += item.value;
				}
			}
		}
		return result;
	}
}
