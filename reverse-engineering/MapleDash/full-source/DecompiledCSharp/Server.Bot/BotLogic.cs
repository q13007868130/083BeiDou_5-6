using System;
using System.Collections.Generic;
using System.Text;
using Server.Field;

namespace Server.Bot;

public static class BotLogic
{
	private const int FM_ENTRANCE = 910000000;

	private const int FM_ROOM_1 = 910000001;

	private const int FM_ROOM_22 = 910000022;

	internal static Func<IEnumerable<BotActor>> OnlinePlayers;

	private static readonly int[] BetStamps = new int[8] { 4002000, 4002001, 4002002, 4002003, 4031558, 4031559, 4031560, 4031561 };

	public static bool IsCharNear(int x1, int y1, int x2, int y2)
	{
		return IsPointNear(x1, y1, x2, y2, 25.0);
	}

	public static bool IsPointNear(int x1, int y1, int x2, int y2, double maxDist)
	{
		return BotHelpers.Distance(x1, y1, x2, y2) <= maxDist;
	}

	public static bool IsPointNearSameY(int x1, int y1, int x2, int y2, double maxX, int yThresh)
	{
		if (Math.Abs(y1 - y2) > yThresh)
		{
			return false;
		}
		return (double)Math.Abs(x1 - x2) <= maxX;
	}

	public static int GetFurthestPointFromOrigin(int bx, int by, List<(int x, int y)> pts)
	{
		if (pts == null || pts.Count == 0)
		{
			return -1;
		}
		int result = -1;
		double num = -1.0;
		for (int i = 0; i < pts.Count; i++)
		{
			int num2 = pts[i].x - bx;
			int num3 = pts[i].y - by;
			double num4 = num2 * num2 + num3 * num3;
			if (num4 > num)
			{
				num = num4;
				result = i;
			}
		}
		return result;
	}

	public static bool IsWithinPriceRange(double current, double market, double tol = 0.8)
	{
		if (tol < 0.0 || tol > 1.0)
		{
			throw new ArgumentException("tolerance must be 0..1");
		}
		double num = market * (1.0 - tol);
		double num2 = market * (1.0 + tol);
		if (current >= num)
		{
			return current <= num2;
		}
		return false;
	}

	public static bool IsInsideFMRooms(int map)
	{
		if (map >= 910000001)
		{
			return map <= 910000022;
		}
		return false;
	}

	public static bool IsAtFMEntrance(int map)
	{
		return map == 910000000;
	}

	public static bool IsInsideFM(int map)
	{
		if (!IsInsideFMRooms(map))
		{
			return IsAtFMEntrance(map);
		}
		return true;
	}

	private static List<BotActor> PlayersInRange(BotCharacter fake, int w, int h, bool realOnly)
	{
		List<BotActor> found = new List<BotActor>();
		if (fake == null || w <= 0 || h <= 0)
		{
			return found;
		}
		int left = (int)fake.x - w / 2;
		int top = (int)fake.y - h / 2 + (int)((double)h * 0.2) - h / 2;
		if (OnlinePlayers != null)
		{
			foreach (BotActor item in OnlinePlayers())
			{
				Add(item);
			}
		}
		if (!realOnly)
		{
			foreach (BotCharacter item2 in BotRegistry.OnMap(fake.mapId))
			{
				Add(item2);
			}
		}
		found.Sort((BotActor a, BotActor b) => a.mapEntryOrder.CompareTo(b.mapEntryOrder));
		return found;
		void Add(BotActor actor)
		{
			if (actor.id != fake.id && actor.online && actor.mapId == fake.mapId && actor.world == fake.world && actor.channel == fake.channel && (int)actor.x >= left && (int)actor.x < left + w && (int)actor.y >= top && (int)actor.y < top + h)
			{
				found.Add(actor);
			}
		}
	}

	public static BotActor WaitForPlayerInRange(BotCharacter fake, int w, int h)
	{
		List<BotActor> list = PlayersInRange(fake, w, h, realOnly: false);
		if (list.Count != 0)
		{
			return list[0];
		}
		return null;
	}

	public static List<BotActor> GetRealPlayersInRange(BotCharacter fake, int w, int h)
	{
		return PlayersInRange(fake, w, h, realOnly: true);
	}

	public static bool IsNpcPresent(int mapId, int npcId)
	{
		return MapService.Get(mapId)?.npcs.Exists((MapNpcPosition npc) => npc.id == npcId) ?? false;
	}

	public static InventoryEntry GenerateCleanItemEquip(int id)
	{
		return BotItemCatalog.Shared.CleanItem(id);
	}

	public static InventoryEntry GenerateCleanEquip(int itemId)
	{
		return BotItemCatalog.Shared.CleanEquip(itemId);
	}

	public static InventoryEntry GenerateCleanItem(int itemId)
	{
		return GenerateCleanItemWithQty(itemId, 1);
	}

	public static InventoryEntry GenerateCleanItemWithQty(int id, int quantity)
	{
		return new InventoryEntry
		{
			itemId = id,
			inventoryType = id / 1000000,
			quantity = (short)quantity
		};
	}

	public static List<MapItem> CheckForItemsOnFloor(int mapId, int x, int y, int[] itemIds)
	{
		return CheckForItemsOnFloor(mapId, x, y, 5000.0, itemIds);
	}

	public static List<MapItem> CheckForItemsOnFloor(int mapId, int x, int y, double rangeSq, int[] itemIds)
	{
		return FilterDroppedItems(ReadItemOnFloor(mapId, x, y, rangeSq), itemIds);
	}

	public static List<MapItem> CheckForItemOnFloor(int mapId, int x, int y, int itemId)
	{
		return CheckForItemsOnFloor(mapId, x, y, new int[1] { itemId });
	}

	public static List<MapItem> ReadItemOnFloor(int mapId, int x, int y, double rangeSq)
	{
		return MapItemService.ItemsInRange(mapId, x, y, rangeSq);
	}

	public static List<MapItem> FilterDroppedItems(List<MapItem> items, int[] filterList)
	{
		if (items == null)
		{
			return new List<MapItem>();
		}
		HashSet<int> keep = new HashSet<int>(filterList ?? new int[0]);
		items.RemoveAll((MapItem m) => !keep.Contains(m.ItemId));
		return items;
	}

	public static List<MapItem> FilterPlayersDroppedItems(List<MapItem> items, BotActor player)
	{
		if (items == null)
		{
			return new List<MapItem>();
		}
		items.RemoveAll((MapItem m) => m.ownerCharacterId != player.id);
		return items;
	}

	public static bool CheckIfItemsOnFloorStill(List<MapItem> itemsToCheck, List<MapItem> itemsAtPlayer)
	{
		return BotHelpers.CheckSecondListInsideFirstList(itemsAtPlayer, itemsToCheck);
	}

	public static StringBuilder CleanUpBetsString(List<MapItem> items, StringBuilder betString)
	{
		foreach (MapItem item in items)
		{
			string value = BotHelpers.ConvertItemIdToName(item.ItemId);
			betString.Append(item.Quantity).Append("x ").Append(value)
				.Append(", ");
		}
		return new StringBuilder(betString.ToString().Replace("'s Stamp", "").Replace(" Stamp", "")
			.Replace(" the Really Old", "")
			.Replace(" Pierce", ""));
	}

	public static string AnnounceBetString(BotActor better, List<MapItem> items)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(better.name).Append("'s Bet: ");
		return CleanUpBetsString(items, stringBuilder).ToString();
	}

	public static List<MapItem> ReadPlayersBetsStamps(BotActor player, double rangeSq)
	{
		return ReadPlayersBetsStamps(player, (int)player.x, (int)player.y, rangeSq);
	}

	public static List<MapItem> ReadPlayersBetsStamps(BotActor player, int x, int y, double rangeSq)
	{
		return FilterDroppedItems(FilterPlayersDroppedItems(ReadItemOnFloor(player.mapId, x, y, rangeSq), player), BetStamps);
	}
}
