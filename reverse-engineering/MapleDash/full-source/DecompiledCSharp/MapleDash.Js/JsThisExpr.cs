namespace MapleDash.Js;

public sealed class JsThisExpr : JsNode
{
	public override JsValue Eval(JsInterpreter it, JsScope scope)
	{
		if (!scope.TryGet("this", out var value))
		{
			return JsUndefined.Value;
		}
		return value;
	}
}
