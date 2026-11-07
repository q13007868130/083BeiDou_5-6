using System.Collections.Generic;

public sealed class JobDef
{
	public int id;

	public string name = "";

	public string book = "";

	public string family = "";

	public int tier;

	public int parent = -1;

	public List<int> next = new List<int>();

	public List<int> skills = new List<int>();

	public List<int> allSkills = new List<int>();

	public JobAdvance advance;

	public bool IsBeginner => tier == 0;

	public bool IsCygnus => id >= 1000;

	public bool IsAdvanced => tier >= 5;
}
