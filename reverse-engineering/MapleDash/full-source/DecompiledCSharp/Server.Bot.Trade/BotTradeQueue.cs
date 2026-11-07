using System.Collections.Generic;

namespace Server.Bot.Trade;

public sealed class BotTradeQueue
{
	private readonly Dictionary<BotActor, BotActor> requests = new Dictionary<BotActor, BotActor>();

	public void Add(BotActor bot, BotActor partner)
	{
		if (!requests.ContainsKey(bot))
		{
			requests.Add(bot, partner);
		}
	}

	public BotActor Get(BotActor bot)
	{
		if (!requests.TryGetValue(bot, out var value))
		{
			return null;
		}
		return value;
	}

	public bool HasPending(BotActor bot)
	{
		return requests.ContainsKey(bot);
	}

	public void Remove(BotActor bot)
	{
		requests.Remove(bot);
	}
}
