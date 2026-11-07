using System.Collections.Generic;
using UnityEngine;

public static class NativeSpResetWindow
{
	private const string Root = "UI/UIWindow.img/Reset/SP/";

	private const int WinW = 175;

	private const int WinH = 289;

	private const int ConfirmH = 230;

	private const int RowX = 7;

	private const int RowY = 99;

	private const int RowStep = 40;

	private const int RowW = 141;

	private const int RowH = 35;

	private const int Rows = 5;

	private const int IconOffX = 1;

	private const int IconOffY = 1;

	private const int IconSize = 32;

	private const int NameX = 45;

	private const int NameY = 0;

	private const int NameW = 84;

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

	private const int ScrollTop = 99;

	private const int ScrollBottom = 254;

	private const int TabX = 3;

	private const int TabY = 57;

	private const int TabW = 34;

	private const int TabH = 19;

	private static readonly int[] GroupLine = new int[2] { 91, 155 };

	private static int itemId;

	private static int itemPosition;

	private static int stage;

	private static int tab;

	private static int scroll;

	private static int fromId;

	private static int toId;

	private static Vector2 pos;

	private static Vector2 grab;

	private static bool dragging;

	private static bool centered;

	private static bool scrollDrag;

	private static float scrollGrab;

	private static readonly Color Ink = new Color(0.1f, 0.11f, 0.15f);

	public static bool Visible => stage != 0;

	private static float S => BottomBar.S;

	private static Vector2 Size
	{
		get
		{
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			return new Vector2(175f, (float)((stage == 3) ? 230 : 289));
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

	private static Texture2D Wz(string node)
	{
		return Art.Tex("wz://" + node);
	}

	private static void Image(Rect r, string node, float x, float y)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Wz(node);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(At(r, x, y, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
		}
	}

	public static int MaxStageOf(int id)
	{
		return Mathf.Clamp(id - 5050000, 1, 4);
	}

	public static int StageOfJob(int job)
	{
		int num = job % 100;
		if (num == 0)
		{
			return 1;
		}
		if (num < 10)
		{
			return 2;
		}
		if (num % 10 != 1)
		{
			return 4;
		}
		return 3;
	}

	private static List<SkillData> StageSkills(int stage)
	{
		List<SkillData> list = new List<SkillData>();
		foreach (Dictionary<string, object> item in SkillBook.Tabs(SkillBook.LineForJob(Profile.JobId)))
		{
			if (!item.TryGetValue("skills", out var value))
			{
				continue;
			}
			foreach (object item2 in (List<object>)value)
			{
				SkillData skillData = SkillBook.Get(int.Parse(item2.ToString()));
				if (skillData != null && !skillData.autoLearn && skillData.job != 0 && skillData.job != 1000 && StageOfJob(skillData.job) == stage)
				{
					list.Add(skillData);
				}
			}
		}
		return list;
	}

	public static void Open(int id, int inventoryPosition = 0)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		if (MaxStageOf(id) >= 1)
		{
			itemId = id;
			itemPosition = inventoryPosition;
			stage = 1;
			fromId = 0;
			toId = 0;
			tab = 0;
			scroll = 0;
			dragging = false;
			centered = false;
			scrollDrag = false;
			pos = new Vector2(300f, 120f);
			Center();
		}
	}

	public static void Close()
	{
		itemId = 0;
		stage = 0;
		fromId = 0;
		toId = 0;
		scroll = 0;
		dragging = false;
		centered = false;
		scrollDrag = false;
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

	private static GUIStyle Style(int size, Color color, TextAnchor anchor)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected Obj, but got Unknown
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = Mathf.Max(7, Mathf.RoundToInt((float)size * S)),
			alignment = anchor
		};
		val.normal.textColor = color;
		return val;
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
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Invalid comparison between Unknown and I4
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
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
		NativeUi.RegisterWindow("NativeSpResetWindow", window, () => Visible);
		if ((int)Event.current.type == 7 && Pointer.Pressed)
		{
			Rect val = At(window, 0f, 0f, 175f, 22f);
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
			DrawConfirm(window);
		}
		else
		{
			DrawList(window, stage);
		}
	}

	private static void DrawList(Rect r, int page)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Invalid comparison between Unknown and I4
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		Image(r, (page == 1) ? "UI/UIWindow.img/Reset/SP/backgrnd" : "UI/UIWindow.img/Reset/SP/backgrnd2", 0f, 0f);
		if (NativeUi.Button(At(r, 155f, 6f, 12f, 12f), "UI/Basic.img/BtClose"))
		{
			Close();
			return;
		}
		int num = MaxStageOf(itemId);
		for (int i = 0; i < 4; i++)
		{
			bool flag = i == tab;
			bool flag2 = i + 1 <= num;
			Rect rect = At(r, 3 + i * 34, 57f, 34f, 19f);
			DrawTabPlate(rect, flag);
			Texture2D val = Wz("UI/UIWindow.img/Reset/SP/Tab/" + (flag ? "enabled" : "disabled") + "/" + i);
			if ((Object)(object)val != (Object)null)
			{
				GUI.DrawTexture(new Rect(rect.x + (rect.width - (float)((Texture)val).width * S) * 0.5f, rect.y + (19f * S - (float)((Texture)val).height * S) * 0.5f, (float)((Texture)val).width * S, (float)((Texture)val).height * S), (Texture)(object)val);
			}
			if (flag2 && (int)Event.current.type == 7 && Pointer.Pressed && rect.Contains(Pointer.Pos) && tab != i)
			{
				tab = i;
				scroll = 0;
			}
		}
		List<SkillData> list = PageSkills(page);
		scroll = Mathf.Clamp(scroll, 0, Mathf.Max(0, list.Count - 5));
		DrawRows(r, list, page);
		DrawScrollbar(r, list);
		if (list.Count == 0)
		{
			GUI.Label(At(r, 12f, 109f, 140f, 18f), (page == 1) ? "这个转数还没有已加点的技能" : "这个转数没有可再分配的技能", Style(11, new Color(0.35f, 0.38f, 0.45f), (TextAnchor)4));
		}
	}

	private static List<SkillData> PageSkills(int page)
	{
		List<SkillData> list = StageSkills(tab + 1);
		List<SkillData> list2 = new List<SkillData>();
		foreach (SkillData item in list)
		{
			int num = SkillProgress.BaseLevelOf(item.id);
			int num2 = SkillBook.MasterCapOf(item.id);
			if (page == 1 && num > 0)
			{
				list2.Add(item);
			}
			if (page == 2 && num < num2 && item.id != fromId)
			{
				list2.Add(item);
			}
		}
		return list2;
	}

	private static void DrawRows(Rect r, List<SkillData> skills, int page)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Invalid comparison between Unknown and I4
		for (int i = 0; i < 5; i++)
		{
			int num = scroll + i;
			if (num >= skills.Count)
			{
				break;
			}
			SkillData skillData = skills[num];
			float num2 = 99 + i * 40;
			Texture2D val = Wz("UI/UIWindow.img/Reset/SP/skill1");
			if ((Object)(object)val != (Object)null)
			{
				GUI.DrawTexture(At(r, 7f, num2, 141f, 35f), (Texture)(object)val, (ScaleMode)0);
			}
			Texture2D val2 = (string.IsNullOrEmpty(skillData.icon) ? null : Art.Tex(skillData.icon));
			if ((Object)(object)val2 != (Object)null)
			{
				GUI.DrawTexture(At(r, 8f, num2 + 1f, 32f, 32f), (Texture)(object)val2, (ScaleMode)0);
			}
			int num3 = SkillProgress.BaseLevelOf(skillData.id);
			GUI.Label(At(r, 45f, num2 + 0f, 84f, 14f), skillData.name, Style(12, Ink, (TextAnchor)3));
			int num4 = SkillBook.MasterCapOf(skillData.id);
			GUI.Label(At(r, 45f, num2 + 20f, 60f, 12f), (page == 1) ? (num3 + " / " + num4) : (num3 + " / " + num4), Style(12, Ink, (TextAnchor)3));
			Texture2D val3 = Wz("UI/UIWindow.img/Reset/SP/line");
			if ((Object)(object)val3 != (Object)null)
			{
				GUI.DrawTexture(At(r, 7f, num2 + 37f, 141f, 1f), (Texture)(object)val3, (ScaleMode)0);
			}
			Rect val4 = At(r, 131f, num2 + 20f, 12f, 12f);
			bool flag = val4.Contains(Pointer.Pos);
			string text;
			if (flag)
			{
				text = (Pointer.Down ? "pressed" : "mouseOver");
			}
			else
			{
				text = "normal";
			}
			Texture2D val5 = Wz("UI/UIWindow.img/Skill/BtSpUp/" + text + "/0");
			if ((Object)(object)val5 != (Object)null)
			{
				GUI.DrawTexture(val4, (Texture)(object)val5, (ScaleMode)0);
			}
			if (flag && (int)Event.current.type == 7 && Pointer.Pressed)
			{
				if (page == 1)
				{
					fromId = skillData.id;
					toId = 0;
					scroll = 0;
					stage = 2;
				}
				else
				{
					toId = skillData.id;
					stage = 3;
				}
				break;
			}
		}
	}

	private static Rect ScrollTrack(Rect r)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		return At(r, 153f, 114f, 15f, Mathf.Max(0f, 125f));
	}

	private static Rect ScrollThumb(Rect r, List<SkillData> skills)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		Rect result = ScrollTrack(r);
		if (skills.Count <= 5)
		{
			return result;
		}
		float num = Mathf.Max(24f * S, result.height * 5f / (float)skills.Count);
		int num2 = Mathf.Max(1, skills.Count - 5);
		float num3 = Mathf.Clamp01((float)scroll / (float)num2);
		return new Rect(result.x, result.y + (result.height - num) * num3, result.width, num);
	}

	private static void DrawScrollbar(Rect r, List<SkillData> skills)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Invalid comparison between Unknown and I4
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		bool flag = skills.Count > 5;
		string text = (flag ? "enabled" : "disabled");
		Rect val = At(r, 153f, 99f, 15f, 15f);
		Rect val2 = At(r, 153f, 239f, 15f, 15f);
		Rect val3 = ScrollTrack(r);
		Texture2D val4 = Wz("UI/Basic.img/VScr5/" + text + "/base");
		if ((Object)(object)val4 != (Object)null)
		{
			GUI.DrawTexture(val3, (Texture)(object)val4, (ScaleMode)0);
		}
		Texture2D val5 = Wz("UI/Basic.img/VScr5/" + text + (flag ? "/prev0" : "/prev"));
		Texture2D val6 = Wz("UI/Basic.img/VScr5/" + text + (flag ? "/next0" : "/next"));
		if ((Object)(object)val5 != (Object)null)
		{
			GUI.DrawTexture(val, (Texture)(object)val5, (ScaleMode)0);
		}
		if ((Object)(object)val6 != (Object)null)
		{
			GUI.DrawTexture(val2, (Texture)(object)val6, (ScaleMode)0);
		}
		if (flag)
		{
			Texture2D val7 = Wz("UI/Basic.img/VScr5/enabled/thumb0");
			if ((Object)(object)val7 != (Object)null)
			{
				GUI.DrawTexture(ScrollThumb(r, skills), (Texture)(object)val7, (ScaleMode)0);
			}
		}
		if ((int)Event.current.type != 7)
		{
			return;
		}
		if (flag && Pointer.Pressed)
		{
			if (val.Contains(Pointer.Pos))
			{
				scroll = Mathf.Max(0, scroll - 1);
				return;
			}
			if (val2.Contains(Pointer.Pos))
			{
				scroll = Mathf.Min(skills.Count - 5, scroll + 1);
				return;
			}
			Rect val8 = ScrollThumb(r, skills);
			if (val8.Contains(Pointer.Pos))
			{
				scrollDrag = true;
				scrollGrab = Pointer.Pos.y - val8.y;
				return;
			}
		}
		if (scrollDrag && Pointer.Down)
		{
			Rect val9 = ScrollTrack(r);
			Rect val10 = ScrollThumb(r, skills);
			float height = val10.height;
			float num = Mathf.Max(1f, val9.height - height);
			scroll = Mathf.RoundToInt(Mathf.Clamp01((Pointer.Pos.y - scrollGrab - val9.y) / num) * (float)Mathf.Max(0, skills.Count - 5));
		}
		if (!Pointer.Down)
		{
			scrollDrag = false;
		}
		Rect val11 = At(r, 7f, 99f, 156f, 200f);
		if (val11.Contains(Pointer.Pos) && skills.Count > 5)
		{
			float y = Input.mouseScrollDelta.y;
			if (Mathf.Abs(y) > 0.01f)
			{
				scroll = Mathf.Clamp(scroll - (int)Mathf.Sign(y), 0, skills.Count - 5);
			}
		}
	}

	private static void DrawTabPlate(Rect rect, bool on)
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
			GUI.DrawTexture(new Rect(rect.x + num, rect.y, rect.width - num * 2f, rect.height), (Texture)(object)val2);
		}
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(rect.x, rect.y, num, rect.height), (Texture)(object)val);
		}
		if ((Object)(object)val3 != (Object)null)
		{
			GUI.DrawTexture(new Rect(rect.xMax - num, rect.y, num, rect.height), (Texture)(object)val3);
		}
	}

	private static void DrawConfirm(Rect r)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		Image(r, "UI/UIWindow.img/Reset/SP/backgrnd3", 0f, 0f);
		if (NativeUi.Button(At(r, 155f, 6f, 12f, 12f), "UI/Basic.img/BtClose"))
		{
			Close();
			return;
		}
		SkillData d = SkillBook.Get(fromId);
		SkillData d2 = SkillBook.Get(toId);
		DrawGroup(r, 0, d, SkillProgress.BaseLevelOf(fromId), SkillProgress.BaseLevelOf(fromId) - 1);
		DrawGroup(r, 1, d2, SkillProgress.BaseLevelOf(toId), SkillProgress.BaseLevelOf(toId) + 1);
		if (NativeUi.Button(At(r, 70f, 207f, 47f, 19f), "UI/Basic.img/BtOK2"))
		{
			Confirm();
		}
		if (NativeUi.Button(At(r, 119f, 207f, 47f, 19f), "UI/Basic.img/BtCancel2"))
		{
			Close();
		}
	}

	private static void DrawGroup(Rect r, int group, SkillData d, int lvBefore, int lvAfter)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		if (d != null)
		{
			int num = GroupLine[group] + 1;
			Texture2D val = (string.IsNullOrEmpty(d.icon) ? null : Art.Tex(d.icon));
			if ((Object)(object)val != (Object)null)
			{
				GUI.DrawTexture(At(r, 9f, num + 1, 34f, 28f), (Texture)(object)val, (ScaleMode)2);
			}
			GUI.Label(At(r, 46f, num + 1, 122f, 14f), d.name, Style(12, Ink, (TextAnchor)3));
			GUI.Label(At(r, 46f, num + 15, 122f, 14f), "Lv." + lvBefore + " → Lv." + lvAfter, Style(12, Ink, (TextAnchor)3));
		}
	}

	private static void Confirm()
	{
		int spFromId = fromId;
		int spToId = toId;
		int sourcePosition = itemPosition;
		int id = itemId;
		Close();
		string text = Profile.ResetSp(spFromId, spToId);
		if (text != null)
		{
			BottomBar.Toast(text);
		}
		else
		{
			Profile.RemoveItem(id, 1, sourcePosition);
		}
	}

	static NativeSpResetWindow()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
	}
}
