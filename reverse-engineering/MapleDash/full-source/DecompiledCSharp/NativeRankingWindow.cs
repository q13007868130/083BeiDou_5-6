using System;
using System.Collections.Generic;
using UnityEngine;

public static class NativeRankingWindow
{
	private const string Root = "UI/UIWindow.img/Ranking/";

	private const float W = 295f;

	private const float H = 335f;

	private const float RowH = 14f;

	private const int RowsVisible = 20;

	private const float ListX = 10f;

	private const float ListY = 46f;

	private const float ListW = 268f;

	private const float ListH = 281f;

	private static bool visible;

	private static int tab;

	private static float scroll;

	private static List<RankingEntry> entries = new List<RankingEntry>();

	private static RankingResponse info;

	private static string hint = "";

	private static Vector2 position = new Vector2(250f, 50f);

	private static Vector2 grab;

	private static bool dragging;

	public static bool Visible => visible;

	public static void Reset()
	{
		visible = false;
		tab = 0;
		scroll = 0f;
		entries = new List<RankingEntry>();
		info = null;
		hint = "";
	}

	public static void Close()
	{
		Reset();
	}

	public static void Toggle()
	{
		if (visible)
		{
			Close();
		}
		else
		{
			Open();
		}
	}

	public static void Open()
	{
		if (GameSession.InGame)
		{
			Reset();
			if (Fetch())
			{
				visible = true;
			}
		}
	}

	private static bool Fetch()
	{
		if (!GameSession.InGame)
		{
			return false;
		}
		int num = ((GameSession.Character != null) ? GameSession.Character.record.job : 0);
		int jobGroup = ((tab == 1) ? (num / 100) : (-1));
		RankingResponse rankingResponse = GameSession.Net.Ranking(new RankingRequest
		{
			characterId = GameSession.CharacterId,
			operation = RankingOperation.Top,
			jobGroup = jobGroup
		});
		if (!rankingResponse.ok)
		{
			BottomBar.Toast(rankingResponse.error);
			return false;
		}
		info = rankingResponse;
		entries = rankingResponse.entries ?? new List<RankingEntry>();
		hint = ((entries.Count == 0) ? "暂无排行数据" : "");
		scroll = 0f;
		return true;
	}

	private static GUIStyle Label(bool right = false, int size = 10)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected Obj, but got Unknown
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected Obj, but got Unknown
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = size,
			padding = new RectOffset(),
			clipping = (TextClipping)1,
			alignment = (TextAnchor)(right ? 2 : 0),
			wordWrap = false
		};
		val.normal.textColor = Color.black;
		return val;
	}

	private static GUIStyle TabLabel()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected Obj, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected Obj, but got Unknown
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = 10,
			padding = new RectOffset(),
			clipping = (TextClipping)1,
			alignment = (TextAnchor)4,
			wordWrap = false
		};
		val.normal.textColor = Color.black;
		return val;
	}

	private static void Image(string node, float x, float y)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://" + node);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(x, y, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
		}
	}

	private static string Move(int move)
	{
		if (move > 0)
		{
			return " ▲" + move;
		}
		if (move < 0)
		{
			return " ▼" + -move;
		}
		return "";
	}

	private static void Tabs(Vector2 p)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Invalid comparison between Unknown and I4
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		string[] array = new string[2] { "综合", "职业" };
		for (int i = 0; i < array.Length; i++)
		{
			Rect val = new Rect((float)(12 + i * 56), 23f, 53f, 14f);
			Image("UI/UIWindow.img/Ranking/Tab/" + ((tab == i) ? "enabled" : "disabled") + "/0", val.x, val.y);
			GUI.Label(val, array[i], TabLabel());
			if (GUI.enabled && (int)Event.current.type == 7 && Pointer.Released && val.Contains(p))
			{
				Pointer.ConsumeClick();
				if (tab != i)
				{
					tab = i;
					Fetch();
				}
				break;
			}
		}
	}

	private static void DrawList(Vector2 p)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		int num = Math.Max(0, entries.Count - 20);
		scroll = NativeUi.Scroll4(new Rect(276f, 46f, 12f, 281f), new Rect(10f, 46f, 268f, 281f), scroll, num, 1f, p);
		for (int i = 0; i < 20; i++)
		{
			int num2 = i + (int)scroll;
			if (num2 >= entries.Count)
			{
				break;
			}
			RankingEntry rankingEntry = entries[num2];
			float num3 = 46f + (float)i * 14f;
			if (rankingEntry.name == ((info != null && GameSession.Character != null) ? GameSession.Character.record.name : null))
			{
				GUI.Box(new Rect(10f, num3 - 1f, 256f, 14f), "");
			}
			GUI.Label(new Rect(10f, num3, 26f, 13f), rankingEntry.rank.ToString(), Label(right: true));
			GUI.Label(new Rect(40f, num3, 118f, 13f), rankingEntry.name, Label());
			GUI.Label(new Rect(160f, num3, 32f, 13f), rankingEntry.level.ToString(), Label(right: true));
			GUI.Label(new Rect(196f, num3, 68f, 13f), JobDefs.NameOf(rankingEntry.job), Label());
			GUI.Label(new Rect(252f, num3, 26f, 13f), Move(rankingEntry.rankMove), Label(right: true));
		}
		if (!string.IsNullOrEmpty(hint))
		{
			GUI.Label(new Rect(10f, 50f, 268f, 13f), hint, Label());
		}
	}

	public static void Draw()
	{
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Invalid comparison between Unknown and I4
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
		if (!visible)
		{
			return;
		}
		if (!GameSession.InGame)
		{
			Reset();
			return;
		}
		float s = BottomBar.S;
		position = NativeUi.ClampWindowPosition(position, new Vector2(295f, 335f), s);
		NativeUi.RegisterWindow("NativeRankingWindow", new Rect(position.x * s, position.y * s, 295f * s, 335f * s), () => Visible);
		Vector2 val = Pointer.Pos / s;
		Matrix4x4 matrix = GUI.matrix;
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(s, s, 1f));
		try
		{
			if (GUI.enabled && (int)Event.current.type == 7 && Pointer.Pressed)
			{
				Rect val2 = new Rect(position.x, position.y, 295f, 16f);
				if (val2.Contains(val))
				{
					dragging = true;
					grab = val - position;
				}
			}
			if (dragging && Pointer.Down)
			{
				position = NativeUi.ClampWindowPosition(val - grab, new Vector2(295f, 335f), s);
			}
			if (!Pointer.Down)
			{
				dragging = false;
			}
			GUI.BeginGroup(new Rect(position.x, position.y, 295f, 335f));
			try
			{
				Vector2 val3 = val - position;
				Image("UI/UIWindow.img/Ranking/backgrnd", 0f, 0f);
				GUI.Label(new Rect(12f, 7f, 120f, 13f), (info != null && info.wholeServer) ? "全服排行" : "我的排行", Label());
				if (info != null)
				{
					GUI.Label(new Rect(140f, 7f, 143f, 13f), "综合 #" + info.myRank + Move(info.myRankMove) + "   职业 #" + info.myJobRank + Move(info.myJobRankMove), Label(right: true));
				}
				if (NativeUi.Button(new Rect(273f, 4f, 16f, 16f), "UI/UIWindow.img/BtUIClose2", val3))
				{
					Close();
					return;
				}
				Tabs(val3);
				DrawList(val3);
			}
			finally
			{
				GUI.EndGroup();
			}
		}
		finally
		{
			GUI.matrix = matrix;
		}
	}

	static NativeRankingWindow()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
	}
}
