using System.Collections.Generic;

namespace Server.Field;

public static class MapService
{
	private static readonly Dictionary<int, MapRuntime> maps = new Dictionary<int, MapRuntime>();

	public static void Register(MapRuntime rt)
	{
		if (rt != null)
		{
			maps[rt.mapId] = rt;
		}
	}

	public static void Unregister(int mapId)
	{
		maps.Remove(mapId);
	}

	public static bool Has(int mapId)
	{
		return maps.ContainsKey(mapId);
	}

	public static MapRuntime Get(int mapId)
	{
		if (!maps.TryGetValue(mapId, out var value))
		{
			return null;
		}
		return value;
	}

	public static float SnapToGround(int mapId, float x, float y)
	{
		MapRuntime mapRuntime = Get(mapId);
		if (mapRuntime == null || mapRuntime.footholds.Count == 0)
		{
			return y;
		}
		float? num = mapRuntime.SnapToGroundRaw(x, y);
		if (!num.HasValue)
		{
			return y;
		}
		return num.Value - 1f;
	}
}
