using System;
using System.Collections.Generic;
using UnityEngine;

public static class NativeUtilPrompt
{
	private const string OwnerKey = "NativeUtilPrompt";

	private static string text;

	private static string input;

	private static bool notice;

	private static long minimum;

	private static long maximum;

	private static int textMin;

	private static int textMax;

	private static Action<long> number;

	private static Action<bool> answer;

	private static Action<string> entered;

	private static bool numberSelectPending;

	private const string NumberFieldName = "NativeUtilPromptNumber";

	public static bool Visible
	{
		get
		{
			if (number == null && answer == null)
			{
				return entered != null;
			}
			return true;
		}
	}

	public static bool Drawing { get; private set; }

	public static void Number(string prompt, int value, int min, int max, Action<int> done)
	{
		NumberLong(prompt, value, min, max, (long v) =>
		{
			done?.Invoke((int)Math.Max(-2147483648L, Math.Min(2147483647L, v)));
		});
	}

	public static void NumberLong(string prompt, long value, long min, long max, Action<long> done)
	{
		Clear();
		text = prompt;
		input = value.ToString();
		minimum = min;
		maximum = max;
		number = done;
		numberSelectPending = true;
	}

	public static void Confirm(string prompt, Action<bool> done)
	{
		Clear();
		text = prompt;
		answer = done;
	}

	public static void Pages(string title, string[] pages, int index = 0)
	{
		if (pages == null || index >= pages.Length)
		{
			return;
		}
		Clear();
		text = title + " (" + (index + 1) + "/" + pages.Length + ")\n\n" + pages[index];
		notice = true;
		answer = (bool next) =>
		{
			if (next)
			{
				Pages(title, pages, index + 1);
			}
		};
	}

	public static void Notice(string prompt)
	{
		Clear();
		text = prompt;
		notice = true;
		answer = (bool _) =>
		{
		};
	}

	public static void Text(string prompt, string value, int min, int max, Action<string> done)
	{
		Clear();
		text = prompt;
		input = value ?? "";
		textMin = min;
		textMax = max;
		entered = done;
	}

	public static void Clear()
	{
		number = null;
		answer = null;
		entered = null;
		notice = false;
		text = (input = null);
		numberSelectPending = false;
		NativeEditKeyboard.Close();
	}

	public static void Cancel()
	{
		Action<bool> action = answer;
		Clear();
		action?.Invoke(obj: false);
	}

	private static List<string> WrapConfirmation(string value, GUIStyle style)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Expected Obj, but got Unknown
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		List<string> list = new List<string>();
		string[] array = (value ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		foreach (string obj in array)
		{
			string text = "";
			string text2 = obj;
			for (int j = 0; j < text2.Length; j++)
			{
				char c = text2[j];
				if (text.Length > 0 && style.CalcSize(new GUIContent(text + c)).x > 200f)
				{
					list.Add(text);
					text = "";
				}
				text += c;
			}
			list.Add(text);
		}
		return list;
	}

	public static void Draw()
	{
		//IL_07ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_07df: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e5: Invalid comparison between Unknown and I4
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected Obj, but got Unknown
		//IL_007e: Expected Obj, but got Unknown
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_0276: Unknown result type (might be due to invalid IL or missing references)
		//IL_0413: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0661: Unknown result type (might be due to invalid IL or missing references)
		//IL_066b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0464: Unknown result type (might be due to invalid IL or missing references)
		//IL_045d: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ce: Invalid comparison between Unknown and I4
		//IL_06a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f4: Invalid comparison between Unknown and I4
		//IL_06d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d9: Invalid comparison between Unknown and I4
		//IL_06f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ff: Invalid comparison between Unknown and I4
		//IL_0703: Unknown result type (might be due to invalid IL or missing references)
		//IL_070d: Invalid comparison between Unknown and I4
		//IL_055f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0565: Invalid comparison between Unknown and I4
		//IL_0502: Unknown result type (might be due to invalid IL or missing references)
		//IL_0508: Invalid comparison between Unknown and I4
		//IL_05d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_05db: Unknown result type (might be due to invalid IL or missing references)
		//IL_060b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0615: Unknown result type (might be due to invalid IL or missing references)
		//IL_0577: Unknown result type (might be due to invalid IL or missing references)
		//IL_0542: Unknown result type (might be due to invalid IL or missing references)
		//IL_0549: Expected Obj, but got Unknown
		if (!Visible)
		{
			return;
		}
		NativeEditKeyboard.Signal signal = NativeEditKeyboard.Sync("NativeUtilPrompt", ref input);
		Drawing = true;
		float s = BottomBar.S;
		Matrix4x4 matrix = GUI.matrix;
		Vector2 val = Pointer.Pos / s;
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(s, s, 1f));
		try
		{
			GUIStyle val2 = new GUIStyle
			{
				font = UiKit.Font,
				fontSize = 12,
				wordWrap = true,
				padding = new RectOffset()
			};
			val2.normal.textColor = Color.white;
			bool flag = number != null;
			bool flag2 = flag || entered != null;
			DialogFlow.Layout layout = (flag2 ? DialogFlow.Measure(DialogText.Parse("#w" + NativeUtilPrompt.text), 225f, input: true) : null);
			List<string> list = (flag2 ? null : WrapConfirmation(NativeUtilPrompt.text, val2));
			float num = (flag2 ? layout.Height : ((float)(list.Count * 14)));
			int num2 = Mathf.Max(2, Mathf.FloorToInt(num / 20f) + 1);
			float num3 = (flag2 ? (num + 99f) : ((float)(76 + num2 * 20)));
			float x = (UiKit.W / s - 266f) / 2f;
			float y = (UiKit.H / s - num3) / 2f;
			string text = ((flag2 || notice) ? "Notice3" : "YesNo3");
			Image(text + "/t", 0f, 21f);
			float num4 = (flag2 ? 78 : 55);
			for (float num5 = 21f; num5 < num3 - num4; num5 += 20f)
			{
				Image(text + "/c", num5, 20f);
			}
			Image(flag2 ? "Notice4/s" : (text + "/s"), num3 - num4, num4);
			if (flag2)
			{
				foreach (DialogFlow.Span span in layout.Spans)
				{
					Rect val3 = new Rect(x + 20f + span.Rect.x, y + 22f + span.Rect.y, span.Rect.width, span.Rect.height);
					if ((Object)(object)span.Tex != (Object)null)
					{
						GUI.DrawTexture(val3, (Texture)(object)span.Tex);
						continue;
					}
					val2.normal.textColor = span.Run.Color;
					val2.fontStyle = (FontStyle)(span.Run.Bold ? 1 : 0);
					GUI.Label(val3, span.Text, val2);
				}
			}
			else
			{
				val2.wordWrap = false;
				val2.alignment = (TextAnchor)1;
				float num6 = y + 24f + Mathf.Floor(((float)(num2 * 20) - num) / 2f);
				for (int i = 0; i < list.Count; i++)
				{
					GUI.Label(new Rect(x + 15f, num6 + (float)(i * 14), 234f, 14f), list[i], val2);
				}
			}
			bool flag3 = false;
			if (flag2)
			{
				Rect val4 = new Rect(x + 19f, y + num3 - 73f, 226f, 15f);
				if (!flag)
				{
					UiKit.Fill(val4, Color.black);
					UiKit.Fill(new Rect(val4.x + 1f, val4.y + 1f, val4.width - 2f, val4.height - 2f), Color.white);
				}
				val2.wordWrap = false;
				val2.normal.textColor = Color.black;
				val2.fontStyle = (FontStyle)0;
				_003F val5 = (flag ? val4 : new Rect(val4.x + 2f, val4.y + 2f, val4.width - 4f, val4.height - 4f));
				if (numberSelectPending & flag)
				{
					GUI.SetNextControlName("NativeUtilPromptNumber");
				}
				string text2 = GUI.TextField((Rect)val5, input, flag ? 19 : textMax, val2);
				if (flag)
				{
					input = "";
					string text3 = text2;
					for (int j = 0; j < text3.Length; j++)
					{
						char c = text3[j];
						if (c >= '0' && c <= '9')
						{
							input += c;
						}
					}
				}
				else
				{
					input = text2;
				}
				if ((numberSelectPending & flag) && (int)Event.current.type == 7)
				{
					if (GUIUtility.keyboardControl == 0 || GUI.GetNameOfFocusedControl() != "NativeUtilPromptNumber")
					{
						GUI.FocusControl("NativeUtilPromptNumber");
					}
					else
					{
						TextEditor val6 = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
						if (val6 != null)
						{
							val6.SelectAll();
							numberSelectPending = false;
						}
					}
				}
				if ((int)Event.current.type == 7 && GUI.enabled && Pointer.Clicked && val4.Contains(val))
				{
					Pointer.ConsumeClick();
					NativeEditKeyboard.Open("NativeUtilPrompt", input, (TouchScreenKeyboardType)(flag ? 4 : 0), flag ? 19 : textMax, flag);
				}
				flag3 = NativeUi.Button(new Rect(x + 158f, y + num3 - 30f, 47f, 19f), "UI/Basic.img/BtOK2", val);
				if (NativeUi.Button(new Rect(x + 208f, y + num3 - 30f, 47f, 19f), "UI/Basic.img/BtCancel2", val))
				{
					Clear();
					return;
				}
			}
			else
			{
				flag3 = NativeUi.Button(new Rect(x + (float)(notice ? 205 : 155), y + num3 - 30f, 47f, 19f), "UI/Basic.img/BtOK2", val);
				if (!notice && NativeUi.Button(new Rect(x + 205f, y + num3 - 30f, 47f, 19f), "UI/Basic.img/BtCancel2", val))
				{
					Cancel();
					return;
				}
			}
			Event current2 = Event.current;
			if ((int)current2.type == 4 && (int)current2.keyCode == 27)
			{
				Cancel();
				current2.Use();
				return;
			}
			if ((int)current2.type == 4 && ((int)current2.keyCode == 13 || (int)current2.keyCode == 271))
			{
				flag3 = true;
				current2.Use();
			}
			if (signal == NativeEditKeyboard.Signal.Done)
			{
				flag3 = true;
			}
			if (!flag3)
			{
				return;
			}
			if (flag)
			{
				if (long.TryParse(input, out var result) && result >= minimum && result <= maximum)
				{
					Action<long> action = number;
					Clear();
					action?.Invoke(result);
				}
			}
			else if (entered != null)
			{
				if (input.Length >= textMin && input.Length <= textMax)
				{
					string obj = input;
					Action<string> action2 = entered;
					Clear();
					action2(obj);
				}
			}
			else
			{
				Action<bool> action3 = answer;
				Clear();
				action3?.Invoke(obj: true);
			}
			void Image(string path, float dy, float h)
			{
				//IL_002e: Unknown result type (might be due to invalid IL or missing references)
				Texture2D val7 = Art.Tex("wz://UI/Basic.img/" + path);
				if ((Object)(object)val7 != (Object)null)
				{
					GUI.DrawTexture(new Rect(x, y + dy, 266f, h), (Texture)(object)val7);
				}
			}
		}
		finally
		{
			GUI.matrix = matrix;
			Drawing = false;
			if ((int)Event.current.type == 7)
			{
				Pointer.ConsumeClick();
				Pointer.Pressed = (Pointer.RightPressed = (Pointer.RightReleased = false));
			}
		}
	}
}
