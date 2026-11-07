using UnityEngine;

public static class ChannelWindow
{
	public static bool Visible;

	private const int W = 436;

	private const int H = 143;

	private const int BodyTop = 67;

	private const int BodyH = 40;

	private const int BottomH = 36;

	private const int ListX = 16;

	private const int ListY = 30;

	private static Vector2 pos = new Vector2(220f, 140f);

	private static bool dragging;

	private static Vector2 grab;

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

	private static void Text(Rect rect, string value, Color color, TextAnchor anchor = (TextAnchor)3)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
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
			alignment = anchor,
			clipping = (TextClipping)1,
			padding = new RectOffset(0, 0, 0, 0)
		};
		val.normal.textColor = color;
		GUI.Label(rect, value, val);
	}

	public static void Draw()
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Invalid comparison between Unknown and I4
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_030d: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		pos = NativeUi.ClampWindowPosition(pos, new Vector2(436f, 143f), S);
		Rect val = new Rect(pos.x * S, pos.y * S, 436f * S, 143f * S);
		NativeUi.RegisterWindow("ChannelWindow", val, () => Visible);
		if ((int)Event.current.type == 7 && Pointer.Pressed)
		{
			Rect val2 = At(val, 0f, 0f, 436f, 26f);
			if (val2.Contains(Pointer.Pos))
			{
				dragging = true;
				grab = Pointer.Pos / S - pos;
			}
		}
		if (dragging && Pointer.Down)
		{
			pos = NativeUi.ClampWindowPosition(Pointer.Pos / S - grab, new Vector2(436f, 143f), S);
			val.position = pos * S;
		}
		if (!Pointer.Down)
		{
			dragging = false;
		}
		Texture2D val3 = Art.Tex("wz://UI/UIWindow.img/Channel/t");
		Texture2D val4 = Art.Tex("wz://UI/UIWindow.img/Channel/c");
		Texture2D val5 = Art.Tex("wz://UI/UIWindow.img/Channel/s");
		if ((Object)(object)val3 != (Object)null)
		{
			GUI.DrawTexture(At(val, 0f, 0f, 436f, 67f), (Texture)(object)val3);
		}
		if ((Object)(object)val4 != (Object)null)
		{
			for (int num = 67; num < 107; num += 10)
			{
				GUI.DrawTexture(At(val, 0f, num, 436f, Mathf.Min(10, 107 - num)), (Texture)(object)val4);
			}
		}
		if ((Object)(object)val5 != (Object)null)
		{
			GUI.DrawTexture(At(val, 0f, 107f, 436f, 36f), (Texture)(object)val5);
		}
		Rect val6 = At(val, 16f, 30f, 68f, 20f);
		Texture2D val7 = Art.Tex("wz://UI/UIWindow.img/Channel/channel2");
		if ((Object)(object)val7 != (Object)null)
		{
			GUI.DrawTexture(val6, (Texture)(object)val7);
		}
		Texture2D val8 = Art.Tex("wz://UI/UIWindow.img/Channel/ch/0");
		if ((Object)(object)val8 != (Object)null)
		{
			GUI.DrawTexture(At(val, 30f, 35f, 20f, 10f), (Texture)(object)val8);
		}
		Text(At(val, 18f, 69f, 400f, 14f), "本机服务器只开放 1 个频道，无法变更。", Color.black, (TextAnchor)3);
		float num2 = 159.5f;
		float y = 116f;
		if (NativeUi.Button(At(val, num2, y, 47f, 19f), "UI/UIWindow.img/Channel/BtCancel"))
		{
			Visible = false;
			return;
		}
		Texture2D val9 = Art.Tex("wz://UI/UIWindow.img/Channel/BtChange/disabled/0");
		if ((Object)(object)val9 != (Object)null)
		{
			GUI.DrawTexture(At(val, num2 + 57f, y, 60f, 19f), (Texture)(object)val9);
		}
	}

	static ChannelWindow()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
	}
}
