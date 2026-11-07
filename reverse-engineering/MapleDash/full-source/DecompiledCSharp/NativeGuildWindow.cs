using System.Collections.Generic;
using UnityEngine;

public static class NativeGuildWindow
{
	public static bool Visible;

	private const int WinW = 312;

	private const int WinH = 389;

	private const string Root = "UI/UIWindow.img/UserList/";

	private const string Info = "UI/UIWindow.img/UserList/Guild/GuildInfo/";

	private const int TabX = 6;

	private const int TabY = 23;

	private const int TabW = 270;

	private const int TabGap = 13;

	private const int TabH = 23;

	private static readonly string[] TabNames = new string[5] { "好友", "组员", "家族", "家族联盟", "拒绝列表" };

	private const int ContentX = 3;

	private const int ContentY = 53;

	private const int BarW = 306;

	private const int MarkOffsetY = 1;

	private const int MarkSize = 20;

	private const int NameGap = 26;

	private const int NameOffsetY = 3;

	private const int NameTextH = 16;

	private const int MarkY = 54;

	private const int NameTextY = 56;

	private const int NoticeX = 3;

	private const int NoticeY = 78;

	private const int NoticeW = 282;

	private const int NoticeH = 20;

	private const int NoticeTextX = 11;

	private const int NoticeTextY = 83;

	private const int NoticeButtonX = 287;

	private const int NoticeButtonY = 80;

	private const int ListX = 6;

	private const int ListTop = 113;

	private const int ListW = 281;

	private const int ListH = 192;

	private const int GroupTitleH = 27;

	private const int HeaderY = 31;

	private const int HeaderH = 18;

	private const int RowsY = 49;

	private const int RowH = 18;

	private const int GroupGap = 10;

	private const int ColName = 10;

	private const int ColClass = 100;

	private const int ColLevel = 170;

	private const int ColRank = 210;

	private const int ScrollStep = 33;

	private static int tab = 2;

	private static int selectedId;

	private static int scroll;

	private static double nextRefresh;

	private static bool dragging;

	private static bool windowDragging;

	private static Vector2 windowGrab;

	private static float dragFromY;

	private static int dragFromScroll;

	private static readonly Color TextOn = new Color(0.08f, 0.08f, 0.1f);

	private static readonly Color TextDim = new Color(0.42f, 0.44f, 0.48f);

	private static readonly Color TextSelected = new Color(0.93f, 0.94f, 0.96f);

	private static readonly Color RowSelected = Color32.op_Implicit(new Color32((byte)36, (byte)71, (byte)104, byte.MaxValue));

	private static readonly string[] ButtonNodes = new string[11]
	{
		"BtInvite", "BtWithdraw", "BtWhere", "BtWhisper", "BtUp", "BtDown", "BtKick", "BtChat", "BtGuildBBS", "BtPartyInvite",
		"BtInfo"
	};

	private static readonly int[,] ButtonAt = new int[11, 2]
	{
		{ 9, 316 },
		{ 80, 316 },
		{ 159, 316 },
		{ 238, 316 },
		{ 9, 336 },
		{ 80, 336 },
		{ 159, 336 },
		{ 238, 336 },
		{ 80, 363 },
		{ 159, 363 },
		{ 238, 363 }
	};

	private static Rect[] buttonRects;

	private static Vector2[] buttonSize;

	private static float S => BottomBar.S;

	private static CommunityResponse State => NativeCommunityWindow.State;

	public static int ButtonCount => ButtonNodes.Length;

	private static Rect At(Rect win, float x, float y, float w, float h)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(win.x + x * S, win.y + y * S, w * S, h * S);
	}

	private static GUIStyle Style(int size, Color color, TextAnchor anchor = (TextAnchor)0)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected Obj, but got Unknown
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected Obj, but got Unknown
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = Mathf.Max(7, Mathf.RoundToInt((float)size * S)),
			alignment = anchor,
			clipping = (TextClipping)1,
			wordWrap = false,
			padding = new RectOffset()
		};
		val.normal.textColor = color;
		return val;
	}

	public static void Toggle()
	{
		if (Visible)
		{
			Reset();
			return;
		}
		if (!GameSession.InGame)
		{
			BottomBar.Toast("请先选择角色");
			return;
		}
		NativeSocialWindow.Close();
		tab = 2;
		selectedId = 0;
		scroll = 0;
		dragging = false;
		NativeCommunityWindow.Request(CommunityOperation.Read);
		Visible = true;
	}

	public static void Reset()
	{
		Visible = false;
		selectedId = 0;
		scroll = 0;
		dragging = false;
		windowDragging = false;
		CloseInput();
		NativeGuildGradeWindow.Reset();
	}

	private static void Tick()
	{
		if (!(Time.realtimeSinceStartupAsDouble < nextRefresh))
		{
			nextRefresh = Time.realtimeSinceStartupAsDouble + 1.0;
			NativeCommunityWindow.Request(CommunityOperation.Read);
		}
	}

	public static void Draw()
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Invalid comparison between Unknown and I4
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		CommunityResponse state = State;
		if (state == null)
		{
			Visible = false;
			return;
		}
		if (!NativeDlgInput.Visible)
		{
			Tick();
		}
		float s = S;
		Vector2 size = new Vector2(312f, 389f);
		Vector2 val = NativeUi.ClampWindowPosition(NativeSocialWindow.Position, size, s);
		Vector2 val2 = Pointer.Pos / s;
		if ((int)Event.current.type == 7 && Pointer.Pressed)
		{
			Rect val3 = new Rect(val.x, val.y, 268f, 22f);
			if (val3.Contains(val2))
			{
				windowDragging = true;
				windowGrab = val2 - val;
			}
		}
		if (windowDragging && Pointer.Down)
		{
			val = NativeUi.ClampWindowPosition(val2 - windowGrab, size, s);
		}
		if (!Pointer.Down)
		{
			windowDragging = false;
		}
		NativeSocialWindow.Position = val;
		Rect val4 = new Rect(val.x * s, val.y * s, 312f * s, 389f * s);
		NativeUi.RegisterWindow("NativeGuildWindow", val4, () => Visible);
		string text;
		if (tab == 2 || tab == 3)
		{
			text = "backgrnd2";
		}
		else
		{
			text = ((tab == 0) ? "backgrnd3" : "backgrnd");
		}
		Texture2D val5 = Art.Tex("wz://UI/UIWindow.img/UserList/" + text);
		if ((Object)(object)val5 != (Object)null)
		{
			GUI.DrawTexture(val4, (Texture)(object)val5);
		}
		if (NativeUi.Button(At(val4, 293f, 5f, 12f, 12f), "UI/Basic.img/BtClose"))
		{
			Reset();
			return;
		}
		DrawTabs(val4);
		if (tab == 2)
		{
			DrawGuild(val4, state);
		}
		else
		{
			Centered(val4, "该页签尚未迁移", 186f);
		}
		NativeDlgInput.Draw();
	}

	private static void DrawTabs(Rect win)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		NativeSocialWindow.DrawUserTabs(win, S, tab, SwitchTab);
	}

	private static void SwitchTab(int index)
	{
		if (index != tab)
		{
			if (index == 0)
			{
				Reset();
				NativeSocialWindow.Toggle(GameAction.Buddy);
			}
			else if (index == 1)
			{
				Reset();
				NativeSocialWindow.Toggle(GameAction.Party);
			}
			else if (index >= 3)
			{
				NativeSystemMessage.Drop(5, TabNames[index] + "尚未开放");
			}
			else
			{
				tab = index;
				selectedId = 0;
				scroll = 0;
			}
		}
	}

	private static void DrawGuild(Rect win, CommunityResponse st)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		bool flag = st.guildId > 0;
		if (flag)
		{
			DrawNameBar(win, st);
		}
		else
		{
			Centered(win, "没有参加的家族", 56f);
		}
		DrawNoticeBar(win, st, flag);
		if (flag)
		{
			DrawList(win, st);
		}
		DrawButtons(win, st, flag);
	}

	private static void DrawNameBar(Rect win, CommunityResponse st)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected Obj, but got Unknown
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		string text = st.guildName ?? "";
		GUIStyle val = Style(12, TextOn, (TextAnchor)0);
		float num = val.CalcSize(new GUIContent(text)).x / S;
		float num2 = 3f + Mathf.Max(0f, (306f - (num + 26f)) / 2f);
		Texture2D val2 = Art.Tex("wz://UI/UIWindow.img/UserList/Guild/GuildInfo/guildmark");
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(At(win, num2, 54f, 20f, 20f), (Texture)(object)val2);
		}
		GUI.Label(At(win, num2 + 26f, 56f, num + 8f, 16f), text, val);
	}

	private static void DrawNoticeBar(Rect win, CommunityResponse st, bool inGuild)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://UI/UIWindow.img/UserList/Guild/GuildInfo/guildinfo6");
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(At(win, 3f, 78f, 282f, 20f), (Texture)(object)val);
		}
		if (inGuild)
		{
			GUI.Label(At(win, 11f, 83f, 266f, 14f), st.notice ?? "", Style(10, TextOn, (TextAnchor)0));
		}
		bool enabled = GUI.enabled;
		GUI.enabled = (enabled & inGuild) && !NativeDlgInput.Visible;
		bool flag = NativeUi.Button(At(win, 287f, 80f, 20f, 20f), "UI/UIWindow.img/UserList/Guild/GuildInfo/Btnotice");
		GUI.enabled = enabled;
		if (flag & inGuild)
		{
			NativeDlgInput.Open("请输入家族公告。（最多 100 个字）", st.notice ?? "", 100, (string value) =>
			{
				NativeCommunityWindow.InputText = value;
				NativeCommunityWindow.Request(CommunityOperation.GuildNotice);
			});
		}
	}

	private static void DrawList(Rect win, CommunityResponse st)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Invalid comparison between Unknown and I4
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		Rect val = At(win, 6f, 113f, 281f, 192f);
		List<CommunityMember> list = Flatten(st, out var online);
		int num = GroupHeight(online) + 10 + GroupHeight(list.Count - online);
		int num2 = Mathf.Max(0, num - 192);
		scroll = Mathf.Clamp(scroll, 0, num2);
		Vector2 val2 = Pointer.Pos - new Vector2(val.x, val.y);
		int num3;
		if (val2.x >= 0f && val2.y >= 0f && val2.x < 281f * S)
		{
			num3 = ((val2.y < 192f * S) ? 1 : 0);
			if (num3 != 0 && (int)Event.current.type == 7)
			{
				scroll = Mathf.Clamp(scroll - Mathf.RoundToInt(Input.mouseScrollDelta.y) * 33, 0, num2);
			}
		}
		else
		{
			num3 = 0;
		}
		if (num3 != 0 && num2 > 0 && Pointer.Pressed)
		{
			dragging = true;
			dragFromY = val2.y;
			dragFromScroll = scroll;
		}
		if (dragging)
		{
			if (Pointer.Down)
			{
				int num4 = Mathf.RoundToInt(val2.y - dragFromY);
				if ((float)Mathf.Abs(num4) > 6f * S)
				{
					scroll = Mathf.Clamp(dragFromScroll - num4, 0, num2);
				}
			}
			else
			{
				dragging = false;
			}
		}
		GUI.BeginClip(val);
		DrawGroup(DrawGroup(-scroll, list, 0, online, online: true) + 10f, list, online, list.Count - online, online: false);
		if (num3 != 0 && Pointer.Clicked && !dragging)
		{
			Vector2 val3 = val2 / S;
			int num5 = HitRow(new Vector2(val3.x, val3.y + (float)scroll), list.Count, online);
			if (num5 >= 0)
			{
				selectedId = list[num5].id;
				Pointer.ConsumeClick();
			}
		}
		GUI.EndClip();
	}

	private static int HitRow(Vector2 pointer, int count, int online)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		float num = 0f;
		for (int i = 0; i < 2; i++)
		{
			int num2 = ((i != 0) ? online : 0);
			int num3 = ((i == 0) ? online : (count - online));
			float num4 = num + 49f;
			for (int j = 0; j < Mathf.Max(1, num3); j++)
			{
				Rect val = new Rect(0f, num4 + (float)(j * 18), 281f, 18f);
				if (val.Contains(pointer))
				{
					if (j >= num3)
					{
						return -1;
					}
					return num2 + j;
				}
			}
			num += (float)(GroupHeight(num3) + 10);
		}
		return -1;
	}

	private static int GroupHeight(int count)
	{
		return 49 + 18 * Mathf.Max(1, count) + 1;
	}

	private static List<CommunityMember> Flatten(CommunityResponse st, out int online)
	{
		List<CommunityMember> list = new List<CommunityMember>();
		online = 0;
		if (st?.guild == null)
		{
			return list;
		}
		foreach (CommunityMember item in st.guild)
		{
			if (item.online)
			{
				list.Add(item);
			}
		}
		list.Sort(Compare);
		online = list.Count;
		foreach (CommunityMember item2 in st.guild)
		{
			if (!item2.online)
			{
				list.Add(item2);
			}
		}
		list.Sort(online, list.Count - online, Comparer<CommunityMember>.Create(Compare));
		return list;
	}

	private static int Compare(CommunityMember a, CommunityMember b)
	{
		if (a.rank == b.rank)
		{
			return string.CompareOrdinal(a.name ?? "", b.name ?? "");
		}
		return a.rank.CompareTo(b.rank);
	}

	private static float DrawGroup(float y, List<CommunityMember> rows, int start, int count, bool online)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://UI/UIWindow.img/UserList/Guild/GuildInfo/" + (online ? "guildinfo0" : "guildinfo1"));
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(0f, y, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
		}
		Texture2D val2 = Art.Tex("wz://UI/UIWindow.img/UserList/Guild/GuildInfo/" + (online ? "guildinfo2" : "guildinfo7"));
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(new Rect(0f, y + 31f, (float)((Texture)val2).width, (float)((Texture)val2).height), (Texture)(object)val2);
		}
		Texture2D val3 = Art.Tex("wz://UI/UIWindow.img/UserList/Guild/GuildInfo/guildinfo3");
		GUIStyle val4 = Style(11, TextOn, (TextAnchor)0);
		GUIStyle val5 = Style(11, TextDim, (TextAnchor)0);
		GUIStyle val6 = Style(11, TextSelected, (TextAnchor)0);
		int num = Mathf.Max(1, count);
		for (int i = 0; i < num; i++)
		{
			float num2 = y + 49f + (float)(i * 18);
			if ((Object)(object)val3 != (Object)null)
			{
				GUI.DrawTexture(new Rect(0f, num2, (float)((Texture)val3).width, (float)((Texture)val3).height), (Texture)(object)val3);
			}
			if (i >= count)
			{
				continue;
			}
			CommunityMember communityMember = rows[start + i];
			int num3;
			if (communityMember.id == selectedId)
			{
				num3 = ((selectedId != 0) ? 1 : 0);
				if (num3 != 0)
				{
					UiKit.Fill(new Rect(1f, num2, 279f, 18f), RowSelected);
				}
			}
			else
			{
				num3 = 0;
			}
			GUIStyle val7;
			if (num3 != 0)
			{
				val7 = val6;
			}
			else
			{
				val7 = (communityMember.online ? val4 : val5);
			}
			GUI.Label(new Rect(8f, num2 + 4f, 86f, 18f), communityMember.name ?? "", val7);
			GUI.Label(new Rect(100f, num2 + 4f, 64f, 18f), JobName(communityMember.job), val7);
			GUI.Label(new Rect(170f, num2 + 4f, 34f, 18f), communityMember.level.ToString(), val7);
			GUI.Label(new Rect(210f, num2 + 4f, 66f, 18f), RankName(communityMember.rank), val7);
		}
		Texture2D val8 = Art.Tex("wz://UI/UIWindow.img/UserList/Guild/GuildInfo/guildinfo4");
		float num4 = y + 49f + (float)(num * 18);
		if ((Object)(object)val8 != (Object)null)
		{
			GUI.DrawTexture(new Rect(0f, num4, (float)((Texture)val8).width, (float)((Texture)val8).height), (Texture)(object)val8);
		}
		return num4 + 1f;
	}

	private static string RankName(int rank)
	{
		string[] array = State?.titles;
		if (array != null && rank >= 1 && rank <= array.Length && !string.IsNullOrEmpty(array[rank - 1]))
		{
			return array[rank - 1];
		}
		return rank switch
		{
			2 => "副族长", 
			1 => "族长", 
			_ => "", 
		};
	}

	private static string JobName(int jobId)
	{
		JobEnum.Const obj = JobEnum.Of(jobId);
		if (obj == null)
		{
			return "";
		}
		return obj.Display;
	}

	private static void Centered(Rect win, string text, float y)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		GUI.Label(At(win, 3f, y, 306f, 16f), text, Style(12, TextOn, (TextAnchor)4));
	}

	public static Rect PlaceButton(int index, Vector2 size)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		return new Rect((float)ButtonAt[index, 0], (float)ButtonAt[index, 1], size.x, size.y);
	}

	private static void ResolveButtons()
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		if (buttonRects == null)
		{
			buttonRects = new Rect[ButtonNodes.Length];
			buttonSize = new Vector2[ButtonNodes.Length];
			for (int i = 0; i < ButtonNodes.Length; i++)
			{
				string text = "UI/UIWindow.img/UserList/Guild/GuildInfo/" + ButtonNodes[i] + "/normal/0";
				Texture2D val = Art.Tex("wz://" + text);
				float num = (((Object)(object)val != (Object)null) ? ((Texture)val).width : 53);
				float num2 = (((Object)(object)val != (Object)null) ? ((Texture)val).height : 18);
				buttonSize[i] = new Vector2(num, num2);
				buttonRects[i] = PlaceButton(i, buttonSize[i]);
			}
		}
	}

	private static void DrawButtons(Rect win, CommunityResponse st, bool inGuild)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		ResolveButtons();
		CommunityMember target = SelectedMember(st);
		bool[] array = EnabledRules(st, target);
		for (int i = 0; i < ButtonNodes.Length; i++)
		{
			string node = "UI/UIWindow.img/UserList/Guild/GuildInfo/" + ButtonNodes[i];
			Rect val = buttonRects[i];
			Rect rect = At(win, val.x, val.y, val.width, val.height);
			bool enabled = GUI.enabled;
			GUI.enabled = enabled && array[i] && !NativeDlgInput.Visible;
			bool flag = NativeUi.Button(rect, node);
			GUI.enabled = enabled;
			if (flag)
			{
				Act(i, st, target);
			}
		}
	}

	public static bool[] EnabledRules(CommunityResponse st, CommunityMember target)
	{
		bool flag = st != null && st.guildId > 0;
		int num = Self(st)?.rank ?? 5;
		bool flag2 = target != null && target.id != GameSession.CharacterId;
		bool flag3 = flag2 && target.online;
		int num2 = target?.rank ?? 0;
		return new bool[11]
		{
			flag && num <= 2,
			flag && num != 1,
			flag3,
			flag3,
			(flag & flag2) && num <= 2 && num2 > num + 1 && num2 - 1 >= 2 && (num2 - 1 != 2 || num == 1),
			(flag & flag2) && num <= 2 && num2 > num && num2 < 5,
			(flag & flag2) && num <= 2 && num2 > num,
			flag,
			false,
			flag3,
			flag
		};
	}

	private static void Act(int index, CommunityResponse st, CommunityMember target)
	{
		switch (index)
		{
		case 0:
			NativeDlgInput.Open("请输入要邀请的角色名。（4～12 个字）", "", 12, (string value) =>
			{
				NativeCommunityWindow.InputName = value;
				NativeCommunityWindow.Request(CommunityOperation.InviteGuild);
			});
			break;
		case 1:
			NativeUtilPrompt.Confirm("确定要退出家族吗？", (bool yes) =>
			{
				if (yes)
				{
					NativeCommunityWindow.Request(CommunityOperation.LeaveGuild);
				}
			});
			break;
		case 2:
			if (target != null)
			{
				string text = NativeMapNames.Of(target.map);
				NativeSystemMessage.Drop(5, target.name + " 目前在 " + ((text != "") ? text : ("地图 " + target.map)));
			}
			break;
		case 3:
			if (target != null)
			{
				Reset();
				BottomBar.FocusChat(NativeChatChannel.Whisper, target.name);
			}
			break;
		case 4:
			NativeCommunityWindow.Request(CommunityOperation.GuildRank, target.id, Mathf.Max(2, target.rank - 1));
			break;
		case 5:
			NativeCommunityWindow.Request(CommunityOperation.GuildRank, target.id, Mathf.Min(5, target.rank + 1));
			break;
		case 6:
			NativeCommunityWindow.Request(CommunityOperation.ExpelGuild, target.id);
			break;
		case 7:
			Reset();
			BottomBar.FocusChat(NativeChatChannel.Guild);
			break;
		case 9:
			if (target != null)
			{
				NativeSocialWindow.Request(SocialOperation.InviteParty, target.name);
			}
			break;
		case 10:
			NativeGuildGradeWindow.Toggle();
			break;
		case 8:
			break;
		}
	}

	private static CommunityMember Self(CommunityResponse st)
	{
		if (st?.guild == null)
		{
			return null;
		}
		foreach (CommunityMember item in st.guild)
		{
			if (item.id == GameSession.CharacterId)
			{
				return item;
			}
		}
		return null;
	}

	private static CommunityMember SelectedMember(CommunityResponse st)
	{
		if (st?.guild == null || selectedId == 0)
		{
			return null;
		}
		foreach (CommunityMember item in st.guild)
		{
			if (item.id == selectedId)
			{
				return item;
			}
		}
		return null;
	}

	private static void CloseInput()
	{
		NativeDlgInput.Close();
	}

	public static Vector2 SizeOf()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2(312f, 389f);
	}

	public static Vector2 TabStrip()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2(6f, 23f);
	}

	public static Vector2 ContentOrigin()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2(3f, 53f);
	}

	public static Rect MarkRect(float nameWidth)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(3f + Mathf.Max(0f, (306f - (nameWidth + 26f)) / 2f), 54f, 20f, 20f);
	}

	public static float NameTextLeft(float nameWidth)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		Rect val = MarkRect(nameWidth);
		return val.x + 26f;
	}

	public static Rect ListViewport()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(6f, 113f, 281f, 192f);
	}

	public static Rect NoticeBar()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(3f, 78f, 282f, 20f);
	}

	public static Rect NoticeButton()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(287f, 80f, 20f, 20f);
	}

	public static int GroupHeightOf(int members)
	{
		return GroupHeight(members);
	}

	public static float RowTop()
	{
		return 49f;
	}

	public static int RowStep()
	{
		return 18;
	}

	public static int ColumnX(int column)
	{
		return column switch
		{
			2 => 170, 
			1 => 100, 
			0 => 10, 
			_ => 210, 
		};
	}

	public static int NameTextTop()
	{
		return 56;
	}

	public static string ButtonNode(int index)
	{
		return ButtonNodes[index];
	}

	public static Vector2 ButtonAnchor(int index)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2((float)ButtonAt[index, 0], (float)ButtonAt[index, 1]);
	}

	static NativeGuildWindow()
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
	}
}
