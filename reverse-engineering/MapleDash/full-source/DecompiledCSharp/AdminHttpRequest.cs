using System;
using System.Collections.Generic;
using System.Globalization;

public sealed class AdminHttpRequest
{
	public string Method = "GET";

	public string RawPath = "/";

	public string Path = "/";

	public string RawQuery = "";

	public readonly Dictionary<string, string> Query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public readonly Dictionary<string, string> Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public string BodyText = "";

	public byte[] BodyBytes = Array.Empty<byte>();

	public string Header(string name)
	{
		if (!Headers.TryGetValue(name, out var value))
		{
			return null;
		}
		return value;
	}

	public string QueryValue(string name, string fallback = null)
	{
		if (!Query.TryGetValue(name, out var value))
		{
			return fallback;
		}
		return value;
	}

	public int QueryInt(string name, int fallback = 0)
	{
		if (!Query.TryGetValue(name, out var value) || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return fallback;
		}
		return result;
	}
}
