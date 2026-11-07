using System.Collections.Generic;

public sealed class SkillData
{
	public int id;

	public int maxLevel;

	public int masterLevel;

	public int job;

	public string name;

	public string desc;

	public string type;

	public string icon;

	public bool magic;

	public bool noName;

	public bool autoLearn;

	public bool hexa;

	public List<int[]> req = new List<int[]>();

	public Dictionary<string, object> attackInfo;

	public Dictionary<string, object> levels = new Dictionary<string, object>();

	public bool IsAttack => type == "attack";

	public bool IsBuff
	{
		get
		{
			if (type == "buff")
			{
				return SkillBook.SpecialKind(id) == null;
			}
			return false;
		}
	}

	public bool IsSummon => type == "summon";

	public bool IsPassive
	{
		get
		{
			if (type == "passive" && !SkillSystem.InstantSelfSkill(id))
			{
				return SkillBook.SpecialKind(id) == null;
			}
			return false;
		}
	}

	public SkillData ForTooltip(string title, string detail)
	{
		return new SkillData
		{
			id = id,
			maxLevel = maxLevel,
			masterLevel = masterLevel,
			job = job,
			name = (string.IsNullOrEmpty(title) ? name : title),
			desc = (string.IsNullOrEmpty(detail) ? desc : detail),
			type = type,
			icon = icon,
			magic = magic,
			noName = false,
			autoLearn = autoLearn,
			hexa = hexa,
			req = req,
			attackInfo = attackInfo,
			levels = levels
		};
	}

	public Dictionary<string, object> Level(int lv)
	{
		if (lv < 1)
		{
			lv = 1;
		}
		if (lv > maxLevel)
		{
			lv = maxLevel;
		}
		if (!levels.TryGetValue(lv.ToString(), out var value))
		{
			return null;
		}
		return value as Dictionary<string, object>;
	}
}
