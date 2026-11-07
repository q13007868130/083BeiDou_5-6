using System;
using System.Collections.Generic;

public sealed class AdminCtx
{
	public AdminApi Api;

	public AdminHttpRequest Req;

	public readonly Dictionary<string, string> Params = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public object RawBody;

	public object Body;

	public string Token;

	public AdminAccount User;

	public string Param(string key)
	{
		if (!Params.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public long ParamLong(string key)
	{
		if (!long.TryParse(Param(key), out var result))
		{
			return 0L;
		}
		return result;
	}

	public string Query(string key, string fallback = null)
	{
		return Req.QueryValue(key, fallback);
	}

	public int QueryInt(string key, int fallback = 0)
	{
		return Req.QueryInt(key, fallback);
	}

	public long QueryLong(string key, long fallback = 0L)
	{
		if (!long.TryParse(Req.QueryValue(key), out var result))
		{
			return fallback;
		}
		return result;
	}

	public string BodyString(string key, string fallback = "")
	{
		return AdminJson.GetString(Body, key, fallback);
	}

	public long BodyLong(string key, long fallback = 0L)
	{
		return AdminJson.GetLong(Body, key, fallback);
	}

	public int BodyInt(string key, int fallback = 0)
	{
		return AdminJson.GetInt(Body, key, fallback);
	}

	public bool BodyBool(string key, bool fallback = false)
	{
		return AdminJson.GetBool(Body, key, fallback);
	}
}
