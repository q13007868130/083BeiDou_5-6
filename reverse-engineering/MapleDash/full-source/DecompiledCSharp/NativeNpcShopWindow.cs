using System;
using System.Collections.Generic;
using MapleDash.Js;
using MapleDash.Wz;
using Server.Bot.Trade;
using UnityEngine;

public static class NativeNpcShopWindow
{
	private const string Root = "UI/UIWindow.img/Shop/";

	private static NpcShopResponse state;

	private static Vector2 position = new Vector2(100f, 70f);

	private static Vector2 grab;

	private static bool dragging;

	private static int buySelection = -1;

	private static int sellSelection = -1;

	private static int sellTab;

	private static float buyScroll;

	private static float sellScroll;

	private static int generation;

	public static bool Visible => state?.opened ?? false;

	public static bool HasNpc(int id)
	{
		if (GameSession.InGame)
		{
			return GameSession.Net.HasNpcShop(id);
		}
		return false;
	}

	public static void Reset()
	{
		state = null;
		dragging = false;
		buySelection = (sellSelection = -1);
		buyScroll = (sellScroll = 0f);
		generation++;
	}

	public static void Close()
	{
		if (Visible && GameSession.InGame)
		{
			GameSession.Net.NpcShop(new NpcShopRequest
			{
				operation = NpcShopOperation.Close,
				characterId = GameSession.CharacterId
			});
		}
		Reset();
	}

	public static void OpenNpc(int id)
	{
		Open(id, shop: false, fallback: false);
	}

	public static void OpenShop(int id, bool fallback)
	{
		Open(id, shop: true, fallback);
	}

	public static void OpenAndroid(int templateId)
	{
		if (GameSession.InGame && templateId != 0)
		{
			Close();
			GameSession.SaveToDb();
			state = GameSession.Net.NpcShop(new NpcShopRequest
			{
				characterId = GameSession.CharacterId,
				id = 9201060,
				npcTemplateId = templateId,
				operation = NpcShopOperation.OpenShop
			});
			sellTab = 0;
			if (!state.ok)
			{
				BottomBar.Toast(state.error);
				Reset();
			}
		}
	}

	private static void Open(int id, bool shop, bool fallback)
	{
		if (GameSession.InGame)
		{
			Close();
			GameSession.SaveToDb();
			state = GameSession.Net.NpcShop(new NpcShopRequest
			{
				characterId = GameSession.CharacterId,
				id = id,
				fallback = fallback,
				operation = (shop ? NpcShopOperation.OpenShop : NpcShopOperation.OpenNpc)
			});
			sellTab = 0;
			if (!state.ok)
			{
				BottomBar.Toast(state.error);
				Reset();
			}
		}
	}

	private static void Transact(NpcShopRequest request)
	{
		if (Visible && GameSession.InGame)
		{
			GameSession.SaveToDb();
			request.characterId = GameSession.CharacterId;
			state = GameSession.Net.NpcShop(request);
			if (!state.ok)
			{
				BottomBar.Toast(state.error);
			}
			if (state.character != null)
			{
				GameSession.Character.inventory = state.character.inventory;
				GameSession.Character.record.meso = state.character.record.meso;
				Profile.Data.gold = state.character.record.meso;
				GameSession.ApplyInventoryToProfile();
				Profile.Save();
			}
		}
	}

	public static void SellFromScript(int shopId, int type, int slot, int quantity)
	{
		GameSession.SaveToDb();
		NpcShopResponse npcShopResponse = GameSession.Net.NpcShop(new NpcShopRequest
		{
			operation = NpcShopOperation.OpenShop,
			characterId = GameSession.CharacterId,
			id = shopId
		});
		if (!npcShopResponse.ok)
		{
			throw new JsRuntimeException(npcShopResponse.error);
		}
		NpcShopResponse npcShopResponse2 = GameSession.Net.NpcShop(new NpcShopRequest
		{
			operation = NpcShopOperation.Sell,
			characterId = GameSession.CharacterId,
			inventoryType = type,
			position = slot,
			quantity = quantity
		});
		GameSession.Net.NpcShop(new NpcShopRequest
		{
			operation = NpcShopOperation.Close,
			characterId = GameSession.CharacterId
		});
		if (!npcShopResponse2.ok)
		{
			throw new JsRuntimeException(npcShopResponse2.error);
		}
		GameSession.Character.inventory = npcShopResponse2.character.inventory;
		GameSession.Character.record.meso = npcShopResponse2.character.record.meso;
		Profile.Data.gold = npcShopResponse2.character.record.meso;
		GameSession.ApplyInventoryToProfile();
		Profile.Save();
	}

	private static List<int> BuyItems()
	{
		List<int> list = new List<int>();
		if (state != null)
		{
			for (int i = 0; i < state.items.Count; i++)
			{
				if (state.items[i].price > 0 || state.items[i].pitch > 0)
				{
					list.Add(i);
				}
			}
		}
		return list;
	}

	private static List<InventoryEntry> SellItems()
	{
		List<InventoryEntry> list = new List<InventoryEntry>();
		if (GameSession.Character != null)
		{
			foreach (InventoryEntry item in GameSession.Character.inventory)
			{
				if (item.position > 0 && item.inventoryType == sellTab + 1)
				{
					ImgNode imgNode = NativeItemSource.Node(item.itemId)?.Get("info");
					if (imgNode != null && imgNode.Int("cash") == 0 && imgNode.Int("notSale") == 0 && imgNode.Int("quest") == 0 && item.expiration <= 0)
					{
						list.Add(item);
					}
				}
			}
		}
		list.Sort((InventoryEntry a, InventoryEntry b) => a.position.CompareTo(b.position));
		return list;
	}

	public static void BuySelected()
	{
		if (!Visible || buySelection < 0 || buySelection >= state.items.Count)
		{
			return;
		}
		int index = buySelection;
		int version = generation;
		NpcShopItem stock = state.items[index];
		Action<int> action = (int q) =>
		{
			if (version == generation && Visible)
			{
				Transact(new NpcShopRequest
				{
					operation = NpcShopOperation.Buy,
					index = index,
					itemId = stock.itemId,
					quantity = q
				});
			}
		};
		if (stock.itemId / 1000000 == 1 || TradeItemRules.Rechargeable(stock.itemId))
		{
			action(1);
		}
		else
		{
			NativeUtilPrompt.Number("要购买多少个？", 1, 1, 32767, action);
		}
	}

	public static void SellSelected()
	{
		List<InventoryEntry> list = SellItems();
		if (!Visible || sellSelection < 0 || sellSelection >= list.Count)
		{
			return;
		}
		InventoryEntry item = list[sellSelection];
		int version = generation;
		Action<int> sell = (int q) =>
		{
			if (version == generation && Visible)
			{
				Transact(new NpcShopRequest
				{
					operation = NpcShopOperation.Sell,
					inventoryType = item.inventoryType,
					position = item.position,
					quantity = q
				});
				sellSelection = -1;
			}
		};
		if (TradeItemRules.Rechargeable(item.itemId) || item.quantity <= 1)
		{
			NativeUtilPrompt.Confirm("要出售这件道具吗？", (bool yes) =>
			{
				if (yes)
				{
					sell(1);
				}
			});
		}
		else
		{
			NativeUtilPrompt.Number("要出售多少个？", 1, 1, item.quantity, sell);
		}
	}

	private static GUIStyle Text(bool right = false)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected Obj, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected Obj, but got Unknown
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = 12,
			padding = new RectOffset(),
			clipping = (TextClipping)1,
			alignment = (TextAnchor)(right ? 2 : 0)
		};
		val.normal.textColor = Color.black;
		return val;
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

	private static int Tabs(string labels, int count, int selected, float x, float width, Vector2 pointer, int fixedWidth = 0)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return NativeUi.TabsStyle2("UI/UIWindow.img/Shop/" + labels, count, selected, x, 95f, (int)width, pointer, fixedWidth);
	}

	private static void Icon(int id, float x, float foot)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = NativeItemSource.IconNode(id);
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

	public static void Portrait(int npcId, float x, float y)
	{
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = WzAssets.FileRoot("Npc/" + npcId.ToString("D7") + ".img");
		HashSet<int> hashSet = new HashSet<int>();
		while (imgNode.Get("info/link") != null)
		{
			int item = imgNode.Int("info/link");
			if (!hashSet.Add(item))
			{
				return;
			}
			imgNode = WzAssets.FileRoot("Npc/" + item.ToString("D7") + ".img");
		}
		List<ImgNode> list = WzAssets.Frames(imgNode.Get("stand"));
		if (list.Count == 0)
		{
			return;
		}
		float num = 0f;
		foreach (ImgNode item2 in list)
		{
			num += (float)Math.Max(1, item2.Int("delay", 100));
		}
		float num2 = Time.unscaledTime * 1000f % num;
		ImgNode frame = list[0];
		foreach (ImgNode item3 in list)
		{
			frame = item3;
			num2 -= (float)Math.Max(1, item3.Int("delay", 100));
			if (num2 < 0f)
			{
				break;
			}
		}
		Texture2D val = WzAssets.Texture(frame);
		Vector2 val2 = WzAssets.Origin(frame);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(x - val2.x, y - val2.y, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
		}
	}

	private static void AndroidPortrait(int templateId, float x, float y)
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		string[] array = AndroidLook.Ids(NativeAndroidService.Resolve(GameSession.CharacterId, Profile.EquipOfSlot));
		Sprite val = ((array != null) ? WzCharacter.Appearance(AndroidLook.Key(array), array, WzCharacter.AppearanceFrame(array, Time.unscaledTime)) : null);
		if ((Object)(object)val == (Object)null)
		{
			Portrait(state.npcId, x, y);
			return;
		}
		float num = x - val.pivot.x;
		Rect rect = val.rect;
		float num2 = y - rect.height + val.pivot.y;
		rect = val.rect;
		float width = rect.width;
		rect = val.rect;
		GUI.DrawTexture(new Rect(num, num2, width, rect.height), (Texture)(object)val.texture);
	}

	public static void Draw()
	{
		//IL_071a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Invalid comparison between Unknown and I4
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
		//IL_037c: Unknown result type (might be due to invalid IL or missing references)
		//IL_039c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_046f: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0620: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0670: Unknown result type (might be due to invalid IL or missing references)
		//IL_067a: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_06eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_051c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0523: Unknown result type (might be due to invalid IL or missing references)
		//IL_0541: Unknown result type (might be due to invalid IL or missing references)
		//IL_0543: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		if (!GameSession.InGame || Profile.LastMapId != state.mapId)
		{
			Close();
			return;
		}
		float s = BottomBar.S;
		position = NativeUi.ClampWindowPosition(position, new Vector2(463f, 339f), s);
		NativeUi.RegisterWindow("NativeNpcShopWindow", new Rect(position.x * s, position.y * s, 463f * s, 339f * s), () => Visible);
		Vector2 val = Pointer.Pos / s;
		Matrix4x4 matrix = GUI.matrix;
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(s, s, 1f));
		try
		{
			if (GUI.enabled && (int)Event.current.type == 7 && Pointer.Pressed)
			{
				Rect val2 = new Rect(position.x, position.y, 463f, 12f);
				if (val2.Contains(val))
				{
					dragging = true;
					grab = val - position;
				}
			}
			if (dragging && Pointer.Down)
			{
				position = NativeUi.ClampWindowPosition(val - grab, new Vector2(463f, 339f), s);
			}
			if (!Pointer.Down)
			{
				dragging = false;
			}
			GUI.BeginGroup(new Rect(position.x, position.y, 463f, 339f));
			try
			{
				Vector2 val3 = val - position;
				Image("UI/UIWindow.img/Shop/backgrnd", 0f, 0f);
				if (state.androidTemplateId != 0)
				{
					AndroidPortrait(state.androidTemplateId, 56f, 76f);
				}
				else
				{
					Portrait(state.npcId, 56f, 76f);
				}
				NativeUi.CharacterPortrait(285f, 76f);
				GUI.Label(new Rect(350f, 64f, 97f, 16f), Profile.Data.gold.ToString("N0"), Text(right: true));
				if (NativeUi.Button(new Rect(142f, 15f, 70f, 19f), "UI/UIWindow.img/PersonalShop/BtExit", val3))
				{
					Close();
					return;
				}
				if (NativeUi.Button(new Rect(142f, 35f, 70f, 19f), "UI/UIWindow.img/Shop/BtBuy", val3))
				{
					BuySelected();
				}
				if (NativeUi.Button(new Rect(372f, 35f, 70f, 19f), "UI/UIWindow.img/Shop/BtSell", val3))
				{
					SellSelected();
				}
				Tabs("TabBuy", 1, 0, 5f, 222f, val3, 50);
				int num = Tabs("TabSell", 5, sellTab, 235f, 222f, val3);
				if (num != sellTab)
				{
					sellTab = num;
					sellScroll = 0f;
					sellSelection = -1;
				}
				List<int> list = BuyItems();
				List<InventoryEntry> list2 = SellItems();
				buyScroll = NativeUi.Scroll4(new Rect(210f, 127f, 15f, 194f), new Rect(6f, 127f, 199f, 194f), buyScroll, Math.Max(0, list.Count - 5), 1f, val3);
				sellScroll = NativeUi.Scroll4(new Rect(441f, 127f, 15f, 194f), new Rect(236f, 127f, 199f, 194f), sellScroll, Math.Max(0, list2.Count - 5), 1f, val3);
				for (int num2 = 0; num2 < 5; num2++)
				{
					int num3 = num2 + (int)buyScroll;
					int num4 = 127 + num2 * 40;
					if (num3 < list.Count)
					{
						int num5 = list[num3];
						NpcShopItem npcShopItem = state.items[num5];
						if (num5 == buySelection)
						{
							Image("UI/UIWindow.img/Shop/select", 43f, num4);
						}
						Icon(npcShopItem.itemId, 8f, num4 + 34);
						GUI.Label(new Rect(49f, (float)(num4 + 1), 152f, 16f), ItemCatalog.Name(npcShopItem.itemId), Text());
						Image("UI/UIWindow.img/Shop/" + ((npcShopItem.price > 0) ? "meso" : "token"), 49f, num4 + 20);
						GUI.Label(new Rect(65f, (float)(num4 + 19), 137f, 16f), ((npcShopItem.price > 0) ? npcShopItem.price : npcShopItem.pitch).ToString("N0"), Text());
						Rect rect = new Rect(6f, (float)num4, 199f, 35f);
						if (rect.Contains(val3))
						{
							ItemTooltip.SetHover(npcShopItem.itemId);
						}
						bool doubleClicked = Pointer.DoubleClicked;
						if (Hit(rect, val3))
						{
							buySelection = num5;
							if (doubleClicked)
							{
								BuySelected();
							}
						}
					}
					num3 = num2 + (int)sellScroll;
					if (num3 >= list2.Count)
					{
						continue;
					}
					InventoryEntry inventoryEntry = list2[num3];
					if (num3 == sellSelection)
					{
						Image("UI/UIWindow.img/Shop/select", 273f, num4 - 2);
					}
					Icon(inventoryEntry.itemId, 238f, num4 + 34);
					GUI.Label(new Rect(279f, (float)(num4 - 1), 152f, 16f), ItemCatalog.Name(inventoryEntry.itemId), Text());
					state.sellPrices.TryGetValue(inventoryEntry.inventoryType * 1000 + inventoryEntry.position, out var value);
					GUI.Label(new Rect(279f, (float)(num4 + 19), 122f, 16f), Math.Max(0, value).ToString("N0") + " 金币", Text());
					if (TradeItemRules.Rechargeable(inventoryEntry.itemId) && NativeUi.Button(new Rect(404f, (float)num4, 35f, 19f), "UI/UIWindow.img/Shop/BtRecharge", val3))
					{
						Transact(new NpcShopRequest
						{
							operation = NpcShopOperation.Recharge,
							inventoryType = inventoryEntry.inventoryType,
							position = inventoryEntry.position
						});
					}
					Rect rect2 = new Rect(236f, (float)num4, 165f, 35f);
					if (rect2.Contains(val3))
					{
						ItemTooltip.SetHover(inventoryEntry.itemId, equipped: false, inventoryEntry);
					}
					bool doubleClicked2 = Pointer.DoubleClicked;
					if (Hit(rect2, val3))
					{
						sellSelection = num3;
						if (doubleClicked2)
						{
							SellSelected();
						}
					}
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

	static NativeNpcShopWindow()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
	}
}
