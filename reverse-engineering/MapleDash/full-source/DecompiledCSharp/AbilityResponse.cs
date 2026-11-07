using System.Collections.Generic;

public sealed class AbilityResponse
{
	public bool ok = true;

	public bool pending;

	public string error = "";

	public int preset;

	public int activePreset;

	public int grade;

	public int lineCount;

	public int cost;

	public long honorExp;

	public List<AbilityLine> lines = new List<AbilityLine>();

	public List<AbilityLine> candidate = new List<AbilityLine>();
}
