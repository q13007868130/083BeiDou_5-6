using System.Collections.Generic;

namespace Server.Bot.Types.Pq;

public sealed class PqDef
{
	public string key;

	public string name;

	public string eventName;

	public int recruitMap;

	public int entryMap;

	public int exitMap;

	public int recruitNpc;

	public int minLevel;

	public string[] keywords;

	public List<PqStage> stages = new List<PqStage>();

	public List<int> extraMaps = new List<int>();

	public PqStage Stage(int mapId)
	{
		foreach (PqStage stage in stages)
		{
			if (stage.mapId == mapId)
			{
				return stage;
			}
		}
		return null;
	}
}
