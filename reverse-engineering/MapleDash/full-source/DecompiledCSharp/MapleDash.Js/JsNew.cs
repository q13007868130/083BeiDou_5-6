using System.Collections.Generic;

namespace MapleDash.Js;

public sealed class JsNew : JsNode
{
	public JsNode Callee;

	public readonly List<JsNode> Args = new List<JsNode>();

	public override JsValue Eval(JsInterpreter it, JsScope scope)
	{
		JsValue fnValue = Callee.Eval(it, scope);
		JsValue[] args = JsPatterns.Arguments(Args, it, scope);
		JsObject jsObject = new JsObject();
		JsValue jsValue = it.Call(fnValue, jsObject, args);
		if (!(jsValue is JsObject) && !(jsValue is JsArray) && !(jsValue is JsHostObject) && !(jsValue is JsDate) && !(jsValue is JsRegExp))
		{
			return jsObject;
		}
		return jsValue;
	}
}
