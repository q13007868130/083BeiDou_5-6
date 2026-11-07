using System.Collections.Generic;

public sealed class DialogContent
{
	public string Body = "";

	public readonly List<DialogRun> Runs = new List<DialogRun>();

	public readonly List<DialogSelection> Selections = new List<DialogSelection>();
}
