using System;
using System.Collections.Generic;
using Server.Bot;
using UnityEngine;

public sealed class NativeMarketField : MonoBehaviour
{
	private readonly Dictionary<int, NativeImgAnimation> employees = new Dictionary<int, NativeImgAnimation>();

	private readonly Dictionary<int, GameObject> stalls = new Dictionary<int, GameObject>();

	private readonly Dictionary<int, Texture2D> signboards = new Dictionary<int, Texture2D>();

	private readonly HashSet<int> present = new HashSet<int>();

	private readonly List<int> removed = new List<int>();

	private List<MarketShopSnapshot> shops = new List<MarketShopSnapshot>();

	private int map;

	private int world;

	private int channel;

	private float nextRead;

	private GUIStyle nameStyle;

	private GUIStyle signStyle;

	private Texture2D Signboard(int permit)
	{
		if (signboards.TryGetValue(permit, out var value))
		{
			return value;
		}
		Texture2D val = TrySignboard(permit) ?? TrySignboard(5030000);
		signboards[permit] = val;
		return val;
	}

	private static Texture2D TrySignboard(int permit)
	{
		try
		{
			return Art.Tex("wz://Item/Cash/0503.img/" + permit.ToString("D8") + "/skin/signboard");
		}
		catch (Exception)
		{
			return null;
		}
	}

	public void Synchronize(int mapId, int worldId, int channelId)
	{
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		if (map == mapId && world == worldId && channel == channelId && Time.unscaledTime < nextRead)
		{
			return;
		}
		nextRead = Time.unscaledTime + 0.5f;
		map = mapId;
		world = worldId;
		channel = channelId;
		if (!GameSession.InGame)
		{
			Clear();
			return;
		}
		MarketResponse marketResponse = GameSession.Net.Market(new MarketRequest
		{
			characterId = GameSession.CharacterId,
			operation = MarketOperation.Read
		});
		shops = marketResponse.shops;
		present.Clear();
		foreach (MarketShopSnapshot shop in shops)
		{
			if (shop.playerStore)
			{
				continue;
			}
			present.Add(shop.ownerId);
			if (!employees.ContainsKey(shop.ownerId))
			{
				NativeImgAnimation nativeImgAnimation = NativeImgAnimation.Spawn("Item/Cash/0503.img/" + shop.permitId.ToString("D8") + "/employee/stand", ((Component)this).transform, new Vector3((float)shop.x / 40f, (float)(-shop.y) / 40f, 0f), 300, flip: false, repeat: true);
				if ((Object)(object)nativeImgAnimation != (Object)null)
				{
					employees.Add(shop.ownerId, nativeImgAnimation);
				}
				CreateStall(shop);
			}
		}
		removed.Clear();
		foreach (KeyValuePair<int, NativeImgAnimation> employee in employees)
		{
			if (!present.Contains(employee.Key))
			{
				removed.Add(employee.Key);
			}
		}
		foreach (int item in removed)
		{
			employees[item].Remove();
			employees.Remove(item);
			if (stalls.TryGetValue(item, out var value))
			{
				Object.Destroy((Object)(object)value);
				stalls.Remove(item);
			}
		}
	}

	private void CreateStall(MarketShopSnapshot shop)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected Obj, but got Unknown
		if (!stalls.ContainsKey(shop.ownerId))
		{
			Texture2D val = Signboard(shop.permitId);
			if (!((Object)(object)val == (Object)null))
			{
				GameObject val2 = new GameObject("merchant-skin");
				val2.transform.SetParent(((Component)this).transform, false);
				stalls.Add(shop.ownerId, val2);
				AddStallLayer(val2, shop, val, "backgrnd", 299);
				AddStallLayer(val2, shop, val, "shop", 301);
			}
		}
	}

	private static void AddStallLayer(GameObject stall, MarketShopSnapshot shop, Texture2D sign, string node, int order)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		string rel = "wz://Item/Cash/0503.img/" + shop.permitId.ToString("D8") + "/skin/" + node;
		Sprite val = Art.Frame(rel);
		if (!((Object)(object)val == (Object)null))
		{
			Vector2 val2 = Art.Origin(rel);
			GameObject val3 = new GameObject(node);
			val3.transform.SetParent(stall.transform, false);
			val3.transform.position = new Vector3(((float)(shop.x - ((Texture)sign).width / 2) + val2.x) / 40f, ((float)(-shop.y + ((Texture)sign).height - 2) - val2.y) / 40f, 0f);
			SpriteRenderer val4 = val3.AddComponent<SpriteRenderer>();
			val4.sprite = val;
			((Renderer)val4).sortingOrder = order;
		}
	}

	public void Clear()
	{
		foreach (NativeImgAnimation value in employees.Values)
		{
			if ((Object)(object)value != (Object)null)
			{
				value.Remove();
			}
		}
		foreach (GameObject value2 in stalls.Values)
		{
			if ((Object)(object)value2 != (Object)null)
			{
				Object.Destroy((Object)(object)value2);
			}
		}
		stalls.Clear();
		employees.Clear();
		signboards.Clear();
		shops.Clear();
		nextRead = 0f;
	}

	public bool TryVisit(Vector3 point, Func<int, SpriteRenderer> playerRenderer)
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		foreach (MarketShopSnapshot shop in shops)
		{
			SpriteRenderer val;
			if (shop.playerStore)
			{
				val = playerRenderer(shop.ownerId);
			}
			else
			{
				val = (employees.TryGetValue(shop.ownerId, out var value) ? value.Image : null);
			}
			if ((Object)(object)val == (Object)null || (Object)(object)val.sprite == (Object)null)
			{
				continue;
			}
			Bounds bounds = ((Renderer)val).bounds;
			float x = point.x;
			float y = point.y;
			Bounds bounds2 = ((Renderer)val).bounds;
			if (!bounds.Contains(new Vector3(x, y, bounds2.center.z)))
			{
				continue;
			}
			if (shop.playerStore && shop.ownerId == GameSession.CharacterId)
			{
				if (shop.permitId / 10000 == 514)
				{
					NativePlayerShopWindow.Open(shop.permitId);
				}
				else
				{
					NativeMerchantWindow.Open(shop.permitId);
				}
				Pointer.ConsumeClick();
				return true;
			}
			NativeMarketWindow.Visit(shop.ownerId);
			Pointer.ConsumeClick();
			return true;
		}
		return false;
	}

	private void OnGUI()
	{
		//IL_04b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected Obj, but got Unknown
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected Obj, but got Unknown
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Expected Obj, but got Unknown
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Expected Obj, but got Unknown
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Expected Obj, but got Unknown
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Expected Obj, but got Unknown
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Unknown result type (might be due to invalid IL or missing references)
		//IL_031b: Unknown result type (might be due to invalid IL or missing references)
		//IL_031d: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0355: Unknown result type (might be due to invalid IL or missing references)
		//IL_0375: Unknown result type (might be due to invalid IL or missing references)
		//IL_037f: Expected Obj, but got Unknown
		//IL_037a: Unknown result type (might be due to invalid IL or missing references)
		//IL_039e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0402: Unknown result type (might be due to invalid IL or missing references)
		//IL_0435: Unknown result type (might be due to invalid IL or missing references)
		//IL_043a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0441: Unknown result type (might be due to invalid IL or missing references)
		//IL_0455: Unknown result type (might be due to invalid IL or missing references)
		//IL_045b: Invalid comparison between Unknown and I4
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Invalid comparison between Unknown and I4
		//IL_0474: Unknown result type (might be due to invalid IL or missing references)
		//IL_047a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c5: Unknown result type (might be due to invalid IL or missing references)
		Camera main = Camera.main;
		if ((Object)(object)main == (Object)null || !GameSession.InGame || shops.Count == 0)
		{
			return;
		}
		UiKit.Begin();
		if (nameStyle == null)
		{
			nameStyle = new GUIStyle
			{
				font = UiKit.Font,
				fontSize = 12,
				padding = new RectOffset(),
				margin = new RectOffset(),
				alignment = (TextAnchor)0,
				clipping = (TextClipping)1
			};
			nameStyle.normal.textColor = Color.white;
		}
		float num = UiKit.H / (2f * main.orthographicSize * 40f);
		Matrix4x4 matrix = GUI.matrix;
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(num, num, 1f));
		try
		{
			if (signStyle == null)
			{
				signStyle = new GUIStyle
				{
					font = UiKit.Font,
					fontSize = 12,
					padding = new RectOffset(),
					margin = new RectOffset(),
					alignment = (TextAnchor)0,
					clipping = (TextClipping)1,
					wordWrap = false
				};
			}
			signStyle.normal.textColor = Color.black;
			foreach (MarketShopSnapshot shop in shops)
			{
				Texture2D val = Signboard(shop.permitId);
				if ((Object)(object)val != (Object)null)
				{
					Vector3 val2 = main.WorldToScreenPoint(new Vector3((float)shop.x / 40f, (float)(-shop.y) / 40f, 0f));
					if (val2.z >= 0f)
					{
						Vector2 val3 = new Vector2(val2.x / UiKit.Scale, ((float)Screen.height - val2.y) / UiKit.Scale) / num;
						Rect val4 = new Rect(val3.x - (float)(((Texture)val).width / 2), val3.y - (float)((Texture)val).height + 2f, (float)((Texture)val).width, (float)((Texture)val).height);
						GUI.DrawTexture(val4, (Texture)(object)val);
						Vector2 val5 = Art.Origin("wz://Item/Cash/0503.img/" + ((shop.permitId / 10000 == 503) ? shop.permitId : 5030000).ToString("D8") + "/skin/signboard");
						DrawDescription(val4.x + val5.x + 8f, val4.y + val5.y + 8f, shop.description);
						if (!shop.playerStore && (int)Event.current.type == 7 && Pointer.DoubleClicked && !Pointer.Blocked && !GameWindows.AnyVisible && val4.Contains(Pointer.Pos / num))
						{
							NativeMarketWindow.Visit(shop.ownerId);
							Pointer.ConsumeClick();
						}
					}
				}
				if (shop.playerStore)
				{
					continue;
				}
				Vector3 val6 = main.WorldToScreenPoint(new Vector3((float)shop.x / 40f, (float)(-shop.y) / 40f, 0f));
				if (!(val6.z < 0f))
				{
					Vector2 anchor = new Vector2(val6.x / UiKit.Scale, ((float)Screen.height - val6.y) / UiKit.Scale) / num;
					string text = shop.ownerName + "的雇佣商店";
					NpcDialogUi.NamePlateRects(anchor, nameStyle.CalcSize(new GUIContent(text)), out var plate, out var text2);
					Color c = new Color(0f, 0f, 0f, 160f / 255f);
					UiKit.Fill(new Rect(plate.x, plate.y + 1f, plate.width, plate.height - 2f), c);
					UiKit.Fill(new Rect(plate.x + 1f, plate.y, plate.width - 2f, 1f), c);
					UiKit.Fill(new Rect(plate.x + 1f, plate.yMax - 1f, plate.width - 2f, 1f), c);
					GUI.Label(text2, text, nameStyle);
					if ((int)Event.current.type == 7 && Pointer.DoubleClicked && !Pointer.Blocked && !GameWindows.AnyVisible && plate.Contains(Pointer.Pos / num))
					{
						NativeMarketWindow.Visit(shop.ownerId);
						Pointer.ConsumeClick();
					}
				}
			}
		}
		finally
		{
			GUI.matrix = matrix;
		}
	}

	private void DrawDescription(float x, float y, string description)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected Obj, but got Unknown
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrEmpty(description))
		{
			return;
		}
		string text = description.Replace("\r", "").Replace("\n", " ");
		for (int i = 0; i < 2; i++)
		{
			if (text.Length <= 0)
			{
				break;
			}
			int j;
			for (j = 0; j < text.Length && signStyle.CalcSize(new GUIContent(text.Substring(0, j + 1))).x <= 100f; j++)
			{
			}
			if (j != 0)
			{
				GUI.Label(new Rect(x, y + (float)(14 * i), 100f, 14f), text.Substring(0, j), signStyle);
				text = text.Substring(j);
				continue;
			}
			break;
		}
	}

	private void OnDestroy()
	{
		Clear();
	}
}
