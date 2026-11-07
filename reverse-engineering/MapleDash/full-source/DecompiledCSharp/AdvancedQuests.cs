using System;
using System.Collections.Generic;
using UnityEngine;

public static class AdvancedQuests
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

		public int autoStart;

		public string name;

		public string summary;

		public string[] info;

		public ItemReq[] items;

		public Prereq[] prereq;

		public Prereq[] prereqDone;
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

	public const string Resource = "Quests/advanced";

	public static void Register(Dictionary<int, QuestDef> cache, List<int> ids, Dictionary<int, List<int>> byNpc)
	{
		TextAsset val = Resources.Load<TextAsset>("Quests/advanced");
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
				throw new InvalidOperationException("进阶任务ID与WZ冲突: " + record.id);
			}
			if (cache.ContainsKey(record.id))
			{
				continue;
			}
			QuestDef questDef = new QuestDef
			{
				id = record.id,
				name = (string.IsNullOrEmpty(record.name) ? ("進階任务 " + record.id) : record.name),
				area = ((record.area > 0) ? record.area : 46),
				summary = (string.IsNullOrEmpty(record.summary) ? "五轉 / 六轉任务。" : record.summary)
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
			questDef.autoStart = record.autoStart == 1;
			if (record.prereq != null)
			{
				Prereq[] prereq = record.prereq;
				foreach (Prereq prereq2 in prereq)
				{
					questDef.startReq.quests.Add(new KeyValuePair<int, int>(prereq2.id, prereq2.state));
				}
			}
			if (record.prereqDone != null)
			{
				Prereq[] prereq = record.prereqDone;
				foreach (Prereq prereq3 in prereq)
				{
					questDef.completeReq.quests.Add(new KeyValuePair<int, int>(prereq3.id, prereq3.state));
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
			string text = "与 #r#p" + record.npc + "##k 对话。\r\n";
			if (record.prereqDone != null && record.prereqDone.Length != 0)
			{
				text += "\r\n交付前需完成：";
				Prereq[] prereq = record.prereqDone;
				foreach (Prereq prereq4 in prereq)
				{
					text = text + "\r\n· #b" + (cache.TryGetValue(prereq4.id, out var value) ? value.name : ("任务 " + prereq4.id)) + "#k";
				}
			}
			if (record.items != null)
			{
				ItemReq[] items = record.items;
				foreach (ItemReq itemReq2 in items)
				{
					text = text + "\r\n#i" + itemReq2.item + "# #t" + itemReq2.item + "# #a" + record.id + "1#";
				}
			}
			questDef.demandSummary = text;
			cache.Add(record.id, questDef);
			ids.Add(record.id);
			if (!byNpc.TryGetValue(record.npc, out var value2))
			{
				value2 = (byNpc[record.npc] = new List<int>());
			}
			if (!value2.Contains(record.id))
			{
				value2.Add(record.id);
			}
		}
	}
}
