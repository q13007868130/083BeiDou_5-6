using System.Collections.Generic;
using System.Text;
using MapleDash.Wz;
using UnityEngine;

public static class SkillWindow
{
	public static bool Visible;

	private static Vector2 pos = new Vector2(-1f, -1f);

	private static int tab;

	private static int selected = -1;

	private static int scroll;

	private const int WinW = 175;

	private const int WinH = 289;

	private const int TabY = 23;

	private const int TabH = 19;

	private const int TabX = 3;

	private const int TabW = 34;

	private const int TabGlyphY = 4;

	private const int BookX = 10;

	private const int BookY = 55;

	private const int BookW = 26;

	private const int BookH = 30;

	private const int BookTitleX = 45;

	private const int BookTitleY = 61;

	private const int BookTitleW = 123;

	private const int BookTitleH = 22;

	private const int RowX = 7;

	private const int RowY = 99;

	private const int RowStep = 40;

	private const int RowW = 141;

	private const int RowH = 35;

	private const int Rows = 4;

	private const int RowHitW = 154;

	private const int RowHitH = 38;

	private const int IconOffX = 1;

	private const int IconOffY = 1;

	private const int IconSize = 32;

	private const int NameX = 45;

	private const int NameY = 0;

	private const int NameW = 100;

	private const int NameH = 14;

	private const int LevelX = 45;

	private const int LevelY = 20;

	private const int LevelW = 60;

	private const int LevelH = 12;

	private const int SpUpX = 131;

	private const int SpUpY = 20;

	private const int SpUpSize = 12;

	private const int ScrollX = 153;

	private const int ScrollW = 15;

	private const int ScrollArrowH = 13;

	private const int ScrollTop = 99;

	private const int ScrollBottom = 254;

	private const int FooterX = 58;

	private const int FooterY = 265;

	private const int FooterW = 52;

	private const int FooterH = 17;

	private const int MacroX = 120;

	private const int MacroY = 265;

	private const int MacroW = 48;

	private const int MacroH = 18;

	private const int RecOffX = 36;

	private const int RecW = 105;

	private const int RecH = 35;

	private static readonly Color HeaderText = Color.white;

	private static readonly Color NameOn = new Color(0.1f, 0.11f, 0.15f);

	private static readonly Color NameOff = new Color(0.1f, 0.11f, 0.15f);

	private static readonly Color LevelText = new Color(0.1f, 0.11f, 0.15f);

	private static readonly Color SpNumber = new Color(0.1f, 0.23f, 0.72f);

	private static readonly Color SpZero = new Color(0.1f, 0.11f, 0.15f);

	private static bool dragging;

	private static Vector2 dragOffset;

	private static bool macroVisible = true;

	private static int macroScroll;

	private static int macroSelected = -1;

	private static int macroLoadCid;

	private static bool macroDirty;

	private static readonly int[,] macroSkills = new int[5, 3];

	private static readonly string[] macroNames = new string[5];

	private static readonly bool[] macroShowName = new bool[5];

	private static bool scrollDrag;

	private static float scrollDragGrab;

	private static string tipName;

	private static float tipUntil;

	private const int TipW = 320;

	private const int TipHeader = 114;

	private const int TipStep = 16;

	private const int TipDescX = 87;

	private const int TipRight = 20;

	private const int TipIconX = 10;

	private const int TipIconY = 32;

	private const int TipIconBox = 68;

	private static Texture2D tipWhite;

	private static readonly Dictionary<string, List<List<KeyValuePair<string, Color>>>> tipWrap = new Dictionary<string, List<List<KeyValuePair<string, Color>>>>();

	private static Dictionary<string, object> recommend;

	private static bool recommendTried;

	private static int dragId;

	private static bool carrying;

	private static bool externalCarry;

	private static float S => BottomBar.S;

	private static Dictionary<string, object> Line => SkillBook.TreeForJob(Profile.JobId);

	private static List<Dictionary<string, object>> Tabs => SkillBook.Tabs(Line);

	private static Rect MacroRect
	{
		get
		{
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			return new Rect((pos.x + 175f - 2f) * S, pos.y * S, 207f * S, 289f * S);
		}
	}

	private static Texture2D TipWhite
	{
		get
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Expected Obj, but got Unknown
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)tipWhite == (Object)null)
			{
				tipWhite = new Texture2D(1, 1);
				tipWhite.SetPixel(0, 0, Color.white);
				tipWhite.Apply();
			}
			return tipWhite;
		}
	}

	public static bool Dragging
	{
		get
		{
			if (dragId != 0)
			{
				return carrying;
			}
			return false;
		}
	}

	public static int DragSkillId => dragId;

	private static Texture2D Tex(string name)
	{
		string? text;
		if (name.StartsWith("spup"))
		{
			text = "BtSpUp/" + ((name == "spup") ? "normal" : name.Substring(5)) + "/0";
		}
		else if (name.StartsWith("cool_"))
		{
			text = "CoolTime/" + name.Substring(5);
		}
		else
		{
			text = ((name == "recommend") ? "recommend/0" : name);
		}
		return Wz("UI/UIWindow.img/Skill/" + text);
	}

	private static Texture2D Wz(string node)
	{
		return Art.Tex("wz://" + node);
	}

	private static Texture2D SkillIcon(SkillData d)
	{
		if (d != null && !string.IsNullOrEmpty(d.icon))
		{
			return Art.Tex(d.icon);
		}
		return null;
	}

	private static Rect At(Rect win, float x, float y, float w, float h)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(win.x + x * S, win.y + y * S, w * S, h * S);
	}

	private static GUIStyle Style(float size, Color color, TextAnchor anchor, bool wrap = false, bool clip = false)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected Obj, but got Unknown
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = Mathf.Max(7, Mathf.RoundToInt(size * S)),
			alignment = anchor,
			wordWrap = wrap,
			clipping = (TextClipping)(clip ? 1 : 0)
		};
		val.normal.textColor = color;
		return val;
	}

	public static void Toggle()
	{
		Visible = !Visible;
		if (!Visible)
		{
			CancelDrag();
		}
	}

	private static List<int> TabSkills()
	{
		List<Dictionary<string, object>> tabs = Tabs;
		if (tabs.Count == 0)
		{
			tab = 0;
			return new List<int>();
		}
		tab = Mathf.Clamp(tab, 0, tabs.Count - 1);
		List<int> list = new List<int>();
		if (tabs[tab].TryGetValue("skills", out var value))
		{
			foreach (object item in (List<object>)value)
			{
				list.Add(int.Parse(item.ToString()));
			}
		}
		if (Art.Int(tabs[tab], "job") == 0 && SkillProgress.LevelOf(1016) > 0 && !list.Contains(1016))
		{
			list.Add(1016);
		}
		int num = Art.Int(tabs[tab], "job") * 10000 + 1004;
		if (NativeMount.IsSkill(num) && SkillProgress.LevelOf(num) > 0 && !list.Contains(num))
		{
			list.Add(num);
		}
		return list;
	}

	private static void ClampState(List<int> list)
	{
		if (list.Count == 0)
		{
			selected = -1;
			scroll = 0;
			return;
		}
		scroll = Mathf.Clamp(scroll, 0, Mathf.Max(0, list.Count - 4));
		if (selected >= list.Count)
		{
			selected = list.Count - 1;
		}
	}

	public static void Draw()
	{
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible && PlayerPrefs.GetInt("debug_skillwin", 0) == 1)
		{
			PlayerPrefs.DeleteKey("debug_skillwin");
			Visible = true;
		}
		if (!Visible)
		{
			if (externalCarry)
			{
				HandleDrop();
			}
			else
			{
				CancelDrag();
			}
		}
		else
		{
			if ((Object)(object)GameLoop.I == (Object)null || (Object)(object)GameLoop.I.player == (Object)null)
			{
				return;
			}
			if (pos.x < 0f)
			{
				pos = new Vector2(18f, 70f);
			}
			ClampWindow();
			Rect val = new Rect(pos.x * S, pos.y * S, 175f * S, 289f * S);
			HandleWindowDrag(val);
			val.position = pos * S;
			Rect rect;
			if (!macroVisible)
			{
				rect = val;
			}
			else
			{
				float x = val.x;
				float y = val.y;
				Rect macroRect = MacroRect;
				rect = Rect.MinMaxRect(x, y, macroRect.xMax, val.yMax);
			}
			NativeUi.RegisterWindow("SkillWindow", rect, () => Visible);
			List<Dictionary<string, object>> tabs = Tabs;
			List<int> list = TabSkills();
			HandleTabInput(val, tabs);
			list = TabSkills();
			HandleScroll(val, list);
			HandleScrollbar(val, list);
			ClampState(list);
			int hover = RowAt(val, list);
			if (Dragging && Pointer.Pressed)
			{
				bool flag = DropOnSlot(dragId);
				CancelDrag();
				if (flag)
				{
					return;
				}
			}
			HandleRows(val, list);
			HandleHints(val, list, hover);
			UpdateCursor(val, list, hover);
			DrawMacro();
			DrawBackground(val);
			if (NativeUi.Button(At(val, 156f, 5f, 12f, 12f), "UI/Basic.img/BtClose"))
			{
				Visible = false;
				CancelDrag();
				return;
			}
			DrawTabs(val, tabs);
			DrawHeader(val);
			DrawRows(val, list, hover);
			DrawScrollbar(val, list);
			DrawFooter(val);
			DrawTooltip(val, list, hover);
			DrawTip();
		}
	}

	private static void ClampWindow()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		float num = (macroVisible ? 380 : 175);
		pos = NativeUi.ClampWindowPosition(pos, new Vector2(num, 289f), S);
	}

	private static void HandleWindowDrag(Rect win)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Invalid comparison between Unknown and I4
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		Rect val = At(win, 0f, 0f, 175f, 22f);
		if ((int)Event.current.type == 7 && Pointer.Pressed && val.Contains(Pointer.Pos))
		{
			dragging = true;
			dragOffset = Pointer.Pos / S - pos;
		}
		if (dragging && Pointer.Down)
		{
			pos = Pointer.Pos / S - dragOffset;
			ClampWindow();
		}
		if (!Pointer.Down)
		{
			dragging = false;
		}
	}

	private static float TabWidth(int n)
	{
		return Mathf.Min(34f, 168f / (float)Mathf.Max(1, n));
	}

	private static float TabLeft(int i, int n)
	{
		return 3f + (float)i * TabWidth(n);
	}

	private static void HandleTabInput(Rect win, List<Dictionary<string, object>> tabs)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		if ((int)Event.current.type != 7 || !Pointer.Pressed)
		{
			return;
		}
		for (int i = 0; i < tabs.Count && i < 6; i++)
		{
			Rect val = At(win, TabLeft(i, tabs.Count), 23f, TabWidth(tabs.Count), 19f);
			if (val.Contains(Pointer.Pos))
			{
				if (tab != i)
				{
					tab = i;
					selected = -1;
					scroll = 0;
					CancelDrag();
				}
				break;
			}
		}
	}

	private static void DrawBackground(Rect win)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Tex("backgrnd");
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(win, (Texture)(object)val, (ScaleMode)0);
		}
	}

	private static void DrawTabPlate(Rect r, bool on)
	{
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		string text = (on ? "1" : "0");
		Texture2D val = Wz("UI/Basic.img/Tab2/left" + text);
		Texture2D val2 = Wz("UI/Basic.img/Tab2/fill" + text);
		Texture2D val3 = Wz("UI/Basic.img/Tab2/right" + text);
		float num = 4f * S;
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(new Rect(r.x + num, r.y, r.width - num * 2f, r.height), (Texture)(object)val2);
		}
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(r.x, r.y, num, r.height), (Texture)(object)val);
		}
		if ((Object)(object)val3 != (Object)null)
		{
			GUI.DrawTexture(new Rect(r.xMax - num, r.y, num, r.height), (Texture)(object)val3);
		}
	}

	private static void DrawTabs(Rect win, List<Dictionary<string, object>> tabs)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < tabs.Count && i < 6; i++)
		{
			bool flag = i == tab;
			Rect r = At(win, TabLeft(i, tabs.Count), 23f, TabWidth(tabs.Count), 19f);
			DrawTabPlate(r, flag);
			Texture2D val = Wz("UI/UIWindow.img/Skill/Tab/" + (flag ? "enabled" : "disabled") + "/" + i);
			if (!((Object)(object)val == (Object)null))
			{
				GUI.DrawTexture(new Rect(r.x + (r.width - (float)((Texture)val).width * S) * 0.5f, r.y + 4f * S, (float)((Texture)val).width * S, (float)((Texture)val).height * S), (Texture)(object)val, (ScaleMode)0);
			}
		}
	}

	private static void DrawHeader(Rect win)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		List<Dictionary<string, object>> tabs = Tabs;
		if (tab >= 0 && tab < tabs.Count)
		{
			Dictionary<string, object> dictionary = tabs[tab];
			Texture2D val = Art.Tex("wz://Skill/" + Art.Int(dictionary, "job").ToString("D3") + ".img/info/icon");
			if ((Object)(object)val != (Object)null)
			{
				GUI.DrawTexture(At(win, 10f, 55f, 26f, 30f), (Texture)(object)val, (ScaleMode)0);
			}
			string text = (dictionary.TryGetValue("book", out var value) ? (value as string) : null);
			if (string.IsNullOrEmpty(text))
			{
				text = (dictionary.TryGetValue("name", out var value2) ? (value2 as string) : "");
			}
			GUI.Label(At(win, 45f, 61f, 123f, 22f), text, Style(12f, HeaderText, (TextAnchor)4, wrap: false, clip: true));
		}
	}

	private static int RowAt(Rect win, List<int> list)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		Rect val = At(win, 153f, 99f, 15f, 155f);
		if (val.Contains(Pointer.Pos))
		{
			return -1;
		}
		for (int i = 0; i < 4; i++)
		{
			int num = scroll + i;
			if (num >= list.Count)
			{
				break;
			}
			float y = 99 + i * 40;
			Rect val2 = At(win, 7f, y, 154f, 38f);
			if (val2.Contains(Pointer.Pos))
			{
				return num;
			}
		}
		return -1;
	}

	private static void UpdateCursor(Rect win, List<int> list, int hover)
	{
		if (carrying && dragId != 0)
		{
			NativeCursor.Request(12);
		}
		else if (!Pointer.Down && !Pointer.RightDown && hover >= 0 && hover < list.Count)
		{
			SkillData skillData = SkillBook.Get(list[hover]);
			if (skillData != null && SkillProgress.LevelOf(skillData.id) > 0)
			{
				NativeCursor.Request(11);
			}
		}
	}

	private static void HandleScroll(Rect win, List<int> list)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		if ((int)Event.current.type != 7)
		{
			return;
		}
		Rect val = At(win, 7f, 87f, 161f, 169f);
		if (val.Contains(Pointer.Pos))
		{
			float y = Input.mouseScrollDelta.y;
			if (!(Mathf.Abs(y) < 0.01f))
			{
				int num = Mathf.Max(0, list.Count - 4);
				scroll = Mathf.Clamp(scroll - (int)Mathf.Sign(y), 0, num);
			}
		}
	}

	private static void HandleRows(Rect win, List<int> list)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		if ((int)Event.current.type != 7)
		{
			return;
		}
		Rect val = At(win, 153f, 99f, 15f, 155f);
		if (val.Contains(Pointer.Pos))
		{
			return;
		}
		if (Pointer.DoubleClicked)
		{
			for (int i = 0; i < 4; i++)
			{
				int num = scroll + i;
				if (num >= list.Count)
				{
					break;
				}
				float num2 = 99 + i * 40;
				val = At(win, 7f, num2, 154f, 38f);
				if (!val.Contains(Pointer.Pos))
				{
					continue;
				}
				SkillData skillData = SkillBook.Get(list[num]);
				selected = num;
				CancelDrag();
				if (skillData == null)
				{
					return;
				}
				val = At(win, 131f, num2 + 20f, 12f, 12f);
				if (val.Contains(Pointer.Pos))
				{
					if (!SkillProgress.Raise(skillData))
					{
						string text = SkillProgress.WhyNot(skillData);
						if (!string.IsNullOrEmpty(text))
						{
							NativeUtilPrompt.Notice(text);
						}
					}
				}
				else if (skillData.id % 10000 == 1007)
				{
					NativeMakerWindow.Open();
				}
				else
				{
					SkillCast.TryCast(skillData.id, out var _);
				}
				return;
			}
			CancelDrag();
		}
		else
		{
			if (!Pointer.Pressed)
			{
				return;
			}
			for (int j = 0; j < 4; j++)
			{
				int num3 = scroll + j;
				if (num3 >= list.Count)
				{
					break;
				}
				SkillData skillData2 = SkillBook.Get(list[num3]);
				if (skillData2 == null)
				{
					continue;
				}
				float num4 = 99 + j * 40;
				Rect val2 = At(win, 7f, num4, 154f, 38f);
				if (!val2.Contains(Pointer.Pos))
				{
					continue;
				}
				Rect val3 = At(win, 131f, num4 + 20f, 12f, 12f);
				selected = num3;
				if (val3.Contains(Pointer.Pos))
				{
					CancelDrag();
					if (!SkillProgress.Raise(skillData2))
					{
						string text2 = SkillProgress.WhyNot(skillData2);
						if (!string.IsNullOrEmpty(text2))
						{
							NativeUtilPrompt.Notice(text2);
						}
					}
				}
				else if (SkillProgress.LevelOf(skillData2.id) > 0)
				{
					dragId = skillData2.id;
					carrying = true;
				}
				break;
			}
		}
	}

	private static void HandleHints(Rect win, List<int> list, int hover)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		if (!string.IsNullOrEmpty(tipName) && Time.unscaledTime < tipUntil)
		{
			return;
		}
		Rect val = At(win, 58f, 265f, 52f, 17f);
		if (val.Contains(Pointer.Pos))
		{
			ShowTip("tip2");
			return;
		}
		int num = hover - scroll;
		if (num < 0 || num >= 4 || hover >= list.Count)
		{
			return;
		}
		float num2 = 99 + num * 40;
		val = At(win, 8f, num2 + 1f, 32f, 32f);
		if (val.Contains(Pointer.Pos))
		{
			ShowTip("tip0");
			return;
		}
		val = At(win, 131f, num2 + 20f, 12f, 12f);
		if (val.Contains(Pointer.Pos))
		{
			ShowTip("tip1");
		}
	}

	private static void DrawRows(Rect win, List<int> list, int hover)
	{
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0323: Unknown result type (might be due to invalid IL or missing references)
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		SkillSystem skillSystem = (((Object)(object)GameLoop.I.player != (Object)null) ? ((Component)GameLoop.I.player).GetComponent<SkillSystem>() : null);
		int num = RecommendedId();
		for (int i = 0; i < 4; i++)
		{
			int num2 = scroll + i;
			if (num2 >= list.Count)
			{
				break;
			}
			SkillData skillData = SkillBook.Get(list[num2]);
			if (skillData == null)
			{
				continue;
			}
			float num3 = 99 + i * 40;
			Rect val = At(win, 7f, num3, 141f, 35f);
			Texture2D val2 = Tex("skill1");
			if ((Object)(object)val2 != (Object)null)
			{
				GUI.DrawTexture(val, (Texture)(object)val2, (ScaleMode)0);
			}
			if (num == skillData.id)
			{
				Texture2D val3 = Tex("recommend");
				if ((Object)(object)val3 != (Object)null)
				{
					GUI.DrawTexture(At(win, 43f, num3, 105f, 35f), (Texture)(object)val3, (ScaleMode)0);
				}
			}
			Rect val4 = At(win, 8f, num3 + 1f, 32f, 32f);
			Texture2D val5 = SkillIcon(skillData);
			if ((Object)(object)val5 != (Object)null)
			{
				GUI.DrawTexture(val4, (Texture)(object)val5, (ScaleMode)0);
			}
			if ((Object)(object)skillSystem != (Object)null)
			{
				float num4 = skillSystem.RemainingById(skillData.id);
				if (num4 > 0.001f)
				{
					float num5 = Mathf.Max(0.01f, skillSystem.CooldownOf(skillData.id));
					Texture2D val6 = Tex("cool_" + Mathf.Clamp(Mathf.RoundToInt((1f - Mathf.Clamp01(num4 / num5)) * 15f), 0, 15));
					if ((Object)(object)val6 != (Object)null)
					{
						GUI.DrawTexture(val4, (Texture)(object)val6, (ScaleMode)0);
					}
				}
			}
			int num6 = SkillProgress.LevelOf(skillData.id);
			GUIStyle val7 = Style(12f, (num6 > 0) ? NameOn : NameOff, (TextAnchor)3, wrap: false, clip: true);
			GUIStyle val8 = Style(12f, LevelText, (TextAnchor)3, wrap: false, clip: true);
			GUI.Label(At(win, 45f, num3 + 0f, 100f, 14f), skillData.name, val7);
			GUI.Label(At(win, 45f, num3 + 20f, 60f, 12f), num6.ToString(), val8);
			Rect val9 = At(win, 131f, num3 + 20f, 12f, 12f);
			bool flag = SkillProgress.CanRaise(skillData);
			bool flag2 = val9.Contains(Pointer.Pos);
			bool flag3 = flag2 && Pointer.Down;
			string name;
			if (flag)
			{
				if (flag3)
				{
					name = "spup_pressed";
				}
				else
				{
					name = (flag2 ? "spup_mouseOver" : "spup");
				}
			}
			else
			{
				name = "spup_disabled";
			}
			Texture2D val10 = Tex(name);
			if ((Object)(object)val10 != (Object)null)
			{
				GUI.DrawTexture(val9, (Texture)(object)val10, (ScaleMode)0);
			}
			Texture2D val11 = Tex("line");
			if ((Object)(object)val11 != (Object)null)
			{
				GUI.DrawTexture(At(win, 7f, num3 + 37f, 141f, 1f), (Texture)(object)val11, (ScaleMode)0);
			}
		}
		if (list.Count == 0)
		{
			WindowShell.Label(At(win, 12f, 96f, 151f, 18f), "这条职业线没有技能数据", 11, new Color(0.35f, 0.38f, 0.45f));
		}
	}

	private static void DrawFooter(Rect win)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		int availableSp = SkillProgress.AvailableSp;
		GUIStyle val = Style(12f, (availableSp > 0) ? SpNumber : SpZero, (TextAnchor)5);
		GUI.Label(At(win, 58f, 265f, 52f, 17f), availableSp.ToString(), val);
		if (NativeUi.Button(At(win, 120f, 265f, 48f, 18f), "UI/Basic.img/BtMacro"))
		{
			macroVisible = !macroVisible;
		}
	}

	private static void MacroImage(Rect win, string node, float x, float y)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Wz("UI/UIWindow.img/SkillMacro/" + node);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(At(win, x, y, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
		}
	}

	private static void LoadMacros()
	{
		int num = (GameSession.InGame ? GameSession.CharacterId : 0);
		if (macroLoadCid == num)
		{
			return;
		}
		macroLoadCid = num;
		macroDirty = false;
		for (int i = 0; i < 5; i++)
		{
			macroNames[i] = NativeSkillMacros.Name(i);
			macroShowName[i] = NativeSkillMacros.ShowName(i);
			for (int j = 0; j < 3; j++)
			{
				macroSkills[i, j] = NativeSkillMacros.Skill(i, j);
			}
		}
	}

	private static void DrawMacro()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Invalid comparison between Unknown and I4
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Invalid comparison between Unknown and I4
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0305: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_034b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0351: Invalid comparison between Unknown and I4
		//IL_0328: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0412: Unknown result type (might be due to invalid IL or missing references)
		//IL_0417: Unknown result type (might be due to invalid IL or missing references)
		//IL_0419: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_036b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_050f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0514: Unknown result type (might be due to invalid IL or missing references)
		//IL_0430: Unknown result type (might be due to invalid IL or missing references)
		//IL_0459: Unknown result type (might be due to invalid IL or missing references)
		//IL_046e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0473: Unknown result type (might be due to invalid IL or missing references)
		//IL_0380: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_047a: Unknown result type (might be due to invalid IL or missing references)
		//IL_052e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0583: Unknown result type (might be due to invalid IL or missing references)
		//IL_0598: Unknown result type (might be due to invalid IL or missing references)
		//IL_059d: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0561: Unknown result type (might be due to invalid IL or missing references)
		if (!macroVisible)
		{
			return;
		}
		LoadMacros();
		Rect macroRect = MacroRect;
		MacroImage(macroRect, "backgrnd", 0f, 0f);
		MacroImage(macroRect, "line01", 17f, 49f);
		MacroImage(macroRect, "line02", 17f, 188f);
		MacroImage(macroRect, "macroname", 17f, 195f);
		bool flag = (int)Event.current.type == 7 && Pointer.Pressed;
		Rect val3;
		for (int i = 0; i < 3; i++)
		{
			int num = macroScroll + i;
			int num2 = 53 + i * 45;
			if (macroSelected == num)
			{
				MacroImage(macroRect, "macroslot2", 15f, num2 - 2);
			}
			MacroImage(macroRect, "macroslot", 17f, num2);
			MacroImage(macroRect, "macroslot1", 143f, num2);
			MacroImage(macroRect, "Macroicon/" + num + "/icon", 146f, num2 + 3);
			for (int j = 0; j < 3; j++)
			{
				Rect val = At(macroRect, 20 + j * 37, num2 + 3, 32f, 32f);
				Texture2D val2 = SkillIcon(SkillBook.Get(macroSkills[num, j]));
				if ((Object)(object)val2 != (Object)null)
				{
					GUI.DrawTexture(val, (Texture)(object)val2);
				}
				if ((int)Event.current.type == 7 && Pointer.RightPressed && val.Contains(Pointer.Pos))
				{
					macroSkills[num, j] = 0;
					macroSelected = num;
					macroDirty = true;
				}
			}
			if (flag)
			{
				val3 = At(macroRect, 17f, num2, 164f, 38f);
				if (val3.Contains(Pointer.Pos))
				{
					macroSelected = num;
				}
			}
		}
		Rect val4 = At(macroRect, 185f, 54f, 15f, 15f);
		Rect val5 = At(macroRect, 185f, 164f, 15f, 15f);
		Rect val6 = At(macroRect, 185f, 69f, 15f, 95f);
		Rect val7 = At(macroRect, 185f, 69 + macroScroll * 35, 15f, 25f);
		string text = "UI/Basic.img/VScr4/enabled/";
		GUI.DrawTexture(val6, (Texture)(object)Wz(text + "base"));
		GUI.DrawTexture(val7, (Texture)(object)Wz(text + "thumb0"));
		Rect val8 = val4;
		string text2 = text;
		string text3;
		if (val4.Contains(Pointer.Pos))
		{
			text3 = (Pointer.Down ? "prev2" : "prev1");
		}
		else
		{
			text3 = "prev0";
		}
		GUI.DrawTexture(val8, (Texture)(object)Wz(text2 + text3));
		Rect val9 = val5;
		string text4 = text;
		string text5;
		if (val5.Contains(Pointer.Pos))
		{
			text5 = (Pointer.Down ? "next2" : "next1");
		}
		else
		{
			text5 = "next0";
		}
		GUI.DrawTexture(val9, (Texture)(object)Wz(text4 + text5));
		if (flag && val4.Contains(Pointer.Pos))
		{
			macroScroll = Mathf.Max(0, macroScroll - 1);
		}
		if (flag && val5.Contains(Pointer.Pos))
		{
			macroScroll = Mathf.Min(2, macroScroll + 1);
		}
		if ((int)Event.current.type == 7)
		{
			val3 = At(macroRect, 17f, 53f, 183f, 126f);
			if (val3.Contains(Pointer.Pos))
			{
				float y = Input.mouseScrollDelta.y;
				if (Mathf.Abs(y) > 0.01f)
				{
					macroScroll = Mathf.Clamp(macroScroll - (int)Mathf.Sign(y), 0, 2);
				}
				if (flag && val6.Contains(Pointer.Pos))
				{
					macroScroll = Mathf.Clamp(Mathf.RoundToInt((Pointer.Pos.y / S - pos.y - 81.5f) / 35f), 0, 2);
				}
			}
		}
		Rect val10 = At(macroRect, 59f, 215f, 117f, 16f);
		GUI.DrawTexture(val10, (Texture)(object)Texture2D.whiteTexture);
		if (macroSelected >= 0)
		{
			MacroImage(macroRect, "Macroicon/" + macroSelected + "/icon", 20f, 198f);
			Rect val11 = At(macroRect, 20f, 198f, 32f, 32f);
			if (flag && val11.Contains(Pointer.Pos) && NativeSkillMacros.HasContent(macroSelected))
			{
				dragId = NativeSkillMacros.Marker(macroSelected);
				carrying = true;
			}
			GUIStyle val12 = Style(12f, Color.black, (TextAnchor)3, wrap: false, clip: true);
			string text6 = GUI.TextField(val10, macroNames[macroSelected], 12, val12);
			if (text6 != macroNames[macroSelected])
			{
				macroNames[macroSelected] = text6;
				macroDirty = true;
			}
		}
		Rect val13 = At(macroRect, 169f, 243f, 12f, 12f);
		bool flag2 = macroSelected >= 0 && macroShowName[macroSelected];
		GUI.DrawTexture(val13, (Texture)(object)Wz("UI/Basic.img/CheckBox/" + (flag2 ? "1" : "0")));
		if (flag && macroSelected >= 0 && val13.Contains(Pointer.Pos))
		{
			macroShowName[macroSelected] = !flag2;
			macroDirty = true;
		}
		Rect val14 = At(macroRect, 154f, 265f, 47f, 19f);
		if (!macroDirty)
		{
			GUI.DrawTexture(val14, (Texture)(object)Wz("UI/Basic.img/BtOK2/disabled/0"));
		}
		else
		{
			if (!NativeUi.Button(val14, "UI/Basic.img/BtOK2"))
			{
				return;
			}
			for (int k = 0; k < 5; k++)
			{
				NativeSkillMacros.SetName(k, macroNames[k]);
				if (macroShowName[k] != NativeSkillMacros.ShowName(k))
				{
					NativeSkillMacros.ToggleShow(k);
				}
				for (int l = 0; l < 3; l++)
				{
					NativeSkillMacros.SetSkill(k, l, macroSkills[k, l]);
				}
			}
			string text7 = NativeSkillMacros.Commit();
			if (text7 != null)
			{
				BottomBar.Toast("宏保存失败：" + text7);
			}
			else
			{
				macroDirty = false;
			}
		}
	}

	private static bool DropOnMacro(int id)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		if (!macroVisible)
		{
			return false;
		}
		SkillData skillData = SkillBook.Get(id);
		if (skillData == null || skillData.IsPassive || SkillProgress.LevelOf(id) <= 0)
		{
			return false;
		}
		LoadMacros();
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				Rect val = At(MacroRect, 20 + j * 37, 56 + i * 45, 32f, 32f);
				if (val.Contains(Pointer.Pos))
				{
					macroSelected = macroScroll + i;
					macroSkills[macroSelected, j] = id;
					macroDirty = true;
					return true;
				}
			}
		}
		return false;
	}

	private static Rect ScrollPrevRect(Rect win)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		return At(win, 153f, 99f, 15f, 13f);
	}

	private static Rect ScrollNextRect(Rect win)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		return At(win, 153f, 241f, 15f, 13f);
	}

	private static Rect ScrollTrackRect(Rect win)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		return At(win, 153f, 112f, 15f, Mathf.Max(0f, 129f));
	}

	private static Rect ScrollThumbRect(Rect win, List<int> list)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		Rect result = ScrollTrackRect(win);
		if (list.Count <= 4)
		{
			return result;
		}
		Texture2D val = Wz("UI/Basic.img/VScr4/enabled/thumb0");
		float num = Mathf.Min(result.height, (float)(((Object)(object)val != (Object)null) ? ((Texture)val).height : 15) * S);
		float num2 = Mathf.Max(1, list.Count - 4);
		float num3 = Mathf.Clamp01((float)scroll / num2);
		return new Rect(result.x, result.y + (result.height - num) * num3, result.width, num);
	}

	private static void HandleScrollbar(Rect win, List<int> list)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Invalid comparison between Unknown and I4
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		if (list.Count <= 4)
		{
			scrollDrag = false;
			return;
		}
		int num = Mathf.Max(0, list.Count - 4);
		Rect val;
		if ((int)Event.current.type == 7 && Pointer.Pressed)
		{
			val = ScrollPrevRect(win);
			if (val.Contains(Pointer.Pos))
			{
				scroll = Mathf.Max(0, scroll - 1);
				Pointer.ConsumeClick();
				Pointer.Pressed = false;
				return;
			}
			val = ScrollNextRect(win);
			if (val.Contains(Pointer.Pos))
			{
				scroll = Mathf.Min(num, scroll + 1);
				Pointer.ConsumeClick();
				Pointer.Pressed = false;
				return;
			}
			Rect val2 = ScrollThumbRect(win, list);
			if (val2.Contains(Pointer.Pos))
			{
				scrollDrag = true;
				scrollDragGrab = Pointer.Pos.y - val2.y;
				Pointer.ConsumeClick();
				Pointer.Pressed = false;
				return;
			}
		}
		if (scrollDrag && Pointer.Down)
		{
			Rect val3 = ScrollTrackRect(win);
			val = ScrollThumbRect(win, list);
			float height = val.height;
			float num2 = Mathf.Max(1f, val3.height - height);
			scroll = Mathf.RoundToInt(Mathf.Clamp01((Pointer.Pos.y - scrollDragGrab - val3.y) / num2) * (float)num);
		}
		if (!Pointer.Down)
		{
			scrollDrag = false;
		}
	}

	private static void DrawScrollbar(Rect win, List<int> list)
	{
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		bool flag = list.Count > 4;
		string text = (flag ? "enabled" : "disabled");
		Texture2D val = Wz("UI/Basic.img/VScr4/" + text + "/base");
		Texture2D val2 = Wz("UI/Basic.img/VScr4/" + text + (flag ? "/prev0" : "/prev"));
		Texture2D val3 = Wz("UI/Basic.img/VScr4/" + text + (flag ? "/next0" : "/next"));
		Texture2D val4 = (flag ? Wz("UI/Basic.img/VScr4/enabled/thumb0") : null);
		Rect val5 = ScrollTrackRect(win);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(val5, (Texture)(object)val, (ScaleMode)0);
		}
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(ScrollPrevRect(win), (Texture)(object)val2, (ScaleMode)0);
		}
		if ((Object)(object)val3 != (Object)null)
		{
			GUI.DrawTexture(ScrollNextRect(win), (Texture)(object)val3, (ScaleMode)0);
		}
		if (flag && (Object)(object)val4 != (Object)null)
		{
			GUI.DrawTexture(ScrollThumbRect(win, list), (Texture)(object)val4, (ScaleMode)0);
		}
	}

	private static void ShowTip(string name)
	{
		if ((string.IsNullOrEmpty(tipName) || !(Time.unscaledTime < tipUntil)) && PlayerPrefs.GetInt("mxd.skill." + name, 0) != 1)
		{
			PlayerPrefs.SetInt("mxd.skill." + name, 1);
			PlayerPrefs.Save();
			tipName = name;
			tipUntil = Time.unscaledTime + 4f;
		}
	}

	private static void DrawTip()
	{
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrEmpty(tipName))
		{
			return;
		}
		if (Time.unscaledTime >= tipUntil)
		{
			tipName = null;
			return;
		}
		Texture2D val = Tex(tipName);
		if ((Object)(object)val == (Object)null)
		{
			tipName = null;
			return;
		}
		float num = (float)((Texture)val).width * S;
		float num2 = (float)((Texture)val).height * S;
		float num3 = Pointer.Pos.x + 12f * S;
		float num4 = Pointer.Pos.y + 14f * S;
		if (num3 + num > UiKit.W - 4f)
		{
			num3 = Pointer.Pos.x - num - 12f * S;
		}
		if (num4 + num2 > UiKit.H - 4f)
		{
			num4 = Pointer.Pos.y - num2 - 14f * S;
		}
		num3 = Mathf.Clamp(num3, 4f, Mathf.Max(4f, UiKit.W - num - 4f));
		num4 = Mathf.Clamp(num4, 4f, Mathf.Max(4f, UiKit.H - num2 - 4f));
		GUI.DrawTexture(new Rect(num3, num4, num, num2), (Texture)(object)val, (ScaleMode)0);
	}

	private static void DrawTooltip(Rect win, List<int> list, int hover)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		if (hover < 0 || hover >= list.Count || carrying || (tipName == "tip0" && Time.unscaledTime < tipUntil))
		{
			return;
		}
		int num = hover - scroll;
		if (num < 0 || num >= 4)
		{
			return;
		}
		Rect val = At(win, 8f, 99 + num * 40 + 1, 32f, 32f);
		if (val.Contains(Pointer.Pos))
		{
			SkillData skillData = SkillBook.Get(list[hover]);
			if (skillData != null)
			{
				DrawSkillData(skillData, Pointer.Pos);
			}
		}
	}

	public static void DrawSkillData(SkillData d, Vector2 mouse, int activeLevel = -1)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Unknown result type (might be due to invalid IL or missing references)
		//IL_0436: Unknown result type (might be due to invalid IL or missing references)
		//IL_044f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0454: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_050e: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c3: Unknown result type (might be due to invalid IL or missing references)
		if (d != null)
		{
			int num = Mathf.Clamp((activeLevel >= 0) ? activeLevel : SkillProgress.LevelOf(d.id), 0, Mathf.Max(0, d.maxLevel));
			int fontSize = Mathf.Max(9, Mathf.RoundToInt(12f * S));
			GUIStyle val = TipStyle(fontSize, Color.white);
			List<List<KeyValuePair<string, Color>>> list = TipWrap(TipText(d.desc), 213f * S, fontSize);
			int num2 = Mathf.Max(0, list.Count - 4);
			float num3 = 114 + num2 * 20;
			bool flag = num < d.maxLevel;
			string text = (flag ? ("[下一等级 " + (num + 1) + "]") : "[已达最高等级]");
			List<List<KeyValuePair<string, Color>>> list2 = (flag ? TipWrap(SkillH(d.id, num + 1), 290f * S, fontSize) : new List<List<KeyValuePair<string, Color>>>());
			float num4 = 320f * S;
			float num5 = (num3 + 8f + (float)((1 + list2.Count) * 16) + 4f) * S;
			float num6 = mouse.x + 16f * S;
			float num7 = mouse.y + 14f * S;
			if (num6 + num4 > UiKit.W - 4f)
			{
				num6 = mouse.x - num4 - 12f * S;
			}
			if (num7 + num5 > UiKit.H - 4f)
			{
				num7 = mouse.y - num5 - 12f * S;
			}
			num6 = Mathf.Clamp(num6, 4f, Mathf.Max(4f, UiKit.W - num4 - 4f));
			num7 = Mathf.Clamp(num7, 4f, Mathf.Max(4f, UiKit.H - num5 - 4f));
			Rect rect = new Rect(num6, num7, num4, num5);
			ItemTooltip.DrawFrame(rect, S);
			Texture2D val2 = WzAssets.Texture("UI/UIWindow.img/ToolTip/Equip/Dot/0");
			if ((Object)(object)val2 != (Object)null)
			{
				GUI.DrawTexture(new Rect(rect.x + 10f * S, rect.y + 15f * S, (float)((Texture)val2).width * S, (float)((Texture)val2).height * S), (Texture)(object)val2);
			}
			GUI.Label(new Rect(rect.x + 18f * S, rect.y + 10f * S, 292f * S, 20f * S), string.IsNullOrEmpty(d.name) ? d.id.ToString() : d.name, val);
			Fill(new Rect(rect.x + 10f * S, rect.y + 32f * S, 68f * S, 68f * S), Color32.op_Implicit(new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)160)));
			Texture2D val3 = SkillIcon(d);
			if ((Object)(object)val3 != (Object)null)
			{
				float num8 = (float)((Texture)val3).width * 2f * S;
				float num9 = (float)((Texture)val3).height * 2f * S;
				GUI.DrawTexture(new Rect(rect.x + 10f * S + (68f * S - num8) * 0.5f, rect.y + 32f * S + (68f * S - num9) * 0.5f, num8, num9), (Texture)(object)val3);
			}
			TipDraw(list, rect.x + 87f * S, rect.y + 30f * S, 16f * S, fontSize);
			Fill(new Rect(rect.x + 4f * S, rect.y + num3 * S, 312f * S, Mathf.Max(1f, S)), Color32.op_Implicit(new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)216)));
			float num10 = num3 + 8f;
			Texture2D val4 = WzAssets.Texture("UI/UIWindow.img/ToolTip/Equip/Dot/1");
			if ((Object)(object)val4 != (Object)null)
			{
				GUI.DrawTexture(new Rect(rect.x + 10f * S, rect.y + (num10 + 4f) * S, (float)((Texture)val4).width * S, (float)((Texture)val4).height * S), (Texture)(object)val4);
			}
			GUI.Label(new Rect(rect.x + 15f * S, rect.y + (num10 - 1f) * S, 295f * S, 20f * S), text, val);
			TipDraw(list2, rect.x + 10f * S, rect.y + (num10 + 15f) * S, 16f * S, fontSize);
		}
	}

	private static string TipText(string s)
	{
		if (!string.IsNullOrEmpty(s))
		{
			return s.Replace("\\n", "\n");
		}
		return "";
	}

	private static string SkillH(int id, int lv)
	{
		ImgNode imgNode = WzAssets.Node("String/Skill.img/" + id.ToString("D7"));
		string text;
		if (imgNode != null)
		{
			text = imgNode.Text("h" + lv);
			if (text == null)
			{
				return "";
			}
		}
		else
		{
			text = "";
		}
		return text;
	}

	private static GUIStyle TipStyle(int fontSize, Color color)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected Obj, but got Unknown
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected Obj, but got Unknown
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected Obj, but got Unknown
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = fontSize,
			alignment = (TextAnchor)0,
			wordWrap = false,
			clipping = (TextClipping)0,
			richText = false,
			padding = new RectOffset(),
			margin = new RectOffset()
		};
		val.normal.textColor = color;
		return val;
	}

	private static void Fill(Rect r, Color color)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Color color2 = GUI.color;
		GUI.color = color;
		GUI.DrawTexture(r, (Texture)(object)TipWhite);
		GUI.color = color2;
	}

	private static List<List<KeyValuePair<string, Color>>> TipWrap(string text, float width, int fontSize)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected Obj, but got Unknown
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Expected Obj, but got Unknown
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		List<List<KeyValuePair<string, Color>>> list = new List<List<KeyValuePair<string, Color>>>();
		if (string.IsNullOrEmpty(text))
		{
			return list;
		}
		string key = fontSize + "|" + Mathf.RoundToInt(width) + "|" + text;
		if (tipWrap.TryGetValue(key, out var value))
		{
			return value;
		}
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = fontSize
		};
		List<KeyValuePair<string, Color>> list2 = new List<KeyValuePair<string, Color>>();
		StringBuilder stringBuilder = new StringBuilder();
		Color val2 = Color.white;
		float num = 0f;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			switch (c)
			{
			case '#':
				if (i + 1 < text.Length && text[i + 1] == 'c')
				{
					i++;
				}
				FlushRun(list2, stringBuilder, val2);
				val2 = ((val2 == Color.white) ? Color32.op_Implicit(new Color32(byte.MaxValue, (byte)153, (byte)0, byte.MaxValue)) : Color.white);
				continue;
			case '\n':
				FlushRun(list2, stringBuilder, val2);
				list.Add(list2);
				list2 = new List<KeyValuePair<string, Color>>();
				num = 0f;
				continue;
			case '\r':
				continue;
			}
			float x = val.CalcSize(new GUIContent(c.ToString())).x;
			if (num > 0f && num + x > width)
			{
				FlushRun(list2, stringBuilder, val2);
				list.Add(list2);
				list2 = new List<KeyValuePair<string, Color>>();
				num = 0f;
			}
			stringBuilder.Append(c);
			num += x;
		}
		FlushRun(list2, stringBuilder, val2);
		if (list2.Count > 0)
		{
			list.Add(list2);
		}
		tipWrap[key] = list;
		return list;
	}

	private static void FlushRun(List<KeyValuePair<string, Color>> line, StringBuilder run, Color color)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		if (run.Length != 0)
		{
			line.Add(new KeyValuePair<string, Color>(run.ToString(), color));
			run.Length = 0;
		}
	}

	private static void TipDraw(List<List<KeyValuePair<string, Color>>> lines, float x, float y, float step, int fontSize)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected Obj, but got Unknown
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		if (lines == null || lines.Count == 0)
		{
			return;
		}
		GUIStyle val = TipStyle(fontSize, Color.white);
		GUIStyle val2 = TipStyle(fontSize, Color32.op_Implicit(new Color32(byte.MaxValue, (byte)153, (byte)0, byte.MaxValue)));
		for (int i = 0; i < lines.Count; i++)
		{
			float num = x;
			for (int j = 0; j < lines[i].Count; j++)
			{
				KeyValuePair<string, Color> keyValuePair = lines[i][j];
				GUIStyle val3 = ((keyValuePair.Value == Color.white) ? val : val2);
				Vector2 val4 = val3.CalcSize(new GUIContent(keyValuePair.Key));
				GUI.Label(new Rect(num, y + (float)i * step, val4.x + 4f, val4.y), keyValuePair.Key, val3);
				num += val4.x;
			}
		}
	}

	private static int RecommendedId()
	{
		if (!recommendTried)
		{
			recommendTried = true;
			TextAsset val = Resources.Load<TextAsset>("UI/RecommendSkill");
			if ((Object)(object)val != (Object)null)
			{
				recommend = MiniJson.Parse(val.text) as Dictionary<string, object>;
			}
		}
		if (recommend == null)
		{
			return 0;
		}
		if (Tabs.Count == 0)
		{
			return 0;
		}
		int num = Profile.JobId / 100 * 100;
		if (!recommend.TryGetValue(num.ToString(), out var value))
		{
			return 0;
		}
		if (!(value is Dictionary<string, object> dictionary))
		{
			return 0;
		}
		int num2 = -1;
		int result = 0;
		foreach (KeyValuePair<string, object> item in dictionary)
		{
			if (int.TryParse(item.Key, out var result2) && result2 <= Profile.level && result2 >= num2)
			{
				num2 = result2;
				int.TryParse(item.Value.ToString(), out result);
			}
		}
		return result;
	}

	private static void CancelDrag()
	{
		dragId = 0;
		carrying = false;
		externalCarry = false;
	}

	public static void CancelSkillDrag()
	{
		CancelDrag();
	}

	public static bool HandleDrop()
	{
		if (!Dragging || !Pointer.Pressed)
		{
			return false;
		}
		bool result = DropOnSlot(dragId);
		CancelDrag();
		return result;
	}

	public static void PickSkill(int id)
	{
		dragId = id;
		carrying = id != 0;
		externalCarry = carrying;
	}

	public static void PickForTest(int id)
	{
		PickSkill(id);
	}

	public static bool DropAt(Vector2 point)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		if (!Dragging)
		{
			return false;
		}
		Vector2 val = Pointer.Pos;
		Pointer.Pos = point;
		bool result = DropOnSlot(dragId);
		Pointer.Pos = val;
		CancelDrag();
		return result;
	}

	public static void DrawDragGhost()
	{
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		if (!Dragging)
		{
			return;
		}
		if (NativeSkillMacros.IsMacro(dragId))
		{
			int num = NativeSkillMacros.FirstSkill(NativeSkillMacros.MarkerIndex(dragId));
			Texture2D val = SkillIcon((num != 0) ? SkillBook.Get(num) : null);
			if ((Object)(object)val != (Object)null)
			{
				GUI.DrawTexture(new Rect(Pointer.Pos.x - 16f * S, Pointer.Pos.y - 16f * S, 32f * S, 32f * S), (Texture)(object)val, (ScaleMode)2);
			}
		}
		else
		{
			Texture2D val2 = SkillIcon(SkillBook.Get(dragId));
			if ((Object)(object)val2 != (Object)null)
			{
				GUI.DrawTexture(new Rect(Pointer.Pos.x - 16f * S, Pointer.Pos.y - 16f * S, 32f * S, 32f * S), (Texture)(object)val2, (ScaleMode)2);
			}
		}
	}

	private static bool DropOnSlot(int id)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Invalid comparison between Unknown and I4
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		Rect val;
		if (NativeSkillMacros.IsMacro(id))
		{
			for (int i = 0; i < 26; i++)
			{
				val = BottomBar.SlotRect(i);
				if (val.Contains(Pointer.Pos))
				{
					if (!QuickBarKeys.BindSkill(i, id))
					{
						return true;
					}
					int index = NativeSkillMacros.MarkerIndex(id);
					BottomBar.Toast("已把宏「" + NativeSkillMacros.Name(index) + "」绑定到快捷格 " + (i + 1));
					return true;
				}
			}
			return false;
		}
		if (DropOnMacro(id))
		{
			return true;
		}
		if (KeyConfigWindow.Visible && !KeyConfigWindow.Carrying && (int)KeyConfigWindow.HitKey(Pointer.Pos) > 0 && KeyConfigWindow.DropSkill(Pointer.Pos, id))
		{
			return true;
		}
		if (!KeyConfigWindow.Visible && VirtualControls.HitButtonAt(Pointer.Pos))
		{
			NativeUtilPrompt.Notice("技能只能绑到键盘的键位上：在键盘设定里拖到键位，或放进快捷栏后指定按键。");
			return true;
		}
		for (int j = 0; j < 26; j++)
		{
			val = BottomBar.SlotRect(j);
			if (val.Contains(Pointer.Pos))
			{
				if (!QuickBarKeys.BindSkill(j, id))
				{
					return true;
				}
				BottomBar.QuickCollapsed = false;
				BottomBar.Toast("已绑定到快捷格 " + (j + 1) + "（键 " + KeyMap.Label(QuickBarKeys.Of(j)) + "）");
				return true;
			}
		}
		return false;
	}

	static SkillWindow()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
	}
}
