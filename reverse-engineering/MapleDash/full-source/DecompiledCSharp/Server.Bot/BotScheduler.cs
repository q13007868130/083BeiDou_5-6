using System;
using System.Collections.Generic;

namespace Server.Bot;

public static class BotScheduler
{
	private sealed class Entry
	{
		public Action tick;

		public long periodMs;

		public long nextDueMs;

		public long pendingDueMs;

		public bool noThrottle;

		public bool ticking;
	}

	private const long DriverPeriodMs = 250L;

	private const long GovernorWindowMs = 5000L;

	private const long LagHiMs = 1000L;

	private const long LagLoMs = 300L;

	private const double FactorMax = 4.0;

	private const double FactorStep = 1.5;

	private const int MinTicksPerRound = 4;

	private static readonly Dictionary<int, Entry> entries = new Dictionary<int, Entry>();

	private static double throttleFactor = 1.0;

	private static long governorWindowStart;

	private static long windowLagSum;

	private static long windowDispatches;

	private static bool started;

	private static long nextDriverMs;

	private static long previousFrameMs;

	private static bool hasFrame;

	private static float deltaSeconds;

	public static long Clock;

	public static double ThrottleFactor()
	{
		return throttleFactor;
	}

	public static void Register(int botId, Action tick, long initialDelayMs, long periodMs)
	{
		EnsureDriver();
		if (!entries.ContainsKey(botId))
		{
			entries[botId] = new Entry
			{
				tick = tick,
				periodMs = periodMs,
				nextDueMs = Now() + initialDelayMs,
				pendingDueMs = 0L,
				noThrottle = false,
				ticking = false
			};
		}
	}

	public static void Unregister(int botId)
	{
		entries.Remove(botId);
	}

	public static bool IsRegistered(int botId)
	{
		return entries.ContainsKey(botId);
	}

	public static void SetNoThrottle(int botId, bool on)
	{
		if (entries.TryGetValue(botId, out var value))
		{
			value.noThrottle = on;
		}
	}

	public static int Size()
	{
		return entries.Count;
	}

	public static void Reschedule(int botId, long periodMs)
	{
		if (entries.TryGetValue(botId, out var value))
		{
			value.periodMs = periodMs;
			RequestFireAt(value, Now() + periodMs);
		}
	}

	public static void Nudge(int botId, long initialDelayMs, long periodMs)
	{
		if (entries.TryGetValue(botId, out var value))
		{
			value.periodMs = periodMs;
			RequestFireAt(value, Now() + initialDelayMs);
		}
	}

	private static void RequestFireAt(Entry e, long at)
	{
		e.pendingDueMs = at;
		if (e.nextDueMs != long.MaxValue)
		{
			e.nextDueMs = at;
		}
	}

	public static void Stop()
	{
		entries.Clear();
		started = false;
		throttleFactor = 1.0;
		governorWindowStart = (windowLagSum = (windowDispatches = 0L));
		hasFrame = false;
		deltaSeconds = 0f;
	}

	public static long Now()
	{
		return Clock;
	}

	public static float Dt()
	{
		return deltaSeconds;
	}

	private static void EnsureDriver()
	{
		if (!started)
		{
			started = true;
			nextDriverMs = Now() + 250;
		}
	}

	public static void Drive()
	{
		using (BotPerf.Timer("BotScheduler.Drive"))
		{
			long num = Now();
			deltaSeconds = (hasFrame ? ((float)Math.Max(0L, num - previousFrameMs) / 1000f) : 0f);
			previousFrameMs = num;
			hasFrame = true;
			BotTasks.Drive(num);
			BotWander.AdvanceAll(num);
			if (!IsOperating() || num < nextDriverMs)
			{
				return;
			}
			nextDriverMs = num + 250;
			long num2 = 0L;
			int num3 = 0;
			bool flag = false;
			foreach (KeyValuePair<int, Entry> item in new List<KeyValuePair<int, Entry>>(entries))
			{
				if (entries.TryGetValue(item.Key, out var value) && value == item.Value && num >= value.nextDueMs && !value.ticking)
				{
					if (num3 >= 4 && !BotWorkPump.Has(0.5f))
					{
						flag = true;
						break;
					}
					value.ticking = true;
					long val = num - value.nextDueMs;
					value.nextDueMs = long.MaxValue;
					if (value.pendingDueMs > 0 && num >= value.pendingDueMs)
					{
						value.pendingDueMs = 0L;
					}
					num2 += Math.Max(0L, val);
					num3++;
					RunTick(value, num);
				}
			}
			if (flag)
			{
				nextDriverMs = num;
			}
			GovernorTick(num, num2, num3);
		}
	}

	private static void RunTick(Entry e, long now)
	{
		try
		{
			e.tick();
		}
		catch
		{
		}
		finally
		{
			e.ticking = false;
		}
		if (IsOperating())
		{
			long num = now + (e.noThrottle ? e.periodMs : ((long)((double)e.periodMs * throttleFactor)));
			if (e.pendingDueMs > 0)
			{
				long pendingDueMs = e.pendingDueMs;
				e.pendingDueMs = 0L;
				num = Math.Min(num, Math.Max(pendingDueMs, now));
			}
			e.nextDueMs = num;
		}
	}

	private static void GovernorTick(long now, long lagSumMs, int dispatches)
	{
		windowLagSum += lagSumMs;
		windowDispatches += dispatches;
		if (governorWindowStart == 0L)
		{
			governorWindowStart = now;
		}
		else if (now - governorWindowStart >= 5000)
		{
			long num = ((windowDispatches > 0) ? (windowLagSum / windowDispatches) : 0);
			windowLagSum = 0L;
			windowDispatches = 0L;
			governorWindowStart = now;
			if (num > 1000 && throttleFactor < 4.0)
			{
				throttleFactor = Math.Min(4.0, throttleFactor * 1.5);
			}
			else if (num < 300 && throttleFactor > 1.0)
			{
				throttleFactor = Math.Max(1.0, throttleFactor / 1.5);
			}
		}
	}

	public static void RunAsync(Action task)
	{
		task();
	}

	private static bool IsOperating()
	{
		return started;
	}
}
