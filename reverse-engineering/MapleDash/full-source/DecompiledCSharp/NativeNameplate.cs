using System;
using System.Globalization;
using UnityEngine;

public static class NativeNameplate
{
	public const int SeriesCount = 9;

	public const int TierCount = 10;

	private static readonly long[] Thresholds = new long[8] { 10000L, 100000L, 1000000L, 10000000L, 50000000L, 200000000L, 500000000L, 2000000000L };

	public const long TopTierPower = 10000000000L;

	private static readonly string[][] Titles = new string[9][]
	{
		new string[10] { "乾元玄阶", "坤极天域", "天元墟境", "玄枢天宿", "星墟玄阙", "太白星河", "苍元帝宿", "昊天玄宿", "九曜神王", "无敌大帝" },
		new string[10] { "魂师", "大魂师", "魂尊", "魂王", "魂圣", "封号斗罗", "极限斗罗", "三级神祇", "神帝", "神帝" },
		new string[10] { "赛亚人", "超级赛亚", "赛亚二阶", "赛亚三阶", "超赛神境", "超赛蓝境", "蓝级进化", "破坏神位", "自在极意", "自在极意" },
		new string[10] { "学院生徒", "下忍", "中忍", "上忍", "影级强者", "仙人模式", "尾兽化身", "六道之力", "大筒木境", "大筒木境" },
		new string[10] { "海贼新秀", "超新星", "七武海", "中将", "四皇候补", "四皇", "大将", "觉醒者", "海贼王", "海贼王" },
		new string[10] { "席官", "副队长", "队长", "破面十刃", "归刃解放", "完现术者", "零番队级", "灵王断片", "灵王", "灵王" },
		new string[10] { "鬼杀队员", "十人众级", "柱级", "下弦之鬼", "上弦之鬼", "上弦首座", "鬼舞辻无", "赫刀觉醒", "缘壹之境", "缘壹之境" },
		new string[10] { "四级术师", "三级术师", "二级术师", "一级术师", "准特级位", "特别一级", "特级术师", "领域展开", "最强之境", "最强之境" },
		new string[10] { "无个性者", "普通学生", "雄英生徒", "实习英雄", "职业英雄", "前十强者", "前五英雄", "第一英雄", "超觉醒者", "超觉醒者" }
	};

	private static bool hasSeries;

	private static string cachedLine;

	private static long cachedPower;

	private static int cachedSeries = -1;

	private static double cachedUntil;

	public static int Series => Mathf.Clamp(Profile.Data.nameplateTitleSeries, 0, 8);

	public static bool HasSeries => hasSeries;

	public static bool WearsNameTagRing => NativeNameTag.WearsField("nameTag");

	public static int TierOf(long power)
	{
		if (power <= 0)
		{
			return 0;
		}
		if (power >= 10000000000L)
		{
			return 9;
		}
		int i;
		for (i = 0; i < Thresholds.Length && power >= Thresholds[i]; i++)
		{
		}
		return i;
	}

	public static long TierRequirement(int tier)
	{
		if (tier < 10)
		{
			return Thresholds[tier - 1];
		}
		return 10000000000L;
	}

	public static string TitleOf(int series, int tier)
	{
		int num = Mathf.Clamp(series, 0, 8);
		int num2 = Mathf.Clamp(tier, 0, 9);
		return Titles[num][num2];
	}

	public static void SetSeries(int series)
	{
		int nameplateTitleSeries = Mathf.Clamp(series, 0, 8);
		Profile.Data.nameplateTitleSeries = nameplateTitleSeries;
		hasSeries = true;
		cachedSeries = -1;
		Profile.Save();
	}

	public static void RefreshFromServer()
	{
		if (!GameSession.InGame)
		{
			return;
		}
		try
		{
			if (int.TryParse(GameSession.Net.ScriptValue(new ScriptValueRequest
			{
				characterId = GameSession.CharacterId,
				name = "nameplate_title_series"
			}).value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result))
			{
				Profile.Data.nameplateTitleSeries = Mathf.Clamp(result, 0, 8);
				hasSeries = true;
			}
			else
			{
				Profile.Data.nameplateTitleSeries = 0;
				hasSeries = false;
			}
			cachedSeries = -1;
		}
		catch (Exception ex)
		{
			Debug.LogWarning((object)("[Nameplate] 读取称号系列失败：" + ex.Message));
		}
		NativeTitleTrials.LoadLitFromServer();
	}

	public static void OnLitChanged()
	{
		cachedSeries = -1;
	}

	public static long PowerOf(PlayerCtrl player)
	{
		if (!((Object)(object)player == (Object)null))
		{
			return CombatPower.Of(player.Attributes, player.WeaponAttack, player.MagicAttack);
		}
		return 0L;
	}

	public static string LineFor(PlayerCtrl player)
	{
		if ((Object)(object)player == (Object)null || player.Attributes == null)
		{
			return null;
		}
		if (!hasSeries)
		{
			return null;
		}
		long num = PowerOf(player);
		double unscaledTimeAsDouble = Time.unscaledTimeAsDouble;
		if (unscaledTimeAsDouble < cachedUntil && cachedPower == num && cachedSeries == Series)
		{
			return cachedLine;
		}
		cachedPower = num;
		cachedSeries = Series;
		cachedUntil = unscaledTimeAsDouble + 0.5;
		int num2 = NativeTitleTrials.Lit(cachedSeries);
		int tier = ((num2 > 0) ? (num2 - 1) : TierOf(num));
		cachedLine = (WearsNameTagRing ? (TitleOf(cachedSeries, tier) + " || 战斗力：" + NumberFormat.Compact(num)) : null);
		return cachedLine;
	}
}
