using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using MapleDash.Wz;
using UnityEngine;

public static class ItemTooltip
{
	private sealed class SetHeader
	{
		public string Title;

		public string Count;

		public bool Lucky;
	}

	private sealed class SetRow
	{
		public string part;

		public string text;

		public string status;

		public Color32 partColor;

		public Color32 textColor;

		public Color32 statusColor;

		public bool guide;

		public bool separator;

		public bool spacer;

		public bool highlight;

		public bool lucky;

		public bool tierLabel;

		public float height = 15f;
	}

	private static int hoverId;

	private static bool hasHover;

	private static InventoryEntry hoverItem;

	private static Texture2D white;

	private const string Atlas = "UI/UIWindow.img/ToolTip/Equip/";

	private const string LCategory = "装备分类 :";

	private const string LAttackSpeed = "攻击速度 :";

	private const string LUpgrades = "可升级次数 :";

	private const string LPowerGain = "战斗力提升量";

	private const string LQuestItem = "任务道具";

	private static readonly string[] StatLabels = new string[15]
	{
		"力量 :", "敏捷 :", "智力 :", "运气 :", "HP :", "MP :", "攻击力 :", "魔法力 :", "防御力 :", "魔法防御力 :",
		"命中率 :", "回避率 :", "手技 :", "移动速度 :", "跳跃力 :"
	};

	private static readonly string[] SpeedNames = new string[13]
	{
		"非常快", "非常快", "比较快", "比较快", "快", "快", "普通", "慢", "慢", "比较慢",
		"比较慢", "非常慢", "非常慢"
	};

	private static readonly string[] WeaponNames = new string[20]
	{
		"单手剑", "单手斧", "单手钝器", "短刀", "", "", "", "短杖", "长杖", "",
		"双手剑", "双手斧", "双手钝器", "枪", "矛", "弓", "弩", "拳套", "指节", "短枪"
	};

	private const string EquipAtlas = "UI/UIToolTip.img/Item/Equip/";

	private static ImgNode equipmentSkin;

	public const float SetPanelWidth = 298f;

	public const float SetPanelPitch = 302f;

	private const float SetLineSpace = 15f;

	private const float SetTitleH = 30f;

	private const float SetBtmH = 12f;

	private const float SetPartX = 14f;

	private const float SetNameX = 89f;

	private const float SetStatusRight = 283f;

	private const float SetFirstRowY = 48f;

	private const float SetItemBottomMargin = 7f;

	private const float SetEffectMargin = 7f;

	private const float SetListBottomMargin = 12f;

	private static readonly Color32 SetWhite = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

	private static readonly Color32 SetGray = new Color32((byte)183, (byte)191, (byte)197, byte.MaxValue);

	private static readonly Color32 SetDarkGray = new Color32((byte)133, (byte)145, (byte)159, byte.MaxValue);

	private static readonly Color32 SetEmphasis = new Color32(byte.MaxValue, (byte)204, (byte)0, byte.MaxValue);

	private static readonly Color32 SetRed = new Color32(byte.MaxValue, (byte)138, (byte)24, byte.MaxValue);

	private static readonly Color32 SetFrameBody = new Color32((byte)46, (byte)53, (byte)61, byte.MaxValue);

	private static int setCacheKey = int.MinValue;

	private static List<AdminSetItem.SetPanel> setCache;

	private static readonly List<int> wornBuffer = new List<int>();

	private const float SetFooterH = 24f;

	private static readonly Dictionary<int, Texture2D> frames = new Dictionary<int, Texture2D>();

	private static Texture2D White
	{
		get
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Expected Obj, but got Unknown
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)white == (Object)null)
			{
				white = new Texture2D(1, 1);
				white.SetPixel(0, 0, Color.white);
				white.Apply();
			}
			return white;
		}
	}

	public static bool HasHover => hasHover;

	public static int HoverId
	{
		get
		{
			if (!hasHover)
			{
				return 0;
			}
			return hoverId;
		}
	}

	public static Rect LastEquipRect
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

	public static Nullable LastSetRect { get; private set; }

	public static void Reset()
	{
		hasHover = false;
	}

	public static void SetHover(int id, bool equipped = false, InventoryEntry instance = null)
	{
		if (id <= 0)
		{
			return;
		}
		hoverItem = instance;
		if (instance == null && GameSession.Character != null)
		{
			foreach (InventoryEntry item in GameSession.Character.inventory)
			{
				if (item.itemId == id && item.position < 0 == equipped)
				{
					hoverItem = item;
					break;
				}
			}
		}
		hoverId = id;
		hasHover = true;
	}

	public static void Flush()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Invalid comparison between Unknown and I4
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		if (hasHover && hoverId > 0 && (int)Event.current.type == 7)
		{
			Draw(hoverId, Pointer.Pos);
			hasHover = false;
		}
	}

	public static void DrawForStatus(int id)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		Draw(id, Pointer.Pos);
	}

	public static void PreviewDraw(int id, Vector2 anchor, InventoryEntry instance)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		if (id > 0)
		{
			hoverItem = instance;
			hoverId = id;
			Draw(id, anchor);
		}
	}

	private static void Draw(int id, Vector2 anchor)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		LastSetRect = null;
		if (ItemCatalog.IsEquip(id))
		{
			DrawEquipment(id, anchor);
		}
		else
		{
			DrawCommon(id, anchor);
		}
	}

	private static void Canvas(Rect rect, string node, float x, float y, float s)
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		string text = "UI/UIWindow.img/ToolTip/Equip/" + node;
		Texture2D val = WzAssets.Texture(text);
		if (!((Object)(object)val == (Object)null))
		{
			ImgNode imgNode = WzAssets.Node(text)?.Get("origin");
			GUI.DrawTexture(new Rect(rect.x + (x - (float)(imgNode?.X ?? 0)) * s, rect.y + (y - (float)(imgNode?.Y ?? 0)) * s, (float)((Texture)val).width * s, (float)((Texture)val).height * s), (Texture)(object)val);
		}
	}

	private static void DrawEquipment(int id, Vector2 anchor)
	{
		//IL_1b64: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b6b: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b72: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cc0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cc5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cd0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cd8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cdf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ce6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ce7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cf1: Expected Obj, but got Unknown
		//IL_0cf1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cf2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cfc: Expected Obj, but got Unknown
		//IL_0cfe: Expected Obj, but got Unknown
		//IL_0d05: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d11: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d16: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d1e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d25: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d2e: Expected Obj, but got Unknown
		//IL_0d30: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d37: Expected Obj, but got Unknown
		//IL_0d52: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d57: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d6f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d7e: Expected Obj, but got Unknown
		//IL_0f7f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f8e: Expected Obj, but got Unknown
		//IL_0fea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ff9: Expected Obj, but got Unknown
		//IL_1051: Unknown result type (might be due to invalid IL or missing references)
		//IL_1060: Expected Obj, but got Unknown
		//IL_120c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1238: Unknown result type (might be due to invalid IL or missing references)
		//IL_1283: Unknown result type (might be due to invalid IL or missing references)
		//IL_1294: Unknown result type (might be due to invalid IL or missing references)
		//IL_129e: Unknown result type (might be due to invalid IL or missing references)
		//IL_12a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_12a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_12aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_12ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_12b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_12c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_12ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_12d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_12e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_12ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_12fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_12ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_1304: Unknown result type (might be due to invalid IL or missing references)
		//IL_133c: Unknown result type (might be due to invalid IL or missing references)
		//IL_135c: Unknown result type (might be due to invalid IL or missing references)
		//IL_137f: Unknown result type (might be due to invalid IL or missing references)
		//IL_13cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_1446: Unknown result type (might be due to invalid IL or missing references)
		//IL_144b: Unknown result type (might be due to invalid IL or missing references)
		//IL_1454: Expected Obj, but got Unknown
		//IL_146b: Unknown result type (might be due to invalid IL or missing references)
		//IL_1470: Unknown result type (might be due to invalid IL or missing references)
		//IL_148e: Unknown result type (might be due to invalid IL or missing references)
		//IL_14bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_14db: Unknown result type (might be due to invalid IL or missing references)
		//IL_1563: Unknown result type (might be due to invalid IL or missing references)
		//IL_1568: Unknown result type (might be due to invalid IL or missing references)
		//IL_156f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1577: Unknown result type (might be due to invalid IL or missing references)
		//IL_1580: Expected Obj, but got Unknown
		//IL_158e: Unknown result type (might be due to invalid IL or missing references)
		//IL_15f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_15fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_1603: Unknown result type (might be due to invalid IL or missing references)
		//IL_160f: Expected Obj, but got Unknown
		//IL_15c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_16cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_16d6: Expected Obj, but got Unknown
		//IL_16d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_1706: Unknown result type (might be due to invalid IL or missing references)
		//IL_1732: Unknown result type (might be due to invalid IL or missing references)
		//IL_175e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1779: Unknown result type (might be due to invalid IL or missing references)
		//IL_1782: Unknown result type (might be due to invalid IL or missing references)
		//IL_1787: Unknown result type (might be due to invalid IL or missing references)
		//IL_1793: Expected Obj, but got Unknown
		//IL_1555: Unknown result type (might be due to invalid IL or missing references)
		//IL_17bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_17cb: Expected Obj, but got Unknown
		//IL_17e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_17fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_1644: Unknown result type (might be due to invalid IL or missing references)
		//IL_1649: Unknown result type (might be due to invalid IL or missing references)
		//IL_1650: Unknown result type (might be due to invalid IL or missing references)
		//IL_165d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1666: Expected Obj, but got Unknown
		//IL_184a: Unknown result type (might be due to invalid IL or missing references)
		//IL_168c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1680: Unknown result type (might be due to invalid IL or missing references)
		//IL_1685: Unknown result type (might be due to invalid IL or missing references)
		//IL_16ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_18b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b54: Unknown result type (might be due to invalid IL or missing references)
		//IL_1982: Unknown result type (might be due to invalid IL or missing references)
		//IL_1989: Expected Obj, but got Unknown
		//IL_1b90: Unknown result type (might be due to invalid IL or missing references)
		//IL_1a34: Unknown result type (might be due to invalid IL or missing references)
		//IL_1a39: Unknown result type (might be due to invalid IL or missing references)
		//IL_19a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_19a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ad0: Unknown result type (might be due to invalid IL or missing references)
		//IL_1a70: Unknown result type (might be due to invalid IL or missing references)
		//IL_1a75: Unknown result type (might be due to invalid IL or missing references)
		if (hoverItem != null && GameSession.Character != null)
		{
			hoverItem = GameSession.Character.inventory.Find((InventoryEntry x) => x.itemRowId == hoverItem.itemRowId) ?? hoverItem;
		}
		ImgNode imgNode = EquipCatalog.Info(id);
		if (imgNode == null)
		{
			return;
		}
		float s = BottomBar.S;
		List<string[]> list = new List<string[]>();
		int num = -1;
		int num2 = id / 10000;
		string text = ((num2 >= 130 && num2 <= 149 && num2 - 130 < WeaponNames.Length) ? WeaponNames[num2 - 130] : null);
		if (string.IsNullOrEmpty(text))
		{
			switch (num2)
			{
			case 100:
				text = "帽子";
				break;
			case 101:
				text = "脸饰";
				break;
			case 102:
				text = "眼饰";
				break;
			case 103:
				text = "耳环";
				break;
			case 104:
				text = "上衣";
				break;
			case 105:
				text = "套服";
				break;
			case 106:
				text = "裤/裙";
				break;
			case 107:
				text = "鞋子";
				break;
			case 108:
				text = "手套";
				break;
			case 109:
				text = "盾牌";
				break;
			case 110:
				text = "披风";
				break;
			case 111:
				text = "戒指";
				break;
			case 112:
				text = "坠子";
				break;
			case 113:
				text = "腰带";
				break;
			case 114:
				text = "勋章";
				break;
			}
		}
		if (num2 >= 130 && num2 <= 149 && imgNode.Get("attackSpeed") != null)
		{
			int num3 = imgNode.Int("attackSpeed");
			if (num3 >= 0 && num3 < SpeedNames.Length)
			{
				list.Add(new string[2]
				{
					"攻击速度 :",
					" " + SpeedNames[num3]
				});
			}
		}
		ItemStats itemStats = ((hoverItem != null) ? ItemStats.Of(hoverItem) : ItemStats.Template(id));
		string[] statLabels = StatLabels;
		InventoryEntry inventoryEntry = hoverItem;
		int[] array = ((inventoryEntry == null || !inventoryEntry.isEquip) ? null : new int[15]
		{
			hoverItem.str, hoverItem.dex, hoverItem.int_, hoverItem.luk, hoverItem.hp, hoverItem.mp, hoverItem.watk, hoverItem.matk, hoverItem.wdef, hoverItem.mdef,
			hoverItem.acc, hoverItem.avoid, hoverItem.hands, hoverItem.speed, hoverItem.jump
		});
		bool flag = hoverItem?.equipmentData != null && hoverItem.equipmentData.TryGetValue("enhancementVersion", out var value) && Convert.ToInt32(value) == 282;
		bool starForceVestige = itemStats.starForceVestige;
		int num4 = imgNode.Int("reqLevel");
		if (hoverItem?.equipmentData != null && hoverItem.equipmentData.TryGetValue("iIncReq", out var value2))
		{
			num4 += Convert.ToInt32(value2);
		}
		for (int num5 = 0; num5 < statLabels.Length; num5++)
		{
			int num6 = itemStats.baseStats[num5];
			int num7 = itemStats.starStats[num5];
			int num8 = itemStats.flameStats[num5];
			int num9 = num6 + num7 + num8;
			if (num9 <= 0)
			{
				continue;
			}
			string text2 = " +" + num9;
			if (num7 != 0 || num8 != 0)
			{
				text2 = text2 + " (" + num6;
				if (num7 != 0)
				{
					text2 = text2 + " <color=#FFCC00>+" + num7 + "</color>";
				}
				if (num8 != 0)
				{
					text2 = text2 + " <color=#0AE3AD>+" + num8 + "</color>";
				}
				text2 += ")";
			}
			list.Add(new string[2]
			{
				statLabels[num5],
				text2
			});
		}
		if (flag)
		{
			list.Add(new string[2]
			{
				starForceVestige ? "装备痕迹 :" : "星力强化 :",
				" " + hoverItem.starLevel + "星"
			});
			if (!starForceVestige)
			{
				list.Add(new string[2]
				{
					"星力攻击力 :",
					" +" + itemStats.starStats[6]
				});
				list.Add(new string[2]
				{
					"星力魔法力 :",
					" +" + itemStats.starStats[7]
				});
			}
		}
		int num10 = ((array == null) ? imgNode.Int("tuc") : hoverItem.upgradeslots);
		if (array != null && num10 <= 0 && hoverItem.equipLevel == 0)
		{
			num10 = Math.Max(num10, imgNode.Int("tuc"));
		}
		if (imgNode.Get("tuc") != null || num10 > 0)
		{
			num = num10;
			list.Add(new string[2]
			{
				"可升级次数 :",
				" " + num10
			});
		}
		long num11 = 0L;
		bool flag2 = CombatPower.IsEquippable(id);
		bool flag3 = CombatPower.TryPreview(hoverItem, out var current, out var after);
		if (flag3)
		{
			num11 = after - current;
		}
		if (hoverItem?.equipmentData != null)
		{
			int optionLevel = PotentialCatalog.OptionLevel(imgNode.Int("reqLevel") + Read("iIncReq"));
			string[] array2 = new string[5] { "", "特殊", "稀有", "罕见", "传说" };
			bool[] array3 = new bool[2] { false, true };
			foreach (bool flag4 in array3)
			{
				string text3 = (flag4 ? "bonusOption" : "potentialOption");
				int val = Read(flag4 ? "bonusGrade" : "potentialGrade");
				if (Read(text3 + "1") == 0)
				{
					continue;
				}
				list.Add(new string[2]
				{
					flag4 ? "附加潜能 :" : "主潜能 :",
					" " + array2[Math.Max(0, Math.Min(4, val))]
				});
				for (int num13 = 1; num13 <= 3; num13++)
				{
					int num14 = Read(text3 + num13);
					if (num14 != 0)
					{
						list.Add(new string[2]
						{
							(num14 < 0) ? "未鉴定潜能" : PotentialCatalog.Shared.Describe(num14, optionLevel),
							""
						});
					}
				}
			}
		}
		if (hoverItem != null && SymbolCatalog.IsSymbol(hoverItem.itemId))
		{
			SymbolInfo symbolInfo = SymbolCatalog.Of(hoverItem.itemId);
			if (symbolInfo != null)
			{
				int level = ((hoverItem.symbolLevel < 1) ? 1 : hoverItem.symbolLevel);
				int num15 = symbolInfo.GrowthCost(level);
				list.Add(new string[2]
				{
					"徽章等级 :",
					" Lv" + level + " / " + symbolInfo.maxLevel
				});
				list.Add(new string[2]
				{
					"徽章经验 :",
					" " + hoverItem.symbolExp + " / " + num15
				});
				list.Add(new string[2]
				{
					symbolInfo.IsArcane ? "ARC :" : "AUT :",
					" +" + hoverItem.symbolInc
				});
				string text4 = NativeSymbolService.MainStatColumn(Profile.JobId);
				int num16 = text4 switch
				{
					"hp" => hoverItem.hp, 
					"luk" => hoverItem.luk, 
					"int" => hoverItem.int_, 
					"dex" => hoverItem.dex, 
					_ => hoverItem.str, 
				};
				if (num16 > 0)
				{
					list.Add(new string[2]
					{
						(text4 == "hp") ? "HP增加 :" : "主属性增加 :",
						" +" + num16
					});
				}
			}
		}
		else if (hoverItem != null)
		{
			foreach (FlameLine item in NativeGrowthStats.DescribeFlame(hoverItem))
			{
				if (item.value > 0 && item.statVal >= 17)
				{
					list.Add(new string[2]
					{
						"星火 · " + item.label + " :",
						" +" + item.value
					});
				}
			}
			if (!string.IsNullOrEmpty(hoverItem.flameChoice))
			{
				list.Add(new string[2] { "星火：", " 有待确认的结果" });
			}
			SoulWeaponWz2State soulWeaponWz2State = GameSession.Character?.soulWeapon;
			SoulGrowth soulGrowth = soulWeaponWz2State?.Of(hoverItem.itemRowId);
			if (soulGrowth != null)
			{
				bool flag5 = soulWeaponWz2State.weapon == hoverItem.itemRowId;
				bool flag6 = flag5 && soulWeaponWz2State.Equipped(GameSession.Character);
				list.Add(new string[2]
				{
					"灵魂武器 · ",
					"第" + soulGrowth.grade + "阶段"
				});
				list.Add(new string[2]
				{
					"",
					"Lv. " + soulGrowth.level + "（攻击力／魔法力：+" + SoulWeaponWz2Catalog.PassiveAttack(soulGrowth.level) + "）"
				});
				string text5 = SoulWeaponWz2Catalog.DescribeOption(soulGrowth.option);
				if (soulGrowth.option > 0)
				{
					list.Add(new string[2] { "", text5 });
				}
				string text6;
				if (flag6)
				{
					text6 = "可使用";
				}
				else
				{
					text6 = (flag5 ? "装备后可使用" : "启用并装备后可使用");
				}
				list.Add(new string[2]
				{
					"",
					text6 + "「" + SoulWeaponWz2Catalog.SkillName(soulGrowth.grade) + "」技能"
				});
			}
			if (hoverItem.soulSocketId > 0)
			{
				list.Add(new string[2] { "灵魂插槽 :", " 已开启" });
				if (hoverItem.soulItemId > 0)
				{
					list.Add(new string[2]
					{
						"灵魂 :",
						" " + ItemCatalog.Name(hoverItem.soulItemId)
					});
				}
				SoulOption soulOption = SoulCatalog.Of(hoverItem.soulOption);
				if (soulOption != null && !string.IsNullOrEmpty(soulOption.key))
				{
					list.Add(new string[2]
					{
						"灵魂属性 :",
						" " + soulOption.key + " +" + soulOption.value
					});
				}
			}
		}
		string text7 = ItemCatalog.Name(id);
		if (string.IsNullOrEmpty(text7))
		{
			text7 = WzNames.Item(id);
		}
		if (string.IsNullOrEmpty(text7))
		{
			text7 = id.ToString();
		}
		int num17 = ((hoverItem != null) ? hoverItem.starLevel : StarFromName(text7));
		int num18 = ((imgNode.Int("cash") == 0 && imgNode.Int("tuc") > 0 && imgNode.Int("exceptUpgrade") == 0 && imgNode.Int("exUpgradeBlock") == 0) ? StarForceRules.Shared.Cap(id, num4, imgNode.Int("superiorEqp") != 0) : 0);
		if (num17 > num18)
		{
			num18 = num17;
		}
		float num19;
		if (num18 <= 0)
		{
			num19 = 0f;
		}
		else
		{
			num19 = ((num18 > 15) ? 42f : 27f);
		}
		GUIStyle val2 = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = 12,
			wordWrap = true,
			richText = true,
			padding = new RectOffset(),
			margin = new RectOffset()
		};
		val2.normal.textColor = Color.white;
		GUIStyle val3 = new GUIStyle(val2)
		{
			fontSize = 14,
			fontStyle = (FontStyle)1,
			alignment = (TextAnchor)1
		};
		GUIStyle val4 = new GUIStyle(val2);
		val4.normal.textColor = Color32.op_Implicit(new Color32((byte)183, (byte)191, (byte)197, byte.MaxValue));
		float num20 = 6f + num19;
		float num21 = val3.CalcHeight(new GUIContent(text7), 294f);
		float num22 = num20 + num21 + 22f;
		float num23 = num22 + 82f;
		List<string[]> list2 = new List<string[]>();
		list2.Add(new string[2]
		{
			"装备种类",
			text ?? "装备"
		});
		list2.Add(new string[2]
		{
			"要求等级",
			"Lv. " + num4
		});
		string[] array4 = new string[5] { "reqSTR", "reqDEX", "reqINT", "reqLUK", "reqPOP" };
		string[] array5 = new string[5] { "力量", "敏捷", "智力", "运气", "人气" };
		for (int num24 = 0; num24 < array4.Length; num24++)
		{
			if (imgNode.Int(array4[num24]) > 0)
			{
				list2.Add(new string[2]
				{
					array5[num24],
					imgNode.Int(array4[num24]).ToString()
				});
			}
		}
		int num25 = imgNode.Int("reqJob");
		string[] array6 = new string[5] { "战士", "魔法师", "弓箭手", "飞侠", "海盗" };
		string text8 = num25 switch
		{
			-1 => "新手", 
			0 => "全职业", 
			_ => "", 
		};
		int num26 = 0;
		while (num25 > 0 && num26 < array6.Length)
		{
			if ((num25 & (1 << num26)) != 0)
			{
				text8 = text8 + ((text8.Length > 0) ? "／" : "") + array6[num26];
			}
			num26++;
		}
		list2.Add(new string[2] { "可用职业", text8 });
		float num27 = 0f;
		foreach (string[] item2 in list2)
		{
			num27 += Mathf.Max(16f, val2.CalcHeight(new GUIContent(item2[1]), 209f));
		}
		num23 += num27 + 8f;
		List<float> list3 = new List<float>();
		foreach (string[] item3 in list)
		{
			float num28 = Mathf.Max(16f, val2.CalcHeight(new GUIContent(item3[0] + item3[1]), 279f));
			if (PotentialHeader(item3[0]))
			{
				num28 += 9f;
			}
			list3.Add(num28);
		}
		string text9 = FormatDescription(ItemCatalog.Desc(id));
		float num29 = (string.IsNullOrEmpty(text9) ? 0f : (val2.CalcHeight(new GUIContent(text9), 294f) + 8f));
		List<SetRow> list4 = SetRowsFor(id, out var header);
		List<int> list5 = new List<int>();
		List<float> list6 = new List<float>();
		List<float> list7 = new List<float>();
		float num30 = 0f;
		int num31 = 0;
		List<int[]> list8 = null;
		s = Mathf.Max(0.01f, Mathf.Min(s, (UiKit.H - 8f) / (num23 + 44f)));
		for (int num32 = 0; num32 < 8; num32++)
		{
			num30 = (UiKit.H - 8f) / s;
			FlowEquipRows(list, list3, num23, num29, num30, list5, list6, list7);
			list8 = ((list4.Count == 0) ? null : SplitSetColumns(list4, num30));
			num31 = list8?.Count ?? 0;
			float num33 = (float)(list7.Count * 328 - 4) + (float)num31 * 302f;
			float num34 = (UiKit.W - 8f) / num33;
			if (s <= num34 + 0.0001f)
			{
				break;
			}
			s = Mathf.Max(0.01f, num34);
		}
		float num35 = list7.Count * 328 - 4;
		float num36 = num35 + (float)num31 * 302f;
		float num37 = 0f;
		foreach (float item4 in list7)
		{
			num37 = Mathf.Max(num37, item4);
		}
		if (list8 != null)
		{
			foreach (int[] item5 in list8)
			{
				num37 = Mathf.Max(num37, ColumnHeight(list4, item5[0], item5[1]));
			}
		}
		float num38 = Mathf.Clamp(anchor.x, 4f, Mathf.Max(4f, UiKit.W - num36 * s - 4f));
		float num39 = Mathf.Clamp(anchor.y + 20f * s, 4f, Mathf.Max(4f, UiKit.H - num37 * s - 4f));
		Rect val5 = new Rect(num38, num39, 324f * s, list7[0] * s);
		LastEquipRect = new Rect(num38, num39, num35 * s, num37 * s);
		Matrix4x4 matrix = GUI.matrix;
		Color color = GUI.color;
		Color contentColor = GUI.contentColor;
		bool enabled = GUI.enabled;
		GUI.enabled = true;
		GUI.color = Color.white;
		GUI.contentColor = Color.white;
		try
		{
			GUI.matrix = matrix * Matrix4x4.TRS(new Vector3(val5.x, val5.y, 0f), Quaternion.identity, new Vector3(s, s, 1f));
			for (int num40 = 0; num40 < list7.Count; num40++)
			{
				float num41 = num40 * 328;
				float num42 = list7[num40];
				EquipTexture("frame/common/mid", new Rect(num41, 0f, 324f, num42));
				EquipTexture("frame/common/top", new Rect(num41, 0f, 324f, 30f));
				EquipTexture("frame/common/btm", new Rect(num41, num42 - 12f, 324f, 12f));
			}
			if (num18 > 0)
			{
				DrawEquipStars(324f, num19, num18, num17, imgNode.Int("superiorEqp") != 0);
			}
			GUI.Label(new Rect(15f, num20, 294f, num21), text7, val3);
			bool num43 = hoverItem != null && (hoverItem.flag & 8) != 0;
			bool flag7 = imgNode.Int("tradeBlock") == 1 || imgNode.Int("accountSharable") == 1 || imgNode.Int("quest") == 1;
			if (num43 || (flag7 && (hoverItem == null || (hoverItem.flag & 0x10) == 0)))
			{
				GUIStyle val6 = new GUIStyle(val2)
				{
					alignment = (TextAnchor)1
				};
				val6.normal.textColor = Color32.op_Implicit(new Color32(byte.MaxValue, (byte)153, (byte)0, byte.MaxValue));
				GUI.Label(new Rect(15f, num20 + num21, 294f, 16f), "无法交换", val6);
			}
			EquipTexture("frame/common/line", new Rect(0f, num22 - 7f, 324f, 3f));
			EquipTexture("itemIcon/base", new Rect(15f, num22, 78f, 78f));
			Texture2D val7 = NativeItemSource.Icon(id);
			ImgNode imgNode2 = NativeItemSource.IconNode(id)?.Get("origin");
			if ((Object)(object)val7 != (Object)null)
			{
				GUI.DrawTexture(new Rect(21f - (float)(2 * (imgNode2?.X ?? 0)), num22 + 72f - (float)(2 * (imgNode2?.Y ?? 0)), (float)(((Texture)val7).width * 2), (float)(((Texture)val7).height * 2)), (Texture)(object)val7);
			}
			GUIStyle val8 = new GUIStyle(val3)
			{
				alignment = (TextAnchor)4,
				fontSize = 18,
				fontStyle = (FontStyle)1
			};
			val8.normal.textColor = val4.normal.textColor;
			if (hoverItem != null && hoverItem.position < 0)
			{
				GUI.Label(new Rect(100f, num22 + 18f, 209f, 32f), "当前穿戴中的装备", val8);
			}
			if (flag2)
			{
				GUI.Label(new Rect(100f, num22 - 1f, 209f, 16f), "战斗力提升量", new GUIStyle(val4)
				{
					alignment = (TextAnchor)2
				});
				if (flag3 && num11 != 0L)
				{
					string text10 = ((num11 > 0) ? "+ " : "- ") + NumberFormat.Compact(Math.Abs(num11));
					GUIStyle val9 = new GUIStyle(val3)
					{
						alignment = (TextAnchor)5,
						fontSize = PowerFontSize(text10),
						fontStyle = (FontStyle)1
					};
					val9.normal.textColor = ((num11 > 0) ? Color.white : Color32.op_Implicit(new Color32((byte)240, (byte)0, (byte)104, byte.MaxValue)));
					GUI.Label(new Rect(100f, num22 + 16f, 209f, 30f), text10, val9);
				}
			}
			string text11 = text ?? "装备";
			float num44 = val4.CalcSize(new GUIContent(text11)).x + 18f;
			float num45 = 309f - num44;
			EquipTexture("frame/common/category/w", new Rect(num45, num22 + 57f, 8f, 14f));
			EquipTexture("frame/common/category/c", new Rect(num45 + 8f, num22 + 57f, num44 - 16f, 14f));
			EquipTexture("frame/common/category/e", new Rect(num45 + num44 - 8f, num22 + 57f, 8f, 14f));
			GUI.Label(new Rect(num45, num22 + 56f, num44, 16f), text11, new GUIStyle(val4)
			{
				alignment = (TextAnchor)1
			});
			float num46 = num22 + 82f;
			foreach (string[] item6 in list2)
			{
				float num47 = Mathf.Max(16f, val2.CalcHeight(new GUIContent(item6[1]), 209f));
				GUI.Label(new Rect(15f, num46, 85f, num47), item6[0], val4);
				GUI.Label(new Rect(100f, num46, 209f, num47), item6[1], val2);
				num46 += num47;
			}
			EquipTexture("frame/common/line", new Rect(0f, num23 - 6f, 324f, 3f));
			float num48 = num23;
			string text12 = null;
			string text13 = null;
			for (int num49 = 0; num49 < list.Count; num49++)
			{
				string[] array7 = list[num49];
				float num50 = list5[num49] * 328;
				num48 = list6[num49];
				bool flag8 = PotentialHeader(array7[0]);
				if (flag8)
				{
					EquipTexture("frame/common/line", new Rect(num50, num48 + 2f, 324f, 3f));
					num48 += 9f;
					text12 = (array7[0].StartsWith("附加") ? "additionalPotential" : "potential");
					int num51 = 0;
					if (hoverItem?.equipmentData != null && hoverItem.equipmentData.TryGetValue((text12 == "potential") ? "potentialGrade" : "bonusGrade", out var value3))
					{
						num51 = Convert.ToInt32(value3);
					}
					text13 = (new string[5] { "normal", "rare", "epic", "unique", "legendary" })[Mathf.Clamp(num51, 0, 4)];
				}
				else if (array7[1].Length != 0)
				{
					text12 = null;
				}
				GUIStyle val10 = new GUIStyle(val2);
				float num52 = 15f;
				if (text12 != null)
				{
					if (flag8)
					{
						val10.normal.textColor = Color32.op_Implicit(PotentialColor(text13));
					}
					EquipNode("textIcon/" + text12 + "/" + (flag8 ? "title/" : "detail/") + text13, num50 + 15f, num48);
					num52 += (float)(flag8 ? 15 : 12);
				}
				else if (array7[0].StartsWith("星火"))
				{
					val10.normal.textColor = Color32.op_Implicit(new Color32((byte)10, (byte)227, (byte)173, byte.MaxValue));
				}
				else if (array7[0] == "可升级次数 :")
				{
					val10.normal.textColor = Color32.op_Implicit(new Color32((byte)175, (byte)173, byte.MaxValue, byte.MaxValue));
					EquipNode("textIcon/scroll/" + ((num > 0) ? "enhanced" : "normal"), num50 + 15f, num48 + 3f);
					num52 += 15f;
				}
				GUI.Label(new Rect(num50 + num52, num48, 309f - num52, list3[num49]), array7[0] + array7[1], val10);
				num48 += list3[num49] - (float)(flag8 ? 9 : 0);
			}
			if (num29 > 0f)
			{
				GUI.Label(new Rect((float)((list7.Count - 1) * 328) + 15f, list7[list7.Count - 1] - num29 - 6f, 294f, num29), text9, val2);
			}
		}
		finally
		{
			GUI.matrix = matrix;
			GUI.color = color;
			GUI.contentColor = contentColor;
			GUI.enabled = enabled;
		}
		DrawSetColumns(new Vector2(num38 + (num35 + 4f) * s, num39), list4, header, list8, s);
		static int Read(string key)
		{
			if (!hoverItem.equipmentData.TryGetValue(key, out var value4))
			{
				return 0;
			}
			return Convert.ToInt32(value4);
		}
	}

	private static void FlowEquipRows(List<string[]> rows, List<float> heights, float firstY, float descriptionHeight, float limit, List<int> columns, List<float> positions, List<float> pageHeights)
	{
		columns.Clear();
		positions.Clear();
		pageHeights.Clear();
		int num = 0;
		float num2 = firstY;
		for (int i = 0; i < rows.Count; i++)
		{
			float num3 = heights[i];
			if (PotentialHeader(rows[i][0]) || rows[i][0].StartsWith("灵魂武器"))
			{
				for (int j = i + 1; j < rows.Count && rows[j][1].Length == 0; j++)
				{
					num3 += heights[j];
				}
			}
			if (num2 + num3 + 12f > limit && num2 > 15f)
			{
				pageHeights.Add(num2 + 12f);
				num++;
				num2 = 15f;
			}
			columns.Add(num);
			positions.Add(num2);
			num2 += heights[i];
		}
		if (descriptionHeight > 0f && num2 + descriptionHeight + 12f > limit)
		{
			pageHeights.Add(num2 + 12f);
			num2 = 15f;
		}
		pageHeights.Add(num2 + descriptionHeight + 12f);
	}

	private static ImgNode EquipSkinNode(string path)
	{
		if (equipmentSkin == null)
		{
			TextAsset val = Resources.Load<TextAsset>("UI/EquipmentTooltip");
			if ((Object)(object)val != (Object)null)
			{
				using ImgFile imgFile = new ImgFile(val.bytes, "UI/UIToolTip.img");
				equipmentSkin = imgFile.Root.Get("Item/Equip");
			}
			else
			{
				equipmentSkin = WzAssets.Node("UI/UIToolTip.img/Item/Equip/".TrimEnd('/'));
			}
		}
		return equipmentSkin?.Get(path)?.TryResolve();
	}

	private static void DrawEquipStars(float width, float height, int capacity, int stars, bool superior)
	{
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		int num = StarForceStars.Rows(capacity);
		float num2 = 4f + (height - StarForceStars.BlockHeight(num)) * 0.5f;
		for (int i = 0; i < num; i++)
		{
			int num3 = Mathf.Min(15, capacity - i * 15);
			float num4 = (width - StarForceStars.RowWidth(num3)) * 0.5f;
			for (int j = 0; j < num3; j++)
			{
				string text;
				if (i * 15 + j < stars)
				{
					text = (superior ? "blue" : "star");
				}
				else
				{
					text = "empty";
				}
				EquipTexture("textIcon/starForce/" + text, new Rect(num4 + (float)(j * 11) + (float)(j / 5 * 10), num2 + (float)(i * 15), 11f, 10f));
			}
		}
	}

	private static Color32 PotentialColor(string grade)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		return grade switch
		{
			"rare" => new Color32((byte)102, byte.MaxValue, byte.MaxValue, byte.MaxValue), 
			"epic" => new Color32((byte)187, (byte)119, byte.MaxValue, byte.MaxValue), 
			"unique" => new Color32(byte.MaxValue, (byte)204, (byte)0, byte.MaxValue), 
			"legendary" => new Color32((byte)204, byte.MaxValue, (byte)0, byte.MaxValue), 
			_ => new Color32((byte)133, (byte)145, (byte)159, byte.MaxValue), 
		};
	}

	private static bool PotentialHeader(string label)
	{
		if (!(label == "主潜能 :"))
		{
			return label == "附加潜能 :";
		}
		return true;
	}

	private static void EquipTexture(string path, Rect rect)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = EquipSkinNode(path);
		if (imgNode != null && !(imgNode.Kind != "Canvas"))
		{
			Texture2D val = WzAssets.Texture(imgNode);
			if ((Object)(object)val != (Object)null)
			{
				GUI.DrawTexture(rect, (Texture)(object)val);
			}
		}
	}

	private static void EquipNode(string path, float x, float y)
	{
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = EquipSkinNode(path);
		if (imgNode != null && !(imgNode.Kind != "Canvas"))
		{
			Texture2D val = WzAssets.Texture(imgNode);
			if (!((Object)(object)val == (Object)null))
			{
				ImgNode imgNode2 = EquipSkinNode(path)?.Get("origin");
				GUI.DrawTexture(new Rect(x - (float)(imgNode2?.X ?? 0), y - (float)(imgNode2?.Y ?? 0), (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
			}
		}
	}

	public static int StarCapacity(int level)
	{
		if (level < 0)
		{
			return 0;
		}
		if (level <= 94)
		{
			return 5;
		}
		if (level <= 107)
		{
			return 8;
		}
		if (level <= 117)
		{
			return 10;
		}
		if (level <= 127)
		{
			return 15;
		}
		if (level <= 137)
		{
			return 20;
		}
		return 25;
	}

	public static int StarFromName(string name)
	{
		if (string.IsNullOrEmpty(name) || name[name.Length - 1] != '★')
		{
			return 0;
		}
		int num = name.Length - 2;
		int num2 = 0;
		int num3 = 1;
		while (num >= 0 && name[num] >= '0' && name[num] <= '9' && num3 <= 10)
		{
			num2 += (name[num] - 48) * num3;
			num3 *= 10;
			num--;
		}
		return num2;
	}

	private static void DrawStarBar(Rect bar, int capacity, int stars, bool superior, float s)
	{
		if (capacity > 0 && StarForceStars.Ready)
		{
			StarForceStars.Draw(bar.x + bar.width * 0.5f, bar.y + bar.height * 0.5f, s, stars, capacity, superior);
		}
	}

	private static List<SetRow> SetRowsFor(int itemId, out SetHeader header)
	{
		//IL_0337: Unknown result type (might be due to invalid IL or missing references)
		//IL_033c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_039a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_042b: Unknown result type (might be due to invalid IL or missing references)
		//IL_042d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0433: Unknown result type (might be due to invalid IL or missing references)
		//IL_0435: Unknown result type (might be due to invalid IL or missing references)
		//IL_043b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0440: Unknown result type (might be due to invalid IL or missing references)
		header = new SetHeader
		{
			Title = "",
			Count = ""
		};
		wornBuffer.Clear();
		if (GameSession.Character != null)
		{
			foreach (InventoryEntry item in GameSession.Character.inventory)
			{
				if (item.position < 0 && item.itemId > 0)
				{
					wornBuffer.Add(item.itemId);
				}
			}
		}
		wornBuffer.Sort();
		int num = itemId;
		foreach (int item2 in wornBuffer)
		{
			num = num * 31 + item2;
		}
		if (num != setCacheKey || setCache == null)
		{
			setCache = AdminSetItem.PanelsFor(itemId, wornBuffer);
			setCacheKey = num;
		}
		List<SetRow> list = new List<SetRow>();
		if (setCache.Count == 0)
		{
			return list;
		}
		AdminSetItem.SetPanel setPanel = setCache[0];
		header.Title = setPanel.Name;
		header.Count = setPanel.EquippedCount + " / " + setPanel.CompleteCount;
		header.Lucky = setPanel.JokerPossible != 0;
		foreach (AdminSetItem.SetPanelSlot slot in setPanel.Slots)
		{
			AdminSetItem.SetPanelAlt setPanelAlt = ChooseAlt(slot, itemId);
			bool flag = setPanelAlt?.Equipped ?? false;
			string text = ((setPanelAlt != null) ? setPanelAlt.Name : "");
			if (!flag && (setPanelAlt == null || setPanelAlt.ItemId != itemId) && slot.Alts.Count > 1 && !string.IsNullOrEmpty(slot.Represent))
			{
				text = slot.Represent;
			}
			if (string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(slot.Represent))
			{
				text = slot.Represent;
			}
			string status;
			if (flag)
			{
				status = "装备中";
			}
			else
			{
				status = ((slot.Alts.Count == 0) ? "(无)" : "未装备");
			}
			Color32 textColor;
			if (flag)
			{
				textColor = SetWhite;
			}
			else
			{
				textColor = ((slot.Alts.Count == 0) ? SetDarkGray : SetGray);
			}
			if (setPanelAlt != null && setPanelAlt.ItemId == itemId)
			{
				textColor = SetEmphasis;
			}
			list.Add(new SetRow
			{
				part = "（" + slot.Part + "）",
				text = text,
				status = status,
				partColor = SetGray,
				textColor = textColor,
				statusColor = (flag ? SetGray : SetDarkGray),
				highlight = (setPanelAlt != null && setPanelAlt.ItemId == itemId),
				lucky = header.Lucky
			});
		}
		if (setPanel.Tiers.Count > 0)
		{
			list.Add(new SetRow
			{
				separator = true,
				height = 10f
			});
			list.Add(new SetRow
			{
				guide = true,
				partColor = SetGray
			});
		}
		bool flag2 = true;
		foreach (AdminSetItem.SetPanelTier tier in setPanel.Tiers)
		{
			if (!flag2)
			{
				list.Add(new SetRow
				{
					spacer = true,
					height = 7f
				});
			}
			flag2 = false;
			bool flag3 = setPanel.EquippedCount >= tier.Required;
			Color32 partColor = (flag3 ? SetEmphasis : SetDarkGray);
			Color32 val = (flag3 ? SetWhite : SetDarkGray);
			bool flag4 = true;
			foreach (string item3 in EffectLines(tier))
			{
				Color32 textColor2 = (item3.EndsWith("（未接入）") ? SetRed : val);
				list.Add(new SetRow
				{
					part = (flag4 ? ("[" + tier.Required + " 件]") : ""),
					text = item3,
					partColor = partColor,
					textColor = textColor2,
					statusColor = SetDarkGray,
					tierLabel = flag4
				});
				flag4 = false;
			}
		}
		return list;
	}

	private static AdminSetItem.SetPanelAlt ChooseAlt(AdminSetItem.SetPanelSlot slot, int itemId)
	{
		if (slot.Alts.Count == 0)
		{
			return null;
		}
		foreach (AdminSetItem.SetPanelAlt alt in slot.Alts)
		{
			if (alt.ItemId == itemId)
			{
				return alt;
			}
		}
		foreach (AdminSetItem.SetPanelAlt alt2 in slot.Alts)
		{
			if (alt2.Equipped)
			{
				return alt2;
			}
		}
		return slot.Alts[0];
	}

	private static IEnumerable<string> EffectLines(AdminSetItem.SetPanelTier tier)
	{
		bool any = false;
		foreach (KeyValuePair<string, int> stat in tier.Stats)
		{
			any = true;
			yield return SetStatName(stat.Key) + " +" + stat.Value + SetStatSuffix(stat.Key);
		}
		foreach (string option in tier.Options)
		{
			any = true;
			yield return option;
		}
		foreach (string skill in tier.Skills)
		{
			any = true;
			yield return skill;
		}
		foreach (string item in tier.Unmapped)
		{
			any = true;
			yield return item;
		}
		if (!any)
		{
			yield return "—";
		}
	}

	public static int PowerFontSize(string text)
	{
		int num = ((!string.IsNullOrEmpty(text)) ? text.Length : 0);
		if (num <= 10)
		{
			return 20;
		}
		if (num <= 16)
		{
			return 16;
		}
		if (num <= 22)
		{
			return 14;
		}
		return 12;
	}

	private static List<int[]> SplitSetColumns(List<SetRow> rows, float maxHeight)
	{
		List<int[]> list = new List<int[]>();
		int i = 0;
		while (i < rows.Count)
		{
			int num = i;
			float num2 = 0f;
			for (; i < rows.Count; i++)
			{
				float num3 = num2 + rows[i].height;
				if (i > num && 48f + num3 + 24f > maxHeight)
				{
					break;
				}
				num2 = num3;
			}
			if (i == num)
			{
				i++;
			}
			list.Add(new int[2]
			{
				num,
				i - num
			});
		}
		return list;
	}

	private static float ColumnHeight(List<SetRow> rows, int start, int count)
	{
		float num = 0f;
		for (int i = 0; i < count; i++)
		{
			num += rows[start + i].height;
		}
		return 48f + num + 24f;
	}

	private static void DrawSetColumns(Vector2 origin, List<SetRow> rows, SetHeader header, List<int[]> columns, float s)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Expected Obj, but got Unknown
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Expected Obj, but got Unknown
		//IL_00db: Expected Obj, but got Unknown
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Expected Obj, but got Unknown
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Expected Obj, but got Unknown
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Expected Obj, but got Unknown
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Expected Obj, but got Unknown
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Expected Obj, but got Unknown
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Unknown result type (might be due to invalid IL or missing references)
		//IL_025b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_030c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_0345: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Expected Obj, but got Unknown
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03af: Unknown result type (might be due to invalid IL or missing references)
		//IL_0454: Unknown result type (might be due to invalid IL or missing references)
		//IL_0459: Unknown result type (might be due to invalid IL or missing references)
		//IL_0482: Unknown result type (might be due to invalid IL or missing references)
		//IL_049d: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_051c: Unknown result type (might be due to invalid IL or missing references)
		if (rows.Count == 0 || columns.Count == 0)
		{
			return;
		}
		float num = 0f;
		foreach (int[] column in columns)
		{
			num = Mathf.Max(num, ColumnHeight(rows, column[0], column[1]));
		}
		LastSetRect = new Rect(origin.x, origin.y, ((float)columns.Count * 302f - 4f) * s, num * s);
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = Mathf.Max(1, Mathf.RoundToInt(12f * s)),
			alignment = (TextAnchor)3,
			clipping = (TextClipping)1,
			padding = new RectOffset(),
			margin = new RectOffset()
		};
		GUIStyle val2 = new GUIStyle(val)
		{
			fontSize = Mathf.Max(1, Mathf.RoundToInt(14f * s)),
			fontStyle = (FontStyle)1
		};
		GUIStyle val3 = new GUIStyle(val2)
		{
			alignment = (TextAnchor)5
		};
		GUIStyle val4 = new GUIStyle(val)
		{
			alignment = (TextAnchor)5
		};
		GUIStyle val5 = new GUIStyle(val)
		{
			fontStyle = (FontStyle)1
		};
		GUIStyle val6 = new GUIStyle(val);
		val6.normal.textColor = Color32.op_Implicit(SetGray);
		for (int i = 0; i < columns.Count; i++)
		{
			int num2 = columns[i][0];
			int num3 = columns[i][1];
			float num4 = origin.x + (float)i * 302f * s;
			float num5 = ColumnHeight(rows, num2, num3);
			EquipTexture("frame/set/mid", new Rect(num4, origin.y + 30f * s, 298f * s, (num5 - 30f - 12f) * s));
			EquipTexture("frame/set/top", new Rect(num4, origin.y, 298f * s, 30f * s));
			EquipTexture("frame/set/btm", new Rect(num4, origin.y + (num5 - 12f) * s, 298f * s, 12f * s));
			SetIcon("textIcon/set/normal", num4 + 14f * s, origin.y + 14f * s, 23f, 11f, s);
			val2.normal.textColor = Color32.op_Implicit(SetWhite);
			GUI.Label(new Rect(num4 + 46f * s, origin.y + 10f * s, 192f * s, 18f * s), header.Title, val2);
			if (header.Lucky)
			{
				float num6 = val2.CalcSize(new GUIContent(header.Title)).x / Mathf.Max(0.01f, s);
				SetIcon("textIcon/set/lucky", num4 + (46f + num6 + 4f) * s, origin.y + 17f * s, 7f, 7f, s);
			}
			val3.normal.textColor = Color32.op_Implicit(SetEmphasis);
			GUI.Label(new Rect(num4 + 240f * s, origin.y + 10f * s, 43f * s, 18f * s), header.Count, val3);
			float num7 = origin.y + 48f * s;
			for (int j = 0; j < num3; j++)
			{
				SetRow setRow = rows[num2 + j];
				if (setRow.separator)
				{
					EquipTexture("frame/set/line", new Rect(num4, num7 + (setRow.height - 3f) * s, 298f * s, 3f * s));
					num7 += setRow.height * s;
					continue;
				}
				if (setRow.spacer)
				{
					num7 += setRow.height * s;
					continue;
				}
				if (setRow.guide)
				{
					DrawGuide(num4 + 14f * s, num7, s, val6);
					num7 += setRow.height * s;
					continue;
				}
				if (setRow.highlight)
				{
					DrawRowBox(num4, num7 - s, s, setRow.lucky);
				}
				GUIStyle val7 = (setRow.tierLabel ? val5 : val);
				val7.normal.textColor = Color32.op_Implicit(setRow.partColor);
				GUI.Label(new Rect(num4 + 14f * s, num7, 72f * s, setRow.height * s), setRow.part, val7);
				val.normal.textColor = Color32.op_Implicit(setRow.textColor);
				GUI.Label(new Rect(num4 + 89f * s, num7, 146f * s, setRow.height * s), setRow.text, val);
				if (setRow.status != null)
				{
					val.normal.textColor = Color32.op_Implicit(setRow.statusColor);
					GUI.Label(new Rect(num4 + 235f * s, num7, 48f * s, setRow.height * s), setRow.status, val4);
				}
				num7 += setRow.height * s;
			}
		}
	}

	private static void SetIcon(string path, float px, float py, float w, float h, float s)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = EquipSkinNode(path);
		if (imgNode != null && !(imgNode.Kind != "Canvas"))
		{
			Texture2D val = WzAssets.Texture(imgNode);
			if (!((Object)(object)val == (Object)null))
			{
				ImgNode imgNode2 = imgNode.Get("origin");
				GUI.DrawTexture(new Rect(px - (float)(imgNode2?.X ?? 0) * s, py - (float)(imgNode2?.Y ?? 0) * s, w * s, h * s), (Texture)(object)val);
			}
		}
	}

	private static void DrawGuide(float px, float py, float s, GUIStyle style)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		EquipTexture("textIcon/set/guide", new Rect(px, py, 61f * s, 12f * s));
		Fill(new Rect(px, py, 48f * s, 12f * s), Color32.op_Implicit(SetFrameBody));
		GUI.Label(new Rect(px, py, 48f * s, 12f * s), "套装效果", style);
	}

	private static void DrawRowBox(float x, float y, float s, bool lucky)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		string text = (lucky ? "frame/set/boxLucky/" : "frame/set/boxNormal/");
		float num = (lucky ? 16f : 14f);
		EquipTexture(text + "w", new Rect(x, y, 8f * s, num * s));
		EquipTexture(text + "c", new Rect(x + 8f * s, y, 282f * s, num * s));
		EquipTexture(text + "e", new Rect(x + 290f * s, y, 8f * s, num * s));
	}

	public static Rect SetPanelRect(Rect equip, float h, float s, float screenW, float screenH)
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		float num = 4f * s;
		float num2 = 298f * s;
		float num3 = h * s;
		float num4 = equip.xMax + num;
		if (num4 + num2 > screenW - 4f)
		{
			num4 = ((!(equip.x - num - num2 >= 4f)) ? Mathf.Clamp(num4, 4f, Mathf.Max(4f, screenW - num2 - 4f)) : (equip.x - num - num2));
		}
		float num5 = Mathf.Clamp(equip.y, 4f, Mathf.Max(4f, screenH - num3 - 4f));
		return new Rect(num4, num5, num2, num3);
	}

	private static string SetStatName(string key)
	{
		if (key == null)
		{
			return "属性";
		}
		if (key.Contains("FinalDamage"))
		{
			return "最终伤害";
		}
		if (key.Contains("BossDamage"))
		{
			return "Boss伤害";
		}
		if (key.Contains("NormalDamage"))
		{
			return "普通伤害";
		}
		if (key.Contains("PAD"))
		{
			return "攻击力";
		}
		if (key.Contains("MAD"))
		{
			return "魔法攻击力";
		}
		if (key.Contains("PDD"))
		{
			return "物理防御力";
		}
		if (key.Contains("MDD"))
		{
			return "魔法防御力";
		}
		if (key.Contains("ACC"))
		{
			return "命中率";
		}
		if (key.Contains("EVA"))
		{
			return "回避率";
		}
		if (key.Contains("HP"))
		{
			return "最大HP";
		}
		if (key.Contains("MP"))
		{
			return "最大MP";
		}
		if (key.Contains("ExpRate"))
		{
			return "经验获得";
		}
		if (key.Contains("DropRate"))
		{
			return "掉落率";
		}
		if (key.Contains("MesoRate"))
		{
			return "金币获得";
		}
		if (key.Contains("STR"))
		{
			return "力量";
		}
		if (key.Contains("DEX"))
		{
			return "敏捷";
		}
		if (key.Contains("INT"))
		{
			return "智力";
		}
		if (key.Contains("LUK"))
		{
			return "幸运";
		}
		if (key.Contains("SPD"))
		{
			return "移动速度";
		}
		if (key.Contains("JMP"))
		{
			return "跳跃力";
		}
		if (key.Contains("IgnoreDefense"))
		{
			return "无视防御率";
		}
		if (key.Contains("CriticalChance"))
		{
			return "暴击率";
		}
		if (key.Contains("CriticalDamage"))
		{
			return "暴击伤害";
		}
		if (key.Contains("StatusRes"))
		{
			return "状态异常耐性";
		}
		if (key.Contains("PvpDamage"))
		{
			return "PvP伤害";
		}
		return "属性";
	}

	private static string SetStatSuffix(string key)
	{
		if (key == null)
		{
			return "";
		}
		if (key.Contains("StatusRes") || key.Contains("BuffDuration"))
		{
			return "";
		}
		if (key.Contains("Pct") || key.Contains("Rate") || key.Contains("Damage") || key.Contains("Chance") || key.Contains("IgnoreDefense"))
		{
			return "%";
		}
		return "";
	}

	private static void DrawCommon(int id, Vector2 anchor)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected Obj, but got Unknown
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected Obj, but got Unknown
		//IL_0046: Expected Obj, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Expected Obj, but got Unknown
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Expected Obj, but got Unknown
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_038b: Unknown result type (might be due to invalid IL or missing references)
		float s = BottomBar.S;
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = Mathf.RoundToInt(12f * s),
			wordWrap = true,
			padding = new RectOffset(),
			margin = new RectOffset()
		};
		val.normal.textColor = Color.white;
		string text = FormatDescription(ItemCatalog.Desc(id));
		val.richText = true;
		ImgNode imgNode = NativeItemSource.Node(id);
		int num;
		float num2;
		if (imgNode == null)
		{
			num = 0;
		}
		else
		{
			num = ((imgNode.Get("info")?.Int("quest") == 1) ? 1 : 0);
			if (num != 0)
			{
				num2 = 19f * s;
				goto IL_00c9;
			}
		}
		num2 = 0f;
		goto IL_00c9;
		IL_00c9:
		float num3 = num2;
		float num4 = val.CalcHeight(new GUIContent(text), 178f * s);
		int num5 = Mathf.Max(1, Mathf.RoundToInt(num4 / Mathf.Max(1f, 12f * s)));
		float num6 = (116f + (float)Mathf.Max(0, num5 - 4) * 16f) * s;
		float num7 = num3 + Mathf.Max(num6, 52f * s + num4);
		Rect rect = (LastEquipRect = new Rect(Mathf.Clamp(anchor.x, 0f, Mathf.Max(0f, UiKit.W - 290f * s)), Mathf.Clamp(anchor.y + 20f * s, 0f, Mathf.Max(0f, UiKit.H - num7)), 290f * s, num7));
		DrawFrame(rect, s);
		GUI.Label(new Rect(rect.x + 18f * s, rect.y + 10f * s, 262f * s, 20f * s), ItemCatalog.Name(id), val);
		Texture2D val3 = WzAssets.Texture("UI/UIWindow.img/ToolTip/Equip/Dot/0");
		if ((Object)(object)val3 != (Object)null)
		{
			GUI.DrawTexture(new Rect(rect.x + 10f * s, rect.y + 15f * s, (float)((Texture)val3).width * s, (float)((Texture)val3).height * s), (Texture)(object)val3);
		}
		if (num != 0)
		{
			GUIStyle val4 = new GUIStyle(val)
			{
				alignment = (TextAnchor)1
			};
			val4.normal.textColor = Color32.op_Implicit(new Color32(byte.MaxValue, (byte)138, (byte)24, byte.MaxValue));
			GUI.Label(new Rect(rect.x, rect.y + 29f * s, rect.width, 19f * s), "任务道具", val4);
		}
		Fill(new Rect(rect.x + 10f * s, rect.y + num3 + 32f * s, 68f * s, 68f * s), Color32.op_Implicit(new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)160)));
		Texture2D val5 = NativeItemSource.Icon(id);
		ImgNode imgNode2 = NativeItemSource.IconNode(id);
		if ((Object)(object)val5 != (Object)null)
		{
			ImgNode imgNode3 = imgNode2.Get("origin");
			float num8 = imgNode3?.X ?? 0;
			float num9 = imgNode3?.Y ?? 0;
			GUI.DrawTexture(new Rect(rect.x + (12f - 2f * num8) * s, rect.y + num3 + (98f - 2f * num9) * s, (float)(((Texture)val5).width * 2) * s, (float)(((Texture)val5).height * 2) * s), (Texture)(object)val5);
		}
		GUI.Label(new Rect(rect.x + 92f * s, rect.y + num3 + 32f * s, 178f * s, num7 - num3 - 40f * s), text, val);
	}

	public static string FormatDescription(string text)
	{
		text = (text ?? "").Replace("\\\\n", "\n").Replace("\\n", "\n");
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			switch (c)
			{
			case '#':
				stringBuilder.Append(flag ? "</color>" : "<color=#FF9900>");
				if (!flag && i + 1 < text.Length && text[i + 1] == 'c')
				{
					i++;
				}
				flag = !flag;
				break;
			case '<':
				stringBuilder.Append("&lt;");
				break;
			default:
				stringBuilder.Append(c);
				break;
			}
		}
		if (flag)
		{
			stringBuilder.Append("</color>");
		}
		return stringBuilder.ToString();
	}

	private static void Fill(Rect rect, Color color)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Color color2 = GUI.color;
		GUI.color = color;
		GUI.DrawTexture(rect, (Texture)(object)White);
		GUI.color = color2;
	}

	public static void DrawFrame(Rect rect, float s)
	{
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Expected Obj, but got Unknown
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		int num = Mathf.RoundToInt(rect.width / s);
		int num2 = Mathf.RoundToInt(rect.height / s);
		int key = num * 4096 + num2;
		if (!frames.TryGetValue(key, out var value) || (Object)(object)value == (Object)null)
		{
			if (frames.Count >= 6)
			{
				foreach (Texture2D value2 in frames.Values)
				{
					if ((Object)(object)value2 != (Object)null)
					{
						Object.Destroy((Object)(object)value2);
					}
				}
				frames.Clear();
			}
			value = new Texture2D(num, num2, (TextureFormat)4, false)
			{
				filterMode = (FilterMode)0
			};
			Color32[] array = new Color32[num * num2];
			for (int i = 0; i < num2; i++)
			{
				for (int j = 0; j < num; j++)
				{
					bool flag = ((j == 0 || j == num - 1) && (i == 0 || i == num2 - 1)) || ((j == 1 || j == num - 2) && i >= 2 && i < num2 - 2) || ((i == 1 || i == num2 - 2) && j >= 2 && j < num - 2);
					array[i * num + j] = (flag ? new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)0) : new Color32((byte)0, (byte)0, (byte)64, (byte)160));
				}
			}
			value.SetPixels32(array);
			value.Apply();
			frames[key] = value;
		}
		GUI.DrawTexture(rect, (Texture)(object)value);
	}

	static ItemTooltip()
	{
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
	}
}
