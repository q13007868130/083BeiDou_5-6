using System.Collections.Generic;

public sealed class SoulResponse
{
	public bool ok = true;

	public bool success;

	public string error = "";

	public string optionKey = "";

	public int socketId;

	public int soulId;

	public int soulItemId;

	public int optionCode;

	public int optionValue;

	public int soulMp;

	public int soulMax;

	public List<InventoryEntry> inventory;

	public SoulWeaponWz2State modern;
}
