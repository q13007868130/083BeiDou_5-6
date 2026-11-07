using System;
using System.Collections.Generic;
using System.Text;

public static class NativeEquipmentCube
{
	public sealed class Roll
	{
		public int itemId;

		public int cubeItemId;

		public int grade;

		public string data;

		public string description;

		public bool rankedUp;

		public bool canKeepOld;
	}

	private sealed class Data
	{
		public int cube;

		public int grade;

		public readonly List<KeyValuePair<int, int>> lines = new List<KeyValuePair<int, int>>();
	}

	private static readonly string[] Stats = new string[14]
	{
		"STR", "DEX", "INT", "LUK", "WATK", "MATK", "HP", "MP", "WDEF", "MDEF",
		"ACC", "AVOID", "SPEED", "JUMP"
	};

	private static readonly string[] Labels = new string[14]
	{
		"力量", "敏捷", "智力", "运气", "攻击力", "魔法力", "生命值", "魔法值", "物理防御", "魔法防御",
		"命中", "回避", "移动速度", "跳跃力"
	};

	private static readonly string[] Names = new string[8] { "奇幻魔方", "白金奇幻魔方", "超级奇幻魔方", "星星魔方", "太阳魔方", "传说魔方", "红色魔方", "黑色魔方" };

	private static readonly string[] Grades = new string[4] { "特殊", "稀有", "罕见", "传说" };

	private static readonly int[] MaxGrades = new int[8] { 3, 4, 3, 3, 4, 4, 4, 4 };

	private static readonly int[,] Rates = new int[8, 3]
	{
		{ 12, 6, 0 },
		{ 20, 10, 3 },
		{ 18, 8, 0 },
		{ 20, 10, 0 },
		{ 25, 12, 4 },
		{ 18, 8, 2 },
		{ 15, 6, 1 },
		{ 20, 8, 2 }
	};

	private static readonly int[] Powers = new int[4] { 1, 2, 4, 8 };

	private static readonly int[] Weapon = new int[9] { 0, 1, 2, 3, 4, 5, 6, 7, 10 };

	private static readonly int[] Armor = new int[12]
	{
		0, 1, 2, 3, 6, 7, 8, 9, 10, 11,
		12, 13
	};

	private static readonly Random Random = new Random();

	public static bool IsCube(int id)
	{
		if (id >= 4007000)
		{
			return id <= 4007007;
		}
		return false;
	}

	private static int Index(int id)
	{
		if (!IsCube(id))
		{
			throw new ArgumentException("未知魔方道具：" + id);
		}
		return id - 4007000;
	}

	public static string CubeName(int id)
	{
		if (!IsCube(id))
		{
			return "未知魔方";
		}
		return Names[id - 4007000];
	}

	public static string Raw(InventoryEntry equip)
	{
		if (equip?.equipmentData == null || !equip.equipmentData.TryGetValue("expandAttribute4", out var value))
		{
			return "";
		}
		return Convert.ToString(value);
	}

	private static int Num(Dictionary<string, object> row, string key)
	{
		if (!row.TryGetValue(key, out var value))
		{
			return 0;
		}
		return Convert.ToInt32(value);
	}

	private static Data Read(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return null;
		}
		try
		{
			if (!(MiniJsonDb.Parse(raw) is Dictionary<string, object> dictionary))
			{
				throw new ArgumentException();
			}
			int num = Num(dictionary, "v");
			int num2 = Num(dictionary, "cube");
			Index(num2);
			if (num != 1 && num != 2)
			{
				throw new ArgumentException();
			}
			if (!dictionary.TryGetValue("lines", out var value) || !(value is List<object> list))
			{
				throw new ArgumentException();
			}
			int num3 = ((num != 1) ? Num(dictionary, "grade") : ((num2 <= 4007001) ? 1 : ((num2 <= 4007004) ? 2 : 3)));
			if (num3 < 1 || num3 > 4 || list.Count != num3 switch
			{
				2 => 2, 
				1 => 1, 
				_ => 3, 
			})
			{
				throw new ArgumentException();
			}
			Data data = new Data
			{
				cube = num2,
				grade = num3
			};
			foreach (object item in list)
			{
				Dictionary<string, object> dictionary2 = item as Dictionary<string, object>;
				int num4 = ((dictionary2 != null && dictionary2.TryGetValue("stat", out var value2)) ? Array.IndexOf(Stats, Convert.ToString(value2)) : (-1));
				int num5 = ((dictionary2 != null) ? Num(dictionary2, "value") : 0);
				if (num4 < 0 || num5 <= 0 || num5 > 10000)
				{
					throw new ArgumentException();
				}
				data.lines.Add(new KeyValuePair<int, int>(num4, num5));
			}
			return data;
		}
		catch (Exception innerException)
		{
			throw new ArgumentException("无法解析装备魔方数据", innerException);
		}
	}

	private static string Write(Data data)
	{
		StringBuilder stringBuilder = new StringBuilder("{\"v\":2,\"cube\":").Append(data.cube).Append(",\"grade\":").Append(data.grade)
			.Append(",\"lines\":[");
		for (int i = 0; i < data.lines.Count; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append(',');
			}
			KeyValuePair<int, int> keyValuePair = data.lines[i];
			stringBuilder.Append("{\"stat\":\"").Append(Stats[keyValuePair.Key]).Append("\",\"value\":")
				.Append(keyValuePair.Value)
				.Append('}');
		}
		return stringBuilder.Append("]}").ToString();
	}

	private static string Describe(Data data)
	{
		if (data == null)
		{
			return "潜能强度：特殊；尚未拥有魔方词条";
		}
		StringBuilder stringBuilder = new StringBuilder("潜能强度：").Append(Grades[data.grade - 1]).Append('；').Append(CubeName(data.cube))
			.Append('：');
		for (int i = 0; i < data.lines.Count; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append('；');
			}
			KeyValuePair<int, int> keyValuePair = data.lines[i];
			stringBuilder.Append(Labels[keyValuePair.Key]).Append('+').Append(keyValuePair.Value);
		}
		return stringBuilder.ToString();
	}

	public static string Describe(InventoryEntry equip)
	{
		if (equip == null)
		{
			return "无装备";
		}
		try
		{
			return Describe(Read(Raw(equip)));
		}
		catch
		{
			return "魔方数据异常，请联系管理员";
		}
	}

	public static bool CanRoll(InventoryEntry equip)
	{
		if (equip == null)
		{
			return false;
		}
		try
		{
			Read(Raw(equip));
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool CanUse(InventoryEntry equip, int cube)
	{
		if (CanRoll(equip))
		{
			return (Read(Raw(equip))?.grade ?? 1) <= MaxGrades[Index(cube)];
		}
		return false;
	}

	public static string Summary(InventoryEntry equip, int cube)
	{
		int num = Index(cube);
		int num2 = Read(Raw(equip))?.grade ?? 1;
		if (num2 > MaxGrades[num])
		{
			return "最高" + Grades[MaxGrades[num] - 1] + "，当前强度不可使用";
		}
		int num3 = ((num2 < MaxGrades[num]) ? Rates[num, num2 - 1] : 0);
		return "最高" + Grades[MaxGrades[num] - 1] + "，" + ((num3 == 0) ? "已达该魔方上限" : ("当前升阶率 " + num3 + "%")) + ((num == 7) ? "，可保留原词条" : "");
	}

	public static string Summary(int cube)
	{
		int num = Index(cube);
		return "最高" + Grades[MaxGrades[num] - 1] + "，升阶率 " + Rates[num, 0] + "%/" + Rates[num, 1] + "%/" + Rates[num, 2] + "%" + ((num == 7) ? "，可保留原词条" : "");
	}

	public static Roll CreateRoll(InventoryEntry equip, int cube, Func<int, int> next = null)
	{
		if (equip == null)
		{
			throw new ArgumentException("装备不存在");
		}
		next = next ?? new Func<int, int>(Random.Next);
		int num = Index(cube);
		int num2 = Read(Raw(equip))?.grade ?? 1;
		if (num2 > MaxGrades[num])
		{
			throw new ArgumentException(Names[num] + "最高只能洗练" + Grades[MaxGrades[num] - 1] + "强度的装备");
		}
		int num3 = ((num2 < MaxGrades[num]) ? Rates[num, num2 - 1] : 0);
		bool flag = next(100) < num3;
		Data data = new Data
		{
			cube = cube,
			grade = (flag ? Math.Min(num2 + 1, 4) : num2)
		};
		List<int> list = new List<int>();
		if (equip.itemId >= 1302000 && equip.itemId < 1493000)
		{
			list.AddRange(Weapon);
		}
		else if (MaxGrades[num] == 4)
		{
			for (int i = 0; i < Stats.Length; i++)
			{
				list.Add(i);
			}
		}
		else
		{
			list.AddRange(Armor);
		}
		int num4 = Powers[data.grade - 1];
		int num5 = ((data.grade == 1) ? 1 : ((data.grade == 2) ? 2 : 3));
		for (int j = 0; j < num5; j++)
		{
			int index = next(list.Count);
			int num6 = list[index];
			list.RemoveAt(index);
			int num7;
			int num8;
			if (num6 == 6 || num6 == 7)
			{
				num7 = num4 * 10;
				num8 = num4 * 20;
			}
			else if (num6 == 8 || num6 == 9)
			{
				num7 = num4 * 4;
				num8 = num4 * 8;
			}
			else if (num6 == 4 || num6 == 5)
			{
				num7 = Math.Max(1, (num4 + 1) / 2);
				num8 = num4;
			}
			else if (num6 >= 10)
			{
				num7 = Math.Max(1, num4 / 2);
				num8 = num4;
			}
			else
			{
				num7 = num4;
				num8 = num4 * 2;
			}
			data.lines.Add(new KeyValuePair<int, int>(num6, num7 + next(num8 - num7 + 1)));
		}
		return new Roll
		{
			itemId = equip.itemId,
			cubeItemId = cube,
			grade = data.grade,
			data = Write(data),
			description = Describe(data) + (flag ? "（升阶成功）" : "（强度未提升）"),
			rankedUp = flag,
			canKeepOld = (num == 7)
		};
	}

	public static void Apply(InventoryEntry equip, Roll roll)
	{
		if (equip == null || roll == null || equip.itemId != roll.itemId)
		{
			throw new ArgumentException("装备已变化，不能应用该魔方结果");
		}
		Data data = Read(roll.data);
		if (data == null || data.cube != roll.cubeItemId || data.grade != roll.grade)
		{
			throw new ArgumentException("魔方结果无效");
		}
		ApplyData(equip, data);
	}

	public static void Inherit(InventoryEntry source, InventoryEntry target)
	{
		if (source != null && target != null)
		{
			ApplyData(target, Read(Raw(source)));
		}
	}

	private static int[] Totals(Data data)
	{
		int[] array = new int[Stats.Length];
		if (data != null)
		{
			foreach (KeyValuePair<int, int> line in data.lines)
			{
				array[line.Key] += line.Value;
			}
		}
		return array;
	}

	private static void ApplyData(InventoryEntry equip, Data data)
	{
		int[] array = Totals(Read(Raw(equip)));
		int[] array2 = Totals(data);
		int[] array3 = new int[14]
		{
			equip.str, equip.dex, equip.int_, equip.luk, equip.watk, equip.matk, equip.hp, equip.mp, equip.wdef, equip.mdef,
			equip.acc, equip.avoid, equip.speed, equip.jump
		};
		for (int i = 0; i < array3.Length; i++)
		{
			array3[i] = array3[i] - array[i] + array2[i];
			if (array3[i] < 0 || array3[i] > 32767)
			{
				throw new InvalidOperationException("魔方属性超出装备属性范围");
			}
		}
		equip.str = array3[0];
		equip.dex = array3[1];
		equip.int_ = array3[2];
		equip.luk = array3[3];
		equip.watk = array3[4];
		equip.matk = array3[5];
		equip.hp = array3[6];
		equip.mp = array3[7];
		equip.wdef = array3[8];
		equip.mdef = array3[9];
		equip.acc = array3[10];
		equip.avoid = array3[11];
		equip.speed = array3[12];
		equip.jump = array3[13];
		if (equip.equipmentData == null)
		{
			equip.equipmentData = new Dictionary<string, object>();
		}
		equip.equipmentData["expandAttribute4"] = ((data == null) ? "" : Write(data));
	}
}
