using System;
using System.Collections.Generic;
using System.Globalization;

public static class NativeWeeklyGemstoneService
{
	private sealed class State
	{
		public string week = "";

		public string day = "";

		public string creditedDay = "";

		public int clear;

		public bool claimed;

		public bool started;

		public HashSet<int> tasks = new HashSet<int>();
	}

	private const int LedgerType = 31;

	private const string LedgerName = "tms282.weeklyGemstone";

	public static DateTimeOffset LocalTime(DateTimeOffset now)
	{
		return now.ToOffset(TimeSpan.FromHours(8.0));
	}

	public static string Week(DateTimeOffset now)
	{
		DateTime date = LocalTime(now).Date;
		return date.AddDays(-((int)(7 + date.DayOfWeek - 4) % 7)).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
	}

	private static string Day(DateTimeOffset now)
	{
		return LocalTime(now).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
	}

	private static int Canonical(int quest)
	{
		if (quest >= 0)
		{
			return quest;
		}
		return quest + 65536;
	}

	public static bool IsDaily(int quest)
	{
		return Array.IndexOf(WeeklyGemstoneData.DailyIds, Canonical(quest)) >= 0;
	}

	private static DbRow Row(DbStore db, int cid)
	{
		return db.Table("extend_value").Find((DbRow r) => DbStore.Int(r, "extend_type") == 31 && DbStore.Str(r, "extend_id") == cid.ToString(CultureInfo.InvariantCulture) && DbStore.Str(r, "extend_name") == "tms282.weeklyGemstone");
	}

	private static State Load(DbRow row)
	{
		State state = new State();
		string[] array = ((row == null) ? "" : DbStore.Str(row, "extend_value")).Split('|');
		if (array.Length != 7)
		{
			return state;
		}
		state.week = array[0];
		state.day = array[1];
		state.creditedDay = array[2];
		int.TryParse(array[3], out state.clear);
		state.claimed = array[4] == "1";
		state.started = array[5] == "1";
		string[] array2 = array[6].Split(',');
		for (int i = 0; i < array2.Length; i++)
		{
			if (int.TryParse(array2[i], out var result))
			{
				state.tasks.Add(result);
			}
		}
		return state;
	}

	private static bool Store(DbStore db, int cid, State s, DateTimeOffset now)
	{
		string text = string.Join("|", s.week, s.day, s.creditedDay, s.clear.ToString(CultureInfo.InvariantCulture), s.claimed ? "1" : "0", s.started ? "1" : "0", string.Join(",", s.tasks));
		DbRow dbRow = Row(db, cid);
		if (dbRow != null && DbStore.Str(dbRow, "extend_value") == text)
		{
			return false;
		}
		if (dbRow == null)
		{
			db.Table("extend_value").Insert(new DbRow
			{
				["extend_type"] = 31L,
				["extend_id"] = cid.ToString(CultureInfo.InvariantCulture),
				["extend_name"] = "tms282.weeklyGemstone",
				["extend_value"] = text,
				["create_time"] = now.ToUnixTimeMilliseconds(),
				["update_time"] = 0L
			});
		}
		else
		{
			dbRow["extend_value"] = text;
			dbRow["update_time"] = now.ToUnixTimeMilliseconds();
		}
		return true;
	}

	private static bool Eligible(DbStore db, int cid)
	{
		CharRecord charRecord = CharactersDao.FindById(db, cid);
		if (charRecord != null && charRecord.level >= 200)
		{
			if (QuestStatusDao.StatusOf(db, cid, 34120) != 2)
			{
				return QuestStatusDao.StatusOf(db, cid, -31416) == 2;
			}
			return true;
		}
		return false;
	}

	private static void RollWeek(State s, DateTimeOffset now)
	{
		string text = Week(now);
		if (!(s.week == text))
		{
			s.week = text;
			s.clear = 0;
			s.claimed = false;
			s.started = false;
		}
	}

	public static WeeklyGemstoneResponse Handle(DbStore db, WeeklyGemstoneRequest req, DateTimeOffset? instant = null)
	{
		WeeklyGemstoneResponse weeklyGemstoneResponse = new WeeklyGemstoneResponse();
		if (db == null || req == null || CharactersDao.FindById(db, req.characterId) == null)
		{
			weeklyGemstoneResponse.ok = false;
			weeklyGemstoneResponse.error = "角色不存在";
			return weeklyGemstoneResponse;
		}
		lock (db)
		{
			DateTimeOffset now = instant ?? DateTimeOffset.UtcNow;
			State state = Load(Row(db, req.characterId));
			RollWeek(state, now);
			string text = Day(now);
			if (state.day != text)
			{
				if (state.day != "")
				{
					foreach (DbRow item in QuestStatusDao.ByCharacter(db, req.characterId))
					{
						int num = DbStore.Int(item, "quest");
						if (IsDaily(num))
						{
							item["status"] = 0L;
							item["completed"] = 0L;
							weeklyGemstoneResponse.resetQuests.Add(num);
						}
					}
				}
				state.day = text;
				state.tasks.Clear();
			}
			weeklyGemstoneResponse.eligible = Eligible(db, req.characterId);
			if (req.operation != WeeklyGemstoneOperation.Read && !weeklyGemstoneResponse.eligible)
			{
				weeklyGemstoneResponse.ok = false;
				weeklyGemstoneResponse.error = "需达到等级并完成消逝的旅途前置任务";
			}
			else if (req.operation == WeeklyGemstoneOperation.Start)
			{
				state.started = true;
			}
			else if (req.operation == WeeklyGemstoneOperation.Claim)
			{
				if (state.claimed)
				{
					weeklyGemstoneResponse.ok = false;
					weeklyGemstoneResponse.error = "本周奖励已经领取";
				}
				else if (state.clear < 2)
				{
					weeklyGemstoneResponse.ok = false;
					weeklyGemstoneResponse.error = "本周调查次数不足";
				}
				else
				{
					CharRecord c = CharactersDao.FindById(db, req.characterId);
					db.BeginTransaction();
					try
					{
						NativeInventoryService.AddItems(db, c, 2435902, 3, -1L);
						state.claimed = true;
						state.started = true;
						Store(db, req.characterId, state, now);
						db.Save();
						db.Commit();
					}
					catch (InvalidOperationException)
					{
						db.Rollback();
						state.claimed = false;
						weeklyGemstoneResponse.ok = false;
						weeklyGemstoneResponse.error = "消耗栏空间不足，请整理背包后重试";
					}
					catch
					{
						db.Rollback();
						throw;
					}
				}
			}
			if (Store(db, req.characterId, state, now) || weeklyGemstoneResponse.resetQuests.Count > 0)
			{
				db.Save();
			}
			weeklyGemstoneResponse.clear = state.clear;
			weeklyGemstoneResponse.claimed = state.claimed;
			weeklyGemstoneResponse.started = state.started;
			return weeklyGemstoneResponse;
		}
	}

	public static void OnCompleted(DbStore db, int cid, int quest, DateTimeOffset? instant = null)
	{
		if (IsDaily(quest) && Eligible(db, cid))
		{
			DateTimeOffset now = instant ?? DateTimeOffset.UtcNow;
			State state = Load(Row(db, cid));
			RollWeek(state, now);
			string text = Day(now);
			if (state.day != text)
			{
				state.day = text;
				state.tasks.Clear();
			}
			state.tasks.Add(Canonical(quest));
			if (state.tasks.Count >= 3 && state.creditedDay != text)
			{
				state.clear = Math.Min(2, state.clear + 1);
				state.creditedDay = text;
				state.started = true;
			}
			Store(db, cid, state, now);
		}
	}
}
