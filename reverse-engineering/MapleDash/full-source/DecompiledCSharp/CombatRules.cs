using System;
using System.Collections.Generic;

public static class CombatRules
{
	public const long InitialDamageCap = 19999999L;

	public static DamageResult Resolve(double rolledDamage, bool critical = false, long damageCap = 19999999L)
	{
		if (double.IsNaN(rolledDamage) || rolledDamage <= 0.0)
		{
			return new DamageResult(0L, critical: false);
		}
		long num = Math.Max(19999999L, damageCap);
		double num2 = Math.Floor(rolledDamage);
		if (num2 >= (double)num)
		{
			return new DamageResult(num, critical);
		}
		return new DamageResult((long)num2, critical);
	}

	public static int MaxPhysicalDamage(int mainStat, int secondaryStat, int weaponAttack, double weaponMultiplier)
	{
		return (int)Math.Min(2147483647.0, Math.Max(0.0, Math.Ceiling((weaponMultiplier * (double)mainStat + (double)secondaryStat) * (double)weaponAttack / 100.0)));
	}

	public static double MaxMagicDamage(int intelligence, int totalMagic, double skillMagicAttack)
	{
		return (Math.Ceiling(((double)totalMagic * Math.Ceiling((double)totalMagic / 1000.0) + (double)totalMagic) / 30.0) + Math.Ceiling((double)intelligence / 200.0)) * skillMagicAttack;
	}

	public static DamageResult Attack(CharacterAttributes a, WeaponDef w, int watk, int matk, Dictionary<string, object> target, NativeSkillDefinition skill, double roll, double critRoll, double hitRoll, long damageCap = 19999999L)
	{
		bool flag = skill?.Magic ?? false;
		NativeSkillEnhance.Bonus(skill?.Id ?? 0, out var finalPct, out var _, out var ignorePdp, out var bossPct);
		int num = Math.Max(0, NativeCombatData.Int(target, "level", 1) - a.level);
		double num2 = (flag ? ((double)a.intelligence / 10.0 + (double)a.luk / 10.0) : ((double)a.accuracy + (double)a.dex * 0.8 + (double)a.luk * 0.5));
		double num3 = (1.84 + 0.07 * (double)num) * (double)NativeCombatData.Int(target, "eva");
		double num4 = ((num3 <= 0.0) ? 1.0 : Math.Max(0.0, Math.Min(1.0, num2 / num3)));
		if (hitRoll >= num4)
		{
			return new DamageResult(0L, critical: false);
		}
		if (skill != null && skill.FixedDamage >= 0)
		{
			return Resolve(skill.FixedDamage, critical: false, damageCap);
		}
		bool flag2 = w.kind == WeaponKind.bow || w.kind == WeaponKind.crossbow || w.kind == WeaponKind.gun;
		int num5;
		if (w.kind == WeaponKind.claw)
		{
			num5 = a.luk;
		}
		else
		{
			num5 = (flag2 ? a.dex : a.str);
		}
		int num6;
		if (w.kind == WeaponKind.claw)
		{
			num6 = a.dex + a.str;
		}
		else
		{
			num6 = (flag2 ? a.str : a.dex);
		}
		double skillMagicAttack = ((skill == null || skill.MagicAttack <= 0f) ? 100f : skill.MagicAttack);
		double num7 = (flag ? MaxMagicDamage(a.intelligence, matk, skillMagicAttack) : ((double)MaxPhysicalDamage(num5, num6, watk, w.multiplier)));
		double num8 = (flag ? (num7 * 0.5) : ((w.multiplier * (double)num5 * 0.9 * Math.Max(0.1, Math.Min(1f, a.mastery)) + (double)num6) * (double)watk / 100.0));
		double num9 = num8 + (num7 - num8) * Math.Max(0.0, Math.Min(1.0, roll));
		num9 *= Math.Max(0.0, 1.0 - 0.01 * (double)num);
		int value = 0;
		if (a.setItemBonus != null)
		{
			a.setItemBonus.TryGetValue("IgnoreDefense", out value);
		}
		if (ignorePdp > 0)
		{
			value = Math.Min(100, value + ignorePdp);
		}
		num9 *= PercentageDefense.Multiplier(NativeCombatData.Int(target, flag ? "MDRate" : "PDRate"), 100.0 - (double)(100 - value) * (1.0 - (double)a.hexaIgnorePercent / 100.0));
		bool flag3 = !flag && critRoll < (double)Math.Max(0f, Math.Min(1f, a.criticalChance));
		if (!flag || skill.MagicAttack <= 0f)
		{
			num9 *= (double)(skill?.DamagePercent ?? 100f) / 100.0 + (double)(flag3 ? (a.criticalBonus + a.overCritBonus) : 0f);
		}
		if (finalPct > 0)
		{
			num9 *= 1.0 + (double)finalPct / 100.0;
		}
		double num10 = ElementModifier(target, (skill == null) ? "" : skill.Element);
		num9 = ((num10 == 0.0) ? 0.0 : Math.Max(1.0, num9 * num10));
		if (num9 > 0.0 && a.setItemBonus != null)
		{
			num9 = AdminSetItem.ApplyDamage(a.setItemBonus, NativeCombatData.Int(target, "boss") != 0, (long)Math.Ceiling(num9), a.hexaDamagePercent + ((NativeCombatData.Int(target, "boss") != 0) ? (a.hexaBossPercent + (float)bossPct) : 0f));
		}
		return Resolve(num9, flag3, damageCap);
	}

	public static double ElementModifier(Dictionary<string, object> target, string element)
	{
		if (target == null || string.IsNullOrEmpty(element) || !target.TryGetValue("elemAttr", out var value))
		{
			return 1.0;
		}
		string text = value as string;
		if (string.IsNullOrEmpty(text))
		{
			return 1.0;
		}
		for (int i = 0; i + 1 < text.Length; i += 2)
		{
			if (char.ToUpperInvariant(text[i]) == char.ToUpperInvariant(element[0]))
			{
				switch (text[i + 1])
				{
				case '1':
					return 0.0;
				case '2':
					return 0.5;
				case '3':
					return 1.5;
				}
			}
		}
		return 1.0;
	}

	public static bool PhysicalMiss(int totalAvoid, int accuracy, int level, int mobLevel, int job, int roll)
	{
		int num = Math.Max(0, Math.Min(999, totalAvoid) - Math.Max(0, mobLevel - level) / 2);
		accuracy = Math.Max(0, Math.Min(999, accuracy));
		double num2 = (double)num / ((double)accuracy * 4.5) * 100.0;
		if (double.IsNaN(num2))
		{
			return false;
		}
		bool flag = job % 1000 / 100 == 4;
		num2 = Math.Max(flag ? 5 : 2, Math.Min(flag ? 95 : 80, num2));
		return num2 > (double)(roll % 10000000) * 1.00000010000001E-05;
	}

	public static DamageResult Incoming(double attack, CharacterAttributes a, bool magic, int attackerLevel, double roll, int invincible = 0)
	{
		if (double.IsNaN(attack) || attack <= 0.0)
		{
			return new DamageResult(0L, critical: false);
		}
		double num = (magic ? a.magicDefense : a.weaponDefense);
		double num2 = attack * Math.Max(0.0, 1.0 - 0.01 * (double)Math.Max(0, a.level - attackerLevel)) * (0.85 + 0.3 * Math.Max(0.0, Math.Min(1.0, roll))) - num * (magic ? 0.6 : 0.5);
		if (invincible >= 100)
		{
			return new DamageResult(0L, critical: false);
		}
		if (!magic)
		{
			num2 = InvincibleDamage(num2, invincible);
		}
		return Resolve(Math.Max(1.0, num2), critical: false, long.MaxValue);
	}

	public static double InvincibleDamage(double damage, int percent)
	{
		return damage - (double)percent * damage * 0.01;
	}
}
