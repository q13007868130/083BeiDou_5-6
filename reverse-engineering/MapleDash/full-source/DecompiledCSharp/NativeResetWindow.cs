using System;
using UnityEngine;

public static class NativeResetWindow
{
	private const string Root = "UI/UIWindow.img/Reset/AP/";

	private static readonly int[] RowY = new int[6] { 71, 89, 113, 131, 149, 167 };

	private static readonly int[] ArrowY = new int[6] { 74, 92, 116, 134, 152, 170 };

	private static readonly int[] StatBit = new int[6] { 2048, 8192, 64, 128, 256, 512 };

	private static int itemId;

	private static int itemPosition;

	private static int stage;

	private static int from;

	private static int to;

	private static Vector2 pos;

	private static Vector2 grab;

	private static bool dragging;

	private static bool centered;

	public static bool Visible => stage != 0;

	private static float S => BottomBar.S;

	private static Vector2 Size
	{
		get
		{
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			return new Vector2(175f, (float)((stage == 3) ? 141 : 220));
		}
	}

	private static Rect Window
	{
		get
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			return new Rect(pos.x * S, pos.y * S, Size.x * S, Size.y * S);
		}
	}

	private static Rect At(Rect r, float x, float y, float w, float h)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(r.x + x * S, r.y + y * S, w * S, h * S);
	}

	private static void Image(Rect r, string node, float x, float y)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://UI/UIWindow.img/Reset/AP/" + node);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(At(r, x, y, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
		}
	}

	private static void Value(Rect r, string text, float y)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected Obj, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = Mathf.RoundToInt(11f * S),
			alignment = (TextAnchor)5
		};
		val.normal.textColor = Color.black;
		GUI.Label(At(r, 62f, y, 88f, 16f), text, val);
	}

	public static void Open(int id, int inventoryPosition = 0)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		if (id == 5050000)
		{
			itemId = id;
			itemPosition = inventoryPosition;
			stage = 1;
			from = 0;
			to = 0;
			dragging = false;
			centered = false;
			pos = new Vector2(300f, 160f);
			Center();
		}
	}

	public static void Close()
	{
		itemId = 0;
		stage = 0;
		from = 0;
		to = 0;
		dragging = false;
		centered = false;
	}

	private static void Center()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		if (!(S < 0.3f))
		{
			pos = NativeUi.ClampWindowPosition(new Vector2((UiKit.W / S - Size.x) / 2f, (UiKit.H / S - Size.y) / 2f), Size, S);
			centered = true;
		}
	}

	private static string RowValue(int index)
	{
		CharacterAttributes attributes = Profile.Attributes;
		return index switch
		{
			0 => Mathf.CeilToInt(((Object)(object)GameLoop.I != (Object)null && (Object)(object)GameLoop.I.player != (Object)null) ? GameLoop.I.player.hpMax : ((float)attributes.maxHp)).ToString(), 
			1 => Mathf.CeilToInt(((Object)(object)GameLoop.I != (Object)null && (Object)(object)GameLoop.I.player != (Object)null) ? GameLoop.I.player.mpMax : ((float)attributes.maxMp)).ToString(), 
			2 => attributes.str.ToString(), 
			3 => attributes.dex.ToString(), 
			4 => attributes.intelligence.ToString(), 
			_ => attributes.luk.ToString(), 
		};
	}

	public static void Draw()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Invalid comparison between Unknown and I4
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		if (!centered)
		{
			Center();
		}
		pos = NativeUi.ClampWindowPosition(pos, Size, S);
		Rect window = Window;
		NativeUi.RegisterWindow("NativeResetWindow", window, () => Visible);
		Vector2 pointer = Pointer.Pos / S;
		if ((int)Event.current.type == 7 && Pointer.Pressed)
		{
			Rect val = At(window, 0f, 0f, 150f, 22f);
			if (val.Contains(Pointer.Pos) && stage != 3)
			{
				dragging = true;
				centered = true;
				grab = Pointer.Pos / S - pos;
			}
		}
		if (dragging && Pointer.Down)
		{
			pos = NativeUi.ClampWindowPosition(Pointer.Pos / S - grab, Size, S);
			window = Window;
		}
		if (!Pointer.Down)
		{
			dragging = false;
		}
		if (stage == 3)
		{
			DrawConfirm(window, pointer);
			return;
		}
		Image(window, (stage == 1) ? "backgrnd" : "backgrnd2", 0f, 0f);
		if (NativeUi.Button(At(window, 156f, 6f, 12f, 12f), "UI/Basic.img/BtClose", pointer))
		{
			Close();
			return;
		}
		string text = ((stage == 1) ? "BtDown" : "BtUP");
		for (int num = 0; num < RowY.Length; num++)
		{
			int num2 = StatBit[num];
			bool num3 = num < 2 || (stage == 2 && num2 == from);
			Value(window, RowValue(num), RowY[num]);
			if (num3)
			{
				Image(window, "stat1/" + num, 8f, RowY[num]);
				Texture2D val2 = Art.Tex("wz://UI/UIWindow.img/Reset/Common/" + text + "/disabled/0");
				if ((Object)(object)val2 != (Object)null)
				{
					GUI.DrawTexture(At(window, 153f, ArrowY[num], 12f, 12f), (Texture)(object)val2);
				}
			}
			else if (NativeUi.Button(At(window, 153f, ArrowY[num], 12f, 12f), "UI/UIWindow.img/Reset/Common/" + text, pointer))
			{
				if (stage == 1)
				{
					from = num2;
					stage = 2;
				}
				else
				{
					to = num2;
					stage = 3;
				}
				break;
			}
		}
	}

	private static void DrawConfirm(Rect r, Vector2 pointer)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		Image(r, "backgrnd3", 0f, 0f);
		int num = Array.IndexOf(StatBit, to);
		if (num >= 0)
		{
			Image(r, "stat0/" + num, 8f, 81f);
			Value(r, RowValue(num), 81f);
		}
		if (NativeUi.Button(At(r, 70f, 117f, 47f, 19f), "UI/Basic.img/BtOK2", pointer))
		{
			Confirm();
		}
		if (NativeUi.Button(At(r, 119f, 117f, 47f, 19f), "UI/Basic.img/BtCancel2", pointer))
		{
			Close();
		}
		else if (NativeUi.Button(At(r, 156f, 6f, 12f, 12f), "UI/Basic.img/BtClose", pointer))
		{
			Close();
		}
	}

	private static void Confirm()
	{
		int num = from;
		int num2 = to;
		int sourcePosition = itemPosition;
		int id = itemId;
		Close();
		string text = Profile.ResetAp(num, num2);
		if (text != null)
		{
			BottomBar.Toast(text);
		}
		else
		{
			Profile.RemoveItem(id, 1, sourcePosition);
		}
	}
}
