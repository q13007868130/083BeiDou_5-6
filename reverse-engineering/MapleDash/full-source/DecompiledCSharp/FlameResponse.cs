using System.Collections.Generic;

public sealed class FlameResponse
{
	public bool ok = true;

	public bool pending;

	public string error = "";

	public int revision;

	public int lineCount;

	public int createType;

	public List<FlameLine> current = new List<FlameLine>();

	public List<FlameLine> candidate = new List<FlameLine>();

	public long exGradeOption;

	public List<InventoryEntry> inventory;
}
