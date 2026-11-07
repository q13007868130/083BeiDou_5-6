namespace MapleDash.Js;

public abstract class JsValue
{
	public virtual string TypeName
	{
		get
		{
			if (this is JsUndefined)
			{
				return "undefined";
			}
			if (this is JsNull)
			{
				return "null";
			}
			if (this is JsNumber)
			{
				return "number";
			}
			if (this is JsString)
			{
				return "string";
			}
			if (this is JsBoolean)
			{
				return "boolean";
			}
			if (this is JsFunction)
			{
				return "function";
			}
			return "object";
		}
	}
}
