using System;
using System.Collections.Generic;
using MapleDash.Wz;
using Server.Bot.Trade;
using UnityEngine;

public static class NativeMerchantWindow
{
	private static MerchantSnapshot shop;

	private static Vector2 position = new Vector2(120f, 10f);

	private static Vector2 grab;

	private static bool dragging;

	private static int shopSelection = -1;

	private static int bagSelection = -1;

	private static float shopScroll;

	private static float bagScroll;

	private static int generation;

	private static double nextRead;

	public static bool Visible => shop != null;

	public static bool HasMerchant => shop != null;

	public static void Reset()
	{
		shop = null;
		shopSelection = (bagSelection = -1);
		shopScroll = (bagScroll = 0f);
		dragging = false;
		generation++;
	}

	public static void Open(int permitItemId)
	{
		if (!GameSession.InGame)
		{
			return;
		}
		if (Profile.LastMapId / 1000000 != 910 || Profile.LastMapId <= 910000000)
		{
			BottomBar.Toast("只能在自由市场里开雇佣商店");
			return;
		}
		Reset();
		MerchantResponse merchantResponse = Request(MerchantOperation.Snapshot);
		if (merchantResponse?.shop != null)
		{
			shop = merchantResponse.shop;
			return;
		}
		MerchantResponse merchantResponse2 = Request(MerchantOperation.Box);
		if (merchantResponse2 == null || !merchantResponse2.ok)
		{
			BottomBar.Toast(merchantResponse2?.error ?? "商店盒无法打开");
			return;
		}
		if (!merchantResponse2.box)
		{
			BottomBar.Toast(merchantResponse2.error);
			return;
		}
		MerchantResponse merchantResponse3 = GameSession.Net.Merchant(new MerchantRequest
		{
			characterId = GameSession.CharacterId,
			operation = MerchantOperation.Create,
			price = permitItemId,
			description = ""
		});
		if (!merchantResponse3.ok)
		{
			BottomBar.Toast(merchantResponse3.error);
			return;
		}
		shop = merchantResponse3.shop;
		RefreshInventory(merchantResponse3);
	}

	public static void Retrieve()
	{
		if (!GameSession.InGame)
		{
			return;
		}
		GameSession.SaveToDb();
		MerchantResponse merchantResponse = GameSession.Net.Merchant(new MerchantRequest
		{
			characterId = GameSession.CharacterId,
			operation = MerchantOperation.Retrieve
		});
		if (!merchantResponse.ok)
		{
			BottomBar.Toast(merchantResponse.error);
			return;
		}
		if (merchantResponse.inventory != null)
		{
			GameSession.Character.inventory = merchantResponse.inventory;
			GameSession.ApplyInventoryToProfile();
		}
		if (merchantResponse.meso >= 0)
		{
			GameSession.Character.record.meso = merchantResponse.meso;
			Profile.Data.gold = merchantResponse.meso;
		}
		Profile.Save();
		BottomBar.Toast((merchantResponse.code == 30) ? "已取回雇佣商店寄存" : merchantResponse.error);
	}

	public static void Close()
	{
		if (shop != null && GameSession.InGame && shop.items.Count == 0)
		{
			Request(MerchantOperation.Close);
		}
		Reset();
	}

	private static MerchantResponse Request(MerchantOperation operation, int slot = -1, int quantity = 0, int bundles = 0, int price = 0, int inventoryType = 0, int itemPosition = 0)
	{
		if (!GameSession.InGame)
		{
			return new MerchantResponse
			{
				ok = false,
				error = "请先选择角色"
			};
		}
		int num = generation;
		GameSession.SaveToDb();
		MerchantResponse merchantResponse = GameSession.Net.Merchant(new MerchantRequest
		{
			characterId = GameSession.CharacterId,
			operation = operation,
			slot = slot,
			quantity = quantity,
			bundles = bundles,
			price = price,
			inventoryType = inventoryType,
			itemPosition = itemPosition
		});
		if (num != generation)
		{
			return merchantResponse;
		}
		if (!merchantResponse.ok)
		{
			if (!string.IsNullOrEmpty(merchantResponse.error))
			{
				BottomBar.Toast(merchantResponse.error);
			}
			return merchantResponse;
		}
		if (merchantResponse.shop != null)
		{
			shop = merchantResponse.shop;
		}
		RefreshInventory(merchantResponse);
		return merchantResponse;
	}

	private static void RefreshInventory(MerchantResponse res)
	{
		if (res?.inventory != null)
		{
			GameSession.Character.inventory = res.inventory;
			GameSession.ApplyInventoryToProfile();
		}
		if (res != null && res.meso >= 0)
		{
			GameSession.Character.record.meso = res.meso;
			Profile.Data.gold = res.meso;
		}
		if (res?.inventory != null || (res != null && res.meso >= 0))
		{
			Profile.Save();
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
		int version = generation;
		int perBundle = 1;
		int bundles = 1;
		Action askPrice = () =>
		{
			NativeUtilPrompt.Number("每份卖多少金币？", 1, 1, int.MaxValue, (int price) =>
			{
				if (version == generation)
				{
					Request(MerchantOperation.AddItem, 0, perBundle, bundles, price, item.inventoryType, item.position);
					bagSelection = -1;
				}
			});
		};
		Action askBundles = () =>
		{
			NativeUtilPrompt.Number("一共摆几份？", 1, 1, 2000, (int value) =>
			{
				bundles = value;
				if (version == generation)
				{
					askPrice();
				}
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
			if (version == generation)
			{
				askBundles();
			}
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
		//IL_092f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Invalid comparison between Unknown and I4
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_0308: Unknown result type (might be due to invalid IL or missing references)
		//IL_0337: Unknown result type (might be due to invalid IL or missing references)
		//IL_037a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0393: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_059f: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0609: Unknown result type (might be due to invalid IL or missing references)
		//IL_073a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0744: Unknown result type (might be due to invalid IL or missing references)
		//IL_0468: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0669: Unknown result type (might be due to invalid IL or missing references)
		//IL_0780: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_07af: Unknown result type (might be due to invalid IL or missing references)
		//IL_0702: Unknown result type (might be due to invalid IL or missing references)
		//IL_0707: Unknown result type (might be due to invalid IL or missing references)
		//IL_0810: Unknown result type (might be due to invalid IL or missing references)
		//IL_0515: Unknown result type (might be due to invalid IL or missing references)
		//IL_0848: Unknown result type (might be due to invalid IL or missing references)
		//IL_0568: Unknown result type (might be due to invalid IL or missing references)
		//IL_056d: Unknown result type (might be due to invalid IL or missing references)
		//IL_08b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_087f: Unknown result type (might be due to invalid IL or missing references)
		//IL_08e7: Unknown result type (might be due to invalid IL or missing references)
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
			MerchantResponse merchantResponse = GameSession.Net.Merchant(new MerchantRequest
			{
				characterId = GameSession.CharacterId,
				operation = MerchantOperation.Snapshot
			});
			if (merchantResponse?.shop != null)
			{
				shop = merchantResponse.shop;
			}
			else if (merchantResponse != null && merchantResponse.ok && merchantResponse.shop == null)
			{
				Reset();
			}
		}
		float s = BottomBar.S;
		position = NativeUi.ClampWindowPosition(position, new Vector2(525f, 553f), s);
		NativeUi.RegisterWindow("NativeMerchantWindow", new Rect(position.x * s, position.y * s, 525f * s, 553f * s), () => Visible);
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
				Image("backgrnd6", 0f, 0f);
				ImgNode imgNode = WzAssets.Node("Item/Cash/0503.img/" + shop.permitId.ToString("D8") + "/employee/stand/0");
				if (imgNode != null)
				{
					Texture2D val3 = WzAssets.Texture(imgNode);
					if ((Object)(object)val3 != (Object)null)
					{
						GUI.DrawTexture(new Rect((float)(53 - ((Texture)val3).width / 2), (float)(127 - ((Texture)val3).height), (float)((Texture)val3).width, (float)((Texture)val3).height), (Texture)(object)val3);
					}
				}
				GUI.Label(new Rect(23f, 9f, 460f, 16f), (string.IsNullOrEmpty(shop.description) ? "雇佣商店" : shop.description) + (shop.open ? "（营业中）" : "（准备中）"), Text(right: false, (TextAnchor)0));
				GUI.Label(new Rect(9f, 135f, 96f, 16f), shop.ownerName, Text(right: false, (TextAnchor)1));
				GUI.Label(new Rect(9f, 147f, 180f, 14f), "店内存货 · 金币 " + shop.merchantMeso.ToString("N0"), Text(right: false, (TextAnchor)0));
				shopScroll = NativeUi.Scroll4(new Rect(211f, 158f, 15f, 197f), new Rect(7f, 157f, 199f, 200f), shopScroll, Math.Max(0, shop.items.Count - 5), 1f, pointer);
				for (int num = 0; num < 5; num++)
				{
					int num2 = num + (int)shopScroll;
					if (num2 >= shop.items.Count)
					{
						break;
					}
					MerchantSlotData merchantSlotData = shop.items[num2];
					float num3 = 157 + 40 * num;
					if (num2 == shopSelection)
					{
						Image("select", 44f, num3);
					}
					if (merchantSlotData.item != null)
					{
						Icon(merchantSlotData.item.itemId, 8f, num3 + 34f);
					}
					GUI.Label(new Rect(48f, num3 + 1f, 152f, 16f), (merchantSlotData.item == null) ? "?" : ItemCatalog.Name(merchantSlotData.item.itemId), Text(right: false, (TextAnchor)0));
					GUI.Label(new Rect(48f, num3 + 19f, 152f, 16f), (merchantSlotData.item?.quantity ?? 0) + " 个 / " + merchantSlotData.price.ToString("N0") + " 金币", Text(right: false, (TextAnchor)0));
					GUI.Label(new Rect(176f, num3 + 23f, 24f, 12f), "x" + merchantSlotData.bundles, Text(right: true, (TextAnchor)0));
					if (merchantSlotData.bundles < 1)
					{
						Image("soldout", 7f, num3);
					}
					if (Hit(new Rect(7f, num3, 199f, 35f), pointer))
					{
						shopSelection = num2;
					}
				}
				GUI.Label(new Rect(236f, 135f, 200f, 14f), "背包 · 选中后点「上架」", Text(right: false, (TextAnchor)0));
				List<InventoryEntry> list = Bag();
				bagScroll = NativeUi.Scroll4(new Rect(508f, 158f, 15f, 197f), new Rect(236f, 157f, 270f, 200f), bagScroll, Math.Max(0, list.Count - 10), 1f, pointer);
				for (int num4 = 0; num4 < 10; num4++)
				{
					int num5 = num4 + (int)bagScroll;
					if (num5 >= list.Count)
					{
						break;
					}
					InventoryEntry inventoryEntry = list[num5];
					float num6 = 157 + 20 * num4;
					if (num5 == bagSelection)
					{
						GUI.Box(new Rect(236f, num6, 270f, 19f), "");
					}
					Icon(inventoryEntry.itemId, 246f, num6 + 16f);
					GUI.Label(new Rect(262f, num6 + 2f, 240f, 14f), ItemCatalog.Name(inventoryEntry.itemId) + ((inventoryEntry.quantity > 1) ? (" x" + inventoryEntry.quantity) : ""), Text(right: false, (TextAnchor)0));
					if (Hit(new Rect(236f, num6, 270f, 19f), pointer))
					{
						bagSelection = num5;
					}
				}
				if (NativeUi.Button(new Rect(144f, 110f, 60f, 19f), "UI/UIWindow.img/PersonalShop/BtExit", pointer))
				{
					Close();
					return;
				}
				float num7 = 366f;
				bool flag = !shop.open;
				if (flag && GUI.Button(new Rect(7f, num7, 64f, 20f), "上架", Small()))
				{
					Stock();
				}
				if (flag && GUI.Button(new Rect(75f, num7, 64f, 20f), "取回", Small()) && shopSelection >= 0)
				{
					MerchantSlotData merchantSlotData2 = shop.items[shopSelection];
					Request(MerchantOperation.TakeItemBack, merchantSlotData2.slot);
					shopSelection = -1;
				}
				if (flag && GUI.Button(new Rect(143f, num7, 64f, 20f), "开始营业", Small()))
				{
					Request(MerchantOperation.MaintenanceOff);
				}
				if (!flag && GUI.Button(new Rect(7f, num7, 64f, 20f), "结束营业", Small()))
				{
					Request(MerchantOperation.SetOpen, 0);
				}
				if (!flag && GUI.Button(new Rect(75f, num7, 64f, 20f), "整理", Small()))
				{
					Request(MerchantOperation.Organize);
				}
				if (GUI.Button(new Rect(211f, num7, 64f, 20f), "提金币", Small()))
				{
					Request(MerchantOperation.WithdrawMesos);
				}
				if (!GUI.Button(new Rect(279f, num7, 64f, 20f), "关店", Small()))
				{
					return;
				}
				int version = generation;
				NativeUtilPrompt.Confirm("要关闭雇佣商店吗？\n装不下的道具会留在弗利德里那里寄存。", (bool yes) =>
				{
					if (yes && version == generation)
					{
						Request(MerchantOperation.Close);
						Reset();
					}
				});
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

	static NativeMerchantWindow()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
	}
}
