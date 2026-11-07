using System;
using System.Collections.Generic;

namespace Server.Bot.Types.Blackjack;

public sealed class BlackjackTable
{
	public enum Phase
	{
		WAITING,
		BETTING,
		PROCESSING_BETS,
		DEALING,
		PLAYER_TURNS,
		PLAYER_RESPONSE,
		DEALER_TURN,
		RESOLVING,
		PAYOUT
	}

	public const int MaxPlayers = 8;

	private readonly Deck deck;

	private readonly List<BlackjackPlayer> players = new List<BlackjackPlayer>(8);

	public Phase phase { get; private set; }

	public int currentPlayerIndex { get; private set; }

	public int waitCount { get; private set; }

	public IReadOnlyList<BlackjackPlayer> Players => players;

	public int PlayerCount => players.Count;

	public BlackjackPlayer Dealer => players[0];

	public BlackjackPlayer CurrentPlayer => PlayerAt(currentPlayerIndex);

	public bool IsCurrentPlayerDealer => currentPlayerIndex == 0;

	public bool HasEnoughPlayers => players.Count > 1;

	public BlackjackTable(BotActor dealerCharacter, Random random = null)
	{
		deck = new Deck(random);
		players.Add(new BlackjackPlayer(dealerCharacter));
		phase = Phase.WAITING;
		currentPlayerIndex = 1;
	}

	public void SetPhase(Phase value)
	{
		phase = value;
	}

	public BlackjackPlayer PlayerAt(int index)
	{
		if (index < 0 || index >= players.Count)
		{
			return null;
		}
		return players[index];
	}

	public bool AddPlayer(BotActor character)
	{
		if (players.Count >= 8)
		{
			return false;
		}
		players.Add(new BlackjackPlayer(character));
		return true;
	}

	public bool RemovePlayer(BotActor character)
	{
		for (int i = 1; i < players.Count; i++)
		{
			if (players[i].character == character)
			{
				players.RemoveAt(i);
				if (currentPlayerIndex > i)
				{
					currentPlayerIndex--;
				}
				if (currentPlayerIndex >= players.Count)
				{
					currentPlayerIndex = 0;
				}
				return true;
			}
		}
		return false;
	}

	public void IncrementToNextPlayer()
	{
		currentPlayerIndex++;
		if (currentPlayerIndex >= players.Count)
		{
			currentPlayerIndex = 0;
		}
	}

	public void ResetForNewRound()
	{
		foreach (BlackjackPlayer player in players)
		{
			player.ResetForNewRound();
		}
		currentPlayerIndex = 1;
		waitCount = 0;
	}

	public void IncrementWaitCount()
	{
		waitCount++;
	}

	public bool HasAtLeastOneBet()
	{
		for (int i = 1; i < players.Count; i++)
		{
			if (players[i].HasBets)
			{
				return true;
			}
		}
		return false;
	}

	public static int GetDealSequenceIndex(int index, int totalPlayers)
	{
		if (index == totalPlayers - 1)
		{
			return 0;
		}
		return index % (totalPlayers - 1) + 1;
	}

	public string DrawCard()
	{
		return deck.Draw();
	}

	public void DealCardToPlayer(BlackjackPlayer player, string card)
	{
		player.AddCardToHand(card);
	}

	public bool ShouldDealerHit()
	{
		return BlackjackRules.ShouldDealerHit(Dealer.HandValue);
	}

	public void ResolveAllOutcomes()
	{
		List<string> hand = Dealer.hand;
		for (int i = 1; i < players.Count; i++)
		{
			BlackjackPlayer blackjackPlayer = players[i];
			if (blackjackPlayer.hand.Count != 0)
			{
				switch (BlackjackRules.DetermineOutcome(blackjackPlayer.hand, hand))
				{
				case BlackjackOutcome.WIN:
					blackjackPlayer.SetResult(BlackjackPlayer.HandResult.WIN);
					break;
				case BlackjackOutcome.LOSE:
					blackjackPlayer.SetResult(BlackjackPlayer.HandResult.LOSE);
					break;
				case BlackjackOutcome.PUSH:
					blackjackPlayer.SetResult(BlackjackPlayer.HandResult.PUSH);
					break;
				case BlackjackOutcome.BLACKJACK_WIN:
					blackjackPlayer.SetResult(BlackjackPlayer.HandResult.BLACKJACK_WIN);
					break;
				}
			}
		}
	}

	public bool IsAlreadyAtTable(BotActor character)
	{
		foreach (BlackjackPlayer player in players)
		{
			if (player.character == character)
			{
				return true;
			}
		}
		return false;
	}
}
