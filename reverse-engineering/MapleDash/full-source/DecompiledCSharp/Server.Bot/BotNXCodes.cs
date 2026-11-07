using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Bot;

public sealed class BotNXCodes
{
	private readonly DbStore db;

	private readonly Random random;

	public BotNXCodes(DbStore db, Random random = null)
	{
		this.db = db;
		this.random = random ?? new Random();
	}

	public string Generate()
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < 3; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append('-');
			}
			for (int j = 0; j < 5; j++)
			{
				stringBuilder.Append("23456789ABCDEFGHJKMNPQRSTUVWXYZ"[random.Next("23456789ABCDEFGHJKMNPQRSTUVWXYZ".Length)]);
			}
		}
		return stringBuilder.ToString();
	}

	public int Create(string code, int quantity = 10000)
	{
		code = code.Replace("-", "");
		lock (db)
		{
			if (db.Table("nxcode").Find((DbRow r) => DbStore.Str(r, "code") == code) != null)
			{
				return -1;
			}
			db.BeginTransaction();
			try
			{
				long num = db.Table("nxcode").Insert(new DbRow
				{
					["code"] = code,
					["retriever"] = null,
					["expiration"] = 8901234567890L
				});
				db.Table("nxcode_items").Insert(new DbRow
				{
					["codeid"] = num,
					["type"] = 4L,
					["item"] = 0L,
					["quantity"] = (long)quantity
				});
				db.Commit();
				db.Save();
				return checked((int)num);
			}
			catch
			{
				db.Rollback();
				throw;
			}
		}
	}

	public string Redeem(CharRecord actor, string code, long now)
	{
		DbRow row = db.Table("nxcode").Find((DbRow r) => DbStore.Str(r, "code") == code);
		if (row == null)
		{
			return "兑换码不存在";
		}
		if (row["retriever"] != null)
		{
			return "兑换码已使用";
		}
		if (DbStore.Long(row, "expiration", 0L) < now)
		{
			return "兑换码已过期";
		}
		List<DbRow> list = from r in db.Table("nxcode_items")
			select DbStore.Int(r, "codeid") == DbStore.Int(row, "id");
		DbRow dbRow = AccountsDao.FindById(db, actor.accountid);
		if (dbRow == null)
		{
			return "账号不存在";
		}
		foreach (DbRow item in list)
		{
			if (DbStore.Int(item, "type") != 4)
			{
				return "该兑换码奖励类型尚未支持";
			}
		}
		long num = DbStore.Int(dbRow, "nxCredit");
		foreach (DbRow item2 in list)
		{
			num += DbStore.Int(item2, "quantity");
		}
		if (num < 0 || num > int.MaxValue)
		{
			return "余额超出上限";
		}
		dbRow["nxCredit"] = num;
		row["retriever"] = actor.name;
		return null;
	}
}
