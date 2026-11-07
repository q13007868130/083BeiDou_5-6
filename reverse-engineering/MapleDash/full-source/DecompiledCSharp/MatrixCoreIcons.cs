using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class MatrixCoreIcons
{
	private static Dictionary<string, string> v;

	private static Dictionary<string, string> h;

	private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

	private static bool loaded;

	public static void Invalidate()
	{
		loaded = false;
		v = null;
		h = null;
		cache.Clear();
	}

	private static void Ensure()
	{
		if (!loaded)
		{
			loaded = true;
			v = new Dictionary<string, string>();
			h = new Dictionary<string, string>();
			if (MiniJsonDb.Parse(PlatformRes.LoadText("UI/MatrixCoreIcons")) is Dictionary<string, object> root)
			{
				Read(root, "v", v);
				Read(root, "h", h);
			}
		}
	}

	private static void Read(Dictionary<string, object> root, string key, Dictionary<string, string> table)
	{
		if (!root.TryGetValue(key, out var value) || !(value is Dictionary<string, object> dictionary))
		{
			return;
		}
		foreach (KeyValuePair<string, object> item in dictionary)
		{
			table[item.Key] = item.Value as string;
		}
	}

	public static Texture2D Texture(int coreId, bool hexa)
	{
		Ensure();
		Dictionary<string, string> dictionary = (hexa ? h : v);
		if (dictionary == null || !dictionary.TryGetValue(coreId.ToString(), out var value) || string.IsNullOrEmpty(value))
		{
			return null;
		}
		if (cache.TryGetValue(value, out var value2))
		{
			return value2;
		}
		ImgNode imgNode = WzAssets.Node(value);
		Texture2D val = ((imgNode == null) ? null : WzAssets.Texture(imgNode));
		cache[value] = val;
		return val;
	}

	public static Texture2D Core(int coreId, bool hexa, int[] skills)
	{
		Texture2D val = Texture(coreId, hexa);
		if ((Object)(object)val != (Object)null)
		{
			return val;
		}
		if (skills == null || skills.Length == 0)
		{
			return null;
		}
		return WzAssets.Texture("skillicon/" + skills[0]);
	}
}
