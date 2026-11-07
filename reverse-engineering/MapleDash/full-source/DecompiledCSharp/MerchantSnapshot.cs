using System.Collections.Generic;

public sealed class MerchantSnapshot
{
	public int ownerId;

	public int mapId;

	public int permitId;

	public string ownerName = "";

	public string description = "";

	public bool open;

	public bool published;

	public bool visitor;

	public bool mine;

	public long merchantMeso;

	public long startedAt;

	public readonly List<MerchantSlotData> items = new List<MerchantSlotData>();

	public readonly List<string> blacklist = new List<string>();
}
