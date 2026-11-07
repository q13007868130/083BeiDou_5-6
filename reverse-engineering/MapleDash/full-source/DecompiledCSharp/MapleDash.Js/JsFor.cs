namespace MapleDash.Js;

public sealed class JsFor : JsNode
{
	public JsNode Init;

	public JsNode Test;

	public JsNode Update;

	public JsNode Body;

	public override void Exec(JsInterpreter it, JsScope scope)
	{
		if (Init != null)
		{
			if (Init is JsVarDecl || Init is JsExprStmt || Init is JsBlock)
			{
				Init.Exec(it, scope);
			}
			else
			{
				Init.Eval(it, scope);
			}
		}
		while (Test == null || JsConvert.ToBoolean(Test.Eval(it, scope)))
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
			Update?.Eval(it, scope);
		}
	}
}
