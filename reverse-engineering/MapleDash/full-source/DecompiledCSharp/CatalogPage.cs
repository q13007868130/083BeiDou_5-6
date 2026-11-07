using System.Collections.Generic;

public sealed class CatalogPage
{
	public readonly List<object> Records;

	public readonly int PageNo;

	public readonly int PageSize;

	public readonly long Total;

	private readonly List<KeyValuePair<string, object>> extras = new List<KeyValuePair<string, object>>();

	public CatalogPage(List<object> records, int pageNo, int pageSize, long total)
	{
		Records = records ?? new List<object>();
		PageNo = pageNo;
		PageSize = pageSize;
		Total = total;
	}

	public CatalogPage Extra(string key, object value)
	{
		extras.Add(new KeyValuePair<string, object>(key, value));
		return this;
	}

	public Dictionary<string, object> ToDict()
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>
		{
			{ "records", Records },
			{ "pageNo", PageNo },
			{ "pageSize", PageSize },
			{ "total", Total }
		};
		foreach (KeyValuePair<string, object> extra in extras)
		{
			dictionary[extra.Key] = extra.Value;
		}
		return dictionary;
	}
}
