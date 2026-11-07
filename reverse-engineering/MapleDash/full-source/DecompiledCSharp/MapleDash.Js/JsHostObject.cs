using System;

namespace MapleDash.Js;

public sealed class JsHostObject : JsValue
{
	public readonly object Host;

	public readonly JsObject Members = new JsObject();

	public Func<string, JsValue> Missing;

	public JsHostObject(object host)
	{
		Host = host;
	}

	public JsValue Get(string key)
	{
		if (Members.Has(key))
		{
			return Members.Get(key);
		}
		if (Missing == null)
		{
			return JsUndefined.Value;
		}
		return Missing(key);
	}

	public void Set(string key, JsValue v)
	{
		Members.Set(key, v);
	}

	public override string ToString()
	{
		return "[host " + ((Host == null) ? "null" : Host.GetType().Name) + "]";
	}
}
