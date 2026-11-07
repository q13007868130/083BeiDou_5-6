using UnityEngine;

public static class NativeKeyActions
{
	public sealed class Entry
	{
		public readonly GameAction action;

		public readonly int id;

		public readonly string name;

		public int Type
		{
			get
			{
				if (id < 100)
				{
					if (id < 50)
					{
						return 4;
					}
					return 5;
				}
				return 6;
			}
		}

		public Entry(GameAction action, int id, string name)
		{
			this.action = action;
			this.id = id;
			this.name = name;
		}
	}

	public static readonly Entry[] Entries = new Entry[40]
	{
		new Entry(GameAction.Equip, 0, "装备窗"),
		new Entry(GameAction.Inven, 1, "道具"),
		new Entry(GameAction.Stat, 2, "能力值"),
		new Entry(GameAction.Skill, 3, "技能"),
		new Entry(GameAction.Buddy, 4, "好友"),
		new Entry(GameAction.WorldMap, 5, "大地图"),
		new Entry(GameAction.Chat, 6, "聊天"),
		new Entry(GameAction.Minimap, 7, "小地图"),
		new Entry(GameAction.Quest, 8, "任务"),
		new Entry(GameAction.KeySet, 9, "键盘设定"),
		new Entry(GameAction.ChatAll, 10, "对所有人"),
		new Entry(GameAction.ChatWhisper, 11, "悄悄话"),
		new Entry(GameAction.ChatParty, 12, "对组员"),
		new Entry(GameAction.ChatBuddy, 13, "对好友"),
		new Entry(GameAction.Interface, 14, "界面"),
		new Entry(GameAction.Shortcut, 15, "快捷键"),
		new Entry(GameAction.ChatRoom, 16, "对话板"),
		new Entry(GameAction.Family, 17, "家族"),
		new Entry(GameAction.ChatFamily, 18, "对家族"),
		new Entry(GameAction.Party, 19, "组队"),
		new Entry(GameAction.Messenger, 20, "讯息"),
		new Entry(GameAction.ChatSpouse, 21, "对夫妻说"),
		new Entry(GameAction.MonsterBook, 22, "怪物手册"),
		new Entry(GameAction.Shop, 23, "商城"),
		new Entry(GameAction.ChatAlliance, 24, "对联盟说"),
		new Entry(GameAction.PartySearch, 25, "寻找组队"),
		new Entry(GameAction.School, 26, "学院"),
		new Entry(GameAction.Medal, 27, "勋章"),
		new Entry(GameAction.Pickup, 50, "捡取"),
		new Entry(GameAction.Sit, 51, "坐下"),
		new Entry(GameAction.Attack, 52, "攻击"),
		new Entry(GameAction.Jump, 53, "跳跃"),
		new Entry(GameAction.NpcTalk, 54, "NPC 对话"),
		new Entry(GameAction.FaceHit, 100, "受伤"),
		new Entry(GameAction.FaceSmile, 101, "微笑"),
		new Entry(GameAction.FaceTroubled, 102, "苦恼"),
		new Entry(GameAction.FaceCry, 103, "哭泣"),
		new Entry(GameAction.FaceAngry, 104, "生气"),
		new Entry(GameAction.FaceBewildered, 105, "惊讶"),
		new Entry(GameAction.FaceStunned, 106, "发呆")
	};

	public static bool AvailableDuringNpcDialog(GameAction action)
	{
		if (action != GameAction.NpcTalk && action != GameAction.Menu && action != GameAction.Help && action != GameAction.QuickSlot)
		{
			Entry entry = Of(action);
			if (entry == null)
			{
				return false;
			}
			return entry.Type == 4;
		}
		return true;
	}

	public static void Execute(GameAction action)
	{
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		Entry entry = Of(action);
		if (entry != null && entry.Type == 6)
		{
			playerCtrl?.Express(entry.id - 99);
			return;
		}
		switch (action)
		{
		case GameAction.Equip:
			EquipWindow.Toggle();
			break;
		case GameAction.Inven:
			InvenWindow.Toggle();
			break;
		case GameAction.Stat:
			StatWindow.Toggle();
			break;
		case GameAction.Skill:
			SkillWindow.Toggle();
			break;
		case GameAction.KeySet:
			KeyConfigWindow.Toggle();
			break;
		case GameAction.QuickSlot:
			BottomBar.QuickCollapsed = !BottomBar.QuickCollapsed;
			break;
		case GameAction.Menu:
			if (!GameWindows.CloseTop() && !((Object)(object)playerCtrl == (Object)null))
			{
				GameMenuWindow.Toggle();
			}
			break;
		case GameAction.Shop:
			ShopWindow.Toggle();
			break;
		case GameAction.Shortcut:
			ShortcutWindow.Toggle();
			break;
		case GameAction.Help:
			HelpWindow.Toggle();
			break;
		case GameAction.Quest:
			QuestWindow.Toggle();
			break;
		case GameAction.Interface:
			GameHUD.InterfaceHidden = !GameHUD.InterfaceHidden;
			break;
		case GameAction.Sit:
			playerCtrl?.ToggleSit();
			break;
		case GameAction.NpcTalk:
			if (NpcSession.Blocking)
			{
				NpcDialogUi.ContinueWithTalkKey();
				break;
			}
			NpcDialogUi.ConsumeTalkKeyFrame();
			NpcField.TryInteractNearest();
			break;
		case GameAction.Chat:
			BottomBar.FocusChat();
			break;
		case GameAction.ChatAll:
			BottomBar.FocusChat(NativeChatChannel.All);
			break;
		case GameAction.Family:
			NativeGuildWindow.Toggle();
			break;
		case GameAction.ChatRoom:
		case GameAction.School:
			NativeCommunityWindow.Toggle(action);
			break;
		case GameAction.Medal:
			NativeMedalWindow.Toggle();
			break;
		case GameAction.MonsterBook:
			NativeMonsterBook.Toggle();
			break;
		case GameAction.ChatWhisper:
			BottomBar.FocusChat(NativeChatChannel.Whisper);
			break;
		case GameAction.ChatParty:
			BottomBar.FocusChat(NativeChatChannel.Party);
			break;
		case GameAction.ChatBuddy:
			BottomBar.FocusChat(NativeChatChannel.Buddy);
			break;
		case GameAction.ChatFamily:
			BottomBar.FocusChat(NativeChatChannel.Guild);
			break;
		case GameAction.ChatAlliance:
			BottomBar.FocusChat(NativeChatChannel.Alliance);
			break;
		case GameAction.ChatSpouse:
			BottomBar.FocusChat(NativeChatChannel.Spouse);
			break;
		case GameAction.WorldMap:
			NativeMapWindows.ToggleWorld();
			break;
		case GameAction.Minimap:
			NativeMapWindows.ToggleMini();
			break;
		case GameAction.Buddy:
		case GameAction.Party:
		case GameAction.Messenger:
		case GameAction.PartySearch:
			NativeSocialWindow.Toggle(action);
			break;
		default:
			if (entry != null)
			{
				BottomBar.Toast(entry.name + "功能尚未开放");
			}
			break;
		}
	}

	public static Entry Of(GameAction action)
	{
		Entry[] entries = Entries;
		foreach (Entry entry in entries)
		{
			if (entry.action == action)
			{
				return entry;
			}
		}
		return null;
	}

	public static bool Find(int type, int id, out GameAction action)
	{
		Entry[] entries = Entries;
		foreach (Entry entry in entries)
		{
			if (entry.id == id && entry.Type == type)
			{
				action = entry.action;
				return true;
			}
		}
		action = GameAction.Left;
		return false;
	}
}
