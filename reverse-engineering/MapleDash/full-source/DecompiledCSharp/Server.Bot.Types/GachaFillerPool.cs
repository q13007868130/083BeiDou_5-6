using System;
using System.Collections.Generic;
using System.Globalization;

namespace Server.Bot.Types;

internal sealed class GachaFillerPool
{
	public const string ResourcePath = "soloMapling/itemPool/itemConfig/gachaFiller.yaml";

	private static readonly KeyValuePair<string, double>[] TypeWeights = new KeyValuePair<string, double>[4]
	{
		new KeyValuePair<string, double>("useables", 0.5),
		new KeyValuePair<string, double>("ores", 0.2),
		new KeyValuePair<string, double>("jewels", 0.2),
		new KeyValuePair<string, double>("crystals", 0.1)
	};

	private static readonly KeyValuePair<string, double>[] TierWeights = new KeyValuePair<string, double>[3]
	{
		new KeyValuePair<string, double>("s_tier", 0.2),
		new KeyValuePair<string, double>("a_tier", 0.3),
		new KeyValuePair<string, double>("b_tier", 0.5)
	};

	private readonly Dictionary<string, Dictionary<string, List<int>>> data = new Dictionary<string, Dictionary<string, List<int>>>();

	private static GachaFillerPool shared;

	public int Count { get; private set; }

	public static GachaFillerPool Shared => shared ?? (shared = Load());

	private static GachaFillerPool Load()
	{
		GachaFillerPool gachaFillerPool = new GachaFillerPool();
		try
		{
			gachaFillerPool.Parse(BotResources.Load().ReadText("soloMapling/itemPool/itemConfig/gachaFiller.yaml"));
		}
		catch (Exception)
		{
		}
		return gachaFillerPool;
	}

	private void Parse(string yaml)
	{
		if (string.IsNullOrEmpty(yaml))
		{
			return;
		}
		string text = null;
		string[] array = yaml.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i].Trim();
			if (text2.Length == 0 || text2.StartsWith("#", StringComparison.Ordinal))
			{
				continue;
			}
			int num = text2.IndexOf(':');
			if (num < 0)
			{
				continue;
			}
			string text3 = text2.Substring(0, num).Trim();
			string text4 = text2.Substring(num + 1).Trim();
			if (text4.Length == 0)
			{
				text = text3;
				data[text3] = new Dictionary<string, List<int>>();
			}
			else
			{
				if (text == null)
				{
					continue;
				}
				List<int> list = new List<int>();
				string[] array2 = text4.Trim('[', ']').Split(',');
				for (int j = 0; j < array2.Length; j++)
				{
					if (int.TryParse(array2[j].Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result))
					{
						list.Add(result);
					}
				}
				data[text][text3] = list;
				Count += list.Count;
			}
		}
	}

	public List<int> CreateGachaListWithPrize(int prizeId, Random random)
	{
		List<int> list = CreateGachaFillerList(random);
		int num = list.Count / 4;
		int index = num + random.Next(list.Count - num + 1);
		list.Insert(index, prizeId);
		return list;
	}

	public List<int> CreateGachaFillerList(Random random)
	{
		List<int> list = new List<int>(10);
		for (int i = 0; i < 10; i++)
		{
			list.Add(0);
		}
		for (int j = 0; j < list.Count; j++)
		{
			if (!(random.NextDouble() >= 0.3))
			{
				list[j] = RandomItem(RollWeighted(TypeWeights, random), RollWeighted(TierWeights, random), random);
			}
		}
		return list;
	}

	public int RandomItem(string type, string tier, Random random)
	{
		if (!data.TryGetValue(type, out var value))
		{
			return 0;
		}
		if (!value.TryGetValue(tier, out var value2) || value2.Count == 0)
		{
			return 0;
		}
		return value2[random.Next(value2.Count)];
	}

	private static string RollWeighted(KeyValuePair<string, double>[] weights, Random random)
	{
		double num = random.NextDouble();
		double num2 = 0.0;
		for (int i = 0; i < weights.Length; i++)
		{
			KeyValuePair<string, double> keyValuePair = weights[i];
			num2 += keyValuePair.Value;
			if (num < num2)
			{
				return keyValuePair.Key;
			}
		}
		return weights[^1].Key;
	}
}
