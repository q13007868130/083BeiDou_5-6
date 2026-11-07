using System;
using System.Collections.Generic;

namespace Server.Bot.Types.Pq;

public sealed class PqStep
{
	public PqAction action;

	public int[] ids = Array.Empty<int>();

	public int[] reactors = Array.Empty<int>();

	public int[] maps = Array.Empty<int>();

	public int minId;

	public int maxId;

	public int need;

	public int itemId;

	public int itemCount;

	public int npc;

	public bool openOnly;

	public int rounds = 1;

	public string[] prefer;

	public List<PqRect> stands = new List<PqRect>();

	public string comboProperty;

	public string awaitProperty;

	public string awaitValue;

	public string say;

	public long timeoutMs;
}
