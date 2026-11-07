using System;
using System.Collections.Generic;
using Server.Bot.Environment;
using Server.Bot.Social;
using Server.Bot.Trade;

namespace Server.Bot.Types;

public sealed class DropGameBot : BotStateMachine
{
	public enum DropGameState
	{
		Reset,
		IdleAdvertise,
		TradeWait,
		TradeValidate,
		PartySetup,
		PartyWait,
		PreGame,
		ActiveGame,
		EndGame
	}

	private const int MediumTierCost = 10000000;

	private const int EliteTierCost = 50000000;

	private const int GameDurationMs = 120000;

	private const int DropIntervalMinMs = 4000;

	private const int DropIntervalMaxMs = 6000;

	private const int PartyTimeoutMs = 15000;

	private const int HasteSkillId = 4101004;

	private const string MovementRecordingName = "dg_potshop_1";

	private const int DropInitialDelayMinMs = 3000;

	private const int DropInitialDelayMaxMs = 6000;

	private const int MediumDespawnDelayMs = 1400;

	private const int EliteDespawnDelayMs = 1150;

	private static readonly int[] FallbackDropItemIds;

	private const string DialoguePack = "DropGameBotDialogue.yaml";

	private const string SpectatorDialoguePack = "DropGameSpectatorDialogue.yaml";

	private const string SpectatorBotType = "DropGameSpectator";

	private readonly Random random = new Random();

	private readonly BotPartyService parties;

	private readonly BotDialogue dialogue;

	private long stateEndTime;

	private BotTasks.Handle dropTask;

	private BotTasks.Handle sequenceTask;

	private bool scripted;

	private bool paymentSucceeded;

	private const int SpectatorCooldownMinDrops = 7;

	private const int SpectatorCooldownMaxDrops = 12;

	private static readonly Random spectatorRandom;

	private static readonly string[] SpectatorCategories;

	private static readonly int[] SpectatorCategoryWeights;

	private const int SpectatorWeightTotal = 100;

	private static int spectatorDropCounter;

	private static int spectatorNextReactionAt;

	private static int spectatorLastReactorId;

	public DropGameState GameState { get; private set; }

	public string SelectedTier { get; private set; }

	public BotActor Player { get; private set; }

	public DropGameLootPool LootPool { get; private set; }

	public DropGameBot(BotCharacter c, BotPartyService parties = null, BotDialogue dialogue = null)
		: base(c)
	{
		this.parties = parties;
		this.dialogue = dialogue ?? BotTypeManager.Dialogue;
		botType = "DropGameBot";
	}

	protected override long DefaultPeriodMs()
	{
		return 1000L;
	}

	public override void CheckPrioritySpeed()
	{
		UpdateScheduleDelay(1000L);
	}

	public override bool IsAvailableForAmbientActions()
	{
		return GameState == DropGameState.IdleAdvertise;
	}

	private void DebugPrint(string msg)
	{
		DebugLog("DropGameBot[" + chr.name + "]: " + msg);
	}

	private void SetState(DropGameState next)
	{
		if (GameState != next)
		{
			DebugPrint("STATE " + GameState.ToString() + " -> " + next);
		}
		GameState = next;
	}

	private void Later(long ms, Action action)
	{
		sequenceTask?.Cancel();
		sequenceTask = BotTasks.Schedule(() =>
		{
			if (!running || !chr.online || BotStore.GetBotById(chr.id) != this)
			{
				return;
			}
			try
			{
				action();
			}
			catch (Exception ex)
			{
				DebugPrint("sequence error: " + ex.Message);
				scripted = false;
			}
		}, ms, "Later", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Types/DropGameBot.cs");
	}

	public override void UpdateState()
	{
		base.UpdateState();
		if (CheckIfNotRunningOrPaused())
		{
			return;
		}
		if (state == State.TRADING)
		{
			state = State.RUNNING;
		}
		if (!scripted)
		{
			if (GameState != DropGameState.Reset && GameState != DropGameState.IdleAdvertise)
			{
				RejectOtherTradeRequests();
			}
			switch (GameState)
			{
			case DropGameState.Reset:
				ResetDropGameState();
				SetState(DropGameState.IdleAdvertise);
				break;
			case DropGameState.IdleAdvertise:
				IdleAdvertise();
				break;
			case DropGameState.TradeWait:
				TradeWait();
				break;
			case DropGameState.TradeValidate:
				TradeValidate();
				break;
			case DropGameState.PartySetup:
				PartySetup();
				break;
			case DropGameState.PartyWait:
				PartyWait();
				break;
			case DropGameState.PreGame:
				PreGame();
				break;
			case DropGameState.ActiveGame:
				ActiveGame();
				break;
			case DropGameState.EndGame:
				EndGame();
				break;
			default:
				DebugPrint("unexpected state: " + GameState);
				state = State.FINISHED;
				ResetDropGameState();
				break;
			}
		}
	}

	private void RejectOtherTradeRequests()
	{
		if (!tradeLogic.queue.HasPending(chr))
		{
			return;
		}
		BotActor botActor = tradeLogic.queue.Get(chr);
		if (botActor == null || (Player != null && botActor == Player))
		{
			return;
		}
		tradeLogic.queue.Remove(chr);
		if (chr.trade == null || chr.trade.partner?.actor != botActor)
		{
			return;
		}
		string text = Player?.name ?? "someone";
		DebugPrint("REJECT trade from " + botActor.name + " — already engaged with " + text);
		BotTradeCommands.WriteChat(chr, "Busy with " + text + "! Try again after this game.");
		IBotTradeSession session = chr.trade;
		BotTasks.Schedule(() =>
		{
			if (chr.trade != null && chr.trade == session)
			{
				BotTradeCommands.Decline(chr);
			}
		}, 1500L, "RejectOtherTradeRequests", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Types/DropGameBot.cs");
	}

	private bool TryAcceptTrade()
	{
		if (!tradeLogic.queue.HasPending(chr))
		{
			return false;
		}
		if (chr.trade is BotTradeService.Session { FullTrade: not false })
		{
			return false;
		}
		if (!tradeLogic.CheckQueue(chr))
		{
			return false;
		}
		if (!(chr.trade is BotTradeService.Session { FullTrade: not false } session2))
		{
			tradeLogic.Clear(chr);
			return false;
		}
		tradeHandler.Reset();
		BotActor botActor = chr.trade?.partner?.actor;
		if (botActor == null)
		{
			BotTradeCommands.Decline(chr);
			return false;
		}
		paymentSucceeded = false;
		session2.ResultCallback = (TradeResult result) =>
		{
			paymentSucceeded = result == TradeResult.SUCCESSFUL;
		};
		Player = botActor;
		DebugPrint("trade accepted — partner=" + botActor.name);
		SetState(DropGameState.TradeWait);
		return true;
	}

	private void PurgeStaleInvitation()
	{
		if (chr.trade is BotTradeService.Session { FullTrade: false, InvitationPending: false })
		{
			BotTradeCommands.Decline(chr);
		}
	}

	private void IdleAdvertise()
	{
		PurgeStaleInvitation();
		if (!TryAcceptTrade() && random.Next(100) < 8)
		{
			DebugPrint("advertising");
			Flavor("Advertise");
		}
	}

	private void TradeWait()
	{
		if (!IsTradeActive())
		{
			DebugPrint("TRADE_WAIT: trade no longer active, cancelling");
			CancelAndReset("Trade cancelled.");
			return;
		}
		DebugPrint("TRADE_WAIT: writing rules, starting 60s timer");
		BotTradeCommands.WriteChat(chr, "Drop Game! Medium: 10m / Elite: 50m");
		BotTradeCommands.WriteChat(chr, "Put in your mesos and confirm!");
		StartTimer(60000L);
		SetState(DropGameState.TradeValidate);
	}

	private void TradeValidate()
	{
		if (!IsTradeActive())
		{
			CancelAndReset("Trade cancelled.");
			return;
		}
		if (!BotTradeCommands.IsPartnerLocked(chr))
		{
			if (BotScheduler.Now() > stateEndTime)
			{
				DebugPrint("TRADE_VALIDATE: partner lock timed out");
				BotTradeCommands.WriteChat(chr, "Too slow! Trade timed out.");
				scripted = true;
				Later(2000L, () =>
				{
					BotTradeCommands.Decline(chr);
					CleanupTradeAndReset();
				});
			}
			return;
		}
		long num = BotTradeCommands.ReadPartnerMeso(chr);
		DebugPrint("TRADE_VALIDATE: partner locked, offered=" + num);
		switch (num)
		{
		case 10000000L:
			SelectedTier = "medium";
			break;
		case 50000000L:
			SelectedTier = "elite";
			break;
		default:
			DebugPrint("TRADE_VALIDATE: invalid meso amount, rejecting");
			BotTradeCommands.WriteChat(chr, "Wrong amount! 10m or 50m only.");
			scripted = true;
			Later(2000L, () =>
			{
				BotTradeCommands.Decline(chr);
				Flavor("InvalidMeso");
				CleanupTradeAndReset();
			});
			return;
		}
		DebugPrint("TRADE_VALIDATE: tier=" + SelectedTier + ", confirming trade");
		BotTradeCommands.WriteChat(chr, SelectedTier.ToUpperInvariant() + " tier locked in!");
		scripted = true;
		Later(1000L, () =>
		{
			BotTradeCommands.Confirm(chr);
			Later(2000L, TradeConfirmed);
		});
	}

	private void TradeConfirmed()
	{
		if (!paymentSucceeded)
		{
			CleanupTradeAndReset();
			return;
		}
		LootPool = DropGameLootPool.Load(SelectedTier, random);
		DebugPrint("TRADE_VALIDATE: loot pool loaded, size=" + LootPool.Size);
		if (LootPool.IsEmpty)
		{
			DebugPrint("TRADE_VALIDATE: loot pool empty for tier=" + SelectedTier);
			output.Speak(chr, "Loot pool error. Refunding and resetting.");
			CleanupTradeAndReset();
		}
		else
		{
			Flavor((SelectedTier == "elite") ? "TierConfirmElite" : "TierConfirmMedium");
			CleanupTrade();
			scripted = false;
			SetState(DropGameState.PartySetup);
		}
	}

	private void PartySetup()
	{
		if (!IsPlayerOnMap())
		{
			DebugPrint("PARTY_SETUP: player off-map, cancelling");
			CancelAndReset("Player left the map.");
			return;
		}
		if (parties == null)
		{
			DebugPrint("PARTY_SETUP: BotPartyService not wired, back to IDLE_ADVERTISE");
			output.Speak(chr, "Couldn't send party invite. Resetting.");
			ForfeitAndReset();
			return;
		}
		Flavor("PartyInvite");
		bool flag = parties.Invite(chr, Player);
		DebugPrint("PARTY_SETUP: Invite -> " + flag);
		if (!flag)
		{
			output.Speak(chr, "Couldn't send party invite. Resetting.");
			ForfeitAndReset();
		}
		else
		{
			StartTimer(15000L);
			SetState(DropGameState.PartyWait);
		}
	}

	private void PartyWait()
	{
		if (!IsPlayerOnMap())
		{
			CancelAndReset("Player left the map.");
			return;
		}
		if (parties == null)
		{
			ForfeitAndReset();
			return;
		}
		BotPartyService.Party party = parties.For(chr.id);
		if (party != null && party == parties.For(Player.id))
		{
			DebugPrint("PARTY_WAIT: player joined, advancing to PRE_GAME");
			SetState(DropGameState.PreGame);
		}
		else if (BotScheduler.Now() > stateEndTime)
		{
			DebugPrint("PARTY_WAIT: timed out waiting for player to accept");
			output.Speak(chr, "Party invite timed out — mesos forfeited. Don't waste my time next round!");
			scripted = true;
			Later(2000L, () =>
			{
				scripted = false;
				ForfeitAndReset();
			});
		}
	}

	private void PreGame()
	{
		if (!IsPlayerOnMap())
		{
			CancelAndReset("Player left the map.");
			return;
		}
		DebugPrint("PRE_GAME: casting Haste");
		scripted = true;
		Flavor("PreGame");
		Later(1000L, () =>
		{
			CastHaste();
			Later(2000L, () =>
			{
				Flavor("GameStart");
				stateEndTime = BotScheduler.Now() + 120000;
				DebugPrint("PRE_GAME: starting movement playback + drop scheduler, gameDuration=" + 120000 + "ms");
				StartMovementPlayback();
				int num = 3000 + random.Next(3000);
				ScheduleDrop(num);
				scripted = false;
				SetState(DropGameState.ActiveGame);
			});
		});
	}

	private void ActiveGame()
	{
		if (!IsPlayerOnMap())
		{
			DebugPrint("ACTIVE_GAME: player off-map, ending early");
			StopDropScheduler();
			Flavor("PlayerDisconnect");
			EndGameCleanup();
		}
		else if (BotScheduler.Now() >= stateEndTime)
		{
			DebugPrint("ACTIVE_GAME: time up, advancing to END_GAME");
			SetState(DropGameState.EndGame);
		}
	}

	private void EndGame()
	{
		DebugPrint("END_GAME: stopping scheduler, cleaning up");
		StopDropScheduler();
		Flavor("GameEnd");
		output.Emote(chr, 2);
		scripted = true;
		Later(3000L, () =>
		{
			scripted = false;
			EndGameCleanup();
		});
	}

	private void StartMovementPlayback()
	{
		List<BotMovementRecording.Packet> list = LoadMovementRecording(chr.mapId);
		if (list != null && list.Count > 0)
		{
			BotMovementPlayback.Play(chr, list);
		}
		else
		{
			DebugPrint("movement recording missing for map " + chr.mapId);
		}
	}

	private static List<BotMovementRecording.Packet> LoadMovementRecording(int mapId)
	{
		try
		{
			byte[] array = BotResources.Load().ReadBytes(BotMovementRecording.ResourcePath(mapId, "dg_potshop_1", "bin"));
			if (array == null)
			{
				return null;
			}
			return BotMovementRecording.ReadBinary(array);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private void ScheduleDrop(long delayMs)
	{
		dropTask?.Cancel();
		dropTask = BotTasks.Schedule(() =>
		{
			if (running && chr.online && BotStore.GetBotById(chr.id) == this)
			{
				PerformDrop();
			}
		}, delayMs, "ScheduleDrop", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Types/DropGameBot.cs");
	}

	private void ScheduleNextDrop()
	{
		int num = 4000 + random.Next(2000);
		ScheduleDrop(num);
	}

	private void PerformDrop()
	{
		if (BotScheduler.Now() < stateEndTime)
		{
			bool isEquip = false;
			DropGameLootPool.LootEntry? lootEntry = ((LootPool != null && !LootPool.IsEmpty) ? LootPool.RollItem() : ((DropGameLootPool.LootEntry?)null));
			int itemId;
			if (lootEntry.HasValue)
			{
				itemId = lootEntry.Value.itemId;
				isEquip = lootEntry.Value.isEquip;
			}
			else
			{
				itemId = FallbackDropItemIds[random.Next(FallbackDropItemIds.Length)];
			}
			DebugPrint("DROP itemId=" + itemId + " equip=" + isEquip);
			long expiryMs = ((SelectedTier == "elite") ? 1150 : 1400);
			try
			{
				DropCommands.BotDropItemWithExpiry(chr, itemId, isEquip, expiryMs);
				SpectatorReact(itemId);
			}
			catch (Exception ex)
			{
				DebugPrint("Drop error: " + ex.Message);
			}
			ScheduleNextDrop();
		}
	}

	private void StopDropScheduler()
	{
		dropTask?.Cancel();
		dropTask = null;
		StopMovementPlayback();
	}

	private void StopMovementPlayback()
	{
		BotMovementPlayback.Cancel(chr);
	}

	static DropGameBot()
	{
		FallbackDropItemIds = new int[8] { 2000004, 2000005, 2022000, 2022179, 2040002, 2040505, 2049000, 2070005 };
		spectatorRandom = new Random();
		SpectatorCategories = new string[5] { "Excitement", "Envy", "Hype", "Question", "Cheer" };
		SpectatorCategoryWeights = new int[5] { 30, 20, 20, 15, 15 };
		spectatorLastReactorId = -1;
		spectatorNextReactionAt = 7 + spectatorRandom.Next(6);
	}

	private void SpectatorReact(int itemId)
	{
		spectatorDropCounter++;
		if (spectatorDropCounter < spectatorNextReactionAt)
		{
			return;
		}
		spectatorDropCounter = 0;
		spectatorNextReactionAt = 7 + spectatorRandom.Next(6);
		string itemName = ItemName(itemId);
		if (string.IsNullOrEmpty(itemName) || itemName == "NULL")
		{
			return;
		}
		List<BotCharacter> list = new List<BotCharacter>();
		foreach (BotCharacter item in BotRegistry.OnMap(chr.mapId))
		{
			if (item != null && item.id != chr.id)
			{
				BotStateMachine botById = BotStore.GetBotById(item.id);
				if (botById != null && botById.IsAvailableForAmbientActions())
				{
					list.Add(item);
				}
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		for (int num = list.Count - 1; num > 0; num--)
		{
			int index = spectatorRandom.Next(num + 1);
			BotCharacter value = list[num];
			list[num] = list[index];
			list[index] = value;
		}
		BotCharacter reactor = PickDifferentSpectator(list);
		if (reactor == null)
		{
			return;
		}
		spectatorLastReactorId = reactor.id;
		int num2 = 1500 + spectatorRandom.Next(3500);
		int mapId = chr.mapId;
		BotActor watcher = Player;
		BotTasks.Schedule(() =>
		{
			if (BotRegistry.Contains(reactor.id) && reactor.mapId == mapId)
			{
				if (watcher != null)
				{
					reactor.facing = ((watcher.x >= reactor.x) ? 1 : (-1));
				}
				ReactWithDialogue(reactor, itemName);
			}
		}, num2, "SpectatorReact", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Types/DropGameBot.cs");
	}

	private static BotCharacter PickDifferentSpectator(List<BotCharacter> spectators)
	{
		if (spectators.Count == 0)
		{
			return null;
		}
		if (spectators.Count == 1)
		{
			return spectators[0];
		}
		foreach (BotCharacter spectator in spectators)
		{
			if (spectator.id != spectatorLastReactorId)
			{
				return spectator;
			}
		}
		return spectators[0];
	}

	private void ReactWithDialogue(BotCharacter reactor, string itemName)
	{
		try
		{
			string node = SelectWeightedCategory();
			BotDialogue.Dialogue dialogue = this.dialogue?.Get("DropGameSpectatorDialogue.yaml", "DropGameSpectator", node);
			if (dialogue?.text == null || dialogue.text.Count == 0)
			{
				return;
			}
			string text = dialogue.text[spectatorRandom.Next(dialogue.text.Count)].Replace("{item}", itemName);
			output.Speak(reactor, text);
			if (dialogue.emotes != null && dialogue.emotes.Count > 0)
			{
				int num = dialogue.emotes[0];
				if (num > 0 && spectatorRandom.Next(100) < 50)
				{
					output.Emote(reactor, num);
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private static string SelectWeightedCategory()
	{
		int num = spectatorRandom.Next(100);
		int num2 = 0;
		for (int i = 0; i < SpectatorCategories.Length; i++)
		{
			num2 += SpectatorCategoryWeights[i];
			if (num < num2)
			{
				return SpectatorCategories[i];
			}
		}
		return SpectatorCategories[SpectatorCategories.Length - 1];
	}

	private void CastHaste()
	{
		try
		{
			BotTypeManager.ApplySkillEffect?.Invoke(chr, 4101004);
			if (Player != null)
			{
				BotTypeManager.ApplySkillEffect?.Invoke(Player, 4101004);
			}
			output.Send(BotOutput.Kind.Skill, chr, null, 4101004);
			if (Player != null)
			{
				output.Send(BotOutput.Kind.Skill, Player, null, 4101004, Player.id);
			}
		}
		catch (Exception ex)
		{
			DebugPrint("Failed to cast Haste: " + ex.Message);
		}
	}

	private void Flavor(string node)
	{
		try
		{
			BotDialogue.Dialogue dialogue = this.dialogue?.Get("DropGameBotDialogue.yaml", botType, node);
			if (dialogue != null)
			{
				this.dialogue.Run(chr, dialogue, output, flavor: true);
			}
		}
		catch (Exception)
		{
		}
	}

	private bool IsTradeActive()
	{
		if (!(chr.trade is BotTradeService.Session session))
		{
			return chr.trade != null;
		}
		return session.FullTrade;
	}

	private bool IsPlayerOnMap()
	{
		if (Player == null)
		{
			return false;
		}
		if (!Player.online)
		{
			return false;
		}
		if (Player.mapId == chr.mapId && Player.channel == chr.channel)
		{
			return Player.world == chr.world;
		}
		return false;
	}

	private void StartTimer(long durationMs)
	{
		stateEndTime = BotScheduler.Now() + durationMs;
	}

	private void CancelAndReset(string reason)
	{
		output.Speak(chr, reason);
		CleanupTradeAndReset();
	}

	private void CleanupTrade()
	{
		tradeLogic.Clear(chr);
		tradeHandler.Reset();
	}

	private void CleanupTradeAndReset()
	{
		CleanupTrade();
		ResetDropGameState();
		SetState(DropGameState.IdleAdvertise);
	}

	private void ResetDropGameState()
	{
		SetState(DropGameState.Reset);
		SelectedTier = null;
		Player = null;
		LootPool = null;
		paymentSucceeded = false;
		stateEndTime = 0L;
		scripted = false;
		sequenceTask?.Cancel();
		sequenceTask = null;
		StopDropScheduler();
	}

	private void EndGameCleanup()
	{
		try
		{
			parties?.Leave(chr.id);
		}
		catch (Exception ex)
		{
			DebugPrint("Party cleanup error: " + ex.Message);
		}
		ResetDropGameState();
		SetState(DropGameState.IdleAdvertise);
	}

	private void ForfeitAndReset()
	{
		if (parties != null)
		{
			try
			{
				parties.Leave(chr.id);
			}
			catch (Exception ex)
			{
				DebugPrint("forfeit party cleanup error: " + ex.Message);
			}
		}
		ResetDropGameState();
		SetState(DropGameState.IdleAdvertise);
	}

	public override void DisplayCommands(BotActor player)
	{
		output.ShowOptions(player, new string[1] { chr.name });
	}

	public override void StopScheduledTask()
	{
		dropTask?.Cancel();
		dropTask = null;
		sequenceTask?.Cancel();
		sequenceTask = null;
		StopMovementPlayback();
		parties?.Leave(chr.id);
		chr.trade?.Decline();
		base.StopScheduledTask();
	}
}
