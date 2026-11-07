using System.Collections.Generic;

public sealed class PotentialResponse
{
	public bool ok = true;

	public bool pending;

	public string error = "";

	public string scrollResult = "";

	public int revision;

	public int grade;

	public int[] main;

	public int[] bonus;

	public int[] candidate;

	public long cost;

	public long meso;

	public List<InventoryEntry> inventory;
}
