using System.Collections.Generic;
using UnityEngine;

public static class InvenWindow
{
	private static readonly List<KeyValuePair<Rect, InventoryEntry>> equipmentTargets = new List<KeyValuePair<Rect, InventoryEntry>>();

	public static bool Visible;

	private static Vector2 pos = new Vector2(30f, 90f);

	private static bool expanded;

	private static bool dragging;

	private static bool sorted;

	private static Vector2 grab;

	private static int tab;

	private static int scroll;

	private static float S => BottomBar.S;

	public static int InventoryType => tab + 1;

	public static InventoryEntry EquipmentAt(Vector2 pointer)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible || tab != 0)
		{
			return null;
		}
		foreach (KeyValuePair<Rect, InventoryEntry> equipmentTarget in equipmentTargets)
		{
			Rect key = equipmentTarget.Key;
			if (key.Contains(pointer))
			{
				return equipmentTarget.Value;
			}
		}
		return null;
	}

	public static bool SlotAt(Vector2 pointer, out int position)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		position = 0;
		if (!Visible)
		{
			return false;
		}
		int num = Profile.InventoryCapacity(tab);
		Rect r = new Rect(pos.x * S, pos.y * S, (float)(expanded ? 603 : 175) * S, 289f * S);
		for (int i = 0; i < (expanded ? 96 : 24); i++)
		{
			int num2 = (expanded ? i : (scroll * 4 + i));
			if (num2 >= num)
			{
				break;
			}
			int num3 = (expanded ? (i / 24) : 0);
			int num4 = i % 24;
			float x = 8 + num3 * (expanded ? 149 : 0) + num4 % 4 * 36;
			float y = 50 + num4 / 4 * 34;
			Rect val = At(r, x, y, 32f, 32f);
			if (val.Contains(pointer))
			{
				position = num2 + 1;
				return true;
			}
		}
		return false;
	}

	private static Texture2D Tex(string node)
	{
		return Art.Tex("wz://UI/UIWindow.img/Item/" + node);
	}

	private static Rect At(Rect r, float x, float y, float w, float h)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(r.x + x * S, r.y + y * S, w * S, h * S);
	}

	private static void Image(Rect r, string node, float x, float y)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Tex(node);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(At(r, x, y, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
		}
	}

	private static void Text(Rect r, string value, bool right = false, int size = 11)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected Obj, but got Unknown
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = Mathf.RoundToInt((float)size * S),
			alignment = (TextAnchor)(right ? 5 : 3),
			clipping = (TextClipping)1
		};
		val.normal.textColor = Color.black;
		GUI.Label(r, value, val);
	}

	public static void Toggle()
	{
		Visible = !Visible;
	}

	private static void Organize(bool merge)
	{
		if (GameSession.InGame)
		{
			InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
			{
				characterId = GameSession.CharacterId,
				mergeItems = merge,
				sortItems = !merge,
				sortInventoryType = tab + 1
			});
			if (inventoryChangeResponse == null || !inventoryChangeResponse.ok)
			{
				BottomBar.Toast((inventoryChangeResponse == null || string.IsNullOrEmpty(inventoryChangeResponse.error)) ? "无法整理背包" : inventoryChangeResponse.error);
				return;
			}
			GameSession.Character.inventory = inventoryChangeResponse.inventory;
			GameSession.ApplyInventoryToProfile();
			Profile.Save();
			scroll = 0;
		}
	}

	private static void TabPlate(Rect r, bool on)
	{
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		string text = (on ? "1" : "0");
		Texture2D val = Art.Tex("wz://UI/Basic.img/Tab2/left" + text);
		Texture2D val2 = Art.Tex("wz://UI/Basic.img/Tab2/fill" + text);
		Texture2D val3 = Art.Tex("wz://UI/Basic.img/Tab2/right" + text);
		float num = 4f * S;
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(new Rect(r.x + num, r.y, r.width - num * 2f, r.height), (Texture)(object)val2);
		}
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(r.x, r.y, num, r.height), (Texture)(object)val);
		}
		if ((Object)(object)val3 != (Object)null)
		{
			GUI.DrawTexture(new Rect(r.xMax - num, r.y, num, r.height), (Texture)(object)val3);
		}
	}

	public static void Draw()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Invalid comparison between Unknown and I4
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Invalid comparison between Unknown and I4
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0452: Unknown result type (might be due to invalid IL or missing references)
		//IL_0458: Invalid comparison between Unknown and I4
		//IL_03e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ec: Invalid comparison between Unknown and I4
		//IL_03ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0403: Unknown result type (might be due to invalid IL or missing references)
		//IL_0408: Unknown result type (might be due to invalid IL or missing references)
		//IL_040c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0418: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_08df: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_08fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0905: Unknown result type (might be due to invalid IL or missing references)
		//IL_091b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0922: Unknown result type (might be due to invalid IL or missing references)
		//IL_092b: Expected Obj, but got Unknown
		//IL_0932: Unknown result type (might be due to invalid IL or missing references)
		//IL_093c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0951: Unknown result type (might be due to invalid IL or missing references)
		//IL_0967: Unknown result type (might be due to invalid IL or missing references)
		//IL_097c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0791: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0753: Unknown result type (might be due to invalid IL or missing references)
		//IL_0772: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a12: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a27: Unknown result type (might be due to invalid IL or missing references)
		//IL_0813: Unknown result type (might be due to invalid IL or missing references)
		//IL_0545: Unknown result type (might be due to invalid IL or missing references)
		//IL_054b: Invalid comparison between Unknown and I4
		//IL_0a46: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a5b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0826: Unknown result type (might be due to invalid IL or missing references)
		//IL_083f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0868: Unknown result type (might be due to invalid IL or missing references)
		//IL_057b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0888: Unknown result type (might be due to invalid IL or missing references)
		//IL_062e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0634: Invalid comparison between Unknown and I4
		//IL_055a: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_063d: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b2: Invalid comparison between Unknown and I4
		//IL_065f: Unknown result type (might be due to invalid IL or missing references)
		//IL_06bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0685: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ff: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		int num = (expanded ? 603 : 175);
		pos = NativeUi.ClampWindowPosition(pos, new Vector2((float)num, 289f), S);
		Rect val = new Rect(pos.x * S, pos.y * S, (float)num * S, 289f * S);
		NativeUi.RegisterWindow("InvenWindow", val, () => Visible);
		bool flag = (int)Event.current.type == 7 && Pointer.Pressed;
		bool flag2 = (int)Event.current.type == 7 && Pointer.Released;
		Rect val2;
		if (flag)
		{
			val2 = At(val, 0f, 0f, num - 50, 22f);
			if (val2.Contains(Pointer.Pos))
			{
				dragging = true;
				grab = Pointer.Pos / S - pos;
			}
		}
		if (dragging && Pointer.Down)
		{
			pos = NativeUi.ClampWindowPosition(Pointer.Pos / S - grab, new Vector2((float)num, 289f), S);
			val.position = pos * S;
		}
		if (!Pointer.Down)
		{
			dragging = false;
		}
		Image(val, expanded ? "FullBackgrnd" : "backgrnd", 0f, 0f);
		for (int num2 = 0; num2 < 5; num2++)
		{
			bool flag3 = num2 == tab;
			Rect r = At(val, 3 + num2 * 34, 23f, 34f, 19f);
			TabPlate(r, flag3);
			Image(val, "Tab/" + (flag3 ? "enabled" : "disabled") + "/" + num2, (float)(3 + num2 * 34) + 3.5f, flag3 ? 24 : 25);
			if (flag2 && r.Contains(Pointer.Pos))
			{
				tab = num2;
				scroll = 0;
			}
		}
		int num3 = Profile.InventoryCapacity(tab);
		int num4 = Mathf.Max(0, Mathf.CeilToInt((float)num3 / 4f) - 6);
		scroll = Mathf.Clamp(scroll, 0, num4);
		bool flag4 = GameSession.InGame && GameSession.Character != null && GameSession.Character.inventory != null;
		Dictionary<int, InventoryEntry> dictionary = new Dictionary<int, InventoryEntry>();
		List<InventoryEntry> list = null;
		List<int> list2 = null;
		if (flag4)
		{
			foreach (InventoryEntry item in GameSession.Character.inventory)
			{
				if (item.inventoryType == tab + 1 && item.position > 0)
				{
					dictionary[item.position] = item;
				}
			}
			if (tab == 0)
			{
				list = new List<InventoryEntry>(dictionary.Values);
				list.Sort((InventoryEntry left, InventoryEntry right) => left.position.CompareTo(right.position));
			}
		}
		else
		{
			list2 = new List<int>();
			if (tab == 0)
			{
				bool flag5 = false;
				foreach (string ownedWeapon in Profile.OwnedWeapons)
				{
					int result;
					if (!flag5 && ownedWeapon == Profile.equipWeapon)
					{
						flag5 = true;
					}
					else if (int.TryParse(ownedWeapon, out result))
					{
						list2.Add(result);
					}
				}
			}
			list2.AddRange(Profile.InventoryItems(tab));
		}
		if (!expanded && num4 > 0 && (int)Event.current.type == 7)
		{
			val2 = At(val, 8f, 50f, 148f, 202f);
			if (val2.Contains(Pointer.Pos))
			{
				float y = Input.mouseScrollDelta.y;
				if (Mathf.Abs(y) > 0.01f)
				{
					scroll = Mathf.Clamp(scroll - (int)Mathf.Sign(y), 0, num4);
				}
			}
		}
		if ((int)Event.current.type == 7)
		{
			equipmentTargets.Clear();
		}
		for (int num5 = 0; num5 < (expanded ? 96 : 24); num5++)
		{
			int num6 = (expanded ? num5 : (scroll * 4 + num5));
			int num7 = (expanded ? (num5 / 24) : 0);
			int num8 = num5 % 24;
			float num9 = 8 + num7 * (expanded ? 149 : 0) + num8 % 4 * 36;
			float num10 = 50 + num8 / 4 * 34;
			Rect val3 = At(val, num9, num10, 32f, 32f);
			if (num6 >= num3)
			{
				Image(val, "disabled", num9, num10);
				continue;
			}
			int num11 = num6 + 1;
			InventoryEntry value = null;
			int num12 = 0;
			if (flag4)
			{
				dictionary.TryGetValue(num11, out value);
				if (value != null)
				{
					num12 = value.itemId;
				}
			}
			else if (num6 < list2.Count)
			{
				num12 = list2[num6];
			}
			if (num12 <= 0)
			{
				continue;
			}
			if ((int)Event.current.type == 7 && list != null && value != null)
			{
				equipmentTargets.Add(new KeyValuePair<Rect, InventoryEntry>(val3, value));
			}
			Texture2D val4 = NativeItemSource.Icon(num12);
			if ((Object)(object)val4 != (Object)null)
			{
				GUI.DrawTexture(val3, (Texture)(object)val4, (ScaleMode)2);
			}
			if (tab != 0)
			{
				float num13 = num9;
				string text = (value?.quantity ?? Profile.ItemCount(num12)).ToString();
				for (int num14 = 0; num14 < text.Length; num14++)
				{
					Texture2D val5 = Art.Tex("wz://UI/Basic.img/ItemNo/" + text[num14]);
					if (!((Object)(object)val5 == (Object)null))
					{
						GUI.DrawTexture(At(val, num13, num10 + 20f, ((Texture)val5).width, ((Texture)val5).height), (Texture)(object)val5);
						num13 += (float)((Texture)val5).width;
					}
				}
			}
			if ((int)Event.current.type == 7 && num12 > 0 && val3.Contains(Pointer.Pos))
			{
				ItemTooltip.SetHover(num12, equipped: false, value);
			}
			if (flag && NativeTradeWindow.CanOffer && val3.Contains(Pointer.Pos))
			{
				NativeTradeWindow.BeginItemDrag(value);
			}
			if (flag && !NativeTradeWindow.CanOffer && !NativeInventoryActions.Carrying && val3.Contains(Pointer.Pos))
			{
				NativeInventoryActions.PickFromInventory(num12, value?.position ?? num11);
			}
			if ((int)Event.current.type == 7 && Pointer.DoubleClicked && val3.Contains(Pointer.Pos))
			{
				Pointer.ConsumeClick();
				if (!NativeTradeWindow.CanOffer)
				{
					NativeInventoryActions.DoubleClick(num12, value?.position ?? num11);
				}
			}
		}
		if (!expanded)
		{
			string text2 = "wz://UI/Basic.img/VScr4/" + ((num3 > 24) ? "enabled/" : "disabled/");
			Texture2D val6 = Art.Tex(text2 + "base");
			if ((Object)(object)val6 != (Object)null)
			{
				for (float num15 = 64f; num15 < 238f; num15 += 13f)
				{
					GUI.DrawTexture(At(val, 152f, num15, 15f, Mathf.Min(13f, 238f - num15)), (Texture)(object)val6);
				}
			}
			Rect val7 = At(val, 152f, 51f, 15f, 13f);
			Rect val8 = At(val, 152f, 238f, 15f, 13f);
			Texture2D val9 = Art.Tex(text2 + ((num3 > 24) ? "prev0" : "prev"));
			Texture2D val10 = Art.Tex(text2 + ((num3 > 24) ? "next0" : "next"));
			if ((Object)(object)val9 != (Object)null)
			{
				GUI.DrawTexture(val7, (Texture)(object)val9);
			}
			if ((Object)(object)val10 != (Object)null)
			{
				GUI.DrawTexture(val8, (Texture)(object)val10);
			}
			if (num3 > 24)
			{
				float num16 = 149f;
				GUI.DrawTexture(At(val, 152f, 64f + (float)scroll * num16 / (float)Mathf.Max(1, num4), 15f, 25f), (Texture)(object)Art.Tex(text2 + "thumb0"));
				if (flag && val7.Contains(Pointer.Pos))
				{
					scroll = Mathf.Max(0, scroll - 1);
				}
				if (flag && val8.Contains(Pointer.Pos))
				{
					scroll = Mathf.Min(num4, scroll + 1);
				}
			}
		}
		if (NativeUi.Button(At(val, 7f, 266f, 14f, 14f), "UI/UIWindow.img/Item/BtCoin"))
		{
			NativeInventoryActions.PromptDropMeso();
		}
		GUIStyle val11 = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = Mathf.RoundToInt(12f * S),
			alignment = (TextAnchor)2,
			clipping = (TextClipping)1
		};
		val11.normal.textColor = Color.black;
		GUI.Label(At(val, 25f, 266f, 113f, 16f), NumberFormat.Compact(Profile.gold), val11);
		if (NativeUi.Button(At(val, num - 50, 6f, 12f, 12f), "UI/UIWindow.img/Item/" + (expanded ? "BtSmall" : "BtFull")))
		{
			expanded = !expanded;
			scroll = 0;
		}
		else if (NativeUi.Button(At(val, num - 35, 6f, 12f, 12f), "UI/UIWindow.img/Item/" + (sorted ? "BtSort" : "BtGather")))
		{
			Organize(!sorted);
			sorted = !sorted;
		}
		else if (NativeUi.Button(At(val, num - 20, 6f, 12f, 12f), "UI/Basic.img/BtClose"))
		{
			Visible = false;
		}
		else if (expanded && NativeUi.Button(At(val, 449f, 258f, 93f, 18f), "UI/UIWindow.img/Item/BtCashshop"))
		{
			BottomBar.Toast("容量扩充需在商城购买：2000点券增加4格，最多96格");
		}
	}

	static InvenWindow()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
	}
}
