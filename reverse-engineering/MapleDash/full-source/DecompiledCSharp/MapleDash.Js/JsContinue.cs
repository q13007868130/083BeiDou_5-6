namespace MapleDash.Js;

public sealed class JsContinue : JsNode
{
	public override void Exec(JsInterpreter it, JsScope scope)
	{
		throw new JsContinueSignal(Label);
	}
}
