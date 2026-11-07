namespace MapleDash.Js;

public sealed class JsFunctionDecl : JsNode
{
	public string Name;

	public string[] Parameters;

	public JsNode[] Patterns;

	public JsBlock Body;

	public override void Exec(JsInterpreter it, JsScope scope)
	{
	}
}
