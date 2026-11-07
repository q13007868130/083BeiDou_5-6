using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class StatusBar
{
	private const int PanelW = 570;

	private const int PanelH = 35;

	private const int GaugeX = 216;

	private const int GaugeY = 2;

	private const int LabelH = 13;

	private const int BarH = 18;

	private const int LevelX = 42;

	private const int LevelY = 11;

	private const int NameX = 88;

	private static readonly int[] GaugeW = new int[3] { 109, 108, 123 };

	private static readonly float[] LabelTextW = new float[3] { 18f, 20f, 29f };

	private static readonly string[] GaugeName = new string[3] { "hp", "mp", "exp" };

	private static readonly string[] LabelRel = new string[3] { "ui/statusbar/gauge_hp_label.bytes", "ui/statusbar/gauge_mp_label.bytes", "ui/statusbar/gauge_exp_label.bytes" };

	private static readonly string[] FillRel = new string[3] { "ui/statusbar/gauge_hp_fill.bytes", "ui/statusbar/gauge_mp_fill.bytes", "ui/statusbar/gauge_exp_fill.bytes" };

	private static readonly string[] FrameRel = new string[3] { "ui/statusbar/gauge_hp_frame.bytes", "ui/statusbar/gauge_mp_frame.bytes", "ui/statusbar/gauge_exp_frame.bytes" };

	private static bool loaded;

	private static Texture2D panel;

	private static Texture2D gray;

	private static Texture2D digitSlash;

	private static Texture2D digitPercent;

	private static Texture2D digitLb;

	private static Texture2D digitRb;

	private static Texture2D[] labels = new Texture2D[3];

	private static Texture2D[] fills = new Texture2D[3];

	private static Texture2D[] frames = new Texture2D[3];

	private static Texture2D[] digits = new Texture2D[10];

	private static Texture2D[] levelNo = new Texture2D[10];

	private static Font font;

	private static bool fontTried;

	private static readonly GUIStyle textStyle = new GUIStyle();

	private const float FlashFrame = 0.13f;

	private const int FlashFrames = 5;

	private static float hpFlashAt = -99f;

	private static float mpFlashAt = -99f;

	private static float lastHp;

	private static float lastMp;

	private static bool gaugeInit;

	private const float GaugeDuration = 0.7f;

	private static readonly float[] gaugeFrom = new float[3];

	private static readonly float[] gaugeTo = new float[3];

	private static readonly float[] gaugeAt = new float[3];

	public static float S { get; private set; } = 1f;

	public static Rect Panel
	{
		[CompilerGenerated]
		get
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			return field;
		}
		[CompilerGenerated]
		private set
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			field = value;
		}
	}

	public static float ComputeScale()
	{
		return BottomBar.S;
	}

	public static void Draw()
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		Load();
		if (!((Object)(object)panel == (Object)null))
		{
			S = ComputeScale();
			float num = 570f * S;
			float num2 = 35f * S;
			Panel = new Rect(2f * S, UiKit.H - num2, num, num2);
			GUI.DrawTexture(Panel, (Texture)(object)panel, (ScaleMode)0);
			DrawLevelTag();
			DrawIdentity();
			DrawGauges();
		}
	}

	private static void DrawLevelTag()
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		string text = Mathf.Max(1, Profile.level).ToString();
		float num = 42f;
		float oy = 11f;
		for (int i = 0; i < text.Length; i++)
		{
			Texture2D val = Digit(levelNo, text[i]);
			if (!((Object)(object)val == (Object)null))
			{
				GUI.DrawTexture(At(num + (float)i * 11f, oy, 11f, 13f), (Texture)(object)val, (ScaleMode)0);
			}
		}
	}

	private static void DrawIdentity()
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		string value = (string.IsNullOrEmpty(Profile.playerName) ? "枫叶旅人" : Profile.playerName);
		string value2 = JobDefs.NameOf(Profile.JobId);
		GUIStyle style = Style(12, Color32.op_Implicit(new Color32((byte)192, (byte)198, (byte)200, byte.MaxValue)), (TextAnchor)0, (FontStyle)0);
		Shadow(At(88f, 3f, 116f, 15f), value2, style);
		Shadow(At(88f, 17f, 116f, 15f), value, style);
	}

	private static void DrawGauges()
	{
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		float num = (((Object)(object)playerCtrl != (Object)null) ? playerCtrl.hp : ((float)Profile.Attributes.maxHp));
		float num2 = (((Object)(object)playerCtrl != (Object)null) ? Mathf.Max(1f, playerCtrl.hpMax) : ((float)Profile.Attributes.maxHp));
		float num3 = (((Object)(object)playerCtrl != (Object)null) ? playerCtrl.mp : ((float)Profile.Attributes.maxMp));
		float num4 = (((Object)(object)playerCtrl != (Object)null) ? Mathf.Max(1f, playerCtrl.mpMax) : ((float)Profile.Attributes.maxMp));
		float[] array = new float[3]
		{
			Mathf.Clamp01(num / num2),
			Mathf.Clamp01(num3 / num4),
			Profile.ExpRatio
		};
		TrackGauges(array, num, num2, num3, num4);
		float num5 = 216f;
		for (int i = 0; i < 3; i++)
		{
			int num6 = GaugeW[i];
			if ((Object)(object)labels[i] != (Object)null)
			{
				GUI.DrawTexture(At(num5, 2f, num6, 13f), (Texture)(object)labels[i], (ScaleMode)0);
			}
			float num7 = 15f;
			if ((Object)(object)gray != (Object)null)
			{
				GUI.DrawTexture(At(num5, num7 + 1f, num6, 16f), (Texture)(object)gray, (ScaleMode)0);
			}
			DrawClippedFill(At(num5, num7, (float)num6 * GaugeRatio(i, Time.unscaledTime), 18f), fills[i], num6);
			if ((Object)(object)frames[i] != (Object)null)
			{
				GUI.DrawTexture(At(num5, num7, num6, 18f), (Texture)(object)frames[i], (ScaleMode)0);
			}
			if (i < 2)
			{
				DrawFlash(i, num5, num7);
			}
			if (i == 2)
			{
				DrawTemporaryExp(num5, num7);
			}
			string text = i switch
			{
				1 => Number(num3, num4), 
				0 => Number(num, num2), 
				_ => Mathf.FloorToInt(array[2] * 100f) + "%", 
			};
			Rect val = Panel;
			float leftX = val.x + (num5 + LabelTextW[i] + 1f) * S;
			val = Panel;
			DrawNumber(leftX, val.y + 3f * S, text);
			num5 += (float)num6;
		}
	}

	private static float GaugeRatio(int index, float now)
	{
		return Mathf.Lerp(gaugeFrom[index], gaugeTo[index], Mathf.Clamp01((now - gaugeAt[index]) / 0.7f));
	}

	private static void TrackGauges(float[] ratio, float hp, float hpMax, float mp, float mpMax)
	{
		float unscaledTime = Time.unscaledTime;
		if (!gaugeInit)
		{
			gaugeInit = true;
			lastHp = WarningValue(hp, hpMax, ClientSettings.HpAlarm);
			lastMp = WarningValue(mp, mpMax, ClientSettings.MpAlarm);
			for (int i = 0; i < 3; i++)
			{
				gaugeFrom[i] = (gaugeTo[i] = ratio[i]);
			}
			return;
		}
		for (int j = 0; j < 2; j++)
		{
			if (ratio[j] != gaugeTo[j])
			{
				gaugeFrom[j] = GaugeRatio(j, unscaledTime);
				gaugeTo[j] = ratio[j];
				gaugeAt[j] = unscaledTime;
			}
		}
		gaugeFrom[2] = (gaugeTo[2] = ratio[2]);
		if (AdvanceWarning(hp, hpMax, ClientSettings.HpAlarm, ref lastHp))
		{
			hpFlashAt = unscaledTime;
		}
		if (AdvanceWarning(mp, mpMax, ClientSettings.MpAlarm, ref lastMp))
		{
			mpFlashAt = unscaledTime;
		}
	}

	private static float WarningValue(float current, float maximum, int alarm)
	{
		if (alarm <= 0 || Mathf.FloorToInt(current * 100f / Mathf.Max(1f, maximum)) >= alarm)
		{
			return Mathf.Floor((float)alarm * maximum / 100f);
		}
		return current;
	}

	private static bool AdvanceWarning(float current, float maximum, int alarm, ref float previous)
	{
		bool result = alarm > 0 && Mathf.FloorToInt(current * 100f / Mathf.Max(1f, maximum)) < alarm && previous > current;
		previous = WarningValue(current, maximum, alarm);
		return result;
	}

	private static void DrawFlash(int index, float x, float barY)
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		float num = Time.unscaledTime - ((index == 0) ? hpFlashAt : mpFlashAt);
		if (!(num < 0f) && !(num >= 0.65f))
		{
			int num2 = Mathf.Clamp(Mathf.FloorToInt(num / 0.13f), 0, 4) % 2;
			Texture2D val = Art.Tex("ui/statusbar/" + ((index == 0) ? "hp" : "mp") + "flash_" + num2 + ".bytes");
			if (!((Object)(object)val == (Object)null))
			{
				GUI.DrawTexture(At(x, barY, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val, (ScaleMode)0);
			}
		}
	}

	private static Rect TemporaryExpRect(long exp, long maximum, long temporary)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		if (maximum <= 0)
		{
			return Rect.zero;
		}
		exp = Math.Max(0L, Math.Min(maximum, exp));
		temporary = Math.Max(0L, Math.Min(maximum - exp, temporary));
		int num = (int)(115 * exp / maximum);
		int num2 = (int)(115 * temporary / maximum);
		return new Rect((float)(6 + num), 1f, (float)num2, 15f);
	}

	private static void DrawTemporaryExp(float x, float barY)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		long temporary = (GameSession.Character?.record?.gachaexp).GetValueOrDefault();
		Rect val = TemporaryExpRect(Profile.experience, Profile.ExpNeeded, temporary);
		if (!(val.width <= 0f))
		{
			Texture2D val2 = Art.Tex("wz://UI/StatusBar.img/gauge/tempExp");
			if (!((Object)(object)val2 == (Object)null))
			{
				GUI.DrawTexture(At(x + val.x, barY + val.y, val.width, val.height), (Texture)(object)val2, (ScaleMode)0);
			}
		}
	}

	private static string Number(float current, float max)
	{
		return "[" + Mathf.CeilToInt(current) + "/" + Mathf.CeilToInt(max) + "]";
	}

	private static void DrawClippedFill(Rect clipped, Texture2D fill, int fullWidth)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)fill == (Object)null) && !(clipped.width <= 0.5f))
		{
			GUI.BeginGroup(clipped);
			GUI.DrawTexture(new Rect(0f, 0f, (float)fullWidth * S, 18f * S), (Texture)(object)fill, (ScaleMode)0);
			GUI.EndGroup();
		}
	}

	private static void DrawNumber(float leftX, float topY, string text)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		float num = leftX;
		foreach (char c in text)
		{
			Texture2D val = Glyph(c);
			if ((Object)(object)val != (Object)null)
			{
				float num2 = topY + (9f - (float)((Texture)val).height) * 0.5f * S;
				GUI.DrawTexture(new Rect(num, num2, (float)((Texture)val).width * S, (float)((Texture)val).height * S), (Texture)(object)val, (ScaleMode)0);
			}
			num += Advance(c) * S;
		}
	}

	private static Texture2D Glyph(char c)
	{
		return c switch
		{
			'/' => digitSlash, 
			'%' => digitPercent, 
			'[' => digitLb, 
			']' => digitRb, 
			_ => Digit(digits, c), 
		};
	}

	private static float Advance(char c)
	{
		switch (c)
		{
		case '%':
		case '/':
			return 8f;
		case '[':
		case ']':
			return 3f;
		default:
			return 6f;
		}
	}

	private static Texture2D Digit(Texture2D[] set, char c)
	{
		if (c < '0' || c > '9' || set == null)
		{
			return null;
		}
		return set[c - 48];
	}

	private static Rect At(float ox, float oy, float w, float h)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		Rect val = Panel;
		float num = val.x + ox * S;
		val = Panel;
		return new Rect(num, val.y + oy * S, w * S, h * S);
	}

	private static GUIStyle Style(int size, Color color, TextAnchor anchor, FontStyle style)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		textStyle.font = Font();
		textStyle.fontSize = Mathf.Max(8, Mathf.RoundToInt((float)size * S));
		textStyle.alignment = anchor;
		textStyle.fontStyle = style;
		textStyle.normal.textColor = color;
		textStyle.wordWrap = false;
		textStyle.clipping = (TextClipping)1;
		return textStyle;
	}

	private static void Shadow(Rect r, string value, GUIStyle style)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		Color textColor = style.normal.textColor;
		style.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
		GUI.Label(new Rect(r.x + 0.5f * S, r.y + 0.5f * S, r.width, r.height), value, style);
		style.normal.textColor = textColor;
		GUI.Label(r, value, style);
	}

	private static Font Font()
	{
		if (fontTried)
		{
			return font;
		}
		fontTried = true;
		font = Resources.Load<Font>("Fonts/ui");
		return font;
	}

	private static void Load()
	{
		if (!loaded)
		{
			loaded = true;
			panel = Art.Tex("ui/statusbar/bar_lv.bytes");
			for (int i = 0; i < 3; i++)
			{
				labels[i] = Art.Tex(LabelRel[i]);
				fills[i] = Art.Tex(FillRel[i]);
				frames[i] = Art.Tex(FrameRel[i]);
			}
			for (int j = 0; j < 10; j++)
			{
				digits[j] = Art.Tex("ui/statusbar/num_" + j + ".bytes");
				levelNo[j] = Art.Tex("ui/level/lv_" + j + ".bytes");
			}
			gray = Art.Tex("ui/statusbar/gauge_gray.bytes");
			digitSlash = Art.Tex("ui/statusbar/num_slash.bytes");
			digitPercent = Art.Tex("ui/statusbar/num_percent.bytes");
			digitLb = Art.Tex("ui/statusbar/num_lb.bytes");
			digitRb = Art.Tex("ui/statusbar/num_rb.bytes");
		}
	}

	static StatusBar()
	{
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Expected Obj, but got Unknown
	}
}
