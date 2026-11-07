using System;
using Server.Bot.Events;

namespace Server.Bot;

public sealed class BotMapEntryResponder : IEventSubscriber, IDisposable
{
	private readonly EventBus bus;

	private readonly Random random;

	private bool registered;

	public BotMapEntryResponder(EventBus bus, Random random = null)
	{
		this.bus = bus;
		this.random = random ?? new Random();
	}

	public void Register()
	{
		if (!registered)
		{
			registered = true;
			bus.Subscribe(EventType.MAP_ENTERED, this);
		}
	}

	public bool MatchesFilter(GameEvent ev)
	{
		if (ev != null)
		{
			return ev.type == EventType.MAP_ENTERED;
		}
		return false;
	}

	public void OnEvent(GameEvent ev)
	{
		foreach (BotCharacter item in BotRegistry.OnMap(ev.mapId))
		{
			if (item.world == ev.world && item.channel == ev.channel)
			{
				OnBotArrivedObserved(item);
			}
		}
	}

	public void OnBotArrivedObserved(BotCharacter actor)
	{
		if (actor != null)
		{
			BotStateMachine botById = BotStore.GetBotById(actor.id);
			if (botById != null && botById.running)
			{
				botById.NudgeSoon(random.Next(150, 701));
			}
		}
	}

	public void Dispose()
	{
		bus.UnsubscribeAll(this);
		registered = false;
	}
}
