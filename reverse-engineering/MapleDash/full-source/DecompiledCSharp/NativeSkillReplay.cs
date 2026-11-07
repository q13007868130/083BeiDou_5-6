using System;
using System.Collections.Generic;
using UnityEngine;

public static class NativeSkillReplay
{
	private static Dictionary<int, List<NativeReplayStage>> schedules;

	public static IReadOnlyList<NativeReplayStage> Get(int skill)
	{
		if (schedules == null)
		{
			schedules = new Dictionary<int, List<NativeReplayStage>>();
			TextAsset val = Resources.Load<TextAsset>("NativeSkillReplays");
			if ((Object)(object)val == (Object)null)
			{
				throw new InvalidOperationException("Missing original replay schedules");
			}
			foreach (KeyValuePair<string, object> item in (Dictionary<string, object>)MiniJson.Parse(val.text))
			{
				List<NativeReplayStage> list = new List<NativeReplayStage>();
				foreach (Dictionary<string, object> item2 in (List<object>)item.Value)
				{
					List<object> list2 = (List<object>)item2["times"];
					NativeReplayStage nativeReplayStage = new NativeReplayStage
					{
						Skill = Convert.ToInt32(item2["skill"]),
						Interval = Convert.ToInt32(item2["interval"]),
						Times = new int[list2.Count],
						Fixed = ((string)item2["origin"] == "fixed"),
						OriginalFirst = (bool)item2["originalFirst"],
						OriginalBounds = (bool)item2["originalBounds"],
						Warrior = (bool)item2["warrior"],
						Indexed = (bool)item2["indexed"],
						Total = (item2.TryGetValue("total", out var value) && (bool)value),
						LiveFacing = (item2.TryGetValue("liveFacing", out var value2) && (bool)value2)
					};
					for (int i = 0; i < list2.Count; i++)
					{
						nativeReplayStage.Times[i] = Convert.ToInt32(list2[i]);
					}
					list.Add(nativeReplayStage);
				}
				schedules.Add(int.Parse(item.Key), list);
			}
		}
		if (!schedules.TryGetValue(skill, out var value3))
		{
			return null;
		}
		return value3;
	}

	public static DamageResult[] Adapt(IReadOnlyList<DamageResult> source, int count, double fromPercent, double toPercent, long damageCap = 19999999L)
	{
		if (source.Count == 0)
		{
			return Array.Empty<DamageResult>();
		}
		DamageResult[] array = new DamageResult[Math.Max(1, Math.Min(15, count))];
		for (int i = 0; i < array.Length; i++)
		{
			DamageResult damageResult = source[i % source.Count];
			double val = ((fromPercent > 0.0) ? Math.Floor((double)damageResult.Amount * toPercent / fromPercent + 0.5) : ((double)damageResult.Amount));
			array[i] = new DamageResult((long)Math.Max(0.0, Math.Min(damageCap, val)), damageResult.Critical);
		}
		return array;
	}
}
