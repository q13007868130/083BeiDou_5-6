using System.Collections.Generic;

public sealed class InventoryEntry
{
	public long itemRowId;

	public long expiration = -1L;

	public int itemId;

	public int inventoryType;

	public int position;

	public int quantity;

	public int flag;

	public string owner = "";

	public string giftFrom = "";

	public int petId = -1;

	public Dictionary<string, object> petData;

	public Dictionary<string, object> itemData;

	public Dictionary<string, object> equipmentData;

	public bool isEquip;

	public int str;

	public int dex;

	public int int_;

	public int luk;

	public int hp;

	public int mp;

	public int watk;

	public int matk;

	public int wdef;

	public int mdef;

	public int acc;

	public int avoid;

	public int hands;

	public int speed;

	public int jump;

	public int upgradeslots;

	public int equipLevel;

	public int maxStar;

	public int starLevel;

	public int starCount;

	public int symbolLevel;

	public int symbolExp;

	public int symbolInc;

	public long exGradeOption;

	public string flameChoice = "";

	public int flameRevision;

	public int soulSocketId;

	public int soulItemId;

	public int soulOptionId;

	public int soulOption;

	public InventoryEntry Copy()
	{
		InventoryEntry inventoryEntry = (InventoryEntry)MemberwiseClone();
		if (itemData != null)
		{
			inventoryEntry.itemData = new Dictionary<string, object>(itemData);
		}
		if (equipmentData != null)
		{
			inventoryEntry.equipmentData = new Dictionary<string, object>(equipmentData);
		}
		if (petData != null)
		{
			inventoryEntry.petData = new Dictionary<string, object>(petData);
		}
		return inventoryEntry;
	}
}
