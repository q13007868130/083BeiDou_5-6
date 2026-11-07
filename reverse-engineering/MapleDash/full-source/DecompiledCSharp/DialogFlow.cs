using System.Collections.Generic;
using UnityEngine;

public static class DialogFlow
{
	public sealed class Span
	{
		public DialogRun Run;

		public string Text;

		public Rect Rect;

		public Texture2D Tex;
	}

	public sealed class Layout
	{
		public readonly List<Span> Spans = new List<Span>();

		public float Height;
	}

	private sealed class Builder
	{
		private readonly Layout layout;

		private readonly float maxW;

		private readonly float margin;

		private float x;

		private float y;

		private float lineH = 16f;

		private int lineFirst;

		private float measuredBottom;

		public Builder(Layout layout, float maxW, float margin)
		{
			this.layout = layout;
			this.margin = margin;
			this.maxW = maxW - margin;
			x = (y = margin);
		}

		public void Run(List<DialogRun> runs)
		{
			for (int i = 0; i < runs.Count; i++)
			{
				if (runs[i].Kind == DialogRunKind.Icon)
				{
					AddIcon(runs[i]);
				}
				else
				{
					AddText(runs[i]);
				}
			}
			layout.Height = ((layout.Spans.Count == 0) ? 16f : (((lineFirst < layout.Spans.Count) ? (y + lineH + 2f) : measuredBottom) + margin));
		}

		private void AddIcon(DialogRun run)
		{
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			//IL_0129: Unknown result type (might be due to invalid IL or missing references)
			//IL_012e: Unknown result type (might be due to invalid IL or missing references)
			Texture2D val = (string.IsNullOrEmpty(run.Art) ? null : Art.Tex("wz://" + run.Art));
			if ((Object)(object)val == (Object)null)
			{
				if (!string.IsNullOrEmpty(run.Fallback))
				{
					AddText(new DialogRun
					{
						Kind = ((run.Link >= 0) ? DialogRunKind.Link : DialogRunKind.Text),
						Link = run.Link,
						Text = run.Fallback,
						Bold = run.Bold,
						Color = run.Color
					});
				}
				return;
			}
			float num = (run.SelectionMarker ? 18f : ((float)((Texture)val).width));
			float num2 = (run.SelectionMarker ? 16f : ((float)((Texture)val).height));
			if (x > margin && x + num > maxW)
			{
				NewLine();
			}
			GrowLine(num2);
			layout.Spans.Add(new Span
			{
				Run = run,
				Rect = new Rect(x, y + lineH - num2 + ((run.Link >= 0) ? 10f : 0f), num, num2),
				Tex = val
			});
			x += num;
		}

		private void AddText(DialogRun run)
		{
			string text = run.Text;
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			int start = 0;
			int i = 0;
			float segX = x;
			while (i < text.Length)
			{
				if (text[i] == '\n')
				{
					Flush(run, text, start, i, segX);
					NewLine();
					i++;
					start = i;
					segX = x;
					continue;
				}
				float num = CharWidth(text[i], run.Bold);
				if (x > margin && x + num > maxW)
				{
					Flush(run, text, start, i, segX);
					NewLine();
					for (; i < text.Length && text[i] == ' '; i++)
					{
					}
					start = i;
					segX = x;
				}
				else
				{
					x += num;
					i++;
				}
			}
			Flush(run, text, start, i, segX);
		}

		private void Flush(DialogRun run, string s, int start, int end, float segX)
		{
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Expected Obj, but got Unknown
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			if (end > start)
			{
				string text = s.Substring(start, end - start).TrimEnd(' ');
				if (text.Length != 0)
				{
					float num = (run.Bold ? strong : plain).CalcSize(new GUIContent(text)).x;
					layout.Spans.Add(new Span
					{
						Run = run,
						Text = text,
						Rect = new Rect(segX, y + lineH - 16f + ((run.Link >= 0) ? 10f : 0f), num, 16f)
					});
				}
			}
		}

		private void GrowLine(float h)
		{
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			if (!(h <= lineH))
			{
				float num = h - lineH;
				for (int i = lineFirst; i < layout.Spans.Count; i++)
				{
					Rect rect = layout.Spans[i].Rect;
					rect.y += num;
					layout.Spans[i].Rect = rect;
				}
				lineH = h;
			}
		}

		private void NewLine()
		{
			if (lineFirst < layout.Spans.Count)
			{
				measuredBottom = y + lineH + 2f;
			}
			y += lineH + 2f;
			x = margin;
			lineH = 16f;
			lineFirst = layout.Spans.Count;
		}
	}

	public const float PaperX = 152f;

	public const float PaperRight = 506f;

	public const float PaperY = 23f;

	public const float PaperBottom = 45f;

	private const float BarW = 15f;

	private const float BarArrow = 13f;

	private const float BarTile = 13f;

	private const float ThumbH = 25f;

	private const float LineH = 16f;

	private const int FontSize = 12;

	private const string BarRoot = "wz://UI/Basic.img/VScr4/enabled/";

	private const string DlgRoot = "wz://UI/UIWindow.img/UtilDlgEx/";

	private static GUIStyle plain;

	private static GUIStyle strong;

	private static readonly Dictionary<char, float> plainWidth = new Dictionary<char, float>();

	private static readonly Dictionary<char, float> strongWidth = new Dictionary<char, float>();

	private static bool dragging;

	private static float grabOffset;

	private static Vector2 hoverPoint;

	private static bool hoverKnown;

	public const float MaxBodyHeight = 480f;

	public static float PaperWidth(float windowWidth)
	{
		return windowWidth - 152f - 23f;
	}

	public static float ContentWidth(float windowWidth)
	{
		return 341f;
	}

	public static float PaperHeight(float windowHeight)
	{
		return windowHeight - 23f - 45f;
	}

	private static void EnsureStyles()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected Obj, but got Unknown
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected Obj, but got Unknown
		if (plain == null)
		{
			Font font = UiKit.Font;
			plain = new GUIStyle
			{
				font = font,
				fontSize = 12,
				richText = false,
				wordWrap = false,
				clipping = (TextClipping)0
			};
			plain.normal.textColor = DialogText.Ink;
			strong = new GUIStyle(plain)
			{
				fontStyle = (FontStyle)1
			};
		}
	}

	public static Layout Measure(DialogContent content, float width, bool input = false, float margin = 8f)
	{
		Layout layout = new Layout();
		EnsureStyles();
		if (content == null || width <= 1f)
		{
			return layout;
		}
		List<DialogRun> list = new List<DialogRun>(content.Runs);
		foreach (DialogSelection selection in content.Selections)
		{
			string raw = ((selection.HeaderArt != null) ? ("\n\n#fUI/UIWindow.img/UtilDlgEx/" + selection.HeaderArt + "#\n") : ("#d#L" + selection.Index + "# " + selection.Text + "#l#k\n"));
			list.AddRange(DialogText.Parse(raw).Runs);
		}
		new Builder(layout, width, input ? 2f : margin).Run(list);
		return layout;
	}

	private static float CharWidth(char ch, bool bold)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected Obj, but got Unknown
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected Obj, but got Unknown
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected Obj, but got Unknown
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		Dictionary<char, float> dictionary = (bold ? strongWidth : plainWidth);
		if (dictionary.TryGetValue(ch, out var value))
		{
			return value;
		}
		GUIStyle val = (bold ? strong : plain);
		float num = ((ch == ' ') ? Mathf.Max(1f, val.CalcSize(new GUIContent("a a")).x - val.CalcSize(new GUIContent("a")).x * 2f) : val.CalcSize(new GUIContent(ch.ToString())).x);
		return dictionary[ch] = num;
	}

	public static float ContentHeight(DialogContent content, float windowWidth, bool input = false)
	{
		return Measure(content, ContentWidth(windowWidth), input).Height;
	}

	public static List<int> Links(DialogContent content)
	{
		List<int> list = new List<int>();
		if (content == null)
		{
			return list;
		}
		foreach (DialogRun run in content.Runs)
		{
			if (run.SelectionMarker && run.Link >= 0)
			{
				list.Add(run.Link);
			}
		}
		foreach (DialogSelection selection in content.Selections)
		{
			if (selection.HeaderArt == null && selection.Index >= 0 && !list.Contains(selection.Index))
			{
				list.Add(selection.Index);
			}
		}
		return list;
	}

	public static bool NeedsScroll(float contentHeight)
	{
		return (int)(contentHeight - 480f) / 8 > 1;
	}

	public static float BodyHeight(float contentHeight)
	{
		if (!NeedsScroll(contentHeight))
		{
			return Mathf.Max(120f, contentHeight);
		}
		return 480f;
	}

	public static int DrawPane(Rect window, DialogContent content, ref float scroll, float reservedHeight, bool input, ref int selected, bool revealSelection = false)
	{
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Invalid comparison between Unknown and I4
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Invalid comparison between Unknown and I4
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_0432: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0512: Unknown result type (might be due to invalid IL or missing references)
		//IL_051c: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_054a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0555: Unknown result type (might be due to invalid IL or missing references)
		//IL_055b: Invalid comparison between Unknown and I4
		EnsureStyles();
		if (content == null)
		{
			return -1;
		}
		List<int> list = Links(content);
		ref int reference = ref selected;
		int num;
		if (list.Count != 0)
		{
			num = (list.Contains(selected) ? selected : list[0]);
		}
		else
		{
			num = -1;
		}
		reference = num;
		float num2 = ContentWidth(window.width);
		Layout layout = Measure(content, num2, input);
		float num3 = layout.Height + reservedHeight;
		float num4 = window.height - 86f;
		int num5;
		float num6;
		if (!input)
		{
			num5 = (NeedsScroll(num3) ? 1 : 0);
			if (num5 != 0)
			{
				num6 = 155f;
				goto IL_0086;
			}
		}
		else
		{
			num5 = 0;
		}
		num6 = 157f;
		goto IL_0086;
		IL_0086:
		float num7 = num6;
		float num8 = ((num5 != 0) ? 22f : (28f + (float)(int)((num4 - num3 - (input ? 20f : 6f)) / 2f)));
		Rect val = new Rect(window.x + num7 + 1f, window.y + num8 + 1f, num2 - 2f, num4 - 2f);
		float num9 = ((num5 != 0) ? Mathf.Max(0f, Mathf.Floor((num3 - num4) / 8f) * 8f) : 0f);
		scroll = Mathf.Clamp(scroll, 0f, num9);
		if (num9 > 0f)
		{
			DrawBar(val, num3, num9, ref scroll);
		}
		else
		{
			dragging = false;
		}
		Event current = Event.current;
		bool flag = val.Contains(Pointer.Pos);
		Vector2 val2 = Pointer.Pos - val.position + new Vector2(1f, scroll + 1f);
		int num10 = -1;
		int num11 = -1;
		if (((num9 > 0f) & flag) && (int)current.type == 7)
		{
			float y = Input.mouseScrollDelta.y;
			if (Mathf.Abs(y) > 0.01f)
			{
				scroll = Mathf.Clamp(scroll - Mathf.Round(y) * 16f, 0f, num9);
			}
		}
		val2 = Pointer.Pos - val.position + new Vector2(1f, scroll + 1f);
		if (flag && (int)current.type == 7)
		{
			if (!hoverKnown || Pointer.Pos != hoverPoint)
			{
				for (int i = 0; i < layout.Spans.Count; i++)
				{
					Span span = layout.Spans[i];
					if (span.Run.Link >= 0 && span.Rect.Contains(val2))
					{
						num11 = span.Run.Link;
						break;
					}
				}
				if (num11 >= 0)
				{
					selected = num11;
				}
			}
			hoverPoint = Pointer.Pos;
			hoverKnown = true;
		}
		else if (!flag)
		{
			hoverKnown = false;
		}
		if ((num9 > 0f && selected >= 0) & revealSelection)
		{
			for (int j = 0; j < layout.Spans.Count; j++)
			{
				Span span2 = layout.Spans[j];
				if (span2.Run.SelectionMarker && span2.Run.Link == selected)
				{
					if (span2.Rect.y < scroll)
					{
						scroll = Mathf.Clamp(Mathf.Floor(span2.Rect.y / 8f) * 8f, 0f, num9);
					}
					else if (span2.Rect.yMax > scroll + num4 - 2f)
					{
						scroll = Mathf.Clamp(Mathf.Floor((span2.Rect.yMax - num4 + 2f) / 8f) * 8f, 0f, num9);
					}
					break;
				}
			}
		}
		val2 = Pointer.Pos - val.position + new Vector2(1f, scroll + 1f);
		GUI.BeginClip(val);
		for (int k = 0; k < layout.Spans.Count; k++)
		{
			Span span3 = layout.Spans[k];
			Rect val3 = new Rect(span3.Rect.x - 1f, span3.Rect.y - scroll - 1f, span3.Rect.width, span3.Rect.height);
			if (val3.yMax < 0f || val3.y > val.height)
			{
				continue;
			}
			if ((Object)(object)span3.Tex != (Object)null)
			{
				if (span3.Run.SelectionMarker)
				{
					Texture2D val4 = Art.Tex(DotArt(selected, span3.Run.Link));
					if ((Object)(object)val4 != (Object)null)
					{
						GUI.DrawTexture(new Rect(val3.x + 10f, val3.y + 2f, (float)((Texture)val4).width, (float)((Texture)val4).height), (Texture)(object)val4);
					}
				}
				else
				{
					GUI.DrawTexture(val3, (Texture)(object)span3.Tex);
				}
			}
			else
			{
				GUIStyle val5 = (span3.Run.Bold ? strong : plain);
				val5.normal.textColor = span3.Run.Color;
				GUI.Label(val3, span3.Text, val5);
			}
			if (((span3.Run.Link >= 0) & flag) && span3.Rect.Contains(val2) && (int)current.type == 7 && Pointer.Clicked)
			{
				num10 = span3.Run.Link;
			}
		}
		GUI.EndClip();
		if (num10 >= 0)
		{
			Pointer.ConsumeClick();
		}
		return num10;
	}

	public static string DotArt(int selected, int link)
	{
		return "wz://UI/UIWindow.img/UtilDlgEx/" + ((link >= 0 && link == selected) ? "dot1" : "dot0");
	}

	private static void DrawBar(Rect inner, float contentH, float maxScroll, ref float scroll)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Invalid comparison between Unknown and I4
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		Rect val = new Rect(inner.x + 337f, inner.y + 1f, 15f, inner.height + 5f);
		float num = val.y + 13f;
		float num2 = val.height - 26f;
		if (num2 <= 0f)
		{
			return;
		}
		Texture2D val2 = Art.Tex("wz://UI/Basic.img/VScr4/enabled/base");
		for (float num3 = num; num3 < num + num2; num3 += 13f)
		{
			float num4 = Mathf.Min(13f, num + num2 - num3);
			if ((Object)(object)val2 != (Object)null)
			{
				GUI.DrawTextureWithTexCoords(new Rect(val.x, num3, 15f, num4), (Texture)(object)val2, new Rect(0f, 1f - num4 / 13f, 1f, num4 / 13f));
			}
		}
		Rect val3 = new Rect(val.x, val.y, 15f, 13f);
		Rect val4 = new Rect(val.x, val.yMax - 13f, 15f, 13f);
		GUI.DrawTexture(val3, (Texture)(object)Art.Tex("wz://UI/Basic.img/VScr4/enabled/prev0"));
		GUI.DrawTexture(val4, (Texture)(object)Art.Tex("wz://UI/Basic.img/VScr4/enabled/next0"));
		float num5 = Mathf.Min(25f, num2);
		float num6 = Mathf.Max(1f, num2 - num5);
		Rect val5 = new Rect(val.x, num + num6 * (scroll / maxScroll), 15f, num5);
		bool flag = (int)Event.current.type == 7 && Pointer.Pressed;
		if (GUI.enabled & flag)
		{
			if (val3.Contains(Pointer.Pos))
			{
				scroll = Mathf.Clamp(scroll - 8f, 0f, maxScroll);
				Pointer.ConsumeClick();
			}
			else if (val4.Contains(Pointer.Pos))
			{
				scroll = Mathf.Clamp(scroll + 8f, 0f, maxScroll);
				Pointer.ConsumeClick();
			}
			else if (val5.Contains(Pointer.Pos))
			{
				dragging = true;
				grabOffset = Pointer.Pos.y - val5.y;
				Pointer.ConsumeClick();
			}
			else if (val.Contains(Pointer.Pos))
			{
				scroll = Mathf.Clamp(scroll + ((Pointer.Pos.y < val5.y) ? (-1f) : 1f) * Mathf.Max(16f, Mathf.Floor((maxScroll / 8f + 1f) / 16f)) * 8f, 0f, maxScroll);
				Pointer.ConsumeClick();
			}
		}
		if (dragging)
		{
			if (Pointer.Down)
			{
				scroll = Mathf.Clamp(Mathf.Round((Pointer.Pos.y - grabOffset - num) / num6 * maxScroll / 8f) * 8f, 0f, maxScroll);
			}
			else
			{
				dragging = false;
			}
		}
		val5.y = num + num6 * (scroll / maxScroll);
		GUI.DrawTexture(val5, (Texture)(object)Art.Tex("wz://UI/Basic.img/VScr4/enabled/thumb0"));
	}
}
