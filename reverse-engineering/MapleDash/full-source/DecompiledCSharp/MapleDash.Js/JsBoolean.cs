namespace MapleDash.Js;

public sealed class JsBoolean : JsValue
{
	public static readonly JsBoolean True = new JsBoolean(v: true);

	public static readonly JsBoolean False = new JsBoolean(v: false);

	public readonly bool Value;

	private JsBoolean(bool v)
	{
		Value = v;
	}

	public static JsBoolean Of(bool v)
	{
		if (!v)
		{
			return False;
		}
		return True;
	}

	public override string ToString()
	{
		if (!Value)
		{
			return "false";
		}
		return "true";
	}
}
