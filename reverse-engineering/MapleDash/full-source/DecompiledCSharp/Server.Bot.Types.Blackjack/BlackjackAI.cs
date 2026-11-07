using System;

namespace Server.Bot.Types.Blackjack;

public static class BlackjackAI
{
	public static bool HitOrStand(int handValue, Random random)
	{
		if (handValue <= 11)
		{
			return true;
		}
		if (handValue <= 16)
		{
			return random.NextDouble() < (17.0 - (double)handValue) / 10.0;
		}
		if (handValue == 17)
		{
			return random.NextDouble() < 0.1;
		}
		return false;
	}
}
