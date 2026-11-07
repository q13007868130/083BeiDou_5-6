using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class NativeSymbolWindow
{
	private const string Root = "UI/UIEquip.img/Equip/Symbol/";

	private const float Width = 228f;

	private const float Height = 339f;

	private static bool visible;

	private static bool dragging;

	private static bool positioned;

	private static Vector2 position;

	private static Vector2 grab;

	private static int owner;

	private static SymbolKind category;

	private static readonly List<InventoryEntry> worn = new List<InventoryEntry>();

	public static bool Visible => visible;

	public static bool AtPointer
	{
		get
		{
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			if (visible)
			{
				if (!dragging)
				{
					return Pointer.HitsWindow(Window);
				}
				return true;
			}
			return false;
		}
	}

	private static float S => Mathf.Min(BottomBar.S, Mathf.Min((UiKit.W - 24f) / 228f, UiKit.H * 0.9f / 339f));

	private static Vector2 Free
	{
		get
		{
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			if (!positioned)
			{
				return new Vector2((UiKit.W / S - 228f) / 2f, (UiKit.H / S - 339f) / 2f);
			}
			return NativeUi.ClampWindowPosition(position, new Vector2(228f, 339f), S);
		}
	}

	private static Rect Window
	{
		get
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			return new Rect(Free.x * S, Free.y * S, 228f * S, 339f * S);
		}
	}

	private static string Branch
	{
		get
		{
			string text;
			if (category != SymbolKind.Arcane)
			{
				text = ((category == SymbolKind.Authentic) ? "AutEquip/" : "GrandAutEquip/");
			}
			else
			{
				text = "ArcEquip/";
			}
			return "UI/UIEquip.img/Equip/Symbol/" + text;
		}
	}

	private static Vector2 Vector(string root, string key)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = WzAssets.Node(root + key);
		if (imgNode == null || !imgNode.TryVector("", out var x, out var y))
		{
			return Vector2.zero;
		}
		return new Vector2((float)x, (float)y);
	}

	private static Vector2 Constant(string key)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		return Vector(Branch, "constants/vector:" + key);
	}

	private static Vector2 Size(string root, string key)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = WzAssets.Texture(WzAssets.Node(root + key));
		if (!((Object)(object)val == (Object)null))
		{
			return new Vector2((float)((Texture)val).width, (float)((Texture)val).height);
		}
		return Vector2.zero;
	}

	private static void Glyphs(string root, string text, float x, float y, bool centre = false)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		float num = 0f;
		string text2 = text;
		for (int i = 0; i < text2.Length; i++)
		{
			num += Size(root, text2[i].ToString()).x;
		}
		x -= (centre ? ((float)(int)(num / 2f)) : num);
		text2 = text;
		for (int i = 0; i < text2.Length; i++)
		{
			char c = text2[i];
			Vector2 val = WzAssets.Origin(WzAssets.Node(root + c));
			GrowthUi.Image(root, c.ToString(), x - val.x, y - val.y);
			x += Size(root, c.ToString()).x;
		}
	}

	public static void Close()
	{
		visible = (dragging = (positioned = false));
		worn.Clear();
	}

	public static void Open()
	{
		Close();
		NativeHyperStatWindow.Close();
		NativeInnerAbilityWindow.Close();
		if (GameSession.InGame)
		{
			owner = GameSession.CharacterId;
			category = SymbolKind.Arcane;
			visible = true;
			Refresh();
		}
	}

	private static void Refresh(bool normalize = true)
	{
		worn.Clear();
		List<InventoryEntry> list = GameSession.Character?.inventory;
		bool flag = false;
		if (list != null)
		{
			foreach (InventoryEntry item in list)
			{
				if (item.position < 0 && SymbolCatalog.IsSymbol(item.itemId))
				{
					worn.Add(item);
					if (normalize && item.symbolLevel == 0)
					{
						flag |= GameSession.Net.Symbol(new SymbolRequest
						{
							characterId = owner,
							inventoryItemId = item.itemRowId,
							operation = SymbolOperation.Preview
						}).ok;
					}
				}
			}
		}
		if (flag)
		{
			GameSession.ReloadAfterCommand();
			Refresh(normalize: false);
		}
	}

	private static void Operate(InventoryEntry item, SymbolOperation operation)
	{
		SymbolResponse symbolResponse = GameSession.Net.Symbol(new SymbolRequest
		{
			characterId = owner,
			inventoryItemId = item.itemRowId,
			operation = SymbolOperation.Preview
		});
		if (!symbolResponse.ok)
		{
			BottomBar.Toast(symbolResponse.error);
			return;
		}
		NativeUtilPrompt.Confirm((operation == SymbolOperation.LevelUp) ? ("升级「" + SymbolCatalog.Of(item.itemId).name + "」需要 " + symbolResponse.mesoCost + " 枫币，是否继续？") : ("将背包中符合条件的同款符文合并到「" + SymbolCatalog.Of(item.itemId).name + "」？"), (bool yes) =>
		{
			if (yes && visible && owner == GameSession.CharacterId)
			{
				SymbolResponse symbolResponse2 = GameSession.Net.Symbol(new SymbolRequest
				{
					characterId = owner,
					inventoryItemId = item.itemRowId,
					operation = operation,
					commit = true
				});
				if (!symbolResponse2.ok)
				{
					BottomBar.Toast(symbolResponse2.error);
				}
				else
				{
					GameSession.ReloadAfterCommand();
					Refresh();
				}
			}
		});
	}

	private static int Stat(InventoryEntry item, string name)
	{
		return name switch
		{
			"str" => item.str, 
			"dex" => item.dex, 
			"int" => item.int_, 
			"luk" => item.luk, 
			_ => item.hp, 
		};
	}

	private static void Summary()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e2: Unknown result type (might be due to invalid IL or missing references)
		int jobId = Profile.JobId;
		bool flag = NativeSymbolService.IsXenon(jobId);
		Vector2 val = Constant(flag ? "statNamePosXenon" : "statNamePos");
		Vector2 val2 = Constant(flag ? "statNumberPosXenon" : "statNumberPos");
		int num = 0;
		foreach (InventoryEntry item in worn)
		{
			if (SymbolCatalog.Of(item.itemId)?.kind == category)
			{
				num += item.symbolInc;
			}
		}
		GrowthUi.Image(Branch, "textUp/force", val.x, val.y);
		Glyphs(Branch + "textUp/", "+" + num, val2.x, val2.y);
		if (category == SymbolKind.Grand)
		{
			int num2 = WzAssets.Node(Branch + "constants")?.Int("lineGap", 20) ?? 20;
			string[] array = new string[3] { "ExpRate", "MesoRate", "DropRate" };
			string[] array2 = new string[3] { "exp", "meso", "drop" };
			for (int i = 0; i < array.Length; i++)
			{
				val.y += num2;
				val2.y += num2;
				int num3 = 0;
				foreach (InventoryEntry item2 in worn)
				{
					if (item2.position == SymbolCatalog.SlotOf(item2.itemId))
					{
						num3 += SymbolCatalog.GrandBonus(item2.itemId, item2.symbolLevel, array[i]);
					}
				}
				GrowthUi.Image(Branch, "textUp/" + array2[i], val.x, val.y);
				Glyphs(Branch + "textUp/", "+" + num3 + "%", val2.x, val2.y);
			}
			return;
		}
		string[] array3 = ((!flag) ? new string[1] { NativeSymbolService.IsDemonAvenger(jobId) ? "hp" : NativeSymbolService.MainStatColumn(jobId) } : new string[3] { "str", "dex", "luk" });
		int num4 = WzAssets.Node(Branch + "constants")?.Int(flag ? "lineGapXenon" : "lineGap", 20) ?? 20;
		string[] array4 = array3;
		foreach (string text in array4)
		{
			val.y += num4;
			val2.y += num4;
			int num5 = 0;
			foreach (InventoryEntry item3 in worn)
			{
				if (SymbolCatalog.Of(item3.itemId)?.kind == category)
				{
					num5 += Stat(item3, text);
				}
			}
			GrowthUi.Image(Branch, "textUp/" + text, val.x, val.y);
			Glyphs(Branch + "textUp/", "+" + num5, val2.x, val2.y);
		}
	}

	public static void Draw()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0308: Unknown result type (might be due to invalid IL or missing references)
		//IL_030d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0319: Unknown result type (might be due to invalid IL or missing references)
		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
		//IL_032a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0331: Unknown result type (might be due to invalid IL or missing references)
		//IL_0357: Unknown result type (might be due to invalid IL or missing references)
		//IL_035c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03be: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0403: Unknown result type (might be due to invalid IL or missing references)
		//IL_040a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0368: Unknown result type (might be due to invalid IL or missing references)
		//IL_036f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0385: Unknown result type (might be due to invalid IL or missing references)
		//IL_038d: Unknown result type (might be due to invalid IL or missing references)
		//IL_039a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_043b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0440: Unknown result type (might be due to invalid IL or missing references)
		//IL_0456: Unknown result type (might be due to invalid IL or missing references)
		//IL_045b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0467: Unknown result type (might be due to invalid IL or missing references)
		//IL_046e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0476: Unknown result type (might be due to invalid IL or missing references)
		//IL_047e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0485: Unknown result type (might be due to invalid IL or missing references)
		//IL_048d: Unknown result type (might be due to invalid IL or missing references)
		//IL_052a: Unknown result type (might be due to invalid IL or missing references)
		//IL_052f: Unknown result type (might be due to invalid IL or missing references)
		//IL_053b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0540: Unknown result type (might be due to invalid IL or missing references)
		//IL_0543: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0502: Unknown result type (might be due to invalid IL or missing references)
		//IL_0509: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0605: Unknown result type (might be due to invalid IL or missing references)
		//IL_060a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0596: Unknown result type (might be due to invalid IL or missing references)
		//IL_059d: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05be: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05df: Unknown result type (might be due to invalid IL or missing references)
		//IL_064b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0623: Unknown result type (might be due to invalid IL or missing references)
		if (!visible)
		{
			return;
		}
		if (!GameSession.InGame || owner != GameSession.CharacterId)
		{
			Close();
			return;
		}
		Refresh(normalize: false);
		bool atPointer = AtPointer;
		GrowthUi.S = S;
		GrowthUi.R = Window;
		GrowthUi.Drag(ref dragging, ref positioned, ref position, ref grab, 228f, 339f, () =>
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			return Window;
		}, 12f, 0f, typeof(NativeSymbolWindow));
		GrowthUi.R = Window;
		NativeUi.RegisterWindow("NativeSymbolWindow", Window, () => visible);
		GrowthUi.Image("UI/UIEquip.img/Equip/Symbol/", "backgrnd", 0f, 0f);
		Vector2 val = Vector("UI/UIEquip.img/Equip/Symbol/", "tab:categoryTab/lt");
		for (int num = 0; num < 2; num++)
		{
			string text = "tab:categoryTab/" + ((num == 0 == (category == SymbolKind.Arcane)) ? "selected/" : "normal/") + num;
			Vector2 val2 = Size("UI/UIEquip.img/Equip/Symbol/", text);
			GrowthUi.Image("UI/UIEquip.img/Equip/Symbol/", text, val.x + (float)num * val2.x, val.y);
			if (GUI.enabled && GrowthUi.Repaint && Pointer.Clicked)
			{
				Rect val3 = GrowthUi.At(val.x + (float)num * val2.x, val.y, val2.x, 27f);
				if (val3.Contains(Pointer.Pos))
				{
					Pointer.ConsumeClick();
					category = ((num != 0) ? SymbolKind.Authentic : SymbolKind.Arcane);
				}
			}
		}
		Vector2 val4 = Vector("UI/UIEquip.img/Equip/Symbol/", "vector:childLT");
		Rect previous = GrowthUi.Shift(val4.x, val4.y, 184f, 272f);
		GrowthUi.Image(Branch, "backgrnd", 0f, 0f);
		Summary();
		if (category != SymbolKind.Arcane)
		{
			Vector2 val5 = Vector(Branch, "nextPos");
			Vector2 val6 = Size(Branch, "button:next/normal/0");
			if (GrowthUi.ButtonAt(Branch, "button:next", GrowthUi.At(val5.x, val5.y, val6.x, val6.y)))
			{
				category = ((category != SymbolKind.Authentic) ? SymbolKind.Authentic : SymbolKind.Grand);
			}
		}
		int num2 = ((category == SymbolKind.Grand) ? 3 : 6);
		for (int num3 = 0; num3 < num2; num3++)
		{
			int id;
			if (category != SymbolKind.Arcane)
			{
				id = ((category == SymbolKind.Authentic) ? (1713000 + num3) : (1714000 + num3));
			}
			else
			{
				id = 1712001 + num3;
			}
			InventoryEntry inventoryEntry = worn.Find((InventoryEntry e) => e.itemId == id);
			Vector2 val7 = Vector(Branch, "slotPos/" + num3);
			Vector2 val8 = Size(Branch, "box");
			GrowthUi.Image(Branch, "box", val7.x, val7.y);
			if (inventoryEntry == null)
			{
				continue;
			}
			Texture2D val9 = NativeItemSource.Icon(inventoryEntry.itemId);
			Vector2 val10 = Constant("slotIconPos");
			if ((Object)(object)val9 != (Object)null)
			{
				GUI.DrawTexture(GrowthUi.At(val7.x + (float)(int)((val8.x - 32f) / 2f) + val10.x, val7.y + 7f + val10.y, 32f, 32f), (Texture)(object)val9, (ScaleMode)2);
			}
			Vector2 val11 = Constant("slotLevelPos");
			Glyphs(Branch + "textDown/", "v" + Math.Max(1, inventoryEntry.symbolLevel), val7.x + val11.x, val7.y + val11.y, centre: true);
			SymbolInfo symbolInfo = SymbolCatalog.Of(id);
			if (inventoryEntry.symbolLevel >= symbolInfo.maxLevel)
			{
				Vector2 val12 = Constant("slotMaxPos");
				Vector2 val13 = WzAssets.Origin(WzAssets.Node(Branch + "base2"));
				GrowthUi.Image(Branch, "base2", val7.x + val12.x - val13.x, val7.y + val12.y - val13.y);
			}
			else if (symbolInfo.CanLevelUp(inventoryEntry.symbolLevel, inventoryEntry.symbolExp))
			{
				Vector2 val14 = Constant("slotButtonPos");
				Vector2 val15 = Size(Branch, "button:enchant/normal/0");
				if (GrowthUi.ButtonAt(Branch, "button:enchant", GrowthUi.At(val7.x + val14.x, val7.y + val14.y, val15.x, val15.y)))
				{
					Operate(inventoryEntry, SymbolOperation.LevelUp);
				}
			}
			else
			{
				Vector2 val16 = Constant("slotGaugePos");
				Vector2 val17 = Size(Branch, "canvas:gauge");
				int num4 = Math.Max(1, (int)(val17.x * (float)inventoryEntry.symbolExp / (float)symbolInfo.GrowthCost(Math.Max(1, inventoryEntry.symbolLevel))));
				Texture2D val18 = WzAssets.Texture(WzAssets.Node(Branch + "canvas:gauge"));
				if ((Object)(object)val18 != (Object)null)
				{
					GUI.DrawTextureWithTexCoords(GrowthUi.At(val7.x + val16.x, val7.y + val16.y, num4, val17.y), (Texture)(object)val18, new Rect(0f, 0f, (float)num4 / val17.x, 1f));
				}
			}
			Rect val19 = GrowthUi.At(val7.x, val7.y, val8.x, val8.y);
			if (GUI.enabled && GrowthUi.Repaint && Pointer.DragStarted && val19.Contains(Pointer.PressPosition))
			{
				NativeInventoryActions.PickEquipped(inventoryEntry.itemId, inventoryEntry.position);
			}
			if (!GrowthUi.Repaint || !val19.Contains(Pointer.Pos))
			{
				continue;
			}
			ItemTooltip.SetHover(inventoryEntry.itemId, equipped: false, inventoryEntry);
			if (GUI.enabled && Pointer.RightPressed)
			{
				Pointer.RightPressed = false;
				string text2 = EquipLoadout.UnequipToInventory(inventoryEntry.position);
				if (text2 != null)
				{
					BottomBar.Toast(text2);
				}
				else
				{
					Refresh();
				}
				break;
			}
			if (GUI.enabled && Pointer.Clicked)
			{
				Pointer.ConsumeClick();
				Operate(inventoryEntry, SymbolOperation.MergeAll);
			}
		}
		GrowthUi.Restore(previous);
		if (atPointer && GrowthUi.Repaint && Pointer.Clicked)
		{
			Pointer.ConsumeClick();
		}
	}
}
