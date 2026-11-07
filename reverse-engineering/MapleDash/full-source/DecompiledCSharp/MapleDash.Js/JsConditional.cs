namespace MapleDash.Js;

public sealed class JsConditional : JsNode
{
	public JsNode Test;

	public JsNode Then;

	public JsNode Else;

	public override JsValue Eval(JsInterpreter it, JsScope scope)
	{
		if (!JsConvert.ToBoolean(Test.Eval(it, scope)))
		{
			return Else.Eval(it, scope);
		}
		return Then.Eval(it, scope);
	}
}
