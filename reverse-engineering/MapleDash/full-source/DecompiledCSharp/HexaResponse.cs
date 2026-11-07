using System.Collections.Generic;

public sealed class HexaResponse
{
	public bool ok = true;

	public string error = "";

	public int level;

	public int job;

	public bool eligible;

	public long solErda;

	public long fragments;

	public long solErdaGauge;

	public long solErdaGaugeMax;

	public int unlockLevel = 260;

	public int unlockQuest = 38625;

	public List<HexaCoreView> cores = new List<HexaCoreView>();

	public List<HexaStatView> stats = new List<HexaStatView>();

	public long nextSolErda;

	public long nextFragments;
}
