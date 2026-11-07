using System.Collections.Generic;

public class StageLevel
{
	public string id = "";

	public string name = "";

	public string goal = "";

	public string theme = "";

	public string teaches = "";

	public int suppression;

	public int recLevel = 1;

	public List<string> dialogue = new List<string>();

	public List<StageSegment> segments = new List<StageSegment>();

	public int rewardExp;

	public int rewardGold;
}
