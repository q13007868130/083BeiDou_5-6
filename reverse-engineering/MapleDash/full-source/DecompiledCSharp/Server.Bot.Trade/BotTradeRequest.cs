namespace Server.Bot.Trade;

public sealed class BotTradeRequest
{
	public int characterId;

	public int botId;

	public int inventoryType;

	public int inventoryPosition;

	public int quantity;

	public int slot;

	public long meso;

	public string text;

	public BotTradeOperation operation;
}
