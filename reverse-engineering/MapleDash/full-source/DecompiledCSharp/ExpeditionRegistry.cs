using System.Collections.Generic;

public static class ExpeditionRegistry
{
	private static readonly Dictionary<ExpeditionType, Expedition> expeditions = new Dictionary<ExpeditionType, Expedition>();

	public static int Count => expeditions.Count;

	public static Expedition GetExpedition(ExpeditionType type)
	{
		if (!expeditions.TryGetValue(type, out var value) || value.Disposed)
		{
			return null;
		}
		return value;
	}

	public static bool AddExpedition(Expedition exped)
	{
		if (exped == null || expeditions.ContainsKey(exped.Type))
		{
			return false;
		}
		expeditions[exped.Type] = exped;
		exped.BeginRegistration();
		return true;
	}

	public static void RemoveExpedition(Expedition exped)
	{
		if (exped != null)
		{
			Detach(exped);
		}
	}

	internal static void Detach(Expedition exped)
	{
		if (exped != null && expeditions.TryGetValue(exped.Type, out var value) && value == exped)
		{
			expeditions.Remove(exped.Type);
		}
	}

	public static void Clear()
	{
		foreach (Expedition item in new List<Expedition>(expeditions.Values))
		{
			item.Dispose(log: false);
		}
		expeditions.Clear();
	}

	public static void Tick(long now)
	{
		foreach (Expedition item in new List<Expedition>(expeditions.Values))
		{
			if (!item.Disposed && item.Registering && item.RegistrationEndAt > 0 && now >= item.RegistrationEndAt)
			{
				Detach(item);
				if (!item.Silent)
				{
					Expedition.Notice?.Invoke(6, "[远征队] 远征队组建倒计时已结束. 远征队已经被解散。");
				}
				item.Dispose(log: false);
			}
		}
	}
}
