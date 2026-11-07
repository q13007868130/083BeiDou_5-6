using System;
using System.Collections.Generic;
using UnityEngine;

public static class NativeSocialWindow
{
	private struct PartyButton
	{
		public int id;

		public string node;

		public float x;

		public float y;
	}

	public struct PartyButtonInfo
	{
		public int id;

		public string node;

		public float x;

		public float y;

		public float w;

		public float h;
	}

	private const int WindowW = 312;

	private const int WindowH = 389;

	private const int ListX = 6;

	private const int ListY = 53;

	private const int ListW = 281;

	private const int ListH = 278;

	private const int GroupHeadH = 27;

	private const int ColumnHeadY = 31;

	private const int RowTop = 49;

	private const int RowH = 18;

	private const int ColNameX = 25;

	private const int ColJobX = 125;

	private const int ColLevelX = 205;

	private const int NameClip = 67;

	private const int GroupLineH = 4;

	private const int GroupGap = 10;

	private const string PartyRoot = "UI/UIWindow.img/UserList/Party/";

	private static GameAction action;

	private static SocialResponse state;

	private static string target = "";

	private static string group = "Default Group";

	private static string minimum = "1";

	private static string maximum = "30";

	private static Vector2 scroll;

	public static Vector2 Position = new Vector2(60f, 40f);

	private static Vector2 grab;

	private static bool dragging;

	private static double nextRefresh;

	private static int partyInvitation;

	private static int messengerInvitation;

	private static int selectedId;

	private static readonly PartyButton[] Buttons = new PartyButton[9]
	{
		new PartyButton
		{
			id = 2200,
			node = "BtCreate",
			x = 13f,
			y = 355f
		},
		new PartyButton
		{
			id = 2201,
			node = "BtInvite",
			x = 72f,
			y = 365f
		},
		new PartyButton
		{
			id = 2202,
			node = "BtKick",
			x = 190f,
			y = 344f
		},
		new PartyButton
		{
			id = 2203,
			node = "BtWithdraw",
			x = 190f,
			y = 364f
		},
		new PartyButton
		{
			id = 2204,
			node = "BtWhisper",
			x = 249f,
			y = 345f
		},
		new PartyButton
		{
			id = 2205,
			node = "BtChat",
			x = 249f,
			y = 365f
		},
		new PartyButton
		{
			id = 2206,
			node = "BtChangeBoss",
			x = 131f,
			y = 365f
		},
		new PartyButton
		{
			id = 2207,
			node = "BtHP",
			x = 131f,
			y = 345f
		},
		new PartyButton
		{
			id = 2208,
			node = "BtSearch",
			x = 72f,
			y = 345f
		}
	};

	public static bool Visible { get; private set; }

	public static bool MessengerVisible
	{
		get
		{
			if (Visible)
			{
				return action == GameAction.Messenger;
			}
			return false;
		}
	}

	public static SocialResponse Request(SocialOperation operation, string name = "", string text = "", NativeChatChannel channel = NativeChatChannel.All, int partyId = 0, int messengerRoom = 0, int memberId = 0)
	{
		if (!GameSession.InGame || GameSession.Net == null)
		{
			return new SocialResponse
			{
				ok = false,
				error = "请先进入角色"
			};
		}
		if (operation == SocialOperation.CreateParty || operation == SocialOperation.SearchParty || (operation == SocialOperation.SendChat && NativeGmCommands.IsCommand(GameSession.Character.record, text)))
		{
			GameSession.SaveToDb();
		}
		int.TryParse(minimum, out var result);
		int.TryParse(maximum, out var result2);
		state = GameSession.Net.Social(new SocialRequest
		{
			characterId = GameSession.CharacterId,
			operation = operation,
			target = name,
			group = group,
			partyId = partyId,
			messengerRoom = messengerRoom,
			text = text,
			channel = channel,
			minLevel = result,
			maxLevel = result2,
			memberId = memberId
		});
		GameSession.Character.record.party = state.partyId;
		return state;
	}

	private static void Run(SocialOperation operation, string name = "")
	{
		SocialResponse socialResponse = Request(operation, name);
		if (!socialResponse.ok)
		{
			NativeSystemMessage.Drop(5, socialResponse.error);
		}
	}

	private static void RunMember(SocialOperation operation)
	{
		if (selectedId != 0)
		{
			SocialResponse socialResponse = Request(operation, "", "", NativeChatChannel.All, 0, 0, selectedId);
			if (!socialResponse.ok)
			{
				NativeSystemMessage.Drop(5, socialResponse.error);
			}
			selectedId = 0;
		}
	}

	public static void Toggle(GameAction next)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (Visible && action == next)
		{
			Close();
			return;
		}
		if (next != GameAction.Messenger)
		{
			NativeGuildWindow.Reset();
		}
		action = next;
		Visible = true;
		scroll = Vector2.zero;
		selectedId = 0;
		state = Request(SocialOperation.Read);
		if (next == GameAction.Messenger)
		{
			Run(SocialOperation.OpenMessenger);
		}
		minimum = Mathf.Max(1, Profile.level - 15).ToString();
		maximum = (Mathf.Max(1, Profile.level - 15) + 30).ToString();
	}

	public static void Reset()
	{
		Visible = false;
		state = null;
		target = "";
		selectedId = 0;
		partyInvitation = (messengerInvitation = 0);
		nextRefresh = 0.0;
		NativeMessengerWindow.Reset();
	}

	public static void Close()
	{
		if (Visible && action == GameAction.Messenger)
		{
			Run(SocialOperation.CloseMessenger);
		}
		Visible = false;
	}

	public static void Tick()
	{
		if (!GameSession.InGame || GameSession.Net == null || Time.realtimeSinceStartupAsDouble < nextRefresh)
		{
			return;
		}
		state = Request(SocialOperation.Read);
		nextRefresh = Time.realtimeSinceStartupAsDouble + 0.25;
		if (NativeUtilPrompt.Visible)
		{
			return;
		}
		if (state.partyInvite != 0 && partyInvitation != state.partyInvite)
		{
			int invite = (partyInvitation = state.partyInvite);
			NativeUtilPrompt.Confirm("收到组队邀请。是否接受？", (bool yes) =>
			{
				Request(yes ? SocialOperation.AcceptParty : SocialOperation.DeclineParty, "", "", NativeChatChannel.All, invite);
				partyInvitation = 0;
			});
		}
		else if (state.messengerInvite != 0 && messengerInvitation != state.messengerInvite)
		{
			int invite2 = (messengerInvitation = state.messengerInvite);
			NativeUtilPrompt.Confirm(state.messengerInviter + " 邀请你加入聊天。是否接受？", (bool yes) =>
			{
				SocialResponse socialResponse = Request(yes ? SocialOperation.AcceptMessenger : SocialOperation.DeclineMessenger, "", "", NativeChatChannel.All, 0, invite2);
				messengerInvitation = 0;
				if (yes && socialResponse.ok)
				{
					action = GameAction.Messenger;
					Visible = true;
					NativeMessengerWindow.Reset();
				}
			});
		}
		if (state.partyInvite == 0)
		{
			partyInvitation = 0;
		}
		if (state.messengerInvite == 0)
		{
			messengerInvitation = 0;
		}
	}

	public static void Draw()
	{
		Tick();
		if (Visible && state != null)
		{
			if (action == GameAction.Messenger)
			{
				NativeMessengerWindow.Draw(state);
				return;
			}
			DrawUserList();
			NativeDlgInput.Draw();
		}
	}

	private static void DrawUserList()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_025b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Invalid comparison between Unknown and I4
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		float s = BottomBar.S;
		Vector2 val = new Vector2(312f, 389f);
		Position = NativeUi.ClampWindowPosition(Position, val, s);
		NativeUi.RegisterWindow("NativeSocialWindow", new Rect(Position.x * s, Position.y * s, val.x * s, val.y * s), () => Visible);
		Vector2 val2 = Pointer.Pos / s;
		Matrix4x4 matrix = GUI.matrix;
		bool enabled = GUI.enabled;
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(s, s, 1f));
		try
		{
			if ((int)Event.current.type == 7 && Pointer.Pressed)
			{
				Rect val3 = new Rect(Position.x, Position.y, val.x - 44f, 22f);
				if (val3.Contains(val2))
				{
					dragging = true;
					grab = val2 - Position;
				}
			}
			if (dragging && Pointer.Down)
			{
				Position = NativeUi.ClampWindowPosition(val2 - grab, val, s);
			}
			if (!Pointer.Down)
			{
				dragging = false;
			}
			GUI.BeginGroup(new Rect(Position.x, Position.y, val.x, val.y));
			try
			{
				Vector2 val4 = val2 - Position;
				string text = ((action == GameAction.Buddy) ? "backgrnd3" : "backgrnd");
				Texture2D val5 = Art.Tex("wz://UI/UIWindow.img/UserList/" + text);
				if ((Object)(object)val5 != (Object)null)
				{
					GUI.DrawTexture(new Rect(0f, 0f, 312f, 389f), (Texture)(object)val5);
				}
				GUI.enabled = enabled && !NativeUtilPrompt.Visible && !NativeDlgInput.Visible;
				if (NativeUi.Button(new Rect(293f, 5f, 12f, 12f), "UI/Basic.img/BtClose", val4))
				{
					Close();
					return;
				}
				DrawTabs(val4);
				if (action == GameAction.Buddy)
				{
					DrawBuddyTab(val4);
				}
				else if (action == GameAction.PartySearch)
				{
					DrawSearchTab(val4);
				}
				else
				{
					DrawPartyTab(val4);
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

	public static void DrawUserTabs(Rect window, float scale, int current, Action<int> select, Nullable pointer = null)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Invalid comparison between Unknown and I4
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		float[] array = new float[5];
		for (int i = 0; i < array.Length; i++)
		{
			Texture2D val = Art.Tex("wz://UI/UIWindow.img/UserList/Tab/" + ((i == current) ? "enabled/" : "disabled/") + i);
			array[i] = (((Object)(object)val == (Object)null) ? 23 : (((Texture)val).width - 4));
		}
		float num = 6f;
		Slice("left" + ((current == 0) ? 1 : 0), num);
		num += 6f;
		for (int j = 0; j < array.Length; j++)
		{
			Slice("fill" + ((j == current) ? 1 : 0), num, array[j]);
			Texture2D val2 = Art.Tex("wz://UI/UIWindow.img/UserList/Tab/" + ((j == current) ? "enabled/" : "disabled/") + j);
			if ((Object)(object)val2 != (Object)null)
			{
				GUI.DrawTexture(new Rect(window.x + (num + (array[j] - (float)((Texture)val2).width) / 2f) * scale, window.y + (23f + (float)(24 - ((Texture)val2).height) / 2f) * scale, (float)((Texture)val2).width * scale, (float)((Texture)val2).height * scale), (Texture)(object)val2);
			}
			Rect val3 = new Rect(window.x + num * scale, window.y + 23f * scale, array[j] * scale, 24f * scale);
			if ((int)Event.current.type == 7 && GUI.enabled && !NativeUtilPrompt.Visible && !NativeDlgInput.Visible && Pointer.Clicked && val3.Contains(pointer ?? Pointer.Pos))
			{
				Pointer.ConsumeClick();
				select(j);
				break;
			}
			num += array[j];
			if (j == array.Length - 1)
			{
				Slice("right" + ((j == current) ? 1 : 0), num);
				continue;
			}
			Slice("middle" + ((j == current) ? 1 : ((j + 1 == current) ? 2 : 0)), num);
			num += 13f;
		}
		void Slice(string name, float at, float width = 0f)
		{
			//IL_006d: Unknown result type (might be due to invalid IL or missing references)
			Texture2D val4 = Art.Tex("wz://UI/Basic.img/Tab/" + name);
			if ((Object)(object)val4 != (Object)null)
			{
				GUI.DrawTexture(new Rect(window.x + at * scale, window.y + 23f * scale, ((width > 0f) ? width : ((float)((Texture)val4).width)) * scale, (float)((Texture)val4).height * scale), (Texture)(object)val4);
			}
		}
	}

	private static void DrawTabs(Vector2 p)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		DrawUserTabs(default, 1f, (action != GameAction.Buddy) ? 1 : 0, SwitchTab, p);
	}

	private static void SwitchTab(int index)
	{
		selectedId = 0;
		string text;
		switch (index)
		{
		case 0:
			action = GameAction.Buddy;
			state = Request(SocialOperation.Read);
			return;
		case 1:
			action = GameAction.Party;
			state = Request(SocialOperation.Read);
			return;
		case 2:
			Close();
			NativeCommunityWindow.Toggle(GameAction.Family);
			return;
		default:
			text = "拒绝列表尚未开放";
			break;
		case 3:
			text = "家族联盟尚未开放";
			break;
		}
		BottomBar.Toast(text);
	}

	private static SocialMember SelectedMember()
	{
		if (selectedId == 0)
		{
			return null;
		}
		foreach (SocialMember partyMember in state.partyMembers)
		{
			if (partyMember.id == selectedId)
			{
				return partyMember;
			}
		}
		return null;
	}

	private static GUIStyle RowStyle(bool selected)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected Obj, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected Obj, but got Unknown
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = 12,
			padding = new RectOffset(),
			clipping = (TextClipping)1,
			alignment = (TextAnchor)0
		};
		val.normal.textColor = (selected ? Color.white : Color.black);
		return val;
	}

	private static void MemberRow(Rect row, SocialMember member, bool leader, bool selected, Vector2 p)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Invalid comparison between Unknown and I4
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://UI/UIWindow.img/UserList/Party/party1");
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(1f, row.y, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
		}
		if (selected)
		{
			UiKit.Fill(new Rect(0f, row.y, 281f, 18f), Color32.op_Implicit(new Color32((byte)36, (byte)71, (byte)104, byte.MaxValue)));
		}
		if (leader)
		{
			Texture2D val2 = Art.Tex("wz://UI/UIWindow.img/UserList/Party/icon1");
			if ((Object)(object)val2 != (Object)null)
			{
				GUI.DrawTexture(new Rect(6f, row.y + 1f, (float)((Texture)val2).width, (float)((Texture)val2).height), (Texture)(object)val2);
			}
		}
		GUIStyle val3 = RowStyle(selected);
		GUI.Label(new Rect(25f, row.y + 2f, 67f, 14f), member.name, val3);
		GUI.Label(new Rect(125f, row.y + 2f, 78f, 14f), JobDefs.NameOf(member.job), val3);
		GUI.Label(new Rect(205f, row.y + 2f, 40f, 14f), member.level.ToString(), val3);
		if ((int)Event.current.type == 7 && Pointer.Clicked && row.Contains(p))
		{
			Pointer.ConsumeClick();
			selectedId = member.id;
		}
	}

	private static float DrawGroup(List<SocialMember> members, string header, bool online, float y, Vector2 p)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://UI/UIWindow.img/UserList/Party/" + header);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(0f, y, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
		}
		y += 31f;
		Texture2D val2 = Art.Tex("wz://UI/UIWindow.img/UserList/Party/party5");
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(new Rect(1f, y, (float)((Texture)val2).width, (float)((Texture)val2).height), (Texture)(object)val2);
		}
		y += 18f;
		if (members.Count == 0)
		{
			y += 18f;
		}
		foreach (SocialMember member in members)
		{
			MemberRow(new Rect(0f, y, 281f, 18f), member, member.id == state.partyLeader, member.id == selectedId, p);
			y += 18f;
		}
		if (online)
		{
			Texture2D val3 = Art.Tex("wz://UI/UIWindow.img/UserList/Party/party2");
			if ((Object)(object)val3 != (Object)null)
			{
				GUI.DrawTexture(new Rect(1f, y, (float)((Texture)val3).width, (float)((Texture)val3).height), (Texture)(object)val3);
			}
			y += 14f;
		}
		else
		{
			Texture2D val4 = Art.Tex("wz://UI/UIWindow.img/UserList/Party/party3");
			if ((Object)(object)val4 != (Object)null)
			{
				GUI.DrawTexture(new Rect(1f, y, (float)((Texture)val4).width, (float)((Texture)val4).height), (Texture)(object)val4);
			}
		}
		return y;
	}

	public static void GroupMembers(SocialResponse state, int selfMap, out List<SocialMember> online, out List<SocialMember> offline)
	{
		online = new List<SocialMember>();
		offline = new List<SocialMember>();
		if (state == null)
		{
			return;
		}
		foreach (SocialMember partyMember in state.partyMembers)
		{
			((partyMember.channel == -2) ? offline : online).Add(partyMember);
		}
		online.Sort((SocialMember a, SocialMember b) =>
		{
			int num = ((a.mapId != selfMap) ? 1 : 0).CompareTo((b.mapId != selfMap) ? 1 : 0);
			return (num == 0) ? string.CompareOrdinal(a.name, b.name) : num;
		});
		offline.Sort((SocialMember a, SocialMember b) => string.CompareOrdinal(a.name, b.name));
	}

	public static float PartyContentHeight(int online, int offline)
	{
		float num = 49 + Mathf.Max(1, online) * 18 + 4 + 10;
		float num2 = 49 + Mathf.Max(1, offline) * 18 + 1;
		return num + num2;
	}

	private static void DrawPartyTab(Vector2 p)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		int selfMap = ((GameSession.Character != null) ? GameSession.Character.record.map : 0);
		GroupMembers(state, selfMap, out var online, out var offline);
		if (SelectedMember() == null)
		{
			selectedId = 0;
		}
		GUI.BeginGroup(new Rect(6f, 53f, 281f, 278f));
		try
		{
			Vector2 p2 = p - new Vector2(6f, 53f);
			float y = DrawGroup(online, "party0", online: true, 0f, p2);
			DrawGroup(offline, "party4", online: false, y, p2);
		}
		finally
		{
			GUI.EndGroup();
		}
		DrawPartyButtons(p);
	}

	public static PartyButtonInfo[] PartyButtons()
	{
		List<PartyButtonInfo> list = new List<PartyButtonInfo>();
		PartyButton[] buttons = Buttons;
		for (int i = 0; i < buttons.Length; i++)
		{
			PartyButton partyButton = buttons[i];
			Texture2D val = Art.Tex("wz://UI/UIWindow.img/UserList/Party/" + partyButton.node + "/normal/0");
			list.Add(new PartyButtonInfo
			{
				id = partyButton.id,
				node = partyButton.node,
				x = partyButton.x,
				y = partyButton.y,
				w = (((Object)(object)val != (Object)null) ? ((Texture)val).width : 47),
				h = (((Object)(object)val != (Object)null) ? ((Texture)val).height : 19)
			});
		}
		return list.ToArray();
	}

	public static Vector4 PartyListBox()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		return new Vector4(6f, 53f, 281f, 278f);
	}

	private static void DrawPartyButtons(Vector2 p)
	{
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		bool party = state.party;
		bool flag = party && state.partyLeader == GameSession.CharacterId;
		bool flag2 = selectedId != 0 && selectedId != GameSession.CharacterId && SelectedMember() != null;
		bool flag3 = party && state.partyMembers.Count < 6;
		bool enabled = GUI.enabled;
		PartyButton[] buttons = Buttons;
		for (int i = 0; i < buttons.Length; i++)
		{
			PartyButton partyButton = buttons[i];
			Texture2D val = Art.Tex("wz://UI/UIWindow.img/UserList/Party/" + partyButton.node + "/normal/0");
			float num = (((Object)(object)val != (Object)null) ? ((Texture)val).width : 47);
			float num2 = (((Object)(object)val != (Object)null) ? ((Texture)val).height : 19);
			GUI.enabled = partyButton.id switch
			{
				2200 => !party, 
				2201 => flag & flag3, 
				2202 => flag & flag2, 
				2203 => party, 
				2204 => flag2, 
				2205 => party, 
				2206 => flag & flag2, 
				2207 => false, 
				2208 => flag, 
				_ => false, 
			};
			if (NativeUi.Button(new Rect(partyButton.x, partyButton.y, num, num2), "UI/UIWindow.img/UserList/Party/" + partyButton.node, p))
			{
				PartyAction(partyButton.id);
			}
		}
		GUI.enabled = enabled;
	}

	private static void PartyAction(int id)
	{
		SocialMember socialMember = SelectedMember();
		switch (id)
		{
		case 2200:
			Run(SocialOperation.CreateParty);
			break;
		case 2201:
			NativeDlgInput.Open("请输入要邀请的角色名。", target, 12, (string name) =>
			{
				target = name;
				Run(SocialOperation.InviteParty, name);
			});
			break;
		case 2202:
			RunMember(SocialOperation.ExpelParty);
			break;
		case 2203:
			Run(SocialOperation.LeaveParty);
			selectedId = 0;
			break;
		case 2204:
			if (socialMember != null)
			{
				BottomBar.FocusChat(NativeChatChannel.Whisper, socialMember.name);
			}
			break;
		case 2205:
			BottomBar.FocusChat(NativeChatChannel.Party);
			break;
		case 2206:
			RunMember(SocialOperation.ChangePartyLeader);
			break;
		case 2208:
			action = GameAction.PartySearch;
			break;
		case 2207:
			break;
		}
	}

	private static void DrawBuddyTab(Vector2 p)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		GUI.BeginGroup(new Rect(6f, 53f, 281f, 278f));
		try
		{
			GUILayout.BeginArea(new Rect(0f, 0f, 281f, 278f));
			Font font = GUI.skin.font;
			GUI.skin.font = UiKit.Font;
			scroll = GUILayout.BeginScrollView(scroll, Array.Empty<GUILayoutOption>());
			DrawBuddies();
			GUILayout.EndScrollView();
			GUI.skin.font = font;
			GUILayout.EndArea();
		}
		finally
		{
			GUI.EndGroup();
		}
	}

	private static void DrawBuddies()
	{
		int num = 0;
		foreach (SocialMember buddy in state.buddies)
		{
			if (!buddy.pending)
			{
				num++;
			}
		}
		GUILayout.Label("好友 " + num + " / " + state.capacity, Array.Empty<GUILayoutOption>());
		target = GUILayout.TextField(target, 13, Array.Empty<GUILayoutOption>());
		group = GUILayout.TextField(group, 16, Array.Empty<GUILayoutOption>());
		if (GUILayout.Button("添加好友 / 修改分组", Array.Empty<GUILayoutOption>()))
		{
			Run(SocialOperation.AddBuddy, target);
		}
		SocialMember[] array = state.buddies.ToArray();
		foreach (SocialMember socialMember in array)
		{
			GUILayout.Label(socialMember.name + (socialMember.pending ? "  申请添加好友" : ("  离线 · " + socialMember.group)), Array.Empty<GUILayoutOption>());
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			if (socialMember.pending && GUILayout.Button("接受", Array.Empty<GUILayoutOption>()))
			{
				Run(SocialOperation.AcceptBuddy, socialMember.name);
			}
			if (GUILayout.Button(socialMember.pending ? "拒绝" : "删除", Array.Empty<GUILayoutOption>()))
			{
				Run(SocialOperation.DeleteBuddy, socialMember.name);
			}
			if (!socialMember.pending && GUILayout.Button("悄悄话", Array.Empty<GUILayoutOption>()))
			{
				BottomBar.FocusChat(NativeChatChannel.Whisper, socialMember.name);
			}
			GUILayout.EndHorizontal();
		}
	}

	private static void DrawSearchTab(Vector2 p)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		GUI.BeginGroup(new Rect(6f, 53f, 281f, 240f));
		try
		{
			GUILayout.BeginArea(new Rect(0f, 0f, 281f, 240f));
			Font font = GUI.skin.font;
			GUI.skin.font = UiKit.Font;
			GUILayout.Label("搜索等级范围（最多相差30级）", Array.Empty<GUILayoutOption>());
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			minimum = GUILayout.TextField(minimum, 3, Array.Empty<GUILayoutOption>());
			maximum = GUILayout.TextField(maximum, 3, Array.Empty<GUILayoutOption>());
			GUILayout.EndHorizontal();
			GUILayout.Label(state.searching ? "正在寻找符合条件的在线队员" : "搜索已停止", Array.Empty<GUILayoutOption>());
			if (GUILayout.Button(state.searching ? "停止搜索" : "开始搜索", Array.Empty<GUILayoutOption>()))
			{
				Run(state.searching ? SocialOperation.StopSearch : SocialOperation.SearchParty);
			}
			GUI.skin.font = font;
			GUILayout.EndArea();
		}
		finally
		{
			GUI.EndGroup();
		}
	}

	static NativeSocialWindow()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
	}
}
