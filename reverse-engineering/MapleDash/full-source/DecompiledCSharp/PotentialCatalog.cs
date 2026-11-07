using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

public sealed class PotentialCatalog
{
	private readonly Dictionary<int, Dictionary<string, object>> entries = new Dictionary<int, Dictionary<string, object>>();

	private static PotentialCatalog shared;

	public static PotentialCatalog Shared => shared ?? (shared = new PotentialCatalog(PlatformRes.LoadText("Enhancement/ItemOption")));

	public string SourceSha256 { get; }

	public int Count => entries.Count;

	public IEnumerable<int> Ids => entries.Keys;

	public PotentialCatalog(string json)
	{
		if (!(MiniJsonDb.Parse(json ?? "") is Dictionary<string, object> dictionary) || Convert.ToInt32(dictionary["schemaVersion"]) != 1 || !(dictionary["options"] is Dictionary<string, object> dictionary2))
		{
			throw new InvalidOperationException("282 潜能数据缺失或版本错误");
		}
		SourceSha256 = (string)dictionary["sourceSha256"];
		foreach (KeyValuePair<string, object> item in dictionary2)
		{
			int num = int.Parse(item.Key, CultureInfo.InvariantCulture);
			Dictionary<string, object> dictionary3 = item.Value as Dictionary<string, object>;
			if (num <= 0 || dictionary3 == null || !(dictionary3["info"] is Dictionary<string, object>) || !(dictionary3["level"] is Dictionary<string, object>))
			{
				throw new InvalidOperationException("无效潜能条目：" + item.Key);
			}
			entries.Add(num, dictionary3);
		}
	}

	public Dictionary<string, object> Info(int id)
	{
		if (!entries.TryGetValue(id, out var value))
		{
			return null;
		}
		return new Dictionary<string, object>((Dictionary<string, object>)value["info"]);
	}

	public static int OptionLevel(int rawLevel)
	{
		if (rawLevel < 0)
		{
			throw new ArgumentOutOfRangeException("rawLevel");
		}
		if (rawLevel != 0)
		{
			return rawLevel / 10 + ((rawLevel % 10 > 1) ? 1 : 0);
		}
		return 1;
	}

	public Dictionary<string, object> ValuesAt(int id, int optionLevel)
	{
		if (!entries.TryGetValue(id, out var value))
		{
			return null;
		}
		if (!((Dictionary<string, object>)value["level"]).TryGetValue(optionLevel.ToString(CultureInfo.InvariantCulture), out var value2))
		{
			return null;
		}
		return new Dictionary<string, object>((Dictionary<string, object>)value2);
	}

	public string Describe(int id, int optionLevel)
	{
		Dictionary<string, object> dictionary = Info(id);
		Dictionary<string, object> values = ValuesAt(id, optionLevel);
		if (dictionary == null || values == null)
		{
			throw new ArgumentException("潜能或等级档不存在");
		}
		object value;
		return Regex.Replace((dictionary.TryGetValue("string", out value) ? (value as string) : null) ?? throw new InvalidOperationException("潜能缺少文本模板"), "#([A-Za-z][A-Za-z0-9]*)", (Match match) => (!values.TryGetValue(match.Groups[1].Value, out var value2)) ? match.Value : Convert.ToString(value2, CultureInfo.InvariantCulture));
	}
}
