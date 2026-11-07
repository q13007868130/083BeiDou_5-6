using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class NativeSoulWeaponPanel
{
	private const string Root = "UI/UIWindowWz2.img/SoulWeaponWz2/";

	private static float scale;

	private static string modal;

	private static Rect modalRect;

	private static InventoryEntry selected;

	private static int owner;

	private static string amountText = "1";

	private static int previousGrade;

	private static SoulWeaponWz2State State => GameSession.Character?.soulWeapon ?? new SoulWeaponWz2State();

	public static bool AtPointer
	{
		get
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			if (modal != null)
			{
				return modalRect.Contains(Pointer.Pos);
			}
			return false;
		}
	}

	public static bool HasModal => modal != null;

	public static void CloseModal()
	{
		modal = null;
		if (NativeEditKeyboard.Owns("SoulWeaponLevel"))
		{
			NativeEditKeyboard.Close();
		}
	}

	public static void Reset()
	{
		CloseModal();
		selected = null;
		owner = 0;
		if (NativeSoulWeapon.IsSkill(SkillWindow.DragSkillId))
		{
			SkillWindow.CancelSkillDrag();
		}
	}

	public static void Refresh()
	{
		if (GameSession.InGame)
		{
			SoulResponse soulResponse = GameSession.Net.Soul(new SoulRequest
			{
				characterId = GameSession.CharacterId,
				operation = SoulOperation.ReadModern
			});
			if (soulResponse.ok)
			{
				GameSession.Character.soulWeapon = soulResponse.modern;
			}
			else
			{
				BottomBar.Toast(soulResponse.error);
			}
			owner = GameSession.CharacterId;
		}
	}

	private static ImgNode Node(string path)
	{
		return WzAssets.Node("UI/UIWindowWz2.img/SoulWeaponWz2/" + path);
	}

	private static Rect At(Rect r, float x, float y, float w, float h)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(r.x + x * scale, r.y + y * scale, w * scale, h * scale);
	}

	private static void Image(Rect r, string path, float x = 0f, float y = 0f)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		ImgNode frame = Node(path);
		Texture2D val = WzAssets.Texture(frame);
		if (!((Object)(object)val == (Object)null))
		{
			Vector2 val2 = WzAssets.Origin(frame);
			GUI.DrawTexture(At(r, x - val2.x, y - val2.y, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
		}
	}

	private static bool Button(Rect r, string path, float x = 0f, float y = 0f, bool enabled = true)
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Invalid comparison between Unknown and I4
		ImgNode imgNode = Node(path)?.Get("slotOffset");
		if (imgNode != null)
		{
			x += (float)imgNode.X;
			y += (float)imgNode.Y;
		}
		ImgNode frame = Node(path + "/normal/0");
		Texture2D val = WzAssets.Texture(frame);
		if ((Object)(object)val == (Object)null)
		{
			return false;
		}
		Vector2 val2 = WzAssets.Origin(frame);
		Rect val3 = At(r, x - val2.x, y - val2.y, ((Texture)val).width, ((Texture)val).height);
		bool flag = enabled && GUI.enabled;
		bool flag2 = val3.Contains(Pointer.Pos);
		string text;
		if (flag)
		{
			if (flag2)
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
		GUI.DrawTexture(val3, (Texture)(object)(WzAssets.Texture(Node(path + "/" + text + "/0")) ?? val));
		if ((int)Event.current.type != 7 || !Pointer.Clicked || !flag2 || !GUI.enabled)
		{
			return false;
		}
		Pointer.ConsumeClick();
		if (!flag)
		{
			return false;
		}
		AudioManager.Play("UI/BtMouseClick", 0.7f, pitchVary: false);
		return true;
	}

	private static void Label(Rect r, string text, string path, float x = 0f, float y = 0f)
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Expected Obj, but got Unknown
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = Node(path);
		if (imgNode == null)
		{
			return;
		}
		ImgNode imgNode2 = imgNode.Get("lt");
		ImgNode imgNode3 = imgNode.Get("rb");
		if (imgNode2 == null || imgNode3 == null)
		{
			return;
		}
		ImgNode imgNode4 = null;
		foreach (ImgNode child in imgNode.Children)
		{
			if (child.Text("class") == "TextBlock")
			{
				imgNode4 = child;
				break;
			}
		}
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = Math.Max(1, Mathf.RoundToInt((float)(imgNode4?.Int("font/size", 12) ?? 12) * scale)),
			wordWrap = true,
			alignment = (TextAnchor)3
		};
		string text2 = imgNode.Text("HAlign");
		if (text2 == "Center")
		{
			val.alignment = (TextAnchor)4;
		}
		else if (text2 == "Right")
		{
			val.alignment = (TextAnchor)5;
		}
		val.normal.textColor = Color.white;
		GUI.Label(At(r, x + (float)imgNode2.X, y + (float)imgNode2.Y, imgNode3.X - imgNode2.X, imgNode3.Y - imgNode2.Y), text, val);
	}

	private static bool Send(SoulOperation operation, int amount = 0)
	{
		SoulResponse soulResponse = GameSession.Net.Soul(new SoulRequest
		{
			characterId = GameSession.CharacterId,
			inventoryItemId = (selected?.itemRowId ?? 0),
			operation = operation,
			amount = amount,
			revision = State.revision,
			commit = true
		});
		if (soulResponse.modern != null)
		{
			GameSession.Character.soulWeapon = soulResponse.modern;
		}
		if (!soulResponse.ok)
		{
			BottomBar.Toast(soulResponse.error);
			return false;
		}
		if (soulResponse.inventory != null)
		{
			GameSession.Character.inventory = soulResponse.inventory;
			GameSession.ApplyInventoryToProfile();
		}
		NativeGrowthStats.Invalidate();
		GameSession.MarkStatsDirty();
		GameLoop.I?.player?.RefreshBuffAttributes();
		return true;
	}

	public static bool Draw(Rect r, float s, InventoryEntry target)
	{
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Invalid comparison between Unknown and I4
		//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0407: Unknown result type (might be due to invalid IL or missing references)
		//IL_0486: Unknown result type (might be due to invalid IL or missing references)
		//IL_049b: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0465: Unknown result type (might be due to invalid IL or missing references)
		//IL_047a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0517: Unknown result type (might be due to invalid IL or missing references)
		//IL_054b: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b8: Invalid comparison between Unknown and I4
		//IL_0572: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_062b: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_068b: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_06bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_072b: Unknown result type (might be due to invalid IL or missing references)
		//IL_079f: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e3: Unknown result type (might be due to invalid IL or missing references)
		scale = s;
		if (owner != GameSession.CharacterId)
		{
			Refresh();
		}
		selected = target;
		SoulWeaponWz2State state = State;
		InventoryEntry inventoryEntry = GameSession.Character.inventory.Find((InventoryEntry i) => i.itemRowId == state.weapon);
		bool flag = state.unlocked && inventoryEntry != null;
		bool flag2 = flag && target != null && target.itemRowId != inventoryEntry.itemRowId;
		InventoryEntry inventoryEntry2;
		if (flag2)
		{
			inventoryEntry2 = target;
		}
		else
		{
			inventoryEntry2 = (flag ? inventoryEntry : target);
		}
		bool flag3 = flag && !flag2;
		SoulGrowth soulGrowth = ((inventoryEntry2 == null) ? null : state.Of(inventoryEntry2.itemRowId));
		bool flag4 = inventoryEntry2 != null && state.Has(inventoryEntry2.itemRowId);
		int num = Mathf.Clamp(soulGrowth?.grade ?? 1, 1, 10);
		int num2 = soulGrowth?.level ?? 1;
		int num3 = soulGrowth?.exp ?? 0;
		int id = soulGrowth?.option ?? 0;
		Image(r, "Main/backgrnd");
		bool enabled = GUI.enabled;
		GUI.enabled &= modal == null;
		if (Button(r, "Main/$btExit"))
		{
			GUI.enabled = enabled;
			return true;
		}
		if (Button(r, "Main/$btHelp"))
		{
			NativeUtilPrompt.Pages("灵魂武器", new string[5] { "200级后可启用灵魂武器。武器需求等级须达到150级。将背包或装备栏的武器放入上方武器格，点击武器确认启用；点击已启用的武器可以停用；已有启用武器时放入另一件武器并点击，可确认切换。角色同一时间只能启用一件。启用期间不能转移武器，停用后按武器原有交易规则处理。", "灵魂等级、阶级、经验和烙印跟着武器实例走：换一把武器就是它自己的进度，灵魂碎片和Boss单人击破记录是全角色共用的。", "单人击败列表内Boss可获得灵魂碎片。每周同一Boss只补发较高难度与已获得碎片的差额；周四00:00（UTC+8）重置。碎片用于成长和重设灵魂烙印。", "灵魂等级上限100级、阶级上限10阶。当前阶级经验蓄满且完成对应Boss单人击破后可升阶。共鸣效果和烙印只有装备启用的武器才生效。", "灵魂的斗志持续40秒、冷却120秒。点击技能图标拿起并放到快捷栏，双击图标施放。停用、换下或失去武器时属性立即失效。旧灵魂转换仅在原端转换活动时间内、首次启用时处理。" });
		}
		if (Button(r, "Main/$btReward"))
		{
			ShowBosses();
		}
		string text = "Main/WeaponArea/$WeaponAreaSwitcher/" + (flag3 ? "Enable" : "Disable");
		Image(r, text + "/back/image", 12f, 36f);
		if (flag3)
		{
			Image(r, text + "/slot/image", 12f, 36f);
			Animate(r, text + "/ani/Timeline", 12f, 36f, 60);
		}
		Texture2D val = ((inventoryEntry2 == null) ? null : NativeItemSource.Icon(inventoryEntry2.itemId));
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(At(r, 201f, 91f, 64f, 64f), (Texture)(object)val);
		}
		Rect val2 = At(r, 182f, 73f, 102f, 100f);
		if (enabled && modal == null && (int)Event.current.type == 7 && val2.Contains(Pointer.Pos))
		{
			if (inventoryEntry2 != null)
			{
				ItemTooltip.SetHover(inventoryEntry2.itemId, equipped: false, inventoryEntry2);
			}
			if (Pointer.Clicked)
			{
				Pointer.ConsumeClick();
				if (inventoryEntry2 == null)
				{
					BottomBar.Toast("请从背包或装备栏放入武器");
				}
				else if (!flag)
				{
					NativeUtilPrompt.Confirm("确定将这件武器启用为灵魂武器？\n启用期间无法转移；装备该武器后，共鸣和烙印才会生效。\n主动技能可放入快捷栏使用，也可在面板中双击施放。", (bool ok) =>
					{
						if (ok)
						{
							Send(SoulOperation.ActivateModern);
						}
					});
				}
				else if (flag3)
				{
					NativeUtilPrompt.Confirm("确定停用这件灵魂武器？\n成长和烙印会保留，共鸣和主动技能效果会停止。", (bool ok) =>
					{
						if (ok)
						{
							Send(SoulOperation.DeactivateModern);
						}
					});
				}
				else
				{
					NativeUtilPrompt.Confirm("确定切换灵魂武器？\n将停用当前武器并启用这件武器；等级、阶级、经验和烙印各自独立，切换后显示的是这件武器自己的进度。\n切换后需装备新武器，共鸣和烙印才会生效。", (bool ok) =>
					{
						if (Send(SoulOperation.DeactivateModern))
						{
							Send(SoulOperation.ActivateModern);
						}
					});
				}
			}
		}
		string path = "Main/$GradeAreaSwitcher/" + (flag3 ? "Enable/$GradeAreaEnableSwitcher/" : "Disable/$GradeAreaDisableSwitcher/") + (num - 1);
		Image(r, path, 7f, 210f);
		string text2 = "Main/ActiveSkillArea/$ActiveSkillAreaSwitcher/" + (flag3 ? "Enable" : "Disable");
		Image(r, text2 + "/back/image", 375f, 236f);
		if (flag3)
		{
			Animate(r, text2 + "/ani/Timeline", 402f, 267f, 60);
		}
		int id2 = SoulWeaponWz2Catalog.Int(SoulWeaponWz2Catalog.Grade(num), "ActiveSkillID");
		Texture2D val3 = WzAssets.Texture(WzAssets.Node("Skill/800128.img/skill/" + id2 + "/icon"));
		if ((Object)(object)val3 != (Object)null)
		{
			GUI.DrawTexture(At(r, 386f, 251f, 32f, 32f), (Texture)(object)val3);
		}
		Rect val4 = At(r, 387f, 251f, 59f, 46f);
		if (enabled && modal == null && (int)Event.current.type == 7 && val4.Contains(Pointer.Pos) && Pointer.Clicked)
		{
			Pointer.ConsumeClick();
			if (Event.current.clickCount >= 2)
			{
				if (!flag3)
				{
					BottomBar.Toast("请先启用这件武器");
				}
				else
				{
					SkillWindow.CancelSkillDrag();
					string text3 = NativeSoulWeapon.Cast(id2);
					if (text3 != null)
					{
						BottomBar.Toast(text3);
					}
				}
			}
			else if (flag3)
			{
				SkillWindow.PickSkill(id2);
			}
		}
		Label(r, "攻／魔 +" + SoulWeaponWz2Catalog.ActiveAttackPercent(num) + "%（40秒）\n单击放快捷栏／双击施放\n冷却120秒", "Main/ActiveSkillArea/ActiveSkillDescBorder");
		Label(r, flag4 ? SoulWeaponWz2Catalog.DescribeOption(id) : "尚未启用灵魂武器", "Main/PotentialOptionArea/PotentialOptionTextBorder");
		if (Button(r, "Main/PotentialOptionArea/$btRoll", 0f, 0f, flag3))
		{
			NativeUtilPrompt.Confirm("当前烙印：" + SoulWeaponWz2Catalog.DescribeOption(id) + "\n消耗灵魂碎片 " + SoulWeaponWz2Catalog.Int(SoulWeaponWz2Catalog.Grade(num), "OptionRollCost") + " 个重设？", (bool ok) =>
			{
				if (ok)
				{
					Send(SoulOperation.RollModern);
				}
			});
		}
		Digits(r, "L" + num2, "Main/PassiveSkillArea/SoulLevelTextBorder/$txtSoulLevel/font", 38f, 418f);
		int num4 = SoulWeaponWz2Catalog.RequiredExp(num2 + 1);
		Image(r, "Main/PassiveSkillArea/$SoulGauge/backgrnd", 88f, 413f);
		Texture2D val5 = WzAssets.Texture(Node("Main/PassiveSkillArea/$SoulGauge/fill"));
		float num5;
		if (num2 >= 100)
		{
			num5 = 1f;
		}
		else
		{
			num5 = ((num4 <= 0) ? 0f : Mathf.Clamp01((float)num3 / (float)num4));
		}
		if ((Object)(object)val5 != (Object)null && num5 > 0f)
		{
			GUI.DrawTextureWithTexCoords(At(r, 88f, 413f, 263f * num5, 18f), (Texture)(object)val5, new Rect(0f, 0f, num5, 1f));
		}
		Label(r, (num2 >= 100) ? "MAX" : (num3 + " / " + num4), "Main/PassiveSkillArea/SoulExpTextBorder");
		bool flag5 = num2 >= SoulWeaponWz2Catalog.MaxLevel(num);
		string path2 = "Main/PassiveSkillArea/$LevelGradeUpButtonSwitcher/" + (flag5 ? "GradeUp/$btGradeUp" : "LevelUp/$btLevelUp");
		if (Button(r, path2, 352f, 412f, flag3 && num2 < 100))
		{
			if (flag5)
			{
				modal = "GradeUp";
			}
			else if (Math.Min(state.fragments, NativeSoulWeaponWz2Service.RemainingExp(soulGrowth ?? new SoulGrowth())) <= 0)
			{
				BottomBar.Toast("灵魂碎片不足");
			}
			else
			{
				amountText = "1";
				modal = "LevelUp";
			}
		}
		Label(r, "物理攻击力／魔法攻击力 + " + SoulWeaponWz2Catalog.PassiveAttack(num2), "Main/PassiveSkillArea/PassiveSkillOptionTextBorder");
		Image(r, "Main/FragmentArea/FragmentImage/image", 351f, 498f);
		Label(r, state.fragments + "个", "Main/FragmentArea/FragmentAmountTextBorder");
		GUI.enabled = enabled;
		if (modal != null)
		{
			DrawModal();
		}
		return false;
	}

	private static void Digits(Rect r, string text, string path, float x, float y)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			Texture2D val = WzAssets.Texture(Node(path + "/" + c));
			if (!((Object)(object)val == (Object)null))
			{
				Image(r, path + "/" + c, x, y);
				x += (float)((Texture)val).width;
			}
		}
	}

	private static void Animate(Rect r, string path, float x, float y, int delay)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = Node(path);
		if (imgNode == null)
		{
			return;
		}
		int num = 0;
		foreach (ImgNode child in imgNode.Children)
		{
			if (int.TryParse(child.Name, out var _))
			{
				num++;
			}
		}
		if (num > 0)
		{
			Image(r, path + "/" + (int)(Time.realtimeSinceStartup * 1000f / (float)delay) % num, x, y);
		}
	}

	private static void DrawModal()
	{
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0418: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_031a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		//IL_046a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0385: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c8: Unknown result type (might be due to invalid IL or missing references)
		string path;
		if (modal == "GradeUp")
		{
			path = "GradeUp/$GradeUpSwitcher/Confirm/back/image";
		}
		else
		{
			path = ((modal == "GradeResult") ? "GradeUp/$GradeUpSwitcher/Result/back/backgrnd" : (modal + "/backgrnd"));
		}
		Texture2D val = WzAssets.Texture(Node(path));
		if ((Object)(object)val == (Object)null)
		{
			modal = null;
			return;
		}
		modalRect = new Rect((UiKit.W - (float)((Texture)val).width * scale) / 2f, (UiKit.H - (float)((Texture)val).height * scale) / 2f, (float)((Texture)val).width * scale, (float)((Texture)val).height * scale);
		NativeUi.RegisterWindow("SoulWeaponWz2Modal", modalRect, () => modal != null);
		if (modal == "LevelUp")
		{
			DrawLevelUp();
		}
		else if (modal == "GradeResult")
		{
			string text = "GradeUp/$GradeUpSwitcher/Result";
			SoulGrowth soulGrowth = State.Active ?? new SoulGrowth();
			Image(modalRect, text + "/back/backgrnd");
			Image(modalRect, text + "/$ResultCurrentGradeImageSwiter/" + (soulGrowth.grade - 1), 90f);
			Image(modalRect, text + "/$ResultBeforeGradeImageSwitcher/" + (previousGrade - 1), 94f, 223f);
			Image(modalRect, text + "/$ResultAfterGradeImageSwitcher/" + (soulGrowth.grade - 1), 220f, 215f);
			if (Button(modalRect, text + "/btResultOK"))
			{
				CloseModal();
			}
		}
		else if (modal == "GradeUp")
		{
			string text2 = "GradeUp/$GradeUpSwitcher/Confirm";
			SoulGrowth soulGrowth2 = State.Active ?? new SoulGrowth();
			Image(modalRect, text2 + "/back/image");
			Image(modalRect, text2 + "/$ConfrimCurrentGradeImageSwitcher/" + (soulGrowth2.grade - 1), 90f);
			Image(modalRect, text2 + "/$ConfirmBeforeGradeImageSwitcher/" + (soulGrowth2.grade - 1), 94f, 205f);
			Image(modalRect, text2 + "/$ConfirmAfterGradeImageSwitcher/" + Math.Min(9, soulGrowth2.grade), 220f, 196f);
			Image(modalRect, text2 + "/ConditionArea/back/image", 46f, 321f);
			int num = SoulWeaponWz2Catalog.Int(SoulWeaponWz2Catalog.Grade(soulGrowth2.grade), "GradeUpReqQuestID");
			Label(modalRect, ConditionText(num), text2 + "/ConditionArea/QuestNameBorder");
			Image(modalRect, text2 + "/ConditionArea/$ConditionAreaSwitcher/" + (State.cleared.Contains(num) ? "Enable" : "Disable") + "/back/image", 271f, 329f);
			if (Button(modalRect, text2 + "/$btConfirmOK"))
			{
				previousGrade = soulGrowth2.grade;
				if (Send(SoulOperation.GradeModern))
				{
					modal = "GradeResult";
				}
			}
			if (Button(modalRect, text2 + "/btConfirmCancel"))
			{
				modal = null;
			}
		}
		else
		{
			Image(modalRect, modal + "/backgrnd");
			if (Button(modalRect, modal + "/$btOK") && Send((modal == "Notice/Deactivate") ? SoulOperation.DeactivateModern : SoulOperation.ActivateModern))
			{
				modal = null;
			}
			if (modal != null && Button(modalRect, modal + "/$btClose"))
			{
				modal = null;
			}
		}
	}

	private static void DrawLevelUp()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Expected Obj, but got Unknown
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Invalid comparison between Unknown and I4
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_028e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		SoulGrowth soulGrowth = State.Active ?? new SoulGrowth();
		Image(modalRect, "LevelUp/backgrnd");
		Digits(modalRect, soulGrowth.level.ToString(), "LevelUp/LevelArea/LevelTextBorder/$txtLevel/font", 33f, 39f);
		int num = SoulWeaponWz2Catalog.RequiredExp(soulGrowth.level + 1);
		Image(modalRect, "LevelUp/LevelArea/$ExpGauge/backgrnd", 60f, 41f);
		Texture2D val = WzAssets.Texture(Node("LevelUp/LevelArea/$ExpGauge/fill"));
		float num2 = ((num <= 0) ? 0f : Mathf.Clamp01((float)soulGrowth.exp / (float)num));
		if ((Object)(object)val != (Object)null && num2 > 0f)
		{
			GUI.DrawTextureWithTexCoords(At(modalRect, 60f, 41f, 166f * num2, 10f), (Texture)(object)val, new Rect(0f, 0f, num2, 1f));
		}
		Digits(modalRect, soulGrowth.exp.ToString(), "LevelUp/LevelArea/CurrentExpTextBorder/$txtCurrentExp/font", 234f, 43f);
		Digits(modalRect, num.ToString(), "LevelUp/LevelArea/ReqExpTextBorder/$txtReqExp/font", 267f, 43f);
		Image(modalRect, "LevelUp/AmountControlArea/IconImage/image", 42f, 78f);
		Image(modalRect, "LevelUp/AmountControlArea/InputBoxArea/InputBoxImage/image", 71f, 73f);
		NativeEditKeyboard.Sync("SoulWeaponLevel", ref amountText);
		Rect val2 = At(modalRect, 71f, 75f, 109f, 20f);
		GUIStyle val3 = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = Math.Max(1, Mathf.RoundToInt(12f * scale)),
			alignment = (TextAnchor)5
		};
		val3.normal.textColor = Color.white;
		amountText = GUI.TextField(val2, amountText, 10, val3);
		if ((int)Event.current.type == 7 && Pointer.Clicked && val2.Contains(Pointer.Pos))
		{
			Pointer.ConsumeClick();
			NativeEditKeyboard.Open("SoulWeaponLevel", amountText, (TouchScreenKeyboardType)4, 10, selectAll: true);
		}
		int num3 = Math.Min(State.fragments, NativeSoulWeaponWz2Service.RemainingExp(soulGrowth));
		int.TryParse(amountText, out var result);
		if (Button(modalRect, "LevelUp/AmountControlArea/$btUp", 0f, 0f, result < num3))
		{
			amountText = Math.Min(num3, Math.Max(0, result) + 1).ToString();
		}
		if (Button(modalRect, "LevelUp/AmountControlArea/$btDown", 0f, 0f, result > 1))
		{
			amountText = Math.Max(1, result - 1).ToString();
		}
		if (Button(modalRect, "LevelUp/AmountControlArea/$btMax", 0f, 0f, num3 > 0))
		{
			amountText = num3.ToString();
		}
		if (Button(modalRect, "LevelUp/$btOK", 0f, 0f, result > 0 && result <= num3) && Send(SoulOperation.LevelModern, result))
		{
			CloseModal();
		}
		if (Button(modalRect, "LevelUp/btClose"))
		{
			CloseModal();
		}
	}

	private static string ConditionText(int quest)
	{
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, object> boss in SoulWeaponWz2Catalog.Bosses)
		{
			if (boss.Value is Dictionary<string, object> dictionary && SoulWeaponWz2Catalog.Int(dictionary, "QuestID_GradeUp") == quest)
			{
				list.Add(SoulWeaponWz2Catalog.BossName(dictionary));
			}
		}
		if (list.Count != 0)
		{
			return string.Join(" / ", list) + " 单人击破";
		}
		return "已达最高阶级";
	}

	private static void ShowBosses()
	{
		List<string> list = new List<string>();
		string text = "";
		foreach (KeyValuePair<string, object> boss in SoulWeaponWz2Catalog.Bosses)
		{
			Dictionary<string, object> dictionary = boss.Value as Dictionary<string, object>;
			int num = SoulWeaponWz2Catalog.Int(dictionary, "BossIcon");
			bool flag = State.week == NativeWeeklyGemstoneService.Week(DateTimeOffset.UtcNow) && State.weekly.TryGetValue(num.ToString(), out var value) && value >= SoulWeaponWz2Catalog.Int(dictionary, "FragmentAmount");
			text = text + SoulWeaponWz2Catalog.BossName(dictionary) + "：" + SoulWeaponWz2Catalog.Int(dictionary, "FragmentAmount") + "个" + (flag ? "（本周已记录）" : "") + "\n";
			if (text.Split('\n').Length >= 7)
			{
				list.Add(text);
				text = "";
			}
		}
		if (text != "")
		{
			list.Add(text);
		}
		NativeUtilPrompt.Pages("灵魂碎片获得记录", list.ToArray());
	}
}
