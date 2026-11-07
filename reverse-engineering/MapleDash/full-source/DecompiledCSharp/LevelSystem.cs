using System;
using UnityEngine;

public static class LevelSystem
{
	public const int ApPerLevel = 5;

	public const int SpPerLevel = 3;

	public const bool LevelUpProtect = true;

	public const bool RandomizeHpMpGain = true;

	public const bool AddSlotsByLevel = false;

	public static JobBranch BranchOf(int jobId)
	{
		switch (JobDefs.FamilyOf(jobId))
		{
		case "warrior":
		case "cygnus-dawnwarrior":
			return JobBranch.Warrior;
		case "magician":
		case "cygnus-blazewizard":
			return JobBranch.Magician;
		case "bowman":
		case "cygnus-windarcher":
			return JobBranch.Bowman;
		case "thief":
		case "cygnus-nightwalker":
			return JobBranch.Thief;
		case "pirate":
		case "cygnus-thunderbreaker":
			return JobBranch.Pirate;
		default:
			return JobBranch.Beginner;
		}
	}

	public static JobBranch BranchOf(string cls)
	{
		return BranchOf(JobDefs.JobIdOf(cls));
	}

	public static void HpMpRange(JobBranch branch, out int hpMin, out int hpMax, out int mpMin, out int mpMax)
	{
		switch (branch)
		{
		case JobBranch.Warrior:
			hpMin = 24;
			hpMax = 28;
			mpMin = 4;
			mpMax = 6;
			break;
		case JobBranch.Magician:
			hpMin = 10;
			hpMax = 14;
			mpMin = 22;
			mpMax = 24;
			break;
		case JobBranch.Bowman:
			hpMin = 20;
			hpMax = 24;
			mpMin = 14;
			mpMax = 16;
			break;
		case JobBranch.Thief:
			hpMin = 20;
			hpMax = 24;
			mpMin = 14;
			mpMax = 16;
			break;
		case JobBranch.Pirate:
			hpMin = 22;
			hpMax = 28;
			mpMin = 18;
			mpMax = 23;
			break;
		case JobBranch.Aran:
			hpMin = 44;
			hpMax = 48;
			mpMin = 4;
			mpMax = 8;
			break;
		default:
			hpMin = 12;
			hpMax = 16;
			mpMin = 10;
			mpMax = 12;
			break;
		}
	}

	public static int AddExperience(PlayerProfileData d, long amount, bool protect)
	{
		if (d == null || amount <= 0)
		{
			return 0;
		}
		EnsureBaseline(d);
		int maxLevel = ExpTable.MaxLevel;
		long num = Math.Max(0L, d.experience);
		long num2 = ((amount > long.MaxValue - num) ? long.MaxValue : (num + amount));
		int num3 = 0;
		while (d.level < maxLevel)
		{
			long num4 = Math.Max(1L, ExpTable.Needed(d.level));
			if (num2 < num4)
			{
				break;
			}
			num2 -= num4;
			LevelUp(d);
			num3++;
			if (protect)
			{
				break;
			}
		}
		if (d.level >= maxLevel)
		{
			long num5 = ExpCap(d.level);
			if (num2 > num5)
			{
				num2 = num5;
			}
		}
		d.experience = num2;
		return num3;
	}

	public static int Reconcile(PlayerProfileData d)
	{
		if (d == null)
		{
			return 0;
		}
		EnsureBaseline(d);
		int num = 0;
		while (d.level < ExpTable.MaxLevel && d.experience >= Math.Max(1L, ExpTable.Needed(d.level)))
		{
			d.experience -= Math.Max(1L, ExpTable.Needed(d.level));
			LevelUp(d);
			num++;
		}
		if (d.level >= ExpTable.MaxLevel)
		{
			long num2 = ExpCap(d.level);
			if (d.experience > num2)
			{
				d.experience = num2;
			}
		}
		return num;
	}

	public static long ExpCap(int level)
	{
		long num = Math.Max(1L, ExpTable.Needed(level));
		return num / 100 * 99 + num % 100 * 99 / 100;
	}

	public static int SetLevel(PlayerProfileData d, int target)
	{
		if (d == null)
		{
			return 0;
		}
		EnsureBaseline(d);
		target = Math.Min(ExpTable.MaxLevel, Math.Max(1, target));
		int num = 0;
		while (d.level < target)
		{
			LevelUp(d);
			num++;
		}
		d.attributes.level = d.level;
		d.attributes.maxHp = d.baseMaxHp;
		d.attributes.maxMp = d.baseMaxMp;
		return num;
	}

	public static void EnsureBaseline(PlayerProfileData d)
	{
		if (d.attributes == null)
		{
			d.attributes = new CharacterAttributes();
		}
		if (d.level <= 0)
		{
			d.level = 1;
		}
		if (d.baseMaxHp <= 0)
		{
			d.baseMaxHp = Math.Max(50, d.attributes.maxHp);
		}
		if (d.baseMaxMp <= 0)
		{
			d.baseMaxMp = Math.Max(5, d.attributes.maxMp);
		}
		if (d.experience < 0)
		{
			d.experience = 0L;
		}
		d.attributes.level = d.level;
		d.attributes.maxHp = d.baseMaxHp;
		d.attributes.maxMp = d.baseMaxMp;
	}

	public static bool MigrateNoviceAp(PlayerProfileData d)
	{
		if (d.noviceApMigrated)
		{
			return false;
		}
		d.noviceApMigrated = true;
		if (d.jobId != 0 || d.ap <= 0)
		{
			return true;
		}
		int num = Math.Min(d.ap, Math.Min(10, Math.Max(0, d.level - 1)) * 5);
		for (int i = 1; i < Math.Min(d.level, 11); i++)
		{
			if (num <= 0)
			{
				break;
			}
			int num2 = Math.Min(num, (i < 6) ? 5 : 4);
			d.attributes.str += num2;
			d.ap -= num2;
			num -= num2;
			if (i >= 6 && num > 0)
			{
				d.attributes.dex++;
				d.ap--;
				num--;
			}
		}
		return true;
	}

	public static void ResetForRebirth(PlayerProfileData d, int target)
	{
		EnsureBaseline(d);
		d.experience = 0L;
		d.level = Math.Max(1, Math.Min(ExpTable.MaxLevel, target)) - 1;
		LevelUp(d);
	}

	private static void LevelUp(PlayerProfileData d)
	{
		AudioManager.Play("Game/LevelUp", 0.9f, pitchVary: false);
		JobBranch jobBranch = BranchOf(d.jobId);
		HpMpRange(jobBranch, out var hpMin, out var hpMax, out var mpMin, out var mpMax);
		int num = Random.Range(hpMin, hpMax + 1);
		int num2 = Random.Range(mpMin, mpMax + 1);
		int num3 = ((d.attributes != null) ? d.attributes.intelligence : 0);
		num2 += ((jobBranch == JobBranch.Magician) ? (num3 / 20) : (num3 / 10));
		d.baseMaxHp = Math.Max(1, d.baseMaxHp + num);
		d.baseMaxMp = Math.Max(1, d.baseMaxMp + num2);
		d.level = Math.Min(ExpTable.MaxLevel, d.level + 1);
		if (jobBranch == JobBranch.Beginner && d.level - 1 < 11)
		{
			d.attributes.str += ((d.level - 1 < 6) ? 5 : 4);
			if (d.level - 1 >= 6)
			{
				d.attributes.dex++;
			}
		}
		else
		{
			d.ap = Math.Max(0, d.ap) + 5;
			if (jobBranch != JobBranch.Beginner)
			{
				d.sp = Math.Max(0, d.sp) + 3;
			}
		}
		d.attributes.level = d.level;
		d.attributes.maxHp = d.baseMaxHp;
		d.attributes.maxMp = d.baseMaxMp;
	}
}
