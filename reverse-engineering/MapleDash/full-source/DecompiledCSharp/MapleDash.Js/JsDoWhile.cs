namespace MapleDash.Js;

public sealed class JsDoWhile : JsNode
{
	public JsNode Test;

	public JsNode Body;

	public override void Exec(JsInterpreter it, JsScope scope)
	{
		do
		{
			it.Step();
			try
			{
				Body?.Exec(it, scope);
			}
			catch (JsBreakSignal jsBreakSignal) when (jsBreakSignal.Label == null || jsBreakSignal.Label == Label)
			{
				break;
			}
			catch (JsContinueSignal jsContinueSignal) when (jsContinueSignal.Label == null || jsContinueSignal.Label == Label)
			{
			}
		}
		while (JsConvert.ToBoolean(Test.Eval(it, scope)));
	}
}
