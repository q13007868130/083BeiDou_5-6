using System;
using System.Collections.Generic;
using MapleDash.Wz;
using Server.Bot;
using UnityEngine;

public static class NativePetExclusionWindow
{
	private static bool visible;

	private static bool search;

	private static bool results;

	private static int petId;

	private static int selected = -1;

	private static int resultSelected = -1;

	private static float scroll;

	private static float resultScroll;

	private static string query = "";

	private static List<int> entries = new List<int>();

	private static List<int> matches = new List<int>();

	private const string User = "UI/UIWindow.img/UserInfo/";

	private const string Search = "UI/UIWindow.img/ExceptionItemSearch/";

	public static bool Visible => visible;

	public static void Close()
	{
		visible = (search = (results = false));
	}

	public static bool CoversPointer(Vector2 parent, int mode, float scale)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		if (!visible)
		{
			return false;
		}
		Vector2 val = parent + new Vector2(270f, (float)((mode == 0) ? 34 : 196));
		Vector2 point = Pointer.Pos / scale;
		if (Hit(new Rect(val.x, val.y, 159f, 147f)))
		{
			return true;
		}
		if (!search)
		{
			return false;
		}
		Vector2 val2 = NativeUi.ClampWindowPosition(val, new Vector2(219f, (float)(results ? 240 : 123)), scale);
		return Hit(new Rect(val2.x, val2.y, 219f, (float)(results ? 240 : 123)));
		bool Hit(Rect rect)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			if (!rect.Contains(point))
			{
				if (Pointer.Down || Pointer.Released)
				{
					return rect.Contains(Pointer.PressPosition / scale);
				}
				return false;
			}
			return true;
		}
	}

	public static void Toggle(InventoryEntry pet)
	{
		if (visible && petId == pet.petId)
		{
			Close();
			return;
		}
		InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
		{
			characterId = GameSession.CharacterId,
			petId = pet.petId,
			readPetExclusions = true
		});
		if (!inventoryChangeResponse.ok)
		{
			BottomBar.Toast(inventoryChangeResponse.error);
			return;
		}
		petId = pet.petId;
		entries = inventoryChangeResponse.petExclusions;
		selected = -1;
		scroll = 0f;
		visible = true;
		search = (results = false);
	}

	private static void Save(List<int> replacement)
	{
		InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
		{
			characterId = GameSession.CharacterId,
			petId = petId,
			writePetExclusions = true,
			petExclusions = replacement.ToArray()
		});
		if (!inventoryChangeResponse.ok)
		{
			BottomBar.Toast(inventoryChangeResponse.error);
			return;
		}
		entries = inventoryChangeResponse.petExclusions;
		selected = -1;
	}

	private static void Add(int id)
	{
		if (entries.Count < 10 && !entries.Contains(id))
		{
			List<int> list = new List<int>(entries);
			list.Add(id);
			list.Sort((int a, int b) => string.Compare(Name(a), Name(b), StringComparison.CurrentCulture));
			Save(list);
		}
	}

	private static string Name(int id)
	{
		if (id != int.MaxValue)
		{
			return WzNames.Item(id);
		}
		return "金币";
	}

	private static void Find()
	{
		matches.Clear();
		resultSelected = -1;
		resultScroll = 0f;
		string text = query.Replace(" ", "").ToLowerInvariant();
		if (text.Length > 0)
		{
			foreach (int itemId in BotItemCatalog.Shared.ItemIds)
			{
				string text2 = BotItemCatalog.Shared.Name(itemId);
				if (!string.IsNullOrEmpty(text2) && text2.ToLowerInvariant().Contains(text))
				{
					ImgNode imgNode = NativeItemSource.Node(itemId);
					if (imgNode != null && imgNode.Int("info/cash") == 0)
					{
						matches.Add(itemId);
					}
				}
			}
		}
		results = true;
	}

	private static void Image(string path, int x, int y)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://" + path);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect((float)x, (float)y, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
		}
	}

	private static bool Button(string path, int x, int y, Vector2 pointer, bool enabled = true)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://" + path + "/normal/0");
		if ((Object)(object)val == (Object)null)
		{
			return false;
		}
		bool enabled2 = GUI.enabled;
		GUI.enabled = enabled2 & enabled;
		bool result = NativeUi.Button(new Rect((float)x, (float)y, (float)((Texture)val).width, (float)((Texture)val).height), path, pointer);
		GUI.enabled = enabled2;
		return result;
	}

	private static void List(List<int> items, ref float offset, ref int selection, int y, int count, Vector2 p, GUIStyle style)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Invalid comparison between Unknown and I4
		for (int i = 0; i < count; i++)
		{
			int num = (int)offset + i;
			if (num < items.Count)
			{
				Rect val = new Rect(10f, (float)(y + i * 14), 138f, 13f);
				bool flag = val.Contains(p);
				if ((num == selection) | flag)
				{
					Color color = GUI.color;
					GUI.color = Color32.op_Implicit((num == selection) ? new Color32((byte)91, (byte)123, (byte)158, byte.MaxValue) : new Color32((byte)187, (byte)201, (byte)217, byte.MaxValue));
					GUI.DrawTexture(val, (Texture)(object)Texture2D.whiteTexture);
					GUI.color = color;
				}
				GUI.Label(new Rect((float)((y == 12) ? 10 : 15), val.y, 138f, 14f), Name(items[num]), style);
				if ((GUI.enabled & flag) && (int)Event.current.type == 7 && Pointer.Clicked)
				{
					selection = num;
					Pointer.ConsumeClick();
				}
				continue;
			}
			break;
		}
	}

	public static void Draw(Vector2 parent, int mode, float scale)
	{
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_0429: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected Obj, but got Unknown
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0305: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0351: Unknown result type (might be due to invalid IL or missing references)
		//IL_037a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0380: Unknown result type (might be due to invalid IL or missing references)
		//IL_0385: Unknown result type (might be due to invalid IL or missing references)
		//IL_0387: Unknown result type (might be due to invalid IL or missing references)
		//IL_038c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_040d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0438: Unknown result type (might be due to invalid IL or missing references)
		//IL_0444: Unknown result type (might be due to invalid IL or missing references)
		//IL_0449: Unknown result type (might be due to invalid IL or missing references)
		//IL_044e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0450: Unknown result type (might be due to invalid IL or missing references)
		//IL_0458: Unknown result type (might be due to invalid IL or missing references)
		//IL_045d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0462: Unknown result type (might be due to invalid IL or missing references)
		//IL_046c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0473: Unknown result type (might be due to invalid IL or missing references)
		//IL_0484: Unknown result type (might be due to invalid IL or missing references)
		//IL_048e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0494: Unknown result type (might be due to invalid IL or missing references)
		//IL_0499: Unknown result type (might be due to invalid IL or missing references)
		//IL_049b: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0510: Unknown result type (might be due to invalid IL or missing references)
		//IL_052e: Unknown result type (might be due to invalid IL or missing references)
		//IL_053f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0581: Unknown result type (might be due to invalid IL or missing references)
		if (!visible || !NativeCharacterInfo.Visible)
		{
			return;
		}
		Vector2 val = parent + new Vector2(270f, (float)((mode == 0) ? 34 : 196));
		Matrix4x4 matrix = GUI.matrix;
		bool enabled = GUI.enabled;
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(scale, scale, 1f));
		GUIStyle val2 = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = 12,
			clipping = (TextClipping)1
		};
		val2.normal.textColor = Color.black;
		GUI.BeginGroup(new Rect(val.x, val.y, 159f, 147f));
		try
		{
			NativeUi.RegisterWindow("PetExclusions", new Rect(val.x * scale, val.y * scale, 159f * scale, 147f * scale), () => visible && NativeCharacterInfo.Visible);
			Vector2 val3 = Pointer.Pos / scale - val;
			GUI.enabled = enabled && !NativeUtilPrompt.Visible && !search;
			Image("UI/UIWindow.img/UserInfo/backgrnd6", 0, 0);
			if (Button("UI/UIWindow.img/UserInfo/BtExceptionHide", 142, 5, val3))
			{
				Close();
				return;
			}
			if (Button("UI/UIWindow.img/UserInfo/BtMeso", 10, 124, val3, entries.Contains(int.MaxValue) || entries.Count < 10))
			{
				if (entries.Contains(int.MaxValue))
				{
					List<int> list = new List<int>(entries);
					list.Remove(int.MaxValue);
					Save(list);
				}
				else
				{
					Add(int.MaxValue);
				}
			}
			if (Button("UI/UIWindow.img/UserInfo/BtRegister", 80, 124, val3, entries.Count < 10))
			{
				search = true;
				query = "";
			}
			if (Button("UI/UIWindow.img/UserInfo/BtDelete", 124, 124, val3, selected >= 0 && selected < entries.Count))
			{
				List<int> list2 = new List<int>(entries);
				list2.RemoveAt(selected);
				Save(list2);
			}
			scroll = NativeUi.Scroll4(new Rect(140f, 25f, 15f, 90f), new Rect(10f, 30f, 128f, 84f), scroll, Mathf.Max(0, entries.Count - 6), 1f, val3);
			List(entries, ref scroll, ref selected, 30, 6, val3, val2);
		}
		finally
		{
			GUI.EndGroup();
			GUI.enabled = enabled;
			GUI.matrix = matrix;
		}
		if (!search)
		{
			return;
		}
		Vector2 val4 = NativeUi.ClampWindowPosition(val, new Vector2(219f, (float)(results ? 240 : 123)), scale);
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(scale, scale, 1f));
		GUI.BeginGroup(new Rect(val4.x, val4.y, 219f, 123f));
		try
		{
			NativeUi.RegisterWindow("PetExclusions.Search", new Rect(val4.x * scale, val4.y * scale, 219f * scale, (float)(results ? 240 : 123) * scale), () => visible && search && NativeCharacterInfo.Visible);
			Vector2 pointer = Pointer.Pos / scale - val4;
			GUI.enabled = enabled && !NativeUtilPrompt.Visible;
			Image("UI/UIWindow.img/ExceptionItemSearch/backgrnd", 0, 0);
			if (Button("UI/Basic.img/BtClose", 195, 8, pointer))
			{
				search = (results = false);
				return;
			}
			query = GUI.TextField(new Rect(15f, 78f, 115f, 13f), query, 256, val2);
			if (Button("UI/UIWindow.img/ExceptionItemSearch/BtSearch", 140, 76, pointer))
			{
				Find();
			}
		}
		finally
		{
			GUI.EndGroup();
			GUI.enabled = enabled;
			GUI.matrix = matrix;
		}
		if (!results)
		{
			return;
		}
		Vector2 val5 = val4 + new Vector2(0f, 120f);
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(scale, scale, 1f));
		GUI.BeginGroup(new Rect(val5.x, val5.y, 162f, 117f));
		try
		{
			Vector2 val6 = Pointer.Pos / scale - val5;
			GUI.enabled = enabled && !NativeUtilPrompt.Visible;
			Image("UI/UIWindow.img/ExceptionItemSearch/resultback", 0, 0);
			resultScroll = NativeUi.Scroll4(new Rect(142f, 8f, 15f, 75f), new Rect(10f, 12f, 130f, 70f), resultScroll, Mathf.Max(0, matches.Count - 5), 1f, val6);
			List(matches, ref resultScroll, ref resultSelected, 12, 5, val6, val2);
			if (Button("UI/UIWindow.img/ExceptionItemSearch/BtSave", 49, 94, val6, resultSelected >= 0 && resultSelected < matches.Count))
			{
				Add(matches[resultSelected]);
			}
			if (Button("UI/UIWindow.img/ExceptionItemSearch/BtDelete", 82, 94, val6, resultSelected >= 0 && resultSelected < matches.Count))
			{
				List<int> list3 = new List<int>(entries);
				list3.Remove(matches[resultSelected]);
				Save(list3);
			}
		}
		finally
		{
			GUI.EndGroup();
			GUI.enabled = enabled;
			GUI.matrix = matrix;
		}
	}
}
