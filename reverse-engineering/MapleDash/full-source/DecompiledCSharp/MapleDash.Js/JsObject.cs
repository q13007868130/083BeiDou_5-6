using System;
using System.Collections.Generic;

namespace MapleDash.Js;

public sealed class JsObject : JsValue
{
	public readonly Dictionary<string, JsValue> Props = new Dictionary<string, JsValue>(StringComparer.Ordinal);

	public bool Frozen { get; private set; }

	public JsObject()
	{
	}

	public JsObject(IDictionary<string, JsValue> init)
	{
		if (init == null)
		{
			return;
		}
		foreach (KeyValuePair<string, JsValue> item in init)
		{
			Props[item.Key] = item.Value ?? JsNull.Value;
		}
	}

	public void Freeze()
	{
		Frozen = true;
	}

	public JsValue Get(string key)
	{
		if (!Props.TryGetValue(key, out var value))
		{
			return JsUndefined.Value;
		}
		return value;
	}

	public void Set(string key, JsValue v)
	{
		if (!Frozen)
		{
			Props[key] = v ?? JsNull.Value;
		}
	}

	public bool Has(string key)
	{
		return Props.ContainsKey(key);
	}

	public void Remove(string key)
	{
		if (!Frozen)
		{
			Props.Remove(key);
		}
	}

	public override string ToString()
	{
		return "[object Object]";
	}
}
