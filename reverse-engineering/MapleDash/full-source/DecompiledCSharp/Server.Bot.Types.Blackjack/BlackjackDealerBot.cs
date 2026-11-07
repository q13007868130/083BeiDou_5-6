using System;
using System.Collections.Generic;
using Server.Bot.Decorator;
using Server.Bot.Social;
using Server.Field;

namespace Server.Bot.Types.Blackjack;

public sealed class BlackjackDealerBot : BotStateMachine
{
	private static readonly int[] BetStamps = new int[8] { 4002000, 4002001, 4002002, 4002003, 4031558, 4031559, 4031560, 4031561 };

	private const int AiBetItem = 4031560;

	private const int AiBetQty = 5;

	private static readonly int[] DealerOutfit = new int[4] { 1042129, 1060001, 1072010, 1702099 };

	private readonly BlackjackTable table;

	private readonly Random random = new Random();

	private readonly BotMessages messages = BotTypeManager.Messages;

	private BlackjackTable.Phase lastPhase;

	private long bettingDeadline;

	private bool finalDecision;

	private bool payoutsIssued;

	private bool dealerTurnAnnounced;

	private long playerTurnStart;

	public BlackjackTable Table => table;

	public BlackjackDealerBot(BotCharacter chr, BotMessages messagesOverride = null)
		: base(chr)
	{
		table = new BlackjackTable(chr, random);
		botType = "BLACKJACK_DEALER";
		if (messagesOverride != null)
		{
			messages = messagesOverride;
		}
		lastPhase = table.phase;
		DressDealer();
	}

	private void DressDealer()
	{
		try
		{
			BotCustomization botCustomization = new BotCustomization(BotItemCatalog.Shared, BotTypeManager.Output);
			int[] dealerOutfit = DealerOutfit;
			foreach (int value in dealerOutfit)
			{
				botCustomization.Equip(chr, value);
			}
		}
		catch (Exception)
		{
		}
	}

	protected override long DefaultPeriodMs()
	{
		return 2000L;
	}

	public override void CheckPrioritySpeed()
	{
		UpdateScheduleDelay(2000L);
	}

	public override void UpdateState()
	{
		base.UpdateState();
		if (!CheckIfNotRunningOrPaused())
		{
			ProcessJoinInquiries();
			bool firstTickOfPhase = lastPhase != table.phase;
			switch (table.phase)
			{
			case BlackjackTable.Phase.WAITING:
				HandleWaiting(firstTickOfPhase);
				break;
			case BlackjackTable.Phase.BETTING:
				HandleBetting();
				break;
			case BlackjackTable.Phase.PROCESSING_BETS:
				HandleProcessingBets();
				break;
			case BlackjackTable.Phase.DEALING:
				HandleDealing();
				break;
			case BlackjackTable.Phase.PLAYER_TURNS:
				HandlePlayerTurn();
				break;
			case BlackjackTable.Phase.PLAYER_RESPONSE:
				HandlePlayerResponse();
				break;
			case BlackjackTable.Phase.DEALER_TURN:
				HandleDealerTurn();
				break;
			case BlackjackTable.Phase.RESOLVING:
				HandleResolving();
				break;
			case BlackjackTable.Phase.PAYOUT:
				HandlePayout();
				break;
			default:
				state = State.FINISHED;
				break;
			}
			lastPhase = table.phase;
		}
	}

	private void HandleWaiting(bool firstTickOfPhase)
	{
		if (firstTickOfPhase)
		{
			table.ResetForNewRound();
			if (!table.HasEnoughPlayers)
			{
				ChatBubble("Waiting for Players to join table..");
			}
		}
		if (table.HasEnoughPlayers)
		{
			bettingDeadline = 0L;
			table.SetPhase(BlackjackTable.Phase.BETTING);
		}
	}

	private void HandleBetting()
	{
		if (bettingDeadline == 0L)
		{
			ChatBubble("Please Place your Bets!");
			TriggerArtificialPlayerBets();
			bettingDeadline = BotScheduler.Now() + 10000;
		}
		else if (BotScheduler.Now() >= bettingDeadline)
		{
			bettingDeadline = 0L;
			table.SetPhase(BlackjackTable.Phase.PROCESSING_BETS);
		}
	}

	private void HandleProcessingBets()
	{
		table.IncrementWaitCount();
		ProcessAllPlayersBets();
		if (table.PlayerCount <= 1)
		{
			table.SetPhase(BlackjackTable.Phase.WAITING);
		}
		else if (table.HasAtLeastOneBet() && table.waitCount > 1)
		{
			ChatBubble("All bets are closed. Let's Begin.");
			table.SetPhase(BlackjackTable.Phase.DEALING);
		}
		else
		{
			table.SetPhase(BlackjackTable.Phase.BETTING);
		}
	}

	private void HandleDealing()
	{
		int playerCount = table.PlayerCount;
		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < playerCount; j++)
			{
				int dealSequenceIndex = BlackjackTable.GetDealSequenceIndex(j, playerCount);
				BlackjackPlayer blackjackPlayer = table.PlayerAt(dealSequenceIndex);
				if (blackjackPlayer != null && (dealSequenceIndex == 0 || blackjackPlayer.HasBets) && (dealSequenceIndex != 0 || i != 1))
				{
					DealCardToPlayer(blackjackPlayer);
				}
			}
		}
		Speak("My card: " + table.Dealer.HandValue + ".");
		table.SetPhase(BlackjackTable.Phase.PLAYER_TURNS);
	}

	private void HandlePlayerTurn()
	{
		playerTurnStart = BotScheduler.Now();
		finalDecision = false;
		BlackjackPlayer currentPlayer = table.CurrentPlayer;
		if (currentPlayer == null)
		{
			table.IncrementToNextPlayer();
			return;
		}
		int handValue = currentPlayer.HandValue;
		if (handValue == 0)
		{
			table.IncrementToNextPlayer();
			if (table.IsCurrentPlayerDealer)
			{
				table.SetPhase(BlackjackTable.Phase.DEALER_TURN);
			}
			return;
		}
		if (handValue > 21)
		{
			HandleBust(currentPlayer, handValue);
			return;
		}
		if (handValue == 21)
		{
			HandleAutoStand(currentPlayer);
			return;
		}
		FaceTowards(currentPlayer.character);
		Speak(currentPlayer.Name + ": " + handValue + ". What will you do?");
		if (!BotHelpers.IsBot(currentPlayer.character))
		{
			ShowActionHint(currentPlayer.character);
		}
		TriggerArtificialPlayerResponse(currentPlayer);
		table.SetPhase(BlackjackTable.Phase.PLAYER_RESPONSE);
	}

	private void HandleBust(BlackjackPlayer player, int handValue)
	{
		player.SetResponseStatus("RESPONDED");
		player.SetStatus(BlackjackPlayer.PlayerStatus.BUST);
		Speak(player.Name + ": " + handValue + " Too Many.");
		TriggerAiPlayerReaction(player, "PlayerBust");
		BotActor botActor = LootLoserBets(player);
		if (player.cardsOnMap.Count > 0)
		{
			DropCommands.LootItemListOnFloor(chr, new List<MapItem>(player.cardsOnMap));
			player.ClearCardsOnMap();
		}
		if (botActor != null)
		{
			KickPlayer(botActor);
		}
		else
		{
			table.IncrementToNextPlayer();
		}
		table.SetPhase(table.IsCurrentPlayerDealer ? BlackjackTable.Phase.DEALER_TURN : BlackjackTable.Phase.PLAYER_TURNS);
	}

	private void HandleAutoStand(BlackjackPlayer player)
	{
		player.SetResponseStatus("RESPONDED");
		player.SetStatus(BlackjackPlayer.PlayerStatus.STAND);
		Speak(player.Name + ": 21!");
		table.IncrementToNextPlayer();
		table.SetPhase(table.IsCurrentPlayerDealer ? BlackjackTable.Phase.DEALER_TURN : BlackjackTable.Phase.PLAYER_TURNS);
	}

	private void HandlePlayerResponse()
	{
		BlackjackPlayer currentPlayer = table.CurrentPlayer;
		if (currentPlayer == null)
		{
			table.SetPhase(BlackjackTable.Phase.DEALER_TURN);
			return;
		}
		if (!currentPlayer.HasResponded)
		{
			WaitForResponse(currentPlayer);
		}
		if (currentPlayer.HasResponded)
		{
			ProcessPlayerAction(currentPlayer);
		}
	}

	private void ProcessPlayerAction(BlackjackPlayer player)
	{
		switch (player.status)
		{
		case BlackjackPlayer.PlayerStatus.ACTIVE:
			DealCardToPlayer(player);
			player.SetResponseStatus("WAITING");
			player.SetStatus(BlackjackPlayer.PlayerStatus.WAITING);
			table.SetPhase(BlackjackTable.Phase.PLAYER_TURNS);
			break;
		case BlackjackPlayer.PlayerStatus.STAND:
		case BlackjackPlayer.PlayerStatus.BUST:
			table.IncrementToNextPlayer();
			player.SetResponseStatus("WAITING");
			table.SetPhase(table.IsCurrentPlayerDealer ? BlackjackTable.Phase.DEALER_TURN : BlackjackTable.Phase.PLAYER_TURNS);
			break;
		}
	}

	private void HandleDealerTurn()
	{
		BlackjackPlayer dealer = table.Dealer;
		if (!dealerTurnAnnounced)
		{
			dealerTurnAnnounced = true;
			Speak("My Turn.");
		}
		int handValue = dealer.HandValue;
		if (!BlackjackRules.ShouldDealerHit(handValue))
		{
			Speak(handValue + ".");
			if (handValue > 21)
			{
				Speak("Too many. I Bust.");
			}
			else if (handValue == 21)
			{
				Speak("21 for me :P");
				output.Emote(chr, 3);
			}
			dealerTurnAnnounced = false;
			table.SetPhase(BlackjackTable.Phase.RESOLVING);
		}
		else
		{
			Speak(handValue + ".");
			DealCardToPlayer(dealer);
		}
	}

	private void HandleResolving()
	{
		table.ResolveAllOutcomes();
		List<string> hand = table.Dealer.hand;
		List<string> list = new List<string>();
		for (int i = 1; i < table.PlayerCount; i++)
		{
			BlackjackPlayer blackjackPlayer = table.PlayerAt(i);
			if (blackjackPlayer != null && blackjackPlayer.hand.Count != 0)
			{
				list.Add(BlackjackRules.FormatOutcomeMessage(blackjackPlayer.Name, BlackjackRules.DetermineOutcome(blackjackPlayer.hand, hand), blackjackPlayer.hand, hand));
			}
		}
		if (list.Count > 0)
		{
			Speak(string.Join(" | ", list));
		}
		for (int j = 1; j < table.PlayerCount; j++)
		{
			BlackjackPlayer blackjackPlayer2 = table.PlayerAt(j);
			if (blackjackPlayer2 != null && blackjackPlayer2.hand.Count != 0)
			{
				string text = OutcomeToDialogueNode(BlackjackRules.DetermineOutcome(blackjackPlayer2.hand, hand));
				if (text != null)
				{
					TriggerAiPlayerReaction(blackjackPlayer2, text);
				}
			}
		}
		table.SetPhase(BlackjackTable.Phase.PAYOUT);
	}

	private void HandlePayout()
	{
		if (!payoutsIssued)
		{
			List<BotActor> list = new List<BotActor>();
			int num = 0;
			for (int i = 1; i < table.PlayerCount; i++)
			{
				BlackjackPlayer player = table.PlayerAt(i);
				if (player == null || player.result == BlackjackPlayer.HandResult.PENDING || player.betsProcessed)
				{
					continue;
				}
				if (player.result == BlackjackPlayer.HandResult.WIN || player.result == BlackjackPlayer.HandResult.BLACKJACK_WIN)
				{
					bool bonus = player.result == BlackjackPlayer.HandResult.BLACKJACK_WIN;
					int num2 = num;
					BotTasks.Schedule(() =>
					{
						PayWinner(player, bonus);
					}, num2, "HandlePayout", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Types/Blackjack/BlackjackDealerBot.cs");
					num += 2000 + random.Next(1500);
				}
				else
				{
					BotActor botActor = PayoutOrCollect(player);
					if (botActor != null)
					{
						list.Add(botActor);
					}
				}
			}
			foreach (BotActor item in list)
			{
				KickPlayer(item);
			}
			payoutsIssued = true;
			return;
		}
		for (int num3 = 1; num3 < table.PlayerCount; num3++)
		{
			BlackjackPlayer blackjackPlayer = table.PlayerAt(num3);
			if (blackjackPlayer != null && blackjackPlayer.result != BlackjackPlayer.HandResult.PENDING && !blackjackPlayer.betsProcessed)
			{
				return;
			}
		}
		CleanupAllCards();
		ChatBubble("Let's go again.");
		payoutsIssued = false;
		table.SetPhase(BlackjackTable.Phase.WAITING);
	}

	private void KickPlayer(BotActor actor)
	{
		table.RemovePlayer(actor);
		interactors.RemoveRespondant(actor);
	}

	private void DealCardToPlayer(BlackjackPlayer player)
	{
		string card = table.DrawCard();
		int throwX = player.originX + (player.hand.Count + 1) * 35;
		int originY = player.originY;
		int cardItemId = CardVisuals.GetCardItemId(card);
		MapItem card2 = DropCommands.BotThrowItemNoExpireOwnerOnly(chr, cardItemId, throwX, originY);
		table.DealCardToPlayer(player, card);
		player.AddCardOnMap(card2);
	}

	private void ProcessAllPlayersBets()
	{
		List<BotActor> list = new List<BotActor>();
		List<string> list2 = new List<string>();
		for (int i = 0; i < table.PlayerCount; i++)
		{
			BlackjackPlayer blackjackPlayer = table.PlayerAt(i);
			if (blackjackPlayer == null)
			{
				continue;
			}
			if (i == 0)
			{
				blackjackPlayer.SetOriginLocation((int)chr.x, (int)chr.y);
				continue;
			}
			BotActor character = blackjackPlayer.character;
			if (character.mapId != chr.mapId)
			{
				Speak(character.name + " has left the map.");
				list.Add(character);
				continue;
			}
			blackjackPlayer.SetOriginLocation((int)character.x, (int)character.y);
			List<MapItem> list3 = BotLogic.ReadPlayersBetsStamps(character, 5000.0);
			if (list3.Count == 0)
			{
				ChatBubble("No Bet detected for: " + character.name);
				if (blackjackPlayer.SkipHand())
				{
					list.Add(character);
				}
			}
			else
			{
				blackjackPlayer.ResetSkippedHands();
				blackjackPlayer.SetBets(list3);
				list2.Add(BotLogic.AnnounceBetString(character, list3));
			}
		}
		if (list2.Count > 0)
		{
			Speak(string.Join(" | ", list2));
		}
		foreach (BotActor item in list)
		{
			KickPlayer(item);
		}
	}

	private BotActor PayoutOrCollect(BlackjackPlayer player)
	{
		switch (player.result)
		{
		case BlackjackPlayer.HandResult.BLACKJACK_WIN:
			PayWinner(player, blackjackBonus: true);
			return null;
		case BlackjackPlayer.HandResult.WIN:
			PayWinner(player, blackjackBonus: false);
			return null;
		case BlackjackPlayer.HandResult.LOSE:
			return LootLoserBets(player);
		case BlackjackPlayer.HandResult.PUSH:
			player.betsProcessed = true;
			return null;
		default:
			player.betsProcessed = true;
			return null;
		}
	}

	private void PayWinner(BlackjackPlayer player, bool blackjackBonus)
	{
		if (player.character == null)
		{
			player.betsProcessed = true;
			return;
		}
		bool flag = BotHelpers.IsBot(player.character);
		foreach (MapItem item in new List<MapItem>(player.bets))
		{
			int num = (blackjackBonus ? ((int)((double)item.Quantity * 1.5)) : item.Quantity);
			for (int i = 0; i < num && i < 60; i++)
			{
				(int, int) tuple = BotHelpers.AdjustCenterPositionXAxis(player.originX, player.originY, i, 2, 4, 20);
				DropCommands.BotThrowItemToOwner(chr, item.ItemId, tuple.Item1, tuple.Item2, player.character);
			}
		}
		player.betsProcessed = true;
		if (flag)
		{
			BotTasks.Schedule(() =>
			{
				LootBackForAi(player);
			}, 2000 + random.Next(1500), "PayWinner", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Types/Blackjack/BlackjackDealerBot.cs");
		}
	}

	private void LootBackForAi(BlackjackPlayer player)
	{
		BotActor character = player.character;
		if (character == null)
		{
			return;
		}
		foreach (MapItem item in new List<MapItem>(BotLogic.ReadPlayersBetsStamps(character, player.originX, player.originY, 1500.0)))
		{
			DropCommands.LootItemListOnFloor(character, new List<MapItem> { item });
		}
	}

	private BotActor LootLoserBets(BlackjackPlayer player)
	{
		BotActor character = player.character;
		if (character == null)
		{
			return null;
		}
		double rangeSq = (BotHelpers.IsBot(character) ? 1500 : 5000);
		bool flag = BotLogic.CheckIfItemsOnFloorStill(player.bets, BotLogic.ReadPlayersBetsStamps(character, player.originX, player.originY, rangeSq));
		player.betsProcessed = true;
		if (flag)
		{
			DropCommands.LootItemListOnFloor(chr, new List<MapItem>(player.bets));
			return null;
		}
		character.fame -= 10;
		Speak("Player has stolen back his bet.");
		LootPlayerCards(player);
		return character;
	}

	private void CleanupAllCards()
	{
		foreach (BlackjackPlayer player in table.Players)
		{
			LootPlayerCards(player);
		}
	}

	private void LootPlayerCards(BlackjackPlayer player)
	{
		try
		{
			if (player.cardsOnMap.Count != 0)
			{
				DropCommands.LootItemListOnFloor(chr, new List<MapItem>(player.cardsOnMap));
				player.ClearCardsOnMap();
			}
		}
		catch (Exception)
		{
		}
	}

	private void TriggerArtificialPlayerBets()
	{
		int num = 0;
		for (int i = 1; i < table.PlayerCount; i++)
		{
			BlackjackPlayer blackjackPlayer = table.PlayerAt(i);
			if (blackjackPlayer != null && BotHelpers.IsBot(blackjackPlayer.character) && !blackjackPlayer.HasBets)
			{
				BotActor who = blackjackPlayer.character;
				BotTasks.Schedule(() =>
				{
					DropCommands.BotDropItemQtyOwnerOnly(who, 4031560, 5);
				}, 1250 + num, "TriggerArtificialPlayerBets", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Types/Blackjack/BlackjackDealerBot.cs");
				num += 1500 + random.Next(2000);
			}
		}
	}

	private void TriggerArtificialPlayerResponse(BlackjackPlayer player)
	{
		if (!BotHelpers.IsBot(player.character))
		{
			return;
		}
		BotActor character = player.character;
		BotCharacter botChr = character as BotCharacter;
		int handValue = player.HandValue;
		BotTasks.Schedule(() =>
		{
			bool flag = BlackjackAI.HitOrStand(handValue, random);
			string node = (flag ? "HitResponse" : "StandResponse");
			if (botChr != null)
			{
				string text = DialogueLine(node);
				output.Speak(botChr, text ?? (flag ? "hit" : "stand"));
			}
			if (flag)
			{
				player.SetResponseStatus("RESPONDED");
				player.SetStatus(BlackjackPlayer.PlayerStatus.ACTIVE);
			}
			else
			{
				player.SetResponseStatus("RESPONDED");
				player.SetStatus(BlackjackPlayer.PlayerStatus.STAND);
			}
		}, 1250 + random.Next(1500), "TriggerArtificialPlayerResponse", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Types/Blackjack/BlackjackDealerBot.cs");
	}

	private static string OutcomeToDialogueNode(BlackjackOutcome outcome)
	{
		return outcome switch
		{
			BlackjackOutcome.WIN => "PlayerWin", 
			BlackjackOutcome.BLACKJACK_WIN => "PlayerBlackjack", 
			BlackjackOutcome.LOSE => "PlayerLose", 
			BlackjackOutcome.PUSH => "PlayerPush", 
			_ => null, 
		};
	}

	private void TriggerAiPlayerReaction(BlackjackPlayer player, string dialogueNode)
	{
		if (!BotHelpers.IsBot(player.character) || !(player.character is BotCharacter bot))
		{
			return;
		}
		int num = DialogueEmote(dialogueNode);
		if (num > 0)
		{
			output.Emote(bot, num);
		}
		if (random.NextDouble() < 0.3)
		{
			string text = DialogueLine(dialogueNode);
			if (text != null)
			{
				output.Speak(bot, text);
			}
		}
	}

	private static string DialogueLine(string node)
	{
		try
		{
			return BotTypeManager.Dialogue?.RandomLine("BlackjackDealerBotDialogue.yaml", "BLACKJACK_DEALER", node);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static int DialogueEmote(string node)
	{
		try
		{
			return BotTypeManager.Dialogue?.RandomEmote("BlackjackDealerBotDialogue.yaml", "BLACKJACK_DEALER", node) ?? (-1);
		}
		catch (Exception)
		{
			return -1;
		}
	}

	private void WaitForResponse(BlackjackPlayer player)
	{
		BotActor character = player.character;
		if (character == null)
		{
			return;
		}
		if (!BotHelpers.IsBot(character) && character.mapId != chr.mapId)
		{
			Speak(player.Name + " has left. Automatic Stand.");
			player.SetResponseStatus("RESPONDED");
			player.SetStatus(BlackjackPlayer.PlayerStatus.STAND);
			return;
		}
		long num = playerTurnStart + 15000;
		if (BotScheduler.Now() >= num)
		{
			if (finalDecision)
			{
				Speak("No response from " + player.Name + ". Automatic Stand.");
				player.SetResponseStatus("RESPONDED");
				player.SetStatus(BlackjackPlayer.PlayerStatus.STAND);
				finalDecision = false;
				return;
			}
			Speak("Please Make a decision " + player.Name);
			if (!BotHelpers.IsBot(character))
			{
				ShowActionHint(character);
			}
			playerTurnStart = BotScheduler.Now();
			finalDecision = true;
		}
		else
		{
			ProcessBlackjackMessages(player);
		}
	}

	private void ProcessBlackjackMessages(BlackjackPlayer player)
	{
		ChatMessage chatMessage = messages.Poll();
		if (chatMessage == null)
		{
			return;
		}
		if (!interactors.IsRespondant(chatMessage.sender))
		{
			messages.Add(chatMessage);
		}
		else if (chatMessage.sender == player.character)
		{
			string text = chatMessage.content.ToLowerInvariant();
			if (text.Contains("stand") || text.Contains("stay") || text.Contains("停牌") || text.Contains("不要") || text.Contains("停"))
			{
				player.SetResponseStatus("RESPONDED");
				player.SetStatus(BlackjackPlayer.PlayerStatus.STAND);
			}
			else if (text.Contains("hit") || text.Contains("要牌") || text.Contains("补牌"))
			{
				player.SetResponseStatus("RESPONDED");
				player.SetStatus(BlackjackPlayer.PlayerStatus.ACTIVE);
			}
		}
	}

	private void ProcessJoinInquiries()
	{
		ChatMessage chatMessage = messages.Poll(QueueType.TERTIARY);
		if (chatMessage == null)
		{
			return;
		}
		if (!interactors.IsInquirer(chatMessage.sender))
		{
			messages.Add(chatMessage, QueueType.TERTIARY);
			return;
		}
		string text = chatMessage.content.ToLowerInvariant();
		if (text.Contains("join") || text.Contains("加入"))
		{
			BotActor sender = chatMessage.sender;
			if (table.IsAlreadyAtTable(sender))
			{
				output.Send(BotOutput.Kind.YellowMessage, sender, "You are already at the table.", 0, sender.id);
			}
			else if (table.AddPlayer(sender))
			{
				interactors.RemoveInquirer(sender);
				interactors.SetRespondant(sender);
				ChatBubble(sender.name + " has joined the table.");
				output.Send(BotOutput.Kind.YellowMessage, sender, "You have joined " + chr.name + "'s Blackjack table. You will be in the next round.", 0, sender.id);
			}
			else
			{
				output.Send(BotOutput.Kind.YellowMessage, sender, "Table is full.", 0, sender.id);
			}
		}
	}

	public static string JoinNearestTable(BotActor player)
	{
		BlackjackDealerBot blackjackDealerBot = null;
		double num = double.MaxValue;
		foreach (BotStateMachine value in BotStore.GetAllBots().Values)
		{
			if (value is BlackjackDealerBot blackjackDealerBot2 && blackjackDealerBot2.chr.mapId == player.mapId)
			{
				double num2 = blackjackDealerBot2.chr.x - player.x;
				double num3 = blackjackDealerBot2.chr.y - player.y;
				double num4 = num2 * num2 + num3 * num3;
				if (num4 < num)
				{
					num = num4;
					blackjackDealerBot = blackjackDealerBot2;
				}
			}
		}
		if (blackjackDealerBot == null)
		{
			return "当前地图没有可加入的 21 点荷官。请确认 21 点桌功能已开启，并重新进入自由市场。";
		}
		return blackjackDealerBot.JoinPlayerFromNpc(player);
	}

	public string JoinPlayerFromNpc(BotActor player)
	{
		if (table.IsAlreadyAtTable(player))
		{
			return "你已经在这张 21 点桌里了。\r\n\r\n下注方式：把赌场筹码丢在自己附近。轮到你行动时，输入“要牌”或“停牌”。";
		}
		if (!table.AddPlayer(player))
		{
			return "这张 21 点桌已经满了。";
		}
		interactors.SetRespondant(player);
		FaceTowards(player);
		ChatBubble(player.name + " 加入了 21 点桌。");
		return "已加入 " + chr.name + " 的 21 点桌。\r\n\r\n下注方式：把赌场筹码丢在自己附近。轮到你行动时，输入“要牌”或“停牌”。";
	}

	public override void DisplayCommands(BotActor player)
	{
		output.ShowOptions(player, new string[1] { "加入" });
	}

	private void ShowActionHint(BotActor player)
	{
		output.ShowOptions(player, new string[2] { "停牌", "要牌" });
	}

	private void Speak(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			output.Speak(chr, text);
		}
	}

	private void ChatBubble(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			output.Send(BotOutput.Kind.Bubble, chr, text);
		}
	}

	private void FaceTowards(BotActor target)
	{
		if (target != null)
		{
			chr.facing = ((target.x >= chr.x) ? 1 : (-1));
		}
	}
}
