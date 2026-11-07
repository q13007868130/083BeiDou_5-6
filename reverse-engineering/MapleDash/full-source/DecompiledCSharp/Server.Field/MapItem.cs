using System;

namespace Server.Field;

public sealed class MapItem
{
	public int objectId;

	public InventoryEntry item;

	public int meso;

	public int dropperId;

	public int ownerCharacterId;

	public int partyOwnerId = -1;

	public int questId = -1;

	public byte type;

	public bool pickedUp;

	public bool playerDrop;

	public bool partyDrop;

	public bool permanentOwner;

	public long dropTime;

	public long expiresAt = long.MaxValue;

	public float x;

	public float y;

	public int pickupCharacterId;

	public int pickupPetIndex = -1;

	public int mapId;

	public int ItemId
	{
		get
		{
			if (meso <= 0)
			{
				return item.itemId;
			}
			return meso;
		}
	}

	public int Quantity
	{
		get
		{
			if (meso <= 0)
			{
				return item.quantity;
			}
			return meso;
		}
	}

	public bool IsMeso => meso > 0;

	public int ClientSideOwnerId
	{
		get
		{
			if (partyOwnerId != -1)
			{
				return partyOwnerId;
			}
			return ownerCharacterId;
		}
	}

	public long AgeMs(long nowMs)
	{
		return nowMs - dropTime;
	}

	public bool HasExpiredOwnershipTime(long nowMs)
	{
		return AgeMs(nowMs) >= 15000;
	}

	public bool IsFFADrop(long nowMs)
	{
		if (type != 2 && type != 3)
		{
			return HasExpiredOwnershipTime(nowMs);
		}
		return true;
	}

	public bool HasClientsideOwnership(long nowMs, int characterId, int partyId)
	{
		if (ownerCharacterId != characterId && partyOwnerId != partyId)
		{
			return HasExpiredOwnershipTime(nowMs);
		}
		return true;
	}

	public bool CanBePickedBy(long nowMs, int characterId, int partyId, Func<int, bool> isPartyMemberOfOwner)
	{
		if (ownerCharacterId <= 0 || IsFFADrop(nowMs))
		{
			return true;
		}
		if (partyOwnerId == -1)
		{
			if (characterId == ownerCharacterId)
			{
				return true;
			}
			if (isPartyMemberOfOwner != null && isPartyMemberOfOwner(ownerCharacterId))
			{
				partyOwnerId = partyId;
				return true;
			}
		}
		else
		{
			if (partyId == partyOwnerId)
			{
				return true;
			}
			if (characterId == ownerCharacterId)
			{
				partyOwnerId = partyId;
				return true;
			}
		}
		if (!permanentOwner)
		{
			return HasExpiredOwnershipTime(nowMs);
		}
		return false;
	}

	public void SetOwner(int characterId, int partyId)
	{
		ownerCharacterId = characterId;
		partyOwnerId = partyId;
		partyDrop = partyId != -1;
	}
}
