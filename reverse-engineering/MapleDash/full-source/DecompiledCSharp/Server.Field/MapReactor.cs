namespace Server.Field;

public sealed class MapReactor
{
	public int mapId;

	public int id;

	public int objectId;

	public int x;

	public int y;

	public string name = "";

	public int state;

	public int previousState = -1;

	public int visualRevision;

	public int eventState;

	public bool shouldCollect = true;

	public MapReactorTemplate template;

	public int maxState = 4;

	public bool alive = true;

	public int respawnSeconds;

	public long lastHitAt;

	public long respawnAt;

	public bool Broken => !alive;

	public bool Hittable
	{
		get
		{
			if (alive)
			{
				return state < maxState;
			}
			return false;
		}
	}
}
