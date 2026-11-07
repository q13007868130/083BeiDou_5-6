namespace MapleDash.Js;

public abstract class JsNode
{
	public int Line;

	public int Column;

	public string Label;

	public virtual JsValue Eval(JsInterpreter it, JsScope scope)
	{
		throw new JsRuntimeException("Cannot evaluate statement as expression");
	}

	public virtual void Exec(JsInterpreter it, JsScope scope)
	{
		throw new JsRuntimeException("Cannot execute expression as statement");
	}
}
