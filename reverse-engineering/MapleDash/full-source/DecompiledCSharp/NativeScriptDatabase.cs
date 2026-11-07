using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using MapleDash.Js;

public static class NativeScriptDatabase
{
	private sealed class Rows
	{
		public readonly string[] columns;

		public readonly List<Dictionary<string, object>> values = new List<Dictionary<string, object>>();

		public Rows(params string[] columns)
		{
			this.columns = columns;
		}

		public void Add(params object[] fields)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < columns.Length; i++)
			{
				dictionary[columns[i]] = fields[i];
			}
			values.Add(dictionary);
		}
	}

	private static JsValue Fn(string name, Func<JsCall, JsValue> call)
	{
		return JsFunction.FromNative(name, call);
	}

	private static JsHostObject Host(object value, string kind)
	{
		return new JsHostObject(value)
		{
			Missing = (string k) =>
			{
				throw new JsRuntimeException("Unmigrated " + kind + " API: " + k);
			}
		};
	}

	private static void Open(bool closed)
	{
		if (closed)
		{
			throw new JsRuntimeException("JDBC object is closed");
		}
	}

	public static JsValue Type(DbStore db)
	{
		JsHostObject jsHostObject = Host(null, "DatabaseConnection");
		jsHostObject.Set("getConnection", Fn("getConnection", (JsCall _) => Connection(db)));
		return jsHostObject;
	}

	private static JsValue Connection(DbStore db)
	{
		bool closed = false;
		JsHostObject jsHostObject = Host(db, "Connection");
		jsHostObject.Set("close", Fn("close", (JsCall _) =>
		{
			closed = true;
			return JsUndefined.Value;
		}));
		jsHostObject.Set("isClosed", Fn("isClosed", (JsCall _) => JsBoolean.Of(closed)));
		jsHostObject.Set("prepareStatement", Fn("prepareStatement", (JsCall c) =>
		{
			Open(closed);
			return Statement(db, JsConvert.ToString(c.Arg(0)), () => closed);
		}));
		return jsHostObject;
	}

	private static JsValue Statement(DbStore db, string sql, Func<bool> connectionClosed)
	{
		bool closed = false;
		Dictionary<int, object> parameters = new Dictionary<int, object>();
		JsHostObject jsHostObject = Host(sql, "PreparedStatement");
		int parameterCount = Regex.Matches(sql, "\\?").Count;
		string[] array = new string[4] { "setInt", "setLong", "setDouble", "setString" };
		foreach (string text in array)
		{
			string setter = text;
			jsHostObject.Set(setter, Fn(setter, (JsCall c) =>
			{
				Check();
				int num = JsConvert.ToInt32(c.Arg(0));
				if (num < 1 || num > parameterCount)
				{
					throw new JsRuntimeException("Invalid JDBC parameter index: " + num);
				}
				parameters[num] = ((setter == "setString") ? JsConvert.ToString(c.Arg(1)) : ((object)JsConvert.ToNumber(c.Arg(1))));
				return JsUndefined.Value;
			}));
		}
		jsHostObject.Set("clearParameters", Fn("clearParameters", (JsCall _) =>
		{
			Check();
			parameters.Clear();
			return JsUndefined.Value;
		}));
		jsHostObject.Set("close", Fn("close", (JsCall _) =>
		{
			closed = true;
			return JsUndefined.Value;
		}));
		jsHostObject.Set("isClosed", Fn("isClosed", (JsCall _) => JsBoolean.Of(closed)));
		jsHostObject.Set("executeQuery", Fn("executeQuery", (JsCall _) =>
		{
			Check();
			return Result(Query(db, sql, parameters), () => closed || connectionClosed());
		}));
		return jsHostObject;
		void Check()
		{
			Open(closed || connectionClosed());
		}
	}

	private static JsValue Result(Rows rows, Func<bool> parentClosed)
	{
		int index = -1;
		bool closed = false;
		bool wasNull = false;
		JsHostObject jsHostObject = Host(rows, "ResultSet");
		jsHostObject.Set("next", Fn("next", (JsCall _) =>
		{
			Check();
			index++;
			return JsBoolean.Of(index < rows.values.Count);
		}));
		jsHostObject.Set("getString", Fn("getString", (JsCall c) =>
		{
			object obj = Read(c);
			return (obj != null) ? ((JsValue)new JsString(Convert.ToString(obj, CultureInfo.InvariantCulture))) : ((JsValue)JsNull.Value);
		}));
		jsHostObject.Set("getInt", Fn("getInt", (JsCall c) => new JsNumber(checked((int)Integral(Read(c))))));
		jsHostObject.Set("getLong", Fn("getLong", (JsCall c) => new JsNumber(Integral(Read(c)))));
		jsHostObject.Set("getDouble", Fn("getDouble", (JsCall c) => new JsNumber(Convert.ToDouble(Read(c) ?? ((object)0), CultureInfo.InvariantCulture))));
		jsHostObject.Set("wasNull", Fn("wasNull", (JsCall _) =>
		{
			Check();
			return JsBoolean.Of(wasNull);
		}));
		jsHostObject.Set("close", Fn("close", (JsCall _) =>
		{
			closed = true;
			return JsUndefined.Value;
		}));
		jsHostObject.Set("isClosed", Fn("isClosed", (JsCall _) => JsBoolean.Of(closed)));
		return jsHostObject;
		void Check()
		{
			Open(closed || parentClosed());
		}
		object Read(JsCall c)
		{
			Check();
			if (index < 0 || index >= rows.values.Count)
			{
				throw new JsRuntimeException("JDBC cursor is not on a row");
			}
			string text;
			if (c.Arg(0) is JsNumber)
			{
				int num = JsConvert.ToInt32(c.Arg(0));
				if (num < 1 || num > rows.columns.Length)
				{
					throw new JsRuntimeException("Invalid JDBC column index");
				}
				text = rows.columns[num - 1];
			}
			else
			{
				text = JsConvert.ToString(c.Arg(0));
			}
			if (!rows.values[index].TryGetValue(text, out var value))
			{
				throw new JsRuntimeException("Unknown JDBC column: " + text);
			}
			wasNull = value == null;
			return value;
		}
	}

	private static object Parameter(Dictionary<int, object> parameters, int index)
	{
		if (!parameters.TryGetValue(index, out var value))
		{
			throw new JsRuntimeException("Unbound JDBC parameter: " + index);
		}
		return value;
	}

	private static long Number(Dictionary<int, object> parameters, int index)
	{
		return Convert.ToInt64(Parameter(parameters, index), CultureInfo.InvariantCulture);
	}

	private static long Integral(object value)
	{
		return decimal.ToInt64(decimal.Truncate(Convert.ToDecimal(value ?? ((object)0), CultureInfo.InvariantCulture)));
	}

	private static string Normalize(string sql)
	{
		return Regex.Replace(sql.Trim().TrimEnd(';').ToLowerInvariant(), "\\s+", " ");
	}

	private static Dictionary<long, List<DbRow>> EquipmentIndex(DbStore db)
	{
		Dictionary<long, List<DbRow>> dictionary = new Dictionary<long, List<DbRow>>();
		foreach (DbRow row in db.Table("inventoryequipment").rows)
		{
			long key = DbStore.Long(row, "inventoryitemid", 0L);
			if (!dictionary.TryGetValue(key, out var value))
			{
				value = (dictionary[key] = new List<DbRow>());
			}
			value.Add(row);
		}
		return dictionary;
	}

	private static Rows Query(DbStore db, string sql, Dictionary<int, object> parameters)
	{
		string text = Normalize(sql);
		if (text.Contains(" from drop_data "))
		{
			DropStore.EnsureDatabase(db);
		}
		lock (db)
		{
			if (text == "select dropperid, chance, questid from drop_data where itemid = ? order by chance desc limit 30")
			{
				long item = Number(parameters, 1);
				List<DbRow> list = from r in db.Table("drop_data")
					select DbStore.Long(r, "itemid", 0L) == item;
				list.Sort((DbRow a, DbRow b) => DbStore.Long(b, "chance", 0L).CompareTo(DbStore.Long(a, "chance", 0L)));
				Rows rows = new Rows("dropperid", "chance", "questid");
				foreach (DbRow item3 in list)
				{
					if (rows.values.Count == 30)
					{
						break;
					}
					rows.Add(DbStore.Long(item3, "dropperid", 0L), DbStore.Long(item3, "chance", 0L), DbStore.Long(item3, "questid", 0L));
				}
				return rows;
			}
			Match match = Regex.Match(text, "^select itemid from drop_data where (\\(itemid between \\? and \\?\\)(?: or \\(itemid between \\? and \\?\\))*) group by itemid limit 200$");
			if (match.Success)
			{
				int count = Regex.Matches(match.Groups[1].Value, "between").Count;
				List<(long, long)> list2 = new List<(long, long)>();
				for (int num = 0; num < count; num++)
				{
					list2.Add((Number(parameters, num * 2 + 1), Number(parameters, num * 2 + 2)));
				}
				Rows rows2 = new Rows("itemid");
				HashSet<long> hashSet = new HashSet<long>();
				foreach (DbRow row in db.Table("drop_data").rows)
				{
					long num2 = DbStore.Long(row, "itemid", 0L);
					bool flag = false;
					foreach (var item4 in list2)
					{
						if (num2 >= item4.Item1 && num2 <= item4.Item2)
						{
							flag = true;
							break;
						}
					}
					if (flag && hashSet.Add(num2))
					{
						rows2.Add(num2);
					}
					if (rows2.values.Count == 200)
					{
						break;
					}
				}
				return rows2;
			}
			if (text == "select distinct itemid, dropperid from drop_data where itemid between 4000000 and 4009999 and questid = 0 and chance > 0 order by dropperid, itemid")
			{
				List<DbRow> list3 = from r in db.Table("drop_data")
					select DbStore.Long(r, "itemid", 0L) >= 4000000 && DbStore.Long(r, "itemid", 0L) <= 4009999 && DbStore.Long(r, "questid", 0L) == 0L && DbStore.Long(r, "chance", 0L) > 0;
				list3.Sort((DbRow a, DbRow b) =>
				{
					int num4 = DbStore.Long(a, "dropperid", 0L).CompareTo(DbStore.Long(b, "dropperid", 0L));
					return (num4 == 0) ? DbStore.Long(a, "itemid", 0L).CompareTo(DbStore.Long(b, "itemid", 0L)) : num4;
				});
				Rows rows3 = new Rows("itemid", "dropperid");
				HashSet<(long, long)> hashSet2 = new HashSet<(long, long)>();
				foreach (DbRow item5 in list3)
				{
					(long, long) item2 = (DbStore.Long(item5, "itemid", 0L), DbStore.Long(item5, "dropperid", 0L));
					if (hashSet2.Add(item2))
					{
						rows3.Add(item2.Item1, item2.Item2);
					}
				}
				return rows3;
			}
			if (text == "select chr.name, it.itemid, it.position, men.str, men.dex, men.int, men.luk, men.watk, men.matk, men.mdef, men.wdef, men.upgradeslots, men.level from inventoryitems it join inventoryequipment men on it.inventoryitemid = men.inventoryitemid join characters chr on it.characterid = chr.id where it.position < 0 and chr.id = ? order by it.position desc")
			{
				int cid = (int)Number(parameters, 1);
				CharRecord charRecord = CharactersDao.FindById(db, cid);
				Rows rows4 = new Rows("name", "itemid", "position", "str", "dex", "int", "luk", "watk", "matk", "mdef", "wdef", "upgradeslots", "level");
				if (charRecord == null)
				{
					return rows4;
				}
				List<DbRow> list4 = from r in db.Table("inventoryitems")
					select DbStore.Int(r, "characterid") == cid && DbStore.Int(r, "position") < 0;
				list4.Sort((DbRow a, DbRow b) => DbStore.Int(b, "position").CompareTo(DbStore.Int(a, "position")));
				Dictionary<long, List<DbRow>> dictionary = EquipmentIndex(db);
				foreach (DbRow item6 in list4)
				{
					if (!dictionary.TryGetValue(DbStore.Long(item6, "inventoryitemid", 0L), out var value))
					{
						continue;
					}
					foreach (DbRow item7 in value)
					{
						if (DbStore.Long(item6, "inventoryitemid", 0L) == DbStore.Long(item7, "inventoryitemid", 0L))
						{
							rows4.Add(charRecord.name, DbStore.Long(item6, "itemid", 0L), DbStore.Long(item6, "position", 0L), DbStore.Long(item7, "str", 0L), DbStore.Long(item7, "dex", 0L), DbStore.Long(item7, "int", 0L), DbStore.Long(item7, "luk", 0L), DbStore.Long(item7, "watk", 0L), DbStore.Long(item7, "matk", 0L), DbStore.Long(item7, "mdef", 0L), DbStore.Long(item7, "wdef", 0L), DbStore.Long(item7, "upgradeslots", 0L), DbStore.Long(item7, "level", 0L));
						}
					}
				}
				return rows4;
			}
			Match match2 = Regex.Match(text, "^select chr.id, chr.name, chr.job, sum\\( (.+) \\) as totalcombatpower from inventoryitems it, inventoryequipment men, characters chr where it.position < 0 and it.inventoryitemid = men.inventoryitemid and chr.id = it.characterid and chr.gm <= 0 group by chr.id order by totalcombatpower desc limit 100$");
			if (match2.Success)
			{
				List<(string, double)> list5 = new List<(string, double)>();
				string[] array = match2.Groups[1].Value.Split('+');
				foreach (string text2 in array)
				{
					Match match3 = Regex.Match(text2.Trim(), "^men\\.(str|dex|int|luk|watk|matk|wdef|mdef) \\* ([0-9]+(?:\\.[0-9]+)?)$");
					if (!match3.Success)
					{
						throw new JsRuntimeException("Unsupported ranking SQL expression: " + text2);
					}
					list5.Add((match3.Groups[1].Value, double.Parse(match3.Groups[2].Value, CultureInfo.InvariantCulture)));
				}
				Rows rows5 = new Rows("id", "name", "job", "totalCombatPower");
				Dictionary<long, List<DbRow>> dictionary2 = EquipmentIndex(db);
				Dictionary<long, double> dictionary3 = new Dictionary<long, double>();
				foreach (DbRow row2 in db.Table("inventoryitems").rows)
				{
					if (DbStore.Int(row2, "position") >= 0 || !dictionary2.TryGetValue(DbStore.Long(row2, "inventoryitemid", 0L), out var value2))
					{
						continue;
					}
					long key = DbStore.Long(row2, "characterid", 0L);
					if (!dictionary3.ContainsKey(key))
					{
						dictionary3[key] = 0.0;
					}
					foreach (DbRow item8 in value2)
					{
						foreach (var item9 in list5)
						{
							dictionary3[key] += DbStore.Dbl(item8, item9.Item1) * item9.Item2;
						}
					}
				}
				foreach (DbRow row3 in db.Table("characters").rows)
				{
					if (DbStore.Int(row3, "gm") <= 0 && dictionary3.TryGetValue(DbStore.Long(row3, "id", 0L), out var value3))
					{
						rows5.Add(DbStore.Long(row3, "id", 0L), DbStore.Str(row3, "name"), DbStore.Long(row3, "job", 0L), value3);
					}
				}
				rows5.values.Sort((Dictionary<string, object> a, Dictionary<string, object> b) => Convert.ToDouble(b["totalCombatPower"]).CompareTo(Convert.ToDouble(a["totalCombatPower"])));
				if (rows5.values.Count > 100)
				{
					rows5.values.RemoveRange(100, rows5.values.Count - 100);
				}
				return rows5;
			}
			if (text == "select c.name, ev.extend_value as profit from extend_value ev join characters c on c.id = ev.extend_id where ev.extend_type = 21 and ev.extend_name = 'stock_profit' order by profit + 0 desc limit 100")
			{
				Rows rows6 = new Rows("name", "profit");
				foreach (DbRow row4 in db.Table("extend_value").rows)
				{
					if (DbStore.Int(row4, "extend_type") != 21 || DbStore.Str(row4, "extend_name") != "stock_profit" || !long.TryParse(DbStore.Str(row4, "extend_id"), out var result))
					{
						continue;
					}
					foreach (DbRow row5 in db.Table("characters").rows)
					{
						if (DbStore.Long(row5, "id", 0L) == result)
						{
							rows6.Add(DbStore.Str(row5, "name"), DbStore.Str(row4, "extend_value"));
						}
					}
				}
				rows6.values.Sort((Dictionary<string, object> a, Dictionary<string, object> b) => Profit(b).CompareTo(Profit(a)));
				if (rows6.values.Count > 100)
				{
					rows6.values.RemoveRange(100, rows6.values.Count - 100);
				}
				return rows6;
			}
			throw new JsRuntimeException("Unmigrated script SQL: " + sql);
		}
		static double Profit(Dictionary<string, object> row)
		{
			if (!double.TryParse(Convert.ToString(row["profit"]), NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
			{
				return 0.0;
			}
			return result2;
		}
	}
}
