using System;
using System.Collections.Generic;
using Server.Bot;
using UnityEngine;

public static class NativeItemSearchWindow
{
	private enum Mode
	{
		Entry,
		Results,
		Hot,
		Category,
		Items
	}

	private static readonly string[] Categories = new string[5] { "装备", "消耗", "装饰", "其他", "现金" };

	private static Mode mode = Mode.Entry;

	private static bool opened;

	private static List<int> suggestions = new List<int>();

	private static List<OwlHit> hits = new List<OwlHit>();

	private static readonly List<int> filtered = new List<int>();

	private static int category = -1;

	private static int selection = -1;

	private static int page;

	private static string query = "";

	private static int owlItemId;

	private static int owlPosition;

	private static Vector2 position = new Vector2(80f, 40f);

	private static Vector2 grab;

	private static bool dragging;

	private static double lastClick;

	private static double pendingVisitAt;

	private static int pendingOwner;

	private static int pendingMap;

	private const int Rows = 8;

	private static float W => (mode == Mode.Results) ? 438 : ((mode == Mode.Entry) ? 248 : ((mode == Mode.Items) ? 205 : 270));

	private static float H => (mode == Mode.Results) ? 430 : 271;

	public static bool Visible => opened;

	public static void Reset()
	{
		mode = Mode.Entry;
		opened = false;
		suggestions.Clear();
		hits.Clear();
		filtered.Clear();
		category = (selection = -1);
		page = 0;
		query = "";
		owlItemId = (owlPosition = 0);
		dragging = false;
		pendingOwner = (pendingMap = 0);
		pendingVisitAt = 0.0;
	}

	public static void Open(int itemId, int inventoryPosition)
	{
		if (GameSession.InGame)
		{
			owlItemId = itemId;
			owlPosition = inventoryPosition;
			OwlResponse owlResponse = Request(OwlOperation.Open);
			if (owlResponse?.suggestions != null)
			{
				suggestions = owlResponse.suggestions;
			}
			mode = Mode.Entry;
			opened = true;
			selection = -1;
			page = 0;
		}
	}

	public static void Close()
	{
		Reset();
	}

	private static OwlResponse Request(OwlOperation operation, int itemId = 0, int ownerId = 0, int mapId = 0)
	{
		if (!GameSession.InGame)
		{
			return null;
		}
		return GameSession.Net.Owl(new OwlRequest
		{
			characterId = GameSession.CharacterId,
			operation = operation,
			itemId = itemId,
			ownerId = ownerId,
			mapId = mapId
		});
	}

	public static void Tick()
	{
		if (pendingOwner == 0 || (pendingVisitAt > 0.0 && (double)Time.realtimeSinceStartup < pendingVisitAt))
		{
			return;
		}
		if (Profile.LastMapId != pendingMap)
		{
			if ((double)Time.realtimeSinceStartup > pendingVisitAt + 5.0)
			{
				pendingOwner = 0;
			}
		}
		else
		{
			int owner = pendingOwner;
			pendingOwner = 0;
			NativeMarketWindow.Visit(owner);
		}
	}

	private static void SearchByText()
	{
		string text = query.Trim();
		if (text.Length == 0)
		{
			BottomBar.Toast("请输入想搜索的道具名称");
			return;
		}
		if (int.TryParse(text, out var result) && CatalogName(result) != null)
		{
			Search(result);
			return;
		}
		List<int> list = MatchNames(text);
		if (list.Count == 1)
		{
			Search(list[0]);
			return;
		}
		if (list.Count == 0)
		{
			BottomBar.Toast("没有找到名字里带「" + text + "」的道具");
			return;
		}
		filtered.Clear();
		filtered.AddRange(list);
		category = -1;
		mode = Mode.Items;
		selection = -1;
		page = 0;
	}

	private static void Search(int itemId)
	{
		OwlResponse owlResponse = Request(OwlOperation.Search, itemId);
		if (owlResponse == null)
		{
			return;
		}
		if (!owlResponse.ok)
		{
			BottomBar.Toast(owlResponse.error);
			return;
		}
		if (owlResponse.charged && !Profile.RemoveItem(owlItemId, 1, owlPosition))
		{
			BottomBar.Toast("猫头鹰搜索器已用完");
		}
		hits = owlResponse.hits ?? new List<OwlHit>();
		mode = Mode.Results;
		selection = -1;
		page = 0;
	}

	private static List<int> MatchNames(string text)
	{
		List<int> list = new List<int>();
		BotItemCatalog shared = BotItemCatalog.Shared;
		foreach (int itemId in shared.ItemIds)
		{
			string text2 = shared.Name(itemId);
			if (!string.IsNullOrEmpty(text2) && text2.Contains(text))
			{
				list.Add(itemId);
			}
			if (list.Count >= 200)
			{
				break;
			}
		}
		list.Sort(ByName);
		return list;
	}

	private static string CatalogName(int id)
	{
		return BotItemCatalog.Shared.Name(id);
	}

	private static int ByName(int a, int b)
	{
		string strA = CatalogName(a) ?? "";
		string strB = CatalogName(b) ?? "";
		return string.CompareOrdinal(strA, strB);
	}

	private static void Warp(OwlHit hit)
	{
		OwlResponse owlResponse = Request(OwlOperation.Warp, CurrentItemId(), hit.ownerId, hit.mapId);
		if (owlResponse == null)
		{
			return;
		}
		if (!owlResponse.ok || owlResponse.map < 0)
		{
			NativeSystemMessage.Drop(5, owlResponse.error);
			return;
		}
		pendingOwner = hit.ownerId;
		pendingMap = owlResponse.map;
		pendingVisitAt = Time.realtimeSinceStartup + 0.5f;
		Reset();
		if ((Object)(object)GameLoop.I?.field != (Object)null && Profile.LastMapId != owlResponse.map)
		{
			GameLoop.I.field.WarpTo(owlResponse.map);
		}
	}

	private static int CurrentItemId()
	{
		int num = page * 8 + selection;
		if (num < 0 || num >= hits.Count)
		{
			return 0;
		}
		return hits[num].item.itemId;
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
		Texture2D val = Art.Tex("wz://UI/UIWindow.img/itemSearch/" + name);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(x, y, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
		}
	}

	private static void Hotspot(string caption, Rect rect, Vector2 p, Action click)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Invalid comparison between Unknown and I4
		bool flag = rect.Contains(p);
		if (flag)
		{
			GUI.color = new Color(1f, 1f, 1f, 0.25f);
			GUI.DrawTexture(rect, (Texture)(object)Texture2D.whiteTexture);
			GUI.color = Color.white;
		}
		if ((GUI.enabled && (int)Event.current.type == 7 && Pointer.Released) & flag)
		{
			Pointer.ConsumeClick();
			click();
		}
	}

	public static void Draw()
	{
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Invalid comparison between Unknown and I4
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		if (!GameSession.InGame)
		{
			Reset();
			return;
		}
		float s = BottomBar.S;
		Vector2 size = new Vector2(W, H);
		position = NativeUi.ClampWindowPosition(position, size, s);
		NativeUi.RegisterWindow("NativeItemSearchWindow", new Rect(position.x * s, position.y * s, W * s, H * s), () => Visible);
		Vector2 val = Pointer.Pos / s;
		Matrix4x4 matrix = GUI.matrix;
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(s, s, 1f));
		try
		{
			if (GUI.enabled && (int)Event.current.type == 7 && Pointer.Pressed)
			{
				Rect val2 = new Rect(position.x, position.y, W, 22f);
				if (val2.Contains(val))
				{
					dragging = true;
					grab = val - position;
				}
			}
			if (dragging && Pointer.Down)
			{
				position = NativeUi.ClampWindowPosition(val - grab, size, s);
			}
			if (!Pointer.Down)
			{
				dragging = false;
			}
			GUI.BeginGroup(new Rect(position.x, position.y, W, H));
			try
			{
				Vector2 p = val - position;
				switch (mode)
				{
				case Mode.Entry:
					DrawEntry(p);
					break;
				case Mode.Results:
					DrawResults(p);
					break;
				case Mode.Hot:
					DrawHot(p);
					break;
				case Mode.Category:
					DrawCategory(p);
					break;
				case Mode.Items:
					DrawItems(p);
					break;
				}
				DrawClose(p);
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

	private static void DrawClose(Vector2 p)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected Obj, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Invalid comparison between Unknown and I4
		Rect val = new Rect(W - 20f, 4f, 16f, 16f);
		bool flag = val.Contains(p);
		if (flag)
		{
			GUI.color = new Color(1f, 0.85f, 0.85f, 0.5f);
			GUI.DrawTexture(val, (Texture)(object)Texture2D.whiteTexture);
			GUI.color = Color.white;
		}
		GUI.Label(val, "×", new GUIStyle(Text())
		{
			alignment = (TextAnchor)4,
			fontSize = 14
		});
		if ((GUI.enabled && (int)Event.current.type == 7 && Pointer.Released) & flag)
		{
			Pointer.ConsumeClick();
			Close();
		}
	}

	private static void DrawEntry(Vector2 p)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Invalid comparison between Unknown and I4
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Invalid comparison between Unknown and I4
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		Image("backgrnd", 0f, 0f);
		GUI.SetNextControlName("owlquery");
		query = GUI.TextField(new Rect(26f, 96f, 140f, 18f), query, 30);
		if ((int)Event.current.type == 4 && (int)Event.current.keyCode == 13 && GUI.GetNameOfFocusedControl() == "owlquery")
		{
			Event.current.Use();
			SearchByText();
		}
		if (NativeUi.Button(new Rect(170f, 96f, 38f, 17f), "UI/UIWindow.img/itemSearch/BtSearch", p))
		{
			SearchByText();
		}
		Hotspot("人气查询前十名", new Rect(4f, 208f, 240f, 29f), p, () =>
		{
			mode = Mode.Hot;
			selection = -1;
			page = 0;
		});
		Hotspot("分类查询留言", new Rect(4f, 238f, 240f, 29f), p, () =>
		{
			mode = Mode.Category;
			selection = -1;
			page = 0;
		});
	}

	private static void DrawResults(Vector2 p)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0306: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_0383: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0405: Unknown result type (might be due to invalid IL or missing references)
		//IL_040b: Invalid comparison between Unknown and I4
		Image("resultback", 0f, 0f);
		query = GUI.TextField(new Rect(14f, 80f, 300f, 22f), query, 30);
		if (NativeUi.Button(new Rect(322f, 81f, 38f, 17f), "UI/UIWindow.img/itemSearch/BtSearch", p))
		{
			SearchByText();
		}
		if (NativeUi.Button(new Rect(12f, 399f, 59f, 17f), "UI/UIWindow.img/itemSearch/BtBack", p))
		{
			mode = Mode.Entry;
			return;
		}
		if (NativeUi.Button(new Rect(78f, 399f, 59f, 17f), "UI/UIWindow.img/itemSearch/BtRetry", p))
		{
			SearchByText();
		}
		int num = Mathf.Max(1, (hits.Count + 8 - 1) / 8);
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && page > 0;
		if (NativeUi.Button(new Rect(300f, 401f, 13f, 13f), "UI/UIWindow.img/itemSearch/BtLeft", p))
		{
			page--;
		}
		GUI.enabled = enabled && page < num - 1;
		if (NativeUi.Button(new Rect(318f, 401f, 13f, 13f), "UI/UIWindow.img/itemSearch/BtRight", p))
		{
			page++;
		}
		GUI.enabled = enabled;
		GUI.Label(new Rect(334f, 402f, 30f, 13f), page + 1 + "/" + num, Center());
		if (NativeUi.Button(new Rect(370f, 399f, 43f, 17f), "UI/UIWindow.img/itemSearch/BtGo", p) && selection >= 0 && selection < 8)
		{
			int num2 = page * 8 + selection;
			if (num2 < hits.Count)
			{
				Warp(hits[num2]);
			}
		}
		for (int i = 0; i < 8; i++)
		{
			float num3 = 158 + 30 * i;
			int num4 = page * 8 + i;
			Rect val = new Rect(8f, num3 - 4f, 422f, 28f);
			bool flag = val.Contains(p);
			if (num4 == selection)
			{
				GUI.color = new Color(0.8f, 0.9f, 1f, 0.45f);
				GUI.DrawTexture(val, (Texture)(object)Texture2D.whiteTexture);
				GUI.color = Color.white;
			}
			else if (flag)
			{
				GUI.color = new Color(1f, 1f, 1f, 0.25f);
				GUI.DrawTexture(val, (Texture)(object)Texture2D.whiteTexture);
				GUI.color = Color.white;
			}
			if (num4 >= hits.Count)
			{
				continue;
			}
			OwlHit owlHit = hits[num4];
			GUI.Label(new Rect(14f, num3, 72f, 14f), owlHit.ownerName, Text());
			GUI.Label(new Rect(88f, num3, 80f, 14f), owlHit.shopName, Text());
			GUI.Label(new Rect(170f, num3, 38f, 14f), owlHit.bundles.ToString(), Text(right: true));
			GUI.Label(new Rect(212f, num3, 52f, 14f), owlHit.price.ToString("N0"), Text(right: true));
			GUI.Label(new Rect(268f, num3, 26f, 14f), owlHit.quantity.ToString(), Text(right: true));
			GUI.Label(new Rect(300f, num3, 130f, 14f), Info(owlHit), Text());
			if (!((GUI.enabled && (int)Event.current.type == 7) & flag))
			{
				continue;
			}
			if (Pointer.Released)
			{
				Pointer.ConsumeClick();
				if (Time.realtimeSinceStartupAsDouble - lastClick < 0.45 && selection == i)
				{
					Warp(owlHit);
					break;
				}
				lastClick = Time.realtimeSinceStartupAsDouble;
				selection = i;
			}
			else if (Pointer.Pressed)
			{
				Pointer.ConsumeClick();
			}
		}
	}

	private static string Info(OwlHit hit)
	{
		if (!hit.equip || hit.item == null)
		{
			return "";
		}
		InventoryEntry item = hit.item;
		List<string> parts = new List<string>();
		Add("攻", item.watk);
		Add("魔", item.matk);
		Add("防", item.wdef);
		Add("魔防", item.mdef);
		Add("力", item.str);
		Add("敏", item.dex);
		Add("智", item.int_);
		Add("运", item.luk);
		Add("HP", item.hp);
		Add("MP", item.mp);
		Add("移", item.speed);
		Add("跳", item.jump);
		if (parts.Count != 0)
		{
			return string.Join(" ", parts.ToArray());
		}
		return "装备";
		void Add(string label, int value)
		{
			if (value != 0)
			{
				parts.Add(label + value);
			}
		}
	}

	private static void DrawHot(Vector2 p)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		Image("categoryback", 0f, 0f);
		GUI.Label(new Rect(22f, 40f, 200f, 14f), "人气查询前十名", Text());
		ListPage(p, suggestions.Count, (int index) =>
		{
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			int id = suggestions[index];
			string text = CatalogName(id);
			GUI.Label(new Rect(22f, (float)(58 + 22 * (index - page * 8)), 230f, 16f), (text == null) ? id.ToString() : text, Text());
			GUI.Label(new Rect(150f, (float)(58 + 22 * (index - page * 8)), 100f, 16f), id.ToString(), Text(right: true));
		}, (int index) =>
		{
			int num = suggestions[index];
			if (num > 0)
			{
				Search(num);
			}
		});
		if (NativeUi.Button(new Rect(12f, 240f, 59f, 17f), "UI/UIWindow.img/itemSearch/BtBack", p))
		{
			mode = Mode.Entry;
		}
	}

	private static void DrawCategory(Vector2 p)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		Image("categoryback", 0f, 0f);
		GUI.Label(new Rect(22f, 40f, 200f, 14f), "分类查询（先在首页输入名字可过滤）", Text());
		ListPage(p, Categories.Length, (int index) =>
		{
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			GUI.Label(new Rect(30f, (float)(58 + 22 * (index - page * 8)), 220f, 16f), Categories[index], Text());
		}, (int index) =>
		{
			int num = index + 1;
			filtered.Clear();
			foreach (int itemId in BotItemCatalog.Shared.ItemIds)
			{
				if (itemId / 1000000 == num)
				{
					string text = CatalogName(itemId);
					if (query.Trim().Length <= 0 || (text != null && text.Contains(query.Trim())))
					{
						filtered.Add(itemId);
						if (filtered.Count >= 200)
						{
							break;
						}
					}
				}
			}
			filtered.Sort(ByName);
			category = index;
			mode = Mode.Items;
			selection = -1;
			page = 0;
		});
		if (NativeUi.Button(new Rect(12f, 240f, 59f, 17f), "UI/UIWindow.img/itemSearch/BtBack", p))
		{
			mode = Mode.Entry;
		}
	}

	private static void DrawItems(Vector2 p)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		Image("searchback", 0f, 0f);
		string text = ((category >= 0 && category < Categories.Length) ? Categories[category] : "搜索结果");
		GUI.Label(new Rect(18f, 40f, 170f, 14f), text + "（" + filtered.Count + "）", Text());
		ListPage(p, filtered.Count, (int index) =>
		{
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			int id = filtered[index];
			string text2 = CatalogName(id);
			GUI.Label(new Rect(18f, (float)(58 + 22 * (index - page * 8)), 120f, 16f), string.IsNullOrEmpty(text2) ? id.ToString() : text2, Text());
			GUI.Label(new Rect(140f, (float)(58 + 22 * (index - page * 8)), 55f, 16f), id.ToString(), Text(right: true));
		}, (int index) =>
		{
			int num = filtered[index];
			if (num > 0)
			{
				Search(num);
			}
		});
		if (NativeUi.Button(new Rect(12f, 240f, 59f, 17f), "UI/UIWindow.img/itemSearch/BtBack", p))
		{
			mode = ((category >= 0) ? Mode.Category : Mode.Entry);
			page = 0;
		}
	}

	private static void ListPage(Vector2 p, int count, Action<int> drawRow, Action<int> clickRow)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Invalid comparison between Unknown and I4
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		int num = Mathf.Max(1, (count + 8 - 1) / 8);
		page = Mathf.Clamp(page, 0, num - 1);
		for (int i = 0; i < 8; i++)
		{
			int num2 = page * 8 + i;
			if (num2 >= count)
			{
				break;
			}
			Rect val = new Rect(4f, (float)(54 + 22 * i), W - 8f, 21f);
			bool flag = val.Contains(p);
			if (num2 == selection)
			{
				GUI.color = new Color(0.8f, 0.9f, 1f, 0.45f);
				GUI.DrawTexture(val, (Texture)(object)Texture2D.whiteTexture);
				GUI.color = Color.white;
			}
			else if (flag)
			{
				GUI.color = new Color(1f, 1f, 1f, 0.25f);
				GUI.DrawTexture(val, (Texture)(object)Texture2D.whiteTexture);
				GUI.color = Color.white;
			}
			drawRow(num2);
			if (((GUI.enabled && (int)Event.current.type == 7) & flag) && Pointer.Released)
			{
				Pointer.ConsumeClick();
				selection = num2;
				clickRow(num2);
			}
		}
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && page > 0;
		if (NativeUi.Button(new Rect(160f, 244f, 13f, 13f), "UI/UIWindow.img/itemSearch/BtLeft", p))
		{
			page--;
		}
		GUI.enabled = enabled && page < num - 1;
		if (NativeUi.Button(new Rect(178f, 244f, 13f, 13f), "UI/UIWindow.img/itemSearch/BtRight", p))
		{
			page++;
		}
		GUI.enabled = enabled;
		GUI.Label(new Rect(196f, 245f, 60f, 13f), page + 1 + "/" + num, Text());
	}

	static NativeItemSearchWindow()
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
	}
}
