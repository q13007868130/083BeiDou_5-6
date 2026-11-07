using System;
using System.Collections.Generic;
using MapleDash.Wz;
using Server.Bot.Trade;
using UnityEngine;

public static class NativePlayerShopWindow
{
	private static PlayerShopInfo shop;

	private static long meso;

	private static Vector2 position = new Vector2(100f, 20f);

	private static Vector2 grab;

	private static bool dragging;

	private static int shopSelection = -1;

	private static int bagSelection = -1;

	private static int page;

	private static float shopScroll;

	private static float bagScroll;

	private static string chatInput = "";

	private static double nextRead;

	private const float W = 525f;

	private const float H = 553f;

	private static readonly string[] pages = new string[3] { "背包", "已售出", "聊天" };

	public static bool Visible => shop != null;

	public static bool HasShop => shop != null;

	public static void Reset()
	{
		shop = null;
		bagSelection = (shopSelection = -1);
		shopScroll = (bagScroll = 0f);
		page = 0;
		chatInput = "";
		dragging = false;
	}

	public static void Open(int permitItemId)
	{
		if (!GameSession.InGame)
		{
			return;
		}
		if (Profile.LastMapId / 1000000 != 910 || Profile.LastMapId <= 910000000)
		{
			BottomBar.Toast("只能在自由市场里开个人商店");
		}
		else
		{
			if (Request(PlayerShopOperation.Snapshot)?.shop != null)
			{
				return;
			}
			NativeUtilPrompt.Text("商店名称（最多 40 字）", "", 1, 40, (string text) =>
			{
				PlayerShopResponse playerShopResponse = Request(PlayerShopOperation.Create, -1, 0, 0, 0, 0, 0, permitItemId, text);
				if (playerShopResponse != null && !playerShopResponse.ok)
				{
					BottomBar.Toast(playerShopResponse.error);
				}
			});
		}
	}

	public static void Close()
	{
		if (shop != null && GameSession.InGame)
		{
			Request(PlayerShopOperation.Close);
		}
		Reset();
	}

	private static PlayerShopResponse Request(PlayerShopOperation operation, int slot = -1, int inventoryType = 0, int itemPosition = 0, int perBundle = 0, int bundles = 0, int price = 0, int permitItemId = 0, string description = "", string name = "", string text = "")
	{
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		if (!GameSession.InGame)
		{
			return new PlayerShopResponse
			{
				ok = false,
				error = "请先选择角色"
			};
		}
		GameSession.SaveToDb();
		GameLoop i = GameLoop.I;
		PlayerCtrl playerCtrl = (((Object)(object)i == (Object)null) ? null : i.player);
		PlayerShopResponse playerShopResponse = GameSession.Net.PlayerShop(new PlayerShopRequest
		{
			characterId = GameSession.CharacterId,
			operation = operation,
			slot = slot,
			inventoryType = inventoryType,
			itemPosition = itemPosition,
			perBundle = perBundle,
			bundles = bundles,
			price = price,
			permitId = permitItemId,
			description = description,
			name = name,
			text = text,
			x = ((!((Object)(object)playerCtrl == (Object)null)) ? Mathf.RoundToInt(((Component)playerCtrl).transform.position.x * 40f) : 0),
			y = ((!((Object)(object)playerCtrl == (Object)null)) ? Mathf.RoundToInt((0f - ((Component)playerCtrl).transform.position.y) * 40f) : 0)
		});
		if (playerShopResponse == null)
		{
			return null;
		}
		Apply(playerShopResponse);
		return playerShopResponse;
	}

	private static void Apply(PlayerShopResponse res)
	{
		shop = res.shop;
		if (res.inventory != null)
		{
			GameSession.Character.inventory = res.inventory;
			GameSession.ApplyInventoryToProfile();
		}
		if (res.meso >= 0)
		{
			meso = res.meso;
			GameSession.Character.record.meso = res.meso;
			Profile.Data.gold = res.meso;
		}
		if (res.inventory != null || res.meso >= 0)
		{
			Profile.Save();
		}
		if (res.shop == null)
		{
			Reset();
		}
	}

	private static List<InventoryEntry> Bag()
	{
		List<InventoryEntry> list = new List<InventoryEntry>();
		FullCharacter character = GameSession.Character;
		if (character == null)
		{
			return list;
		}
		foreach (InventoryEntry item in character.inventory)
		{
			if (item.position > 0 && item.inventoryType > 0)
			{
				list.Add(item);
			}
		}
		list.Sort((InventoryEntry a, InventoryEntry b) => (a.inventoryType == b.inventoryType) ? a.position.CompareTo(b.position) : a.inventoryType.CompareTo(b.inventoryType));
		return list;
	}

	private static void Stock()
	{
		List<InventoryEntry> list = Bag();
		if (bagSelection < 0 || bagSelection >= list.Count)
		{
			return;
		}
		InventoryEntry item = list[bagSelection];
		bool flag = TradeItemRules.Rechargeable(item.itemId);
		int perBundle = 1;
		int bundles = 1;
		Action askPrice = () =>
		{
			NativeUtilPrompt.Number("每份卖多少金币？", 1, 1, int.MaxValue, (int price) =>
			{
				Request(PlayerShopOperation.AddItem, -1, item.inventoryType, item.position, perBundle, bundles, price);
				bagSelection = -1;
			});
		};
		Action askBundles = () =>
		{
			NativeUtilPrompt.Number("一共摆几份？（每格上限 2000 个）", 1, 1, Math.Max(1, Math.Min(2000 / Math.Max(1, perBundle), 2000)), (int value) =>
			{
				bundles = value;
				askPrice();
			});
		};
		if (flag || item.quantity <= 1)
		{
			perBundle = (flag ? 1 : item.quantity);
			askPrice();
			return;
		}
		NativeUtilPrompt.Number("每份多少个？", 1, 1, item.quantity, (int value) =>
		{
			perBundle = value;
			askBundles();
		});
	}

	private static GUIStyle Text(bool right = false, TextAnchor align = (TextAnchor)0)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected Obj, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected Obj, but got Unknown
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = 11,
			padding = new RectOffset(),
			clipping = (TextClipping)1,
			alignment = align
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

	private static void Image(string name, float x, float y)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://UI/UIWindow.img/PersonalShop/" + name);
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

	public static void Draw()
	{
		//IL_0b37: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Invalid comparison between Unknown and I4
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_039a: Unknown result type (might be due to invalid IL or missing references)
		//IL_054b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0564: Unknown result type (might be due to invalid IL or missing references)
		//IL_0584: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0880: Unknown result type (might be due to invalid IL or missing references)
		//IL_08aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_08d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0957: Unknown result type (might be due to invalid IL or missing references)
		//IL_0447: Unknown result type (might be due to invalid IL or missing references)
		//IL_083e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0983: Unknown result type (might be due to invalid IL or missing references)
		//IL_0749: Unknown result type (might be due to invalid IL or missing references)
		//IL_0772: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0623: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_067d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0682: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a6b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a44: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ac3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0af5: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		if (!GameSession.InGame)
		{
			Reset();
			return;
		}
		if (Time.realtimeSinceStartupAsDouble >= nextRead)
		{
			nextRead = Time.realtimeSinceStartupAsDouble + 0.5;
			PlayerShopResponse playerShopResponse = GameSession.Net.PlayerShop(new PlayerShopRequest
			{
				characterId = GameSession.CharacterId,
				operation = PlayerShopOperation.Snapshot
			});
			if (playerShopResponse != null)
			{
				Apply(playerShopResponse);
			}
		}
		float s = BottomBar.S;
		position = NativeUi.ClampWindowPosition(position, new Vector2(525f, 553f), s);
		NativeUi.RegisterWindow("NativePlayerShopWindow", new Rect(position.x * s, position.y * s, 525f * s, 553f * s), () => Visible);
		Vector2 val = Pointer.Pos / s;
		Matrix4x4 matrix = GUI.matrix;
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(s, s, 1f));
		try
		{
			if (GUI.enabled && (int)Event.current.type == 7 && Pointer.Pressed)
			{
				Rect val2 = new Rect(position.x, position.y, 500f, 22f);
				if (val2.Contains(val))
				{
					dragging = true;
					grab = val - position;
				}
			}
			if (dragging && Pointer.Down)
			{
				position = NativeUi.ClampWindowPosition(val - grab, new Vector2(525f, 553f), s);
			}
			if (!Pointer.Down)
			{
				dragging = false;
			}
			GUI.BeginGroup(new Rect(position.x, position.y, 525f, 553f));
			try
			{
				Vector2 pointer = val - position;
				Image("backgrnd", 0f, 0f);
				GUI.Label(new Rect(23f, 9f, 460f, 16f), string.IsNullOrEmpty(shop.description) ? "个人商店" : shop.description, Text(right: false, (TextAnchor)0));
				GUI.Label(new Rect(9f, 135f, 96f, 16f), shop.ownerName, Text(right: false, (TextAnchor)1));
				if (NativeUi.Button(new Rect(144f, 110f, 60f, 19f), "UI/UIWindow.img/PersonalShop/BtExit", pointer))
				{
					Close();
					return;
				}
				shopScroll = NativeUi.Scroll4(new Rect(211f, 158f, 15f, 197f), new Rect(7f, 157f, 199f, 200f), shopScroll, Math.Max(0, shop.items.Count - 5), 1f, pointer);
				for (int num = 0; num < 5; num++)
				{
					int num2 = num + (int)shopScroll;
					if (num2 >= shop.items.Count)
					{
						break;
					}
					PlayerShopSlotData playerShopSlotData = shop.items[num2];
					float num3 = 157 + 40 * num;
					if (num2 == shopSelection)
					{
						Image("select", 44f, num3);
					}
					if (playerShopSlotData.item != null)
					{
						Icon(playerShopSlotData.item.itemId, 8f, num3 + 34f);
					}
					GUI.Label(new Rect(48f, num3 + 1f, 152f, 16f), (playerShopSlotData.item == null) ? "?" : ItemCatalog.Name(playerShopSlotData.item.itemId), Text(right: false, (TextAnchor)0));
					GUI.Label(new Rect(48f, num3 + 19f, 152f, 16f), (playerShopSlotData.item?.quantity ?? 0) + " 个 / " + playerShopSlotData.price.ToString("N0") + " 金币", Text(right: false, (TextAnchor)0));
					GUI.Label(new Rect(176f, num3 + 23f, 24f, 12f), "x" + playerShopSlotData.bundles, Text(right: true, (TextAnchor)0));
					if (!playerShopSlotData.exists || playerShopSlotData.bundles < 1)
					{
						Image("soldout", 7f, num3);
					}
					if (Hit(new Rect(7f, num3, 199f, 35f), pointer))
					{
						shopSelection = num2;
					}
				}
				for (int num4 = 0; num4 < pages.Length; num4++)
				{
					if (GUI.Button(new Rect((float)(236 + 90 * num4), 133f, 84f, 18f), pages[num4], Small()) && page != num4)
					{
						page = num4;
						bagSelection = -1;
					}
				}
				if (page == 0)
				{
					List<InventoryEntry> list = Bag();
					bagScroll = NativeUi.Scroll4(new Rect(508f, 158f, 15f, 197f), new Rect(236f, 157f, 270f, 200f), bagScroll, Math.Max(0, list.Count - 10), 1f, pointer);
					for (int num5 = 0; num5 < 10; num5++)
					{
						int num6 = num5 + (int)bagScroll;
						if (num6 >= list.Count)
						{
							break;
						}
						InventoryEntry inventoryEntry = list[num6];
						float num7 = 157 + 20 * num5;
						if (num6 == bagSelection)
						{
							GUI.Box(new Rect(236f, num7, 270f, 19f), "");
						}
						Icon(inventoryEntry.itemId, 246f, num7 + 16f);
						GUI.Label(new Rect(262f, num7 + 2f, 240f, 14f), ItemCatalog.Name(inventoryEntry.itemId) + ((inventoryEntry.quantity > 1) ? (" x" + inventoryEntry.quantity) : ""), Text(right: false, (TextAnchor)0));
						if (Hit(new Rect(236f, num7, 270f, 19f), pointer))
						{
							bagSelection = num6;
						}
					}
				}
				else if (page == 1)
				{
					GUI.Label(new Rect(236f, 157f, 270f, 14f), "买家 / 物品 / 实收 " + shop.sold.Count + " 笔", Text(right: false, (TextAnchor)0));
					for (int num8 = 0; num8 < 9 && num8 < shop.sold.Count; num8++)
					{
						PlayerShopSoldData playerShopSoldData = shop.sold[shop.sold.Count - 1 - num8];
						float num9 = 175 + 20 * num8;
						GUI.Label(new Rect(236f, num9, 74f, 14f), playerShopSoldData.buyer, Text(right: false, (TextAnchor)0));
						GUI.Label(new Rect(310f, num9, 130f, 14f), ItemCatalog.Name(playerShopSoldData.itemId) + " x" + playerShopSoldData.quantity, Text(right: false, (TextAnchor)0));
						GUI.Label(new Rect(440f, num9, 66f, 14f), playerShopSoldData.meso.ToString("N0"), Text(right: true, (TextAnchor)0));
					}
				}
				else
				{
					for (int num10 = 0; num10 < 9; num10++)
					{
						int num11 = shop.chat.Count - 9 + num10;
						if (num11 >= 0)
						{
							GUI.Label(new Rect(236f, (float)(175 + 14 * num10), 270f, 13f), shop.chat[num11], Text(right: false, (TextAnchor)0));
						}
					}
					GUI.Label(new Rect(236f, 306f, 270f, 13f), "店铺聊天（单机没有其他真人）", Text(right: false, (TextAnchor)0));
					chatInput = GUI.TextField(new Rect(238f, 323f, 231f, 15f), chatInput, 40);
					if (GUI.Button(new Rect(472f, 321f, 40f, 18f), "说", Small()) && chatInput.Length > 0)
					{
						string text = chatInput;
						chatInput = "";
						Request(PlayerShopOperation.Chat, -1, 0, 0, 0, 0, 0, 0, "", "", text);
					}
				}
				float num12 = 366f;
				bool flag = !shop.open;
				bool enabled = GUI.enabled;
				GUI.enabled = enabled & flag;
				if (GUI.Button(new Rect(7f, num12, 64f, 20f), "上架", Small()))
				{
					Stock();
				}
				if (GUI.Button(new Rect(75f, num12, 64f, 20f), "取回", Small()) && shopSelection >= 0)
				{
					Request(PlayerShopOperation.TakeItemBack, shop.items[shopSelection].slot);
					shopSelection = -1;
				}
				GUI.enabled = enabled;
				if (flag && GUI.Button(new Rect(143f, num12, 78f, 20f), "开始营业", Small()))
				{
					Request(PlayerShopOperation.Open);
				}
				if (!flag)
				{
					GUI.Label(new Rect(143f, num12 + 3f, 78f, 16f), "营业中", Text(right: false, (TextAnchor)1));
				}
				if (GUI.Button(new Rect(225f, num12, 78f, 20f), "拉黑", Small()))
				{
					NativeUtilPrompt.Text("把谁请出店铺？", "", 1, 13, (string name) =>
					{
						Request(PlayerShopOperation.Ban, -1, 0, 0, 0, 0, 0, 0, "", name);
					});
				}
				if (GUI.Button(new Rect(307f, num12, 78f, 20f), "关闭店铺", Small()))
				{
					Close();
				}
				GUI.Label(new Rect(7f, num12 + 24f, 400f, 14f), "金币 " + meso.ToString("N0") + (flag ? "（准备中，可自由上下架）" : "（营业中，货架锁定）"), Text(right: false, (TextAnchor)0));
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

	static NativePlayerShopWindow()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
	}
}
