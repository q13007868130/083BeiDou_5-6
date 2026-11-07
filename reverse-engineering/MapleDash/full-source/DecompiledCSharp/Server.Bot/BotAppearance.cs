using System.Collections.Generic;

namespace Server.Bot;

public static class BotAppearance
{
	public static string[] CharacterIds(BotCharacter actor)
	{
		SortedDictionary<int, int> sortedDictionary = new SortedDictionary<int, int>();
		int num = 0;
		foreach (KeyValuePair<int, int> equip in actor.equips)
		{
			int num2 = -equip.Key;
			switch (num2)
			{
			case 111:
				num = equip.Value;
				break;
			case 1:
			case 2:
			case 3:
			case 4:
			case 5:
			case 6:
			case 7:
			case 8:
			case 9:
			case 10:
			case 11:
			case 12:
			case 13:
			case 14:
			case 15:
			case 16:
			case 17:
			case 18:
			case 19:
			case 20:
			case 21:
			case 22:
			case 23:
			case 24:
			case 25:
			case 26:
			case 27:
			case 28:
			case 29:
			case 30:
			case 31:
			case 32:
			case 33:
			case 34:
			case 35:
			case 36:
			case 37:
			case 38:
			case 39:
			case 40:
			case 41:
			case 42:
			case 43:
			case 44:
			case 45:
			case 46:
			case 47:
			case 48:
			case 49:
			case 50:
			case 51:
			case 52:
			case 53:
			case 54:
			case 55:
			case 56:
			case 57:
			case 58:
			case 59:
			case 60:
			case 61:
			case 62:
			case 63:
			case 64:
			case 65:
			case 66:
			case 67:
			case 68:
			case 69:
			case 70:
			case 71:
			case 72:
			case 73:
			case 74:
			case 75:
			case 76:
			case 77:
			case 78:
			case 79:
			case 80:
			case 81:
			case 82:
			case 83:
			case 84:
			case 85:
			case 86:
			case 87:
			case 88:
			case 89:
			case 90:
			case 91:
			case 92:
			case 93:
			case 94:
			case 95:
			case 96:
			case 97:
			case 98:
			case 99:
				if (!sortedDictionary.ContainsKey(num2))
				{
					sortedDictionary.Add(num2, equip.Value);
				}
				break;
			}
		}
		foreach (KeyValuePair<int, int> equip2 in actor.equips)
		{
			int num3 = -equip2.Key;
			if (num3 > 100 && num3 != 111)
			{
				sortedDictionary[num3 - 100] = equip2.Value;
			}
		}
		List<string> list = new List<string>
		{
			(2000 + actor.skinColor).ToString("D8"),
			(12000 + actor.skinColor).ToString("D8"),
			actor.face.ToString("D8"),
			actor.hair.ToString("D8")
		};
		int num4 = 0;
		foreach (KeyValuePair<int, int> item in sortedDictionary)
		{
			int num5 = item.Value / 10000;
			if (item.Key == 11)
			{
				num4 = item.Value;
			}
			else if (num5 == 100 || num5 == 104 || num5 == 105 || num5 == 106 || num5 == 107 || num5 == 108 || num5 == 109 || num5 == 110)
			{
				list.Add(item.Value.ToString("D8"));
			}
		}
		if (num > 0)
		{
			num4 = num;
		}
		if (num4 > 0)
		{
			list.Add(num4.ToString("D8"));
		}
		return list.ToArray();
	}
}
