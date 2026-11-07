using System;
using System.Collections.Generic;
using UnityEngine;

public static class ScreenshotTrainingQuests
{
	[Serializable]
	public sealed class Records
	{
		public Record[] quests;
	}

	[Serializable]
	public sealed class Record
	{
		public int id;

		public int npc;

		public int mob;

		public int count;

		public int maxLevel;

		public int exp;

		public string name;

		public string available;

		public string progress;

		public string completed;
	}

	public static void Register(Dictionary<int, QuestDef> cache, List<int> ids, Dictionary<int, List<int>> byNpc)
	{
		TextAsset val = Resources.Load<TextAsset>("Quests/screenshot_training");
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		Record[] quests = JsonUtility.FromJson<Records>(val.text).quests;
		foreach (Record record in quests)
		{
			if (ids.Contains(record.id))
			{
				throw new InvalidOperationException("截图任务ID与WZ冲突: " + record.id);
			}
			QuestDef questDef = new QuestDef();
			questDef.id = record.id;
			questDef.name = record.name;
			questDef.area = 20;
			questDef.info = new string[3] { record.available, record.progress, record.completed };
			questDef.summary = "消灭指定数量的怪物。";
			questDef.demandSummary = "#o" + record.mob + "# #a" + record.id + "1#\n\n完成后对话：#r#p" + record.npc + "##k";
			QuestDef questDef2 = questDef;
			questDef2.startReq.npc = (questDef2.completeReq.npc = record.npc);
			questDef2.startReq.maxLevel = record.maxLevel;
			questDef2.completeAct.exp = record.exp;
			questDef2.completeReq.mobs.Add(new QuestItemReq
			{
				item = record.mob,
				count = record.count
			});
			for (int j = 0; j < questDef2.info.Length; j++)
			{
				questDef2.stateInfo[j] = questDef2.info[j];
			}
			cache.Add(record.id, questDef2);
			ids.Add(record.id);
			if (!byNpc.TryGetValue(record.npc, out var value))
			{
				value = (byNpc[record.npc] = new List<int>());
			}
			value.Remove(8142);
			value.Add(record.id);
		}
	}
}
