namespace MapleDash.Js;

public sealed class JsUnary : JsNode
{
	public string Op;

	public JsNode Operand;

	public override JsValue Eval(JsInterpreter it, JsScope scope)
	{
		if (Op == "typeof")
		{
			if (Operand is JsIdentifier jsIdentifier)
			{
				JsValue value;
				return new JsString(scope.TryGet(jsIdentifier.Name, out value) ? value.TypeName : "undefined");
			}
			return new JsString(Operand.Eval(it, scope).TypeName);
		}
		if (Op == "delete")
		{
			if (Operand is JsMember jsMember)
			{
				JsValue jsValue = jsMember.Object.Eval(it, scope);
				string key = jsMember.PropertyName(it, scope);
				if (jsValue is JsObject jsObject)
				{
					jsObject.Remove(key);
					return JsBoolean.True;
				}
				if (jsValue is JsArray jsArray)
				{
					jsArray.Props.Remove(key);
					return JsBoolean.True;
				}
			}
			return JsBoolean.False;
		}
		JsValue v = Operand.Eval(it, scope);
		return Op switch
		{
			"!" => (JsValue)JsBoolean.Of(!JsConvert.ToBoolean(v)), 
			"-" => new JsNumber(0.0 - JsConvert.ToNumber(v)), 
			"+" => new JsNumber(JsConvert.ToNumber(v)), 
			"~" => new JsNumber(~JsConvert.ToInt32(v)), 
			"void" => JsUndefined.Value, 
			_ => throw new JsRuntimeException("Unknown unary operator " + Op), 
		};
	}
}
