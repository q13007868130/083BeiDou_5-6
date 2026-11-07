namespace MapleDash.Js;

public sealed class JsIdentifier : JsNode
{
	public string Name;

	public override JsValue Eval(JsInterpreter it, JsScope scope)
	{
		return scope.Get(Name);
	}
}
