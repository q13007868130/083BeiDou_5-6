using System.Collections.Generic;
using UnityEngine;

public static class SkillProgress
{
	public const int SpPerLevel = 3;

	private static readonly List<SkillData> cache = new List<SkillData>();

	private static bool cacheReady;

	private static int cacheJob = -1;

	public static int BeginnerSp => Mathf.Clamp(Profile.level - 1, 0, 6);

	public static int TotalSp
	{
		get
		{
			if (Profile.JobId != 0)
			{
				return Profile.Sp + SpentSp;
			}
			return BeginnerSp;
		}
	}

	public static int SpentSp
	{
		get
		{
			int num = 0;
			foreach (SkillData item in AllSkills())
			{
				num += BaseLevelOf(item.id);
			}
			return num;
		}
	}

	public static int BeginnerSpent => BaseLevelOf(1000) + BaseLevelOf(1001) + BaseLevelOf(1002);

	public static int AvailableSp
	{
		get
		{
			if (Profile.JobId != 0)
			{
				return Mathf.Max(0, Profile.Sp);
			}
			return Mathf.Max(0, BeginnerSp - BeginnerSpent);
		}
	}

	public static int LevelOf(int id)
	{
		int num = BaseLevelOf(id);
		if (num <= 0)
		{
			return 0;
		}
		int num2 = NativeCombatOrders.Bonus(id);
		if (num2 > 0)
		{
			return Mathf.Min(num + num2, NativeCombatOrders.Cap(id, num2));
		}
		return num;
	}

	public static int BaseLevelOf(int id)
	{
		if (NativeSetItemSkills.IsSkill(id))
		{
			return NativeSetItemSkills.LevelOf(id);
		}
		if (NativeSoulWeapon.IsSkill(id))
		{
			return NativeSoulWeapon.LevelOf(id);
		}
		if (VMatrixLevels.TryGet(id, out var level))
		{
			return level;
		}
		SkillData skillData = SkillBook.Get(id);
		int num = Profile.Data.SkillLevelOf(id);
		if (skillData != null)
		{
			return Mathf.Clamp(num, 0, SkillBook.MasterCapOf(id));
		}
		return 0;
	}

	public static void SetLevel(int id, int lv)
	{
		if (SkillBook.Get(id) != null)
		{
			Profile.Data.SetSkillLevel(id, Mathf.Clamp(lv, 0, SkillBook.MasterCapOf(id)));
			Profile.Save();
		}
	}

	public static void ClearAll()
	{
		Profile.Data.ClearSkills();
		Profile.Save();
	}

	public static void SetLearned(int id, int lv)
	{
		SetLevel(id, lv);
	}

	public static IEnumerable<KeyValuePair<int, int>> AllLearned()
	{
		PlayerProfileData d = Profile.Data;
		for (int i = 0; i < d.skillIds.Count && i < d.skillLevels.Count; i++)
		{
			if (d.skillLevels[i] > 0)
			{
				yield return new KeyValuePair<int, int>(d.skillIds[i], d.skillLevels[i]);
			}
		}
	}

	private static List<SkillData> AllSkills()
	{
		if (cacheReady && cacheJob == Profile.JobId)
		{
			return cache;
		}
		cacheReady = true;
		cacheJob = Profile.JobId;
		cache.Clear();
		foreach (Dictionary<string, object> item in SkillBook.Tabs(SkillBook.LineForJob(Profile.JobId)))
		{
			if (!item.TryGetValue("skills", out var value))
			{
				continue;
			}
			foreach (object item2 in (List<object>)value)
			{
				SkillData skillData = SkillBook.Get(int.Parse(item2.ToString()));
				if (skillData != null)
				{
					cache.Add(skillData);
				}
			}
		}
		return cache;
	}

	public static string WhyNot(SkillData d)
	{
		if (d == null)
		{
			return "没有这个技能的数据";
		}
		if (d.autoLearn)
		{
			return "这个技能会随等级自动习得，不能手动加点";
		}
		if (d.hexa)
		{
			if (d.id < 400000000)
			{
				return "六转技能只能在 HEXA 矩阵中提升";
			}
			return "这个技能只能在 V 矩阵里提升（核心等级）";
		}
		if (BaseLevelOf(d.id) >= SkillBook.MasterCapOf(d.id))
		{
			int num = LevelOf(d.id) - BaseLevelOf(d.id);
			return "「" + NameOf(d) + "」已达最高等级 " + SkillBook.MasterCapOf(d.id) + " 级" + ((num > 0) ? ("（实用的战斗命令生效中，临时 +" + num + " 级）") : "");
		}
		bool flag = IsBeginnerSkill(d.id);
		if ((flag ? (BeginnerSp - BeginnerSpent) : AvailableSp) <= 0)
		{
			if (!flag)
			{
				return "SP 不足，无法继续提升技能";
			}
			return "可使用的新手技能点不足";
		}
		if (Profile.JobId == 0 && d.id != 1000 && d.id != 1001 && d.id != 1002)
		{
			return "新手只能提升基础技能";
		}
		foreach (int[] item in d.req)
		{
			if (LevelOf(item[0]) < item[1])
			{
				SkillData skillData = SkillBook.Get(item[0]);
				return "需要先把「" + ((skillData != null) ? NameOf(skillData) : item[0].ToString()) + "」提升到 " + item[1] + " 级";
			}
		}
		return null;
	}

	private static string NameOf(SkillData d)
	{
		if (!string.IsNullOrEmpty(d.name))
		{
			return d.name;
		}
		return d.id.ToString();
	}

	public static bool CanRaise(SkillData d)
	{
		return WhyNot(d) == null;
	}

	public static bool Raise(SkillData d)
	{
		if (!CanRaise(d))
		{
			return false;
		}
		SetLevel(d.id, BaseLevelOf(d.id) + 1);
		if (!IsBeginnerSkill(d.id))
		{
			Profile.AddSp(-1);
		}
		return true;
	}

	private static bool IsBeginnerSkill(int id)
	{
		if (id % 10000000 >= 1000)
		{
			return id % 10000000 <= 1002;
		}
		return false;
	}

	public static void Reset()
	{
		int num = 0;
		foreach (SkillData item in AllSkills())
		{
			if (!item.hexa)
			{
				num += BaseLevelOf(item.id);
				Profile.Data.SetSkillLevel(item.id, 0);
			}
		}
		if (Profile.JobId != 0 && num > 0)
		{
			Profile.AddSp(num);
		}
		Profile.Save();
	}
}
