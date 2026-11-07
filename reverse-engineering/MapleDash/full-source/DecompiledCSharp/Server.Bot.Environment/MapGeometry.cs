using Server.Field;

namespace Server.Bot.Environment;

public sealed class MapGeometry
{
	public readonly MapRuntime map;

	public readonly BotGroundIndex ground;

	public readonly BotWalkRegions regions;

	public readonly BotAirCollision collision;

	private readonly int footholdCount;

	private readonly int ropeCount;

	public bool Stale
	{
		get
		{
			if (map.footholds.Count == footholdCount)
			{
				return map.ropes.Count != ropeCount;
			}
			return true;
		}
	}

	public MapGeometry(MapRuntime map)
	{
		this.map = map;
		footholdCount = map.footholds.Count;
		ropeCount = map.ropes.Count;
		ground = new BotGroundIndex(map);
		regions = new BotWalkRegions(map);
		collision = new BotAirCollision(map, ground);
	}
}
