using System.Collections.Generic;

public sealed class SymbolResponse
{
	public bool ok = true;

	public string error = "";

	public int itemId;

	public int level;

	public int exp;

	public int growthCost;

	public int inc;

	public int maxLevel;

	public long mesoCost;

	public long meso;

	public bool canLevelUp;

	public bool equipped;

	public int mergedCount;

	public List<InventoryEntry> inventory;
}
