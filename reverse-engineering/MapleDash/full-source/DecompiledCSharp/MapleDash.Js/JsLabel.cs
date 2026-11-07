namespace MapleDash.Js;

public sealed class JsLabel : JsNode
{
	public JsNode Body;

	public override void Exec(JsInterpreter it, JsScope scope)
	{
		try
		{
			Body.Exec(it, scope);
		}
		catch (JsBreakSignal jsBreakSignal) when (jsBreakSignal.Label == Label)
		{
		}
	}
}
