using System;

public static class NativeCarnivalService
{
	public sealed class Session
	{
		public int mapId;

		public long deadline;

		public bool extended;

		public int redCp;

		public int blueCp;

		public int redTotal;

		public int blueTotal;

		public int summons;
	}

	public static Session Active;

	public static int FestivalPoints;

	public static Func<long> Now = () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

	public static Func<long> BotTickMs = null;

	private static long lastBotTick;

	private const int TimeDefault = 600;

	private const int TimeExpand = 180;

	private const int MinLevel = 30;

	private const int MaxLevel = 50;

	private static readonly int[] Cpq1Fields = new int[6] { 980000101, 980000201, 980000301, 980000401, 980000501, 980000601 };

	public static void ResetForTest()
	{
		Active = null;
		FestivalPoints = 0;
		Now = () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		BotTickMs = null;
		lastBotTick = 0L;
	}

	public static void AbandonOnLogout()
	{
		Active = null;
		lastBotTick = 0L;
	}

	private static bool IsCpq1Field(int mapId)
	{
		int[] cpq1Fields = Cpq1Fields;
		for (int i = 0; i < cpq1Fields.Length; i++)
		{
			if (cpq1Fields[i] == mapId)
			{
				return true;
			}
		}
		return false;
	}

	public static CarnivalResponse Handle(DbStore db, CarnivalRequest r)
	{
		CarnivalResponse carnivalResponse = new CarnivalResponse();
		DbRow dbRow = db.Table("characters").Find((DbRow x) => DbStore.Int(x, "id") == r.characterId);
		if (dbRow == null)
		{
			carnivalResponse.error = "角色不存在";
			return carnivalResponse;
		}
		long num = Now();
		if (Active != null && num >= Active.deadline)
		{
			Finish(carnivalResponse, dbRow, db, num);
		}
		BotTick(db, r.characterId);
		switch (r.operation)
		{
		case 0:
		{
			if (Active != null)
			{
				carnivalResponse.error = "已有嘉年华战斗正在进行";
				return carnivalResponse;
			}
			if (!IsCpq1Field(r.mapId))
			{
				carnivalResponse.error = "不是怪物嘉年华战斗图";
				return carnivalResponse;
			}
			int num2 = DbStore.Int(dbRow, "level");
			if (num2 < 30 || num2 > 50)
			{
				carnivalResponse.error = "等级须在 " + 30 + "~" + 50 + " 之间";
				return carnivalResponse;
			}
			Active = new Session
			{
				mapId = r.mapId,
				deadline = num + 600000
			};
			lastBotTick = num;
			carnivalResponse.ok = true;
			carnivalResponse.inCarnival = true;
			FillLive(carnivalResponse, num);
			return carnivalResponse;
		}
		case 1:
			if (Active == null)
			{
				carnivalResponse.error = "不在嘉年华战斗中";
				return carnivalResponse;
			}
			Active.redCp += r.cp;
			if (r.cp > 0)
			{
				Active.redTotal += r.cp;
			}
			if (Active.redCp < 0)
			{
				Active.redCp = 0;
			}
			carnivalResponse.ok = true;
			FillLive(carnivalResponse, num);
			return carnivalResponse;
		case 2:
			carnivalResponse.ok = true;
			FillLive(carnivalResponse, num);
			return carnivalResponse;
		case 3:
			if (Active == null)
			{
				carnivalResponse.error = "不在嘉年华战斗中";
				return carnivalResponse;
			}
			Finish(carnivalResponse, dbRow, db, num, forceLose: true);
			return carnivalResponse;
		default:
			carnivalResponse.error = "未知操作";
			return carnivalResponse;
		}
	}

	private static void FillLive(CarnivalResponse resp, long now)
	{
		if (Active != null)
		{
			resp.inCarnival = true;
			resp.mapId = Active.mapId;
			resp.team = 0;
			resp.redCp = Active.redCp;
			resp.blueCp = Active.blueCp;
			resp.redTotal = Active.redTotal;
			resp.blueTotal = Active.blueTotal;
			resp.secondsLeft = (int)Math.Max(0L, (Active.deadline - now) / 1000);
			resp.festivalPoints = FestivalPoints;
		}
	}

	public static void BotTick(DbStore db, int characterId)
	{
		if (Active == null || BotTickMs == null)
		{
			return;
		}
		long num = Now();
		if (num >= Active.deadline)
		{
			Handle(db, new CarnivalRequest
			{
				characterId = characterId,
				operation = 2
			});
			return;
		}
		if (lastBotTick == 0L)
		{
			lastBotTick = num;
		}
		while (num - lastBotTick >= 1000)
		{
			lastBotTick += 1000L;
			long num2 = BotTickMs();
			if (num2 < 90)
			{
				if (num2 < 40)
				{
					Active.blueCp++;
					Active.blueTotal++;
				}
				else if (num2 < 60)
				{
					Active.blueCp += 2;
					Active.blueTotal += 2;
				}
				else
				{
					Active.blueCp++;
					Active.blueTotal++;
				}
				if (Active.blueCp < 0)
				{
					Active.blueCp = 0;
				}
			}
		}
	}

	private static void Finish(CarnivalResponse resp, DbRow self, DbStore db, long now, bool forceLose = false)
	{
		Session active = Active;
		if (active == null)
		{
			return;
		}
		if (!forceLose && active.redTotal == active.blueTotal && !active.extended)
		{
			active.extended = true;
			active.deadline = now + 180000;
			resp.ok = true;
			FillLive(resp, now);
			return;
		}
		int num = (forceLose ? 1 : ((active.redTotal <= active.blueTotal) ? 1 : 0));
		bool flag = IsCpq1Field(active.mapId);
		int num2 = active.mapId / 100 * 100;
		int num3;
		int num4;
		if (flag)
		{
			num3 = num2 + 3;
			num4 = num2 + 4;
		}
		else
		{
			num3 = num2 + 200;
			num4 = num2 + 300;
		}
		bool flag2 = num == 0;
		int num5 = (flag2 ? active.redTotal : active.blueTotal);
		if (flag2)
		{
			FestivalPoints += num5;
		}
		resp.ok = true;
		resp.finished = true;
		resp.inCarnival = false;
		resp.winner = num;
		resp.win = flag2;
		resp.warpTo = (flag2 ? num3 : num4);
		resp.festivalPoints = FestivalPoints;
		resp.redTotal = active.redTotal;
		resp.blueTotal = active.blueTotal;
		Active = null;
	}
}
