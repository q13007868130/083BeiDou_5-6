using System.Collections.Generic;

public sealed class HyperStatResponse
{
	public bool ok = true;

	public string error = "";

	public int preset;

	public int activePreset;

	public int totalSp;

	public int spentSp;

	public int availableSp;

	public int neededSp;

	public int level;

	public long meso;

	public long resetCost;

	public long switchCost;

	public List<HyperStatLine> lines = new List<HyperStatLine>();
}
