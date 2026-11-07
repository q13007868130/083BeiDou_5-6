using System;
using System.Collections.Generic;

namespace Server.Field;

public static class MapItemService
{
	public static Func<long> Clock = () => 0L;

	public static int ItemLimitOnMap = 200;

	public static long ExpireMilliseconds = 180000L;

	public const long OwnershipWindowMs = 15000L;

	public const int DropLimitBit = 4194304;

	private static readonly Dictionary<int, List<MapItem>> byMap = new Dictionary<int, List<MapItem>>();

	private static readonly Dictionary<int, List<MapItem>> registered = new Dictionary<int, List<MapItem>>();

	private static int nextObjectId = 1000000;

	public static Func<int, int> PartyOf;

	public static Func<int, int, bool> IsPartyMember;

	private static readonly Dictionary<int, float[]> dropLimits = new Dictionary<int, float[]>();

	public const int FmEntranceMapId = 910000000;

	public static event Action<MapItem> Spawned;

	public static event Action<MapItem, int> Removed;

	public static long Now()
	{
		if (Clock == null)
		{
			return 0L;
		}
		return Clock();
	}

	public static bool CalcPointBelow(int mapId, float x, float y, out float outY)
	{
		outY = 0f;
		MapRuntime mapRuntime = MapService.Get(mapId);
		if (mapRuntime == null)
		{
			return false;
		}
		float? num = mapRuntime.PointBelow(x, y);
		if (!num.HasValue)
		{
			return false;
		}
		outY = num.Value;
		return true;
	}

	private static float[] DropLimits(int mapId, MapRuntime map)
	{
		if (dropLimits.TryGetValue(mapId, out var value))
		{
			return value;
		}
		float awayx = (float)(map.left + map.right) / 2f;
		float num = map.top;
		float item = BsearchDropPos(mapId, map.left, num, awayx, num).Item1;
		float item2 = BsearchDropPos(mapId, map.right, num, awayx, num).Item1;
		float[] array = new float[2]
		{
			item + 14f,
			item2 - 14f
		};
		dropLimits[mapId] = array;
		return array;
	}

	private static (float, float) BsearchDropPos(int mapId, float homex, float y, float awayx, float fallbackY)
	{
		float item = homex;
		float item2 = fallbackY;
		float y2 = y - 85f;
		do
		{
			float num = (awayx - homex) / 2f;
			float num2 = homex + num;
			if (CalcPointBelow(mapId, num2, y2, out var outY))
			{
				awayx = num2;
				item = num2;
				item2 = outY;
			}
			else
			{
				homex = num2;
			}
		}
		while (Math.Abs(homex - awayx) > 5f);
		return (item, item2);
	}

	private static bool InMapArea(MapRuntime map, float x, float y)
	{
		if (x >= (float)map.left && x <= (float)map.right && y >= (float)map.top)
		{
			return y <= (float)map.bottom;
		}
		return false;
	}

	public static void CalcDropPos(int mapId, float x, float y, float fallbackX, float fallbackY, out float outX, out float outY)
	{
		outX = fallbackX;
		outY = fallbackY;
		MapRuntime mapRuntime = MapService.Get(mapId);
		if (mapRuntime != null)
		{
			float[] array = DropLimits(mapId, mapRuntime);
			float num = x;
			if (num < array[0])
			{
				num = array[0];
			}
			else if (num > array[1])
			{
				num = array[1];
			}
			if (!CalcPointBelow(mapId, num, y - 85f, out var outY2))
			{
				(num, outY2) = BsearchDropPos(mapId, num, y, fallbackX, fallbackY);
			}
			if (InMapArea(mapRuntime, num, outY2))
			{
				outX = num;
				outY = outY2;
			}
		}
	}

	public static void CalcPlayerDropPos(int mapId, float x, float y, float fallbackX, float fallbackY, out float outX, out float outY)
	{
		outX = fallbackX;
		outY = fallbackY;
		MapRuntime mapRuntime = MapService.Get(mapId);
		if (mapRuntime != null)
		{
			float[] array = DropLimits(mapId, mapRuntime);
			float num = x;
			if (num < array[0])
			{
				num = array[0];
			}
			else if (num > array[1])
			{
				num = array[1];
			}
			float outX2;
			if (!CalcPointBelow(mapId, num, y - 6f, out var outY2))
			{
				CalcDropPos(mapId, num, y, fallbackX, fallbackY, out outX2, out outY2);
			}
			else
			{
				outX2 = num;
			}
			if (InMapArea(mapRuntime, outX2, outY2))
			{
				outX = outX2;
				outY = outY2;
			}
		}
	}

	public static void CalcItemDropPos(int mapId, float x, float y, bool playerDrop, out float outX, out float outY)
	{
		if (playerDrop && mapId == 910000000)
		{
			CalcPlayerDropPos(mapId, x, y, x, y, out outX, out outY);
		}
		else
		{
			CalcDropPos(mapId, x, y, x, y, out outX, out outY);
		}
	}

	private static bool DropLimited(int mapId)
	{
		MapRuntime mapRuntime = MapService.Get(mapId);
		if (mapRuntime != null)
		{
			return (mapRuntime.fieldLimit & 0x400000) != 0;
		}
		return false;
	}

	private static MapItem NewItem(int mapId, InventoryEntry item, int dropperId, int ownerId, int partyId, byte dropType, bool playerDrop, float x, float y)
	{
		return new MapItem
		{
			objectId = nextObjectId++,
			mapId = mapId,
			item = item,
			meso = 0,
			dropperId = dropperId,
			type = dropType,
			playerDrop = playerDrop,
			x = x,
			y = y,
			dropTime = Now(),
			partyDrop = (partyId != -1),
			partyOwnerId = partyId,
			ownerCharacterId = ownerId
		};
	}

	private static MapItem NewMeso(int mapId, int meso, int dropperId, int ownerId, int partyId, byte dropType, bool playerDrop, float x, float y)
	{
		return new MapItem
		{
			objectId = nextObjectId++,
			mapId = mapId,
			item = null,
			meso = meso,
			dropperId = dropperId,
			type = dropType,
			playerDrop = playerDrop,
			x = x,
			y = y,
			dropTime = Now(),
			partyDrop = (partyId != -1),
			partyOwnerId = partyId,
			ownerCharacterId = ownerId
		};
	}

	private static void Register(int mapId, MapItem mdrop, bool evictable)
	{
		if (!byMap.TryGetValue(mapId, out var value))
		{
			value = (byMap[mapId] = new List<MapItem>());
		}
		value.Add(mdrop);
		if (evictable)
		{
			MapRuntime mapRuntime = MapService.Get(mapId);
			if (mapRuntime == null || !mapRuntime.everlast)
			{
				mdrop.expiresAt = Now() + ExpireMilliseconds;
			}
			if (!registered.TryGetValue(mapId, out var value2))
			{
				value2 = (registered[mapId] = new List<MapItem>());
			}
			while (ItemLimitOnMap > 0 && value2.Count >= ItemLimitOnMap)
			{
				MapItem mapitem = value2[0];
				value2.RemoveAt(0);
				MakeDisappear(mapId, mapitem);
			}
			value2.Add(mdrop);
		}
	}

	public static MapItem SpawnItemDrop(int mapId, int dropperId, int ownerId, int partyId, InventoryEntry item, float x, float y, bool ffaDrop, bool playerDrop)
	{
		return SpawnItemDrop(mapId, dropperId, ownerId, partyId, item, x, y, (byte)(ffaDrop ? 2u : 0u), playerDrop);
	}

	public static MapItem SpawnItemDrop(int mapId, int dropperId, int ownerId, int partyId, InventoryEntry item, float x, float y, byte dropType, bool playerDrop)
	{
		if (item == null)
		{
			return null;
		}
		if (DropLimited(mapId))
		{
			DisappearingItemDrop(mapId, dropperId, ownerId, partyId, item, x, y);
			return null;
		}
		CalcItemDropPos(mapId, x, y, playerDrop, out var outX, out var outY);
		MapItem mapItem = NewItem(mapId, item, dropperId, ownerId, partyId, dropType, playerDrop, outX, outY);
		Register(mapId, mapItem, evictable: true);
		Spawned?.Invoke(mapItem);
		return mapItem;
	}

	public static MapItem SpawnItemDropNoExpire(int mapId, int dropperId, int ownerId, int partyId, InventoryEntry item, float x, float y, bool ffaDrop, bool playerDrop)
	{
		if (item == null)
		{
			return null;
		}
		if (DropLimited(mapId))
		{
			DisappearingItemDrop(mapId, dropperId, ownerId, partyId, item, x, y);
			return null;
		}
		CalcItemDropPos(mapId, x, y, playerDrop, out var outX, out var outY);
		MapItem mapItem = NewItem(mapId, item, dropperId, ownerId, partyId, (byte)(ffaDrop ? 2u : 0u), playerDrop, outX, outY);
		Register(mapId, mapItem, evictable: false);
		Spawned?.Invoke(mapItem);
		return mapItem;
	}

	public static MapItem DropFromReactor(int mapId, int dropperId, int ownerId, InventoryEntry item, float x, float y, float reactorX, float reactorY, int questId = -1)
	{
		if (item == null)
		{
			return null;
		}
		int num = PartyOf?.Invoke(ownerId) ?? (-1);
		CalcDropPos(mapId, x, y, reactorX, reactorY, out var outX, out var outY);
		MapItem mapItem = NewItem(mapId, item, dropperId, ownerId, num, (num != -1) ? ((byte)1) : ((byte)0), playerDrop: false, outX, outY);
		mapItem.questId = questId;
		Register(mapId, mapItem, evictable: true);
		Spawned?.Invoke(mapItem);
		return mapItem;
	}

	public static MapItem SpawnMesoDrop(int mapId, int meso, float x, float y, int dropperId, int ownerId, int partyId, bool playerDrop, byte dropType)
	{
		if (meso <= 0)
		{
			return null;
		}
		CalcDropPos(mapId, x, y, x, y, out var outX, out var outY);
		MapItem mapItem = NewMeso(mapId, meso, dropperId, ownerId, partyId, dropType, playerDrop, outX, outY);
		Register(mapId, mapItem, evictable: true);
		Spawned?.Invoke(mapItem);
		return mapItem;
	}

	public static void DisappearingItemDrop(int mapId, int dropperId, int ownerId, int partyId, InventoryEntry item, float x, float y)
	{
		CalcDropPos(mapId, x, y, x, y, out var outX, out var outY);
		MapItem mapItem = NewItem(mapId, item, dropperId, ownerId, partyId, 1, playerDrop: false, outX, outY);
		Spawned?.Invoke(mapItem);
		Removed?.Invoke(mapItem, 3);
	}

	public static void DisappearingMesoDrop(int mapId, int meso, float x, float y, int dropperId, int ownerId, int partyId)
	{
		CalcDropPos(mapId, x, y, x, y, out var outX, out var outY);
		MapItem mapItem = NewMeso(mapId, meso, dropperId, ownerId, partyId, 1, playerDrop: false, outX, outY);
		Spawned?.Invoke(mapItem);
		Removed?.Invoke(mapItem, 3);
	}

	public static void NotifyRemoved(MapItem mdrop, int reason)
	{
		Removed?.Invoke(mdrop, reason);
	}

	public static void PickItemDrop(int mapId, MapItem mdrop, int reason, int pickerId = 0, int petIndex = -1)
	{
		if (mdrop != null)
		{
			Detach(mapId, mdrop);
			mdrop.pickedUp = true;
			mdrop.pickupCharacterId = pickerId;
			mdrop.pickupPetIndex = petIndex;
			Removed?.Invoke(mdrop, reason);
		}
	}

	public static bool MakeDisappear(int mapId, MapItem mapitem)
	{
		if (mapitem == null)
		{
			return false;
		}
		if (Find(mapId, mapitem.objectId) != mapitem)
		{
			return false;
		}
		if (mapitem.pickedUp)
		{
			return true;
		}
		PickItemDrop(mapId, mapitem, 0);
		return true;
	}

	private static void Detach(int mapId, MapItem mdrop)
	{
		if (byMap.TryGetValue(mapId, out var value))
		{
			value.Remove(mdrop);
		}
		if (registered.TryGetValue(mapId, out var value2))
		{
			value2.Remove(mdrop);
		}
	}

	public static MapItem Find(int mapId, int objectId)
	{
		if (!byMap.TryGetValue(mapId, out var value))
		{
			return null;
		}
		foreach (MapItem item in value)
		{
			if (item.objectId == objectId)
			{
				return item;
			}
		}
		return null;
	}

	public static void Tick(long now)
	{
		List<MapItem> list = new List<MapItem>();
		foreach (List<MapItem> value in byMap.Values)
		{
			foreach (MapItem item in value)
			{
				if (item.expiresAt < now)
				{
					list.Add(item);
				}
			}
		}
		foreach (MapItem item2 in list)
		{
			MakeDisappear(item2.mapId, item2);
		}
	}

	public static int DroppedItemCount(int mapId)
	{
		if (!byMap.TryGetValue(mapId, out var value))
		{
			return 0;
		}
		return value.Count;
	}

	public static List<MapItem> DroppedItems(int mapId)
	{
		if (!byMap.TryGetValue(mapId, out var value))
		{
			return new List<MapItem>();
		}
		return new List<MapItem>(value);
	}

	public static List<MapItem> ItemsInRange(int mapId, float x, float y, double rangeSq)
	{
		List<MapItem> list = new List<MapItem>();
		if (!byMap.TryGetValue(mapId, out var value))
		{
			return list;
		}
		foreach (MapItem item in value)
		{
			double num = item.x - x;
			double num2 = item.y - y;
			if (num * num + num2 * num2 <= rangeSq)
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static List<MapItem> ItemsInRect(int mapId, int left, int top, int width, int height)
	{
		List<MapItem> list = new List<MapItem>();
		if (!byMap.TryGetValue(mapId, out var value))
		{
			return list;
		}
		foreach (MapItem item in value)
		{
			int num = (int)item.x;
			int num2 = (int)item.y;
			if (num >= left && num < left + width && num2 >= top && num2 < top + height)
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static void ClearMap(int mapId)
	{
		if (byMap.TryGetValue(mapId, out var value))
		{
			foreach (MapItem item in new List<MapItem>(value))
			{
				item.pickedUp = true;
				Removed?.Invoke(item, 0);
			}
			value.Clear();
		}
		registered.Remove(mapId);
		dropLimits.Remove(mapId);
	}

	public static void ClearAll()
	{
		foreach (int item in new List<int>(byMap.Keys))
		{
			ClearMap(item);
		}
		byMap.Clear();
		registered.Clear();
		dropLimits.Clear();
	}

	public static void InvalidateMapGeometry(int mapId)
	{
		dropLimits.Remove(mapId);
	}
}
