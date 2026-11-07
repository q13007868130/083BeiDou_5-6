public static class VMatrixSlots
{
	public const int SkillCount = 4;

	public const int EnforceCount = 6;

	public const int CommonCount = 21;

	public const int Count = 31;

	public static int[] Assign(VMatrixGroup group)
	{
		int[] array = new int[31];
		if (group == null)
		{
			return array;
		}
		Fill(array, 0, group.skill);
		Fill(array, 4, group.enforce);
		Fill(array, 10, group.common);
		return array;
	}

	private static void Fill(int[] slots, int start, int[] ids)
	{
		if (ids != null)
		{
			for (int i = 0; i < ids.Length && start + i < slots.Length; i++)
			{
				slots[start + i] = ids[i];
			}
		}
	}

	public static int SectionOf(int slot)
	{
		if (slot >= 4)
		{
			if (slot >= 10)
			{
				return 2;
			}
			return 1;
		}
		return 0;
	}
}
