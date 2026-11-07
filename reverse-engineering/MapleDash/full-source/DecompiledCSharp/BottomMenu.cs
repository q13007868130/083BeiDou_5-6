using UnityEngine;

public static class BottomMenu
{
	public const int None = -1;

	public const int Catalog = 0;

	public const int Interface = 1;

	private static readonly string[] CatalogButtons = new string[4] { "BtChannel", "BtGameOpt", "BtSysOpt", "BtQuit" };

	private static readonly string[] InterfaceButtons = new string[11]
	{
		"BtItem", "BtEquip", "BtStat", "BtSkill", "BtParty", "BtQuest", "BtMessenger", "BtGuild", "BtComm", "BtMobbook",
		"BtRanking"
	};

	private static readonly string[] CatalogNames = new string[4] { "频道变更", "游戏设置", "系统设置", "游戏结束" };

	private static readonly string[] InterfaceNames = new string[11]
	{
		"物品", "装备", "能力值", "技能", "组队", "任务", "好友", "公会", "社区", "怪物图鉴",
		"我的排行"
	};

	public static int Open { get; private set; } = -1;

	public static bool AnyOpen => Open != -1;

	public static bool IsOpen(int kind)
	{
		return Open == kind;
	}

	public static void Toggle(int kind)
	{
		Open = ((Open == kind) ? (-1) : kind);
	}

	public static void Show(int kind)
	{
		Open = kind;
	}

	public static void Close()
	{
		Open = -1;
	}

	private static string[] Buttons(int kind)
	{
		if (kind != 0)
		{
			return InterfaceButtons;
		}
		return CatalogButtons;
	}

	private static int Height(int kind)
	{
		if (kind != 0)
		{
			return 245;
		}
		return 140;
	}

	private static int Pitch(int kind)
	{
		if (kind != 0)
		{
			return 21;
		}
		return 26;
	}

	private static int AnchorButton(int kind)
	{
		if (kind != 0)
		{
			return 3;
		}
		return 2;
	}

	private static string MenuArt(int kind, string node)
	{
		return "ui/" + ((kind == 0) ? "gamemenu" : "shortcut") + "/" + node + ".bytes";
	}

	private static int FirstRow(int kind)
	{
		if (kind != 0)
		{
			return 5;
		}
		return 24;
	}

	private static bool Enabled(int kind, int index)
	{
		return true;
	}

	private static string DisabledHint(int kind, int index)
	{
		return "";
	}

	private static void Click(int kind, int index)
	{
		if (kind == 1)
		{
			ClickInterface(index);
			return;
		}
		switch (index)
		{
		case 0:
			ChannelWindow.Toggle();
			break;
		case 1:
			GameOptWindow.Toggle();
			break;
		case 2:
			SysOptWindow.Toggle();
			break;
		case 3:
			ConfirmQuit();
			break;
		}
	}

	private static void ClickInterface(int index)
	{
		if (index == 0)
		{
			InvenWindow.Toggle();
		}
		if (index == 1)
		{
			EquipWindow.Toggle();
		}
		if (index == 2)
		{
			StatWindow.Toggle();
		}
		if (index == 3)
		{
			SkillWindow.Toggle();
		}
		if (index == 4)
		{
			NativeSocialWindow.Toggle(GameAction.Party);
		}
		if (index == 5)
		{
			QuestWindow.Toggle();
		}
		if (index == 6)
		{
			NativeSocialWindow.Toggle(GameAction.Messenger);
		}
		if (index == 7)
		{
			NativeGuildWindow.Toggle();
		}
		if (index == 8)
		{
			NativeSocialWindow.Toggle(GameAction.Buddy);
		}
		if (index == 9)
		{
			NativeMonsterBook.Toggle();
		}
		if (index == 10)
		{
			NativeRankingWindow.Toggle();
		}
	}

	private static void ConfirmQuit()
	{
		NativeUtilPrompt.Confirm("确定要结束游戏吗？", (bool ok) =>
		{
			if (ok)
			{
				if ((Object)(object)GameLoop.I != (Object)null)
				{
					GameLoop.I.RequestExitToLogin();
				}
				else
				{
					Application.Quit();
				}
			}
		});
	}

	public static Rect PanelRect(int kind)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		float s = BottomBar.S;
		Rect val = BottomBar.BigButtonRect(AnchorButton(kind));
		float num = 93f * s;
		float num2 = (float)Height(kind) * s;
		float num3 = Mathf.Clamp(val.xMax - num, 0f, Mathf.Max(0f, UiKit.W - num));
		float num4 = Mathf.Clamp(val.y - num2, 0f, Mathf.Max(0f, UiKit.H - num2));
		return new Rect(num3, num4, num, num2);
	}

	public static Rect ItemRect(int kind, int index)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		Rect val = PanelRect(kind);
		float s = BottomBar.S;
		return new Rect(val.x + 6f * s, val.y + (float)(FirstRow(kind) + index * Pitch(kind)) * s, 81f * s, 25f * s);
	}

	public static int ItemCount(int kind)
	{
		return Buttons(kind).Length;
	}

	public static bool ItemEnabled(int kind, int index)
	{
		return Enabled(kind, index);
	}

	public static string NameOf(int kind, int index)
	{
		string[] array = ((kind == 0) ? CatalogNames : InterfaceNames);
		if (index < 0 || index >= array.Length)
		{
			return "?";
		}
		return array[index];
	}

	public static void Draw(int kind)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Invalid comparison between Unknown and I4
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Invalid comparison between Unknown and I4
		if (!IsOpen(kind))
		{
			return;
		}
		Rect val = PanelRect(kind);
		Texture2D val2 = Art.Tex(MenuArt(kind, "backgrnd"));
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(val, (Texture)(object)val2, (ScaleMode)0);
		}
		string[] array = Buttons(kind);
		bool flag = NpcSession.Blocking || VirtualControlsEditor.Visible;
		for (int i = 0; i < array.Length; i++)
		{
			Rect val3 = ItemRect(kind, i);
			bool flag2 = Enabled(kind, i);
			bool flag3 = val3.Contains(Pointer.Pos) && !flag;
			string text;
			if (flag2)
			{
				if (flag3)
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
			Texture2D val4 = Art.Tex(MenuArt(kind, array[i] + "_" + text)) ?? Art.Tex(MenuArt(kind, array[i] + "_normal"));
			if ((Object)(object)val4 != (Object)null)
			{
				GUI.DrawTexture(val3, (Texture)(object)val4, (ScaleMode)0);
			}
			if (flag3 && (int)Event.current.type == 7 && Pointer.Released)
			{
				Pointer.ConsumeClick();
				if (flag2)
				{
					AudioManager.Play("UI/BtMouseClick", 0.7f, pitchVary: false);
					Close();
					Click(kind, i);
					return;
				}
				BottomBar.Toast(DisabledHint(kind, i));
			}
		}
		if (flag || (int)Event.current.type != 7 || !Pointer.Clicked || val.Contains(Pointer.Pos))
		{
			return;
		}
		Rect val5 = BottomBar.BigButtonRect(2);
		if (!val5.Contains(Pointer.Pos))
		{
			val5 = BottomBar.BigButtonRect(3);
			if (!val5.Contains(Pointer.Pos))
			{
				Close();
			}
		}
	}
}
