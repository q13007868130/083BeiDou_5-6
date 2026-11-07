using System.Collections.Generic;

public sealed class StarForceResponse
{
	public bool ok = true;

	public bool restored;

	public string error = "";

	public StarForceRules.Quote quote;

	public StarForceRules.Result result;

	public int revision;

	public int cap;

	public long meso;

	public Dictionary<string, int> derivedStats;

	public Dictionary<string, int> nextStats;

	public List<InventoryEntry> inventory;
}
