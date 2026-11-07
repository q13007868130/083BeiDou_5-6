using UnityEngine;

public static class NativeCarnivalLobby
{
	private static readonly string[] FieldNames = new string[6] { "第一战场", "第二战场", "第三战场", "第四战场", "第五战场", "第六战场" };

	private static int pendingLobby = -1;

	private const int WinW = 220;

	private const int WinH = 200;

	private static Vector2 pos = new Vector2(-1f, -1f);

	public static bool Visible { get; private set; }

	private static float S => BottomBar.S;

	private static Rect At(float x, float y, float w, float h)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		return new Rect((pos.x + x) * S, (pos.y + y) * S, w * S, h * S);
	}

	public static void Open(int field = -1)
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		Visible = true;
		pendingLobby = field;
		if (pos.x < 0f)
		{
			pos = new Vector2((UiKit.W / S - 220f) / 2f, (UiKit.H / S - 200f) / 2f);
		}
		pos = NativeUi.ClampWindowPosition(pos, new Vector2(220f, 200f), S);
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
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		if (pos.x < 0f)
		{
			pos = new Vector2((UiKit.W / S - 220f) / 2f, (UiKit.H / S - 200f) / 2f);
		}
		NativeUi.RegisterWindow("NativeCarnivalLobby", new Rect(pos.x * S, pos.y * S, 220f * S, 200f * S), () => Visible);
		UiKit.Fill(new Rect(pos.x * S, pos.y * S, 220f * S, 200f * S), new Color(0.08f, 0.09f, 0.13f, 0.95f));
		UiKit.Fill(new Rect(pos.x * S, pos.y * S, 220f * S, 22f * S), new Color(0.16f, 0.2f, 0.32f));
		GUI.Label(new Rect((pos.x + 8f) * S, pos.y * S + 3f * S, 160f * S, 18f * S), "怪物嘉年华 · 选择战场", Label(12, Color.white, (TextAnchor)3));
		if (NativeUi.Button(At(203f, 3f, 12f, 12f), "UI/Basic.img/BtClose"))
		{
			Close();
			return;
		}
		float num = 30f;
		if (pendingLobby >= 0)
		{
			GUI.Label(At(10f, num, 200f, 30f), "已选择 " + FieldNames[Mathf.Clamp(pendingLobby, 0, 5)] + "，点击进入战斗。", Label(11, Color.white, (TextAnchor)0));
			num += 34f;
			if (Button(At(40f, num, 140f, 26f), "进入战斗"))
			{
				Enter(pendingLobby);
			}
			return;
		}
		for (int num2 = 0; num2 < FieldNames.Length; num2++)
		{
			if (Button(At(20f, num, 180f, 22f), FieldNames[num2]))
			{
				Enter(num2);
				return;
			}
			num += 26f;
		}
		GUI.Label(At(10f, num + 2f, 200f, 16f), "30~50 级 · 击杀怪物为红队赢取 CP", Label(10, new Color(0.7f, 0.72f, 0.8f), (TextAnchor)3));
	}

	private static void Enter(int field)
	{
		int mapId = 980000101 + field * 100;
		CarnivalResponse carnivalResponse = GameSession.Net.Carnival(new CarnivalRequest
		{
			characterId = GameSession.CharacterId,
			operation = 0,
			mapId = mapId
		});
		if (carnivalResponse == null || !carnivalResponse.ok)
		{
			BottomBar.Toast(carnivalResponse?.error ?? "无法进入嘉年华");
			return;
		}
		Close();
		NativeCarnivalHud.Reset();
		GameLoop.I?.field?.WarpTo(mapId);
	}

	private static bool Button(Rect r, string label)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Invalid comparison between Unknown and I4
		bool flag = r.Contains(Pointer.Pos);
		UiKit.Fill(r, flag ? new Color(0.3f, 0.42f, 0.7f, 0.95f) : new Color(0.2f, 0.28f, 0.5f, 0.9f));
		GUI.Label(r, label, Label(12, Color.white, (TextAnchor)4));
		int num;
		if (flag && (int)Event.current.type == 7)
		{
			num = (Pointer.Clicked ? 1 : 0);
			if (num != 0)
			{
				Pointer.ConsumeClick();
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}

	private static GUIStyle Label(int size, Color color, TextAnchor anchor)
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
			fontSize = Mathf.Max(8, Mathf.RoundToInt((float)size * S)),
			alignment = anchor
		};
		val.normal.textColor = color;
		return val;
	}

	static NativeCarnivalLobby()
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
	}
}
