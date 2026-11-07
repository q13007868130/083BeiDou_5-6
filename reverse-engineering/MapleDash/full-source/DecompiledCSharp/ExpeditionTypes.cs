using System;

public static class ExpeditionTypes
{
	public static readonly string[] Names = Enum.GetNames(typeof(ExpeditionType));

	public static ExpeditionType[] All => (ExpeditionType[])Enum.GetValues(typeof(ExpeditionType));

	public static void Stats(ExpeditionType type, out int minSize, out int maxSize, out int minLevel, out int maxLevel, out int registrationMinutes)
	{
		minSize = 1;
		maxSize = 30;
		registrationMinutes = 5;
		switch (type)
		{
		case ExpeditionType.ARIANT:
		case ExpeditionType.ARIANT1:
		case ExpeditionType.ARIANT2:
			maxSize = 7;
			minLevel = 20;
			break;
		case ExpeditionType.BALROG_EASY:
		case ExpeditionType.BALROG_NORMAL:
		case ExpeditionType.ZAKUM:
			minLevel = 50;
			break;
		case ExpeditionType.CWKPQ:
			minLevel = 90;
			break;
		case ExpeditionType.SCARGA:
		case ExpeditionType.SHOWA:
		case ExpeditionType.HORNTAIL:
		case ExpeditionType.KARING:
			minLevel = 100;
			break;
		case ExpeditionType.CHAOS_ZAKUM:
		case ExpeditionType.CHAOS_HORNTAIL:
		case ExpeditionType.PINKBEAN:
			minLevel = 120;
			break;
		case ExpeditionType.AKAYRUM:
			minLevel = 140;
			break;
		case ExpeditionType.CYGNUS:
			minLevel = 170;
			break;
		case ExpeditionType.LUCID:
			minLevel = 220;
			break;
		default:
			minLevel = 50;
			break;
		}
		maxLevel = 255;
	}

	public static int MinSize(ExpeditionType t)
	{
		Stats(t, out var minSize, out var _, out var _, out var _, out var _);
		return minSize;
	}

	public static int MaxSize(ExpeditionType t)
	{
		Stats(t, out var _, out var maxSize, out var _, out var _, out var _);
		return maxSize;
	}

	public static int MinLevel(ExpeditionType t)
	{
		Stats(t, out var _, out var _, out var minLevel, out var _, out var _);
		return minLevel;
	}

	public static int MaxLevel(ExpeditionType t)
	{
		Stats(t, out var _, out var _, out var _, out var maxLevel, out var _);
		return maxLevel;
	}

	public static int RegistrationMinutes(ExpeditionType t)
	{
		Stats(t, out var _, out var _, out var _, out var _, out var registrationMinutes);
		return registrationMinutes;
	}

	public static bool TryParse(string name, out ExpeditionType type)
	{
		ExpeditionType[] all = All;
		for (int i = 0; i < all.Length; i++)
		{
			ExpeditionType expeditionType = all[i];
			if (string.Equals(expeditionType.ToString(), name, StringComparison.Ordinal))
			{
				type = expeditionType;
				return true;
			}
		}
		type = ExpeditionType.BALROG_EASY;
		return false;
	}
}
