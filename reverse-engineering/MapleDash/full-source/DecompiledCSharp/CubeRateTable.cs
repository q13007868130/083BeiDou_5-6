using System;
using System.Collections.Generic;
using System.Globalization;

public sealed class CubeRateTable
{
	public sealed class Weighted
	{
		private readonly int[] keys;

		private readonly int[] weights;

		public int Total { get; }

		public int Count => keys.Length;

		public Weighted(string spec)
		{
			List<int> list = new List<int>();
			List<int> list2 = new List<int>();
			int num = 0;
			string[] array = spec.Split(',');
			foreach (string text in array)
			{
				if (!string.IsNullOrWhiteSpace(text))
				{
					string[] array2 = text.Split(':');
					if (array2.Length != 2)
					{
						throw new ArgumentException("Invalid option weight");
					}
					int item = int.Parse(array2[0], CultureInfo.InvariantCulture);
					int num2 = int.Parse(array2[1], CultureInfo.InvariantCulture);
					if (num2 < 0)
					{
						throw new ArgumentException("Negative option weight");
					}
					if (num2 != 0)
					{
						num = checked(num + num2);
						list.Add(item);
						list2.Add(num2);
					}
				}
			}
			if (num <= 0)
			{
				throw new ArgumentException("Empty option weights");
			}
			keys = list.ToArray();
			weights = list2.ToArray();
			Total = num;
		}

		public int Pick(int draw)
		{
			if (draw < 0 || draw >= Total)
			{
				throw new ArgumentOutOfRangeException("draw");
			}
			for (int i = 0; i < keys.Length; i++)
			{
				if (draw < weights[i])
				{
					return keys[i];
				}
				draw -= weights[i];
			}
			throw new InvalidOperationException();
		}

		public int WeightOf(int id)
		{
			for (int i = 0; i < keys.Length; i++)
			{
				if (keys[i] == id)
				{
					return weights[i];
				}
			}
			return 0;
		}

		internal int Overlap(HashSet<int> legal)
		{
			int num = 0;
			int[] array = keys;
			foreach (int item in array)
			{
				if (legal.Contains(item))
				{
					num++;
				}
			}
			return num;
		}

		internal Weighted Restrict(HashSet<int> legal)
		{
			List<string> list = new List<string>();
			int[] array = keys;
			for (int i = 0; i < array.Length; i++)
			{
				int num = array[i];
				if (legal.Contains(num))
				{
					list.Add(num + ":" + WeightOf(num));
				}
			}
			if (list.Count != 0)
			{
				return new Weighted(string.Join(",", list));
			}
			return null;
		}
	}

	private readonly Dictionary<string, int[]> builtins = new Dictionary<string, int[]>();

	private readonly Dictionary<string, HashSet<int>> families = new Dictionary<string, HashSet<int>>();

	private readonly Dictionary<long, int> tier = new Dictionary<long, int>();

	private readonly Dictionary<long, int> prime = new Dictionary<long, int>();

	private readonly HashSet<int> tierCubes = new HashSet<int>();

	private readonly HashSet<int> primeCubes = new HashSet<int>();

	private readonly Dictionary<long, List<Weighted>> categories = new Dictionary<long, List<Weighted>>();

	private readonly Dictionary<long, Weighted> overrides = new Dictionary<long, Weighted>();

	public bool InFamily(string name, int id)
	{
		if (families.TryGetValue(name, out var value))
		{
			return value.Contains(id);
		}
		return false;
	}

	private static long Key(int cube, int grade)
	{
		return ((long)cube << 8) | (uint)grade;
	}

	private static int Number(Dictionary<string, object> row, string field)
	{
		return int.Parse((string)row[field], CultureInfo.InvariantCulture);
	}

	public CubeRateTable(string json)
	{
		if (!(MiniJsonDb.Parse(json) is Dictionary<string, object> dictionary) || Convert.ToInt32(dictionary["schemaVersion"]) != 1 || Convert.ToInt32(dictionary["tierUpUnit"]) != 1000)
		{
			throw new ArgumentException("Invalid cube rate data");
		}
		foreach (KeyValuePair<string, object> item in (Dictionary<string, object>)dictionary["builtinTierUpRates"])
		{
			List<object> list = (List<object>)item.Value;
			if (list.Count != 3)
			{
				throw new ArgumentException("Invalid tier-up array");
			}
			builtins.Add(item.Key, new int[3]
			{
				Convert.ToInt32(list[0]),
				Convert.ToInt32(list[1]),
				Convert.ToInt32(list[2])
			});
		}
		Dictionary<string, object> tables = (Dictionary<string, object>)dictionary["tables"];
		foreach (KeyValuePair<string, object> item2 in (Dictionary<string, object>)dictionary["optionFamilies"])
		{
			HashSet<int> hashSet = new HashSet<int>();
			foreach (object item3 in (List<object>)item2.Value)
			{
				hashSet.Add(Convert.ToInt32(item3));
			}
			families.Add(item2.Key, hashSet);
		}
		if (Setting("tier_up"))
		{
			foreach (Dictionary<string, object> item4 in Rows("cube-grade"))
			{
				int num = Number(item4, "cube_id");
				int grade = Number(item4, "grade");
				tierCubes.Add(num);
				tier[Key(num, grade)] = Number(item4, "tier_up");
			}
		}
		if (Setting("option_level"))
		{
			foreach (Dictionary<string, object> item5 in Rows("cube-option-level"))
			{
				int num2 = Number(item5, "cube_id");
				int grade2 = Number(item5, "grade");
				int num3 = Number(item5, "line");
				primeCubes.Add(num2);
				prime[(Key(num2, grade2) << 2) | (uint)num3] = Number(item5, "same_grade");
			}
		}
		foreach (Dictionary<string, object> item6 in Rows("cube-options"))
		{
			long key = Key(Number(item6, "cube_id"), Number(item6, "grade"));
			if (!categories.TryGetValue(key, out var value))
			{
				categories.Add(key, value = new List<Weighted>());
			}
			value.Add(new Weighted((string)item6["weights"]));
		}
		foreach (Dictionary<string, object> item7 in Rows("cube-option-override"))
		{
			overrides[Key(Number(item7, "cube_id"), Number(item7, "grade"))] = new Weighted((string)item7["weights"]);
		}
		List<object> Rows(string name)
		{
			return (List<object>)tables[name];
		}
		bool Setting(string name)
		{
			foreach (Dictionary<string, object> item8 in Rows("cube-settings"))
			{
				if ((string)item8["key"] == name)
				{
					return bool.Parse((string)item8["value"]);
				}
			}
			return false;
		}
	}

	public int BuiltinTierUp(string policy, int grade)
	{
		if (grade < 1 || grade > 3)
		{
			throw new ArgumentOutOfRangeException("grade");
		}
		return builtins[policy][grade - 1];
	}

	public int TierUpOverride(int cube, int grade)
	{
		if (grade >= 1 && grade <= 3 && tierCubes.Contains(cube))
		{
			if (!tier.TryGetValue(Key(cube, grade), out var value))
			{
				return 0;
			}
			return value;
		}
		return -1;
	}

	public int PrimeOverride(int cube, int grade, int line)
	{
		if (grade >= 1 && grade <= 4 && line >= 1 && line <= 3 && primeCubes.Contains(cube))
		{
			if (!prime.TryGetValue((Key(cube, grade) << 2) | (uint)line, out var value))
			{
				return 10000;
			}
			return value;
		}
		return -1;
	}

	public Weighted OptionWeights(int cube, int grade, IEnumerable<int> legalOptions)
	{
		HashSet<int> hashSet = new HashSet<int>(legalOptions);
		if (hashSet.Count == 0)
		{
			return null;
		}
		long key = Key(cube, grade);
		if (overrides.TryGetValue(key, out var value))
		{
			Weighted weighted = value.Restrict(hashSet);
			if (weighted != null)
			{
				return weighted;
			}
		}
		if (!categories.TryGetValue(key, out var value2))
		{
			return null;
		}
		Weighted weighted2 = null;
		int num = 0;
		int num2 = int.MaxValue;
		foreach (Weighted item in value2)
		{
			int num3 = item.Overlap(hashSet);
			int num4 = item.Count - num3;
			if (num3 > num || (num3 == num && num4 < num2))
			{
				weighted2 = item;
				num = num3;
				num2 = num4;
			}
		}
		if (weighted2 != null && num * 2 >= weighted2.Count)
		{
			return weighted2.Restrict(hashSet);
		}
		return null;
	}
}
