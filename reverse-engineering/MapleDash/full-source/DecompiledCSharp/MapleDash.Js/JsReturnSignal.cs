using System;

namespace MapleDash.Js;

public sealed class JsReturnSignal : Exception
{
	public readonly JsValue Value;

	public JsReturnSignal(JsValue v)
	{
		Value = v;
	}
}
