using System;

namespace MapleDash.Js;

public sealed class JsContinueSignal : Exception
{
	public readonly string Label;

	public JsContinueSignal(string label = null)
	{
		Label = label;
	}
}
