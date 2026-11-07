using System.Collections.Generic;

namespace Server.Bot;

public sealed class BotCharacter : BotActor
{
	public BotTier tier = BotTier.S;

	public BotWander wander;

	internal BotTasks.Handle arrivalTask;

	public int profileId;

	public bool moving;

	public int facing = -1;

	public int stance = 5;

	public int chair;

	public int mountItemId;

	public int mountSkillId;

	public bool mountActive;

	public int accId;

	public int gmLevel;

	public int hp = 1;

	public int maxHp = 1;

	public int str = 12;

	public int dex = 5;

	public int intelligence = 4;

	public int luk = 4;

	public long meso;

	public readonly List<InventoryEntry> inventory = new List<InventoryEntry>();

	public Dictionary<int, int> equips = new Dictionary<int, int>();

	public readonly Dictionary<int, InventoryEntry> equipment = new Dictionary<int, InventoryEntry>();

	public BotClient client;

	public override bool IsBot()
	{
		return true;
	}
}
