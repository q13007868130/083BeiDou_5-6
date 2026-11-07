namespace MapleDash.Js;

public sealed class JsBinary : JsNode
{
	public string Op;

	public JsNode Left;

	public JsNode Right;

	public override JsValue Eval(JsInterpreter it, JsScope scope)
	{
		return JsInterpreter.Binary(Op, Left.Eval(it, scope), Right.Eval(it, scope));
	}
}
