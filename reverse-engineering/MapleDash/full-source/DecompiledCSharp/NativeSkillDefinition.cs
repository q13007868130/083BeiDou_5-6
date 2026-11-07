using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class NativeSkillDefinition
{
	public readonly int Id;

	public readonly int Level;

	public readonly int MpCost;

	public readonly int HpCost;

	public readonly int HitCount;

	public readonly int TargetCount;

	public readonly int FixedDamage;

	public readonly float Cooldown;

	public readonly float DamagePercent;

	public readonly float MagicAttack;

	public readonly float Duration;

	public readonly float Probability;

	public readonly float AttackBuff;

	public readonly float SummonInterval;

	public readonly int AttackDelay;

	public readonly Vector2 Min;

	public readonly Vector2 Max;

	public readonly string Kind;

	public readonly string Element;

	public readonly bool Magic;

	public readonly bool HasBounds;

	public readonly bool FullScreen;

	public readonly bool AreaAttack;

	public NativeSkillDefinition(Dictionary<string, object> skill, Dictionary<string, object> level)
	{
		//IL_0334: Unknown result type (might be due to invalid IL or missing references)
		//IL_0339: Unknown result type (might be due to invalid IL or missing references)
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0364: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_029f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d7: Unknown result type (might be due to invalid IL or missing references)
		Id = Art.Id(skill, "id");
		Level = Art.Id(level, "level", 1);
		Element = (skill.TryGetValue("element", out var value) ? (value as string) : "");
		Kind = skill["kind"] as string;
		object obj = skill["magic"];
		bool flag = default;
		int num;
		if (obj is bool)
		{
			flag = (bool)obj;
			num = 1;
		}
		else
		{
			num = 0;
		}
		Magic = (byte)((uint)num & (flag ? 1u : 0u)) != 0;
		SummonInterval = Mathf.Max(0.1f, Art.Num(skill, "summonInterval", 1f));
		MpCost = (int)Art.Num(level, "mpCon");
		float num2 = Art.Num(level, "cooltime");
		if (num2 <= 0f)
		{
			num2 = Art.Num(level, "cooltimeMS") / 1000f;
		}
		Cooldown = num2;
		AttackDelay = (int)Art.Num(level, "attackDelay");
		HpCost = (int)Art.Num(level, "hpCon");
		FixedDamage = (int)Art.Num(level, "fixdamage", -1f);
		HitCount = Mathf.Max(1, Mathf.Max((int)Art.Num(level, "attackCount"), (int)Art.Num(level, "bulletCount")));
		TargetCount = (int)Art.Num(level, "mobCount", 1f);
		DamagePercent = Art.Num(level, "damage");
		MagicAttack = Art.Num(level, "mad");
		Duration = Art.Num(level, "time");
		Probability = Art.Num(level, "prop", 100f) / 100f;
		AttackBuff = Art.Num(level, "pad");
		string text = "";
		string[] array = new string[4] { "", "2", "3", "4" };
		foreach (string text2 in array)
		{
			if (!level.ContainsKey("lt" + text2) || !level.ContainsKey("rb" + text2))
			{
				continue;
			}
			if (text2.Length > 0)
			{
				Vector2 val = Point(level, "lt" + text2, Vector2.zero);
				Vector2 val2 = Point(level, "rb" + text2, Vector2.zero);
				float num3 = 0.5f;
				if (val2.x - val.x < num3 || val2.y - val.y < num3)
				{
					continue;
				}
			}
			text = text2;
			break;
		}
		HasBounds = text.Length > 0 || (level.ContainsKey("lt") && level.ContainsKey("rb"));
		Min = Point(level, "lt" + text, new Vector2(0f - Art.Num(level, "range", 200f), -100f));
		Max = Point(level, "rb" + text, new Vector2(0f, 100f));
		int num4 = (int)Art.Num(level, "w");
		if (num4 > 0 && HasBounds && Kind == "Attack" && NativeSkillVfx.RapidAttack(Id))
		{
			float num5 = (float)num4 / 40f;
			if (num5 > 0f - Min.x)
			{
				Min = new Vector2(0f - num5, Min.y);
			}
		}
		obj = skill["vMatrix"];
		bool flag2 = default;
		int num6;
		if (obj is bool)
		{
			flag2 = (bool)obj;
			num6 = 1;
		}
		else
		{
			num6 = 0;
		}
		bool flag3 = (byte)((uint)num6 & (flag2 ? 1u : 0u)) != 0;
		AreaAttack = Kind == "Attack" && (((int)Art.Num(skill, "skillType") == 3) | flag3);
		FullScreen = !HasBounds && !level.ContainsKey("range") && AreaAttack;
	}

	private static Vector2 Point(Dictionary<string, object> d, string key, Vector2 fallback)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		if (!d.TryGetValue(key, out var value) || !(value is List<object> { Count: >=2 } list))
		{
			return fallback / 40f;
		}
		return new Vector2(Convert.ToSingle(list[0]), Convert.ToSingle(list[1])) / 40f;
	}
}
