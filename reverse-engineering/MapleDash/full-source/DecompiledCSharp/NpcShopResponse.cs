using System.Collections.Generic;

public sealed class NpcShopResponse
{
	public bool ok = true;

	public bool opened;

	public string error = "";

	public int shopId;

	public int npcId;

	public int mapId;

	public int androidTemplateId;

	public List<NpcShopItem> items = new List<NpcShopItem>();

	public FullCharacter character;

	public Dictionary<int, int> sellPrices = new Dictionary<int, int>();
}
