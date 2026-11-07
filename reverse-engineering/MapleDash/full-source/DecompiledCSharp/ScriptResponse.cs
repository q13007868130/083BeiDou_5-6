using MapleDash.Js;

public sealed class ScriptResponse
{
	public int sessionId;

	public bool hasStart;

	public bool hasAction;

	public bool hasEnd;

	public JsValue value;
}
