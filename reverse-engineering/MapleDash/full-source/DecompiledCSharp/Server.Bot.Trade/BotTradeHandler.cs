namespace Server.Bot.Trade;

public sealed class BotTradeHandler
{
	private readonly BotActor actor;

	public BotActor confirmedPartner;

	public BotActor RawPartner => actor.trade?.partner?.actor;

	public BotTradeHandler(BotActor actor)
	{
		this.actor = actor;
	}

	public bool VerifyPartner()
	{
		if (RawPartner != null && confirmedPartner != null)
		{
			return confirmedPartner.mapId == actor.mapId;
		}
		return false;
	}

	public void Reset()
	{
		confirmedPartner = null;
	}
}
