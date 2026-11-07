using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class NativeClaimWindow
{
	private const string Reasons = "辱骂/淫秽词语/泄露个人信息|广告：业务目的的网站宣传|欺诈：非法网站宣传及诱导玩家登陆|现金交易：物品、帐号、现金等交易情况|冒充运营人员";

	private static Vector2 pos = new Vector2(-1f, -1f);

	private static bool notice;

	private static int type = 1;

	private static int reason;

	private static bool reasonOpen;

	private static string victim = "";

	private static string description = "";

	private const int WinW = 268;

	private const int WinH = 345;

	private static bool dragging;

	private static Vector2 dragOff;

	public static bool Visible { get; private set; }

	private static float S => BottomBar.S;

	private static Texture2D Wz(string node)
	{
		return Art.Tex("wz://UI/UIWindow.img/Claim/" + node);
	}

	private static Rect At(float x, float y, float w, float h)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		return new Rect((pos.x + x) * S, (pos.y + y) * S, w * S, h * S);
	}

	private static GUIStyle Style(int size, Color color, TextAnchor anchor = (TextAnchor)3, bool wrap = false)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected Obj, but got Unknown
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = Mathf.Max(7, Mathf.RoundToInt((float)size * S)),
			alignment = anchor,
			wordWrap = wrap
		};
		val.normal.textColor = color;
		return val;
	}

	public static void Open(string victimName)
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		Visible = true;
		notice = false;
		type = 1;
		reason = 0;
		reasonOpen = false;
		victim = victimName ?? "";
		description = "";
		if (pos.x < 0f)
		{
			pos = new Vector2((UiKit.W / S - 268f) / 2f, (UiKit.H / S - 345f) / 2f);
		}
		pos = NativeUi.ClampWindowPosition(pos, new Vector2(268f, (float)(notice ? 429 : 345)), S);
	}

	public static void Close()
	{
		Visible = false;
		reasonOpen = false;
	}

	public static void Reset()
	{
		Close();
	}

	public static void Draw()
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		if (pos.x < 0f)
		{
			pos = new Vector2((UiKit.W / S - 268f) / 2f, (UiKit.H / S - 345f) / 2f);
		}
		NativeUi.RegisterWindow("NativeClaimWindow", new Rect(pos.x * S, pos.y * S, 268f * S, (float)(notice ? 429 : 345) * S), () => Visible);
		Texture2D val = Wz(notice ? "notice" : "backgrnd");
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(pos.x * S, pos.y * S, (float)((Texture)val).width * S, (float)((Texture)val).height * S), (Texture)(object)val);
		}
		if (NativeUi.Button(At(251f, 3f, 12f, 12f), "UI/Basic.img/BtClose"))
		{
			Close();
			return;
		}
		if (notice)
		{
			DrawNotice();
		}
		else
		{
			DrawForm();
		}
		HandleDrag();
	}

	private static void HandleDrag()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Invalid comparison between Unknown and I4
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		float num = (notice ? 429 : 345);
		if ((int)Event.current.type == 7 && Pointer.Pressed)
		{
			Rect val = new Rect(pos.x * S, pos.y * S, 268f * S, 18f * S);
			if (val.Contains(Pointer.Pos))
			{
				dragging = true;
				dragOff = Pointer.Pos / S - pos;
			}
		}
		if (dragging && Pointer.Down)
		{
			pos = Pointer.Pos / S - dragOff;
			pos = NativeUi.ClampWindowPosition(pos, new Vector2(268f, num), S);
		}
		if (!Pointer.Down)
		{
			dragging = false;
		}
	}

	private static void DrawForm()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Invalid comparison between Unknown and I4
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		if (reasonOpen)
		{
			DrawReasonList();
		}
		bool num = (int)Event.current.type == 7 && Pointer.Pressed;
		Rect val = At(107f, 155f, 157f, 19f);
		Texture2D val2 = Wz("box1");
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(val, (Texture)(object)val2);
		}
		GUI.Label(At(112f, 157f, 147f, 16f), "辱骂/淫秽词语/泄露个人信息|广告：业务目的的网站宣传|欺诈：非法网站宣传及诱导玩家登陆|现金交易：物品、帐号、现金等交易情况|冒充运营人员".Split('|')[reason], Style(12, new Color(0.1f, 0.11f, 0.15f), (TextAnchor)3));
		if (num && val.Contains(Pointer.Pos))
		{
			reasonOpen = !reasonOpen;
		}
		Rect val3 = At(107f, 176f, 137f, 17f);
		Texture2D val4 = Wz("box2");
		if ((Object)(object)val4 != (Object)null)
		{
			GUI.DrawTexture(val3, (Texture)(object)val4);
		}
		GUIStyle val5 = Style(12, new Color(0.1f, 0.11f, 0.15f), (TextAnchor)3);
		string text = GUI.TextField(val3, victim, 12, val5);
		if (text != victim)
		{
			victim = text;
		}
		Rect val6 = At(110f, 200f, 152f, 60f);
		GUIStyle val7 = Style(12, new Color(0.1f, 0.11f, 0.15f), (TextAnchor)0, wrap: true);
		string text2 = GUI.TextArea(val6, description, 200, val7);
		if (text2 != description)
		{
			description = text2;
		}
		if (NativeUi.Button(At(7f, 313f, 108f, 17f), "UI/UIWindow.img/Claim/BtCClaim"))
		{
			type = 1;
			notice = true;
		}
		if (NativeUi.Button(At(141f, 313f, 108f, 17f), "UI/UIWindow.img/Claim/BtPClaim"))
		{
			type = 0;
			notice = true;
		}
	}

	private static void DrawReasonList()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Invalid comparison between Unknown and I4
		string[] array = "辱骂/淫秽词语/泄露个人信息|广告：业务目的的网站宣传|欺诈：非法网站宣传及诱导玩家登陆|现金交易：物品、帐号、现金等交易情况|冒充运营人员".Split('|');
		float num = 174f;
		Rect r = At(107f, num, 157f, (float)array.Length * 17f + 2f);
		UiKit.Fill(r, new Color(1f, 1f, 1f));
		UiKit.Fill(new Rect(r.x, r.y, r.width, 1f), new Color(0.55f, 0.58f, 0.65f));
		UiKit.Fill(new Rect(r.x, r.yMax - 1f, r.width, 1f), new Color(0.55f, 0.58f, 0.65f));
		UiKit.Fill(new Rect(r.x, r.y, 1f, r.height), new Color(0.55f, 0.58f, 0.65f));
		UiKit.Fill(new Rect(r.xMax - 1f, r.y, 1f, r.height), new Color(0.55f, 0.58f, 0.65f));
		for (int i = 0; i < array.Length; i++)
		{
			Rect val = At(108f, num + 1f + (float)(i * 17), 155f, 17f);
			bool flag = val.Contains(Pointer.Pos);
			if (flag)
			{
				UiKit.Fill(val, new Color(0.85f, 0.88f, 0.95f));
			}
			GUI.Label(val, array[i], Style(12, new Color(0.1f, 0.11f, 0.15f), (TextAnchor)3));
			if (flag && (int)Event.current.type == 7 && Pointer.Pressed)
			{
				reason = i;
				reasonOpen = false;
				Pointer.ConsumeClick();
			}
		}
	}

	private static void DrawNotice()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		if (NativeUi.Button(At(73f, 406f, 59f, 17f), "UI/UIWindow.img/Claim/BtClaim"))
		{
			Submit();
		}
		if (NativeUi.Button(At(136f, 406f, 61f, 17f), "UI/UIWindow.img/Claim/BtCancel"))
		{
			notice = false;
		}
	}

	private static void Submit()
	{
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrEmpty(victim))
		{
			BottomBar.Toast("请填写要举报的角色名。");
			return;
		}
		if (type == 0)
		{
			reason = 0;
		}
		ReportResponse reportResponse = GameSession.Net.Report(new ReportRequest
		{
			characterId = GameSession.CharacterId,
			type = type,
			victim = victim,
			reason = reason,
			description = description,
			chatlog = ((type == 1) ? ChatLogSnapshot() : "")
		});
		if (reportResponse.ok)
		{
			BottomBar.AddChat("举报已成功登记。", BottomBar.ChatYellow);
			Close();
			return;
		}
		BottomBar.AddChat(reportResponse.error, BottomBar.ChatPink);
		if (reportResponse.code == 2 || reportResponse.code == 4)
		{
			Close();
		}
	}

	private static string ChatLogSnapshot()
	{
		IReadOnlyList<BottomBar.ChatLine> chatHistory = BottomBar.ChatHistory;
		int num = Mathf.Max(0, chatHistory.Count - 30);
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = num; i < chatHistory.Count; i++)
		{
			stringBuilder.AppendLine(chatHistory[i].text);
		}
		return stringBuilder.ToString();
	}

	static NativeClaimWindow()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
	}
}
