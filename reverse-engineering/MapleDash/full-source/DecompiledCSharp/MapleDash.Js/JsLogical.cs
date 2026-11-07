namespace MapleDash.Js;

public sealed class JsLogical : JsNode
{
	public string Op;

	public JsNode Left;

	public JsNode Right;

	public override JsValue Eval(JsInterpreter it, JsScope scope)
	{
		JsValue jsValue = Left.Eval(it, scope);
		if (Op == "??")
		{
			if (!(jsValue is JsNull) && !(jsValue is JsUndefined))
			{
				return jsValue;
			}
			return Right.Eval(it, scope);
		}
		if (Op == "&&")
		{
			if (!JsConvert.ToBoolean(jsValue))
			{
				return jsValue;
			}
			return Right.Eval(it, scope);
		}
		if (!JsConvert.ToBoolean(jsValue))
		{
			return Right.Eval(it, scope);
		}
		return jsValue;
	}
}
