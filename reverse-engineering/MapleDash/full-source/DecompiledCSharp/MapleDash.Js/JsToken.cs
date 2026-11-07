namespace MapleDash.Js;

public struct JsToken
{
	public JsTokenKind Kind;

	public string Text;

	public double Number;

	public string Flags;

	public int Line;

	public int Column;

	public override string ToString()
	{
		return Kind.ToString() + " '" + Text + "' @" + Line + ":" + Column;
	}
}
