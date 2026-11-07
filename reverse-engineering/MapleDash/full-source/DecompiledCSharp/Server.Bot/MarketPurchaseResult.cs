namespace Server.Bot;

public enum MarketPurchaseResult
{
	Success,
	Unavailable,
	InvalidQuantity,
	InsufficientMeso,
	InventoryFull,
	OwnerMesoFull
}
