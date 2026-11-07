using System;
using System.Collections.Generic;
using MapleDash.Wz;
using Server.Bot;
using Server.Bot.Social;
using Server.Bot.Trade;
using UnityEngine;

public static class NativeTradeWindow
{
	private static BotTradeResponse state;

	private static double nextRead;

	private static int invitation;

	private static Vector2 position = new Vector2(180f, 60f);

	private static Vector2 grab;

	private static bool dragging;

	private static float chatScroll;

	private static int messageCount;

	private static string chat = "";

	private static readonly List<string> messages = new List<string>();

	private static InventoryEntry draggedItem;

	private static BotOutput subscribed;

	public static bool Visible
	{
		get
		{
			if (state != null && state.active)
			{
				return state.accepted;
			}
			return false;
		}
	}

	public static bool CanOffer
	{
		get
		{
			if (Visible)
			{
				return !state.locked;
			}
			return false;
		}
	}

	public static void Reset()
	{
		if (subscribed != null)
		{
			subscribed.Published -= OnOutput;
		}
		subscribed = null;
		state = null;
		invitation = 0;
		nextRead = 0.0;
		chat = "";
		messages.Clear();
		draggedItem = null;
	}

	private static void OnOutput(BotOutput.Message message)
	{
		if (message.kind == BotOutput.Kind.TradeChat && message.targetId == GameSession.CharacterId)
		{
			string text = ((message.actorId == GameSession.CharacterId) ? GameSession.Character.record.name : state?.partnerName);
			messages.Add(text + " : " + message.text);
			if (messages.Count > 200)
			{
				messages.RemoveAt(0);
			}
		}
	}

	private static BotTradeResponse Request(BotTradeOperation operation, int target = 0, long amount = 0L, InventoryEntry item = null, int slot = 0, string text = null)
	{
		if (!GameSession.InGame || GameSession.Net == null)
		{
			return null;
		}
		state = GameSession.Net.BotTrade(new BotTradeRequest
		{
			characterId = GameSession.CharacterId,
			operation = operation,
			botId = target,
			meso = amount,
			inventoryType = (item?.inventoryType ?? 0),
			inventoryPosition = (item?.position ?? 0),
			quantity = (int)Math.Min(2147483647L, amount),
			slot = slot,
			text = text
		});
		if (!state.ok)
		{
			NativeSystemMessage.Drop(5, state.error);
		}
		if (!state.active)
		{
			draggedItem = null;
			messages.Clear();
		}
		return state;
	}

	public static void Invite(int id)
	{
		GameSession.SaveToDb();
		Request(BotTradeOperation.Invite, id, 0L);
	}

	public static void Close()
	{
		Request(BotTradeOperation.Cancel, 0, 0L);
		NativeUtilPrompt.Clear();
	}

	public static void BeginItemDrag(InventoryEntry item)
	{
		if (CanOffer && !NativeUtilPrompt.Visible && item != null && item.position > 0)
		{
			draggedItem = item.Copy();
		}
	}

	private static GUIStyle Style(bool right = false, bool white = false, int size = 12)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected Obj, but got Unknown
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected Obj, but got Unknown
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = size,
			clipping = (TextClipping)1,
			alignment = (TextAnchor)(right ? 2 : 0),
			padding = new RectOffset()
		};
		val.normal.textColor = (white ? Color.white : Color.black);
		return val;
	}

	private static void Avatar(string[] ids, float x, float y)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		if (ids != null)
		{
			Sprite val = WzCharacter.Appearance(string.Join("_", ids), ids, WzCharacter.AppearanceFrame(ids, Time.unscaledTime));
			if (!((Object)(object)val == (Object)null))
			{
				float num = x - val.pivot.x;
				Rect rect = val.rect;
				float num2 = y - rect.height + val.pivot.y;
				rect = val.rect;
				float width = rect.width;
				rect = val.rect;
				GUI.DrawTexture(new Rect(num, num2, width, rect.height), (Texture)(object)val.texture);
			}
		}
	}

	public static void Tick()
	{
		if (!GameSession.InGame)
		{
			if (state != null)
			{
				Reset();
			}
			return;
		}
		if (subscribed == null)
		{
			subscribed = BotTypeManager.Output;
			subscribed.Published += OnOutput;
		}
		if (Time.realtimeSinceStartupAsDouble < nextRead)
		{
			return;
		}
		nextRead = Time.realtimeSinceStartupAsDouble + 0.2;
		Request(BotTradeOperation.Read, 0, 0L);
		if (state.incomingInvitation && invitation != state.partnerId && !NativeUtilPrompt.Visible)
		{
			invitation = state.partnerId;
			int partner = invitation;
			NativeUtilPrompt.Confirm(state.partnerName + " 向你提出交易申请。是否接受？", (bool yes) =>
			{
				Request(yes ? BotTradeOperation.Accept : BotTradeOperation.Cancel, partner, 0L);
				invitation = 0;
			});
		}
		if (!state.incomingInvitation)
		{
			invitation = 0;
		}
	}

	public static void Draw()
	{
		//IL_087f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Invalid comparison between Unknown and I4
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_03af: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0441: Unknown result type (might be due to invalid IL or missing references)
		//IL_044b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_040b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0492: Unknown result type (might be due to invalid IL or missing references)
		//IL_049c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0500: Unknown result type (might be due to invalid IL or missing references)
		//IL_050a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0542: Unknown result type (might be due to invalid IL or missing references)
		//IL_056e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0578: Unknown result type (might be due to invalid IL or missing references)
		//IL_0587: Unknown result type (might be due to invalid IL or missing references)
		//IL_058d: Invalid comparison between Unknown and I4
		//IL_0594: Unknown result type (might be due to invalid IL or missing references)
		//IL_059b: Invalid comparison between Unknown and I4
		//IL_0637: Unknown result type (might be due to invalid IL or missing references)
		//IL_0650: Unknown result type (might be due to invalid IL or missing references)
		//IL_0662: Unknown result type (might be due to invalid IL or missing references)
		//IL_0682: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_074e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0754: Invalid comparison between Unknown and I4
		//IL_0719: Unknown result type (might be due to invalid IL or missing references)
		//IL_0726: Unknown result type (might be due to invalid IL or missing references)
		//IL_073d: Unknown result type (might be due to invalid IL or missing references)
		//IL_07aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_07af: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b3: Unknown result type (might be due to invalid IL or missing references)
		Tick();
		if (!Visible)
		{
			return;
		}
		float s = BottomBar.S;
		Vector2 val = Pointer.Pos / s;
		position = NativeUi.ClampWindowPosition(position, new Vector2(565f, 474f), s);
		NativeUi.RegisterWindow("NativeTradeWindow", new Rect(position.x * s, position.y * s, 565f * s, 474f * s), () => Visible);
		Matrix4x4 matrix = GUI.matrix;
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(s, s, 1f));
		try
		{
			Rect val2 = new Rect(position.x, position.y, 565f, 474f);
			Rect val3;
			if ((int)Event.current.type == 7 && Pointer.Pressed)
			{
				val3 = new Rect(val2.x, val2.y, 565f, 20f);
				if (val3.Contains(val))
				{
					dragging = true;
					grab = val - position;
				}
			}
			if (dragging && Pointer.Down)
			{
				position = NativeUi.ClampWindowPosition(val - grab, new Vector2(565f, 474f), s);
				val2.position = position;
			}
			if (!Pointer.Down)
			{
				dragging = false;
			}
			GUI.DrawTexture(val2, (Texture)(object)Art.Tex("wz://UI/UIWindow.img/TradingRoom/backgrnd"));
			GUI.BeginGroup(val2);
			Vector2 val4 = val - val2.position;
			bool enabled = GUI.enabled;
			GUI.enabled &= !NativeUtilPrompt.Visible;
			Avatar(WzCharacter.CharacterIds(GameSession.Character.record, GameSession.Character.inventory), 227f, 101f);
			Avatar(state.partnerAppearance, 66f, 101f);
			GUIStyle val5 = Style(right: false, white: true);
			val5.alignment = (TextAnchor)1;
			GUI.Label(new Rect(185f, 107f, 88f, 16f), GameSession.Character.record.name, val5);
			GUI.Label(new Rect(20f, 107f, 88f, 16f), state.partnerName, val5);
			DrawItems(state.items, 169, val4);
			DrawItems(state.partnerItems, 7, val4);
			GUIStyle val6 = Style(right: true);
			GUI.Label(new Rect(184f, 243f, 97f, 14f), state.meso.ToString("N0"), val6);
			GUI.Label(new Rect(22f, 243f, 97f, 14f), state.partnerMeso.ToString("N0"), val6);
			GUI.Label(new Rect(184f, 258f, 97f, 14f), (state.meso - BotTradeService.Fee(state.meso)).ToString("N0"), val6);
			GUI.Label(new Rect(22f, 258f, 97f, 14f), (state.partnerMeso - BotTradeService.Fee(state.partnerMeso)).ToString("N0"), val6);
			if (state.locked)
			{
				UiKit.Fill(new Rect(169f, 129f, 113f, 109f), new Color(1f, 1f, 1f, 0.5f));
			}
			if (state.partnerLocked)
			{
				UiKit.Fill(new Rect(6f, 129f, 113f, 109f), new Color(1f, 1f, 1f, 0.5f));
			}
			GUI.enabled &= !state.locked;
			if (NativeUi.Button(new Rect(116f, 49f, 60f, 19f), "UI/UIWindow.img/TradingRoom/BtTrade", val4))
			{
				NativeUtilPrompt.Confirm("确定要进行交易吗？", (bool yes) =>
				{
					if (yes)
					{
						Request(BotTradeOperation.Confirm, 0, 0L);
					}
				});
			}
			if (NativeUi.Button(new Rect(170f, 244f, 14f, 14f), "UI/UIWindow.img/TradingRoom/BtCoin", val4))
			{
				NativeUtilPrompt.NumberLong("要放入多少金币？", 1L, 0L, Profile.gold, (long value) =>
				{
					Request(BotTradeOperation.OfferMeso, 0, value);
				});
			}
			GUI.enabled = enabled && !NativeUtilPrompt.Visible;
			if (NativeUi.Button(new Rect(116f, 71f, 60f, 19f), "UI/UIWindow.img/TradingRoom/BtReset", val4))
			{
				Close();
			}
			GUIStyle val7 = Style();
			GUI.SetNextControlName("nativeTradeChat");
			chat = GUI.TextField(new Rect(296f, 245f, 192f, 15f), chat, 127, val7);
			bool flag = NativeUi.Button(new Rect(520f, 244f, 39f, 17f), "UI/UIWindow.img/Messenger/BtEnter", val4);
			if ((int)Event.current.type == 4 && (int)Event.current.keyCode == 13 && GUI.GetNameOfFocusedControl() == "nativeTradeChat")
			{
				flag = true;
				Event.current.Use();
			}
			if (flag && !string.IsNullOrWhiteSpace(chat))
			{
				Request(BotTradeOperation.Chat, 0, 0L, null, 0, chat);
				chat = "";
			}
			int num = Math.Max(0, messages.Count - 11);
			if (messageCount != messages.Count)
			{
				messageCount = messages.Count;
				chatScroll = num;
			}
			chatScroll = NativeUi.Scroll4(new Rect(542f, 23f, 15f, 192f), new Rect(295f, 25f, 237f, 192f), chatScroll, num, 1f, val4);
			GUI.BeginGroup(new Rect(297f, 27f, 237f, 188f));
			int num2 = (int)chatScroll;
			for (int num3 = num2; num3 < Math.Min(messages.Count, num2 + 11); num3++)
			{
				GUI.Label(new Rect(0f, (float)((num3 - num2) * 16), 237f, 16f), messages[num3], Style());
			}
			GUI.EndGroup();
			if (draggedItem != null)
			{
				Texture2D val8 = NativeItemSource.Icon(draggedItem.itemId);
				if ((Object)(object)val8 != (Object)null)
				{
					GUI.DrawTexture(new Rect(val4.x - 16f, val4.y - 16f, 32f, 32f), (Texture)(object)val8);
				}
				if ((int)Event.current.type == 7 && Pointer.Released)
				{
					InventoryEntry item = draggedItem;
					draggedItem = null;
					for (int num4 = 0; num4 < 9; num4++)
					{
						val3 = new Rect((float)(169 + num4 % 3 * 39), (float)(129 + num4 / 3 * 37), 32f, 32f);
						if (!val3.Contains(val4))
						{
							continue;
						}
						int slot = num4 + 1;
						if (item.quantity == 1 || TradeItemRules.Rechargeable(item.itemId))
						{
							Request(BotTradeOperation.OfferItem, 0, item.quantity, item, slot);
							break;
						}
						NativeUtilPrompt.Number("要放入多少个？", 1, 1, item.quantity, (int value) =>
						{
							Request(BotTradeOperation.OfferItem, 0, value, item, slot);
						});
						break;
					}
				}
			}
			GUI.enabled = enabled;
			GUI.EndGroup();
		}
		finally
		{
			GUI.matrix = matrix;
		}
	}

	private static void DrawItems(List<InventoryEntry> items, int x, Vector2 pointer)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Invalid comparison between Unknown and I4
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		foreach (InventoryEntry item in items)
		{
			int num = item.position - 1;
			if (num >= 0 && num < 9)
			{
				Rect val = new Rect((float)(x + num % 3 * 39), (float)(129 + num / 3 * 37), 32f, 32f);
				ImgNode imgNode = NativeItemSource.IconNode(item.itemId);
				if (imgNode != null)
				{
					Texture2D val2 = WzAssets.Texture(imgNode);
					Vector2 val3 = WzAssets.Origin(imgNode);
					GUI.DrawTexture(new Rect(val.x + 2f - val3.x, val.y + 32f - val3.y, (float)((Texture)val2).width, (float)((Texture)val2).height), (Texture)(object)val2);
				}
				if (item.inventoryType >= 2 && item.inventoryType <= 4)
				{
					GUI.Label(new Rect(val.x, val.y + 20f, 32f, 12f), item.quantity.ToString(), Style(right: true, white: false, 9));
				}
				if ((int)Event.current.type == 7 && val.Contains(pointer))
				{
					ItemTooltip.SetHover(item.itemId, equipped: false, item);
				}
			}
		}
	}

	static NativeTradeWindow()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
	}
}
