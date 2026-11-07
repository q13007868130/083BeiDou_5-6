using System.Collections.Generic;

public sealed class SkillFxPlan
{
	public enum SpecialKind
	{
		None,
		Wave,
		Fall,
		Burst
	}

	public float windup;

	public float impact;

	public bool ball;

	public float ballDelay;

	public float ballSpeed = 14f;

	public float ballRange = 12f;

	public float ballHeight = 0.95f;

	public SpecialKind special;

	public float specialGap = 0.12f;

	public float specialSpread = 4f;

	public bool tiles;

	public float tileGap = 0.14f;

	public bool atImpact;

	public bool affected;

	public float cast2Delay = -1f;

	public bool cast2Follow;

	private static readonly Dictionary<string, SkillFxPlan> table = new Dictionary<string, SkillFxPlan>
	{
		{
			"crush",
			new SkillFxPlan
			{
				impact = 0.25f
			}
		},
		{
			"dash",
			new SkillFxPlan
			{
				impact = 0f,
				cast2Delay = 0f,
				cast2Follow = true
			}
		},
		{
			"roar",
			new SkillFxPlan
			{
				impact = 0.3f
			}
		},
		{
			"illusion",
			new SkillFxPlan
			{
				impact = 0f
			}
		},
		{
			"rage",
			new SkillFxPlan
			{
				impact = 0f,
				affected = true
			}
		},
		{
			"rupture",
			new SkillFxPlan
			{
				impact = 1f,
				special = SpecialKind.Wave,
				specialGap = 0.42f
			}
		},
		{
			"magnet",
			new SkillFxPlan
			{
				impact = 0.18f
			}
		},
		{
			"berserk",
			new SkillFxPlan
			{
				impact = 0.2f
			}
		},
		{
			"firearrow",
			new SkillFxPlan
			{
				ball = true,
				ballDelay = 0.24f,
				ballSpeed = 15f
			}
		},
		{
			"doom",
			new SkillFxPlan
			{
				windup = 0.92f,
				impact = 1.15f,
				special = SpecialKind.Burst,
				specialSpread = 7.5f
			}
		},
		{
			"chain",
			new SkillFxPlan
			{
				impact = 0.2f
			}
		},
		{
			"starfall",
			new SkillFxPlan
			{
				impact = 0.85f,
				cast2Delay = 0.65f,
				tiles = true,
				tileGap = 0.16f
			}
		},
		{
			"firebolt",
			new SkillFxPlan
			{
				ball = true,
				ballDelay = 0.2f,
				ballSpeed = 14f,
				atImpact = true,
				special = SpecialKind.Fall,
				tiles = true,
				tileGap = 0.12f
			}
		},
		{
			"arrowrain",
			new SkillFxPlan
			{
				impact = 0.35f,
				special = SpecialKind.Fall,
				specialSpread = 2.1f
			}
		},
		{
			"dragon",
			new SkillFxPlan
			{
				ball = true,
				ballDelay = 0.3f,
				ballSpeed = 12f
			}
		},
		{
			"storm",
			new SkillFxPlan
			{
				impact = 0.2f
			}
		},
		{
			"holyarrow",
			new SkillFxPlan
			{
				ball = true,
				ballDelay = 0.18f,
				ballSpeed = 15f
			}
		},
		{
			"holy",
			new SkillFxPlan
			{
				impact = 0.3f
			}
		},
		{
			"ray",
			new SkillFxPlan
			{
				ball = true,
				ballDelay = 0.18f,
				ballSpeed = 16f
			}
		},
		{
			"libra",
			new SkillFxPlan
			{
				impact = 0f,
				affected = true
			}
		}
	};

	public static SkillFxPlan Get(string key)
	{
		if (key == null || !table.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}
}
