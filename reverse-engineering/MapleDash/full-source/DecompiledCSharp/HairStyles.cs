public static class HairStyles
{
	public static readonly string[] Keys = new string[6] { "h0", "h1", "h2", "h3", "h4", "h5" };

	public static readonly string[] Names = new string[6] { "发型一", "发型二", "发型三", "发型四", "发型五", "发型六" };

	public static int Count => Keys.Length;

	public static string KeyOf(int i)
	{
		if (i < 0 || i >= Keys.Length)
		{
			return Keys[0];
		}
		return Keys[i];
	}

	public static int IndexOf(string key)
	{
		for (int i = 0; i < Keys.Length; i++)
		{
			if (Keys[i] == key)
			{
				return i;
			}
		}
		return 0;
	}

	public static bool Contains(string key)
	{
		for (int i = 0; i < Keys.Length; i++)
		{
			if (Keys[i] == key)
			{
				return true;
			}
		}
		return false;
	}

	public static string NameOf(string key)
	{
		return Names[IndexOf(key)];
	}
}
