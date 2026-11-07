using System;
using System.Collections.Generic;
using Server.Bot;
using Server.Bot.Environment;
using Server.Field;
using UnityEngine;

public sealed class BotGroundPickup
{
	public struct LootSpot
	{
		public bool found;

		public int x;

		public int y;

		public float distance;
	}

	private BotRuntime runtime;

	private readonly Dictionary<int, BotActorLooter> looters = new Dictionary<int, BotActorLooter>();

	public void Bind(BotRuntime bots)
	{
		if (runtime != bots)
		{
			runtime = bots;
			looters.Clear();
		}
	}

	private BotActorLooter Looter(BotCharacter bot)
	{
		if (bot == null || runtime == null)
		{
			return null;
		}
		if (looters.TryGetValue(bot.id, out var value) && value != null)
		{
			return value;
		}
		TrimLooters();
		value = new BotActorLooter(bot, runtime.trades, runtime.parties, new BotLootHooks
		{
			NeedQuestItem = (int _, int __) => true
		});
		looters[bot.id] = value;
		return value;
	}

	private void TrimLooters()
	{
		if (looters.Count < 64)
		{
			return;
		}
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, BotActorLooter> looter in looters)
		{
			if (BotRegistry.Get(looter.Key) == null)
			{
				list.Add(looter.Key);
			}
		}
		foreach (int item in list)
		{
			looters.Remove(item);
		}
	}

	public int PickNearby(BotCharacter bot, int itemId, float range = 90f, int max = int.MaxValue)
	{
		if (bot == null || itemId <= 0 || bot.mapId <= 0 || max <= 0)
		{
			return 0;
		}
		BotActorLooter botActorLooter = Looter(bot);
		if (botActorLooter == null)
		{
			return 0;
		}
		int mapId = NativeEventScripts.ResolveMap(bot.mapId)?.Scope ?? bot.mapId;
		List<MapItem> list;
		try
		{
			list = MapItemService.ItemsInRange(mapId, bot.x, bot.y, (double)range * (double)range);
		}
		catch (Exception)
		{
			return 0;
		}
		int num = 0;
		long nowMs = (long)(Time.realtimeSinceStartup * 1000f);
		foreach (MapItem item in list)
		{
			if (num >= max)
			{
				break;
			}
			if (item != null && !item.pickedUp && !item.IsMeso && item.ItemId == itemId)
			{
				bool flag;
				try
				{
					flag = MapItemPickup.PickupItem(item, botActorLooter, nowMs);
				}
				catch (Exception)
				{
					flag = false;
				}
				if (flag)
				{
					num++;
				}
			}
		}
		if (num < max)
		{
			num += PickLegacy(bot, botActorLooter, itemId, range, max - num);
		}
		return num;
	}

	private int PickLegacy(BotCharacter bot, BotActorLooter looter, int itemId, float range, int max)
	{
		if (max <= 0)
		{
			return 0;
		}
		int mapX = (int)bot.x;
		int mapY = (int)bot.y;
		long rangeSq = (long)(range * range);
		int num = 0;
		GroundItem[] array = GroundItem.All.ToArray();
		foreach (GroundItem groundItem in array)
		{
			if (num >= max)
			{
				break;
			}
			if ((Object)(object)groundItem == (Object)null || groundItem.bound != null || groundItem.itemId != itemId || groundItem.amount <= 0)
			{
				continue;
			}
			int num2 = Math.Min(groundItem.amount, max - num);
			InventoryEntry inventoryEntry = new InventoryEntry
			{
				itemId = itemId,
				inventoryType = Math.Max(1, itemId / 1000000),
				quantity = num2
			};
			if (!looter.HasSpace(inventoryEntry))
			{
				continue;
			}
			int num3;
			try
			{
				num3 = groundItem.TakeForBot(mapX, mapY, rangeSq, num2, () => PickupTarget(bot));
			}
			catch (Exception)
			{
				num3 = 0;
			}
			if (num3 > 0)
			{
				inventoryEntry.quantity = num3;
				looter.AddFromDrop(inventoryEntry);
				num += num3;
			}
		}
		return num;
	}

	public LootSpot Nearest(BotCharacter bot, float searchRange = 340f, int floorTolerance = 140)
	{
		LootSpot spot = new LootSpot
		{
			distance = float.MaxValue
		};
		if (bot == null || bot.mapId <= 0 || searchRange <= 0f)
		{
			return spot;
		}
		int mx = (int)bot.x;
		int my = (int)bot.y;
		float best = searchRange;
		int mapId = NativeEventScripts.ResolveMap(bot.mapId)?.Scope ?? bot.mapId;
		List<MapItem> list;
		try
		{
			list = MapItemService.DroppedItems(mapId);
		}
		catch (Exception)
		{
			list = null;
		}
		if (list != null)
		{
			foreach (MapItem item in list)
			{
				if (item != null && !item.pickedUp && !(item.IsMeso ? (item.meso <= 0) : (item.item == null)))
				{
					Consider((int)item.x, (int)item.y);
				}
			}
		}
		foreach (GroundItem item2 in GroundItem.All)
		{
			if (!((Object)(object)item2 == (Object)null) && item2.Ready)
			{
				Consider(item2.MapX, item2.MapY);
			}
		}
		spot.distance = best;
		return spot;
		void Consider(int x, int y)
		{
			int num = x - mx;
			int num2 = y - my;
			if (Math.Abs(num2) <= floorTolerance)
			{
				float num3 = (float)Math.Sqrt((double)num * (double)num + (double)num2 * (double)num2);
				if (!(num3 > best))
				{
					best = num3;
					spot.found = true;
					spot.x = x;
					spot.y = y;
				}
			}
		}
	}

	public int PickAny(BotCharacter bot, float range = 60f, int max = 2, bool meso = true)
	{
		if (bot == null || bot.mapId <= 0 || max <= 0 || range <= 0f)
		{
			return 0;
		}
		BotActor botActor = runtime?.partyGrind.Leader(bot.id);
		if (botActor == null || botActor.id != GameSession.CharacterId || botActor.mapId != bot.mapId || !runtime.partyGrind.SameParty(bot, botActor))
		{
			return 0;
		}
		int num = 0;
		long rangeSq = (long)(range * range);
		GroundItem[] array = GroundItem.All.ToArray();
		foreach (GroundItem groundItem in array)
		{
			if (num >= max)
			{
				break;
			}
			if (!((Object)(object)groundItem == (Object)null) && (groundItem.itemId != 0 || meso) && groundItem.PickupForCompanion(bot, rangeSq))
			{
				num++;
			}
		}
		return num;
	}

	internal static Nullable PickupTarget(BotCharacter bot)
	{
		if (bot == null)
		{
			return null;
		}
		GameLoop i = GameLoop.I;
		if (i == null)
		{
			return null;
		}
		FieldDirector field = i.field;
		if (field == null)
		{
			return null;
		}
		return ((Component)field).GetComponent<BotField>()?.PickupTarget(bot.id);
	}

	public bool WalkTo(BotCharacter bot, int itemId, float stopRange = 60f, float searchRange = float.MaxValue)
	{
		if (bot == null || itemId <= 0 || bot.mapId <= 0)
		{
			return false;
		}
		int num = (int)bot.x;
		int num2 = (int)bot.y;
		int num3 = 0;
		int num4 = 0;
		float num5 = searchRange;
		bool flag = false;
		int mapId = NativeEventScripts.ResolveMap(bot.mapId)?.Scope ?? bot.mapId;
		List<MapItem> list;
		try
		{
			list = MapItemService.DroppedItems(mapId);
		}
		catch (Exception)
		{
			list = null;
		}
		if (list != null)
		{
			foreach (MapItem item in list)
			{
				if (item != null && !item.pickedUp && !item.IsMeso && item.ItemId == itemId)
				{
					float num6 = Math.Abs(item.x - (float)num) + Math.Abs(item.y - (float)num2) * 0.6f;
					if (num6 < num5)
					{
						flag = true;
						num5 = num6;
						num3 = (int)item.x;
						num4 = (int)item.y;
					}
				}
			}
		}
		foreach (GroundItem item2 in GroundItem.All)
		{
			if (!((Object)(object)item2 == (Object)null) && item2.Ready && item2.itemId == itemId)
			{
				int mapX = item2.MapX;
				int mapY = item2.MapY;
				float num7 = (float)Math.Abs(mapX - num) + (float)Math.Abs(mapY - num2) * 0.6f;
				if (num7 < num5)
				{
					flag = true;
					num5 = num7;
					num3 = mapX;
					num4 = mapY;
				}
			}
		}
		if (!flag || num5 <= stopRange)
		{
			return false;
		}
		try
		{
			if (!BotMovementService.PrepareNavigation(bot))
			{
				return false;
			}
			int y = (int)MapService.SnapToGround(bot.mapId, num3, num4);
			if (!BotMovementService.IsMovingToward(bot, num3, y))
			{
				BotMovementService.Move(bot, num3, y);
			}
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}
}
