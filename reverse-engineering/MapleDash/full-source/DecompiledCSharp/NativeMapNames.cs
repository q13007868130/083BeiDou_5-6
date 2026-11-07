using System.Collections.Generic;
using MapleDash.Wz;

public static class NativeMapNames
{
	private const string Source = "String/Map.img";

	private static readonly Dictionary<int, string> cache = new Dictionary<int, string>();

	private static bool loaded;

	private static ImgNode cachedRoot;

	public static string Of(int mapId)
	{
		if (mapId < 0)
		{
			return "";
		}
		if (cache.TryGetValue(mapId, out var value))
		{
			return value;
		}
		value = Lookup(mapId);
		cache[mapId] = value;
		return value;
	}

	private static string Lookup(int mapId)
	{
		ImgNode imgNode = Root();
		if (imgNode == null)
		{
			return "";
		}
		string path = mapId.ToString();
		foreach (ImgNode child in imgNode.Children)
		{
			ImgNode imgNode2 = child.Get(path);
			if (imgNode2 != null)
			{
				string text = imgNode2.Text("mapName");
				if (text != "")
				{
					return text;
				}
			}
		}
		return "";
	}

	private static ImgNode Root()
	{
		if (loaded)
		{
			return cachedRoot;
		}
		loaded = true;
		try
		{
			cachedRoot = WzAssets.FileRoot("String/Map.img");
		}
		catch
		{
			cachedRoot = null;
		}
		return cachedRoot;
	}

	public static void Reset()
	{
		cache.Clear();
		loaded = false;
		cachedRoot = null;
	}
}
