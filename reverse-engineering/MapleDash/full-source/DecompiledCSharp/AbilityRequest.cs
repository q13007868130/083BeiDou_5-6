using System.Collections.Generic;

public sealed class AbilityRequest
{
	public int characterId;

	public int preset;

	public AbilityOperation operation;

	public List<int> lockedRows = new List<int>();

	public long circulatorInventoryItemId;

	public bool commit;

	public bool accept;
}
