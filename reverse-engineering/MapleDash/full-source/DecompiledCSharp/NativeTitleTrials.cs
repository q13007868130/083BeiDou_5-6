using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public static class NativeTitleTrials
{
	public const int SeriesCount = 9;

	public const int TierCount = 10;

	private static readonly int[] Quad = new int[10] { 2, 5, 11, 18, 27, 38, 51, 65, 82, 100 };

	private static readonly int[] Attack = new int[10] { 3, 8, 16, 27, 41, 57, 76, 98, 123, 150 };

	private static readonly int[] Hp = new int[10] { 100, 300, 600, 1000, 1500, 2100, 2800, 3600, 4500, 5500 };

	private static readonly int[] Defense = new int[10] { 4, 11, 22, 36, 55, 73, 102, 129, 164, 200 };

	private static readonly int[] RingMilestoneTiers = new int[4] { 1, 5, 8, 10 };

	private static readonly int[] RingIds = new int[4] { 1112100, 1112110, 1112200, 1112230 };

	private const string LitKey = "title_trial_lit";

	private const string KillKey = "title_trial_kill";

	private const string StarKey = "title_trial_star";

	private const string RingMaskKey = "title_trial_claimed_ring";

	private static readonly string[] BossKeys = new string[15]
	{
		"papulatus", "scarga", "yaoseng", "krexel", "zakum", "showa", "horntail", "pinkbean", "tokyo_vergamot", "tokyo_dunas",
		"tokyo_nibergen", "tokyo_nux", "tokyo_dunas2", "tokyo_aufheben", "vonleon"
	};

	private static readonly int[] litCache = new int[9];

	private static bool litLoaded;

	private static double litUntil;

	private static string ReadValue(string key)
	{
		if (!GameSession.InGame)
		{
			return null;
		}
		return GameSession.Net.ScriptValue(new ScriptValueRequest
		{
			characterId = GameSession.CharacterId,
			name = key
		}).value;
	}

	private static void WriteValue(string key, string value)
	{
		if (GameSession.InGame)
		{
			GameSession.Net.ScriptValue(new ScriptValueRequest
			{
				characterId = GameSession.CharacterId,
				write = true,
				name = key,
				value = value
			});
		}
	}

	private static long ReadLong(string key)
	{
		if (!long.TryParse(ReadValue(key), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result))
		{
			return 0L;
		}
		return result;
	}

	public static int Lit(int series)
	{
		EnsureLit();
		if (series < 0 || series >= 9)
		{
			return 0;
		}
		return litCache[series];
	}

	private static void EnsureLit()
	{
		if (litLoaded && Time.unscaledTimeAsDouble < litUntil)
		{
			return;
		}
		for (int i = 0; i < 9; i++)
		{
			litCache[i] = 0;
		}
		string text = ReadValue("title_trial_lit");
		if (!string.IsNullOrEmpty(text))
		{
			string[] array = text.Split(',');
			for (int j = 0; j < array.Length && j < 9; j++)
			{
				int.TryParse(array[j], out litCache[j]);
			}
		}
		litLoaded = true;
		litUntil = Time.unscaledTimeAsDouble + 3.0;
	}

	public static void LoadLitFromServer()
	{
		litLoaded = false;
	}

	private static void SaveLit()
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < 9; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append(',');
			}
			stringBuilder.Append(litCache[i]);
		}
		WriteValue("title_trial_lit", stringBuilder.ToString());
		litUntil = Time.unscaledTimeAsDouble + 3.0;
		NativeNameplate.OnLitChanged();
	}

	public static long Count(int series)
	{
		if (series < 0 || series >= 9 || !GameSession.InGame)
		{
			return 0L;
		}
		switch (series)
		{
		case 0:
			return ReadLong("title_trial_kill");
		case 1:
		{
			long num = 0L;
			string[] bossKeys = BossKeys;
			foreach (string text in bossKeys)
			{
				num += ReadLong("boss_growth_kill_" + text);
			}
			return num;
		}
		case 2:
			return GameSession.Net.Social(new SocialRequest
			{
				characterId = GameSession.CharacterId
			}).cards.Count;
		case 3:
			return GameSession.Net.Boss(new BossRequest
			{
				characterId = GameSession.CharacterId
			}).questCount;
		case 4:
			return ReadLong("副本总积分");
		case 5:
			return ReadLong("title_trial_star");
		case 6:
			return ReadLong("每日任务总完成轮数");
		case 7:
			return GameSession.Character?.record.fishLevel ?? 1;
		case 8:
			return ReadLong("累计签到次数");
		default:
			return 0L;
		}
	}

	public static long Need(int series, int tier)
	{
		if (tier <= 1)
		{
			return 0L;
		}
		if (tier > 10)
		{
			return long.MaxValue;
		}
		return series switch
		{
			0 => 1000L << tier - 2, 
			1 => 20L << tier - 2, 
			2 => 30 + 27 * (tier - 2), 
			3 => 50L * (long)(tier - 1), 
			4 => 200L * (long)(tier - 1), 
			5 => 10L << tier - 2, 
			6 => 30L * (long)(tier - 1), 
			7 => 5L * (long)(tier - 1), 
			8 => 10L * (long)(tier - 1), 
			_ => long.MaxValue, 
		};
	}

	public static void CountKill()
	{
		if (GameSession.InGame)
		{
			WriteValue("title_trial_kill", (ReadLong("title_trial_kill") + 1).ToString(CultureInfo.InvariantCulture));
		}
	}

	public static void AddStar(int delta)
	{
		if (GameSession.InGame && delta != 0)
		{
			WriteValue("title_trial_star", (ReadLong("title_trial_star") + delta).ToString(CultureInfo.InvariantCulture));
		}
	}

	public static string Claim(int series, out string granted)
	{
		granted = null;
		if (!GameSession.InGame)
		{
			return "需要进入游戏";
		}
		if (series < 0 || series >= 9)
		{
			return "系列不存在";
		}
		PlayerCtrl playerCtrl = GameLoop.I?.player;
		if ((Object)(object)playerCtrl == (Object)null)
		{
			return "需要进入游戏";
		}
		EnsureLit();
		int num = litCache[series] + 1;
		if (num > 10)
		{
			return "该系列已点亮至最高档";
		}
		long num2 = NativeNameplate.TierRequirement(num);
		long num3 = NativeNameplate.PowerOf(playerCtrl);
		if (num3 < num2)
		{
			return "战力未达标：需要 " + NumberFormat.Compact(num2) + "（当前 " + NumberFormat.Compact(num3) + "）";
		}
		long num4 = Need(series, num);
		long num5 = Count(series);
		if (num5 < num4)
		{
			return "试炼进度未达标：需要 " + num4 + "（当前 " + num5 + "）";
		}
		litCache[series] = num;
		SaveLit();
		string text = "【" + NativeNameplate.TitleOf(series, num - 1) + "】已点亮！当前第 " + num + " 档";
		granted = ClaimRing(num);
		if (!string.IsNullOrEmpty(granted))
		{
			text = text + "\r\n" + granted;
		}
		if (num == 10)
		{
			NativeSystemMessage.Drop(6, "恭喜玩家点亮了 " + NativeNameplate.TitleOf(series, num - 1) + "（第 10 档）称号！");
		}
		return null;
	}

	private static string ClaimRing(int tier)
	{
		int num = Array.IndexOf(RingMilestoneTiers, tier);
		if (num < 0)
		{
			return null;
		}
		int num2 = 1 << num;
		int num3 = (int)ReadLong("title_trial_claimed_ring");
		if ((num3 & num2) != 0)
		{
			return null;
		}
		int id = RingIds[num];
		if (!Profile.TryAddItem(id, 1))
		{
			return "（装备栏已满，未能发放名牌戒指；稍后重新点亮领取即可补发）";
		}
		WriteValue("title_trial_claimed_ring", (num3 | num2).ToString(CultureInfo.InvariantCulture));
		return "获得里程碑奖励：" + (WzNames.Item(id) ?? "名牌戒指") + "（" + id + "）";
	}

	public static IEnumerable<KeyValuePair<string, int>> BonusSet()
	{
		if (GameSession.InGame && NativeNameplate.HasSeries)
		{
			EnsureLit();
			int num = Lit(NativeNameplate.Series);
			if (num > 0)
			{
				int quad = Quad[num - 1];
				int attack = Attack[num - 1];
				int hp = Hp[num - 1];
				int defense = Defense[num - 1];
				yield return new KeyValuePair<string, int>("STR", quad);
				yield return new KeyValuePair<string, int>("DEX", quad);
				yield return new KeyValuePair<string, int>("INT", quad);
				yield return new KeyValuePair<string, int>("LUK", quad);
				yield return new KeyValuePair<string, int>("PAD", attack);
				yield return new KeyValuePair<string, int>("MAD", attack);
				yield return new KeyValuePair<string, int>("HP", hp);
				yield return new KeyValuePair<string, int>("PDD", defense);
				yield return new KeyValuePair<string, int>("MDD", defense);
			}
		}
	}
}
