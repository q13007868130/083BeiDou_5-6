using System.Collections.Generic;

public sealed class VMatrixState
{
	public long points;

	public int revision;

	public readonly Dictionary<int, VMatrixCoreView> cores = new Dictionary<int, VMatrixCoreView>();

	public VMatrixCoreView Find(int coreId)
	{
		if (!cores.TryGetValue(coreId, out var value))
		{
			return null;
		}
		return value;
	}

	public int LevelOf(int coreId)
	{
		return Find(coreId)?.level ?? 0;
	}
}
