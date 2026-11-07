public sealed class InventoryChangeRequest
{
	public ExtraStorageOperation extraStorage;

	public int extraStorageType;

	public int characterId;

	public int itemId;

	public int quantity;

	public int equipSlot;

	public int sourcePosition;

	public int petSlot;

	public int evolvePetId;

	public int petTameness;

	public bool petLeaderOnly;

	public bool readPetExclusions;

	public bool writePetExclusions;

	public int petId;

	public int[] petExclusions;

	public bool getPet;

	public bool createPet;

	public long itemExpiration = -1L;

	public bool equip;

	public bool projectile;

	public bool removeAll;

	public bool togglePet;

	public bool worldQuestDraw;

	public bool petTick;

	public int grantItemId;

	public int grantQuantity;

	public int grantMinutes;

	public bool consumeGrantSource;

	public bool breakthrough;

	public int bankExp;

	public int maxBankLevel;

	public long grantDurationMs = -1L;

	public int[] checkItems;

	public int[] checkQuantities;

	public int removeCheckItem;

	public int removeCheckQuantity;

	public bool dropToField;

	public int dropQuantity;

	public int dropMapId;

	public int dropX;

	public int dropY;

	public long dropMeso;

	public bool sortItems;

	public bool mergeItems;

	public int sortInventoryType;

	public int moveFrom;

	public int moveTo;

	public int moveInventoryType;
}
