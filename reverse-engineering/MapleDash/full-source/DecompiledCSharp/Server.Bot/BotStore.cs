using System.Collections.Generic;

namespace Server.Bot;

public static class BotStore
{
	private static readonly Dictionary<int, BotStateMachine> activeBotMap = new Dictionary<int, BotStateMachine>();

	private static readonly List<BotActor> respondants = new List<BotActor>();

	private static readonly List<BotActor> inquirers = new List<BotActor>();

	public static IReadOnlyList<BotActor> Respondants => respondants;

	public static void AddRespondant(BotActor actor)
	{
		respondants.Add(actor);
	}

	public static void RemoveRespondant(BotActor actor)
	{
		respondants.Remove(actor);
	}

	public static bool IsRespondant(BotActor actor)
	{
		return respondants.Contains(actor);
	}

	public static void AddInquirer(BotActor actor)
	{
		inquirers.Add(actor);
	}

	public static void RemoveInquirer(BotActor actor)
	{
		inquirers.Remove(actor);
	}

	public static bool IsInquirer(BotActor actor)
	{
		return inquirers.Contains(actor);
	}

	public static bool IsInvisibleBot(int id)
	{
		return id == 100;
	}

	public static void AddActiveBot(int id, BotStateMachine character)
	{
		if (activeBotMap.TryGetValue(id, out var value) && value != character)
		{
			value.StopScheduledTask();
		}
		activeBotMap[id] = character;
	}

	public static void RemoveActiveBot(int id)
	{
		if (activeBotMap.TryGetValue(id, out var value))
		{
			value.StopScheduledTask();
		}
		activeBotMap.Remove(id);
	}

	public static bool BotLoggedIn(int id)
	{
		return activeBotMap.ContainsKey(id);
	}

	public static BotStateMachine GetBotById(int id)
	{
		if (!activeBotMap.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public static Dictionary<int, BotStateMachine> GetAllBots()
	{
		return activeBotMap;
	}

	public static void UpdateAllBots()
	{
		foreach (BotStateMachine item in new List<BotStateMachine>(activeBotMap.Values))
		{
			if (item.running && GetBotById(item.chr.id) == item)
			{
				item.UpdateState();
			}
		}
	}
}
