public static class FlameStats
{
	public static readonly FlameStatDef[] All = new FlameStatDef[21]
	{
		new FlameStatDef
		{
			val = 0,
			exGrade = 0,
			label = "STR"
		},
		new FlameStatDef
		{
			val = 1,
			exGrade = 1,
			label = "DEX"
		},
		new FlameStatDef
		{
			val = 2,
			exGrade = 2,
			label = "INT"
		},
		new FlameStatDef
		{
			val = 3,
			exGrade = 3,
			label = "LUK"
		},
		new FlameStatDef
		{
			val = 4,
			exGrade = 4,
			label = "STR/DEX"
		},
		new FlameStatDef
		{
			val = 5,
			exGrade = 5,
			label = "STR/INT"
		},
		new FlameStatDef
		{
			val = 6,
			exGrade = 6,
			label = "STR/LUK"
		},
		new FlameStatDef
		{
			val = 7,
			exGrade = 7,
			label = "DEX/INT"
		},
		new FlameStatDef
		{
			val = 8,
			exGrade = 8,
			label = "DEX/LUK"
		},
		new FlameStatDef
		{
			val = 9,
			exGrade = 9,
			label = "INT/LUK"
		},
		new FlameStatDef
		{
			val = 10,
			exGrade = 17,
			label = "攻擊力"
		},
		new FlameStatDef
		{
			val = 11,
			exGrade = 18,
			label = "魔力"
		},
		new FlameStatDef
		{
			val = 12,
			exGrade = 13,
			label = "防禦力"
		},
		new FlameStatDef
		{
			val = 13,
			exGrade = 10,
			label = "最大HP"
		},
		new FlameStatDef
		{
			val = 14,
			exGrade = 11,
			label = "最大MP"
		},
		new FlameStatDef
		{
			val = 15,
			exGrade = 19,
			label = "移動速度"
		},
		new FlameStatDef
		{
			val = 16,
			exGrade = 20,
			label = "跳躍力"
		},
		new FlameStatDef
		{
			val = 17,
			exGrade = 24,
			label = "全屬性%"
		},
		new FlameStatDef
		{
			val = 18,
			exGrade = 21,
			label = "BOSS傷害%"
		},
		new FlameStatDef
		{
			val = 19,
			exGrade = 23,
			label = "傷害%"
		},
		new FlameStatDef
		{
			val = 20,
			exGrade = 12,
			label = "等級要求減免"
		}
	};

	public const int RollableCount = 21;

	public static FlameStatDef ByVal(int val)
	{
		if (val < 0 || val >= All.Length)
		{
			return null;
		}
		return All[val];
	}

	public static FlameStatDef ByExGrade(int exGrade)
	{
		FlameStatDef[] all = All;
		foreach (FlameStatDef flameStatDef in all)
		{
			if (flameStatDef.exGrade == exGrade)
			{
				return flameStatDef;
			}
		}
		return null;
	}
}
