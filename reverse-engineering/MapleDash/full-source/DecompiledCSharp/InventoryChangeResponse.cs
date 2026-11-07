using System.Collections.Generic;

public sealed class InventoryChangeResponse
{
	public int extraStorageQuantity;

	public bool ok = true;

	public string error = "";

	public string message;

	public InventoryEntry pet;

	public List<int> petExclusions;

	public long damageCap;

	public int bankedExp;

	public int createdPetId;

	public bool inventoryChanged;

	public List<InventoryEntry> inventory;

	public long meso;
}
