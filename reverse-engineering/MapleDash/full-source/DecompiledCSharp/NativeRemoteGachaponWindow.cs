using UnityEngine;

public static class NativeRemoteGachaponWindow
{
	private static readonly (int npc, string name)[] Machines = new (int, string)[10]
	{
		(9100100, "射手村"),
		(9100101, "魔法密林"),
		(9100102, "勇士部落"),
		(9100103, "废弃都市"),
		(9100104, "林中之城"),
		(9100105, "蘑菇神社"),
		(9100106, "昭和澡堂（男）"),
		(9100107, "昭和澡堂（女）"),
		(9100109, "新叶城"),
		(9100117, "海豚码头")
	};

	private const int WinW = 246;

	private const int WinH = 235;

	private static Vector2 pos = new Vector2(-1f, -1f);

	private static bool dragging;

	private static Vector2 dragOff;

	public static bool Visible { get; private set; }

	private static float S => BottomBar.S;

	private static Rect At(float x, float y, float w, float h)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		return new Rect((pos.x + x) * S, (pos.y + y) * S, w * S, h * S);
	}

	public static void Open()
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		Visible = true;
		if (pos.x < 0f)
		{
			pos = new Vector2((UiKit.W / S - 246f) / 2f, (UiKit.H / S - 235f) / 2f);
		}
		pos = NativeUi.ClampWindowPosition(pos, new Vector2(246f, 235f), S);
	}

	public static void Close()
	{
		Visible = false;
	}

	public static void Reset()
	{
		Close();
	}

	public static void Draw()
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Invalid comparison between Unknown and I4
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		if (pos.x < 0f)
		{
			pos = new Vector2((UiKit.W / S - 246f) / 2f, (UiKit.H / S - 235f) / 2f);
		}
		NativeUi.RegisterWindow("NativeRemoteGachaponWindow", new Rect(pos.x * S, pos.y * S, 246f * S, 235f * S), () => Visible);
		Texture2D val = Art.Tex("wz://UI/UIWindow.img/RemoteGachapon/backgrnd");
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(pos.x * S, pos.y * S, (float)((Texture)val).width * S, (float)((Texture)val).height * S), (Texture)(object)val);
		}
		if (NativeUi.Button(At(229f, 3f, 12f, 12f), "UI/Basic.img/BtClose"))
		{
			Close();
			return;
		}
		for (int num = 0; num < Machines.Length; num++)
		{
			float x = ((num % 2 == 0) ? 18 : 125);
			float y = 48 + num / 2 * 36;
			if (NativeUi.Button(At(x, y, 103f, 18f), "UI/UIWindow.img/RemoteGachapon/Bt" + num))
			{
				Visible = false;
				NpcSession.Talk(Machines[num].npc, "gachaponRemote");
				return;
			}
		}
		if ((int)Event.current.type == 7 && Pointer.Pressed)
		{
			Rect val2 = new Rect(pos.x * S, pos.y * S, 246f * S, 22f * S);
			if (val2.Contains(Pointer.Pos))
			{
				dragging = true;
				dragOff = Pointer.Pos / S - pos;
			}
		}
		if (dragging && Pointer.Down)
		{
			pos = Pointer.Pos / S - dragOff;
			pos = NativeUi.ClampWindowPosition(pos, new Vector2(246f, 235f), S);
		}
		if (!Pointer.Down)
		{
			dragging = false;
		}
	}

	static NativeRemoteGachaponWindow()
	{
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
	}
}
