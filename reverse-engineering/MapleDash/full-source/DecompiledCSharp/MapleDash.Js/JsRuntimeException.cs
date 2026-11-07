using System;

namespace MapleDash.Js;

public sealed class JsRuntimeException : Exception
{
	public readonly JsValue Value;

	public JsRuntimeException(string message, JsValue value = null)
		: base(message)
	{
		Value = value ?? new JsString(message);
	}
}
