using UnityEngine;

public static class GameOptWindow
{
	public static bool Visible;

	private const int W = 158;

	private const int H = 284;

	private const int RowTop = 32;

	private const int RowPitch = 18;

	private const int BoxX = 90;

	private const int BoxW = 60;

	private const int BoxH = 16;

	private static Vector2 pos = new Vector2(238f, 132f);

	private static bool dragging;

	private static Vector2 grab;

	private static readonly Color BoxFill = Color32.op_Implicit(new Color32((byte)221, (byte)221, (byte)204, byte.MaxValue));

	private static readonly Color BoxText = Color32.op_Implicit(new Color32((byte)34, (byte)68, (byte)102, byte.MaxValue));

	private static float S => BottomBar.S;

	public static void Toggle()
	{
		Visible = !Visible;
	}

	public static void Close()
	{
		Visible = false;
	}

	private static Rect At(Rect r, float x, float y, float w, float h)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(r.x + x * S, r.y + y * S, w * S, h * S);
	}

	private static void Text(Rect r, string value, Color color)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected Obj, but got Unknown
		//IL_004a: Expected Obj, but got Unknown
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = Mathf.Max(8, Mathf.RoundToInt(11f * S)),
			alignment = (TextAnchor)4,
			clipping = (TextClipping)1,
			padding = new RectOffset(0, 0, 0, 0)
		};
		val.normal.textColor = color;
		GUI.Label(r, value, val);
	}

	public static void Draw()
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Invalid comparison between Unknown and I4
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		if (!SettingsShell.Begin("wz://UI/UIWindow.img/GameOpt/backgrnd", ref pos, 158, 284, S, ref dragging, ref grab, out var rect))
		{
			Visible = false;
			return;
		}
		for (int i = 0; i < 12; i++)
		{
			float num = 32 + i * 18;
			bool flag = ClientSettings.Invite(i);
			if (!flag)
			{
				UiKit.Fill(At(rect, 91f, num + 1f, 58f, 14f), BoxFill);
				Text(At(rect, 90f, num, 60f, 16f), "拒绝", BoxText);
			}
			Rect val = At(rect, 3f, num - 1f, 152f, 18f);
			if ((int)Event.current.type == 7 && Pointer.Clicked && val.Contains(Pointer.Pos))
			{
				Pointer.ConsumeClick();
				AudioManager.Play("UI/BtMouseClick", 0.7f, pitchVary: false);
				ClientSettings.SetInvite(i, !flag);
			}
		}
	}

	static GameOptWindow()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
	}
}
