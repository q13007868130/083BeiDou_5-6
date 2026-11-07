using System;
using Server.Bot;
using Server.Field;
using UnityEngine;

public sealed class GroundItemBridge : MonoBehaviour
{
	private FieldDirector owner;

	private BotRuntime runtime;

	private BotActorLooter looter;

	private int looterId = -1;

	private Func<MapItem, bool> pickupHandler;

	private Func<MapItem, int, bool> petPickupHandler;

	private bool subscribed;

	private Func<Nullable> companionPickupTarget;

	private int CurrentMapId
	{
		get
		{
			if (!((Object)(object)owner != (Object)null) || owner.Map == null)
			{
				return -1;
			}
			return owner.StateId;
		}
	}

	public void Bind(FieldDirector field, BotRuntime bots)
	{
		owner = field;
		runtime = bots;
		MapItemService.ExpireMilliseconds = ((GameServer.Local == null) ? 180000 : GameConfigDao.GetInt(GameServer.Local.Db, "item_expire_time", 180000));
		if (!subscribed)
		{
			pickupHandler = TryPickup;
			MapItemService.Spawned += OnSpawned;
			MapItemService.Removed += OnRemoved;
			GroundItem.AuthoritativePickup = pickupHandler;
			petPickupHandler = TryPetPickup;
			GroundItem.AuthoritativePetPickup = petPickupHandler;
			subscribed = true;
		}
	}

	public void Unbind()
	{
		if (subscribed)
		{
			subscribed = false;
			MapItemService.Spawned -= OnSpawned;
			MapItemService.Removed -= OnRemoved;
			if ((object)GroundItem.AuthoritativePickup == pickupHandler)
			{
				GroundItem.AuthoritativePickup = null;
			}
			if ((object)GroundItem.AuthoritativePetPickup == petPickupHandler)
			{
				GroundItem.AuthoritativePetPickup = null;
			}
			petPickupHandler = null;
			pickupHandler = null;
			looter = null;
			looterId = -1;
		}
	}

	private void OnDisable()
	{
		Unbind();
	}

	private void OnDestroy()
	{
		Unbind();
	}

	private BotActorLooter Looter()
	{
		if (looter != null && looterId == GameSession.CharacterId)
		{
			return looter;
		}
		looter = null;
		looterId = -1;
		if (runtime == null)
		{
			return null;
		}
		BotActor botActor = runtime.Player(GameSession.CharacterId);
		if (botActor == null)
		{
			return null;
		}
		looter = new BotActorLooter(botActor, runtime.trades, runtime.parties, new BotLootHooks
		{
			NeedQuestItem = QuestLogic.NeedQuestItem,
			GainNxCredit = (int amount) =>
			{
				GameSession.Net.Cash(new CashRequest
				{
					characterId = GameSession.CharacterId,
					operation = CashOperation.AdjustBalance,
					currency = 1,
					amount = amount
				});
			}
		});
		looterId = botActor.id;
		return looter;
	}

	private static Vector3 ToUnity(MapItem item)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		return new Vector3(item.x / 40f, (0f - item.y) / 40f, 0f);
	}

	private void OnSpawned(MapItem item)
	{
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		if (item == null || item.mapId != CurrentMapId || (!item.IsMeso && !QuestLogic.NeedQuestItem(item.questId, item.ItemId)))
		{
			return;
		}
		foreach (GroundItem item2 in GroundItem.All)
		{
			if ((Object)(object)item2 != (Object)null && item2.bound != null && item2.bound.objectId == item.objectId)
			{
				return;
			}
		}
		BotCharacter botCharacter = BotRegistry.Get(item.dropperId);
		MapReactor mapReactor = MapReactorService.Find(item.mapId, item.dropperId);
		Vector3 value;
		if (botCharacter != null && botCharacter.mapId == item.mapId)
		{
			value = new Vector3(botCharacter.x / 40f, (0f - botCharacter.y) / 40f, 0f);
		}
		else
		{
			value = (Vector3)((mapReactor != null) ? new Vector3((float)mapReactor.x / 40f, (float)(-mapReactor.y) / 40f, 0f) : ToUnity(item));
		}
		if (item.dropperId == GameSession.CharacterId && item.ownerCharacterId == GameSession.CharacterId)
		{
			PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
			if ((Object)(object)playerCtrl != (Object)null)
			{
				value = Vector2.op_Implicit(playerCtrl.FeetPos);
			}
		}
		GroundItem.Spawn((!item.IsMeso) ? item.ItemId : 0, item.IsMeso ? item.meso : item.Quantity, ToUnity(item), item, value);
	}

	private void OnRemoved(MapItem item, int reason)
	{
		if (item == null)
		{
			return;
		}
		GroundItem[] array = GroundItem.All.ToArray();
		foreach (GroundItem groundItem in array)
		{
			if (!((Object)(object)groundItem != (Object)null) || groundItem.bound == null || groundItem.bound.objectId != item.objectId)
			{
				continue;
			}
			Func<Nullable> target = null;
			if (item.pickupCharacterId == GameSession.CharacterId && item.pickupPetIndex < 0)
			{
				target = companionPickupTarget ?? new Func<Nullable>(GroundItem.LocalPickupTarget);
			}
			else if (item.pickupCharacterId == GameSession.CharacterId && item.pickupPetIndex >= 0)
			{
				int pet = item.pickupPetIndex;
				target = () => (!((Object)(object)owner != (Object)null)) ? ((Nullable)null) : ((Component)owner).GetComponent<NativePetField>()?.PickupTarget(pet);
			}
			else if (item.pickupCharacterId != 0 && item.pickupPetIndex < 0)
			{
				int picker = item.pickupCharacterId;
				target = () => (!((Object)(object)owner != (Object)null)) ? ((Nullable)null) : ((Component)owner).GetComponent<BotField>()?.PickupTarget(picker);
			}
			groundItem.Remove(reason, target, item.pickupCharacterId == GameSession.CharacterId);
		}
	}

	public bool TryPickupForCompanion(MapItem item, Func<Nullable> target)
	{
		Func<Nullable> func = companionPickupTarget;
		companionPickupTarget = target;
		try
		{
			return TryPickup(item, -1);
		}
		finally
		{
			companionPickupTarget = func;
		}
	}

	private bool TryPickup(MapItem item)
	{
		return TryPickup(item, -1);
	}

	private bool TryPetPickup(MapItem item, int petSlot)
	{
		if (item == null)
		{
			return false;
		}
		if (GameSession.Net.ChangeInventory(new InventoryChangeRequest
		{
			characterId = GameSession.CharacterId,
			getPet = true,
			petSlot = petSlot
		}).pet == null || !NativePetField.CanPickup(petSlot, item.IsMeso))
		{
			return false;
		}
		if (NativePetItems.ExcludesPickup(GameSession.Server.Db, GameSession.CharacterId, (!item.IsMeso) ? item.ItemId : 0))
		{
			return false;
		}
		return TryPickup(item, petSlot);
	}

	private bool TryPickup(MapItem item, int petSlot)
	{
		BotActorLooter botActorLooter = Looter();
		if (item == null || botActorLooter == null)
		{
			return false;
		}
		if (item.mapId != CurrentMapId)
		{
			return false;
		}
		BotActor botActor = runtime.Player(GameSession.CharacterId);
		long meso = botActor.character.record.meso;
		int itemId = item.ItemId;
		int quantity = item.Quantity;
		bool isMeso = item.IsMeso;
		bool flag = MapItemPickup.PickupItem(item, botActorLooter, (long)(Time.realtimeSinceStartup * 1000f), petSlot, companionPickupTarget == null);
		if (flag)
		{
			long num = botActor.character.record.meso - meso;
			GameSession.ApplyGroundPickup(botActor, num);
			NativeGainNotice.Pickup((!isMeso) ? itemId : 0, isMeso ? num : quantity);
		}
		return flag;
	}
}
