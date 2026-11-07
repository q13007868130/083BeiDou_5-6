using System;
using System.Collections.Generic;

namespace MapleDash.Js;

public sealed class JsInterpreter
{
	private readonly JsHostObject globalObject;

	private long steps;

	public long MaxSteps = 4000000L;

	private int depth;

	public int MaxDepth = 96;

	private readonly Random rng = new Random();

	public Action<string> Trace;

	public Func<string, JsValue> ResolveHostType;

	internal readonly Dictionary<string, JsValue> JavaTypes = new Dictionary<string, JsValue>(StringComparer.Ordinal);

	internal static Func<double> RandomSource;

	public static bool StrictNullMembers = true;

	public JsScope Global { get; }

	public JsInterpreter()
	{
		Global = new JsScope(null, functionBoundary: true);
		globalObject = new JsHostObject(Global);
		Global.Declare("this", globalObject);
		JsBuiltins.Install(this, Global);
	}

	public void Step()
	{
		if (++steps <= MaxSteps)
		{
			return;
		}
		throw new JsRuntimeException("Script exceeded execution budget (possible infinite loop)");
	}

	public void ResetSteps()
	{
		steps = 0L;
	}

	public double Random()
	{
		return RandomSource?.Invoke() ?? rng.NextDouble();
	}

	public void Hoist(List<JsNode> body, JsScope scope)
	{
		for (int i = 0; i < body.Count; i++)
		{
			HoistNode(body[i], scope);
		}
	}

	private void HoistNode(JsNode n, JsScope scope)
	{
		if (n == null)
		{
			return;
		}
		if (n is JsFunctionDecl jsFunctionDecl)
		{
			scope.DeclareVar(jsFunctionDecl.Name, new JsFunction
			{
				Name = jsFunctionDecl.Name,
				Parameters = jsFunctionDecl.Parameters,
				Patterns = jsFunctionDecl.Patterns,
				Body = jsFunctionDecl.Body,
				Closure = scope
			});
		}
		else if (n is JsVarDecl jsVarDecl)
		{
			for (int i = 0; i < jsVarDecl.Decls.Count; i++)
			{
				if (jsVarDecl.Decls[i].Pattern != null)
				{
					JsPatterns.Hoist(jsVarDecl.Decls[i].Pattern, scope);
				}
				else if (!scope.FunctionScope.HasOwn(jsVarDecl.Decls[i].Name))
				{
					scope.DeclareVar(jsVarDecl.Decls[i].Name, JsUndefined.Value);
				}
			}
		}
		else if (n is JsLabel jsLabel)
		{
			HoistNode(jsLabel.Body, scope);
		}
		else if (n is JsBlock jsBlock)
		{
			Hoist(jsBlock.Body, scope);
		}
		else if (n is JsIf jsIf)
		{
			HoistNode(jsIf.Then, scope);
			HoistNode(jsIf.Else, scope);
		}
		else if (n is JsFor jsFor)
		{
			HoistNode(jsFor.Init, scope);
			HoistNode(jsFor.Body, scope);
		}
		else if (n is JsForIn jsForIn)
		{
			if (jsForIn.Pattern != null)
			{
				JsPatterns.Hoist(jsForIn.Pattern, scope);
			}
			if (!string.IsNullOrEmpty(jsForIn.VarName) && !scope.FunctionScope.HasOwn(jsForIn.VarName))
			{
				scope.DeclareVar(jsForIn.VarName, JsUndefined.Value);
			}
			HoistNode(jsForIn.Body, scope);
		}
		else if (n is JsWhile jsWhile)
		{
			HoistNode(jsWhile.Body, scope);
		}
		else if (n is JsDoWhile jsDoWhile)
		{
			HoistNode(jsDoWhile.Body, scope);
		}
		else if (n is JsSwitch jsSwitch)
		{
			for (int j = 0; j < jsSwitch.Cases.Count; j++)
			{
				for (int k = 0; k < jsSwitch.Cases[j].Body.Count; k++)
				{
					HoistNode(jsSwitch.Cases[j].Body[k], scope);
				}
			}
		}
		else if (n is JsTry jsTry)
		{
			if (jsTry.TryBlock != null)
			{
				Hoist(jsTry.TryBlock.Body, scope);
			}
			if (jsTry.CatchBlock != null)
			{
				Hoist(jsTry.CatchBlock.Body, scope);
			}
			if (jsTry.FinallyBlock != null)
			{
				Hoist(jsTry.FinallyBlock.Body, scope);
			}
		}
	}

	public JsValue Call(JsValue fnValue, JsValue thisValue, JsValue[] args)
	{
		if (!(fnValue is JsFunction jsFunction))
		{
			throw new JsRuntimeException("TypeError: " + JsConvert.ToString(fnValue) + " is not a function");
		}
		if (jsFunction.IsNative)
		{
			return jsFunction.Native(new JsCall
			{
				This = thisValue,
				Args = (args ?? Array.Empty<JsValue>()),
				Interp = this
			});
		}
		if (++depth > MaxDepth)
		{
			depth--;
			throw new JsRuntimeException("RangeError: call stack exceeded in " + jsFunction.Name);
		}
		try
		{
			JsScope jsScope = new JsScope(jsFunction.Closure ?? Global, functionBoundary: true);
			JsScope jsScope2 = jsScope;
			JsValue value2;
			if (jsFunction.Arrow && jsFunction.Closure.TryGet("this", out var value))
			{
				value2 = value;
			}
			else
			{
				value2 = ((thisValue == null || thisValue is JsNull || thisValue is JsUndefined) ? globalObject : thisValue);
			}
			jsScope2.Declare("this", value2);
			if (jsFunction.Patterns != null)
			{
				for (int i = 0; i < jsFunction.Patterns.Length; i++)
				{
					JsPatterns.Bind(jsFunction.Patterns[i], (args != null && i < args.Length) ? args[i] : JsUndefined.Value, this, jsScope, declare: true);
				}
			}
			string[] array = jsFunction.Parameters ?? Array.Empty<string>();
			for (int j = 0; j < array.Length; j++)
			{
				jsScope.Declare(array[j], (args != null && j < args.Length) ? args[j] : JsUndefined.Value);
			}
			jsScope.Declare("arguments", new JsArray(args ?? Array.Empty<JsValue>()));
			if (jsFunction.Body != null)
			{
				Hoist(jsFunction.Body.Body, jsScope);
				jsFunction.Body.Exec(this, jsScope);
			}
			return JsUndefined.Value;
		}
		catch (JsReturnSignal jsReturnSignal)
		{
			return jsReturnSignal.Value;
		}
		finally
		{
			depth--;
		}
	}

	public JsValue CallGlobal(string name, params JsValue[] args)
	{
		JsValue fnValue = Global.Get(name);
		return Call(fnValue, JsUndefined.Value, args);
	}

	public JsValue GetMember(JsValue obj, string key)
	{
		if (obj == null)
		{
			obj = JsUndefined.Value;
		}
		if (obj == globalObject)
		{
			if (!Global.TryGet(key, out var value))
			{
				return JsUndefined.Value;
			}
			return value;
		}
		if (obj is JsDate jsDate)
		{
			return jsDate.Get(key);
		}
		if (obj is JsRegExp jsRegExp)
		{
			return jsRegExp.Get(key);
		}
		if (obj is JsHostObject jsHostObject)
		{
			return jsHostObject.Get(key);
		}
		if (obj is JsObject jsObject)
		{
			return jsObject.Get(key);
		}
		if (obj is JsArray jsArray)
		{
			if (key == "length")
			{
				return new JsNumber(jsArray.Items.Count);
			}
			if (int.TryParse(key, out var result))
			{
				return jsArray.GetIndex(result);
			}
			if (jsArray.Props.TryGetValue(key, out var value2))
			{
				return value2;
			}
			if (JsBuiltins.HasArrayMethod(key))
			{
				return JsBuiltins.ArrayMethod(this, key);
			}
			return JsUndefined.Value;
		}
		if (obj is JsString jsString)
		{
			if (key == "length")
			{
				return new JsNumber(jsString.Value.Length);
			}
			if (int.TryParse(key, out var result2))
			{
				if (result2 < 0 || result2 >= jsString.Value.Length)
				{
					return JsUndefined.Value;
				}
				return new JsString(jsString.Value[result2].ToString());
			}
			if (JsBuiltins.HasStringMethod(key))
			{
				return JsBuiltins.StringMethod(this, key);
			}
			return JsUndefined.Value;
		}
		if (obj is JsNumber)
		{
			if (JsBuiltins.HasNumberMethod(key))
			{
				return JsBuiltins.NumberMethod(this, key);
			}
			return JsUndefined.Value;
		}
		if (obj is JsBoolean)
		{
			if (key == "toString")
			{
				return JsFunction.FromNative("toString", (JsCall c) => new JsString(JsConvert.ToString(c.This)));
			}
			return JsUndefined.Value;
		}
		if (obj is JsFunction jsFunction)
		{
			if (jsFunction.Members.Has(key))
			{
				return jsFunction.Members.Get(key);
			}
			return key switch
			{
				"call" => (JsValue)JsFunction.FromNative("call", (JsCall c) =>
				{
					JsValue[] array = new JsValue[Math.Max(0, c.ArgCount - 1)];
					for (int i = 1; i < c.ArgCount; i++)
					{
						array[i - 1] = c.Arg(i);
					}
					return Call(c.This, c.Arg(0), array);
				}), 
				"apply" => JsFunction.FromNative("apply", (JsCall c) =>
				{
					JsValue jsValue = c.Arg(1);
					List<JsValue> list = new List<JsValue>();
					if (jsValue is JsArray jsArray2)
					{
						list.AddRange(jsArray2.Items);
					}
					return Call(c.This, c.Arg(0), list.ToArray());
				}), 
				"length" => new JsNumber((jsFunction.Parameters != null) ? jsFunction.Parameters.Length : 0), 
				_ => JsUndefined.Value, 
			};
		}
		if (obj is JsNull)
		{
			if (StrictNullMembers)
			{
				throw new JsRuntimeException("TypeError: cannot read property '" + key + "' of null");
			}
			return NullMember(key);
		}
		if (obj is JsUndefined)
		{
			if (StrictNullMembers)
			{
				throw new JsRuntimeException("TypeError: cannot read property '" + key + "' of undefined");
			}
			return NullMember(key);
		}
		return JsUndefined.Value;
	}

	private static JsValue NullMember(string key)
	{
		return key switch
		{
			"length" => (JsValue)new JsNumber(0.0), 
			"size" => JsFunction.FromNative(key, (JsCall c) => new JsNumber(0.0)), 
			"isEmpty" => JsFunction.FromNative(key, (JsCall c) => JsBoolean.Of(v: true)), 
			"toString" => JsFunction.FromNative(key, (JsCall c) => new JsString("null")), 
			_ => JsFunction.FromNative(key, (JsCall c) => JsNull.Value), 
		};
	}

	public void SetMember(JsValue obj, string key, JsValue value)
	{
		value = value ?? JsNull.Value;
		if (obj == globalObject)
		{
			Global.Set(key, value);
		}
		else if (obj is JsRegExp jsRegExp && key == "lastIndex")
		{
			jsRegExp.SetLastIndex(value);
		}
		else if (obj is JsHostObject jsHostObject)
		{
			jsHostObject.Set(key, value);
		}
		else if (obj is JsObject jsObject)
		{
			jsObject.Set(key, value);
		}
		else if (obj is JsArray jsArray)
		{
			jsArray.Set(key, value);
		}
		else if ((obj is JsNull || obj is JsUndefined) && StrictNullMembers)
		{
			throw new JsRuntimeException("TypeError: cannot set property '" + key + "' of " + ((obj is JsNull) ? "null" : "undefined"));
		}
	}

	public static JsValue Binary(string op, JsValue l, JsValue r)
	{
		switch (op)
		{
		case "+":
			if (l is JsString || r is JsString)
			{
				return new JsString(JsConvert.ToString(l) + JsConvert.ToString(r));
			}
			return new JsNumber(JsConvert.ToNumber(l) + JsConvert.ToNumber(r));
		case "-":
			return new JsNumber(JsConvert.ToNumber(l) - JsConvert.ToNumber(r));
		case "*":
			return new JsNumber(JsConvert.ToNumber(l) * JsConvert.ToNumber(r));
		case "/":
			return new JsNumber(JsConvert.ToNumber(l) / JsConvert.ToNumber(r));
		case "%":
			return new JsNumber(JsConvert.ToNumber(l) % JsConvert.ToNumber(r));
		case "==":
			return JsBoolean.Of(JsConvert.LooseEquals(l, r));
		case "!=":
			return JsBoolean.Of(!JsConvert.LooseEquals(l, r));
		case "===":
			return JsBoolean.Of(JsConvert.StrictEquals(l, r));
		case "!==":
			return JsBoolean.Of(!JsConvert.StrictEquals(l, r));
		case "<":
			return JsBoolean.Of(Relational(op, l, r));
		case ">":
			return JsBoolean.Of(Relational(op, l, r));
		case "<=":
			return JsBoolean.Of(Relational(op, l, r));
		case ">=":
			return JsBoolean.Of(Relational(op, l, r));
		case "&":
			return new JsNumber(JsConvert.ToInt32(l) & JsConvert.ToInt32(r));
		case "|":
			return new JsNumber(JsConvert.ToInt32(l) | JsConvert.ToInt32(r));
		case "^":
			return new JsNumber(JsConvert.ToInt32(l) ^ JsConvert.ToInt32(r));
		case "<<":
			return new JsNumber(JsConvert.ToInt32(l) << (JsConvert.ToInt32(r) & 0x1F));
		case ">>":
			return new JsNumber(JsConvert.ToInt32(l) >> (JsConvert.ToInt32(r) & 0x1F));
		case ">>>":
			return new JsNumber((uint)JsConvert.ToInt32(l) >> (JsConvert.ToInt32(r) & 0x1F));
		case "in":
		{
			string text = JsConvert.ToString(l);
			if (r is JsObject jsObject)
			{
				return JsBoolean.Of(jsObject.Has(text));
			}
			if (r is JsArray jsArray)
			{
				int result;
				return JsBoolean.Of(text == "length" || (int.TryParse(text, out result) && result >= 0 && result < jsArray.Items.Count) || jsArray.Props.ContainsKey(text));
			}
			if (r is JsHostObject jsHostObject)
			{
				return JsBoolean.Of(jsHostObject.Members.Has(text));
			}
			return JsBoolean.False;
		}
		case "instanceof":
			return JsBoolean.Of(((r as JsFunction) ?? throw new JsRuntimeException("Right operand of instanceof is not a constructor")).InstanceCheck?.Invoke(l) ?? false);
		default:
			throw new JsRuntimeException("Unknown operator " + op);
		}
	}

	private static bool Relational(string op, JsValue l, JsValue r)
	{
		if (l is JsString jsString && r is JsString jsString2)
		{
			int num = string.CompareOrdinal(jsString.Value, jsString2.Value);
			return op switch
			{
				"<" => num < 0, 
				">" => num > 0, 
				"<=" => num <= 0, 
				_ => num >= 0, 
			};
		}
		double num2 = JsConvert.ToNumber(l);
		double num3 = JsConvert.ToNumber(r);
		if (double.IsNaN(num2) || double.IsNaN(num3))
		{
			return false;
		}
		return op switch
		{
			"<" => num2 < num3, 
			">" => num2 > num3, 
			"<=" => num2 <= num3, 
			_ => num2 >= num3, 
		};
	}
}
