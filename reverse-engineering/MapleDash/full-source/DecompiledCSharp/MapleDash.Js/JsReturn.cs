namespace MapleDash.Js;

public sealed class JsReturn : JsNode
{
	public JsNode Value;

	public override void Exec(JsInterpreter it, JsScope scope)
	{
		throw new JsReturnSignal((Value == null) ? JsUndefined.Value : Value.Eval(it, scope));
	}
}
