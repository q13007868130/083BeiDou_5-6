using System;
using Server.Bot.Events;
using Server.Bot.Social;
using Server.Bot.Trade;

namespace Server.Bot;

public abstract class BotStateMachine : IEventSubscriber
{
	public enum State
	{
		IDLE,
		RUNNING,
		PAUSE,
		TRADING,
		FINISHED
	}

	public BotCharacter chr;

	public bool running;

	public string botType = "Generic";

	public readonly BotTradeHandler tradeHandler;

	public readonly BotTradeInventory tradeInventory = new BotTradeInventory();

	public readonly BotTradeWants tradeWants = new BotTradeWants();

	public TradeMode tradeMode = TradeMode.NULL;

	public TradeResult? lastTradeResult;

	public BotActor lastTradedCharacter;

	public BotOutput output = BotTypeManager.Output;

	public BotTradeLogic tradeLogic = BotTypeManager.Trades;

	public MarketBotFlavor marketFlavor = BotTypeManager.MarketFlavor;

	public MarketBotDirector marketDirector = BotTypeManager.Market;

	public Func<int, string> ItemName = BotHelpers.ConvertItemIdToName;

	public Func<bool> IsMapObserved;

	public Func<bool> IsCompanion;

	private long suspendedUntil;

	private readonly BotEventBuffer eventBuffer = new BotEventBuffer(100);

	private EventBus eventBus;

	public bool movementInterrupted;

	public readonly BotInteractors interactors = new BotInteractors();

	private static readonly Random random = new Random();

	private long currentDelay;

	private long busyUntilMs;

	private long lastNudgeMs;

	public Func<bool> HasPlayersOnMap;

	public State state { get; protected set; }

	public BotTradeStateMachine tradeStateMachine { get; private set; }

	public virtual bool UsesSharedMarketTick => false;

	public bool HasQueuedEvents => !eventBuffer.IsEmpty;

	public bool hasActiveRespondant => interactors.Respondant != null;

	public bool IsBusy => BotScheduler.Now() < busyUntilMs;

	public virtual bool SkipsSpawnChoreography => false;

	public void InterruptMovement()
	{
		movementInterrupted = true;
	}

	public void ClearMovementInterrupt()
	{
		movementInterrupted = false;
	}

	public void Subscribe(EventBus bus, params EventType[] types)
	{
		if (eventBus != null && eventBus != bus)
		{
			eventBus.UnsubscribeAll(this);
		}
		eventBus = bus;
		foreach (EventType type in types)
		{
			bus.Subscribe(type, this);
		}
	}

	public void OnEvent(GameEvent ev)
	{
		eventBuffer.Add(ev);
	}

	public bool MatchesFilter(GameEvent ev)
	{
		if (ev.world == chr.world && ev.channel == chr.channel)
		{
			return ev.mapId == chr.mapId;
		}
		return false;
	}

	public void ProcessQueuedEvents()
	{
		GameEvent gameEvent = eventBuffer.Poll();
		if (gameEvent != null)
		{
			HandleEvent(gameEvent);
		}
	}

	public virtual void HandleEvent(GameEvent ev)
	{
	}

	public bool CheckMainPlayersOnMap()
	{
		return HasPlayersOnMap?.Invoke() ?? false;
	}

	public void BusyFor(long ms)
	{
		busyUntilMs = BotScheduler.Now() + Math.Max(0L, ms);
	}

	public void ClearBusy()
	{
		busyUntilMs = 0L;
	}

	public void UpdateScheduleDelay(long ms)
	{
		if (currentDelay != ms)
		{
			currentDelay = ms;
			if (!UsesSharedMarketTick)
			{
				BotScheduler.Reschedule(chr.id, ms);
			}
		}
	}

	public void SetPriorityLow()
	{
		UpdateScheduleDelay(10000L);
	}

	public void SetPriorityHigh()
	{
		UpdateScheduleDelay(2000L);
	}

	public void SetPriorityNormal()
	{
		UpdateScheduleDelay(2000 + random.Next(4000));
	}

	public void NudgeSoon(long initialDelay)
	{
		if (!UsesSharedMarketTick && running && state != State.TRADING && state != State.FINISHED && BotScheduler.IsRegistered(chr.id) && BotScheduler.Now() - lastNudgeMs >= 1500)
		{
			lastNudgeMs = BotScheduler.Now();
			currentDelay = 2000 + random.Next(4000);
			BotScheduler.Nudge(chr.id, initialDelay, currentDelay);
		}
	}

	protected BotStateMachine(BotCharacter c)
	{
		chr = c;
		tradeHandler = new BotTradeHandler(c);
	}

	public virtual void UpdateState()
	{
		if (BotScheduler.Now() < suspendedUntil)
		{
			return;
		}
		switch (state)
		{
		case State.IDLE:
			if (CheckRunningOnline())
			{
				state = State.RUNNING;
			}
			break;
		case State.RUNNING:
			CheckPrioritySpeed();
			if (!CheckRunningOnline())
			{
				state = State.FINISHED;
				break;
			}
			if (!IsBusy && chr.trade == null && !chr.moving)
			{
				Func<bool> isMapObserved = IsMapObserved;
				if (isMapObserved != null && isMapObserved())
				{
					Func<bool> isCompanion = IsCompanion;
					if ((isCompanion == null || !isCompanion()) && (chr.stance == 4 || chr.stance == 5))
					{
						output.IdleStanding(chr);
					}
				}
			}
			if (tradeHandler.VerifyPartner())
			{
				tradeStateMachine = new BotTradeStateMachine(this, tradeMode);
				state = State.TRADING;
			}
			break;
		case State.PAUSE:
			if (UsesSharedMarketTick || CheckMainPlayersOnMap())
			{
				state = State.RUNNING;
			}
			break;
		case State.TRADING:
			if (!tradeHandler.VerifyPartner() && (tradeStateMachine.TradeComplete || !tradeStateMachine.OfferAccepted))
			{
				tradeStateMachine = null;
				tradeLogic.Clear(chr);
				tradeHandler.Reset();
				if (UsesSharedMarketTick)
				{
					BusyFor(2000L);
				}
				else
				{
					suspendedUntil = BotScheduler.Now() + 2000;
				}
				state = State.RUNNING;
			}
			else
			{
				tradeStateMachine.Update();
			}
			break;
		case State.FINISHED:
			running = false;
			interactors.ResetRespondants();
			StopScheduledTask();
			state = State.IDLE;
			break;
		default:
			throw new InvalidOperationException("Unexpected bot state: " + state);
		}
	}

	public bool CheckRunningOnline()
	{
		if (running)
		{
			return BotStore.BotLoggedIn(chr.id);
		}
		return false;
	}

	public void CheckForTrades()
	{
		if (tradeLogic.CheckQueue(chr))
		{
			tradeHandler.confirmedPartner = tradeHandler.RawPartner;
		}
	}

	public virtual void StartScheduledTask(long initialDelayMs)
	{
		running = true;
		if (UsesSharedMarketTick)
		{
			BotScheduler.Unregister(chr.id);
			marketDirector.Register(this, initialDelayMs);
		}
		else
		{
			currentDelay = DefaultPeriodMs();
			BotScheduler.Register(chr.id, Tick, initialDelayMs, currentDelay);
		}
	}

	public virtual void StopScheduledTask()
	{
		running = false;
		eventBus?.UnsubscribeAll(this);
		output.Send(BotOutput.Kind.Chalkboard, chr, "");
		if (UsesSharedMarketTick)
		{
			marketDirector.Unregister(this);
		}
		BotWander.Stop(chr);
		BotScheduler.Unregister(chr.id);
		interactors.ResetRespondants();
		interactors.ResetInquirers();
	}

	internal void Tick()
	{
		using (BotPerf.Timer("BotDecision." + botType))
		{
			if (running && BotScheduler.Now() >= suspendedUntil)
			{
				UpdateState();
			}
		}
	}

	protected virtual long DefaultPeriodMs()
	{
		return 2000 + random.Next(4000);
	}

	public virtual bool IsAvailableForAmbientActions()
	{
		return false;
	}

	public virtual void DisplayCommands(BotActor player)
	{
		output.Send(BotOutput.Kind.GuideTalk, player, "1. " + chr.name + "\r\n", 0, player.id);
	}

	public virtual void CheckPrioritySpeed()
	{
		if (CheckMainPlayersOnMap())
		{
			SetPriorityNormal();
		}
		else
		{
			SetPriorityLow();
		}
	}

	public bool CheckIfNotRunningOrPaused()
	{
		if (running && state != State.PAUSE)
		{
			return BotScheduler.Now() < suspendedUntil;
		}
		return true;
	}

	public virtual void DebugLog(string msg)
	{
	}
}
