using System;
using System.Collections.Generic;
using UnityEngine;

public static class NativeMemoWindow
{
	private enum Mode
	{
		Read,
		Detail,
		Write
	}

	private static Mode mode = Mode.Read;

	private static MemoNote[] notes = Array.Empty<MemoNote>();

	private static bool[] checkedList = Array.Empty<bool>();

	private static MemoNote detail;

	private static string target = "";

	private static string message = "";

	private static bool busy;

	private static string hint = "";

	private const int WinW = 295;

	private const int WinH = 240;

	private const int ListY = 26;

	private const int ListH = 168;

	private const int RowH = 28;

	private static Vector2 pos = new Vector2(-1f, -1f);

	private static bool dragging;

	private static Vector2 dragOff;

	public static bool Visible { get; private set; }

	private static float S => BottomBar.S;

	private static Texture2D Wz(string node)
	{
		return Art.Tex("wz://UI/UIWindow.img/Memo/" + node);
	}

	private static Rect At(float x, float y, float w, float h)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		return new Rect((pos.x + x) * S, (pos.y + y) * S, w * S, h * S);
	}

	private static GUIStyle Style(int size, Color color, TextAnchor anchor = (TextAnchor)3)
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
			clipping = (TextClipping)1
		};
		val.normal.textColor = color;
		return val;
	}

	private static GUIStyle Field()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected Obj, but got Unknown
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = 12,
			clipping = (TextClipping)1,
			alignment = (TextAnchor)0
		};
		val.normal.textColor = Color.black;
		return val;
	}

	public static void Open()
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		Visible = true;
		mode = Mode.Read;
		busy = false;
		hint = "";
		target = "";
		message = "";
		detail = null;
		if (pos.x < 0f)
		{
			pos = new Vector2((UiKit.W / S - 295f) / 2f, (UiKit.H / S - 240f) / 2f);
		}
		pos = NativeUi.ClampWindowPosition(pos, new Vector2(295f, 240f), S);
		Refresh();
	}

	public static void Refresh()
	{
		if (GameSession.InGame)
		{
			notes = GameSession.Net.Memo(new MemoRequest
			{
				characterId = GameSession.CharacterId,
				action = 2
			})?.notes ?? Array.Empty<MemoNote>();
			checkedList = new bool[notes.Length];
		}
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
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Invalid comparison between Unknown and I4
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		if (pos.x < 0f)
		{
			pos = new Vector2((UiKit.W / S - 295f) / 2f, (UiKit.H / S - 240f) / 2f);
		}
		NativeUi.RegisterWindow("NativeMemoWindow", new Rect(pos.x * S, pos.y * S, 295f * S, 240f * S), () => Visible);
		Texture2D val = Wz("backgrnd");
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(pos.x * S, pos.y * S, (float)((Texture)val).width * S, (float)((Texture)val).height * S), (Texture)(object)val);
		}
		Texture2D val2 = Wz("backgrnd2");
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(new Rect(pos.x * S, pos.y * S, (float)((Texture)val2).width * S, (float)((Texture)val2).height * S), (Texture)(object)val2);
		}
		if (NativeUi.Button(At(278f, 3f, 12f, 12f), "UI/Basic.img/BtClose"))
		{
			Close();
			return;
		}
		switch (mode)
		{
		case Mode.Read:
			DrawRead();
			break;
		case Mode.Detail:
			DrawDetail();
			break;
		case Mode.Write:
			DrawWrite();
			break;
		}
		if (!string.IsNullOrEmpty(hint))
		{
			GUI.Label(At(18f, 200f, 259f, 14f), hint, Style(11, new Color(0.55f, 0.15f, 0.15f), (TextAnchor)3));
		}
		if ((int)Event.current.type == 7 && Pointer.Pressed)
		{
			Rect val3 = new Rect(pos.x * S, pos.y * S, 295f * S, 18f * S);
			if (val3.Contains(Pointer.Pos))
			{
				dragging = true;
				dragOff = Pointer.Pos / S - pos;
			}
		}
		if (dragging && Pointer.Down)
		{
			pos = Pointer.Pos / S - dragOff;
			pos = NativeUi.ClampWindowPosition(pos, new Vector2(295f, 240f), S);
		}
		if (!Pointer.Down)
		{
			dragging = false;
		}
	}

	private static void DrawRead()
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Invalid comparison between Unknown and I4
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0421: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Wz("backgrnd3");
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(At(0f, 26f, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
		}
		GUI.Label(At(18f, 30f, 259f, 14f), (notes.Length == 0) ? "没有未读的便签。" : ("共 " + notes.Length + " 张未读便签（点击勾选）"), Style(11, new Color(0.35f, 0.3f, 0.2f), (TextAnchor)3));
		for (int i = 0; i < notes.Length; i++)
		{
			float num = 48 + i * 28;
			if (num + 28f > 194f)
			{
				break;
			}
			MemoNote memoNote = notes[i];
			Rect val2 = new Rect(0f, num, 295f, 28f);
			if ((int)Event.current.type == 7 && Pointer.Clicked)
			{
				Rect val3 = At(val2.x, val2.y, val2.width, val2.height);
				if (val3.Contains(Pointer.Pos))
				{
					Pointer.ConsumeClick();
					checkedList[i] = !checkedList[i];
				}
			}
			Texture2D val4 = Wz(checkedList[i] ? "check1" : "check0");
			if ((Object)(object)val4 != (Object)null)
			{
				GUI.DrawTexture(At(20f, num + 8f, ((Texture)val4).width, ((Texture)val4).height), (Texture)(object)val4);
			}
			Texture2D val5 = Wz("line");
			if ((Object)(object)val5 != (Object)null)
			{
				GUI.DrawTexture(At(38f, num + 28f - 1f, ((Texture)val5).width, ((Texture)val5).height), (Texture)(object)val5);
			}
			GUI.Label(At(38f, num + 2f, 120f, 13f), memoNote.from, Style(12, new Color(0.15f, 0.2f, 0.5f), (TextAnchor)3));
			GUI.Label(At(158f, num + 2f, 120f, 13f), TimeText(memoNote.timestamp), Style(11, new Color(0.45f, 0.42f, 0.35f), (TextAnchor)3));
			GUI.Label(At(38f, num + 14f, 240f, 12f), memoNote.message, Style(11, new Color(0.25f, 0.22f, 0.16f), (TextAnchor)3));
		}
		if (NativeUi.Button(At(28f, 214f, 64f, 19f), "UI/UIWindow.img/Memo/BtOpen") && !busy)
		{
			int num2 = Selected();
			if (num2 < 0)
			{
				hint = "先勾选一张便签。";
			}
			else
			{
				detail = notes[num2];
				mode = Mode.Detail;
				hint = "";
			}
		}
		if (NativeUi.Button(At(104f, 214f, 39f, 19f), "UI/UIWindow.img/Memo/BtDelete") && !busy)
		{
			int[] array = CheckedIds();
			if (array.Length == 0)
			{
				hint = "先勾选要丢弃的便签。";
			}
			else
			{
				busy = true;
				Discard(array);
			}
		}
		bool flag = NativeNoteService.AvailableNotes > 0;
		Wz("BtSave/disabled/0");
		GUI.enabled = flag && !busy;
		if (NativeUi.Button(At(225f, 215f, 38f, 18f), "UI/UIWindow.img/Memo/BtSave") & flag)
		{
			target = "";
			message = "";
			mode = Mode.Write;
			hint = "";
		}
		GUI.enabled = true;
		if (flag)
		{
			GUI.Label(At(175f, 215f, 48f, 14f), "×" + NativeNoteService.AvailableNotes, Style(11, new Color(0.3f, 0.3f, 0.3f), (TextAnchor)5));
		}
	}

	private static void DrawDetail()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Wz("backgrnd3");
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(At(0f, 26f, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
		}
		if (detail == null)
		{
			mode = Mode.Read;
			return;
		}
		GUI.Label(At(20f, 34f, 150f, 14f), "发件人：" + detail.from, Style(12, new Color(0.15f, 0.2f, 0.5f), (TextAnchor)3));
		GUI.Label(At(20f, 50f, 250f, 14f), TimeText(detail.timestamp), Style(11, new Color(0.45f, 0.42f, 0.35f), (TextAnchor)3));
		GUI.Label(At(20f, 70f, 255f, 110f), detail.message, Style(12, new Color(0.2f, 0.18f, 0.12f), (TextAnchor)0));
		if (NativeUi.Button(At(115.5f, 214f, 64f, 19f), "UI/UIWindow.img/Memo/BtOpen"))
		{
			mode = Mode.Read;
			Refresh();
		}
	}

	private static void DrawWrite()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		float num = 17.5f;
		Texture2D val = Wz("innerTop");
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(At(num, 26f, 260f, 24f), (Texture)(object)val);
		}
		Texture2D val2 = Wz("innerCenter");
		if ((Object)(object)val2 != (Object)null)
		{
			for (float num2 = 50f; num2 < 126f; num2 += 18f)
			{
				GUI.DrawTexture(At(num, num2, 260f, 18f), (Texture)(object)val2);
			}
		}
		Texture2D val3 = Wz("innerBottom");
		if ((Object)(object)val3 != (Object)null)
		{
			GUI.DrawTexture(At(num, 126f, 260f, 8f), (Texture)(object)val3);
		}
		GUI.Label(At(num + 6f, 29f, 40f, 14f), "收件人", Style(10, new Color(0.4f, 0.36f, 0.28f), (TextAnchor)3));
		target = GUI.TextField(At(num + 52f, 30f, 200f, 15f), target ?? "", 13, Field());
		GUI.Label(At(num + 6f, 56f, 60f, 14f), "正文(73字)", Style(10, new Color(0.4f, 0.36f, 0.28f), (TextAnchor)3));
		message = GUI.TextField(At(num + 52f, 56f, 200f, 60f), message ?? "", 73, Field());
		if (NativeUi.Button(At(num + 92f, 138f, 38f, 18f), "UI/UIWindow.img/Memo/BtSave") && !busy)
		{
			if (string.IsNullOrEmpty(target.Trim()))
			{
				hint = "请输入收件角色名。";
			}
			else if (string.IsNullOrEmpty(message) || message.Length > 73)
			{
				hint = "便签须为1～73个字符。";
			}
			else
			{
				busy = true;
				SendReply();
			}
		}
		if (NativeUi.Button(At(num + 140f, 138f, 38f, 18f), "UI/Basic.img/BtCancel2"))
		{
			mode = Mode.Read;
			hint = "";
		}
	}

	private static int Selected()
	{
		for (int i = 0; i < checkedList.Length; i++)
		{
			if (checkedList[i])
			{
				return i;
			}
		}
		return -1;
	}

	private static int[] CheckedIds()
	{
		List<int> list = new List<int>();
		for (int i = 0; i < checkedList.Length && i < notes.Length; i++)
		{
			if (checkedList[i])
			{
				list.Add(notes[i].id);
			}
		}
		return list.ToArray();
	}

	private static void Discard(int[] ids)
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		MemoResponse memoResponse = GameSession.Net.Memo(new MemoRequest
		{
			characterId = GameSession.CharacterId,
			action = 1,
			discardIds = ids
		});
		busy = false;
		if (memoResponse == null || !memoResponse.ok)
		{
			hint = memoResponse?.error ?? "丢弃失败";
			return;
		}
		if (memoResponse.fameGain > 0)
		{
			BottomBar.AddChat("丢弃便签，名望 +" + memoResponse.fameGain, BottomBar.ChatYellow);
			GameSession.ReloadAfterCommand();
		}
		mode = Mode.Read;
		Refresh();
	}

	private static void SendReply()
	{
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		MemoResponse memoResponse = GameSession.Net.Memo(new MemoRequest
		{
			characterId = GameSession.CharacterId,
			action = 0,
			target = target,
			message = message
		});
		busy = false;
		if (memoResponse == null || !memoResponse.ok)
		{
			hint = memoResponse?.error ?? "发送失败";
			return;
		}
		BottomBar.AddChat("便签已寄出。", BottomBar.ChatYellow);
		mode = Mode.Read;
		Refresh();
	}

	private static string TimeText(long unixMs)
	{
		try
		{
			return DateTimeOffset.FromUnixTimeMilliseconds(unixMs).LocalDateTime.ToString("MM-dd HH:mm");
		}
		catch
		{
			return "";
		}
	}

	static NativeMemoWindow()
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
	}
}
