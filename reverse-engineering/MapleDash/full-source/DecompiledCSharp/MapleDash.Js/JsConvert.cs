using System;
using System.Collections.Generic;
using System.Globalization;

namespace MapleDash.Js;

public static class JsConvert
{
	public static bool ToBoolean(JsValue v)
	{
		if (v == null || v is JsUndefined || v is JsNull)
		{
			return false;
		}
		if (v is JsBoolean jsBoolean)
		{
			return jsBoolean.Value;
		}
		if (v is JsNumber jsNumber)
		{
			if (jsNumber.Value != 0.0)
			{
				return !double.IsNaN(jsNumber.Value);
			}
			return false;
		}
		if (v is JsString jsString)
		{
			return jsString.Value.Length > 0;
		}
		return true;
	}

	public static double ToNumber(JsValue v)
	{
		if (v == null || v is JsUndefined)
		{
			return double.NaN;
		}
		if (v is JsNull)
		{
			return 0.0;
		}
		if (v is JsNumber jsNumber)
		{
			return jsNumber.Value;
		}
		if (v is JsDate jsDate)
		{
			return jsDate.Milliseconds;
		}
		if (v is JsBoolean jsBoolean)
		{
			return jsBoolean.Value ? 1 : 0;
		}
		if (v is JsString jsString)
		{
			return StringToNumber(jsString.Value);
		}
		return double.NaN;
	}

	public static double StringToNumber(string s)
	{
		if (s == null)
		{
			return 0.0;
		}
		s = s.Trim();
		if (s.Length == 0)
		{
			return 0.0;
		}
		if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && long.TryParse(s.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
		{
			return result2;
		}
		switch (s)
		{
		case "Infinity":
		case "+Infinity":
			return double.PositiveInfinity;
		case "-Infinity":
			return double.NegativeInfinity;
		default:
			return double.NaN;
		}
	}

	public static string ToString(JsValue v)
	{
		if (v == null || v is JsUndefined)
		{
			return "undefined";
		}
		if (v is JsNull)
		{
			return "null";
		}
		if (v is JsNumber jsNumber)
		{
			return NumberToString(jsNumber.Value);
		}
		if (v is JsString jsString)
		{
			return jsString.Value;
		}
		if (v is JsBoolean jsBoolean)
		{
			if (!jsBoolean.Value)
			{
				return "false";
			}
			return "true";
		}
		if (v is JsArray jsArray)
		{
			List<string> list = new List<string>(jsArray.Items.Count);
			for (int i = 0; i < jsArray.Items.Count; i++)
			{
				JsValue jsValue = jsArray.Items[i];
				list.Add((jsValue == null || jsValue is JsUndefined || jsValue is JsNull) ? "" : ToString(jsValue));
			}
			return string.Join(",", list);
		}
		if (v is JsFunction jsFunction)
		{
			return jsFunction.ToString();
		}
		return v.ToString();
	}

	public static string NumberToString(double d)
	{
		if (double.IsNaN(d))
		{
			return "NaN";
		}
		if (double.IsPositiveInfinity(d))
		{
			return "Infinity";
		}
		if (double.IsNegativeInfinity(d))
		{
			return "-Infinity";
		}
		if (d == 0.0)
		{
			return "0";
		}
		if (d == Math.Floor(d) && Math.Abs(d) < 1E+21 && Math.Abs(d) <= 9007199254740992.0)
		{
			return ((long)d).ToString(CultureInfo.InvariantCulture);
		}
		return d.ToString("R", CultureInfo.InvariantCulture);
	}

	public static int ToInt32(JsValue v)
	{
		double d = ToNumber(v);
		if (double.IsNaN(d) || double.IsInfinity(d))
		{
			return 0;
		}
		d = Math.Truncate(d) % 4294967296.0;
		if (d < 0.0)
		{
			d += 4294967296.0;
		}
		return (int)(uint)d;
	}

	public static bool LooseEquals(JsValue a, JsValue b)
	{
		if (a == null)
		{
			a = JsUndefined.Value;
		}
		if (b == null)
		{
			b = JsUndefined.Value;
		}
		if (SameType(a, b))
		{
			return StrictEquals(a, b);
		}
		bool flag = a is JsUndefined || a is JsNull;
		bool flag2 = b is JsUndefined || b is JsNull;
		if (flag | flag2)
		{
			return flag & flag2;
		}
		if (a is JsBoolean)
		{
			return LooseEquals(new JsNumber(ToNumber(a)), b);
		}
		if (b is JsBoolean)
		{
			return LooseEquals(a, new JsNumber(ToNumber(b)));
		}
		if (a is JsNumber && b is JsString)
		{
			return ToNumber(a) == ToNumber(b);
		}
		if (a is JsString && b is JsNumber)
		{
			return ToNumber(a) == ToNumber(b);
		}
		return StrictEquals(a, b);
	}

	public static bool StrictEquals(JsValue a, JsValue b)
	{
		if (a == null)
		{
			a = JsUndefined.Value;
		}
		if (b == null)
		{
			b = JsUndefined.Value;
		}
		if (a is JsNumber jsNumber && b is JsNumber jsNumber2)
		{
			return jsNumber.Value == jsNumber2.Value;
		}
		if (a is JsString jsString && b is JsString jsString2)
		{
			return jsString.Value == jsString2.Value;
		}
		if (a is JsBoolean jsBoolean && b is JsBoolean jsBoolean2)
		{
			return jsBoolean.Value == jsBoolean2.Value;
		}
		return a == b;
	}

	private static bool SameType(JsValue a, JsValue b)
	{
		if (a is JsNumber && b is JsNumber)
		{
			return true;
		}
		if (a is JsString && b is JsString)
		{
			return true;
		}
		if (a is JsBoolean && b is JsBoolean)
		{
			return true;
		}
		if (a is JsUndefined && b is JsUndefined)
		{
			return true;
		}
		if (a is JsNull && b is JsNull)
		{
			return true;
		}
		return false;
	}
}
