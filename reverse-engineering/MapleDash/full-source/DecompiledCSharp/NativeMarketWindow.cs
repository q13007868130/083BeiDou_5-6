using System;
using MapleDash.Wz;
using Server.Bot;
using Server.Bot.Trade;
using UnityEngine;

public static class NativeMarketWindow
{
	private static MarketResponse state;

	private static Vector2 position = new Vector2(100f, 20f);

	private static Vector2 grab;

	private static bool dragging;

	private static int selection = -1;

	private static float scroll;

	private static string chatInput = "";

	private const string ChatKey = "MarketChat";

	private static double nextRead;

	public static bool Visible => state?.visiting != null;

	public static void Reset()
	{
		if (NativeEditKeyboard.Owns("MarketChat"))
		{
			NativeEditKeyboard.Close();
		}
		chatInput = "";
		state = null;
		selection = -1;
		scroll = 0f;
		nextRead = 0.0;
		dragging = false;
	}

	private static MarketResponse Request(MarketOperation operation, int owner = 0, int index = 0, int quantity = 0, string text = null)
	{
		if (!GameSession.InGame)
		{
			return null;
		}
		state = GameSession.Net.Market(new MarketRequest
		{
			characterId = GameSession.CharacterId,
			operation = operation,
			ownerId = owner,
			itemIndex = index,
			quantity = quantity,
			text = text
		});
		if (!state.ok)
		{
			NativeSystemMessage.Drop(5, state.error);
		}
		return state;
	}

	public static void Visit(int owner)
	{
		GameSession.SaveToDb();
		if (Request(MarketOperation.Visit, owner)?.visiting != null)
		{
			selection = -1;
			scroll = 0f;
			chatInput = "";
		}
	}

	public static void Close()
	{
		Request(MarketOperation.Leave);
		Reset();
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

	private static void Image(string name, float x, float y)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://UI/UIWindow.img/PersonalShop/" + name);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(x, y, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
		}
	}

	public static void BuySelected()
	{
		MarketShopSnapshot marketShopSnapshot = state?.visiting;
		if (marketShopSnapshot == null || selection < 0 || selection >= marketShopSnapshot.items.Count)
		{
			return;
		}
		int index = selection;
		int owner = marketShopSnapshot.ownerId;
		MarketShopStock marketShopStock = marketShopSnapshot.items[index];
		if (marketShopStock.exists && marketShopStock.bundles > 0)
		{
			NativeUtilPrompt.Number("要购买多少捆？", 1, 1, Math.Min(marketShopStock.bundles, 32767), (int quantity) =>
			{
				Request(MarketOperation.Buy, owner, index, quantity);
			});
		}
	}

	private static void DrawVisitors(MarketShopSnapshot shop)
	{
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		foreach (MarketVisitorSnapshot visitor in shop.visitors)
		{
			if (visitor.slot < 0 || visitor.slot >= 3)
			{
				continue;
			}
			float num = (visitor.slot + 3) * 95;
			FullCharacter fullCharacter = ((visitor.id == GameSession.CharacterId) ? GameSession.Character : visitor.character);
			if (fullCharacter != null)
			{
				string[] array = WzCharacter.CharacterIds(fullCharacter.record, fullCharacter.inventory);
				Sprite val = WzCharacter.Appearance("market_" + string.Join("_", array), array, WzCharacter.AppearanceFrame(array, Time.unscaledTime));
				if ((Object)(object)val != (Object)null)
				{
					float num2 = num - val.pivot.x;
					Rect rect = val.rect;
					float num3 = 127f - rect.height + val.pivot.y;
					rect = val.rect;
					float width = rect.width;
					rect = val.rect;
					GUI.DrawTexture(new Rect(num2, num3, width, rect.height), (Texture)(object)val.texture);
				}
			}
			GUIStyle val2 = Text();
			val2.alignment = (TextAnchor)1;
			val2.normal.textColor = Color.white;
			GUI.Label(new Rect(num - 45f, 135f, 90f, 16f), visitor.name, val2);
		}
	}

	private static void SendChat()
	{
		if (!string.IsNullOrWhiteSpace(chatInput))
		{
			MarketResponse marketResponse = Request(MarketOperation.Chat, 0, 0, 0, chatInput);
			if (marketResponse != null && marketResponse.ok)
			{
				chatInput = "";
			}
		}
	}

	private static void DrawChat(MarketShopSnapshot shop, Vector2 pointer)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Invalid comparison between Unknown and I4
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Invalid comparison between Unknown and I4
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Invalid comparison between Unknown and I4
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Invalid comparison between Unknown and I4
		NativeEditKeyboard.Signal signal = NativeEditKeyboard.Sync("MarketChat", ref chatInput);
		if (chatInput.Length > 127)
		{
			chatInput = chatInput.Substring(0, 127);
		}
		GUI.BeginGroup(new Rect(238f, 159f, 258f, 158f));
		for (int i = 0; i < 11 && i + Math.Max(0, shop.chat.Count - 11) < shop.chat.Count; i++)
		{
			GUI.Label(new Rect(0f, (float)(i * 14), 258f, 14f), shop.chat[i + Math.Max(0, shop.chat.Count - 11)], Text());
		}
		GUI.EndGroup();
		Rect val = new Rect(238f, 323f, 231f, 15f);
		GUI.SetNextControlName("MarketChat");
		chatInput = GUI.TextField(val, chatInput, 127, Text());
		if (GUI.enabled && (int)Event.current.type == 7 && Pointer.Pressed && val.Contains(pointer))
		{
			GUI.FocusControl("MarketChat");
			NativeEditKeyboard.Open("MarketChat", chatInput, (TouchScreenKeyboardType)0, 127);
			Pointer.ConsumeClick();
		}
		bool flag = NativeUi.Button(new Rect(482f, 321f, 34f, 19f), "UI/UIWindow.img/PersonalShop/BtEnter", pointer);
		if (GUI.enabled && (int)Event.current.type == 4 && GUI.GetNameOfFocusedControl() == "MarketChat" && ((int)Event.current.keyCode == 13 || (int)Event.current.keyCode == 271))
		{
			flag = true;
			Event.current.Use();
		}
		if (GUI.enabled && (flag || signal == NativeEditKeyboard.Signal.Done))
		{
			SendChat();
		}
	}

	public static void Draw()
	{
		//IL_0714: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Invalid comparison between Unknown and I4
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02de: Expected Obj, but got Unknown
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_0380: Unknown result type (might be due to invalid IL or missing references)
		//IL_038a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0705: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0514: Unknown result type (might be due to invalid IL or missing references)
		//IL_058a: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_069b: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a1: Invalid comparison between Unknown and I4
		//IL_06bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0665: Unknown result type (might be due to invalid IL or missing references)
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
			nextRead = Time.realtimeSinceStartupAsDouble + 0.25;
			Request(MarketOperation.Read);
		}
		MarketShopSnapshot marketShopSnapshot = state?.visiting;
		if (marketShopSnapshot == null)
		{
			Reset();
			return;
		}
		float s = BottomBar.S;
		position = NativeUi.ClampWindowPosition(position, new Vector2(525f, 553f), s);
		NativeUi.RegisterWindow("NativeMarketWindow", new Rect(position.x * s, position.y * s, 525f * s, 553f * s), () => Visible);
		Vector2 val = Pointer.Pos / s;
		Matrix4x4 matrix = GUI.matrix;
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(s, s, 1f));
		try
		{
			Rect val2;
			if (GUI.enabled && (int)Event.current.type == 7 && Pointer.Pressed)
			{
				val2 = new Rect(position.x, position.y, 500f, 22f);
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
				Vector2 val3 = val - position;
				Image((marketShopSnapshot.permitId / 10000 == 514) ? "backgrnd" : "backgrnd6", 0f, 0f);
				if (!marketShopSnapshot.playerStore)
				{
					ImgNode imgNode = WzAssets.Node("Item/Cash/0503.img/" + marketShopSnapshot.permitId.ToString("D8") + "/employee/stand/0");
					if (imgNode != null)
					{
						Texture2D val4 = WzAssets.Texture(imgNode);
						if ((Object)(object)val4 != (Object)null)
						{
							GUI.DrawTexture(new Rect((float)(53 - ((Texture)val4).width / 2), (float)(127 - ((Texture)val4).height), (float)((Texture)val4).width, (float)((Texture)val4).height), (Texture)(object)val4);
						}
					}
				}
				GUI.Label(new Rect(23f, 9f, 460f, 16f), marketShopSnapshot.description, Text());
				GUI.Label(new Rect(9f, 135f, 96f, 16f), marketShopSnapshot.ownerName, new GUIStyle(Text())
				{
					alignment = (TextAnchor)1
				});
				if (NativeUi.Button(new Rect(144f, 110f, 60f, 19f), "UI/UIWindow.img/PersonalShop/BtExit", val3))
				{
					Close();
					return;
				}
				bool enabled = GUI.enabled;
				GUI.enabled &= selection >= 0 && selection < marketShopSnapshot.items.Count && marketShopSnapshot.items[selection].exists && marketShopSnapshot.items[selection].bundles > 0;
				if (NativeUi.Button(new Rect(144f, 90f, 60f, 19f), "UI/UIWindow.img/PersonalShop/BtBuy", val3))
				{
					BuySelected();
				}
				GUI.enabled = enabled;
				scroll = NativeUi.Scroll4(new Rect(211f, 158f, 15f, 197f), new Rect(7f, 157f, 199f, 200f), scroll, Math.Max(0, marketShopSnapshot.items.Count - 5), 1f, val3);
				for (int num = 0; num < 5 && num + (int)scroll < marketShopSnapshot.items.Count; num++)
				{
					int num2 = num + (int)scroll;
					int num3 = 157 + 40 * num;
					MarketShopStock marketShopStock = marketShopSnapshot.items[num2];
					if (selection == num2)
					{
						Image("select", 44f, num3);
					}
					ImgNode imgNode2 = NativeItemSource.IconNode(marketShopStock.item.itemId);
					if (imgNode2 != null)
					{
						ImgNode imgNode3 = imgNode2.Get("origin");
						Texture2D val5 = WzAssets.Texture(imgNode2);
						GUI.DrawTexture(new Rect((float)(8 - (imgNode3?.X ?? 0)), (float)(num3 + 34 - (imgNode3?.Y ?? 0)), (float)((Texture)val5).width, (float)((Texture)val5).height), (Texture)(object)val5);
					}
					if (!marketShopStock.exists || marketShopStock.bundles == 0)
					{
						UiKit.Fill(new Rect(7f, (float)num3, 199f, 35f), Color32.op_Implicit(new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)128)));
					}
					GUI.Label(new Rect(48f, (float)(num3 + 1), 152f, 16f), ItemCatalog.Name(marketShopStock.item.itemId), Text());
					bool flag = (marketShopStock.item.itemId / 1000000 >= 2 && marketShopStock.item.itemId / 1000000 <= 4) || TradeItemRules.Rechargeable(marketShopStock.item.itemId);
					GUI.Label(new Rect(48f, (float)(num3 + 19), 152f, 16f), flag ? (marketShopStock.item.quantity + "个 / " + marketShopStock.price.ToString("N0") + " 金币") : (marketShopStock.price.ToString("N0") + " 金币"), Text());
					if (flag)
					{
						float num4 = 8f;
						string text = ((long)marketShopStock.item.quantity * (long)marketShopStock.bundles).ToString();
						for (int num5 = 0; num5 < text.Length; num5++)
						{
							Texture2D val6 = Art.Tex("wz://UI/Basic.img/ItemNo/" + text[num5]);
							if (!((Object)(object)val6 == (Object)null))
							{
								GUI.DrawTexture(new Rect(num4, (float)(num3 + 23), (float)((Texture)val6).width, (float)((Texture)val6).height), (Texture)(object)val6);
								num4 += (float)((Texture)val6).width;
							}
						}
					}
					if (GUI.enabled && (int)Event.current.type == 7 && Pointer.Released)
					{
						val2 = new Rect(7f, (float)num3, 199f, 35f);
						if (val2.Contains(val3))
						{
							selection = num2;
							Pointer.ConsumeClick();
						}
					}
				}
				DrawVisitors(marketShopSnapshot);
				DrawChat(marketShopSnapshot, val3);
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

	static NativeMarketWindow()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
	}
}
