using System;
using UnityEngine;

public static class NativeCommunityWindow
{
	public static bool Visible;

	private static GameAction action;

	private static CommunityResponse state;

	private static string name = "";

	private static string text = "";

	private static string rank = "5";

	private static string logo = "0";

	private static string logoColor = "0";

	private static string background = "0";

	private static string backgroundColor = "0";

	private static string[] titles = new string[5];

	private static Vector2 scroll;

	private static string board = "";

	private static int chatChannel;

	private static readonly string[] Privileges = new string[11]
	{
		"传送到同学", "召集同学", "本人掉落1.5倍 / 15分", "本人经验1.5倍 / 15分", "学院团结 经验掉落2倍 / 30分", "本人掉落2倍 / 15分", "本人经验2倍 / 15分", "本人掉落2倍 / 30分", "本人经验2倍 / 30分", "组队掉落2倍 / 30分",
		"组队经验2倍 / 30分"
	};

	public static bool SchoolVisible
	{
		get
		{
			if (Visible)
			{
				return action == GameAction.School;
			}
			return false;
		}
	}

	public static CommunityResponse State => state;

	public static string InputName
	{
		get
		{
			return name;
		}
		set
		{
			name = value ?? "";
		}
	}

	public static string InputText
	{
		get
		{
			return text;
		}
		set
		{
			text = value ?? "";
		}
	}

	public static string InputRank
	{
		get
		{
			return rank;
		}
		set
		{
			rank = value ?? "5";
		}
	}

	public static string[] InputTitles
	{
		get
		{
			return titles;
		}
		set
		{
			titles = value ?? new string[5];
		}
	}

	public static CommunityResponse Request(CommunityOperation operation, int targetId = 0, int value = 0)
	{
		if (!GameSession.InGame)
		{
			return new CommunityResponse
			{
				ok = false,
				error = "请先选择角色"
			};
		}
		GameSession.SaveToDb();
		int.TryParse(logo, out var result);
		int.TryParse(logoColor, out var result2);
		int.TryParse(background, out var result3);
		int.TryParse(backgroundColor, out var result4);
		state = GameSession.Net.Community(new CommunityRequest
		{
			characterId = GameSession.CharacterId,
			operation = operation,
			targetId = targetId,
			value = value,
			name = name,
			text = text,
			titles = titles,
			logo = result,
			logoColor = result2,
			background = result3,
			backgroundColor = result4
		});
		if (state.ok)
		{
			CharRecord record = GameSession.Character.record;
			record.guildid = state.guildId;
			record.familyId = ((state.familyId == 0) ? (-1) : state.familyId);
			record.meso = state.meso;
			Profile.Data.gold = state.meso;
			foreach (CommunityMember item in state.guild)
			{
				if (item.id == GameSession.CharacterId)
				{
					record.guildrank = item.rank;
				}
			}
			board = state.board;
			Profile.Save();
		}
		else
		{
			BottomBar.Toast(state.error);
		}
		return state;
	}

	public static void Toggle(GameAction next, string familyTarget = null)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		if (next == GameAction.Family)
		{
			NativeGuildWindow.Toggle();
			return;
		}
		if (Visible && action == next && familyTarget == null)
		{
			Visible = false;
			return;
		}
		action = next;
		state = Request(CommunityOperation.Read);
		Visible = true;
		scroll = Vector2.zero;
		text = ((next == GameAction.School) ? state.precepts : state.board);
		if (next == GameAction.School)
		{
			NativeFamilyWindow.Open(familyTarget);
		}
		titles = (string[])state.titles.Clone();
	}

	public static void Reset()
	{
		Visible = false;
		state = null;
		board = "";
		NativeGuildWindow.Reset();
		NativeGuildGradeWindow.Reset();
	}

	private static int Int(string value)
	{
		int.TryParse(value, out var result);
		return result;
	}

	public static void Draw()
	{
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible || state == null)
		{
			return;
		}
		if (action == GameAction.School)
		{
			NativeFamilyWindow.Draw(state);
			return;
		}
		float num = Mathf.Min(570f, UiKit.W - 24f);
		float num2 = Mathf.Min(490f, UiKit.H - BottomBar.Height - 24f);
		Rect val = new Rect((UiKit.W - num) / 2f, 12f, num, num2);
		NativeUi.RegisterWindow("NativeCommunityWindow", val, () => Visible);
		GUI.Box(val, "");
		GUILayout.BeginArea(new Rect(val.x + 12f, val.y + 10f, val.width - 24f, val.height - 20f));
		Font font = GUI.skin.font;
		GUI.skin.font = UiKit.Font;
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		GUILayout.Label(NativeKeyActions.Of(action).name, Array.Empty<GUILayoutOption>());
		if (GUILayout.Button("刷新", new GUILayoutOption[1] { GUILayout.Width(60f) }))
		{
			Request(CommunityOperation.Read);
		}
		if (GUILayout.Button("关闭", new GUILayoutOption[1] { GUILayout.Width(60f) }))
		{
			Visible = false;
		}
		GUILayout.EndHorizontal();
		scroll = GUILayout.BeginScrollView(scroll, Array.Empty<GUILayoutOption>());
		if (action == GameAction.School)
		{
			Family();
		}
		else
		{
			Board();
		}
		GUILayout.EndScrollView();
		GUI.skin.font = font;
		GUILayout.EndArea();
	}

	private static void Family()
	{
		GUILayout.Label((state.familyId > 0) ? ("学院 " + state.familyId) : "尚未加入学院", Array.Empty<GUILayoutOption>());
		if (state.summonFrom > 0)
		{
			GUILayout.Label("收到同学召集请求", Array.Empty<GUILayoutOption>());
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			if (GUILayout.Button("同意召集", Array.Empty<GUILayoutOption>()))
			{
				CommunityResponse communityResponse = Request(CommunityOperation.AnswerSummon, 0, 1);
				if (communityResponse.ok && communityResponse.map != Profile.LastMapId)
				{
					Visible = false;
					GameLoop.I?.field?.WarpTo(communityResponse.map);
				}
			}
			if (GUILayout.Button("拒绝召集", Array.Empty<GUILayoutOption>()))
			{
				Request(CommunityOperation.AnswerSummon);
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.Label("声望 " + state.reputation + "   今日 " + state.todayRep + "   累计 " + state.totalRep, Array.Empty<GUILayoutOption>());
		GUILayout.Label("家训：" + state.precepts, Array.Empty<GUILayoutOption>());
		name = GUILayout.TextField(name, 13, Array.Empty<GUILayoutOption>());
		if (GUILayout.Button("登记直属同学（需对方在线且在同地图）", Array.Empty<GUILayoutOption>()))
		{
			Request(CommunityOperation.InviteFamily);
		}
		if (state.familyInvite > 0 && GUILayout.Button("接受学长登记邀请", Array.Empty<GUILayoutOption>()))
		{
			Request(CommunityOperation.AcceptFamily);
		}
		CommunityMember[] array = state.family.ToArray();
		foreach (CommunityMember communityMember in array)
		{
			string text = "学院首领";
			foreach (CommunityMember item in state.family)
			{
				if (item.id == communityMember.senior)
				{
					text = "学长 " + item.name;
				}
			}
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			GUILayout.Label(communityMember.name + " Lv." + communityMember.level + " · " + text + (communityMember.online ? " 在线" : " 离线"), Array.Empty<GUILayoutOption>());
			if (communityMember.senior == GameSession.CharacterId && GUILayout.Button("解除", new GUILayoutOption[1] { GUILayout.Width(50f) }))
			{
				Request(CommunityOperation.SeparateFamily, communityMember.id);
			}
			GUILayout.EndHorizontal();
		}
		if (state.familyId <= 0)
		{
			return;
		}
		if (GUILayout.Button("解除与学长的关系（按等级差收费）", Array.Empty<GUILayoutOption>()))
		{
			Request(CommunityOperation.SeparateFamily);
		}
		NativeCommunityWindow.text = GUILayout.TextField(NativeCommunityWindow.text, 200, Array.Empty<GUILayoutOption>());
		if (GUILayout.Button("保存家训（学院首领）", Array.Empty<GUILayoutOption>()))
		{
			Request(CommunityOperation.FamilyPrecepts);
		}
		GUILayout.Label("特权：召集/传送目标使用上方角色名", Array.Empty<GUILayoutOption>());
		for (int j = 0; j < Privileges.Length; j++)
		{
			int value = j;
			bool enabled = GUI.enabled;
			GUI.enabled = !state.used.Contains(j);
			if (GUILayout.Button(Privileges[j] + "  声望 " + NativeCommunityService.FamilyCosts[j], Array.Empty<GUILayoutOption>()))
			{
				CommunityResponse communityResponse2 = Request(CommunityOperation.UseFamily, 0, value);
				if (communityResponse2.ok && communityResponse2.map != Profile.LastMapId)
				{
					Visible = false;
					GameLoop.I?.field?.WarpTo(communityResponse2.map);
				}
			}
			GUI.enabled = enabled;
		}
	}

	private static void Board()
	{
		GUILayout.Label("聊天记录", Array.Empty<GUILayoutOption>());
		foreach (BottomBar.ChatLine item in BottomBar.ChatHistory)
		{
			GUILayout.Label(item.text, Array.Empty<GUILayoutOption>());
		}
		chatChannel = GUILayout.Toolbar(chatChannel, new string[8] { "所有人", "悄悄话", "组队", "好友", "家族", "联盟", "夫妻", "讯息" }, Array.Empty<GUILayoutOption>());
		name = GUILayout.TextField(name, 13, Array.Empty<GUILayoutOption>());
		GUILayout.Label("消息内容 / 悄悄话对象填写在上方", Array.Empty<GUILayoutOption>());
		text = GUILayout.TextField(text, 127, Array.Empty<GUILayoutOption>());
		if (GUILayout.Button("发送消息", Array.Empty<GUILayoutOption>()) && BottomBar.SubmitChat(text, (NativeChatChannel)chatChannel, name))
		{
			text = "";
		}
		GUILayout.Space(12f);
		GUILayout.Label("现金黑板（需要持有537类现金道具，使用不消耗）", Array.Empty<GUILayoutOption>());
		text = GUILayout.TextArea(text, 255, new GUILayoutOption[1] { GUILayout.Height(100f) });
		if (GUILayout.Button("显示黑板", Array.Empty<GUILayoutOption>()))
		{
			Request(CommunityOperation.SetBoard);
		}
		if (GUILayout.Button("收起黑板", Array.Empty<GUILayoutOption>()))
		{
			Request(CommunityOperation.CloseBoard);
		}
	}

	public static void DrawBoard()
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Expected Obj, but got Unknown
		if (GameSession.InGame && !string.IsNullOrEmpty(board))
		{
			PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
			Camera main = Camera.main;
			if (!((Object)(object)playerCtrl == (Object)null) && !((Object)(object)main == (Object)null))
			{
				Vector3 val = main.WorldToScreenPoint(((Component)playerCtrl).transform.position + new Vector3(0f, 2.3f, 0f));
				Rect val2 = new Rect(val.x - 120f, (float)Screen.height - val.y - 48f, 240f, 48f);
				GUIStyle val3 = new GUIStyle(GUI.skin.box)
				{
					font = UiKit.Font,
					wordWrap = true,
					alignment = (TextAnchor)4
				};
				GUI.Box(val2, board, val3);
			}
		}
	}
}
