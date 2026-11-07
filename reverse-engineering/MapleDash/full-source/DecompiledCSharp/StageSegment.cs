using System.Collections.Generic;

public class StageSegment
{
	public string kind = "walk";

	public float duration;

	public string hint = "";

	public string name = "";

	public string boss = "";

	public List<StageSpawn> spawns = new List<StageSpawn>();
}
