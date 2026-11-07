using UnityEngine;

public static class ClassDefs
{
	private static readonly ClassDef[] All = new ClassDef[6]
	{
		new ClassDef
		{
			id = "beginner",
			name = "新手",
			artKey = "beginner",
			accent = Color.white,
			skills = new ClassSkill[0]
		},
		new ClassDef
		{
			id = "knight",
			name = "骑士",
			artKey = "knight",
			accent = new Color(0.95f, 0.62f, 0.28f),
			skills = new ClassSkill[4]
			{
				S("防御崩坏", "crush"),
				S("突进", "dash"),
				S("虎咆哮", "roar"),
				S("剑影分身", "illusion")
			}
		},
		new ClassDef
		{
			id = "warrior",
			name = "狂战士",
			artKey = "warrior",
			accent = new Color(0.9f, 0.45f, 0.3f),
			skills = new ClassSkill[4]
			{
				S("愤怒之火", "rage"),
				S("斗气死亡断层", "rupture"),
				S("磁石", "magnet"),
				S("狂暴攻击", "berserk")
			}
		},
		new ClassDef
		{
			id = "mage",
			name = "魔法师",
			artKey = "mage",
			accent = new Color(0.55f, 0.8f, 1f),
			skills = new ClassSkill[4]
			{
				S("火焰箭", "firearrow"),
				S("末日烈焰", "doom"),
				S("连环爆破", "chain"),
				S("天降落星", "starfall")
			}
		},
		new ClassDef
		{
			id = "archer",
			name = "弓箭手",
			artKey = "archer",
			accent = new Color(0.65f, 0.9f, 0.5f),
			skills = new ClassSkill[4]
			{
				S("烈火箭", "firebolt"),
				S("箭雨", "arrowrain"),
				S("飞龙冲击波", "dragon"),
				S("骤雨狂矢", "storm")
			}
		},
		new ClassDef
		{
			id = "priest",
			name = "牧师",
			artKey = "priest",
			accent = new Color(0.8f, 0.95f, 0.7f),
			skills = new ClassSkill[4]
			{
				S("圣箭术", "holyarrow"),
				S("圣光", "holy"),
				S("光芒飞箭", "ray"),
				S("天秤之使", "libra")
			}
		}
	};

	public static int Count => All.Length;

	private static ClassSkill S(string name, string art)
	{
		return new ClassSkill
		{
			name = name,
			art = art
		};
	}

	public static ClassDef Get(string id)
	{
		ClassDef[] all = All;
		for (int i = 0; i < all.Length; i++)
		{
			ClassDef result = all[i];
			if (result.id == id)
			{
				return result;
			}
		}
		return All[0];
	}

	public static ClassDef At(int i)
	{
		return All[i];
	}

	static ClassDefs()
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0362: Unknown result type (might be due to invalid IL or missing references)
		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
	}
}
