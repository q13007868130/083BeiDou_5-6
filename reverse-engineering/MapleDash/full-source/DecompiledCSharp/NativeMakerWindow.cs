using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class NativeMakerWindow
{
	private const string Root = "UI/UIWindow.img/Maker/";

	private const float W = 312f;

	private const float H = 393f;

	private const float RowH = 19f;

	private const int RowsVisible = 17;

	private const float ListX = 8f;

	private const float ListY = 42f;

	private const float ListW = 148f;

	private const float ListH = 345f;

	private const float PanelX = 162f;

	private const float PanelW = 142f;

	private static bool visible;

	private static int tab;

	private static bool jobOnly = true;

	private static List<MakerRecipeView> recipes = new List<MakerRecipeView>();

	private static List<int> rows = new List<int>();

	private static float scroll;

	private static int selected;

	private static bool stimulant;

	private static List<int> reagents = new List<int>();

	private static bool picking;

	private static int level;

	private static int makerLevel;

	private static long meso;

	private static string hint = "";

	private static Vector2 position = new Vector2(240f, 50f);

	private static Vector2 grab;

	private static bool dragging;

	private static int generation;

	public static bool Visible => visible;

	public static void Reset()
	{
		visible = false;
		tab = 0;
		jobOnly = true;
		recipes = new List<MakerRecipeView>();
		rows = new List<int>();
		scroll = 0f;
		selected = 0;
		stimulant = false;
		reagents = new List<int>();
		picking = false;
		hint = "";
		generation++;
	}

	public static void Open()
	{
		if (!GameSession.InGame)
		{
			return;
		}
		Reset();
		if (!EnsureTable())
		{
			BottomBar.Toast("制作数据缺失");
			return;
		}
		MakerResponse makerResponse = Request(MakerOperation.Open);
		if (!makerResponse.ok)
		{
			BottomBar.Toast(makerResponse.error);
			Reset();
			return;
		}
		recipes = makerResponse.recipes ?? new List<MakerRecipeView>();
		level = makerResponse.level;
		makerLevel = makerResponse.makerLevel;
		meso = makerResponse.meso;
		visible = true;
		Rebuild();
	}

	public static void Close()
	{
		Reset();
	}

	private static bool EnsureTable()
	{
		if (MakerTable.Loaded)
		{
			return true;
		}
		TextAsset val = Resources.Load<TextAsset>("Maker/MakerTable");
		if ((Object)(object)val == (Object)null)
		{
			return false;
		}
		MakerTable.Load(val.text);
		return true;
	}

	private static MakerResponse Request(MakerOperation operation, int toCreate = 0, int position_ = 0, bool useStimulant = false, int[] reagentIds = null)
	{
		if (!GameSession.InGame)
		{
			return new MakerResponse
			{
				ok = false,
				error = "请先选择角色"
			};
		}
		GameSession.SaveToDb();
		MakerResponse makerResponse = GameSession.Net.Maker(new MakerRequest
		{
			characterId = GameSession.CharacterId,
			operation = operation,
			toCreate = toCreate,
			position = position_,
			stimulant = useStimulant,
			reagentIds = reagentIds
		});
		if (makerResponse.inventory != null)
		{
			GameSession.Character.inventory = makerResponse.inventory;
			GameSession.ApplyInventoryToProfile();
		}
		if (makerResponse.meso >= 0)
		{
			GameSession.Character.record.meso = makerResponse.meso;
			Profile.Data.gold = makerResponse.meso;
			meso = makerResponse.meso;
		}
		if (makerResponse.inventory != null || makerResponse.meso >= 0)
		{
			Profile.Save();
		}
		return makerResponse;
	}

	private static MakerRecipeView RecipeOf(int itemId)
	{
		for (int i = 0; i < recipes.Count; i++)
		{
			if (recipes[i].itemId == itemId)
			{
				return recipes[i];
			}
		}
		return null;
	}

	private static void Rebuild()
	{
		rows = new List<int>();
		scroll = 0f;
		if (tab == 0)
		{
			int num = MakerTable.CategoryOf((GameSession.Character != null) ? GameSession.Character.record.job : 0);
			foreach (MakerRecipeView recipe in recipes)
			{
				if (!jobOnly || recipe.category == 0 || recipe.category == num)
				{
					rows.Add(recipe.itemId);
				}
			}
		}
		else
		{
			HashSet<int> hashSet = new HashSet<int>();
			foreach (InventoryEntry item in GameSession.Character.inventory)
			{
				if (item.position <= 0 || !hashSet.Add(item.itemId))
				{
					continue;
				}
				if (tab == 1)
				{
					if (item.inventoryType == 1 && NativeMakerService.CanDisassemble(item.itemId))
					{
						rows.Add(item.itemId);
					}
				}
				else if (item.inventoryType == 4 && MakerTable.CrystalFromLeftover(item.itemId) != -1)
				{
					rows.Add(item.itemId);
				}
			}
			rows.Sort();
		}
		selected = 0;
		stimulant = false;
		reagents = new List<int>();
		picking = false;
	}

	private static int EquipPositionOf(int itemId)
	{
		foreach (InventoryEntry item in GameSession.Character.inventory)
		{
			if (item.inventoryType == 1 && item.position > 0 && item.itemId == itemId)
			{
				return item.position;
			}
		}
		return 0;
	}

	private static int HeldOf(int itemId)
	{
		int num = 0;
		foreach (InventoryEntry item in GameSession.Character.inventory)
		{
			if (item.itemId == itemId && item.position > 0)
			{
				num += item.quantity;
			}
		}
		return num;
	}

	private static void DoCraft()
	{
		if (selected < 0 || selected >= rows.Count)
		{
			return;
		}
		int num = rows[selected];
		MakerRecipeView makerRecipeView = RecipeOf(num);
		if (makerRecipeView == null)
		{
			return;
		}
		if (makerRecipeView.reqLevel > level)
		{
			hint = "等级不足";
			return;
		}
		int[] reagentIds = reagents.ToArray();
		MakerResponse makerResponse = Request(MakerOperation.Craft, num, 0, stimulant, reagentIds);
		if (!makerResponse.ok)
		{
			hint = makerResponse.error;
			BottomBar.Toast(makerResponse.error);
			return;
		}
		if (!makerResponse.craftSucceeded)
		{
			hint = "制作失败，材料已消耗";
			BottomBar.Toast("制作失败");
		}
		else
		{
			string text = ItemCatalog.Name(num);
			hint = "已制作 " + text;
			BottomBar.Toast("制作成功：" + text);
		}
		stimulant = false;
		reagents = new List<int>();
		MakerResponse makerResponse2 = Request(MakerOperation.Open);
		if (makerResponse2.ok)
		{
			recipes = makerResponse2.recipes ?? recipes;
			level = makerResponse2.level;
			makerLevel = makerResponse2.makerLevel;
		}
		Rebuild();
	}

	private static void DoDisassemble()
	{
		if (selected < 0 || selected >= rows.Count)
		{
			return;
		}
		int num = rows[selected];
		int num2 = EquipPositionOf(num);
		if (num2 == 0)
		{
			hint = "背包里没有这件装备";
			return;
		}
		MakerResponse makerResponse = Request(MakerOperation.Disassemble, num, num2);
		if (!makerResponse.ok)
		{
			hint = makerResponse.error;
			BottomBar.Toast(makerResponse.error);
			return;
		}
		List<MakerReqItem> list = NativeMakerService.DisassemblyGains(num);
		string text = "已分解";
		foreach (MakerReqItem item in list)
		{
			if (item.count > 0)
			{
				text = text + "  " + ItemCatalog.Name(item.itemId) + " x" + item.count;
			}
		}
		hint = text;
		BottomBar.Toast(text);
		MakerResponse makerResponse2 = Request(MakerOperation.Open);
		if (makerResponse2.ok)
		{
			recipes = makerResponse2.recipes ?? recipes;
			level = makerResponse2.level;
			makerLevel = makerResponse2.makerLevel;
		}
		Rebuild();
	}

	private static void DoCrystal()
	{
		if (selected < 0 || selected >= rows.Count)
		{
			return;
		}
		int num = rows[selected];
		int num2 = MakerTable.CrystalFromLeftover(num);
		if (num2 == -1)
		{
			hint = "这种原石不能合成";
			return;
		}
		if (HeldOf(num) < 100)
		{
			hint = "需要 100 份 " + ItemCatalog.Name(num);
			return;
		}
		MakerResponse makerResponse = Request(MakerOperation.Crystal, num);
		if (!makerResponse.ok)
		{
			hint = makerResponse.error;
			BottomBar.Toast(makerResponse.error);
			return;
		}
		hint = "已合成 " + ItemCatalog.Name(num2);
		BottomBar.Toast("已合成 " + ItemCatalog.Name(num2));
		MakerResponse makerResponse2 = Request(MakerOperation.Open);
		if (makerResponse2.ok)
		{
			recipes = makerResponse2.recipes ?? recipes;
			level = makerResponse2.level;
			makerLevel = makerResponse2.makerLevel;
		}
		Rebuild();
	}

	private static GUIStyle Label(bool right = false, int size = 11)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected Obj, but got Unknown
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected Obj, but got Unknown
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = size,
			padding = new RectOffset(),
			clipping = (TextClipping)1,
			alignment = (TextAnchor)(right ? 2 : 0),
			wordWrap = false
		};
		val.normal.textColor = Color.black;
		return val;
	}

	private static GUIStyle Small()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected Obj, but got Unknown
		//IL_0032: Expected Obj, but got Unknown
		return new GUIStyle(GUI.skin.button)
		{
			font = UiKit.Font,
			fontSize = 10,
			padding = new RectOffset(2, 2, 1, 1)
		};
	}

	private static void Image(string node, float x, float y)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://" + node);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(x, y, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
		}
	}

	private static void Icon(int itemId, float x, float foot)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = NativeItemSource.IconNode(itemId);
		if (imgNode != null)
		{
			Texture2D val = WzAssets.Texture(imgNode);
			if (!((Object)(object)val == (Object)null))
			{
				Vector2 val2 = WzAssets.Origin(imgNode);
				GUI.DrawTexture(new Rect(x - val2.x, foot - val2.y, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
			}
		}
	}

	private static bool Hit(Rect rect, Vector2 pointer)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I4
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if (!GUI.enabled || (int)Event.current.type != 7 || !Pointer.Released || !rect.Contains(pointer))
		{
			return false;
		}
		Pointer.ConsumeClick();
		return true;
	}

	private static void Tabs(Vector2 p)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		string[] array = new string[3] { "制作", "分解", "结晶" };
		for (int i = 0; i < array.Length; i++)
		{
			Rect val = new Rect((float)(10 + i * 46), 22f, 44f, 17f);
			if (tab == i)
			{
				GUI.Box(val, "");
			}
			if (GUI.Button(val, array[i], Small()))
			{
				tab = i;
				Rebuild();
				return;
			}
		}
		Rect val2 = new Rect((float)(10 + array.Length * 46), 22f, 44f, 17f);
		if (tab == 0 && GUI.Button(val2, jobOnly ? "本职业" : "全部", Small()))
		{
			jobOnly = !jobOnly;
			Rebuild();
		}
		GUI.Label(new Rect(220f, 24f, 84f, 14f), "金币 " + meso.ToString("N0"), Label(right: true));
	}

	private static void DrawList(Vector2 p)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		int num = Math.Max(0, rows.Count - 17);
		scroll = NativeUi.Scroll4(new Rect(152f, 42f, 12f, 345f), new Rect(8f, 42f, 148f, 345f), scroll, num, 1f, p);
		for (int i = 0; i < 17; i++)
		{
			int num2 = i + (int)scroll;
			if (num2 < rows.Count)
			{
				int num3 = rows[num2];
				float num4 = 42f + (float)i * 19f;
				Rect val = new Rect(8f, num4, 148f, 18f);
				if (num2 == selected)
				{
					GUI.Box(val, "");
				}
				Icon(num3, 22f, num4 + 19f - 3f);
				string text = ItemCatalog.Name(num3) ?? num3.ToString();
				if (text.Length > 9)
				{
					text = text.Substring(0, 9);
				}
				GUI.Label(new Rect(34f, num4 + 2f, 88f, 14f), text, Label());
				MakerRecipeView makerRecipeView = RecipeOf(num3);
				string text2;
				if (tab != 0)
				{
					text2 = "x" + HeldOf(num3);
				}
				else
				{
					text2 = ((makerRecipeView != null) ? ("Lv." + makerRecipeView.reqLevel) : "");
				}
				GUI.Label(new Rect(122f, num4 + 2f, 30f, 14f), text2, Label(right: true));
				if (Hit(val, p))
				{
					selected = num2;
					hint = "";
					picking = false;
					reagents = new List<int>();
				}
				continue;
			}
			break;
		}
	}

	private static void DrawCraftPanel(Vector2 p)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_040c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_037b: Unknown result type (might be due to invalid IL or missing references)
		//IL_051c: Unknown result type (might be due to invalid IL or missing references)
		//IL_056d: Unknown result type (might be due to invalid IL or missing references)
		//IL_04da: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0653: Unknown result type (might be due to invalid IL or missing references)
		if (selected < 0 || selected >= rows.Count)
		{
			GUI.Label(new Rect(162f, 42f, 142f, 14f), "请选择配方", Label());
			return;
		}
		MakerRecipeView makerRecipeView = RecipeOf(rows[selected]);
		if (makerRecipeView == null)
		{
			return;
		}
		float num = 42f;
		GUI.Label(new Rect(162f, num, 142f, 26f), ItemCatalog.Name(makerRecipeView.itemId), Label());
		num += 26f;
		bool flag = level >= makerRecipeView.reqLevel;
		bool flag2 = makerLevel >= makerRecipeView.reqSkillLevel;
		GUI.Label(new Rect(162f, num, 142f, 14f), "等级 " + level + " / " + makerRecipeView.reqLevel + (flag ? "" : " 不足"), Label());
		num += 14f;
		GUI.Label(new Rect(162f, num, 142f, 14f), "制作技能 " + makerLevel + " / " + makerRecipeView.reqSkillLevel + (flag2 ? "" : " 不足"), Label());
		num += 14f;
		GUI.Label(new Rect(162f, num, 142f, 14f), "金币 " + makerRecipeView.meso.ToString("N0"), Label());
		num += 16f;
		GUI.Label(new Rect(162f, num, 142f, 13f), "材料", Label());
		num += 13f;
		if (picking)
		{
			float num2 = num;
			GUI.Label(new Rect(162f, num2, 142f, 13f), "选强化宝石（点击加入）", Label());
			num2 += 14f;
			int num3 = NativeMakerService.ReagentSlotsFor(makerRecipeView.itemId);
			int num4 = 0;
			foreach (InventoryEntry item in GameSession.Character.inventory)
			{
				if (item.position <= 0 || item.quantity <= 0 || !NativeMakerService.IsMakerReagent(item.itemId))
				{
					continue;
				}
				Rect val = new Rect(162f, num2 + (float)(num4 * 16), 142f, 15f);
				string text = ItemCatalog.Name(item.itemId) + " x" + item.quantity;
				if (GUI.Button(val, text, Small()))
				{
					if (reagents.Count >= num3)
					{
						hint = "这件装备只能放 " + num3 + " 颗宝石";
					}
					else
					{
						reagents.Add(item.itemId);
					}
					return;
				}
				num4++;
				if (num4 > 8)
				{
					break;
				}
			}
			if (GUI.Button(new Rect(162f, 347f, 142f, 18f), "返回", Small()))
			{
				picking = false;
			}
			return;
		}
		int num5 = 0;
		foreach (MakerIngredient reqItem in makerRecipeView.reqItems)
		{
			if (num5 >= 5)
			{
				GUI.Label(new Rect(162f, num, 142f, 13f), "…", Label());
				break;
			}
			bool flag3 = reqItem.held >= reqItem.count;
			GUI.Label(new Rect(162f, num, 142f, 13f), ItemCatalog.Name(reqItem.itemId) + "  " + reqItem.held + "/" + reqItem.count + (flag3 ? "" : " 缺"), Label());
			num += 13f;
			num5++;
		}
		num += 4f;
		int num6 = NativeMakerService.ReagentSlotsFor(makerRecipeView.itemId);
		int num7 = NativeMakerService.StimulantFor(makerRecipeView.itemId);
		if (num7 != -1)
		{
			stimulant = GUI.Toggle(new Rect(162f, num, 142f, 15f), stimulant, " 刺激剂 " + ItemCatalog.Name(num7), Small());
			num += 16f;
		}
		GUI.Label(new Rect(162f, num, 60f, 14f), "宝石 " + reagents.Count + "/" + num6, Label());
		if (GUI.Button(new Rect(232f, num - 1f, 28f, 16f), "+", Small()))
		{
			picking = true;
		}
		if (reagents.Count > 0 && GUI.Button(new Rect(264f, num - 1f, 36f, 16f), "清空", Small()))
		{
			reagents = new List<int>();
		}
		num += 18f;
		foreach (int reagent in reagents)
		{
			GUI.Label(new Rect(162f, num, 142f, 13f), "· " + ItemCatalog.Name(reagent), Label());
			num += 13f;
		}
		if (GUI.Button(new Rect(162f, 363f, 142f, 22f), "制作", Small()))
		{
			DoCraft();
		}
	}

	private static void DrawDisassemblePanel(Vector2 p)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		if (selected < 0 || selected >= rows.Count)
		{
			GUI.Label(new Rect(162f, 42f, 142f, 14f), "请选择装备", Label());
			return;
		}
		int num = rows[selected];
		float num2 = 42f;
		GUI.Label(new Rect(162f, num2, 142f, 26f), ItemCatalog.Name(num), Label());
		num2 += 28f;
		GUI.Label(new Rect(162f, num2, 142f, 14f), "分解可得（配方的一半）", Label());
		num2 += 14f;
		foreach (MakerReqItem item in NativeMakerService.DisassemblyGains(num))
		{
			GUI.Label(new Rect(162f, num2, 142f, 13f), ItemCatalog.Name(item.itemId) + " x" + item.count, Label());
			num2 += 13f;
		}
		if (GUI.Button(new Rect(162f, 363f, 142f, 22f), "分解", Small()))
		{
			DoDisassemble();
		}
	}

	private static void DrawCrystalPanel(Vector2 p)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		if (selected < 0 || selected >= rows.Count)
		{
			GUI.Label(new Rect(162f, 42f, 142f, 14f), "请选择原石", Label());
			return;
		}
		int num = rows[selected];
		int id = MakerTable.CrystalFromLeftover(num);
		float num2 = 42f;
		GUI.Label(new Rect(162f, num2, 142f, 26f), ItemCatalog.Name(num), Label());
		num2 += 28f;
		GUI.Label(new Rect(162f, num2, 142f, 14f), "→ " + ItemCatalog.Name(id), Label());
		num2 += 14f;
		GUI.Label(new Rect(162f, num2, 142f, 14f), "需要 100 份（持有 " + HeldOf(num) + "）", Label());
		if (GUI.Button(new Rect(162f, 363f, 142f, 22f), "合成", Small()))
		{
			DoCrystal();
		}
	}

	public static void Draw()
	{
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Invalid comparison between Unknown and I4
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		if (!visible)
		{
			return;
		}
		if (!GameSession.InGame)
		{
			Reset();
			return;
		}
		float s = BottomBar.S;
		position = NativeUi.ClampWindowPosition(position, new Vector2(312f, 393f), s);
		NativeUi.RegisterWindow("NativeMakerWindow", new Rect(position.x * s, position.y * s, 312f * s, 393f * s), () => Visible);
		Vector2 val = Pointer.Pos / s;
		Matrix4x4 matrix = GUI.matrix;
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(s, s, 1f));
		try
		{
			if (GUI.enabled && (int)Event.current.type == 7 && Pointer.Pressed)
			{
				Rect val2 = new Rect(position.x, position.y, 312f, 16f);
				if (val2.Contains(val))
				{
					dragging = true;
					grab = val - position;
				}
			}
			if (dragging && Pointer.Down)
			{
				position = NativeUi.ClampWindowPosition(val - grab, new Vector2(312f, 393f), s);
			}
			if (!Pointer.Down)
			{
				dragging = false;
			}
			GUI.BeginGroup(new Rect(position.x, position.y, 312f, 393f));
			try
			{
				Vector2 val3 = val - position;
				Image("UI/UIWindow.img/Maker/backgrnd", 0f, 0f);
				GUI.Label(new Rect(10f, 4f, 200f, 14f), "制作", Label());
				if (NativeUi.Button(new Rect(290f, 4f, 16f, 16f), "UI/UIWindow.img/BtUIClose2", val3))
				{
					Close();
					return;
				}
				Tabs(val3);
				DrawList(val3);
				if (tab == 0)
				{
					DrawCraftPanel(val3);
				}
				else if (tab == 1)
				{
					DrawDisassemblePanel(val3);
				}
				else
				{
					DrawCrystalPanel(val3);
				}
				if (!string.IsNullOrEmpty(hint))
				{
					GUI.Label(new Rect(10f, 377f, 292f, 13f), hint, Label());
				}
			}
			finally
			{
				GUI.EndGroup();
			}
		}
		finally
		{
			GUI.matrix = matrix;
		}
	}

	static NativeMakerWindow()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
	}
}
