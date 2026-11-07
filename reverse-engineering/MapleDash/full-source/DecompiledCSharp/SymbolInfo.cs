using System;

public sealed class SymbolInfo
{
	public int itemId;

	public string name;

	public SymbolKind kind;

	public int maxLevel;

	public int initialInc;

	public int mainStatPerLevel;

	public int demonAvengerHpPerLevel;

	public int xenonPerLevel;

	public bool IsArcane => kind == SymbolKind.Arcane;

	public int GrowthCost(int level)
	{
		if (!IsArcane)
		{
			return 9 * level * level + 20 * level;
		}
		return level * level + 11;
	}

	public int IncAt(int level)
	{
		if (!IsArcane)
		{
			return Math.Min(110, 10 * level);
		}
		return Math.Min(220, 10 * level + 20);
	}

	public bool CanLevelUp(int level, int exp)
	{
		if (level >= 1 && level < maxLevel)
		{
			return exp >= GrowthCost(level);
		}
		return false;
	}
}
