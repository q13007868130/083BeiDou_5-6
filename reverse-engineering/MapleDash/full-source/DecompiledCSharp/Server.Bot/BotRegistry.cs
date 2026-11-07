using System.Collections.Generic;
using Server.Bot.Environment;

namespace Server.Bot;

public static class BotRegistry
{
	private static readonly Dictionary<int, BotCharacter> byId = new Dictionary<int, BotCharacter>();

	private static readonly Dictionary<int, List<int>> byMap = new Dictionary<int, List<int>>();

	public static void Add(BotCharacter c)
	{
		byId[c.id] = c;
	}

	public static void EnterMap(BotCharacter c, int mapId)
	{
		LeaveMap(c);
		c.mapId = mapId;
		c.MarkMapEntry();
		if (!byMap.TryGetValue(mapId, out var value))
		{
			value = new List<int>();
			byMap[mapId] = value;
		}
		if (!value.Contains(c.id))
		{
			value.Add(c.id);
		}
	}

	public static void LeaveMap(BotCharacter c)
	{
		BotRecordedActions.Cancel(c);
		c.arrivalTask?.Cancel();
		BotMovementPlayback.Cancel(c);
		if (byMap.TryGetValue(c.mapId, out var value))
		{
			value.Remove(c.id);
			if (value.Count == 0)
			{
				byMap.Remove(c.mapId);
			}
		}
	}

	public static void Remove(BotCharacter c)
	{
		BotMovementService.Disable(c);
		byId.Remove(c.id);
		LeaveMap(c);
	}

	public static void Remove(int id)
	{
		if (byId.TryGetValue(id, out var value))
		{
			Remove(value);
		}
	}

	public static BotCharacter Get(int id)
	{
		if (!byId.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public static bool Contains(int id)
	{
		return byId.ContainsKey(id);
	}

	public static List<BotCharacter> OnMap(int mapId)
	{
		List<BotCharacter> list = new List<BotCharacter>();
		if (byMap.TryGetValue(mapId, out var value))
		{
			foreach (int item in value)
			{
				if (byId.TryGetValue(item, out var value2))
				{
					list.Add(value2);
				}
			}
		}
		return list;
	}

	public static IReadOnlyDictionary<int, BotCharacter> All()
	{
		return byId;
	}

	public static int Count()
	{
		return byId.Count;
	}
}
