using System.Collections.Generic;

public sealed class DeathSettlement
{
	private readonly HashSet<int> settled = new HashSet<int>();

	public bool TrySettle(int entityId)
	{
		return settled.Add(entityId);
	}

	public void Clear()
	{
		settled.Clear();
	}
}
