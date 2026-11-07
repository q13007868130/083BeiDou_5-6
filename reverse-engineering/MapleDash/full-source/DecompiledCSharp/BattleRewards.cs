using System;
using System.Collections.Generic;
using UnityEngine;

public static class BattleRewards
{
	private static Dictionary<string, Dictionary<string, object>> mobs;

	public static void Drop(string artKey, Vector3 position)
	{
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		if (mobs == null)
		{
			mobs = new Dictionary<string, Dictionary<string, object>>();
			TextAsset val = Resources.Load<TextAsset>("GameplayRewards");
			if ((Object)(object)val == (Object)null)
			{
				throw new InvalidOperationException("缺少 GameplayRewards 数据");
			}
			foreach (Dictionary<string, object> item in (List<object>)((Dictionary<string, object>)MiniJson.Parse(val.text))["mobs"])
			{
				mobs.Add((string)item["key"], item);
			}
		}
		if (mobs.TryGetValue(artKey, out var value))
		{
			Profile.GrantExperience((long)Art.Num(value, "exp"));
			Dictionary<string, object> d = (Dictionary<string, object>)value["meso"];
			int num = (int)Art.Num(d, "minimum");
			int num2 = (int)Art.Num(d, "maximum");
			int num3 = RewardRules.RollMeso((int)Art.Num(d, "chance"), num, num2, Random.Range(0, 999999), (num2 > num) ? Random.Range(0, num2 - num) : 0);
			if (num3 > 0)
			{
				GroundItem.Spawn(0, num3, position);
			}
		}
	}
}
