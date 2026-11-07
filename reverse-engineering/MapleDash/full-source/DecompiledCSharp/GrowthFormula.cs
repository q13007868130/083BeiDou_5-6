using System;

public static class GrowthFormula
{
	public static int Evaluate(string formula, int x)
	{
		int i = 0;
		return (int)Math.Floor(ParseExpr(formula, ref i, x) + 1E-09);
	}

	private static double ParseExpr(string s, ref int i, int x)
	{
		double num = ParseTerm(s, ref i, x);
		while (i < s.Length)
		{
			switch (s[i])
			{
			case '+':
				i++;
				num += ParseTerm(s, ref i, x);
				continue;
			case '-':
				i++;
				num -= ParseTerm(s, ref i, x);
				continue;
			}
			break;
		}
		return num;
	}

	private static double ParseTerm(string s, ref int i, int x)
	{
		double num = ParseFactor(s, ref i, x);
		while (i < s.Length)
		{
			switch (s[i])
			{
			case '*':
				i++;
				num *= ParseFactor(s, ref i, x);
				continue;
			case '/':
				i++;
				num /= ParseFactor(s, ref i, x);
				continue;
			}
			break;
		}
		return num;
	}

	private static double ParseFactor(string s, ref int i, int x)
	{
		while (i < s.Length && s[i] == ' ')
		{
			i++;
		}
		if (i >= s.Length)
		{
			return 0.0;
		}
		char c = s[i];
		switch (c)
		{
		case '(':
		{
			i++;
			double result = ParseExpr(s, ref i, x);
			if (i < s.Length && s[i] == ')')
			{
				i++;
			}
			return result;
		}
		case '-':
			i++;
			return 0.0 - ParseFactor(s, ref i, x);
		case 'X':
		case 'x':
			i++;
			return x;
		case 'D':
		case 'U':
		case 'd':
		case 'u':
		{
			bool flag = c == 'u' || c == 'U';
			i++;
			while (i < s.Length && s[i] == ' ')
			{
				i++;
			}
			if (i >= s.Length || s[i] != '(')
			{
				return 0.0;
			}
			i++;
			double num2 = ParseExpr(s, ref i, x);
			if (i < s.Length && s[i] == ')')
			{
				i++;
			}
			if (!flag)
			{
				return Math.Floor(num2);
			}
			return Math.Ceiling(num2);
		}
		default:
			if (char.IsDigit(c))
			{
				int num = i;
				while (i < s.Length && char.IsDigit(s[i]))
				{
					i++;
				}
				return double.Parse(s.Substring(num, i - num));
			}
			i++;
			return 0.0;
		}
	}
}
