namespace Server.Field;

public sealed class MapReactorSpawn
{
	public int id;

	public int x;

	public int y;

	public int reactorTime = -1;

	public string name = "";

	public int maxState = 4;

	public int RespawnSeconds
	{
		get
		{
			if (reactorTime <= 0)
			{
				return 0;
			}
			return reactorTime;
		}
	}
}
