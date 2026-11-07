using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace MapleDash.Js;

public static class JsJavaInterop
{
	private static JsFunction Fn(string name, Func<JsCall, JsValue> body)
	{
		return JsFunction.FromNative(name, body);
	}

	private static JsNumber N(double value)
	{
		return new JsNumber(value);
	}

	private static string Text(JsCall c, int n = 0)
	{
		return JsConvert.ToString(c.Arg(n));
	}

	private static int Int(JsCall c, int n = 0)
	{
		return JsConvert.ToInt32(c.Arg(n));
	}

	public static void Install(JsInterpreter it, JsScope scope)
	{
		JsObject jsObject = new JsObject();
		jsObject.Set("type", Fn("type", (JsCall c) => Resolve(it, Text(c))));
		jsObject.Set("from", Fn("from", (JsCall c) => new JsArray(JsPatterns.Iterate(c.Arg(0)))));
		jsObject.Set("to", Fn("to", (JsCall c) => c.Arg(0)));
		scope.Declare("Java", jsObject);
		string[] array = new string[6] { "java", "javax", "org", "net", "com", "Packages" };
		foreach (string text in array)
		{
			scope.Declare(text, Package(it, (text == "Packages") ? "" : text));
		}
		scope.Declare("importClass", Fn("importClass", (JsCall c) =>
		{
			JsValue[] args = c.Args;
			foreach (JsValue obj in args)
			{
				string text2 = JsConvert.ToString(it.GetMember(obj, "__javaClassName"));
				if (text2 == "undefined")
				{
					throw new JsRuntimeException("Invalid importClass argument");
				}
				scope.Declare(text2.Substring(text2.LastIndexOf('.') + 1), Resolve(it, text2));
			}
			return JsUndefined.Value;
		}));
		List<string> imports = new List<string>();
		scope.ResolveImport = (string name) =>
		{
			if (name.Length == 0 || !char.IsUpper(name[0]))
			{
				return (JsValue)null;
			}
			JsValue jsValue = null;
			foreach (string item in imports)
			{
				string text2 = item + "." + name;
				JsValue jsValue2;
				try
				{
					jsValue2 = Resolve(it, text2);
				}
				catch (JsRuntimeException ex) when (ex.Message == "Java interop: class not migrated: " + text2)
				{
					continue;
				}
				if (jsValue != null && jsValue != jsValue2)
				{
					throw new JsRuntimeException("Ambiguous Java import: " + name);
				}
				jsValue = jsValue2;
			}
			return jsValue;
		};
		scope.Declare("importPackage", Fn("importPackage", (JsCall c) =>
		{
			JsValue[] args = c.Args;
			for (int i = 0; i < args.Length; i++)
			{
				if (!(args[i] is JsHostObject { Host: string host }))
				{
					throw new JsRuntimeException("Invalid importPackage argument");
				}
				if (!imports.Contains(host))
				{
					imports.Add(host);
				}
			}
			return JsUndefined.Value;
		}));
	}

	private static JsHostObject Package(JsInterpreter it, string name)
	{
		return new JsHostObject(name)
		{
			Missing = (string key) =>
			{
				string name2 = ((name.Length == 0) ? key : (name + "." + key));
				return (key.Length > 0 && char.IsUpper(key[0])) ? Resolve(it, name2) : Package(it, name2);
			}
		};
	}

	public static JsValue Resolve(JsInterpreter it, string name)
	{
		if (it.JavaTypes.TryGetValue(name, out var value))
		{
			return value;
		}
		JsValue jsValue = ResolveType(it, name);
		if (jsValue is JsFunction jsFunction)
		{
			jsFunction.Members.Set("__javaClassName", new JsString(name));
		}
		else if (jsValue is JsHostObject jsHostObject)
		{
			jsHostObject.Set("__javaClassName", new JsString(name));
		}
		else if (jsValue is JsObject jsObject)
		{
			jsObject.Set("__javaClassName", new JsString(name));
		}
		it.JavaTypes[name] = jsValue;
		return jsValue;
	}

	private static JsValue ResolveType(JsInterpreter it, string name)
	{
		switch (name)
		{
		case "java.awt.Point":
			return Fn("Point", (JsCall c) => Point(c));
		case "java.awt.Rectangle":
			return Fn("Rectangle", (JsCall c) => Rectangle(c));
		case "java.util.ArrayList":
		case "java.util.LinkedList":
			return Fn("ArrayList", (JsCall c) => (!(c.Arg(0) is JsArray jsArray)) ? new JsArray() : new JsArray(jsArray.Items));
		case "java.util.HashMap":
		case "java.util.LinkedHashMap":
		case "java.util.TreeMap":
			return Fn("HashMap", (JsCall c) => Map());
		case "java.lang.Integer":
		case "java.lang.Long":
		case "java.lang.Short":
		case "java.lang.Byte":
		{
			JsFunction jsFunction = Fn("Number", (JsCall c) => N(JsConvert.ToNumber(c.Arg(0))));
			jsFunction.Members.Set("MAX_VALUE", N(name.EndsWith("Integer", StringComparison.Ordinal) ? int.MaxValue : (name.EndsWith("Short", StringComparison.Ordinal) ? 32767 : (name.EndsWith("Byte", StringComparison.Ordinal) ? 127 : long.MaxValue))));
			jsFunction.Members.Set("parseInt", Fn("parseInt", (JsCall c) => N(JsBuiltins.ParseInt(Text(c), (c.ArgCount > 1) ? Int(c, 1) : 10))));
			jsFunction.Members.Set("valueOf", Fn("valueOf", (JsCall c) => N(JsConvert.ToNumber(c.Arg(0)))));
			return jsFunction;
		}
		case "java.lang.String":
			return Fn("String", (JsCall c) => new JsString(Text(c)));
		case "java.lang.System":
		{
			JsObject jsObject2 = new JsObject();
			jsObject2.Set("currentTimeMillis", Fn("currentTimeMillis", (JsCall c) => N(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())));
			string[] array = new string[2] { "out", "err" };
			foreach (string text in array)
			{
				TextWriter writer = ((text == "out") ? Console.Out : Console.Error);
				JsObject jsObject3 = new JsObject();
				jsObject3.Set("println", Fn("println", (JsCall c) =>
				{
					writer.WriteLine((c.ArgCount == 0) ? "" : Text(c));
					return JsUndefined.Value;
				}));
				jsObject3.Set("print", Fn("print", (JsCall c) =>
				{
					writer.Write((c.ArgCount == 0) ? "" : Text(c));
					return JsUndefined.Value;
				}));
				jsObject2.Set(text, jsObject3);
			}
			return jsObject2;
		}
		case "java.util.Date":
			return JsDate.Constructor();
		case "java.util.Calendar":
			return Calendar();
		case "java.text.SimpleDateFormat":
			return Fn("SimpleDateFormat", (JsCall c) =>
			{
				string pattern = Text(c);
				if (pattern != "yyyyMMdd")
				{
					throw new JsRuntimeException("Unmigrated SimpleDateFormat pattern: " + pattern);
				}
				JsObject jsObject5 = new JsObject();
				jsObject5.Set("format", Fn("format", (JsCall a) => new JsString(DateTimeOffset.FromUnixTimeMilliseconds((long)((a.Arg(0) as JsDate) ?? throw new JsRuntimeException("SimpleDateFormat.format requires a Date")).Milliseconds).ToLocalTime().ToString(pattern, CultureInfo.InvariantCulture))));
				return jsObject5;
			});
		case "java.util.Arrays":
		{
			JsObject jsObject4 = new JsObject();
			jsObject4.Set("asList", Fn("asList", (JsCall c) => (c.ArgCount != 1 || !(c.Arg(0) is JsArray jsArray)) ? new JsArray(c.Args) : new JsArray(jsArray.Items)));
			return jsObject4;
		}
		default:
		{
			if (name.EndsWith("[]", StringComparison.Ordinal))
			{
				JsObject jsObject = new JsObject();
				jsObject.Set("__javaArrayType", new JsString(name));
				return jsObject;
			}
			JsValue jsValue = it.ResolveHostType?.Invoke(name);
			if (jsValue != null && !(jsValue is JsNull) && !(jsValue is JsUndefined))
			{
				return jsValue;
			}
			throw new JsRuntimeException("Java interop: class not migrated: " + name);
		}
		}
	}

	private static JsObject Calendar()
	{
		JsObject jsObject = new JsObject();
		foreach (KeyValuePair<string, int> item in new Dictionary<string, int>
		{
			["YEAR"] = 1,
			["MONTH"] = 2,
			["DATE"] = 5,
			["DAY_OF_MONTH"] = 5,
			["DAY_OF_WEEK"] = 7,
			["HOUR_OF_DAY"] = 11,
			["MINUTE"] = 12,
			["SECOND"] = 13,
			["MILLISECOND"] = 14,
			["SUNDAY"] = 1,
			["MONDAY"] = 2,
			["TUESDAY"] = 3,
			["WEDNESDAY"] = 4,
			["THURSDAY"] = 5,
			["FRIDAY"] = 6,
			["SATURDAY"] = 7
		})
		{
			jsObject.Set(item.Key, N(item.Value));
		}
		jsObject.Set("getInstance", Fn("getInstance", (JsCall c) =>
		{
			DateTimeOffset now = DateTimeOffset.Now;
			JsObject jsObject2 = new JsObject();
			jsObject2.Set("get", Fn("get", (JsCall a) => Int(a) switch
			{
				1 => (JsValue)N(now.Year), 
				2 => N(now.Month - 1), 
				5 => N(now.Day), 
				7 => N((double)(now.DayOfWeek + 1)), 
				11 => N(now.Hour), 
				12 => N(now.Minute), 
				13 => N(now.Second), 
				14 => N(now.Millisecond), 
				_ => throw new JsRuntimeException("Unmigrated Calendar field: " + Int(a)), 
			}));
			jsObject2.Set("getTime", Fn("getTime", (JsCall a) => new JsDate(now.ToUnixTimeMilliseconds())));
			jsObject2.Set("getTimeInMillis", Fn("getTimeInMillis", (JsCall a) => N(now.ToUnixTimeMilliseconds())));
			jsObject2.Set("setTime", Fn("setTime", (JsCall a) =>
			{
				if (!(a.Arg(0) is JsDate jsDate))
				{
					throw new JsRuntimeException("Calendar.setTime requires a Date");
				}
				now = DateTimeOffset.FromUnixTimeMilliseconds((long)jsDate.Milliseconds).ToLocalTime();
				return JsUndefined.Value;
			}));
			return jsObject2;
		}));
		return jsObject;
	}

	private static JsHostObject Point(JsCall c)
	{
		JsHostObject point = new JsHostObject(null);
		point.Set("x", N(Int(c)));
		point.Set("y", N(Int(c, 1)));
		point.Set("getX", Fn("getX", (JsCall a) => point.Get("x")));
		point.Set("getY", Fn("getY", (JsCall a) => point.Get("y")));
		point.Set("setLocation", Fn("setLocation", (JsCall a) =>
		{
			point.Set("x", a.Arg(0));
			point.Set("y", a.Arg(1));
			return JsUndefined.Value;
		}));
		return point;
	}

	private static JsHostObject Rectangle(JsCall c)
	{
		JsHostObject rectangle = Point(c);
		rectangle.Set("width", N(Int(c, 2)));
		rectangle.Set("height", N(Int(c, 3)));
		rectangle.Set("contains", Fn("contains", (JsCall a) =>
		{
			double num = ((a.Arg(0) is JsHostObject jsHostObject) ? JsConvert.ToNumber(jsHostObject.Get("x")) : JsConvert.ToNumber(a.Arg(0)));
			double num2 = ((a.Arg(0) is JsHostObject jsHostObject2) ? JsConvert.ToNumber(jsHostObject2.Get("y")) : JsConvert.ToNumber(a.Arg(1)));
			double num3 = JsConvert.ToNumber(rectangle.Get("x"));
			double num4 = JsConvert.ToNumber(rectangle.Get("y"));
			return JsBoolean.Of(num >= num3 && num2 >= num4 && num < num3 + JsConvert.ToNumber(rectangle.Get("width")) && num2 < num4 + JsConvert.ToNumber(rectangle.Get("height")));
		}));
		return rectangle;
	}

	private static JsHostObject Map()
	{
		List<JsValue> keys = new List<JsValue>();
		List<JsValue> values = new List<JsValue>();
		JsHostObject jsHostObject = new JsHostObject(null);
		Func<JsValue, int> find = (JsValue key) => keys.FindIndex((JsValue k) => JsConvert.StrictEquals(k, key));
		jsHostObject.Set("get", Fn("get", (JsCall c) =>
		{
			int num = find(c.Arg(0));
			return (num >= 0) ? values[num] : JsNull.Value;
		}));
		jsHostObject.Set("put", Fn("put", (JsCall c) =>
		{
			int num = find(c.Arg(0));
			JsValue result = ((num < 0) ? JsNull.Value : values[num]);
			if (num >= 0)
			{
				values[num] = c.Arg(1);
				return result;
			}
			keys.Add(c.Arg(0));
			values.Add(c.Arg(1));
			return result;
		}));
		jsHostObject.Set("containsKey", Fn("containsKey", (JsCall c) => JsBoolean.Of(find(c.Arg(0)) >= 0)));
		jsHostObject.Set("remove", Fn("remove", (JsCall c) =>
		{
			int num = find(c.Arg(0));
			if (num >= 0)
			{
				JsValue result = values[num];
				keys.RemoveAt(num);
				values.RemoveAt(num);
				return result;
			}
			return JsNull.Value;
		}));
		jsHostObject.Set("size", Fn("size", (JsCall c) => N(keys.Count)));
		jsHostObject.Set("isEmpty", Fn("isEmpty", (JsCall c) => JsBoolean.Of(keys.Count == 0)));
		jsHostObject.Set("keySet", Fn("keySet", (JsCall c) => new JsArray(keys)));
		jsHostObject.Set("values", Fn("values", (JsCall c) => new JsArray(values)));
		jsHostObject.Set("clear", Fn("clear", (JsCall c) =>
		{
			keys.Clear();
			values.Clear();
			return JsUndefined.Value;
		}));
		return jsHostObject;
	}
}
