using System.Collections.Generic;

namespace Server.Field;

public static class MapItemPickup
{
	public const long PickupDelayMs = 400L;

	public const int NxCard100 = 4031865;

	public const int NxCard250 = 4031866;

	public const int ArpqSpiritJewel = 4001157;

	public static bool IsNxCard(int itemId)
	{
		if (itemId != 4031865)
		{
			return itemId == 4031866;
		}
		return true;
	}

	public static bool IsMonsterCard(int itemId)
	{
		return itemId / 10000 == 238;
	}

	public static bool IsSelfLootableOnly(int mapId)
	{
		if (mapId < 209000001 || mapId > 209000015)
		{
			if (mapId >= 990000500)
			{
				return mapId <= 990000502;
			}
			return false;
		}
		return true;
	}

	public static bool NeedQuestItemUnrestricted(int questId)
	{
		if (questId != 0)
		{
			return questId == -1;
		}
		return true;
	}

	public static bool PickupItem(MapItem mapitem, IMapItemLooter looter, long nowMs, int petIndex = -1, bool sharePartyMeso = true)
	{
		if (mapitem == null || looter == null)
		{
			return false;
		}
		if (mapitem.AgeMs(nowMs) < 400)
		{
			looter.EnableActions();
			return false;
		}
		if (!mapitem.CanBePickedBy(nowMs, looter.CharacterId, looter.PartyId, looter.IsPartyMember))
		{
			looter.EnableActions();
			return false;
		}
		List<int> list = new List<int>();
		if (sharePartyMeso && mapitem.meso > 0 && !mapitem.pickedUp)
		{
			list.AddRange(looter.PartyMembersOnSameMap());
		}
		if (mapitem.pickedUp)
		{
			looter.ShowItemUnavailable();
			looter.EnableActions();
			return false;
		}
		int reason = ((petIndex > -1) ? 5 : 2);
		InventoryEntry item = mapitem.item;
		bool flag = true;
		if (IsNxCard(mapitem.ItemId) || mapitem.meso > 0 || (looter.IsConsumeOnPickup(mapitem.ItemId) && !IsMonsterCard(mapitem.ItemId)) || (flag = looter.HasSpace(item)))
		{
			int mapId = mapitem.mapId;
			if (IsSelfLootableOnly(mapId))
			{
				if (!mapitem.playerDrop || mapitem.dropperId == looter.CharacterId)
				{
					if (mapitem.meso > 0)
					{
						GrantMeso(mapitem, looter, list);
						MapItemService.PickItemDrop(mapId, mapitem, reason, looter.CharacterId, petIndex);
					}
					else if (IsNxCard(mapitem.ItemId))
					{
						GrantNx(mapitem, looter);
						MapItemService.PickItemDrop(mapId, mapitem, reason, looter.CharacterId, petIndex);
					}
					else
					{
						if (!looter.AddFromDrop(item))
						{
							looter.EnableActions();
							return false;
						}
						MapItemService.PickItemDrop(mapId, mapitem, reason, looter.CharacterId, petIndex);
					}
					looter.EnableActions();
					return mapitem.pickedUp;
				}
				looter.ShowItemUnavailable();
				looter.EnableActions();
				return false;
			}
			if (!NeedQuestItemUnrestricted(mapitem.questId) && !looter.NeedQuestItem(mapitem.questId, mapitem.ItemId))
			{
				looter.ShowItemUnavailable();
				looter.EnableActions();
				return false;
			}
			if (mapitem.meso > 0)
			{
				GrantMeso(mapitem, looter, list);
			}
			else if (item != null && item.itemId / 10000 == 243)
			{
				if (!looter.TryRunItemScript(item.itemId) && !looter.AddFromDrop(item))
				{
					looter.EnableActions();
					return false;
				}
			}
			else if (IsNxCard(mapitem.ItemId))
			{
				GrantNx(mapitem, looter);
			}
			else if (!looter.ApplyConsumeOnPickup(mapitem.ItemId))
			{
				if (!looter.AddFromDrop(item))
				{
					looter.EnableActions();
					return false;
				}
				if (item != null && item.itemId == 4001157)
				{
					looter.OnAriantSpiritJewel();
				}
			}
			MapItemService.PickItemDrop(mapId, mapitem, reason, looter.CharacterId, petIndex);
		}
		else if (!flag)
		{
			looter.ShowInventoryFull();
		}
		looter.EnableActions();
		return mapitem.pickedUp;
	}

	private static void GrantMeso(MapItem mapitem, IMapItemLooter looter, List<int> mpcs)
	{
		if (mpcs.Count > 0)
		{
			int amount = mapitem.meso / mpcs.Count;
			{
				foreach (int mpc in mpcs)
				{
					if (looter.IsLoggedInWorld(mpc))
					{
						looter.GainMeso(amount);
					}
				}
				return;
			}
		}
		looter.GainMeso(mapitem.meso);
	}

	private static void GrantNx(MapItem mapitem, IMapItemLooter looter)
	{
		int amount = ((mapitem.ItemId == 4031865) ? 100 : 250) * (mapitem.item?.quantity ?? 1);
		looter.GainNxCredit(amount);
	}
}
