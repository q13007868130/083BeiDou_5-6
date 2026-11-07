using System.Collections.Generic;

namespace MapleDash.Js;

public sealed class JsCallExpr : JsNode
{
	public JsNode Callee;

	public readonly List<JsNode> Args = new List<JsNode>();

	public override JsValue Eval(JsInterpreter it, JsScope scope)
	{
		JsValue jsValue = JsUndefined.Value;
		JsValue jsValue2;
		if (Callee is JsMember jsMember)
		{
			jsValue = jsMember.Object.Eval(it, scope);
			if (jsMember.Optional && (jsValue is JsNull || jsValue is JsUndefined))
			{
				return JsUndefined.Value;
			}
			jsValue2 = it.GetMember(jsValue, jsMember.PropertyName(it, scope));
		}
		else
		{
			if (Callee is JsIdentifier { Name: "eval" } && !scope.TryGet("eval", out var _))
			{
				if (Args.Count == 0)
				{
					return JsUndefined.Value;
				}
				JsValue jsValue3 = Args[0].Eval(it, scope);
				if (!(jsValue3 is JsString jsString))
				{
					return jsValue3;
				}
				JsProgram jsProgram = new JsParser(jsString.Value).ParseProgram();
				it.Hoist(jsProgram.Body, scope);
				JsValue result = JsUndefined.Value;
				{
					foreach (JsNode item in jsProgram.Body)
					{
						it.Step();
						if (item is JsExprStmt jsExprStmt)
						{
							result = jsExprStmt.Expr.Eval(it, scope);
							continue;
						}
						item.Exec(it, scope);
						result = JsUndefined.Value;
					}
					return result;
				}
			}
			jsValue2 = Callee.Eval(it, scope);
		}
		if (!(jsValue2 is JsFunction))
		{
			string text = ((Callee is JsMember jsMember2 && !string.IsNullOrEmpty(jsMember2.Name)) ? jsMember2.Name : "expression");
			throw new JsRuntimeException("TypeError: " + text + " is not a function (" + JsConvert.ToString(jsValue2) + ")");
		}
		JsValue[] args = JsPatterns.Arguments(Args, it, scope);
		return it.Call(jsValue2, jsValue, args);
	}
}
