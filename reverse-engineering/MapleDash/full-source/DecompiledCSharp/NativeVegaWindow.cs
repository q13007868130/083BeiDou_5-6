using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class NativeVegaWindow
{
	private const string Root = "UI/UIWindow.img/VegaSpell/";

	private static int cashId;

	private static InventoryEntry equip;

	private static InventoryEntry scroll;

	private static readonly HashSet<int> allowed = new HashSet<int>();

	private static Vector2 position = new Vector2(420f, 100f);

	private static Vector2 grab;

	private static bool dragging;

	private static bool resultSound;

	private static double started;

	private static EquipPatchResponse result;

	public static bool Visible => cashId != 0;

	public static bool Busy => result != null;

	private static float S => BottomBar.S;

	private static Rect Window
	{
		get
		{
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			return new Rect(position.x * S, position.y * S, 178f * S, 235f * S);
		}
	}

	private static Rect At(Rect r, float x, float y, float w, float h)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(r.x + x * S, r.y + y * S, w * S, h * S);
	}

	public static void Close()
	{
		cashId = 0;
		equip = null;
		scroll = null;
		result = null;
		dragging = false;
		resultSound = false;
	}

	public static void Open(int id)
	{
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		Close();
		if (id != 5610000 && id != 5610001)
		{
			return;
		}
		if (allowed.Count == 0)
		{
			foreach (ImgNode child in WzAssets.Node("Etc/VegaSpell.img").Children)
			{
				allowed.Add(child.Int("item"));
			}
		}
		cashId = id;
		position = new Vector2((UiKit.W / S - 178f) / 2f, (UiKit.H / S - 235f) / 2f);
	}

	public static bool Drop(Vector2 pointer, int id, int inventoryPosition)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		if (Visible)
		{
			Rect window = Window;
			if (window.Contains(pointer))
			{
				if (Busy)
				{
					return true;
				}
				InventoryEntry inventoryEntry = GameSession.Character?.inventory.Find((InventoryEntry x) => x.itemId == id && x.position > 0 && (inventoryPosition == 0 || x.position == inventoryPosition));
				if (inventoryEntry == null)
				{
					return true;
				}
				string text = Select(inventoryEntry);
				if (text != null)
				{
					BottomBar.Toast(text);
				}
				return true;
			}
		}
		return false;
	}

	public static string Select(InventoryEntry item)
	{
		if (!Visible || Busy || item == null || item.position <= 0)
		{
			return "当前无法选择物品";
		}
		if (item.inventoryType == 1)
		{
			if (((NativeScrollService.HasStats(item) && (item.upgradeslots > 0 || item.equipLevel != 0)) ? item.upgradeslots : (NativeItemSource.Node(item.itemId)?.Int("info/tuc") ?? 0)) <= 0)
			{
				return "装备可升级次数不足";
			}
			equip = item.Copy();
			return null;
		}
		if (item.inventoryType != 2 || item.itemId / 10000 != 204)
		{
			return "请选择装备或强化卷轴";
		}
		int itemId = item.itemId;
		if (itemId == 2041211 || itemId == 2041212 || itemId == 2049100 || !allowed.Contains(itemId) || NativeItemSource.Node(itemId)?.Int("info/success") != ((cashId == 5610000) ? 10 : 60))
		{
			return "此卷轴无法使用这个咒语";
		}
		scroll = item.Copy();
		return null;
	}

	public static string Start()
	{
		if (!Visible || Busy || equip == null || scroll == null)
		{
			return "请先选择装备和卷轴";
		}
		List<int> list = new List<int>();
		ImgNode imgNode = NativeItemSource.Node(scroll.itemId)?.Get("req");
		if (imgNode != null)
		{
			foreach (ImgNode child in imgNode.Children)
			{
				list.Add(Convert.ToInt32(child.Value));
			}
		}
		EquipPatchResponse equipPatchResponse = GameSession.Net.UpdateEquip(new EquipPatchRequest
		{
			characterId = GameSession.CharacterId,
			inventoryItemId = equip.itemRowId,
			scrollId = scroll.itemId,
			scrollPosition = scroll.position,
			scrollRequirements = list.ToArray(),
			vegaItemId = cashId
		});
		if (!equipPatchResponse.ok)
		{
			return equipPatchResponse.error;
		}
		GameSession.Character.inventory = equipPatchResponse.inventory;
		GameSession.ApplyInventoryToProfile();
		GameLoop.I?.player?.RefreshBuffAttributes();
		Profile.Save();
		result = equipPatchResponse;
		started = NativeBuffs.Now;
		AudioManager.Play("UI/VegaTwinkling", 1f, pitchVary: false);
		return null;
	}

	private static void Image(Rect r, string path, float x, float y)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		ImgNode frame = WzAssets.Node("UI/UIWindow.img/VegaSpell/" + path);
		Texture2D val = WzAssets.Texture(frame);
		Vector2 val2 = WzAssets.Origin(frame);
		GUI.DrawTexture(At(r, x - val2.x, y - val2.y, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
	}

	private static void Animation(Rect r, string path, double elapsed, float x, float y, bool loop)
	{
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = WzAssets.Node("UI/UIWindow.img/VegaSpell/" + path);
		if (imgNode == null)
		{
			return;
		}
		List<ImgNode> list = WzAssets.Frames(imgNode);
		int num = 0;
		foreach (ImgNode item in list)
		{
			num += Math.Max(1, Math.Abs(item.Resolve().Int("delay", 100)));
		}
		if (num <= 0)
		{
			return;
		}
		int num2 = (int)(elapsed * 1000.0);
		if (loop)
		{
			num2 %= num;
		}
		else if (num2 >= num)
		{
			return;
		}
		foreach (ImgNode item2 in list)
		{
			num2 -= Math.Max(1, Math.Abs(item2.Resolve().Int("delay", 100)));
			if (num2 < 0)
			{
				Image(r, path + "/" + item2.Name, x, y);
				break;
			}
		}
	}

	private static void Icon(Rect r, InventoryEntry item, float x, float y)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (item != null)
		{
			Texture2D val = NativeItemSource.Icon(item.itemId);
			if ((Object)(object)val != (Object)null)
			{
				GUI.DrawTexture(At(r, x, y - 32f, 32f, 32f), (Texture)(object)val, (ScaleMode)2);
			}
		}
	}

	public static void Draw()
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Invalid comparison between Unknown and I4
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0387: Unknown result type (might be due to invalid IL or missing references)
		//IL_03be: Unknown result type (might be due to invalid IL or missing references)
		//IL_046f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0484: Unknown result type (might be due to invalid IL or missing references)
		//IL_0422: Unknown result type (might be due to invalid IL or missing references)
		//IL_043a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0303: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0340: Unknown result type (might be due to invalid IL or missing references)
		//IL_0368: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0509: Unknown result type (might be due to invalid IL or missing references)
		//IL_050e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0511: Unknown result type (might be due to invalid IL or missing references)
		//IL_053c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0551: Unknown result type (might be due to invalid IL or missing references)
		//IL_0556: Unknown result type (might be due to invalid IL or missing references)
		//IL_0559: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		position = NativeUi.ClampWindowPosition(position, new Vector2(178f, 235f), S);
		Rect window = Window;
		NativeUi.RegisterWindow("NativeVegaWindow", window, () => Visible);
		bool flag = (int)Event.current.type == 7;
		Rect val;
		if (flag && Pointer.Pressed && !NativeInventoryActions.Carrying)
		{
			val = At(window, 0f, 0f, 150f, 22f);
			if (val.Contains(Pointer.Pos))
			{
				dragging = true;
				grab = Pointer.Pos / S - position;
			}
		}
		if (dragging && Pointer.Down)
		{
			position = NativeUi.ClampWindowPosition(Pointer.Pos / S - grab, new Vector2(178f, 235f), S);
			window = Window;
		}
		if (!Pointer.Down)
		{
			dragging = false;
		}
		if (Busy && NativeBuffs.Now - started >= 3.0)
		{
			bool flag2 = result.scrollResult == "success";
			Rect r = new Rect((UiKit.W - 152f * S) / 2f, (UiKit.H - 159f * S) / 2f, 152f * S, 159f * S);
			Image(r, flag2 ? "SuccessWnd" : "FailWnd", 0f, 0f);
			Icon(r, equip, 60f, 97f);
			Animation(r, flag2 ? "EffectSuccess" : "EffectFail", NativeBuffs.Now - started - 3.0, flag2 ? 79 : 6, flag2 ? 71 : 45, loop: true);
			if (!resultSound)
			{
				AudioManager.Play(flag2 ? "UI/VegaSuccess" : "UI/VegaFail", 1f, pitchVary: false);
				resultSound = true;
			}
			if (NativeUi.Button(At(r, 55f, 135f, 43f, 17f), "UI/UIWindow.img/VegaSpell/BtOK"))
			{
				Close();
			}
			return;
		}
		Image(window, (cashId == 5610000) ? "backgrnd10" : "backgrnd60", 0f, 0f);
		Animation(window, "EffectArrow", NativeBuffs.Now, 91f, 41f, loop: true);
		Icon(window, equip, 30f, 157f);
		Icon(window, scroll, 117f, 157f);
		if (scroll != null)
		{
			int num = ((cashId == 5610000) ? 10 : 60);
			int num2 = ((cashId == 5610000) ? 30 : 90);
			Image(window, "Count/" + num / 10, 31f, 37f);
			Image(window, "Count/0", 39f, 37f);
			Image(window, "Count/" + num2 / 10, 123f, 37f);
			Image(window, "Count/0", 131f, 37f);
		}
		if (Busy)
		{
			Animation(window, "EffectSpelling", NativeBuffs.Now - started, 85f, 114f, loop: false);
			if (NativeBuffs.Now - started < 2.5)
			{
				Animation(window, "EffectTwinkling", NativeBuffs.Now - started, 85f, 114f, loop: true);
			}
			Texture2D val2 = Art.Tex("wz://UI/UIWindow.img/VegaSpell/GaugeBar/gauge");
			float num3 = Mathf.Min(159f, 12f + (float)(NativeBuffs.Now - started) * 63f);
			if ((Object)(object)val2 != (Object)null && num3 > 19f)
			{
				GUI.DrawTexture(At(window, 19f, 178f, num3 - 19f, 9f), (Texture)(object)val2);
			}
		}
		bool enabled = GUI.enabled;
		GUI.enabled &= !Busy && equip != null && scroll != null;
		if (NativeUi.Button(At(window, 67f, 209f, 43f, 17f), "UI/UIWindow.img/VegaSpell/BtStart"))
		{
			string text = Start();
			if (text != null)
			{
				BottomBar.Toast(text);
			}
		}
		GUI.enabled = enabled && !Busy;
		if (NativeUi.Button(At(window, 158f, 8f, 12f, 12f), "UI/UIWindow.img/VegaSpell/BtCancel"))
		{
			Close();
		}
		GUI.enabled = enabled;
		if (flag && equip != null)
		{
			val = At(window, 31f, 117f, 25f, 40f);
			if (val.Contains(Pointer.Pos))
			{
				ItemTooltip.SetHover(equip.itemId, equipped: false, equip);
			}
		}
		if (flag && scroll != null)
		{
			val = At(window, 117f, 117f, 25f, 40f);
			if (val.Contains(Pointer.Pos))
			{
				ItemTooltip.SetHover(scroll.itemId);
			}
		}
	}

	static NativeVegaWindow()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
	}
}
