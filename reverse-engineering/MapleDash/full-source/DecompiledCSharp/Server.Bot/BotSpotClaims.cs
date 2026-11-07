using System;
using System.Collections.Generic;

namespace Server.Bot;

public sealed class BotSpotClaims
{
	private readonly Dictionary<int, Dictionary<int, Dictionary<int, int>>> claims = new Dictionary<int, Dictionary<int, Dictionary<int, int>>>();

	public int Claim(int mapId, int spotId, int capacity, int botId)
	{
		if (!claims.TryGetValue(mapId, out var value))
		{
			value = (claims[mapId] = new Dictionary<int, Dictionary<int, int>>());
		}
		if (!value.TryGetValue(spotId, out var value2))
		{
			value2 = (value[spotId] = new Dictionary<int, int>());
		}
		if (value2.TryGetValue(botId, out var value3))
		{
			return value3;
		}
		int num = Math.Max(1, capacity);
		if (value2.Count >= num)
		{
			return -1;
		}
		HashSet<int> hashSet = new HashSet<int>(value2.Values);
		for (int i = 0; i < num; i++)
		{
			if (!hashSet.Contains(i))
			{
				value2[botId] = i;
				return i;
			}
		}
		return -1;
	}

	public void Release(int mapId, int spotId, int botId)
	{
		if (claims.TryGetValue(mapId, out var value) && value.TryGetValue(spotId, out var value2))
		{
			value2.Remove(botId);
			if (value2.Count == 0)
			{
				value.Remove(spotId);
			}
			if (value.Count == 0)
			{
				claims.Remove(mapId);
			}
		}
	}

	public int Holders(int mapId, int spotId)
	{
		if (!claims.TryGetValue(mapId, out var value) || !value.TryGetValue(spotId, out var value2))
		{
			return 0;
		}
		return value2.Count;
	}

	public static (int minX, int maxX) Section(int minX, int maxX, int slot, int capacity)
	{
		int num = Math.Max(1, capacity);
		int num2 = Math.Max(0, Math.Min(slot, num - 1));
		int num3 = Math.Max(0, maxX - minX);
		return (minX: minX + num3 * num2 / num, maxX: minX + num3 * (num2 + 1) / num);
	}
}
