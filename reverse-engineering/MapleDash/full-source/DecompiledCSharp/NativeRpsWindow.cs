using UnityEngine;

public static class NativeRpsWindow
{
	private enum Phase
	{
		Idle,
		Choosing,
		Result
	}

	private static Phase phase = Phase.Idle;

	private static int round = 1;

	private static int myHand = -1;

	private static int npcHand = -1;

	private static int banner;

	private static bool busy;

	private static string roundText = "";

	private const int WinW = 310;

	private const int WinH = 358;

	private static Vector2 pos = new Vector2(-1f, -1f);

	private static bool dragging;

	private static Vector2 dragOff;

	private static readonly string[] HandNodes = new string[3] { "rock", "paper", "scissor" };

	private static readonly string[] FHandNodes = new string[3] { "Frock", "Fpaper", "Fscissor" };

	private static int LastAnswer;

	public static bool Visible { get; private set; }

	private static float S => BottomBar.S;

	private static Texture2D Wz(string node)
	{
		return Art.Tex("wz://UI/UIWindow.img/RpsGame/" + node);
	}

	private static Rect At(float x, float y, float w, float h)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		return new Rect((pos.x + x) * S, (pos.y + y) * S, w * S, h * S);
	}

	private static GUIStyle Style(int size, Color color, TextAnchor anchor = (TextAnchor)4)
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

	public static void Open()
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		Visible = true;
		phase = Phase.Idle;
		round = 1;
		myHand = (npcHand = -1);
		banner = 0;
		busy = false;
		roundText = "与 9000019 号管理员猜拳，一局 1000 金币，赢满 10 轮有大奖！";
		if (pos.x < 0f)
		{
			pos = new Vector2((UiKit.W / S - 310f) / 2f, (UiKit.H / S - 358f) / 2f);
		}
		pos = NativeUi.ClampWindowPosition(pos, new Vector2(310f, 358f), S);
	}

	public static void Close()
	{
		Visible = false;
	}

	public static void Reset()
	{
		Close();
	}

	public static void RequestLeave()
	{
		if (phase == Phase.Idle && round == 1 && banner == 0 && myHand < 0)
		{
			Close();
		}
		else
		{
			Send(RpsOperation.Leave, 0);
		}
	}

	public static void Draw()
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Invalid comparison between Unknown and I4
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		if (pos.x < 0f)
		{
			pos = new Vector2((UiKit.W / S - 310f) / 2f, (UiKit.H / S - 358f) / 2f);
		}
		NativeUi.RegisterWindow("NativeRpsWindow", new Rect(pos.x * S, pos.y * S, 310f * S, 358f * S), () => Visible);
		Texture2D val = Wz("backgrnd");
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(pos.x * S, pos.y * S, (float)((Texture)val).width * S, (float)((Texture)val).height * S), (Texture)(object)val);
		}
		if (NativeUi.Button(At(293f, 3f, 12f, 12f), "UI/Basic.img/BtClose"))
		{
			RequestLeave();
			return;
		}
		DrawHands();
		DrawBanner();
		DrawButtons();
		if ((int)Event.current.type == 7 && Pointer.Pressed)
		{
			Rect val2 = new Rect(pos.x * S, pos.y * S, 310f * S, 18f * S);
			if (val2.Contains(Pointer.Pos))
			{
				dragging = true;
				dragOff = Pointer.Pos / S - pos;
			}
		}
		if (dragging && Pointer.Down)
		{
			pos = Pointer.Pos / S - dragOff;
			pos = NativeUi.ClampWindowPosition(pos, new Vector2(310f, 358f), S);
		}
		if (!Pointer.Down)
		{
			dragging = false;
		}
	}

	private static void DrawHands()
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		if (myHand >= 0)
		{
			Texture2D val = Wz(HandNodes[myHand]);
			if ((Object)(object)val != (Object)null)
			{
				GUI.DrawTexture(At(24f, 42f, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
			}
		}
		if (npcHand >= 0)
		{
			Texture2D val2 = Wz(FHandNodes[npcHand]);
			if ((Object)(object)val2 != (Object)null)
			{
				GUI.DrawTexture(At(172f, 42f, ((Texture)val2).width, ((Texture)val2).height), (Texture)(object)val2);
			}
		}
		if (banner == 1)
		{
			Texture2D val3 = Wz("charWin");
			if ((Object)(object)val3 != (Object)null)
			{
				GUI.DrawTexture(At(22f, 148f, ((Texture)val3).width, ((Texture)val3).height), (Texture)(object)val3);
			}
		}
		else if (banner == 2)
		{
			Texture2D val4 = Wz("charLose");
			if ((Object)(object)val4 != (Object)null)
			{
				GUI.DrawTexture(At(222f, 148f, ((Texture)val4).width, ((Texture)val4).height), (Texture)(object)val4);
			}
		}
	}

	private static void DrawBanner()
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		if (banner != 0)
		{
			string node;
			if (banner == 1)
			{
				node = "win";
			}
			else if (banner == 2)
			{
				node = "lose";
			}
			else
			{
				node = ((banner == 3) ? "draw" : "timeover");
			}
			Texture2D val = Wz(node);
			if ((Object)(object)val != (Object)null)
			{
				GUI.DrawTexture(At((float)(310 - ((Texture)val).width) / 2f, 118f, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
			}
		}
	}

	private static void DrawButtons()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		if ((int)Event.current.type == 7)
		{
			_ = Pointer.Pressed;
		}
		else
			_ = 0;
		if (!string.IsNullOrEmpty(roundText))
		{
			GUI.Label(At(20f, 290f, 270f, 20f), roundText, Style(12, new Color(0.25f, 0.22f, 0.16f), (TextAnchor)4));
		}
		switch (phase)
		{
		case Phase.Idle:
			if (NativeUi.Button(At(105f, 291f, 99f, 32f), "UI/UIWindow.img/RpsGame/BtStart") && !busy)
			{
				busy = true;
				Send(RpsOperation.Start, 0);
			}
			break;
		case Phase.Choosing:
			if (NativeUi.Button(At(45f, 291f, 33f, 33f), "UI/UIWindow.img/RpsGame/BtRock") && !busy)
			{
				busy = true;
				Send(RpsOperation.Answer, 0);
			}
			if (NativeUi.Button(At(138f, 291f, 33f, 33f), "UI/UIWindow.img/RpsGame/BtPaper") && !busy)
			{
				busy = true;
				Send(RpsOperation.Answer, 1);
			}
			if (NativeUi.Button(At(231f, 291f, 33f, 33f), "UI/UIWindow.img/RpsGame/BtScissor") && !busy)
			{
				busy = true;
				Send(RpsOperation.Answer, 2);
			}
			break;
		case Phase.Result:
			if (banner == 1)
			{
				if (NativeUi.Button(At(104f, 290f, 101f, 35f), "UI/UIWindow.img/RpsGame/BtContinue") && !busy)
				{
					busy = true;
					Send(RpsOperation.Continue, 0);
				}
				break;
			}
			if (NativeUi.Button(At(60f, 290f, 101f, 35f), "UI/UIWindow.img/RpsGame/BtRetry") && !busy)
			{
				busy = true;
				Send(RpsOperation.Retry, 0);
			}
			if (NativeUi.Button(At(180f, 299f, 59f, 17f), "UI/UIWindow.img/RpsGame/BtExit") && !busy)
			{
				busy = true;
				Send(RpsOperation.Leave, 0);
			}
			break;
		}
	}

	private static void Send(RpsOperation op, int answer)
	{
		if (op == RpsOperation.Answer)
		{
			LastAnswer = answer;
		}
		RpsResponse r = GameSession.Net.Rps(new RpsRequest
		{
			characterId = GameSession.CharacterId,
			operation = op,
			answer = answer
		});
		busy = false;
		Apply(r);
	}

	private static void Apply(RpsResponse r)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		if (r.rewardItem != 0)
		{
			BottomBar.AddChat("获得了猜拳胜利证书。", BottomBar.ChatYellow);
			GameSession.ReloadAfterCommand();
		}
		else if (r.mode == 9 || r.mode == 14)
		{
			GameSession.ReloadAfterCommand();
		}
		if (!r.ok)
		{
			if (!string.IsNullOrEmpty(r.error))
			{
				BottomBar.AddChat(r.error, BottomBar.ChatPink);
			}
			if (r.closed)
			{
				Close();
			}
			return;
		}
		switch (r.mode)
		{
		case 9:
		case 14:
			phase = Phase.Choosing;
			round = 1;
			myHand = (npcHand = -1);
			banner = 0;
			roundText = "第 1 轮 —— 出拳吧！";
			break;
		case 11:
			npcHand = r.selection;
			myHand = LastAnswer;
			if (r.roundLabel < 0)
			{
				banner = 2;
				phase = Phase.Result;
				roundText = "第 " + round + " 轮输了……（赢了 " + (round - 1) + " 轮）";
			}
			else if (r.roundLabel == round)
			{
				banner = 3;
				roundText = "平局！再出一次。";
				myHand = (npcHand = -1);
			}
			else
			{
				round = r.roundLabel;
				banner = 1;
				phase = Phase.Result;
				roundText = "第 " + round + " 轮获胜！" + ((round >= 10) ? "" : "点「继续」进入下一轮。");
			}
			break;
		case 12:
			phase = Phase.Choosing;
			myHand = (npcHand = -1);
			banner = 0;
			roundText = "第 " + round + " 轮 —— 出拳吧！";
			break;
		case 13:
			Close();
			break;
		case 10:
			break;
		}
	}

	static NativeRpsWindow()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
	}
}
