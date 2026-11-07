using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MapleDash.Wz;
using UnityEngine;

public static class AdminQuestBrowse
{
	private sealed class QuestRecord
	{
		public string QuestId;

		public string Name = "";

		public int? LevelMin;

		public int? LevelMax;

		public string StartNpcId;

		public string EndNpcId;

		public string ContentStart;

		public string ContentProgress;

		public string ContentComplete;

		public string ParentName;

		public int? Order;

		public string NextQuestId;

		public string Region;

		public string Town;
	}

	private sealed class MapEntry
	{
		public string Region;

		public string Town;
	}

	private sealed class QuestIndex
	{
		public readonly List<QuestRecord> Quests = new List<QuestRecord>();

		public readonly Dictionary<string, QuestRecord> ById = new Dictionary<string, QuestRecord>(StringComparer.Ordinal);

		public readonly Dictionary<string, List<string>> ChainGroups = new Dictionary<string, List<string>>(StringComparer.Ordinal);

		public readonly HashSet<string> NextQuestTargets = new HashSet<string>(StringComparer.Ordinal);

		public Dictionary<string, List<string>> NpcMaps = new Dictionary<string, List<string>>(StringComparer.Ordinal);

		public Dictionary<string, MapEntry> MapInfo = new Dictionary<string, MapEntry>(StringComparer.Ordinal);

		public Dictionary<string, string> NpcNames = new Dictionary<string, string>(StringComparer.Ordinal);
	}

	private sealed class TownCount
	{
		public string Town;

		public int Count;
	}

	private sealed class RegionCount
	{
		public string Region;

		public int Total;

		public readonly List<TownCount> Towns = new List<TownCount>();
	}

	private const string UnknownRegion = "unknown";

	private const string UnknownRegionLabel = "未定位";

	private const string UnknownStreet = "(未知街道)";

	private static readonly Dictionary<string, string> RegionNames = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		{ "victoria", "金银岛" },
		{ "ossyria", "神秘岛" },
		{ "elin", "艾琳森林" },
		{ "china", "东方神州" },
		{ "jp", "日本" },
		{ "thai", "泰国" },
		{ "singapore", "新加坡·马来西亚" },
		{ "maple", "彩虹岛" },
		{ "MasteriaGL", "马斯特里亚" },
		{ "weddingGL", "婚礼村" },
		{ "HalloweenGL", "万圣节" },
		{ "Episode1GL", "剧情活动" },
		{ "event", "活动地图" },
		{ "etc", "其他" },
		{ "grandis", "格兰蒂斯" }
	};

	private static QuestIndex cache;

	private static readonly object gate = new object();

	public static void Register(AdminApi api)
	{
		api.Get("/quest/v1/townTree", (AdminCtx ctx) => TownTree());
		api.Post("/quest/v1/list", (AdminCtx ctx) => QuestList(ctx.BodyString("region"), ctx.BodyString("town")));
		api.Post("/quest/v1/detail", (AdminCtx ctx) => QuestDetail(ctx.BodyString("questId")));
	}

	private static QuestIndex Index()
	{
		lock (gate)
		{
			if (cache == null)
			{
				cache = AdminMainThread.Run((Func<QuestIndex>)BuildIndex, 20000);
			}
			return cache;
		}
	}

	private static QuestIndex BuildIndex()
	{
		QuestIndex questIndex = new QuestIndex();
		LoadMapIndex(questIndex);
		LoadNpcNames(questIndex);
		LoadQuestInfo(questIndex);
		LoadCheck(questIndex);
		LoadAct(questIndex);
		Attribute(questIndex);
		AggregateChains(questIndex);
		return questIndex;
	}

	private static void LoadMapIndex(QuestIndex idx)
	{
		string text = LoadIndexText();
		if (string.IsNullOrEmpty(text))
		{
			throw new AdminApiException("缺少任务浏览索引 Quest/QuestBrowseIndex.json，请先运行 tools/export_quest_index.py 生成");
		}
		if (!(AdminJson.Parse(text) is Dictionary<string, object> o))
		{
			throw new AdminApiException("任务浏览索引无法解析（Quest/QuestBrowseIndex.json 损坏）");
		}
		Dictionary<string, object> dict = AdminJson.GetDict(o, "npcMaps");
		if (dict != null)
		{
			foreach (KeyValuePair<string, object> item in dict)
			{
				List<string> list = new List<string>();
				List<object> list2 = AdminJson.AsList(item.Value);
				if (list2 != null)
				{
					foreach (object item2 in list2)
					{
						list.Add(NormId(Convert.ToString(item2, CultureInfo.InvariantCulture)));
					}
				}
				idx.NpcMaps[NormId(item.Key)] = list;
			}
		}
		Dictionary<string, object> dict2 = AdminJson.GetDict(o, "mapInfo");
		if (dict2 == null)
		{
			return;
		}
		foreach (KeyValuePair<string, object> item3 in dict2)
		{
			Dictionary<string, object> dictionary = AdminJson.AsDict(item3.Value);
			if (dictionary != null)
			{
				idx.MapInfo[NormId(item3.Key)] = new MapEntry
				{
					Region = AdminJson.GetString(dictionary, "region"),
					Town = AdminJson.GetString(dictionary, "town")
				};
			}
		}
	}

	private static void LoadNpcNames(QuestIndex idx)
	{
		foreach (ImgNode child in WzRoot("String/Npc.img").Children)
		{
			string value = child.Text("name");
			if (!string.IsNullOrEmpty(value))
			{
				idx.NpcNames[NormId(child.Name)] = value;
			}
		}
	}

	private static void LoadQuestInfo(QuestIndex idx)
	{
		foreach (ImgNode child in WzRoot("Quest/QuestInfo.img").Children)
		{
			QuestRecord questRecord = new QuestRecord
			{
				QuestId = child.Name,
				Name = (StrVal(child.Get("name")) ?? ""),
				ContentStart = StrVal(child.Get("0")),
				ContentProgress = StrVal(child.Get("1")),
				ContentComplete = StrVal(child.Get("2")),
				ParentName = StrVal(child.Get("parent")),
				Order = TryInt(child.Get("order"))
			};
			idx.Quests.Add(questRecord);
			idx.ById[questRecord.QuestId] = questRecord;
		}
	}

	private static void LoadCheck(QuestIndex idx)
	{
		foreach (ImgNode child in WzRoot("Quest/Check.img").Children)
		{
			if (idx.ById.TryGetValue(child.Name, out var value))
			{
				ImgNode imgNode = child.Get("0");
				if (imgNode != null)
				{
					value.StartNpcId = IdVal(imgNode.Get("npc"));
					value.LevelMin = TryInt(imgNode.Get("lvmin"));
					value.LevelMax = TryInt(imgNode.Get("lvmax"));
				}
				ImgNode imgNode2 = child.Get("1");
				if (imgNode2 != null)
				{
					value.EndNpcId = IdVal(imgNode2.Get("npc"));
				}
			}
		}
	}

	private static void LoadAct(QuestIndex idx)
	{
		foreach (ImgNode child in WzRoot("Quest/Act.img").Children)
		{
			if (!idx.ById.TryGetValue(child.Name, out var value))
			{
				continue;
			}
			foreach (ImgNode child2 in child.Children)
			{
				string text = IdVal(child2.Get("nextQuest"));
				if (!string.IsNullOrEmpty(text))
				{
					value.NextQuestId = text;
					break;
				}
			}
		}
	}

	private static void Attribute(QuestIndex idx)
	{
		foreach (QuestRecord quest in idx.Quests)
		{
			if (string.IsNullOrEmpty(quest.StartNpcId) || !idx.NpcMaps.TryGetValue(NormId(quest.StartNpcId), out var value) || value.Count == 0)
			{
				continue;
			}
			string text = null;
			string text2 = null;
			foreach (string item in value)
			{
				if (!idx.MapInfo.TryGetValue(item, out var value2))
				{
					continue;
				}
				string text3 = (string.IsNullOrEmpty(value2.Town) ? "(未知街道)" : value2.Town);
				if (text == null)
				{
					text = item;
					text2 = text3;
					continue;
				}
				bool flag = text3 == "(未知街道)";
				bool flag2 = text2 == "(未知街道)";
				if (flag2 && !flag)
				{
					text = item;
					text2 = text3;
				}
				else if (flag == flag2 && QuestNum(item) < QuestNum(text))
				{
					text = item;
					text2 = text3;
				}
			}
			if (text != null)
			{
				quest.Town = text2;
				string region = idx.MapInfo[text].Region;
				quest.Region = (string.IsNullOrEmpty(region) ? "unknown" : region);
			}
		}
	}

	private static void AggregateChains(QuestIndex idx)
	{
		Dictionary<string, List<QuestRecord>> dictionary = new Dictionary<string, List<QuestRecord>>(StringComparer.Ordinal);
		foreach (QuestRecord quest in idx.Quests)
		{
			if (!string.IsNullOrEmpty(quest.ParentName))
			{
				if (!dictionary.TryGetValue(quest.ParentName, out var value))
				{
					value = (dictionary[quest.ParentName] = new List<QuestRecord>());
				}
				value.Add(quest);
			}
			if (!string.IsNullOrEmpty(quest.NextQuestId))
			{
				idx.NextQuestTargets.Add(quest.NextQuestId);
			}
		}
		foreach (KeyValuePair<string, List<QuestRecord>> item in dictionary)
		{
			List<QuestRecord> value2 = item.Value;
			value2.Sort((QuestRecord a, QuestRecord b) =>
			{
				int num = a.Order.GetValueOrDefault().CompareTo(b.Order.GetValueOrDefault());
				return (num == 0) ? QuestNum(a.QuestId).CompareTo(QuestNum(b.QuestId)) : num;
			});
			List<string> list2 = new List<string>(value2.Count);
			foreach (QuestRecord item2 in value2)
			{
				list2.Add(item2.QuestId);
			}
			idx.ChainGroups[item.Key] = list2;
		}
	}

	private static bool IsInChain(QuestIndex idx, QuestRecord q)
	{
		if (!string.IsNullOrEmpty(q.ParentName))
		{
			return true;
		}
		if (!string.IsNullOrEmpty(q.NextQuestId))
		{
			return true;
		}
		return idx.NextQuestTargets.Contains(q.QuestId);
	}

	private static List<object> TownTree()
	{
		QuestIndex questIndex = Index();
		List<RegionCount> list = new List<RegionCount>();
		Dictionary<string, RegionCount> dictionary = new Dictionary<string, RegionCount>(StringComparer.Ordinal);
		foreach (QuestRecord quest in questIndex.Quests)
		{
			string text = quest.Region ?? "unknown";
			string text2 = quest.Town ?? "(未知街道)";
			if (!dictionary.TryGetValue(text, out var value))
			{
				value = (dictionary[text] = new RegionCount
				{
					Region = text
				});
				list.Add(value);
			}
			List<TownCount> towns = value.Towns;
			TownCount townCount = null;
			foreach (TownCount item in towns)
			{
				if (item.Town == text2)
				{
					townCount = item;
					break;
				}
			}
			if (townCount == null)
			{
				TownCount townCount2 = new TownCount
				{
					Town = text2
				};
				townCount = townCount2;
				towns.Add(townCount2);
			}
			townCount.Count++;
			value.Total++;
		}
		List<object> list2 = new List<object>(list.Count);
		foreach (RegionCount r in list)
		{
			List<int> list3 = new List<int>(r.Towns.Count);
			for (int i = 0; i < r.Towns.Count; i++)
			{
				list3.Add(i);
			}
			list3.Sort((int a, int b) =>
			{
				int num = r.Towns[b].Count.CompareTo(r.Towns[a].Count);
				return (num == 0) ? a.CompareTo(b) : num;
			});
			List<object> list4 = new List<object>(r.Towns.Count);
			foreach (int item2 in list3)
			{
				TownCount townCount3 = r.Towns[item2];
				list4.Add(AdminJson.Obj("value", townCount3.Town, "label", townCount3.Town + " (" + townCount3.Count + ")", "count", townCount3.Count, "isLeaf", true, "children", null));
			}
			list2.Add(AdminJson.Obj("value", r.Region, "label", RegionLabel(r.Region) + " (" + r.Total + ")", "count", r.Total, "isLeaf", null, "children", list4));
		}
		return list2;
	}

	private static List<object> QuestList(string region, string town)
	{
		QuestIndex questIndex = Index();
		bool flag = string.IsNullOrEmpty(region);
		bool flag2 = string.IsNullOrEmpty(town);
		List<QuestRecord> list = new List<QuestRecord>();
		foreach (QuestRecord quest in questIndex.Quests)
		{
			if ((flag || !(region != (quest.Region ?? "unknown"))) && (flag2 || !(town != (quest.Town ?? "(未知街道)"))))
			{
				list.Add(quest);
			}
		}
		list.Sort((QuestRecord a, QuestRecord b) =>
		{
			int num = a.LevelMin.GetValueOrDefault().CompareTo(b.LevelMin.GetValueOrDefault());
			return (num == 0) ? QuestNum(a.QuestId).CompareTo(QuestNum(b.QuestId)) : num;
		});
		List<object> list2 = new List<object>(list.Count);
		foreach (QuestRecord item in list)
		{
			list2.Add(SummaryDto(questIndex, item));
		}
		return list2;
	}

	private static Dictionary<string, object> SummaryDto(QuestIndex idx, QuestRecord q)
	{
		return AdminJson.Obj("questId", q.QuestId, "name", q.Name, "levelMin", q.LevelMin, "levelMax", q.LevelMax, "startNpcId", q.StartNpcId, "startNpcName", NpcName(idx, q.StartNpcId), "endNpcId", q.EndNpcId, "endNpcName", NpcName(idx, q.EndNpcId), "inChain", IsInChain(idx, q), "region", q.Region ?? "unknown", "town", q.Town ?? "(未知街道)");
	}

	private static Dictionary<string, object> QuestDetail(string questId)
	{
		QuestIndex questIndex = Index();
		string text = ((questId == null) ? "" : questId.Trim());
		if (text.Length == 0 || !questIndex.ById.TryGetValue(text, out var value))
		{
			throw new AdminApiException("任务不存在: " + text);
		}
		List<object> list = new List<object>();
		List<string> value2 = null;
		if (!string.IsNullOrEmpty(value.ParentName))
		{
			questIndex.ChainGroups.TryGetValue(value.ParentName, out value2);
		}
		if (value2 == null || value2.Count == 0)
		{
			value2 = new List<string> { value.QuestId };
		}
		foreach (string item in value2)
		{
			if (questIndex.ById.TryGetValue(item, out var value3))
			{
				list.Add(AdminJson.Obj("questId", value3.QuestId, "name", value3.Name, "levelMin", value3.LevelMin, "current", value3.QuestId == value.QuestId));
			}
		}
		return AdminJson.Obj("questId", value.QuestId, "name", value.Name, "levelMin", value.LevelMin, "levelMax", value.LevelMax, "startNpcId", value.StartNpcId, "startNpcName", NpcName(questIndex, value.StartNpcId), "endNpcId", value.EndNpcId, "endNpcName", NpcName(questIndex, value.EndNpcId), "contentStart", value.ContentStart, "contentProgress", value.ContentProgress, "contentComplete", value.ContentComplete, "parentName", value.ParentName, "order", value.Order, "chain", list, "region", value.Region ?? "unknown", "town", value.Town ?? "(未知街道)");
	}

	private static string RegionLabel(string key)
	{
		if (key == "unknown")
		{
			return "未定位";
		}
		if (!RegionNames.TryGetValue(key, out var value))
		{
			return key;
		}
		return value;
	}

	private static string NpcName(QuestIndex idx, string npcId)
	{
		if (string.IsNullOrEmpty(npcId))
		{
			return null;
		}
		if (!idx.NpcNames.TryGetValue(NormId(npcId), out var value))
		{
			return null;
		}
		return value;
	}

	private static string NormId(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return id;
		}
		if (!long.TryParse(id.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return id;
		}
		return result.ToString(CultureInfo.InvariantCulture);
	}

	private static long QuestNum(string id)
	{
		if (!long.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return 0L;
		}
		return result;
	}

	private static string IdVal(ImgNode node)
	{
		object obj = node?.Value;
		if (obj == null)
		{
			return null;
		}
		return NormId(Convert.ToString(obj, CultureInfo.InvariantCulture));
	}

	private static string StrVal(ImgNode node)
	{
		object obj = node?.Value;
		if (obj == null)
		{
			return null;
		}
		return (obj as string) ?? Convert.ToString(obj, CultureInfo.InvariantCulture);
	}

	private static int? TryInt(ImgNode node)
	{
		object obj = node?.Value;
		if (obj == null)
		{
			return null;
		}
		try
		{
			return Convert.ToInt32(obj, CultureInfo.InvariantCulture);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static ImgNode WzRoot(string relative)
	{
		return WzAssets.FileRoot(relative);
	}

	private static string LoadIndexText()
	{
		TextAsset val = Resources.Load<TextAsset>("Quest/QuestBrowseIndex");
		if ((Object)(object)val != (Object)null && !string.IsNullOrEmpty(val.text))
		{
			return val.text;
		}
		string path = Path.Combine("Assets", "Resources", "Quest", "QuestBrowseIndex.json");
		if (!File.Exists(path))
		{
			return null;
		}
		return File.ReadAllText(path);
	}
}
