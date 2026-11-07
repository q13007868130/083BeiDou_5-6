namespace MapleDash.Js;

public sealed class JsFunctionExpr : JsNode
{
	public string Name;

	public string[] Parameters;

	public JsBlock Body;

	public JsNode[] Patterns;

	public bool Arrow;

	public override JsValue Eval(JsInterpreter it, JsScope scope)
	{
		return new JsFunction
		{
			Name = (Name ?? ""),
			Parameters = Parameters,
			Patterns = Patterns,
			Arrow = Arrow,
			Body = Body,
			Closure = scope
		};
	}
}
