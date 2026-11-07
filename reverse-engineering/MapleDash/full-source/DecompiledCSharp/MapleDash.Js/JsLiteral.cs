namespace MapleDash.Js;

public sealed class JsLiteral : JsNode
{
	public JsValue Value;

	public override JsValue Eval(JsInterpreter it, JsScope scope)
	{
		return Value;
	}
}
