namespace MapleDash.Js;

public sealed class JsExprStmt : JsNode
{
	public JsNode Expr;

	public override void Exec(JsInterpreter it, JsScope scope)
	{
		if (Expr != null)
		{
			Expr.Eval(it, scope);
		}
	}
}
