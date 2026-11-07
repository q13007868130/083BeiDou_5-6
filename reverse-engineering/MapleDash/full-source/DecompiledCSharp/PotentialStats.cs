using System;
using System.Collections.Generic;

public static class PotentialStats
{
	private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>
	{
		{ "incSTR", "STR" },
		{ "incDEX", "DEX" },
		{ "incINT", "INT" },
		{ "incLUK", "LUK" },
		{ "incMHP", "HP" },
		{ "incMMP", "MP" },
		{ "incPAD", "PAD" },
		{ "incMAD", "MAD" },
		{ "incPDD", "PDD" },
		{ "incMDD", "MDD" },
		{ "incACC", "ACC" },
		{ "incEVA", "EVA" },
		{ "incSTRr", "STRPct" },
		{ "incDEXr", "DEXPct" },
		{ "incINTr", "INTPct" },
		{ "incLUKr", "LUKPct" },
		{ "incMHPr", "HPPct" },
		{ "incMMPr", "MPPct" },
		{ "incPADr", "PADPct" },
		{ "incMADr", "MADPct" },
		{ "incPDDr", "PDDPct" },
		{ "incMDDr", "MDDPct" },
		{ "incACCr", "ACCPct" },
		{ "incEVAr", "EVAPct" },
		{ "incCr", "CriticalChance" },
		{ "incCriticaldamage", "CriticalDamage" },
		{ "incSpeed", "Speed" },
		{ "incJump", "Jump" },
		{ "incEXPr", "ExpRate" },
		{ "incMesoProp", "MesoRate" },
		{ "incRewardProp", "DropRate" },
		{ "ignoreTargetDEF", "IgnoreDefense" }
	};

	private static readonly Dictionary<string, string> LevelKeys = new Dictionary<string, string>
	{
		{ "incSTRlv", "STR" },
		{ "incDEXlv", "DEX" },
		{ "incINTlv", "INT" },
		{ "incLUKlv", "LUK" },
		{ "incMHPlv", "HP" }
	};

	public static Dictionary<string, int> Calculate(PotentialCatalog catalog, int equipLevel, IEnumerable<int> options, int characterLevel = 0)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		int optionLevel = PotentialCatalog.OptionLevel(equipLevel);
		checked
		{
			foreach (int option in options)
			{
				if (option <= 0)
				{
					continue;
				}
				Dictionary<string, object> dictionary2 = catalog.ValuesAt(option, optionLevel);
				if (dictionary2 == null)
				{
					throw new InvalidOperationException("潜能等级档不存在：" + option + "/" + optionLevel);
				}
				foreach (KeyValuePair<string, int> item in CalculateValues(dictionary2))
				{
					Add(dictionary, item.Key, item.Value);
				}
				foreach (KeyValuePair<string, string> levelKey in LevelKeys)
				{
					if (dictionary2.TryGetValue(levelKey.Key, out var value))
					{
						Add(dictionary, levelKey.Value, Convert.ToInt32(value) * unchecked(Math.Max(0, characterLevel) / 9));
					}
				}
			}
			return dictionary;
		}
	}

	public static Dictionary<string, int> CalculateValues(Dictionary<string, object> values)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		if (values == null)
		{
			return dictionary;
		}
		foreach (KeyValuePair<string, string> key in Keys)
		{
			if (values.TryGetValue(key.Key, out var value))
			{
				Add(dictionary, key.Value, Convert.ToInt32(value));
			}
		}
		if (values.TryGetValue("incDAMr", out var value2))
		{
			Add(dictionary, (values.TryGetValue("boss", out var value3) && Convert.ToInt32(value3) != 0) ? "BossDamage" : "Damage", Convert.ToInt32(value2));
		}
		return dictionary;
	}

	public static void Add(Dictionary<string, int> values, string key, int amount)
	{
		values[key] = checked((values.TryGetValue(key, out var value) ? value : 0) + amount);
	}
}
