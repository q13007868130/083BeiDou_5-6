using System;
using System.Collections.Generic;
using MapleDash.Js;

namespace Server.Field;

public static class MapReactorService
{
	private sealed class ItemActivation
	{
		public MapReactor reactor;

		public MapItem item;

		public long at;
	}

	public static Func<long> Clock = () => 0L;

	public static Dictionary<int, MapReactorTemplate> Templates = new Dictionary<int, MapReactorTemplate>();

	public static Action<MapReactor, int> Act;

	public static Func<string, string> ScriptSource;

	public static Action<int, int, string> ScriptMessage;

	public static Func<int, JsValue> EventInstance;

	public static Func<int, JsValue> Player;

	public static Func<int, JsValue> MapHost;

	public static Func<int, int, float, float, int, bool> SpawnMonsterAt;

	public static Func<MapReactor, bool> AllowHit;

	public static Action<Exception> ScriptError;

	private static readonly List<ItemActivation> itemActivations = new List<ItemActivation>();

	private static int nextObjectId = 1000000000;

	private static readonly Dictionary<int, List<MapReactor>> byMap = new Dictionary<int, List<MapReactor>>();

	public static event Action<MapReactor> Changed;

	public static void ActivateItemReactors(MapItem item)
	{
		if (item == null || item.item == null || item.pickedUp)
		{
			return;
		}
		foreach (MapReactor item2 in Get(item.mapId))
		{
			MapReactorTemplate template = item2.template;
			List<MapReactorTemplate.Event> list = template?.Events(item2.state);
			if (item2.alive && list != null && list.Count != 0 && list[0].type == 100 && item2.eventState >= 0 && item2.eventState < list.Count)
			{
				MapReactorTemplate.Event obj = list[item2.eventState];
				if (obj.item == item.ItemId && obj.quantity == item.Quantity && !(item.x < (float)(item2.x + template.left)) && !(item.x >= (float)(item2.x + template.right)) && !(item.y < (float)(item2.y + template.top)) && !(item.y >= (float)(item2.y + template.bottom)))
				{
					itemActivations.Add(new ItemActivation
					{
						reactor = item2,
						item = item,
						at = Now() + 5000
					});
					break;
				}
			}
		}
	}

	private static void ResolveItemActivations(long now)
	{
		foreach (ItemActivation item2 in itemActivations.FindAll((ItemActivation a) => a.at <= now))
		{
			itemActivations.Remove(item2);
			MapReactor reactor = item2.reactor;
			MapItem item = item2.item;
			if (Find(reactor.mapId, reactor.objectId) != reactor || !reactor.alive || !reactor.shouldCollect || item.pickedUp || MapItemService.Find(item.mapId, item.objectId) != item)
			{
				continue;
			}
			List<MapReactorTemplate.Event> list = reactor.template?.Events(reactor.state);
			if (list != null && list.Count != 0 && list[0].type == 100)
			{
				MapItemService.PickItemDrop(item.mapId, item, 0);
				reactor.shouldCollect = false;
				reactor.previousState = reactor.state;
				reactor.visualRevision++;
				reactor.lastHitAt = now;
				reactor.state = list[0].next;
				int num = reactor.template.Next(reactor.state, 0);
				bool flag = num < reactor.state;
				Changed?.Invoke(reactor);
				if (flag || num == reactor.state)
				{
					Act?.Invoke(reactor, item.ownerCharacterId);
				}
				if (!flag)
				{
					reactor.shouldCollect = true;
				}
			}
		}
	}

	public static long Now()
	{
		if (Clock == null)
		{
			return 0L;
		}
		return Clock();
	}

	public static List<MapReactor> Get(int mapId)
	{
		if (!byMap.TryGetValue(mapId, out var value))
		{
			MapRuntime mapRuntime = MapService.Get(mapId);
			if (mapRuntime == null)
			{
				return new List<MapReactor>();
			}
			value = Build(mapId, mapRuntime);
			byMap[mapId] = value;
		}
		ResolveRespawns(mapId, value, Now());
		return value;
	}

	private static List<MapReactor> Build(int mapId, MapRuntime runtime)
	{
		List<MapReactor> list = new List<MapReactor>(runtime.reactors.Count);
		foreach (MapReactorSpawn reactor in runtime.reactors)
		{
			list.Add(new MapReactor
			{
				template = (Templates.TryGetValue(reactor.id, out var value) ? value : null),
				mapId = mapId,
				id = reactor.id,
				objectId = nextObjectId++,
				x = reactor.x,
				y = reactor.y,
				name = (reactor.name ?? ""),
				maxState = ((reactor.maxState > 0) ? reactor.maxState : 4),
				respawnSeconds = reactor.RespawnSeconds
			});
		}
		return list;
	}

	public static MapReactor Find(int mapId, int objectId)
	{
		foreach (MapReactor item in Get(mapId))
		{
			if (item.objectId == objectId)
			{
				return item;
			}
		}
		return null;
	}

	public static MapReactor Spawn(int mapId, int id, int x, int y, int maxState = 4)
	{
		List<MapReactor> list = Get(mapId);
		if (!byMap.ContainsKey(mapId))
		{
			byMap[mapId] = list;
		}
		MapReactor mapReactor = new MapReactor
		{
			mapId = mapId,
			id = id,
			objectId = nextObjectId++,
			x = x,
			y = y,
			maxState = maxState,
			lastHitAt = Now(),
			template = (Templates.TryGetValue(id, out var value) ? value : null)
		};
		list.Add(mapReactor);
		Changed?.Invoke(mapReactor);
		return mapReactor;
	}

	public static int AliveCount(int mapId, int reactorId)
	{
		int num = 0;
		foreach (MapReactor item in Get(mapId))
		{
			if (item.id == reactorId && item.Hittable)
			{
				num++;
			}
		}
		return num;
	}

	public static bool HitPlayer(int mapId, int objectId, int ownerId)
	{
		MapReactor mapReactor = Find(mapId, objectId);
		if (mapReactor != null && AllowHit != null && !AllowHit(mapReactor))
		{
			return false;
		}
		if (mapReactor == null || !mapReactor.alive)
		{
			return false;
		}
		if (mapReactor.template == null || mapReactor.template.states.Count == 0)
		{
			return Hit(mapId, objectId, null, ownerId) != null;
		}
		List<MapReactorTemplate.Event> list = mapReactor.template.Events(mapReactor.state);
		if (list == null || list.Count == 0 || list[0].type != 0)
		{
			return false;
		}
		mapReactor.previousState = mapReactor.state;
		mapReactor.state = list[0].next;
		mapReactor.visualRevision++;
		mapReactor.lastHitAt = Now();
		int num = mapReactor.template.Next(mapReactor.state, 0);
		bool flag = num < mapReactor.state;
		if (flag)
		{
			mapReactor.alive = false;
			mapReactor.respawnAt = ((mapReactor.respawnSeconds > 0) ? (Now() + (long)mapReactor.respawnSeconds * 1000L) : 0);
		}
		Changed?.Invoke(mapReactor);
		if (flag || num == mapReactor.state)
		{
			Act?.Invoke(mapReactor, ownerId);
		}
		return true;
	}

	public static MapReactor ForceHit(int mapId, int objectId)
	{
		MapReactor mapReactor = Find(mapId, objectId);
		if (mapReactor == null)
		{
			return null;
		}
		mapReactor.previousState = mapReactor.state;
		mapReactor.state++;
		mapReactor.visualRevision++;
		mapReactor.lastHitAt = Now();
		Changed?.Invoke(mapReactor);
		return mapReactor;
	}

	public static MapReactor Hit(int mapId, int objectId, IList<InventoryEntry> drops = null, int ownerId = 0, int partyId = -1, bool playerDrop = false)
	{
		MapReactor mapReactor = Find(mapId, objectId);
		if (mapReactor == null)
		{
			return null;
		}
		if (!mapReactor.Hittable)
		{
			return mapReactor;
		}
		long lastHitAt = Now();
		mapReactor.previousState = mapReactor.state;
		mapReactor.visualRevision++;
		mapReactor.state++;
		mapReactor.lastHitAt = lastHitAt;
		bool flag = mapReactor.state >= mapReactor.maxState;
		if (flag)
		{
			Destroy(mapId, mapReactor, drops, ownerId, partyId, playerDrop);
		}
		else
		{
			Changed?.Invoke(mapReactor);
		}
		if (flag)
		{
			Act?.Invoke(mapReactor, ownerId);
		}
		return mapReactor;
	}

	public static void Destroy(int mapId, MapReactor reactor, IList<InventoryEntry> drops = null, int ownerId = 0, int partyId = -1, bool playerDrop = false)
	{
		if (reactor == null || !reactor.alive)
		{
			return;
		}
		reactor.alive = false;
		reactor.state = reactor.maxState;
		if (drops != null)
		{
			foreach (InventoryEntry drop in drops)
			{
				if (drop != null)
				{
					MapItemService.SpawnItemDrop(mapId, reactor.objectId, ownerId, partyId, drop, reactor.x, reactor.y, ffaDrop: false, playerDrop);
				}
			}
		}
		reactor.respawnAt = ((reactor.respawnSeconds > 0) ? (Now() + (long)reactor.respawnSeconds * 1000L) : 0);
		Changed?.Invoke(reactor);
	}

	public static void Respawn(int mapId, MapReactor reactor)
	{
		if (reactor != null)
		{
			reactor.alive = true;
			reactor.state = 0;
			reactor.previousState = -1;
			reactor.visualRevision++;
			reactor.lastHitAt = Now();
			reactor.respawnAt = 0L;
			Changed?.Invoke(reactor);
		}
	}

	public static void ResetActions(int mapId, MapReactor reactor, int state)
	{
		if (reactor != null)
		{
			reactor.previousState = reactor.state;
			reactor.state = Math.Max(0, state);
			reactor.visualRevision++;
			reactor.lastHitAt = 0L;
			Changed?.Invoke(reactor);
		}
	}

	public static void RespawnAll(int mapId)
	{
		foreach (MapReactor item in Get(mapId))
		{
			if (item != null)
			{
				item.alive = true;
				item.state = 0;
				item.previousState = -1;
				item.respawnAt = 0L;
				item.shouldCollect = true;
				item.visualRevision++;
				Changed?.Invoke(item);
			}
		}
	}

	public static void Tick(long now)
	{
		foreach (KeyValuePair<int, List<MapReactor>> item in byMap)
		{
			ResolveRespawns(item.Key, item.Value, now);
		}
		ResolveItemActivations(now);
		NativeReactorDrops.Tick(now);
	}

	private static void ResolveRespawns(int mapId, List<MapReactor> list, long now)
	{
		if (now <= 0)
		{
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			MapReactor mapReactor = list[i];
			if (!mapReactor.alive && mapReactor.respawnAt > 0 && now >= mapReactor.respawnAt)
			{
				Respawn(mapId, mapReactor);
			}
		}
	}

	public static void Clear(int mapId)
	{
		byMap.Remove(mapId);
		itemActivations.RemoveAll((ItemActivation a) => a.reactor.mapId == mapId);
		NativeReactorDrops.Clear(mapId);
	}

	public static void ClearAll()
	{
		byMap.Clear();
		itemActivations.Clear();
		NativeReactorDrops.Clear();
	}
}
