namespace MapleDash.Js;

public sealed class JsAssign : JsNode
{
	public string Op;

	public JsNode Target;

	public JsNode Value;

	public override JsValue Eval(JsInterpreter it, JsScope scope)
	{
		JsValue jsValue;
		if (Op == "=")
		{
			jsValue = Value.Eval(it, scope);
		}
		else
		{
			JsValue l = Target.Eval(it, scope);
			JsValue r = Value.Eval(it, scope);
			jsValue = JsInterpreter.Binary(Op.Substring(0, Op.Length - 1), l, r);
		}
		if (Target is JsArrayLiteral || Target is JsObjectLiteral)
		{
			JsPatterns.Bind(Target, jsValue, it, scope, declare: false);
		}
		else if (Target is JsIdentifier jsIdentifier)
		{
			scope.Set(jsIdentifier.Name, jsValue);
		}
		else
		{
			if (!(Target is JsMember jsMember))
			{
				throw new JsRuntimeException("Invalid assignment target");
			}
			JsValue obj = jsMember.Object.Eval(it, scope);
			it.SetMember(obj, jsMember.PropertyName(it, scope), jsValue);
		}
		return jsValue;
	}
}
