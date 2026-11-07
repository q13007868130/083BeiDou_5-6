using System.Collections.Generic;

namespace Server.Bot.Events;

public sealed class BotEventBuffer
{
	private readonly Queue<GameEvent> queue = new Queue<GameEvent>();

	private readonly int maxSize;

	public bool IsEmpty => queue.Count == 0;

	public int Size => queue.Count;

	public BotEventBuffer(int maxSize)
	{
		this.maxSize = maxSize;
	}

	public bool Add(GameEvent ev)
	{
		if (queue.Count >= maxSize && queue.Count > 0)
		{
			queue.Dequeue();
		}
		queue.Enqueue(ev);
		return true;
	}

	public GameEvent Poll()
	{
		if (queue.Count != 0)
		{
			return queue.Dequeue();
		}
		return null;
	}

	public void Clear()
	{
		queue.Clear();
	}
}
