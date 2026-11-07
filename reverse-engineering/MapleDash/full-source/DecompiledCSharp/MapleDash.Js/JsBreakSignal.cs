using System;

namespace MapleDash.Js;

public sealed class JsBreakSignal : Exception
{
	public readonly string Label;

	public JsBreakSignal(string label = null)
	{
		Label = label;
	}
}
