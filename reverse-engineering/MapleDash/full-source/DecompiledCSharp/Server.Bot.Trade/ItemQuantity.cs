namespace Server.Bot.Trade;

public readonly struct ItemQuantity(int itemId, int quantity = 1)
{
	public readonly int itemId = itemId;

	public readonly int quantity = ((quantity <= 0) ? 1 : quantity);
}
