using System;
using System.Collections.Generic;
using System.Globalization;

public sealed class DbTable
{
	public string name;

	public List<DbColumn> columns = new List<DbColumn>();

	public List<DbRow> rows = new List<DbRow>();

	public long nextId = 1L;

	public string PrimaryName
	{
		get
		{
			foreach (DbColumn column in columns)
			{
				if (column.primaryKey)
				{
					return column.name;
				}
			}
			return null;
		}
	}

	public DbColumn Col(string n)
	{
		foreach (DbColumn column in columns)
		{
			if (column.name == n)
			{
				return column;
			}
		}
		return null;
	}

	public long Insert(DbRow values)
	{
		DbRow dbRow = new DbRow();
		foreach (DbColumn column in columns)
		{
			object value;
			if (column.primaryKey && column.autoIncrement && (!values.ContainsKey(column.name) || values[column.name] == null))
			{
				dbRow[column.name] = nextId;
			}
			else if (values.TryGetValue(column.name, out value) && value != null)
			{
				dbRow[column.name] = Coerce(column, value);
			}
			else
			{
				dbRow[column.name] = column.defaultValue ?? ((column.notNull || column.primaryKey) ? DefaultFor(column.type) : null);
			}
		}
		rows.Add(dbRow);
		string primaryName = PrimaryName;
		long num = ((primaryName != null && dbRow.ContainsKey(primaryName)) ? Convert.ToInt64(dbRow[primaryName], CultureInfo.InvariantCulture) : 0);
		if (num >= nextId)
		{
			nextId = num + 1;
		}
		return num;
	}

	public int Update(Func<DbRow, bool> where, DbRow values)
	{
		int num = 0;
		foreach (DbRow row in rows)
		{
			if (!where(row))
			{
				continue;
			}
			foreach (KeyValuePair<string, object> value in values)
			{
				DbColumn dbColumn = Col(value.Key);
				if (dbColumn != null)
				{
					row[value.Key] = Coerce(dbColumn, value.Value);
				}
			}
			num++;
		}
		return num;
	}

	public int Delete(Func<DbRow, bool> where)
	{
		return rows.RemoveAll((DbRow r) => where(r));
	}

	public List<DbRow> Select(Func<DbRow, bool> where)
	{
		List<DbRow> list = new List<DbRow>();
		foreach (DbRow row in rows)
		{
			if (where == null || where(row))
			{
				list.Add(row);
			}
		}
		return list;
	}

	public DbRow Find(Func<DbRow, bool> where)
	{
		foreach (DbRow row in rows)
		{
			if (where(row))
			{
				return row;
			}
		}
		return null;
	}

	private static object DefaultFor(DbType t)
	{
		return t switch
		{
			DbType.Integer => (object)0L, 
			DbType.Real => 0.0, 
			_ => string.Empty, 
		};
	}

	public static object Coerce(DbColumn c, object v)
	{
		if (v == null)
		{
			return null;
		}
		switch (c.type)
		{
		case DbType.Integer:
			if (v is bool)
			{
				return (long)(((bool)v) ? 1 : 0);
			}
			try
			{
				return Convert.ToInt64(v, CultureInfo.InvariantCulture);
			}
			catch
			{
				return 0L;
			}
		case DbType.Real:
			try
			{
				return Convert.ToDouble(v, CultureInfo.InvariantCulture);
			}
			catch
			{
				return 0.0;
			}
		default:
			if (!(v is string result))
			{
				return v.ToString();
			}
			return result;
		}
	}
}
