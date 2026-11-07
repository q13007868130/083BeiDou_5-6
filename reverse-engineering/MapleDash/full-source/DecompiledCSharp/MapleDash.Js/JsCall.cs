namespace MapleDash.Js;

public struct JsCall
{
	public JsValue This;

	public JsValue[] Args;

	public JsInterpreter Interp;

	public int ArgCount
	{
		get
		{
			if (Args != null)
			{
				return Args.Length;
			}
			return 0;
		}
	}

	public JsValue Arg(int i)
	{
		if (Args == null || i >= Args.Length)
		{
			return JsUndefined.Value;
		}
		return Args[i];
	}
}
