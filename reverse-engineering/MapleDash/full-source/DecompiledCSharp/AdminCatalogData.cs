using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using MapleDash.Wz;

public static class AdminCatalogData
{
	private sealed class ItemEntry
	{
		public long Id;

		public string Name;

		public string Description;

		public string Category;

		public Dictionary<string, object> Specs = new Dictionary<string, object>();

		public bool Icon;
	}

	private sealed class EquipEntry
	{
		public long Id;

		public string Name;

		public string Description;

		public string Category;

		public string Kind;

		public Dictionary<string, object> Stats = new Dictionary<string, object>();

		public bool Icon;

		public bool Cash;

		public int ReqJob;

		public int ReqLevel;
	}

	private sealed class MobEntry
	{
		public long Id;

		public string Name;

		public Dictionary<string, object> Stats = new Dictionary<string, object>();

		public bool Icon;

		public int Level;
	}

	private sealed class SkillEntry
	{
		public long Id;

		public string Name;

		public string Desc;

		public string JobName;

		public int JobId;

		public int FileJob;

		public int MaxLevel;

		public bool Icon;

		public Dictionary<string, object> Attributes = new Dictionary<string, object>();

		public List<object> LevelDescs = new List<object>();
	}

	private static bool hasFileWarmed;

	private static List<string> fileIndex;

	private static readonly string[] ItemCategoryKeys = new string[6] { "Consume", "Etc", "Install", "Cash", "Pet", "Special" };

	private static readonly object buildGate = new object();

	private static List<ItemEntry> itemEntries;

	private static List<object> itemCategoryFacet;

	private static List<EquipEntry> equipEntries;

	private static List<object> equipCategoryFacet;

	private static readonly Dictionary<int, string> EquipKinds = new Dictionary<int, string>
	{
		{ 100, "帽子" },
		{ 101, "脸饰" },
		{ 102, "眼饰" },
		{ 103, "耳环" },
		{ 104, "上衣" },
		{ 105, "套服" },
		{ 106, "裤子" },
		{ 107, "鞋子" },
		{ 108, "手套" },
		{ 109, "盾牌" },
		{ 110, "披风" },
		{ 111, "戒指" },
		{ 112, "项环" },
		{ 113, "腰带" },
		{ 114, "勋章" },
		{ 115, "护肩" },
		{ 118, "纹章" },
		{ 119, "徽章" },
		{ 130, "单手剑" },
		{ 131, "单手斧" },
		{ 132, "单手钝器" },
		{ 133, "短刀" },
		{ 134, "副刀" },
		{ 137, "短杖" },
		{ 138, "长杖" },
		{ 140, "双手剑" },
		{ 141, "双手斧" },
		{ 142, "双手钝器" },
		{ 143, "长枪" },
		{ 144, "枪戟" },
		{ 145, "弓" },
		{ 146, "弩" },
		{ 147, "拳套" },
		{ 148, "指虎" },
		{ 149, "短枪" },
		{ 167, "心脏" },
		{ 170, "扇子" },
		{ 180, "头饰" }
	};

	private static readonly string[] EquipDirs = new string[14]
	{
		"Cap", "Coat", "Longcoat", "Pants", "Shoes", "Glove", "Shield", "Cape", "Ring", "Accessory",
		"PetEquip", "Taming", "Dragon", "Weapon"
	};

	private static readonly string[] MobStatKeys = new string[11]
	{
		"level", "exp", "maxHP", "maxMP", "PADamage", "PDDamage", "MADamage", "MDDamage", "acc", "eva",
		"speed"
	};

	private static List<MobEntry> mobEntries;

	private static readonly Dictionary<int, string> JobNames = new Dictionary<int, string>
	{
		{ 0, "初心者" },
		{ 100, "战士" },
		{ 110, "剑客" },
		{ 111, "勇士" },
		{ 112, "英雄" },
		{ 120, "准骑士" },
		{ 121, "骑士" },
		{ 122, "圣骑士" },
		{ 130, "枪战士" },
		{ 131, "龙骑士" },
		{ 132, "黑骑士" },
		{ 200, "魔法师" },
		{ 210, "法师(火毒)" },
		{ 211, "魔法师(火毒)" },
		{ 212, "大魔导师(火毒)" },
		{ 220, "法师(冰雷)" },
		{ 221, "魔法师(冰雷)" },
		{ 222, "大魔导师(冰雷)" },
		{ 230, "牧师" },
		{ 231, "祭司" },
		{ 232, "主教" },
		{ 233, "主教(扩展)" },
		{ 300, "弓箭手" },
		{ 310, "猎人" },
		{ 311, "射手" },
		{ 312, "箭神" },
		{ 320, "弩弓手" },
		{ 321, "游侠" },
		{ 322, "神射手" },
		{ 400, "飞侠" },
		{ 410, "刺客" },
		{ 411, "无影人" },
		{ 412, "夜使者" },
		{ 420, "侠客" },
		{ 421, "独行客" },
		{ 422, "暗影神偷" },
		{ 430, "见习刀客" },
		{ 431, "双刀客" },
		{ 432, "血刀" },
		{ 433, "暗影双刀" },
		{ 434, "暗影双刀" },
		{ 500, "海盗" },
		{ 510, "拳手" },
		{ 511, "斗士" },
		{ 512, "冲锋队长" },
		{ 520, "枪手" },
		{ 521, "大副" },
		{ 522, "船长" },
		{ 1000, "初心者(骑士团)" },
		{ 1100, "魂骑士" },
		{ 1110, "魂骑士" },
		{ 1111, "魂骑士" },
		{ 1112, "魂骑士" },
		{ 1200, "炎术士" },
		{ 1210, "炎术士" },
		{ 1211, "炎术士" },
		{ 1212, "炎术士" },
		{ 1300, "风行者" },
		{ 1310, "风行者" },
		{ 1311, "风行者" },
		{ 1312, "风行者" },
		{ 1400, "夜行者" },
		{ 1410, "夜行者" },
		{ 1411, "夜行者" },
		{ 1412, "夜行者" },
		{ 1500, "奇袭者" },
		{ 1510, "奇袭者" },
		{ 1511, "奇袭者" },
		{ 1512, "奇袭者" },
		{ 2000, "战神(初心者)" },
		{ 2100, "战神" },
		{ 2110, "战神" },
		{ 2111, "战神" },
		{ 2112, "战神" },
		{ 2200, "龙神(初心者)" },
		{ 2210, "龙神" },
		{ 2211, "龙神" },
		{ 2212, "龙神" },
		{ 2213, "龙神" },
		{ 2214, "龙神" },
		{ 2215, "龙神" },
		{ 2216, "龙神" },
		{ 2217, "龙神" },
		{ 2218, "龙神" },
		{ 3000, "双弩精灵(初心者)" },
		{ 3100, "双弩精灵" },
		{ 3110, "双弩精灵" },
		{ 3111, "双弩精灵" },
		{ 3112, "双弩精灵" },
		{ 4000, "幻影(初心者)" },
		{ 4100, "幻影" },
		{ 4110, "幻影" },
		{ 4111, "幻影" },
		{ 4112, "幻影" },
		{ 5000, "隐月(初心者)" },
		{ 5100, "隐月" },
		{ 5110, "隐月" },
		{ 5111, "隐月" },
		{ 5112, "隐月" },
		{ 800, "管理员" },
		{ 900, "GM" },
		{ 910, "超级GM" }
	};

	private static readonly Dictionary<int, int> JobAdvancement = new Dictionary<int, int>
	{
		{ 0, 0 },
		{ 100, 1 },
		{ 200, 1 },
		{ 300, 1 },
		{ 400, 1 },
		{ 500, 1 },
		{ 110, 2 },
		{ 120, 2 },
		{ 130, 2 },
		{ 210, 2 },
		{ 220, 2 },
		{ 230, 2 },
		{ 310, 2 },
		{ 320, 2 },
		{ 410, 2 },
		{ 420, 2 },
		{ 430, 2 },
		{ 431, 2 },
		{ 510, 2 },
		{ 520, 2 },
		{ 111, 3 },
		{ 121, 3 },
		{ 131, 3 },
		{ 211, 3 },
		{ 221, 3 },
		{ 231, 3 },
		{ 311, 3 },
		{ 321, 3 },
		{ 411, 3 },
		{ 421, 3 },
		{ 432, 3 },
		{ 433, 3 },
		{ 511, 3 },
		{ 521, 3 },
		{ 112, 4 },
		{ 122, 4 },
		{ 132, 4 },
		{ 212, 4 },
		{ 222, 4 },
		{ 232, 4 },
		{ 312, 4 },
		{ 322, 4 },
		{ 412, 4 },
		{ 422, 4 },
		{ 434, 4 },
		{ 512, 4 },
		{ 522, 4 },
		{ 1000, 0 },
		{ 1100, 1 },
		{ 1200, 1 },
		{ 1300, 1 },
		{ 1400, 1 },
		{ 1500, 1 },
		{ 1110, 2 },
		{ 1210, 2 },
		{ 1310, 2 },
		{ 1410, 2 },
		{ 1510, 2 },
		{ 1111, 3 },
		{ 1211, 3 },
		{ 1311, 3 },
		{ 1411, 3 },
		{ 1511, 3 },
		{ 1112, 4 },
		{ 1212, 4 },
		{ 1312, 4 },
		{ 1412, 4 },
		{ 1512, 4 },
		{ 2000, 0 },
		{ 2100, 1 },
		{ 2110, 2 },
		{ 2111, 3 },
		{ 2112, 4 },
		{ 2200, 0 },
		{ 2210, 1 },
		{ 2211, 2 },
		{ 2212, 3 },
		{ 2213, 4 },
		{ 2214, 5 },
		{ 2215, 6 },
		{ 2216, 7 },
		{ 2217, 8 },
		{ 2218, 9 },
		{ 3000, 0 },
		{ 3100, 1 },
		{ 3110, 2 },
		{ 3111, 3 },
		{ 3112, 4 },
		{ 4000, 0 },
		{ 4100, 1 },
		{ 4110, 2 },
		{ 4111, 3 },
		{ 4112, 4 },
		{ 5000, 0 },
		{ 5100, 1 },
		{ 5110, 2 },
		{ 5111, 3 },
		{ 5112, 4 },
		{ 800, 0 },
		{ 900, 0 },
		{ 910, 0 },
		{ 233, 4 }
	};

	private static Dictionary<long, SkillEntry> skillEntries;

	private static List<SkillEntry> skillSorted;

	private static List<object> skillJobFacet;

	private static string skillLoadError;

	private static readonly Dictionary<long, string> questNames = new Dictionary<long, string>();

	private static readonly object iconGate = new object();

	private static readonly Dictionary<string, byte[]> iconCache = new Dictionary<string, byte[]>();

	private const int IconCacheMax = 512;

	public static void Prewarm()
	{
		Thread thread = new Thread(() =>
		{
			try
			{
				EnsureItems();
			}
			catch
			{
			}
			try
			{
				EnsureEquips();
			}
			catch
			{
			}
			try
			{
				EnsureMobs();
			}
			catch
			{
			}
			try
			{
				EnsureSkills();
			}
			catch
			{
			}
		});
		thread.IsBackground = true;
		thread.Name = "AdminCatalogPrewarm";
		thread.Start();
	}

	public static List<object> InformationSearch(object body)
	{
		string text = AdminJson.GetString(body, "filter");
		if (string.IsNullOrEmpty(text))
		{
			throw new AdminApiException("filter不能为空");
		}
		List<object> list = AdminJson.GetList(body, "types");
		if (list == null || list.Count == 0)
		{
			list = new List<object> { "cash", "consume", "eqp", "etc", "ins", "map", "mob", "npc", "pet", "skill" };
		}
		int filterType = AdminJson.GetInt(body, "filterType");
		bool full = AdminJson.GetBool(body, "fullMatch");
		List<object> list2 = new List<object>();
		foreach (object item in list)
		{
			string text2 = Convert.ToString(item, CultureInfo.InvariantCulture);
			ImgNode imgNode = OpenRoot("String/" + text2 switch
			{
				"cash" => "Cash", 
				"consume" => "Consume", 
				"eqp" => "Eqp", 
				"etc" => "Etc", 
				"ins" => "Ins", 
				"map" => "Map", 
				"mob" => "Mob", 
				"npc" => "Npc", 
				"pet" => "Pet", 
				"skill" => "Skill", 
				_ => throw new AdminApiException("不支持的资料类型: " + text2), 
			} + ".img");
			if (text2 == "etc")
			{
				imgNode = imgNode.Get("Etc");
			}
			if (text2 == "eqp")
			{
				imgNode = imgNode.Get("Eqp");
			}
			if (text2 == "map" || text2 == "eqp")
			{
				foreach (ImgNode child in imgNode.Children)
				{
					AddInformation(list2, text2, child, text, filterType, full);
				}
			}
			else
			{
				AddInformation(list2, text2, imgNode, text, filterType, full);
			}
		}
		return list2;
	}

	private static void AddInformation(List<object> results, string type, ImgNode root, string filter, int filterType, bool full)
	{
		foreach (ImgNode child in root.Children)
		{
			if (int.TryParse(child.Name, out var result))
			{
				string text = child.Text((type == "map") ? "mapName" : "name");
				bool num = (filterType == 0 || filterType == 1) && (full ? (child.Name == filter) : child.Name.Contains(filter));
				bool flag = (filterType == 0 || filterType == 2) && (full ? (text == filter) : text.Contains(filter));
				if (num | flag)
				{
					results.Add(AdminJson.Obj("type", type, "id", result, "name", text, "desc", child.Text((type == "map") ? "streetName" : "desc")));
				}
			}
		}
	}

	public static string Normalize(string keyword)
	{
		if (!string.IsNullOrEmpty(keyword))
		{
			return keyword.Trim().ToLowerInvariant();
		}
		return "";
	}

	public static bool Matches(long id, string name, string query)
	{
		if (query.Length == 0)
		{
			return true;
		}
		if (id.ToString(CultureInfo.InvariantCulture).IndexOf(query, StringComparison.Ordinal) < 0)
		{
			return (name ?? "").ToLowerInvariant().IndexOf(query, StringComparison.Ordinal) >= 0;
		}
		return true;
	}

	public static int MatchRank(long id, string name, string query)
	{
		if (query.Length == 0)
		{
			return 0;
		}
		string text = id.ToString(CultureInfo.InvariantCulture);
		string text2 = (name ?? "").ToLowerInvariant();
		if (text == query || text2 == query)
		{
			return 0;
		}
		if (text.StartsWith(query, StringComparison.Ordinal))
		{
			return 1;
		}
		if (!text2.StartsWith(query, StringComparison.Ordinal))
		{
			return 3;
		}
		return 2;
	}

	private static CatalogPage Slice<T>(List<T> matched, int pageNo, int pageSize, Func<T, object> map)
	{
		int num = ((pageSize <= 0) ? 20 : pageSize);
		int num2 = ((pageNo <= 0) ? 1 : pageNo);
		List<object> list = new List<object>();
		long num3 = (long)(num2 - 1) * (long)num;
		for (long num4 = num3; num4 < matched.Count && num4 < num3 + num; num4++)
		{
			list.Add(map(matched[(int)num4]));
		}
		return new CatalogPage(list, num2, num, matched.Count);
	}

	private static ImgNode OpenRoot(string relative)
	{
		ImgFile imgFile = new ImgFile(Path.Combine(WzAssets.DataRoot, relative));
		try
		{
			return imgFile.Root;
		}
		finally
		{
			imgFile.Dispose();
		}
	}

	private static bool FileReady(string relative)
	{
		WarmUpHasFile();
		return WzAssets.HasFile(relative);
	}

	private static void WarmUpHasFile()
	{
		if (!hasFileWarmed)
		{
			AdminMainThread.Run(() => WzAssets.HasFile("String/Mob.img"));
			hasFileWarmed = true;
		}
	}

	private static void CollectScalars(ImgNode node, Dictionary<string, object> into, bool numbersOnly)
	{
		if (node == null)
		{
			return;
		}
		foreach (ImgNode child in node.Children)
		{
			if (child.Kind == null && child.Value != null)
			{
				object value = child.Value;
				if (!numbersOnly || value is int || value is long || value is short || value is byte || value is sbyte || value is float || value is double)
				{
					into[child.Name] = value;
				}
			}
		}
	}

	private static int ScalarInt(ImgNode node, string key, int fallback = 0)
	{
		object obj = node.Get(key)?.Value;
		if (obj == null)
		{
			return fallback;
		}
		try
		{
			return Convert.ToInt32(obj, CultureInfo.InvariantCulture);
		}
		catch
		{
			return fallback;
		}
	}

	private static List<string> FileIndex()
	{
		if (fileIndex != null)
		{
			return fileIndex;
		}
		List<string> list = new List<string>();
		try
		{
			foreach (object item2 in AdminMainThread.Run(() => WzAssets.Index["files"] as List<object>))
			{
				if (item2 is Dictionary<string, object> dictionary && dictionary.TryGetValue("path", out var value) && value is string item)
				{
					list.Add(item);
				}
			}
		}
		catch
		{
			list = null;
		}
		fileIndex = list ?? new List<string>();
		return fileIndex;
	}

	private static List<string> EnumerateImgNames(string dir, bool numericOnly)
	{
		List<string> list = new List<string>();
		string text = dir + "/";
		foreach (string item in FileIndex())
		{
			if (item.StartsWith(text, StringComparison.Ordinal))
			{
				string text2 = item.Substring(text.Length);
				if (text2.IndexOf('/') < 0 && text2.EndsWith(".img", StringComparison.OrdinalIgnoreCase) && (!numericOnly || IsNumericName(text2, 0, text2.Length - 4)))
				{
					list.Add(text2);
				}
			}
		}
		if (list.Count == 0)
		{
			try
			{
				string[] files = Directory.GetFiles(Path.Combine(WzAssets.DataRoot, dir), "*.img");
				for (int i = 0; i < files.Length; i++)
				{
					string fileName = Path.GetFileName(files[i]);
					if (!numericOnly || IsNumericName(fileName, 0, fileName.Length - 4))
					{
						list.Add(fileName);
					}
				}
			}
			catch
			{
			}
		}
		return list;
	}

	private static bool IsNumericName(string s, int start, int end)
	{
		if (end <= start)
		{
			return false;
		}
		for (int i = start; i < end; i++)
		{
			if (s[i] < '0' || s[i] > '9')
			{
				return false;
			}
		}
		return true;
	}

	private static void EnsureItems()
	{
		if (itemEntries != null)
		{
			return;
		}
		lock (buildGate)
		{
			if (itemEntries != null)
			{
				return;
			}
			List<ItemEntry> list = new List<ItemEntry>();
			AddStringItems(list, "String/Consume.img", null, "Consume");
			AddStringItems(list, "String/Etc.img", "Etc", "Etc");
			AddStringItems(list, "String/Ins.img", null, "Install");
			AddStringItems(list, "String/Cash.img", null, "Cash");
			foreach (string item in EnumerateImgNames("Item/Special", numericOnly: false))
			{
				ImgNode imgNode;
				try
				{
					imgNode = OpenRoot("Item/Special/" + item);
				}
				catch
				{
					continue;
				}
				foreach (ImgNode child in imgNode.Children)
				{
					if (long.TryParse(child.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
					{
						ItemEntry itemEntry = new ItemEntry
						{
							Id = result,
							Name = "Item " + child.Name,
							Description = "",
							Category = "Special",
							Icon = (child.Get("info/icon") != null || child.Get("icon") != null)
						};
						CollectScalars(child.Get("info"), itemEntry.Specs, numbersOnly: false);
						CollectScalars(child.Get("spec"), itemEntry.Specs, numbersOnly: false);
						list.Add(itemEntry);
					}
				}
			}
			list.Sort((ItemEntry a, ItemEntry b) => a.Id.CompareTo(b.Id));
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			string[] itemCategoryKeys = ItemCategoryKeys;
			foreach (string key in itemCategoryKeys)
			{
				dictionary[key] = 0;
			}
			foreach (ItemEntry item2 in list)
			{
				dictionary[item2.Category] = dictionary[item2.Category] + 1;
			}
			List<object> list2 = new List<object>();
			itemCategoryKeys = ItemCategoryKeys;
			foreach (string text in itemCategoryKeys)
			{
				list2.Add(AdminJson.Obj("key", text, "count", dictionary[text]));
			}
			itemCategoryFacet = list2;
			itemEntries = list;
		}
	}

	private static void AddStringItems(List<ItemEntry> entries, string file, string subNode, string category)
	{
		ImgNode imgNode;
		try
		{
			imgNode = OpenRoot(file);
		}
		catch
		{
			return;
		}
		ImgNode imgNode2 = ((subNode == null) ? imgNode : imgNode.Get(subNode));
		if (imgNode2 == null)
		{
			return;
		}
		List<KeyValuePair<long, ImgNode>> list = new List<KeyValuePair<long, ImgNode>>();
		foreach (ImgNode child in imgNode2.Children)
		{
			if (long.TryParse(child.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
			{
				list.Add(new KeyValuePair<long, ImgNode>(result, child));
			}
		}
		string text = category switch
		{
			"Install" => "Install", 
			"Etc" => "Etc", 
			"Consume" => "Consume", 
			_ => "Cash", 
		};
		Dictionary<long, ImgNode> dictionary = new Dictionary<long, ImgNode>();
		foreach (KeyValuePair<long, ImgNode> item in list)
		{
			long key = item.Key / 10000;
			if (!dictionary.TryGetValue(key, out var value))
			{
				string relative = "Item/" + text + "/" + key.ToString("D4", CultureInfo.InvariantCulture) + ".img";
				value = null;
				if (FileReady(relative))
				{
					try
					{
						value = OpenRoot(relative);
					}
					catch
					{
						value = null;
					}
				}
				dictionary[key] = value;
			}
			ItemEntry itemEntry = new ItemEntry
			{
				Id = item.Key,
				Name = item.Value.Text("name", "Item " + item.Value.Name),
				Description = item.Value.Text("desc"),
				Category = category
			};
			if (value != null)
			{
				ImgNode imgNode3 = value.Get(item.Key.ToString("D8", CultureInfo.InvariantCulture)) ?? value.Get(item.Key.ToString(CultureInfo.InvariantCulture));
				if (imgNode3 != null)
				{
					CollectScalars(imgNode3.Get("info"), itemEntry.Specs, numbersOnly: false);
					CollectScalars(imgNode3.Get("spec"), itemEntry.Specs, numbersOnly: false);
					itemEntry.Icon = imgNode3.Get("info/icon") != null || imgNode3.Get("icon") != null;
				}
			}
			entries.Add(itemEntry);
		}
	}

	public static CatalogPage ItemCatalog(string keyword, string category, int pageNo, int pageSize)
	{
		EnsureItems();
		string query = Normalize(keyword);
		string text = (string.IsNullOrEmpty(category) ? "" : category.Trim());
		List<ItemEntry> list = new List<ItemEntry>();
		foreach (ItemEntry itemEntry in itemEntries)
		{
			if ((text.Length <= 0 || !(itemEntry.Category != text)) && Matches(itemEntry.Id, itemEntry.Name, query))
			{
				list.Add(itemEntry);
			}
		}
		if (query.Length > 0)
		{
			list.Sort((ItemEntry a, ItemEntry b) =>
			{
				int num = MatchRank(a.Id, a.Name, query).CompareTo(MatchRank(b.Id, b.Name, query));
				return (num == 0) ? a.Id.CompareTo(b.Id) : num;
			});
		}
		return Slice(list, pageNo, Clamp(pageSize, 60, 120), ItemDto).Extra("categories", itemCategoryFacet);
	}

	private static Dictionary<string, object> ItemDto(ItemEntry e)
	{
		return AdminJson.Obj("id", e.Id, "name", e.Name, "description", e.Description, "category", e.Category, "specs", e.Specs, "iconAvailable", e.Icon);
	}

	private static int Clamp(int value, int fallback, int max)
	{
		if (value <= 0)
		{
			return fallback;
		}
		if (value <= max)
		{
			return value;
		}
		return max;
	}

	private static void EnsureEquips()
	{
		if (equipEntries != null)
		{
			return;
		}
		lock (buildGate)
		{
			if (equipEntries != null)
			{
				return;
			}
			Dictionary<long, string[]> dictionary = LoadEquipStrings();
			List<EquipEntry> list = new List<EquipEntry>();
			foreach (KeyValuePair<string, long> item in EnumerateEquipFiles())
			{
				string key = item.Key;
				long value = item.Value;
				ImgNode imgNode = null;
				ImgNode imgNode2 = null;
				try
				{
					imgNode = OpenRoot("Character/" + key + "/" + value.ToString("D8", CultureInfo.InvariantCulture) + ".img");
					imgNode2 = imgNode.Get("info");
				}
				catch
				{
					continue;
				}
				if (imgNode2 != null)
				{
					EquipEntry equipEntry = new EquipEntry
					{
						Id = value,
						Name = "",
						Description = "",
						Category = key,
						ReqJob = ScalarInt(imgNode2, "reqJob"),
						ReqLevel = ScalarInt(imgNode2, "reqLevel"),
						Cash = (ScalarInt(imgNode2, "cash") == 1),
						Icon = (imgNode2.Get("icon") != null || imgNode.Get("icon") != null)
					};
					dictionary.TryGetValue(value, out var value2);
					if (value2 != null)
					{
						equipEntry.Name = value2[0];
						equipEntry.Description = value2[1];
					}
					CollectScalars(imgNode2, equipEntry.Stats, numbersOnly: true);
					EquipKinds.TryGetValue((int)(value / 10000), out equipEntry.Kind);
					list.Add(equipEntry);
				}
			}
			list.Sort((EquipEntry a, EquipEntry b) => a.Id.CompareTo(b.Id));
			Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
			foreach (EquipEntry item2 in list)
			{
				dictionary2[item2.Category] = ((!dictionary2.TryGetValue(item2.Category, out var value3)) ? 1 : (value3 + 1));
			}
			List<object> list2 = new List<object>();
			foreach (string key2 in dictionary2.Keys)
			{
				list2.Add(AdminJson.Obj("key", key2, "count", dictionary2[key2]));
			}
			list2.Sort((object a, object b) => string.CompareOrdinal((string)AdminJson.Get(a, "key"), (string)AdminJson.Get(b, "key")));
			equipCategoryFacet = list2;
			equipEntries = list;
		}
	}

	private static Dictionary<long, string[]> LoadEquipStrings()
	{
		Dictionary<long, string[]> dictionary = new Dictionary<long, string[]>();
		ImgNode imgNode;
		try
		{
			imgNode = OpenRoot("String/Eqp.img");
		}
		catch
		{
			return dictionary;
		}
		foreach (ImgNode child in (imgNode.Get("Eqp") ?? imgNode).Children)
		{
			foreach (ImgNode child2 in child.Children)
			{
				if (long.TryParse(child2.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) && !dictionary.ContainsKey(result))
				{
					dictionary[result] = new string[2]
					{
						child2.Text("name"),
						child2.Text("desc")
					};
				}
			}
		}
		return dictionary;
	}

	private static List<KeyValuePair<string, long>> EnumerateEquipFiles()
	{
		List<KeyValuePair<string, long>> list = new List<KeyValuePair<string, long>>();
		HashSet<string> hashSet = new HashSet<string>(EquipDirs);
		foreach (string item in FileIndex())
		{
			if (!item.StartsWith("Character/", StringComparison.Ordinal))
			{
				continue;
			}
			string text = item.Substring("Character/".Length);
			int num = text.IndexOf('/');
			if (num > 0 && text.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
			{
				string text2 = text.Substring(0, num);
				string text3 = text.Substring(num + 1);
				if (hashSet.Contains(text2) && text3.Length == 12 && IsNumericName(text3, 0, 8) && long.TryParse(text3.Substring(0, 8), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
				{
					list.Add(new KeyValuePair<string, long>(text2, result));
				}
			}
		}
		if (list.Count == 0)
		{
			string[] equipDirs = EquipDirs;
			foreach (string text4 in equipDirs)
			{
				string path = Path.Combine(WzAssets.DataRoot, "Character", text4);
				string[] files;
				try
				{
					files = Directory.GetFiles(path, "*.img");
				}
				catch
				{
					continue;
				}
				string[] array = files;
				for (int j = 0; j < array.Length; j++)
				{
					string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(array[j]);
					if (fileNameWithoutExtension.Length == 8 && IsNumericName(fileNameWithoutExtension, 0, 8) && long.TryParse(fileNameWithoutExtension, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result2))
					{
						list.Add(new KeyValuePair<string, long>(text4, result2));
					}
				}
			}
		}
		return list;
	}

	public static CatalogPage EquipmentCatalog(string keyword, string category, int pageNo, int pageSize, bool? cash, string weaponType, int? job, int? minLevel, int? maxLevel)
	{
		EnsureEquips();
		string query = Normalize(keyword);
		string text = (string.IsNullOrEmpty(category) ? "" : category.Trim());
		string text2 = (string.IsNullOrEmpty(weaponType) ? "" : weaponType.Trim());
		List<EquipEntry> list = new List<EquipEntry>();
		foreach (EquipEntry equipEntry in equipEntries)
		{
			if ((text.Length <= 0 || !(equipEntry.Category != text)) && Matches(equipEntry.Id, equipEntry.Name, query))
			{
				list.Add(equipEntry);
			}
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (EquipEntry item in list)
		{
			if ((!cash.HasValue || item.Cash == cash.Value) && item.Kind != null)
			{
				dictionary[item.Kind] = ((!dictionary.TryGetValue(item.Kind, out var value)) ? 1 : (value + 1));
			}
		}
		List<object> list2 = new List<object>();
		foreach (KeyValuePair<string, int> item2 in dictionary)
		{
			list2.Add(AdminJson.Obj("key", item2.Key, "count", item2.Value));
		}
		list2.Sort((object a, object b) => string.CompareOrdinal((string)AdminJson.Get(a, "key"), (string)AdminJson.Get(b, "key")));
		long num = 0L;
		long num2 = 0L;
		foreach (EquipEntry item3 in list)
		{
			if (text2.Length <= 0 || !(item3.Kind != text2))
			{
				if (item3.Cash)
				{
					num++;
				}
				else
				{
					num2++;
				}
			}
		}
		List<EquipEntry> list3 = new List<EquipEntry>();
		foreach (EquipEntry item4 in list)
		{
			if ((!cash.HasValue || item4.Cash == cash.Value) && (text2.Length <= 0 || !(item4.Kind != text2)) && (!job.HasValue || job.Value == 0 || item4.ReqJob == 0 || (item4.ReqJob & job.Value) != 0) && (!minLevel.HasValue || item4.ReqLevel >= minLevel.Value) && (!maxLevel.HasValue || item4.ReqLevel <= maxLevel.Value))
			{
				list3.Add(item4);
			}
		}
		if (query.Length > 0)
		{
			list3.Sort((EquipEntry a, EquipEntry b) =>
			{
				int num3 = MatchRank(a.Id, a.Name, query).CompareTo(MatchRank(b.Id, b.Name, query));
				return (num3 == 0) ? a.Id.CompareTo(b.Id) : num3;
			});
		}
		return Slice(list3, pageNo, Clamp(pageSize, 60, 120), EquipDto).Extra("categories", equipCategoryFacet).Extra("weaponTypes", list2).Extra("cashCount", num)
			.Extra("nonCashCount", num2);
	}

	private static Dictionary<string, object> EquipDto(EquipEntry e)
	{
		return AdminJson.Obj("id", e.Id, "name", e.Name, "description", e.Description, "category", e.Category, "stats", e.Stats, "iconAvailable", e.Icon);
	}

	private static void EnsureMobs()
	{
		if (mobEntries != null)
		{
			return;
		}
		lock (buildGate)
		{
			if (mobEntries != null)
			{
				return;
			}
			List<MobEntry> list = new List<MobEntry>();
			ImgNode imgNode;
			try
			{
				imgNode = OpenRoot("String/Mob.img");
			}
			catch
			{
				imgNode = null;
			}
			if (imgNode != null)
			{
				foreach (ImgNode child in imgNode.Children)
				{
					if (!long.TryParse(child.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
					{
						continue;
					}
					MobEntry mobEntry = new MobEntry
					{
						Id = result,
						Name = child.Text("name", "Mob " + result)
					};
					ImgNode imgNode2 = MobInfo(result, out var resolvedRoot);
					if (imgNode2 != null)
					{
						Dictionary<string, object> dictionary = new Dictionary<string, object>();
						CollectScalars(imgNode2, dictionary, numbersOnly: true);
						string[] mobStatKeys = MobStatKeys;
						foreach (string key in mobStatKeys)
						{
							if (dictionary.TryGetValue(key, out var value))
							{
								mobEntry.Stats[key] = value;
							}
						}
						mobEntry.Level = ScalarInt(imgNode2, "level");
						mobEntry.Icon = resolvedRoot != null && resolvedRoot.Get("stand/0") != null;
					}
					list.Add(mobEntry);
				}
			}
			list.Sort((MobEntry a, MobEntry b) => a.Id.CompareTo(b.Id));
			mobEntries = list;
		}
	}

	private static ImgNode MobInfo(long id, out ImgNode resolvedRoot)
	{
		resolvedRoot = null;
		string relative = "Mob/" + id.ToString("D7", CultureInfo.InvariantCulture) + ".img";
		if (!FileReady(relative))
		{
			return null;
		}
		ImgNode imgNode;
		try
		{
			imgNode = OpenRoot(relative);
		}
		catch
		{
			return null;
		}
		for (int i = 0; i < 64; i++)
		{
			ImgNode imgNode2 = imgNode.Get("info/link");
			if (imgNode2 == null || imgNode2.Value == null)
			{
				break;
			}
			long num;
			try
			{
				num = Convert.ToInt64(imgNode2.Value, CultureInfo.InvariantCulture);
			}
			catch
			{
				break;
			}
			string relative2 = "Mob/" + num.ToString("D7", CultureInfo.InvariantCulture) + ".img";
			if (!FileReady(relative2))
			{
				return null;
			}
			try
			{
				imgNode = OpenRoot(relative2);
			}
			catch
			{
				return null;
			}
		}
		resolvedRoot = imgNode;
		return imgNode.Get("info");
	}

	public static CatalogPage MobCatalog(string keyword, int pageNo, int pageSize, int? minLevel, int? maxLevel)
	{
		EnsureMobs();
		string query = Normalize(keyword);
		List<MobEntry> list = new List<MobEntry>();
		foreach (MobEntry mobEntry in mobEntries)
		{
			if (Matches(mobEntry.Id, mobEntry.Name, query) && (!minLevel.HasValue || mobEntry.Level >= minLevel.Value) && (!maxLevel.HasValue || mobEntry.Level <= maxLevel.Value))
			{
				list.Add(mobEntry);
			}
		}
		if (query.Length > 0)
		{
			list.Sort((MobEntry a, MobEntry b) =>
			{
				int num = MatchRank(a.Id, a.Name, query).CompareTo(MatchRank(b.Id, b.Name, query));
				return (num == 0) ? a.Id.CompareTo(b.Id) : num;
			});
		}
		return Slice(list, pageNo, Clamp(pageSize, 60, 120), MobDto);
	}

	private static Dictionary<string, object> MobDto(MobEntry e)
	{
		return AdminJson.Obj("id", e.Id, "name", e.Name, "stats", e.Stats, "iconAvailable", e.Icon);
	}

	private static int AdvancementOf(int jobId)
	{
		if (!JobAdvancement.TryGetValue(jobId, out var value))
		{
			return 0;
		}
		return value;
	}

	private static string JobNameOf(int jobId)
	{
		if (!JobNames.TryGetValue(jobId, out var value))
		{
			return "职业 " + jobId;
		}
		return value;
	}

	private static void EnsureSkills()
	{
		if (skillEntries != null)
		{
			return;
		}
		lock (buildGate)
		{
			if (skillEntries != null)
			{
				return;
			}
			skillLoadError = null;
			try
			{
				BuildSkills();
			}
			catch (Exception ex)
			{
				skillLoadError = ex.GetType().Name + ": " + ex.Message;
				skillEntries = skillEntries ?? new Dictionary<long, SkillEntry>();
				skillSorted = skillSorted ?? new List<SkillEntry>();
				skillJobFacet = skillJobFacet ?? new List<object>();
			}
		}
	}

	private static void BuildSkills()
	{
		Dictionary<long, ImgNode> dictionary = new Dictionary<long, ImgNode>();
		try
		{
			foreach (ImgNode child in OpenRoot("String/Skill.img").Children)
			{
				if (child.Name.Length == 7 && IsNumericName(child.Name, 0, 7) && long.TryParse(child.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
				{
					dictionary[result] = child;
				}
			}
		}
		catch
		{
		}
		Dictionary<long, SkillEntry> dictionary2 = new Dictionary<long, SkillEntry>();
		foreach (string item in EnumerateImgNames("Skill", numericOnly: true))
		{
			if (!int.TryParse(item.Substring(0, item.Length - 4), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result2))
			{
				continue;
			}
			ImgNode imgNode;
			try
			{
				imgNode = OpenRoot("Skill/" + item);
			}
			catch
			{
				continue;
			}
			ImgNode imgNode2 = imgNode.Get("skill");
			if (imgNode2 == null)
			{
				continue;
			}
			foreach (ImgNode child2 in imgNode2.Children)
			{
				if (!long.TryParse(child2.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result3) || dictionary2.ContainsKey(result3))
				{
					continue;
				}
				SkillEntry skillEntry = new SkillEntry
				{
					Id = result3,
					JobId = (int)(result3 / 10000),
					FileJob = result2,
					Icon = (child2.Get("icon") != null)
				};
				dictionary.TryGetValue(result3, out var value);
				if (value != null)
				{
					skillEntry.Name = value.Text("name");
					skillEntry.Desc = value.Text("desc");
					for (int i = 1; i <= 30; i++)
					{
						string text = value.Text("h" + i).Trim();
						if (text.Length > 0)
						{
							skillEntry.LevelDescs.Add(text);
						}
					}
				}
				else
				{
					skillEntry.Name = "";
					skillEntry.Desc = "";
				}
				ImgNode imgNode3 = child2.Get("level");
				if (imgNode3 != null)
				{
					int num = 0;
					ImgNode imgNode4 = null;
					foreach (ImgNode child3 in imgNode3.Children)
					{
						if (int.TryParse(child3.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result4))
						{
							if (result4 > num)
							{
								num = result4;
							}
							if (result4 == 1)
							{
								imgNode4 = child3;
							}
						}
					}
					if (imgNode4 == null)
					{
						foreach (ImgNode child4 in imgNode3.Children)
						{
							if (int.TryParse(child4.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var _))
							{
								imgNode4 = child4;
								break;
							}
						}
					}
					skillEntry.MaxLevel = num;
					if (imgNode4 != null)
					{
						CollectScalars(imgNode4, skillEntry.Attributes, numbersOnly: false);
					}
				}
				ImgNode imgNode5 = child2.Get("elemAttr");
				if (imgNode5 != null && imgNode5.Value is string { Length: >0 } text2)
				{
					skillEntry.Attributes["elemAttr"] = text2;
				}
				ImgNode imgNode6 = child2.Get("skillType");
				if (imgNode6 != null && imgNode6.Value != null && imgNode6.Kind == null)
				{
					skillEntry.Attributes["skillType"] = imgNode6.Value;
				}
				skillEntry.JobName = JobNameOf(skillEntry.JobId);
				dictionary2[result3] = skillEntry;
			}
		}
		List<SkillEntry> list = new List<SkillEntry>(dictionary2.Values);
		list.Sort((SkillEntry a, SkillEntry b) =>
		{
			int num2 = a.JobId.CompareTo(b.JobId);
			return (num2 == 0) ? a.Id.CompareTo(b.Id) : num2;
		});
		Dictionary<int, int> dictionary3 = new Dictionary<int, int>();
		foreach (SkillEntry item2 in list)
		{
			dictionary3[item2.JobId] = ((!dictionary3.TryGetValue(item2.JobId, out var value2)) ? 1 : (value2 + 1));
		}
		List<int> list2 = new List<int>(dictionary3.Keys);
		list2.Sort();
		List<object> list3 = new List<object>();
		foreach (int item3 in list2)
		{
			list3.Add(AdminJson.Obj("jobId", item3, "jobName", JobNameOf(item3), "count", dictionary3[item3]));
		}
		skillJobFacet = list3;
		skillSorted = list;
		skillEntries = dictionary2;
	}

	public static CatalogPage SkillCatalog(string keyword, int? jobId, int? advancement, int pageNo, int pageSize)
	{
		EnsureSkills();
		string text = (string.IsNullOrEmpty(keyword) ? "" : keyword.Trim().ToLowerInvariant());
		List<SkillEntry> list = new List<SkillEntry>();
		foreach (SkillEntry item in skillSorted)
		{
			if ((!jobId.HasValue || item.JobId == jobId.Value) && (!advancement.HasValue || AdvancementOf(item.JobId) == advancement.Value) && (text.Length <= 0 || item.Id.ToString(CultureInfo.InvariantCulture).IndexOf(text, StringComparison.Ordinal) >= 0 || (item.Name ?? "").ToLowerInvariant().IndexOf(text, StringComparison.Ordinal) >= 0 || (item.JobName ?? "").ToLowerInvariant().IndexOf(text, StringComparison.Ordinal) >= 0))
			{
				list.Add(item);
			}
		}
		return Slice(list, pageNo, Clamp(pageSize, 120, 500), SkillDto).Extra("jobs", skillJobFacet).Extra("loaded", skillLoadError == null).Extra("message", skillLoadError ?? "");
	}

	public static Dictionary<string, object> SkillDetail(long skillId)
	{
		EnsureSkills();
		if (!skillEntries.TryGetValue(skillId, out var value))
		{
			throw new AdminApiException("技能不存在: " + skillId);
		}
		return SkillDto(value);
	}

	public static List<object> SkillJobs()
	{
		EnsureSkills();
		return skillJobFacet;
	}

	public static int SkillReload()
	{
		lock (buildGate)
		{
			skillEntries = null;
			skillSorted = null;
			skillJobFacet = null;
			skillLoadError = null;
		}
		EnsureSkills();
		return skillSorted.Count;
	}

	private static Dictionary<string, object> SkillDto(SkillEntry e)
	{
		return AdminJson.Obj("id", e.Id, "name", e.Name, "desc", e.Desc, "jobName", e.JobName, "jobId", e.JobId, "maxLevel", e.MaxLevel, "iconAvailable", e.Icon, "attributes", e.Attributes, "levelDescs", e.LevelDescs);
	}

	public static string QuestName(long questId)
	{
		if (questId <= 0)
		{
			return "";
		}
		if (questNames.TryGetValue(questId, out var value))
		{
			return value;
		}
		string text = "";
		try
		{
			text = WzAssets.FileRoot("Quest/QuestInfo.img").Get(questId.ToString(CultureInfo.InvariantCulture))?.Text("name") ?? "";
		}
		catch
		{
			text = "";
		}
		questNames[questId] = text;
		return text;
	}

	public static byte[] ItemIconPng(long id)
	{
		return IconPng("item:" + id, () => NativeItemSource.IconNode((int)id));
	}

	public static byte[] EquipmentIconPng(long id)
	{
		return IconPng("equip:" + id, () => NativeItemSource.IconNode((int)id));
	}

	public static byte[] MobIconPng(long id)
	{
		return IconPng("mob:" + id, () =>
		{
			string relative = "Mob/" + id.ToString("D7", CultureInfo.InvariantCulture) + ".img";
			if (!WzAssets.HasFile(relative))
			{
				return (ImgNode)null;
			}
			ImgNode imgNode = WzAssets.FileRoot(relative);
			for (int i = 0; i < 64; i++)
			{
				ImgNode imgNode2 = imgNode.Get("info/link");
				if (imgNode2 == null || imgNode2.Value == null)
				{
					break;
				}
				string relative2 = "Mob/" + Convert.ToInt64(imgNode2.Value, CultureInfo.InvariantCulture).ToString("D7", CultureInfo.InvariantCulture) + ".img";
				if (!WzAssets.HasFile(relative2))
				{
					return (ImgNode)null;
				}
				imgNode = WzAssets.FileRoot(relative2);
			}
			return imgNode.Get("stand/0");
		});
	}

	public static byte[] NpcIconPng(long id)
	{
		return IconPng("npc:" + id, () =>
		{
			ImgNode imgNode = WzAssets.FileRoot("Npc/" + id.ToString("D7", CultureInfo.InvariantCulture) + ".img");
			for (int i = 0; i < 64; i++)
			{
				string text = imgNode.Text("info/link");
				if (string.IsNullOrEmpty(text))
				{
					break;
				}
				imgNode = WzAssets.FileRoot("Npc/" + long.Parse(text, CultureInfo.InvariantCulture).ToString("D7", CultureInfo.InvariantCulture) + ".img");
			}
			return imgNode.Get("stand/0");
		});
	}

	public static byte[] MapIconPng(long id)
	{
		return IconPng("map:" + id, () => WzAssets.FileRoot(WzAssets.MapPath((int)id)).Get("miniMap/canvas"));
	}

	public static byte[] SkillIconPng(long id)
	{
		return IconPng("skill:" + id, () =>
		{
			EnsureSkills();
			SkillEntry value;
			return (!skillEntries.TryGetValue(id, out value)) ? null : WzAssets.Node("Skill/" + value.FileJob.ToString(CultureInfo.InvariantCulture) + ".img/skill/" + id.ToString(CultureInfo.InvariantCulture) + "/icon");
		});
	}

	private static byte[] IconPng(string key, Func<ImgNode> resolve)
	{
		lock (iconGate)
		{
			if (iconCache.TryGetValue(key, out var value))
			{
				return value;
			}
		}
		byte[] array = AdminMainThread.Run(() =>
		{
			try
			{
				ImgNode imgNode = resolve();
				if (imgNode == null)
				{
					return (byte[])null;
				}
				ImgNode imgNode2 = WzAssets.Canvas(imgNode);
				if (imgNode2 == null || imgNode2.Width <= 0 || imgNode2.Height <= 0)
				{
					return (byte[])null;
				}
				return AdminPng.Encode(imgNode2.Width, imgNode2.Height, WzAssets.Pixels(imgNode));
			}
			catch
			{
				return (byte[])null;
			}
		});
		if (array == null)
		{
			return null;
		}
		lock (iconGate)
		{
			if (iconCache.Count >= 512)
			{
				iconCache.Clear();
			}
			iconCache[key] = array;
			return array;
		}
	}
}
