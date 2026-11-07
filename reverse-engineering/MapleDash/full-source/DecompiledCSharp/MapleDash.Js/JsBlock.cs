using System.Collections.Generic;

namespace MapleDash.Js;

public sealed class JsBlock : JsNode
{
	public readonly List<JsNode> Body = new List<JsNode>();

	public override void Exec(JsInterpreter it, JsScope scope)
	{
		for (int i = 0; i < Body.Count; i++)
		{
			Body[i].Exec(it, scope);
		}
	}
}
