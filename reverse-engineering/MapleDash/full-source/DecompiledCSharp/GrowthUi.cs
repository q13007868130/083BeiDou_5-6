using System;
using System.Collections.Generic;
using System.Globalization;
using MapleDash.Wz;
using UnityEngine;

internal static class GrowthUi
{
	public static float S = 1f;

	public static Rect R;

	public static readonly Color Ink = new Color(0.95f, 0.95f, 0.95f, 1f);

	private static readonly Dictionary<long, GUIStyle> styles = new Dictionary<long, GUIStyle>(32);

	private static Font cachedFont;

	private static object dragOwner;

	private static int dragFinger = -1;

	private static object slopOwner;

	private static Vector2 slopFrom;

	public static bool Repaint
	{
		get
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Invalid comparison between Unknown and I4
			return (int)Event.current.type == 7;
		}
	}

	public static Rect At(float x, float y, float w, float h)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(R.x + x * S, R.y + y * S, w * S, h * S);
	}

	public static void Image(string root, string rel, float x, float y)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		if (rel == "backgrnd" && MatrixPresentation.Active && GUI.enabled)
		{
			MatrixPresentation.Regions(root);
		}
		ImgNode imgNode = WzAssets.Node(root + rel);
		if (imgNode != null)
		{
			Texture2D val = WzAssets.Texture(imgNode);
			if (!((Object)(object)val == (Object)null))
			{
				GUI.DrawTexture(At(x, y, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
			}
		}
	}

	public static Rect Shift(float x, float y, float w, float h)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		Rect r = R;
		R = new Rect(r.x + x * S, r.y + y * S, w * S, h * S);
		return r;
	}

	public static void Restore(Rect previous)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		R = previous;
	}

	public static void TextWrapped(string root, string text, float x, float y, float w, float h, string font, Nullable tint = null)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		GUIStyle val = Style(root, font, (TextAnchor)0, h, tint);
		bool wordWrap = val.wordWrap;
		val.wordWrap = true;
		GUI.Label(At(x, y, w, h), text, val);
		val.wordWrap = wordWrap;
	}

	public static void ImageBottom(string root, string rel, float x, float bottom)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = WzAssets.Node(root + rel);
		if (imgNode != null)
		{
			Texture2D val = WzAssets.Texture(imgNode);
			if (!((Object)(object)val == (Object)null))
			{
				GUI.DrawTexture(At(x, bottom - (float)((Texture)val).height, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
			}
		}
	}

	public static bool Button(string root, string rel)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = WzAssets.Node(root + rel + "/normal/0");
		if (imgNode == null)
		{
			return false;
		}
		Texture2D val = WzAssets.Texture(imgNode);
		if ((Object)(object)val == (Object)null)
		{
			return false;
		}
		Vector2 val2 = WzAssets.Origin(imgNode);
		return ButtonAt(root, rel, At(0f - val2.x, 0f - val2.y, ((Texture)val).width, ((Texture)val).height));
	}

	public static bool ButtonAt(string root, string rel, Rect rect)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		bool flag = rect.Contains(Pointer.Pos);
		if (flag && MatrixPresentation.Active)
		{
			ImgNode imgNode = WzAssets.Node(root + rel);
			string value = ((!GUI.enabled) ? imgNode?.Text("ToolTip_Disabled") : imgNode?.Text("ToolTip"));
			if (string.IsNullOrEmpty(value))
			{
				value = imgNode?.Text("toolTipDisabled");
			}
			if (string.IsNullOrEmpty(value))
			{
				value = imgNode?.Text("ToolTip");
			}
			MatrixPresentation.Hover(value);
		}
		bool flag2 = GUI.enabled && (!NativeUtilPrompt.Visible || NativeUtilPrompt.Drawing);
		string text;
		if (flag2)
		{
			if (flag)
			{
				text = (Pointer.Down ? "pressed" : "mouseOver");
			}
			else
			{
				text = "normal";
			}
		}
		else
		{
			text = "disabled";
		}
		Texture2D val = WzAssets.Texture(WzAssets.Node(root + rel + "/" + text + "/0")) ?? WzAssets.Texture(WzAssets.Node(root + rel + "/normal/0"));
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(rect, (Texture)(object)val);
		}
		if (!flag2 || !Repaint || !Pointer.Clicked || !flag)
		{
			return false;
		}
		Pointer.ConsumeClick();
		AudioManager.Play("UI/BtMouseClick", 0.7f, pitchVary: false);
		return true;
	}

	public static void Text(string root, string text, float x, float y, float w, string font, TextAnchor align = (TextAnchor)0, float height = 18f, Nullable tint = null)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		GUIStyle val = Style(root, font, align, height, tint);
		GUI.Label(At(x, y, w, height), text, val);
	}

	public static void ShadowText(string root, string text, float x, float y, float w, string font, TextAnchor align = (TextAnchor)0, float height = 18f, Nullable tint = null)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		GUIStyle val = Style(root, font, align, height, new Color(0f, 0f, 0f, 0.85f));
		GUI.Label(At(x + 1f, y + 1f, w, height), text, val);
		Text(root, text, x, y, w, font, align, height, tint);
	}

	private static GUIStyle Style(string root, string font, TextAnchor align, float height, Nullable tint)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Expected Obj, but got Unknown
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = (string.IsNullOrEmpty(font) ? null : WzAssets.Node(root + font));
		int num = Mathf.Max(1, Mathf.RoundToInt((float)Mathf.Max(12, imgNode?.Int("fontSize", 12) ?? 12) * S));
		Color val = tint ?? Ink;
		string text = imgNode?.Text("fontColor");
		if (!tint.HasValue && !string.IsNullOrEmpty(text) && uint.TryParse(text, NumberStyles.HexNumber, null, out var result))
		{
			val = Color32.op_Implicit(new Color32((byte)(result >> 16), (byte)(result >> 8), (byte)result, (byte)(result >> 24)));
		}
		if (cachedFont != UiKit.Font)
		{
			styles.Clear();
			cachedFont = UiKit.Font;
		}
		long key = ((long)num << 48) ^ ((long)align << 40) ^ ((long)(int)(val.r * 255f) << 32) ^ ((long)(int)(val.g * 255f) << 24) ^ ((long)(int)(val.b * 255f) << 16) ^ (int)(val.a * 255f);
		if (styles.TryGetValue(key, out var value))
		{
			return value;
		}
		GUIStyle val2 = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = num,
			fontStyle = (FontStyle)1,
			alignment = align,
			wordWrap = false,
			clipping = (TextClipping)0
		};
		val2.normal.textColor = val;
		styles[key] = val2;
		return val2;
	}

	public static float Digits(string root, string folder, int value, float x, float y)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		string text = Mathf.Abs(value).ToString(CultureInfo.InvariantCulture);
		for (int i = 0; i < text.Length; i++)
		{
			ImgNode imgNode = WzAssets.Node(root + folder + "/" + text[i]);
			if (imgNode != null)
			{
				Texture2D val = WzAssets.Texture(imgNode);
				if (!((Object)(object)val == (Object)null))
				{
					GUI.DrawTexture(At(x, y, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
					x += (float)((Texture)val).width;
				}
			}
		}
		return x;
	}

	public static void Veil(float x, float y, float w, float h, Color color)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		Color color2 = GUI.color;
		GUI.color = color;
		GUI.DrawTexture(At(x, y, w, h), (Texture)(object)Texture2D.whiteTexture);
		GUI.color = color2;
	}

	public static void Plate(float x, float y, float w, float h, float alpha = 0.55f)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		Veil(x, y, w, h, new Color(0f, 0f, 0f, alpha));
	}

	public static bool Drag(ref bool dragging, ref bool positioned, ref Vector2 position, ref Vector2 grab, float width, float height, Func<Rect> window, float handle = 28f, float rightInset = 30f, object owner = null, float slop = -1f)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Invalid comparison between Unknown and I4
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Invalid comparison between Unknown and I4
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		if (!Repaint || !GUI.enabled || Pointer.Suppressed)
		{
			return false;
		}
		Rect val = At(0f, 0f, width - rightInset, handle);
		Vector2 size = new Vector2(width, height);
		Rect val2;
		if (Input.touchCount > 0)
		{
			for (int i = 0; i < Input.touchCount; i++)
			{
				Touch touch = Input.GetTouch(i);
				if ((int)touch.phase == 0 && val.Contains(TouchPoint(touch.position)) && !VirtualControls.HitsAtScreen(touch.position))
				{
					dragOwner = owner;
					dragFinger = touch.fingerId;
					dragging = true;
					if (!positioned)
					{
						val2 = window();
						position = val2.position / S;
						positioned = true;
					}
				}
			}
			if (owner != null && dragOwner == owner && dragFinger >= 0)
			{
				bool flag = false;
				for (int j = 0; j < Input.touchCount; j++)
				{
					Touch touch2 = Input.GetTouch(j);
					if (touch2.fingerId != dragFinger)
					{
						continue;
					}
					if ((int)touch2.phase != 3 && (int)touch2.phase != 4)
					{
						flag = true;
						if ((int)touch2.phase != 0)
						{
							position = NativeUi.ClampWindowPosition(position + new Vector2(touch2.deltaPosition.x, 0f - touch2.deltaPosition.y) / Mathf.Max(0.0001f, UiKit.Scale * S), size, S);
						}
					}
					break;
				}
				if (!flag)
				{
					dragOwner = null;
					dragFinger = -1;
					dragging = false;
					Pointer.ConsumeClick();
				}
				return true;
			}
		}
		else
		{
			dragOwner = null;
			dragFinger = -1;
		}
		if (!dragging && (Pointer.Pressed || Pointer.Down) && val.Contains(Pointer.Pressed ? Pointer.Pos : Pointer.PressPosition))
		{
			dragging = (positioned = true);
			val2 = window();
			position = val2.position / S;
			grab = Pointer.Pos / S - position;
			Pointer.ConsumeClick();
			return true;
		}
		if (!dragging && slop > 0f && BodyDrag(ref dragging, ref positioned, ref position, ref grab, size, window, owner, slop))
		{
			return true;
		}
		if (!dragging)
		{
			return false;
		}
		if (Pointer.Down)
		{
			position = NativeUi.ClampWindowPosition(Pointer.Pos / S - grab, size, S);
		}
		else
		{
			dragging = false;
			Pointer.ConsumeClick();
		}
		return true;
	}

	private static bool BodyDrag(ref bool dragging, ref bool positioned, ref Vector2 position, ref Vector2 grab, Vector2 size, Func<Rect> window, object owner, float slop)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		Rect val;
		if (Pointer.Pressed)
		{
			val = window();
			if (val.Contains(Pointer.Pos))
			{
				slopOwner = owner;
				slopFrom = Pointer.Pos;
			}
		}
		if (!Pointer.Down)
		{
			slopOwner = null;
			return false;
		}
		if (slopOwner == owner)
		{
			Vector2 val2 = Pointer.Pos - slopFrom;
			if (!(val2.sqrMagnitude <= slop * slop))
			{
				dragging = (positioned = true);
				val = window();
				position = val.position / S;
				grab = Pointer.Pos / S - position;
				slopOwner = null;
				Pointer.ConsumeClick();
				return true;
			}
		}
		return false;
	}

	private static Vector2 TouchPoint(Vector2 screen)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2(screen.x / UiKit.Scale, ((float)Screen.height - screen.y) / UiKit.Scale);
	}

	static GrowthUi()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
	}
}
