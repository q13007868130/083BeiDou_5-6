using System.Collections.Generic;

public sealed class PlayerShopInfo
{
	public int ownerId;

	public int mapId;

	public int permitId;

	public string ownerName = "";

	public string description = "";

	public bool open;

	public int boughtNumber;

	public readonly List<PlayerShopSlotData> items = new List<PlayerShopSlotData>();

	public readonly List<PlayerShopSoldData> sold = new List<PlayerShopSoldData>();

	public readonly List<string> banned = new List<string>();

	public readonly List<string> chat = new List<string>();
}
