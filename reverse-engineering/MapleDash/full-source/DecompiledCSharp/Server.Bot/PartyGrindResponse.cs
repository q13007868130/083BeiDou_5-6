using System.Collections.Generic;

namespace Server.Bot;

public sealed class PartyGrindResponse
{
	public bool ok = true;

	public bool active;

	public long expires;

	public List<int> companions = new List<int>();
}
