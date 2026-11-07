using System.Runtime.CompilerServices;
using Server.Field;

namespace Server.Bot.Environment;

public static class MapGeometryCache
{
	private static readonly ConditionalWeakTable<MapRuntime, MapGeometry> table = new ConditionalWeakTable<MapRuntime, MapGeometry>();

	public static MapGeometry Get(MapRuntime map)
	{
		if (map == null)
		{
			return null;
		}
		if (table.TryGetValue(map, out var value) && !value.Stale)
		{
			return value;
		}
		MapGeometry mapGeometry = new MapGeometry(map);
		if (table.TryGetValue(map, out var _))
		{
			table.Remove(map);
		}
		table.Add(map, mapGeometry);
		return mapGeometry;
	}
}
