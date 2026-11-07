using System.Collections.Generic;

public sealed class MerchantResponse
{
	public bool ok = true;

	public string error = "";

	public byte code;

	public bool box;

	public MerchantSnapshot shop;

	public List<InventoryEntry> inventory;

	public long meso = -1L;

	public long merchantMeso = -1L;
}
