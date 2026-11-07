using System.Collections.Generic;

public sealed class MakerRecipeView
{
	public int itemId;

	public int category;

	public int reqLevel;

	public int reqSkillLevel;

	public int itemNum;

	public int tuc;

	public int meso;

	public int catalyst;

	public List<MakerIngredient> reqItems = new List<MakerIngredient>();
}
