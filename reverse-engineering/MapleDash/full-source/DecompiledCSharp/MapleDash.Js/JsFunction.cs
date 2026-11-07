using System;

namespace MapleDash.Js;

public sealed class JsFunction : JsValue
{
	public string Name = "";

	public string[] Parameters = Array.Empty<string>();

	public JsNode[] Patterns;

	public bool Arrow;

	public JsBlock Body;

	public JsScope Closure;

	public Func<JsCall, JsValue> Native;

	public readonly JsObject Members = new JsObject();

	public Func<JsValue, bool> InstanceCheck;

	public bool IsNative => Native != null;

	public static JsFunction FromNative(string name, Func<JsCall, JsValue> fn)
	{
		return new JsFunction
		{
			Name = (name ?? ""),
			Native = fn
		};
	}

	public override string ToString()
	{
		return "function " + Name + "() { [native code] }";
	}
}
