using System;
using System.Collections.Generic;
using System.Globalization;

public static class NativeRankingService
{
	private sealed class TopCacheEntry
	{
		public DateTime at;

		public List<RankingEntry> list;
	}

	public const int TopLimit = 50;

	public static readonly TimeSpan RankInterval = TimeSpan.FromHours(1.0);

	public static readonly TimeSpan TopInterval = TimeSpan.FromMinutes(5.0);

	private static DateTime lastRankRefresh = DateTime.MinValue;

	private static DateTime lastUpdate = DateTime.Now;

	private static readonly Dictionary<string, TopCacheEntry> topCache = new Dictionary<string, TopCacheEntry>();

	public static void Invalidate()
	{
		topCache.Clear();
		lastRankRefresh = DateTime.MinValue;
		lastUpdate = DateTime.Now;
	}

	public static RankingResponse Handle(DbStore db, RankingRequest r)
	{
		CharRecord charRecord = CharactersDao.FindById(db, r.characterId);
		if (charRecord == null)
		{
			return new RankingResponse
			{
				ok = false,
				error = "角色不存在"
			};
		}
		RefreshIfDue(db);
		bool flag = GameConfigDao.GetBool(db, "use_whole_server_ranking");
		int worlds = Math.Max(1, WorldService.Worlds(db).Count);
		RankingResponse rankingResponse = new RankingResponse
		{
			world = charRecord.world,
			jobGroup = r.jobGroup,
			worlds = worlds,
			wholeServer = flag,
			entries = Top(db, flag, charRecord.world, r.jobGroup, worlds)
		};
		CharRecord charRecord2 = CharactersDao.FindById(db, r.characterId) ?? charRecord;
		rankingResponse.myRank = charRecord2.rank;
		rankingResponse.myRankMove = charRecord2.rankMove;
		rankingResponse.myJobRank = charRecord2.jobRank;
		rankingResponse.myJobRankMove = charRecord2.jobRankMove;
		rankingResponse.myLevel = charRecord2.level;
		rankingResponse.myJob = charRecord2.job;
		return rankingResponse;
	}

	private static void RefreshIfDue(DbStore db)
	{
		DateTime now = DateTime.Now;
		if (!(lastRankRefresh != DateTime.MinValue) || !(now - lastRankRefresh < RankInterval))
		{
			RefreshRank(db, now);
		}
	}

	public static void RefreshRank(DbStore db)
	{
		RefreshRank(db, DateTime.Now);
	}

	private static void RefreshRank(DbStore db, DateTime now)
	{
		if (GameConfigDao.GetBool(db, "use_refresh_rank_move", fallback: true))
		{
			ResetMoveRank(db, job: false);
			ResetMoveRank(db, job: true);
		}
		Dictionary<int, List<int>> dictionary = new Dictionary<int, List<int>>();
		foreach (DbRow row in db.Table("characters").rows)
		{
			int key = DbStore.Int(row, "world");
			if (!dictionary.TryGetValue(key, out var value))
			{
				value = (dictionary[key] = new List<int>());
			}
			if (DbStore.Int(row, "gm") < 2)
			{
				int item = DbStore.Int(row, "job") / 100;
				if (!value.Contains(item))
				{
					value.Add(item);
				}
			}
		}
		foreach (KeyValuePair<int, List<int>> item2 in dictionary)
		{
			UpdateRanking(db, -1, item2.Key);
			foreach (int item3 in item2.Value)
			{
				UpdateRanking(db, item3, item2.Key);
			}
		}
		db.Save();
		lastRankRefresh = now;
		lastUpdate = now;
	}

	private static void ResetMoveRank(DbStore db, bool job)
	{
		string key = (job ? "jobRankMove" : "rankMove");
		foreach (DbRow row in db.Table("characters").rows)
		{
			row[key] = 0L;
		}
	}

	private static void UpdateRanking(DbStore db, int jobGroup, int world)
	{
		List<DbRow> list = new List<DbRow>();
		foreach (DbRow row in db.Table("characters").rows)
		{
			if (DbStore.Int(row, "world") == world && DbStore.Int(row, "gm") < 2 && (jobGroup == -1 || DbStore.Int(row, "job") / 100 == jobGroup))
			{
				list.Add(row);
			}
		}
		list.Sort(CompareRank);
		DbTable accounts = db.Table("accounts");
		string text = ((jobGroup != -1) ? "jobRank" : "rank");
		string text2 = ((jobGroup != -1) ? "jobRankMove" : "rankMove");
		int num = 0;
		foreach (DbRow item in list)
		{
			num++;
			int num2 = 0;
			DbRow dbRow = FindAccount(accounts, DbStore.Long(item, "accountid", 0L));
			if (dbRow == null || IsBefore(DbStore.Str(dbRow, "lastlogin"), lastUpdate) || DbStore.Int(dbRow, "loggedin") > 0)
			{
				num2 = DbStore.Int(item, text2);
			}
			num2 += DbStore.Int(item, text) - num;
			item[text] = (long)num;
			item[text2] = (long)num2;
		}
	}

	private static int CompareRank(DbRow a, DbRow b)
	{
		int num = DbStore.Int(b, "level").CompareTo(DbStore.Int(a, "level"));
		if (num != 0)
		{
			return num;
		}
		num = DbStore.Long(b, "exp", 0L).CompareTo(DbStore.Long(a, "exp", 0L));
		if (num != 0)
		{
			return num;
		}
		num = string.CompareOrdinal(DbStore.Str(a, "lastExpGainTime"), DbStore.Str(b, "lastExpGainTime"));
		if (num != 0)
		{
			return num;
		}
		num = DbStore.Int(b, "fame").CompareTo(DbStore.Int(a, "fame"));
		if (num != 0)
		{
			return num;
		}
		num = DbStore.Long(b, "meso", 0L).CompareTo(DbStore.Long(a, "meso", 0L));
		if (num != 0)
		{
			return num;
		}
		return DbStore.Long(a, "id", 0L).CompareTo(DbStore.Long(b, "id", 0L));
	}

	private static List<RankingEntry> Top(DbStore db, bool whole, int world, int jobGroup, int worlds)
	{
		string key = (whole ? "A" : ("W" + world)) + "/" + jobGroup + "/" + worlds;
		DateTime now = DateTime.Now;
		if (topCache.TryGetValue(key, out var value) && now - value.at < TopInterval)
		{
			return value.list;
		}
		DbTable accounts = db.Table("accounts");
		List<DbRow> list = new List<DbRow>();
		foreach (DbRow row in db.Table("characters").rows)
		{
			if (DbStore.Int(row, "gm") >= 2)
			{
				continue;
			}
			int num = DbStore.Int(row, "world");
			if (whole)
			{
				if (num < 0 || num > worlds - 1)
				{
					continue;
				}
			}
			else if (num != world)
			{
				continue;
			}
			if (jobGroup == -1 || DbStore.Int(row, "job") / 100 == jobGroup)
			{
				DbRow dbRow = FindAccount(accounts, DbStore.Long(row, "accountid", 0L));
				if (dbRow == null || DbStore.Int(dbRow, "banned") == 0 || IsNullTempban(dbRow))
				{
					list.Add(row);
				}
			}
		}
		list.Sort(whole ? new Comparison<DbRow>(CompareWholeTop) : new Comparison<DbRow>(CompareTop));
		List<RankingEntry> list2 = new List<RankingEntry>();
		for (int i = 0; i < list.Count && i < 50; i++)
		{
			DbRow r = list[i];
			list2.Add(new RankingEntry
			{
				name = DbStore.Str(r, "name"),
				level = DbStore.Int(r, "level"),
				job = DbStore.Int(r, "job"),
				world = DbStore.Int(r, "world"),
				rank = i + 1,
				rankMove = DbStore.Int(r, "rankMove")
			});
		}
		topCache[key] = new TopCacheEntry
		{
			at = now,
			list = list2
		};
		return list2;
	}

	private static int CompareTop(DbRow a, DbRow b)
	{
		int num = DbStore.Int(b, "level").CompareTo(DbStore.Int(a, "level"));
		if (num != 0)
		{
			return num;
		}
		num = DbStore.Long(b, "exp", 0L).CompareTo(DbStore.Long(a, "exp", 0L));
		if (num != 0)
		{
			return num;
		}
		num = string.CompareOrdinal(DbStore.Str(a, "lastExpGainTime"), DbStore.Str(b, "lastExpGainTime"));
		if (num != 0)
		{
			return num;
		}
		return DbStore.Long(a, "id", 0L).CompareTo(DbStore.Long(b, "id", 0L));
	}

	private static int CompareWholeTop(DbRow a, DbRow b)
	{
		int num = DbStore.Int(a, "world").CompareTo(DbStore.Int(b, "world"));
		if (num == 0)
		{
			return CompareTop(a, b);
		}
		return num;
	}

	private static DbRow FindAccount(DbTable accounts, long accountId)
	{
		return accounts.Find((DbRow x) => DbStore.Long(x, "id", 0L) == accountId);
	}

	private static bool IsNullTempban(DbRow account)
	{
		if (account.TryGetValue("tempban", out var value))
		{
			return value == null;
		}
		return true;
	}

	private static bool IsBefore(string lastlogin, DateTime reference)
	{
		if (string.IsNullOrEmpty(lastlogin))
		{
			return true;
		}
		if (DateTime.TryParse(lastlogin, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
		{
			return result < reference;
		}
		return false;
	}
}
