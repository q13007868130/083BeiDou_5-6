namespace Server.Bot.Events;

public sealed class GameEvent
{
	private static int nextId = 1;

	public readonly int id;

	public readonly int world;

	public readonly int channel;

	public readonly int mapId;

	public readonly int playerId;

	public readonly long timestamp;

	public readonly BotActor actor;

	public readonly string playerName;

	public readonly string message;

	public readonly EventType type;

	public readonly InventoryEntry item;

	public readonly bool? pass;

	public GameEvent(BotActor actor, EventType type, string message = null, InventoryEntry item = null, bool? pass = null)
	{
		id = nextId++;
		timestamp = BotScheduler.Now();
		this.actor = actor;
		world = actor.world;
		channel = actor.channel;
		mapId = actor.mapId;
		playerName = actor.name;
		playerId = actor.id;
		this.type = type;
		this.message = message;
		this.item = item;
		this.pass = pass;
	}
}
