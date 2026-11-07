using System;
using System.Collections.Generic;
using System.Globalization;

public static class AdminDbUtil
{
	public static Dictionary<string, object> Page<T>(List<T> all, int pageNo, int pageSize, bool onlyTotal, bool notPage, Func<T, object> toDto)
	{
		if (all == null)
		{
			all = new List<T>();
		}
		long num = all.Count;
		int num2 = ((pageNo <= 0) ? 1 : pageNo);
		int num3 = ((pageSize <= 0) ? 20 : pageSize);
		if (onlyTotal)
		{
			return AdminJson.Page(new List<object>(), num2, num3, num);
		}
		List<object> list = new List<object>();
		if (notPage)
		{
			foreach (T item in all)
			{
				list.Add(toDto(item));
			}
			return AdminJson.Page(list, 1, (int)Math.Max(1L, num), num);
		}
		int num4 = (num2 - 1) * num3;
		for (int i = num4; i < all.Count && i < num4 + num3; i++)
		{
			list.Add(toDto(all[i]));
		}
		return AdminJson.Page(list, num2, num3, num);
	}

	public static Dictionary<string, object> Page(List<object> records, int pageNo, int pageSize, bool onlyTotal, bool notPage, long total)
	{
		int pageNumber = ((pageNo <= 0) ? 1 : pageNo);
		int pageSize2 = ((pageSize <= 0) ? 20 : pageSize);
		if (onlyTotal)
		{
			return AdminJson.Page(new List<object>(), pageNumber, pageSize2, total);
		}
		if (notPage)
		{
			return AdminJson.Page(records, 1, (int)Math.Max(1L, total), total);
		}
		return AdminJson.Page(records, pageNumber, pageSize2, total);
	}

	public static long NextId(DbStore db, string table)
	{
		long num = 0L;
		foreach (DbRow row in db.Table(table).rows)
		{
			num = Math.Max(num, DbStore.Long(row, "id", 0L));
		}
		return num + 1;
	}

	public static void Set(DbRow row, string col, object value)
	{
		if (value == null)
		{
			row[col] = null;
		}
		else if (value is bool flag)
		{
			row[col] = (long)(flag ? 1 : 0);
		}
		else if (value is string value2)
		{
			row[col] = value2;
		}
		else if (value is double num)
		{
			row[col] = ((Math.Abs(num - Math.Round(num)) < 1E-09) ? ((object)(long)Math.Round(num)) : ((object)num));
		}
		else if (value is long || value is int)
		{
			row[col] = Convert.ToInt64(value);
		}
		else
		{
			row[col] = Convert.ToString(value, CultureInfo.InvariantCulture);
		}
	}

	public static string Text(DbRow row, string col)
	{
		return DbStore.Str(row, col);
	}
}
