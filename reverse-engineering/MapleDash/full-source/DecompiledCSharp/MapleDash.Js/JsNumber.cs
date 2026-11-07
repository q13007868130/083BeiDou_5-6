namespace MapleDash.Js;

public sealed class JsNumber : JsValue
{
	public readonly double Value;

	public JsNumber(double v)
	{
		Value = v;
	}

	public override string ToString()
	{
		return JsConvert.NumberToString(Value);
	}
}
