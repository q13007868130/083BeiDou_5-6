using UnityEngine;

public sealed class DialogRun
{
	public DialogRunKind Kind;

	public string Text = "";

	public int Link = -1;

	public bool Bold;

	public bool SelectionMarker;

	public Color Color = DialogText.Ink;

	public string Art = "";

	public string Fallback = "";

	public DialogRun()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
	}
}
