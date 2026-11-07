using MapleDash.Wz;

public static class NativeCommunityMaps
{
	private static ImgNode Info(int id)
	{
		return WzAssets.Node("Map/Map/Map" + id / 100000000 + "/" + id.ToString("D9") + ".img/info");
	}

	public static bool CanEnterCash(int map)
	{
		ImgNode imgNode = Info(map);
		if (imgNode != null)
		{
			return (imgNode.Int("fieldLimit") & 0x10) == 0;
		}
		return false;
	}

	public static bool CanTravel(int from, int to)
	{
		ImgNode imgNode = Info(from);
		ImgNode imgNode2 = Info(to);
		if (imgNode == null || imgNode2 == null)
		{
			return false;
		}
		if ((imgNode.Int("fieldLimit") & 0x10) == 0 && (imgNode2.Int("fieldLimit") & 0x40) == 0)
		{
			if (imgNode2.Int("forcedReturn", 999999999) != 999999999)
			{
				return to / 100000000 == 0;
			}
			return true;
		}
		return false;
	}
}
