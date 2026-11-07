using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Server.Bot;

public static class BotPerf
{
	private sealed class Stat
	{
		public long count;

		public double sum;

		public double max;
	}

	public struct Scope(string name) : IDisposable
	{
		private readonly string name = name;

		private readonly long start = (Enabled ? Stopwatch.GetTimestamp() : 0);

		public void Dispose()
		{
			if (Enabled && start != 0L)
			{
				Record(name, (double)(Stopwatch.GetTimestamp() - start) * 1000.0 / (double)Stopwatch.Frequency);
			}
		}
	}

	public static bool Enabled;

	public static Action<string> Sink;

	private static readonly Dictionary<string, Stat> stats = new Dictionary<string, Stat>();

	private static readonly List<string> order = new List<string>();

	public static void Record(string name, double ms)
	{
		if (Enabled)
		{
			if (!stats.TryGetValue(name, out var value))
			{
				value = new Stat();
				stats[name] = value;
				order.Add(name);
			}
			value.count++;
			value.sum += ms;
			if (ms > value.max)
			{
				value.max = ms;
			}
		}
	}

	public static Scope Timer(string name)
	{
		return new Scope(name);
	}

	public static void Flush()
	{
		if (!Enabled || order.Count == 0)
		{
			return;
		}
		foreach (string item in order)
		{
			Stat stat = stats[item];
			if (stat.count != 0L)
			{
				Sink?.Invoke("[Bot/Perf] " + item + " n=" + stat.count + " avg=" + (stat.sum / (double)stat.count).ToString("F2") + " max=" + stat.max.ToString("F2") + " total=" + stat.sum.ToString("F1") + "ms");
			}
		}
		stats.Clear();
		order.Clear();
	}

	public static void Clear()
	{
		stats.Clear();
		order.Clear();
	}
}
