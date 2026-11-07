using System.Collections.Generic;

public sealed class VMatrixResponse
{
	public bool ok = true;

	public string error = "";

	public long points;

	public long meso;

	public int level;

	public int job;

	public int revision;

	public int maxPoints = 9999;

	public bool eligible;

	public int resetMeso;

	public int transformCost;

	public int transformItem;

	public int transformMinutes;

	public int registerCost;

	public List<VMatrixCoreView> cores = new List<VMatrixCoreView>();

	public int[] slots = new int[31];
}
