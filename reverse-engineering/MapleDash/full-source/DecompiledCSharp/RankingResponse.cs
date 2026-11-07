using System.Collections.Generic;

public sealed class RankingResponse
{
	public bool ok = true;

	public string error = "";

	public int world = -1;

	public int jobGroup = -1;

	public int worlds = 1;

	public bool wholeServer;

	public List<RankingEntry> entries = new List<RankingEntry>();

	public int myRank;

	public int myRankMove;

	public int myJobRank;

	public int myJobRankMove;

	public int myLevel;

	public int myJob;
}
