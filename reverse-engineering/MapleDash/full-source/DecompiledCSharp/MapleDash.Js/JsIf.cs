namespace MapleDash.Js;

public sealed class JsIf : JsNode
{
	public JsNode Test;

	public JsNode Then;

	public JsNode Else;

	public override void Exec(JsInterpreter it, JsScope scope)
	{
		if (JsConvert.ToBoolean(Test.Eval(it, scope)))
		{
			Then?.Exec(it, scope);
		}
		else
		{
			Else?.Exec(it, scope);
		}
	}
}
