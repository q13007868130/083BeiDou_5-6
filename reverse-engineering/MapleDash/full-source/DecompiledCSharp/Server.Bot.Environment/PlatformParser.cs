using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Server.Bot.Environment;

public sealed class PlatformParser
{
	public const string ResourcePath = "soloMapling/ArtificialPlayer/BotMovementSystem/movementDataPackets";

	private readonly Func<string, string> readText;

	private readonly Func<string, IEnumerable<string>> listStems;

	private readonly Dictionary<string, Platform> cache = new Dictionary<string, Platform>();

	public PlatformParser(Func<string, string> readText, Func<string, IEnumerable<string>> listStems)
	{
		this.readText = readText;
		this.listStems = listStems;
	}

	public List<BotPoint> ParseCoordinates(int mapId, string fileName)
	{
		List<BotPoint> list = ReadCoordinates(readText?.Invoke("soloMapling/ArtificialPlayer/BotMovementSystem/movementDataPackets/map" + mapId + "/" + fileName + ".csv"));
		if (list.Count != 0)
		{
			return list;
		}
		return FallbackCoordinates(mapId, fileName);
	}

	public Platform ParsePlatform(int mapId, string fileName)
	{
		string key = mapId + ":" + fileName;
		if (!cache.TryGetValue(key, out var value))
		{
			value = (cache[key] = OrganizePlatform(ParseCoordinates(mapId, fileName)));
		}
		return value;
	}

	public Platform ParsePlatform(int mapId, string fileName, Platform.Type type)
	{
		return OrganizePlatform(ParseCoordinates(mapId, fileName), type);
	}

	public List<string> ListPlatformIds(int mapId)
	{
		List<string> list = (listStems?.Invoke("soloMapling/ArtificialPlayer/BotMovementSystem/movementDataPackets/map" + mapId) ?? Array.Empty<string>()).Where((string s) => !s.Contains("/") && s != "depreciated").ToList();
		if (list.Count == 0 && mapId == 910000000)
		{
			list.AddRange(new string[15]
			{
				"m1", "m2", "m3", "m4", "m5", "c1-2a", "c1-2b", "c1-5a", "c2-1b", "c2-3a",
				"c3-2a", "c3-4a", "c3-4b", "c4-3b", "c5-1a"
			});
		}
		return list;
	}

	public static List<BotPoint> ReadCoordinates(string text)
	{
		List<BotPoint> list = new List<BotPoint>();
		if (text == null)
		{
			return list;
		}
		using StringReader stringReader = new StringReader(text);
		string text2;
		while ((text2 = stringReader.ReadLine()) != null)
		{
			text2 = text2.Trim();
			if (text2.Length != 0 && text2.Contains(","))
			{
				string[] array = text2.Split(',');
				if (array.Length >= 3 && int.TryParse(array[1].Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result) && int.TryParse(array[2].Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result2))
				{
					list.Add(new BotPoint(result, result2));
				}
			}
		}
		return list;
	}

	public static Platform OrganizePlatform(List<BotPoint> coordinates)
	{
		Platform.Type type = ((coordinates != null && coordinates.Count > 0 && coordinates.Max((BotPoint p) => p.y) - coordinates.Min((BotPoint p) => p.y) > 50) ? Platform.Type.SLOPED : Platform.Type.FLAT);
		return OrganizePlatform(coordinates, type);
	}

	public static Platform OrganizePlatform(List<BotPoint> coordinates, Platform.Type type)
	{
		if (coordinates == null || coordinates.Count == 0)
		{
			return new Platform(0, 0, 0, new List<BotPoint>(), type);
		}
		List<BotPoint> list = (from p in coordinates.Distinct()
			orderby p.x
			select p).ToList();
		int baseY = ((type == Platform.Type.FLAT) ? ModeY(coordinates) : ((int)coordinates.Average((BotPoint p) => p.y)));
		return new Platform(list[0].x, list[list.Count - 1].x, baseY, list, type);
	}

	private static int ModeY(List<BotPoint> points)
	{
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		foreach (BotPoint point in points)
		{
			dictionary.TryGetValue(point.y, out var value);
			dictionary[point.y] = value + 1;
		}
		int capacity;
		for (capacity = 16; dictionary.Count > capacity * 3 / 4; capacity *= 2)
		{
		}
		int result = 0;
		int num = -1;
		foreach (KeyValuePair<int, int> item in dictionary.OrderBy((KeyValuePair<int, int> p) => (p.Key ^ (p.Key >>> 16)) & (capacity - 1)))
		{
			if (item.Value > num)
			{
				result = item.Key;
				num = item.Value;
			}
		}
		return result;
	}

	private static List<BotPoint> FallbackCoordinates(int mapId, string fileName)
	{
		List<BotPoint> list = new List<BotPoint>();
		if (mapId != 910000000)
		{
			return list;
		}
		int num;
		int num2;
		int y;
		switch (fileName)
		{
		case "m1":
			num = -400;
			num2 = 1400;
			y = 34;
			break;
		case "m2":
			num = -400;
			num2 = 1400;
			y = -266;
			break;
		case "m5":
			num = -350;
			num2 = 500;
			y = 34;
			break;
		default:
			return list;
		}
		for (int i = num; i <= num2; i += 80)
		{
			list.Add(new BotPoint(i, y));
		}
		return list;
	}
}
