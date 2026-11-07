using System;

public static class RewardRules
{
	public static int MesoAmount(int quantity, int? mesoUp, double rate)
	{
		if (quantity <= 0)
		{
			return 0;
		}
		if (mesoUp.HasValue)
		{
			quantity = (int)Math.Min(2147483647.0, (double)quantity * ((double)mesoUp.Value / 100.0));
		}
		float num = (float)quantity * (float)rate;
		int num2 = ((num >= 2.1474836E+09f) ? int.MaxValue : ((int)num));
		if (num2 > 0)
		{
			return num2;
		}
		return int.MaxValue;
	}

	public static float HolySymbolMultiplier(int percent, bool partySharers, bool full)
	{
		return (float)(1.0 + (double)percent / ((partySharers | full) ? 100.0 : 500.0));
	}

	public static int RollMeso(int chance, int minimum, int maximum, int chanceRoll, int quantityRoll)
	{
		if (chanceRoll < 0 || chanceRoll >= 999999 || chanceRoll >= chance || minimum < 0 || maximum <= minimum)
		{
			return 0;
		}
		if (quantityRoll < 0 || quantityRoll >= maximum - minimum)
		{
			return 0;
		}
		return minimum + quantityRoll;
	}
}
