namespace Server.Bot.Events;

public static class EventFactory
{
	public static GameEvent Gachapon(BotActor actor, InventoryEntry item)
	{
		return new GameEvent(actor, EventType.GACHAPON_REWARD, null, item);
	}

	public static GameEvent LevelUp(BotActor actor)
	{
		return new GameEvent(actor, EventType.LEVEL_UP);
	}

	public static GameEvent Scroll(BotActor actor, InventoryEntry item, bool success)
	{
		return new GameEvent(actor, EventType.SCROLLING, null, item, success);
	}

	public static GameEvent ItemMegaphone(BotActor actor, InventoryEntry item, string message)
	{
		return new GameEvent(actor, EventType.ITEM_MEGAPHONE, message, item);
	}

	public static GameEvent ChatMegaphone(BotActor actor, string message)
	{
		return new GameEvent(actor, EventType.CHAT_MEGAPHONE, message);
	}

	public static GameEvent ChatGeneral(BotActor actor, string message)
	{
		return new GameEvent(actor, EventType.CHAT_GENERAL, message);
	}
}
