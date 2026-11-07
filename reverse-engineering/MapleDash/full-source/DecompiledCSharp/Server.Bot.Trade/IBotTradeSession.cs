using System;
using System.Collections.Generic;

namespace Server.Bot.Trade;

public interface IBotTradeSession
{
	BotActor actor { get; }

	IBotTradeSession partner { get; }

	long meso { get; }

	bool locked { get; }

	IList<InventoryEntry> Items { get; }

	Action<TradeResult> ResultCallback { get; set; }

	bool AddItem(InventoryEntry item, int slot);

	bool SwapItem(InventoryEntry item, int slot);

	void Chat(string text);

	void SetMesoBot(long amount);

	void Accept();

	void Confirm();

	void Decline();
}
