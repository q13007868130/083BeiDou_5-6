using System.Collections.Generic;

namespace MapleDash.Js;

public sealed class JsSequence : JsNode
{
	public readonly List<JsNode> Items = new List<JsNode>();

	public override JsValue Eval(JsInterpreter it, JsScope scope)
	{
		JsValue result = JsUndefined.Value;
		for (int i = 0; i < Items.Count; i++)
		{
			result = Items[i].Eval(it, scope);
		}
		return result;
	}
}
