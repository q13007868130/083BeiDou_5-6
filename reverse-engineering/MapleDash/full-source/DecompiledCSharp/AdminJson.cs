using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class AdminJson
{
	public const int CodeSuccess = 20000;

	public const int CodeError = -1;

	public static string Write(object value)
	{
		StringBuilder stringBuilder = new StringBuilder(256);
		WriteValue(stringBuilder, value);
		return stringBuilder.ToString();
	}

	private static void WriteValue(StringBuilder sb, object v)
	{
		if (v == null)
		{
			sb.Append("null");
		}
		else if (v is string s)
		{
			WriteString(sb, s);
		}
		else if (v is bool flag)
		{
			sb.Append(flag ? "true" : "false");
		}
		else if (v is char c)
		{
			WriteString(sb, c.ToString());
		}
		else if (v is int || v is long || v is short || v is sbyte || v is uint || v is ulong || v is ushort || v is byte)
		{
			sb.Append(Convert.ToInt64(v, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
		}
		else if (v is float num)
		{
			WriteDouble(sb, num);
		}
		else if (v is double d)
		{
			WriteDouble(sb, d);
		}
		else if (v is decimal num2)
		{
			sb.Append(num2.ToString(CultureInfo.InvariantCulture));
		}
		else if (v is IDictionary<string, object> dictionary)
		{
			sb.Append('{');
			bool flag2 = true;
			foreach (KeyValuePair<string, object> item in dictionary)
			{
				if (!flag2)
				{
					sb.Append(',');
				}
				flag2 = false;
				WriteString(sb, item.Key);
				sb.Append(':');
				WriteValue(sb, item.Value);
			}
			sb.Append('}');
		}
		else if (v is IDictionary dictionary2)
		{
			sb.Append('{');
			bool flag3 = true;
			foreach (DictionaryEntry item2 in dictionary2)
			{
				if (!flag3)
				{
					sb.Append(',');
				}
				flag3 = false;
				WriteString(sb, Convert.ToString(item2.Key, CultureInfo.InvariantCulture));
				sb.Append(':');
				WriteValue(sb, item2.Value);
			}
			sb.Append('}');
		}
		else if (v is IEnumerable enumerable)
		{
			sb.Append('[');
			bool flag4 = true;
			foreach (object item3 in enumerable)
			{
				if (!flag4)
				{
					sb.Append(',');
				}
				flag4 = false;
				WriteValue(sb, item3);
			}
			sb.Append(']');
		}
		else
		{
			WriteString(sb, Convert.ToString(v, CultureInfo.InvariantCulture));
		}
	}

	private static void WriteDouble(StringBuilder sb, double d)
	{
		if (double.IsNaN(d) || double.IsInfinity(d))
		{
			sb.Append("0");
		}
		else if (Math.Abs(d - Math.Round(d)) < 1E-09 && Math.Abs(d) < 1000000000000000.0)
		{
			sb.Append(((long)Math.Round(d)).ToString(CultureInfo.InvariantCulture));
		}
		else
		{
			sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
		}
	}

	private static void WriteString(StringBuilder sb, string s)
	{
		sb.Append('"');
		foreach (char c in s)
		{
			switch (c)
			{
			case '"':
				sb.Append("\\\"");
				continue;
			case '\\':
				sb.Append("\\\\");
				continue;
			case '\n':
				sb.Append("\\n");
				continue;
			case '\r':
				sb.Append("\\r");
				continue;
			case '\t':
				sb.Append("\\t");
				continue;
			case '\b':
				sb.Append("\\b");
				continue;
			case '\f':
				sb.Append("\\f");
				continue;
			}
			if (c < ' ' || c == '\u2028' || c == '\u2029')
			{
				StringBuilder stringBuilder = sb.Append("\\u");
				int num = c;
				stringBuilder.Append(num.ToString("x4", CultureInfo.InvariantCulture));
			}
			else
			{
				sb.Append(c);
			}
		}
		sb.Append('"');
	}

	public static object Parse(string json)
	{
		if (string.IsNullOrEmpty(json))
		{
			return null;
		}
		return MiniJsonDb.Parse(json);
	}

	public static object UnwrapSubmit(object body)
	{
		if (body is Dictionary<string, object> dictionary && dictionary.ContainsKey("data") && (dictionary.ContainsKey("requestId") || dictionary.Count == 1 || dictionary.Count == 2))
		{
			return dictionary["data"];
		}
		return body;
	}

	public static Dictionary<string, object> AsDict(object o)
	{
		return o as Dictionary<string, object>;
	}

	public static List<object> AsList(object o)
	{
		return o as List<object>;
	}

	public static object Get(object o, string key)
	{
		Dictionary<string, object> dictionary = AsDict(o);
		if (dictionary == null)
		{
			return null;
		}
		if (!dictionary.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public static Dictionary<string, object> GetDict(object o, string key)
	{
		return AsDict(Get(o, key));
	}

	public static List<object> GetList(object o, string key)
	{
		return AsList(Get(o, key));
	}

	public static string GetString(object o, string key, string fallback = "")
	{
		object obj = Get(o, key);
		if (obj == null)
		{
			return fallback;
		}
		return (obj as string) ?? Convert.ToString(obj, CultureInfo.InvariantCulture);
	}

	public static long GetLong(object o, string key, long fallback = 0L)
	{
		object obj = Get(o, key);
		if (obj == null)
		{
			return fallback;
		}
		if (obj is long)
		{
			return (long)obj;
		}
		if (obj is int num)
		{
			return num;
		}
		if (obj is double num2)
		{
			return (long)num2;
		}
		if (obj is bool)
		{
			return ((bool)obj) ? 1 : 0;
		}
		if (long.TryParse(Convert.ToString(obj, CultureInfo.InvariantCulture), out var result))
		{
			return result;
		}
		return fallback;
	}

	public static int GetInt(object o, string key, int fallback = 0)
	{
		return (int)GetLong(o, key, fallback);
	}

	public static bool GetBool(object o, string key, bool fallback = false)
	{
		object obj = Get(o, key);
		if (obj == null)
		{
			return fallback;
		}
		if (obj is bool)
		{
			return (bool)obj;
		}
		if (obj is long num)
		{
			return num != 0;
		}
		if (obj is double value)
		{
			return Math.Abs(value) > 1E-09;
		}
		string text = Convert.ToString(obj, CultureInfo.InvariantCulture);
		if (bool.TryParse(text, out var result))
		{
			return result;
		}
		return text == "1";
	}

	public static Dictionary<string, object> Ok(object data = null)
	{
		return new Dictionary<string, object>
		{
			{ "code", 20000 },
			{ "message", "success" },
			{
				"responseId",
				Guid.NewGuid().ToString()
			},
			{ "data", data }
		};
	}

	public static Dictionary<string, object> Fail(string message, int code = -1)
	{
		return new Dictionary<string, object>
		{
			{ "code", code },
			{
				"message",
				message ?? "error"
			},
			{
				"responseId",
				Guid.NewGuid().ToString()
			},
			{ "data", null }
		};
	}

	public static Dictionary<string, object> Obj(params object[] kv)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		for (int i = 0; i + 1 < kv.Length; i += 2)
		{
			dictionary[Convert.ToString(kv[i], CultureInfo.InvariantCulture)] = kv[i + 1];
		}
		return dictionary;
	}

	public static Dictionary<string, object> Page(List<object> records, int pageNumber, int pageSize, long total)
	{
		int num = ((pageSize <= 0) ? 20 : pageSize);
		long num2 = ((total <= 0) ? 0 : ((total + num - 1) / num));
		return new Dictionary<string, object>
		{
			{
				"records",
				records ?? new List<object>()
			},
			{ "pageNumber", pageNumber },
			{ "pageSize", num },
			{ "totalPage", num2 },
			{ "totalRow", total }
		};
	}
}
