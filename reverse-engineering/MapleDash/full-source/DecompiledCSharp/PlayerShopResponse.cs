using System.Collections.Generic;

public sealed class PlayerShopResponse
{
	public bool ok = true;

	public string error = "";

	public byte code;

	public PlayerShopInfo shop;

	public List<InventoryEntry> inventory;

	public long meso = -1L;
}
