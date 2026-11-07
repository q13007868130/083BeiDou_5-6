using System.Collections.Generic;

public static class AndroidLook
{
	private static readonly Dictionary<int, bool> exists = new Dictionary<int, bool>();

	public static string[] Ids(AndroidState state)
	{
		if (state == null || state.templateId == 0)
		{
			return null;
		}
		int num = Skin(state);
		if (num == 0)
		{
			return null;
		}
		List<string> list = new List<string>(12)
		{
			num.ToString("D8"),
			(num + 10000).ToString("D8")
		};
		int num2 = Available(state.templateId, "face", state.face);
		if (num2 > 0)
		{
			list.Add(num2.ToString("D8"));
		}
		int num3 = Available(state.templateId, "hair", state.hair);
		if (num3 > 0)
		{
			list.Add(num3.ToString("D8"));
		}
		bool flag = state.top / 10000 == 105;
		Add(list, state.hat);
		Add(list, state.faceAccessory);
		Add(list, state.top);
		if (!flag)
		{
			Add(list, state.bottom);
		}
		Add(list, state.shoes);
		Add(list, state.gloves);
		Add(list, state.cape);
		Add(list, state.weapon);
		return list.ToArray();
	}

	public static string Key(string[] ids)
	{
		return "android_" + string.Join("_", ids);
	}

	public static bool Exists(int itemId)
	{
		if (itemId <= 0)
		{
			return false;
		}
		if (exists.TryGetValue(itemId, out var value))
		{
			return value;
		}
		string text = DirOf(itemId);
		bool flag = text != null && WzAssets.HasFile("Character/" + ((text.Length == 0) ? "" : (text + "/")) + itemId.ToString("D8") + ".img");
		exists[itemId] = flag;
		return flag;
	}

	private static string DirOf(int itemId)
	{
		switch (itemId / 10000)
		{
		case 0:
		case 1:
			return "";
		case 2:
		case 5:
			return "Face";
		case 3:
		case 4:
		case 6:
			return "Hair";
		default:
			return EquipCatalog.DirOf(itemId);
		}
	}

	private static int Skin(AndroidState state)
	{
		int num = ((state.skin >= 2000 && state.skin < 3000) ? state.skin : 2000);
		if (Exists(num) && Exists(num + 10000))
		{
			return num;
		}
		int num2 = 2000;
		if (!Exists(num2) || !Exists(num2 + 10000))
		{
			return 0;
		}
		return num2;
	}

	private static int Available(int templateId, string key, int picked)
	{
		List<int> list = NativeAndroidService.Pool(templateId, key);
		if (list == null || list.Count == 0)
		{
			if (!Exists(picked))
			{
				return 0;
			}
			return picked;
		}
		int num = list.IndexOf(picked);
		if (num < 0)
		{
			num = 0;
		}
		for (int i = 0; i < list.Count; i++)
		{
			int num2 = list[(num + i) % list.Count];
			if (Exists(num2))
			{
				return num2;
			}
		}
		if (!Exists(picked))
		{
			return 0;
		}
		return picked;
	}

	private static void Add(List<string> ids, int itemId)
	{
		if (itemId > 0 && Exists(itemId))
		{
			ids.Add(itemId.ToString("D8"));
		}
	}
}
