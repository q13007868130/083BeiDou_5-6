using System;
using System.Collections.Generic;

public sealed class PotentialRoller
{
	public sealed class Result
	{
		public int grade;

		public int[] options;
	}

	private readonly PotentialPool pool;

	private readonly CubeRateTable rates;

	private static readonly string[] Singles = new string[5] { "damagePercent", "criticalDamagePercent", "maxHPPercent", "mesoObtained", "itemDropRate" };

	private static readonly string[] Stats = new string[4] { "strPercent", "dexPercent", "lukPercent", "intPercent" };

	public PotentialRoller(PotentialCatalog catalog, CubeRateTable rates)
	{
		pool = new PotentialPool(catalog);
		this.rates = rates ?? throw new ArgumentNullException("rates");
	}

	public Result Roll(int cubeId, int itemId, int equipLevel, int grade, int[] main, int[] bonus, Random random)
	{
		CubeRules.Family family = CubeRules.GetFamily(cubeId);
		if (family == null || grade < family.minimum || grade > family.maximum || main == null || main.Length != 3 || bonus == null || bonus.Length != 3 || random == null)
		{
			throw new ArgumentException("Invalid cube roll inputs");
		}
		int num = rates.TierUpOverride(cubeId, grade);
		int num2 = ((grade < 4 && ((num >= 0) ? (random.Next(10000) < num) : (random.Next(1000) < rates.BuiltinTierUp(family.policy, grade))) && grade < family.maximum) ? (grade + 1) : grade);
		int[] array = (family.bonus ? bonus : main);
		int num3;
		if (family.bonus)
		{
			num3 = ((array[2] != 0) ? 3 : ((array[1] == 0) ? 1 : 2));
		}
		else
		{
			num3 = ((array[2] != 0) ? 3 : 2);
		}
		bool flag = family.policy == "BONUS_BRIGHT_CUBE_TIER_UP_RATES";
		int extra = 0;
		if (random.Next(1000) < (flag ? 50 : 10))
		{
			extra = 1;
			if (random.Next(1000) < (flag ? 10 : 0))
			{
				extra = 2;
			}
		}
		int[] array2 = new int[3];
		for (int i = 0; i < num3; i++)
		{
			array2[i] = Pick(cubeId, itemId, equipLevel, num2, family.bonus, family.policy == "SOLID_CUBE_TIER_UP_RATES" || family.policy == "HARD_CUBES_TIER_UP_RATES", i, extra, family.bonus ? main[0] : array2[0], random);
		}
		return new Result
		{
			grade = num2,
			options = array2
		};
	}

	public Result Reveal(int itemId, int equipLevel, int grade, bool bonus, int[] main, int[] additional, Random random)
	{
		if (grade < 1 || grade > 4 || main == null || main.Length != 3 || additional == null || additional.Length != 3 || random == null)
		{
			throw new ArgumentException("Invalid potential reveal inputs");
		}
		int[] array = (int[])(bonus ? additional : main).Clone();
		int extra = ((random.Next(1000) < 10) ? 1 : 0);
		for (int i = 0; i < 3; i++)
		{
			if (array[i] < 0)
			{
				array[i] = Pick(-1, itemId, equipLevel, grade, bonus, steal: true, i, extra, bonus ? main[0] : array[0], random);
			}
		}
		return new Result
		{
			grade = grade,
			options = array
		};
	}

	private int Pick(int cubeId, int itemId, int equipLevel, int rolledGrade, bool bonus, bool steal, int line, int extra, int first, Random random)
	{
		int num = extra;
		int num2 = rates.PrimeOverride(cubeId, rolledGrade, line + 1);
		if (line > 0 && num2 >= 0)
		{
			num = ((random.Next(10000) < num2) ? line : 0);
		}
		int grade = ((line == 0 || num >= line) ? rolledGrade : (rolledGrade - 1));
		List<int> list = pool.Legal(itemId, equipLevel, grade, bonus, steal);
		list.RemoveAll((int id) => pool.WeightOf(id) <= 0);
		CubeRateTable.Weighted weighted = rates.OptionWeights(cubeId, grade, list);
		if (weighted != null)
		{
			list.RemoveAll((int id) => weighted.WeightOf(id) == 0);
		}
		if (line > 0 && HasExclusion(first) && random.Next(100) < 10)
		{
			list.RemoveAll((int id) => Excludes(first, id));
		}
		if (list.Count == 0)
		{
			throw new InvalidOperationException("No supported potential outcome for equipment");
		}
		List<string> list2 = new List<string>();
		foreach (int item in list)
		{
			list2.Add(item + ":" + ((weighted == null) ? pool.WeightOf(item) : weighted.WeightOf(item)));
		}
		CubeRateTable.Weighted weighted2 = new CubeRateTable.Weighted(string.Join(",", list2));
		return weighted2.Pick(random.Next(weighted2.Total));
	}

	private bool HasExclusion(int first)
	{
		string[] singles = Singles;
		foreach (string name in singles)
		{
			if (rates.InFamily(name, first))
			{
				return true;
			}
		}
		singles = Stats;
		foreach (string name2 in singles)
		{
			if (rates.InFamily(name2, first))
			{
				return true;
			}
		}
		if (!rates.InFamily("bossDamagePercent", first) && !rates.InFamily("attPercent", first) && !rates.InFamily("magicAttPercent", first) && !rates.InFamily("ignoreMonsterDEFPercent", first))
		{
			return rates.InFamily("allStatsPercent", first);
		}
		return true;
	}

	private bool Excludes(int first, int next)
	{
		if (Is("bossDamagePercent", first))
		{
			if (!Is("bossDamagePercent", next) && !Is("attPercent", next))
			{
				return Is("magicAttPercent", next);
			}
			return true;
		}
		if (Is("damagePercent", first))
		{
			return Is("damagePercent", next);
		}
		if (Is("attPercent", first))
		{
			if (!Is("attPercent", next))
			{
				return Is("bossDamagePercent", next);
			}
			return true;
		}
		if (Is("magicAttPercent", first))
		{
			if (!Is("magicAttPercent", next))
			{
				return Is("bossDamagePercent", next);
			}
			return true;
		}
		if (Is("ignoreMonsterDEFPercent", first))
		{
			if (!Is("ignoreMonsterDEFPercent", next))
			{
				return Is("bossDamagePercent", next);
			}
			return true;
		}
		string[] stats;
		if (Is("allStatsPercent", first))
		{
			stats = Stats;
			foreach (string name in stats)
			{
				if (Is(name, next))
				{
					return true;
				}
			}
			return false;
		}
		stats = Stats;
		foreach (string name2 in stats)
		{
			if (Is(name2, first))
			{
				if (!Is(name2, next))
				{
					return Is("allStatsPercent", next);
				}
				return true;
			}
		}
		stats = Singles;
		foreach (string name3 in stats)
		{
			if (Is(name3, first))
			{
				return Is(name3, next);
			}
		}
		return false;
		bool Is(string name4, int id)
		{
			return rates.InFamily(name4, id);
		}
	}
}
