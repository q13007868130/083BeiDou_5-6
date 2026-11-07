using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Server.Bot;

public static class BotTasks
{
	public sealed class Handle
	{
		internal Action callback;

		internal string perfName;

		internal long due;

		internal long period;

		internal long sequence;

		internal bool fixedRate;

		public bool Cancelled { get; private set; }

		public bool Completed { get; internal set; }

		public void Cancel()
		{
			Cancelled = true;
		}
	}

	private static readonly List<Handle> tasks = new List<Handle>();

	private static readonly Stack<List<Handle>> readyPool = new Stack<List<Handle>>();

	private static readonly Comparison<Handle> DueOrder = (Handle a, Handle b) =>
	{
		int num = a.due.CompareTo(b.due);
		return (num == 0) ? a.sequence.CompareTo(b.sequence) : num;
	};

	private static long sequence;

	public static Action<Exception> OnError;

	public static int PendingCount
	{
		get
		{
			int num = 0;
			foreach (Handle task in tasks)
			{
				if (!task.Cancelled && !task.Completed)
				{
					num++;
				}
			}
			return num;
		}
	}

	public static Handle Schedule(Action task, long delayMs, [CallerMemberName] string caller = "", [CallerFilePath] string file = "")
	{
		return Add(task, delayMs, 0L, fixedRate: false, caller, file);
	}

	public static Handle ScheduleFixedRate(Action task, long initialDelayMs, long periodMs, [CallerMemberName] string caller = "", [CallerFilePath] string file = "")
	{
		if (periodMs <= 0)
		{
			throw new ArgumentOutOfRangeException("periodMs");
		}
		return Add(task, initialDelayMs, periodMs, fixedRate: true, caller, file);
	}

	public static Handle ScheduleFixedDelay(Action task, long initialDelayMs, long periodMs, [CallerMemberName] string caller = "", [CallerFilePath] string file = "")
	{
		if (periodMs <= 0)
		{
			throw new ArgumentOutOfRangeException("periodMs");
		}
		return Add(task, initialDelayMs, periodMs, fixedRate: false, caller, file);
	}

	private static Handle Add(Action task, long delay, long period, bool fixedRate, string caller, string file)
	{
		if (task == null)
		{
			throw new ArgumentNullException("task");
		}
		Handle handle = new Handle
		{
			callback = task,
			due = BotScheduler.Now() + Math.Max(0L, delay),
			period = period,
			fixedRate = fixedRate,
			sequence = sequence++,
			perfName = "BotTask." + Path.GetFileNameWithoutExtension(file) + "." + caller
		};
		tasks.Add(handle);
		return handle;
	}

	public static void Drive(long now)
	{
		using (BotPerf.Timer("BotTasks.Drive"))
		{
			List<Handle> list = ((readyPool.Count > 0) ? readyPool.Pop() : new List<Handle>());
			try
			{
				foreach (Handle task in tasks)
				{
					if (!task.Cancelled && !task.Completed && now >= task.due)
					{
						list.Add(task);
					}
				}
				list.Sort(DueOrder);
				foreach (Handle item in list)
				{
					if (item.Cancelled || item.Completed)
					{
						continue;
					}
					long due = item.due;
					try
					{
						using (BotPerf.Timer(item.perfName))
						{
							item.callback();
						}
					}
					catch (Exception obj)
					{
						item.Cancel();
						OnError?.Invoke(obj);
					}
					if (item.period == 0L)
					{
						item.Completed = true;
					}
					else if (!item.Cancelled)
					{
						item.due = (item.fixedRate ? due : BotScheduler.Now()) + item.period;
					}
				}
				tasks.RemoveAll((Handle t) => t.Cancelled || t.Completed);
			}
			finally
			{
				list.Clear();
				readyPool.Push(list);
			}
		}
	}

	public static void Stop()
	{
		foreach (Handle task in tasks)
		{
			task.Cancel();
		}
		tasks.Clear();
	}
}
