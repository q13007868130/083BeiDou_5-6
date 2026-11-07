using System.Collections.Generic;
using UnityEngine;

public static class GmTimers
{
	private struct Entry
	{
		public int owner;

		public double until;
	}

	private static readonly List<Entry> entries = new List<Entry>();

	private static int lastFrame = -1;

	public static int Count => entries.Count;

	public static void Add(int owner, int seconds)
	{
		entries.Add(new Entry
		{
			owner = owner,
			until = Time.realtimeSinceStartupAsDouble + (double)seconds
		});
	}

	public static void Tick()
	{
		if (Time.frameCount == lastFrame || entries.Count == 0)
		{
			return;
		}
		lastFrame = Time.frameCount;
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		for (int num = entries.Count - 1; num >= 0; num--)
		{
			if (entries[num].until <= realtimeSinceStartupAsDouble)
			{
				entries.RemoveAt(num);
				NativeSystemMessage.Drop(6, "[计时] 时间到");
			}
		}
	}
}
