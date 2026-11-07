using UnityEngine;

public static class QuestUi
{
	private static Vector2 origin;

	private static GUIStyle style;

	private static GUIStyle measureStyle;

	private static int dragging;

	private static float grab;

	public static float Scale { get; private set; }

	public static void Begin(Vector2 position, float scale)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		origin = position;
		Scale = scale;
	}

	public static Rect Rect(float x, float y, float w, float h)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(origin.x + x * Scale, origin.y + y * Scale, w * Scale, h * Scale);
	}

	public static void Image(string node, float x, float y)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://UI/UIWindow.img/" + node);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(Rect(x, y, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
		}
	}

	public static bool Button(string path, float x, float y)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://" + path + "/normal/0");
		if ((Object)(object)val != (Object)null)
		{
			return NativeUi.Button(Rect(x, y, ((Texture)val).width, ((Texture)val).height), path);
		}
		return false;
	}

	public static bool Click(Rect rect)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		int num;
		if ((int)Event.current.type == 7 && Pointer.Clicked)
		{
			num = (rect.Contains(Pointer.Pos) ? 1 : 0);
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

	public static void Fill(Rect rect, Color color)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		UiKit.Fill(Rect(rect.x, rect.y, rect.width, rect.height), color);
	}

	public static void Label(Rect rect, string text, Color color)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		LocalLabel(Rect(rect.x, rect.y, rect.width, rect.height), text, color);
	}

	public static string Fit(string text, float width)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected Obj, but got Unknown
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected Obj, but got Unknown
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected Obj, but got Unknown
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Expected Obj, but got Unknown
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		if (measureStyle == null)
		{
			measureStyle = new GUIStyle
			{
				font = UiKit.Font,
				fontSize = 12
			};
		}
		if (measureStyle.CalcSize(new GUIContent(text)).x <= width)
		{
			return text;
		}
		float num = width - measureStyle.CalcSize(new GUIContent("..")).x;
		int num2 = text.Length;
		while (num2 > 0 && measureStyle.CalcSize(new GUIContent(text.Substring(0, num2))).x > num)
		{
			num2--;
		}
		return text.Substring(0, num2) + "..";
	}

	public static void LocalLabel(Rect rect, string text, Color color, bool bold = false)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected Obj, but got Unknown
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		if (style == null)
		{
			style = new GUIStyle
			{
				font = UiKit.Font,
				clipping = (TextClipping)1
			};
		}
		style.fontSize = Mathf.RoundToInt(12f * Scale);
		style.fontStyle = (FontStyle)(bold ? 1 : 0);
		style.normal.textColor = color;
		GUI.Label(rect, text, style);
	}

	public static float Scroll(Rect bar, Rect view, float value, float maximum, float step)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Invalid comparison between Unknown and I4
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		value = Mathf.Clamp(value, 0f, maximum);
		bool flag = maximum > 0f;
		string text = (flag ? "enabled/" : "disabled/");
		Rect val = new Rect(bar.x, bar.y + 13f, 15f, bar.height - 26f);
		for (float num = val.y; num < val.yMax; num += 13f)
		{
			Texture2D val2 = Art.Tex("wz://UI/Basic.img/VScr4/" + text + "base");
			float num2 = Mathf.Min(13f, val.yMax - num);
			if ((Object)(object)val2 != (Object)null)
			{
				GUI.DrawTextureWithTexCoords(Rect(bar.x, num, 15f, num2), (Texture)(object)val2, new Rect(0f, 1f - num2 / 13f, 1f, num2 / 13f));
			}
		}
		GUI.DrawTexture(Rect(bar.x, bar.y, 15f, 13f), (Texture)(object)Art.Tex("wz://UI/Basic.img/VScr4/" + text + (flag ? "prev0" : "prev")));
		GUI.DrawTexture(Rect(bar.x, bar.yMax - 13f, 15f, 13f), (Texture)(object)Art.Tex("wz://UI/Basic.img/VScr4/" + text + (flag ? "next0" : "next")));
		if (!flag)
		{
			return 0f;
		}
		float num3 = Mathf.Max(1f, val.height - 25f);
		Rect val3 = Rect(bar.x, val.y + num3 * value / maximum, 15f, 25f);
		int controlID = GUIUtility.GetControlID((FocusType)2);
		if ((int)Event.current.type == 7)
		{
			Rect val4 = Rect(view.x, view.y, view.width + 15f, view.height);
			if (val4.Contains(Pointer.Pos))
			{
				value -= Input.mouseScrollDelta.y * step * 2f;
			}
			if (Pointer.Pressed)
			{
				if (val3.Contains(Pointer.Pos))
				{
					dragging = controlID;
					grab = Pointer.Pos.y - val3.y;
					Pointer.ConsumeClick();
				}
				else
				{
					val4 = Rect(bar.x, bar.y, 15f, 13f);
					if (val4.Contains(Pointer.Pos))
					{
						value -= step;
						Pointer.ConsumeClick();
					}
					else
					{
						val4 = Rect(bar.x, bar.yMax - 13f, 15f, 13f);
						if (val4.Contains(Pointer.Pos))
						{
							value += step;
							Pointer.ConsumeClick();
						}
					}
				}
			}
			if (dragging == controlID)
			{
				if (Pointer.Down)
				{
					value = (Pointer.Pos.y - grab - origin.y - val.y * Scale) / (num3 * Scale) * maximum;
				}
				else
				{
					dragging = 0;
				}
			}
		}
		value = Mathf.Clamp(Mathf.Round(value / step) * step, 0f, maximum);
		GUI.DrawTexture(Rect(bar.x, val.y + num3 * value / maximum, 15f, 25f), (Texture)(object)Art.Tex("wz://UI/Basic.img/VScr4/enabled/thumb0"));
		return value;
	}
}
