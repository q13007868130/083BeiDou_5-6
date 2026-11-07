namespace MapleDash.Js;

public sealed class JsUndefined : JsValue
{
	public static readonly JsUndefined Value = new JsUndefined();

	private JsUndefined()
	{
	}

	public override string ToString()
	{
		return "undefined";
	}
}
