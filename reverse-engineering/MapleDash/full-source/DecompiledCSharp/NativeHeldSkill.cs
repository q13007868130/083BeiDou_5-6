using System;

public static class NativeHeldSkill
{
	public static bool Continuous(int id)
	{
		if (id != 3121004 && id != 5221004)
		{
			return id == 13111002;
		}
		return true;
	}

	public static bool KeyDown(int id)
	{
		switch (id)
		{
		case 1121001:
		case 1221001:
		case 1321001:
		case 2121001:
		case 2221001:
		case 2321001:
		case 3121004:
		case 3221001:
		case 5101004:
		case 5201002:
		case 5221004:
		case 13111002:
		case 14111006:
		case 15101003:
		case 22121000:
		case 22151001:
			return true;
		default:
			return false;
		}
	}

	public static int ReleaseElapsed(int id, long elapsed)
	{
		if (!KeyDown(id))
		{
			return 0;
		}
		return (int)Math.Min(Continuous(id) ? 2000 : 1000, Math.Max(30L, elapsed));
	}

	public static bool ShotDue(long now, long previous)
	{
		return ShotDue(now, previous, 0);
	}

	public static bool ShotDue(long now, long previous, int attackDelayMs)
	{
		return now - previous > ((attackDelayMs > 0) ? attackDelayMs : 100);
	}
}
