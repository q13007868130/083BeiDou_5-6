namespace Server.Bot.Events;

public interface IEventSubscriber
{
	void OnEvent(GameEvent ev);

	bool MatchesFilter(GameEvent ev);
}
