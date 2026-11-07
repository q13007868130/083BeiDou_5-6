using System.Collections.Generic;

public sealed class CashResponse
{
	public bool ok = true;

	public bool opened;

	public string error = "";

	public int credit;

	public int points;

	public int prepaid;

	public int gifts;

	public int storageSlots;

	public List<CashCommodity> catalog = new List<CashCommodity>();

	public List<CashStock> stock = new List<CashStock>();

	public List<int> wishes = new List<int>();

	public FullCharacter character;

	public int rewardItemId;

	public int rewardQuantity = 1;

	public int rewardSn;
}
