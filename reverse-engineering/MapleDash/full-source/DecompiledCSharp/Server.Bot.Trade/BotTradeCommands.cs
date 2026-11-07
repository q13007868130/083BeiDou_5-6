using System;
using System.Collections.Generic;

namespace Server.Bot.Trade;

public static class BotTradeCommands
{
	public static void StartRequest(BotActor actor, BotTradeService service)
	{
		service.Start(actor);
	}

	public static bool SendRequest(BotActor actor, BotActor invitee, BotTradeService service)
	{
		return service.Invite(actor, invitee);
	}

	public static void WriteChat(BotActor actor, string text)
	{
		actor.trade?.Chat(text);
	}

	public static void SetMeso(BotActor actor, long amount)
	{
		if (amount < 0)
		{
			throw new ArgumentOutOfRangeException("amount");
		}
		actor.trade?.SetMesoBot(amount);
	}

	public static long ReadMeso(BotActor actor)
	{
		return actor.trade?.meso ?? 0;
	}

	public static long ReadPartnerMeso(BotActor actor)
	{
		if (actor.trade != null)
		{
			return actor.trade.partner.meso;
		}
		return 0L;
	}

	public static IList<InventoryEntry> LocalItems(BotActor actor)
	{
		return actor.trade?.Items;
	}

	public static IList<InventoryEntry> PartnerItems(BotActor actor)
	{
		return actor.trade?.partner.Items;
	}

	public static bool IsPartnerLocked(BotActor actor)
	{
		if (actor.trade != null)
		{
			return actor.trade.partner.locked;
		}
		return false;
	}

	public static bool IsConfirmed(BotActor actor)
	{
		return actor.trade?.locked ?? false;
	}

	public static void Confirm(BotActor actor)
	{
		actor.trade?.Confirm();
	}

	public static void Accept(BotActor actor)
	{
		actor.trade?.Accept();
	}

	public static void Decline(BotActor actor)
	{
		actor.trade?.Decline();
	}

	public static List<int> OccupiedSlots(BotActor actor)
	{
		List<int> list = new List<int>();
		if (actor.trade != null)
		{
			foreach (InventoryEntry item in actor.trade.Items)
			{
				list.Add(item.position);
			}
		}
		return list;
	}

	public static List<int> EmptySlots(BotActor actor)
	{
		List<int> list = new List<int>();
		if (actor.trade == null)
		{
			return list;
		}
		List<int> list2 = OccupiedSlots(actor);
		for (int i = 1; i <= 9; i++)
		{
			if (!list2.Contains(i))
			{
				list.Add(i);
			}
		}
		return list;
	}

	public static int? RandomEmptySlot(BotActor actor, Random random)
	{
		List<int> list = EmptySlots(actor);
		if (list.Count != 0)
		{
			return list[random.Next(list.Count)];
		}
		return null;
	}

	private static bool Validate(BotActor actor, int slot)
	{
		if (actor.trade == null)
		{
			return false;
		}
		if (slot < 1 || slot > 9)
		{
			throw new ArgumentOutOfRangeException("slot", "slot must be 1-9");
		}
		return true;
	}

	public static bool AddEquip(BotActor actor, InventoryEntry equip, int slot)
	{
		if (Validate(actor, slot) && equip != null)
		{
			return actor.trade.AddItem(equip, slot);
		}
		return false;
	}

	public static bool AddCleanEquip(BotActor actor, int itemId, int slot)
	{
		if (Validate(actor, slot))
		{
			return AddEquip(actor, BotLogic.GenerateCleanItemEquip(itemId), slot);
		}
		return false;
	}

	public static bool AddItem(BotActor actor, int itemId, int quantity, int slot)
	{
		if (Validate(actor, slot))
		{
			return actor.trade.AddItem(BotLogic.GenerateCleanItemWithQty(itemId, quantity), slot);
		}
		return false;
	}

	public static bool SwapItem(BotActor actor, InventoryEntry item, int slot)
	{
		if (!IsConfirmed(actor) && Validate(actor, slot))
		{
			return actor.trade.SwapItem(item, slot);
		}
		return false;
	}

	public static bool SwapCleanEquip(BotActor actor, int itemId, int slot)
	{
		if (!IsConfirmed(actor) && Validate(actor, slot))
		{
			return SwapItem(actor, BotLogic.GenerateCleanItemEquip(itemId), slot);
		}
		return false;
	}

	public static bool SwapItem(BotActor actor, int itemId, int quantity, int slot)
	{
		if (!IsConfirmed(actor) && Validate(actor, slot))
		{
			return SwapItem(actor, BotLogic.GenerateCleanItemWithQty(itemId, quantity), slot);
		}
		return false;
	}

	public static int PartnerFame(BotActor actor)
	{
		return actor.trade?.partner.actor.fame ?? 0;
	}

	public static int PartnerLevel(BotActor actor)
	{
		return actor.trade?.partner.actor.level ?? 0;
	}

	public static InventoryEntry InSlot(IList<InventoryEntry> items, int slot)
	{
		if (items == null)
		{
			throw new InvalidOperationException("Trade items list is null");
		}
		foreach (InventoryEntry item in items)
		{
			if (item.position == slot)
			{
				return item;
			}
		}
		return null;
	}
}
