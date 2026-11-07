public sealed class FieldMapInfo : IMapInfo, IRespawnMapInfo
{
	public int ClosestSpawn(int mapId, int x, int y)
	{
		FieldMap map = FieldCatalog.GetMap(mapId);
		int result = 0;
		double num = double.PositiveInfinity;
		if (map == null)
		{
			return result;
		}
		for (int i = 0; i < map.portals.Count; i++)
		{
			FieldPortal fieldPortal = map.portals[i];
			double num2 = fieldPortal.x - (float)x;
			double num3 = fieldPortal.y - (float)y;
			double num4 = num2 * num2 + num3 * num3;
			if (fieldPortal.type >= 0 && fieldPortal.type <= 1 && fieldPortal.tm == 999999999 && num4 < num)
			{
				result = i;
				num = num4;
			}
		}
		return result;
	}

	public bool IsTown(int mapId)
	{
		FieldCatalog.Ensure();
		if (FieldCatalog.Maps.TryGetValue(mapId, out var value))
		{
			return value.town;
		}
		return false;
	}

	public int ReturnMap(int mapId)
	{
		FieldCatalog.Ensure();
		if (!FieldCatalog.Maps.TryGetValue(mapId, out var value))
		{
			return 999999999;
		}
		return value.returnMap;
	}

	public bool NoExpDecrease(int mapId)
	{
		FieldCatalog.Ensure();
		if (!FieldCatalog.Maps.TryGetValue(mapId, out var value))
		{
			return false;
		}
		return (value.fieldLimit & 0x10) != 0;
	}
}
