using System.Collections.Generic;
using Server.Bot;
using Server.Bot.Social;
using UnityEngine;

public static class BottomBar
{
	public struct ChatLine
	{
		public string text;

		public Color color;
	}

	public const float NativeWidth = 1281f;

	private const int BarH = 71;

	public const int PlateX = 2;

	private const int BigW = 54;

	private const int BigH = 34;

	private const int SmallW = 28;

	private const int SmallH = 20;

	private const int BigTop = 35;

	private const int SmallTop = 63;

	private const int BoxX = 573;

	private const int BoxW = 42;

	private const int BoxH = 19;

	private const int ClaimW = 20;

	private const int ClaimH = 19;

	private const int MemoX = 599;

	private const int MemoY = 519;

	private const int QuickW = 466;

	private const int QuickH = 80;

	private static readonly int[] CellY = new int[2] { 10, 44 };

	private const int CellW = 27;

	private const int CellH = 24;

	private const int IconW = 32;

	private const int IconH = 32;

	private static readonly Dictionary<string, Texture2D> tex = new Dictionary<string, Texture2D>();

	private static bool collapsed;

	private static string toast;

	private static float toastUntil;

	private static bool botBroadcastSink;

	public static bool ChatFocused;

	private static bool focusChat;

	private static bool enterPending;

	private static string whisperTarget = "";

	private static readonly string[] ChatNames = new string[9] { "所有人", "悄悄话", "组队", "好友", "家族", "联盟", "夫妻", "讯息", "宠物" };

	private static TouchScreenKeyboard chatKeyboard;

	private static string chatInput = "";

	public static readonly Color ChatWhite = Color.white;

	public static readonly Color ChatPink = Color32.op_Implicit(new Color32(byte.MaxValue, (byte)208, (byte)208, byte.MaxValue));

	public static readonly Color ChatLightBlue = Color32.op_Implicit(new Color32((byte)170, (byte)215, byte.MaxValue, byte.MaxValue));

	public static readonly Color ChatYellow = Color32.op_Implicit(new Color32(byte.MaxValue, byte.MaxValue, (byte)96, byte.MaxValue));

	private static readonly List<ChatLine> chatLines = new List<ChatLine>();

	private const int ChatLineH = 13;

	private const int ChatPanelFromBottom = 69;

	private const int ChatPanelMinH = 24;

	private const int ChatPanelMaxH = 507;

	private const int ChatPanelDefaultH = 76;

	private static bool chatExpanded;

	private static int panelH = 76;

	private static int chatScroll;

	private static bool draggingHeight;

	private static bool draggingThumb;

	private static bool channelOpen;

	private static readonly GUIStyle chatStyle = new GUIStyle();

	private static bool PointerAvailable
	{
		get
		{
			if (!NpcSession.Blocking && !NativeUtilPrompt.Visible && !NativeUi.WindowAtPointer && !VirtualControlsEditor.Visible)
			{
				return !VirtualControls.BlockingPointer;
			}
			return false;
		}
	}

	public static float S => ScaleFor(Screen.width, Screen.height) / UiKit.Scale;

	public static float Height => 35f * S;

	public static bool InterfaceVisible
	{
		get
		{
			return ShortcutWindow.Visible;
		}
		set
		{
			ShortcutWindow.Visible = value;
		}
	}

	public static string ActiveToast
	{
		get
		{
			if (!(Time.unscaledTime < toastUntil))
			{
				return null;
			}
			return toast;
		}
	}

	public static bool QuickCollapsed
	{
		get
		{
			return collapsed;
		}
		set
		{
			collapsed = value;
		}
	}

	public static NativeChatChannel ChatChannel { get; private set; }

	public static IReadOnlyList<ChatLine> ChatHistory => chatLines;

	public static bool ChatExpanded => chatExpanded;

	public static bool ChannelListOpen => channelOpen;

	public static float ScaleFor(float width, float height)
	{
		return Mathf.Min(width / 1281f, height / 600f);
	}

	private static Texture2D T(string name)
	{
		if (tex.TryGetValue(name, out var value))
		{
			return value;
		}
		value = Art.Tex("ui/bar/" + name + ".bytes");
		tex[name] = value;
		return value;
	}

	public static void Toast(string text)
	{
		toast = text;
		toastUntil = Time.unscaledTime + 1.8f;
	}

	public static Rect BigButtonRect(int index)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		return new Rect((float)(573 + index * 56) * S, UiKit.H - 35f * S, 54f * S, 34f * S);
	}

	public static Rect SmallButtonRect(int index)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		return new Rect((float)(618 + index * 30) * S, UiKit.H - 63f * S, 28f * S, 20f * S);
	}

	public static Rect NotifyBoxRect()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(573f * S, UiKit.H - 63f * S, 42f * S, 19f * S);
	}

	public static Rect ClaimRect()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(573f * S, UiKit.H - 63f * S, 20f * S, 19f * S);
	}

	public static Rect QuickPanelRect()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(815f * S, UiKit.H - 79f * S, 466f * S, 80f * S);
	}

	public static Rect SlotRect(int i)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		if (collapsed || i < 0 || i >= 26)
		{
			return Rect.zero;
		}
		Rect val = QuickPanelRect();
		return new Rect(val.x + (7.5f + (float)(i % 13 * 35)) * S, val.y + (float)(CellY[i / 13] - 4) * S, 32f * S, 32f * S);
	}

	public static Rect CellRect(int i)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		Rect val = QuickPanelRect();
		return new Rect(val.x + (float)(10 + i % 13 * 35) * S, val.y + (float)CellY[i / 13] * S, 27f * S, 24f * S);
	}

	public static void Draw()
	{
		if (!((Object)(object)GameLoop.I == (Object)null) && !((Object)(object)GameLoop.I.player == (Object)null) && GameLoop.I.state == GameLoop.GameState.Playing)
		{
			EnsureBotBroadcastSink();
			DrawPlate();
			DrawChat();
			StatusBar.Draw();
			DrawNotifyBox();
			DrawBigButtons();
			if (!collapsed)
			{
				DrawQuickSlots();
			}
			DrawSmallButtons();
			DrawClaim();
		}
	}

	private static void EnsureBotBroadcastSink()
	{
		if (botBroadcastSink)
		{
			return;
		}
		botBroadcastSink = true;
		BotTypeManager.Output.Published += (BotOutput.Message message) =>
		{
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			if (!string.IsNullOrEmpty(message.text))
			{
				switch (message.kind)
				{
				case BotOutput.Kind.Whisper:
					if (message.targetId == GameSession.CharacterId)
					{
						BotCharacter botCharacter = BotRegistry.Get(message.actorId);
						AddChat("[悄悄话] " + ((botCharacter != null) ? botCharacter.name : "?") + " : " + message.text, ChatPink);
					}
					break;
				case BotOutput.Kind.ItemMegaphone:
					AddChat(message.text, ChatWhite);
					break;
				}
			}
		};
	}

	private static void DrawPlate()
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = T("plate_col");
		if (!((Object)(object)val == (Object)null))
		{
			Texture2D val2 = T("backgrnd");
			if ((Object)(object)val2 != (Object)null)
			{
				GUI.DrawTexture(new Rect(0f, UiKit.H - 71f * S, 817f * S, 71f * S), (Texture)(object)val2);
			}
			if (UiKit.W > 817f * S)
			{
				GUI.DrawTexture(new Rect(817f * S, UiKit.H - 71f * S, UiKit.W - 817f * S, 71f * S), (Texture)(object)val);
			}
			Texture2D val3 = T("backgrnd2_full");
			if ((Object)(object)val3 != (Object)null)
			{
				GUI.DrawTexture(new Rect(2f * S, UiKit.H - 71f * S, 570f * S, 71f * S), (Texture)(object)val3);
			}
		}
	}

	private static void CloseChatKeyboard()
	{
		if (chatKeyboard != null)
		{
			chatKeyboard.active = false;
		}
		chatKeyboard = null;
	}

	public static bool QueueChatEnter()
	{
		GameLoop i = GameLoop.I;
		if ((Object)(object)i == (Object)null || i.state != GameLoop.GameState.Playing || (Object)(object)i.player == (Object)null || NpcSession.Blocking || ShopWindow.Visible || NativeUtilPrompt.Visible || KeyConfigWindow.Visible || VirtualControlsEditor.Visible || GameSideMenu.Blocking)
		{
			return false;
		}
		enterPending = true;
		return true;
	}

	public static void FocusChat()
	{
		ChatFocused = true;
		focusChat = true;
		if (Application.isMobilePlatform && (chatKeyboard == null || !chatKeyboard.active))
		{
			chatKeyboard = TouchScreenKeyboard.Open(chatInput, (TouchScreenKeyboardType)0, false, false, false, false, "", 127);
		}
	}

	public static void FocusChat(NativeChatChannel channel, string target = "")
	{
		ChatChannel = channel;
		whisperTarget = target;
		FocusChat();
	}

	public static void ResetChat()
	{
		CloseChatKeyboard();
		enterPending = false;
		ChatFocused = (focusChat = false);
		chatInput = (whisperTarget = "");
		chatLines.Clear();
		ChatChannel = NativeChatChannel.All;
		channelOpen = false;
		NativeGainNotice.Clear();
		chatExpanded = false;
		panelH = 76;
		chatScroll = 0;
	}

	public static void AddChat(string text, Color color)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (!string.IsNullOrEmpty(text))
		{
			chatLines.Add(new ChatLine
			{
				text = text,
				color = color
			});
			if (chatLines.Count > 200)
			{
				chatLines.RemoveAt(0);
			}
			chatScroll = 0;
		}
	}

	public static bool SubmitChat(string text, NativeChatChannel channel, string target = "")
	{
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		bool flag = !string.IsNullOrEmpty(text) && (text[0] == '@' || (text[0] == '!' && (GameSession.Character?.record?.gm).GetValueOrDefault() > 0));
		if (GameMute.MapMuted && !flag)
		{
			AddChat("当前地图已被 GM 禁言", ChatPink);
			return false;
		}
		if (channel == NativeChatChannel.Pet)
		{
			GameLoop i = GameLoop.I;
			object obj;
			if (i == null)
			{
				obj = null;
			}
			else
			{
				FieldDirector field = i.field;
				obj = ((field != null) ? ((Component)field).GetComponent<NativePetField>() : null);
			}
			NativePetField nativePetField = (NativePetField)obj;
			bool flag2 = (Object)(object)nativePetField != (Object)null && nativePetField.ShowChat(text);
			AddChat("[宠物] " + Profile.playerName + " : " + text, flag2 ? ChatWhite : ChatPink);
			if (!flag2)
			{
				AddChat("没有出场的宠物可以说话", ChatPink);
			}
			return flag2;
		}
		SocialResponse socialResponse = NativeSocialWindow.Request(SocialOperation.SendChat, target, text, channel);
		string text2 = ((channel == NativeChatChannel.All) ? (Profile.playerName + " : " + text) : ("[" + ChatNames[(int)channel] + "] " + Profile.playerName + " : " + text));
		if (socialResponse.command != null)
		{
			if (socialResponse.command.changed)
			{
				GameSession.ReloadAfterCommand();
			}
			foreach (string message in socialResponse.command.messages)
			{
				NativeSystemMessage.Drop(6, message);
			}
		}
		if (socialResponse.ok && socialResponse.command == null)
		{
			AddChat(text2, ChatWhite);
			if (channel == NativeChatChannel.All && (Object)(object)GameLoop.I?.field != (Object)null)
			{
				FieldDirector field2 = GameLoop.I.field;
				(((Component)field2).GetComponent<BotField>() ?? ((Component)field2).gameObject.AddComponent<BotField>()).ShowLocalBalloon(Profile.playerName + ": " + text);
			}
		}
		else if (!socialResponse.ok)
		{
			AddChat(socialResponse.error, ChatPink);
		}
		return socialResponse.ok;
	}

	private static void FinishChat()
	{
		if (!string.IsNullOrWhiteSpace(chatInput))
		{
			SubmitChat(chatInput, ChatChannel, whisperTarget);
		}
		enterPending = false;
		chatInput = "";
		ChatFocused = false;
		focusChat = false;
		GUI.FocusControl((string)null);
		CloseChatKeyboard();
	}

	private static void ReadChatKeyboard(string text, Status status)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Invalid comparison between Unknown and I4
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		chatInput = text ?? "";
		if ((int)status == 1)
		{
			FinishChat();
		}
		else if ((int)status == 2 || ((int)status == 3 && !enterPending))
		{
			enterPending = false;
			ChatFocused = (focusChat = false);
			GUI.FocusControl((string)null);
			CloseChatKeyboard();
		}
	}

	private static void DrawChat()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Invalid comparison between Unknown and I4
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Invalid comparison between Unknown and I4
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Invalid comparison between Unknown and I4
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Invalid comparison between Unknown and I4
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Invalid comparison between Unknown and I4
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Invalid comparison between Unknown and I4
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		Event current = Event.current;
		if (chatKeyboard != null)
		{
			ReadChatKeyboard(chatKeyboard.text, chatKeyboard.status);
		}
		if (enterPending)
		{
			enterPending = false;
			if (!NpcSession.Blocking)
			{
				if (!ChatFocused)
				{
					FocusChat();
				}
				else
				{
					FinishChat();
				}
			}
		}
		if (!NpcSession.Blocking && !NativeUtilPrompt.Visible && (int)current.type == 4 && ((int)current.keyCode == 13 || (int)current.keyCode == 271))
		{
			current.Use();
		}
		float num = UiKit.H - 64f * S;
		Rect strip = new Rect(2f * S, num, 566f * S, 20f * S);
		Rect val = new Rect(2f * S, num, 81f * S, 20f * S);
		bool flag = chatExpanded;
		Rect panel = (Rect)(flag ? new Rect(2f * S, UiKit.H - (float)(69 + panelH) * S, 566f * S, (float)panelH * S) : Rect.zero);
		Texture2D val2 = T("chat_full");
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(new Rect(2f * S, UiKit.H - 43f * S, 566f * S, 5f * S), (Texture)(object)val2);
		}
		if (ChatFocused || channelOpen)
		{
			DrawChatInput(num, val, strip);
		}
		else if (flag)
		{
			DrawTargetButton(val);
			if (PointerAvailable && (int)current.type == 7 && Pointer.Clicked && strip.Contains(Pointer.Pos) && Pointer.Pos.x >= val.xMax && Pointer.Pos.x < 549f * S)
			{
				FocusChat();
			}
		}
		else
		{
			DrawCollapsedChat(strip);
		}
		if (flag)
		{
			DrawChatPanel(panel);
		}
		DrawExpandButton(num);
		if (!flag)
		{
			DrawCollapsedArrows(num);
		}
		if (ChatFocused && (int)current.type == 4 && (int)current.keyCode == 27)
		{
			GUI.FocusControl((string)null);
			ChatFocused = false;
			CloseChatKeyboard();
			current.Use();
		}
		DrawChannelList(val);
	}

	private static void DrawCollapsedChat(Rect strip)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Invalid comparison between Unknown and I4
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		UiKit.Fill(strip, new Color(0f, 0f, 0f, 0.51f));
		if (chatLines.Count > 0)
		{
			ChatLine chatLine = chatLines[chatLines.Count - 1];
			GUIStyle val = Style(11, chatLine.color, (TextAnchor)3);
			GUI.Label(new Rect(strip.x + 4f * S, strip.y, strip.width - 8f * S, strip.height), chatLine.text, val);
		}
		if (PointerAvailable && (int)Event.current.type == 7 && Pointer.Clicked && strip.Contains(Pointer.Pos) && Pointer.Pos.x < 549f * S)
		{
			FocusChat();
		}
	}

	private static void DrawChatPanel(Rect panel)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_042d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ed: Invalid comparison between Unknown and I4
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0404: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_030d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Invalid comparison between Unknown and I4
		//IL_0328: Unknown result type (might be due to invalid IL or missing references)
		//IL_0358: Unknown result type (might be due to invalid IL or missing references)
		//IL_0388: Unknown result type (might be due to invalid IL or missing references)
		UiKit.Fill(panel, new Color(0f, 0f, 0f, 0.55f));
		UiKit.Fill(new Rect(panel.x, panel.y, panel.width, 2f * S), new Color(0f, 0f, 0f, 0.9f));
		int count = chatLines.Count;
		int num = Mathf.Max(1, panelH / 13);
		int num2 = Mathf.Max(0, count - num);
		chatScroll = Mathf.Clamp(chatScroll, 0, num2);
		float num3 = 13f * S;
		int num4 = count - num - chatScroll;
		for (int i = 0; i < num; i++)
		{
			int num5 = num4 + i;
			if (num5 >= 0 && num5 < count)
			{
				ChatLine chatLine = chatLines[num5];
				ShadowLabel(new Rect(panel.x + 4f * S, panel.yMax - (float)(num - i) * num3, panel.width - 24f * S, num3), chatLine.text, Style(11, chatLine.color, (TextAnchor)3));
			}
		}
		float num6 = panel.xMax - 15f * S;
		bool flag = num2 > 0;
		Rect val = new Rect(num6, panel.y, 15f * S, 13f * S);
		Rect val2 = new Rect(num6, panel.yMax - 13f * S, 15f * S, 13f * S);
		Texture2D val3 = T(flag ? "VScr5_base" : "VScr5d_base");
		if ((Object)(object)val3 != (Object)null)
		{
			GUI.DrawTexture(new Rect(num6, val.yMax, 15f * S, val2.y - val.yMax), (Texture)(object)val3, (ScaleMode)0);
		}
		Texture2D val4 = T(flag ? "VScr5_prev0" : "VScr5d_prev");
		Texture2D val5 = T(flag ? "VScr5_next0" : "VScr5d_next");
		if ((Object)(object)val4 != (Object)null)
		{
			GUI.DrawTexture(val, (Texture)(object)val4);
		}
		if ((Object)(object)val5 != (Object)null)
		{
			GUI.DrawTexture(val2, (Texture)(object)val5);
		}
		Event current = Event.current;
		bool flag2 = !NpcSession.Blocking;
		if (flag)
		{
			float num7 = val2.y - val.yMax - 25f * S;
			if (num7 > 0f)
			{
				float num8 = 1f - (float)chatScroll / (float)num2;
				Rect val6 = new Rect(num6, val.yMax + num7 * num8, 15f * S, 25f * S);
				Texture2D val7 = T((draggingThumb || (Pointer.Down && val6.Contains(Pointer.Pos))) ? "VScr5_thumb1" : "VScr5_thumb0");
				if ((Object)(object)val7 != (Object)null)
				{
					GUI.DrawTexture(val6, (Texture)(object)val7);
				}
				if (flag2 && (int)current.type == 7)
				{
					if (PointerAvailable && Pointer.Clicked && val.Contains(Pointer.Pos))
					{
						chatScroll = Mathf.Min(num2, chatScroll + 1);
					}
					else if (PointerAvailable && Pointer.Clicked && val2.Contains(Pointer.Pos))
					{
						chatScroll = Mathf.Max(0, chatScroll - 1);
					}
					else if (PointerAvailable && Pointer.Clicked && val6.Contains(Pointer.Pos))
					{
						draggingThumb = true;
					}
					if (draggingThumb)
					{
						if (!Pointer.Down)
						{
							draggingThumb = false;
						}
						else
						{
							float num9 = Mathf.Clamp01((Pointer.Pos.y - val.yMax) / num7);
							chatScroll = Mathf.RoundToInt((1f - num9) * (float)num2);
						}
					}
				}
			}
		}
		if (flag2 && (int)current.type == 6 && panel.Contains(Pointer.Pos))
		{
			chatScroll = Mathf.Clamp(chatScroll + ((!(current.delta.y > 0f)) ? 1 : (-1)), 0, num2);
			current.Use();
		}
		HandlePanelResize(panel);
	}

	private static void HandlePanelResize(Rect panel)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		if ((int)Event.current.type != 7 || NpcSession.Blocking)
		{
			return;
		}
		Rect val = new Rect(panel.x, panel.y - 4f * S, panel.width, 8f * S);
		bool flag = val.Contains(Pointer.Pos);
		if ((PointerAvailable && Pointer.Clicked) & flag)
		{
			draggingHeight = true;
		}
		if (draggingHeight)
		{
			if (!Pointer.Down)
			{
				draggingHeight = false;
			}
			else
			{
				panelH = Mathf.Clamp(Mathf.RoundToInt((UiKit.H - 69f * S - Pointer.Pos.y) / S), 24, 507);
			}
		}
		if (flag || draggingHeight)
		{
			float num = Mathf.Clamp(Pointer.Pos.x, panel.x + 6f * S, panel.xMax - 6f * S);
			Texture2D val2 = T("BtUP_normal");
			Texture2D val3 = T("BtDown_normal");
			if ((Object)(object)val2 != (Object)null)
			{
				GUI.DrawTexture(new Rect(num - 6f * S, panel.y - 12f * S, 12f * S, 12f * S), (Texture)(object)val2);
			}
			if ((Object)(object)val3 != (Object)null)
			{
				GUI.DrawTexture(new Rect(num - 6f * S, panel.y, 12f * S, 12f * S), (Texture)(object)val3);
			}
		}
	}

	private static void DrawExpandButton(float stripTop)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Invalid comparison between Unknown and I4
		Rect val = new Rect(549f * S, stripTop + 4f * S, 12f * S, 12f * S);
		bool flag = chatExpanded && panelH > 26;
		string text = (flag ? "BtMin" : "BtMax");
		bool flag2 = val.Contains(Pointer.Pos);
		string text2;
		if (Pointer.Down & flag2)
		{
			text2 = "pressed";
		}
		else
		{
			text2 = (flag2 ? "mouseOver" : "normal");
		}
		Texture2D val2 = T(text + "_" + text2) ?? T(text + "_normal");
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(val, (Texture)(object)val2, (ScaleMode)0);
		}
		if ((PointerAvailable && (int)Event.current.type == 7 && Pointer.Released) & flag2)
		{
			if (flag)
			{
				chatExpanded = false;
				return;
			}
			if (chatExpanded)
			{
				panelH = 76;
				return;
			}
			chatExpanded = true;
			panelH = 76;
			chatScroll = 0;
		}
	}

	private static void DrawCollapsedArrows(float stripTop)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = T("VScrD_prev");
		Texture2D val2 = T("VScrD_next");
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(561f * S, stripTop - 2f * S, 12f * S, 12f * S), (Texture)(object)val);
		}
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(new Rect(561f * S, stripTop + 10f * S, 12f * S, 12f * S), (Texture)(object)val2);
		}
	}

	private static void DrawTargetButton(Rect targetRect)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Invalid comparison between Unknown and I4
		bool flag = targetRect.Contains(Pointer.Pos);
		Texture2D val = (channelOpen ? (T("combo_pressed") ?? T("chatTarget_full")) : T("chatTarget_full"));
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(targetRect, (Texture)(object)val, (ScaleMode)0);
		}
		ShadowLabel(targetRect, ChatNames[(int)ChatChannel], Style(11, Color.white, (TextAnchor)4));
		if ((PointerAvailable && (int)Event.current.type == 7 && Pointer.Clicked) & flag)
		{
			channelOpen = !channelOpen;
			if (channelOpen)
			{
				ChatFocused = false;
				GUI.FocusControl((string)null);
				CloseChatKeyboard();
			}
		}
	}

	private static void DrawChatInput(float y, Rect targetRect, Rect strip)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected Obj, but got Unknown
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected Obj, but got Unknown
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Expected Obj, but got Unknown
		//IL_0075: Expected Obj, but got Unknown
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Expected Obj, but got Unknown
		//IL_0116: Expected Obj, but got Unknown
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Invalid comparison between Unknown and I4
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		DrawTargetButton(targetRect);
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = Mathf.Max(8, Mathf.RoundToInt(11f * S)),
			alignment = (TextAnchor)3,
			wordWrap = false,
			clipping = (TextClipping)1,
			padding = new RectOffset(0, 0, 0, 0),
			border = new RectOffset(0, 0, 0, 0),
			margin = new RectOffset(0, 0, 0, 0)
		};
		val.normal.textColor = Color.black;
		val.focused.textColor = Color.black;
		val.hover.textColor = Color.black;
		val.active.textColor = Color.black;
		float num = 86f;
		float num2 = 479f;
		if (ChatFocused && ChatChannel == NativeChatChannel.Whisper)
		{
			GUIStyle val2 = new GUIStyle(GUI.skin.label)
			{
				fontSize = val.fontSize,
				alignment = (TextAnchor)3,
				wordWrap = false,
				clipping = (TextClipping)1,
				padding = new RectOffset(0, 0, 0, 0)
			};
			val2.normal.textColor = Color.black;
			GUI.Label(new Rect(num * S, y, 18f * S, 20f * S), "To.", val2);
			GUI.SetNextControlName("NativeWhisper");
			whisperTarget = GUI.TextField(new Rect((num + 20f) * S, y, 118f * S, 20f * S), whisperTarget, 13, val);
			GUI.Label(new Rect((num + 142f) * S, y, 12f * S, 20f * S), ":", val2);
			num += 158f;
			num2 -= 158f;
		}
		GUI.SetNextControlName("NativeChat");
		if (ChatFocused)
		{
			Rect val3 = new Rect(num * S, y, num2 * S, 20f * S);
			if (chatKeyboard != null)
			{
				GUI.Label(val3, chatInput, val);
			}
			else
			{
				chatInput = GUI.TextField(val3, chatInput, 127, val);
			}
		}
		if (focusChat && (int)Event.current.type == 7)
		{
			GUI.FocusControl("NativeChat");
			focusChat = false;
		}
	}

	private static void DrawChannelList(Rect anchor)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Invalid comparison between Unknown and I4
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Invalid comparison between Unknown and I4
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Invalid comparison between Unknown and I4
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Invalid comparison between Unknown and I4
		if (!channelOpen)
		{
			return;
		}
		int num = ChatNames.Length;
		float num2 = 20f * S;
		float num3 = anchor.y - num2 * (float)num;
		Rect val = new Rect(anchor.x, num3, anchor.width, num2 * (float)num);
		for (int i = 0; i < num; i++)
		{
			Rect val2 = new Rect(anchor.x, num3 + (float)i * num2, anchor.width, num2);
			bool flag = val2.Contains(Pointer.Pos);
			string text;
			if (i == (int)ChatChannel)
			{
				text = "pressed";
			}
			else
			{
				text = (flag ? "mouseOver" : "normal");
			}
			Texture2D val3 = T("combo_" + text) ?? T("combo_normal");
			if ((Object)(object)val3 != (Object)null)
			{
				GUI.DrawTexture(val2, (Texture)(object)val3, (ScaleMode)0);
			}
			ShadowLabel(val2, ChatNames[i], Style(11, Color.white, (TextAnchor)4));
			if ((PointerAvailable && (int)Event.current.type == 7 && Pointer.Clicked) & flag)
			{
				ChatChannel = (NativeChatChannel)i;
				channelOpen = false;
				Pointer.ConsumeClick();
				return;
			}
		}
		Event current = Event.current;
		if (PointerAvailable && (int)current.type == 7 && Pointer.Clicked && !val.Contains(Pointer.Pos) && !anchor.Contains(Pointer.Pos))
		{
			channelOpen = false;
		}
		else if ((int)current.type == 4 && (int)current.keyCode == 27)
		{
			channelOpen = false;
			current.Use();
		}
	}

	private static GUIStyle Style(int size, Color color, TextAnchor anchor)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		chatStyle.font = UiKit.Font;
		chatStyle.fontSize = Mathf.Max(8, Mathf.RoundToInt((float)size * S));
		chatStyle.alignment = anchor;
		chatStyle.normal.textColor = color;
		chatStyle.wordWrap = false;
		chatStyle.clipping = (TextClipping)1;
		return chatStyle;
	}

	private static void ShadowLabel(Rect r, string value, GUIStyle style)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		Color textColor = style.normal.textColor;
		style.normal.textColor = new Color(0f, 0f, 0f, 0.9f);
		float num = Mathf.Max(0.5f, S * 0.5f);
		GUI.Label(new Rect(r.x - num, r.y, r.width, r.height), value, style);
		GUI.Label(new Rect(r.x + num, r.y, r.width, r.height), value, style);
		GUI.Label(new Rect(r.x, r.y - num, r.width, r.height), value, style);
		GUI.Label(new Rect(r.x, r.y + num, r.width, r.height), value, style);
		style.normal.textColor = textColor;
		GUI.Label(r, value, style);
	}

	private static void DrawBigButtons()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Invalid comparison between Unknown and I4
		string[] array = new string[4] { "BtShop", "BtNPT", "BtMenu", "BtShort" };
		for (int i = 0; i < array.Length; i++)
		{
			Rect val = BigButtonRect(i);
			bool flag = val.Contains(Pointer.Pos);
			string text;
			if (Pointer.Down & flag)
			{
				text = "pressed";
			}
			else
			{
				text = (flag ? "mouseOver" : "normal");
			}
			Texture2D val2 = T(array[i] + "_" + text) ?? T(array[i] + "_normal");
			if ((Object)(object)val2 != (Object)null)
			{
				GUI.DrawTexture(val, (Texture)(object)val2, (ScaleMode)0);
			}
			if ((PointerAvailable && (int)Event.current.type == 7 && Pointer.Released) & flag)
			{
				PressBig(i);
			}
		}
	}

	private static void PressBig(int i)
	{
		switch (i)
		{
		case 0:
			ShopWindow.Toggle();
			break;
		case 1:
			HelpWindow.Toggle();
			break;
		case 2:
			GameMenuWindow.Toggle();
			break;
		case 3:
			ShortcutWindow.Toggle();
			break;
		}
	}

	private static void DrawNotifyBox()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = T("box_full");
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(NotifyBoxRect(), (Texture)(object)val, (ScaleMode)0);
		}
		Texture2D val2 = Art.Tex("ui/statusbar/iconMemo.bytes");
		if (!((Object)(object)val2 == (Object)null))
		{
			GUI.DrawTexture(new Rect(599f * S, UiKit.H - 59f * S, (float)((Texture)val2).width * S, (float)((Texture)val2).height * S), (Texture)(object)val2, (ScaleMode)0);
		}
	}

	private static void DrawClaim()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Invalid comparison between Unknown and I4
		Rect val = ClaimRect();
		bool flag = val.Contains(Pointer.Pos);
		string text;
		if (Pointer.Down & flag)
		{
			text = "pressed";
		}
		else
		{
			text = (flag ? "mouseOver" : "normal");
		}
		Texture2D val2 = T("BtClaim_" + text) ?? T("BtClaim_normal");
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(val, (Texture)(object)val2, (ScaleMode)0);
		}
		if ((PointerAvailable && (int)Event.current.type == 7 && Pointer.Released) & flag)
		{
			AudioManager.Play("UI/BtMouseClick", 0.7f, pitchVary: false);
			Toast("暂无可领取的道具");
		}
	}

	private static void DrawSmallButtons()
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Invalid comparison between Unknown and I4
		string[] array = new string[6]
		{
			"EquipKey",
			"InvenKey",
			"StatKey",
			"SkillKey",
			"KeySet",
			collapsed ? "QuickSlot" : "QuickSlotD"
		};
		for (int i = 0; i < array.Length; i++)
		{
			Rect val = SmallButtonRect(i);
			bool flag = val.Contains(Pointer.Pos);
			string text;
			if (Pointer.Down & flag)
			{
				text = "pressed";
			}
			else
			{
				text = (flag ? "mouseOver" : "normal");
			}
			Texture2D val2 = T(array[i] + "_" + text) ?? T(array[i] + "_normal");
			if ((Object)(object)val2 != (Object)null)
			{
				GUI.DrawTexture(val, (Texture)(object)val2, (ScaleMode)0);
			}
			if ((PointerAvailable && (int)Event.current.type == 7 && Pointer.Released) & flag)
			{
				PressSmall(i);
			}
		}
	}

	private static void PressSmall(int i)
	{
		switch (i)
		{
		case 0:
			EquipWindow.Toggle();
			break;
		case 1:
			InvenWindow.Toggle();
			break;
		case 2:
			StatWindow.Toggle();
			break;
		case 3:
			SkillWindow.Toggle();
			break;
		case 4:
			KeyConfigWindow.Toggle();
			break;
		case 5:
			collapsed = !collapsed;
			break;
		}
	}

	private static void DrawQuickSlots()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Invalid comparison between Unknown and I4
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Invalid comparison between Unknown and I4
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Invalid comparison between Unknown and I4
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e3: Unknown result type (might be due to invalid IL or missing references)
		Rect val = QuickPanelRect();
		Texture2D val2 = T("quickSlot26");
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(val, (Texture)(object)val2, (ScaleMode)0);
		}
		for (int i = 0; i < 26; i++)
		{
			Rect val3 = SlotRect(i);
			KeyCode val4 = QuickBarKeys.Of(i);
			int num = QuickBarKeys.SkillSlot(val4);
			int num2 = ((num >= 0) ? QuickSlots.Id(num) : 0);
			if (QuickSlots.IsItem(num2))
			{
				Texture2D val5 = QuickSlots.BindingIcon(num2);
				if ((Object)(object)val5 != (Object)null)
				{
					GUI.DrawTexture(val3, (Texture)(object)val5, (ScaleMode)2);
				}
			}
			else if (NativeSkillMacros.IsMacro(num2))
			{
				Texture2D val6 = QuickSlots.BindingIcon(num2);
				if ((Object)(object)val6 != (Object)null)
				{
					GUI.DrawTexture(val3, (Texture)(object)val6, (ScaleMode)2);
				}
			}
			SkillData skillData = ((num2 > 0) ? SkillBook.Get(num2) : null);
			if (skillData != null)
			{
				Texture2D val7 = (string.IsNullOrEmpty(skillData.icon) ? null : Art.Tex(skillData.icon));
				if ((Object)(object)val7 != (Object)null)
				{
					GUI.DrawTexture(val3, (Texture)(object)val7, (ScaleMode)2);
				}
				int num3 = SkillProgress.LevelOf(num2);
				SkillSystem skillSystem = (((Object)(object)GameLoop.I.player != (Object)null) ? ((Component)GameLoop.I.player).GetComponent<SkillSystem>() : null);
				float num4 = (((Object)(object)skillSystem != (Object)null) ? skillSystem.RemainingById(num2) : 0f);
				if (num4 > 0f)
				{
					float num5 = Mathf.Clamp01(num4 / Mathf.Max(0.01f, skillSystem.CooldownOf(num2)));
					Texture2D val8 = Art.Tex("ui/skillwin/cool_" + Mathf.Clamp(Mathf.FloorToInt(num5 * 16f), 0, 15) + ".png");
					if ((Object)(object)val8 != (Object)null)
					{
						GUI.DrawTexture(val3, (Texture)(object)val8);
					}
				}
				if (num3 > 0)
				{
					DrawSlotLevel(num3, val3);
				}
			}
			Texture2D val9 = NativeKeyLayout.NameTexture(val4);
			if ((Object)(object)val9 != (Object)null)
			{
				Rect val10 = CellRect(i);
				GUI.DrawTexture(new Rect(val10.x, val10.y, (float)((Texture)val9).width * S, (float)((Texture)val9).height * S), (Texture)(object)val9);
			}
			if (PointerAvailable && (int)Event.current.type == 7 && Pointer.RightPressed && val3.Contains(Pointer.Pos) && num >= 0)
			{
				QuickSlots.Clear(num);
			}
			if (SkillWindow.Dragging || NativeInventoryActions.Carrying)
			{
				if ((int)Event.current.type == 7 && Pointer.RightPressed && val3.Contains(Pointer.Pos))
				{
					SkillWindow.CancelSkillDrag();
				}
			}
			else if (PointerAvailable && (int)Event.current.type == 7 && Pointer.Clicked && val3.Contains(Pointer.Pos))
			{
				AudioManager.Play("UI/BtMouseClick", 0.7f, pitchVary: false);
				if (num2 != 0)
				{
					QuickSlots.Cast(num);
				}
				else if ((int)val4 != 0 && val4 == KeyMap.Of(GameAction.Attack))
				{
					GameLoop.I.player.RequestAttack();
				}
				else if ((int)val4 != 0)
				{
					InputHub.SetVirtual(val4, down: true);
					InputHub.SetVirtual(val4, down: false);
				}
			}
		}
	}

	private static void DrawSlotLevel(int level, Rect cell)
	{
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		string text = level.ToString();
		float num = cell.xMax - ((float)text.Length * 6f - 1f) * S;
		float num2 = cell.yMax - 8f * S;
		for (int i = 0; i < text.Length; i++)
		{
			Texture2D val = Art.Tex("ui/statusbar/num_" + text[i] + ".bytes");
			if (!((Object)(object)val == (Object)null))
			{
				GUI.DrawTexture(new Rect(num + (float)i * 6f * S, num2, (float)((Texture)val).width * S, (float)((Texture)val).height * S), (Texture)(object)val);
			}
		}
	}

	static BottomBar()
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Expected Obj, but got Unknown
	}
}
