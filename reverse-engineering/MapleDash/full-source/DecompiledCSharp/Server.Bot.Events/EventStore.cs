using System;
using System.Collections.Generic;

namespace Server.Bot.Events;

public sealed class EventStore
{
	private readonly GameEvent[] buffer;

	private int head;

	private int size;

	public EventStore(int capacity)
	{
		buffer = new GameEvent[capacity];
	}

	public void Add(GameEvent ev)
	{
		buffer[head] = ev;
		head = (head + 1) % buffer.Length;
		if (size < buffer.Length)
		{
			size++;
		}
	}

	public List<GameEvent> GetEventsByType(EventType type, long sinceTimestamp = long.MaxValue)
	{
		return Filter((GameEvent e) => e.type == type && e.timestamp >= sinceTimestamp);
	}

	public List<GameEvent> GetEventsByLocation(int world, int channel, int? mapId = null)
	{
		return Filter((GameEvent e) => e.world == world && e.channel == channel && (!mapId.HasValue || e.mapId == mapId));
	}

	public List<GameEvent> GetEventsInTimeRange(long start, long end)
	{
		return Filter((GameEvent e) => e.timestamp >= start && e.timestamp <= end);
	}

	private List<GameEvent> Filter(Predicate<GameEvent> predicate)
	{
		List<GameEvent> list = new List<GameEvent>();
		for (int i = 0; i < size; i++)
		{
			if (buffer[i] != null && predicate(buffer[i]))
			{
				list.Add(buffer[i]);
			}
		}
		return list;
	}

	public List<GameEvent> GetRecentEvents(int count)
	{
		List<GameEvent> list = new List<GameEvent>();
		int num = Math.Min(count, size);
		int num2 = (head - num + buffer.Length) % buffer.Length;
		for (int i = 0; i < num; i++)
		{
			if (buffer[(num2 + i) % buffer.Length] != null)
			{
				list.Add(buffer[(num2 + i) % buffer.Length]);
			}
		}
		return list;
	}
}
