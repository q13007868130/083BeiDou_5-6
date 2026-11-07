using System;
using System.Collections.Generic;
using Server.Bot.Social;
using Server.Field;

namespace Server.Bot.Types;

public sealed class DiceBot : BotStateMachine
{
	public enum DiceBotState
	{
		Reset,
		WaitForBet,
		Bet,
		Roll,
		RollPending,
		ProcessBet,
		BetPayout
	}

	private const long BetTimeoutMs = 25000L;

	private const double BetScanRangeSq = 15000.0;

	private const double LoseRecheckRangeSq = 75000.0;

	private const long RollDelayMs = 3000L;

	private const long ProcessDelayMs = 2000L;

	private const long PayoutStaggerMs = 100L;

	private static readonly string[] BetHint = new string[2] { "Bet Cho (even)", "Bet Han (odd)" };

	private readonly Random random;

	private readonly BotMessages messages;

	private BotDealerDice.BetType currentBet = BotDealerDice.BetType.None;

	private int[] rolls;

	private List<MapItem> currentPot;

	private long startTime;

	private long endTime;

	private bool betsProcessed;

	private bool optionsShown;

	private int generation;

	private BotTasks.Handle sequenceTask;

	public DiceBotState diceBotState { get; private set; }

	private BotActor Player => interactors.Respondant;

	public DiceBot(BotCharacter c, BotMessages messages = null, Random random = null)
		: base(c)
	{
		this.messages = messages ?? BotTypeManager.Messages;
		this.random = random ?? new Random();
		botType = "DiceBot";
		startTime = BotScheduler.Now();
	}

	public override void CheckPrioritySpeed()
	{
		if (interactors.Respondant != null)
		{
			UpdateScheduleDelay(1000L);
		}
		else
		{
			base.CheckPrioritySpeed();
		}
	}

	public override void UpdateState()
	{
		base.UpdateState();
		if (CheckIfNotRunningOrPaused())
		{
			return;
		}
		DebugLog(chr.name + " DiceBotState: " + diceBotState);
		switch (diceBotState)
		{
		case DiceBotState.Reset:
			ResetDiceBotState();
			StartGame();
			SetState(DiceBotState.WaitForBet);
			break;
		case DiceBotState.WaitForBet:
			WaitForBet();
			break;
		case DiceBotState.Bet:
			SetState((!DetectBet()) ? DiceBotState.WaitForBet : DiceBotState.Roll);
			break;
		case DiceBotState.Roll:
			CalculateRolls();
			SetState(DiceBotState.RollPending);
			break;
		case DiceBotState.RollPending:
			if (rolls != null)
			{
				SetState(DiceBotState.ProcessBet);
			}
			break;
		case DiceBotState.ProcessBet:
			CalculateBet();
			SetState(DiceBotState.BetPayout);
			break;
		case DiceBotState.BetPayout:
			if (betsProcessed)
			{
				SetState(DiceBotState.Reset);
			}
			break;
		default:
			DebugLog("Unexpected DiceBot state: " + diceBotState);
			state = State.FINISHED;
			ResetDiceBotState();
			break;
		}
	}

	public override void DisplayCommands(BotActor player)
	{
		if (player != null)
		{
			output.ShowOptions(player, BetHint);
		}
	}

	private void SetState(DiceBotState next)
	{
		diceBotState = next;
	}

	private void SetBet(BotDealerDice.BetType bet)
	{
		currentBet = bet;
	}

	private bool Alive()
	{
		if (running && chr.online)
		{
			return BotStore.GetBotById(chr.id) == this;
		}
		return false;
	}

	private void ResetDiceBotState()
	{
		SetState(DiceBotState.Reset);
		SetBet(BotDealerDice.BetType.None);
		currentPot = null;
		rolls = null;
		startTime = BotScheduler.Now();
		endTime = 0L;
		betsProcessed = false;
		optionsShown = false;
		sequenceTask?.Cancel();
		sequenceTask = null;
	}

	private void Later(long ms, Action next)
	{
		sequenceTask?.Cancel();
		sequenceTask = BotTasks.Schedule(() =>
		{
			if (!Alive())
			{
				return;
			}
			try
			{
				next();
			}
			catch (Exception ex)
			{
				DebugLog(chr.name + " sequence error: " + ex.Message);
			}
		}, ms, "Later", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Types/DiceBot.cs");
	}

	private void StartGame()
	{
		output.Send(BotOutput.Kind.Bubble, chr, "Please Place your bets!");
	}

	private void WaitForBet()
	{
		if (Player == null)
		{
			CheckForInquirer();
		}
		BotActor player = Player;
		if (player == null)
		{
			return;
		}
		endTime = startTime + 25000;
		if (diceBotState != DiceBotState.Bet && BotScheduler.Now() < endTime)
		{
			if (!optionsShown)
			{
				optionsShown = true;
				DisplayCommands(player);
			}
			ProcessMessages();
		}
		else
		{
			ExpireOptions();
			output.Speak(chr, "Please place your bets when you are ready!");
			state = State.FINISHED;
			ResetDiceBotState();
		}
	}

	private void CheckForInquirer()
	{
		if (interactors.inquirers.Count != 0)
		{
			BotActor botActor = interactors.inquirers[0];
			interactors.RemoveInquirer(botActor);
			interactors.SetRespondant(botActor);
		}
	}

	private bool DetectBet()
	{
		BotActor player = Player;
		if (player == null)
		{
			return false;
		}
		List<MapItem> list = BotLogic.ReadPlayersBetsStamps(player, 15000.0);
		if (list.Count == 0)
		{
			output.Speak(chr, "No Bet detected. Please Place your bets!");
			startTime = BotScheduler.Now();
			return false;
		}
		output.Speak(chr, BotLogic.AnnounceBetString(player, list));
		currentPot = list;
		return true;
	}

	private void CalculateRolls()
	{
		Later(3000L, () =>
		{
			rolls = BotDealerDice.Roll(chr, random, output);
		});
	}

	private void CalculateBet()
	{
		Later(2000L, () =>
		{
			ProcessBet(rolls);
		});
	}

	private void ProcessBet(int[] rolled)
	{
		if (rolled == null || rolled.Length < 2)
		{
			betsProcessed = true;
			return;
		}
		if (currentBet == BotDealerDice.BetType.None)
		{
			throw new ArgumentException("Invalid bet: " + currentBet);
		}
		bool flag = BotDealerDice.PlayerWins(currentBet, rolled);
		output.Speak(chr, BotDealerDice.ResultText(rolled));
		output.Emote(chr, 2);
		if (flag)
		{
			PayoutWinnersBets();
		}
		else
		{
			HandlePlayerLose();
		}
		betsProcessed = true;
	}

	private void PayoutWinnersBets()
	{
		if (currentPot == null)
		{
			return;
		}
		BotActor player = Player;
		if (player == null)
		{
			return;
		}
		int token = generation;
		int num = 0;
		foreach (MapItem item in currentPot)
		{
			if (item == null)
			{
				continue;
			}
			int itemId = item.ItemId;
			int quantity = item.Quantity;
			int num2 = 0;
			while (num2 < quantity)
			{
				(int, int) tuple = BotHelpers.AdjustCenterPositionXAxis((int)player.x, (int)player.y, num2, 2, 4, 20);
				int num3 = num * 100;
				int capturedItemId = itemId;
				var (px, py) = tuple;
				BotTasks.Schedule(() =>
				{
					if (Alive() && token == generation)
					{
						DropCommands.BotThrowItem(chr, capturedItemId, px, py, out var _);
					}
				}, num3, "PayoutWinnersBets", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Types/DiceBot.cs");
				num2++;
				num++;
			}
		}
	}

	private void HandlePlayerLose()
	{
		BotActor player = Player;
		if (player != null && currentPot != null && currentPot.Count != 0)
		{
			MapItem mapItem = currentPot[0];
			List<MapItem> itemsAtPlayer = BotLogic.ReadPlayersBetsStamps(player, (int)mapItem.x, (int)mapItem.y, 75000.0);
			if (BotLogic.CheckIfItemsOnFloorStill(currentPot, itemsAtPlayer))
			{
				DropCommands.LootItemListOnFloor(chr, currentPot);
			}
			else
			{
				HandlePlayerStealsBets();
			}
		}
	}

	private void HandlePlayerStealsBets()
	{
		BotActor player = Player;
		if (player != null)
		{
			player.fame -= 10;
			output.Send(BotOutput.Kind.YellowMessage, player, "[Mushroom Casino] You have stolen your bets. You have been defamed by the Mushroom Casino.", 0, player.id);
		}
		output.Speak(chr, "Player has stolen back his bet. Ceasing Game.");
		state = State.FINISHED;
	}

	private void ExpireOptions()
	{
		BotActor player = Player;
		if (player != null)
		{
			output.ExpireOptions(player);
		}
	}

	private void ProcessMessages()
	{
		Consume(QueueType.SECONDARY);
		Consume(QueueType.PRIMARY);
	}

	private void Consume(QueueType type)
	{
		ChatMessage chatMessage = messages.Poll(type);
		if (chatMessage != null)
		{
			if (!interactors.IsMessageFromRespondant(chatMessage))
			{
				messages.Add(chatMessage, type);
			}
			else
			{
				HandleBetCommand(chatMessage);
			}
		}
	}

	private void HandleBetCommand(ChatMessage message)
	{
		string text = ((message.content == null) ? "" : message.content.ToLowerInvariant());
		if (text.Contains("bet"))
		{
			if (text.Contains("han"))
			{
				ProcessBetCommand(BotDealerDice.BetType.Han, message);
			}
			else if (text.Contains("cho"))
			{
				ProcessBetCommand(BotDealerDice.BetType.Cho, message);
			}
			else
			{
				HandleInvalidBet();
			}
		}
	}

	private void ProcessBetCommand(BotDealerDice.BetType betType, ChatMessage message)
	{
		SetState(DiceBotState.Bet);
		output.Speak(chr, BotDealerDice.BetAnnouncement(message.sender.name, betType));
		SetBet(betType);
	}

	private void HandleInvalidBet()
	{
		output.Speak(chr, "Please Select Han or Cho!");
		startTime = BotScheduler.Now();
		SetBet(BotDealerDice.BetType.None);
	}

	public override void StopScheduledTask()
	{
		generation++;
		sequenceTask?.Cancel();
		sequenceTask = null;
		base.StopScheduledTask();
	}
}
