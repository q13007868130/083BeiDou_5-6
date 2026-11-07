using System.Collections.Generic;

public sealed class MakerResponse
{
	public bool ok = true;

	public string error = "";

	public short status;

	public bool craftSucceeded = true;

	public long meso = -1L;

	public int level;

	public int makerLevel = -1;

	public List<MakerRecipeView> recipes;

	public List<MakerIngredient> gained;

	public List<InventoryEntry> inventory;
}
