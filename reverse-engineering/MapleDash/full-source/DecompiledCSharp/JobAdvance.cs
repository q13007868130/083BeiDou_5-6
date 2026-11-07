using System.Collections.Generic;

public sealed class JobAdvance
{
	public int level;

	public List<KeyValuePair<string, int>> stat = new List<KeyValuePair<string, int>>();

	public int quest;

	public int requireItem;

	public int consumeItem;

	public List<int> grant = new List<int>();

	public List<int> teach = new List<int>();

	public bool resetStats;

	public int sp;

	public int ap;

	public bool derived;
}
