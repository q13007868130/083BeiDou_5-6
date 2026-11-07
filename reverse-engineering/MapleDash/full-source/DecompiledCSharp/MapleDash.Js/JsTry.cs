namespace MapleDash.Js;

public sealed class JsTry : JsNode
{
	public JsBlock TryBlock;

	public string CatchName;

	public JsBlock CatchBlock;

	public JsBlock FinallyBlock;

	public override void Exec(JsInterpreter it, JsScope scope)
	{
		try
		{
			TryBlock?.Exec(it, scope);
		}
		catch (JsRuntimeException ex)
		{
			if (CatchBlock == null)
			{
				throw;
			}
			JsScope jsScope = new JsScope(scope);
			if (!string.IsNullOrEmpty(CatchName))
			{
				jsScope.Declare(CatchName, ex.Value);
			}
			CatchBlock.Exec(it, jsScope);
		}
		finally
		{
			FinallyBlock?.Exec(it, scope);
		}
	}
}
