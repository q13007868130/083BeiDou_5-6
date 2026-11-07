namespace Server.Bot.Trade;

public sealed class BotTradeLogic
{
	public readonly BotTradeQueue queue = new BotTradeQueue();

	public readonly BotBlockList blocked = new BotBlockList();

	public bool CheckQueue(BotActor actor)
	{
		if (!queue.HasPending(actor))
		{
			return false;
		}
		if (actor.trade?.partner?.actor == null)
		{
			queue.Remove(actor);
			return false;
		}
		if (blocked.IsBlocked(actor.id, actor.trade.partner.actor.id))
		{
			queue.Remove(actor);
			BotTradeCommands.Decline(actor);
			return false;
		}
		BotTradeCommands.Accept(actor);
		queue.Remove(actor);
		return true;
	}

	public void Clear(BotActor actor)
	{
		queue.Remove(actor);
	}
}
