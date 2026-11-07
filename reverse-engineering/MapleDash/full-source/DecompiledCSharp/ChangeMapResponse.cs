using System.Collections.Generic;

public sealed class ChangeMapResponse
{
	public bool ok;

	public int mapId;

	public int spawnPoint;

	public int hp;

	public int mp;

	public bool wheelUsed;

	public List<InventoryEntry> inventory;

	public bool respawned;
}
