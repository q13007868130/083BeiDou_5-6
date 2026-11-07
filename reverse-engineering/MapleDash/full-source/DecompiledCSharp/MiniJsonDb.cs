using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class MiniJsonDb
{
	public static object Parse(string json)
	{
		int i = 0;
		return ParseValue(json, ref i);
	}

	private static object ParseValue(string s, ref int i)
	{
		SkipWs(s, ref i);
		if (i >= s.Length)
		{
			return null;
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
			i += 4;
			return true;
		case 'f':
			i += 5;
			return false;
		case 'n':
			i += 4;
			return null;
		default:
			return ParseNumber(s, ref i);
		}
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
		while (i < s.Length)
		{
			SkipWs(s, ref i);
			string key = ParseString(s, ref i);
			SkipWs(s, ref i);
			if (i < s.Length && s[i] == ':')
			{
				i++;
			}
			object value = ParseValue(s, ref i);
			dictionary[key] = value;
			SkipWs(s, ref i);
			if (i < s.Length && s[i] == ',')
			{
				i++;
				continue;
			}
			if (i < s.Length && s[i] == '}')
			{
				i++;
			}
			break;
		}
		return dictionary;
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
		while (i < s.Length)
		{
			list.Add(ParseValue(s, ref i));
			SkipWs(s, ref i);
			if (i < s.Length && s[i] == ',')
			{
				i++;
				continue;
			}
			if (i < s.Length && s[i] == ']')
			{
				i++;
			}
			break;
		}
		return list;
	}

	private static string ParseString(string s, ref int i)
	{
		StringBuilder stringBuilder = new StringBuilder();
		i++;
		while (i < s.Length)
		{
			char c = s[i++];
			if (c == '"')
			{
				break;
			}
			if (c == '\\' && i < s.Length)
			{
				char c2 = s[i++];
				switch (c2)
				{
				case 'n':
					stringBuilder.Append('\n');
					break;
				case 'r':
					stringBuilder.Append('\r');
					break;
				case 't':
					stringBuilder.Append('\t');
					break;
				case 'b':
					stringBuilder.Append('\b');
					break;
				case 'f':
					stringBuilder.Append('\f');
					break;
				case 'u':
					if (i + 4 <= s.Length)
					{
						stringBuilder.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
						i += 4;
					}
					break;
				default:
					stringBuilder.Append(c2);
					break;
				}
			}
			else
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString();
	}

	private static object ParseNumber(string s, ref int i)
	{
		int num = i;
		while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0)
		{
			i++;
		}
		string text = s.Substring(num, i - num);
		double result;
		if (text.Contains(".") || text.Contains("e") || text.Contains("E"))
		{
			return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ? result : 0.0;
		}
		long result2;
		return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result2) ? result2 : 0;
	}

	private static void SkipWs(string s, ref int i)
	{
		while (i < s.Length && char.IsWhiteSpace(s[i]))
		{
			i++;
		}
	}
}
