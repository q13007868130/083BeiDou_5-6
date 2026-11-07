using System;
using System.Collections.Generic;

public sealed class HexaGroup
{
	public int[] mastery = new int[0];

	public int[] enforce = new int[0];

	public int[] skill = new int[0];

	public int[] common = new int[0];

	public IEnumerable<int> All
	{
		get
		{
			int[] array = mastery;
			for (int i = 0; i < array.Length; i++)
			{
				yield return array[i];
			}
			array = enforce;
			for (int i = 0; i < array.Length; i++)
			{
				yield return array[i];
			}
			array = skill;
			for (int i = 0; i < array.Length; i++)
			{
				yield return array[i];
			}
			array = common;
			for (int i = 0; i < array.Length; i++)
			{
				yield return array[i];
			}
		}
	}

	public bool Contains(int id)
	{
		if (Array.IndexOf(mastery, id) < 0 && Array.IndexOf(enforce, id) < 0 && Array.IndexOf(skill, id) < 0)
		{
			return Array.IndexOf(common, id) >= 0;
		}
		return true;
	}
}
