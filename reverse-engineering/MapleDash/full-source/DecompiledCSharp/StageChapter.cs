using System;
using System.Collections.Generic;
using UnityEngine;

public class StageChapter
{
	public int chapterId;

	public string name = "";

	public string subtitle = "";

	public string goal = "";

	public Dictionary<string, StageTheme> themes = new Dictionary<string, StageTheme>();

	public Dictionary<string, StageBossDef> bosses = new Dictionary<string, StageBossDef>();

	public List<StageLevel> levels = new List<StageLevel>();

	public static StageChapter Load(string resourcePath)
	{
		TextAsset val = Resources.Load<TextAsset>(resourcePath);
		if ((Object)(object)val == (Object)null)
		{
			Debug.LogWarning((object)("[Stage] 找不到关卡数据：" + resourcePath + "（应放在 Assets/Resources/" + resourcePath + ".json）"));
			return null;
		}
		return FromJson(val.text);
	}

	public static StageChapter FromJson(string json)
	{
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		if (!(MiniJson.Parse(json) is Dictionary<string, object> d))
		{
			Debug.LogError((object)"[Stage] JSON 根节点不是对象");
			return null;
		}
		StageChapter stageChapter = new StageChapter();
		Dictionary<string, object> dictionary = Dict(d, "chapter");
		if (dictionary != null)
		{
			stageChapter.chapterId = Int(dictionary, "id");
			stageChapter.name = Str(dictionary, "name");
			stageChapter.subtitle = Str(dictionary, "subtitle");
			stageChapter.goal = Str(dictionary, "goal");
		}
		Dictionary<string, object> dictionary2 = Dict(d, "themes");
		if (dictionary2 != null)
		{
			foreach (KeyValuePair<string, object> item in dictionary2)
			{
				if (item.Value is Dictionary<string, object> d2)
				{
					stageChapter.themes[item.Key] = new StageTheme
					{
						bgTop = Hex(Str(d2, "bgTop"), new Color(0.11f, 0.13f, 0.18f)),
						bgBottom = Hex(Str(d2, "bgBottom"), new Color(0.16f, 0.19f, 0.25f)),
						mid = Hex(Str(d2, "mid"), new Color(0.19f, 0.23f, 0.29f)),
						ground = Hex(Str(d2, "ground"), new Color(0.17f, 0.19f, 0.23f)),
						groundDeep = Hex(Str(d2, "groundDeep"), new Color(0.12f, 0.13f, 0.16f)),
						accent = Hex(Str(d2, "accent"), new Color(0.42f, 0.62f, 0.5f))
					};
				}
			}
		}
		Dictionary<string, object> dictionary3 = Dict(d, "bosses");
		if (dictionary3 != null)
		{
			foreach (KeyValuePair<string, object> item2 in dictionary3)
			{
				if (!(item2.Value is Dictionary<string, object> d3))
				{
					continue;
				}
				StageBossDef stageBossDef = new StageBossDef
				{
					id = item2.Key,
					name = Str(d3, "name"),
					type = Str(d3, "type"),
					hint = Str(d3, "hint"),
					hp = Flt(d3, "hp", 900f),
					spd = Flt(d3, "spd", 46f),
					scale = Flt(d3, "scale", 3f),
					contact = Flt(d3, "contact", 16f)
				};
				Dictionary<string, object> dictionary4 = Dict(d3, "slam");
				if (dictionary4 != null)
				{
					stageBossDef.slam = new SlamDef
					{
						telegraph = Flt(dictionary4, "telegraph", 1f),
						air = Flt(dictionary4, "air", 0.5f),
						radius = Flt(dictionary4, "radius", 122f),
						damage = Flt(dictionary4, "damage", 26f),
						recover = Flt(dictionary4, "recover", 1.6f)
					};
				}
				Dictionary<string, object> dictionary5 = Dict(d3, "summon");
				if (dictionary5 != null)
				{
					stageBossDef.summon = new SummonDef
					{
						count = Int(dictionary5, "count", 3),
						gap = Flt(dictionary5, "gap", 0.35f),
						type = Str(dictionary5, "type", "mushroom")
					};
				}
				List<object> list = Arr(d3, "phases");
				if (list != null)
				{
					foreach (object item3 in list)
					{
						if (item3 is Dictionary<string, object> d4)
						{
							stageBossDef.phases.Add(new PhaseDef
							{
								at = Flt(d4, "at", 1f),
								cooldown = Flt(d4, "cooldown", 2.2f)
							});
						}
					}
				}
				List<object> list2 = Arr(d3, "pattern");
				if (list2 != null)
				{
					foreach (object item4 in list2)
					{
						if (item4 is string)
						{
							stageBossDef.pattern.Add((string)item4);
						}
					}
				}
				stageChapter.bosses[item2.Key] = stageBossDef;
			}
		}
		List<object> list3 = Arr(d, "levels");
		if (list3 != null)
		{
			foreach (object item5 in list3)
			{
				if (!(item5 is Dictionary<string, object> d5))
				{
					continue;
				}
				StageLevel stageLevel = new StageLevel
				{
					id = Str(d5, "id"),
					name = Str(d5, "name"),
					goal = Str(d5, "goal"),
					theme = Str(d5, "theme"),
					teaches = Str(d5, "teaches"),
					suppression = Int(d5, "suppression"),
					recLevel = Int(d5, "recLevel", 1)
				};
				List<object> list4 = Arr(d5, "dialogue");
				if (list4 != null)
				{
					foreach (object item6 in list4)
					{
						if (item6 is string)
						{
							stageLevel.dialogue.Add((string)item6);
						}
					}
				}
				Dictionary<string, object> dictionary6 = Dict(d5, "rewards");
				if (dictionary6 != null)
				{
					stageLevel.rewardExp = Int(dictionary6, "exp");
					stageLevel.rewardGold = Int(dictionary6, "gold");
				}
				List<object> list5 = Arr(d5, "segments");
				if (list5 != null)
				{
					foreach (object item7 in list5)
					{
						if (!(item7 is Dictionary<string, object> d6))
						{
							continue;
						}
						StageSegment stageSegment = new StageSegment
						{
							kind = Str(d6, "kind", "walk"),
							duration = Flt(d6, "duration"),
							hint = Str(d6, "hint"),
							name = Str(d6, "name"),
							boss = Str(d6, "bossId", Str(d6, "boss"))
						};
						List<object> list6 = Arr(d6, "spawns");
						if (list6 != null)
						{
							foreach (object item8 in list6)
							{
								if (item8 is Dictionary<string, object> d7)
								{
									stageSegment.spawns.Add(new StageSpawn
									{
										type = Str(d7, "type", "mushroom"),
										count = Int(d7, "count", 1),
										gap = Flt(d7, "gap", 0.5f),
										side = Str(d7, "side", "right"),
										z = Str(d7, "z", "random")
									});
								}
							}
						}
						stageLevel.segments.Add(stageSegment);
					}
				}
				stageChapter.levels.Add(stageLevel);
			}
		}
		return stageChapter;
	}

	private static Dictionary<string, object> Dict(Dictionary<string, object> d, string k)
	{
		if (d == null || !d.TryGetValue(k, out var value))
		{
			return null;
		}
		return value as Dictionary<string, object>;
	}

	private static List<object> Arr(Dictionary<string, object> d, string k)
	{
		if (d == null || !d.TryGetValue(k, out var value))
		{
			return null;
		}
		return value as List<object>;
	}

	private static string Str(Dictionary<string, object> d, string k, string def = "")
	{
		if (d == null || !d.TryGetValue(k, out var value) || !(value is string result))
		{
			return def;
		}
		return result;
	}

	private static float Flt(Dictionary<string, object> d, string k, float def = 0f)
	{
		if (d == null || !d.TryGetValue(k, out var value) || value == null)
		{
			return def;
		}
		if (value is double num)
		{
			return (float)num;
		}
		if (value is string s && float.TryParse(s, out var result))
		{
			return result;
		}
		return def;
	}

	private static int Int(Dictionary<string, object> d, string k, int def = 0)
	{
		if (d == null || !d.TryGetValue(k, out var value) || value == null)
		{
			return def;
		}
		if (value is double num)
		{
			return (int)num;
		}
		return def;
	}

	public static Color Hex(string s, Color fallback)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrEmpty(s))
		{
			return fallback;
		}
		s = s.TrimStart('#');
		if (s.Length != 6 && s.Length != 8)
		{
			return fallback;
		}
		int num = Convert.ToInt32(s.Substring(0, 2), 16);
		int num2 = Convert.ToInt32(s.Substring(2, 2), 16);
		int num3 = Convert.ToInt32(s.Substring(4, 2), 16);
		int num4 = ((s.Length == 8) ? Convert.ToInt32(s.Substring(6, 2), 16) : 255);
		return new Color((float)num / 255f, (float)num2 / 255f, (float)num3 / 255f, (float)num4 / 255f);
	}
}
