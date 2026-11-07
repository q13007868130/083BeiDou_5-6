using System;
using System.Collections.Generic;

namespace MapleDash.Js;

public sealed class JsScope
{
	private readonly Dictionary<string, JsValue> vars = new Dictionary<string, JsValue>(StringComparer.Ordinal);

	public readonly JsScope Parent;

	public readonly JsScope FunctionScope;

	public Func<string, JsValue> ResolveImport;

	public IEnumerable<string> OwnNames => vars.Keys;

	public JsScope(JsScope parent, bool functionBoundary = false)
	{
		Parent = parent;
		FunctionScope = ((functionBoundary || parent == null) ? this : parent.FunctionScope);
	}

	public bool HasOwn(string name)
	{
		return vars.ContainsKey(name);
	}

	public void Declare(string name, JsValue value)
	{
		vars[name] = value ?? JsUndefined.Value;
	}

	public JsValue Get(string name)
	{
		if (TryGet(name, out var value))
		{
			return value;
		}
		throw new JsRuntimeException("ReferenceError: " + name + " is not defined");
	}

	public bool TryGet(string name, out JsValue value)
	{
		for (JsScope jsScope = this; jsScope != null; jsScope = jsScope.Parent)
		{
			if (jsScope.vars.TryGetValue(name, out value))
			{
				return true;
			}
		}
		for (JsScope jsScope2 = this; jsScope2 != null; jsScope2 = jsScope2.Parent)
		{
			value = jsScope2.ResolveImport?.Invoke(name);
			if (value != null && !(value is JsUndefined))
			{
				return true;
			}
		}
		value = JsUndefined.Value;
		return false;
	}

	public void Set(string name, JsValue value)
	{
		value = value ?? JsNull.Value;
		for (JsScope jsScope = this; jsScope != null; jsScope = jsScope.Parent)
		{
			if (jsScope.vars.ContainsKey(name))
			{
				jsScope.vars[name] = value;
				return;
			}
		}
		JsScope jsScope2 = this;
		while (jsScope2.Parent != null)
		{
			jsScope2 = jsScope2.Parent;
		}
		jsScope2.vars[name] = value;
	}

	public void DeclareVar(string name, JsValue value)
	{
		FunctionScope.vars[name] = value ?? JsUndefined.Value;
	}
}
