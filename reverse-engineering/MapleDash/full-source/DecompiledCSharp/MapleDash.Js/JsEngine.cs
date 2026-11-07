using System;

namespace MapleDash.Js;

public sealed class JsEngine
{
	public readonly JsInterpreter Interp;

	public readonly JsScope Scope;

	public readonly JsProgram Program;

	public readonly string Name;

	public JsEngine(string source, string name, JsInterpreter shared = null)
	{
		Name = name ?? "script";
		Interp = shared ?? new JsInterpreter();
		Scope = new JsScope(Interp.Global);
		Program = new JsParser(source ?? "").ParseProgram();
	}

	public bool HasFunction(string name)
	{
		if (Scope.TryGet(name, out var value))
		{
			return value is JsFunction;
		}
		return false;
	}

	public void Run(Action<JsScope> bindGlobals = null)
	{
		Interp.ResetSteps();
		bindGlobals?.Invoke(Scope);
		Program.Run(Interp, Scope);
	}

	public JsValue Call(string fn, params JsValue[] args)
	{
		Interp.ResetSteps();
		JsValue jsValue = Scope.Get(fn);
		if (!(jsValue is JsFunction))
		{
			throw new JsRuntimeException("Script " + Name + " has no function " + fn + "()");
		}
		return Interp.Call(jsValue, JsUndefined.Value, args);
	}

	public void Bind(string name, JsValue value)
	{
		Scope.Declare(name, value);
	}
}
