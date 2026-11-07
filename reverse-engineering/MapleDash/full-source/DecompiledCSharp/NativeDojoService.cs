using System;
using System.Collections.Generic;
using UnityEngine;

public static class NativeDojoService
{
	public const int PartyBase = 925030000;

	public const int SoloBase = 925020000;

	public const int DojoExit = 925020002;

	private const int PartyRange = 5;

	private const int SoloRange = 15;

	private static int usedDojo;

	private static readonly Dictionary<long, int> dojoParty = new Dictionary<long, int>();

	private static readonly int[] dojoStage = new int[20];

	private static readonly long[] dojoFinishTime = new long[20];

	private static readonly int[] dojoFinishStage = new int[20];

	public static int LookupPartyDojo(long partyId)
	{
		if (partyId == 0L || !dojoParty.TryGetValue(partyId, out var value))
		{
			return -1;
		}
		return value;
	}

	public static int IngressDojo(bool isPartyDojo, long partyId, int fromStage)
	{
		int num = usedDojo;
		int num2 = 0;
		int num3;
		if (!isPartyDojo)
		{
			num >>= 5;
			num3 = 15;
		}
		else
		{
			num3 = 5;
		}
		while ((num & 1) != 0)
		{
			num >>= 1;
			num2++;
		}
		if (num2 >= num3)
		{
			return -1;
		}
		int num4 = (isPartyDojo ? 925030000 : 925020000) + 100 * (fromStage + 1) + num2;
		int num5 = SlotOf(num4);
		if (isPartyDojo)
		{
			if (partyId != 0L && dojoParty.ContainsKey(partyId))
			{
				return -2;
			}
			if (partyId != 0L)
			{
				dojoParty[partyId] = num5;
			}
		}
		usedDojo |= 1 << num5;
		StartSchedule(num4);
		return num2;
	}

	public static void ResetDojoMap(int fromMapId)
	{
	}

	public static void FreeDojoSectionIfEmpty(int mapId)
	{
		int num = SlotOf(mapId);
		if ((usedDojo & (1 << num)) != 0)
		{
			ReleaseSlot(num, PartyIdOfLocal());
		}
	}

	public static void DismissDojoSchedule(int mapId, long partyId)
	{
		int num = SlotOf(mapId);
		dojoFinishTime[num] = 0L;
		ReleaseSlot(num, partyId);
	}

	public static bool SetDojoProgress(int mapId)
	{
		int num = SlotOf(mapId);
		int num2 = mapId / 100 % 100;
		if (dojoStage[num] < num2)
		{
			dojoStage[num] = num2;
			return true;
		}
		return false;
	}

	public static long GetDojoFinishTime(int mapId)
	{
		return dojoFinishTime[SlotOf(mapId)];
	}

	public static long StageClockMillis(int stage)
	{
		return (long)((stage > 36) ? 15 : (stage / 6 + 5)) * 60000L;
	}

	private static void StartSchedule(int slotMapId)
	{
		int num = SlotOf(slotMapId);
		int num2 = slotMapId / 100 % 100;
		if (num2 > dojoStage[num])
		{
			dojoFinishStage[num] = num2;
			dojoFinishTime[num] = ServerClock() + StageClockMillis(num2) + 3000;
		}
	}

	public static void Tick()
	{
		long num = ServerClock();
		for (int i = 0; i < 20; i++)
		{
			if (dojoFinishTime[i] != 0L && num >= dojoFinishTime[i])
			{
				dojoFinishTime[i] = 0L;
				long partyId = PartyIdOfLocal();
				FieldDirector fieldDirector = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.field : null);
				if (fieldDirector?.Map != null && IsDojo(fieldDirector.Map.id))
				{
					NpcSession.MapContext().RequestWarp(925020002);
				}
				ReleaseSlot(i, partyId);
			}
		}
	}

	private static void ReleaseSlot(int slot, long partyId)
	{
		usedDojo &= ~(1 << slot);
		dojoFinishTime[slot] = 0L;
		if (partyId != 0L && dojoParty.Remove(partyId))
		{
			return;
		}
		long num = 0L;
		foreach (KeyValuePair<long, int> item in dojoParty)
		{
			if (item.Value == slot)
			{
				num = item.Key;
				break;
			}
		}
		if (num != 0L)
		{
			dojoParty.Remove(num);
		}
	}

	public static int SlotOf(int dojoMapId)
	{
		return dojoMapId % 100 + ((dojoMapId / 10000 == 92502) ? 5 : 0);
	}

	public static bool IsDojo(int mapId)
	{
		if (mapId < 925020000 || mapId >= 925020600)
		{
			if (mapId >= 925030000)
			{
				return mapId <= 925033804;
			}
			return false;
		}
		return true;
	}

	private static long PartyIdOfLocal()
	{
		if (!GameSession.InGame)
		{
			return 0L;
		}
		SocialResponse socialResponse = GameSession.Net.Social(new SocialRequest
		{
			characterId = GameSession.CharacterId
		});
		return socialResponse.party ? socialResponse.partyId : 0;
	}

	private static long ServerClock()
	{
		return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
	}
}
