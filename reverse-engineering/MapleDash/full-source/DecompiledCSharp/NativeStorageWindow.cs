using System;
using System.Collections.Generic;
using MapleDash.Wz;
using Server.Bot.Trade;
using UnityEngine;

public static class NativeStorageWindow
{
	private const string TakeMesoKey = "TrunkTakeMeso";

	private const string PutMesoKey = "TrunkPutMeso";

	private static StorageSnapshot storage;

	private static int tab = 1;

	private static int selection = -1;

	private static int bagSelection = -1;

	private static float scroll;

	private static float bagScroll;

	private static string takeMeso = "";

	private static string putMeso = "";

	private static Vector2 position = new Vector2(60f, 30f);

	private static Vector2 grab;

	private static bool dragging;

	private static int npcId;

	private const float W = 463f;

	private const float H = 318f;

	private const int Rows = 5;

	private const int BagRows = 4;

	private static readonly string[] Tabs = new string[5] { "装备", "消耗", "装饰", "其他", "现金" };

	public static bool Visible => storage != null;

	public static void Reset()
	{
		storage = null;
		tab = 1;
		selection = (bagSelection = -1);
		scroll = (bagScroll = 0f);
		takeMeso = (putMeso = "");
		dragging = false;
		npcId = 0;
		NativeEditKeyboard.Close();
	}

	public static void Open(int npc)
	{
		if (!GameSession.InGame)
		{
			return;
		}
		StorageResponse storageResponse = Request(StorageOperation.Open, npc, 0, 0, -1, 0, 0, 0L);
		if (storageResponse != null)
		{
			if (!storageResponse.ok)
			{
				BottomBar.Toast(storageResponse.error);
				return;
			}
			npcId = npc;
			storage = storageResponse.storage;
			selection = (bagSelection = -1);
			scroll = (bagScroll = 0f);
		}
	}

	public static void Close()
	{
		if (storage != null && GameSession.InGame)
		{
			Request(StorageOperation.Close, npcId, 0, 0, -1, 0, 0, 0L);
		}
		Reset();
	}

	private static StorageResponse Request(StorageOperation operation, int npc, int inventoryType = 0, int itemPosition = 0, int storageIndex = -1, int quantity = 0, int itemId = 0, long meso = 0L, int slots = 0)
	{
		if (!GameSession.InGame)
		{
			return null;
		}
		GameSession.SaveToDb();
		StorageResponse storageResponse = GameSession.Net.Storage(new StorageRequest
		{
			characterId = GameSession.CharacterId,
			operation = operation,
			npcId = npc,
			inventoryType = inventoryType,
			itemPosition = itemPosition,
			storageIndex = storageIndex,
			quantity = quantity,
			itemId = itemId,
			meso = meso,
			slots = slots
		});
		Apply(storageResponse);
		return storageResponse;
	}

	private static void Apply(StorageResponse res)
	{
		if (res != null)
		{
			if (res.storage != null)
			{
				storage = res.storage;
			}
			if (res.inventory != null)
			{
				GameSession.Character.inventory = res.inventory;
				GameSession.ApplyInventoryToProfile();
				Profile.Save();
			}
			if (res.meso >= 0)
			{
				GameSession.Character.record.meso = res.meso;
				Profile.Data.gold = res.meso;
				Profile.Save();
			}
			if (!res.ok && res.error.Length > 0)
			{
				NativeSystemMessage.Drop(5, res.error);
			}
		}
	}

	private static List<StorageSlotData> Shown()
	{
		List<StorageSlotData> list = new List<StorageSlotData>();
		if (storage == null)
		{
			return list;
		}
		list.AddRange(storage.items);
		return list;
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
			if (item.position > 0 && item.inventoryType > 0 && item.inventoryType == tab)
			{
				list.Add(item);
			}
		}
		list.Sort((InventoryEntry a, InventoryEntry b) => (a.inventoryType == b.inventoryType) ? a.position.CompareTo(b.position) : a.inventoryType.CompareTo(b.inventoryType));
		return list;
	}

	private static void TakeOut()
	{
		List<StorageSlotData> list = Shown();
		if (selection < 0 || selection >= list.Count)
		{
			return;
		}
		StorageSlotData slot = list[selection];
		int num = NativeStorageService.TakeFee(npcId);
		NativeUtilPrompt.Confirm((num > 0) ? ("取出道具需要 " + num + " 金币。\n确定要取出吗？") : "确定要从仓库取出道具吗？", (bool yes) =>
		{
			if (yes && Visible)
			{
				Request(StorageOperation.TakeOut, npcId, slot.item.inventoryType, 0, slot.index, 0, 0, 0L);
				selection = -1;
			}
		});
	}

	private static void TakeOutAll()
	{
		for (int i = 0; i < 48; i++)
		{
			List<StorageSlotData> list = Shown();
			if (list.Count == 0)
			{
				break;
			}
			StorageSlotData storageSlotData = list[0];
			StorageResponse storageResponse = Request(StorageOperation.TakeOut, npcId, storageSlotData.item.inventoryType, 0, storageSlotData.index, 0, 0, 0L);
			if (storageResponse == null || !storageResponse.ok)
			{
				break;
			}
		}
		selection = -1;
	}

	private static void Store()
	{
		List<InventoryEntry> list = Bag();
		InventoryEntry item;
		if (bagSelection >= 0 && bagSelection < list.Count)
		{
			item = list[bagSelection];
			bool flag = TradeItemRules.Rechargeable(item.itemId);
			if (item.isEquip | flag)
			{
				Put(item.quantity);
			}
			else
			{
				NativeUtilPrompt.Number("存入多少个？", item.quantity, 1, item.quantity, Put);
			}
		}
		void Put(int quantity)
		{
			int num = NativeStorageService.PutFee(npcId);
			NativeUtilPrompt.Confirm((num > 0) ? ("保管道具需要 " + num + " 金币。\n确定要保管吗？") : "确定要保管道具吗？", (bool yes) =>
			{
				if (yes && Visible)
				{
					Request(StorageOperation.Store, npcId, item.inventoryType, item.position, -1, quantity, item.itemId, 0L);
					bagSelection = -1;
				}
			});
		}
	}

	private static void Arrange()
	{
		Request(StorageOperation.Arrange, npcId, 0, 0, -1, 0, 0, 0L);
	}

	private static void Meso(bool takeOut)
	{
		if (!long.TryParse(takeOut ? takeMeso : putMeso, out var result) || result <= 0)
		{
			BottomBar.Toast("请输入金币数量");
			return;
		}
		Request(StorageOperation.Meso, npcId, 0, 0, -1, 0, 0, takeOut ? result : (-result));
		if (takeOut)
		{
			takeMeso = "";
		}
		else
		{
			putMeso = "";
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

	private static GUIStyle Center()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected Obj, but got Unknown
		return new GUIStyle(Text())
		{
			alignment = (TextAnchor)1
		};
	}

	private static void Image(string name, float x, float y)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://UI/UIWindow.img/Trunk/" + name);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(x, y, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
		}
	}

	private static void DrawIcon(int itemId, float x, float y)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = NativeItemSource.IconNode(itemId);
		if (imgNode != null)
		{
			ImgNode imgNode2 = imgNode.Get("origin");
			Texture2D val = WzAssets.Texture(imgNode);
			GUI.DrawTexture(new Rect(x - (float)(imgNode2?.X ?? 0), y - (float)(imgNode2?.Y ?? 0), (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
		}
	}

	private static void Highlight(Rect rect)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		GUI.color = new Color(1f, 1f, 1f, 0.25f);
		GUI.DrawTexture(rect, (Texture)(object)Texture2D.whiteTexture);
		GUI.color = Color.white;
	}

	public static void Draw()
	{
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Invalid comparison between Unknown and I4
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		if (!GameSession.InGame)
		{
			Reset();
			return;
		}
		NativeEditKeyboard.Sync("TrunkTakeMeso", ref takeMeso);
		NativeEditKeyboard.Sync("TrunkPutMeso", ref putMeso);
		float s = BottomBar.S;
		position = NativeUi.ClampWindowPosition(position, new Vector2(463f, 318f), s);
		NativeUi.RegisterWindow("NativeStorageWindow", new Rect(position.x * s, position.y * s, 463f * s, 318f * s), () => Visible);
		Vector2 val = Pointer.Pos / s;
		Matrix4x4 matrix = GUI.matrix;
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(s, s, 1f));
		try
		{
			if (GUI.enabled && (int)Event.current.type == 7 && Pointer.Pressed)
			{
				Rect val2 = new Rect(position.x, position.y, 463f, 22f);
				if (val2.Contains(val))
				{
					dragging = true;
					grab = val - position;
				}
			}
			if (dragging && Pointer.Down)
			{
				position = NativeUi.ClampWindowPosition(val - grab, new Vector2(463f, 318f), s);
			}
			if (!Pointer.Down)
			{
				dragging = false;
			}
			GUI.BeginGroup(new Rect(position.x, position.y, 463f, 318f));
			try
			{
				DrawBody(val - position);
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

	private static void DrawBody(Vector2 p)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0328: Unknown result type (might be due to invalid IL or missing references)
		//IL_0346: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_0533: Unknown result type (might be due to invalid IL or missing references)
		//IL_053d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03be: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_041f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0426: Unknown result type (might be due to invalid IL or missing references)
		//IL_0591: Unknown result type (might be due to invalid IL or missing references)
		//IL_059b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Invalid comparison between Unknown and I4
		//IL_0450: Unknown result type (might be due to invalid IL or missing references)
		//IL_0456: Invalid comparison between Unknown and I4
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0461: Unknown result type (might be due to invalid IL or missing references)
		Image("backgrnd", 0f, 0f);
		NativeNpcShopWindow.Portrait(npcId, 53f, 76f);
		NativeUi.CharacterPortrait(288f, 75f);
		if (NativeUi.Button(new Rect(154f, 15f, 70f, 19f), "UI/UIWindow.img/Trunk/BtExit", p))
		{
			Close();
			return;
		}
		if (NativeUi.Button(new Rect(154f, 35f, 70f, 19f), "UI/UIWindow.img/Trunk/BtGet", p))
		{
			TakeOut();
		}
		if (NativeUi.Button(new Rect(154f, 55f, 71f, 18f), "UI/UIWindow.img/Trunk/BtSort", p))
		{
			Arrange();
		}
		if (NativeUi.Button(new Rect(384f, 15f, 70f, 19f), "UI/UIWindow.img/Trunk/BtPut", p))
		{
			Store();
		}
		int num = NativeUi.TabsStyle2("UI/UIWindow.img/Trunk/Tab", 5, tab - 1, 235f, 92f, 222, p) + 1;
		if (num != tab)
		{
			tab = num;
			bagSelection = -1;
			bagScroll = 0f;
		}
		List<StorageSlotData> list = Shown();
		scroll = NativeUi.Scroll4(new Rect(210f, 88f, 15f, 197f), new Rect(6f, 87f, 199f, 198f), scroll, Math.Max(0, list.Count - 5), 1f, p);
		for (int i = 0; i < 5 && i + (int)scroll < list.Count; i++)
		{
			int num2 = i + (int)scroll;
			int num3 = 87 + 40 * i;
			InventoryEntry item = list[num2].item;
			if (selection == num2)
			{
				Image("select", 44f, num3);
			}
			else if (num2 < storage.slots)
			{
				Image("en", 6f, num3);
			}
			DrawIcon(item.itemId, 7f, num3 + 34);
			GUI.Label(new Rect(50f, (float)(num3 + 1), 152f, 16f), ItemCatalog.Name(item.itemId), Text());
			GUI.Label(new Rect(50f, (float)(num3 + 20), 152f, 16f), item.quantity.ToString(), Text());
			Rect val = new Rect(6f, (float)num3, 199f, 35f);
			if (val.Contains(p))
			{
				ItemTooltip.SetHover(item.itemId, equipped: false, item);
			}
			bool doubleClicked = Pointer.DoubleClicked;
			if (GUI.enabled && (int)Event.current.type == 7 && Pointer.Released && val.Contains(p))
			{
				Pointer.ConsumeClick();
				selection = num2;
				if (doubleClicked)
				{
					TakeOut();
					return;
				}
			}
		}
		List<InventoryEntry> list2 = Bag();
		bagScroll = NativeUi.Scroll4(new Rect(441f, 127f, 15f, 158f), new Rect(236f, 127f, 199f, 158f), bagScroll, Math.Max(0, list2.Count - 4), 1f, p);
		for (int j = 0; j < 4 && j + (int)bagScroll < list2.Count; j++)
		{
			int num4 = j + (int)bagScroll;
			int num5 = 127 + 40 * j;
			InventoryEntry inventoryEntry = list2[num4];
			if (bagSelection == num4)
			{
				Image("select", 273f, num5);
			}
			DrawIcon(inventoryEntry.itemId, 238f, num5 + 34);
			GUI.Label(new Rect(279f, (float)(num5 + 1), 152f, 16f), ItemCatalog.Name(inventoryEntry.itemId), Text());
			GUI.Label(new Rect(279f, (float)(num5 + 20), 152f, 16f), inventoryEntry.quantity.ToString(), Text());
			Rect val2 = new Rect(236f, (float)num5, 199f, 35f);
			if (val2.Contains(p))
			{
				ItemTooltip.SetHover(inventoryEntry.itemId, equipped: false, inventoryEntry);
			}
			bool doubleClicked2 = Pointer.DoubleClicked;
			if (GUI.enabled && (int)Event.current.type == 7 && Pointer.Released && val2.Contains(p))
			{
				Pointer.ConsumeClick();
				bagSelection = num4;
				if (doubleClicked2)
				{
					Store();
					return;
				}
			}
		}
		GUI.Label(new Rect(44f, 296f, 146f, 16f), storage.meso.ToString("N0"), Text(right: true));
		GUI.Label(new Rect(275f, 296f, 143f, 16f), (GameSession.Character?.record.meso ?? 0).ToString("N0"), Text(right: true));
		if (NativeUi.Button(new Rect(5f, 294f, 36f, 18f), "UI/UIWindow.img/Trunk/BtOutCoin", p))
		{
			NativeUtilPrompt.NumberLong("取出多少金币？", 1L, 1L, storage.meso, (long value) =>
			{
				Request(StorageOperation.Meso, npcId, 0, 0, -1, 0, 0, value);
			});
		}
		if (NativeUi.Button(new Rect(236f, 294f, 36f, 18f), "UI/UIWindow.img/Trunk/BtInCoin", p))
		{
			NativeUtilPrompt.NumberLong("存入多少金币？", 1L, 1L, GameSession.Character.record.meso, (long value) =>
			{
				Request(StorageOperation.Meso, npcId, 0, 0, -1, 0, 0, -value);
			});
		}
	}

	static NativeStorageWindow()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
	}
}
