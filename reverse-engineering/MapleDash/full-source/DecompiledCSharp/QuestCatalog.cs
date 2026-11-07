using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class QuestCatalog
{
	private static ImgNode checkRoot;

	private static ImgNode actRoot;

	private static ImgNode infoRoot;

	private static readonly Dictionary<int, QuestDef> cache = new Dictionary<int, QuestDef>();

	private static readonly List<int> allIds = new List<int>();

	private static readonly Dictionary<int, List<int>> byNpc = new Dictionary<int, List<int>>();

	private static bool tried;

	private static readonly List<int> Empty = new List<int>();

	public static bool Ready { get; private set; }

	public static int QuestCount
	{
		get
		{
			if (byNpc.Count <= 0)
			{
				return 0;
			}
			return allIds.Count;
		}
	}

	public static List<int> Ids
	{
		get
		{
			Ensure();
			return allIds;
		}
	}

	public static void Ensure()
	{
		if (tried)
		{
			return;
		}
		tried = true;
		try
		{
			checkRoot = WzAssets.FileRoot("Quest/Check.img");
			actRoot = WzAssets.FileRoot("Quest/Act.img");
			infoRoot = WzAssets.FileRoot("Quest/QuestInfo.img");
		}
		catch (Exception ex)
		{
			Debug.LogWarning((object)("[Quest] Quest.wz 不可用: " + ex.Message));
			return;
		}
		foreach (ImgNode child in checkRoot.Children)
		{
			if (!int.TryParse(child.Name, out var result))
			{
				continue;
			}
			allIds.Add(result);
			for (int i = 0; i <= 1; i++)
			{
				int num = ((child.Get(i + "/npc")?.Value != null) ? child.Int(i + "/npc", -1) : (-1));
				if (num > 0)
				{
					if (!byNpc.TryGetValue(num, out var value))
					{
						value = (byNpc[num] = new List<int>());
					}
					if (!value.Contains(result))
					{
						value.Add(result);
					}
				}
			}
		}
		ScreenshotTrainingQuests.Register(cache, allIds, byNpc);
		RobotQuests.Register(cache, allIds, byNpc);
		AdvancedQuests.Register(cache, allIds, byNpc);
		WeeklyGemstoneQuest.Register(cache, allIds, byNpc);
		allIds.Sort();
		Ready = true;
		Debug.Log((object)("[Quest] Quest.wz 任务 " + allIds.Count + " 个，涉及 NPC " + byNpc.Count + " 个"));
	}

	public static void ClearCache(int id = -1)
	{
		if (id >= 0)
		{
			cache.Remove(id);
			return;
		}
		cache.Clear();
		allIds.Clear();
		byNpc.Clear();
		tried = false;
		Ready = false;
	}

	public static QuestDef Get(int id)
	{
		Ensure();
		if (cache.TryGetValue(id, out var value))
		{
			return value;
		}
		value = Build(id);
		cache[id] = value;
		return value;
	}

	public static bool Exists(int id)
	{
		Ensure();
		if (!cache.ContainsKey(id))
		{
			if (checkRoot != null)
			{
				return checkRoot.Get(id.ToString()) != null;
			}
			return false;
		}
		return true;
	}

	public static List<int> ForNpc(int npcId)
	{
		Ensure();
		if (!byNpc.TryGetValue(npcId, out var value))
		{
			return Empty;
		}
		return value;
	}

	private static QuestDef Build(int id)
	{
		QuestDef questDef = new QuestDef
		{
			id = id
		};
		ImgNode imgNode = checkRoot?.Get(id.ToString());
		ImgNode imgNode2 = actRoot?.Get(id.ToString());
		ImgNode imgNode3 = infoRoot?.Get(id.ToString());
		if (imgNode3 != null)
		{
			questDef.name = imgNode3.Text("name");
			questDef.parent = imgNode3.Text("parent");
			questDef.summary = imgNode3.Text("summary");
			questDef.demandSummary = imgNode3.Text("demandSummary");
			questDef.rewardSummary = imgNode3.Text("rewardSummary");
			questDef.area = imgNode3.Int("area");
			questDef.medalCategory = imgNode3.Int("medalCategory", -1);
			questDef.medalItem = imgNode3.Int("viewMedalItem");
			questDef.autoStart = imgNode3.Int("autoStart") == 1;
			questDef.autoAccept = imgNode3.Int("autoAccept") == 1;
			questDef.autoPreComplete = imgNode3.Int("autoPreComplete") == 1;
			questDef.autoComplete = imgNode3.Int("autoComplete") == 1 || imgNode3.Int("autoPreComplete") == 1;
			List<KeyValuePair<int, string>> list = new List<KeyValuePair<int, string>>();
			foreach (ImgNode child in imgNode3.Children)
			{
				if (int.TryParse(child.Name, out var result))
				{
					list.Add(new KeyValuePair<int, string>(result, (child.Value as string) ?? ""));
					questDef.stateInfo[result] = (child.Value as string) ?? "";
				}
			}
			list.Sort((KeyValuePair<int, string> x, KeyValuePair<int, string> y) => x.Key.CompareTo(y.Key));
			questDef.info = new string[list.Count];
			for (int num = 0; num < list.Count; num++)
			{
				questDef.info[num] = list[num].Value;
			}
		}
		if (string.IsNullOrEmpty(questDef.name))
		{
			questDef.name = "任务 " + id;
		}
		questDef.startReq = ReadRequirement(imgNode?.Get("0"));
		questDef.completeReq = ReadRequirement(imgNode?.Get("1"));
		if (id == 8142)
		{
			questDef.startReq.items.RemoveAll((QuestItemReq item) => item.item == 1042003);
		}
		questDef.startScript = imgNode?.Text("0/startscript") ?? "";
		questDef.endScript = imgNode?.Text("1/endscript") ?? "";
		questDef.startAct = ReadActions(imgNode2?.Get("0"));
		questDef.completeAct = ReadActions(imgNode2?.Get("1"));
		return questDef;
	}

	private static QuestRequirement ReadRequirement(ImgNode node)
	{
		QuestRequirement questRequirement = new QuestRequirement();
		if (node == null)
		{
			return questRequirement;
		}
		questRequirement.completedQuests = node.Int("questComplete");
		questRequirement.monsterBookMin = node.Int("mbmin");
		questRequirement.petTameness = node.Int("pettamenessmin");
		questRequirement.startDate = node.Text("start");
		questRequirement.endDate = node.Text("end");
		ImgNode imgNode = node.Get("pet");
		if (imgNode != null)
		{
			foreach (ImgNode child in imgNode.Children)
			{
				questRequirement.pets.Add(child.Int("id"));
			}
		}
		questRequirement.level = node.Int("lvmin", node.Int("level"));
		questRequirement.maxLevel = node.Int("lvmax", node.Int("maxLevel"));
		questRequirement.meso = ((node.Get("meso") != null) ? node.Int("meso") : 0);
		questRequirement.fame = ((node.Get("fame") != null) ? node.Int("fame") : 0);
		questRequirement.interval = ((node.Get("interval") != null) ? node.Int("interval") : 0);
		questRequirement.fieldEnter = ((node.Get("fieldEnter/0") != null) ? node.Int("fieldEnter/0") : 0);
		questRequirement.npc = ((node.Get("npc") != null) ? node.Int("npc", -1) : (-1));
		ImgNode imgNode2 = node.Get("job");
		if (imgNode2 != null)
		{
			foreach (ImgNode child2 in imgNode2.Children)
			{
				if (child2.Value != null)
				{
					questRequirement.jobs.Add(Convert.ToInt32(child2.Value));
				}
			}
		}
		ReadPairs(node.Get("item"), questRequirement.items, "id", "count");
		ReadPairs(node.Get("mob"), questRequirement.mobs, "id", "count");
		ImgNode imgNode3 = node.Get("quest");
		if (imgNode3 != null)
		{
			foreach (ImgNode child3 in imgNode3.Children)
			{
				ImgNode imgNode4 = child3.Get("id");
				ImgNode imgNode5 = child3.Get("state");
				if (imgNode4 != null && imgNode4.Value != null)
				{
					questRequirement.quests.Add(new KeyValuePair<int, int>(Convert.ToInt32(imgNode4.Value), (imgNode5 != null && imgNode5.Value != null) ? Convert.ToInt32(imgNode5.Value) : 0));
				}
			}
		}
		ImgNode imgNode6 = node.Get("infoEx");
		if (imgNode6 != null)
		{
			foreach (ImgNode child4 in imgNode6.Children)
			{
				questRequirement.infoEx.Add((child4.Value as string) ?? "");
			}
		}
		return questRequirement;
	}

	private static void ReadPairs(ImgNode parent, List<QuestItemReq> list, string idKey, string countKey)
	{
		if (parent == null)
		{
			return;
		}
		foreach (ImgNode child in parent.Children)
		{
			ImgNode imgNode = child.Get(idKey);
			if (imgNode != null && imgNode.Value != null)
			{
				list.Add(new QuestItemReq
				{
					item = Convert.ToInt32(imgNode.Value),
					count = ((child.Get(countKey)?.Value != null) ? Convert.ToInt32(child.Get(countKey).Value) : 0)
				});
			}
		}
	}

	private static QuestActions ReadActions(ImgNode node)
	{
		QuestActions questActions = new QuestActions();
		if (node == null)
		{
			return questActions;
		}
		if (node.Get("exp") != null)
		{
			questActions.exp = node.Int("exp");
		}
		if (node.Get("money") != null)
		{
			questActions.money = node.Int("money");
		}
		if (node.Get("fame") != null)
		{
			questActions.fame = node.Int("fame");
		}
		questActions.petTameness = node.Int("pettameness");
		if (node.Get("nextQuest") != null)
		{
			questActions.nextQuest = node.Int("nextQuest");
		}
		ImgNode imgNode = node.Get("item");
		if (imgNode != null)
		{
			foreach (ImgNode child in imgNode.Children)
			{
				ImgNode imgNode2 = child.Get("id");
				if (imgNode2 != null && imgNode2.Value != null)
				{
					QuestItemReward questItemReward = new QuestItemReward
					{
						item = Convert.ToInt32(imgNode2.Value),
						count = ((child.Get("count")?.Value == null) ? 1 : Convert.ToInt32(child.Get("count").Value))
					};
					ImgNode imgNode3 = child.Get("prop");
					if (imgNode3 != null && imgNode3.Value != null)
					{
						questItemReward.prop = Convert.ToInt32(imgNode3.Value);
					}
					questActions.items.Add(questItemReward);
				}
			}
		}
		ImgNode imgNode4 = node.Get("skill");
		if (imgNode4 != null)
		{
			foreach (ImgNode child2 in imgNode4.Children)
			{
				ImgNode imgNode5 = child2.Get("id");
				if (imgNode5 != null && imgNode5.Value != null)
				{
					questActions.skills.Add(new QuestSkillReward
					{
						skill = Convert.ToInt32(imgNode5.Value),
						level = ((child2.Get("level")?.Value == null) ? 1 : Convert.ToInt32(child2.Get("level").Value)),
						masterLevel = ((child2.Get("masterLevel")?.Value != null) ? Convert.ToInt32(child2.Get("masterLevel").Value) : 0)
					});
				}
			}
		}
		return questActions;
	}
}
