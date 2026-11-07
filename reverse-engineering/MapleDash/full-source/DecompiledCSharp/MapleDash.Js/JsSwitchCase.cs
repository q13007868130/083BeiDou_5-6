using System.Collections.Generic;

namespace MapleDash.Js;

public sealed class JsSwitchCase
{
	public JsNode Test;

	public readonly List<JsNode> Body = new List<JsNode>();
}
