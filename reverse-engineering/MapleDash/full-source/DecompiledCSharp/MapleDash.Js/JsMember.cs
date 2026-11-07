namespace MapleDash.Js;

public sealed class JsMember : JsNode
{
	public JsNode Object;

	public JsNode Property;

	public string Name;

	public bool Optional;

	public string PropertyName(JsInterpreter it, JsScope scope)
	{
		return Name ?? JsConvert.ToString(Property.Eval(it, scope));
	}

	public override JsValue Eval(JsInterpreter it, JsScope scope)
	{
		JsValue jsValue = Object.Eval(it, scope);
		if (Optional && (jsValue is JsNull || jsValue is JsUndefined))
		{
			return JsUndefined.Value;
		}
		return it.GetMember(jsValue, PropertyName(it, scope));
	}
}
