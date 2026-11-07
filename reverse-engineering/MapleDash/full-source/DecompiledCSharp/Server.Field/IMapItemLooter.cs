using System.Collections.Generic;

namespace Server.Field;

public interface IMapItemLooter
{
	int CharacterId { get; }

	int PartyId { get; }

	bool IsLoggedInWorld(int characterId);

	bool IsPartyMember(int characterId);

	IReadOnlyList<int> PartyMembersOnSameMap();

	void GainMeso(int amount);

	bool HasSpace(InventoryEntry item);

	bool AddFromDrop(InventoryEntry item);

	bool NeedQuestItem(int questId, int itemId);

	bool IsConsumeOnPickup(int itemId);

	bool ApplyConsumeOnPickup(int itemId);

	bool TryRunItemScript(int itemId);

	void GainNxCredit(int amount);

	void OnAriantSpiritJewel();

	void ShowItemUnavailable();

	void ShowInventoryFull();

	void EnableActions();
}
