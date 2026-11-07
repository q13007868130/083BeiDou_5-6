using System.Collections.Generic;

namespace Server.Bot.Types.Pq;

public sealed class PqStage
{
	public int mapId;

	public int stageNo;

	public string title;

	public List<PqStep> steps = new List<PqStep>();

	public int npc;

	public int requireItem;

	public int requireCount;

	public long gateMs;
}
