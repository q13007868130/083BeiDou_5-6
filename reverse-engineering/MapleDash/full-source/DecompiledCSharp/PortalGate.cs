using System;
using System.Collections.Generic;

public static class PortalGate
{
	private static readonly Dictionary<string, string> gates = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		["lpq0"] = "1stageclear",
		["lpq1"] = "2stageclear",
		["lpq2"] = "3stageclear",
		["lpq3"] = "4stageclear",
		["lpq4"] = "5stageclear",
		["lpq5"] = "5stageclear",
		["lpq6"] = "7stageclear",
		["lpq7"] = "8stageclear",
		["kpq0"] = "1stageclear",
		["kpq1"] = "2stageclear",
		["kpq2"] = "3stageclear",
		["kpq3"] = "4stageclear",
		["kpq4"] = "5stageclear"
	};

	public static bool Open(string script)
	{
		string text = Required(script);
		if (text != null)
		{
			return NativeEventScripts.InstanceProperty(text) != null;
		}
		return true;
	}

	public static string Required(string script)
	{
		if (script == null || !gates.TryGetValue(script, out var value))
		{
			return null;
		}
		return value;
	}
}
