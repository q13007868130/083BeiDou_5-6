using UnityEngine;

public static class NativeGuildGradeWindow
{
	public static bool Visible;

	private const int WinW = 233;

	private const int WinH = 304;

	private const string Root = "UI/UIWindow.img/UserList/Guild/";

	private const int ColNumber = 26;

	private const int ColCount = 66;

	private const int ColTitle = 146;

	private const int ColTitleW = 86;

	private const int RowsTop = 146;

	private const int RowStep = 18;

	private const int Rows = 5;

	private static bool editing;

	private static string[] typed = new string[5];

	private static readonly Color TextOn = new Color(0.08f, 0.08f, 0.1f);

	private static float S => BottomBar.S;

	private static CommunityResponse State => NativeCommunityWindow.State;

	private static Rect At(Rect win, float x, float y, float w, float h)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(win.x + x * S, win.y + y * S, w * S, h * S);
	}

	private static GUIStyle Style(int size, Color color, TextAnchor anchor = (TextAnchor)4)
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
		Visible = !Visible;
		editing = false;
		if (Visible)
		{
			Snapshot();
		}
	}

	public static void Reset()
	{
		Visible = false;
		editing = false;
	}

	private static void Snapshot()
	{
		string[] array = State?.titles;
		for (int i = 0; i < 5; i++)
		{
			typed[i] = ((array != null && i < array.Length) ? (array[i] ?? "") : "");
		}
	}

	public static void Draw()
	{
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02de: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		//IL_0336: Unknown result type (might be due to invalid IL or missing references)
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
		float s = S;
		float num = 312f * s;
		float num2 = Mathf.Max(4f, (UiKit.W - num) / 2f);
		float num3 = num2 + num + 4f;
		float num4 = ((num3 + 233f * s <= UiKit.W - 4f) ? num3 : (num2 - 233f * s - 4f));
		num4 = Mathf.Max(4f, Mathf.Min(num4, UiKit.W - 233f * s - 4f));
		Rect val = new Rect(num4, 26f, 233f * s, 304f * s);
		NativeUi.RegisterWindow("NativeGuildGradeWindow", val, () => Visible);
		Texture2D val2 = Art.Tex("wz://UI/UIWindow.img/UserList/Guild/backgrnd");
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(val, (Texture)(object)val2);
		}
		Slice(val, "GuildGrade/guildgrade3", 6, 8);
		Slice(val, "GuildGrade/guildgrade4", 6, 45);
		Slice(val, "GuildGrade/guildgrade0", 6, 91);
		Slice(val, "GuildGrade/guildgrade1", 6, 128);
		Slice(val, "GuildGrade/guildgrade2", 6, 146);
		GUI.Label(At(val, 10f, 64f, 200f, 15f), state.gp.ToString(), Style(11, TextOn, (TextAnchor)3));
		for (int num5 = 0; num5 < 5; num5++)
		{
			float num6 = 146 + num5 * 18;
			GUI.Label(At(val, 20f, num6 + 3f, 24f, 18f), (num5 + 1).ToString(), Style(11, TextOn, (TextAnchor)4));
			GUI.Label(At(val, 60f, num6 + 3f, 30f, 18f), CountOf(state, num5 + 1).ToString(), Style(11, TextOn, (TextAnchor)4));
			if (editing)
			{
				GUIStyle val3 = Style(11, TextOn, (TextAnchor)3);
				typed[num5] = GUI.TextField(At(val, 142f, num6 + 2f, 86f, 14f), typed[num5] ?? "", 10, val3);
			}
			else
			{
				GUI.Label(At(val, 142f, num6 + 3f, 86f, 18f), typed[num5] ?? "", Style(11, TextOn, (TextAnchor)4));
			}
		}
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && !editing;
		if (NativeUi.Button(At(val, 25f, 268f, 80f, 17f), "UI/UIWindow.img/UserList/Guild/GuildGrade/BtModf"))
		{
			editing = true;
			Snapshot();
		}
		GUI.enabled = enabled && editing;
		if (NativeUi.Button(At(val, 125f, 268f, 80f, 17f), "UI/UIWindow.img/UserList/Guild/GuildGrade/BtSave"))
		{
			NativeCommunityWindow.InputTitles = (string[])typed.Clone();
			NativeCommunityWindow.Request(CommunityOperation.GuildTitles);
			editing = false;
		}
		GUI.enabled = enabled;
	}

	private static void Slice(Rect win, string node, int x, int y)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://UI/UIWindow.img/UserList/Guild/" + node);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(At(win, x, y, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
		}
	}

	private static int CountOf(CommunityResponse st, int rank)
	{
		if (st?.guild == null)
		{
			return 0;
		}
		int num = 0;
		foreach (CommunityMember item in st.guild)
		{
			if (item.rank == rank)
			{
				num++;
			}
		}
		return num;
	}

	public static Vector2 SizeOf()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2(233f, 304f);
	}

	public static int RowsTopY()
	{
		return 146;
	}

	public static int RowPitch()
	{
		return 18;
	}

	public static int RowCount()
	{
		return 5;
	}

	public static int TitleColumn()
	{
		return 146;
	}

	public static int TitleColumnWidth()
	{
		return 86;
	}

	static NativeGuildGradeWindow()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
	}
}
