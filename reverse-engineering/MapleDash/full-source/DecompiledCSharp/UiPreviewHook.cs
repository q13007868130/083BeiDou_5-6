using System;
using UnityEngine;

public static class UiPreviewHook
{
	public static Action Draw;

	public static Color Backdrop = Color32.op_Implicit(new Color32((byte)30, (byte)34, (byte)40, byte.MaxValue));

	public static bool SolidBackdrop;

	private static Texture2D white;

	private static Texture2D White
	{
		get
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Expected Obj, but got Unknown
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)white == (Object)null)
			{
				white = new Texture2D(1, 1);
				white.SetPixel(0, 0, Color.white);
				white.Apply();
			}
			return white;
		}
	}

	public static void Emit()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		if (Draw != null && Event.current != null && (int)Event.current.type == 7)
		{
			int depth = GUI.depth;
			GUI.depth = -10000;
			if (SolidBackdrop)
			{
				Color color = GUI.color;
				GUI.color = Backdrop;
				GUI.DrawTexture(new Rect(-20000f, -20000f, 40000f, 40000f), (Texture)(object)White);
				GUI.color = color;
			}
			Draw();
			GUI.depth = depth;
		}
	}

	static UiPreviewHook()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
	}
}
