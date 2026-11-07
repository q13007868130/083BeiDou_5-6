using System.Collections.Generic;

namespace Server.Bot.Events;

public sealed class EventBus
{
	private readonly Dictionary<EventType, List<IEventSubscriber>> subscribers = new Dictionary<EventType, List<IEventSubscriber>>();

	public readonly EventStore store = new EventStore(5000);

	public void Publish(GameEvent ev)
	{
		store.Add(ev);
		if (!subscribers.TryGetValue(ev.type, out var value))
		{
			return;
		}
		foreach (IEventSubscriber item in new List<IEventSubscriber>(value))
		{
			if (item.MatchesFilter(ev))
			{
				item.OnEvent(ev);
			}
		}
	}

	public void Subscribe(EventType type, IEventSubscriber subscriber)
	{
		if (!subscribers.TryGetValue(type, out var value))
		{
			value = (subscribers[type] = new List<IEventSubscriber>());
		}
		value.Add(subscriber);
	}

	public void Unsubscribe(EventType type, IEventSubscriber subscriber)
	{
		if (subscribers.TryGetValue(type, out var value))
		{
			value.Remove(subscriber);
		}
	}

	public void UnsubscribeAll(IEventSubscriber subscriber)
	{
		foreach (List<IEventSubscriber> value in subscribers.Values)
		{
			value.Remove(subscriber);
		}
	}
}
