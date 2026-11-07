public static class NativeRebirthRules
{
	private static readonly int[] BaseJobs = new int[22]
	{
		110, 120, 130, 200, 210, 220, 230, 300, 310, 320,
		400, 410, 420, 500, 510, 520, 1100, 1200, 1300, 1400,
		1500, 2000
	};

	private static readonly int[] AdventurerBranches = new int[10] { 220, 210, 230, 110, 120, 130, 310, 320, 410, 420 };

	private static bool IsA(int job, int parent)
	{
		if (job / 10 != parent / 10 || job < parent)
		{
			if (parent / 10 % 10 == 0)
			{
				return job / 100 == parent / 100;
			}
			return false;
		}
		return true;
	}

	public static bool IsCygnusFifth(int job)
	{
		if (job >= 1112 && job <= 1512)
		{
			return job % 100 == 12;
		}
		return false;
	}

	public static int JobAtLevel(int current, int level, int requestedJob, bool completedCygnusFifth)
	{
		int num = requestedJob;
		if (num <= 0)
		{
			int job = current;
			int[] baseJobs = BaseJobs;
			foreach (int num2 in baseJobs)
			{
				if ((num2 >= 1000) ? (num2 / 100 == current / 100) : (num2 / 10 == current / 10))
				{
					job = num2;
					break;
				}
			}
			num = current;
			baseJobs = AdventurerBranches;
			foreach (int num3 in baseJobs)
			{
				if (IsA(job, num3))
				{
					num = ((level >= 8) ? ((level < 31) ? (num3 / 100 * 100) : ((level < 70) ? num3 : ((level < 120) ? (num3 + 1) : (num3 + 2)))) : 0);
					break;
				}
			}
			for (int j = 1100; j <= 1500; j += 100)
			{
				if (IsA(job, j))
				{
					if (level < 10)
					{
						num = 1000;
					}
					else if (level < 30)
					{
						num = j;
					}
					else if (level < 70)
					{
						num = j + 10;
					}
					else
					{
						num = ((level < 120) ? (j + 11) : (j + 12));
					}
				}
			}
			if (IsA(job, 2000))
			{
				if (level < 10)
				{
					num = 2000;
				}
				else if (level < 30)
				{
					num = 2100;
				}
				else if (level < 70)
				{
					num = 2110;
				}
				else
				{
					num = ((level < 120) ? 2111 : 2112);
				}
			}
		}
		if (completedCygnusFifth || !IsCygnusFifth(num))
		{
			return num;
		}
		return num - 1;
	}
}
