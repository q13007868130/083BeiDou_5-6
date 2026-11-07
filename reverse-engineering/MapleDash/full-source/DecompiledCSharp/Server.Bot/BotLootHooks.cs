using System;

namespace Server.Bot;

public sealed class BotLootHooks
{
	public Func<int, int, bool> NeedQuestItem;

	public Func<int, bool> IsConsumeOnPickup;

	public Func<int, bool> ApplyConsumeOnPickup;

	public Func<int, bool> TryRunItemScript;

	public Action<int> GainNxCredit;

	public Action OnAriantSpiritJewel;

	public Action ShowItemUnavailable;

	public Action ShowInventoryFull;

	public Action EnableActions;
}
