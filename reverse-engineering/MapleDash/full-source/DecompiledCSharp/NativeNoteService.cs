using System;
using System.Collections.Generic;

public static class NativeNoteService
{
	public static int AvailableNotes;

	public static readonly TimeSpan FredrickInterval = TimeSpan.FromHours(1.0);

	private static DateTime lastFredrick = DateTime.MinValue;

	internal static void RunFredrickIfDue(DbStore db)
	{
		if (!(DateTime.Now - lastFredrick < FredrickInterval))
		{
			lastFredrick = DateTime.Now;
			NativeMerchantService.FredrickDueReminders(db, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
		}
	}

	public static void ResetForTest()
	{
		AvailableNotes = 0;
		lastFredrick = DateTime.MinValue;
	}

	public static void SendNormal(DbStore db, string message, string from, string to)
	{
		DbRow values = new DbRow
		{
			["to"] = to,
			["from"] = from,
			["message"] = message,
			["timestamp"] = Now(),
			["fame"] = 0L,
			["deleted"] = 0L
		};
		db.Table("notes").Insert(values);
		db.Save();
	}

	public static void SendWithFame(DbStore db, string message, string from, string to)
	{
		DbRow values = new DbRow
		{
			["to"] = to,
			["from"] = from,
			["message"] = message,
			["timestamp"] = Now(),
			["fame"] = 1L,
			["deleted"] = 0L
		};
		db.Table("notes").Insert(values);
		db.Save();
	}

	public static List<MemoNote> Unread(DbStore db, string to)
	{
		List<MemoNote> list = new List<MemoNote>();
		foreach (DbRow item in from x in db.Table("notes")
			select string.Equals(DbStore.Str(x, "to"), to, StringComparison.Ordinal) && DbStore.Int(x, "deleted") == 0)
		{
			list.Add(new MemoNote
			{
				id = DbStore.Int(item, "id"),
				from = DbStore.Str(item, "from"),
				message = DbStore.Str(item, "message"),
				timestamp = DbStore.Long(item, "timestamp", 0L),
				fame = DbStore.Int(item, "fame")
			});
		}
		return list;
	}

	public static int Delete(DbStore db, int noteId)
	{
		DbRow dbRow = db.Table("notes").Find((DbRow x) => DbStore.Int(x, "id") == noteId);
		if (dbRow == null)
		{
			return -1;
		}
		int result = DbStore.Int(dbRow, "fame");
		db.Table("notes").Delete((DbRow x) => DbStore.Int(x, "id") == noteId);
		db.Save();
		return result;
	}

	public static MemoResponse Handle(DbStore db, MemoRequest r)
	{
		MemoResponse memoResponse = new MemoResponse();
		DbRow dbRow = db.Table("characters").Find((DbRow x) => DbStore.Int(x, "id") == r.characterId);
		if (dbRow == null)
		{
			memoResponse.error = "角色不存在";
			return memoResponse;
		}
		string text = DbStore.Str(dbRow, "name");
		RunFredrickIfDue(db);
		switch (r.action)
		{
		case 0:
		{
			if (AvailableNotes <= 0)
			{
				memoResponse.error = "没有可回复的便签次数";
				return memoResponse;
			}
			if (string.IsNullOrEmpty(r.message) || r.message.Length > 73)
			{
				memoResponse.error = "便签须为1～73个字符";
				return memoResponse;
			}
			DbRow dbRow3 = db.Table("characters").Find((DbRow x) => string.Equals(DbStore.Str(x, "name"), r.target, StringComparison.OrdinalIgnoreCase));
			if (dbRow3 == null)
			{
				memoResponse.error = "收件角色不存在";
				return memoResponse;
			}
			SendWithFame(db, r.message, text, DbStore.Str(dbRow3, "name"));
			AvailableNotes--;
			memoResponse.sent = true;
			memoResponse.ok = true;
			return memoResponse;
		}
		case 1:
		{
			int num = 0;
			if (r.discardIds != null)
			{
				int[] discardIds = r.discardIds;
				foreach (int noteId in discardIds)
				{
					int num3 = Delete(db, noteId);
					if (num3 >= 0)
					{
						num += num3;
					}
				}
			}
			if (num > 0)
			{
				int num4 = DbStore.Int(dbRow, "fame") + num;
				db.Table("characters").Update(DbStore.Eq("id", r.characterId), new DbRow { ["fame"] = (long)num4 });
				db.Save();
			}
			memoResponse.fameGain = num;
			memoResponse.ok = true;
			return memoResponse;
		}
		case 2:
			memoResponse.notes = Unread(db, text).ToArray();
			memoResponse.ok = true;
			return memoResponse;
		case 3:
		{
			if (string.IsNullOrEmpty(r.message) || r.message.Length > 73)
			{
				memoResponse.error = "便签须为1～73个字符";
				return memoResponse;
			}
			DbRow dbRow2 = db.Table("characters").Find((DbRow x) => string.Equals(DbStore.Str(x, "name"), r.target, StringComparison.OrdinalIgnoreCase));
			if (dbRow2 == null)
			{
				memoResponse.error = "收件角色不存在";
				return memoResponse;
			}
			SendNormal(db, r.message, text, DbStore.Str(dbRow2, "name"));
			memoResponse.sent = true;
			memoResponse.ok = true;
			return memoResponse;
		}
		default:
			memoResponse.error = "未知操作";
			return memoResponse;
		}
	}

	private static long Now()
	{
		return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
	}
}
