using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public static class AdminSetItem
{
	private sealed class TierOption
	{
		public string Text;

		public Dictionary<string, int> Stats = new Dictionary<string, int>();
	}

	private sealed class TierSkill
	{
		public int Id;

		public int Level;

		public string Name;
	}

	private sealed class Tier
	{
		public int Required;

		public Dictionary<string, int> Stats = new Dictionary<string, int>();

		public Dictionary<string, int> OptionStats = new Dictionary<string, int>();

		public readonly List<TierOption> Options = new List<TierOption>();

		public readonly List<TierSkill> Skills = new List<TierSkill>();

		public readonly Dictionary<string, int> Unmapped = new Dictionary<string, int>();
	}

	private sealed class SlotMeta
	{
		public string Part;

		public string Represent;
	}

	private sealed class Definition
	{
		public int Id;

		public int JobIndex;

		public string Name;

		public int CompleteCount;

		public List<List<int>> Slots = new List<List<int>>();

		public readonly List<SlotMeta> Metas = new List<SlotMeta>();

		public readonly Dictionary<string, string> Desc = new Dictionary<string, string>();

		public HashSet<int> ItemIds;

		public int JokerPossible;

		public List<Tier> Tiers = new List<Tier>();

		public bool BuiltIn;
	}

	public sealed class GrantedSkill
	{
		public int Id;

		public int Level;

		public string Name;

		public string Desc;
	}

	public sealed class SetPanelAlt
	{
		public int ItemId;

		public string Name;

		public string SlotName;

		public bool Equipped;
	}

	public sealed class SetPanelSlot
	{
		public string Part;

		public string Represent;

		public readonly List<SetPanelAlt> Alts = new List<SetPanelAlt>();

		public bool AnyWorn
		{
			get
			{
				foreach (SetPanelAlt alt in Alts)
				{
					if (alt.Equipped)
					{
						return true;
					}
				}
				return false;
			}
		}
	}

	public sealed class SetPanelTier
	{
		public int Required;

		public readonly List<KeyValuePair<string, int>> Stats = new List<KeyValuePair<string, int>>();

		public readonly List<string> Options = new List<string>();

		public readonly List<string> Skills = new List<string>();

		public readonly List<string> Unmapped = new List<string>();
	}

	public sealed class SetPanel
	{
		public int Id;

		public string Name;

		public int CompleteCount;

		public int EquippedCount;

		public int JokerPossible;

		public readonly List<SetPanelSlot> Slots = new List<SetPanelSlot>();

		public readonly List<SetPanelTier> Tiers = new List<SetPanelTier>();
	}

	public const string BonusCode = "set_item_bonus_overrides";

	public const string CatalogCode = "set_item_catalog_customizations";

	private const int FirstCustomId = 20000;

	private const int RemovedValue = int.MinValue;

	private static readonly string[] SupportedStats = new string[38]
	{
		"STR", "DEX", "INT", "LUK", "PAD", "MAD", "PDD", "MDD", "ACC", "EVA",
		"SPD", "JMP", "HP", "MP", "FinalDamage", "BossDamage", "NormalDamage", "Damage", "ExpRate", "DropRate",
		"MesoRate", "STRPct", "DEXPct", "INTPct", "LUKPct", "PADPct", "MADPct", "PDDPct", "MDDPct", "ACCPct",
		"EVAPct", "HPPct", "MPPct", "IgnoreDefense", "CriticalChance", "CriticalDamage", "StatusRes", "PvpDamage"
	};

	private static List<Definition> builtins;

	private static List<Definition> customDefs;

	private static HashSet<int> disabledIds;

	private static Dictionary<int, List<List<int>>> slotOverrides;

	private static readonly Dictionary<string, Dictionary<string, int>> bonusStore = new Dictionary<string, Dictionary<string, int>>();

	private static readonly Dictionary<int, string> equipNames282 = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> skillNames282 = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> skillDescs282 = new Dictionary<int, string>();

	private static readonly Dictionary<int, List<Definition>> defsByItem = new Dictionary<int, List<Definition>>();

	private static bool using282Data;

	private static readonly Dictionary<string, string> DescKeyOfPart = new Dictionary<string, string>
	{
		{ "武器", "weapon" },
		{ "副武器", "subweapon" },
		{ "口袋", "pocket" },
		{ "纹章", "emblem" },
		{ "帽子", "cap" }
	};

	public static bool Using282Data
	{
		get
		{
			EnsureLoaded();
			return using282Data;
		}
	}

	public static int BuiltInSetCount
	{
		get
		{
			EnsureLoaded();
			return builtins.Count;
		}
	}

	public static void Register(AdminApi api)
	{
		api.Get("/setItem/v1/catalog", (AdminCtx ctx) => Catalog());
		api.Put("/setItem/v1/{definitionId}", Update);
		api.Delete("/setItem/v1/{definitionId}", Reset);
		api.Put("/setItem/v1/series/{definitionId}", UpdateSeries);
		api.Get("/setItem/v1/equipment/search", SearchEquipment);
		api.Post("/setItem/v1/custom", Create);
		api.Put("/setItem/v1/{definitionId}/enabled", SetEnabled);
		api.Delete("/setItem/v1/custom/{definitionId}", DeleteCustom);
	}

	public static string EquipName282(int id)
	{
		EnsureLoaded();
		if (!equipNames282.TryGetValue(id, out var value))
		{
			return "";
		}
		return value;
	}

	public static string SkillName282(int id)
	{
		EnsureLoaded();
		if (!skillNames282.TryGetValue(id, out var value))
		{
			return "技能 " + id;
		}
		return value;
	}

	private static void EnsureLoaded()
	{
		if (builtins == null)
		{
			builtins = new List<Definition>();
			equipNames282.Clear();
			skillNames282.Clear();
			skillDescs282.Clear();
			defsByItem.Clear();
			Dictionary<string, object> dictionary = AdminJson.AsDict(AdminJson.Parse(PlatformRes.LoadText("Admin/SetItem282")));
			if (dictionary != null)
			{
				Load282(dictionary);
			}
			if (builtins.Count == 0)
			{
				LoadLegacyCatalog();
			}
			using282Data = builtins.Count > 0 && dictionary != null && builtins.Count > 55;
			RebuildItemIndex();
			ReloadConfig();
		}
	}

	private static void Load282(Dictionary<string, object> root)
	{
		foreach (KeyValuePair<string, object> item in AdminJson.AsDict(AdminJson.Get(root, "equipNames")) ?? new Dictionary<string, object>())
		{
			if (int.TryParse(item.Key, out var result))
			{
				equipNames282[result] = (item.Value as string) ?? "";
			}
		}
		foreach (KeyValuePair<string, object> item2 in AdminJson.AsDict(AdminJson.Get(root, "skills")) ?? new Dictionary<string, object>())
		{
			if (int.TryParse(item2.Key, out var result2))
			{
				Dictionary<string, object> dictionary = AdminJson.AsDict(item2.Value);
				if (dictionary != null)
				{
					skillNames282[result2] = AdminJson.GetString(dictionary, "name");
					skillDescs282[result2] = AdminJson.GetString(dictionary, "desc");
				}
			}
		}
		foreach (object item3 in AdminJson.AsList(AdminJson.Get(root, "sets")) ?? new List<object>())
		{
			Dictionary<string, object> dictionary2 = AdminJson.AsDict(item3);
			if (dictionary2 == null)
			{
				continue;
			}
			Definition definition = new Definition
			{
				Id = AdminJson.GetInt(dictionary2, "id"),
				JobIndex = -1,
				Name = AdminJson.GetString(dictionary2, "name"),
				CompleteCount = AdminJson.GetInt(dictionary2, "completeCount"),
				JokerPossible = AdminJson.GetInt(dictionary2, "jokerPossible"),
				BuiltIn = true,
				ItemIds = new HashSet<int>()
			};
			foreach (object item4 in AdminJson.AsList(AdminJson.Get(dictionary2, "slots")) ?? new List<object>())
			{
				Dictionary<string, object> dictionary3 = AdminJson.AsDict(item4);
				if (dictionary3 == null)
				{
					continue;
				}
				List<int> list = new List<int>();
				foreach (object item5 in AdminJson.AsList(AdminJson.Get(dictionary3, "items")) ?? new List<object>())
				{
					int num = Convert.ToInt32(item5);
					if (num > 0)
					{
						list.Add(num);
						definition.ItemIds.Add(num);
					}
				}
				definition.Slots.Add(list);
				definition.Metas.Add(new SlotMeta
				{
					Part = AdminJson.GetString(dictionary3, "part"),
					Represent = AdminJson.GetString(dictionary3, "represent")
				});
			}
			foreach (KeyValuePair<string, object> item6 in AdminJson.AsDict(AdminJson.Get(dictionary2, "desc")) ?? new Dictionary<string, object>())
			{
				definition.Desc[item6.Key] = (item6.Value as string) ?? "";
			}
			foreach (object item7 in AdminJson.AsList(AdminJson.Get(dictionary2, "tiers")) ?? new List<object>())
			{
				Dictionary<string, object> dictionary4 = AdminJson.AsDict(item7);
				if (dictionary4 == null)
				{
					continue;
				}
				Tier tier = new Tier
				{
					Required = AdminJson.GetInt(dictionary4, "required")
				};
				foreach (KeyValuePair<string, object> item8 in AdminJson.AsDict(AdminJson.Get(dictionary4, "stats")) ?? new Dictionary<string, object>())
				{
					tier.Stats[item8.Key] = Convert.ToInt32(item8.Value);
				}
				foreach (KeyValuePair<string, object> item9 in AdminJson.AsDict(AdminJson.Get(dictionary4, "unmapped")) ?? new Dictionary<string, object>())
				{
					tier.Unmapped[item9.Key] = Convert.ToInt32(item9.Value);
				}
				foreach (object item10 in AdminJson.AsList(AdminJson.Get(dictionary4, "options")) ?? new List<object>())
				{
					Dictionary<string, object> dictionary5 = AdminJson.AsDict(item10);
					if (dictionary5 == null)
					{
						continue;
					}
					TierOption tierOption = new TierOption
					{
						Text = AdminJson.GetString(dictionary5, "text")
					};
					foreach (KeyValuePair<string, object> item11 in AdminJson.AsDict(AdminJson.Get(dictionary5, "stats")) ?? new Dictionary<string, object>())
					{
						int num2 = Convert.ToInt32(item11.Value);
						tierOption.Stats[item11.Key] = num2;
						tier.OptionStats[item11.Key] = (tier.OptionStats.TryGetValue(item11.Key, out var value) ? (value + num2) : num2);
					}
					tier.Options.Add(tierOption);
				}
				foreach (object item12 in AdminJson.AsList(AdminJson.Get(dictionary4, "activeSkills")) ?? new List<object>())
				{
					Dictionary<string, object> dictionary6 = AdminJson.AsDict(item12);
					if (dictionary6 != null)
					{
						int num3 = AdminJson.GetInt(dictionary6, "id");
						if (num3 > 0)
						{
							tier.Skills.Add(new TierSkill
							{
								Id = num3,
								Level = Math.Max(1, AdminJson.GetInt(dictionary6, "level", 1)),
								Name = (skillNames282.TryGetValue(num3, out var value2) ? value2 : ("技能 " + num3))
							});
						}
					}
				}
				definition.Tiers.Add(tier);
			}
			definition.Tiers.Sort((Tier a, Tier b) => a.Required.CompareTo(b.Required));
			builtins.Add(definition);
		}
	}

	private static void LoadLegacyCatalog()
	{
		foreach (object item in AdminJson.AsList(AdminJson.Parse(PlatformRes.LoadText("Admin/SetItemCatalog"))) ?? new List<object>())
		{
			Dictionary<string, object> dictionary = AdminJson.AsDict(item);
			Definition definition = new Definition
			{
				Id = AdminJson.GetInt(dictionary, "id"),
				JobIndex = AdminJson.GetInt(dictionary, "jobIndex", -1),
				Name = AdminJson.GetString(dictionary, "name"),
				BuiltIn = true
			};
			foreach (object item2 in AdminJson.AsList(dictionary["slots"]) ?? new List<object>())
			{
				List<int> list = new List<int>();
				foreach (object item3 in AdminJson.AsList(item2) ?? new List<object>())
				{
					list.Add(Convert.ToInt32(item3));
				}
				definition.Slots.Add(list);
				definition.Metas.Add(new SlotMeta());
			}
			foreach (object item4 in AdminJson.AsList(dictionary["tiers"]) ?? new List<object>())
			{
				Dictionary<string, object> dictionary2 = AdminJson.AsDict(item4);
				Tier tier = new Tier
				{
					Required = AdminJson.GetInt(dictionary2, "requiredCount")
				};
				foreach (KeyValuePair<string, object> item5 in AdminJson.AsDict(dictionary2["stats"]) ?? new Dictionary<string, object>())
				{
					tier.Stats[item5.Key] = Convert.ToInt32(item5.Value);
				}
				definition.Tiers.Add(tier);
			}
			definition.Tiers.Sort((Tier a, Tier b) => a.Required.CompareTo(b.Required));
			builtins.Add(definition);
		}
	}

	private static void RebuildItemIndex()
	{
		defsByItem.Clear();
		foreach (Definition builtin in builtins)
		{
			foreach (List<int> slot in builtin.Slots)
			{
				foreach (int item in slot)
				{
					if (!defsByItem.TryGetValue(item, out var value))
					{
						value = (defsByItem[item] = new List<Definition>());
					}
					if (!value.Contains(builtin))
					{
						value.Add(builtin);
					}
				}
			}
		}
	}

	private static void ReloadConfig()
	{
		customDefs = new List<Definition>();
		disabledIds = new HashSet<int>();
		slotOverrides = new Dictionary<int, List<List<int>>>();
		try
		{
			DbStore db = AdminBackend.Db;
			if (db == null)
			{
				return;
			}
			Dictionary<string, object> dictionary = AdminJson.AsDict(AdminJson.Parse(GameConfigDao.Get(db, "set_item_catalog_customizations")));
			if (dictionary != null)
			{
				foreach (object item in AdminJson.GetList(dictionary, "customDefinitions"))
				{
					customDefs.Add(ParseStored(AdminJson.AsDict(item)));
				}
				foreach (object item2 in AdminJson.GetList(dictionary, "disabledBuiltInIds"))
				{
					disabledIds.Add(Convert.ToInt32(item2));
				}
				Dictionary<string, object> dict = AdminJson.GetDict(dictionary, "slotOverrides");
				if (dict != null)
				{
					foreach (KeyValuePair<string, object> item3 in dict)
					{
						List<List<int>> list = new List<List<int>>();
						foreach (object item4 in AdminJson.AsList(item3.Value))
						{
							List<int> list2 = new List<int>();
							foreach (object item5 in AdminJson.AsList(item4))
							{
								list2.Add(Convert.ToInt32(item5));
							}
							list.Add(list2);
						}
						slotOverrides[Convert.ToInt32(item3.Key)] = list;
					}
				}
			}
			Dictionary<string, object> dictionary2 = AdminJson.AsDict(AdminJson.Parse(GameConfigDao.Get(db, "set_item_bonus_overrides")));
			bonusStore.Clear();
			if (dictionary2 == null)
			{
				return;
			}
			foreach (KeyValuePair<string, object> item6 in dictionary2)
			{
				Dictionary<string, int> dictionary3 = new Dictionary<string, int>();
				foreach (KeyValuePair<string, object> item7 in AdminJson.AsDict(item6.Value))
				{
					dictionary3[item7.Key] = Convert.ToInt32(item7.Value);
				}
				bonusStore[item6.Key] = dictionary3;
			}
		}
		catch
		{
		}
	}

	private static Definition ParseStored(Dictionary<string, object> d)
	{
		Definition definition = new Definition
		{
			Id = AdminJson.GetInt(d, "id"),
			JobIndex = AdminJson.GetInt(d, "jobIndex", -1),
			Name = AdminJson.GetString(d, "name"),
			BuiltIn = false
		};
		foreach (object item in AdminJson.GetList(d, "slots"))
		{
			List<int> list = new List<int>();
			foreach (object item2 in AdminJson.AsList(item))
			{
				list.Add(Convert.ToInt32(item2));
			}
			definition.Slots.Add(list);
		}
		Dictionary<string, object> dict = AdminJson.GetDict(d, "tiers");
		if (dict != null)
		{
			foreach (KeyValuePair<string, object> item3 in dict)
			{
				Tier tier = new Tier
				{
					Required = Convert.ToInt32(item3.Key)
				};
				foreach (KeyValuePair<string, object> item4 in AdminJson.AsDict(item3.Value))
				{
					tier.Stats[item4.Key] = Convert.ToInt32(item4.Value);
				}
				definition.Tiers.Add(tier);
			}
		}
		definition.Tiers.Sort((Tier a, Tier b) => a.Required.CompareTo(b.Required));
		return definition;
	}

	private static List<Definition> AllDefs()
	{
		List<Definition> list = new List<Definition>(builtins);
		list.AddRange(customDefs);
		list.Sort((Definition a, Definition b) => a.Id.CompareTo(b.Id));
		return list;
	}

	private static Definition Require(int id)
	{
		foreach (Definition builtin in builtins)
		{
			if (builtin.Id == id)
			{
				return builtin;
			}
		}
		foreach (Definition customDef in customDefs)
		{
			if (customDef.Id == id)
			{
				return customDef;
			}
		}
		throw new AdminApiException("套装不存在: " + id);
	}

	private static object Catalog()
	{
		EnsureLoaded();
		List<object> list = new List<object>();
		foreach (Definition def in AllDefs())
		{
			Definition definition = (def.BuiltIn ? builtins.FirstOrDefault((Definition b) => b.Id == def.Id) : null);
			List<List<int>> list2 = (slotOverrides.TryGetValue(def.Id, out var value) ? value : def.Slots);
			List<object> list3 = new List<object>();
			foreach (List<int> item in list2)
			{
				List<object> list4 = new List<object>();
				foreach (int item2 in item)
				{
					list4.Add(AdminJson.Obj("id", (long)item2, "name", AdminNames.ItemName(item2)));
				}
				list3.Add(list4);
			}
			List<object> list5 = new List<object>();
			foreach (List<int> item3 in (definition != null) ? definition.Slots : def.Slots)
			{
				List<object> list6 = new List<object>();
				foreach (int item4 in item3)
				{
					list6.Add(AdminJson.Obj("id", (long)item4, "name", AdminNames.ItemName(item4)));
				}
				list5.Add(list6);
			}
			List<object> list7 = new List<object>();
			for (int num = 0; num < def.Tiers.Count; num++)
			{
				Tier tier = def.Tiers[num];
				Dictionary<string, int> dictionary = new Dictionary<string, int>(tier.Stats);
				string key = def.Id + ":" + tier.Required;
				if (bonusStore.TryGetValue(key, out var value2))
				{
					foreach (KeyValuePair<string, int> item5 in value2)
					{
						if (item5.Value == int.MinValue)
						{
							dictionary.Remove(item5.Key);
						}
						else
						{
							dictionary[item5.Key] = item5.Value;
						}
					}
				}
				Dictionary<string, int> dictionary2 = ((definition != null && num < definition.Tiers.Count) ? definition.Tiers[num].Stats : tier.Stats);
				List<object> list8 = new List<object>();
				foreach (KeyValuePair<string, int> item6 in dictionary)
				{
					list8.Add(item6.Key);
					list8.Add((long)item6.Value);
				}
				List<object> list9 = new List<object>();
				foreach (KeyValuePair<string, int> item7 in dictionary2)
				{
					list9.Add(item7.Key);
					list9.Add((long)item7.Value);
				}
				list7.Add(AdminJson.Obj("requiredCount", (long)tier.Required, "stats", FlatObj(list8), "defaultStats", FlatObj(list9), "customized", bonusStore.ContainsKey(key)));
			}
			list.Add(AdminJson.Obj("id", (long)def.Id, "jobIndex", (long)def.JobIndex, "name", def.Name, "completeCount", (long)((def.CompleteCount > 0) ? def.CompleteCount : list2.Count), "builtIn", def.BuiltIn, "enabled", !def.BuiltIn || !disabledIds.Contains(def.Id), "slots", list3, "defaultSlots", list5, "slotsCustomized", slotOverrides.ContainsKey(def.Id), "tiers", list7));
		}
		return list;
	}

	private static Dictionary<string, object> FlatObj(List<object> flat)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		for (int i = 0; i + 1 < flat.Count; i += 2)
		{
			dictionary[(string)flat[i]] = flat[i + 1];
		}
		return dictionary;
	}

	private static object Update(AdminCtx ctx)
	{
		int id = (int)ctx.ParamLong("definitionId");
		object body = ctx.Body;
		return AdminMainThread.Run(() =>
		{
			PrepareUpdate(Require(id), AdminJson.AsDict(body), allowSlots: true);
			Persist();
			return 1L;
		});
	}

	private static object UpdateSeries(AdminCtx ctx)
	{
		int id = (int)ctx.ParamLong("definitionId");
		object body = ctx.Body;
		if (AdminJson.Get(body, "slots") != null)
		{
			throw new AdminApiException("系列统一修改仅支持加成，槽位请进入套装详情修改");
		}
		return AdminMainThread.Run(() =>
		{
			Definition definition = Require(id);
			foreach (Definition item in builtins.ToList())
			{
				if (item.Id == id || (definition.BuiltIn && item.BuiltIn && item.Name == definition.Name))
				{
					PrepareUpdate(item, AdminJson.AsDict(body), allowSlots: false);
				}
			}
			foreach (Definition item2 in customDefs.ToList())
			{
				if (item2.Id == id || (definition.BuiltIn && item2.BuiltIn && item2.Name == definition.Name))
				{
					PrepareUpdate(item2, AdminJson.AsDict(body), allowSlots: false);
				}
			}
			Persist();
			return 1L;
		});
	}

	private static void PrepareUpdate(Definition def, Dictionary<string, object> body, bool allowSlots)
	{
		Dictionary<string, object> dict = AdminJson.GetDict(body, "tiers");
		if (dict == null)
		{
			throw new AdminApiException("套装档位配置不能为空");
		}
		HashSet<string> hashSet = new HashSet<string>(def.Tiers.Select((Tier t) => t.Required.ToString()));
		if (!new HashSet<string>(dict.Keys).SetEquals(hashSet))
		{
			throw new AdminApiException("套装档位不完整或包含未知档位");
		}
		if (allowSlots && AdminJson.Get(body, "slots") != null)
		{
			List<List<int>> list = ValidateSlots(AdminJson.GetList(body, "slots"));
			if (SameSlots(list, def.Slots))
			{
				slotOverrides.Remove(def.Id);
			}
			else
			{
				slotOverrides[def.Id] = list;
			}
		}
		string value = def.Id + ":";
		foreach (string item in bonusStore.Keys.ToList())
		{
			if (item.StartsWith(value, StringComparison.Ordinal))
			{
				bonusStore.Remove(item);
			}
		}
		foreach (Tier tier in def.Tiers)
		{
			Dictionary<string, object> dictionary = AdminJson.AsDict(dict[tier.Required.ToString()]);
			if (dictionary == null)
			{
				throw new AdminApiException("套装档位属性不能为空");
			}
			if (dictionary.Count > 24)
			{
				throw new AdminApiException("每档最多支持 24 项属性");
			}
			Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
			foreach (KeyValuePair<string, object> item2 in dictionary)
			{
				if (!Enumerable.Contains(SupportedStats, item2.Key))
				{
					throw new AdminApiException("不支持的套装属性: " + item2.Key);
				}
				int num = Convert.ToInt32(item2.Value);
				ValidateValue(item2.Key, num);
				if (!tier.Stats.TryGetValue(item2.Key, out var value2) || value2 != num)
				{
					dictionary2[item2.Key] = num;
				}
			}
			foreach (KeyValuePair<string, int> stat in tier.Stats)
			{
				if (!dictionary.ContainsKey(stat.Key))
				{
					dictionary2[stat.Key] = int.MinValue;
				}
			}
			if (dictionary2.Count > 0)
			{
				bonusStore[def.Id + ":" + tier.Required] = dictionary2;
			}
		}
	}

	private static List<List<int>> ValidateSlots(List<object> raw)
	{
		if (raw == null || raw.Count == 0 || raw.Count > 20)
		{
			throw new AdminApiException("槽位数量须在 1 到 20 之间");
		}
		HashSet<int> hashSet = new HashSet<int>();
		List<List<int>> list = new List<List<int>>();
		foreach (object item in raw)
		{
			List<int> list2 = new List<int>();
			foreach (object item2 in AdminJson.AsList(item))
			{
				int num = Convert.ToInt32(item2);
				if (num <= 0 || !hashSet.Add(num))
				{
					throw new AdminApiException("槽位道具无效或重复: " + num);
				}
				list2.Add(num);
			}
			if (list2.Count == 0 || list2.Count > 50)
			{
				throw new AdminApiException("每槽道具数量须在 1 到 50 之间");
			}
			list.Add(list2);
		}
		return list;
	}

	private static bool SameSlots(List<List<int>> a, List<List<int>> b)
	{
		if (a.Count != b.Count)
		{
			return false;
		}
		for (int i = 0; i < a.Count; i++)
		{
			if (a[i].Count != b[i].Count)
			{
				return false;
			}
			for (int j = 0; j < a[i].Count; j++)
			{
				if (a[i][j] != b[i][j])
				{
					return false;
				}
			}
		}
		return true;
	}

	private static void ValidateValue(string stat, int value)
	{
		int num = (IsRate(stat) ? 10000 : 1000000);
		if (value < 0 || value > num)
		{
			throw new AdminApiException(stat + " 必须在 0 到 " + num + " 之间");
		}
	}

	private static bool IsRate(string stat)
	{
		string text = stat.ToLowerInvariant();
		if (!text.EndsWith("rate") && !text.EndsWith("pct") && !text.EndsWith("damage") && !text.EndsWith("chance") && !(text == "ignoredefense") && !(text == "statusres"))
		{
			return text == "buffduration";
		}
		return true;
	}

	private static object Reset(AdminCtx ctx)
	{
		int id = (int)ctx.ParamLong("definitionId");
		return AdminMainThread.Run(() =>
		{
			Require(id);
			RemoveDefConfig(id);
			Persist();
			return 1L;
		});
	}

	private static void RemoveDefConfig(int id)
	{
		string value = id + ":";
		foreach (string item in bonusStore.Keys.ToList())
		{
			if (item.StartsWith(value, StringComparison.Ordinal))
			{
				bonusStore.Remove(item);
			}
		}
		slotOverrides.Remove(id);
	}

	private static object SetEnabled(AdminCtx ctx)
	{
		int id = (int)ctx.ParamLong("definitionId");
		bool enabled = AdminJson.GetBool(ctx.Body, "enabled", fallback: true);
		return AdminMainThread.Run(() =>
		{
			if (!Require(id).BuiltIn)
			{
				throw new AdminApiException("只有内置套装可以停用或恢复");
			}
			if (enabled)
			{
				disabledIds.Remove(id);
			}
			else
			{
				disabledIds.Add(id);
			}
			Persist();
			return 1L;
		});
	}

	private static object Create(AdminCtx ctx)
	{
		object body = ctx.Body;
		return AdminMainThread.Run((Func<long>)(() =>
		{
			EnsureLoaded();
			string text = AdminJson.GetString(body, "name");
			if (string.IsNullOrEmpty(text) || text.Length > 64)
			{
				throw new AdminApiException("套装名称须为 1 到 64 个字符");
			}
			int id = 20000;
			foreach (Definition customDef in customDefs)
			{
				id = Math.Max(id, customDef.Id + 1);
			}
			Definition definition = new Definition
			{
				Id = id,
				JobIndex = AdminJson.GetInt(body, "jobIndex", -1),
				Name = text,
				BuiltIn = false
			};
			List<object> list = AdminJson.GetList(body, "slots");
			if (list == null || list.Count == 0)
			{
				throw new AdminApiException("套装槽位不能为空");
			}
			definition.Slots = ValidateSlots(list);
			Dictionary<string, object> dict = AdminJson.GetDict(body, "tiers");
			if (dict == null || dict.Count == 0)
			{
				throw new AdminApiException("套装档位不能为空");
			}
			foreach (KeyValuePair<string, object> item in dict)
			{
				if (!int.TryParse(item.Key, out var result) || result <= 0 || result > definition.Slots.Count)
				{
					throw new AdminApiException("档位件数无效: " + item.Key);
				}
				Tier tier = new Tier
				{
					Required = result
				};
				Dictionary<string, object> dictionary = AdminJson.AsDict(item.Value);
				if (dictionary == null || dictionary.Count > 24)
				{
					throw new AdminApiException("每档最多支持 24 项属性");
				}
				foreach (KeyValuePair<string, object> item2 in dictionary)
				{
					if (!Enumerable.Contains(SupportedStats, item2.Key))
					{
						throw new AdminApiException("不支持的套装属性: " + item2.Key);
					}
					int value = Convert.ToInt32(item2.Value);
					ValidateValue(item2.Key, value);
					tier.Stats[item2.Key] = value;
				}
				definition.Tiers.Add(tier);
			}
			definition.Tiers.Sort((Tier a, Tier b) => a.Required.CompareTo(b.Required));
			if (customDefs.Any((Definition d) => d.Id == id))
			{
				throw new AdminApiException("自定义套装ID无效或重复");
			}
			customDefs.Add(definition);
			Persist();
			return id;
		}), 8000);
	}

	private static object DeleteCustom(AdminCtx ctx)
	{
		int id = (int)ctx.ParamLong("definitionId");
		return AdminMainThread.Run(() =>
		{
			Definition definition = customDefs.FirstOrDefault((Definition d) => d.Id == id);
			if (definition == null)
			{
				throw new AdminApiException("自定义套装不存在: " + id);
			}
			customDefs.Remove(definition);
			RemoveDefConfig(id);
			Persist();
			return 1L;
		});
	}

	private static object SearchEquipment(AdminCtx ctx)
	{
		List<object> records = AdminCatalogData.EquipmentCatalog(ctx.Query("keyword", ""), "", 1, 20, null, "", null, null, null).Records;
		List<object> list = new List<object>();
		if (records != null)
		{
			foreach (object item in records)
			{
				Dictionary<string, object> o = AdminJson.AsDict(item);
				list.Add(AdminJson.Obj("id", AdminJson.GetLong(o, "id", 0L), "name", AdminJson.GetString(o, "name")));
			}
		}
		return list;
	}

	private static void Persist()
	{
		Invalidate();
		DbStore db = AdminBackend.RequireDb();
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		foreach (KeyValuePair<string, Dictionary<string, int>> item in bonusStore)
		{
			Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
			foreach (KeyValuePair<string, int> item2 in item.Value)
			{
				dictionary2[item2.Key] = (long)item2.Value;
			}
			dictionary[item.Key] = dictionary2;
		}
		List<object> list = new List<object>();
		foreach (Definition customDef in customDefs)
		{
			List<object> list2 = new List<object>();
			foreach (List<int> slot in customDef.Slots)
			{
				list2.Add(((IEnumerable<int>)slot).Select((Func<int, long>)((int i) => i)).ToList());
			}
			Dictionary<string, object> dictionary3 = new Dictionary<string, object>();
			foreach (Tier tier in customDef.Tiers)
			{
				Dictionary<string, object> dictionary4 = new Dictionary<string, object>();
				foreach (KeyValuePair<string, int> stat in tier.Stats)
				{
					dictionary4[stat.Key] = (long)stat.Value;
				}
				dictionary3[tier.Required.ToString()] = dictionary4;
			}
			list.Add(AdminJson.Obj("id", (long)customDef.Id, "name", customDef.Name, "jobIndex", (long)customDef.JobIndex, "slots", list2, "tiers", dictionary3));
		}
		Dictionary<string, object> dictionary5 = new Dictionary<string, object>();
		foreach (KeyValuePair<int, List<List<int>>> slotOverride in slotOverrides)
		{
			dictionary5[slotOverride.Key.ToString()] = slotOverride.Value.Select((List<int> slot) => ((IEnumerable<int>)slot).Select((Func<int, long>)((int i) => i)).ToList()).ToList();
		}
		Upsert(db, "set_item_bonus_overrides", AdminJson.Write(dictionary));
		Upsert(db, "set_item_catalog_customizations", AdminJson.Write(AdminJson.Obj("customDefinitions", list, "disabledBuiltInIds", ((IEnumerable<int>)disabledIds).Select((Func<int, long>)((int i) => i)).ToList(), "slotOverrides", dictionary5)));
	}

	private static void Upsert(DbStore db, string code, string value)
	{
		DbTable dbTable = db.Table("game_config");
		DbRow row = dbTable.Find((DbRow r) => DbStore.Str(r, "config_code") == code);
		string value2 = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		if (row != null)
		{
			dbTable.Update((DbRow r) => r == row, new DbRow
			{
				["config_value"] = value,
				["update_time"] = value2
			});
		}
		else
		{
			dbTable.Insert(new DbRow
			{
				["config_type"] = "server",
				["config_sub_type"] = "Set Items",
				["config_clazz"] = "java.lang.String",
				["config_code"] = code,
				["config_value"] = value,
				["config_desc"] = code,
				["update_time"] = value2
			});
		}
	}

	public static int JobIndexOf(int jobId)
	{
		if (jobId < 100)
		{
			return -1;
		}
		return ((jobId >= 1000) ? (jobId / 100 % 10) : (jobId / 100)) switch
		{
			1 => 0, 
			2 => 2, 
			3 => 1, 
			4 => 3, 
			5 => 4, 
			_ => -1, 
		};
	}

	private static IEnumerable<KeyValuePair<Definition, int>> AchievedDefs(IEnumerable<int> wornIds, int jobIndex)
	{
		EnsureLoaded();
		List<int> worn = new List<int>();
		if (wornIds != null)
		{
			foreach (int wornId in wornIds)
			{
				if (wornId > 0)
				{
					worn.Add(wornId);
				}
			}
		}
		HashSet<int> equipped = new HashSet<int>(worn);
		foreach (Definition item in AllDefs())
		{
			if ((item.JobIndex >= 0 && item.JobIndex != jobIndex) || (item.BuiltIn && disabledIds.Contains(item.Id)))
			{
				continue;
			}
			bool flag = slotOverrides.ContainsKey(item.Id);
			List<List<int>> list = (flag ? slotOverrides[item.Id] : item.Slots);
			int num;
			if (item.ItemIds != null && !flag)
			{
				num = 0;
				foreach (int item2 in worn)
				{
					if (item.ItemIds.Contains(item2))
					{
						num++;
					}
				}
			}
			else
			{
				num = 0;
				foreach (List<int> item3 in list)
				{
					foreach (int item4 in item3)
					{
						if (equipped.Contains(item4))
						{
							num++;
							break;
						}
					}
				}
			}
			if (num > 0)
			{
				yield return new KeyValuePair<Definition, int>(item, num);
			}
		}
	}

	public static Dictionary<string, int> ComputeBonus(IEnumerable<int> wornIds, int jobIndex)
	{
		Dictionary<string, int> bonus = new Dictionary<string, int>();
		try
		{
			foreach (KeyValuePair<Definition, int> item in AchievedDefs(wornIds, jobIndex))
			{
				Definition key = item.Key;
				foreach (Tier tier in key.Tiers)
				{
					if (item.Value < tier.Required)
					{
						continue;
					}
					Dictionary<string, int> dictionary = new Dictionary<string, int>(tier.Stats);
					if (bonusStore.TryGetValue(key.Id + ":" + tier.Required, out var value))
					{
						foreach (KeyValuePair<string, int> item2 in value)
						{
							if (item2.Value == int.MinValue)
							{
								dictionary.Remove(item2.Key);
							}
							else
							{
								dictionary[item2.Key] = item2.Value;
							}
						}
					}
					Add(dictionary);
					Add(tier.OptionStats);
				}
			}
		}
		catch
		{
		}
		return bonus;
		void Add(Dictionary<string, int> stats)
		{
			foreach (KeyValuePair<string, int> stat in stats)
			{
				bonus[stat.Key] = (int)(bonus.TryGetValue(stat.Key, out var value2) ? Math.Min(2147483647L, (long)value2 + (long)stat.Value) : stat.Value);
			}
		}
	}

	public static List<GrantedSkill> ActiveSkillsFor(IEnumerable<int> wornIds, int jobIndex)
	{
		List<GrantedSkill> list = new List<GrantedSkill>();
		try
		{
			Dictionary<int, GrantedSkill> dictionary = new Dictionary<int, GrantedSkill>();
			foreach (KeyValuePair<Definition, int> item in AchievedDefs(wornIds, jobIndex))
			{
				foreach (Tier tier in item.Key.Tiers)
				{
					if (item.Value < tier.Required)
					{
						continue;
					}
					foreach (TierSkill skill in tier.Skills)
					{
						if (!dictionary.TryGetValue(skill.Id, out var value))
						{
							value = new GrantedSkill
							{
								Id = skill.Id,
								Level = skill.Level,
								Name = skill.Name,
								Desc = (skillDescs282.TryGetValue(skill.Id, out var value2) ? value2 : "")
							};
							dictionary[skill.Id] = value;
						}
						else if (skill.Level > value.Level)
						{
							value.Level = skill.Level;
						}
					}
				}
			}
			list.AddRange(dictionary.Values);
			list.Sort((GrantedSkill a, GrantedSkill b) => a.Id.CompareTo(b.Id));
		}
		catch
		{
		}
		return list;
	}

	public static int ApplyStat(Dictionary<string, int> bonus, string key, int baseValue)
	{
		bonus.TryGetValue(key, out var value);
		bonus.TryGetValue(key + "Pct", out var value2);
		long num = Math.Max(0L, (long)baseValue + (long)value);
		return (int)Math.Min(2147483647L, num + num * value2 / 100);
	}

	public static long ApplyDamage(Dictionary<string, int> bonus, bool boss, long damage, double additionalPercent = 0.0)
	{
		if (damage <= 0 || damage == long.MaxValue)
		{
			return damage;
		}
		bonus.TryGetValue("Damage", out var value);
		bonus.TryGetValue(boss ? "BossDamage" : "NormalDamage", out var value2);
		value += value2;
		bonus.TryGetValue("FinalDamage", out var value3);
		double num = Math.Round((double)damage * (100.0 + (double)value + additionalPercent) / 100.0 * (100.0 + (double)value3) / 100.0);
		if (num >= 9.223372036854776E+18)
		{
			return long.MaxValue;
		}
		return Math.Max(1L, (long)num);
	}

	public static double ApplyRate(Dictionary<string, int> bonus, string key, double rate)
	{
		bonus.TryGetValue(key, out var value);
		return rate * (double)(100 + value) / 100.0;
	}

	private static string UnmappedLabel(string key)
	{
		if (key == "incPQEXPr")
		{
			return "组队任务经验获得";
		}
		return key;
	}

	public static string SlotNameOf(int itemId)
	{
		switch (itemId / 10000)
		{
		case 100:
			return "帽子";
		case 101:
			return "脸饰";
		case 102:
			return "眼饰";
		case 103:
			return "耳环";
		case 109:
			return "盾牌";
		case 104:
			return "上衣";
		case 105:
			return "套服";
		case 106:
			return "裤子";
		case 107:
			return "鞋子";
		case 108:
			return "手套";
		case 110:
			return "披风";
		case 111:
			return "戒指";
		case 112:
			return "吊坠";
		case 113:
			return "腰带";
		case 114:
			return "勋章";
		case 115:
			return "肩章";
		default:
			if (itemId / 1000000 != 1)
			{
				return "装备";
			}
			return "武器";
		}
	}

	public static List<SetPanel> PanelsFor(int itemId, IEnumerable<int> wornIds)
	{
		List<SetPanel> list = new List<SetPanel>();
		if (itemId <= 0)
		{
			return list;
		}
		try
		{
			EnsureLoaded();
			Dictionary<int, int> dictionary = new Dictionary<int, int>();
			if (wornIds != null)
			{
				foreach (int wornId in wornIds)
				{
					if (wornId > 0)
					{
						dictionary[wornId] = ((!dictionary.TryGetValue(wornId, out var value)) ? 1 : (value + 1));
					}
				}
			}
			HashSet<int> hashSet = new HashSet<int>(dictionary.Keys);
			List<Definition> list2 = new List<Definition>();
			if (defsByItem.TryGetValue(itemId, out var value2))
			{
				list2.AddRange(value2);
			}
			foreach (KeyValuePair<int, List<List<int>>> kv in slotOverrides)
			{
				foreach (List<int> item in kv.Value)
				{
					if (item.Contains(itemId))
					{
						Definition definition = builtins.FirstOrDefault((Definition b) => b.Id == kv.Key) ?? customDefs.FirstOrDefault((Definition c) => c.Id == kv.Key);
						if (definition != null && !list2.Contains(definition))
						{
							list2.Add(definition);
						}
						break;
					}
				}
			}
			foreach (Definition item2 in list2)
			{
				if (item2.BuiltIn && disabledIds.Contains(item2.Id))
				{
					continue;
				}
				List<List<int>> list3 = (slotOverrides.TryGetValue(item2.Id, out var value3) ? value3 : item2.Slots);
				int num = ((item2.CompleteCount > 0) ? item2.CompleteCount : list3.Count);
				if (num <= 0)
				{
					continue;
				}
				SetPanel setPanel = new SetPanel
				{
					Id = item2.Id,
					Name = (string.IsNullOrEmpty(item2.Name) ? ("套装 " + item2.Id) : item2.Name),
					CompleteCount = num,
					JokerPossible = item2.JokerPossible
				};
				Dictionary<int, int> dictionary2 = new Dictionary<int, int>(dictionary);
				int num2 = 0;
				int num3 = Math.Max(num, list3.Count);
				for (int num4 = 0; num4 < num3; num4++)
				{
					int num5 = ((num4 < list3.Count) ? num4 : (list3.Count - 1));
					if (num5 < 0)
					{
						break;
					}
					List<int> list4 = list3[num5];
					SlotMeta slotMeta = ((num5 < item2.Metas.Count) ? item2.Metas[num5] : null);
					SetPanelSlot setPanelSlot = new SetPanelSlot
					{
						Part = ((slotMeta != null && !string.IsNullOrEmpty(slotMeta.Part)) ? slotMeta.Part : ((list4.Count > 0) ? SlotNameOf(list4[0]) : "装备")),
						Represent = ((slotMeta != null) ? slotMeta.Represent : "")
					};
					if (string.IsNullOrEmpty(setPanelSlot.Represent) && DescKeyOfPart.TryGetValue(setPanelSlot.Part, out var value4))
					{
						item2.Desc.TryGetValue(value4, out setPanelSlot.Represent);
					}
					foreach (int item3 in list4)
					{
						string text = ItemCatalog.Name(item3);
						if (string.IsNullOrEmpty(text))
						{
							text = EquipName282(item3);
						}
						if (string.IsNullOrEmpty(text))
						{
							text = setPanelSlot.Represent;
						}
						if (string.IsNullOrEmpty(text))
						{
							text = item3.ToString();
						}
						setPanelSlot.Alts.Add(new SetPanelAlt
						{
							ItemId = item3,
							Name = text,
							SlotName = SlotNameOf(item3),
							Equipped = hashSet.Contains(item3)
						});
					}
					bool flag = false;
					foreach (SetPanelAlt alt in setPanelSlot.Alts)
					{
						if (dictionary2.TryGetValue(alt.ItemId, out var value5) && value5 > 0)
						{
							dictionary2[alt.ItemId] = value5 - 1;
							flag = true;
							break;
						}
					}
					foreach (SetPanelAlt alt2 in setPanelSlot.Alts)
					{
						alt2.Equipped = flag;
					}
					setPanelSlot.Represent = setPanelSlot.Represent ?? "";
					if (flag)
					{
						num2++;
					}
					setPanel.Slots.Add(setPanelSlot);
				}
				setPanel.EquippedCount = num2;
				foreach (Tier tier in item2.Tiers)
				{
					SetPanelTier setPanelTier = new SetPanelTier
					{
						Required = tier.Required
					};
					string[] supportedStats = SupportedStats;
					foreach (string key in supportedStats)
					{
						if (tier.Stats.TryGetValue(key, out var value6) && value6 != 0)
						{
							setPanelTier.Stats.Add(new KeyValuePair<string, int>(key, value6));
						}
					}
					foreach (TierOption option in tier.Options)
					{
						if (!string.IsNullOrEmpty(option.Text))
						{
							setPanelTier.Options.Add(option.Text);
						}
					}
					foreach (TierSkill skill in tier.Skills)
					{
						setPanelTier.Skills.Add("Lv." + skill.Level + " " + skill.Name);
					}
					foreach (KeyValuePair<string, int> item4 in tier.Unmapped)
					{
						setPanelTier.Unmapped.Add(UnmappedLabel(item4.Key) + " +" + item4.Value + "（未接入）");
					}
					setPanel.Tiers.Add(setPanelTier);
				}
				list.Add(setPanel);
			}
			list.Sort((SetPanel a, SetPanel b) => b.EquippedCount.CompareTo(a.EquippedCount));
		}
		catch
		{
		}
		return list;
	}

	public static void Invalidate()
	{
		builtins = null;
	}
}
