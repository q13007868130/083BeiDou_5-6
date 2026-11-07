namespace MapleDash.Js;

public sealed class JsString : JsValue
{
	public readonly string Value;

	public JsString(string v)
	{
		Value = v ?? "";
	}

	public override string ToString()
	{
		return Value;
	}
}
