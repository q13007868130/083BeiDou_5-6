using System;

namespace Server.Bot.Decorator;

public sealed class JobPathLogic
{
	private readonly Random random;

	public JobPathLogic(Random random = null)
	{
		this.random = random ?? new Random();
	}

	public int DetermineThiefPath(int job)
	{
		if (job < 410 || job > 412)
		{
			if (job < 420 || job > 422)
			{
				if (random.Next(2) != 0)
				{
					return 420;
				}
				return 410;
			}
			return 420;
		}
		return 410;
	}

	public int DetermineWarriorPath(int job)
	{
		if (job < 110 || job > 112)
		{
			if (job < 120 || job > 122)
			{
				if (job < 130 || job > 132)
				{
					return 110 + random.Next(3) * 10;
				}
				return 130;
			}
			return 120;
		}
		return 110;
	}

	public int DetermineBowmanPath(int job)
	{
		if (job < 310 || job > 312)
		{
			if (job < 320 || job > 322)
			{
				if (random.Next(2) != 0)
				{
					return 320;
				}
				return 310;
			}
			return 320;
		}
		return 310;
	}
}
