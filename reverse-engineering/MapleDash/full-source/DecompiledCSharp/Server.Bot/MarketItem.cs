namespace Server.Bot;

public sealed class MarketItem
{
	public int itemId;

	public int price;

	public int quantity;

	public MarketItem(int itemId, int price = -1, int quantity = 1)
	{
		this.itemId = itemId;
		this.price = price;
		this.quantity = quantity;
	}
}
