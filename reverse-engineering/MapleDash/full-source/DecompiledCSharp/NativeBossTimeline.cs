using System;
using System.Collections.Generic;

public sealed class NativeBossTimeline
{
	private sealed class Entry
	{
		public long at;

		public long order;

		public Func<bool> valid;

		public Action action;
	}

	private readonly List<Entry> pending = new List<Entry>();

	private long sequence;

	private long generation;

	public void Schedule(long now, long delay, Func<bool> valid, Action action)
	{
		pending.Add(new Entry
		{
			at = now + Math.Max(0L, delay),
			order = sequence++,
			valid = valid,
			action = action
		});
	}

	public void Clear()
	{
		generation++;
		pending.Clear();
	}

	public void Tick(long now)
	{
		long num = generation;
		List<Entry> list = new List<Entry>();
		for (int num2 = pending.Count - 1; num2 >= 0; num2--)
		{
			if (pending[num2].at <= now)
			{
				list.Add(pending[num2]);
				pending.RemoveAt(num2);
			}
		}
		list.Sort((Entry a, Entry b) =>
		{
			int num3 = a.at.CompareTo(b.at);
			return (num3 == 0) ? a.order.CompareTo(b.order) : num3;
		});
		foreach (Entry item in list)
		{
			if (num != generation)
			{
				break;
			}
			if (item.valid == null || item.valid())
			{
				item.action();
			}
		}
	}
}
