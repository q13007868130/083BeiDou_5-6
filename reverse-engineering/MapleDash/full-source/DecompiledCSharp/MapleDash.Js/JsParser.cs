using System;
using System.Collections.Generic;
using System.Text;

namespace MapleDash.Js;

public sealed class JsParser
{
	private readonly List<JsToken> tokens;

	private int i;

	private bool noIn;

	private JsNode[] parsedParameters;

	private JsToken Cur => tokens[i];

	private bool AtEnd => Cur.Kind == JsTokenKind.Eof;

	private bool IsIdentifier => Cur.Kind == JsTokenKind.Identifier;

	public JsParser(string source)
	{
		tokens = new JsLexer(source).Tokenize();
	}

	private JsToken Peek(int n)
	{
		return tokens[Math.Min(i + n, tokens.Count - 1)];
	}

	private bool Is(string text)
	{
		if (Cur.Text == text)
		{
			if (Cur.Kind != JsTokenKind.Punct)
			{
				return Cur.Kind == JsTokenKind.Keyword;
			}
			return true;
		}
		return false;
	}

	private bool IsKeyword(string text)
	{
		if (Cur.Kind == JsTokenKind.Keyword)
		{
			return Cur.Text == text;
		}
		return false;
	}

	private JsToken Take()
	{
		return tokens[i++];
	}

	private JsToken Expect(string text)
	{
		if (!Is(text))
		{
			throw Error("expected '" + text + "' but found '" + Cur.Text + "'");
		}
		return Take();
	}

	private JsRuntimeException Error(string message)
	{
		return new JsRuntimeException("SyntaxError: " + message + " (line " + Cur.Line + ")");
	}

	public JsProgram ParseProgram()
	{
		JsProgram jsProgram = new JsProgram();
		while (!AtEnd)
		{
			if (Is(";"))
			{
				Take();
			}
			else
			{
				jsProgram.Body.Add(ParseStatement());
			}
		}
		return jsProgram;
	}

	private JsNode ParseStatement()
	{
		int line = Cur.Line;
		int column = Cur.Column;
		JsNode jsNode2;
		if (IsIdentifier && Peek(1).Text == ":")
		{
			string text = Take().Text;
			Expect(":");
			JsNode jsNode = ParseStatement();
			jsNode.Label = text;
			jsNode2 = new JsLabel
			{
				Body = jsNode,
				Label = text
			};
		}
		else if (Is("{"))
		{
			jsNode2 = ParseBlock();
		}
		else if (IsKeyword("var") || IsVarLike())
		{
			jsNode2 = ParseVarStatement();
		}
		else if (IsKeyword("function"))
		{
			jsNode2 = ParseFunctionDecl();
		}
		else if (IsKeyword("if"))
		{
			jsNode2 = ParseIf();
		}
		else if (IsKeyword("for"))
		{
			jsNode2 = ParseFor();
		}
		else if (IsKeyword("while"))
		{
			jsNode2 = ParseWhile();
		}
		else if (IsKeyword("do"))
		{
			jsNode2 = ParseDoWhile();
		}
		else if (IsKeyword("return"))
		{
			Take();
			JsNode value = null;
			if (!Is(";") && !Is("}") && !AtEnd)
			{
				value = ParseExpression();
			}
			MatchSemicolon();
			jsNode2 = new JsReturn
			{
				Value = value
			};
		}
		else if (IsKeyword("break"))
		{
			Take();
			string label = ((IsIdentifier && Cur.Line == line) ? Take().Text : null);
			MatchSemicolon();
			jsNode2 = new JsBreak
			{
				Label = label
			};
		}
		else if (IsKeyword("continue"))
		{
			Take();
			string label2 = ((IsIdentifier && Cur.Line == line) ? Take().Text : null);
			MatchSemicolon();
			jsNode2 = new JsContinue
			{
				Label = label2
			};
		}
		else if (IsKeyword("throw"))
		{
			Take();
			jsNode2 = new JsThrow
			{
				Value = ParseExpression()
			};
			MatchSemicolon();
		}
		else if (IsKeyword("switch"))
		{
			jsNode2 = ParseSwitch();
		}
		else if (IsKeyword("try"))
		{
			jsNode2 = ParseTry();
		}
		else if (Is(";"))
		{
			Take();
			jsNode2 = new JsEmpty();
		}
		else
		{
			JsNode expr = ParseExpression();
			MatchSemicolon();
			jsNode2 = new JsExprStmt
			{
				Expr = expr
			};
		}
		jsNode2.Line = line;
		jsNode2.Column = column;
		return jsNode2;
	}

	private bool IsVarLike()
	{
		if (IsIdentifier && (Cur.Text == "let" || Cur.Text == "const"))
		{
			if (Peek(1).Kind != JsTokenKind.Identifier && !(Peek(1).Text == "{"))
			{
				return Peek(1).Text == "[";
			}
			return true;
		}
		return false;
	}

	private void MatchSemicolon()
	{
		if (Is(";"))
		{
			Take();
		}
	}

	private JsBlock ParseBlock()
	{
		JsBlock jsBlock = new JsBlock
		{
			Line = Cur.Line
		};
		Expect("{");
		while (!AtEnd && !Is("}"))
		{
			if (Is(";"))
			{
				Take();
			}
			else
			{
				jsBlock.Body.Add(ParseStatement());
			}
		}
		Expect("}");
		return jsBlock;
	}

	private JsNode ParseVarStatement()
	{
		JsVarDecl result = ParseVarDecl(consumeKeyword: true);
		MatchSemicolon();
		return result;
	}

	private JsVarDecl ParseVarDecl(bool consumeKeyword)
	{
		JsVarDecl jsVarDecl = new JsVarDecl
		{
			Line = Cur.Line
		};
		if (consumeKeyword)
		{
			Take();
		}
		do
		{
			JsNode jsNode = ParsePrimary();
			if (!(jsNode is JsIdentifier) && !(jsNode is JsArrayLiteral) && !(jsNode is JsObjectLiteral))
			{
				throw Error("invalid variable binding");
			}
			JsNode init = null;
			if (Is("="))
			{
				Take();
				init = ParseAssignment();
			}
			jsVarDecl.Decls.Add(new JsVarDeclarator
			{
				Name = (jsNode as JsIdentifier)?.Name,
				Pattern = ((jsNode is JsIdentifier) ? null : jsNode),
				Init = init
			});
		}
		while (Is(",") && Take().Text == ",");
		return jsVarDecl;
	}

	private JsFunctionDecl ParseFunctionDecl()
	{
		JsFunctionDecl jsFunctionDecl = new JsFunctionDecl
		{
			Line = Cur.Line
		};
		Expect("function");
		JsToken jsToken = Take();
		if (jsToken.Kind != JsTokenKind.Identifier)
		{
			throw Error("function name expected");
		}
		jsFunctionDecl.Name = jsToken.Text;
		jsFunctionDecl.Parameters = ParseParameters();
		jsFunctionDecl.Patterns = parsedParameters;
		jsFunctionDecl.Body = ParseBlock();
		return jsFunctionDecl;
	}

	private string[] ParseParameters()
	{
		List<JsNode> list = new List<JsNode>();
		Expect("(");
		while (!AtEnd && !Is(")"))
		{
			list.Add(ParseAssignment());
			if (!Is(","))
			{
				break;
			}
			Take();
		}
		Expect(")");
		parsedParameters = list.ToArray();
		return Array.Empty<string>();
	}

	private JsNode ParseIf()
	{
		Expect("if");
		Expect("(");
		JsNode test = ParseExpression();
		Expect(")");
		JsNode then = ParseStatement();
		JsNode jsNode = null;
		if (IsKeyword("else"))
		{
			Take();
			jsNode = ParseStatement();
		}
		return new JsIf
		{
			Test = test,
			Then = then,
			Else = jsNode
		};
	}

	private JsNode ParseFor()
	{
		Expect("for");
		Expect("(");
		JsNode init = null;
		bool flag = noIn;
		noIn = true;
		if (IsKeyword("var") || IsVarLike())
		{
			JsVarDecl jsVarDecl = ParseVarDecl(consumeKeyword: true);
			if (IsKeyword("in") || Cur.Text == "of")
			{
				bool values = Take().Text == "of";
				noIn = flag;
				JsNode objectExpr = ParseExpression();
				Expect(")");
				JsNode body = ParseStatement();
				return new JsForIn
				{
					VarName = jsVarDecl.Decls[0].Name,
					Pattern = jsVarDecl.Decls[0].Pattern,
					Values = values,
					ObjectExpr = objectExpr,
					Body = body
				};
			}
			init = jsVarDecl;
		}
		else if (!Is(";"))
		{
			JsNode jsNode = ParseExpression();
			if (IsKeyword("in") || Cur.Text == "of")
			{
				bool values2 = Take().Text == "of";
				noIn = flag;
				JsNode objectExpr2 = ParseExpression();
				Expect(")");
				JsNode body2 = ParseStatement();
				return new JsForIn
				{
					Target = jsNode,
					ObjectExpr = objectExpr2,
					Body = body2,
					Values = values2
				};
			}
			init = new JsExprStmt
			{
				Expr = jsNode
			};
		}
		noIn = flag;
		Expect(";");
		JsNode test = (Is(";") ? null : ParseExpression());
		Expect(";");
		JsNode update = (Is(")") ? null : ParseExpression());
		Expect(")");
		JsNode body3 = ParseStatement();
		return new JsFor
		{
			Init = init,
			Test = test,
			Update = update,
			Body = body3
		};
	}

	private JsNode ParseWhile()
	{
		Expect("while");
		Expect("(");
		JsNode test = ParseExpression();
		Expect(")");
		return new JsWhile
		{
			Test = test,
			Body = ParseStatement()
		};
	}

	private JsNode ParseDoWhile()
	{
		Expect("do");
		JsNode body = ParseStatement();
		Expect("while");
		Expect("(");
		JsNode test = ParseExpression();
		Expect(")");
		MatchSemicolon();
		return new JsDoWhile
		{
			Test = test,
			Body = body
		};
	}

	private JsNode ParseSwitch()
	{
		Expect("switch");
		Expect("(");
		JsNode discriminant = ParseExpression();
		Expect(")");
		Expect("{");
		JsSwitch jsSwitch = new JsSwitch
		{
			Discriminant = discriminant
		};
		JsSwitchCase jsSwitchCase = null;
		while (!AtEnd && !Is("}"))
		{
			if (IsKeyword("case"))
			{
				Take();
				JsNode test = ParseExpression();
				Expect(":");
				jsSwitchCase = new JsSwitchCase
				{
					Test = test
				};
				jsSwitch.Cases.Add(jsSwitchCase);
			}
			else if (IsKeyword("default"))
			{
				Take();
				Expect(":");
				jsSwitchCase = new JsSwitchCase
				{
					Test = null
				};
				jsSwitch.Cases.Add(jsSwitchCase);
			}
			else
			{
				if (jsSwitchCase == null)
				{
					throw Error("case expected");
				}
				if (Is(";"))
				{
					Take();
				}
				else
				{
					jsSwitchCase.Body.Add(ParseStatement());
				}
			}
		}
		Expect("}");
		return jsSwitch;
	}

	private JsNode ParseTry()
	{
		Expect("try");
		JsTry jsTry = new JsTry
		{
			TryBlock = ParseBlock()
		};
		if (IsKeyword("catch"))
		{
			Take();
			Expect("(");
			jsTry.CatchName = Take().Text;
			Expect(")");
			jsTry.CatchBlock = ParseBlock();
		}
		if (IsKeyword("finally"))
		{
			Take();
			jsTry.FinallyBlock = ParseBlock();
		}
		return jsTry;
	}

	public JsNode ParseExpression()
	{
		JsNode jsNode = ParseAssignment();
		if (!Is(","))
		{
			return jsNode;
		}
		JsSequence jsSequence = new JsSequence();
		jsSequence.Items.Add(jsNode);
		while (Is(","))
		{
			Take();
			jsSequence.Items.Add(ParseAssignment());
		}
		return jsSequence;
	}

	private JsNode ParseAssignment()
	{
		if (Is("..."))
		{
			Take();
			return new JsSpread
			{
				Value = ParseAssignment()
			};
		}
		if (IsArrow())
		{
			return ParseArrow();
		}
		JsNode jsNode = ParseConditional();
		string text = Cur.Text;
		if (Cur.Kind == JsTokenKind.Punct)
		{
			switch (text)
			{
			case "=":
			case "+=":
			case "-=":
			case "*=":
			case "/=":
			case "%=":
			case "<<=":
			case ">>=":
			case ">>>=":
			case "&=":
			case "|=":
			case "^=":
			{
				Take();
				JsNode value = ParseAssignment();
				return new JsAssign
				{
					Op = text,
					Target = jsNode,
					Value = value
				};
			}
			}
		}
		return jsNode;
	}

	private JsNode ParseConditional()
	{
		JsNode jsNode = ParseBinary(1);
		if (!Is("?"))
		{
			return jsNode;
		}
		Take();
		JsNode then = ParseAssignment();
		Expect(":");
		JsNode jsNode2 = ParseAssignment();
		return new JsConditional
		{
			Test = jsNode,
			Then = then,
			Else = jsNode2
		};
	}

	private static int Precedence(string op)
	{
		switch (op)
		{
		case "??":
		case "||":
			return 1;
		case "&&":
			return 2;
		case "|":
			return 3;
		case "^":
			return 4;
		case "&":
			return 5;
		case "==":
		case "!=":
		case "===":
		case "!==":
			return 6;
		case "<":
		case ">":
		case "<=":
		case ">=":
		case "instanceof":
		case "in":
			return 7;
		case "<<":
		case ">>":
		case ">>>":
			return 8;
		case "+":
		case "-":
			return 9;
		case "*":
		case "/":
		case "%":
			return 10;
		default:
			return -1;
		}
	}

	private JsNode ParseBinary(int minPrecedence)
	{
		JsNode jsNode = ParseUnary();
		while (true)
		{
			string text = Cur.Text;
			int num = ((Cur.Kind == JsTokenKind.Punct || (Cur.Kind == JsTokenKind.Keyword && (text == "instanceof" || text == "in"))) ? Precedence(text) : (-1));
			if (num < minPrecedence || num < 0 || (noIn && text == "in"))
			{
				break;
			}
			Take();
			JsNode right = ParseBinary(num + 1);
			object obj;
			switch (text)
			{
			default:
				obj = new JsBinary
				{
					Op = text,
					Left = jsNode,
					Right = right
				};
				break;
			case "&&":
			case "||":
			case "??":
				obj = new JsLogical
				{
					Op = text,
					Left = jsNode,
					Right = right
				};
				break;
			}
			jsNode = (JsNode)obj;
		}
		return jsNode;
	}

	private JsNode ParseUnary()
	{
		if (Cur.Kind == JsTokenKind.Keyword && (Cur.Text == "typeof" || Cur.Text == "void" || Cur.Text == "delete"))
		{
			string text = Take().Text;
			return new JsUnary
			{
				Op = text,
				Operand = ParseUnary()
			};
		}
		if (Cur.Kind == JsTokenKind.Punct && (Cur.Text == "!" || Cur.Text == "~" || Cur.Text == "+" || Cur.Text == "-"))
		{
			string text2 = Take().Text;
			return new JsUnary
			{
				Op = text2,
				Operand = ParseUnary()
			};
		}
		if (Cur.Kind == JsTokenKind.Punct && (Cur.Text == "++" || Cur.Text == "--"))
		{
			string text3 = Take().Text;
			return new JsUpdate
			{
				Increment = (text3 == "++"),
				Prefix = true,
				Target = ParseUnary()
			};
		}
		if (IsKeyword("new"))
		{
			return ParseNew();
		}
		return ParsePostfix();
	}

	private JsNode ParseNew()
	{
		Expect("new");
		JsNode jsNode = ParsePrimary();
		while (!AtEnd && (Is(".") || Is("[")))
		{
			if (Is("."))
			{
				Take();
				JsToken jsToken = Take();
				jsNode = new JsMember
				{
					Object = jsNode,
					Name = jsToken.Text
				};
			}
			else
			{
				Take();
				JsNode property = ParseExpression();
				Expect("]");
				jsNode = new JsMember
				{
					Object = jsNode,
					Property = property
				};
			}
		}
		JsNew jsNew = new JsNew
		{
			Callee = jsNode
		};
		if (Is("("))
		{
			Take();
			while (!AtEnd && !Is(")"))
			{
				jsNew.Args.Add(ParseAssignment());
				if (Is(","))
				{
					Take();
				}
			}
			Expect(")");
		}
		return ParsePostfixTail(jsNew);
	}

	private JsNode ParsePostfix()
	{
		JsNode expr = ParsePrimary();
		expr = ParsePostfixTail(expr);
		if (Cur.Kind == JsTokenKind.Punct && (Cur.Text == "++" || Cur.Text == "--"))
		{
			string text = Take().Text;
			return new JsUpdate
			{
				Increment = (text == "++"),
				Prefix = false,
				Target = expr
			};
		}
		return expr;
	}

	private JsNode ParsePostfixTail(JsNode expr)
	{
		while (!AtEnd)
		{
			if (Is(".") || Is("?."))
			{
				bool optional = Take().Text == "?.";
				JsToken jsToken = Take();
				expr = new JsMember
				{
					Object = expr,
					Name = jsToken.Text,
					Optional = optional
				};
				continue;
			}
			if (Is("["))
			{
				Take();
				JsNode property = ParseExpression();
				Expect("]");
				expr = new JsMember
				{
					Object = expr,
					Property = property
				};
				continue;
			}
			if (!Is("("))
			{
				break;
			}
			Take();
			JsCallExpr jsCallExpr = new JsCallExpr
			{
				Callee = expr
			};
			while (!AtEnd && !Is(")"))
			{
				jsCallExpr.Args.Add(ParseAssignment());
				if (Is(","))
				{
					Take();
				}
			}
			Expect(")");
			expr = jsCallExpr;
		}
		return expr;
	}

	private JsNode ParsePrimary()
	{
		JsToken cur = Cur;
		if (cur.Kind == JsTokenKind.Number)
		{
			Take();
			return new JsLiteral
			{
				Value = new JsNumber(cur.Number)
			};
		}
		if (cur.Kind == JsTokenKind.Regex)
		{
			Take();
			return new JsRegexLiteral
			{
				Pattern = cur.Text,
				Flags = cur.Flags
			};
		}
		if (cur.Kind == JsTokenKind.String)
		{
			Take();
			return new JsLiteral
			{
				Value = new JsString(cur.Text)
			};
		}
		if (cur.Kind == JsTokenKind.Template)
		{
			Take();
			return BuildTemplate(cur.Text);
		}
		if (cur.Kind == JsTokenKind.Keyword)
		{
			switch (cur.Text)
			{
			case "true":
				Take();
				return new JsLiteral
				{
					Value = JsBoolean.True
				};
			case "false":
				Take();
				return new JsLiteral
				{
					Value = JsBoolean.False
				};
			case "null":
				Take();
				return new JsLiteral
				{
					Value = JsNull.Value
				};
			case "this":
				Take();
				return new JsThisExpr();
			case "function":
				return ParseFunctionExpr();
			}
		}
		if (cur.Kind == JsTokenKind.Identifier)
		{
			Take();
			return new JsIdentifier
			{
				Name = cur.Text
			};
		}
		if (cur.Kind == JsTokenKind.Punct)
		{
			if (cur.Text == "(")
			{
				Take();
				JsNode result = ParseExpression();
				Expect(")");
				return result;
			}
			if (cur.Text == "[")
			{
				Take();
				JsArrayLiteral jsArrayLiteral = new JsArrayLiteral();
				while (!AtEnd && !Is("]"))
				{
					if (Is(","))
					{
						Take();
						jsArrayLiteral.Items.Add(null);
						continue;
					}
					jsArrayLiteral.Items.Add(ParseAssignment());
					if (Is(","))
					{
						Take();
					}
				}
				Expect("]");
				return jsArrayLiteral;
			}
			if (cur.Text == "{")
			{
				return ParseObjectLiteral();
			}
		}
		throw Error("unexpected token '" + cur.Text + "'");
	}

	private bool IsArrow()
	{
		if (IsIdentifier && Peek(1).Text == "=>")
		{
			return true;
		}
		if (!Is("("))
		{
			return false;
		}
		int num = 0;
		for (int i = this.i; i < tokens.Count; i++)
		{
			if (tokens[i].Kind != JsTokenKind.Punct)
			{
				continue;
			}
			if (tokens[i].Text == "(")
			{
				num++;
			}
			if (tokens[i].Text == ")" && --num == 0)
			{
				if (i + 1 < tokens.Count)
				{
					return tokens[i + 1].Text == "=>";
				}
				return false;
			}
		}
		return false;
	}

	private JsNode ParseArrow()
	{
		List<JsNode> list = new List<JsNode>();
		if (IsIdentifier)
		{
			list.Add(new JsIdentifier
			{
				Name = Take().Text
			});
		}
		else
		{
			Expect("(");
			while (!AtEnd && !Is(")"))
			{
				list.Add(ParseAssignment());
				if (!Is(","))
				{
					break;
				}
				Take();
			}
			Expect(")");
		}
		Expect("=>");
		JsBlock jsBlock;
		if (Is("{"))
		{
			jsBlock = ParseBlock();
		}
		else
		{
			jsBlock = new JsBlock();
			jsBlock.Body.Add(new JsReturn
			{
				Value = ParseAssignment()
			});
		}
		return new JsFunctionExpr
		{
			Patterns = list.ToArray(),
			Arrow = true,
			Body = jsBlock
		};
	}

	private JsNode ParseFunctionExpr()
	{
		Expect("function");
		string name = null;
		if (Cur.Kind == JsTokenKind.Identifier)
		{
			name = Take().Text;
		}
		string[] parameters = ParseParameters();
		JsNode[] patterns = parsedParameters;
		JsBlock body = ParseBlock();
		return new JsFunctionExpr
		{
			Name = name,
			Parameters = parameters,
			Patterns = patterns,
			Body = body
		};
	}

	private JsNode BuildTemplate(string raw)
	{
		JsNode jsNode = null;
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		while (num < raw.Length)
		{
			if (raw[num] == '$' && num + 1 < raw.Length && raw[num + 1] == '{')
			{
				jsNode = ConcatNode(jsNode, new JsLiteral
				{
					Value = new JsString(UnescapeTemplate(stringBuilder.ToString()))
				});
				stringBuilder.Length = 0;
				num += 2;
				int num2 = 1;
				int num3 = num;
				for (; num < raw.Length; num++)
				{
					if (num2 <= 0)
					{
						break;
					}
					if (raw[num] == '{')
					{
						num2++;
					}
					else if (raw[num] == '}')
					{
						num2--;
						if (num2 == 0)
						{
							break;
						}
					}
				}
				string source = raw.Substring(num3, num - num3);
				jsNode = ConcatNode(jsNode, new JsParser(source).ParseExpression());
				num++;
			}
			else
			{
				stringBuilder.Append(raw[num]);
				num++;
			}
		}
		if (stringBuilder.Length > 0 || jsNode == null)
		{
			jsNode = ConcatNode(jsNode, new JsLiteral
			{
				Value = new JsString(UnescapeTemplate(stringBuilder.ToString()))
			});
		}
		return jsNode;
	}

	private static JsNode ConcatNode(JsNode left, JsNode right)
	{
		if (left != null)
		{
			return new JsBinary
			{
				Op = "+",
				Left = left,
				Right = right
			};
		}
		return right;
	}

	private static string UnescapeTemplate(string s)
	{
		if (s.IndexOf('\\') < 0)
		{
			return s;
		}
		StringBuilder stringBuilder = new StringBuilder(s.Length);
		for (int i = 0; i < s.Length; i++)
		{
			if (s[i] != '\\' || i + 1 >= s.Length)
			{
				stringBuilder.Append(s[i]);
				continue;
			}
			char c = s[++i];
			switch (c)
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
			case '`':
				stringBuilder.Append('`');
				break;
			case '$':
				stringBuilder.Append('$');
				break;
			default:
				stringBuilder.Append(c);
				break;
			}
		}
		return stringBuilder.ToString();
	}

	private JsNode ParseObjectLiteral()
	{
		Expect("{");
		JsObjectLiteral jsObjectLiteral = new JsObjectLiteral();
		while (!AtEnd && !Is("}"))
		{
			if (Is(","))
			{
				Take();
				continue;
			}
			if (Is("..."))
			{
				Take();
				jsObjectLiteral.Props.Add(new KeyValuePair<string, JsNode>(null, new JsSpread
				{
					Value = ParseAssignment()
				}));
				if (Is(","))
				{
					Take();
				}
				continue;
			}
			JsToken jsToken = Take();
			string text;
			if (jsToken.Kind == JsTokenKind.String || jsToken.Kind == JsTokenKind.Number)
			{
				text = jsToken.Text;
			}
			else
			{
				if (jsToken.Kind != JsTokenKind.Identifier && jsToken.Kind != JsTokenKind.Keyword)
				{
					throw Error("invalid object key");
				}
				text = jsToken.Text;
			}
			JsNode value;
			if (Is(":"))
			{
				Take();
				value = ParseAssignment();
			}
			else
			{
				if (jsToken.Kind != JsTokenKind.Identifier)
				{
					throw Error("'=>' expected in object literal");
				}
				value = new JsIdentifier
				{
					Name = text
				};
			}
			jsObjectLiteral.Props.Add(new KeyValuePair<string, JsNode>(text, value));
			if (Is(","))
			{
				Take();
			}
		}
		Expect("}");
		return jsObjectLiteral;
	}
}
