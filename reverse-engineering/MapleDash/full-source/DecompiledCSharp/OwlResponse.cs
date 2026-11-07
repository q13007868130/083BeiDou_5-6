using System.Collections.Generic;

public sealed class OwlResponse
{
	public bool ok = true;

	public string error = "";

	public byte code;

	public bool charged;

	public List<int> suggestions;

	public List<OwlHit> hits;

	public int map = -1;
}
