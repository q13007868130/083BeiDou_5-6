using System.Collections.Generic;

namespace MapleDash.Js;

public sealed class JsArrayLiteral : JsNode
{
	public readonly List<JsNode> Items = new List<JsNode>();

	public override JsValue Eval(JsInterpreter it, JsScope scope)
	{
		JsArray jsArray = new JsArray();
		for (int i = 0; i < Items.Count; i++)
		{
			if (Items[i] is JsSpread jsSpread)
			{
				jsArray.Items.AddRange(JsPatterns.Iterate(jsSpread.Value.Eval(it, scope)));
			}
			else
			{
				jsArray.Items.Add((Items[i] == null) ? JsUndefined.Value : Items[i].Eval(it, scope));
			}
		}
		return jsArray;
	}
}
