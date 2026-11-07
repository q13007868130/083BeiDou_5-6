using System.Collections.Generic;

public sealed class EquipPatchResponse
{
	public bool ok = true;

	public string error = "";

	public string scrollResult;

	public int rewardItemId;

	public int rewardQuantity;

	public List<InventoryEntry> inventory;
}
