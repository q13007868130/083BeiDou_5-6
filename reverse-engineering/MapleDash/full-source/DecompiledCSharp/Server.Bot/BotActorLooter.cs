using System;
using System.Collections.Generic;
using Server.Bot.Trade;
using Server.Field;

namespace Server.Bot;

public sealed class BotActorLooter : IMapItemLooter
{
	private readonly BotActor actor;

	private readonly BotTradeService trades;

	private readonly BotPartyService parties;

	private readonly BotLootHooks hooks;

	public Func<int, bool> OnlineLookup;

	public int CharacterId => actor.id;

	public int PartyId => parties?.For(actor.id)?.id ?? (-1);

	public event Action<BotActor> Committed;

	public BotActorLooter(BotActor actor, BotTradeService trades, BotPartyService parties, BotLootHooks hooks = null)
	{
		this.actor = actor ?? throw new ArgumentNullException("actor");
		this.trades = trades;
		this.parties = parties;
		this.hooks = hooks ?? new BotLootHooks();
		OnlineLookup = (int id) => (parties?.Get(id))?.online ?? false;
	}

	public bool IsLoggedInWorld(int characterId)
	{
		return OnlineLookup?.Invoke(characterId) ?? true;
	}

	public bool IsPartyMember(int characterId)
	{
		if (parties == null)
		{
			return false;
		}
		return parties.For(characterId)?.members.Contains(actor.id) ?? false;
	}

	public IReadOnlyList<int> PartyMembersOnSameMap()
	{
		List<int> list = new List<int>();
		if (parties == null)
		{
			return list;
		}
		foreach (BotActor item in parties.Members(actor.id))
		{
			if (item.id != actor.id && item.mapId == actor.mapId && item.online)
			{
				list.Add(item.id);
			}
		}
		return list;
	}

	public void GainMeso(int amount)
	{
		checked
		{
			if (amount != 0)
			{
				if (actor is BotCharacter botCharacter)
				{
					botCharacter.meso += amount;
				}
				else if (actor.character != null)
				{
					actor.character.record.meso = actor.character.record.meso + amount;
				}
				Commit();
			}
		}
	}

	private static List<InventoryEntry> Inventory(BotActor actor)
	{
		return (actor is BotCharacter botCharacter) ? botCharacter.inventory : (actor.character?.inventory ?? throw new InvalidOperationException("Loot actor has no loaded inventory"));
	}

	public bool HasSpace(InventoryEntry item)
	{
		if (item == null)
		{
			return false;
		}
		List<InventoryEntry> list = Inventory(actor);
		List<InventoryEntry> list2 = new List<InventoryEntry>();
		foreach (InventoryEntry item2 in list)
		{
			list2.Add(item2.Copy());
		}
		if (trades != null)
		{
			return trades.Rules.Receive(actor, list2, item);
		}
		return false;
	}

	public bool AddFromDrop(InventoryEntry item)
	{
		if (item == null)
		{
			return false;
		}
		if (trades == null)
		{
			return false;
		}
		if (!trades.Rules.Receive(actor, Inventory(actor), item))
		{
			return false;
		}
		Commit();
		return true;
	}

	public bool NeedQuestItem(int questId, int itemId)
	{
		return hooks.NeedQuestItem?.Invoke(questId, itemId) ?? true;
	}

	public bool IsConsumeOnPickup(int itemId)
	{
		return hooks.IsConsumeOnPickup?.Invoke(itemId) ?? false;
	}

	public bool ApplyConsumeOnPickup(int itemId)
	{
		return hooks.ApplyConsumeOnPickup?.Invoke(itemId) ?? false;
	}

	public bool TryRunItemScript(int itemId)
	{
		return hooks.TryRunItemScript?.Invoke(itemId) ?? false;
	}

	public void GainNxCredit(int amount)
	{
		hooks.GainNxCredit?.Invoke(amount);
	}

	public void OnAriantSpiritJewel()
	{
		hooks.OnAriantSpiritJewel?.Invoke();
	}

	public void ShowItemUnavailable()
	{
		hooks.ShowItemUnavailable?.Invoke();
	}

	public void ShowInventoryFull()
	{
		hooks.ShowInventoryFull?.Invoke();
	}

	public void EnableActions()
	{
		hooks.EnableActions?.Invoke();
	}

	public void Commit()
	{
		if (!actor.IsBot() && trades != null && actor.character != null)
		{
			actor.character.tradeRevision++;
			trades.Persist(actor);
			Committed?.Invoke(actor);
		}
	}
}
