using System;
using UnityEngine;

public static class SysOptWindow
{
	public static bool Visible;

	private const int W = 299;

	private const int H = 365;

	private const int RowTop = 32;

	private const int RowPitch = 30;

	private const int RowH = 28;

	private const int RowQuality = 0;

	private const int RowBgm = 1;

	private const int RowSfx = 2;

	private const int RowShot = 3;

	private const int RowMouse = 4;

	private const int RowHp = 5;

	private const int RowMp = 6;

	private const int RowFx = 7;

	private const int RowMobInfo = 8;

	private const int RowScreenMode = 9;

	private const float TrackX = 100f;

	private const float TrackW = 148f;

	private const float TrackAudioX = 96f;

	private const float TrackAudioW = 116f;

	private const float MuteX = 240f;

	private const float CheckX = 262f;

	private static Vector2 pos = new Vector2(196f, 92f);

	private static bool dragging;

	private static Vector2 grab;

	private static int sliderDrag = -1;

	private static float S => BottomBar.S;

	public static void Toggle()
	{
		Visible = !Visible;
	}

	public static void Close()
	{
		Visible = false;
	}

	private static Rect At(Rect r, float x, float y, float w, float h)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(r.x + x * S, r.y + y * S, w * S, h * S);
	}

	public static void Draw()
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		if (!SettingsShell.Begin("wz://UI/UIWindow.img/SysOpt/backgrnd", ref pos, 299, 365, S, ref dragging, ref grab, out var rect))
		{
			Visible = false;
			return;
		}
		Slider(rect, 0, 100f, 148f, 0.5f, 100, enabled: false);
		HintRow(rect, 0);
		float num = Slider(rect, 1, 96f, 116f, AudioManager.BgmVolume, 101, !AudioManager.BgmMuted);
		if (Mathf.Abs(num - AudioManager.BgmVolume) > 0.0005f)
		{
			AudioManager.BgmVolume = num;
		}
		Checkbox(rect, 1, 240f, AudioManager.BgmMuted, "音乐已静音", "音乐已恢复", () =>
		{
			AudioManager.BgmMuted = !AudioManager.BgmMuted;
		});
		float num2 = Slider(rect, 2, 96f, 116f, AudioManager.SfxVolume, 102, !AudioManager.SfxMuted);
		if (Mathf.Abs(num2 - AudioManager.SfxVolume) > 0.0005f)
		{
			AudioManager.SfxVolume = num2;
		}
		Checkbox(rect, 2, 240f, AudioManager.SfxMuted, "音效已静音", "音效已恢复", () =>
		{
			AudioManager.SfxMuted = !AudioManager.SfxMuted;
		});
		RowHit(rect, 3, () =>
		{
			BottomBar.Toast("手机端截图直接保存到系统相册的 MapleDash 目录");
		});
		Slider(rect, 4, 100f, 148f, 0.5f, 103, enabled: false);
		HintRow(rect, 4);
		int num3 = Mathf.RoundToInt(Slider(rect, 5, 100f, 148f, (float)ClientSettings.HpAlarm / 100f, 104, enabled: true) * 100f);
		if (num3 != ClientSettings.HpAlarm)
		{
			ClientSettings.HpAlarm = num3;
		}
		int num4 = Mathf.RoundToInt(Slider(rect, 6, 100f, 148f, (float)ClientSettings.MpAlarm / 100f, 105, enabled: true) * 100f);
		if (num4 != ClientSettings.MpAlarm)
		{
			ClientSettings.MpAlarm = num4;
		}
		Checkbox(rect, 7, 262f, ClientSettings.ScreenEffects, "", "", () =>
		{
			ClientSettings.ScreenEffects = !ClientSettings.ScreenEffects;
		});
		Checkbox(rect, 8, 262f, on: false, "", "", null);
		RowHit(rect, 9, () =>
		{
			BottomBar.Toast("手机端固定全屏，窗口化模式不适用");
		});
	}

	private static void HintRow(Rect r, int row)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		RowHit(r, row, () =>
		{
			BottomBar.Toast((row == 0) ? "画质由手机性能档位决定，此处不适用" : "手机端没有鼠标，速度调整不适用");
		});
	}

	private static void RowHit(Rect r, int row, Action onClick)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Invalid comparison between Unknown and I4
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		Rect val = At(r, 65f, 32 + row * 30, 226f, 28f);
		if ((int)Event.current.type == 7 && Pointer.Clicked && val.Contains(Pointer.Pos))
		{
			Pointer.ConsumeClick();
			onClick();
		}
	}

	private static float Slider(Rect r, int row, float x0, float w, float value, int id, bool enabled)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Invalid comparison between Unknown and I4
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		float num = 32 + row * 30;
		Rect val = At(r, x0, num + 10f, w, 8f);
		Texture2D val2 = Art.Tex("wz://UI/Basic.img/Slider/left");
		Texture2D val3 = Art.Tex("wz://UI/Basic.img/Slider/mid");
		Texture2D val4 = Art.Tex("wz://UI/Basic.img/Slider/right");
		if ((Object)(object)val3 != (Object)null)
		{
			GUI.DrawTexture(new Rect(val.x + 3f * S, val.y, Mathf.Max(0f, val.width - 6f * S), val.height), (Texture)(object)val3, (ScaleMode)0);
		}
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(new Rect(val.x, val.y, 3f * S, val.height), (Texture)(object)val2);
		}
		if ((Object)(object)val4 != (Object)null)
		{
			GUI.DrawTexture(new Rect(val.xMax - 3f * S, val.y, 3f * S, val.height), (Texture)(object)val4);
		}
		float num2 = 22f * S;
		float num3 = 14f * S;
		Rect val5 = new Rect(val.x - 4f * S, val.y - 5f * S, val.width + 8f * S, num3 + 4f * S);
		bool flag = val5.Contains(Pointer.Pos);
		if ((int)Event.current.type == 7)
		{
			if (enabled && Pointer.Pressed && val5.Contains(Pointer.Pos))
			{
				sliderDrag = id;
				Pointer.ConsumeClick();
			}
			if (sliderDrag == id && (!Pointer.Down || !enabled))
			{
				sliderDrag = -1;
			}
		}
		if (enabled && sliderDrag == id && Pointer.Down)
		{
			float num4 = Mathf.Max(1f, val.width - num2);
			value = Mathf.Clamp01((Pointer.Pos.x - val.x - num2 * 0.5f) / num4);
		}
		string text;
		if (enabled)
		{
			if (sliderDrag == id)
			{
				text = "thumbPressed";
			}
			else
			{
				text = (flag ? "thumbMouseOver" : "thumbNormal");
			}
		}
		else
		{
			text = "thumbDisabled";
		}
		Texture2D val6 = Art.Tex("wz://UI/Basic.img/Slider/" + text);
		float num5 = val.x + Mathf.Max(0f, val.width - num2) * Mathf.Clamp01(value);
		if ((Object)(object)val6 != (Object)null)
		{
			GUI.DrawTexture(new Rect(num5, val.y + val.height * 0.5f - num3 * 0.5f, num2, num3), (Texture)(object)val6);
		}
		return Mathf.Clamp01(value);
	}

	private static void Checkbox(Rect r, int row, float x, bool on, string toastOn, string toastOff, Action toggle)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Invalid comparison between Unknown and I4
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		Rect val = At(r, x, (float)(32 + row * 30) + 8f, 12f, 12f);
		Texture2D val2 = Art.Tex("wz://UI/Basic.img/CheckBox/" + (on ? 1 : 0));
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(val, (Texture)(object)val2);
		}
		if ((int)Event.current.type != 7 || !Pointer.Clicked || !val.Contains(Pointer.Pos))
		{
			return;
		}
		Pointer.ConsumeClick();
		if (toggle == null)
		{
			BottomBar.Toast("手机端未开放");
			return;
		}
		AudioManager.Play("UI/BtMouseClick", 0.7f, pitchVary: false);
		toggle();
		string text = ((!on) ? toastOn : toastOff);
		if (!string.IsNullOrEmpty(text))
		{
			BottomBar.Toast(text);
		}
	}

	static SysOptWindow()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
	}
}
