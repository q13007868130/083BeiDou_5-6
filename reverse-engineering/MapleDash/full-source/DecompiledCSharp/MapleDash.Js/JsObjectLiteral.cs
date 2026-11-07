using System.Collections.Generic;

namespace MapleDash.Js;

public sealed class JsObjectLiteral : JsNode
{
	public readonly List<KeyValuePair<string, JsNode>> Props = new List<KeyValuePair<string, JsNode>>();

	public override JsValue Eval(JsInterpreter it, JsScope scope)
	{
		JsObject jsObject = new JsObject();
		for (int i = 0; i < Props.Count; i++)
		{
			if (Props[i].Value is JsSpread jsSpread)
			{
				JsValue jsValue = jsSpread.Value.Eval(it, scope);
				foreach (string item in JsPatterns.Keys(jsValue))
				{
					jsObject.Set(item, it.GetMember(jsValue, item));
				}
			}
			else
			{
				jsObject.Set(Props[i].Key, Props[i].Value.Eval(it, scope));
			}
		}
		return jsObject;
	}
}
