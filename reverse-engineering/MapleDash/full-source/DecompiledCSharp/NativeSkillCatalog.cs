using System.Collections.Generic;
using UnityEngine;

public static class NativeSkillCatalog
{
	private static readonly Dictionary<string, List<NativeSkillDefinition>> skills = new Dictionary<string, List<NativeSkillDefinition>>();

	private static bool loaded;

	public static NativeSkillDefinition Get(string key, int level = 1)
	{
		if (!loaded)
		{
			loaded = true;
			TextAsset val = Resources.Load<TextAsset>("GameplaySkills");
			if ((Object)(object)val == (Object)null)
			{
				Debug.LogError((object)"[Skill] 缺少 GameplaySkills 数据");
				return null;
			}
			foreach (Dictionary<string, object> item in (List<object>)(MiniJson.Parse(val.text) as Dictionary<string, object>)["skills"])
			{
				List<NativeSkillDefinition> list = new List<NativeSkillDefinition>();
				foreach (Dictionary<string, object> item2 in (List<object>)item["levels"])
				{
					list.Add(new NativeSkillDefinition(item, item2));
				}
				skills.Add((string)item["key"], list);
			}
		}
		if (!skills.TryGetValue(key, out var value))
		{
			return null;
		}
		return value.Find((NativeSkillDefinition d) => d.Level == level);
	}
}
