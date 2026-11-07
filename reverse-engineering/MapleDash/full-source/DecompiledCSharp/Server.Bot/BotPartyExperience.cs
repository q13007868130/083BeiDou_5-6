using System;
using System.Collections.Generic;

namespace Server.Bot;

public static class BotPartyExperience
{
	public readonly struct Award(long personal, long party, bool eligible, int reputationCalls = 1)
	{
		public readonly long personal = personal;

		public readonly long party = party;

		public readonly int reputationCalls = (eligible ? reputationCalls : 0);

		public readonly bool eligible = eligible;
	}

	private static long Rounded(double value)
	{
		if (!(value >= 9.223372036854776E+18))
		{
			if (!(value <= -9.223372036854776E+18))
			{
				return (long)Math.Floor(value + 0.5);
			}
			return long.MinValue;
		}
		return long.MaxValue;
	}

	public static Award Calculate(DbStore db, BotPartyService parties, BotActor player, int mobLevel, int mobExp, long totalHp, IReadOnlyDictionary<int, long> damage, float personalRate = 1f, float partyRate = 1f, int holySymbol = 0, bool fullHolySymbol = false, int expIncrease = 0, bool expBuff = false, int showdown = 0)
	{
		if (player == null || totalHp <= 0)
		{
			return default;
		}
		BotPartyService.Party party = parties.For(player.id);
		long value = 0L;
		long num = 0L;
		int num2 = 0;
		if (party == null)
		{
			damage.TryGetValue(player.id, out value);
			double num3 = (double)value * ((double)mobExp / (double)totalHp);
			num3 *= (double)StatusMultiplier(holySymbol, sharers: false, fullHolySymbol, showdown);
			num3 *= (double)personalRate;
			num3 += (double)expIncrease;
			if (expBuff)
			{
				num3 *= 2.0;
			}
			return new Award(Rounded(num3), 0L, value > 0);
		}
		foreach (KeyValuePair<int, long> item in damage)
		{
			BotActor botActor = parties.Get(item.Key);
			if (botActor != null && botActor.online && botActor.world == player.world && botActor.channel == player.channel && botActor.mapId == player.mapId && parties.For(botActor.id) == party)
			{
				value += item.Value;
				if (item.Value > num)
				{
					num = item.Value;
					num2 = item.Key;
				}
			}
		}
		if (value <= 0)
		{
			return default;
		}
		int num4 = 0;
		int num5 = 0;
		bool flag = false;
		int num6 = mobLevel - GameConfigDao.GetInt(db, "exp_split_level_interval", 5);
		foreach (BotActor item2 in parties.Members(player.id))
		{
			if (item2.online && item2.world == player.world && item2.channel == player.channel && item2.mapId == player.mapId && (item2.level >= num6 || (damage.TryGetValue(item2.id, out var value2) && value2 > 0)))
			{
				num4 += item2.level;
				num5++;
				if (item2.id == player.id)
				{
					flag = true;
				}
			}
		}
		if (!flag || num4 <= 0)
		{
			return default;
		}
		double num7 = GameConfigDao.GetFloat(db, "exp_split_common_mod", 0.8) * (double)player.level / (double)num4;
		if (num2 == player.id)
		{
			num7 += GameConfigDao.GetFloat(db, "exp_split_mvp_mod", 0.2);
		}
		double num8 = num7 * ((double)value * ((double)mobExp / (double)totalHp));
		double num9 = ((num5 > 1) ? (0.05 * (double)num5 * num8) : 0.0);
		float num10 = StatusMultiplier(holySymbol, num5 > 1, fullHolySymbol, showdown);
		num8 *= (double)num10;
		num8 *= (double)personalRate;
		num8 += (double)expIncrease;
		if (expBuff)
		{
			num8 *= 2.0;
		}
		num9 *= (double)num10;
		num9 *= (double)partyRate;
		num9 *= GameConfigDao.GetFloat(db, "party_bonus_exp_rate", 1.0);
		return new Award(Rounded(num8), Rounded(num9), eligible: true, 2);
	}

	public static float StatusMultiplier(int percent, bool sharers, bool full, int showdown)
	{
		return (float)((double)Symbol(percent, sharers, full) * (1.0 + (double)showdown / 100.0));
	}

	private static float Symbol(int percent, bool sharers, bool full)
	{
		return (float)(1.0 + (double)percent / ((sharers | full) ? 100.0 : 500.0));
	}
}
