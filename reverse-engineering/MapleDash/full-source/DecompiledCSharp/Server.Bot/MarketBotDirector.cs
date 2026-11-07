using System;
using System.Collections.Generic;
using Server.Bot.Environment;

namespace Server.Bot;

public sealed class MarketBotDirector
{
	private readonly Dictionary<int, BotStateMachine> bots = new Dictionary<int, BotStateMachine>();

	private readonly Dictionary<int, long> due = new Dictionary<int, long>();

	private readonly Dictionary<int, long> lastSpeech = new Dictionary<int, long>();

	private readonly Func<DbStore> database;

	private readonly Random random;

	private BotTasks.Handle loop;

	private long configuredTick = -1L;

	private long lastMegaphone;

	private int pathfinds;

	public Action<Exception> OnError;

	public int ActiveCount => bots.Count;

	public MarketBotDirector(Func<DbStore> database, Random random = null)
	{
		this.database = database;
		this.random = random ?? new Random();
	}

	public void Register(BotStateMachine bot, long initialDelay)
	{
		if (bot?.chr != null)
		{
			bots[bot.chr.id] = bot;
			due[bot.chr.id] = BotScheduler.Now() + Math.Max(0L, initialDelay);
			EnsureLoop();
		}
	}

	public void Unregister(BotStateMachine bot)
	{
		if (bot?.chr != null)
		{
			bots.Remove(bot.chr.id);
			due.Remove(bot.chr.id);
		}
	}

	public bool TryChat(BotActor actor)
	{
		if (actor == null)
		{
			return false;
		}
		long num = BotScheduler.Now();
		if (lastSpeech.TryGetValue(actor.id, out var value) && num - value < BotConfig.MarketSpeechGapMs(database()))
		{
			return false;
		}
		lastSpeech[actor.id] = num;
		return true;
	}

	public bool TrySpeak(BotActor actor)
	{
		if (BotConfig.MarketHawkEnabled(database()))
		{
			return TryChat(actor);
		}
		return false;
	}

	public bool TryMegaphone()
	{
		long num = BotScheduler.Now();
		if (num - lastMegaphone < 8000)
		{
			return false;
		}
		lastMegaphone = num;
		return true;
	}

	public bool TryPathfind()
	{
		return ++pathfinds <= BotConfig.MarketPathfindPerTick(database());
	}

	public bool RunPathfind(Action movement)
	{
		if (movement == null || !TryPathfind())
		{
			return false;
		}
		BotTasks.Schedule(movement, 0L, "RunPathfind", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/MarketBotDirector.cs");
		return true;
	}

	public void RefreshTickInterval()
	{
		long num = BotConfig.MarketTickMs(database());
		if (loop == null || loop.Cancelled || loop.Completed || configuredTick != num)
		{
			loop?.Cancel();
			loop = null;
			configuredTick = num;
			if (bots.Count > 0)
			{
				EnsureLoop();
			}
		}
	}

	private void EnsureLoop()
	{
		long num = BotConfig.MarketTickMs(database());
		if (loop == null || loop.Cancelled || loop.Completed || configuredTick != num)
		{
			loop?.Cancel();
			configuredTick = num;
			loop = BotTasks.ScheduleFixedDelay(Tick, num, num, "EnsureLoop", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/MarketBotDirector.cs");
		}
	}

	public void Tick()
	{
		pathfinds = 0;
		long num = BotScheduler.Now();
		List<BotStateMachine> list = new List<BotStateMachine>(bots.Values);
		Dictionary<int, bool> dictionary = new Dictionary<int, bool>();
		foreach (BotStateMachine item in list)
		{
			if (item.chr != null && !dictionary.ContainsKey(item.chr.mapId))
			{
				dictionary[item.chr.mapId] = item.CheckMainPlayersOnMap();
			}
		}
		foreach (BotStateMachine item2 in list)
		{
			BotCharacter chr = item2.chr;
			if (chr == null || !bots.TryGetValue(chr.id, out var value) || item2 != value || (due.TryGetValue(chr.id, out var value2) && num < value2) || (chr.trade == null && BotRecordedActions.IsMoving(chr)))
			{
				continue;
			}
			bool flag = !dictionary.TryGetValue(chr.mapId, out var value3) | value3;
			if (BotConfig.MarketIdleWhenEmpty(database()) && !flag)
			{
				due[chr.id] = num + 8000 + random.Next(4000);
				continue;
			}
			try
			{
				item2.UpdateState();
			}
			catch (Exception obj)
			{
				OnError?.Invoke(obj);
			}
			if (bots.TryGetValue(chr.id, out value) && value == item2)
			{
				due[chr.id] = BotScheduler.Now() + (flag ? (800 + random.Next(1200)) : (4000 + random.Next(3000)));
			}
		}
	}

	public void Stop()
	{
		loop?.Cancel();
		loop = null;
		bots.Clear();
		due.Clear();
	}
}
