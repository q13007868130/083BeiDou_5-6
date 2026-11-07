using System;
using UnityEngine;

public static class NativeAndroidWindow
{
	public static bool Visible;

	private const int W = 152;

	private const int H = 272;

	private const int GridX = 10;

	private const int GridY = 24;

	private const int ColPitch = 48;

	private const int RowPitch = 45;

	private const int Cell = 28;

	private static readonly int[] Slots = new int[14]
	{
		-32, -54, 1200, 1202, 1203, 1204, 1205, 1206, 1201, 1207,
		1210, 1211, 1212, 1213
	};

	private static readonly string[] Names = new string[14]
	{
		"機器人", "心臟", "帽子", "臉飾", "上衣", "下裝", "鞋", "手套", "披風", "武器",
		"戒指", "戒指", "戒指", "戒指"
	};

	private static GUIStyle label;

	private static GUIStyle title;

	private static string reason = "";

	private static double reasonAt;

	private static float S => BottomBar.S;

	public static void Toggle()
	{
		Visible = !Visible;
	}

	public static bool SlotAt(Vector2 pointer, out int slot)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		slot = 0;
		if (!Visible || !GameSession.InGame)
		{
			return false;
		}
		Rect r = RectOf();
		for (int i = 0; i < Slots.Length; i++)
		{
			Rect val = DropRect(r, i);
			if (val.Contains(pointer))
			{
				slot = Slots[i];
				return true;
			}
		}
		return false;
	}

	public static void Draw()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Expected Obj, but got Unknown
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Expected Obj, but got Unknown
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Invalid comparison between Unknown and I4
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		if (!GameSession.InGame || GameSession.Character == null || !EquipWindow.Visible)
		{
			Visible = false;
			return;
		}
		Rect val = RectOf();
		NativeUi.RegisterWindow("AndroidWindow", val, () => Visible);
		UiKit.Fill(val, new Color(0.075f, 0.085f, 0.115f, 0.97f));
		if (title == null)
		{
			title = new GUIStyle(GUI.skin.label)
			{
				font = UiKit.Font,
				fontSize = 12,
				alignment = (TextAnchor)3
			};
		}
		if (label == null)
		{
			label = new GUIStyle(GUI.skin.label)
			{
				font = UiKit.Font,
				fontSize = 10,
				alignment = (TextAnchor)4
			};
		}
		title.normal.textColor = new Color(0.85f, 0.9f, 1f);
		label.normal.textColor = new Color(0.62f, 0.68f, 0.8f);
		GUI.Label(new Rect(val.x + 6f * S, val.y + 3f * S, 90f * S, 16f * S), "機器人", title);
		if (NativeUi.Button(new Rect(val.xMax - 17f * S, val.y + 4f * S, 12f * S, 12f * S), "UI/Basic.img/BtClose"))
		{
			Visible = false;
			return;
		}
		for (int num = 0; num < Slots.Length; num++)
		{
			int slot = Slots[num];
			Rect val2 = CellRect(val, num);
			UiKit.Fill(val2, new Color(0f, 0f, 0f, 0.45f));
			int num2 = Profile.EquipOfSlot(slot);
			if (num2 > 0)
			{
				Texture2D val3 = EquipWindow.ArmorIcon(num2);
				if ((Object)(object)val3 != (Object)null)
				{
					GUI.DrawTexture(val2, (Texture)(object)val3, (ScaleMode)2);
				}
			}
			GUI.Label(new Rect(val2.x - 8f * S, val2.yMax, val2.width + 16f * S, 12f * S), Names[num], label);
			if ((int)Event.current.type != 7)
			{
				continue;
			}
			if (val2.Contains(Pointer.Pos) && num2 > 0)
			{
				ItemTooltip.SetHover(num2, equipped: true);
			}
			if (num2 > 0 && Pointer.DoubleClicked && val2.Contains(Pointer.Pos))
			{
				Pointer.ConsumeClick();
				EquipLoadout.Unequip(slot);
				if (Profile.EquipOfSlot(slot) == 0)
				{
					BottomBar.Toast("已脱下" + NameOf(slot));
				}
			}
		}
		if ((double)Time.unscaledTime >= reasonAt)
		{
			reasonAt = Time.unscaledTime + 0.5f;
			reason = NativeAndroidService.Resolve(GameSession.CharacterId, Profile.EquipOfSlot).reason;
		}
		if (!string.IsNullOrEmpty(reason))
		{
			label.normal.textColor = new Color(1f, 0.62f, 0.55f);
			label.alignment = (TextAnchor)4;
			GUI.Label(new Rect(val.x + 4f * S, val.yMax - 15f * S, 144f * S, 13f * S), reason, label);
			label.normal.textColor = new Color(0.62f, 0.68f, 0.8f);
		}
	}

	public static string NameOf(int slot)
	{
		int num = Array.IndexOf(Slots, slot);
		if (num < 0)
		{
			return "装备";
		}
		return Names[num];
	}

	private static Rect RectOf()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		Vector2 size = EquipWindow.Size;
		Vector2 position = EquipWindow.Position;
		Vector2 val = NativeUi.ClampWindowPosition((position.x + size.x + 2f + 152f <= UiKit.W / S) ? new Vector2(position.x + size.x + 2f, position.y) : new Vector2(position.x - 152f - 2f, position.y), new Vector2(152f, 272f), S) * S;
		return new Rect(val.x, val.y, 152f * S, 272f * S);
	}

	private static Rect CellRect(Rect r, int index)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(r.x + (float)(10 + index % 3 * 48) * S, r.y + (float)(24 + index / 3 * 45) * S, 28f * S, 28f * S);
	}

	private static Rect DropRect(Rect r, int index)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		Rect val = CellRect(r, index);
		return new Rect(val.x - 10f * S, val.y, 48f * S, 45f * S);
	}

	public static bool ToggleButton(Rect rect, bool on, string text)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Expected Obj, but got Unknown
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Invalid comparison between Unknown and I4
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		UiKit.Fill(rect, on ? new Color(0.24f, 0.44f, 0.72f, 0.95f) : new Color(0.13f, 0.15f, 0.19f, 0.95f));
		if (label == null)
		{
			label = new GUIStyle(GUI.skin.label)
			{
				font = UiKit.Font,
				fontSize = 10,
				alignment = (TextAnchor)4
			};
		}
		Color textColor = label.normal.textColor;
		label.normal.textColor = (Color)(on ? Color.white : new Color(0.72f, 0.78f, 0.9f));
		GUI.Label(rect, text, label);
		label.normal.textColor = textColor;
		if ((int)Event.current.type == 7 && Pointer.Pressed)
		{
			return rect.Contains(Pointer.Pos);
		}
		return false;
	}
}
