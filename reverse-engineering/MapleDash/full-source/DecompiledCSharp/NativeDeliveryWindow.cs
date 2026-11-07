using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class NativeDeliveryWindow
{
	private const string Root = "UI/UIWindow.img/Delivery/";

	private const float W = 290f;

	private const float H = 348f;

	private const int RowsVisible = 6;

	private static bool visible;

	private static bool quickOnly;

	private static int tab;

	private static List<DueyPackageData> packages = new List<DueyPackageData>();

	private static string recipient = "";

	private static string mesosText = "";

	private static string message = "";

	private static int selType;

	private static int selPos;

	private static int selQty;

	private static float recvScroll;

	private static float sendScroll;

	private static Vector2 position = new Vector2(250f, 80f);

	private static Vector2 grab;

	private static bool dragging;

	private static int generation;

	private static float notifyTimer = 3f;

	public static bool Visible => visible;

	public static void Open(bool quick)
	{
		if (GameSession.InGame)
		{
			Reset();
			quickOnly = quick;
			tab = (quick ? 1 : 0);
			DueyResponse dueyResponse = Request(DueyOperation.Open);
			if (!dueyResponse.ok)
			{
				BottomBar.Toast(dueyResponse.error);
				Reset();
			}
			else
			{
				packages = dueyResponse.packages ?? new List<DueyPackageData>();
				visible = true;
			}
		}
	}

	public static void Close()
	{
		Reset();
	}

	public static void Reset()
	{
		visible = false;
		quickOnly = false;
		tab = 0;
		packages = new List<DueyPackageData>();
		recipient = "";
		mesosText = "";
		message = "";
		selType = (selPos = (selQty = 0));
		recvScroll = (sendScroll = 0f);
		generation++;
	}

	private static DueyResponse Request(DueyOperation op, int packageId = 0, int mesos = 0, bool quick = false)
	{
		if (!GameSession.InGame)
		{
			return new DueyResponse
			{
				ok = false,
				error = "请先选择角色"
			};
		}
		GameSession.SaveToDb();
		DueyResponse dueyResponse = GameSession.Net.Duey(new DueyRequest
		{
			characterId = GameSession.CharacterId,
			operation = op,
			packageId = packageId,
			inventoryType = selType,
			itemPosition = selPos,
			itemQuantity = selQty,
			mesos = mesos,
			recipient = recipient,
			message = message,
			quick = quick
		});
		bool num = dueyResponse.inventory != null || dueyResponse.meso >= 0;
		if (dueyResponse.inventory != null)
		{
			GameSession.Character.inventory = dueyResponse.inventory;
			GameSession.ApplyInventoryToProfile();
		}
		if (dueyResponse.meso >= 0)
		{
			GameSession.Character.record.meso = dueyResponse.meso;
			Profile.Data.gold = dueyResponse.meso;
		}
		if (num)
		{
			Profile.Save();
		}
		return dueyResponse;
	}

	private static void Refresh()
	{
		DueyResponse dueyResponse = Request(DueyOperation.Open);
		if (dueyResponse.ok)
		{
			packages = dueyResponse.packages ?? new List<DueyPackageData>();
		}
	}

	private static void ClaimPackage(DueyPackageData data)
	{
		DueyResponse dueyResponse = Request(DueyOperation.Claim, data.packageId);
		if (!dueyResponse.ok)
		{
			BottomBar.Toast(dueyResponse.error);
			return;
		}
		packages = dueyResponse.packages ?? new List<DueyPackageData>();
		BottomBar.Toast("已领取快递");
	}

	private static void RemovePackage(DueyPackageData data)
	{
		int version = generation;
		NativeUtilPrompt.Confirm("要丢弃这个快递吗？\n里面的东西会一起消失。", (bool yes) =>
		{
			if (yes && version == generation)
			{
				DueyResponse dueyResponse = Request(DueyOperation.Remove, data.packageId);
				if (!dueyResponse.ok)
				{
					BottomBar.Toast(dueyResponse.error);
				}
				else
				{
					packages = dueyResponse.packages ?? new List<DueyPackageData>();
				}
			}
		});
	}

	private static void ConfirmSend(int quantity)
	{
		if (!int.TryParse(mesosText, out var mesos) || mesos < 0)
		{
			mesos = 0;
		}
		if (string.IsNullOrEmpty(recipient))
		{
			BottomBar.Toast("请填写收件人");
			return;
		}
		if (quantity <= 0 && mesos <= 0)
		{
			BottomBar.Toast("请选择物品或填写金币");
			return;
		}
		selQty = quantity;
		int num = NativeDueyService.GetFee(mesos) + ((!quickOnly) ? 5000 : 0);
		int version = generation;
		NativeUtilPrompt.Confirm("收件人 " + recipient + "\n金额 " + mesos.ToString("N0") + " 金币\n服务费 " + num.ToString("N0") + " 金币" + (quickOnly ? "\n并消耗 1 张快速投递券" : "") + "\n确定寄出吗？", (bool yes) =>
		{
			if (yes && version == generation)
			{
				DueyResponse dueyResponse = Request(DueyOperation.Send, 0, mesos, quickOnly);
				if (!dueyResponse.ok)
				{
					BottomBar.Toast(dueyResponse.error);
				}
				else
				{
					BottomBar.Toast("已寄出");
					message = "";
					mesosText = "";
					selType = (selPos = (selQty = 0));
				}
			}
		});
	}

	private static void SendSelected()
	{
		if (selQty <= 0)
		{
			if (!int.TryParse(mesosText, out var result) || result <= 0)
			{
				BottomBar.Toast("请选择物品或填写金币");
			}
			else
			{
				ConfirmSend(0);
			}
		}
		else if (selQty <= 1)
		{
			ConfirmSend(1);
		}
		else
		{
			int max = selQty;
			NativeUtilPrompt.Number("要寄出多少个？", 1, 1, max, (int q) =>
			{
				ConfirmSend(q);
			});
		}
	}

	public static void Tick()
	{
		if (!GameSession.InGame)
		{
			return;
		}
		notifyTimer -= Time.unscaledDeltaTime;
		if (notifyTimer > 0f)
		{
			return;
		}
		notifyTimer = 10f;
		DueyResponse dueyResponse = GameSession.Net.Duey(new DueyRequest
		{
			characterId = GameSession.CharacterId,
			operation = DueyOperation.Notify
		});
		if (dueyResponse.pending != null)
		{
			BottomBar.Toast((dueyResponse.pending.quick ? "快速投递到件" : "快递到件") + "：寄件人 " + dueyResponse.pending.sender);
			if (visible)
			{
				Refresh();
			}
		}
	}

	private static GUIStyle Label(bool right = false)
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
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected Obj, but got Unknown
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = 11,
			padding = new RectOffset(),
			clipping = (TextClipping)1,
			alignment = (TextAnchor)(right ? 2 : 0),
			wordWrap = false
		};
		val.normal.textColor = Color.black;
		return val;
	}

	private static GUIStyle Field()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected Obj, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected Obj, but got Unknown
		GUIStyle val = new GUIStyle(GUI.skin.textField)
		{
			font = UiKit.Font,
			fontSize = 11,
			padding = new RectOffset(3, 3, 1, 1)
		};
		val.normal.textColor = Color.black;
		val.focused.textColor = Color.black;
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
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < 2; i++)
		{
			Texture2D val = Art.Tex("wz://UI/UIWindow.img/Delivery/Tab/" + ((tab == i) ? "disabled/" : "enabled/") + i);
			if (!((Object)(object)val == (Object)null))
			{
				float num = 12 + i * 68;
				GUI.DrawTexture(new Rect(num, 22f, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
				if (Hit(new Rect(num, 22f, (float)((Texture)val).width, (float)((Texture)val).height), p))
				{
					tab = i;
				}
			}
		}
	}

	private static List<InventoryEntry> Sendable()
	{
		List<InventoryEntry> list = new List<InventoryEntry>();
		FullCharacter character = GameSession.Character;
		if (character == null)
		{
			return list;
		}
		foreach (InventoryEntry item in character.inventory)
		{
			if (item.position > 0 && item.inventoryType != 5 && item.inventoryType > 0)
			{
				list.Add(item);
			}
		}
		list.Sort((InventoryEntry a, InventoryEntry b) => (a.inventoryType == b.inventoryType) ? a.position.CompareTo(b.position) : a.inventoryType.CompareTo(b.inventoryType));
		return list;
	}

	private static void ReceiveTab(Vector2 p, float top)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		GUI.Label(new Rect(14f, top, 200f, 14f), "共 " + packages.Count + " 个快递", Label());
		float num = top + 16f;
		int num2 = Math.Max(0, packages.Count - 6);
		recvScroll = NativeUi.Scroll4(new Rect(272f, num, 15f, 348f - num - 16f), new Rect(10f, num, 258f, 348f - num - 16f), recvScroll, num2, 1f, p);
		for (int i = 0; i < 6; i++)
		{
			int num3 = i + (int)recvScroll;
			if (num3 < packages.Count)
			{
				DueyPackageData dueyPackageData = packages[num3];
				float num4 = num + (float)(i * 44);
				if (dueyPackageData.item != null)
				{
					Icon(dueyPackageData.item.itemId, 22f, num4 + 32f);
				}
				GUI.Label(new Rect(40f, num4 + 2f, 150f, 14f), dueyPackageData.sender, Label());
				GUI.Label(new Rect(40f, num4 + 17f, 150f, 14f), ((dueyPackageData.mesos > 0) ? (dueyPackageData.mesos.ToString("N0") + " 金币") : "无金币") + ((dueyPackageData.item != null) ? ("  ·  " + ItemCatalog.Name(dueyPackageData.item.itemId) + " x" + dueyPackageData.item.quantity) : ""), Label());
				if (!string.IsNullOrEmpty(dueyPackageData.message))
				{
					GUI.Label(new Rect(40f, num4 + 30f, 170f, 13f), dueyPackageData.message, Label());
				}
				if (GUI.Button(new Rect(200f, num4 + 8f, 44f, 18f), "领取", Small()))
				{
					ClaimPackage(dueyPackageData);
					break;
				}
				if (GUI.Button(new Rect(246f, num4 + 8f, 16f, 18f), "×", Small()))
				{
					RemovePackage(dueyPackageData);
					break;
				}
				continue;
			}
			break;
		}
	}

	private static void SendTab(Vector2 p, float top)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_032d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		GUIStyle val = Field();
		GUI.Label(new Rect(14f, top + 2f, 50f, 14f), "收件人", Label());
		recipient = GUI.TextField(new Rect(64f, top, 200f, 18f), recipient ?? "", 13, val);
		GUI.Label(new Rect(14f, top + 26f, 50f, 14f), "金币", Label());
		mesosText = GUI.TextField(new Rect(64f, top + 24f, 90f, 18f), mesosText ?? "", 10, val);
		GUI.Label(new Rect(14f, top + 50f, 50f, 14f), "留言", Label());
		message = GUI.TextField(new Rect(64f, top + 48f, 200f, 18f), message ?? "", 100, val);
		List<InventoryEntry> list = Sendable();
		float num = top + 74f;
		float num2 = 308f;
		GUI.Label(new Rect(14f, num - 14f, 200f, 13f), "选择要寄出的道具（不选则只寄金币）", Label());
		int num3 = Math.Max(1, (int)((num2 - num) / 20f));
		sendScroll = NativeUi.Scroll4(new Rect(272f, num, 15f, num2 - num), new Rect(10f, num, 258f, num2 - num), sendScroll, Math.Max(0, list.Count - num3), 1f, p);
		for (int i = 0; i < num3; i++)
		{
			int num4 = i + (int)sendScroll;
			if (num4 >= list.Count)
			{
				break;
			}
			InventoryEntry inventoryEntry = list[num4];
			float num5 = num + (float)(i * 20);
			bool flag = inventoryEntry.inventoryType == selType && inventoryEntry.position == selPos;
			if (flag)
			{
				GUI.Box(new Rect(10f, num5, 258f, 19f), "");
			}
			Icon(inventoryEntry.itemId, 22f, num5 + 16f);
			GUI.Label(new Rect(40f, num5 + 2f, 150f, 14f), ItemCatalog.Name(inventoryEntry.itemId) + ((inventoryEntry.quantity > 1) ? (" x" + inventoryEntry.quantity) : ""), Label());
			if (Hit(new Rect(10f, num5, 258f, 19f), p))
			{
				if (flag)
				{
					selType = (selPos = (selQty = 0));
					continue;
				}
				selType = inventoryEntry.inventoryType;
				selPos = inventoryEntry.position;
				selQty = inventoryEntry.quantity;
			}
		}
		if (GUI.Button(new Rect(214f, 316f, 62f, 20f), "寄出", Small()))
		{
			SendSelected();
		}
	}

	public static void Draw()
	{
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
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
		position = NativeUi.ClampWindowPosition(position, new Vector2(290f, 348f), s);
		NativeUi.RegisterWindow("NativeDeliveryWindow", new Rect(position.x * s, position.y * s, 290f * s, 348f * s), () => Visible);
		Vector2 val = Pointer.Pos / s;
		Matrix4x4 matrix = GUI.matrix;
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(s, s, 1f));
		try
		{
			if (GUI.enabled && (int)Event.current.type == 7 && Pointer.Pressed)
			{
				Rect val2 = new Rect(position.x, position.y, 290f, 16f);
				if (val2.Contains(val))
				{
					dragging = true;
					grab = val - position;
				}
			}
			if (dragging && Pointer.Down)
			{
				position = NativeUi.ClampWindowPosition(val - grab, new Vector2(290f, 348f), s);
			}
			if (!Pointer.Down)
			{
				dragging = false;
			}
			GUI.BeginGroup(new Rect(position.x, position.y, 290f, 348f));
			try
			{
				Vector2 val3 = val - position;
				Image("UI/UIWindow.img/Delivery/backgrnd", 0f, 0f);
				GUI.Label(new Rect(12f, 4f, 200f, 14f), quickOnly ? "快速投递" : "快递", Label());
				if (NativeUi.Button(new Rect(268f, 4f, 16f, 16f), "UI/UIWindow.img/BtUIClose2", val3))
				{
					Close();
					return;
				}
				if (!quickOnly)
				{
					Tabs(val3);
				}
				float top = 42f;
				if (tab == 0 && !quickOnly)
				{
					ReceiveTab(val3, top);
				}
				else
				{
					SendTab(val3, top);
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

	static NativeDeliveryWindow()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
	}
}
