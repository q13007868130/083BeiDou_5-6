using System.Collections.Generic;

namespace MapleDash.Js;

public sealed class JsProgram : JsNode
{
	public readonly List<JsNode> Body = new List<JsNode>();

	public void Run(JsInterpreter it, JsScope scope)
	{
		it.Hoist(Body, scope);
		for (int i = 0; i < Body.Count; i++)
		{
			Body[i].Exec(it, scope);
		}
	}

	public override void Exec(JsInterpreter it, JsScope scope)
	{
		Run(it, scope);
	}
}
