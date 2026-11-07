using System;

public static class NativeBossRules
{
	public const long MobAttackGapMillis = 2000L;

	public static bool IsKaring(int id)
	{
		if (id != 8880830 && id != 8880831 && id != 8880832 && id != 8880837)
		{
			return id == 8880842;
		}
		return true;
	}

	public static bool IsLucid(int id)
	{
		if (id != 8880140 && id != 8880141)
		{
			return id == 8880152;
		}
		return true;
	}

	public static int RegenMillis(int id)
	{
		return id switch
		{
			8880830 => 3240, 
			8880831 => 2520, 
			8880832 => 2340, 
			8880837 => 6660, 
			8880842 => 8100, 
			8880153 => 5000, 
			_ => 0, 
		};
	}

	public static int AttackCooldown(int id, int position, int fallback)
	{
		if ((id == 8880140 && position == 1) || (id == 8880141 && position == 2))
		{
			return 60000;
		}
		int[] array = null;
		switch (id)
		{
		case 8880830:
			array = new int[5] { 15000, 15000, 25000, 60000, 20000 };
			break;
		case 8880831:
			array = new int[4] { 60000, 15000, 15000, 25000 };
			break;
		case 8880832:
			array = new int[6] { 15000, 12000, 5400, 8000, 12000, 0 };
			break;
		case 8880837:
			array = new int[6] { 12000, 12000, 18000, 15000, 30000, 7000 };
			break;
		case 8880842:
			array = new int[4] { 12000, 12000, 18000, 15000 };
			break;
		}
		if (array == null || position < 0 || position >= array.Length || array[position] <= 0)
		{
			return fallback;
		}
		return array[position];
	}

	public static long SkillCooldown(int mob, int skill, int level, long fallback)
	{
		if ((mob == 8880140 || mob == 8880141) && skill == 128 && level == 16)
		{
			return 60000L;
		}
		if (mob == 8880830 && skill == 128 && level == 1)
		{
			return 60000L;
		}
		if (mob == 8880830 && skill == 126 && level == 7)
		{
			return 20000L;
		}
		if (mob == 8880831 && skill == 123 && level == 3)
		{
			return 10000L;
		}
		if (mob == 8880831 && skill == 120 && level == 5)
		{
			return 45000L;
		}
		if (mob == 8880837 && skill == 128 && level == 2)
		{
			return 18000L;
		}
		return fallback;
	}

	public static bool Priority(int mob, int skill)
	{
		if ((mob == 8880301 || mob == 8880000 || mob == 8880140 || mob == 8880342) && skill >= 183)
		{
			return skill <= 187;
		}
		return false;
	}

	public static int ButterflyInterval(int hp)
	{
		if (hp < 90)
		{
			if (hp < 70)
			{
				if (hp < 50)
				{
					if (hp < 20)
					{
						return 2000;
					}
					return 3000;
				}
				return 4000;
			}
			return 4500;
		}
		return 5000;
	}

	public static int ButterflyCount(int hp)
	{
		if (hp < 90)
		{
			if (hp < 70)
			{
				if (hp < 50)
				{
					if (hp < 20)
					{
						return 20;
					}
					return 15;
				}
				return 10;
			}
			return 7;
		}
		return 5;
	}

	public static bool MobAttackAllowed(long now, long lastStamp)
	{
		if (lastStamp != 0L)
		{
			return now - lastStamp >= 2000;
		}
		return true;
	}

	public static int PercentDamage(int maximum, int percent)
	{
		return (int)Math.Min(2147483647L, Math.Max(1L, (long)maximum * (long)percent / 100));
	}
}
