using System;
using System.Collections.Generic;
using UnityEngine;

public static class WeeklyGemstoneQuest
{
	[Serializable]
	private sealed class Data
	{
		public int id;

		public int npc;

		public int level;

		public int prerequisite;

		public int required;

		public int dailyRequired;

		public int rewardItem;

		public int rewardCount;

		public string name;

		public string demand;

		public string reward;

		public string[] info;
	}

	private static QuestDef definition;

	private static WeeklyGemstoneResponse cached;

	private static int owner;

	private static DateTime until;

	public static void Register(Dictionary<int, QuestDef> cache, List<int> ids, Dictionary<int, List<int>> byNpc)
	{
		TextAsset val = Resources.Load<TextAsset>("Quests/weekly-gemstone");
		if ((Object)(object)val == (Object)null)
		{
			throw new InvalidOperationException("Missing 282 weekly gemstone quest data");
		}
		Data data = JsonUtility.FromJson<Data>(val.text);
		if (ids.Contains(data.id))
		{
			throw new InvalidOperationException("282 weekly quest conflicts with WZ: " + data.id);
		}
		QuestDef questDef = new QuestDef
		{
			id = data.id,
			name = data.name,
			area = 207,
			info = data.info,
			autoStart = true,
			autoComplete = true,
			summary = data.info[0],
			demandSummary = "[每日任務] 調查消逝的旅途 0 / " + data.required,
			rewardSummary = data.reward
		};
		for (int i = 0; i < data.info.Length; i++)
		{
			questDef.stateInfo[i] = data.info[i];
		}
		questDef.startReq.npc = (questDef.completeReq.npc = data.npc);
		questDef.startReq.level = data.level;
		definition = questDef;
		cache[questDef.id] = questDef;
		ids.Add(questDef.id);
		if (!byNpc.TryGetValue(data.npc, out var value))
		{
			value = (byNpc[data.npc] = new List<int>());
		}
		value.Add(questDef.id);
	}

	public static void Invalidate()
	{
		cached = null;
	}

	public static WeeklyGemstoneResponse Read(bool force = false)
	{
		if (!GameSession.InGame)
		{
			return new WeeklyGemstoneResponse();
		}
		if (!force && cached != null && owner == GameSession.CharacterId && DateTime.UtcNow < until)
		{
			return cached;
		}
		owner = GameSession.CharacterId;
		cached = GameSession.Net.WeeklyGemstone(new WeeklyGemstoneRequest
		{
			characterId = owner
		});
		until = DateTime.UtcNow.AddSeconds(1.0);
		foreach (int id in cached.resetQuests)
		{
			Profile.Data.ResetQuest(id);
			QuestEntry questEntry = GameSession.Character.quests.Find((QuestEntry q) => q.quest == id);
			if (questEntry != null)
			{
				questEntry.status = 0;
				questEntry.completed = 0;
			}
		}
		int num = (cached.claimed ? 2 : (cached.started ? 1 : 0));
		if (Profile.Data.QuestStatusOf(34151) != num)
		{
			Profile.Data.ResetQuest(34151);
			if (cached.claimed)
			{
				Profile.Data.SetQuestCompleted(34151);
			}
			else if (cached.started)
			{
				Profile.Data.SetQuestStarted(34151);
			}
		}
		if (definition != null)
		{
			definition.demandSummary = "[每日任務] 調查消逝的旅途 " + cached.clear + " / " + 2;
		}
		Profile.Data.SetQuestProgress(34151, "clear", cached.clear.ToString());
		return cached;
	}

	public static int Status()
	{
		WeeklyGemstoneResponse weeklyGemstoneResponse = Read();
		if (!weeklyGemstoneResponse.claimed)
		{
			if (!weeklyGemstoneResponse.started)
			{
				return 0;
			}
			return 1;
		}
		return 2;
	}

	public static bool Can(bool complete, int npc, out string reason)
	{
		WeeklyGemstoneResponse weeklyGemstoneResponse = Read();
		reason = null;
		if (!weeklyGemstoneResponse.eligible)
		{
			reason = "需达到 Lv." + 200 + " 并完成消逝的旅途前置任务";
		}
		else if (npc > 0 && npc != 3003104)
		{
			reason = "请找萝娜领取";
		}
		else if (weeklyGemstoneResponse.claimed)
		{
			reason = "本周奖励已经领取";
		}
		else if (complete && weeklyGemstoneResponse.clear < 2)
		{
			reason = "本周调查进度 " + weeklyGemstoneResponse.clear + " / " + 2;
		}
		else if (!complete && weeklyGemstoneResponse.started)
		{
			reason = "任务已在进行中";
		}
		return reason == null;
	}

	public static bool Run(bool claim)
	{
		Profile.Save();
		WeeklyGemstoneResponse weeklyGemstoneResponse = GameSession.Net.WeeklyGemstone(new WeeklyGemstoneRequest
		{
			characterId = GameSession.CharacterId,
			operation = ((!claim) ? WeeklyGemstoneOperation.Start : WeeklyGemstoneOperation.Claim)
		});
		if (!weeklyGemstoneResponse.ok)
		{
			BottomBar.Toast(weeklyGemstoneResponse.error);
			Invalidate();
			return false;
		}
		GameSession.ReloadAfterCommand();
		Invalidate();
		Read(force: true);
		return true;
	}
}
