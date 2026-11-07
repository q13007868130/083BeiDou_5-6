using System.Collections.Generic;

public sealed class SlotRemoveResponse
{
	public bool ok = true;

	public string error = "";

	public List<InventoryEntry> inventory;
}
