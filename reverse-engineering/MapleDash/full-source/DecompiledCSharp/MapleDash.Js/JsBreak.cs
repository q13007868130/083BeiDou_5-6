namespace MapleDash.Js;

public sealed class JsBreak : JsNode
{
	public override void Exec(JsInterpreter it, JsScope scope)
	{
		throw new JsBreakSignal(Label);
	}
}
