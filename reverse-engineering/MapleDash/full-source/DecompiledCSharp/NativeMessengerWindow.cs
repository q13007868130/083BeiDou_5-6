using System;
using System.Collections.Generic;
using Server.Bot.Social;
using UnityEngine;

public static class NativeMessengerWindow
{
	public const int LineHeight = 13;

	public const int LogLineWidth = 250;

	public const int SeatPitch = 94;

	public const int SeatFirst = 54;

	public const int SeatAvatarY = 118;

	public const int NameBarX = 9;

	public const int NameBarY = 122;

	public const int NameBarWidth = 89;

	public const int NameBarHeight = 19;

	private static readonly Vector2[] Sizes = new Vector2[3]
	{
		new Vector2(295f, 364f),
		new Vector2(392f, 240f),
		new Vector2(184f, 22f)
	};

	private static int mode;

	private static int knownCount;

	private static Vector2 position = new Vector2(100f, 40f);

	private static Vector2 grab;

	private static bool dragging;

	private static string input = "";

	private static float scroll;

	private static long sequence;

	private static readonly List<string> messages = new List<string>();

	public static Vector2 SizeOf(int mode)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		return Sizes[Mathf.Clamp(mode, 0, 2)];
	}

	public static Rect TitleButton(int index, int mode)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		bool flag = mode == 2;
		float num = (flag ? 166 : 276);
		return new Rect(index switch
		{
			1 => num - 28f, 
			0 => num, 
			_ => num - 14f, 
		}, (float)(flag ? 5 : 6), 12f, 12f);
	}

	public static Rect TitleClose(int mode)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return TitleButton(0, mode);
	}

	public static Rect TitleMin(int mode)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return TitleButton(1, mode);
	}

	public static Rect TitleMax(int mode)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return TitleButton(2, mode);
	}

	public static Rect Scroll(int mode)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(273f, (float)((mode == 0) ? 148 : 55), 15f, (float)((mode == 0) ? 154 : 125));
	}

	public static Rect Edit(int mode)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(9f, (float)((mode == 0) ? 318 : 194), 207f, 15f);
	}

	public static Rect Enter(int mode)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(249f, (float)((mode == 0) ? 317 : 193), 39f, 17f);
	}

	public static int Lines(int mode)
	{
		if (mode != 0)
		{
			return 9;
		}
		return 11;
	}

	public static Vector2 SeatAnchor(int seat)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2((float)(54 + 94 * seat), 118f);
	}

	public static Rect SeatNameBar(int seat)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		return new Rect((float)(9 + 94 * seat), 122f, 89f, 19f);
	}

	public static void Reset()
	{
		mode = 0;
		input = "";
		scroll = 0f;
		sequence = 0L;
		knownCount = 0;
		messages.Clear();
	}

	private static GUIStyle Text(bool center = false)
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
			alignment = (TextAnchor)(center ? 4 : 0)
		};
		val.normal.textColor = Color.black;
		return val;
	}

	private static void Avatar(string[] ids, float x, float y)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		if (ids != null)
		{
			Sprite val = WzCharacter.Appearance("messenger_" + string.Join("_", ids), ids, WzCharacter.AppearanceFrame(ids, Time.unscaledTime));
			if ((Object)(object)val != (Object)null)
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

	public static void Draw(SocialResponse state)
	{
		//IL_0638: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Invalid comparison between Unknown and I4
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0319: Unknown result type (might be due to invalid IL or missing references)
		//IL_0323: Unknown result type (might be due to invalid IL or missing references)
		//IL_044f: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_040c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0403: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0540: Unknown result type (might be due to invalid IL or missing references)
		//IL_056b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0575: Unknown result type (might be due to invalid IL or missing references)
		//IL_0584: Unknown result type (might be due to invalid IL or missing references)
		//IL_058a: Invalid comparison between Unknown and I4
		//IL_0591: Unknown result type (might be due to invalid IL or missing references)
		//IL_0598: Invalid comparison between Unknown and I4
		foreach (BotMessenger.Entry messengerMessage in state.messengerMessages)
		{
			if (messengerMessage.sequence > sequence)
			{
				sequence = messengerMessage.sequence;
				if (!messengerMessage.typing)
				{
					messages.Add(messengerMessage.sender + " : " + messengerMessage.text);
				}
			}
		}
		if (messages.Count > 200)
		{
			messages.RemoveRange(0, messages.Count - 200);
		}
		float s = BottomBar.S;
		Vector2 val = SizeOf(mode);
		position = NativeUi.ClampWindowPosition(position, val, s);
		NativeUi.RegisterWindow("NativeMessengerWindow", new Rect(position.x * s, position.y * s, val.x * s, val.y * s), () => NativeSocialWindow.MessengerVisible);
		Vector2 val2 = Pointer.Pos / s;
		Matrix4x4 matrix = GUI.matrix;
		bool enabled = GUI.enabled;
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(s, s, 1f));
		try
		{
			if ((int)Event.current.type == 7 && Pointer.Pressed)
			{
				Rect val3 = new Rect(position.x, position.y, val.x - 50f, 22f);
				if (val3.Contains(val2))
				{
					dragging = true;
					grab = val2 - position;
				}
			}
			if (dragging && Pointer.Down)
			{
				position = NativeUi.ClampWindowPosition(val2 - grab, val, s);
			}
			if (!Pointer.Down)
			{
				dragging = false;
			}
			GUI.BeginGroup(new Rect(position.x, position.y, val.x, val.y));
			try
			{
				Vector2 pointer = val2 - position;
				string text;
				if (mode != 0)
				{
					text = ((mode == 1) ? "backgrnd2" : "backgrnd3");
				}
				else
				{
					text = "backgrnd";
				}
				Texture2D val4 = Art.Tex("wz://UI/UIWindow.img/Messenger/" + text);
				if ((Object)(object)val4 != (Object)null)
				{
					GUI.DrawTexture(new Rect(0f, 0f, (float)((Texture)val4).width, (float)((Texture)val4).height), (Texture)(object)val4);
				}
				GUI.enabled = enabled && !NativeUtilPrompt.Visible;
				if (NativeUi.Button(TitleClose(mode), "UI/Basic.img/BtClose", pointer))
				{
					NativeSocialWindow.Close();
					return;
				}
				bool enabled2 = GUI.enabled;
				GUI.enabled = enabled2 && mode != 2;
				if (NativeUi.Button(TitleMin(mode), "UI/Basic.img/BtMin", pointer))
				{
					mode = Math.Min(2, mode + 1);
					return;
				}
				GUI.enabled = enabled2 && mode != 0;
				if (NativeUi.Button(TitleMax(mode), "UI/Basic.img/BtMax", pointer))
				{
					mode = Math.Max(0, mode - 1);
					return;
				}
				GUI.enabled = enabled2;
				if (mode == 2)
				{
					return;
				}
				for (int num = 0; num < Math.Min(3, state.messengerMembers.Count); num++)
				{
					SocialMember socialMember = state.messengerMembers[num];
					int num2 = Mathf.Clamp(socialMember.messengerPosition, 0, 2);
					if (mode == 0)
					{
						string[] ids = ((socialMember.id == GameSession.CharacterId) ? WzCharacter.CharacterIds(GameSession.Character.record, GameSession.Character.inventory) : socialMember.appearance);
						Vector2 val5 = SeatAnchor(num2);
						Avatar(ids, val5.x, val5.y);
					}
					Rect val6 = SeatNameBar(num2);
					Texture2D val7 = Art.Tex("wz://UI/UIWindow.img/Messenger/NameBar/" + (num2 + 1));
					if ((Object)(object)val7 != (Object)null)
					{
						GUI.DrawTexture(val6, (Texture)(object)val7);
					}
					GUI.Label(val6, socialMember.name, Text(center: true));
				}
				int num3 = Lines(mode);
				Rect bar = Scroll(mode);
				int num4 = ((mode == 0) ? 155 : 60);
				int num5 = Math.Max(0, messages.Count - num3);
				if (knownCount != messages.Count)
				{
					knownCount = messages.Count;
					scroll = num5;
				}
				scroll = NativeUi.Scroll4(bar, new Rect(9f, (float)num4, 250f, (float)(num3 * 13)), scroll, num5, 1f, pointer);
				for (int num6 = (int)scroll; num6 < Math.Min(messages.Count, (int)scroll + num3); num6++)
				{
					GUI.Label(new Rect(14f, (float)(num4 + (num6 - (int)scroll) * 13), 250f, 13f), messages[num6], Text());
				}
				Rect val8 = Edit(mode);
				GUI.SetNextControlName("nativeMessengerChat");
				input = GUI.TextField(val8, input, 127, Text());
				bool flag = NativeUi.Button(Enter(mode), "UI/UIWindow.img/Messenger/BtEnter", pointer);
				if ((int)Event.current.type == 4 && (int)Event.current.keyCode == 13 && GUI.GetNameOfFocusedControl() == "nativeMessengerChat")
				{
					flag = true;
					Event.current.Use();
				}
				if (flag && !string.IsNullOrWhiteSpace(input))
				{
					SocialResponse socialResponse = NativeSocialWindow.Request(SocialOperation.SendChat, "", input, NativeChatChannel.Messenger);
					if (socialResponse.ok)
					{
						messages.Add(GameSession.Character.record.name + " : " + input);
						input = "";
					}
					else
					{
						NativeSystemMessage.Drop(5, socialResponse.error);
					}
				}
			}
			finally
			{
				GUI.EndGroup();
			}
		}
		finally
		{
			GUI.enabled = enabled;
			GUI.matrix = matrix;
		}
	}

	static NativeMessengerWindow()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
	}
}
