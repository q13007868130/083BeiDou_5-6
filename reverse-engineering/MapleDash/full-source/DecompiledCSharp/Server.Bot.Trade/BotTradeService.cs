using System;
using System.Collections.Generic;

namespace Server.Bot.Trade;

public sealed class BotTradeService
{
	public sealed class Session : IBotTradeSession
	{
		private readonly BotTradeService service;

		private readonly List<InventoryEntry> items = new List<InventoryEntry>();

		internal Session other;

		private bool ended;

		internal int invitationTicks;

		public BotActor actor { get; }

		public IBotTradeSession partner => other;

		public long meso { get; private set; }

		public bool locked { get; private set; }

		public bool FullTrade { get; private set; }

		public bool IsIncoming { get; internal set; }

		public bool InvitationPending { get; internal set; }

		public IList<InventoryEntry> Items => new List<InventoryEntry>(items);

		public Action<TradeResult> ResultCallback { get; set; }

		internal Session(BotTradeService service, BotActor actor)
		{
			this.service = service;
			this.actor = actor;
		}

		public bool AddItem(InventoryEntry item, int slot)
		{
			if (!actor.IsBot() || locked)
			{
				return false;
			}
			return AddEscrow(item, slot);
		}

		private bool AddEscrow(InventoryEntry item, int slot)
		{
			if (ended || items.Count > 9)
			{
				return false;
			}
			foreach (InventoryEntry item2 in items)
			{
				if (item2.position == slot)
				{
					return false;
				}
			}
			InventoryEntry inventoryEntry = item.Copy();
			inventoryEntry.position = slot;
			items.Add(inventoryEntry);
			return true;
		}

		public bool SwapItem(InventoryEntry item, int slot)
		{
			if (!actor.IsBot() || locked || ended || items.Count > 9)
			{
				return false;
			}
			items.RemoveAll((InventoryEntry prior) => prior.position == slot);
			InventoryEntry inventoryEntry = item.Copy();
			inventoryEntry.position = slot;
			items.Add(inventoryEntry);
			return true;
		}

		public bool OfferFromInventory(int type, int position, int quantity, int targetSlot)
		{
			if (ended || locked || targetSlot < 1 || targetSlot > 9 || !InventoryDao.IsBagPosition(position) || quantity < 1)
			{
				return false;
			}
			List<InventoryEntry> list = Clone(Inventory(actor));
			InventoryEntry inventoryEntry = list.Find((InventoryEntry i) => i.inventoryType == type && i.position == position);
			if (inventoryEntry == null || quantity > inventoryEntry.quantity || !service.rules.CanOffer(inventoryEntry))
			{
				return false;
			}
			if (TradeItemRules.Rechargeable(inventoryEntry.itemId))
			{
				quantity = inventoryEntry.quantity;
			}
			InventoryEntry inventoryEntry2 = inventoryEntry.Copy();
			inventoryEntry2.quantity = quantity;
			if (!AddEscrow(inventoryEntry2, targetSlot))
			{
				return false;
			}
			inventoryEntry.quantity -= quantity;
			if (inventoryEntry.quantity == 0)
			{
				list.Remove(inventoryEntry);
			}
			try
			{
				service.CommitInventories(this, list, Balance(actor), null, null, 0L);
			}
			catch
			{
				items.RemoveAt(items.Count - 1);
				throw;
			}
			return true;
		}

		public void SetMeso(long amount)
		{
			if (locked)
			{
				throw new InvalidOperationException("Trade is locked");
			}
			if (!ended && amount >= 0 && Balance(actor) >= amount)
			{
				service.CommitInventories(this, Clone(Inventory(actor)), Balance(actor) - amount, null, null, 0L);
				meso += amount;
			}
		}

		public void SetMesoBot(long amount)
		{
			if (actor.IsBot() && !ended && !locked && amount >= 0)
			{
				meso = amount;
			}
		}

		public void Chat(string text)
		{
			if (!ended)
			{
				service.ChatOutput?.Invoke(actor, text);
			}
		}

		public void Accept()
		{
			if (!ended && other != null && actor.trade == this && other.actor.trade == other && !FullTrade && IsIncoming)
			{
				if (!InvitationPending)
				{
					Cancel(TradeResult.NO_RESPONSE);
					return;
				}
				InvitationPending = false;
				service.invitations.Remove(this);
				FullTrade = true;
				other.FullTrade = true;
			}
		}

		public void Confirm()
		{
			if (!ended && !locked && other != null)
			{
				locked = true;
				if (other.locked)
				{
					Complete();
				}
			}
		}

		private void Complete()
		{
			long num = Balance(actor) + other.meso - Fee(other.meso);
			long num2 = Balance(other.actor) + meso - Fee(meso);
			if (num < 0 || num2 < 0)
			{
				Cancel(TradeResult.UNSUCCESSFUL);
				return;
			}
			List<InventoryEntry> list = Clone(Inventory(actor));
			List<InventoryEntry> list2 = Clone(Inventory(other.actor));
			if (!BuildIncoming(this, list, other.items) || !BuildIncoming(other, list2, items))
			{
				return;
			}
			int num3 = GameConfigDao.GetInt(service.db, "trade_limit_meso_under_level");
			if (num3 == 0)
			{
				num3 = 15;
			}
			int num4 = GameConfigDao.GetInt(service.db, "trade_limit_meso_max");
			if (num4 == 0)
			{
				num4 = 1000000;
			}
			Session session;
			if (num3 != -1 && actor.level <= num3)
			{
				session = this;
			}
			else
			{
				session = ((num3 != -1 && other.actor.level <= num3) ? other : null);
			}
			long num5 = ((session == this) ? other.meso : meso);
			if (session != null && num4 != -1 && session.actor.mesosTraded + num5 > num4)
			{
				session.Cancel(TradeResult.NO_RESPONSE);
				return;
			}
			try
			{
				service.CommitInventories(this, list, num, other, list2, num2);
			}
			catch (Exception obj)
			{
				service.OnError?.Invoke(obj);
				Cancel(TradeResult.UNSUCCESSFUL);
				return;
			}
			if (session != null)
			{
				session.actor.mesosTraded += num5;
			}
			meso = 0L;
			other.meso = 0L;
			items.Clear();
			other.items.Clear();
			bool invitationPending = (other.InvitationPending = false);
			InvitationPending = invitationPending;
			service.invitations.Remove(this);
			service.invitations.Remove(other);
			ended = (other.ended = true);
			Notify(this, TradeResult.SUCCESSFUL);
			Notify(other, TradeResult.SUCCESSFUL);
			actor.trade = null;
			other.actor.trade = null;
		}

		private bool BuildIncoming(Session receiver, List<InventoryEntry> inventory, List<InventoryEntry> offered)
		{
			foreach (InventoryEntry item in offered)
			{
				if (service.rules.Unique(item.itemId) && item.quantity > 1)
				{
					receiver.Cancel(TradeResult.UNSUCCESSFUL);
					return false;
				}
				InventoryEntry inventoryEntry = item.Copy();
				TradeItemRules.ApplyKarma(inventoryEntry);
				if (service.rules.Receive(receiver.actor, inventory, inventoryEntry))
				{
					continue;
				}
				bool flag = false;
				foreach (InventoryEntry item2 in Inventory(receiver.actor))
				{
					if (item2.itemId == inventoryEntry.itemId && service.rules.Unique(inventoryEntry.itemId))
					{
						flag = true;
						break;
					}
				}
				receiver.Cancel(flag ? TradeResult.UNSUCCESSFUL_UNIQUE_ITEM_LIMIT : TradeResult.UNSUCCESSFUL);
				return false;
			}
			return true;
		}

		private static void Notify(Session session, TradeResult result)
		{
			BotStateMachine botById = BotStore.GetBotById(session.actor.id);
			if (botById != null && botById.chr == session.actor)
			{
				botById.tradeLogic.Clear(session.actor);
			}
			try
			{
				session.service.ResultOutput?.Invoke(session.actor, result);
				if (result == TradeResult.SUCCESSFUL)
				{
					session.ResultCallback?.Invoke(result);
				}
			}
			catch (Exception obj)
			{
				session.service.OnError?.Invoke(obj);
			}
		}

		public void Decline()
		{
			Cancel(TradeResult.PARTNER_CANCEL);
		}

		public void Cancel(TradeResult result)
		{
			if (ended)
			{
				return;
			}
			List<InventoryEntry> list = Clone(Inventory(actor));
			foreach (InventoryEntry item in items)
			{
				if (!service.rules.Receive(actor, list, item))
				{
					throw new InvalidOperationException("Cannot return escrowed trade items");
				}
			}
			List<InventoryEntry> list2 = null;
			if (other != null)
			{
				list2 = Clone(Inventory(other.actor));
				foreach (InventoryEntry item2 in other.items)
				{
					if (!service.rules.Receive(other.actor, list2, item2))
					{
						throw new InvalidOperationException("Cannot return partner escrowed trade items");
					}
				}
			}
			checked
			{
				service.CommitInventories(this, list, Balance(actor) + meso, other, list2, (other == null) ? 0 : (Balance(other.actor) + other.meso));
				InvitationPending = false;
				service.invitations.Remove(this);
				if (other != null)
				{
					other.InvitationPending = false;
					service.invitations.Remove(other);
				}
				meso = 0L;
				items.Clear();
				ended = true;
				actor.trade = null;
				Notify(this, (result == TradeResult.PARTNER_CANCEL) ? TradeResult.NO_RESPONSE : result);
				if (other != null)
				{
					other.meso = 0L;
					other.items.Clear();
					other.ended = true;
					other.actor.trade = null;
					Notify(other, (result == TradeResult.UNSUCCESSFUL_UNIQUE_ITEM_LIMIT) ? TradeResult.UNSUCCESSFUL : result);
				}
			}
		}
	}

	private readonly DbStore db;

	private readonly TradeItemRules rules;

	private readonly HashSet<Session> invitations = new HashSet<Session>();

	public Action<BotActor, string> ChatOutput;

	public Action<BotActor, TradeResult> ResultOutput;

	public Action<Exception> OnError;

	public Action<BotActor, BotActor> Invited;

	public TradeItemRules Rules => rules;

	public event Action<BotActor, long> InventoryCommitted;

	public BotTradeService(DbStore db, BotItemCatalog catalog)
	{
		this.db = db;
		rules = new TradeItemRules(db, catalog);
	}

	public Session Start(BotActor actor)
	{
		if (actor.trade is Session result)
		{
			return result;
		}
		if (actor.trade != null)
		{
			return null;
		}
		return (Session)(actor.trade = new Session(this, actor));
	}

	public bool Invite(BotActor from, BotActor to)
	{
		if (from == null || to == null || from.id == to.id || to.trade != null)
		{
			return false;
		}
		Session session = Start(from);
		if (session == null || session.FullTrade || to.trade != null || session.other != null)
		{
			return false;
		}
		int num = GameConfigDao.GetInt(db, "minimum_gm_level_to_trade");
		int num2 = ((from is BotCharacter botCharacter) ? botCharacter.gmLevel : (from.character?.record.gm ?? 0));
		int num3 = ((to is BotCharacter botCharacter2) ? botCharacter2.gmLevel : (to.character?.record.gm ?? 0));
		if ((num2 > 0 && num3 == 0 && num2 < num) || (num2 == 0 && num3 > 0 && num3 < num))
		{
			session.Cancel(TradeResult.NO_RESPONSE);
			return false;
		}
		Session session2 = (session.other = Start(to));
		session2.other = session;
		session2.IsIncoming = true;
		session2.InvitationPending = true;
		invitations.Add(session2);
		BotStateMachine botById = BotStore.GetBotById(to.id);
		if (to.IsBot() && botById != null && botById.chr == to)
		{
			botById.tradeLogic.queue.Add(to, from);
		}
		Invited?.Invoke(from, to);
		return true;
	}

	public void TickInvitationTimeouts()
	{
		foreach (Session item in new List<Session>(invitations))
		{
			if (item.invitationTicks > 5)
			{
				item.InvitationPending = false;
				invitations.Remove(item);
			}
			else
			{
				item.invitationTicks++;
			}
		}
	}

	public static long Fee(long amount)
	{
		if (amount < 100000000)
		{
			if (amount < 25000000)
			{
				if (amount < 10000000)
				{
					if (amount < 5000000)
					{
						if (amount < 1000000)
						{
							if (amount < 100000)
							{
								return 0L;
							}
							return amount * 8 / 1000;
						}
						return amount * 18 / 1000;
					}
					return amount * 3 / 100;
				}
				return amount * 4 / 100;
			}
			return amount * 5 / 100;
		}
		return amount * 6 / 100;
	}

	private static List<InventoryEntry> Inventory(BotActor actor)
	{
		return (actor is BotCharacter botCharacter) ? botCharacter.inventory : (actor.character?.inventory ?? throw new InvalidOperationException("Trade actor has no loaded inventory"));
	}

	private static long Balance(BotActor actor)
	{
		if (!(actor is BotCharacter botCharacter))
		{
			return actor.character.record.meso;
		}
		return botCharacter.meso;
	}

	private static void Balance(BotActor actor, long value)
	{
		if (actor is BotCharacter botCharacter)
		{
			botCharacter.meso = value;
		}
		else
		{
			actor.character.record.meso = value;
		}
	}

	private static List<InventoryEntry> Clone(List<InventoryEntry> source)
	{
		List<InventoryEntry> list = new List<InventoryEntry>();
		foreach (InventoryEntry item in source)
		{
			list.Add(item.Copy());
		}
		return list;
	}

	public void Persist(BotActor actor)
	{
		if (actor.IsBot())
		{
			return;
		}
		int id = actor.id;
		List<InventoryEntry> list = Inventory(actor);
		HashSet<long> ids = new HashSet<long>();
		foreach (DbRow item in InventoryDao.ItemsByCharacter(db, id))
		{
			ids.Add(DbStore.Long(item, "inventoryitemid", 0L));
		}
		Dictionary<long, DbRow> dictionary = new Dictionary<long, DbRow>();
		foreach (long id2 in ids)
		{
			DbRow dbRow = InventoryDao.EquipmentOf(db, id2);
			if (NativePotentialService.IsReserved(db, id2) && !list.Exists((InventoryEntry item) => item.itemRowId == id2))
			{
				throw new InvalidOperationException("待选方块装备不能从库存镜像移除");
			}
			if (dbRow == null || DbStore.Int(dbRow, "enhancementVersion") != 282)
			{
				continue;
			}
			DbRow dbRow2 = new DbRow();
			foreach (KeyValuePair<string, object> item2 in dbRow)
			{
				dbRow2[item2.Key] = item2.Value;
			}
			dictionary[id2] = dbRow2;
		}
		db.Table("inventoryequipment").Delete((DbRow r) => ids.Contains(DbStore.Long(r, "inventoryitemid", 0L)));
		db.Table("inventoryitems").Delete(DbStore.Eq("characterid", (long)id));
		foreach (InventoryEntry item3 in list)
		{
			DbRow dbRow3 = new DbRow();
			if (item3.itemData != null)
			{
				foreach (KeyValuePair<string, object> itemDatum in item3.itemData)
				{
					dbRow3[itemDatum.Key] = itemDatum.Value;
				}
			}
			if (ids.Contains(item3.itemRowId))
			{
				dbRow3["inventoryitemid"] = item3.itemRowId;
			}
			else
			{
				dbRow3.Remove("inventoryitemid");
			}
			dbRow3["characterid"] = id;
			dbRow3["type"] = item3.itemId / 1000000;
			dbRow3["itemid"] = item3.itemId;
			dbRow3["inventorytype"] = item3.inventoryType;
			dbRow3["position"] = item3.position;
			dbRow3["quantity"] = item3.quantity;
			dbRow3["flag"] = item3.flag;
			dbRow3["owner"] = item3.owner;
			dbRow3["giftFrom"] = item3.giftFrom;
			dbRow3["petid"] = item3.petId;
			dbRow3["expiration"] = item3.expiration;
			long num = (item3.itemRowId = db.Table("inventoryitems").Insert(dbRow3));
			item3.itemData = new Dictionary<string, object>(dbRow3);
			if (!item3.isEquip)
			{
				continue;
			}
			bool flag = dictionary.TryGetValue(num, out var value);
			DbRow dbRow4 = new DbRow();
			Dictionary<string, object> dictionary2 = (flag ? value : item3.equipmentData);
			if (dictionary2 != null)
			{
				foreach (KeyValuePair<string, object> item4 in dictionary2)
				{
					dbRow4[item4.Key] = item4.Value;
				}
			}
			dbRow4.Remove("inventoryequipmentid");
			dbRow4["inventoryitemid"] = num;
			if (!flag)
			{
				dbRow4["str"] = item3.str;
				dbRow4["dex"] = item3.dex;
				dbRow4["int"] = item3.int_;
				dbRow4["luk"] = item3.luk;
				dbRow4["hp"] = item3.hp;
				dbRow4["mp"] = item3.mp;
				dbRow4["watk"] = item3.watk;
				dbRow4["matk"] = item3.matk;
				dbRow4["wdef"] = item3.wdef;
				dbRow4["mdef"] = item3.mdef;
				dbRow4["acc"] = item3.acc;
				dbRow4["avoid"] = item3.avoid;
				dbRow4["hands"] = item3.hands;
				dbRow4["speed"] = item3.speed;
				dbRow4["jump"] = item3.jump;
				dbRow4["upgradeslots"] = item3.upgradeslots;
			}
			db.Table("inventoryequipment").Insert(dbRow4);
			item3.equipmentData = new Dictionary<string, object>(dbRow4);
		}
		db.Table("characters").Update(DbStore.Eq("id", (long)id), new DbRow { ["meso"] = Balance(actor) });
	}

	private void CommitInventories(Session first, List<InventoryEntry> left, long leftBalance, Session second, List<InventoryEntry> right, long rightBalance)
	{
		List<InventoryEntry> collection = Clone(Inventory(first.actor));
		long num = Balance(first.actor);
		List<InventoryEntry> collection2 = ((second == null) ? null : Clone(Inventory(second.actor)));
		long num2 = ((second == null) ? 0 : Balance(second.actor));
		db.BeginTransaction();
		try
		{
			Inventory(first.actor).Clear();
			Inventory(first.actor).AddRange(left);
			Balance(first.actor, leftBalance);
			Persist(first.actor);
			if (second != null)
			{
				Inventory(second.actor).Clear();
				Inventory(second.actor).AddRange(right);
				Balance(second.actor, rightBalance);
				Persist(second.actor);
			}
			db.Commit();
		}
		catch
		{
			db.Rollback();
			Inventory(first.actor).Clear();
			Inventory(first.actor).AddRange(collection);
			Balance(first.actor, num);
			if (second != null)
			{
				Inventory(second.actor).Clear();
				Inventory(second.actor).AddRange(collection2);
				Balance(second.actor, num2);
			}
			throw;
		}
		PublishInventory(first.actor, leftBalance - num);
		if (second != null)
		{
			PublishInventory(second.actor, rightBalance - num2);
		}
	}

	private void PublishInventory(BotActor actor, long mesoDelta)
	{
		if (actor.IsBot())
		{
			return;
		}
		actor.character.tradeRevision++;
		try
		{
			InventoryCommitted?.Invoke(actor, mesoDelta);
		}
		catch (Exception obj)
		{
			OnError?.Invoke(obj);
		}
	}

	public bool Purchase(BotActor actor, InventoryEntry item, long price)
	{
		if (actor == null || actor.trade != null || price < 0 || Balance(actor) < price)
		{
			return false;
		}
		List<InventoryEntry> list = Clone(Inventory(actor));
		List<InventoryEntry> list2 = Clone(list);
		long num = Balance(actor);
		if (!rules.Receive(actor, list2, item))
		{
			return false;
		}
		db.BeginTransaction();
		try
		{
			Inventory(actor).Clear();
			Inventory(actor).AddRange(list2);
			Balance(actor, num - price);
			Persist(actor);
			db.Commit();
		}
		catch
		{
			db.Rollback();
			Inventory(actor).Clear();
			Inventory(actor).AddRange(list);
			Balance(actor, num);
			throw;
		}
		PublishInventory(actor, -price);
		return true;
	}
}
