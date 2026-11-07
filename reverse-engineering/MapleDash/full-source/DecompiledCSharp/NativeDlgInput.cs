using System;
using UnityEngine;

public static class NativeDlgInput
{
	private const int W = 529;

	private const int H = 206;

	private const string OwnerKey = "NativeDlgInput";

	private static string prompt;

	private static string value;

	private static int max;

	private static Action<string> done;

	public static bool Visible => done != null;

	public static void Open(string text, string initial, int limit, Action<string> callback)
	{
		NativeEditKeyboard.Close();
		prompt = text;
		value = initial ?? "";
		max = Mathf.Max(1, limit);
		done = callback;
	}

	public static void Close()
	{
		done = null;
		prompt = (value = null);
		max = 0;
		NativeEditKeyboard.Close();
	}

	private static void Style(out GUIStyle body)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected Obj, but got Unknown
		//IL_003a: Expected Obj, but got Unknown
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		body = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = Mathf.RoundToInt(12f * BottomBar.S),
			wordWrap = true,
			padding = new RectOffset()
		};
		body.normal.textColor = new Color(0.08f, 0.08f, 0.1f);
	}

	public static void Draw()
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Expected Obj, but got Unknown
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Invalid comparison between Unknown and I4
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Invalid comparison between Unknown and I4
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Invalid comparison between Unknown and I4
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Invalid comparison between Unknown and I4
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Invalid comparison between Unknown and I4
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a4: Invalid comparison between Unknown and I4
		if (!Visible)
		{
			return;
		}
		NativeEditKeyboard.Signal signal = NativeEditKeyboard.Sync("NativeDlgInput", ref value);
		float s = BottomBar.S;
		Rect rect = new Rect((UiKit.W - 529f * s) / 2f, (UiKit.H - 206f * s) / 2f, 529f * s, 206f * s);
		NativeUi.DialogPanel(rect);
		Style(out var body);
		GUI.Label(new Rect(rect.x + 157f * s, rect.y + 28f * s, 340f * s, 120f * s), prompt ?? "", body);
		Rect r = new Rect(rect.x + 159f * s, rect.y + 153f * s, 213f * s, 14f * s);
		UiKit.Fill(r, new Color(0.25f, 0.27f, 0.32f));
		UiKit.Fill(new Rect(r.x + s, r.y + s, r.width - 2f * s, r.height - 2f * s), Color.white);
		GUIStyle val = new GUIStyle(body)
		{
			wordWrap = false
		};
		value = GUI.TextField(new Rect(r.x + 2f * s, r.y + s, r.width - 4f * s, r.height - 2f * s), value ?? "", max, val);
		if ((int)Event.current.type == 7 && GUI.enabled && Pointer.Clicked && r.Contains(Pointer.Pos))
		{
			Pointer.ConsumeClick();
			NativeEditKeyboard.Open("NativeDlgInput", value, (TouchScreenKeyboardType)0, max);
		}
		bool flag = NativeUi.Button(new Rect(rect.x + 475f * s, rect.y + 180f * s, 43f * s, 19f * s), "UI/UIWindow.img/UtilDlgEx/BtOK");
		bool flag2 = NativeUi.Button(new Rect(rect.x + 9f * s, rect.y + 180f * s, 43f * s, 19f * s), "UI/UIWindow.img/UtilDlgEx/BtClose");
		Event current = Event.current;
		if ((int)current.type == 4 && (int)current.keyCode == 27)
		{
			flag2 = true;
			current.Use();
		}
		if ((int)current.type == 4 && ((int)current.keyCode == 13 || (int)current.keyCode == 271))
		{
			flag = true;
			current.Use();
		}
		if (signal == NativeEditKeyboard.Signal.Done)
		{
			flag = true;
		}
		if (flag2)
		{
			Close();
		}
		else if (flag)
		{
			string text = (value ?? "").Trim();
			if (text.Length != 0)
			{
				Action<string> action = done;
				Close();
				action?.Invoke(text);
			}
		}
	}
}
