using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MapleDash.Js;

public sealed class JsLexer
{
	private static readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.Ordinal)
	{
		"var", "function", "if", "else", "for", "while", "do", "return", "break", "continue",
		"new", "delete", "typeof", "instanceof", "in", "this", "null", "true", "false", "switch",
		"case", "default", "try", "catch", "finally", "throw", "void", "with"
	};

	private readonly string src;

	private int pos;

	private int line = 1;

	private int column = 1;

	private static readonly string[] Puncts3 = new string[6] { "...", "===", "!==", ">>>", "<<=", ">>=" };

	private static readonly string[] Puncts2 = new string[21]
	{
		"==", "!=", "<=", ">=", "&&", "||", "++", "--", "+=", "-=",
		"*=", "/=", "%=", "&=", "|=", "^=", "<<", ">>", "=>", "??",
		"?."
	};

	private char Cur
	{
		get
		{
			if (pos >= src.Length)
			{
				return '\0';
			}
			return src[pos];
		}
	}

	public JsLexer(string source)
	{
		src = source ?? "";
		if (src.Length > 0 && src[0] == '\ufeff')
		{
			pos = 1;
		}
	}

	private char Peek(int n)
	{
		if (pos + n >= src.Length)
		{
			return '\0';
		}
		return src[pos + n];
	}

	private void Advance()
	{
		if (pos < src.Length)
		{
			if (src[pos] == '\n')
			{
				line++;
				column = 1;
			}
			else
			{
				column++;
			}
			pos++;
		}
	}

	public List<JsToken> Tokenize()
	{
		List<JsToken> list = new List<JsToken>();
		JsToken item;
		do
		{
			SkipTrivia();
			bool flag = list.Count == 0 || ExpectsExpression(list[list.Count - 1]);
			item = (((Cur == '/' && Peek(1) != '=') & flag) ? ReadRegex() : Next());
			list.Add(item);
		}
		while (item.Kind != JsTokenKind.Eof);
		return list;
	}

	private static bool ExpectsExpression(JsToken token)
	{
		if (token.Kind == JsTokenKind.Keyword)
		{
			if (!(token.Text == "return") && !(token.Text == "throw") && !(token.Text == "case") && !(token.Text == "typeof") && !(token.Text == "void") && !(token.Text == "delete"))
			{
				return token.Text == "in";
			}
			return true;
		}
		if (token.Kind == JsTokenKind.Punct && token.Text != ")" && token.Text != "]" && token.Text != "}" && token.Text != "." && token.Text != "++")
		{
			return token.Text != "--";
		}
		return false;
	}

	private JsToken ReadRegex()
	{
		int num = line;
		int num2 = column;
		Advance();
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		bool flag2 = false;
		while (pos < src.Length && Cur != '\n' && Cur != '\r')
		{
			char cur = Cur;
			Advance();
			if (cur == '\\')
			{
				stringBuilder.Append(cur);
				if (pos < src.Length)
				{
					stringBuilder.Append(Cur);
					Advance();
				}
				continue;
			}
			if (cur == '[')
			{
				flag = true;
			}
			else if (cur == ']')
			{
				flag = false;
			}
			else if (cur == '/' && !flag)
			{
				flag2 = true;
				break;
			}
			stringBuilder.Append(cur);
		}
		if (!flag2)
		{
			throw new JsRuntimeException("SyntaxError: unterminated regular expression (line " + num + ")");
		}
		StringBuilder stringBuilder2 = new StringBuilder();
		while (char.IsLetter(Cur))
		{
			stringBuilder2.Append(Cur);
			Advance();
		}
		return new JsToken
		{
			Kind = JsTokenKind.Regex,
			Text = stringBuilder.ToString(),
			Flags = stringBuilder2.ToString(),
			Line = num,
			Column = num2
		};
	}

	private bool SkipTrivia()
	{
		bool result = false;
		while (pos < src.Length)
		{
			char cur = Cur;
			switch (cur)
			{
			case '\t':
			case '\n':
			case '\v':
			case '\f':
			case '\r':
			case ' ':
			case '\u00a0':
				Advance();
				result = true;
				continue;
			case '/':
				if (Peek(1) == '/')
				{
					while (pos < src.Length && Cur != '\n')
					{
						Advance();
					}
					result = true;
					continue;
				}
				break;
			}
			if (cur != '/' || Peek(1) != '*')
			{
				break;
			}
			Advance();
			Advance();
			while (pos < src.Length && (Cur != '*' || Peek(1) != '/'))
			{
				Advance();
			}
			if (pos < src.Length)
			{
				Advance();
				Advance();
			}
			result = true;
		}
		return result;
	}

	private JsToken Next()
	{
		SkipTrivia();
		int l = line;
		int c = column;
		if (pos >= src.Length)
		{
			return new JsToken
			{
				Kind = JsTokenKind.Eof,
				Text = "",
				Line = l,
				Column = c
			};
		}
		char cur = Cur;
		if (char.IsDigit(cur) || (cur == '.' && char.IsDigit(Peek(1))))
		{
			return ReadNumber(l, c);
		}
		switch (cur)
		{
		case '"':
		case '\'':
			return ReadString(l, c);
		case '`':
			return ReadTemplate(l, c);
		default:
			if (IsIdentifierStart(cur))
			{
				return ReadIdentifier(l, c);
			}
			return ReadPunct(l, c);
		}
	}

	private static bool IsIdentifierStart(char c)
	{
		if (!char.IsLetter(c) && c != '_')
		{
			return c == '$';
		}
		return true;
	}

	private static bool IsIdentifierPart(char c)
	{
		if (!char.IsLetterOrDigit(c) && c != '_')
		{
			return c == '$';
		}
		return true;
	}

	private JsToken ReadIdentifier(int l, int c0)
	{
		StringBuilder stringBuilder = new StringBuilder();
		while (pos < src.Length && IsIdentifierPart(Cur))
		{
			stringBuilder.Append(Cur);
			Advance();
		}
		string text = stringBuilder.ToString();
		return new JsToken
		{
			Kind = (Keywords.Contains(text) ? JsTokenKind.Keyword : JsTokenKind.Identifier),
			Text = text,
			Line = l,
			Column = c0
		};
	}

	private JsToken ReadNumber(int l, int c0)
	{
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		if (Cur == '0' && (Peek(1) == 'x' || Peek(1) == 'X'))
		{
			flag = true;
			stringBuilder.Append("0x");
			Advance();
			Advance();
			while (pos < src.Length && Uri.IsHexDigit(Cur))
			{
				stringBuilder.Append(Cur);
				Advance();
			}
		}
		else
		{
			while (pos < src.Length && (char.IsDigit(Cur) || Cur == '_'))
			{
				if (Cur != '_')
				{
					stringBuilder.Append(Cur);
				}
				Advance();
			}
			if (Cur == '.')
			{
				stringBuilder.Append('.');
				Advance();
				while (pos < src.Length && (char.IsDigit(Cur) || Cur == '_'))
				{
					if (Cur != '_')
					{
						stringBuilder.Append(Cur);
					}
					Advance();
				}
			}
			if (Cur == 'e' || Cur == 'E')
			{
				stringBuilder.Append('e');
				Advance();
				if (Cur == '+' || Cur == '-')
				{
					stringBuilder.Append(Cur);
					Advance();
				}
				while (pos < src.Length && (char.IsDigit(Cur) || Cur == '_'))
				{
					if (Cur != '_')
					{
						stringBuilder.Append(Cur);
					}
					Advance();
				}
			}
		}
		string text = stringBuilder.ToString();
		double result2;
		if (flag)
		{
			long.TryParse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result);
			result2 = result;
		}
		else
		{
			double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result2);
		}
		return new JsToken
		{
			Kind = JsTokenKind.Number,
			Text = text,
			Number = result2,
			Line = l,
			Column = c0
		};
	}

	private JsToken ReadString(int l, int c0)
	{
		char cur = Cur;
		Advance();
		StringBuilder stringBuilder = new StringBuilder();
		while (pos < src.Length)
		{
			char cur2 = Cur;
			if (cur2 == cur)
			{
				Advance();
				break;
			}
			switch (cur2)
			{
			case '\\':
			{
				Advance();
				if (pos >= src.Length)
				{
					break;
				}
				char cur3 = Cur;
				switch (cur3)
				{
				case 'n':
					stringBuilder.Append('\n');
					Advance();
					break;
				case 'r':
					stringBuilder.Append('\r');
					Advance();
					break;
				case 't':
					stringBuilder.Append('\t');
					Advance();
					break;
				case 'b':
					stringBuilder.Append('\b');
					Advance();
					break;
				case 'f':
					stringBuilder.Append('\f');
					Advance();
					break;
				case 'v':
					stringBuilder.Append('\v');
					Advance();
					break;
				case '0':
					stringBuilder.Append('\0');
					Advance();
					break;
				case 'x':
				{
					Advance();
					int num2 = 0;
					int j;
					for (j = 0; j < 2; j++)
					{
						if (pos >= src.Length)
						{
							break;
						}
						if (!Uri.IsHexDigit(Cur))
						{
							break;
						}
						num2 = num2 * 16 + HexVal(Cur);
						Advance();
					}
					if (j > 0)
					{
						stringBuilder.Append((char)num2);
					}
					break;
				}
				case 'u':
				{
					Advance();
					int num = 0;
					int i;
					for (i = 0; i < 4; i++)
					{
						if (pos >= src.Length)
						{
							break;
						}
						if (!Uri.IsHexDigit(Cur))
						{
							break;
						}
						num = num * 16 + HexVal(Cur);
						Advance();
					}
					if (i > 0)
					{
						stringBuilder.Append((char)num);
					}
					break;
				}
				case '\n':
					Advance();
					break;
				case '\r':
					Advance();
					if (Cur == '\n')
					{
						Advance();
					}
					break;
				default:
					stringBuilder.Append(cur3);
					Advance();
					break;
				}
				continue;
			}
			default:
				stringBuilder.Append(cur2);
				Advance();
				continue;
			case '\n':
				break;
			}
			break;
		}
		return new JsToken
		{
			Kind = JsTokenKind.String,
			Text = stringBuilder.ToString(),
			Line = l,
			Column = c0
		};
	}

	private JsToken ReadTemplate(int l, int c0)
	{
		Advance();
		StringBuilder stringBuilder = new StringBuilder();
		while (pos < src.Length)
		{
			char cur = Cur;
			if (cur == '`')
			{
				Advance();
				break;
			}
			switch (cur)
			{
			case '\\':
				stringBuilder.Append(cur);
				Advance();
				if (pos < src.Length)
				{
					stringBuilder.Append(Cur);
					Advance();
				}
				continue;
			case '$':
			{
				if (Peek(1) != '{')
				{
					break;
				}
				stringBuilder.Append("${");
				Advance();
				Advance();
				int num = 1;
				while (pos < src.Length && num > 0)
				{
					char cur2 = Cur;
					switch (cur2)
					{
					case '{':
						num++;
						break;
					case '}':
						num--;
						break;
					}
					if (num == 0)
					{
						Advance();
						break;
					}
					stringBuilder.Append(cur2);
					Advance();
				}
				stringBuilder.Append('}');
				continue;
			}
			}
			stringBuilder.Append(cur);
			Advance();
		}
		return new JsToken
		{
			Kind = JsTokenKind.Template,
			Text = stringBuilder.ToString(),
			Line = l,
			Column = c0
		};
	}

	private static int HexVal(char c)
	{
		if (c >= '0' && c <= '9')
		{
			return c - 48;
		}
		if (c >= 'a' && c <= 'f')
		{
			return c - 97 + 10;
		}
		if (c >= 'A' && c <= 'F')
		{
			return c - 65 + 10;
		}
		return 0;
	}

	private JsToken ReadPunct(int l, int c0)
	{
		for (int i = 0; i < Puncts3.Length; i++)
		{
			string text = Puncts3[i];
			if (Match(text))
			{
				for (int j = 0; j < text.Length; j++)
				{
					Advance();
				}
				return new JsToken
				{
					Kind = JsTokenKind.Punct,
					Text = text,
					Line = l,
					Column = c0
				};
			}
		}
		for (int k = 0; k < Puncts2.Length; k++)
		{
			string text2 = Puncts2[k];
			if (Match(text2))
			{
				for (int m = 0; m < text2.Length; m++)
				{
					Advance();
				}
				return new JsToken
				{
					Kind = JsTokenKind.Punct,
					Text = text2,
					Line = l,
					Column = c0
				};
			}
		}
		char cur = Cur;
		Advance();
		return new JsToken
		{
			Kind = JsTokenKind.Punct,
			Text = cur.ToString(),
			Line = l,
			Column = c0
		};
	}

	private bool Match(string s)
	{
		if (pos + s.Length > src.Length)
		{
			return false;
		}
		for (int i = 0; i < s.Length; i++)
		{
			if (src[pos + i] != s[i])
			{
				return false;
			}
		}
		return true;
	}
}
