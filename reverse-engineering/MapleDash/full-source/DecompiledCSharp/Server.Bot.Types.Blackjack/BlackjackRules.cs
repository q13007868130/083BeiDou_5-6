using System;
using System.Collections.Generic;

namespace Server.Bot.Types.Blackjack;

public static class BlackjackRules
{
	public static int CalculateHandValue(List<string> hand)
	{
		int num = 0;
		int num2 = 0;
		foreach (string item in hand)
		{
			switch (item[0])
			{
			case 'A':
				num2++;
				num += 11;
				break;
			case 'J':
			case 'K':
			case 'Q':
			case 'T':
				num += 10;
				break;
			default:
				num += item[0] - 48;
				break;
			}
		}
		while (num > 21 && num2 > 0)
		{
			num -= 10;
			num2--;
		}
		return num;
	}

	public static bool IsBlackjack(List<string> hand)
	{
		if (hand.Count == 2)
		{
			return CalculateHandValue(hand) == 21;
		}
		return false;
	}

	public static bool IsBust(List<string> hand)
	{
		return CalculateHandValue(hand) > 21;
	}

	public static bool ShouldDealerHit(int handValue)
	{
		return handValue < 17;
	}

	public static int GetCardDisplayValue(string card)
	{
		return card[0] switch
		{
			'2' => 2, 
			'3' => 3, 
			'4' => 4, 
			'5' => 5, 
			'6' => 6, 
			'7' => 7, 
			'8' => 8, 
			'9' => 9, 
			'T' => 10, 
			'J' => 11, 
			'Q' => 12, 
			'K' => 13, 
			'A' => 14, 
			_ => throw new ArgumentException("Invalid card rank: " + card[0]), 
		};
	}

	public static BlackjackOutcome DetermineOutcome(List<string> playerHand, List<string> dealerHand)
	{
		int num = CalculateHandValue(playerHand);
		int num2 = CalculateHandValue(dealerHand);
		bool flag = IsBlackjack(playerHand);
		bool flag2 = IsBlackjack(dealerHand);
		if (num > 21)
		{
			return BlackjackOutcome.LOSE;
		}
		if (num2 > 21)
		{
			return BlackjackOutcome.WIN;
		}
		if (flag & flag2)
		{
			return BlackjackOutcome.PUSH;
		}
		if (flag)
		{
			return BlackjackOutcome.BLACKJACK_WIN;
		}
		if (flag2)
		{
			return BlackjackOutcome.LOSE;
		}
		if (num == num2)
		{
			return BlackjackOutcome.PUSH;
		}
		if (num <= num2)
		{
			return BlackjackOutcome.LOSE;
		}
		return BlackjackOutcome.WIN;
	}

	public static int CalculatePayout(int betQuantity, BlackjackOutcome outcome)
	{
		return outcome switch
		{
			BlackjackOutcome.BLACKJACK_WIN => (int)((double)betQuantity * 1.5), 
			BlackjackOutcome.WIN => betQuantity, 
			_ => 0, 
		};
	}

	public static string FormatOutcomeMessage(string playerName, BlackjackOutcome outcome, List<string> playerHand, List<string> dealerHand)
	{
		int num = CalculateHandValue(playerHand);
		int num2 = CalculateHandValue(dealerHand);
		switch (outcome)
		{
		case BlackjackOutcome.LOSE:
			if (num > 21)
			{
				return playerName + ": Busted.";
			}
			if (IsBlackjack(dealerHand))
			{
				return playerName + ": Lose. Dealer Blackjack.";
			}
			return playerName + ": Lose. Dealer wins.";
		case BlackjackOutcome.WIN:
			if (num2 > 21)
			{
				return playerName + ": Win! Dealer busts.";
			}
			return playerName + ": Win!";
		case BlackjackOutcome.BLACKJACK_WIN:
			return playerName + ": Blackjack!";
		case BlackjackOutcome.PUSH:
			return playerName + ": Push.";
		default:
			return playerName + ": Unexpected result.";
		}
	}
}
