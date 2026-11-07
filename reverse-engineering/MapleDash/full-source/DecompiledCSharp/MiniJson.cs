using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class MiniJson
{
	public static object Parse(string src)
	{
		if (string.IsNullOrEmpty(src))
		{
			return null;
		}
		int i = 0;
		object result = ParseValue(src, ref i);
		SkipWs(src, ref i);
		if (i < src.Length)
		{
			throw new FormatException("JSON 尾部有多余内容，位置 " + i);
		}
		return result;
	}

	private static void SkipWs(string s, ref int i)
	{
		while (i < s.Length)
		{
			char c = s[i];
			if (c == ' ' || c == '\t' || c == '\n' || c == '\r' || c == '\ufeff')
			{
				i++;
				continue;
			}
			break;
		}
	}

	private static object ParseValue(string s, ref int i)
	{
		SkipWs(s, ref i);
		if (i >= s.Length)
		{
			throw new FormatException("JSON 意外结束");
		}
		switch (s[i])
		{
		case '{':
			return ParseObject(s, ref i);
		case '[':
			return ParseArray(s, ref i);
		case '"':
			return ParseString(s, ref i);
		case 't':
			Expect(s, ref i, "true");
			return true;
		case 'f':
			Expect(s, ref i, "false");
			return false;
		case 'n':
			Expect(s, ref i, "null");
			return null;
		default:
			return ParseNumber(s, ref i);
		}
	}

	private static void Expect(string s, ref int i, string word)
	{
		if (i + word.Length > s.Length || string.CompareOrdinal(s, i, word, 0, word.Length) != 0)
		{
			throw new FormatException("JSON 期望 " + word + "，位置 " + i);
		}
		i += word.Length;
	}

	private static Dictionary<string, object> ParseObject(string s, ref int i)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		i++;
		SkipWs(s, ref i);
		if (i < s.Length && s[i] == '}')
		{
			i++;
			return dictionary;
		}
		while (true)
		{
			SkipWs(s, ref i);
			if (i >= s.Length || s[i] != '"')
			{
				throw new FormatException("JSON 对象的键必须是字符串，位置 " + i);
			}
			string key = ParseString(s, ref i);
			SkipWs(s, ref i);
			if (i >= s.Length || s[i] != ':')
			{
				throw new FormatException("JSON 缺少冒号，位置 " + i);
			}
			i++;
			dictionary[key] = ParseValue(s, ref i);
			SkipWs(s, ref i);
			if (i >= s.Length)
			{
				throw new FormatException("JSON 对象未闭合");
			}
			if (s[i] != ',')
			{
				break;
			}
			i++;
		}
		if (s[i] == '}')
		{
			i++;
			return dictionary;
		}
		throw new FormatException("JSON 对象里出现意外字符 '" + s[i] + "'，位置 " + i);
	}

	private static List<object> ParseArray(string s, ref int i)
	{
		List<object> list = new List<object>();
		i++;
		SkipWs(s, ref i);
		if (i < s.Length && s[i] == ']')
		{
			i++;
			return list;
		}
		while (true)
		{
			list.Add(ParseValue(s, ref i));
			SkipWs(s, ref i);
			if (i >= s.Length)
			{
				throw new FormatException("JSON 数组未闭合");
			}
			if (s[i] != ',')
			{
				break;
			}
			i++;
		}
		if (s[i] == ']')
		{
			i++;
			return list;
		}
		throw new FormatException("JSON 数组里出现意外字符 '" + s[i] + "'，位置 " + i);
	}

	private static string ParseString(string s, ref int i)
	{
		i++;
		StringBuilder stringBuilder = new StringBuilder();
		while (i < s.Length)
		{
			char c = s[i++];
			switch (c)
			{
			case '"':
				return stringBuilder.ToString();
			default:
				stringBuilder.Append(c);
				break;
			case '\\':
			{
				if (i >= s.Length)
				{
					throw new FormatException("JSON 转义未完成");
				}
				char c2 = s[i++];
				switch (c2)
				{
				case '"':
					stringBuilder.Append('"');
					break;
				case '\\':
					stringBuilder.Append('\\');
					break;
				case '/':
					stringBuilder.Append('/');
					break;
				case 'b':
					stringBuilder.Append('\b');
					break;
				case 'f':
					stringBuilder.Append('\f');
					break;
				case 'n':
					stringBuilder.Append('\n');
					break;
				case 'r':
					stringBuilder.Append('\r');
					break;
				case 't':
					stringBuilder.Append('\t');
					break;
				case 'u':
					if (i + 4 > s.Length)
					{
						throw new FormatException("JSON \\u 转义不完整");
					}
					stringBuilder.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
					i += 4;
					break;
				default:
					throw new FormatException("JSON 未知转义 \\" + c2);
				}
				break;
			}
			}
		}
		throw new FormatException("JSON 字符串未闭合");
	}

	private static double ParseNumber(string s, ref int i)
	{
		int num = i;
		if (i < s.Length && (s[i] == '-' || s[i] == '+'))
		{
			i++;
		}
		while (i < s.Length)
		{
			char c = s[i];
			if ((c < '0' || c > '9') && c != '.' && c != 'e' && c != 'E' && c != '+' && c != '-')
			{
				break;
			}
			i++;
		}
		string text = s.Substring(num, i - num);
		if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			throw new FormatException("JSON 数字格式错误：" + text);
		}
		return result;
	}
}
