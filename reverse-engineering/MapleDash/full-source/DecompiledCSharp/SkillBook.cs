using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MapleDash.Wz;
using UnityEngine;

public static class SkillBook
{
	private static readonly Dictionary<int, SkillData> byId = new Dictionary<int, SkillData>();

	private static readonly List<Dictionary<string, object>> lines = new List<Dictionary<string, object>>();

	private static readonly Dictionary<int, Dictionary<string, object>> lineByJob = new Dictionary<int, Dictionary<string, object>>();

	private static Dictionary<string, object> jobNames = new Dictionary<string, object>();

	private static readonly Dictionary<int, string> matrixNames = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> matrixDescs = new Dictionary<int, string>();

	private static readonly Dictionary<int, Dictionary<string, string>> FieldAliases = new Dictionary<int, Dictionary<string, string>>
	{
		{
			400011039,
			new Dictionary<string, string>
			{
				{ "q", "damage" },
				{ "q2", "attackCount" }
			}
		},
		{
			400021078,
			new Dictionary<string, string>
			{
				{ "dummyStr2", "damage" },
				{ "dummyStr", "mobCount" },
				{ "dummyStr3", "attackCount" }
			}
		},
		{
			400021155,
			new Dictionary<string, string>
			{
				{ "dummyStr2", "damage" },
				{ "dummyStr", "mobCount" },
				{ "dummyStr3", "attackCount" }
			}
		},
		{
			400041075,
			new Dictionary<string, string>
			{
				{ "z", "damage" },
				{ "u", "attackCount" },
				{ "y", "mobCount" }
			}
		},
		{
			400051024,
			new Dictionary<string, string>
			{
				{ "q", "damage" },
				{ "q2", "attackCount" },
				{ "u", "mobCount" }
			}
		}
	};

	public static bool Ready { get; private set; }

	public static string SpecialKind(int id)
	{
		if (id != 1111007 && id != 1211009 && id != 1311007)
		{
			if (!NativeTeleportMovement.IsSkill(id))
			{
				switch (id)
				{
				default:
					return null;
				case 4111006:
				case 11101005:
				case 14101004:
					return "FlashJump";
				case 4121003:
				case 4221003:
					return "Attack";
				}
			}
			return "Teleport";
		}
		return "Dispel";
	}

	public static void Ensure()
	{
		if (Ready)
		{
			return;
		}
		Ready = true;
		TextAsset val = Resources.Load<TextAsset>("UI/JobTree");
		if ((Object)(object)val == (Object)null)
		{
			Debug.LogError((object)"[SkillBook] 缺少 UI/JobTree.json");
			return;
		}
		if (!(MiniJson.Parse(val.text) is Dictionary<string, object> dictionary))
		{
			Debug.LogError((object)"[SkillBook] JobTree.json 解析失败");
			return;
		}
		bool flag = default;
		foreach (KeyValuePair<string, object> item2 in (Dictionary<string, object>)dictionary["skills"])
		{
			Dictionary<string, object> dictionary2 = item2.Value as Dictionary<string, object>;
			SkillData skillData = new SkillData
			{
				id = Art.Id(dictionary2, "id"),
				maxLevel = Mathf.Max(1, (int)Art.Num(dictionary2, "maxLevel", 1f)),
				masterLevel = (int)Art.Num(dictionary2, "masterLevel"),
				job = (int)Art.Num(dictionary2, "job"),
				name = (dictionary2.TryGetValue("name", out var value) ? (value as string) : ""),
				desc = (dictionary2.TryGetValue("desc", out var value2) ? (value2 as string) : ""),
				type = (dictionary2.TryGetValue("type", out var value3) ? (value3 as string) : "attack"),
				icon = (dictionary2.TryGetValue("icon", out var value4) ? (value4 as string) : "")
			};
			skillData.levels = (dictionary2.TryGetValue("levels", out var value5) ? (value5 as Dictionary<string, object>) : skillData.levels);
			skillData.autoLearn = dictionary2.TryGetValue("auto", out var value6) && value6 != null && value6.ToString().ToLowerInvariant() == "true";
			skillData.hexa = dictionary2.TryGetValue("hexa", out var value7) && value7 != null && value7.ToString().ToLowerInvariant() == "true";
			skillData.noName = string.IsNullOrEmpty(skillData.name);
			if (skillData.noName)
			{
				skillData.name = skillData.id.ToString();
			}
			if (dictionary2.TryGetValue("req", out var value8) && value8 is List<object> list)
			{
				foreach (object item3 in list)
				{
					if (item3 is List<object> { Count: >=2 } list2)
					{
						skillData.req.Add(new int[2]
						{
							int.Parse(list2[0].ToString()),
							int.Parse(list2[1].ToString())
						});
					}
				}
			}
			int num;
			if (dictionary2.TryGetValue("magic", out var value9))
			{
				if (value9 is bool)
				{
					flag = (bool)value9;
					num = 1;
				}
				else
				{
					num = 0;
				}
			}
			else
			{
				num = 0;
			}
			if (((uint)num & (flag ? 1u : 0u)) != 0)
			{
				skillData.magic = true;
			}
			foreach (object value17 in skillData.levels.Values)
			{
				if (value17 is Dictionary<string, object> dictionary3 && dictionary3.ContainsKey("mad"))
				{
					skillData.magic = true;
					break;
				}
			}
			skillData.magic = MagicDamageOf(skillData.id, skillData.magic);
			byId[skillData.id] = skillData;
		}
		foreach (object item4 in (List<object>)dictionary["jobs"])
		{
			if (item4 is Dictionary<string, object> dictionary4)
			{
				int num2 = Art.Id(dictionary4, "id");
				List<object> value10 = new List<object>();
				if (dictionary4.TryGetValue("skills", out var value11) && value11 is List<object> list3)
				{
					value10 = list3;
				}
				Dictionary<string, object> item = new Dictionary<string, object>
				{
					{ "job", num2 },
					{
						"name",
						dictionary4.TryGetValue("name", out var value12) ? (value12 as string) : ""
					},
					{
						"book",
						dictionary4.TryGetValue("book", out var value13) ? (value13 as string) : ""
					},
					{ "skills", value10 }
				};
				Dictionary<string, object> dictionary5 = new Dictionary<string, object>
				{
					{ "id", num2 },
					{
						"name",
						dictionary4.TryGetValue("name", out var value14) ? (value14 as string) : ""
					},
					{
						"book",
						dictionary4.TryGetValue("book", out var value15) ? (value15 as string) : ""
					},
					{
						"tabs",
						new List<object> { item }
					}
				};
				lines.Add(dictionary5);
				lineByJob[num2] = dictionary5;
			}
		}
		if (dictionary.TryGetValue("jobNames", out var value16))
		{
			jobNames = value16 as Dictionary<string, object>;
		}
		LoadMatrixSkills();
	}

	private static void LoadMatrixSkills()
	{
		TextAsset val = Resources.Load<TextAsset>("UI/VMatrixSkills");
		if ((Object)(object)val == (Object)null)
		{
			Debug.LogWarning((object)"[SkillBook] 缺少 UI/VMatrixSkills.json，五转核心技能不可施放");
			return;
		}
		if (!(MiniJson.Parse(val.text) is Dictionary<string, object> dictionary) || !dictionary.TryGetValue("skills", out var value) || !(value is Dictionary<string, object> dictionary2))
		{
			Debug.LogWarning((object)"[SkillBook] VMatrixSkills.json 解析失败");
			return;
		}
		bool flag = default;
		bool flag2 = default;
		foreach (KeyValuePair<string, object> item in dictionary2)
		{
			if (!(item.Value is Dictionary<string, object> dictionary3))
			{
				continue;
			}
			int num = Art.Id(dictionary3, "id");
			if (num == 0)
			{
				continue;
			}
			if (byId.TryGetValue(num, out var value2) && num < 400000000)
			{
				int num2;
				if (dictionary3.TryGetValue("magic", out var value3))
				{
					if (value3 is bool)
					{
						flag = (bool)value3;
						num2 = 1;
					}
					else
					{
						num2 = 0;
					}
				}
				else
				{
					num2 = 0;
				}
				if (((uint)num2 & (flag ? 1u : 0u)) != 0)
				{
					value2.magic = true;
				}
				continue;
			}
			SkillData skillData = new SkillData
			{
				id = num,
				maxLevel = Mathf.Max(1, (int)Art.Num(dictionary3, "maxLevel", 1f)),
				masterLevel = (int)Art.Num(dictionary3, "masterLevel"),
				job = (int)Art.Num(dictionary3, "job"),
				name = (dictionary3.TryGetValue("name", out var value4) ? (value4 as string) : ""),
				desc = (dictionary3.TryGetValue("desc", out var value5) ? (value5 as string) : ""),
				type = (dictionary3.TryGetValue("type", out var value6) ? (value6 as string) : "attack"),
				icon = (dictionary3.TryGetValue("icon", out var value7) ? (value7 as string) : ""),
				hexa = true
			};
			skillData.levels = (dictionary3.TryGetValue("levels", out var value8) ? (value8 as Dictionary<string, object>) : skillData.levels);
			if (dictionary3.TryGetValue("attackInfo", out var value9) && value9 is Dictionary<string, object> attackInfo)
			{
				skillData.attackInfo = attackInfo;
			}
			skillData.noName = string.IsNullOrEmpty(skillData.name);
			if (skillData.noName)
			{
				skillData.name = num.ToString();
			}
			if (dictionary3.TryGetValue("req", out var value10) && value10 is List<object> list)
			{
				foreach (object item2 in list)
				{
					if (item2 is List<object> { Count: >=2 } list2)
					{
						skillData.req.Add(new int[2]
						{
							int.Parse(list2[0].ToString()),
							int.Parse(list2[1].ToString())
						});
					}
				}
			}
			int num3;
			if (dictionary3.TryGetValue("magic", out var value11))
			{
				if (value11 is bool)
				{
					flag2 = (bool)value11;
					num3 = 1;
				}
				else
				{
					num3 = 0;
				}
			}
			else
			{
				num3 = 0;
			}
			if (((uint)num3 & (flag2 ? 1u : 0u)) != 0)
			{
				skillData.magic = true;
			}
			foreach (object value15 in skillData.levels.Values)
			{
				if (value15 is Dictionary<string, object> dictionary4 && dictionary4.ContainsKey("mad"))
				{
					skillData.magic = true;
					break;
				}
			}
			skillData.magic = MagicDamageOf(skillData.id, skillData.magic);
			byId[skillData.id] = skillData;
		}
		if (!dictionary.TryGetValue("names", out var value12) || !(value12 is Dictionary<string, object> dictionary5))
		{
			return;
		}
		foreach (KeyValuePair<string, object> item3 in dictionary5)
		{
			if (int.TryParse(item3.Key, out var result) && item3.Value is Dictionary<string, object> dictionary6)
			{
				if (dictionary6.TryGetValue("name", out var value13) && value13 is string { Length: >0 } text)
				{
					matrixNames[result] = text;
				}
				if (dictionary6.TryGetValue("desc", out var value14) && value14 is string { Length: >0 } text2)
				{
					matrixDescs[result] = text2;
				}
			}
		}
	}

	public static SkillData Get(int id)
	{
		Ensure();
		if (byId.TryGetValue(id, out var value))
		{
			return value;
		}
		if (NativeSoulWeapon.IsSkill(id))
		{
			return byId[id] = NativeSoulWeapon.Definition(id);
		}
		ImgNode imgNode = WzAssets.FileRoot("Skill/" + (id / 10000).ToString("D3") + ".img")?.Get("skill/" + id.ToString("D7"));
		ImgNode imgNode2 = imgNode?.Get("level");
		ImgNode imgNode3 = imgNode?.Get("common");
		int num = imgNode3?.Int("maxLevel") ?? 0;
		if (imgNode2 == null && num <= 0)
		{
			return null;
		}
		ImgNode imgNode4 = WzAssets.FileRoot("String/Skill.img")?.Get(id.ToString("D7"));
		string value2 = imgNode4?.Text("name") ?? "";
		string value3 = imgNode4?.Text("desc") ?? "";
		if (value2.Length == 0)
		{
			matrixNames.TryGetValue(id, out value2);
		}
		if (string.IsNullOrEmpty(value3))
		{
			matrixDescs.TryGetValue(id, out value3);
		}
		SkillData skillData2 = new SkillData();
		skillData2.id = id;
		skillData2.job = id / 10000;
		skillData2.name = value2 ?? "";
		skillData2.desc = value3 ?? "";
		skillData2.masterLevel = imgNode.Int("masterLevel");
		skillData2.icon = ((imgNode.Get("icon") != null) ? ("wz://Skill/" + (id / 10000).ToString("D3") + ".img/skill/" + id.ToString("D7") + "/icon") : "");
		skillData2.type = "passive";
		value = skillData2;
		if (imgNode2 == null)
		{
			for (int i = 1; i <= num; i++)
			{
				value.levels[i.ToString()] = ExpandCommon(imgNode3, i);
			}
			value.maxLevel = num;
		}
		else
		{
			foreach (ImgNode child in imgNode2.Children)
			{
				if (!int.TryParse(child.Name, out var result))
				{
					continue;
				}
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				foreach (ImgNode child2 in child.Children)
				{
					ImgNode imgNode5 = child2.TryResolve();
					if (imgNode5 != null)
					{
						if (imgNode5.Kind == "Shape2D#Vector2D")
						{
							dictionary[child2.Name] = new List<object> { imgNode5.X, imgNode5.Y };
						}
						else if (imgNode5.Value != null && imgNode5.Kind != "Canvas")
						{
							dictionary[child2.Name] = imgNode5.Value;
						}
					}
				}
				value.levels[result.ToString()] = dictionary;
				value.maxLevel = Mathf.Max(value.maxLevel, result);
				if (dictionary.ContainsKey("mad"))
				{
					value.magic = true;
				}
			}
		}
		if (value.maxLevel == 0)
		{
			return null;
		}
		if (imgNode.Int("info/magicDamage") != 0)
		{
			value.magic = true;
		}
		value.magic = MagicDamageOf(id, value.magic);
		ApplyFieldAliases(id, value);
		Dictionary<string, object> dictionary2 = value.Level(1) ?? new Dictionary<string, object>();
		if (imgNode.Get("summon") != null)
		{
			value.type = "summon";
		}
		else if (dictionary2.ContainsKey("damage") || dictionary2.ContainsKey("fixdamage") || dictionary2.ContainsKey("mad") || dictionary2.ContainsKey("attackCount"))
		{
			value.type = "attack";
		}
		else if (dictionary2.ContainsKey("time") || dictionary2.ContainsKey("prop"))
		{
			value.type = "buff";
		}
		if (SkillSystem.IsCurseParent(id))
		{
			value.type = "buff";
		}
		if (SkillSystem.IsStackSkill(id))
		{
			value.type = "buff";
		}
		value.noName = string.IsNullOrEmpty(value.name);
		if (value.noName)
		{
			value.name = id.ToString();
		}
		byId[id] = value;
		return value;
	}

	private static void ApplyFieldAliases(int id, SkillData d)
	{
		if (!FieldAliases.TryGetValue(id, out var value))
		{
			return;
		}
		foreach (object value3 in d.levels.Values)
		{
			if (!(value3 is Dictionary<string, object> dictionary))
			{
				continue;
			}
			foreach (KeyValuePair<string, string> item in value)
			{
				if (dictionary.TryGetValue(item.Key, out var value2) && !dictionary.ContainsKey(item.Value))
				{
					dictionary[item.Value] = value2;
				}
			}
		}
	}

	private static Dictionary<string, object> ExpandCommon(ImgNode common, int level)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		foreach (ImgNode child in common.Children)
		{
			ImgNode imgNode = child.TryResolve();
			if (imgNode == null)
			{
				continue;
			}
			if (imgNode.Kind == "Shape2D#Vector2D")
			{
				dictionary[child.Name] = new List<object> { imgNode.X, imgNode.Y };
			}
			else if (imgNode.Value is int num)
			{
				dictionary[child.Name] = num;
			}
			else if (imgNode.Value is float num2)
			{
				dictionary[child.Name] = num2;
			}
			else if (imgNode.Value is double num3)
			{
				dictionary[child.Name] = num3;
			}
			else if (imgNode.Value is string { Length: not 0 } text)
			{
				float num4 = EvalCommon(text, level, float.NaN);
				if (!float.IsNaN(num4))
				{
					float num5 = Mathf.Round(num4);
					dictionary[child.Name] = ((Mathf.Abs(num4 - num5) < 0.0001f) ? ((object)(int)num5) : ((object)num4));
				}
			}
		}
		return dictionary;
	}

	public static int MaxLevelOf(int id)
	{
		return Get(id)?.maxLevel ?? 0;
	}

	public static float Common(int id, string key, int level, float fallback = 0f)
	{
		ImgNode imgNode = CommonNode(id, key);
		if (imgNode == null)
		{
			return fallback;
		}
		if (imgNode.Value is int num)
		{
			return num;
		}
		object value = imgNode.Value;
		if (value is float)
		{
			return (float)value;
		}
		if (imgNode.Value is double num2)
		{
			return (float)num2;
		}
		string text = imgNode.Value as string;
		if (!string.IsNullOrEmpty(text))
		{
			return EvalCommon(text, level, fallback);
		}
		return fallback;
	}

	public static Vector2 CommonVector(int id, string key, Vector2 fallback)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = CommonNode(id, key);
		if (imgNode == null || imgNode.Kind != "Shape2D#Vector2D")
		{
			return fallback;
		}
		return new Vector2((float)imgNode.X, (float)imgNode.Y);
	}

	private static ImgNode CommonNode(int id, string key)
	{
		return WzAssets.FileRoot("Skill/" + (id / 10000).ToString("D3") + ".img")?.Get("skill/" + id.ToString("D7") + "/common")?.Get(key)?.TryResolve();
	}

	private static float EvalCommon(string text, int level, float fallback)
	{
		StringBuilder stringBuilder = new StringBuilder(text.Length + 8);
		string text2 = level.ToString(CultureInfo.InvariantCulture);
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			stringBuilder.Append((c == 'x' || c == 'X') ? text2 : c.ToString());
		}
		string text3 = stringBuilder.ToString();
		int j = 0;
		float result = ParseCommonSum(text3, ref j, fallback);
		for (; j < text3.Length && text3[j] == ' '; j++)
		{
		}
		if (j < text3.Length)
		{
			return fallback;
		}
		return result;
	}

	private static float ParseCommonSum(string text, ref int at, float fallback)
	{
		float num = ParseCommonProduct(text, ref at, fallback);
		while (true)
		{
			if (at < text.Length && text[at] == ' ')
			{
				at++;
				continue;
			}
			if (at >= text.Length || (text[at] != '+' && text[at] != '-'))
			{
				break;
			}
			char c = text[at++];
			float num2 = ParseCommonProduct(text, ref at, fallback);
			num = ((c == '+') ? (num + num2) : (num - num2));
		}
		return num;
	}

	private static float ParseCommonProduct(string text, ref int at, float fallback)
	{
		float num = ParseCommonAtom(text, ref at, fallback);
		while (true)
		{
			if (at < text.Length && text[at] == ' ')
			{
				at++;
				continue;
			}
			if (at >= text.Length || (text[at] != '*' && text[at] != '/'))
			{
				break;
			}
			char c = text[at++];
			float num2 = ParseCommonAtom(text, ref at, fallback);
			if (c == '*')
			{
				num *= num2;
			}
			else if (Mathf.Abs(num2) > 0.0001f)
			{
				num /= num2;
			}
		}
		return num;
	}

	private static float ParseCommonAtom(string text, ref int at, float fallback)
	{
		while (at < text.Length && text[at] == ' ')
		{
			at++;
		}
		if (at >= text.Length)
		{
			return fallback;
		}
		if (text[at] == '(')
		{
			at++;
			float result = ParseCommonSum(text, ref at, fallback);
			if (at < text.Length && text[at] == ')')
			{
				at++;
			}
			return result;
		}
		if (text[at] == '-')
		{
			at++;
			return 0f - ParseCommonAtom(text, ref at, fallback);
		}
		if (char.IsLetter(text[at]))
		{
			int num = at;
			while (at < text.Length && (char.IsLetterOrDigit(text[at]) || text[at] == '_'))
			{
				at++;
			}
			string name = text.Substring(num, at - num);
			while (at < text.Length && text[at] == ' ')
			{
				at++;
			}
			if (at >= text.Length || text[at] != '(')
			{
				return fallback;
			}
			at++;
			float value = ParseCommonSum(text, ref at, fallback);
			if (at < text.Length && text[at] == ')')
			{
				at++;
			}
			return ApplyCommonFunction(name, value, fallback);
		}
		int num2 = at;
		while (at < text.Length && (char.IsDigit(text[at]) || text[at] == '.'))
		{
			at++;
		}
		if (at == num2)
		{
			return fallback;
		}
		if (!float.TryParse(text.Substring(num2, at - num2), NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
		{
			return fallback;
		}
		return result2;
	}

	private static float ApplyCommonFunction(string name, float value, float fallback)
	{
		if (name == "d")
		{
			return Mathf.Floor(value);
		}
		if (name == "u")
		{
			return Mathf.Ceil(value);
		}
		if (name.Length > 3 && name.StartsWith("log") && int.TryParse(name.Substring(3), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) && result > 1 && value > 0f)
		{
			return Mathf.Floor(Mathf.Log(value) / Mathf.Log((float)result));
		}
		return fallback;
	}

	private static bool MagicDamageOf(int id, bool original)
	{
		int num = id / 10000;
		if (num == 1112 || num == 1212 || num == 1312 || num == 1412 || num == 1512)
		{
			return num == 1212;
		}
		return original;
	}

	public static bool IsFourthJobSkill(int skillId)
	{
		return skillId / 10000 % 10 == 2;
	}

	public static int MasterCapOf(int id)
	{
		SkillData skillData = Get(id);
		if (skillData == null)
		{
			return 0;
		}
		int num = skillData.maxLevel;
		if (IsFourthJobSkill(skillData.id) && skillData.masterLevel > 0)
		{
			num = Mathf.Min(num, skillData.masterLevel);
		}
		int num2 = SkillBookItems.MasterLevelFor(skillData.id);
		if (num2 <= num)
		{
			return num;
		}
		return num2;
	}

	public static Dictionary<string, object> LineFor(string cls)
	{
		return LineForJob(JobDefs.JobIdOf(cls));
	}

	public static Dictionary<string, object> LineForJob(int jobId)
	{
		Ensure();
		if (!lineByJob.TryGetValue(jobId, out var value))
		{
			if (lines.Count <= 0)
			{
				return null;
			}
			return lines[0];
		}
		return value;
	}

	public static Dictionary<string, object> TreeForJob(int jobId)
	{
		Ensure();
		if (lineByJob.TryGetValue(jobId, out var value))
		{
			List<object> list = new List<object>();
			HashSet<int> hashSet = new HashSet<int>();
			int num = jobId;
			while (num >= 0 && hashSet.Add(num))
			{
				if (lineByJob.TryGetValue(num, out var value2))
				{
					list.Insert(0, ((List<object>)value2["tabs"])[0]);
				}
				num = JobDefs.ParentOf(num);
			}
			return new Dictionary<string, object>(value) { ["tabs"] = list };
		}
		List<string> list2 = new List<string>();
		foreach (Dictionary<string, object> line in lines)
		{
			list2.Add(line.TryGetValue("id", out var value3) ? value3.ToString() : "?");
		}
		Debug.LogWarning((object)("[SkillBook] 没有职业线 " + jobId + "，可用：" + string.Join(",", list2.ToArray())));
		if (lines.Count <= 0)
		{
			return null;
		}
		return lines[0];
	}

	public static List<Dictionary<string, object>> Tabs(Dictionary<string, object> line)
	{
		List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
		if (line == null || !line.TryGetValue("tabs", out var value) || !(value is List<object> list2))
		{
			return list;
		}
		foreach (object item2 in list2)
		{
			if (item2 is Dictionary<string, object> item)
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static string JobName(int job)
	{
		Ensure();
		if (!jobNames.TryGetValue(job.ToString(), out var value))
		{
			return job.ToString();
		}
		return value as string;
	}

	public static NativeSkillDefinition Native(int id, int level)
	{
		SkillData skillData = Get(id);
		Dictionary<string, object> dictionary = skillData?.Level(level);
		if (dictionary == null)
		{
			return null;
		}
		string value = SpecialKind(id) ?? (skillData.IsSummon ? "Summon" : (skillData.IsBuff ? "Buff" : (skillData.IsPassive ? "Passive" : "Attack")));
		ImgNode imgNode = WzAssets.Node("Skill/" + (id / 10000).ToString("D3") + ".img/skill/" + id.ToString("D7"));
		Dictionary<string, object> skill = new Dictionary<string, object>
		{
			{ "id", skillData.id },
			{ "kind", value },
			{ "magic", skillData.magic },
			{
				"element",
				WzAssets.FileRoot("Skill/" + (id / 10000).ToString("D3") + ".img").Get("skill/" + id.ToString("D7"))?.Text("elemAttr") ?? ""
			},
			{ "summonInterval", 1f },
			{
				"skillType",
				imgNode?.Int("skillType") ?? 0
			},
			{
				"areaAttack",
				Mathf.Max(imgNode?.Int("info/areaAttack") ?? 0, imgNode?.Int("info2/areaAttack") ?? 0)
			},
			{
				"vMatrix",
				skillData.hexa || id / 1000000 >= 400
			}
		};
		Dictionary<string, object> dictionary2 = new Dictionary<string, object>(dictionary);
		dictionary2["level"] = level;
		Dictionary<string, object> dictionary3 = JobAttackRow(skillData, level);
		if (dictionary3 != null)
		{
			foreach (KeyValuePair<string, object> item in dictionary3)
			{
				dictionary2[item.Key] = item.Value;
			}
		}
		int num = NativeSkillBoosts.Pct(skillData.id);
		if (num > 0 && dictionary2.ContainsKey("damage"))
		{
			dictionary2["damage"] = (double)(Art.Num(dictionary2, "damage") * (float)(100 + num)) / 100.0;
		}
		return new NativeSkillDefinition(skill, dictionary2);
	}

	private static Dictionary<string, object> JobAttackRow(SkillData d, int level)
	{
		if (d?.attackInfo == null)
		{
			return null;
		}
		string key = level.ToString();
		if (d.attackInfo.TryGetValue(Profile.JobId.ToString(), out var value) && value is Dictionary<string, object> dictionary && dictionary.TryGetValue(key, out var value2))
		{
			return value2 as Dictionary<string, object>;
		}
		int num = Profile.JobId / 10;
		int num2 = int.MaxValue;
		foreach (KeyValuePair<string, object> item in d.attackInfo)
		{
			if (int.TryParse(item.Key, out var result) && result / 10 == num && result < num2)
			{
				num2 = result;
			}
		}
		if (num2 == int.MaxValue)
		{
			return null;
		}
		if (!d.attackInfo.TryGetValue(num2.ToString(), out var value3) || !(value3 is Dictionary<string, object> dictionary2) || !dictionary2.TryGetValue(key, out var value4))
		{
			return null;
		}
		return value4 as Dictionary<string, object>;
	}
}
