using System.Collections.Generic;

namespace Server.Bot;

public static class BotPartyGrindProtocol
{
	public static PartyGrindResponse Handle(BotRuntime runtime, PartyGrindRequest request)
	{
		BotActor botActor = runtime.Player(request.characterId);
		BotPartyGrindService partyGrind = runtime.partyGrind;
		if (botActor == null || !botActor.online)
		{
			return new PartyGrindResponse
			{
				ok = false
			};
		}
		bool ok = true;
		if (request.operation == PartyGrindOperation.Start)
		{
			ok = partyGrind.Start(botActor);
		}
		else if (request.operation == PartyGrindOperation.Stop)
		{
			partyGrind.Stop(botActor.id);
		}
		return new PartyGrindResponse
		{
			ok = ok,
			active = partyGrind.IsActive(botActor.id),
			expires = partyGrind.Expiry(botActor.id),
			companions = new List<int>(partyGrind.Companions(botActor.id))
		};
	}
}
