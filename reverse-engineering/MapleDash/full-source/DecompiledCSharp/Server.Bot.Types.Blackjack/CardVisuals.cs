using System;
using System.Collections.Generic;

namespace Server.Bot.Types.Blackjack;

public static class CardVisuals
{
	private static readonly Dictionary<int, int> CardValueToItemId = new Dictionary<int, int>
	{
		[2] = 2380004,
		[3] = 2380005,
		[4] = 2380007,
		[5] = 2380009,
		[6] = 2381007,
		[7] = 2381024,
		[8] = 2383012,
		[9] = 2383043,
		[10] = 2388017,
		[11] = 2388022,
		[12] = 2388023,
		[13] = 2388024,
		[14] = 2388043
	};

	public static int GetCardItemId(string card)
	{
		int cardDisplayValue = BlackjackRules.GetCardDisplayValue(card);
		if (!CardValueToItemId.TryGetValue(cardDisplayValue, out var value))
		{
			throw new ArgumentException("No visual mapping for card: " + card);
		}
		return value;
	}
}
