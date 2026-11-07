using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

public sealed class DbStore
{
	private sealed class SaveBatch : IDisposable
	{
		private DbStore store;

		public SaveBatch(DbStore store)
		{
			this.store = store;
		}

		public void Dispose()
		{
			DbStore dbStore = store;
			if (dbStore != null)
			{
				store = null;
				if (--dbStore.saveBatchDepth == 0 && dbStore.saveBatchPending)
				{
					dbStore.saveBatchPending = false;
					dbStore.Save();
				}
			}
		}
	}

	public const int SchemaVersion = 1;

	private readonly Dictionary<string, DbTable> tables = new Dictionary<string, DbTable>();

	private string path;

	private bool dirty;

	private List<Dictionary<string, DbTable>> txStack;

	private List<Dictionary<string, List<DbRow>>> txRows;

	private int saveBatchDepth;

	private bool saveBatchPending;

	public string FilePath => path;

	public bool Dirty => dirty;

	public IEnumerable<string> TableNames => tables.Keys;

	public static DbStore Create(string dbPath)
	{
		DbStore dbStore = new DbStore();
		dbStore.path = dbPath;
		dbStore.LoadSchema();
		GameConfigDao.EnsureLevelCaps(dbStore);
		return dbStore;
	}

	public static DbStore Open(string dbPath)
	{
		DbStore dbStore = new DbStore
		{
			path = dbPath
		};
		dbStore.LoadSchema();
		if (File.Exists(dbPath))
		{
			dbStore.ReadJson(File.ReadAllText(dbPath));
		}
		else
		{
			dbStore.Save();
		}
		if (InventoryDao.PurgeRetiredSoulMaterials(dbStore) > 0)
		{
			dbStore.Save();
		}
		GameConfigDao.EnsureLevelCaps(dbStore);
		return dbStore;
	}

	private void LoadSchema()
	{
		string text = PlatformRes.LoadText("Server/Db/Schema");
		if (string.IsNullOrEmpty(text))
		{
			throw new InvalidOperationException("Schema.sql 缺失");
		}
		Parse(text);
	}

	private void Parse(string sql)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string[] array = sql.Replace("\r\n", "\n").Split('\n');
		foreach (string text in array)
		{
			if (!text.Trim().StartsWith("--"))
			{
				stringBuilder.Append(StripTrailingComment(text)).Append('\n');
			}
		}
		array = stringBuilder.ToString().Split(';');
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i].Trim();
			if (text2.Length != 0)
			{
				if (text2.StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
				{
					ParseCreate(text2);
				}
				else if (text2.StartsWith("INSERT INTO", StringComparison.OrdinalIgnoreCase))
				{
					ParseInsert(text2);
				}
			}
		}
	}

	private static string StripTrailingComment(string line)
	{
		bool flag = false;
		for (int i = 0; i < line.Length; i++)
		{
			char c = line[i];
			if (flag)
			{
				if (c == '\'')
				{
					flag = false;
				}
				continue;
			}
			switch (c)
			{
			case '\'':
				flag = true;
				break;
			case '-':
				if (i + 1 < line.Length && line[i + 1] == '-')
				{
					return line.Substring(0, i);
				}
				break;
			}
		}
		return line;
	}

	private void ParseCreate(string stmt)
	{
		int num = stmt.IndexOf('(');
		string text = stmt.Substring(0, num).Trim();
		string text2 = text.Substring(text.IndexOf("TABLE", StringComparison.OrdinalIgnoreCase) + 5).Trim();
		string body = stmt.Substring(num + 1, stmt.LastIndexOf(')') - num - 1);
		DbTable dbTable = new DbTable
		{
			name = text2
		};
		foreach (string item in SplitTop(body))
		{
			string text3 = item.Trim();
			if (text3.Length == 0)
			{
				continue;
			}
			string text4 = text3.ToUpperInvariant();
			if (text4.StartsWith("UNIQUE") || text4.StartsWith("PRIMARY KEY") || text4.StartsWith("FOREIGN KEY") || text4.StartsWith("CONSTRAINT") || text4.StartsWith("INDEX") || text4.StartsWith("FULLTEXT") || (text4.StartsWith("KEY") && text4.Length > 3 && !text4.Substring(3).TrimStart().StartsWith("INTEGER", StringComparison.Ordinal) && (char.IsWhiteSpace(text4[3]) || text4[3] == '`' || text4[3] == '(')))
			{
				continue;
			}
			int num2 = text3.IndexOf(' ');
			DbColumn dbColumn = new DbColumn();
			dbColumn.name = text3.Substring(0, num2).Trim('`', '"', ' ');
			DbColumn dbColumn2 = dbColumn;
			string text5 = text3.Substring(num2 + 1).Trim();
			int num3 = text5.IndexOf(' ');
			string text6 = ((num3 < 0) ? text5 : text5.Substring(0, num3)).ToUpperInvariant();
			dbColumn2.type = ((!text6.Contains("INT")) ? ((text6.Contains("REAL") || text6.Contains("FLOAT") || text6.Contains("DOUBLE")) ? DbType.Real : DbType.Text) : DbType.Integer);
			string text7 = ((num3 < 0) ? "" : text5.Substring(num3 + 1)).ToUpperInvariant();
			dbColumn2.notNull = text7.Contains("NOT NULL");
			dbColumn2.primaryKey = text7.Contains("PRIMARY KEY");
			dbColumn2.autoIncrement = text7.Contains("AUTOINCREMENT");
			int num4 = text7.IndexOf("DEFAULT ", StringComparison.Ordinal);
			if (num4 >= 0)
			{
				string text8 = text7.Substring(num4 + 8).Trim();
				if (text8.StartsWith("'"))
				{
					int num5 = text8.IndexOf('\'', 1);
					dbColumn2.defaultValue = text8.Substring(1, num5 - 1);
				}
				else
				{
					int num6 = text8.IndexOf(' ');
					string text9 = ((num6 < 0) ? text8 : text8.Substring(0, num6));
					if (text9.StartsWith("CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase))
					{
						dbColumn2.defaultValue = NowText();
					}
					else if (dbColumn2.type == DbType.Integer)
					{
						dbColumn2.defaultValue = ParseLong(text9);
					}
					else if (dbColumn2.type == DbType.Real)
					{
						dbColumn2.defaultValue = ParseDouble(text9);
					}
					else
					{
						dbColumn2.defaultValue = text9;
					}
				}
			}
			dbTable.columns.Add(dbColumn2);
		}
		if (dbTable.columns.Count == 0)
		{
			throw new InvalidOperationException("Schema 解析失败: " + text2);
		}
		tables[text2] = dbTable;
	}

	private void ParseInsert(string stmt)
	{
		int num = stmt.IndexOf('(');
		string text = stmt.Substring(0, num);
		string name = text.Substring(text.IndexOf("INTO", StringComparison.OrdinalIgnoreCase) + 4).Trim();
		string body = stmt.Substring(num + 1, stmt.IndexOf(')') - num - 1);
		List<string> list = new List<string>();
		foreach (string item in SplitTop(body))
		{
			list.Add(item.Trim().Trim('`', '"', ' '));
		}
		int num2 = stmt.IndexOf("VALUES", StringComparison.OrdinalIgnoreCase);
		foreach (string item2 in SplitTuples(stmt.Substring(num2 + 6).Trim()))
		{
			List<string> list2 = SplitTop(item2);
			DbTable dbTable = Table(name);
			DbRow dbRow = new DbRow();
			for (int i = 0; i < list.Count && i < list2.Count; i++)
			{
				DbColumn dbColumn = dbTable.Col(list[i]);
				if (dbColumn != null)
				{
					dbRow[dbColumn.name] = DbTable.Coerce(dbColumn, Unquote(list2[i].Trim()));
				}
			}
			dbTable.Insert(dbRow);
		}
	}

	private static List<string> SplitTuples(string valsPart)
	{
		List<string> list = new List<string>();
		int num = 0;
		int num2 = -1;
		bool flag = false;
		for (int i = 0; i < valsPart.Length; i++)
		{
			char c = valsPart[i];
			if (flag)
			{
				if (c == '\'')
				{
					flag = false;
				}
				continue;
			}
			switch (c)
			{
			case '\'':
				flag = true;
				break;
			case '(':
				if (num == 0)
				{
					num2 = i + 1;
				}
				num++;
				break;
			case ')':
				num--;
				if (num == 0 && num2 >= 0)
				{
					list.Add(valsPart.Substring(num2, i - num2));
					num2 = -1;
				}
				if (num < 0)
				{
					num = 0;
				}
				break;
			}
		}
		return list;
	}

	private static string Unquote(string v)
	{
		if (v.Length >= 2 && v[0] == '\'' && v[v.Length - 1] == '\'')
		{
			return v.Substring(1, v.Length - 2).Replace("''", "'");
		}
		return v;
	}

	private static long ParseLong(string s)
	{
		if (!long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return 0L;
		}
		return result;
	}

	private static double ParseDouble(string s)
	{
		if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			return 0.0;
		}
		return result;
	}

	private static List<string> SplitTop(string body)
	{
		List<string> list = new List<string>();
		int num = 0;
		bool flag = false;
		StringBuilder stringBuilder = new StringBuilder();
		foreach (char c in body)
		{
			if (flag)
			{
				stringBuilder.Append(c);
				if (c == '\'')
				{
					flag = false;
				}
				continue;
			}
			switch (c)
			{
			case '\'':
				flag = true;
				stringBuilder.Append(c);
				continue;
			case '(':
				num++;
				break;
			}
			if (c == ')')
			{
				num--;
			}
			if (c == ',' && num == 0)
			{
				list.Add(stringBuilder.ToString());
				stringBuilder.Clear();
			}
			else
			{
				stringBuilder.Append(c);
			}
		}
		if (stringBuilder.Length > 0)
		{
			list.Add(stringBuilder.ToString());
		}
		return list;
	}

	public DbTable Table(string name)
	{
		if (!tables.TryGetValue(name, out var value))
		{
			throw new KeyNotFoundException("表不存在: " + name);
		}
		return value;
	}

	public bool HasTable(string name)
	{
		return tables.ContainsKey(name);
	}

	public void BeginTransaction()
	{
		if (txStack == null)
		{
			txStack = new List<Dictionary<string, DbTable>>();
			txRows = new List<Dictionary<string, List<DbRow>>>();
		}
		Dictionary<string, DbTable> dictionary = new Dictionary<string, DbTable>();
		Dictionary<string, List<DbRow>> dictionary2 = new Dictionary<string, List<DbRow>>();
		foreach (KeyValuePair<string, DbTable> table in tables)
		{
			dictionary[table.Key] = table.Value;
			dictionary2[table.Key] = new List<DbRow>();
			foreach (DbRow row in table.Value.rows)
			{
				DbRow dbRow = new DbRow();
				foreach (KeyValuePair<string, object> item in row)
				{
					dbRow[item.Key] = item.Value;
				}
				dictionary2[table.Key].Add(dbRow);
			}
		}
		txStack.Add(dictionary);
		txRows.Add(dictionary2);
	}

	public void Commit()
	{
		if (txStack != null && txStack.Count != 0)
		{
			int index = txStack.Count - 1;
			txStack.RemoveAt(index);
			txRows.RemoveAt(index);
			dirty = true;
		}
	}

	public void Rollback()
	{
		if (txStack == null || txStack.Count == 0)
		{
			return;
		}
		int index = txStack.Count - 1;
		Dictionary<string, DbTable> dictionary = txStack[index];
		Dictionary<string, List<DbRow>> dictionary2 = txRows[index];
		foreach (KeyValuePair<string, DbTable> item in dictionary)
		{
			item.Value.rows.Clear();
			item.Value.rows.AddRange(dictionary2[item.Key]);
		}
		txStack.RemoveAt(index);
		txRows.RemoveAt(index);
		dirty = true;
	}

	public IDisposable DeferSaves()
	{
		saveBatchDepth++;
		return new SaveBatch(this);
	}

	public void Save()
	{
		if (saveBatchDepth > 0)
		{
			saveBatchPending = true;
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("{\"version\":").Append(1).Append(",\"tables\":{");
		bool flag = true;
		foreach (KeyValuePair<string, DbTable> table in tables)
		{
			if (!flag)
			{
				stringBuilder.Append(',');
			}
			flag = false;
			stringBuilder.Append('"').Append(table.Key).Append("\":{\"nextId\":")
				.Append(table.Value.nextId)
				.Append(",\"rows\":[");
			for (int i = 0; i < table.Value.rows.Count; i++)
			{
				if (i > 0)
				{
					stringBuilder.Append(',');
				}
				WriteRow(stringBuilder, table.Value, table.Value.rows[i]);
			}
			stringBuilder.Append("]}");
		}
		stringBuilder.Append("}");
		string directoryName = Path.GetDirectoryName(Path.GetFullPath(path));
		if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		File.WriteAllText(path, stringBuilder.ToString());
		dirty = false;
	}

	private static void WriteRow(StringBuilder sb, DbTable t, DbRow row)
	{
		sb.Append('{');
		bool flag = true;
		foreach (DbColumn column in t.columns)
		{
			if (!flag)
			{
				sb.Append(',');
			}
			flag = false;
			sb.Append('"').Append(column.name).Append("\":");
			row.TryGetValue(column.name, out var value);
			WriteValue(sb, value);
		}
		sb.Append('}');
	}

	private static void WriteValue(StringBuilder sb, object v)
	{
		if (v == null)
		{
			sb.Append("null");
		}
		else if (v is long num)
		{
			sb.Append(num.ToString(CultureInfo.InvariantCulture));
		}
		else if (v is int num2)
		{
			sb.Append(num2.ToString(CultureInfo.InvariantCulture));
		}
		else if (v is double num3)
		{
			sb.Append(num3.ToString("R", CultureInfo.InvariantCulture));
		}
		else if (v is bool flag)
		{
			sb.Append(flag ? "1" : "0");
		}
		else
		{
			sb.Append('"').Append(Escape(v.ToString())).Append('"');
		}
	}

	private static string Escape(string s)
	{
		return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n")
			.Replace("\r", "\\r");
	}

	public void ReadJson(string json)
	{
		if (!(MiniJsonDb.Parse(json) is Dictionary<string, object> dictionary) || !dictionary.TryGetValue("tables", out var value) || !(value is Dictionary<string, object> dictionary2))
		{
			return;
		}
		foreach (KeyValuePair<string, object> item in dictionary2)
		{
			if (!tables.TryGetValue(item.Key, out var value2) || !(item.Value is Dictionary<string, object> dictionary3))
			{
				continue;
			}
			if (dictionary3.TryGetValue("nextId", out var value3) && value3 != null)
			{
				value2.nextId = Convert.ToInt64(value3, CultureInfo.InvariantCulture);
			}
			value2.rows.Clear();
			if (!dictionary3.TryGetValue("rows", out var value4) || !(value4 is List<object> list))
			{
				continue;
			}
			foreach (object item2 in list)
			{
				if (!(item2 is Dictionary<string, object> dictionary4))
				{
					continue;
				}
				DbRow dbRow = new DbRow();
				foreach (DbColumn column in value2.columns)
				{
					if (dictionary4.TryGetValue(column.name, out var value5))
					{
						dbRow[column.name] = ((value5 == null) ? null : DbTable.Coerce(column, value5));
					}
				}
				value2.rows.Add(dbRow);
			}
		}
	}

	public static string NowText()
	{
		return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
	}

	public static Func<DbRow, bool> Eq(string col, object val)
	{
		return (DbRow r) => r.TryGetValue(col, out var value) && object.Equals(value, DbTable.Coerce(new DbColumn
		{
			type = GuessType(val)
		}, val));
	}

	private static DbType GuessType(object v)
	{
		if (v is long || v is int || v is bool)
		{
			return DbType.Integer;
		}
		if (v is double || v is float)
		{
			return DbType.Real;
		}
		return DbType.Text;
	}

	public static long Long(DbRow r, string col, long fallback = 0L)
	{
		if (r == null || !r.TryGetValue(col, out var value) || value == null)
		{
			return fallback;
		}
		try
		{
			return Convert.ToInt64(value, CultureInfo.InvariantCulture);
		}
		catch
		{
			return fallback;
		}
	}

	public static int Int(DbRow r, string col, int fallback = 0)
	{
		return (int)Long(r, col, fallback);
	}

	public static double Dbl(DbRow r, string col, double fallback = 0.0)
	{
		if (r == null || !r.TryGetValue(col, out var value) || value == null)
		{
			return fallback;
		}
		try
		{
			return Convert.ToDouble(value, CultureInfo.InvariantCulture);
		}
		catch
		{
			return fallback;
		}
	}

	public static string Str(DbRow r, string col, string fallback = "")
	{
		if (r == null || !r.TryGetValue(col, out var value) || value == null)
		{
			return fallback;
		}
		if (!(value is string result))
		{
			return value.ToString();
		}
		return result;
	}
}
