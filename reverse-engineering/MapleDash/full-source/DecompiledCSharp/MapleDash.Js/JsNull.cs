namespace MapleDash.Js;

public sealed class JsNull : JsValue
{
	public static readonly JsNull Value = new JsNull();

	private JsNull()
	{
	}

	public override string ToString()
	{
		return "null";
	}
}
