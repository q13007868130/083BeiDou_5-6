using Server.Bot.Trade;

namespace Server.Bot;

public class BotActor
{
	public int id;

	public string name = "";

	public int level = 1;

	public int job;

	public int gender;

	public int face;

	public int hair;

	public int skinColor;

	public int fame;

	public int mapId;

	public int world;

	public int channel = 1;

	public float x;

	public float y;

	internal long mapEntryOrder;

	private static long nextMapEntryOrder;

	public bool online = true;

	public FullCharacter character;

	public IBotTradeSession trade;

	public long mesosTraded;

	internal void MarkMapEntry()
	{
		mapEntryOrder = ++nextMapEntryOrder;
	}

	public virtual bool IsBot()
	{
		return false;
	}
}
