using System.Collections.Generic;

public sealed class StorageSnapshot
{
	public int npcId;

	public int slots;

	public long meso;

	public readonly List<StorageSlotData> items = new List<StorageSlotData>();
}
