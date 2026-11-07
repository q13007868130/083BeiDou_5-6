using System.Collections.Generic;

public static class VMatrixLevels
{
	private static readonly Dictionary<int, int> matrixLevels = new Dictionary<int, int>();

	private static readonly Dictionary<int, int> hexaLevels = new Dictionary<int, int>();

	private static int owner = -1;

	public static void Clear()
	{
		matrixLevels.Clear();
		hexaLevels.Clear();
		owner = -1;
	}

	private static void Own(int characterId)
	{
		if (owner != characterId)
		{
			matrixLevels.Clear();
			hexaLevels.Clear();
			owner = characterId;
		}
	}

	public static void Capture(VMatrixResponse response, int characterId)
	{
		Own(characterId);
		if (response?.cores == null)
		{
			return;
		}
		matrixLevels.Clear();
		foreach (VMatrixCoreView core in response.cores)
		{
			if (core == null)
			{
				continue;
			}
			VMatrixCore vMatrixCore = VMatrixCatalog.Core(core.coreId);
			int[] array = ((vMatrixCore != null) ? vMatrixCore.skills : core.skills);
			if (array == null)
			{
				continue;
			}
			int[] array2 = array;
			foreach (int num in array2)
			{
				if (num != 0 && (!matrixLevels.TryGetValue(num, out var value) || core.level > value))
				{
					matrixLevels[num] = core.level;
				}
			}
		}
	}

	public static void CaptureHexa(HexaResponse response, int characterId)
	{
		Own(characterId);
		if (response?.cores == null)
		{
			return;
		}
		hexaLevels.Clear();
		foreach (HexaCoreView core in response.cores)
		{
			if (core == null || core.level <= 0 || core.skills == null)
			{
				continue;
			}
			int[] skills = core.skills;
			foreach (int num in skills)
			{
				if (num != 0 && (!hexaLevels.TryGetValue(num, out var value) || core.level > value))
				{
					hexaLevels[num] = core.level;
				}
			}
		}
	}

	public static bool TryGet(int skillId, out int level)
	{
		if (!matrixLevels.TryGetValue(skillId, out level))
		{
			return hexaLevels.TryGetValue(skillId, out level);
		}
		return true;
	}

	public static int Count(bool hexa)
	{
		if (!hexa)
		{
			return matrixLevels.Count;
		}
		return hexaLevels.Count;
	}
}
