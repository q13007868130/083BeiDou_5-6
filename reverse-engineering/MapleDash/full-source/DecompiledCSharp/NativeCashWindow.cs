using System;
using System.Collections.Generic;
using System.Globalization;
using MapleDash.Wz;
using UnityEngine;

public static class NativeCashWindow
{
	private static CashResponse state;

	private static string target = "";

	private static string message = "";

	private static string messageSecond = "";

	private static string birthday = "";

	private static int selected;

	private static int currency;

	private static int page;

	private static float previousTimeScale = 1f;

	private static Vector2 pointer;

	private static int category = 1;

	private static int subcategory;

	private static int lockerRow;

	private static int inventoryRow;

	private static int categoryGroup;

	private static int pageGroup;

	private static bool giftVisible;

	private static bool purchaseVisible;

	private static bool birthdayVisible;

	private static bool keepNormal;

	private static int purchaseMask;

	private static int previewBackground;

	private static bool previewEnabled = true;

	private static bool previewUndressed;

	private static int previewItem;

	private static long stockSelection;

	private static bool stockSelectionLocker;

	private static float stockClickAt;

	private static readonly Dictionary<string, Sprite> previewFrames = new Dictionary<string, Sprite>();

	private const string CashRoot = "UI/CashShop.img/";

	private static readonly int[] currencies = new int[3] { 1, 2, 4 };

	private static float nextExpiryCheck;

	private static bool surpriseVisible;

	private static int surprisePhase;

	private static long surpriseBoxId;

	private static int surpriseRewardId;

	public static void Toggle()
	{
		if (ScreenFade.Busy)
		{
			return;
		}
		GameLoop i = GameLoop.I;
		if (i == null || i.field?.Warping != true)
		{
			if ((Object)(object)GameLoop.I != (Object)null)
			{
				ScreenFade.Transition((MonoBehaviour)(object)GameLoop.I, ToggleNow);
			}
			else
			{
				ToggleNow();
			}
		}
	}

	private static void ToggleNow()
	{
		if (ShopWindow.Visible)
		{
			Request(CashOperation.Close, 0L);
			Reset();
			return;
		}
		NativeNpcShopWindow.Close();
		ShopWindow.Visible = Request(CashOperation.Open, 0L)?.ok ?? false;
		if (ShopWindow.Visible)
		{
			category = 1;
			subcategory = (page = (lockerRow = (inventoryRow = 0)));
			previousTimeScale = Time.timeScale;
			Time.timeScale = 0f;
		}
	}

	public static void Reset()
	{
		if (ShopWindow.Visible && Time.timeScale == 0f)
		{
			Time.timeScale = previousTimeScale;
		}
		ShopWindow.Visible = false;
		state = null;
		giftVisible = (purchaseVisible = (birthdayVisible = (surpriseVisible = false)));
		NativeUtilPrompt.Clear();
	}

	public static void GiftWish(int sn, string recipient)
	{
		if (!ShopWindow.Visible && (Object)(object)GameLoop.I != (Object)null)
		{
			if (!ScreenFade.Busy)
			{
				ScreenFade.Transition((MonoBehaviour)(object)GameLoop.I, () =>
				{
					ToggleNow();
					GiftWish(sn, recipient);
				});
			}
			return;
		}
		if (!ShopWindow.Visible)
		{
			Toggle();
		}
		if (ShopWindow.Visible && state != null)
		{
			selected = sn;
			target = recipient;
			message = (messageSecond = "");
			giftVisible = true;
			purchaseVisible = (birthdayVisible = false);
		}
	}

	private static CashResponse Request(CashOperation operation, long itemId = 0L, int inventory = 0, int[] wishes = null, bool notify = true)
	{
		if (!GameSession.InGame)
		{
			BottomBar.Toast("请先选择角色");
			return null;
		}
		if (GameSession.Server.Cash.Catalog == null)
		{
			try
			{
				GameSession.Server.Cash.Catalog = new NativeCashCatalog();
			}
			catch (Exception ex)
			{
				BottomBar.Toast("商城目录解析失败：" + ex.Message);
				return null;
			}
		}
		GameSession.SaveToDb();
		int.TryParse(birthday, out var result);
		state = GameSession.Net.Cash(new CashRequest
		{
			characterId = GameSession.CharacterId,
			operation = operation,
			sn = selected,
			itemId = itemId,
			currency = currencies[currency],
			target = target,
			message = message + ((messageSecond.Length > 0) ? ("\r\n" + messageSecond) : ""),
			birthday = result,
			inventoryType = inventory,
			wishes = wishes
		});
		SyncInventory(state.character);
		if (!state.ok & notify)
		{
			BottomBar.Toast(state.error);
		}
		return state;
	}

	public static void SyncInventory(FullCharacter full)
	{
		if (full != null && GameSession.Character != null)
		{
			GameSession.Character.inventory = full.inventory;
			GameSession.Character.record = full.record;
			GameSession.ApplyInventoryToProfile();
			Profile.Data.gold = full.record.meso;
			Profile.Data.equipmentSlots = full.record.equipslots;
			Profile.Save();
		}
	}

	public static void Tick()
	{
		if (!GameSession.InGame || GameSession.Character == null || Time.unscaledTime < nextExpiryCheck)
		{
			return;
		}
		nextExpiryCheck = Time.unscaledTime + 1f;
		long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		if (GameSession.Character.inventory.Exists((InventoryEntry x) => x.expiration > 0 && x.expiration <= now))
		{
			CashResponse cashResponse = GameSession.Net.Cash(new CashRequest
			{
				characterId = GameSession.CharacterId
			});
			SyncInventory(cashResponse.character);
			if (ShopWindow.Visible)
			{
				state = cashResponse;
			}
		}
	}

	public static Rect CatalogCell(int index)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		return new Rect((float)(index % 2 * 206), (float)(index / 2 * 81 + 2), 200f, 80f);
	}

	private static string PriceText(CashCommodity entry)
	{
		string text = entry.price.ToString("N0", CultureInfo.InvariantCulture) + ((entry.itemId >= 80000000 && entry.itemId < 90000000) ? " 金币" : " 点券");
		if (entry.count <= 1)
		{
			return text;
		}
		return text + "(" + entry.count + ")";
	}

	private static void DrawCatalog(Vector2 origin, List<CashCommodity> entries)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected Obj, but got Unknown
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected Obj, but got Unknown
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		GUIStyle val = new GUIStyle(GUI.skin.label)
		{
			font = UiKit.Font,
			fontSize = 12,
			padding = new RectOffset(),
			clipping = (TextClipping)1
		};
		val.normal.textColor = Color.black;
		for (int i = 0; i < 10 && page * 10 + i < entries.Count; i++)
		{
			CashCommodity cashCommodity = entries[page * 10 + i];
			Rect val2 = CatalogCell(i);
			val2.position += origin;
			Texture2D val3 = Art.Tex("wz://UI/CashShop.img/CSList/Base");
			if ((Object)(object)val3 != (Object)null)
			{
				GUI.DrawTexture(val2, (Texture)(object)val3);
			}
			GUI.Label(new Rect(val2.x + 82f, val2.y + 6f, 112f, 23f), WzNames.Item(cashCommodity.itemId), val);
			Texture2D val4 = NativeItemSource.Icon(cashCommodity.itemId);
			if ((Object)(object)val4 != (Object)null)
			{
				GUI.DrawTexture(new Rect(val2.x + 7f, val2.y + 73f - (float)(((Texture)val4).height * 2), (float)(((Texture)val4).width * 2), (float)(((Texture)val4).height * 2)), (Texture)(object)val4);
			}
			if (Hit(new Rect(val2.x, val2.y, 78f, 80f)))
			{
				previewItem = cashCommodity.itemId;
			}
			GUI.Label(new Rect(val2.x + 82f, val2.y + 31f, 112f, 18f), PriceText(cashCommodity), val);
			if (NativeUi.Button(new Rect(val2.x + 80f, val2.y + 53f, 37f, 19f), "UI/CashShop.img/CSList/BtBuy", pointer, keepNormal))
			{
				selected = cashCommodity.sn;
				purchaseMask = ((state.credit >= cashCommodity.price) ? 1 : 0) | ((state.points >= cashCommodity.price) ? 2 : 0) | ((state.prepaid >= cashCommodity.price) ? 4 : 0);
				if (cashCommodity.price == 0)
				{
					purchaseMask = 2;
				}
				if (purchaseMask == 0)
				{
					BottomBar.Toast("点券不足");
				}
				else
				{
					currency = ((purchaseMask == 2) ? 1 : ((purchaseMask == 4) ? 2 : 0));
					purchaseVisible = true;
				}
			}
			if (NativeUi.Button(new Rect(val2.x + 119f, val2.y + 53f, 37f, 19f), "UI/CashShop.img/CSList/BtGift", pointer, keepNormal))
			{
				selected = cashCommodity.sn;
				giftVisible = true;
			}
			bool flag = state.wishes.Contains(cashCommodity.sn);
			if (NativeUi.Button(new Rect(val2.x + 158f, val2.y + 53f, 37f, 19f), "UI/CashShop.img/CSList/" + (flag ? "BtRemove" : "BtReserve"), pointer, keepNormal))
			{
				selected = cashCommodity.sn;
				List<int> list = new List<int>(state.wishes);
				if (!list.Remove(selected))
				{
					list.Add(selected);
				}
				Request(CashOperation.WishList, 0L, 0, list.ToArray());
			}
		}
	}

	public static Rect SceneRect(float width, float height)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Min(width / 800f, height / 600f);
		return new Rect((width - 800f * num) / 2f, (height - 600f * num) / 2f, 800f * num, 600f * num);
	}

	public static string BackgroundNode(int job)
	{
		string text;
		if (job / 1000 == 1)
		{
			text = "1";
		}
		else
		{
			text = ((job / 100 == 21 || job == 2000) ? "2" : "");
		}
		return "UI/CashShop.img/Base/backgrnd" + text;
	}

	public static Rect LockerCell(int index)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		return new Rect((float)(20 + index % 6 * 35), (float)(348 + index / 6 * 35), 32f, 32f);
	}

	public static Rect InventoryCell(int index)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		return new Rect((float)(22 + index % 4 * 35), (float)(481 + index / 4 * 35), 32f, 32f);
	}

	private static void Image(string node, float x, float y)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://UI/CashShop.img/" + node);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(x, y, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
		}
	}

	private static bool Button(string node, float x, float y, bool forceNormal = false)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://UI/CashShop.img/" + node + "/normal/0");
		if ((Object)(object)val != (Object)null)
		{
			return NativeUi.Button(new Rect(x, y, (float)((Texture)val).width, (float)((Texture)val).height), "UI/CashShop.img/" + node, pointer, forceNormal);
		}
		return false;
	}

	private static GUIStyle TextStyle(bool right = false)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected Obj, but got Unknown
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = 12,
			clipping = (TextClipping)1,
			alignment = (TextAnchor)(right ? 2 : 0)
		};
		val.normal.textColor = Color.black;
		return val;
	}

	private static bool Hit(Rect rect)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I4
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if (!GUI.enabled || (int)Event.current.type != 7 || !Pointer.Clicked || !rect.Contains(pointer))
		{
			return false;
		}
		Pointer.ConsumeClick();
		AudioManager.Play("UI/BtMouseClick", 0.7f, pitchVary: false);
		return true;
	}

	public static void Draw()
	{
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		if (!ShopWindow.Visible || state == null)
		{
			return;
		}
		Matrix4x4 matrix = GUI.matrix;
		Rect val = SceneRect(UiKit.W, UiKit.H);
		float num = val.width / 800f;
		pointer = (Pointer.Pos - val.position) / num;
		UiKit.Fill(new Rect(0f, 0f, val.x, UiKit.H), Color.black);
		UiKit.Fill(new Rect(val.xMax, 0f, UiKit.W - val.xMax, UiKit.H), Color.black);
		UiKit.Fill(new Rect(0f, 0f, UiKit.W, val.y), Color.black);
		UiKit.Fill(new Rect(0f, val.yMax, UiKit.W, UiKit.H - val.yMax), Color.black);
		GUI.matrix = matrix * Matrix4x4.TRS(Vector2.op_Implicit(val.position), Quaternion.identity, new Vector3(num, num, 1f));
		bool enabled = GUI.enabled;
		bool flag = false;
		bool visible = NativeUtilPrompt.Visible;
		keepNormal = (giftVisible || purchaseVisible) | visible;
		try
		{
			GUI.enabled = enabled && !giftVisible && !purchaseVisible && !visible;
			Image(BackgroundNode((state.character != null) ? state.character.record.job : 0).Substring("UI/CashShop.img/".Length), 0f, 0f);
			DrawPreview();
			DrawCategories();
			List<CashCommodity> list = state.catalog.FindAll((CashCommodity x) => (category != 9) ? (x.sn / 10000000 == category && x.sn / 100000 % 100 == subcategory) : state.wishes.Contains(x.sn));
			if (category == 6 && subcategory == 1)
			{
				list.Sort((CashCommodity a, CashCommodity b) =>
				{
					int num2 = Rank(a.itemId).CompareTo(Rank(b.itemId));
					return (num2 == 0) ? a.sn.CompareTo(b.sn) : num2;
				});
			}
			page = Mathf.Clamp(page, 0, Mathf.Max(0, (list.Count - 1) / 10));
			DrawCatalog(new Vector2(275f, 95f), list);
			DrawPages(list.Count);
			DrawStorage();
			DrawBalances();
			flag = Button("CSStatus/BtExit", 632f, 545f, keepNormal);
			GUI.enabled = enabled;
			if (giftVisible)
			{
				GUI.enabled = enabled && !birthdayVisible;
				DrawGift();
				GUI.enabled = enabled;
			}
			if (birthdayVisible)
			{
				DrawBirthday();
			}
			if (purchaseVisible)
			{
				DrawPurchase();
			}
			if (surpriseVisible)
			{
				GUI.enabled = enabled;
				DrawSurprise();
			}
		}
		finally
		{
			GUI.enabled = enabled;
			GUI.matrix = matrix;
		}
		if (flag)
		{
			Toggle();
		}
		static int Rank(int id)
		{
			return id switch
			{
				1812003 => 1, 
				1812002 => 0, 
				_ => 2, 
			};
		}
	}

	private static void DrawCategories()
	{
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		Image("CSTab/Tab/" + ((category == 8) ? 1 : ((category == 9) ? 9 : (category + 1))), 274f, 17f);
		int[] array = new int[18]
		{
			57, 69, 107, 119, 157, 169, 209, 220, 260, 272,
			311, 323, 362, 374, 3, 3, 451, 451
		};
		for (int i = 1; i <= 9; i++)
		{
			if (i != category)
			{
				int num = array[2 * (i - 1) + ((i >= category) ? 1 : 0)];
				if (Hit(new Rect((float)(272 + num), 39f, 51f, 31f)))
				{
					category = i;
					subcategory = (page = (categoryGroup = (pageGroup = 0)));
				}
			}
		}
		ImgNode imgNode = WzAssets.FileRoot("Etc/Category.img");
		List<string> list = new List<string>();
		List<int> list2 = new List<int>();
		foreach (ImgNode child in imgNode.Children)
		{
			if (child.Int("Category") == category)
			{
				list.Add(child.Text("Name"));
				list2.Add(child.Int("CategorySub"));
			}
		}
		int num2 = DrawTextStrip(list, list2.IndexOf(subcategory), 280f, 76f, 485f, CategoryColor(category), (category >= 5 && category <= 7) || category == 9, 20, ref categoryGroup);
		if (num2 >= 0)
		{
			subcategory = list2[num2];
			page = 0;
		}
	}

	private static int DrawTextStrip(List<string> labels, int selectedIndex, float x, float y, float width, int background, bool whiteText, int perGroup, ref int group)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Expected Obj, but got Unknown
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Expected Obj, but got Unknown
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		UiKit.Fill(new Rect(x, y, width, 14f), Color32.op_Implicit(new Color32((byte)(background >> 16), (byte)(background >> 8), (byte)background, byte.MaxValue)));
		group = Mathf.Clamp(group, 0, Mathf.Max(0, (labels.Count - 1) / perGroup));
		int num = group * perGroup;
		int num2 = Mathf.Min(labels.Count, num + perGroup);
		List<string> list = new List<string>();
		List<int> list2 = new List<int>();
		if (num > 0)
		{
			list.Add("◀");
			list2.Add(-2);
		}
		for (int i = num; i < num2; i++)
		{
			if (i > num)
			{
				list.Add(" |");
				list2.Add(-3);
			}
			list.Add(labels[i]);
			list2.Add(i);
		}
		if (num2 < labels.Count)
		{
			list.Add(" ▶");
			list2.Add(-1);
		}
		GUIStyle val = TextStyle();
		float num3 = 0f;
		foreach (string item in list)
		{
			num3 += val.CalcSize(new GUIContent(item)).x;
		}
		float num4 = (width - num3) / 2f;
		int result = -1;
		for (int j = 0; j < list.Count; j++)
		{
			int num5 = list2[j];
			bool flag = num5 >= 0 && num5 == selectedIndex;
			val.normal.textColor = ((whiteText != flag) ? Color.white : Color.black);
			float x2 = val.CalcSize(new GUIContent(list[j])).x;
			Rect val2 = new Rect(x + num4, y - 1f, x2, 14f);
			GUI.Label(val2, list[j], val);
			if (num5 != -3 && Hit(val2))
			{
				switch (num5)
				{
				case -2:
					group--;
					break;
				case -1:
					group++;
					break;
				default:
					result = num5;
					break;
				}
			}
			num4 += x2;
		}
		return result;
	}

	private static int CategoryColor(int value)
	{
		return (new int[10] { 0, -7353517, -1874292, -12338980, -1664967, -11711023, -3381933, -7127089, -12346660, -2668626 })[value];
	}

	private static void DrawPages(int count)
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Invalid comparison between Unknown and I4
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		List<string> list = new List<string>();
		for (int i = 0; i < Mathf.CeilToInt((float)count / 10f); i++)
		{
			list.Add((i + 1).ToString());
		}
		int num = DrawTextStrip(list, page, 279f, 507f, 406f, -12949590, whiteText: true, 10, ref pageGroup);
		if (num >= 0)
		{
			page = num;
		}
		if (count > 10 && (int)Event.current.type == 6)
		{
			Rect val = new Rect(275f, 95f, 412f, 430f);
			if (val.Contains(pointer))
			{
				page = Mathf.Clamp(page + ((Event.current.delta.y > 0f) ? 1 : (-1)), 0, (count - 1) / 10);
				Event.current.Use();
			}
		}
	}

	private static void DrawStorage()
	{
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Invalid comparison between Unknown and I4
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		List<CashStock> list = state.stock.FindAll((CashStock x) => x.characterId == 0);
		List<CashStock> list2 = state.stock.FindAll((CashStock x) => x.characterId != 0);
		lockerRow = Mathf.Clamp(lockerRow, 0, Mathf.Max(0, (list.Count - 1) / 6 - 1));
		inventoryRow = Mathf.Clamp(inventoryRow, 0, Mathf.Max(0, (list2.Count - 1) / 4 - 2));
		for (int num = 0; num < 12; num++)
		{
			int num2 = num + lockerRow * 6;
			int num3 = num + inventoryRow * 4;
			if (num2 < list.Count)
			{
				DrawStock(list[num2], LockerCell(num), locker: true);
			}
			if (num3 < list2.Count)
			{
				DrawStock(list2[num3], InventoryCell(num), locker: false);
			}
		}
		string[] array = new string[5] { "BtExEquip", "BtExConsume", "BtExInstall", "BtExEtc", "BtExTrunk" };
		for (int num4 = 0; num4 < array.Length; num4++)
		{
			bool flag = ExpansionFull(num4);
			bool enabled = GUI.enabled;
			if (flag)
			{
				GUI.enabled = false;
			}
			bool flag2 = Button("CSInventory/" + array[num4], 176f, 453 + 27 * num4, keepNormal && !flag);
			GUI.enabled = enabled;
			if (flag2)
			{
				CashResponse cashResponse = ((num4 < 4) ? Request(CashOperation.ExpandInventory, 0L, num4 + 1, null, notify: false) : Request(CashOperation.ExpandStorage, 0L, 0, null, notify: false));
				if (cashResponse != null)
				{
					NativeUtilPrompt.Notice(cashResponse.ok ? "增加了背包的容量。" : cashResponse.error);
				}
			}
		}
		if ((int)Event.current.type != 6)
		{
			return;
		}
		Rect val = new Rect(0f, 318f, 256f, 104f);
		if (val.Contains(pointer))
		{
			lockerRow += ((Event.current.delta.y > 0f) ? 1 : (-1));
			Event.current.Use();
			return;
		}
		val = new Rect(0f, 426f, 170f, 163f);
		if (val.Contains(pointer))
		{
			inventoryRow += ((Event.current.delta.y > 0f) ? 1 : (-1));
			Event.current.Use();
		}
	}

	private static int BagSlots(int type)
	{
		CharRecord charRecord = ((state == null) ? null : state.character?.record);
		if (charRecord == null)
		{
			return 0;
		}
		return type switch
		{
			3 => charRecord.setupslots, 
			2 => charRecord.useslots, 
			1 => charRecord.equipslots, 
			_ => charRecord.etcslots, 
		};
	}

	private static bool ExpansionFull(int index)
	{
		if (index >= 4)
		{
			return state.storageSlots + 4 > 48;
		}
		return BagSlots(index + 1) + 4 > 96;
	}

	private static void DrawStock(CashStock item, Rect rect, bool locker)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = NativeItemSource.Icon(item.itemId);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(rect.x, rect.yMax - (float)((Texture)val).height, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
		}
		if (item.quantity > 1)
		{
			GUI.Label(new Rect(rect.x, rect.yMax - 12f, 32f, 12f), item.quantity.ToString(), TextStyle(right: true));
		}
		if (stockSelection == item.id && stockSelectionLocker == locker)
		{
			UiKit.Fill(rect, Color32.op_Implicit(new Color32((byte)16, (byte)48, (byte)204, (byte)64)));
			Color32 val2 = new Color32((byte)16, (byte)48, (byte)204, (byte)170);
			UiKit.Fill(new Rect(rect.x, rect.y - 1f, 32f, 2f), Color32.op_Implicit(val2));
			UiKit.Fill(new Rect(rect.x, rect.yMax - 1f, 32f, 2f), Color32.op_Implicit(val2));
			UiKit.Fill(new Rect(rect.x - 1f, rect.y, 2f, 32f), Color32.op_Implicit(val2));
			UiKit.Fill(new Rect(rect.xMax - 1f, rect.y, 2f, 32f), Color32.op_Implicit(val2));
		}
		if (!Hit(rect))
		{
			return;
		}
		bool num = stockSelection == item.id && stockSelectionLocker == locker && Time.unscaledTime - stockClickAt < 0.4f;
		stockSelection = item.id;
		stockSelectionLocker = locker;
		stockClickAt = Time.unscaledTime;
		if (num)
		{
			if (item.itemId == 5222000)
			{
				surpriseVisible = true;
				surprisePhase = 0;
				surpriseRewardId = 0;
				surpriseBoxId = item.id;
			}
			else
			{
				Request(locker ? CashOperation.Withdraw : CashOperation.Deposit, item.id);
			}
		}
	}

	private static Texture2D GachaTex(string node)
	{
		return Art.Tex("wz://UI/UIWindow.img/CashGachapon/" + node);
	}

	private static void DrawSurprise()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Expected Obj, but got Unknown
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		if (!surpriseVisible)
		{
			return;
		}
		Vector2 val = new Vector2(315f, 214f);
		UiKit.Fill(new Rect(0f, 0f, 800f, 600f), new Color(0f, 0f, 0f, 0.45f));
		Texture2D val2 = GachaTex("backgrnd");
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(new Rect(val.x, val.y, (float)((Texture)val2).width, (float)((Texture)val2).height), (Texture)(object)val2);
		}
		Texture2D val3 = null;
		int num = 0;
		while ((Object)(object)GachaTex("EffectNormal/" + num) != (Object)null)
		{
			num++;
		}
		if (num > 0)
		{
			val3 = GachaTex("EffectNormal/" + (int)(Time.unscaledTime / 0.15f) % num);
		}
		if ((Object)(object)val3 != (Object)null)
		{
			GUI.DrawTexture(new Rect(val.x + 2f, val.y + 55f, (float)((Texture)val3).width, (float)((Texture)val3).height), (Texture)(object)val3);
		}
		if (surprisePhase == 1 && surpriseRewardId != 0)
		{
			Texture2D val4 = NativeItemSource.Icon(surpriseRewardId);
			if ((Object)(object)val4 != (Object)null)
			{
				GUI.DrawTexture(new Rect(val.x + 69f, val.y + 30f, (float)(((Texture)val4).width * 2), (float)(((Texture)val4).height * 2)), (Texture)(object)val4);
			}
			GUIStyle val5 = new GUIStyle(GUI.skin.label)
			{
				font = UiKit.Font,
				fontSize = 11,
				alignment = (TextAnchor)4,
				wordWrap = true
			};
			val5.normal.textColor = Color.black;
			GUI.Label(new Rect(val.x + 15f, val.y + 78f, 140f, 30f), WzNames.Item(surpriseRewardId), val5);
		}
		if (NativeUi.Button(new Rect(val.x + 67f, val.y + 132f, 35f, 16f), "UI/UIWindow.img/CashGachapon/" + ((surprisePhase == 0) ? "BtOpen" : "BtClose"), pointer))
		{
			if (surprisePhase == 0)
			{
				CashResponse cashResponse = Request(CashOperation.OpenSurprise, surpriseBoxId);
				if (cashResponse != null && cashResponse.ok)
				{
					surpriseRewardId = cashResponse.rewardItemId;
					surprisePhase = 1;
				}
			}
			else
			{
				surpriseVisible = false;
			}
		}
		if (NativeUi.Button(new Rect(val.x + 152f, val.y + 4f, 12f, 12f), "UI/UIWindow.img/CashGachapon/BtClose", pointer))
		{
			surpriseVisible = false;
		}
	}

	private static void DrawPreview()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		Image("Base/Preview/" + previewBackground, 24f, 40f);
		if (Hit(new Rect(24f, 40f, 212f, 165f)) || Hit(new Rect(95f, 5f, 30f, 25f)))
		{
			previewEnabled = !previewEnabled;
		}
		if (state.character != null && previewEnabled)
		{
			try
			{
				List<InventoryEntry> list = new List<InventoryEntry>(state.character.inventory);
				if (previewUndressed)
				{
					list.Clear();
				}
				if (previewItem / 1000000 == 1)
				{
					int slot = EquipSlot.SlotOf(previewItem);
					list.RemoveAll((InventoryEntry x) => x.position == slot && x.inventoryType == 1);
					list.Add(new InventoryEntry
					{
						itemId = previewItem,
						position = slot,
						inventoryType = 1
					});
				}
				string[] array = WzCharacter.CharacterIds(state.character.record, list);
				int frame = WzCharacter.AppearanceFrame(array, Time.unscaledTime);
				string key = string.Join("_", array) + "/" + frame;
				if (!previewFrames.TryGetValue(key, out var value))
				{
					value = WzCharacter.Appearance(string.Join("_", array), array, frame);
					previewFrames[key] = value;
				}
				if ((Object)(object)value != (Object)null)
				{
					GUI.BeginGroup(new Rect(24f, 40f, 212f, 165f));
					float num = 130f - value.pivot.x;
					Rect rect = value.rect;
					float num2 = 125f - (rect.height - value.pivot.y);
					rect = value.rect;
					float width = rect.width;
					rect = value.rect;
					GUI.DrawTexture(new Rect(num, num2, width, rect.height), (Texture)(object)value.texture);
					GUI.EndGroup();
				}
			}
			catch (Exception)
			{
			}
		}
		if (state.character != null)
		{
			GUI.Label(new Rect(50f, 268f, 175f, 16f), state.character.record.name, TextStyle(right: true));
			GUI.Label(new Rect(50f, 287f, 175f, 16f), GameSession.AccountName ?? "-", TextStyle(right: true));
		}
		bool enabled = GUI.enabled;
		GUI.enabled = false;
		Button("CSChar/BtBuyAvatar", 16f, 237f);
		GUI.enabled = enabled;
		if (Button("CSChar/BtDefaultAvatar", 101f, 237f, keepNormal))
		{
			previewItem = 0;
			previewUndressed = false;
		}
		if (Button("CSChar/BtTakeoffAvatar", 187f, 237f, keepNormal))
		{
			previewItem = 0;
			previewUndressed = true;
		}
	}

	private static void DrawPurchase()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02df: Unknown result type (might be due to invalid IL or missing references)
		//IL_030c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0318: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0338: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = new Vector2(257f, 202f);
		Image("CSNotice/0/0", val.x, val.y);
		Image("CSNotice/0/1", val.x, val.y + 21f);
		Image("CSNotice/0/2", val.x, val.y + 61f);
		Image("CSNotice/0/3", val.x, val.y + 66f);
		Image("CSNotice/0/4", val.x, val.y + 85f);
		CashCommodity cashCommodity = state.catalog.Find((CashCommodity x) => x.sn == selected);
		if (cashCommodity == null)
		{
			purchaseVisible = false;
			return;
		}
		Texture2D val2 = NativeItemSource.Icon(cashCommodity.itemId);
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(new Rect(val.x + 21f, val.y + 57f - (float)((Texture)val2).height, (float)((Texture)val2).width, (float)((Texture)val2).height), (Texture)(object)val2);
		}
		GUI.Label(new Rect(val.x + 63f, val.y + 27f, 202f, 16f), WzNames.Item(cashCommodity.itemId), TextStyle());
		GUI.Label(new Rect(val.x + 63f, val.y + 43f, 202f, 16f), PriceText(cashCommodity), TextStyle());
		GUIStyle val3 = TextStyle();
		val3.alignment = (TextAnchor)1;
		GUI.Label(new Rect(val.x, val.y + 73f, 286f, 16f), "确定要购买这个道具吗？", val3);
		int[] array = new int[3] { 1, 2, 0 };
		for (int num = 0; num < array.Length; num++)
		{
			int num2 = array[num];
			bool flag = (purchaseMask & currencies[num2]) != 0;
			Rect val4 = new Rect(val.x + 25f, val.y + 101f + (float)(num * 15), 12f, 12f);
			string text;
			if (flag)
			{
				text = ((num2 == currency) ? "1" : "0");
			}
			else
			{
				text = "2";
			}
			Texture2D val5 = Art.Tex("wz://UI/Basic.img/CheckBox/" + text);
			if ((Object)(object)val5 != (Object)null)
			{
				GUI.DrawTexture(val4, (Texture)(object)val5);
			}
			if (flag && Hit(new Rect(val4.x, val4.y, 230f, 14f)))
			{
				currency = num2;
			}
		}
		if (NativeUi.Button(new Rect(val.x + 157f, val.y + 159f, 47f, 19f), "UI/Basic.img/BtOK2", pointer))
		{
			CashResponse cashResponse = Request(CashOperation.Buy, 0L);
			if (cashResponse != null && cashResponse.ok)
			{
				purchaseVisible = false;
			}
		}
		if (NativeUi.Button(new Rect(val.x + 207f, val.y + 159f, 47f, 19f), "UI/Basic.img/BtCancel2", pointer))
		{
			purchaseVisible = false;
		}
	}

	private static void DrawGift()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = new Vector2(267f, 215f);
		Image("CSGift/backgrnd", val.x, val.y);
		GUIStyle val2 = TextStyle();
		target = GUI.TextField(new Rect(val.x + 84f, val.y + 52f, 122f, 13f), target, 13, val2);
		message = GUI.TextField(new Rect(val.x + 26f, val.y + 76f, 210f, 13f), message, 34, val2);
		messageSecond = GUI.TextField(new Rect(val.x + 26f, val.y + 92f, 210f, 13f), messageSecond, 34, val2);
		bool enabled = GUI.enabled;
		GUI.enabled = false;
		NativeUi.Button(new Rect(val.x + 56f, val.y + 139f, 53f, 18f), "UI/CashShop.img/CSGift/BtGuild", pointer);
		NativeUi.Button(new Rect(val.x + 110f, val.y + 139f, 53f, 18f), "UI/CashShop.img/CSGift/BtBuddy", pointer);
		GUI.enabled = enabled;
		if (NativeUi.Button(new Rect(val.x + 168f, val.y + 139f, 47f, 19f), "UI/Basic.img/BtOK2", pointer))
		{
			birthday = "";
			birthdayVisible = true;
		}
		if (NativeUi.Button(new Rect(val.x + 210f, val.y + 139f, 47f, 19f), "UI/Basic.img/BtCancel2", pointer))
		{
			giftVisible = false;
		}
	}

	private static void DrawBirthday()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected Obj, but got Unknown
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		GUIStyle val = TextStyle();
		val.wordWrap = true;
		float num = Mathf.Ceil(val.CalcHeight(new GUIContent("For the sake of privacy,\nplease enter your 8-digit birthday code.\nex) year/month/day -> 19820911"), 225f));
		float num2 = num + 99f;
		Vector2 val2 = new Vector2(267f, (600f - num2) / 2f);
		Texture2D val3 = Art.Tex("wz://UI/Basic.img/Notice3/t");
		Texture2D val4 = Art.Tex("wz://UI/Basic.img/Notice3/c");
		Texture2D val5 = Art.Tex("wz://UI/Basic.img/Notice4/s");
		if ((Object)(object)val3 != (Object)null)
		{
			GUI.DrawTexture(new Rect(val2.x, val2.y, 266f, 21f), (Texture)(object)val3);
		}
		for (float num3 = 21f; num3 < num2 - 78f; num3 += 20f)
		{
			if ((Object)(object)val4 != (Object)null)
			{
				GUI.DrawTexture(new Rect(val2.x, val2.y + num3, 266f, 20f), (Texture)(object)val4);
			}
		}
		if ((Object)(object)val5 != (Object)null)
		{
			GUI.DrawTexture(new Rect(val2.x, val2.y + num2 - 78f, 266f, 78f), (Texture)(object)val5);
		}
		GUI.Label(new Rect(val2.x + 20f, val2.y + 22f, 225f, num), "For the sake of privacy,\nplease enter your 8-digit birthday code.\nex) year/month/day -> 19820911", val);
		UiKit.Fill(new Rect(val2.x + 19f, val2.y + num + 24f, 229f, 18f), Color.black);
		UiKit.Fill(new Rect(val2.x + 20f, val2.y + num + 25f, 227f, 16f), Color.white);
		birthday = GUI.TextField(new Rect(val2.x + 22f, val2.y + num + 27f, 224f, 14f), birthday, 8, TextStyle());
		if (NativeUi.Button(new Rect(val2.x + 212f, val2.y + num2 - 26f, 43f, 19f), "UI/UIWindow.img/UtilDlgEx/BtOK", pointer))
		{
			if (birthday.Length != 8 || !int.TryParse(birthday, out var _))
			{
				BottomBar.Toast("请输入8位生日代码");
				return;
			}
			CashResponse cashResponse = Request(CashOperation.Gift, 0L);
			if (cashResponse != null && cashResponse.ok)
			{
				giftVisible = (birthdayVisible = false);
			}
		}
		if (NativeUi.Button(new Rect(val2.x + 9f, val2.y + num2 - 26f, 43f, 19f), "UI/UIWindow.img/UtilDlgEx/BtClose", pointer))
		{
			birthdayVisible = false;
		}
	}

	private static void DrawBalances()
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		int[] array = new int[3] { state.credit, state.points, state.prepaid };
		int[] array2 = new int[3] { 9, 23, 38 };
		for (int i = 0; i < 3; i++)
		{
			UiKit.Fill(new Rect(374f, (float)(530 + array2[i] + 2), 107f, 11f), Color32.op_Implicit(new Color32((byte)243, (byte)244, (byte)245, byte.MaxValue)));
			GUI.Label(new Rect(374f, (float)(530 + array2[i]), 100f, 14f), array[i].ToString("N0"), TextStyle(right: true));
		}
		if (Button("CSStatus/BtCheck", 543f, 543f, keepNormal))
		{
			Request(CashOperation.Read, 0L);
		}
		bool enabled = GUI.enabled;
		GUI.enabled = false;
		Button("CSStatus/BtCharge", 502f, 543f);
		Button("CSStatus/BtCoupon", 584f, 543f);
		GUI.enabled = enabled;
	}
}
