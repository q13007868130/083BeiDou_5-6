using System;
using UnityEngine;

public static class WelcomeTutorial
{
	public const string Title = "#r[北斗冒险岛]#k来自北斗开发者的问候。";

	public const string StateKey = "mapledash.welcomeTutorial.accepted";

	public const string Speaker = "昨日小睡";

	private static readonly string[] pages = new string[5] { "哦，尊贵的冒险者#r#h ##k，欢迎莅临#b《北斗冒险岛》#k这方被神秘与奇迹所笼罩的奇幻之地！吾乃您的引导者，#r小睡#k，一个集智慧与优雅于一身的存在，特此恭候您的到来。", "哈哈，恭喜您，被命运的骰子扔进了这场荒诞不经的大冒险！您准备好了吗？前方是无尽的搞笑、离奇和不可思议！从玛加提亚城的狂欢派对，直接跳入冰封雪域的冰雪奇缘，再一头扎进森林迷宫的魔幻蘑菇圈，最后飘向星空岛屿的银河漂流瓶大会！", "记住，冒险岛不只是冒险，它是一场对常规的无情嘲弄，是对逻辑的彻底颠覆！所以，请放开您的笑点，拥抱这份疯狂，与我们一起，在荒诞与奇幻中，创造属于我们的神经质传奇吧！哈哈，让我们一起，笑对人生，疯癫到底！", "你想重新回到#b青苹果乐园#k吗？", "请跟你面前的昨日酣睡对话吧！祝您游戏开心。" };

	private static string State
	{
		get
		{
			if (!GameSession.InGame)
			{
				return "";
			}
			return GameSession.Net.ScriptValue(new ScriptValueRequest
			{
				characterId = GameSession.CharacterId,
				name = "mapledash.welcomeTutorial.accepted"
			}).value;
		}
	}

	public static bool CanComplete
	{
		get
		{
			if (Profile.LastMapId == 4)
			{
				return State == "1";
			}
			return false;
		}
	}

	public static bool Available
	{
		get
		{
			if (Profile.LastMapId == 10000 && Profile.JobId == 0 && Profile.level == 1 && State != "1")
			{
				return State != "2";
			}
			return false;
		}
	}

	public static void Complete()
	{
		if (!CanComplete)
		{
			return;
		}
		NpcSession.Conversation(2104, null, (NpcSession dialog, int mode) =>
		{
			if (mode == 1 && CanComplete)
			{
				GameSession.Net.ScriptValue(new ScriptValueRequest
				{
					characterId = GameSession.CharacterId,
					name = "mapledash.welcomeTutorial.accepted",
					write = true,
					value = "2"
				});
				Profile.AddGold(10000L);
				NpcField.InvalidateMarkers();
			}
			dialog.Close();
		}).SetDialog(NpcDialogMode.Ok, "看来你已经见过小睡了，欢迎来到北斗，这是给你的启动资金，希望对您的冒险有所帮助\r\n\r\n#fUI/UIWindow.img/QuestIcon/3/0# 金币: 10000");
	}

	public static void Open()
	{
		if (!Available)
		{
			return;
		}
		int page = 0;
		Show(NpcSession.Conversation(9010000, "昨日小睡", (NpcSession dialog, int mode) =>
		{
			if (mode < 0 || page == 4 || (page == 3 && mode == 0))
			{
				dialog.Close();
			}
			else
			{
				if (page == 3)
				{
					if (GameSession.InGame)
					{
						GameSession.Net.ScriptValue(new ScriptValueRequest
						{
							characterId = GameSession.CharacterId,
							name = "mapledash.welcomeTutorial.accepted",
							write = true,
							value = "1"
						});
					}
					FieldDirector fieldDirector = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.field : null);
					if ((Object)(object)fieldDirector != (Object)null)
					{
						fieldDirector.WarpTo(4);
					}
					else
					{
						Profile.LastMapId = 4;
					}
					page = 4;
					NpcField.InvalidateMarkers();
				}
				else
				{
					page = Math.Max(0, page + ((mode == 1) ? 1 : (-1)));
				}
				Show(dialog, page);
			}
		}), page);
	}

	private static void Show(NpcSession session, int page)
	{
		session.SetDialog(page switch
		{
			0 => NpcDialogMode.Next, 
			3 => NpcDialogMode.AcceptDecline, 
			4 => NpcDialogMode.Ok, 
			_ => NpcDialogMode.NextPrev, 
		}, pages[page]);
	}
}
