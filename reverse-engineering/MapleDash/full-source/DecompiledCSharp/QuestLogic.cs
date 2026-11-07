using System;
using System.Collections.Generic;
using System.Globalization;
using Server.Field;
using UnityEngine;

public static class QuestLogic
{
	private static readonly Random rng = new Random();

	public static int PlayerJob => Profile.JobId;

	public static int JobId(string cls)
	{
		return cls switch
		{
			"warrior" => 100, 
			"knight" => 110, 
			"mage" => 200, 
			"archer" => 300, 
			"priest" => 230, 
			_ => 0, 
		};
	}

	public static bool NeedQuestItem(int questId, int itemId)
	{
		if (MapItemPickup.NeedQuestItemUnrestricted(questId))
		{
			return true;
		}
		int num = Profile.QuestStatus(questId);
		if (num != 0 && num != 1)
		{
			return false;
		}
		QuestDef questDef = QuestCatalog.Get(questId);
		if (questDef == null)
		{
			return false;
		}
		foreach (QuestItemReq item in (num == 0) ? questDef.startReq.items : questDef.completeReq.items)
		{
			if (item.item == itemId)
			{
				return Profile.ItemCount(itemId) < item.count;
			}
		}
		return num == 1;
	}

	public static bool CanStart(QuestDef q, int npcId, out string reason)
	{
		if (q.id == 34151)
		{
			return WeeklyGemstoneQuest.Can(complete: false, npcId, out reason);
		}
		switch (Profile.QuestStatus(q.id))
		{
		case 1:
			reason = "任务已在进行中";
			return false;
		case 2:
			if (q.startReq.interval <= 0)
			{
				reason = "任务已完成";
				return false;
			}
			break;
		}
		return Check(q.startReq, q.id, npcId, out reason);
	}

	public static bool CanComplete(QuestDef q, int npcId, out string reason)
	{
		if (q.id == 34151)
		{
			return WeeklyGemstoneQuest.Can(complete: true, npcId, out reason);
		}
		if (Profile.QuestStatus(q.id) != 1)
		{
			reason = "任务未开始";
			return false;
		}
		return Check(q.completeReq, q.id, npcId, out reason);
	}

	public static bool CanStart(QuestDef q, int npcId)
	{
		string reason;
		return CanStart(q, npcId, out reason);
	}

	public static bool CanComplete(QuestDef q, int npcId)
	{
		string reason;
		return CanComplete(q, npcId, out reason);
	}

	public static bool InProgressAt(QuestDef q, int npcId)
	{
		if (q.completeReq.npc == npcId)
		{
			return Profile.QuestStatus(q.id) == 1;
		}
		return false;
	}

	public static string IncompleteStop(QuestDef q)
	{
		foreach (QuestItemReq item in q.completeReq.items)
		{
			int num = Profile.ItemCount(item.item);
			if ((item.count <= 0) ? (num > 0) : (num < item.count))
			{
				return "item";
			}
		}
		foreach (QuestItemReq mob in q.completeReq.mobs)
		{
			if (Profile.QuestProgressInt(q.id, mob.item.ToString()) < mob.count)
			{
				return "mob";
			}
		}
		return "";
	}

	public static bool NotificationDateAvailable(QuestRequirement requirement, DateTime now)
	{
		if (!TryNotificationDate(requirement.startDate, out var date) || now >= date)
		{
			if (TryNotificationDate(requirement.endDate, out var date2))
			{
				return now <= date2;
			}
			return true;
		}
		return false;
	}

	private static bool TryNotificationDate(string text, out DateTime date)
	{
		return DateTime.TryParseExact(text, "yyyyMMddHH", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
	}

	private static bool Check(QuestRequirement r, int questId, int npcId, out string reason)
	{
		reason = null;
		if (r.completedQuests > Profile.CompletedQuests.Count)
		{
			reason = "已完成任务数量不足 " + r.completedQuests;
			return false;
		}
		if (r.monsterBookMin > 0)
		{
			SocialResponse socialResponse = (GameSession.InGame ? GameSession.Net.Social(new SocialRequest
			{
				characterId = GameSession.CharacterId
			}) : null);
			if (socialResponse == null || socialResponse.cards.Count < r.monsterBookMin)
			{
				reason = "怪物手册卡片种类不足 " + r.monsterBookMin;
				return false;
			}
		}
		if (r.pets.Count > 0 || r.petTameness > 0)
		{
			bool flag = r.pets.Count == 0;
			int num = 0;
			if (GameSession.InGame)
			{
				for (int i = 0; i < 3; i++)
				{
					InventoryEntry pet = GameSession.Net.ChangeInventory(new InventoryChangeRequest
					{
						characterId = GameSession.CharacterId,
						itemId = 5000000,
						getPet = true,
						petSlot = i
					}).pet;
					if (pet != null)
					{
						if (r.pets.Contains(pet.itemId))
						{
							flag = true;
						}
						num = Math.Max(num, Convert.ToInt32(pet.petData["closeness"]));
					}
				}
			}
			if (!flag || num < r.petTameness)
			{
				reason = "需要召唤符合条件及亲密度的宠物";
				return false;
			}
		}
		if (r.endDate.Length == 10 && int.TryParse(r.endDate.Substring(0, 4), out var result) && int.TryParse(r.endDate.Substring(4, 2), out var result2) && int.TryParse(r.endDate.Substring(6, 2), out var result3) && int.TryParse(r.endDate.Substring(8, 2), out var result4))
		{
			long num2 = new DateTimeOffset(new DateTime(result, 1, 1).AddMonths(result2).AddDays(result3 - 1).AddHours(result4)).ToUnixTimeMilliseconds();
			if (num2 >= 1732027571809L && num2 < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
			{
				reason = "活动已结束";
				return false;
			}
		}
		if (r.level > 0 && Profile.level < r.level)
		{
			reason = "等级不足 Lv." + r.level;
			return false;
		}
		if (r.maxLevel > 0 && Profile.level > r.maxLevel)
		{
			reason = "等级过高";
			return false;
		}
		if (r.jobs.Count > 0)
		{
			bool flag2 = false;
			for (int j = 0; j < r.jobs.Count; j++)
			{
				if (r.jobs[j] == PlayerJob)
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				reason = "职业不符";
				return false;
			}
		}
		if (r.npc > 0 && npcId > 0 && r.npc != npcId)
		{
			reason = "需要找指定 NPC";
			return false;
		}
		if (r.meso > 0 && Profile.gold < r.meso)
		{
			reason = "金币不足";
			return false;
		}
		for (int k = 0; k < r.items.Count; k++)
		{
			QuestItemReq questItemReq = r.items[k];
			int num3 = Profile.ItemCount(questItemReq.item);
			if ((questItemReq.count <= 0) ? (num3 > 0) : (num3 < questItemReq.count))
			{
				reason = "道具不足 " + questItemReq.item + " x" + questItemReq.count;
				return false;
			}
		}
		for (int l = 0; l < r.quests.Count; l++)
		{
			KeyValuePair<int, int> keyValuePair = r.quests[l];
			if (Profile.QuestStatus(keyValuePair.Key) != keyValuePair.Value)
			{
				reason = "前置任务 " + keyValuePair.Key + " 状态不符";
				return false;
			}
		}
		for (int m = 0; m < r.mobs.Count; m++)
		{
			QuestItemReq questItemReq2 = r.mobs[m];
			if (Profile.QuestProgressInt(questId, questItemReq2.item.ToString()) < questItemReq2.count)
			{
				reason = "击杀进度不足 " + questItemReq2.item + " (" + Profile.QuestProgressInt(questId, questItemReq2.item.ToString()) + "/" + questItemReq2.count + ")";
				return false;
			}
		}
		for (int n = 0; n < r.infoEx.Count; n++)
		{
			string text = r.infoEx[n] ?? "";
			if (Profile.QuestProgress(questId, "info" + n, "") != text)
			{
				reason = "任务进度条件未满足";
				return false;
			}
		}
		if (r.fieldEnter > 0 && Profile.LastMapId != r.fieldEnter)
		{
			reason = "需要进入指定地图";
			return false;
		}
		if (r.interval > 0 && Profile.IsQuestCompleted(questId))
		{
			long num4 = Profile.QuestCompletedTime(questId);
			if (num4 > 0 && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - num4 < (long)r.interval * 60000L)
			{
				reason = "任务冷却中";
				return false;
			}
		}
		return true;
	}

	public static void OnMobKilled(int mobId)
	{
		if (mobId <= 0)
		{
			return;
		}
		IReadOnlyList<int> startedQuests = Profile.StartedQuests;
		bool flag = false;
		for (int i = 0; i < startedQuests.Count; i++)
		{
			QuestDef questDef = QuestCatalog.Get(startedQuests[i]);
			if (questDef == null)
			{
				continue;
			}
			List<QuestItemReq> mobs = questDef.completeReq.mobs;
			int num = 0;
			for (int j = 0; j < mobs.Count; j++)
			{
				if (mobs[j].item == mobId)
				{
					num = mobs[j].count;
					break;
				}
			}
			string key = mobId.ToString();
			int num2 = Profile.QuestProgressInt(questDef.id, key);
			if (num2 < num)
			{
				bool flag2 = CanComplete(questDef, 0);
				Profile.SetQuestProgress(questDef.id, key, (num2 + 1).ToString(), save: false);
				QuestAlarm.Progress(questDef.id);
				if (!flag2 && CanComplete(questDef, 0))
				{
					QuestNotice.Ready(questDef.id);
				}
				flag = true;
			}
		}
		if (flag)
		{
			Profile.Save();
			NpcField.InvalidateMarkers();
		}
	}

	public static bool Start(QuestDef q, int npcId, bool applyActions)
	{
		if (q == null)
		{
			return false;
		}
		if (q.id == 34151)
		{
			return WeeklyGemstoneQuest.Run(claim: false);
		}
		if (applyActions)
		{
			Apply(q.startAct, q.id);
		}
		Profile.StartQuest(q.id);
		Debug.Log((object)("[Quest] 接取 " + q.id + " " + q.name + " (NPC " + npcId + ")"));
		return true;
	}

	public static bool Complete(QuestDef q, int npcId, bool applyActions)
	{
		if (q == null)
		{
			return false;
		}
		if (q.id == 34151)
		{
			if (!WeeklyGemstoneQuest.Run(claim: true))
			{
				return false;
			}
			QuestAlarm.Remove(q.id);
			QuestNotice.RemoveReady(q.id);
			QuestNotice.Push(q.name);
			return true;
		}
		if (applyActions)
		{
			Apply(q.completeAct, q.id);
		}
		Profile.CompleteQuest(q.id);
		if (NativeWeeklyGemstoneService.IsDaily(q.id))
		{
			WeeklyGemstoneQuest.Invalidate();
		}
		AdvancementProgress.OnQuestCompleted(q.id);
		QuestAlarm.Remove(q.id);
		QuestNotice.RemoveReady(q.id);
		QuestNotice.Push(q.name);
		Debug.Log((object)("[Quest] 完成 " + q.id + " " + q.name + " (NPC " + npcId + ")"));
		return true;
	}

	public static QuestDef ChainedNext(QuestActions act, int npcId)
	{
		if (act == null || act.nextQuest <= 0)
		{
			return null;
		}
		QuestDef questDef = QuestCatalog.Get(act.nextQuest);
		if (questDef == null)
		{
			return null;
		}
		if (questDef.startReq.npc > 0 && questDef.startReq.npc != npcId)
		{
			return null;
		}
		if (!CanStart(questDef, npcId, out var _))
		{
			return null;
		}
		return questDef;
	}

	public static bool RestoreLostItems(QuestDef q)
	{
		if (q == null || Profile.QuestStatus(q.id) != 1)
		{
			return false;
		}
		bool result = false;
		for (int i = 0; i < q.completeReq.items.Count; i++)
		{
			QuestItemReq questItemReq = q.completeReq.items[i];
			if (questItemReq.count <= 0)
			{
				continue;
			}
			int num = Profile.ItemCount(questItemReq.item);
			if (num >= questItemReq.count)
			{
				continue;
			}
			bool flag = false;
			for (int j = 0; j < q.startAct.items.Count; j++)
			{
				if (q.startAct.items[j].item == questItemReq.item && q.startAct.items[j].count > 0)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				Profile.AddItem(questItemReq.item, questItemReq.count - num);
				Debug.Log((object)("[Quest] 补发任务道具 " + q.id + " 道具 " + questItemReq.item + " x" + (questItemReq.count - num)));
				result = true;
			}
		}
		return result;
	}

	public static void Apply(QuestActions a, int questId)
	{
		if (a == null)
		{
			return;
		}
		if (a.petTameness != 0 && GameSession.InGame)
		{
			InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
			{
				characterId = GameSession.CharacterId,
				itemId = 5000000,
				petTameness = a.petTameness,
				petLeaderOnly = true
			});
			if (inventoryChangeResponse.inventory != null)
			{
				GameSession.Character.inventory = inventoryChangeResponse.inventory;
				GameSession.ApplyInventoryToProfile();
			}
		}
		if (a.exp > 0)
		{
			Profile.GrantExperience(a.exp);
		}
		if (a.money != 0)
		{
			Profile.AddGold(a.money);
		}
		for (int i = 0; i < a.items.Count; i++)
		{
			if (a.items[i].count < 0)
			{
				Profile.RemoveItem(a.items[i].item, -a.items[i].count);
			}
		}
		int num = 0;
		for (int j = 0; j < a.items.Count; j++)
		{
			if (a.items[j].count >= 0 && a.items[j].prop >= 0)
			{
				num += a.items[j].prop;
			}
		}
		int num2 = ((num > 0) ? rng.Next(num) : (-1));
		int num3 = 0;
		for (int k = 0; k < a.items.Count; k++)
		{
			QuestItemReward questItemReward = a.items[k];
			if (questItemReward.count <= 0)
			{
				continue;
			}
			if (questItemReward.prop >= 0)
			{
				num3 += questItemReward.prop;
				if (num3 <= num2)
				{
					continue;
				}
				num3 = int.MinValue;
			}
			Profile.AddItem(questItemReward.item, questItemReward.count);
		}
		for (int l = 0; l < a.skills.Count; l++)
		{
			QuestSkillReward questSkillReward = a.skills[l];
			if (questSkillReward.skill > 0 && questSkillReward.level > 0)
			{
				SkillProgress.SetLevel(questSkillReward.skill, questSkillReward.level);
			}
		}
	}
}
