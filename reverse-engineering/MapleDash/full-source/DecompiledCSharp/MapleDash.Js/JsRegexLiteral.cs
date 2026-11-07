namespace MapleDash.Js;

public sealed class JsRegexLiteral : JsNode
{
	public string Pattern;

	public string Flags;

	public override JsValue Eval(JsInterpreter it, JsScope scope)
	{
		return new JsRegExp(Pattern, Flags);
	}
}
