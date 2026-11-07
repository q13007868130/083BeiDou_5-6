using System.Collections.Generic;

namespace Server.Bot.Trade;

public sealed class BotTradeResponse
{
	public bool ok = true;

	public bool active;

	public bool accepted;

	public bool locked;

	public bool partnerLocked;

	public bool incomingInvitation;

	public string error;

	public string partnerName;

	public string[] partnerAppearance;

	public int partnerId;

	public long meso;

	public long partnerMeso;

	public FullCharacter character;

	public List<InventoryEntry> items = new List<InventoryEntry>();

	public List<InventoryEntry> partnerItems = new List<InventoryEntry>();
}
