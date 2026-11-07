namespace Server.Bot;

public sealed class MarketEquipment
{
	public readonly InventoryEntry Item;

	public readonly int Price;

	public MarketEquipment(InventoryEntry item, int price)
	{
		Item = item;
		Price = price;
	}
}
