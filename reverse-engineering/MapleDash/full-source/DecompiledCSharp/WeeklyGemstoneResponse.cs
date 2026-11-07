using System.Collections.Generic;

public sealed class WeeklyGemstoneResponse
{
	public bool ok = true;

	public bool eligible;

	public bool claimed;

	public bool started;

	public string error = "";

	public int clear;

	public readonly List<int> resetQuests = new List<int>();
}
