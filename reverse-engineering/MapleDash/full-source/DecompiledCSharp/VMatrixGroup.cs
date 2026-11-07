using System;

public sealed class VMatrixGroup
{
	public int[] skill = new int[0];

	public int[] enforce = new int[0];

	public int[] common = new int[0];

	public bool Contains(int id)
	{
		if (Array.IndexOf(skill, id) < 0 && Array.IndexOf(enforce, id) < 0)
		{
			return Array.IndexOf(common, id) >= 0;
		}
		return true;
	}
}
