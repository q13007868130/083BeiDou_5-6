using System;
using UnityEngine;

public static class WindowShell
{
	public static readonly Color Title = new Color(0.92f, 0.95f, 1f);

	public static bool Button(Rect r, string label, bool on = false)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		return NativeUi.Button(r, "UI/Basic.img/BtOK");
	}

	public static void Label(Rect r, string text, int size = 12, Nullable color = null)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected Obj, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		GUIStyle val = new GUIStyle
		{
			fontSize = size
		};
		val.normal.textColor = color ?? Color.black;
		GUI.Label(r, text, val);
	}

	static WindowShell()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
	}
}
