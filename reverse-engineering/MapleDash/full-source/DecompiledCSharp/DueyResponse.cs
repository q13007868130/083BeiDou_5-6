using System.Collections.Generic;

public sealed class DueyResponse
{
	public bool ok = true;

	public string error = "";

	public byte code;

	public bool quick;

	public int fee;

	public long meso = -1L;

	public List<DueyPackageData> packages;

	public List<InventoryEntry> inventory;

	public DueyPackageData pending;
}
