using System.Collections.Generic;

public sealed class MakerRecipe
{
	public int itemId;

	public int category;

	public int reqLevel;

	public int reqSkillLevel;

	public int itemNum = 1;

	public int tuc;

	public int meso;

	public int catalyst = -1;

	public int reqItem;

	public int reqQuest;

	public int reqEquip;

	public readonly List<MakerReqItem> reqItems = new List<MakerReqItem>();

	public readonly List<MakerRewardItem> rewards = new List<MakerRewardItem>();
}
