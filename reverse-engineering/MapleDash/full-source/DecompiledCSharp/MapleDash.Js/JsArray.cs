using System;
using System.Collections.Generic;

namespace MapleDash.Js;

public sealed class JsArray : JsValue
{
	public readonly List<JsValue> Items = new List<JsValue>();

	public readonly Dictionary<string, JsValue> Props = new Dictionary<string, JsValue>(StringComparer.Ordinal);

	public int Length => Items.Count;

	public JsArray()
	{
	}

	public JsArray(IEnumerable<JsValue> items)
	{
		if (items != null)
		{
			Items.AddRange(items);
		}
	}

	public JsValue GetIndex(int i)
	{
		if (i < 0 || i >= Items.Count)
		{
			return JsUndefined.Value;
		}
		return Items[i];
	}

	public void SetIndex(int i, JsValue v)
	{
		v = v ?? JsNull.Value;
		if (i >= 0)
		{
			while (Items.Count <= i)
			{
				Items.Add(JsUndefined.Value);
			}
			Items[i] = v;
		}
	}

	public JsValue Get(string key)
	{
		if (key == "length")
		{
			return new JsNumber(Items.Count);
		}
		if (int.TryParse(key, out var result))
		{
			return GetIndex(result);
		}
		if (!Props.TryGetValue(key, out var value))
		{
			return JsUndefined.Value;
		}
		return value;
	}

	public void Set(string key, JsValue v)
	{
		int result;
		if (key == "length")
		{
			int num = (int)JsConvert.ToNumber(v);
			if (num < 0)
			{
				num = 0;
			}
			while (Items.Count < num)
			{
				Items.Add(JsUndefined.Value);
			}
			while (Items.Count > num)
			{
				Items.RemoveAt(Items.Count - 1);
			}
		}
		else if (int.TryParse(key, out result))
		{
			SetIndex(result, v);
		}
		else
		{
			Props[key] = v ?? JsNull.Value;
		}
	}

	public override string ToString()
	{
		return "[object Array]";
	}
}
