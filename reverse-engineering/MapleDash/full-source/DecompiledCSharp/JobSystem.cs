using System.Collections.Generic;
using UnityEngine;

public static class JobSystem
{
	public const bool UseAutoAssignStartersAp = true;

	public const bool UseStartingAp4 = false;

	public const int LevelUpSpGain = 3;

	public static int Job => Profile.JobId;

	public static JobDef Def => JobDefs.Get(Job);

	public static string JobName => JobDefs.NameOf(Job);

	public static int Tier => JobDefs.TierOf(Job);

	public static bool CanAdvance(int to, out string reason)
	{
		reason = null;
		JobDef jobDef = JobDefs.Get(to);
		if (jobDef == null)
		{
			reason = "未知职业";
			return false;
		}
		if (jobDef.advance == null)
		{
			reason = "该职业无法直接转入";
			return false;
		}
		if (!JobDefs.IsNext(Job, to))
		{
			reason = "只能转往下一阶职业";
			return false;
		}
		JobAdvance advance = jobDef.advance;
		if (Profile.level < advance.level)
		{
			reason = "等级不足 Lv." + advance.level + "（当前 Lv." + Profile.level + "）";
			return false;
		}
		if (advance.quest > 0 && !Profile.IsQuestCompleted(advance.quest))
		{
			reason = "尚未通过转职考验（任务 " + advance.quest + "）";
			return false;
		}
		if (advance.requireItem > 0 && Profile.ItemCount(advance.requireItem) <= 0)
		{
			reason = "缺少凭证 " + ItemNames.Of(advance.requireItem);
			return false;
		}
		if (jobDef.tier == 3 && Profile.Sp > (Profile.level - 70) * 3 + 10)
		{
			reason = "请先在技能窗用完多余的技能点";
			return false;
		}
		return true;
	}

	public static string StatHint(int job)
	{
		JobDef jobDef = JobDefs.Get(job);
		if (jobDef?.advance == null || jobDef.advance.stat.Count == 0)
		{
			return "";
		}
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, int> item in jobDef.advance.stat)
		{
			list.Add(StatLabel(item.Key) + " " + item.Value);
		}
		return string.Join(" / ", list);
	}

	public static string StatLabel(string key)
	{
		return key switch
		{
			"str" => "力量", 
			"dex" => "敏捷", 
			"int" => "智力", 
			"luk" => "运气", 
			_ => key, 
		};
	}

	public static bool Advance(int to)
	{
		if (!CanAdvance(to, out var _))
		{
			return false;
		}
		JobAdvance advance = JobDefs.Get(to).advance;
		if (advance.consumeItem > 0)
		{
			Profile.RemoveItem(advance.consumeItem, 1);
		}
		foreach (int item in advance.grant)
		{
			Profile.AddItem(item, 1);
		}
		if (advance.resetStats)
		{
			ResetStats(to);
		}
		Profile.AddSp(advance.sp);
		Profile.AddAp(advance.ap);
		JobChangeHpMp(to, out var hpMin, out var hpMax, out var mpMin, out var mpMax);
		Profile.AddBaseHpMp(Random.Range(hpMin, hpMax + 1), Random.Range(mpMin, mpMax + 1));
		Profile.AddEquipSlots(4);
		Profile.SetJob(to);
		foreach (int item2 in advance.teach)
		{
			SkillProgress.SetLevel(item2, SkillBook.MasterCapOf(item2));
		}
		return true;
	}

	public static bool ChangeJobById(int jobId)
	{
		if (!JobDefs.IsValid(jobId))
		{
			Debug.LogWarning((object)("[JobSystem] 未知职业 id " + jobId));
			return false;
		}
		if (jobId == Profile.JobId)
		{
			return true;
		}
		Profile.AddSp(SpForJobChange(jobId));
		Profile.AddAp(ApForJobChange(jobId));
		JobChangeHpMp(jobId, out var hpMin, out var hpMax, out var mpMin, out var mpMax);
		Profile.AddBaseHpMp(Random.Range(hpMin, hpMax + 1), Random.Range(mpMin, mpMax + 1));
		Profile.AddEquipSlots(4);
		Profile.SetJob(jobId);
		JobAdvance jobAdvance = JobDefs.Get(jobId)?.advance;
		if (jobAdvance != null)
		{
			foreach (int item in jobAdvance.teach)
			{
				SkillProgress.SetLevel(item, SkillBook.MasterCapOf(item));
			}
		}
		return true;
	}

	public static bool PromoteToAdvanced()
	{
		int num = JobDefs.AdvancedOf(Profile.JobId);
		if (num <= 0)
		{
			return false;
		}
		if (Profile.JobId == num)
		{
			return true;
		}
		int jobId = Profile.JobId;
		Profile.SetJob(num);
		Debug.Log((object)("[JobSystem] 五转觉醒：职业编码 " + jobId + "(" + JobDefs.NameOf(jobId) + ") → " + num + "(" + JobDefs.NameOf(num) + ")"));
		return true;
	}

	public static bool CanGetFirstJob(int jobType)
	{
		return true;
	}

	public static string FirstJobStatRequirement(int jobType)
	{
		switch (jobType)
		{
		case 1:
			return "力量 " + 35;
		case 2:
			return "智力 " + 20;
		case 3:
		case 4:
			return "敏捷 " + 25;
		case 5:
			return "敏捷 " + 20;
		default:
			return "";
		}
	}

	public static void JobChangeHpMp(int newJobId, out int hpMin, out int hpMax, out int mpMin, out int mpMax)
	{
		int num = newJobId % 1000;
		switch (num)
		{
		case 100:
			hpMin = 200;
			hpMax = 250;
			mpMin = 0;
			mpMax = 0;
			return;
		case 200:
			hpMin = 0;
			hpMax = 0;
			mpMin = 100;
			mpMax = 150;
			return;
		}
		if (num % 100 == 0)
		{
			hpMin = 100;
			hpMax = 150;
			mpMin = 25;
			mpMax = 50;
		}
		else if (num > 0 && num < 200)
		{
			hpMin = 300;
			hpMax = 350;
			mpMin = 0;
			mpMax = 0;
		}
		else if (num < 300)
		{
			hpMin = 0;
			hpMax = 0;
			mpMin = 450;
			mpMax = 500;
		}
		else
		{
			hpMin = 300;
			hpMax = 350;
			mpMin = 150;
			mpMax = 200;
		}
	}

	public static int SpForJobChange(int newJobId)
	{
		if (newJobId % 10 != 2)
		{
			return 1;
		}
		return 3;
	}

	public static int ApForJobChange(int newJobId)
	{
		if (newJobId % 100 < 1)
		{
			return 0;
		}
		if (newJobId < 1000)
		{
			return 5;
		}
		return 7;
	}

	public static bool ResetStats(int job)
	{
		CharacterAttributes attributes = Profile.Attributes;
		int num = Profile.ap + attributes.str + attributes.dex + attributes.intelligence + attributes.luk;
		int num2 = 4;
		int num3 = 4;
		int num4 = 4;
		int num5 = 4;
		int num6 = 1;
		switch (job)
		{
		case 100:
		case 1100:
		case 2100:
			num2 = 35;
			num6 += (Profile.level - 10) * 3;
			break;
		case 200:
		case 1200:
			num4 = 20;
			num6 += (Profile.level - 8) * 3;
			break;
		case 300:
		case 400:
		case 1300:
		case 1400:
			num3 = 25;
			num6 += (Profile.level - 10) * 3;
			break;
		case 500:
		case 1500:
			num3 = 20;
			num6 += (Profile.level - 10) * 3;
			break;
		}
		num -= num2 + num3 + num4 + num5;
		if (num < 0)
		{
			Debug.LogWarning((object)"[JobSystem] AP 不足，无法重排能力值");
			return false;
		}
		Profile.SetBaseStats(num2, num3, num4, num5, num, Mathf.Max(0, num6));
		return true;
	}
}
