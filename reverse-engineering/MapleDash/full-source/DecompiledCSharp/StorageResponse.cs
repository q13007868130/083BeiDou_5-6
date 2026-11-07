using System.Collections.Generic;

public sealed class StorageResponse
{
	public bool ok = true;

	public string error = "";

	public byte code;

	public StorageSnapshot storage;

	public List<InventoryEntry> inventory;

	public long meso = -1L;
}
