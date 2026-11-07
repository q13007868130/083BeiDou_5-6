using System.Collections.Generic;

namespace MapleDash.Js;

public static class JsPatterns
{
	public static IEnumerable<string> Keys(JsValue value)
	{
		if (value is JsObject jsObject)
		{
			return new List<string>(jsObject.Props.Keys);
		}
		if (value is JsHostObject jsHostObject)
		{
			return new List<string>(jsHostObject.Members.Props.Keys);
		}
		List<string> list = new List<string>();
		int num = ((value is JsArray jsArray) ? jsArray.Items.Count : ((value is JsString jsString) ? jsString.Value.Length : 0));
		for (int i = 0; i < num; i++)
		{
			list.Add(i.ToString());
		}
		return list;
	}

	public static IEnumerable<JsValue> Iterate(JsValue value)
	{
		if (value is JsArray jsArray)
		{
			return new List<JsValue>(jsArray.Items);
		}
		if (value is JsString jsString)
		{
			List<JsValue> list = new List<JsValue>();
			for (int i = 0; i < jsString.Value.Length; i++)
			{
				list.Add(new JsString(jsString.Value[i].ToString()));
			}
			return list;
		}
		throw new JsRuntimeException("TypeError: value is not iterable");
	}

	public static JsValue[] Arguments(List<JsNode> nodes, JsInterpreter it, JsScope scope)
	{
		List<JsValue> list = new List<JsValue>();
		foreach (JsNode node in nodes)
		{
			if (node is JsSpread jsSpread)
			{
				list.AddRange(Iterate(jsSpread.Value.Eval(it, scope)));
			}
			else
			{
				list.Add(node.Eval(it, scope));
			}
		}
		return list.ToArray();
	}

	public static void Hoist(JsNode node, JsScope scope)
	{
		if (node is JsIdentifier jsIdentifier)
		{
			if (!scope.FunctionScope.HasOwn(jsIdentifier.Name))
			{
				scope.DeclareVar(jsIdentifier.Name, JsUndefined.Value);
			}
			return;
		}
		if (node is JsArrayLiteral jsArrayLiteral)
		{
			{
				foreach (JsNode item in jsArrayLiteral.Items)
				{
					Hoist(item, scope);
				}
				return;
			}
		}
		if (node is JsObjectLiteral jsObjectLiteral)
		{
			{
				foreach (KeyValuePair<string, JsNode> prop in jsObjectLiteral.Props)
				{
					Hoist(prop.Value, scope);
				}
				return;
			}
		}
		if (node is JsAssign jsAssign)
		{
			Hoist(jsAssign.Target, scope);
		}
	}

	public static void Bind(JsNode pattern, JsValue value, JsInterpreter it, JsScope scope, bool declare)
	{
		if (pattern == null)
		{
			return;
		}
		if (pattern is JsIdentifier jsIdentifier)
		{
			if (declare)
			{
				scope.DeclareVar(jsIdentifier.Name, value);
			}
			else
			{
				scope.Set(jsIdentifier.Name, value);
			}
			return;
		}
		if (pattern is JsAssign { Target: var target } jsAssign)
		{
			Bind(target, (value is JsUndefined) ? jsAssign.Value.Eval(it, scope) : value, it, scope, declare);
			return;
		}
		if (pattern is JsMember jsMember)
		{
			it.SetMember(jsMember.Object.Eval(it, scope), jsMember.PropertyName(it, scope), value);
			return;
		}
		if (pattern is JsArrayLiteral jsArrayLiteral)
		{
			List<JsValue> list = new List<JsValue>(Iterate(value));
			for (int i = 0; i < jsArrayLiteral.Items.Count; i++)
			{
				Bind(jsArrayLiteral.Items[i], (i < list.Count) ? list[i] : JsUndefined.Value, it, scope, declare);
			}
			return;
		}
		if (pattern is JsObjectLiteral jsObjectLiteral)
		{
			if (value is JsNull || value is JsUndefined)
			{
				throw new JsRuntimeException("TypeError: cannot destructure null or undefined");
			}
			{
				foreach (KeyValuePair<string, JsNode> prop in jsObjectLiteral.Props)
				{
					Bind(prop.Value, it.GetMember(value, prop.Key), it, scope, declare);
				}
				return;
			}
		}
		throw new JsRuntimeException("SyntaxError: invalid binding pattern");
	}
}
