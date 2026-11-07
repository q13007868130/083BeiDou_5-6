using System;
using System.Collections.Generic;
using UnityEngine;

public static class RobotQuests
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

		public int level;

		public int exp;

		public int area;

		public string name;

		public string summary;

		public string[] info;

		public ItemReq[] items;

		public Prereq[] prereq;
	}

	[Serializable]
	public sealed class ItemReq
	{
		public int item;

		public int count;
	}

	[Serializable]
	public sealed class Prereq
	{
		public int id;

		public int state;
	}

	public const string Resource = "Quests/robot";

	public static void Register(Dictionary<int, QuestDef> cache, List<int> ids, Dictionary<int, List<int>> byNpc)
	{
		TextAsset val = Resources.Load<TextAsset>("Quests/robot");
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		Records records = JsonUtility.FromJson<Records>(val.text);
		if (records?.quests == null)
		{
			return;
		}
		Record[] quests = records.quests;
		foreach (Record record in quests)
		{
			if (record.id <= 0 || record.npc <= 0)
			{
				continue;
			}
			if (ids.Contains(record.id))
			{
				throw new InvalidOperationException("机器人任务ID与WZ冲突: " + record.id);
			}
			if (cache.ContainsKey(record.id))
			{
				continue;
			}
			QuestDef questDef = new QuestDef
			{
				id = record.id,
				name = (string.IsNullOrEmpty(record.name) ? ("機器人任务 " + record.id) : record.name),
				area = ((record.area > 0) ? record.area : 20),
				summary = (string.IsNullOrEmpty(record.summary) ? "机器人玩法任务。" : record.summary)
			};
			questDef.info = record.info ?? Array.Empty<string>();
			for (int j = 0; j < questDef.info.Length; j++)
			{
				questDef.stateInfo[j] = questDef.info[j];
			}
			questDef.startReq.npc = (questDef.completeReq.npc = record.npc);
			if (record.level > 0)
			{
				questDef.startReq.level = (questDef.completeReq.level = record.level);
			}
			if (record.prereq != null)
			{
				Prereq[] prereq = record.prereq;
				foreach (Prereq prereq2 in prereq)
				{
					questDef.startReq.quests.Add(new KeyValuePair<int, int>(prereq2.id, prereq2.state));
				}
			}
			if (record.items != null)
			{
				ItemReq[] items = record.items;
				foreach (ItemReq itemReq in items)
				{
					if (itemReq.item > 0)
					{
						questDef.completeReq.items.Add(new QuestItemReq
						{
							item = itemReq.item,
							count = itemReq.count
						});
					}
				}
			}
			questDef.completeAct.exp = record.exp;
			string text = "";
			if (record.items != null)
			{
				ItemReq[] items = record.items;
				foreach (ItemReq itemReq2 in items)
				{
					text = text + "#i" + itemReq2.item + "# #t" + itemReq2.item + "# #a" + record.id + "1#\r\n";
				}
			}
			if (text.Length == 0)
			{
				text = "与 #r#p" + record.npc + "##k 对话。\r\n";
			}
			questDef.demandSummary = text + "\r\n交付：#r#p" + record.npc + "##k";
			cache.Add(record.id, questDef);
			ids.Add(record.id);
			if (!byNpc.TryGetValue(record.npc, out var value))
			{
				value = (byNpc[record.npc] = new List<int>());
			}
			if (!value.Contains(record.id))
			{
				value.Add(record.id);
			}
		}
	}
}
