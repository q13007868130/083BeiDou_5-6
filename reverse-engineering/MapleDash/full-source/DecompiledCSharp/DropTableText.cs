using System.Collections.Generic;

public static class DropTableText
{
	public static bool TryParse(string text, out Dictionary<int, List<DropEntry>> mobs, out List<DropEntry> global)
	{
		mobs = new Dictionary<int, List<DropEntry>>();
		global = new List<DropEntry>();
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		int num = 0;
		string[] array = text.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i].Trim();
			if (text2.Length < 13 || text2[0] == '#')
			{
				continue;
			}
			string[] array2 = text2.Split('|');
			if (array2.Length < 7)
			{
				continue;
			}
			bool flag = array2[0] == "G";
			if ((!flag && array2[0] != "M") || !int.TryParse(array2[2], out var result) || !int.TryParse(array2[3], out var result2) || !int.TryParse(array2[4], out var result3) || !int.TryParse(array2[5], out var result4) || !int.TryParse(array2[6], out var result5))
			{
				continue;
			}
			DropEntry dropEntry = new DropEntry
			{
				item = result,
				min = result2,
				max = result3,
				quest = result4,
				chance = result5
			};
			int result7;
			if (flag)
			{
				dropEntry.continent = (int.TryParse(array2[1], out var result6) ? result6 : (-1));
				global.Add(dropEntry);
				num++;
			}
			else if (int.TryParse(array2[1], out result7))
			{
				if (!mobs.TryGetValue(result7, out var value))
				{
					value = (mobs[result7] = new List<DropEntry>());
				}
				value.Add(dropEntry);
				num++;
			}
		}
		if (num > 0)
		{
			return mobs.Count > 0;
		}
		return false;
	}
}
