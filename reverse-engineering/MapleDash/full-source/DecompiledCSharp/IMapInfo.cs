public interface IMapInfo
{
	bool IsTown(int mapId);

	int ReturnMap(int mapId);

	bool NoExpDecrease(int mapId);
}
