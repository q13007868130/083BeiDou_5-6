using System;
using System.Collections.Generic;
using System.Globalization;

public static class NativeReportService
{
	public const int MaxReportsPerSession = 10;

	public const int ReportFee = 300;

	private static readonly Dictionary<int, int> remaining = new Dictionary<int, int>();

	public static void EndSession(int characterId)
	{
		remaining.Remove(characterId);
	}

	private static int Remaining(int cid)
	{
		if (!remaining.TryGetValue(cid, out var value))
		{
			return 10;
		}
		return value;
	}

	public static ReportResponse Handle(DbStore db, ReportRequest r)
	{
		if (r == null)
		{
			return new ReportResponse
			{
				ok = false,
				code = 0,
				error = "无效请求"
			};
		}
		if (r.type != 0 && r.type != 1)
		{
			return new ReportResponse
			{
				ok = false,
				code = 0,
				error = "未知举报类型"
			};
		}
		int num = Remaining(r.characterId);
		if (num <= 0)
		{
			return new ReportResponse
			{
				ok = false,
				code = 2,
				error = "你今天已经不能再举报了。"
			};
		}
		CharRecord charRecord = CharactersDao.FindById(db, r.characterId);
		if (charRecord == null)
		{
			return new ReportResponse
			{
				ok = false,
				code = 0,
				error = "角色不存在"
			};
		}
		if (charRecord.meso <= 299)
		{
			return new ReportResponse
			{
				ok = false,
				code = 4,
				error = "金币不足，无法举报。"
			};
		}
		remaining[r.characterId] = num - 1;
		charRecord.meso -= 300L;
		CharactersDao.Save(db, charRecord);
		Console.WriteLine("[GM] " + charRecord.name + " 举报: " + r.victim + " was reported for: " + r.description);
		int num2 = -1;
		using (List<DbRow>.Enumerator enumerator = db.Table("characters").Select(DbStore.Eq("name", r.victim ?? "")).GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				num2 = (int)ConvertId(enumerator.Current);
			}
		}
		db.Table("reports").Insert(new DbRow
		{
			["reporttime"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
			["reporterid"] = r.characterId,
			["victimid"] = num2,
			["reason"] = r.reason,
			["chatlog"] = r.chatlog ?? "",
			["description"] = r.description ?? ""
		});
		db.Save();
		return new ReportResponse
		{
			ok = true,
			code = 0
		};
	}

	private static long ConvertId(DbRow row)
	{
		if (row == null || !row.TryGetValue("id", out var value) || value == null)
		{
			return -1L;
		}
		return Convert.ToInt64(value, CultureInfo.InvariantCulture);
	}
}
