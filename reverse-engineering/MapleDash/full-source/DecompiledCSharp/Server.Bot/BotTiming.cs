using System;
using System.Collections.Generic;

namespace Server.Bot;

public static class BotTiming
{
	public sealed class Chain
	{
		private readonly List<(Action task, long pause)> steps = new List<(Action, long)>();

		private Func<bool> keepGoing = () => true;

		public Chain StopUnless(Func<bool> condition)
		{
			if (condition != null)
			{
				keepGoing = condition;
			}
			return this;
		}

		public Chain Pause(long ms)
		{
			steps.Add((null, Math.Max(0L, ms)));
			return this;
		}

		public Chain PauseRandom(long min, long max)
		{
			return Pause(RandomDelay(min, max));
		}

		public Chain Run(Action action)
		{
			if (action != null)
			{
				steps.Add((action, 0L));
			}
			return this;
		}

		public void Start()
		{
			Advance(0);
		}

		private void Advance(int index)
		{
			while (index < steps.Count && keepGoing())
			{
				(Action, long) tuple = steps[index++];
				if (tuple.Item1 == null)
				{
					int next = index;
					BotTasks.Schedule(() =>
					{
						Advance(next);
					}, tuple.Item2, "Advance", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/BotTiming.cs");
					break;
				}
				tuple.Item1();
			}
		}
	}

	private static readonly Random random = new Random();

	private static long RandomDelay(long min, long max)
	{
		long num = Math.Max(0L, max - min);
		return min + ((num == 0L) ? 0 : ((long)(random.NextDouble() * (double)(num + 1))));
	}

	public static BotTasks.Handle AfterRandom(long minMs, long maxMs, Action task)
	{
		return BotTasks.Schedule(task, RandomDelay(minMs, maxMs), "AfterRandom", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/BotTiming.cs");
	}

	public static Chain CreateChain()
	{
		return new Chain();
	}
}
