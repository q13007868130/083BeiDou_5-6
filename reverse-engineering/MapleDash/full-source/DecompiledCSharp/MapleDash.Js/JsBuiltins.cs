using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MapleDash.Js;

public static class JsBuiltins
{
	private static readonly string[] StringMethodNames = new string[22]
	{
		"charAt", "charCodeAt", "indexOf", "lastIndexOf", "substring", "substr", "slice", "split", "replace", "toLowerCase",
		"toUpperCase", "trim", "concat", "toString", "startsWith", "endsWith", "includes", "repeat", "valueOf", "match",
		"padEnd", "padStart"
	};

	private static readonly string[] ArrayMethodNames = new string[28]
	{
		"push", "pop", "shift", "unshift", "splice", "slice", "join", "indexOf", "lastIndexOf", "reverse",
		"sort", "concat", "forEach", "map", "filter", "toString", "size", "get", "isEmpty", "contains",
		"add", "toArray", "reduce", "some", "every", "find", "findIndex", "includes"
	};

	private static readonly string[] NumberMethodNames = new string[4] { "toString", "toFixed", "toLocaleString", "valueOf" };

	public static void Install(JsInterpreter it, JsScope scope)
	{
		scope.Declare("undefined", JsUndefined.Value);
		scope.Declare("NaN", new JsNumber(double.NaN));
		scope.Declare("Infinity", new JsNumber(double.PositiveInfinity));
		scope.Declare("parseInt", JsFunction.FromNative("parseInt", (JsCall c) => new JsNumber(ParseInt(JsConvert.ToString(c.Arg(0)), (c.ArgCount > 1) ? JsConvert.ToInt32(c.Arg(1)) : 0))));
		scope.Declare("parseFloat", JsFunction.FromNative("parseFloat", (JsCall c) => new JsNumber(ParseFloat(JsConvert.ToString(c.Arg(0))))));
		scope.Declare("isNaN", JsFunction.FromNative("isNaN", (JsCall c) => JsBoolean.Of(double.IsNaN(JsConvert.ToNumber(c.Arg(0))))));
		scope.Declare("isFinite", JsFunction.FromNative("isFinite", (JsCall c) =>
		{
			double d = JsConvert.ToNumber(c.Arg(0));
			return JsBoolean.Of(!double.IsNaN(d) && !double.IsInfinity(d));
		}));
		JsFunction jsFunction = JsFunction.FromNative("Number", (JsCall c) => new JsNumber(JsConvert.ToNumber(c.Arg(0))));
		jsFunction.Members.Set("isInteger", JsFunction.FromNative("isInteger", (JsCall c) => JsBoolean.Of(c.Arg(0) is JsNumber jsNumber && !double.IsInfinity(jsNumber.Value) && Math.Floor(jsNumber.Value) == jsNumber.Value)));
		scope.Declare("Number", jsFunction);
		JsFunction jsFunction2 = JsFunction.FromNative("String", (JsCall c) => new JsString(JsConvert.ToString(c.Arg(0))));
		jsFunction2.Members.Set("fromCharCode", JsFunction.FromNative("fromCharCode", (JsCall c) =>
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < c.ArgCount; i++)
			{
				stringBuilder.Append((char)JsConvert.ToInt32(c.Arg(i)));
			}
			return new JsString(stringBuilder.ToString());
		}));
		scope.Declare("String", jsFunction2);
		JsFunction value = JsFunction.FromNative("Boolean", (JsCall c) => JsBoolean.Of(JsConvert.ToBoolean(c.Arg(0))));
		scope.Declare("Boolean", value);
		scope.Declare("Date", JsDate.Constructor());
		scope.Declare("Math", BuildMath(it));
		scope.Declare("JSON", BuildJson(it));
		JsFunction jsFunction3 = JsFunction.FromNative("Object", (JsCall c) => (c.ArgCount != 0 && !(c.Arg(0) is JsNull) && !(c.Arg(0) is JsUndefined)) ? c.Arg(0) : new JsObject());
		string[] array = new string[3] { "keys", "values", "entries" };
		foreach (string method in array)
		{
			jsFunction3.Members.Set(method, JsFunction.FromNative(method, (JsCall c) =>
			{
				JsArray jsArray = new JsArray();
				foreach (string item2 in JsPatterns.Keys(c.Arg(0)))
				{
					JsValue member = it.GetMember(c.Arg(0), item2);
					List<JsValue> items = jsArray.Items;
					JsValue item = ((method == "keys") ? new JsString(item2) : ((method == "values") ? member : new JsArray(new JsValue[2]
					{
						new JsString(item2),
						member
					})));
					items.Add(item);
				}
				return jsArray;
			}));
		}
		jsFunction3.Members.Set("freeze", JsFunction.FromNative("freeze", (JsCall c) =>
		{
			JsObject obj = (c.Arg(0) as JsObject) ?? throw new JsRuntimeException("Object.freeze currently requires a plain object");
			obj.Freeze();
			return obj;
		}));
		jsFunction3.Members.Set("isFrozen", JsFunction.FromNative("isFrozen", (JsCall c) => JsBoolean.Of(c.Arg(0) is JsObject jsObject4 && jsObject4.Frozen)));
		scope.Declare("Object", jsFunction3);
		scope.Declare("Set", JsFunction.FromNative("Set", (JsCall c) =>
		{
			List<JsValue> values = new List<JsValue>();
			if (c.ArgCount > 0)
			{
				foreach (JsValue value2 in JsPatterns.Iterate(c.Arg(0)))
				{
					if (!values.Exists((JsValue v) => JsConvert.StrictEquals(v, value2)))
					{
						values.Add(value2);
					}
				}
			}
			JsHostObject set = new JsHostObject(values);
			set.Set("has", JsFunction.FromNative("has", (JsCall a) => JsBoolean.Of(values.Exists((JsValue v) => JsConvert.StrictEquals(v, a.Arg(0))))));
			set.Set("add", JsFunction.FromNative("add", (JsCall a) =>
			{
				if (!values.Exists((JsValue v) => JsConvert.StrictEquals(v, a.Arg(0))))
				{
					values.Add(a.Arg(0));
				}
				return set;
			}));
			return set;
		}));
		scope.Declare("Map", JsFunction.FromNative("Map", (JsCall c) =>
		{
			List<JsValue> keys = new List<JsValue>();
			List<JsValue> values = new List<JsValue>();
			JsHostObject map = new JsHostObject(keys);
			Func<JsValue, int> index = (JsValue key) => keys.FindIndex((JsValue v) => JsConvert.StrictEquals(v, key) || (v is JsNumber jsNumber && key is JsNumber jsNumber2 && double.IsNaN(jsNumber.Value) && double.IsNaN(jsNumber2.Value)));
			map.Set("has", JsFunction.FromNative("has", (JsCall a) => JsBoolean.Of(index(a.Arg(0)) >= 0)));
			map.Set("get", JsFunction.FromNative("get", (JsCall a) =>
			{
				int num2 = index(a.Arg(0));
				return (num2 >= 0) ? values[num2] : JsUndefined.Value;
			}));
			map.Set("set", JsFunction.FromNative("set", (JsCall a) =>
			{
				int num2 = index(a.Arg(0));
				if (num2 < 0)
				{
					keys.Add(a.Arg(0));
					values.Add(a.Arg(1));
				}
				else
				{
					values[num2] = a.Arg(1);
				}
				return map;
			}));
			map.Set("values", JsFunction.FromNative("values", (JsCall _) => new JsArray(values)));
			map.Set("keys", JsFunction.FromNative("keys", (JsCall _) => new JsArray(keys)));
			return map;
		}));
		JsFunction jsFunction4 = JsFunction.FromNative("Array", (JsCall c) =>
		{
			if (c.ArgCount == 1 && c.Arg(0) is JsNumber { Value: var value2 } && value2 >= 0.0 && value2 <= 2147483647.0 && Math.Floor(value2) == value2)
			{
				JsArray jsArray = new JsArray();
				for (int i = 0; i < (int)value2; i++)
				{
					jsArray.Items.Add(JsUndefined.Value);
				}
				return jsArray;
			}
			return new JsArray(c.Args);
		});
		jsFunction4.InstanceCheck = (JsValue jsValue) => jsValue is JsArray;
		jsFunction4.Members.Set("from", JsFunction.FromNative("from", (JsCall c) => new JsArray(JsPatterns.Iterate(c.Arg(0)))));
		JsObject jsObject = new JsObject();
		array = ArrayMethodNames;
		foreach (string text in array)
		{
			jsObject.Set(text, ArrayMethod(it, text));
		}
		jsFunction4.Members.Set("prototype", jsObject);
		jsFunction4.Members.Set("isArray", JsFunction.FromNative("isArray", (JsCall c) => JsBoolean.Of(c.Arg(0) is JsArray)));
		scope.Declare("Array", jsFunction4);
		JsObject jsObject2 = new JsObject();
		array = StringMethodNames;
		foreach (string text2 in array)
		{
			jsObject2.Set(text2, StringMethod(it, text2));
		}
		jsFunction2.Members.Set("prototype", jsObject2);
		JsJavaInterop.Install(it, scope);
		JsObject jsObject3 = new JsObject();
		array = new string[7] { "log", "info", "warn", "error", "debug", "println", "print" };
		foreach (string text3 in array)
		{
			jsObject3.Set(text3, JsFunction.FromNative(text3, (JsCall c) => JsUndefined.Value));
		}
		scope.Declare("console", jsObject3);
	}

	private static JsObject BuildMath(JsInterpreter it)
	{
		JsObject jsObject = new JsObject();
		jsObject.Set("PI", new JsNumber(Math.PI));
		jsObject.Set("E", new JsNumber(Math.E));
		jsObject.Set("floor", JsFunction.FromNative("floor", (JsCall c) => new JsNumber(Math.Floor(JsConvert.ToNumber(c.Arg(0))))));
		jsObject.Set("ceil", JsFunction.FromNative("ceil", (JsCall c) => new JsNumber(Math.Ceiling(JsConvert.ToNumber(c.Arg(0))))));
		jsObject.Set("round", JsFunction.FromNative("round", (JsCall c) =>
		{
			double num = JsConvert.ToNumber(c.Arg(0));
			return (double.IsNaN(num) || double.IsInfinity(num)) ? new JsNumber(double.NaN) : new JsNumber(Math.Floor(num + 0.5));
		}));
		jsObject.Set("abs", JsFunction.FromNative("abs", (JsCall c) => new JsNumber(Math.Abs(JsConvert.ToNumber(c.Arg(0))))));
		jsObject.Set("trunc", JsFunction.FromNative("trunc", (JsCall c) => new JsNumber(Math.Truncate(JsConvert.ToNumber(c.Arg(0))))));
		jsObject.Set("sqrt", JsFunction.FromNative("sqrt", (JsCall c) => new JsNumber(Math.Sqrt(JsConvert.ToNumber(c.Arg(0))))));
		jsObject.Set("pow", JsFunction.FromNative("pow", (JsCall c) => new JsNumber(Math.Pow(JsConvert.ToNumber(c.Arg(0)), JsConvert.ToNumber(c.Arg(1))))));
		jsObject.Set("sin", JsFunction.FromNative("sin", (JsCall c) => new JsNumber(Math.Sin(JsConvert.ToNumber(c.Arg(0))))));
		jsObject.Set("cos", JsFunction.FromNative("cos", (JsCall c) => new JsNumber(Math.Cos(JsConvert.ToNumber(c.Arg(0))))));
		jsObject.Set("tan", JsFunction.FromNative("tan", (JsCall c) => new JsNumber(Math.Tan(JsConvert.ToNumber(c.Arg(0))))));
		jsObject.Set("log", JsFunction.FromNative("log", (JsCall c) => new JsNumber(Math.Log(JsConvert.ToNumber(c.Arg(0))))));
		jsObject.Set("exp", JsFunction.FromNative("exp", (JsCall c) => new JsNumber(Math.Exp(JsConvert.ToNumber(c.Arg(0))))));
		jsObject.Set("random", JsFunction.FromNative("random", (JsCall c) => new JsNumber(it.Random())));
		jsObject.Set("max", JsFunction.FromNative("max", (JsCall c) =>
		{
			if (c.ArgCount == 0)
			{
				return new JsNumber(double.NegativeInfinity);
			}
			double num = double.NegativeInfinity;
			for (int i = 0; i < c.ArgCount; i++)
			{
				double num2 = JsConvert.ToNumber(c.Arg(i));
				if (double.IsNaN(num2))
				{
					return new JsNumber(double.NaN);
				}
				if (num2 > num)
				{
					num = num2;
				}
			}
			return new JsNumber(num);
		}));
		jsObject.Set("min", JsFunction.FromNative("min", (JsCall c) =>
		{
			if (c.ArgCount == 0)
			{
				return new JsNumber(double.PositiveInfinity);
			}
			double num = double.PositiveInfinity;
			for (int i = 0; i < c.ArgCount; i++)
			{
				double num2 = JsConvert.ToNumber(c.Arg(i));
				if (double.IsNaN(num2))
				{
					return new JsNumber(double.NaN);
				}
				if (num2 < num)
				{
					num = num2;
				}
			}
			return new JsNumber(num);
		}));
		return jsObject;
	}

	private static JsObject BuildJson(JsInterpreter it)
	{
		JsObject jsObject = new JsObject();
		jsObject.Set("stringify", JsFunction.FromNative("stringify", (JsCall c) => new JsString(Stringify(c.Arg(0)))));
		jsObject.Set("parse", JsFunction.FromNative("parse", (JsCall c) =>
		{
			throw new JsRuntimeException("JSON.parse is not supported");
		}));
		return jsObject;
	}

	private static string Stringify(JsValue v)
	{
		if (v == null || v is JsUndefined || v is JsNull)
		{
			return "null";
		}
		if (v is JsNumber jsNumber)
		{
			return JsConvert.NumberToString(jsNumber.Value);
		}
		if (v is JsBoolean jsBoolean)
		{
			if (!jsBoolean.Value)
			{
				return "false";
			}
			return "true";
		}
		if (v is JsString jsString)
		{
			return "\"" + jsString.Value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n")
				.Replace("\r", "\\r")
				.Replace("\t", "\\t") + "\"";
		}
		if (v is JsArray jsArray)
		{
			List<string> list = new List<string>();
			for (int i = 0; i < jsArray.Items.Count; i++)
			{
				list.Add(Stringify(jsArray.Items[i]));
			}
			return "[" + string.Join(",", list) + "]";
		}
		if (v is JsObject jsObject)
		{
			List<string> list2 = new List<string>();
			foreach (KeyValuePair<string, JsValue> prop in jsObject.Props)
			{
				list2.Add(Stringify(new JsString(prop.Key)) + ":" + Stringify(prop.Value));
			}
			return "{" + string.Join(",", list2) + "}";
		}
		return "null";
	}

	public static bool HasStringMethod(string name)
	{
		for (int i = 0; i < StringMethodNames.Length; i++)
		{
			if (StringMethodNames[i] == name)
			{
				return true;
			}
		}
		return false;
	}

	public static JsFunction StringMethod(JsInterpreter it, string name)
	{
		return JsFunction.FromNative(name, (JsCall c) =>
		{
			string text = ((c.This is JsString jsString) ? jsString.Value : JsConvert.ToString(c.This));
			switch (name)
			{
			case "charAt":
			{
				int num12 = JsConvert.ToInt32(c.Arg(0));
				if (num12 < 0 || num12 >= text.Length)
				{
					return new JsString("");
				}
				return new JsString(text[num12].ToString());
			}
			case "charCodeAt":
			{
				int num7 = JsConvert.ToInt32(c.Arg(0));
				return new JsNumber((num7 >= 0 && num7 < text.Length) ? ((double)(int)text[num7]) : double.NaN);
			}
			case "indexOf":
				return new JsNumber(text.IndexOf(JsConvert.ToString(c.Arg(0)), ClampIndex(JsConvert.ToInt32(c.Arg(1)), 0, text.Length), StringComparison.Ordinal));
			case "lastIndexOf":
				return new JsNumber(text.LastIndexOf(JsConvert.ToString(c.Arg(0)), StringComparison.Ordinal));
			case "substring":
			{
				int num2 = ClampIndex(JsConvert.ToInt32(c.Arg(0)), 0, text.Length);
				int num3 = ((c.ArgCount > 1 && !(c.Arg(1) is JsUndefined)) ? ClampIndex(JsConvert.ToInt32(c.Arg(1)), 0, text.Length) : text.Length);
				if (num2 > num3)
				{
					int num4 = num2;
					num2 = num3;
					num3 = num4;
				}
				return new JsString(text.Substring(num2, num3 - num2));
			}
			case "substr":
			{
				int num8 = JsConvert.ToInt32(c.Arg(0));
				if (num8 < 0)
				{
					num8 = Math.Max(0, text.Length + num8);
				}
				if (num8 > text.Length)
				{
					return new JsString("");
				}
				int num9 = ((c.ArgCount > 1 && !(c.Arg(1) is JsUndefined)) ? JsConvert.ToInt32(c.Arg(1)) : (text.Length - num8));
				if (num9 < 0)
				{
					num9 = 0;
				}
				return new JsString(text.Substring(num8, Math.Min(num9, text.Length - num8)));
			}
			case "slice":
			{
				int num10 = NormalizeSlice(JsConvert.ToInt32(c.Arg(0)), text.Length);
				int num11 = ((c.ArgCount > 1 && !(c.Arg(1) is JsUndefined)) ? NormalizeSlice(JsConvert.ToInt32(c.Arg(1)), text.Length) : text.Length);
				if (num11 < num10)
				{
					num11 = num10;
				}
				return new JsString(text.Substring(num10, num11 - num10));
			}
			case "split":
			{
				JsValue jsValue = c.Arg(0);
				JsArray jsArray = new JsArray();
				if (jsValue is JsUndefined)
				{
					jsArray.Items.Add(new JsString(text));
					return jsArray;
				}
				string[] array;
				if (jsValue is JsRegExp jsRegExp2)
				{
					array = jsRegExp2.Regex.Split(text);
					foreach (string v in array)
					{
						jsArray.Items.Add(new JsString(v));
					}
					return jsArray;
				}
				string text6 = JsConvert.ToString(jsValue);
				if (text6.Length == 0)
				{
					for (int k = 0; k < text.Length; k++)
					{
						jsArray.Items.Add(new JsString(text[k].ToString()));
					}
					return jsArray;
				}
				array = text.Split(new string[1] { text6 }, StringSplitOptions.None);
				foreach (string v2 in array)
				{
					jsArray.Items.Add(new JsString(v2));
				}
				return jsArray;
			}
			case "replace":
			{
				if (c.Arg(0) is JsRegExp jsRegExp)
				{
					string replacement = JsConvert.ToString(c.Arg(1));
					return new JsString(jsRegExp.Regex.Replace(text, replacement, (!jsRegExp.Global) ? 1 : int.MaxValue));
				}
				string text2 = JsConvert.ToString(c.Arg(0));
				string text3 = JsConvert.ToString(c.Arg(1));
				int num5 = text.IndexOf(text2, StringComparison.Ordinal);
				if (num5 >= 0)
				{
					return new JsString(text.Substring(0, num5) + text3 + text.Substring(num5 + text2.Length));
				}
				return new JsString(text);
			}
			case "match":
			{
				JsRegExp jsRegExp3 = (c.Arg(0) as JsRegExp) ?? new JsRegExp(JsConvert.ToString(c.Arg(0)), "");
				JsArray jsArray2 = new JsArray();
				if (!jsRegExp3.Global)
				{
					return it.Call(jsRegExp3.Get("exec"), jsRegExp3, new JsValue[1]
					{
						new JsString(text)
					});
				}
				foreach (Match item in jsRegExp3.Regex.Matches(text))
				{
					jsArray2.Items.Add(new JsString(item.Value));
				}
				if (jsArray2.Items.Count != 0)
				{
					return jsArray2;
				}
				return JsNull.Value;
			}
			case "padEnd":
			case "padStart":
			{
				int num6 = JsConvert.ToInt32(c.Arg(0));
				if (num6 > 100000)
				{
					throw new JsRuntimeException("RangeError: padded string exceeds budget");
				}
				string text4 = ((c.ArgCount < 2) ? " " : JsConvert.ToString(c.Arg(1)));
				if (text4.Length == 0 || num6 <= text.Length)
				{
					return new JsString(text);
				}
				StringBuilder stringBuilder2 = new StringBuilder();
				while (stringBuilder2.Length < num6 - text.Length)
				{
					stringBuilder2.Append(text4);
				}
				string text5 = stringBuilder2.ToString(0, num6 - text.Length);
				return new JsString((name == "padEnd") ? (text + text5) : (text5 + text));
			}
			case "toLowerCase":
				return new JsString(text.ToLowerInvariant());
			case "toUpperCase":
				return new JsString(text.ToUpperInvariant());
			case "trim":
				return new JsString(text.Trim());
			case "concat":
				return new JsString(text + JsConvert.ToString(c.Arg(0)));
			case "toString":
			case "valueOf":
				return new JsString(text);
			case "startsWith":
				return JsBoolean.Of(text.StartsWith(JsConvert.ToString(c.Arg(0)), StringComparison.Ordinal));
			case "endsWith":
				return JsBoolean.Of(text.EndsWith(JsConvert.ToString(c.Arg(0)), StringComparison.Ordinal));
			case "includes":
				return JsBoolean.Of(text.IndexOf(JsConvert.ToString(c.Arg(0)), StringComparison.Ordinal) >= 0);
			case "repeat":
			{
				int num = JsConvert.ToInt32(c.Arg(0));
				if (num <= 0)
				{
					return new JsString("");
				}
				StringBuilder stringBuilder = new StringBuilder();
				for (int i = 0; i < num; i++)
				{
					if (stringBuilder.Length >= 100000)
					{
						break;
					}
					stringBuilder.Append(text);
				}
				return new JsString(stringBuilder.ToString());
			}
			default:
				return JsUndefined.Value;
			}
		});
	}

	public static bool HasArrayMethod(string name)
	{
		for (int i = 0; i < ArrayMethodNames.Length; i++)
		{
			if (ArrayMethodNames[i] == name)
			{
				return true;
			}
		}
		return false;
	}

	public static JsFunction ArrayMethod(JsInterpreter it, string name)
	{
		return JsFunction.FromNative(name, (JsCall c) =>
		{
			if (!(c.This is JsArray jsArray))
			{
				if (name == "indexOf" || name == "lastIndexOf")
				{
					return new JsNumber(-1.0);
				}
				return JsUndefined.Value;
			}
			switch (name)
			{
			case "toArray":
				return jsArray;
			case "size":
				return new JsNumber(jsArray.Items.Count);
			case "isEmpty":
				return JsBoolean.Of(jsArray.Items.Count == 0);
			case "get":
			{
				int num10 = JsConvert.ToInt32(c.Arg(0));
				if (num10 < 0 || num10 >= jsArray.Items.Count)
				{
					return JsNull.Value;
				}
				return jsArray.Items[num10];
			}
			case "contains":
			{
				string text2 = JsConvert.ToString(c.Arg(0));
				for (int num6 = 0; num6 < jsArray.Items.Count; num6++)
				{
					if (JsConvert.ToString(jsArray.Items[num6]) == text2)
					{
						return JsBoolean.Of(v: true);
					}
				}
				return JsBoolean.Of(v: false);
			}
			case "add":
			{
				for (int num15 = 0; num15 < c.ArgCount; num15++)
				{
					jsArray.Items.Add(c.Arg(num15) ?? JsUndefined.Value);
				}
				return JsBoolean.Of(v: true);
			}
			case "push":
			{
				for (int n = 0; n < c.ArgCount; n++)
				{
					jsArray.Items.Add(c.Arg(n) ?? JsUndefined.Value);
				}
				return new JsNumber(jsArray.Items.Count);
			}
			case "pop":
				if (jsArray.Items.Count != 0)
				{
					JsValue result = jsArray.Items[jsArray.Items.Count - 1];
					jsArray.Items.RemoveAt(jsArray.Items.Count - 1);
					return result;
				}
				return JsUndefined.Value;
			case "shift":
				if (jsArray.Items.Count != 0)
				{
					JsValue result2 = jsArray.Items[0];
					jsArray.Items.RemoveAt(0);
					return result2;
				}
				return JsUndefined.Value;
			case "unshift":
			{
				for (int num5 = c.ArgCount - 1; num5 >= 0; num5--)
				{
					jsArray.Items.Insert(0, c.Arg(num5));
				}
				return new JsNumber(jsArray.Items.Count);
			}
			case "splice":
			{
				int num11 = NormalizeSlice(JsConvert.ToInt32(c.Arg(0)), jsArray.Items.Count);
				int num12 = ((c.ArgCount > 1) ? Math.Max(0, Math.Min(JsConvert.ToInt32(c.Arg(1)), jsArray.Items.Count - num11)) : (jsArray.Items.Count - num11));
				JsArray jsArray7 = new JsArray();
				for (int num13 = 0; num13 < num12; num13++)
				{
					jsArray7.Items.Add(jsArray.Items[num11 + num13]);
				}
				jsArray.Items.RemoveRange(num11, num12);
				for (int num14 = c.ArgCount - 1; num14 >= 2; num14--)
				{
					jsArray.Items.Insert(num11, c.Arg(num14));
				}
				return jsArray7;
			}
			case "slice":
			{
				int num = NormalizeSlice(JsConvert.ToInt32(c.Arg(0)), jsArray.Items.Count);
				int num2 = ((c.ArgCount > 1 && !(c.Arg(1) is JsUndefined)) ? NormalizeSlice(JsConvert.ToInt32(c.Arg(1)), jsArray.Items.Count) : jsArray.Items.Count);
				JsArray jsArray6 = new JsArray();
				for (int l = num; l < num2 && l < jsArray.Items.Count; l++)
				{
					jsArray6.Items.Add(jsArray.Items[l]);
				}
				return jsArray6;
			}
			case "join":
			{
				string separator = ((c.ArgCount > 0 && !(c.Arg(0) is JsUndefined)) ? JsConvert.ToString(c.Arg(0)) : ",");
				List<string> list = new List<string>();
				for (int num8 = 0; num8 < jsArray.Items.Count; num8++)
				{
					JsValue jsValue2 = jsArray.Items[num8];
					list.Add((jsValue2 == null || jsValue2 is JsUndefined || jsValue2 is JsNull) ? "" : JsConvert.ToString(jsValue2));
				}
				return new JsString(string.Join(separator, list));
			}
			case "indexOf":
			{
				string text3 = JsConvert.ToString(c.Arg(0));
				for (int num7 = 0; num7 < jsArray.Items.Count; num7++)
				{
					if (JsConvert.ToString(jsArray.Items[num7]) == text3)
					{
						return new JsNumber(num7);
					}
				}
				return new JsNumber(-1.0);
			}
			case "lastIndexOf":
			{
				string text = JsConvert.ToString(c.Arg(0));
				for (int num3 = jsArray.Items.Count - 1; num3 >= 0; num3--)
				{
					if (JsConvert.ToString(jsArray.Items[num3]) == text)
					{
						return new JsNumber(num3);
					}
				}
				return new JsNumber(-1.0);
			}
			case "reverse":
				jsArray.Items.Reverse();
				return jsArray;
			case "sort":
			{
				JsFunction cmp = c.Arg(0) as JsFunction;
				jsArray.Items.Sort((JsValue x, JsValue y) =>
				{
					if (cmp != null)
					{
						double num16 = JsConvert.ToNumber(it.Call(cmp, JsUndefined.Value, new JsValue[2] { x, y }));
						if (!(num16 < 0.0))
						{
							if (!(num16 > 0.0))
							{
								return 0;
							}
							return 1;
						}
						return -1;
					}
					return string.CompareOrdinal(JsConvert.ToString(x), JsConvert.ToString(y));
				});
				return jsArray;
			}
			case "concat":
			{
				JsArray jsArray4 = new JsArray(jsArray.Items);
				for (int k = 0; k < c.ArgCount; k++)
				{
					JsValue jsValue = c.Arg(k);
					if (jsValue is JsArray jsArray5)
					{
						jsArray4.Items.AddRange(jsArray5.Items);
					}
					else
					{
						jsArray4.Items.Add(jsValue);
					}
				}
				return jsArray4;
			}
			case "reduce":
			{
				int num9 = 0;
				JsValue jsValue3;
				if (c.ArgCount > 1)
				{
					jsValue3 = c.Arg(1);
				}
				else
				{
					if (jsArray.Items.Count <= 0)
					{
						throw new JsRuntimeException("TypeError: reduce of empty array with no initial value");
					}
					jsValue3 = jsArray.Items[num9++];
				}
				for (; num9 < jsArray.Items.Count; num9++)
				{
					jsValue3 = it.Call(c.Arg(0), JsUndefined.Value, new JsValue[4]
					{
						jsValue3,
						jsArray.Items[num9],
						new JsNumber(num9),
						jsArray
					});
				}
				return jsValue3;
			}
			case "some":
			case "every":
			case "find":
			case "findIndex":
			{
				for (int num4 = 0; num4 < jsArray.Items.Count; num4++)
				{
					bool flag = JsConvert.ToBoolean(it.Call(c.Arg(0), c.Arg(1), new JsValue[3]
					{
						jsArray.Items[num4],
						new JsNumber(num4),
						jsArray
					}));
					if (flag && name == "find")
					{
						return jsArray.Items[num4];
					}
					if (flag && name == "findIndex")
					{
						return new JsNumber(num4);
					}
					if (flag && name == "some")
					{
						return JsBoolean.True;
					}
					if (!flag && name == "every")
					{
						return JsBoolean.False;
					}
				}
				if (name == "find")
				{
					return JsUndefined.Value;
				}
				if (name == "findIndex")
				{
					return new JsNumber(-1.0);
				}
				return JsBoolean.Of(name == "every");
			}
			case "includes":
				return JsBoolean.Of(jsArray.Items.Exists((JsValue v) => JsConvert.StrictEquals(v, c.Arg(0))));
			case "forEach":
			{
				if (!(c.Arg(0) is JsFunction fnValue3))
				{
					throw new JsRuntimeException("TypeError: forEach callback is not a function");
				}
				for (int m = 0; m < jsArray.Items.Count; m++)
				{
					it.Call(fnValue3, JsUndefined.Value, new JsValue[3]
					{
						jsArray.Items[m],
						new JsNumber(m),
						jsArray
					});
				}
				return JsUndefined.Value;
			}
			case "map":
			{
				JsFunction fnValue2 = c.Arg(0) as JsFunction;
				JsArray jsArray3 = new JsArray();
				for (int j = 0; j < jsArray.Items.Count; j++)
				{
					jsArray3.Items.Add(it.Call(fnValue2, JsUndefined.Value, new JsValue[3]
					{
						jsArray.Items[j],
						new JsNumber(j),
						jsArray
					}));
				}
				return jsArray3;
			}
			case "filter":
			{
				JsFunction fnValue = c.Arg(0) as JsFunction;
				JsArray jsArray2 = new JsArray();
				for (int i = 0; i < jsArray.Items.Count; i++)
				{
					if (JsConvert.ToBoolean(it.Call(fnValue, JsUndefined.Value, new JsValue[3]
					{
						jsArray.Items[i],
						new JsNumber(i),
						jsArray
					})))
					{
						jsArray2.Items.Add(jsArray.Items[i]);
					}
				}
				return jsArray2;
			}
			case "toString":
				return new JsString(JsConvert.ToString(jsArray));
			default:
				return JsUndefined.Value;
			}
		});
	}

	public static bool HasNumberMethod(string name)
	{
		for (int i = 0; i < NumberMethodNames.Length; i++)
		{
			if (NumberMethodNames[i] == name)
			{
				return true;
			}
		}
		return false;
	}

	public static JsFunction NumberMethod(JsInterpreter it, string name)
	{
		return JsFunction.FromNative(name, (JsCall c) =>
		{
			double num = JsConvert.ToNumber(c.This);
			switch (name)
			{
			case "toString":
				return new JsString(JsConvert.NumberToString(num));
			case "toLocaleString":
				return new JsString((double.IsNaN(num) || double.IsInfinity(num)) ? JsConvert.NumberToString(num) : num.ToString("#,0.###", CultureInfo.InvariantCulture));
			case "toFixed":
			{
				int val = ((c.ArgCount > 0) ? JsConvert.ToInt32(c.Arg(0)) : 0);
				return new JsString(num.ToString("F" + Math.Max(0, Math.Min(20, val)), CultureInfo.InvariantCulture));
			}
			case "valueOf":
				return new JsNumber(num);
			default:
				return JsUndefined.Value;
			}
		});
	}

	public static double ParseInt(string s, int radix)
	{
		if (string.IsNullOrEmpty(s))
		{
			return double.NaN;
		}
		s = s.TrimStart();
		int num = 1;
		int i = 0;
		if (s.Length > 0 && (s[0] == '+' || s[0] == '-'))
		{
			if (s[0] == '-')
			{
				num = -1;
			}
			i = 1;
		}
		switch (radix)
		{
		case 0:
			radix = 10;
			if (i + 1 < s.Length && s[i] == '0' && (s[i + 1] == 'x' || s[i + 1] == 'X'))
			{
				radix = 16;
				i += 2;
			}
			break;
		case 16:
			if (i + 1 < s.Length && s[i] == '0' && (s[i + 1] == 'x' || s[i + 1] == 'X'))
			{
				i += 2;
			}
			break;
		}
		if (radix < 2 || radix > 36)
		{
			return double.NaN;
		}
		double num2 = 0.0;
		bool flag = false;
		for (; i < s.Length; i++)
		{
			int num3 = DigitValue(s[i]);
			if (num3 < 0 || num3 >= radix)
			{
				break;
			}
			num2 = num2 * (double)radix + (double)num3;
			flag = true;
		}
		if (!flag)
		{
			return double.NaN;
		}
		return (double)num * num2;
	}

	private static int DigitValue(char c)
	{
		if (c >= '0' && c <= '9')
		{
			return c - 48;
		}
		if (c >= 'a' && c <= 'z')
		{
			return c - 97 + 10;
		}
		if (c >= 'A' && c <= 'Z')
		{
			return c - 65 + 10;
		}
		return -1;
	}

	public static double ParseFloat(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return double.NaN;
		}
		s = s.TrimStart();
		int i;
		for (i = 0; i < s.Length && "0123456789+-.eE".IndexOf(s[i]) >= 0; i++)
		{
		}
		while (i > 0)
		{
			if (double.TryParse(s.Substring(0, i), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				return result;
			}
			i--;
		}
		return double.NaN;
	}

	private static int ClampIndex(int i, int min, int max)
	{
		if (i >= min)
		{
			if (i <= max)
			{
				return i;
			}
			return max;
		}
		return min;
	}

	private static int NormalizeSlice(int i, int len)
	{
		if (i < 0)
		{
			return Math.Max(0, len + i);
		}
		return Math.Min(i, len);
	}
}
