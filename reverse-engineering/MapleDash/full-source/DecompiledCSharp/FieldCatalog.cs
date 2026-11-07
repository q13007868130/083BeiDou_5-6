using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MapleDash.Wz;
using Server.Field;
using UnityEngine;

public static class FieldCatalog
{
	public const int ChanceDenominator = 999999;

	public static readonly Dictionary<int, FieldMap> Maps = new Dictionary<int, FieldMap>();

	public static readonly Dictionary<int, FieldMob> Mobs = new Dictionary<int, FieldMob>();

	public static readonly Dictionary<int, List<FieldDropEntry>> Drops = new Dictionary<int, List<FieldDropEntry>>();

	public static readonly List<FieldDropEntry> GlobalDrops = new List<FieldDropEntry>();

	public static readonly Dictionary<int, FieldItemInfo> Items = new Dictionary<int, FieldItemInfo>();

	private static bool tried;

	private static readonly HashSet<int> loadedMaps = new HashSet<int>();

	private static readonly HashSet<int> loadedMobs = new HashSet<int>();

	public static bool Ready { get; private set; }

	public static void Ensure()
	{
		if (tried)
		{
			return;
		}
		tried = true;
		ImgNode imgNode = WzAssets.FileRoot("String/Map.img");
		Dictionary<int, ImgNode> dictionary = new Dictionary<int, ImgNode>();
		foreach (ImgNode child in imgNode.Children)
		{
			foreach (ImgNode child2 in child.Children)
			{
				if (int.TryParse(child2.Name, out var result))
				{
					dictionary[result] = child2;
				}
			}
		}
		foreach (Dictionary<string, object> item in (List<object>)WzAssets.Index["files"])
		{
			string text = (string)item["path"];
			if (text.StartsWith("Map/Map/Map") && text.EndsWith(".img") && int.TryParse(Path.GetFileNameWithoutExtension(text), out var result2))
			{
				dictionary.TryGetValue(result2, out var value);
				Maps[result2] = new FieldMap
				{
					id = result2,
					name = (value?.Text("mapName") ?? result2.ToString()),
					street = (value?.Text("streetName") ?? "")
				};
			}
		}
		TextAsset val = Resources.Load<TextAsset>("Field/FieldMobs");
		if ((Object)(object)val != (Object)null)
		{
			List<object> list = GetList(MiniJson.Parse(val.text) as Dictionary<string, object>, "mobs");
			if (list != null)
			{
				foreach (object item2 in list)
				{
					if (item2 is Dictionary<string, object> dictionary2)
					{
						FieldMob fieldMob = new FieldMob
						{
							id = (int)Num(dictionary2, "id"),
							name = Str(dictionary2, "name"),
							artKey = Str(dictionary2, "artKey"),
							stats = (dictionary2.ContainsKey("stats") ? (dictionary2["stats"] as Dictionary<string, object>) : new Dictionary<string, object>())
						};
						MobDefenseCatalog.Shared.Apply(fieldMob.id, fieldMob.stats);
						Mobs[fieldMob.id] = fieldMob;
					}
				}
			}
		}
		TextAsset val2 = Resources.Load<TextAsset>("Field/FieldDrops");
		if ((Object)(object)val2 != (Object)null)
		{
			Dictionary<string, object> dictionary3 = MiniJson.Parse(val2.text) as Dictionary<string, object>;
			Dictionary<string, object> dictionary4 = ((dictionary3 != null && dictionary3.ContainsKey("drops")) ? (dictionary3["drops"] as Dictionary<string, object>) : null);
			if (dictionary4 != null)
			{
				foreach (KeyValuePair<string, object> item3 in dictionary4)
				{
					if (!int.TryParse(item3.Key, out var result3))
					{
						continue;
					}
					List<FieldDropEntry> list2 = new List<FieldDropEntry>();
					foreach (object item4 in item3.Value as List<object>)
					{
						if (item4 is Dictionary<string, object> d)
						{
							list2.Add(new FieldDropEntry
							{
								item = (int)Num(d, "item"),
								min = (int)Num(d, "min", 1f),
								max = (int)Num(d, "max", 1f),
								quest = (int)Num(d, "quest"),
								chance = (int)Num(d, "chance")
							});
						}
					}
					Drops[result3] = list2;
				}
			}
			List<object> list3 = ((dictionary3 != null) ? (dictionary3["globalDrops"] as List<object>) : null);
			if (list3 != null)
			{
				foreach (object item5 in list3)
				{
					if (item5 is Dictionary<string, object> d2)
					{
						GlobalDrops.Add(new FieldDropEntry
						{
							item = (int)Num(d2, "item"),
							min = (int)Num(d2, "min", 1f),
							max = (int)Num(d2, "max", 1f),
							quest = (int)Num(d2, "quest"),
							chance = (int)Num(d2, "chance")
						});
					}
				}
			}
			Dictionary<string, object> dictionary5 = ((dictionary3 != null) ? (dictionary3["items"] as Dictionary<string, object>) : null);
			if (dictionary5 != null)
			{
				foreach (KeyValuePair<string, object> item6 in dictionary5)
				{
					if (item6.Value is Dictionary<string, object> d3)
					{
						FieldItemInfo fieldItemInfo = new FieldItemInfo
						{
							id = (int)Num(d3, "id"),
							name = Str(d3, "name"),
							cat = Str(d3, "cat"),
							icon = Str(d3, "icon")
						};
						Items[fieldItemInfo.id] = fieldItemInfo;
					}
				}
			}
		}
		TextAsset val3 = Resources.Load<TextAsset>("Field/FieldDropTable");
		if ((Object)(object)val3 != (Object)null && LoadFullDropTable(val3.text))
		{
			Debug.Log((object)("[Field] 源端掉落全量 " + Drops.Count + " 只怪 / " + GlobalDrops.Count + " 条全局"));
		}
		FieldPortalArt.Ensure();
		DropStore.JsonDrops = () =>
		{
			Dictionary<int, List<DropEntry>> dictionary6 = new Dictionary<int, List<DropEntry>>();
			foreach (KeyValuePair<int, List<FieldDropEntry>> drop in Drops)
			{
				List<DropEntry> list4 = new List<DropEntry>();
				foreach (FieldDropEntry item7 in drop.Value)
				{
					list4.Add(new DropEntry
					{
						item = item7.item,
						min = item7.min,
						max = item7.max,
						quest = item7.quest,
						chance = item7.chance
					});
				}
				dictionary6[drop.Key] = list4;
			}
			return dictionary6;
		};
		DropStore.JsonGlobalDrops = () =>
		{
			List<DropEntry> list4 = new List<DropEntry>();
			foreach (FieldDropEntry globalDrop in GlobalDrops)
			{
				list4.Add(new DropEntry
				{
					item = globalDrop.item,
					min = globalDrop.min,
					max = globalDrop.max,
					quest = globalDrop.quest,
					chance = globalDrop.chance,
					continent = globalDrop.continent
				});
			}
			return list4;
		};
		Ready = Maps.Count > 0;
		Debug.Log((object)("[Field] 原版地图 " + Maps.Count + " 张，怪物 " + Mobs.Count + " 种，掉落表 " + Drops.Count + " 份，物品 " + Items.Count + " 件"));
	}

	private static bool LoadFullDropTable(string text)
	{
		if (!DropTableText.TryParse(text, out var mobs, out var global))
		{
			return false;
		}
		Drops.Clear();
		foreach (KeyValuePair<int, List<DropEntry>> item in mobs)
		{
			List<FieldDropEntry> list = new List<FieldDropEntry>(item.Value.Count);
			foreach (DropEntry item2 in item.Value)
			{
				list.Add(new FieldDropEntry
				{
					item = item2.item,
					min = item2.min,
					max = item2.max,
					quest = item2.quest,
					chance = item2.chance
				});
			}
			Drops[item.Key] = list;
		}
		GlobalDrops.Clear();
		foreach (DropEntry item3 in global)
		{
			GlobalDrops.Add(new FieldDropEntry
			{
				item = item3.item,
				min = item3.min,
				max = item3.max,
				quest = item3.quest,
				chance = item3.chance,
				continent = (int)item3.continent
			});
		}
		return true;
	}

	public static void ResetMap(int id)
	{
		loadedMaps.Remove(id);
		MapItemService.InvalidateMapGeometry(id);
	}

	public static FieldMap GetMap(int id)
	{
		Ensure();
		if (!Maps.TryGetValue(id, out var value))
		{
			return null;
		}
		if (loadedMaps.Contains(id))
		{
			return value;
		}
		value = NativeMapView.LoadMap(value);
		foreach (FieldSpawn spawn in value.spawns)
		{
			LoadMob(spawn.mob);
		}
		Maps[id] = value;
		loadedMaps.Add(id);
		return value;
	}

	public static void LoadMob(int id)
	{
		if (loadedMobs.Contains(id))
		{
			return;
		}
		string text = "Mob/" + id.ToString("D7") + ".img";
		if (!WzAssets.HasFile(text))
		{
			Debug.LogWarning((object)("[Field] Missing original monster IMG: " + text));
			return;
		}
		ImgNode imgNode = WzAssets.FileRoot(text).Get("info");
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		foreach (ImgNode child in imgNode.Children)
		{
			if (child.Value != null)
			{
				dictionary[child.Name] = child.Value;
			}
		}
		MobDefenseCatalog.Shared.Apply(id, dictionary);
		ImgNode imgNode2 = WzAssets.FileRoot("String/Mob.img").Get(id.ToString());
		Mobs[id] = new FieldMob
		{
			id = id,
			name = (imgNode2?.Text("name") ?? id.ToString()),
			artKey = id.ToString(),
			stats = dictionary
		};
		loadedMobs.Add(id);
	}

	private static FieldMap BuildMap(Dictionary<string, object> d)
	{
		//IL_0692: Unknown result type (might be due to invalid IL or missing references)
		//IL_0697: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a9: Unknown result type (might be due to invalid IL or missing references)
		if (d == null || !d.ContainsKey("vr"))
		{
			return null;
		}
		FieldMap fieldMap = new FieldMap
		{
			id = (int)Num(d, "id"),
			name = Str(d, "name"),
			street = Str(d, "street"),
			bgm = Str(d, "bgm"),
			town = (Num(d, "town") != 0f),
			returnMap = (int)Num(d, "returnMap"),
			fieldLimit = (int)Num(d, "fieldLimit"),
			mobRate = Num(d, "mobRate", 1f)
		};
		List<object> list = GetList(d, "vr");
		if (list != null && list.Count >= 4)
		{
			for (int i = 0; i < 4; i++)
			{
				fieldMap.vr[i] = (int)ToF(list[i]);
			}
		}
		List<object> list2 = GetList(d, "footholds");
		if (list2 != null)
		{
			foreach (object item in list2)
			{
				if (item is Dictionary<string, object> d2)
				{
					fieldMap.footholds.Add(new FieldFoothold
					{
						id = (int)Num(d2, "id"),
						layer = (int)Num(d2, "l"),
						group = (int)Num(d2, "g"),
						x1 = Num(d2, "x1"),
						y1 = Num(d2, "y1"),
						x2 = Num(d2, "x2"),
						y2 = Num(d2, "y2")
					});
				}
			}
		}
		List<object> list3 = GetList(d, "ropes");
		if (list3 != null)
		{
			foreach (object item2 in list3)
			{
				if (item2 is Dictionary<string, object> d3)
				{
					fieldMap.ropes.Add(new FieldRope
					{
						x = Num(d3, "x"),
						y1 = Num(d3, "y1"),
						y2 = Num(d3, "y2"),
						ladder = (Num(d3, "l") != 0f),
						upperExit = (Num(d3, "uf") != 0f)
					});
				}
			}
		}
		List<object> list4 = GetList(d, "portals");
		if (list4 != null)
		{
			foreach (object item3 in list4)
			{
				if (item3 is Dictionary<string, object> d4)
				{
					fieldMap.portals.Add(new FieldPortal
					{
						id = (int)Num(d4, "id"),
						name = Str(d4, "pn"),
						targetName = Str(d4, "tn"),
						type = (int)Num(d4, "pt"),
						tm = (int)Num(d4, "tm"),
						x = Num(d4, "x"),
						y = Num(d4, "y"),
						script = Str(d4, "script")
					});
				}
			}
		}
		List<object> list5 = GetList(d, "spawns");
		if (list5 != null)
		{
			foreach (object item4 in list5)
			{
				if (item4 is Dictionary<string, object> d5)
				{
					fieldMap.spawns.Add(new FieldSpawn
					{
						mob = (int)Num(d5, "mob"),
						mobTime = (int)Num(d5, "mobTime"),
						x = Num(d5, "x"),
						y = Num(d5, "y"),
						fh = (int)Num(d5, "fh"),
						rx0 = Num(d5, "rx0"),
						rx1 = Num(d5, "rx1"),
						cy = Num(d5, "cy"),
						dir = (int)Num(d5, "f"),
						team = (int)Num(d5, "team", -1f)
					});
				}
			}
		}
		List<object> list6 = GetList(d, "npcs");
		if (list6 != null)
		{
			foreach (object item5 in list6)
			{
				if (item5 is Dictionary<string, object> dictionary)
				{
					fieldMap.npcs.Add(new FieldNpc
					{
						id = (int)Num(dictionary, "id"),
						x = Num(dictionary, "x"),
						y = Num(dictionary, "y"),
						hide = (int)Num(dictionary, "hide"),
						art = (dictionary.ContainsKey("art") ? (dictionary["art"] as Dictionary<string, object>) : null)
					});
				}
			}
		}
		List<object> list7 = GetList(d, "backs");
		if (list7 != null)
		{
			foreach (object item6 in list7)
			{
				if (item6 is Dictionary<string, object> d6)
				{
					fieldMap.backs.Add(new FieldBack
					{
						file = Str(d6, "file"),
						origin = Vec(d6, "origin"),
						size = Vec(d6, "size"),
						x = (int)Num(d6, "x"),
						y = (int)Num(d6, "y"),
						rx = (int)Num(d6, "rx"),
						ry = (int)Num(d6, "ry"),
						cx = (int)Num(d6, "cx"),
						cy = (int)Num(d6, "cy"),
						type = (int)Num(d6, "type"),
						front = (int)Num(d6, "front"),
						alpha = (int)Num(d6, "a", 255f),
						no = (int)Num(d6, "no")
					});
				}
			}
		}
		Dictionary<string, object> dictionary2 = (d.ContainsKey("body") ? (d["body"] as Dictionary<string, object>) : null);
		List<object> list8 = ((dictionary2 != null) ? GetList(dictionary2, "chunks") : null);
		if (list8 != null)
		{
			foreach (object item7 in list8)
			{
				if (item7 is Dictionary<string, object> d7)
				{
					List<object> list9 = GetList(d7, "pos");
					List<object> list10 = GetList(d7, "size");
					if (list9 != null && list10 != null && list9.Count >= 2 && list10.Count >= 2)
					{
						fieldMap.chunks.Add(new FieldChunk
						{
							file = Str(d7, "file"),
							x = ToF(list9[0]),
							y = ToF(list9[1]),
							w = ToF(list10[0]),
							h = ToF(list10[1])
						});
					}
				}
			}
		}
		return fieldMap;
	}

	public static float ToF(object o)
	{
		if (o == null)
		{
			return 0f;
		}
		if (o is double)
		{
			return (float)(double)o;
		}
		if (o is float)
		{
			return (float)o;
		}
		if (o is int)
		{
			return (int)o;
		}
		if (!float.TryParse(o.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			return 0f;
		}
		return result;
	}

	public static float Num(Dictionary<string, object> d, string key, float def = 0f)
	{
		if (d == null || !d.ContainsKey(key))
		{
			return def;
		}
		return ToF(d[key]);
	}

	public static string Str(Dictionary<string, object> d, string key)
	{
		if (d == null || !d.ContainsKey(key) || d[key] == null)
		{
			return "";
		}
		return d[key].ToString();
	}

	public static List<object> GetList(Dictionary<string, object> d, string key)
	{
		if (d == null || !d.ContainsKey(key))
		{
			return null;
		}
		return d[key] as List<object>;
	}

	public static Vector2 Vec(Dictionary<string, object> d, string key)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		List<object> list = GetList(d, key);
		if (list == null || list.Count < 2)
		{
			return Vector2.zero;
		}
		return new Vector2(ToF(list[0]), ToF(list[1]));
	}
}
