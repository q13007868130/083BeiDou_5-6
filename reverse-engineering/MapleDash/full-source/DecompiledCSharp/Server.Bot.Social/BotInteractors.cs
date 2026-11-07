using System.Collections.Generic;

namespace Server.Bot.Social;

public sealed class BotInteractors
{
	public readonly List<BotActor> respondants = new List<BotActor>();

	public readonly List<BotActor> inquirers = new List<BotActor>();

	public BotActor Respondant
	{
		get
		{
			if (respondants.Count != 0)
			{
				return respondants[0];
			}
			return null;
		}
	}

	public BotActor Inquirer => inquirers[0];

	public void SetRespondant(BotActor actor)
	{
		BotStore.AddRespondant(actor);
		respondants.Add(actor);
	}

	public bool IsRespondant(BotActor actor)
	{
		return respondants.Contains(actor);
	}

	public void RemoveRespondant(BotActor actor)
	{
		BotStore.RemoveRespondant(actor);
		respondants.Remove(actor);
	}

	public void ResetRespondants()
	{
		foreach (BotActor respondant in respondants)
		{
			BotStore.RemoveRespondant(respondant);
		}
		respondants.Clear();
	}

	public void SetInquirer(BotActor actor)
	{
		BotStore.AddInquirer(actor);
		if (!inquirers.Contains(actor))
		{
			inquirers.Add(actor);
		}
	}

	public bool IsInquirer(BotActor actor)
	{
		return inquirers.Contains(actor);
	}

	public void RemoveInquirer(BotActor actor)
	{
		BotStore.RemoveInquirer(actor);
		inquirers.Remove(actor);
	}

	public void ResetInquirers()
	{
		foreach (BotActor inquirer in inquirers)
		{
			BotStore.RemoveInquirer(inquirer);
		}
		inquirers.Clear();
	}

	public bool IsMessageFromRespondant(ChatMessage message)
	{
		return message.sender == Respondant;
	}
}
