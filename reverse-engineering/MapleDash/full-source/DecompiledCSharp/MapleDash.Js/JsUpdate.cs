using System;

namespace MapleDash.Js;

public sealed class JsUpdate : JsNode
{
	public bool Increment;

	public bool Prefix;

	public JsNode Target;

	public override JsValue Eval(JsInterpreter it, JsScope scope)
	{
		JsNode target = Target;
		JsIdentifier id = target as JsIdentifier;
		JsValue v;
		Action<JsValue> action;
		if (id != null)
		{
			v = scope.Get(id.Name);
			action = (JsValue value) =>
			{
				scope.Set(id.Name, value);
			};
		}
		else
		{
			if (!(Target is JsMember jsMember))
			{
				throw new JsRuntimeException("Invalid update target");
			}
			JsValue host = jsMember.Object.Eval(it, scope);
			string key = jsMember.PropertyName(it, scope);
			v = it.GetMember(host, key);
			action = (JsValue value) =>
			{
				it.SetMember(host, key, value);
			};
		}
		double num = JsConvert.ToNumber(v);
		double v2 = (Increment ? (num + 1.0) : (num - 1.0));
		action(new JsNumber(v2));
		if (!Prefix)
		{
			return new JsNumber(num);
		}
		return new JsNumber(v2);
	}
}
