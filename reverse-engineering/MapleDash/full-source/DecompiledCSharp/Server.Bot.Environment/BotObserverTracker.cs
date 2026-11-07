using System;
using System.Collections.Generic;
using Server.Field;

namespace Server.Bot.Environment;

public sealed class BotObserverTracker : IDisposable
{
	private readonly Func<IEnumerable<BotActor>> players;

	private readonly Func<int, MapRuntime> maps;

	private readonly BotWorldGraph graph;

	private readonly Dictionary<int, long> stickyUntil = new Dictionary<int, long>();

	private readonly Dictionary<int, long> forced = new Dictionary<int, long>();

	private HashSet<int> full = new HashSet<int>();

	private HashSet<int> halo = new HashSet<int>();

	private HashSet<int> active = new HashSet<int>();

	private BotTasks.Handle task;

	public bool Started
	{
		get
		{
			if (task != null)
			{
				return !task.Cancelled;
			}
			return false;
		}
	}

	public int FullCount => full.Count;

	public int HaloCount => halo.Count;

	public int ActiveCount => active.Count;

	public BotObserverTracker(Func<IEnumerable<BotActor>> players, Func<int, MapRuntime> maps, BotWorldGraph graph)
	{
		this.players = players;
		this.maps = maps;
		this.graph = graph;
	}

	public void Start()
	{
		if (!Started)
		{
			task = BotTasks.ScheduleFixedRate(SafeRefresh, 0L, 2000L, "Start", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Environment/BotObserverTracker.cs");
		}
	}

	public void Dispose()
	{
		task?.Cancel();
		task = null;
	}

	public void MarkObservedNow(int id)
	{
		forced[id] = BotScheduler.Now() + 2000;
	}

	private bool Forced(int id)
	{
		if (!forced.TryGetValue(id, out var value))
		{
			return false;
		}
		if (value < BotScheduler.Now())
		{
			forced.Remove(id);
			return false;
		}
		return true;
	}

	public bool IsFull(int id)
	{
		if (!full.Contains(id))
		{
			return Forced(id);
		}
		return true;
	}

	public bool IsHalo(int id)
	{
		return halo.Contains(id);
	}

	public bool IsActive(int id)
	{
		if (!active.Contains(id))
		{
			return Forced(id);
		}
		return true;
	}

	public bool HasRealPlayerNow(int id)
	{
		foreach (BotActor item in players())
		{
			if (item != null && !BotHelpers.IsBot(item) && item.mapId == id)
			{
				return true;
			}
		}
		return false;
	}

	private void SafeRefresh()
	{
		try
		{
			Refresh();
		}
		catch (Exception)
		{
		}
	}

	public void Refresh()
	{
		HashSet<int> hashSet = new HashSet<int>();
		List<BotActor> list = new List<BotActor>();
		foreach (BotActor item in players())
		{
			if (item != null && !BotHelpers.IsBot(item))
			{
				hashSet.Add(item.mapId);
				list.Add(item);
			}
		}
		HashSet<int> hashSet2 = new HashSet<int>();
		if (graph != null)
		{
			foreach (BotActor item2 in list)
			{
				MapRuntime mapRuntime = maps(item2.mapId);
				if (mapRuntime == null)
				{
					continue;
				}
				HashSet<int> hashSet3 = new HashSet<int>(graph.PortalNeighbors(item2.mapId));
				foreach (MapPortal portal in mapRuntime.portals)
				{
					int targetMap = portal.targetMap;
					double num = item2.x - (float)portal.x;
					double num2 = item2.y - (float)portal.y;
					if (targetMap > 0 && !hashSet.Contains(targetMap) && hashSet3.Contains(targetMap) && num * num + num2 * num2 <= 122500.0)
					{
						hashSet2.Add(targetMap);
					}
				}
			}
		}
		long num3 = BotScheduler.Now();
		long value = num3 + 4000;
		foreach (int item3 in hashSet)
		{
			stickyUntil[item3] = value;
		}
		foreach (int item4 in hashSet2)
		{
			stickyUntil[item4] = value;
		}
		foreach (int item5 in new List<int>(stickyUntil.Keys))
		{
			if (stickyUntil[item5] <= num3)
			{
				stickyUntil.Remove(item5);
			}
		}
		active = new HashSet<int>(stickyUntil.Keys);
		full = hashSet;
		halo = hashSet2;
	}
}
