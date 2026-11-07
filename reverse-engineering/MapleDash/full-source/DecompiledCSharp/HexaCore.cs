using System.Collections.Generic;

public sealed class HexaCore
{
	public int id;

	public int type;

	public int maxLevel;

	public string name = "";

	public string desc = "";

	public int[] skills = new int[0];

	public int[] subSkills = new int[0];

	public int[] reqForActivation = new int[0];

	public Dictionary<int, int> requiredSkills = new Dictionary<int, int>();
}
