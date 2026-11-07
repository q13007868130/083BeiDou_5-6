using System;
using UnityEngine;

public static class StatWindow
{
	public static bool Visible;

	private static Vector2 pos = new Vector2(220f, 80f);

	private static bool detail = true;

	private static bool dragging;

	private const int WinW = 208;

	private const int WinH = 347;

	private const int ApArrowX = 153;

	private static Vector2 grab;

	private static float S => BottomBar.S;

	private static Rect At(Rect r, float x, float y, float w, float h)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(r.x + x * S, r.y + y * S, w * S, h * S);
	}

	private static Texture2D Tex(string node)
	{
		return Art.Tex("wz://UI/UIWindow.img/Stat/" + node);
	}

	private static void Image(Rect r, string node, float x, float y)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Tex(node);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(At(r, x, y, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
		}
	}

	private static void Text(Rect r, string text, float x, float y, int width = 135)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected Obj, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = Mathf.RoundToInt(11f * S),
			clipping = (TextClipping)1
		};
		val.normal.textColor = Color.black;
		GUI.Label(At(r, x, y, width, 16f), text, val);
	}

	private static void TextFit(Rect r, string text, float x, float y, int width, int maxSize = 11)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected Obj, but got Unknown
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected Obj, but got Unknown
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		for (int num = maxSize; num >= 8; num--)
		{
			GUIStyle val = new GUIStyle
			{
				font = UiKit.Font,
				fontSize = Mathf.RoundToInt((float)num * S),
				clipping = (TextClipping)1
			};
			val.normal.textColor = Color.black;
			if (val.CalcSize(new GUIContent(text)).x <= (float)width * S || num == 8)
			{
				GUI.Label(At(r, x, y, width, 16f), text, val);
				break;
			}
		}
	}

	private static string StatLine(int baseValue, int total)
	{
		int num = total - baseValue;
		if (num == 0)
		{
			return total.ToString();
		}
		return total + " (" + baseValue + ((num > 0) ? "+" : "") + num + ")";
	}

	public static void Toggle()
	{
		Visible = !Visible;
	}

	private static void DrawAuto(Rect r, string node, int y, bool brawler)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Tex(node + "/normal/0");
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		Rect rect = At(r, 97f, y, ((Texture)val).width, ((Texture)val).height);
		int[] plan = Profile.AutoApPlan(brawler);
		if (plan == null || !Array.Exists(plan, (int n) => n > 0))
		{
			Image(r, node + "/disabled/0", 97f, y);
		}
		else
		{
			if (!NativeUi.Button(rect, "UI/UIWindow.img/Stat/" + node))
			{
				return;
			}
			string[] array = new string[4] { "力量", "敏捷", "智力", "运气" };
			string text = "    能力点将按如下方式分配：\n\n";
			for (int num = 0; num < 4; num++)
			{
				text = text + "     " + array[num] + " : +" + plan[num] + "\n";
			}
			int job = Profile.JobId;
			NativeUtilPrompt.Confirm(text, (bool yes) =>
			{
				if (yes && Profile.JobId == job && !Profile.AssignAutoAp(plan))
				{
					NativeUtilPrompt.Notice("能力点不足，无法按该方案分配。");
				}
			});
		}
	}

	public static void Draw()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Invalid comparison between Unknown and I4
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_036f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0333: Unknown result type (might be due to invalid IL or missing references)
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03db: Expected Obj, but got Unknown
		//IL_03e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0401: Unknown result type (might be due to invalid IL or missing references)
		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
		//IL_044b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0427: Unknown result type (might be due to invalid IL or missing references)
		//IL_0438: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_05bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_067d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0682: Unknown result type (might be due to invalid IL or missing references)
		//IL_051a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0537: Unknown result type (might be due to invalid IL or missing references)
		//IL_0553: Unknown result type (might be due to invalid IL or missing references)
		//IL_0558: Unknown result type (might be due to invalid IL or missing references)
		//IL_0581: Unknown result type (might be due to invalid IL or missing references)
		//IL_0563: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0850: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible || (Object)(object)GameLoop.I == (Object)null || (Object)(object)GameLoop.I.player == (Object)null)
		{
			return;
		}
		pos = NativeUi.ClampWindowPosition(pos, new Vector2(208f, 347f), S);
		Rect val = new Rect(pos.x * S, pos.y * S, 208f * S, 347f * S);
		NativeUi.RegisterWindow("StatWindow", val, () => Visible);
		if ((int)Event.current.type == 7 && Pointer.Pressed)
		{
			Rect val2 = At(val, 0f, 0f, 184f, 22f);
			if (val2.Contains(Pointer.Pos))
			{
				dragging = true;
				grab = Pointer.Pos / S - pos;
			}
		}
		if (dragging && Pointer.Down)
		{
			pos = Pointer.Pos / S - grab;
			pos = NativeUi.ClampWindowPosition(pos, new Vector2(208f, 347f), S);
			val.position = pos * S;
		}
		if (!Pointer.Down)
		{
			dragging = false;
		}
		Image(val, "backgrnd", 0f, 0f);
		if (NativeUi.Button(At(val, 189f, 5f, 12f, 12f), "UI/Basic.img/BtClose"))
		{
			Visible = false;
			return;
		}
		PlayerCtrl player = GameLoop.I.player;
		WeaponDef weaponDef = WeaponDefs.Get(Profile.equipWeapon);
		CharacterAttributes characterAttributes = player.ReadCombatAttributes(out var physicalAttack, out var magicalAttack);
		string[] array = new string[8]
		{
			Profile.playerName,
			"",
			Profile.level.ToString(),
			"-",
			Mathf.CeilToInt(player.hp) + " / " + characterAttributes.maxHp,
			Mathf.CeilToInt(player.mp) + " / " + characterAttributes.maxMp,
			Profile.experience + " (" + (Profile.ExpRatio * 100f).ToString("0.##") + "%)",
			"0"
		};
		int[] array2 = new int[8] { 33, 55, 79, 96, 115, 133, 151, 169 };
		for (int num = 0; num < array.Length; num++)
		{
			Text(val, array[num], 61f, array2[num]);
		}
		int jobId = Profile.JobId;
		string node = "Job/sub/" + jobId / 100 + "/" + jobId;
		if ((Object)(object)Tex(node) != (Object)null)
		{
			Image(val, "Job/main/" + jobId / 100, 62f, 53f);
			Image(val, node, 62f, 64f);
		}
		else
		{
			Text(val, JobDefs.NameOf(jobId), 61f, 55f);
		}
		if (Profile.AutomaticAp)
		{
			Image(val, "basicStat", 8f, 195f);
		}
		else
		{
			GUIStyle val3 = new GUIStyle
			{
				font = UiKit.Font,
				fontSize = Mathf.RoundToInt(11f * S),
				alignment = (TextAnchor)5
			};
			val3.normal.textColor = Color.black;
			GUI.Label(At(val, 61f, 213f, 26f, 16f), Profile.ap.ToString(), val3);
			if (Profile.JobId == 500)
			{
				DrawAuto(val, "BtAuto1", 197, brawler: true);
				DrawAuto(val, "BtAuto2", 217, brawler: false);
			}
			else
			{
				DrawAuto(val, "BtAuto", 197, brawler: false);
			}
			int[] array3 = new int[4] { characterAttributes.str, characterAttributes.dex, characterAttributes.intelligence, characterAttributes.luk };
			CharacterAttributes attributes = Profile.Attributes;
			int[] array4 = new int[4] { attributes.str, attributes.dex, attributes.intelligence, attributes.luk };
			for (int num2 = 0; num2 < array3.Length; num2++)
			{
				if (Profile.JobId == 0 && num2 >= 2)
				{
					Image(val, "Disabled/" + ((num2 == 2) ? "INT" : "LUK"), 8f, 244 + num2 * 18);
				}
				string text = StatLine(array4[num2], array3[num2]);
				TextFit(val, text, 61f, 244 + num2 * 18, 88);
				Rect rect = At(val, 153f, 247 + num2 * 18, 12f, 12f);
				if (!Profile.CanAssignAp(num2))
				{
					Image(val, "BtApUp/disabled/0", 153f, 247 + num2 * 18);
				}
				else if (NativeUi.Button(rect, "UI/UIWindow.img/Stat/BtApUp"))
				{
					Profile.AssignAp(num2);
				}
			}
			Image(val, "BtApUp/disabled/0", 153f, 117f);
			Image(val, "BtApUp/disabled/0", 153f, 135f);
		}
		if (NativeUi.Button(At(val, 124f, 324f, 46f, 19f), "UI/UIWindow.img/Stat/BtDetail"))
		{
			detail = !detail;
		}
		if (detail)
		{
			float num3 = val.xMax - S;
			if (num3 + 218f * S > UiKit.W)
			{
				num3 = Mathf.Max(0f, val.x - 217f * S);
			}
			Rect r = new Rect(num3, val.y + 144f * S, 218f * S, 203f * S);
			Image(r, "backgrnd2", 0f, 0f);
			bool flag = weaponDef.kind == WeaponKind.bow || weaponDef.kind == WeaponKind.crossbow || weaponDef.kind == WeaponKind.gun;
			int num4;
			if (weaponDef.kind == WeaponKind.claw)
			{
				num4 = characterAttributes.luk;
			}
			else
			{
				num4 = (flag ? characterAttributes.dex : characterAttributes.str);
			}
			int num5;
			if (weaponDef.kind == WeaponKind.claw)
			{
				num5 = characterAttributes.dex + characterAttributes.str;
			}
			else
			{
				num5 = (flag ? characterAttributes.str : characterAttributes.dex);
			}
			int num6 = CombatRules.MaxPhysicalDamage(num4, num5, physicalAttack, weaponDef.multiplier);
			int num7 = (int)Math.Floor(((double)num4 * weaponDef.multiplier * 0.9 * (double)characterAttributes.mastery + (double)num5) * (double)physicalAttack / 100.0);
			string[] array5 = new string[9]
			{
				num7 + " ~ " + num6,
				characterAttributes.weaponDefense.ToString(),
				magicalAttack.ToString(),
				characterAttributes.magicDefense.ToString(),
				Mathf.FloorToInt((float)characterAttributes.accuracy + (float)characterAttributes.dex * 0.8f + (float)characterAttributes.luk * 0.5f).ToString(),
				Mathf.FloorToInt((float)characterAttributes.avoidability + (float)characterAttributes.dex * 0.25f + (float)characterAttributes.luk * 0.5f).ToString(),
				(characterAttributes.dex + characterAttributes.intelligence + characterAttributes.luk).ToString(),
				"100",
				"100"
			};
			for (int num8 = 0; num8 < array5.Length; num8++)
			{
				Text(r, array5[num8], 80f, 9 + num8 * 18, 130);
			}
		}
	}

	static StatWindow()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
	}
}
