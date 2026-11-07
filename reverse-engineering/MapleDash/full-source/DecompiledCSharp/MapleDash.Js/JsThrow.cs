namespace MapleDash.Js;

public sealed class JsThrow : JsNode
{
	public JsNode Value;

	public override void Exec(JsInterpreter it, JsScope scope)
	{
		JsValue jsValue = Value.Eval(it, scope);
		throw new JsRuntimeException(JsConvert.ToString(jsValue), jsValue);
	}
}
