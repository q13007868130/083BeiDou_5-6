using System;
using System.Collections.Generic;
using UnityEngine;

public static class VirtualControlsEditor
{
	private enum CardKind
	{
		Button,
		Stick,
		Trackpad,
		Dpad,
		KeyGroup
	}

	private const int P_None = 0;

	private const int P_Add = 1;

	private const int P_Edit = 2;

	private const int P_Style = 3;

	private const int P_Shape = 4;

	private const int P_Color = 5;

	private const int P_Keys = 6;

	private static int page = 0;

	private static int pageOpenedFrame = -1;

	private static float pageT = 1f;

	private static VCElem selected;

	private static bool dragging;

	private static bool dragMoved;

	private static Vector2 dragOff;

	private static string dragSnapshot = "";

	private static bool resizing;

	private static bool resizeMoved;

	private static float resizeStartDist;

	private static float resizeStartScale;

	private static string resizeSnapshot = "";

	private static bool aDown;

	private static bool aPressed;

	private static bool aReleased;

	private static Vector2 aPos;

	private static Vector2 aPressPos;

	private static float pressDist;

	private static Vector2 lastPos;

	private static int colorTarget;

	private static readonly List<string> undoStack = new List<string>();

	private static readonly List<string> redoStack = new List<string>();

	private const int HistoryMax = 60;

	private static readonly Color BarBg = new Color(0.052f, 0.052f, 0.058f, 0.96f);

	private static readonly Color BarEdge = new Color(1f, 1f, 1f, 0.16f);

	private static readonly Color Ink = new Color(0.94f, 0.95f, 0.96f, 1f);

	private static readonly Color InkDim = new Color(1f, 1f, 1f, 0.26f);

	private static readonly Color DividerCol = new Color(1f, 1f, 1f, 0.13f);

	private static readonly Color Accent = new Color(0.04f, 0.52f, 1f, 1f);

	private static readonly Color SelectRing = new Color(0.16f, 0.72f, 0.95f, 0.95f);

	private const float HandleGrab = 15f;

	private const float BarH = 84f;

	private const float BarY = 16f;

	private const float PadX = 26f;

	private const float GapIn = 12f;

	private const float GapGroup = 30f;

	private const float DivW = 2f;

	private const float W_BACK = 64f;

	private const float W_LAYER = 140f;

	private const float W_DEFAULT = 140f;

	private const float W_PLUS = 72f;

	private const float W_ICON = 62f;

	private static Rect plusRect;

	private const float AddPad = 18f;

	private const float AddGap = 14f;

	private const float AddCardH = 156f;

	private const float AddLabH = 34f;

	private const float AddHead = 62f;

	private static readonly Vector2[] DpadDirs = new Vector2[4]
	{
		new Vector2(0f, -18f),
		new Vector2(0f, 18f),
		new Vector2(-18f, 0f),
		new Vector2(18f, 0f)
	};

	private const float GS = 1.42f;

	private static int keyTab;

	private static bool keyShift;

	private static int padTab;

	private static int keyTarget;

	private static readonly Color KeyBg = new Color(0.11f, 0.11f, 0.118f, 1f);

	private static readonly Color KeyBgOn = new Color(0.1f, 0.22f, 0.34f, 1f);

	private static readonly Color KeyTxt = new Color(0.86f, 0.87f, 0.89f, 1f);

	private static readonly Color KeyTxtDim = new Color(0.44f, 0.44f, 0.47f, 1f);

	private static readonly Color SheetBg = new Color(0.043f, 0.043f, 0.047f, 1f);

	private static readonly (KeyCode k, string l, string s, float w)[] RowFuncs = new (KeyCode, string, string, float)[13]
	{
		((KeyCode)27, "Esc", null, 1.5f),
		((KeyCode)282, "F1", null, 1.125f),
		((KeyCode)283, "F2", null, 1.125f),
		((KeyCode)284, "F3", null, 1.125f),
		((KeyCode)285, "F4", null, 1.125f),
		((KeyCode)286, "F5", null, 1.125f),
		((KeyCode)287, "F6", null, 1.125f),
		((KeyCode)288, "F7", null, 1.125f),
		((KeyCode)289, "F8", null, 1.125f),
		((KeyCode)290, "F9", null, 1.125f),
		((KeyCode)291, "F10", null, 1.125f),
		((KeyCode)292, "F11", null, 1.125f),
		((KeyCode)293, "F12", null, 1.125f)
	};

	private static readonly (KeyCode k, string l, string s, float w)[] RowDigits = new (KeyCode, string, string, float)[14]
	{
		((KeyCode)96, "`", "~", 1f),
		((KeyCode)49, "1", "!", 1f),
		((KeyCode)50, "2", "@", 1f),
		((KeyCode)51, "3", "#", 1f),
		((KeyCode)52, "4", "$", 1f),
		((KeyCode)53, "5", "%", 1f),
		((KeyCode)54, "6", "^", 1f),
		((KeyCode)55, "7", "&", 1f),
		((KeyCode)56, "8", "*", 1f),
		((KeyCode)57, "9", "(", 1f),
		((KeyCode)48, "0", ")", 1f),
		((KeyCode)45, "-", "_", 1f),
		((KeyCode)61, "=", "+", 1f),
		((KeyCode)8, "←", null, 2f)
	};

	private static readonly (KeyCode k, string l, string s, float w)[] RowQwerty = new (KeyCode, string, string, float)[14]
	{
		((KeyCode)9, "Tab", null, 1.5f),
		((KeyCode)113, "Q", null, 1f),
		((KeyCode)119, "W", null, 1f),
		((KeyCode)101, "E", null, 1f),
		((KeyCode)114, "R", null, 1f),
		((KeyCode)116, "T", null, 1f),
		((KeyCode)121, "Y", null, 1f),
		((KeyCode)117, "U", null, 1f),
		((KeyCode)105, "I", null, 1f),
		((KeyCode)111, "O", null, 1f),
		((KeyCode)112, "P", null, 1f),
		((KeyCode)91, "[", "{", 1f),
		((KeyCode)93, "]", "}", 1f),
		((KeyCode)92, "\\", "|", 1.5f)
	};

	private static readonly (KeyCode k, string l, string s, float w)[] RowHome = new (KeyCode, string, string, float)[13]
	{
		((KeyCode)301, "Caps", null, 1.75f),
		((KeyCode)97, "A", null, 1f),
		((KeyCode)115, "S", null, 1f),
		((KeyCode)100, "D", null, 1f),
		((KeyCode)102, "F", null, 1f),
		((KeyCode)103, "G", null, 1f),
		((KeyCode)104, "H", null, 1f),
		((KeyCode)106, "J", null, 1f),
		((KeyCode)107, "K", null, 1f),
		((KeyCode)108, "L", null, 1f),
		((KeyCode)59, ";", ":", 1f),
		((KeyCode)39, "'", "\"", 1f),
		((KeyCode)13, "Enter", null, 2.25f)
	};

	private static readonly (KeyCode k, string l, string s, float w)[] RowBottom = new (KeyCode, string, string, float)[12]
	{
		((KeyCode)304, "L Shift", null, 2.25f),
		((KeyCode)122, "Z", null, 1f),
		((KeyCode)120, "X", null, 1f),
		((KeyCode)99, "C", null, 1f),
		((KeyCode)118, "V", null, 1f),
		((KeyCode)98, "B", null, 1f),
		((KeyCode)110, "N", null, 1f),
		((KeyCode)109, "M", null, 1f),
		((KeyCode)44, ",", "<", 1f),
		((KeyCode)46, ".", ">", 1f),
		((KeyCode)47, "/", "?", 1f),
		((KeyCode)303, "R Shift", null, 2.75f)
	};

	private static readonly (KeyCode k, string l, string s, float w)[] RowSpace = new (KeyCode, string, string, float)[8]
	{
		((KeyCode)306, "L Ctrl", null, 1.25f),
		((KeyCode)311, "L Win", null, 1.25f),
		((KeyCode)308, "L Alt", null, 1.25f),
		((KeyCode)32, "Space", null, 6.25f),
		((KeyCode)307, "R Alt", null, 1.25f),
		((KeyCode)312, "R Win", null, 1.25f),
		((KeyCode)319, "Menu", null, 1.25f),
		((KeyCode)305, "R Ctrl", null, 1.25f)
	};

	private static readonly Color PnlBg = new Color(0.031f, 0.031f, 0.035f, 0.985f);

	private static readonly Color PanelEdge = new Color(1f, 1f, 1f, 0.1f);

	private static readonly Color CardBg = new Color(0.11f, 0.11f, 0.118f, 1f);

	private static readonly Color Glyph = new Color(0.64f, 0.65f, 0.67f, 1f);

	private static readonly Color TxtMain = new Color(0.91f, 0.91f, 0.93f, 1f);

	private static readonly Color TxtSub = new Color(0.6f, 0.6f, 0.63f, 1f);

	private static readonly Color TrackBg = new Color(0.25f, 0.25f, 0.26f, 1f);

	private static readonly Color RowLineCol = new Color(1f, 1f, 1f, 0.055f);

	private static readonly Color ChipBg = new Color(0.165f, 0.165f, 0.175f, 1f);

	private const float HeadH = 76f;

	private const float RH = 80f;

	private const float RHNav = 58f;

	private const float SecH = 26f;

	private const float GapY = 8f;

	private const float InsetX = 20f;

	private static int dragSlider = -1;

	private static bool dirty;

	private static float panelX = -1f;

	private static bool panelMoving;

	private static float panelMoveLast;

	private static float panelScroll;

	private static float panelScrollMax;

	private static bool scrollArm;

	private static Vector2 scrollArmPos;

	private static Vector2 scrollLast;

	private static float bodyTop;

	private static float bodyBottom;

	private static readonly string[] ShapeNames = new string[8] { "圆形", "矩形", "多边形", "LT", "LB", "RT", "RB", "无" };

	private static readonly string[] DirNames = new string[4] { "上", "下", "左", "右" };

	private static readonly int[] DirRot = new int[4] { 3, 1, 2, 0 };

	private static float ch;

	private static float cs;

	private static float cv;

	private static float ca;

	private static Color colorOrigin;

	public static bool Visible { get; private set; }

	private static bool AbsTouch => Application.isMobilePlatform;

	private static float PanelW => Mathf.Min(520f, UiKit.W - 24f);

	public static void Toggle()
	{
		if (Visible)
		{
			Close();
		}
		else
		{
			Open();
		}
	}

	private static void Open()
	{
		VirtualControls.Ensure();
		Visible = true;
		selected = null;
		dragging = false;
		page = 0;
		panelX = -1f;
		panelMoving = false;
		panelScroll = 0f;
		undoStack.Clear();
		redoStack.Clear();
		InputHub.ReleaseAllVirtual();
		TouchInput.Reset();
	}

	private static void Close()
	{
		Visible = false;
		page = 0;
		VirtualControls.SaveActive();
		InputHub.ReleaseAllVirtual();
		TouchInput.Reset();
	}

	private static Vector2 ToGui(Vector2 screen)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2(screen.x / UiKit.Scale, ((float)Screen.height - screen.y) / UiKit.Scale);
	}

	private static void Poll()
	{
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Invalid comparison between Unknown and I4
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Invalid comparison between Unknown and I4
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Invalid comparison between Unknown and I4
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Invalid comparison between Unknown and I4
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Invalid comparison between Unknown and I4
		aPressed = (aReleased = false);
		if (AbsTouch && Input.touchCount > 0)
		{
			Touch touch = Input.GetTouch(0);
			aPos = ToGui(touch.position);
			aDown = (int)touch.phase == 0 || (int)touch.phase == 1 || (int)touch.phase == 2;
			aPressed = (int)touch.phase == 0;
			aReleased = (int)touch.phase == 3 || (int)touch.phase == 4;
		}
		else
		{
			aPos = Pointer.Pos;
			aDown = Pointer.Down;
			aPressed = Pointer.Pressed;
			aReleased = Pointer.Released;
		}
		if (aPressed)
		{
			aPressPos = aPos;
			pressDist = 0f;
		}
		else if (aDown)
		{
			float num = pressDist;
			Vector2 val = aPos - lastPos;
			pressDist = num + val.magnitude;
		}
		lastPos = aPos;
	}

	private static void OpenPage(int p)
	{
		if (p != 6)
		{
			keyTarget = 0;
		}
		if (page != p)
		{
			panelScroll = 0f;
		}
		page = p;
		pageOpenedFrame = Time.frameCount;
		pageT = 0f;
	}

	public static void Draw()
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Invalid comparison between Unknown and I4
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		VirtualControls.Ensure();
		Poll();
		if (Input.GetKeyDown((KeyCode)27) || Input.GetKeyDown((KeyCode)289))
		{
			if (page == 2 || page == 1)
			{
				page = 0;
			}
			else if (page != 0)
			{
				OpenPage(2);
			}
			else
			{
				Close();
			}
			return;
		}
		bool flag = (int)Event.current.type == 7;
		pageT = Mathf.Min(1f, pageT + Time.unscaledDeltaTime * 7f);
		UiKit.Fill(new Rect(0f, 0f, UiKit.W, UiKit.H), Color.black);
		List<VCElem> elems = VirtualControls.elems;
		for (int i = 0; i < elems.Count; i++)
		{
			VirtualControls.DrawElement(elems[i]);
		}
		if (selected != null)
		{
			DrawSelection(selected);
		}
		if (dragging && selected != null)
		{
			Crosshair(CenterOf(selected));
		}
		bool flag2 = page == 2 || page == 3 || page == 4 || page == 5;
		if (flag && ((page == 0) | flag2))
		{
			HandleScene(flag2);
		}
		if (page != 6 && BarFits())
		{
			Toolbar(flag);
		}
		if (page == 1)
		{
			AddPanel(flag);
		}
		else if (page != 0 && page != 6)
		{
			SidePanel(flag);
		}
		if (page == 6)
		{
			KeySheet(flag);
		}
	}

	private static Vector2 CenterOf(VCElem e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return VirtualControls.Center(e);
	}

	private static void DrawSelection(VCElem e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = CenterOf(e);
		Color selectRing = SelectRing;
		if (e.type != VCElemType.Button)
		{
			float num = e.HalfW + 18f;
			float num2 = e.R + 18f;
			Rect r = new Rect(val.x - num, val.y - num2, num * 2f, num2 * 2f);
			DashRect(r, selectRing);
			DrawHandle(new Vector2(r.x, r.y));
			DrawHandle(new Vector2(r.xMax, r.y));
			DrawHandle(new Vector2(r.x, r.yMax));
			DrawHandle(new Vector2(r.xMax, r.yMax));
		}
		else if (e.shape == 0)
		{
			Ring(val, e.R + 9f, selectRing, 2.6f);
		}
		else
		{
			Outline(new Rect(val.x - e.HalfW - 9f, val.y - e.R - 9f, e.HalfW * 2f + 18f, e.R * 2f + 18f), selectRing, 2.6f);
		}
	}

	private static void DrawHandle(Vector2 p)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		Disc(p, 7.5f, Color.white);
		Ring(p, 7.5f, SelectRing, 2.2f);
	}

	private static bool NearCorner(VCElem e, Vector2 p)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = CenterOf(e);
		float num = e.HalfW + 18f;
		float num2 = e.R + 18f;
		if (Mathf.Abs(Mathf.Abs(p.x - val.x) - num) < 15f)
		{
			return Mathf.Abs(Mathf.Abs(p.y - val.y) - num2) < 15f;
		}
		return false;
	}

	private static void DashRect(Rect r, Color col)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		for (float num = r.x; num < r.xMax; num += 18f)
		{
			float num2 = Mathf.Min(10f, r.xMax - num);
			UiKit.Fill(new Rect(num, r.y - 1.2f, num2, 2.4f), col);
			UiKit.Fill(new Rect(num, r.yMax - 1.2f, num2, 2.4f), col);
		}
		for (float num3 = r.y; num3 < r.yMax; num3 += 18f)
		{
			float num4 = Mathf.Min(10f, r.yMax - num3);
			UiKit.Fill(new Rect(r.x - 1.2f, num3, 2.4f, num4), col);
			UiKit.Fill(new Rect(r.xMax - 1.2f, num3, 2.4f, num4), col);
		}
	}

	private static void Crosshair(Vector2 c)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		Color c2 = new Color(1f, 1f, 1f, 0.38f);
		for (float num = 0f; num < UiKit.W; num += 22f)
		{
			UiKit.Fill(new Rect(num, c.y - 1f, Mathf.Min(11f, UiKit.W - num), 2f), c2);
		}
		for (float num2 = 0f; num2 < UiKit.H; num2 += 22f)
		{
			UiKit.Fill(new Rect(c.x - 1f, num2, 2f, Mathf.Min(11f, UiKit.H - num2)), c2);
		}
	}

	private static void HandleScene(bool keepSelectionOnEmpty)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val;
		if (resizing)
		{
			if (aDown && selected != null)
			{
				val = aPos - CenterOf(selected);
				float magnitude = val.magnitude;
				if (!resizeMoved && Mathf.Abs(magnitude - resizeStartDist) > 2.5f)
				{
					resizeMoved = true;
					PushHistory(resizeSnapshot);
				}
				if (resizeMoved)
				{
					selected.scale = Mathf.Clamp(resizeStartScale * magnitude / Mathf.Max(8f, resizeStartDist), 0.4f, 3f);
					dirty = true;
				}
				return;
			}
			if (resizeMoved)
			{
				VirtualControls.SaveActive();
			}
			resizing = false;
		}
		if (!aDown)
		{
			dragging = false;
		}
		if (aPressed && selected != null && selected.type != VCElemType.Button && !OverUi() && NearCorner(selected, aPos))
		{
			resizing = true;
			resizeMoved = false;
			resizeStartScale = selected.scale;
			val = aPos - CenterOf(selected);
			resizeStartDist = Mathf.Max(8f, val.magnitude);
			resizeSnapshot = VirtualControls.ToJson();
			dragging = false;
		}
		else if (dragging && selected != null && aDown)
		{
			Vector2 val2 = aPos - dragOff;
			float num = Mathf.Clamp(val2.x / UiKit.W, 0.02f, 0.98f);
			float num2 = Mathf.Clamp(val2.y / UiKit.H, 0.02f, 0.98f);
			if (!dragMoved && (Mathf.Abs(num - selected.cx) > 0.001f || Mathf.Abs(num2 - selected.cy) > 0.001f))
			{
				dragMoved = true;
				PushHistory(dragSnapshot);
			}
			if (dragMoved)
			{
				selected.cx = num;
				selected.cy = num2;
			}
		}
		else
		{
			if (!aPressed || OverUi())
			{
				return;
			}
			VCElem vCElem = null;
			List<VCElem> elems = VirtualControls.elems;
			for (int num3 = elems.Count - 1; num3 >= 0; num3--)
			{
				if (elems[num3].Hit(aPos))
				{
					vCElem = elems[num3];
					break;
				}
			}
			if (keepSelectionOnEmpty)
			{
				if (vCElem == null)
				{
					return;
				}
				if (vCElem != selected)
				{
					OpenPage(2);
				}
			}
			selected = vCElem;
			dragging = vCElem != null;
			dragMoved = false;
			if (vCElem != null)
			{
				dragSnapshot = VirtualControls.ToJson();
				dragOff = aPos - CenterOf(vCElem);
			}
		}
	}

	private static bool OverUi()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		Rect val = ToolbarRect();
		if (val.Contains(aPos))
		{
			return true;
		}
		if (page == 1)
		{
			val = AddPanelRect();
			if (val.Contains(aPos))
			{
				return true;
			}
		}
		if (page != 0 && page != 1 && page != 6)
		{
			val = PanelRect();
			if (val.Contains(aPos))
			{
				return true;
			}
		}
		return false;
	}

	private static float BarWidth()
	{
		float num = 26f;
		num += 126f;
		num += 202f;
		num += 202f;
		if (selected == null)
		{
			num += 134f;
		}
		num += 136f;
		if (selected != null)
		{
			num += 222f;
		}
		return num + 26f;
	}

	private static Rect ToolbarRect()
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Min(BarWidth(), UiKit.W - 32f);
		float num2;
		if (page != 2 && page != 3 && page != 4 && page != 5)
		{
			num2 = UiKit.W;
		}
		else
		{
			Rect val = PanelRect();
			num2 = val.x - 10f;
		}
		float num3 = num2;
		return new Rect(Mathf.Max(14f, (num3 - num) * 0.5f), 16f, num, 84f);
	}

	private static void Toolbar(bool act)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_030b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0302: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_0276: Unknown result type (might be due to invalid IL or missing references)
		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_0350: Unknown result type (might be due to invalid IL or missing references)
		//IL_032c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0372: Unknown result type (might be due to invalid IL or missing references)
		//IL_037d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0368: Unknown result type (might be due to invalid IL or missing references)
		//IL_038f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0386: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_039b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0400: Unknown result type (might be due to invalid IL or missing references)
		//IL_0407: Unknown result type (might be due to invalid IL or missing references)
		//IL_041a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0413: Unknown result type (might be due to invalid IL or missing references)
		//IL_0424: Unknown result type (might be due to invalid IL or missing references)
		//IL_0448: Unknown result type (might be due to invalid IL or missing references)
		//IL_044d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0457: Unknown result type (might be due to invalid IL or missing references)
		//IL_0462: Unknown result type (might be due to invalid IL or missing references)
		//IL_0469: Unknown result type (might be due to invalid IL or missing references)
		//IL_047c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0475: Unknown result type (might be due to invalid IL or missing references)
		//IL_0486: Unknown result type (might be due to invalid IL or missing references)
		//IL_04aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_04af: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_050f: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0519: Unknown result type (might be due to invalid IL or missing references)
		//IL_0508: Unknown result type (might be due to invalid IL or missing references)
		Rect r = ToolbarRect();
		Pill(r, BarEdge);
		Pill(new Rect(r.x + 1.6f, r.y + 1.6f, r.width - 3.2f, r.height - 3.2f), BarBg);
		float num = 62f;
		float num2 = r.y + (r.height - num) * 0.5f;
		float num3 = r.y + r.height * 0.5f;
		float num4 = r.x + 26f;
		Rect r2 = new Rect(num4, num2, 64f, num);
		HoverCircle(r2, act);
		ArrowBack(new Vector2(r2.center.x, num3), r2.Contains(aPos) ? Color.white : Ink);
		if (BarTap(r2, act))
		{
			Close();
			return;
		}
		num4 += 94f;
		Divider(num4, num3);
		num4 += 32f;
		LayerChip(new Rect(num4, num2, 140f, num), num3);
		num4 += 170f;
		Divider(num4, num3);
		num4 += 32f;
		Rect r3 = new Rect(num4, num2, 140f, num);
		Pill(r3, r3.Contains(aPos) ? Accent : BarBg);
		Text(r3, "默认布局", 24, Ink, (TextAnchor)4);
		if (BarTap(r3, act))
		{
			PushHistory();
			foreach (VCElem elem in VirtualControls.elems)
			{
				VirtualControls.ReleaseForEdit(elem);
			}
			VirtualControls.RestoreDefaultLayout();
			selected = null;
			dragging = (resizing = false);
			panelMoving = (scrollArm = false);
			dragSlider = -1;
			dirty = false;
			page = 0;
			BottomBar.Toast("已应用默认布局");
			return;
		}
		num4 += 170f;
		Divider(num4, num3);
		num4 += 32f;
		if (selected == null)
		{
			plusRect = new Rect(num4, num2, 72f, num);
			HoverCircle(plusRect, act);
			Plus(new Vector2(plusRect.center.x, num3), (page == 1 || plusRect.Contains(aPos)) ? Color.white : Ink);
			if (BarTap(plusRect, act))
			{
				OpenPage((page != 1) ? 1 : 0);
			}
			num4 += 102f;
			Divider(num4, num3);
			num4 += 32f;
		}
		Rect r4 = new Rect(num4, num2, 62f, num);
		bool flag = undoStack.Count > 0;
		if (flag)
		{
			HoverCircle(r4, act);
		}
		UndoIcon(new Vector2(r4.center.x, num3), (!flag) ? InkDim : (r4.Contains(aPos) ? Color.white : Ink));
		if (flag && BarTap(r4, act))
		{
			Undo();
		}
		num4 += 74f;
		Rect r5 = new Rect(num4, num2, 62f, num);
		bool flag2 = redoStack.Count > 0;
		if (flag2)
		{
			HoverCircle(r5, act);
		}
		RedoIcon(new Vector2(r5.center.x, num3), (!flag2) ? InkDim : (r5.Contains(aPos) ? Color.white : Ink));
		if (flag2 && BarTap(r5, act))
		{
			Redo();
		}
		num4 += 62f;
		if (selected != null)
		{
			num4 += 12f;
			Rect r6 = new Rect(num4, num2, 62f, num);
			HoverCircle(r6, act);
			Trash(new Vector2(r6.center.x, num3), r6.Contains(aPos) ? Color.white : Ink);
			if (BarTap(r6, act))
			{
				DeleteSelected();
			}
			num4 += 74f;
			Rect r7 = new Rect(num4, num2, 62f, num);
			HoverCircle(r7, act);
			CopyIcon(new Vector2(r7.center.x, num3), r7.Contains(aPos) ? Color.white : Ink);
			if (BarTap(r7, act))
			{
				CopySelected();
			}
			num4 += 74f;
			Rect r8 = new Rect(num4, num2, 62f, num);
			HoverCircle(r8, act);
			bool flag3 = page == 2 || page == 3 || page == 4 || page == 5 || page == 6;
			Pencil(new Vector2(r8.center.x, num3), (flag3 || r8.Contains(aPos)) ? Color.white : Ink);
			if (BarTap(r8, act))
			{
				OpenPage((page != 2) ? 2 : 0);
			}
		}
	}

	private static bool BarFits()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (page != 2 && page != 3 && page != 4 && page != 5)
		{
			return true;
		}
		Rect val = PanelRect();
		return val.x - 10f >= BarWidth() + 20f;
	}

	private static bool BarTap(Rect r, bool act)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		if (!Tap(r, act))
		{
			return false;
		}
		if (page == 2 || page == 3 || page == 4 || page == 5)
		{
			Rect val = PanelRect();
			return !val.Contains(r.center);
		}
		return true;
	}

	private static void Divider(float x, float cy)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		UiKit.Fill(new Rect(x, cy - 19f, 2f, 38f), DividerCol);
	}

	private static void LayerChip(Rect r, float cy)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		Text(new Rect(r.x, r.y, r.width - 30f, r.height), "图层 1", 25, Ink, (TextAnchor)3);
		Vector2 val = new Vector2(r.x + r.width - 20f, cy);
		Color col = new Color(1f, 1f, 1f, 0.62f);
		VirtualControls.DrawLine(val + new Vector2(-7f, -3.5f), val + new Vector2(0f, 3.5f), col, 2.6f);
		VirtualControls.DrawLine(val + new Vector2(0f, 3.5f), val + new Vector2(7f, -3.5f), col, 2.6f);
	}

	private static void DeleteSelected()
	{
		if (selected != null)
		{
			PushHistory();
			VirtualControls.ReleaseForEdit(selected);
			VirtualControls.elems.Remove(selected);
			selected = null;
			page = 0;
			VirtualControls.SaveActive();
		}
	}

	private static void CopySelected()
	{
		if (selected != null)
		{
			PushHistory();
			VCElem vCElem = selected.Clone();
			vCElem.cx = Mathf.Clamp(vCElem.cx + 0.06f, 0.02f, 0.98f);
			vCElem.cy = Mathf.Clamp(vCElem.cy + 0.06f, 0.02f, 0.98f);
			VirtualControls.elems.Add(vCElem);
			selected = vCElem;
			VirtualControls.SaveActive();
		}
	}

	private static void PushHistory()
	{
		PushHistory(VirtualControls.ToJson());
	}

	private static void PushHistory(string snapshot)
	{
		if (string.IsNullOrEmpty(snapshot))
		{
			snapshot = VirtualControls.ToJson();
		}
		undoStack.Add(snapshot);
		if (undoStack.Count > 60)
		{
			undoStack.RemoveAt(0);
		}
		redoStack.Clear();
	}

	private static void Restore(string json)
	{
		VirtualControls.FromJson(json);
		VirtualControls.SaveActive();
		selected = null;
		dragging = false;
		page = 0;
	}

	private static void Undo()
	{
		if (undoStack.Count != 0)
		{
			redoStack.Add(VirtualControls.ToJson());
			int index = undoStack.Count - 1;
			string json = undoStack[index];
			undoStack.RemoveAt(index);
			Restore(json);
		}
	}

	private static void Redo()
	{
		if (redoStack.Count != 0)
		{
			undoStack.Add(VirtualControls.ToJson());
			int index = redoStack.Count - 1;
			string json = redoStack[index];
			redoStack.RemoveAt(index);
			Restore(json);
		}
	}

	private static Rect AddPanelRect()
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Min(680f, UiKit.W - 32f);
		float num2 = 190f;
		float num3 = 62f + num2 * 2f + 14f + 20f;
		float num4 = Mathf.Clamp(((plusRect.width > 0f) ? plusRect.center.x : (UiKit.W * 0.5f)) - num * 0.5f, 14f, Mathf.Max(14f, UiKit.W - num - 14f));
		Rect val = ToolbarRect();
		return new Rect(num4, val.yMax + 6f, num, num3);
	}

	private static void AddPanel(bool act)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		Rect r = AddPanelRect();
		Rounded(new Rect(r.x - 1.5f, r.y - 1.5f, r.width + 3f, r.height + 3f), PanelEdge);
		Rounded(r, PnlBg);
		float num = (r.width - 36f - 28f) / 3f;
		float num2 = 190f;
		float num3 = r.y + 62f;
		Text(new Rect(r.x + 18f, r.y + 6f, r.width - 36f, 46f), "添加控件", 27, new Color(0.9f, 0.91f, 0.92f, 1f), (TextAnchor)3);
		for (int i = 0; i < 5; i++)
		{
			int num4 = i % 3;
			int num5 = i / 3;
			Rect card = new Rect(r.x + 18f + (float)num4 * (num + 14f), num3 + (float)num5 * (num2 + 14f), num, 156f);
			Rect lab = new Rect(card.x, card.yMax + 2f, card.width, 30f);
			switch (i)
			{
			case 0:
				AddCard(card, lab, CardKind.Button, "按钮", enabled: true, act, () =>
				{
					AddElem(VCElemType.Button);
				});
				break;
			case 1:
				AddCard(card, lab, CardKind.Stick, "摇杆", enabled: true, act, () =>
				{
					AddElem(VCElemType.Stick);
				});
				break;
			case 2:
				AddCard(card, lab, CardKind.Trackpad, "触控板", enabled: false, act, null);
				break;
			case 3:
				AddCard(card, lab, CardKind.Dpad, "方向键", enabled: true, act, () =>
				{
					AddElem(VCElemType.Dpad);
				});
				break;
			default:
				AddCard(card, lab, CardKind.KeyGroup, "按键组", enabled: false, act, null);
				break;
			}
		}
		if (act && aPressed && Time.frameCount != pageOpenedFrame && !r.Contains(aPos))
		{
			Rect val = ToolbarRect();
			if (!val.Contains(aPos))
			{
				page = 0;
			}
		}
	}

	private static void AddCard(Rect card, Rect lab, CardKind kind, string label, bool enabled, bool act, Action click)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0328: Unknown result type (might be due to invalid IL or missing references)
		//IL_0337: Unknown result type (might be due to invalid IL or missing references)
		//IL_0330: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_034b: Unknown result type (might be due to invalid IL or missing references)
		bool flag = enabled && card.Contains(aPos);
		Rounded(card, (Color)(flag ? new Color(0.145f, 0.145f, 0.158f, 1f) : CardBg));
		if (flag)
		{
			Rounded(card, new Color(1f, 1f, 1f, 0.1f));
		}
		Color val = (Color)(enabled ? Glyph : new Color(Glyph.r, Glyph.g, Glyph.b, 0.3f));
		Vector2 val2 = new Vector2(card.center.x, card.y + card.height * 0.46f);
		switch (kind)
		{
		case CardKind.Button:
			Disc(val2, 38f, new Color(0.16f, 0.16f, 0.17f, 1f));
			Ring(val2, 38f, val, 2.2f);
			Text(new Rect(val2.x - 40f, val2.y - 40f, 80f, 80f), "A", 36, (Color)(enabled ? val : new Color(val.r, val.g, val.b, 0.55f)), (TextAnchor)4);
			break;
		case CardKind.Stick:
			Disc(val2, 40f, new Color(0.075f, 0.1f, 0.135f, 1f));
			Ring(val2, 40f, val, 2.6f);
			Ring(val2, 31f, new Color(val.r, val.g, val.b, 0.45f), 1.6f);
			Disc(val2, 16f, val);
			break;
		case CardKind.Trackpad:
		{
			Rect r2 = new Rect(val2.x - 40f, val2.y - 28f, 80f, 56f);
			Rounded(r2, new Color(0.14f, 0.14f, 0.15f, 1f));
			Outline(r2, val, 2.2f);
			break;
		}
		case CardKind.Dpad:
		{
			Vector2[] dpadDirs = DpadDirs;
			foreach (Vector2 val3 in dpadDirs)
			{
				Rounded(new Rect(val2.x + val3.x - 11.5f, val2.y + val3.y - 11.5f, 23f, 23f), val);
			}
			break;
		}
		case CardKind.KeyGroup:
		{
			for (int i = 0; i < 3; i++)
			{
				Rect r = new Rect(val2.x - 44f + (float)i * 30f, val2.y - 26f, 27f, 52f);
				Rounded(r, val);
				Text(r, "ABC"[i].ToString(), 21, new Color(0.1f, 0.1f, 0.11f, enabled ? 0.85f : 0.45f), (TextAnchor)4);
			}
			break;
		}
		}
		Text(lab, label, 23, enabled ? Ink : InkDim, (TextAnchor)4);
		if (enabled && click != null && Tap(card, act))
		{
			click();
		}
	}

	private static void AddElem(VCElemType t)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		PushHistory();
		VCElem vCElem = new VCElem
		{
			type = t,
			cx = 0.5f,
			cy = 0.5f
		};
		switch (t)
		{
		case VCElemType.Button:
			vCElem.action = GameAction.Jump;
			vCElem.key = (KeyCode)0;
			vCElem.size = 0.18f;
			break;
		case VCElemType.Stick:
			vCElem.size = 0.34f;
			VirtualControls.ApplyStickSkin(vCElem);
			break;
		default:
			vCElem.size = 0.34f;
			break;
		}
		vCElem.opacity = 1f;
		VirtualControls.elems.Add(vCElem);
		VirtualControls.SaveActive();
		selected = vCElem;
		OpenPage(2);
	}

	private static void Seg(Vector2 c, float x0, float y0, float x1, float y1, Color col, float w)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		VirtualControls.DrawLine(c + new Vector2(x0, y0) * 1.42f, c + new Vector2(x1, y1) * 1.42f, col, w * 1.42f);
	}

	private static void ArrowBack(Vector2 c, Color col)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		Seg(c, 10f, 0f, -8f, 0f, col, 2.2f);
		Seg(c, -8f, 0f, 0f, -8f, col, 2.2f);
		Seg(c, -8f, 0f, 0f, 8f, col, 2.2f);
	}

	private static void Plus(Vector2 c, Color col)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		Seg(c, -10f, 0f, 10f, 0f, col, 2.4f);
		Seg(c, 0f, -10f, 0f, 10f, col, 2.4f);
	}

	private static void Gear(Vector2 c, Color col)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < 8; i++)
		{
			float num = (float)i / 8f * MathF.PI * 2f + MathF.PI / 8f;
			Vector2 val = new Vector2(Mathf.Cos(num), Mathf.Sin(num));
			VirtualControls.DrawLine(c + val * 9.23f, c + val * 17.039999f, col, 6.248f);
		}
		Disc(c, 12.78f, col);
		Disc(c, 5.1119995f, new Color(0.055f, 0.055f, 0.06f, 1f));
	}

	private static void UndoIcon(Vector2 c, Color col)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		TurnArrow(c, col, mirror: false);
	}

	private static void RedoIcon(Vector2 c, Color col)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		TurnArrow(c, col, mirror: true);
	}

	private static void TurnArrow(Vector2 c, Color col, bool mirror)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		float num = (mirror ? (-1f) : 1f);
		float num2 = 12.07f;
		for (int i = 0; i < 26; i++)
		{
			float num3 = (float)i / 26f * MathF.PI;
			float num4 = (float)(i + 1) / 26f * MathF.PI;
			Vector2 a = c + new Vector2(Mathf.Cos(num3) * num, 0f - Mathf.Sin(num3)) * num2;
			Vector2 b = c + new Vector2(Mathf.Cos(num4) * num, 0f - Mathf.Sin(num4)) * num2;
			VirtualControls.DrawLine(a, b, col, 3.124f);
		}
		Vector2 val = c + new Vector2((0f - num2) * num, 0f);
		VirtualControls.DrawLine(val, val + new Vector2(5.5f, -6f) * 1.42f, col, 3.124f);
		VirtualControls.DrawLine(val, val + new Vector2(5.5f, 6f) * 1.42f, col, 3.124f);
	}

	private static void Trash(Vector2 c, Color col)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		Seg(c, -11f, -8f, 11f, -8f, col, 2.2f);
		Seg(c, -4f, -8f, -4f, -12f, col, 2f);
		Seg(c, 4f, -8f, 4f, -12f, col, 2f);
		Seg(c, -4f, -12f, 4f, -12f, col, 2f);
		Seg(c, -8f, -8f, -6.5f, 12f, col, 2.2f);
		Seg(c, 8f, -8f, 6.5f, 12f, col, 2.2f);
		Seg(c, -6.5f, 12f, 6.5f, 12f, col, 2.2f);
		Seg(c, -2f, -3f, -1.6f, 8f, col, 1.8f);
		Seg(c, 2f, -3f, 1.6f, 8f, col, 1.8f);
	}

	private static void CopyIcon(Vector2 c, Color col)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		Outline(new Rect(c.x - 15.62f, c.y - 15.62f, 21.3f, 21.3f), col, 2.84f);
		Outline(new Rect(c.x - 4.2599998f, c.y - 4.2599998f, 21.3f, 21.3f), col, 2.84f);
	}

	private static void Pencil(Vector2 c, Color col)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = c + new Vector2(-8f, 7f) * 1.42f;
		Vector2 val2 = c + new Vector2(8f, -9f) * 1.42f;
		Vector2 val3 = val2 - val;
		Vector2 normalized = val3.normalized;
		Vector2 val4 = new Vector2(0f - normalized.y, normalized.x) * 4.544f;
		float width = 2.5559998f;
		VirtualControls.DrawLine(val + val4, val2 + val4, col, width);
		VirtualControls.DrawLine(val - val4, val2 - val4, col, width);
		VirtualControls.DrawLine(val2 - val4, val2 + val4, col, width);
		VirtualControls.DrawLine(val + val4, val - val4, col, width);
		Vector2 b = val - normalized * 5.68f;
		VirtualControls.DrawLine(val + val4, b, col, width);
		VirtualControls.DrawLine(val - val4, b, col, width);
	}

	private static void XIcon(Vector2 c, Color col, float s = 11f)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		VirtualControls.DrawLine(c + new Vector2(0f - s, 0f - s), c + new Vector2(s, s), col, 3f);
		VirtualControls.DrawLine(c + new Vector2(s, 0f - s), c + new Vector2(0f - s, s), col, 3f);
	}

	private static void ChevronRight(Vector2 c, Color col, float s = 7f)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		VirtualControls.DrawLine(c + new Vector2((0f - s) * 0.6f, 0f - s), c + new Vector2(s * 0.6f, 0f), col, 2.8f);
		VirtualControls.DrawLine(c + new Vector2(s * 0.6f, 0f), c + new Vector2((0f - s) * 0.6f, s), col, 2.8f);
	}

	private static void HoverCircle(Rect r, bool act)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		if (act && r.Contains(aPos))
		{
			Disc(new Vector2(r.center.x, r.center.y), 26f, new Color(1f, 1f, 1f, 0.07f));
		}
	}

	private static void Disc(Vector2 c, float r, Color col)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		Color color = GUI.color;
		GUI.color = col;
		GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), (Texture)(object)VirtualControls.DiscTex());
		GUI.color = color;
	}

	private static void Ring(Vector2 c, float r, Color col, float w)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		VirtualControls.DrawAnnulus(c, Mathf.Max(0f, r - w * 0.5f), r + w * 0.5f, col, fade: false);
	}

	private static void StrokeRect(Rect r, Color line, Color fill, float t)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		Rounded(new Rect(r.x - t * 0.5f, r.y - t * 0.5f, r.width + t, r.height + t), line);
		Rounded(r, fill);
	}

	private static void Rounded(Rect r, Color col)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Color color = GUI.color;
		GUI.color = col;
		GUI.DrawTexture(r, (Texture)(object)VirtualControls.SoftTex());
		GUI.color = color;
	}

	private static void Outline(Rect r, Color col, float t)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		UiKit.Fill(new Rect(r.x, r.y, r.width, t), col);
		UiKit.Fill(new Rect(r.x, r.yMax - t, r.width, t), col);
		UiKit.Fill(new Rect(r.x, r.y, t, r.height), col);
		UiKit.Fill(new Rect(r.xMax - t, r.y, t, r.height), col);
	}

	private static void Pill(Rect r, Color col)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		float num = r.height * 0.5f;
		Color color = GUI.color;
		GUI.color = col;
		GUI.DrawTexture(new Rect(r.x + num, r.y, Mathf.Max(0f, r.width - num * 2f), r.height), (Texture)(object)Texture2D.whiteTexture);
		GUI.DrawTexture(new Rect(r.x, r.y, num * 2f, r.height), (Texture)(object)VirtualControls.DiscTex());
		GUI.DrawTexture(new Rect(r.xMax - num * 2f, r.y, num * 2f, r.height), (Texture)(object)VirtualControls.DiscTex());
		GUI.color = color;
	}

	private static bool Tap(Rect r, bool act)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		if (act && Time.frameCount != pageOpenedFrame && aReleased && pressDist < 12f && r.Contains(aPos))
		{
			return r.Contains(aPressPos);
		}
		return false;
	}

	private static void Text(Rect r, string s, int size, Color col, TextAnchor align, int weight = 1)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		VirtualControls.Label(r, s, size, col, align, weight);
	}

	private static KeyCode CurKey()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (selected == null)
		{
			return (KeyCode)0;
		}
		if (keyTarget <= 0 || selected.type == VCElemType.Button)
		{
			return selected.key;
		}
		return selected.DirKey(keyTarget - 1);
	}

	private static void KeySheet(bool act)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		UiKit.Fill(new Rect(0f, 0f, UiKit.W, UiKit.H), SheetBg);
		float num = 54f;
		float num2 = 400f;
		float num3 = 28f;
		float num4 = Mathf.Max(620f, UiKit.W - num * 2f - num3 - num2);
		float num5 = (UiKit.W - (num4 + num3 + num2)) * 0.5f;
		Rect r = new Rect(num5 - 6f, 16f, 48f, 48f);
		ArrowBack(new Vector2(r.center.x + 2f, r.center.y), r.Contains(aPos) ? Color.white : Ink);
		if (Tap(r, act))
		{
			OpenPage(2);
		}
		string s = ((keyTarget > 0 && keyTarget <= 4) ? ("选择按键 · " + DirNames[keyTarget - 1]) : "选择按键");
		Text(new Rect(num5 + 52f, 16f, 400f, 48f), s, 31, TxtMain, (TextAnchor)3);
		float num6 = 152f;
		float num7 = UiKit.W - num - num6 * 3f - 26f;
		Tab(new Rect(num7, 16f, num6, 50f), "主键盘", 0, act);
		Tab(new Rect(num7 + num6 + 12f, 16f, num6, 50f), "鼠标按键", 1, act);
		Tab(new Rect(num7 + (num6 + 12f) * 2f, 16f, num6, 50f), "游戏手柄", 2, act);
		if (keyTab == 0)
		{
			KeyboardPage(act, num5, num4, num3, num2);
		}
		else if (keyTab == 1)
		{
			MousePage(act);
		}
		else
		{
			PadPage(act);
		}
	}

	private static void Tab(Rect r, string label, int id, bool act)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		bool flag = keyTab == id;
		Rounded(r, flag ? Accent : KeyBg);
		GlyphDevice(new Vector2(r.x + 25f, r.center.y), flag ? Color.white : TxtSub, id);
		Text(new Rect(r.x + 44f, r.y, r.width - 50f, r.height), label, 20, flag ? Color.white : TxtSub, (TextAnchor)3);
		if (Tap(r, act))
		{
			keyTab = id;
		}
	}

	private static void GlyphDevice(Vector2 c, Color col, int kind)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		float num = 1.42f;
		switch (kind)
		{
		case 0:
		{
			Rect r3 = new Rect(c.x - 8f * num, c.y - 6f * num, 16f * num, 12f * num);
			Outline(r3, col, 1.4f * num);
			UiKit.Fill(new Rect(r3.x + 3f * num, r3.y + 3f * num, 2.4f * num, 2.4f * num), col);
			UiKit.Fill(new Rect(r3.x + 7f * num, r3.y + 3f * num, 2.4f * num, 2.4f * num), col);
			UiKit.Fill(new Rect(r3.x + 11f * num, r3.y + 3f * num, 2.4f * num, 2.4f * num), col);
			UiKit.Fill(new Rect(r3.x + 4f * num, r3.y + 8f * num, 8f * num, 2f * num), col);
			break;
		}
		case 1:
		{
			Rect r2 = new Rect(c.x - 6f * num, c.y - 8f * num, 12f * num, 16f * num);
			Rounded(r2, col);
			UiKit.Fill(new Rect(c.x - 0.9f * num, r2.y + 2f * num, 1.8f * num, 5f * num), new Color(0.05f, 0.05f, 0.05f, 1f));
			break;
		}
		default:
		{
			Rect r = new Rect(c.x - 9f * num, c.y - 5f * num, 18f * num, 10f * num);
			Rounded(r, col);
			UiKit.Fill(new Rect(r.x + 3f * num, c.y - 2f * num, 3f * num, 3f * num), new Color(0.05f, 0.05f, 0.05f, 1f));
			UiKit.Fill(new Rect(r.xMax - 6f * num, c.y - 2f * num, 3f * num, 3f * num), new Color(0.05f, 0.05f, 0.05f, 1f));
			break;
		}
		}
	}

	private static void KeyboardPage(bool act, float x0, float kw, float gap, float rightW)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		float num = kw * 0.5f;
		Rect r = new Rect(x0, 80f, kw, 50f);
		Rounded(r, KeyBg);
		if (!keyShift)
		{
			Rounded(new Rect(r.x, r.y, num, r.height), Accent);
		}
		else
		{
			Rounded(new Rect(r.x + num, r.y, num, r.height), Accent);
		}
		Text(new Rect(r.x, r.y, num, r.height), "默认", 22, keyShift ? TxtSub : Color.white, (TextAnchor)4);
		Text(new Rect(r.x + num, r.y, num, r.height), "Shift +", 22, keyShift ? Color.white : TxtSub, (TextAnchor)4);
		if (Tap(new Rect(r.x, r.y, num, r.height), act))
		{
			keyShift = false;
		}
		if (Tap(new Rect(r.x + num, r.y, num, r.height), act))
		{
			keyShift = true;
		}
		float num2 = 148f;
		float num3 = UiKit.H - num2 - 30f;
		float num4 = 8f;
		float num5 = kw / 15f;
		float num6 = Mathf.Clamp(num5 * 1.02f, 40f, 76f);
		float num7 = 6f * num6 + num4 * 5f;
		float num8 = num2 + Mathf.Max(0f, (num3 - num7) * 0.5f);
		float num9 = num8;
		KeyRow(RowFuncs, x0, num9, num5, num6, act);
		float num10 = num9 + num6 + num4;
		KeyRow(RowDigits, x0, num10, num5, num6, act);
		num10 += num6 + num4;
		KeyRow(RowQwerty, x0, num10, num5, num6, act);
		num10 += num6 + num4;
		KeyRow(RowHome, x0, num10, num5, num6, act);
		num10 += num6 + num4;
		KeyRow(RowBottom, x0, num10, num5, num6, act);
		num10 += num6 + num4;
		KeyRow(RowSpace, x0, num10, num5, num6, act);
		float num11 = x0 + kw + gap;
		float num12 = (rightW - 10f) * 0.5f;
		if (ToggleChip(new Rect(num11, 80f, num12, 50f), "控制键", padTab == 0, act))
		{
			padTab = 0;
		}
		if (ToggleChip(new Rect(num11 + num12 + 10f, 80f, num12, 50f), "数字键", padTab == 1, act))
		{
			padTab = 1;
		}
		if (padTab == 0)
		{
			ControlPad(num11, num8, rightW, num6, num4, act);
		}
		else
		{
			NumPad(num11, num8, rightW, num6, num4, act);
		}
	}

	private static void KeyRow((KeyCode k, string l, string s, float w)[] row, float x, float y, float unit, float h, bool act)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < row.Length; i++)
		{
			(KeyCode, string, string, float) tuple = row[i];
			KeyCap(new Rect(x, y, Mathf.Max(18f, tuple.Item4 * unit - 7f), h), tuple.Item1, tuple.Item2, tuple.Item3, act);
			x += tuple.Item4 * unit;
		}
	}

	private static void KeyCap(Rect r, KeyCode k, string label, string shiftLabel, bool act)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		bool flag = keyShift;
		string text = label;
		bool flag2 = false;
		if (flag)
		{
			if (!string.IsNullOrEmpty(shiftLabel))
			{
				text = shiftLabel;
			}
			else
			{
				flag2 = true;
			}
		}
		KeyCode val = CurKey();
		bool flag3 = (int)val != 0 && val == k && (!flag || selected == null || selected.shiftMod);
		Rounded(r, flag3 ? KeyBgOn : KeyBg);
		if (flag3)
		{
			StrokeRect(r, SelectRing, flag3 ? KeyBgOn : KeyBg, 2f);
		}
		int num = Mathf.Clamp(Mathf.RoundToInt(r.height * 0.4f), 12, 30);
		if (!string.IsNullOrEmpty(text))
		{
			int num2 = Mathf.FloorToInt((r.width - 10f) / ((float)text.Length * 0.56f));
			num = Mathf.Clamp(Mathf.Min(num, num2), 11, 30);
		}
		Text(r, text, num, flag2 ? KeyTxtDim : KeyTxt, (TextAnchor)4);
		if (Tap(r, act))
		{
			ApplyKey(k);
		}
	}

	private static void ControlPad(float rx, float y, float w, float rh, float gap, bool act)
	{
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		float num = (w - gap * 2f) / 3f;
		(KeyCode, string)[] array = new (KeyCode, string)[9]
		{
			((KeyCode)316, "PrtScr"),
			((KeyCode)302, "ScrLK"),
			((KeyCode)19, "Pause"),
			((KeyCode)277, "Ins"),
			((KeyCode)278, "Home"),
			((KeyCode)280, "PgUp"),
			((KeyCode)127, "Del"),
			((KeyCode)279, "End"),
			((KeyCode)281, "PgDn")
		};
		for (int i = 0; i < array.Length; i++)
		{
			int num2 = i % 3;
			int num3 = i / 3;
			KeyCap(new Rect(rx + (float)num2 * (num + gap), y + (float)num3 * (rh + gap), num, rh), array[i].Item1, array[i].Item2, null, act);
		}
		float num4 = y + 3f * (rh + gap) + gap;
		KeyCap(new Rect(rx + num + gap, num4, num, rh), (KeyCode)273, "↑", null, act);
		KeyCap(new Rect(rx, num4 + rh + gap, num, rh), (KeyCode)276, "←", null, act);
		KeyCap(new Rect(rx + num + gap, num4 + rh + gap, num, rh), (KeyCode)274, "↓", null, act);
		KeyCap(new Rect(rx + (num + gap) * 2f, num4 + rh + gap, num, rh), (KeyCode)275, "→", null, act);
	}

	private static void NumPad(float rx, float y, float w, float rh, float gap, bool act)
	{
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		float num = (w - gap * 3f) / 4f;
		(KeyCode, string)[] array = new (KeyCode, string)[20]
		{
			((KeyCode)300, "Num"),
			((KeyCode)267, "/"),
			((KeyCode)268, "*"),
			((KeyCode)269, "-"),
			((KeyCode)263, "7"),
			((KeyCode)264, "8"),
			((KeyCode)265, "9"),
			((KeyCode)270, "+"),
			((KeyCode)260, "4"),
			((KeyCode)261, "5"),
			((KeyCode)262, "6"),
			((KeyCode)0, ""),
			((KeyCode)257, "1"),
			((KeyCode)258, "2"),
			((KeyCode)259, "3"),
			((KeyCode)271, "Ent"),
			((KeyCode)256, "0"),
			((KeyCode)266, "."),
			((KeyCode)0, ""),
			((KeyCode)0, "")
		};
		for (int i = 0; i < array.Length; i++)
		{
			if ((int)array[i].Item1 != 0)
			{
				int num2 = i % 4;
				int num3 = i / 4;
				KeyCap(new Rect(rx + (float)num2 * (num + gap), y + (float)num3 * (rh + gap), num, rh), array[i].Item1, array[i].Item2, null, act);
			}
		}
	}

	private static void MousePage(bool act)
	{
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Invalid comparison between Unknown and I4
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		(KeyCode, string, int)[] array = new (KeyCode, string, int)[11]
		{
			((KeyCode)323, "左键", 0),
			((KeyCode)324, "右键", 1),
			((KeyCode)325, "中键", 2),
			((KeyCode)326, "上侧键", 3),
			((KeyCode)0, "上滚轮", 4),
			((KeyCode)0, "下滚轮", 5),
			((KeyCode)327, "下侧键", 6),
			((KeyCode)0, "鼠标上移", 7),
			((KeyCode)0, "鼠标下移", 8),
			((KeyCode)0, "鼠标左移", 9),
			((KeyCode)0, "鼠标右移", 10)
		};
		float num = 54f;
		float num2 = 16f;
		float num3 = (UiKit.W - num * 2f - num2 * 4f) / 5f;
		float num4 = 112f;
		float num5 = 128f;
		for (int i = 0; i < array.Length; i++)
		{
			int num6 = i % 5;
			int num7 = i / 5;
			Rect r = new Rect(num + (float)num6 * (num3 + num2), num5 + (float)num7 * (num4 + num2), num3, num4);
			bool flag = (int)array[i].Item1 > 0;
			bool flag2 = flag && CurKey() == array[i].Item1;
			Rounded(r, flag2 ? KeyBgOn : KeyBg);
			if (flag2)
			{
				Outline(r, SelectRing, 2f);
			}
			Color col = (flag ? KeyTxt : KeyTxtDim);
			MouseGlyph(new Vector2(r.center.x, r.y + 38f), col, array[i].Item3);
			Text(new Rect(r.x, r.yMax - 44f, r.width, 30f), array[i].Item2, 19, col, (TextAnchor)4);
			if (flag && Tap(r, act))
			{
				ApplyKey(array[i].Item1);
			}
		}
	}

	private static void MouseGlyph(Vector2 c, Color col, int kind)
	{
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_030b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0319: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0324: Unknown result type (might be due to invalid IL or missing references)
		//IL_0329: Unknown result type (might be due to invalid IL or missing references)
		//IL_0337: Unknown result type (might be due to invalid IL or missing references)
		//IL_033d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_0347: Unknown result type (might be due to invalid IL or missing references)
		float num = 1.42f;
		if (kind >= 7)
		{
			Vector2 val = kind switch
			{
				9 => new Vector2(-9f, 0f), 
				8 => new Vector2(0f, 9f), 
				7 => new Vector2(0f, -9f), 
				_ => new Vector2(9f, 0f), 
			};
			val *= num;
			VirtualControls.DrawLine(c + val * 0.2f, c + val, col, 2.4f * num);
			Vector2 val2 = new Vector2(0f - val.y, val.x);
			Vector2 val3 = val2.normalized * (4f * num);
			VirtualControls.DrawLine(c + val, c + val - val.normalized * (4f * num) + val3, col, 2.4f * num);
			VirtualControls.DrawLine(c + val, c + val - val.normalized * (4f * num) - val3, col, 2.4f * num);
			return;
		}
		Rect r = new Rect(c.x - 9f * num, c.y - 12f * num, 18f * num, 24f * num);
		Rounded(r, new Color(col.r, col.g, col.b, col.a * 0.3f));
		Outline(r, col, 1.6f * num);
		float num2;
		switch (kind)
		{
		case 0:
			UiKit.Fill(new Rect(r.x + 1.4f, r.y + 1.4f, 7f * num, 9f * num), col);
			return;
		case 1:
			UiKit.Fill(new Rect(r.center.x + 0.6f, r.y + 1.4f, 7f * num, 9f * num), col);
			return;
		case 2:
			UiKit.Fill(new Rect(r.center.x - 2f * num, r.y + 1.4f, 4f * num, 7f * num), col);
			return;
		case 3:
			UiKit.Fill(new Rect(r.x, r.y + 3f * num, 4f * num, 8f * num), col);
			return;
		case 6:
			UiKit.Fill(new Rect(r.xMax - 4f * num, r.y + 3f * num, 4f * num, 8f * num), col);
			return;
		default:
			num2 = 1f;
			break;
		case 4:
			num2 = -1f;
			break;
		}
		float num3 = num2;
		VirtualControls.DrawLine(c + new Vector2(-3f, num3 * 2f) * num, c + new Vector2(0f, num3 * 6f) * num, col, 2f * num);
		VirtualControls.DrawLine(c + new Vector2(0f, num3 * 6f) * num, c + new Vector2(3f, num3 * 2f) * num, col, 2f * num);
	}

	private static void PadPage(bool act)
	{
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_0362: Unknown result type (might be due to invalid IL or missing references)
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		(KeyCode, string)[] array = new (KeyCode, string)[12]
		{
			((KeyCode)330, "A"),
			((KeyCode)331, "B"),
			((KeyCode)332, "X"),
			((KeyCode)333, "Y"),
			((KeyCode)334, "LB"),
			((KeyCode)335, "RB"),
			((KeyCode)336, "LT"),
			((KeyCode)337, "RT"),
			((KeyCode)338, "Back"),
			((KeyCode)339, "Start"),
			((KeyCode)340, "LS"),
			((KeyCode)341, "RS")
		};
		float num = 54f;
		float num2 = 16f;
		float num3 = Mathf.Min(300f, (UiKit.W - num * 2f - num2 * 3f) / 4f);
		float num4 = 112f;
		float num5 = 150f;
		float num6 = num3 * 4f + num2 * 3f;
		float num7 = (UiKit.W - num6) * 0.5f;
		for (int i = 0; i < array.Length; i++)
		{
			int num8 = i % 4;
			int num9 = i / 4;
			Rect r = new Rect(num7 + (float)num8 * (num3 + num2), num5 + (float)num9 * (num4 + num2), num3, num4);
			bool flag = selected != null && CurKey() == array[i].Item1;
			Rounded(r, flag ? KeyBgOn : KeyBg);
			if (flag)
			{
				Outline(r, SelectRing, 2f);
			}
			Vector2 val = new Vector2(r.center.x, r.y + 36f);
			Color keyTxt = KeyTxt;
			if (array[i].Item2.Length == 1)
			{
				Disc(val, 20f, keyTxt);
				Text(new Rect(val.x - 20f, val.y - 17f, 40f, 34f), array[i].Item2, 21, new Color(0.1f, 0.1f, 0.11f, 1f), (TextAnchor)4);
			}
			else
			{
				Rounded(new Rect(val.x - 32f, val.y - 15f, 64f, 30f), keyTxt);
				Text(new Rect(val.x - 32f, val.y - 15f, 64f, 30f), array[i].Item2, 18, new Color(0.1f, 0.1f, 0.11f, 1f), (TextAnchor)4);
			}
			Text(new Rect(r.x, r.yMax - 40f, r.width, 28f), array[i].Item2, 18, TxtSub, (TextAnchor)4);
			if (Tap(r, act))
			{
				ApplyKey(array[i].Item1);
			}
		}
	}

	private static void ApplyKey(KeyCode k)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		if (selected == null)
		{
			page = 2;
			return;
		}
		if (keyTarget > 0 && selected.type != VCElemType.Button)
		{
			PushHistory();
			VirtualControls.ReleaseStickKeys(selected);
			selected.SetDirKey(keyTarget - 1, k);
			dirty = true;
			VirtualControls.SaveActive();
			keyTarget = 0;
			OpenPage(2);
			return;
		}
		PushHistory();
		selected.key = k;
		selected.shiftMod = keyShift;
		GameAction? gameAction = VirtualControls.ActionOfKey(k);
		if (gameAction.HasValue)
		{
			selected.action = gameAction.Value;
		}
		dirty = true;
		VirtualControls.SaveActive();
		OpenPage(2);
	}

	private static Rect PanelRect()
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		float panelW = PanelW;
		float num = UiKit.W - panelW - 12f;
		if (panelX < 0f)
		{
			panelX = num;
		}
		panelX = Mathf.Clamp(panelX, 12f, Mathf.Max(12f, UiKit.W - panelW - 12f));
		return new Rect(panelX, 8f, panelW, UiKit.H - 16f);
	}

	private static float Ease(float t)
	{
		return t * t * (3f - 2f * t);
	}

	private static void SidePanel(bool act)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_029f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0382: Unknown result type (might be due to invalid IL or missing references)
		//IL_039b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		Rect r = PanelRect();
		r.x += (1f - Ease(Mathf.Clamp01(pageT))) * 60f;
		Rect val = new Rect(r.x, r.y, r.width, 76f);
		if (act && aPressed && val.Contains(aPos))
		{
			panelMoving = true;
			panelMoveLast = aPos.x;
		}
		if (panelMoving)
		{
			if (aDown)
			{
				float num = aPos.x - panelMoveLast;
				panelMoveLast = aPos.x;
				if (Mathf.Abs(num) > 0.4f)
				{
					panelX = Mathf.Clamp(panelX + num, 12f, Mathf.Max(12f, UiKit.W - r.width - 12f));
				}
			}
			else
			{
				panelMoving = false;
			}
		}
		if (act)
		{
			if (aPressed && !val.Contains(aPos) && r.Contains(aPos))
			{
				scrollArm = true;
				scrollArmPos = (scrollLast = aPos);
			}
			if (scrollArm)
			{
				if (!aDown)
				{
					scrollArm = false;
				}
				else
				{
					float num2 = aPos.y - scrollLast.y;
					scrollLast = aPos;
					float num3 = aPos.y - scrollArmPos.y;
					if (dragSlider < 0 && Mathf.Abs(num3) > 10f && Mathf.Abs(num3) > Mathf.Abs(aPos.x - scrollArmPos.x))
					{
						panelScroll = Mathf.Clamp(panelScroll - num2, 0f, panelScrollMax);
					}
				}
			}
		}
		Rounded(new Rect(r.x - 1.5f, r.y - 1.5f, r.width + 3f, r.height + 3f), PanelEdge);
		Rounded(r, PnlBg);
		bodyTop = r.y + 76f;
		bodyBottom = r.yMax;
		Rect b = new Rect(r.x, bodyTop + 6f - panelScroll, r.width, r.height);
		float num4 = page switch
		{
			2 => EditPage(b, act), 
			3 => StylePage(b, act), 
			4 => ShapePage(b, act), 
			_ => ColorPage(b, act), 
		} + 14f - (bodyTop + 6f);
		panelScrollMax = Mathf.Max(0f, num4 - (r.height - 76f - 6f));
		panelScroll = Mathf.Clamp(panelScroll, 0f, panelScrollMax);
		if (panelScrollMax > 0.5f)
		{
			float num5 = r.height - 76f;
			float num6 = Mathf.Max(48f, num5 * (num5 / (num5 + panelScrollMax)));
			float num7 = r.y + 76f + (num5 - num6) * Mathf.Clamp01(panelScroll / panelScrollMax);
			Rounded(new Rect(r.xMax - 7f, num7, 5f, num6), new Color(1f, 1f, 1f, 0.22f));
		}
		UiKit.Fill(new Rect(r.x, r.yMax - 1f, r.width, UiKit.H - r.yMax + 1f), Color.black);
		HeaderMask(r, act);
		if (act && aReleased)
		{
			if (dirty)
			{
				VirtualControls.SaveActive();
				dirty = false;
			}
			dragSlider = -1;
		}
	}

	private static void HeaderMask(Rect r, bool act)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		UiKit.Fill(new Rect(r.x, r.y, r.width, 76f), PnlBg);
		UiKit.Fill(new Rect(r.x + 20f, r.y + 76f - 1f, r.width - 40f, 1f), RowLineCol);
		string s;
		if (page == 3)
		{
			s = "样式设置";
		}
		else if (page == 4)
		{
			s = "选择形状";
		}
		else if (page == 5)
		{
			s = "颜色";
		}
		else if (selected == null)
		{
			s = "编辑";
		}
		else if (selected.type == VCElemType.Stick)
		{
			s = "编辑 摇杆";
		}
		else
		{
			s = ((selected.type == VCElemType.Dpad) ? "编辑 方向键" : "编辑 按钮");
		}
		bool flag = page != 2;
		int p = ((page == 5) ? 3 : 2);
		float num = r.y + 38f;
		float num2 = r.x + 20f;
		if (flag)
		{
			Rect r2 = new Rect(r.x + 8f, num - 22f, 46f, 44f);
			ArrowBack(new Vector2(r2.center.x, r2.center.y), r2.Contains(aPos) ? Color.white : Ink);
			if (Tap(r2, act))
			{
				OpenPage(p);
			}
			num2 = r.x + 62f;
		}
		Text(new Rect(num2, r.y, r.width - 150f, 76f), s, 27, TxtMain, (TextAnchor)3);
		Rect r3 = new Rect(r.xMax - 56f, num - 22f, 46f, 44f);
		XIcon(new Vector2(r3.center.x, r3.center.y), (Color)(r3.Contains(aPos) ? Color.white : new Color(1f, 1f, 1f, 0.72f)));
		if (Tap(r3, act))
		{
			if (dirty)
			{
				VirtualControls.SaveActive();
				dirty = false;
			}
			page = 0;
		}
	}

	private static void SectionLabel(Rect r, float y, string s)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		Text(new Rect(r.x + 20f, y, r.width - 40f, 22f), s, 19, TxtSub, (TextAnchor)3);
	}

	private static void Card(Rect r)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		Rounded(new Rect(r.x - 1f, r.y - 1f, r.width + 2f, r.height + 2f), new Color(1f, 1f, 1f, 0.05f));
		Rounded(r, CardBg);
	}

	private static void RowLine(Rect card, float y)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		UiKit.Fill(new Rect(card.x + 20f, y, card.width - 40f, 1f), RowLineCol);
	}

	private static bool PTap(Rect r, bool act)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		if (r.y >= bodyTop - 0.5f && r.yMax <= bodyBottom + 0.5f)
		{
			return Tap(r, act);
		}
		return false;
	}

	private static bool PVisible(Rect r)
	{
		if (r.y >= bodyTop - 0.5f)
		{
			return r.yMax <= bodyBottom + 0.5f;
		}
		return false;
	}

	private static float SliderRow(Rect row, string title, string value, float v, float min, float max, int id, bool act)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		Text(new Rect(row.x + 20f, row.y + 12f, row.width - 170f, 30f), title, 24, TxtMain, (TextAnchor)3);
		Text(new Rect(row.x, row.y + 12f, row.width - 20f, 30f), value, 21, TxtSub, (TextAnchor)5);
		return Slider(new Rect(row.x + 20f, row.y + 62f, row.width - 40f, 11f), v, min, max, id, act);
	}

	private static bool NavRow(Rect row, string title, string value, bool act)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		Text(new Rect(row.x + 20f, row.y, row.width - 210f, row.height), title, 24, TxtMain, (TextAnchor)3);
		if (!string.IsNullOrEmpty(value))
		{
			Text(new Rect(row.x, row.y, row.width - 48f, row.height), value, 21, TxtSub, (TextAnchor)5);
		}
		ChevronRight(new Vector2(row.xMax - 26f, row.center.y), new Color(1f, 1f, 1f, 0.45f), 8f);
		return PTap(row, act);
	}

	private static bool ColorRow(Rect row, string title, Color c, bool act)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		Text(new Rect(row.x + 20f, row.y, row.width - 260f, row.height), title, 24, TxtMain, (TextAnchor)3);
		Rect r = new Rect(row.xMax - 200f, row.y + (row.height - 40f) * 0.5f, 180f, 40f);
		Rounded(r, ChipBg);
		Disc(new Vector2(r.x + 26f, r.center.y), 13f, new Color(0.55f, 0.55f, 0.57f, 1f));
		Disc(new Vector2(r.x + 26f, r.center.y), 13f, c);
		Text(new Rect(r.x + 48f, r.y, r.width - 58f, r.height), VirtualControls.Hex(c), 20, new Color(0.78f, 0.78f, 0.81f, 1f), (TextAnchor)3);
		return PTap(row, act);
	}

	private static bool ToggleChip(Rect r, string label, bool on, bool act)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		Rounded(r, on ? Accent : ChipBg);
		Text(r, label, 21, on ? Color.white : TxtSub, (TextAnchor)4);
		return Tap(r, act);
	}

	private static float Slider(Rect track, float v, float min, float max, int id, bool act)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.InverseLerp(min, max, v);
		Vector2 c = new Vector2(track.x + track.width * num, track.center.y);
		Rounded(track, TrackBg);
		Disc(c, 17f, Color.white);
		Rect val = new Rect(track.x - 16f, track.y - 22f, track.width + 32f, track.height + 44f);
		if (act && aPressed && aPos.y >= bodyTop && aPos.y <= bodyBottom && val.Contains(aPos))
		{
			dragSlider = id;
			PushHistory();
		}
		if (dragSlider == id)
		{
			if (aDown)
			{
				float num2 = Mathf.Lerp(min, max, Mathf.Clamp01((aPos.x - track.x) / Mathf.Max(1f, track.width)));
				if (!Mathf.Approximately(num2, v))
				{
					dirty = true;
				}
				v = num2;
			}
			else
			{
				dragSlider = -1;
			}
		}
		return v;
	}

	private static float EditPage(Rect b, bool act)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_028e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		VCElem vCElem = selected;
		if (vCElem == null)
		{
			page = 0;
			return b.y;
		}
		if (vCElem.type != VCElemType.Button)
		{
			return StickPage(b, vCElem, act);
		}
		float num = b.x + 20f;
		float num2 = b.width - 40f;
		float y = b.y;
		SectionLabel(b, y, "基础属性");
		y += 26f;
		Rect val = new Rect(num, y, num2, 242f);
		Card(val);
		float y2 = val.y;
		vCElem.scale = SliderRow(new Rect(num, y2, num2, 80f), "整体缩放", vCElem.scale.ToString("0.0"), vCElem.scale, 0.4f, 3f, 1, act);
		y2 += 80f;
		RowLine(val, y2);
		vCElem.rotation = SliderRow(new Rect(num, y2, num2, 80f), "旋转角度", Mathf.RoundToInt(vCElem.rotation) + "°", vCElem.rotation, -180f, 180f, 2, act);
		y2 += 80f;
		RowLine(val, y2);
		vCElem.aspect = SliderRow(new Rect(num, y2, num2, 80f), "纵横比例", vCElem.aspect.ToString("0.0"), vCElem.aspect, 0.4f, 2.5f, 3, act);
		y = val.yMax + 8f;
		SectionLabel(b, y, "外观样式");
		y += 26f;
		val = new Rect(num, y, num2, 200f);
		Card(val);
		y2 = val.y;
		vCElem.opacity = SliderRow(new Rect(num, y2, num2, 80f), "不透明度", Mathf.RoundToInt(vCElem.opacity * 100f) + "%", vCElem.opacity, 0.05f, 1f, 4, act);
		y2 += 80f;
		RowLine(val, y2);
		if (NavRow(new Rect(num, y2, num2, 58f), "样式设置", "", act))
		{
			OpenPage(3);
		}
		y2 += 58f;
		RowLine(val, y2);
		if (NavRow(new Rect(num, y2, num2, 58f), "形状", ShapeName(vCElem.shape), act))
		{
			OpenPage(4);
		}
		y = val.yMax + 8f;
		SectionLabel(b, y, "按键映射");
		y += 26f;
		val = new Rect(num, y, num2, 58f);
		Card(val);
		string value = (((int)vCElem.key == 0) ? "未绑定" : ((vCElem.shiftMod ? "Shift + " : "") + VirtualControls.KeyLabel(vCElem.key)));
		if (NavRow(new Rect(num, val.y, num2, 58f), "选择按键", value, act))
		{
			keyTarget = 0;
			OpenPage(6);
		}
		return val.yMax;
	}

	private static string ShapeName(int s)
	{
		if (s < 0 || s >= ShapeNames.Length)
		{
			return "圆形";
		}
		return ShapeNames[s];
	}

	private static float StickPage(Rect b, VCElem e, bool act)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		float num = b.x + 20f;
		float num2 = b.width - 40f;
		float y = b.y;
		SectionLabel(b, y, "输入模式");
		y += 26f;
		Rect r = new Rect(num, y, num2, 66f);
		int num3 = Seg2(r, "标准输入", "自定义", (!e.stickStd) ? 1 : 0, act);
		if (num3 == 0 && !e.stickStd)
		{
			PushHistory();
			VirtualControls.ReleaseStickKeys(e);
			e.stickStd = true;
			dirty = true;
		}
		else if (num3 == 1 && e.stickStd)
		{
			PushHistory();
			e.stickStd = false;
			dirty = true;
		}
		y = r.yMax + 12f;
		Text(new Rect(num, y, num2, 30f), e.stickStd ? "直接输出上下左右移动，不用绑键盘。" : "推向某个方向时，同时按下该方向绑的键盘键。", 18, TxtSub, (TextAnchor)3);
		y += 40f;
		if (!e.stickStd && !e.HasDirKey)
		{
			Text(new Rect(num, y, num2, 28f), "四个方向都还没绑键，摇杆目前不会输出。", 17, new Color(1f, 0.72f, 0.3f, 1f), (TextAnchor)3);
			y += 32f;
		}
		if (!e.stickStd)
		{
			SectionLabel(b, y, "按键映射");
			y += 26f;
			Rect val = new Rect(num, y, num2, 235f);
			Card(val);
			for (int i = 0; i < 4; i++)
			{
				if (DirRow(new Rect(num, val.y + 58f * (float)i, num2, 58f), i, e.DirKey(i), act))
				{
					keyTarget = i + 1;
					OpenPage(6);
				}
				if (i < 3)
				{
					RowLine(val, val.y + 58f * (float)(i + 1));
				}
			}
			y = val.yMax + 8f;
		}
		SectionLabel(b, y, "基础属性");
		y += 26f;
		Rect r2 = new Rect(num, y, num2, 81f);
		Card(r2);
		e.scale = SliderRow(new Rect(num, r2.y, num2, 80f), "整体缩放", e.scale.ToString("0.0"), e.scale, 0.4f, 3f, 1, act);
		return r2.yMax;
	}

	private static int Seg2(Rect r, string left, string right, int cur, bool act)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		Rounded(r, ChipBg);
		float num = r.width * 0.5f;
		for (int i = 0; i < 2; i++)
		{
			Rect r2 = new Rect(r.x + num * (float)i, r.y, num, r.height);
			if (i == cur)
			{
				Rounded(new Rect(r2.x + 4f, r2.y + 4f, r2.width - 8f, r2.height - 8f), Accent);
			}
			Text(r2, (i == 0) ? left : right, 23, (i == cur) ? Color.white : TxtSub, (TextAnchor)4);
			if (PTap(r2, act))
			{
				return i;
			}
		}
		return cur;
	}

	private static bool DirRow(Rect row, int dir, KeyCode k, bool act)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Invalid comparison between Unknown and I4
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		ChevronDir(new Vector2(row.x + 20f + 15f, row.center.y), dir, new Color(0.82f, 0.84f, 0.87f, 1f), 9f);
		Text(new Rect(row.x + 20f + 42f, row.y, row.width - 230f, row.height), DirNames[dir], 24, TxtMain, (TextAnchor)3);
		bool flag = (int)k == 0;
		Text(new Rect(row.x, row.y, row.width - 58f, row.height), flag ? "无" : VirtualControls.KeyLabel(k), 22, (Color)(flag ? new Color(0.46f, 0.46f, 0.49f, 1f) : TxtMain), (TextAnchor)5);
		ChevronRight(new Vector2(row.xMax - 28f, row.center.y), new Color(1f, 1f, 1f, 0.42f), 8f);
		return PTap(row, act);
	}

	private static void ChevronDir(Vector2 c, int dir, Color col, float s)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		int times = ((dir >= 0 && dir < 4) ? DirRot[dir] : 0);
		Vector2 val = Rot90(new Vector2((0f - s) * 0.6f, 0f - s), times);
		Vector2 val2 = Rot90(new Vector2(s * 0.6f, 0f), times);
		Vector2 val3 = Rot90(new Vector2((0f - s) * 0.6f, s), times);
		VirtualControls.DrawLine(c + val, c + val2, col, 2.6f);
		VirtualControls.DrawLine(c + val2, c + val3, col, 2.6f);
	}

	private static Vector2 Rot90(Vector2 v, int times)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < times; i++)
		{
			v = new Vector2(0f - v.y, v.x);
		}
		return v;
	}

	private static float StylePage(Rect b, bool act)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		VCElem vCElem = selected;
		if (vCElem == null)
		{
			page = 0;
			return b.y;
		}
		float num = b.x + 20f;
		float num2 = b.width - 40f;
		float y = b.y;
		SectionLabel(b, y, "基础设置");
		y += 26f;
		Rect val = new Rect(num, y, num2, 161f);
		Card(val);
		float y2 = val.y;
		vCElem.borderW = SliderRow(new Rect(num, y2, num2, 80f), "描边宽度", vCElem.borderW.ToString("0.0") + "dp", vCElem.borderW, 0f, 8f, 11, act);
		y2 += 80f;
		RowLine(val, y2);
		vCElem.halo = SliderRow(new Rect(num, y2, num2, 80f), "光环宽度", Mathf.RoundToInt(vCElem.halo * 100f) + "%", vCElem.halo, 0f, 1f, 12, act);
		y = val.yMax + 8f;
		SectionLabel(b, y, "默认状态");
		y += 26f;
		val = new Rect(num, y, num2, 117f);
		Card(val);
		if (ColorRow(new Rect(num, val.y, num2, 58f), "背景颜色", vCElem.bg, act))
		{
			colorTarget = 0;
			OpenColor(vCElem.bg);
		}
		RowLine(val, val.y + 58f);
		if (ColorRow(new Rect(num, val.y + 58f, num2, 58f), "描边颜色", vCElem.line, act))
		{
			colorTarget = 1;
			OpenColor(vCElem.line);
		}
		y = val.yMax + 8f;
		SectionLabel(b, y, "按下状态");
		y += 26f;
		val = new Rect(num, y, num2, 117f);
		Card(val);
		if (ColorRow(new Rect(num, val.y, num2, 58f), "背景颜色", vCElem.bgDown, act))
		{
			colorTarget = 2;
			OpenColor(vCElem.bgDown);
		}
		RowLine(val, val.y + 58f);
		if (ColorRow(new Rect(num, val.y + 58f, num2, 58f), "描边颜色", vCElem.lineDown, act))
		{
			colorTarget = 3;
			OpenColor(vCElem.lineDown);
		}
		return val.yMax;
	}

	private static float ShapePage(Rect b, bool act)
	{
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		VCElem vCElem = selected;
		if (vCElem == null)
		{
			page = 0;
			return b.y;
		}
		float num = b.x + 20f;
		float num2 = b.width - 40f;
		float y = b.y;
		float num3 = 14f;
		float num4 = (num2 - num3 * 2f) / 3f;
		float num5 = 150f;
		for (int i = 0; i < 8; i++)
		{
			int num6 = i % 3;
			int num7 = i / 3;
			Rect r = new Rect(num + (float)num6 * (num4 + num3), y + (float)num7 * (num5 + num3), num4, num5);
			bool flag = vCElem.shape == i;
			Color val = (Color)(flag ? new Color(0.06f, 0.2f, 0.29f, 1f) : CardBg);
			Rounded(r, val);
			if (flag)
			{
				StrokeRect(r, SelectRing, val, 2.4f);
			}
			Vector2 val2 = new Vector2(r.center.x, r.y + 58f);
			Color line = new Color(1f, 1f, 1f, 0.92f);
			if (i < 7)
			{
				Matrix4x4 matrix = GUI.matrix;
				GUI.matrix = matrix * Matrix4x4.TRS(new Vector3(val2.x, val2.y, 0f), Quaternion.identity, Vector3.one);
				VirtualControls.DrawShape(i, 36f, new Color(1f, 1f, 1f, 0.1f), line, 2.6f, 0f);
				GUI.matrix = matrix;
			}
			Text(new Rect(r.x, r.yMax - 52f, r.width, 34f), ShapeNames[i], 21, (i == 7) ? TxtSub : TxtMain, (TextAnchor)4);
			if (PTap(r, act))
			{
				if (vCElem.shape != i)
				{
					PushHistory();
					vCElem.shape = i;
					dirty = true;
				}
				OpenPage(2);
			}
		}
		return y + 3f * num5 + num3 * 2f - 4f;
	}

	private static void OpenColor(Color c)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		RgbToHsv(c, out ch, out cs, out cv);
		ca = c.a;
		colorOrigin = c;
		OpenPage(5);
	}

	private static Color CurrentColor()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		return HsvToRgb(ch, cs, cv, ca);
	}

	private static float ColorPage(Rect b, bool act)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		VCElem vCElem = selected;
		if (vCElem == null)
		{
			page = 0;
			return b.y;
		}
		float num = b.x + 20f;
		float num2 = b.width - 40f;
		float y = b.y;
		Rect r = new Rect(num, y, num2, 140f);
		Rounded(r, ChipBg);
		Rounded(new Rect(r.x + 10f, r.y + 10f, r.width - 20f, r.height - 20f), CurrentColor());
		Color col = HsvToRgb(ch, cs, cv, 1f);
		Rect r2 = new Rect(r.x + 12f, r.yMax - 40f, 64f, 26f);
		Rounded(r2, col);
		Text(new Rect(r2.xMax + 14f, r2.y - 4f, r.width - 110f, 32f), VirtualControls.Hex(CurrentColor()), 20, new Color(0.82f, 0.82f, 0.85f, 1f), (TextAnchor)3);
		y = r.yMax + 16f;
		Rect val = new Rect(num, y, num2, 323f);
		Card(val);
		float y2 = val.y;
		ch = SliderRow(new Rect(num, y2, num2, 80f), "色相", Mathf.RoundToInt(ch * 360f) + "°", ch, 0f, 1f, 21, act);
		y2 += 80f;
		RowLine(val, y2);
		cs = SliderRow(new Rect(num, y2, num2, 80f), "饱和度", Mathf.RoundToInt(cs * 100f) + "%", cs, 0f, 1f, 22, act);
		y2 += 80f;
		RowLine(val, y2);
		cv = SliderRow(new Rect(num, y2, num2, 80f), "亮度", Mathf.RoundToInt(cv * 100f) + "%", cv, 0f, 1f, 23, act);
		y2 += 80f;
		RowLine(val, y2);
		ca = SliderRow(new Rect(num, y2, num2, 80f), "透明度", Mathf.RoundToInt(ca * 100f) + "%", ca, 0f, 1f, 24, act);
		Color val2 = CurrentColor();
		if (!SameColor(val2, TargetColor(vCElem)))
		{
			SetTargetColor(vCElem, val2);
			dirty = true;
		}
		return val.yMax;
	}

	private static bool SameColor(Color a, Color b)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		if (Mathf.Abs(a.r - b.r) < 0.002f && Mathf.Abs(a.g - b.g) < 0.002f && Mathf.Abs(a.b - b.b) < 0.002f)
		{
			return Mathf.Abs(a.a - b.a) < 0.002f;
		}
		return false;
	}

	private static Color TargetColor(VCElem e)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		return colorTarget switch
		{
			0 => e.bg, 
			1 => e.line, 
			2 => e.bgDown, 
			_ => e.lineDown, 
		};
	}

	private static void SetTargetColor(VCElem e, Color c)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		switch (colorTarget)
		{
		case 0:
			e.bg = c;
			break;
		case 1:
			e.line = c;
			break;
		case 2:
			e.bgDown = c;
			break;
		default:
			e.lineDown = c;
			break;
		}
	}

	private static void RgbToHsv(Color c, out float h, out float s, out float v)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
		float num2 = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
		v = num;
		float num3 = num - num2;
		s = ((num <= 0.0001f) ? 0f : (num3 / num));
		if (num3 <= 0.0001f)
		{
			h = 0f;
			return;
		}
		if (num == c.r)
		{
			h = Mathf.Repeat((c.g - c.b) / num3, 6f);
		}
		else if (num == c.g)
		{
			h = (c.b - c.r) / num3 + 2f;
		}
		else
		{
			h = (c.r - c.g) / num3 + 4f;
		}
		h /= 6f;
	}

	private static Color HsvToRgb(float h, float s, float v, float a)
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		h = Mathf.Repeat(h, 1f);
		float num = Mathf.Floor(h * 6f);
		float num2 = h * 6f - num;
		float num3 = v * (1f - s);
		float num4 = v * (1f - num2 * s);
		float num5 = v * (1f - (1f - num2) * s);
		return ((int)num % 6) switch
		{
			0 => new Color(v, num5, num3, a), 
			1 => new Color(num4, v, num3, a), 
			2 => new Color(num3, v, num5, a), 
			3 => new Color(num3, num4, v, a), 
			4 => new Color(num5, num3, v, a), 
			_ => new Color(v, num3, num4, a), 
		};
	}

	static VirtualControlsEditor()
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a48: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a4d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a66: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a6b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a84: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a89: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aa2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aa7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ac0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ac5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ade: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ae3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0afc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b01: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b1a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b1f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b38: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b3d: Unknown result type (might be due to invalid IL or missing references)
	}
}
