namespace Server.Bot;

public sealed class MarketRequest
{
	public int characterId;

	public int ownerId;

	public int itemIndex;

	public int quantity;

	public MarketOperation operation;

	public string text;
}
