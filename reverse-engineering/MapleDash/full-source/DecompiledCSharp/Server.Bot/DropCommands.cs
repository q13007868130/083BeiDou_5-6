using System.Collections.Generic;
using Server.Field;

namespace Server.Bot;

public static class DropCommands
{
	public struct DropOrigin
	{
		public int mapId;

		public int x;

		public int y;

		public int characterId;

		public int partyId;

		public static DropOrigin From(BotActor actor)
		{
			return new DropOrigin
			{
				mapId = actor.mapId,
				x = (int)actor.x,
				y = (int)actor.y,
				characterId = actor.id,
				partyId = ((MapItemService.PartyOf != null) ? MapItemService.PartyOf(actor.id) : (-1))
			};
		}
	}

	public const long PickupGracePeriodMs = 3000L;

	private static void GetPos(BotActor actor, out int mapId, out float x, out float y, out int partyId)
	{
		mapId = actor.mapId;
		x = actor.x;
		y = actor.y;
		partyId = ((MapItemService.PartyOf != null) ? MapItemService.PartyOf(actor.id) : (-1));
	}

	public static MapItem BotDropMeso(BotActor fakechar, int meso)
	{
		GetPos(fakechar, out var mapId, out var x, out var y, out var partyId);
		return MapItemService.SpawnMesoDrop(mapId, meso, x, y, fakechar.id, fakechar.id, partyId, playerDrop: true, 2);
	}

	public static MapItem BotDropEquip(BotActor fakechar, int itemId)
	{
		GetPos(fakechar, out var mapId, out var x, out var y, out var partyId);
		return MapItemService.SpawnItemDrop(mapId, fakechar.id, fakechar.id, partyId, BotLogic.GenerateCleanItemEquip(itemId), x, y, ffaDrop: true, playerDrop: true);
	}

	public static MapItem BotDropItem(BotActor fakechar, int itemId)
	{
		GetPos(fakechar, out var mapId, out var x, out var y, out var partyId);
		return MapItemService.SpawnItemDrop(mapId, fakechar.id, fakechar.id, partyId, BotLogic.GenerateCleanItem(itemId), x, y, ffaDrop: true, playerDrop: true);
	}

	public static MapItem BotDropItemQty(BotActor fakechar, int itemId, int qty)
	{
		GetPos(fakechar, out var mapId, out var x, out var y, out var partyId);
		return MapItemService.SpawnItemDrop(mapId, fakechar.id, fakechar.id, partyId, BotLogic.GenerateCleanItemWithQty(itemId, qty), x, y, ffaDrop: true, playerDrop: true);
	}

	public static MapItem BotDropItemWithExpiry(BotActor fakechar, int itemId, bool isEquip, long expiryMs)
	{
		GetPos(fakechar, out var mapId, out var x, out var y, out var partyId);
		InventoryEntry item = (isEquip ? BotLogic.GenerateCleanItemEquip(itemId) : BotLogic.GenerateCleanItem(itemId));
		MapItem mapItem = MapItemService.SpawnItemDropNoExpire(mapId, fakechar.id, fakechar.id, partyId, item, x, y, ffaDrop: true, playerDrop: true);
		if (mapItem != null && expiryMs > 0)
		{
			ScheduleExpiry(mapId, mapItem, expiryMs);
		}
		return mapItem;
	}

	private static void ScheduleExpiry(int mapId, MapItem drop, long expiryMs)
	{
		BotTasks.Schedule(() =>
		{
			if (!drop.pickedUp)
			{
				RemovedVisual(drop);
			}
		}, expiryMs, "ScheduleExpiry", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/DropCommands.cs");
		BotTasks.Schedule(() =>
		{
			if (!drop.pickedUp)
			{
				MapItemService.MakeDisappear(mapId, drop);
			}
		}, expiryMs + 3000, "ScheduleExpiry", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/DropCommands.cs");
	}

	private static void RemovedVisual(MapItem drop)
	{
		MapItemService.NotifyRemoved(drop, 0);
	}

	public static MapItem BotThrowMeso(BotActor fakechar, int meso, int throwX, int throwY)
	{
		GetPos(fakechar, out var mapId, out var _, out var _, out var partyId);
		return MapItemService.SpawnMesoDrop(mapId, meso, throwX, throwY, fakechar.id, fakechar.id, partyId, playerDrop: false, 2);
	}

	public static MapItem BotThrowEquip(BotActor fakechar, int itemId, int throwX, int throwY)
	{
		GetPos(fakechar, out var mapId, out var _, out var _, out var partyId);
		return MapItemService.SpawnItemDrop(mapId, fakechar.id, fakechar.id, partyId, BotLogic.GenerateCleanItemEquip(itemId), throwX, throwY, ffaDrop: true, playerDrop: false);
	}

	public static MapItem BotThrowEquip(BotActor fakechar, InventoryEntry eq, int throwX, int throwY)
	{
		if (eq == null)
		{
			return null;
		}
		GetPos(fakechar, out var mapId, out var _, out var _, out var partyId);
		return MapItemService.SpawnItemDrop(mapId, fakechar.id, fakechar.id, partyId, eq, throwX, throwY, ffaDrop: true, playerDrop: false);
	}

	public static InventoryEntry BotThrowItem(BotActor fakechar, int itemId, int throwX, int throwY, out MapItem drop)
	{
		InventoryEntry inventoryEntry = BotLogic.GenerateCleanItem(itemId);
		GetPos(fakechar, out var mapId, out var _, out var _, out var partyId);
		drop = MapItemService.SpawnItemDrop(mapId, fakechar.id, fakechar.id, partyId, inventoryEntry, throwX, throwY, ffaDrop: true, playerDrop: false);
		return inventoryEntry;
	}

	public static MapItem BotThrowItemNoExpire(BotActor fakechar, int itemId, int throwX, int throwY)
	{
		GetPos(fakechar, out var mapId, out var _, out var _, out var partyId);
		return MapItemService.SpawnItemDropNoExpire(mapId, fakechar.id, fakechar.id, partyId, BotLogic.GenerateCleanItem(itemId), throwX, throwY, ffaDrop: true, playerDrop: false);
	}

	public static MapItem BotThrowItemNoExpireOwnerOnly(BotActor fakechar, int itemId, int throwX, int throwY)
	{
		GetPos(fakechar, out var mapId, out var _, out var _, out var partyId);
		MapItem mapItem = MapItemService.SpawnItemDropNoExpire(mapId, fakechar.id, fakechar.id, partyId, BotLogic.GenerateCleanItem(itemId), throwX, throwY, ffaDrop: false, playerDrop: false);
		if (mapItem != null)
		{
			mapItem.permanentOwner = true;
		}
		return mapItem;
	}

	public static MapItem BotDropItemQtyOwnerOnly(BotActor fakechar, int itemId, int qty)
	{
		GetPos(fakechar, out var mapId, out var x, out var y, out var partyId);
		MapItem mapItem = MapItemService.SpawnItemDropNoExpire(mapId, fakechar.id, fakechar.id, partyId, BotLogic.GenerateCleanItemWithQty(itemId, qty), x, y, ffaDrop: false, playerDrop: false);
		if (mapItem != null)
		{
			mapItem.permanentOwner = true;
		}
		return mapItem;
	}

	public static MapItem BotThrowItemQty(BotActor fakechar, int itemId, int qty, int throwX, int throwY)
	{
		GetPos(fakechar, out var mapId, out var _, out var _, out var partyId);
		return MapItemService.SpawnItemDrop(mapId, fakechar.id, fakechar.id, partyId, BotLogic.GenerateCleanItemWithQty(itemId, qty), throwX, throwY, ffaDrop: true, playerDrop: false);
	}

	public static MapItem BotThrowItemToOwner(BotActor dropper, int itemId, int throwX, int throwY, BotActor owner)
	{
		if (owner == null)
		{
			return null;
		}
		GetPos(dropper, out var mapId, out var _, out var _, out var _);
		int partyId2 = ((MapItemService.PartyOf != null) ? MapItemService.PartyOf(owner.id) : (-1));
		MapItem mapItem = MapItemService.SpawnItemDropNoExpire(mapId, dropper.id, owner.id, partyId2, BotLogic.GenerateCleanItem(itemId), throwX, throwY, ffaDrop: false, playerDrop: false);
		if (mapItem != null)
		{
			mapItem.permanentOwner = true;
		}
		return mapItem;
	}

	public static MapItem BotThrowToOwnerMeso(BotActor fakechar, int meso, BotActor owner)
	{
		if (owner == null)
		{
			return null;
		}
		GetPos(fakechar, out var mapId, out var _, out var _, out var _);
		int partyId2 = ((MapItemService.PartyOf != null) ? MapItemService.PartyOf(owner.id) : (-1));
		return MapItemService.SpawnMesoDrop(mapId, meso, owner.x, owner.y, fakechar.id, owner.id, partyId2, playerDrop: false, 0);
	}

	public static MapItem BotThrowToOwnerEquip(BotActor fakechar, int itemId, BotActor owner)
	{
		if (owner == null)
		{
			return null;
		}
		GetPos(fakechar, out var mapId, out var _, out var _, out var _);
		int partyId2 = ((MapItemService.PartyOf != null) ? MapItemService.PartyOf(owner.id) : (-1));
		return MapItemService.SpawnItemDrop(mapId, fakechar.id, owner.id, partyId2, BotLogic.GenerateCleanItemEquip(itemId), owner.x, owner.y, ffaDrop: false, playerDrop: false);
	}

	public static MapItem BotThrowToOwnerItem(BotActor fakechar, int itemId, BotActor owner)
	{
		if (owner == null)
		{
			return null;
		}
		GetPos(fakechar, out var mapId, out var _, out var _, out var _);
		int partyId2 = ((MapItemService.PartyOf != null) ? MapItemService.PartyOf(owner.id) : (-1));
		return MapItemService.SpawnItemDrop(mapId, fakechar.id, owner.id, partyId2, BotLogic.GenerateCleanItem(itemId), owner.x, owner.y, ffaDrop: false, playerDrop: false);
	}

	public static MapItem BotThrowToOwnerItemQty(BotActor fakechar, int itemId, int qty, BotActor owner)
	{
		if (owner == null)
		{
			return null;
		}
		GetPos(fakechar, out var mapId, out var _, out var _, out var _);
		int partyId2 = ((MapItemService.PartyOf != null) ? MapItemService.PartyOf(owner.id) : (-1));
		return MapItemService.SpawnItemDrop(mapId, fakechar.id, owner.id, partyId2, BotLogic.GenerateCleanItemWithQty(itemId, qty), owner.x, owner.y, ffaDrop: false, playerDrop: false);
	}

	public static void BotThrowEquipsInARow(BotActor fakechar, int[] items, int centerX, int centerY, int spacing)
	{
		if (items != null && items.Length != 0)
		{
			int num = items.Length;
			int num2 = centerX - num / 2 * spacing;
			for (int i = 0; i < num; i++)
			{
				BotThrowEquip(fakechar, items[i], num2 + i * spacing, centerY);
			}
		}
	}

	public static void BotLoot(BotActor fakechar, double range)
	{
		GetPos(fakechar, out var _, out var x, out var y, out var _);
		BotLoot(fakechar, (int)x, (int)y, range);
	}

	public static void BotLoot(BotActor fakechar, int itemX, int itemY, double range)
	{
		foreach (MapItem item in MapItemService.ItemsInRange(fakechar.mapId, itemX, itemY, range))
		{
			Pick(fakechar, item);
		}
	}

	public static void BotLootLocation(BotActor fakechar, int itemX, int itemY)
	{
		BotLoot(fakechar, itemX, itemY, 1000.0);
	}

	public static void BotLootOwnerItems(BotActor fakechar, int itemX, int itemY, double range)
	{
		BotLootTargetCharactersItems(fakechar, fakechar, itemX, itemY, range);
	}

	public static void BotLootTargetCharactersItems(BotActor fakechar, BotActor targetCharacter, int itemX, int itemY, double range)
	{
		foreach (MapItem item in MapItemService.ItemsInRange(fakechar.mapId, itemX, itemY, range))
		{
			if (item.ownerCharacterId == targetCharacter.id)
			{
				Pick(fakechar, item);
			}
		}
	}

	private static void Pick(BotActor fakechar, MapItem mapItem)
	{
		if (mapItem != null && !mapItem.pickedUp)
		{
			MapItemService.PickItemDrop(fakechar.mapId, mapItem, 2, fakechar.id);
		}
	}

	public static BotTasks.Handle LootItemListOnFloor(BotActor fakechar, List<MapItem> items)
	{
		if (fakechar == null || items == null || items.Count == 0)
		{
			return null;
		}
		int mapId = fakechar.mapId;
		Queue<MapItem> queue = new Queue<MapItem>(items);
		BotTasks.Handle chain = null;
		Step();
		return chain;
		void Step()
		{
			if (queue.Count != 0)
			{
				MapItem mapItem = queue.Dequeue();
				if (mapItem != null && !mapItem.pickedUp)
				{
					MapItemService.PickItemDrop(mapId, mapItem, 2, fakechar.id);
				}
				if (queue.Count > 0)
				{
					chain = BotTasks.Schedule(Step, 100L, "LootItemListOnFloor", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/DropCommands.cs");
				}
			}
		}
	}

	public static void BotLootSelectedItems(BotActor chr, int[] itemsToLoot)
	{
		List<MapItem> list = BotLogic.CheckForItemsOnFloor(chr.mapId, (int)chr.x, (int)chr.y, 10000.0, itemsToLoot);
		if (list.Count > 0)
		{
			LootItemListOnFloor(chr, list);
		}
	}
}
