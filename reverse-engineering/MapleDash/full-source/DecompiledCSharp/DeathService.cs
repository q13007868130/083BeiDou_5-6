using System;

public static class DeathService
{
	public const int SafetyCharm = 5130000;

	public const int EasterBasket = 4031283;

	public const int EasterCharm = 4140903;

	public const int WheelOfFortune = 5510000;

	private static readonly int[] FullHpRespawnMaps = new int[7] { 262030300, 262031300, 450010100, 221040001, 450009400, 900000207, 410002060 };

	public const int MapNone = 999999999;

	public static DeathResult PlayerDead(CharRecord chr, int mapId, IMapInfo mapInfo, int safetyCharmCount)
	{
		DeathResult deathResult = new DeathResult();
		if (safetyCharmCount > 0)
		{
			deathResult.usedSafetyCharm = true;
			return deathResult;
		}
		if (chr.job == 0)
		{
			deathResult.noExpLoss = true;
			return deathResult;
		}
		if (mapInfo != null && mapInfo.NoExpDecrease(mapId))
		{
			deathResult.noExpLoss = true;
			return deathResult;
		}
		long expNeededForLevel = ServerExpTable.GetExpNeededForLevel(chr.level);
		if (mapInfo == null || !mapInfo.IsTown(mapId))
		{
			expNeededForLevel = ((chr.luk < 50) ? (expNeededForLevel / 10) : (expNeededForLevel / 20));
		}
		else
		{
			expNeededForLevel /= 100;
		}
		deathResult.expLost = ((chr.exp > expNeededForLevel) ? expNeededForLevel : chr.exp);
		return deathResult;
	}

	public static int RespawnHp(int returnMap, bool usedSafetyCharm, int maxHp)
	{
		int[] fullHpRespawnMaps = FullHpRespawnMaps;
		for (int i = 0; i < fullHpRespawnMaps.Length; i++)
		{
			if (fullHpRespawnMaps[i] == returnMap)
			{
				return maxHp;
			}
		}
		if (usedSafetyCharm)
		{
			return (int)Math.Ceiling((double)maxHp * 0.3);
		}
		return 50;
	}

	public static int RespawnMp(int returnMap, bool usedSafetyCharm, int maxMp)
	{
		int[] fullHpRespawnMaps = FullHpRespawnMaps;
		for (int i = 0; i < fullHpRespawnMaps.Length; i++)
		{
			if (fullHpRespawnMaps[i] == returnMap)
			{
				return maxMp;
			}
		}
		if (usedSafetyCharm)
		{
			return (int)Math.Ceiling((double)maxMp * 0.3);
		}
		return maxMp;
	}

	public static int RespawnTargetMap(int currentMap, IMapInfo mapInfo)
	{
		if (mapInfo == null)
		{
			return currentMap;
		}
		int num = mapInfo.ReturnMap(currentMap);
		if (num != 999999999)
		{
			return num;
		}
		return currentMap;
	}
}
