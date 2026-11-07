using System;
using System.Collections.Generic;
using System.Globalization;
using MapleDash.Js;

public sealed class NativeScriptService
{
	private sealed class Session
	{
		public int characterId;

		public JsEngine engine;
	}

	private readonly Dictionary<int, Session> sessions = new Dictionary<int, Session>();

	private int sequence;

	private readonly Dictionary<(int character, int quest), string> questCustomData = new Dictionary<(int, int), string>();

	private readonly DbStore db;

	private readonly Dictionary<(int world, int channel, int key), int> storedVars = new Dictionary<(int, int, int), int>();

	private static readonly HashSet<string> LocationTypes = new HashSet<string>(StringComparer.Ordinal)
	{
		"FREE_MARKET", "WORLDTOUR", "FLORINA", "INTRO", "SUNDAY_MARKET", "MIRROR", "EVENT", "BOSSPQ", "HAPPYVILLE", "MONSTER_CARNIVAL",
		"DEVELOPER", "JAIL"
	};

	private static readonly Random gachaRandom = new Random();

	private Action enterMTS;

	public NativeHuntTasks Hunts { get; }

	public int ActiveCount => sessions.Count;

	public Action EnterMTS
	{
		get
		{
			return DefaultEnterMTS;
		}
		set
		{
			enterMTS = value;
		}
	}

	public NativeScriptService(DbStore db = null)
	{
		this.db = db;
		Hunts = new NativeHuntTasks(() => PlatformRes.LoadText("Scripts/hunt_tasks"));
	}

	public ScriptResponse Handle(ScriptRequest request)
	{
		if (request.operation == ScriptOperation.GetRebirthSettings)
		{
			ScriptResponse scriptResponse = new ScriptResponse();
			scriptResponse.value = new JsArray(new JsValue[2]
			{
				JsBoolean.Of(GameConfigDao.GetBool(db, "use_rebirth_system")),
				new JsNumber(Math.Max(1, GameConfigDao.GetInt(db, "rebirth_level")))
			});
			return scriptResponse;
		}
		if (request.operation == ScriptOperation.GainFame || request.operation == ScriptOperation.AwardEventQuestPoints)
		{
			CharRecord charRecord = ((db == null) ? null : CharactersDao.FindById(db, request.characterId));
			if (charRecord == null)
			{
				throw new JsRuntimeException("Script character does not exist");
			}
			int num = request.variableValue;
			if (request.operation == ScriptOperation.AwardEventQuestPoints)
			{
				int num2 = GameConfigDao.GetInt(db, "quest_point_requirement");
				int num3 = GameConfigDao.GetInt(db, "quest_point_per_event_clear");
				num = 0;
				if (num2 > 0 && num3 > 0)
				{
					charRecord.fquest += num3;
					num = charRecord.fquest / num2;
					charRecord.fquest %= num2;
				}
			}
			charRecord.fame = Math.Max(-30000, Math.Min(30000, charRecord.fame + num));
			CharactersDao.Save(db, charRecord);
			db.Save();
			ScriptResponse scriptResponse = new ScriptResponse();
			scriptResponse.value = new JsArray(new JsValue[2]
			{
				new JsNumber(charRecord.fame),
				new JsNumber(charRecord.fquest)
			});
			return scriptResponse;
		}
		if (request.operation == ScriptOperation.GetMaxClassLevel)
		{
			CharRecord charRecord2 = ((db == null) ? null : CharactersDao.FindById(db, request.characterId));
			if (charRecord2 == null)
			{
				throw new JsRuntimeException("Script character does not exist");
			}
			int num4 = GameConfigDao.GetInt(db, (charRecord2.job / 1000 == 1) ? "qst_max_level" : "mxj_max_level");
			return new ScriptResponse
			{
				value = new JsNumber((num4 > 0) ? num4 : 255)
			};
		}
		if (request.operation == ScriptOperation.GetQuestCustomData || request.operation == ScriptOperation.SetQuestCustomData)
		{
			if (db == null || CharactersDao.FindById(db, request.characterId) == null)
			{
				throw new JsRuntimeException("Script character does not exist");
			}
			(int, int) key = (request.characterId, request.questId);
			if (request.operation == ScriptOperation.SetQuestCustomData)
			{
				questCustomData[key] = request.customData;
			}
			string value;
			return new ScriptResponse
			{
				value = ((questCustomData.TryGetValue(key, out value) && value != null) ? ((JsValue)new JsString(value)) : ((JsValue)JsNull.Value))
			};
		}
		if (request.operation == ScriptOperation.GetStoredVar || request.operation == ScriptOperation.SetStoredVar)
		{
			if (db == null || CharactersDao.FindById(db, request.characterId) == null)
			{
				throw new JsRuntimeException("Script character does not exist");
			}
			(int, int, int) key2 = (request.world, request.channel, request.variableKey);
			if (request.operation == ScriptOperation.SetStoredVar)
			{
				storedVars[key2] = request.variableValue;
			}
			int value2;
			return new ScriptResponse
			{
				value = new JsNumber(storedVars.TryGetValue(key2, out value2) ? value2 : 0)
			};
		}
		if (request.operation == ScriptOperation.GachaDraw)
		{
			if (db == null || CharactersDao.FindById(db, request.characterId) == null)
			{
				throw new JsRuntimeException("Script character does not exist");
			}
			return new ScriptResponse
			{
				value = GachaDraw(request.gachaponId)
			};
		}
		if (request.operation >= ScriptOperation.SaveLocation)
		{
			if (db == null || CharactersDao.FindById(db, request.characterId) == null)
			{
				throw new JsRuntimeException("Script character does not exist");
			}
			if (!LocationTypes.Contains(request.location ?? ""))
			{
				throw new JsRuntimeException("Invalid SavedLocationType: " + request.location);
			}
			DbRow dbRow = SavedLocationsDao.Find(db, request.characterId, request.location);
			int num5 = ((dbRow == null) ? (-1) : DbStore.Int(dbRow, "map"));
			if (request.operation == ScriptOperation.SaveLocation)
			{
				SavedLocationsDao.Save(db, request.characterId, request.location, request.mapId, request.portalId);
			}
			if (request.operation == ScriptOperation.GetLocation || request.operation == ScriptOperation.ClearLocation)
			{
				db.Table("savedlocations").Delete((DbRow r) => DbStore.Int(r, "characterid") == request.characterId && DbStore.Str(r, "locationtype") == request.location);
			}
			if (request.operation != ScriptOperation.PeekLocation)
			{
				db.Save();
			}
			return new ScriptResponse
			{
				value = new JsNumber(num5)
			};
		}
		if (request.operation == ScriptOperation.Close)
		{
			if (sessions.TryGetValue(request.sessionId, out var value3) && value3.characterId == request.characterId)
			{
				sessions.Remove(request.sessionId);
			}
			return new ScriptResponse();
		}
		if (request.operation == ScriptOperation.Open)
		{
			JsEngine engine = new JsEngine(request.source, request.name);
			if (request.bindings != null)
			{
				foreach (KeyValuePair<string, JsValue> binding in request.bindings)
				{
					if (binding.Key != "Java")
					{
						engine.Bind(binding.Key, binding.Value);
					}
				}
			}
			engine.Interp.ResolveHostType = (string name) =>
			{
				JsValue jsValue = ResolveHostType(name);
				if (jsValue != null)
				{
					return jsValue;
				}
				JsValue value5;
				return (request.bindings != null && request.bindings.TryGetValue("Java", out value5)) ? engine.Interp.Call(engine.Interp.GetMember(value5, "type"), value5, new JsValue[1]
				{
					new JsString(name)
				}) : null;
			};
			engine.Run();
			int num6 = ++sequence;
			sessions.Add(num6, new Session
			{
				characterId = request.characterId,
				engine = engine
			});
			return new ScriptResponse
			{
				sessionId = num6,
				hasStart = engine.HasFunction("start"),
				hasAction = engine.HasFunction("action"),
				hasEnd = engine.HasFunction("end")
			};
		}
		if (!sessions.TryGetValue(request.sessionId, out var value4) || value4.characterId != request.characterId)
		{
			throw new JsRuntimeException("Script session is closed or belongs to another character");
		}
		try
		{
			return new ScriptResponse
			{
				sessionId = request.sessionId,
				value = value4.engine.Call(request.function, request.arguments ?? Array.Empty<JsValue>())
			};
		}
		catch
		{
			sessions.Remove(request.sessionId);
			throw;
		}
	}

	private List<DbRow> ActiveGachaPools(int gachaponId)
	{
		DateTime now = DateTime.Now;
		List<DbRow> list = new List<DbRow>();
		if (db.HasTable("gachapon_reward_pool"))
		{
			foreach (DbRow row in db.Table("gachapon_reward_pool").rows)
			{
				if (DbStore.Int(row, "gachapon_id") == gachaponId || DbStore.Int(row, "is_public") == 1)
				{
					string text = DbStore.Str(row, "start_time");
					string text2 = DbStore.Str(row, "end_time");
					if ((string.IsNullOrEmpty(text) || !DateTime.TryParse(text, out var result) || !(now < result)) && (string.IsNullOrEmpty(text2) || !DateTime.TryParse(text2, out var result2) || !(now > result2)))
					{
						list.Add(row);
					}
				}
			}
		}
		list.Sort((DbRow a, DbRow b) => DbStore.Int(a, "id").CompareTo(DbStore.Int(b, "id")));
		return list;
	}

	private JsValue GachaDraw(int gachaponId)
	{
		List<DbRow> list = ActiveGachaPools(gachaponId);
		if (list.Count == 0)
		{
			return new JsArray();
		}
		int num = 0;
		int num2 = 0;
		foreach (DbRow item in list)
		{
			num += DbStore.Int(item, "prob");
			num2 += DbStore.Int(item, "weight");
		}
		int num3 = 1000000 - num * 100;
		int num4 = gachaRandom.Next(1000000);
		DbRow dbRow = null;
		int num5 = 0;
		foreach (DbRow item2 in list)
		{
			int num6 = ((DbStore.Int(item2, "is_public") != 0) ? (DbStore.Int(item2, "prob") * 100) : ((num2 > 0) ? MathfRound((float)(num3 * DbStore.Int(item2, "weight")) / (float)num2) : 0));
			num5 += num6;
			if (num5 > num4)
			{
				dbRow = item2;
				break;
			}
		}
		if (dbRow == null)
		{
			dbRow = list[0];
		}
		List<DbRow> list2 = new List<DbRow>();
		if (db.HasTable("gachapon_reward"))
		{
			foreach (DbRow row in db.Table("gachapon_reward").rows)
			{
				if (DbStore.Int(row, "pool_id") == DbStore.Int(dbRow, "id"))
				{
					list2.Add(row);
				}
			}
		}
		if (list2.Count == 0)
		{
			return new JsArray();
		}
		DbRow r = list2[gachaRandom.Next(list2.Count)];
		return new JsArray(new JsValue[2]
		{
			new JsNumber(DbStore.Int(r, "item_id")),
			new JsNumber(DbStore.Int(r, "quantity", 1))
		});
	}

	private static int MathfRound(float v)
	{
		return (int)MathF.Round(v, MidpointRounding.AwayFromZero);
	}

	public JsValue ResolveHostType(string name)
	{
		switch (name)
		{
		case "org.gms.server.timer.HuntTaskAutoPublisher":
			return Hunts.Type();
		case "org.gms.client.Stat":
			return NativeScriptStat.Type();
		case "org.gms.util.DatabaseConnection":
			if (db != null)
			{
				return NativeScriptDatabase.Type(db);
			}
			break;
		}
		switch (name)
		{
		case "org.gms.client.inventory.Item":
			return NativeScriptItem.Constructor();
		case "org.gms.server.gachapon.Gachapon":
			return GachaponClass();
		case "org.gms.manager.ServerManager":
			if (db != null)
			{
				return ServerManagerClass();
			}
			break;
		}
		if (name == "org.gms.config.GameConfig" && db != null)
		{
			return ConfigClass();
		}
		if (name == "org.gms.constants.string.ExtendType")
		{
			return ExtendTypeClass();
		}
		if (name == "org.gms.util.ExtendUtil" && db != null)
		{
			return ExtendClass();
		}
		if (name == "org.gms.util.packets.Fishing")
		{
			return FishingClass();
		}
		if (name == "org.gms.net.server.channel.handlers.EnterMTSHandler")
		{
			return EnterMTSClass();
		}
		return null;
	}

	private void DefaultEnterMTS()
	{
		throw new JsRuntimeException("Unmigrated MTS: ITC window and trading service");
	}

	private JsValue EnterMTSClass()
	{
		JsHostObject jsHostObject = new JsHostObject(null);
		jsHostObject.Set("enterMTS", JsFunction.FromNative("enterMTS", (JsCall c) =>
		{
			EnterMTS();
			return JsNull.Value;
		}));
		return jsHostObject;
	}

	public int TransportationTime(int world, int travelTime)
	{
		float num = Convert.ToSingle((db?.Table("world_prop").Find((DbRow value) => DbStore.Int(value, "id") == world) ?? throw new JsRuntimeException("Missing event world: " + world))["travel_rate"], CultureInfo.InvariantCulture);
		float num2 = (float)travelTime * num;
		if (float.IsNaN(num2))
		{
			return 0;
		}
		if ((double)num2 > 2147483647.0)
		{
			return int.MaxValue;
		}
		if ((double)num2 < -2147483648.0)
		{
			return int.MinValue;
		}
		return (int)num2;
	}

	private static JsValue GachaponClass()
	{
		JsHostObject jsHostObject = new JsHostObject(null);
		JsHostObject jsHostObject2 = new JsHostObject(null);
		string[] names = new string[10] { "射手村", "魔法森林", "勇士部落", "废弃都市", "林中之城", "古代神社", "昭和村更衣室(男)", "昭和村更衣室(女)", "新叶城", "诺特勒斯" };
		int[] ids = new int[10] { 9100100, 9100101, 9100102, 9100103, 9100104, 9100105, 9100106, 9100107, 9100109, 9100117 };
		jsHostObject2.Set("getLootNames", JsFunction.FromNative("getLootNames", (JsCall c) => new JsArray(Array.ConvertAll(names, (Converter<string, JsValue>)((string n) => new JsString(n))))));
		jsHostObject2.Set("getLootIds", JsFunction.FromNative("getLootIds", (JsCall c) => new JsArray(Array.ConvertAll(ids, (Converter<int, JsValue>)((int n) => new JsNumber(n))))));
		jsHostObject.Set("GachaponType", jsHostObject2);
		return jsHostObject;
	}

	private JsValue ServerManagerClass()
	{
		JsHostObject jsHostObject = new JsHostObject(null);
		JsHostObject context = new JsHostObject(null);
		jsHostObject.Set("getApplicationContext", JsFunction.FromNative("getApplicationContext", (JsCall c) => context));
		context.Set("getBean", JsFunction.FromNative("getBean", (JsCall c) =>
		{
			string text = JsConvert.ToString(c.Arg(0));
			if (text != "gachaponService")
			{
				throw new JsRuntimeException("Unmigrated server bean: " + text);
			}
			JsHostObject jsHostObject2 = new JsHostObject(null);
			jsHostObject2.Set("getRewardsByNpcId", JsFunction.FromNative("getRewardsByNpcId", (JsCall args) =>
			{
				JsArray jsArray = new JsArray();
				foreach (DbRow item in ActiveGachaPools(JsConvert.ToInt32(args.Arg(0))))
				{
					foreach (DbRow row in db.Table("gachapon_reward").rows)
					{
						if (DbStore.Int(row, "pool_id") == DbStore.Int(item, "id"))
						{
							JsHostObject jsHostObject3 = new JsHostObject(row);
							jsHostObject3.Set("getItemId", JsFunction.FromNative("getItemId", (JsCall _) => new JsNumber(DbStore.Int(row, "item_id"))));
							jsHostObject3.Set("getQuantity", JsFunction.FromNative("getQuantity", (JsCall _) => new JsNumber(DbStore.Int(row, "quantity"))));
							jsArray.Items.Add(jsHostObject3);
						}
					}
				}
				return jsArray;
			}));
			return jsHostObject2;
		}));
		return jsHostObject;
	}

	private static JsValue ExtendTypeClass()
	{
		JsHostObject jsHostObject = new JsHostObject(null);
		Dictionary<string, JsHostObject> values = new Dictionary<string, JsHostObject>(StringComparer.Ordinal);
		string[] array = new string[7] { "ACCOUNT_EXTEND", "ACCOUNT_EXTEND_DAILY", "ACCOUNT_EXTEND_WEEKLY", "CHARACTER_EXTEND", "CHARACTER_EXTEND_DAILY", "CHARACTER_EXTEND_WEEKLY", "UNSUPPORTED" };
		string[] array2 = new string[7] { "11", "12", "13", "21", "22", "23", "99" };
		for (int i = 0; i < array.Length; i++)
		{
			string name = array[i];
			string code = array2[i];
			JsHostObject jsHostObject2 = new JsHostObject(code);
			jsHostObject2.Set("getType", JsFunction.FromNative("getType", (JsCall _) => new JsString(code)));
			jsHostObject2.Set("name", JsFunction.FromNative("name", (JsCall _) => new JsString(name)));
			jsHostObject2.Set("toString", JsFunction.FromNative("toString", (JsCall _) => new JsString(name)));
			values[code] = jsHostObject2;
			jsHostObject.Set(name, jsHostObject2);
		}
		jsHostObject.Set("getExtendType", JsFunction.FromNative("getExtendType", (JsCall c) => (!values.TryGetValue(JsConvert.ToString(c.Arg(0)), out var value)) ? values["99"] : value));
		jsHostObject.Set("isAccount", JsFunction.FromNative("isAccount", (JsCall c) =>
		{
			string text = JsConvert.ToString(c.Arg(0));
			return JsBoolean.Of(text == "11" || text == "12" || text == "13");
		}));
		jsHostObject.Set("isCharacter", JsFunction.FromNative("isCharacter", (JsCall c) =>
		{
			string text = JsConvert.ToString(c.Arg(0));
			return JsBoolean.Of(text == "21" || text == "22" || text == "23");
		}));
		return jsHostObject;
	}

	private JsValue ExtendClass()
	{
		JsHostObject jsHostObject = new JsHostObject(null);
		jsHostObject.Set("getExtendValue", JsFunction.FromNative("getExtendValue", (JsCall call) =>
		{
			DbRow row = Find(call);
			if (row != null)
			{
				JsHostObject jsHostObject2 = new JsHostObject(row);
				jsHostObject2.Set("getExtendValue", JsFunction.FromNative("getExtendValue", (JsCall _) => new JsString(DbStore.Str(row, "extend_value"))));
				return jsHostObject2;
			}
			return JsNull.Value;
		}));
		jsHostObject.Set("saveOrUpdateExtendValue", JsFunction.FromNative("saveOrUpdateExtendValue", (JsCall call) =>
		{
			DbRow dbRow = Find(call);
			long num = DateTimeOffset.Now.ToUnixTimeMilliseconds();
			if (dbRow == null)
			{
				db.Table("extend_value").Insert(new DbRow
				{
					["extend_id"] = JsConvert.ToString(call.Arg(0)),
					["extend_type"] = JsConvert.ToString(call.Arg(1)),
					["extend_name"] = JsConvert.ToString(call.Arg(2)),
					["extend_value"] = JsConvert.ToString(call.Arg(3)),
					["create_time"] = num,
					["update_time"] = 0L
				});
			}
			else
			{
				dbRow["extend_value"] = JsConvert.ToString(call.Arg(3));
				dbRow["update_time"] = num;
			}
			db.Save();
			return JsUndefined.Value;
		}));
		return jsHostObject;
		DbRow Find(JsCall call)
		{
			return db.Table("extend_value").Find((DbRow row) => DbStore.Str(row, "extend_id") == JsConvert.ToString(call.Arg(0)) && DbStore.Str(row, "extend_type") == JsConvert.ToString(call.Arg(1)) && DbStore.Str(row, "extend_name") == JsConvert.ToString(call.Arg(2)));
		}
	}

	private JsValue ConfigClass()
	{
		JsHostObject jsHostObject = new JsHostObject(null);
		string[] array = new string[18]
		{
			"get", "getObject", "getInteger", "getIntValue", "getLong", "getLongValue", "getShort", "getShortValue", "getByte", "getByteValue",
			"getFloat", "getFloatValue", "getDouble", "getDoubleValue", "getBoolean", "getBooleanValue", "getString", "getStringValue"
		};
		foreach (string method in array)
		{
			jsHostObject.Set(method, JsFunction.FromNative(method, (JsCall call) =>
			{
				if (call.ArgCount != 1)
				{
					throw new JsRuntimeException("Unmigrated GameConfig overload: " + method + "/" + call.ArgCount);
				}
				string key = JsConvert.ToString(call.Arg(0));
				DbRow dbRow = db.Table("game_config").Find((DbRow r) => string.Equals(DbStore.Str(r, "config_code"), key, StringComparison.OrdinalIgnoreCase));
				if (dbRow == null)
				{
					return JsNull.Value;
				}
				string text = DbStore.Str(dbRow, "config_value");
				if (method == "getString" || method == "getStringValue")
				{
					return new JsString(text);
				}
				if (method == "getBoolean" || method == "getBooleanValue")
				{
					return JsBoolean.Of(text == "1" || text.Equals("true", StringComparison.OrdinalIgnoreCase));
				}
				if (method != "get" && method != "getObject")
				{
					return new JsNumber(JsConvert.StringToNumber(text));
				}
				string text2 = DbStore.Str(dbRow, "config_clazz");
				switch (text2)
				{
				case "java.lang.String":
					return new JsString(text);
				case "java.lang.Boolean":
					return JsBoolean.Of(text == "1" || text.Equals("true", StringComparison.OrdinalIgnoreCase));
				case "java.lang.Integer":
				case "java.lang.Long":
				case "java.lang.Double":
				case "java.lang.Float":
				case "java.lang.Short":
				case "java.lang.Byte":
					return new JsNumber(JsConvert.StringToNumber(text));
				default:
					if (text.TrimStart().StartsWith("{", StringComparison.Ordinal) || text.TrimStart().StartsWith("[", StringComparison.Ordinal))
					{
						return FromConfig(MiniJsonDb.Parse(text));
					}
					throw new JsRuntimeException("Unmigrated GameConfig value type: " + text2);
				}
			}));
		}
		jsHostObject.Set("getServerInt", JsFunction.FromNative("getServerInt", (JsCall call) => new JsNumber(JsConvert.StringToNumber(Scoped("server", call, asText: false)))));
		jsHostObject.Set("getServerString", JsFunction.FromNative("getServerString", (JsCall call) => new JsString(Scoped("server", call, asText: true))));
		jsHostObject.Set("getServerBoolean", JsFunction.FromNative("getServerBoolean", (JsCall call) =>
		{
			string text = Scoped("server", call, asText: true);
			return JsBoolean.Of(text != null && (text == "1" || text.Equals("true", StringComparison.OrdinalIgnoreCase)));
		}));
		return jsHostObject;
	}

	private JsValue FishingClass()
	{
		JsHostObject jsHostObject = new JsHostObject(null);
		jsHostObject.Set("COMMON_BAIT_BASE_RATE", new JsNumber(0.3));
		jsHostObject.Set("ADVANCED_BAIT_BASE_RATE", new JsNumber(0.6));
		jsHostObject.Set("MAX_SUCCESS_LEVEL_RATE", new JsNumber(0.3));
		jsHostObject.Set("ITEM_RATE_MYTHIC", new JsNumber(0.0001));
		jsHostObject.Set("ITEM_RATE_LEGENDARY", new JsNumber(0.0002));
		jsHostObject.Set("ITEM_RATE_SUPER_RARE", new JsNumber(0.001));
		jsHostObject.Set("ITEM_RATE_RARE", new JsNumber(0.01));
		jsHostObject.Set("ITEM_RATE_UNCOMMON", new JsNumber(0.1));
		jsHostObject.Set("ITEM_RATE_COMMON", new JsNumber(0.8887));
		JsHostObject instance = new JsHostObject(null);
		instance.Set("getItemsByType", JsFunction.FromNative("getItemsByType", (JsCall call) => new JsArray(FishingItems(JsConvert.ToInt32(call.Arg(0))).ConvertAll((Converter<int, JsValue>)((int id) => new JsNumber(id))))));
		instance.Set("refreshItemCache", JsFunction.FromNative("refreshItemCache", (JsCall _) => JsUndefined.Value));
		jsHostObject.Set("getInstance", JsFunction.FromNative("getInstance", (JsCall _) => instance));
		return jsHostObject;
	}

	private List<int> FishingItems(int itemType)
	{
		List<int> list = new List<int>();
		if (db == null)
		{
			return list;
		}
		foreach (DbRow row in db.Table("fishing_items").rows)
		{
			if (DbStore.Int(row, "item_type") == itemType)
			{
				list.Add(DbStore.Int(row, "item_id"));
			}
		}
		return list;
	}

	private string Scoped(string scope, JsCall call, bool asText)
	{
		string key = JsConvert.ToString(call.Arg(0));
		DbRow dbRow = db.Table("game_config").Find((DbRow r) => string.Equals(DbStore.Str(r, "config_type"), scope, StringComparison.OrdinalIgnoreCase) && string.Equals(DbStore.Str(r, "config_code"), key, StringComparison.OrdinalIgnoreCase));
		if (dbRow != null)
		{
			return DbStore.Str(dbRow, "config_value");
		}
		if (!asText)
		{
			return "0";
		}
		return "";
	}

	private static JsValue FromConfig(object value)
	{
		if (value == null)
		{
			return JsNull.Value;
		}
		if (value is bool v)
		{
			return JsBoolean.Of(v);
		}
		if (value is string v2)
		{
			return new JsString(v2);
		}
		if (value is Dictionary<string, object> dictionary)
		{
			JsObject jsObject = new JsObject();
			{
				foreach (KeyValuePair<string, object> item in dictionary)
				{
					jsObject.Set(item.Key, FromConfig(item.Value));
				}
				return jsObject;
			}
		}
		if (value is List<object> list)
		{
			JsArray jsArray = new JsArray();
			{
				foreach (object item2 in list)
				{
					jsArray.Items.Add(FromConfig(item2));
				}
				return jsArray;
			}
		}
		return new JsNumber(Convert.ToDouble(value, CultureInfo.InvariantCulture));
	}

	public void EndSession(int characterId)
	{
		List<(int, int)> list = new List<(int, int)>();
		foreach (var key in questCustomData.Keys)
		{
			if (key.character == characterId)
			{
				list.Add(key);
			}
		}
		foreach (var item in list)
		{
			questCustomData.Remove(item);
		}
		List<int> list2 = new List<int>();
		foreach (KeyValuePair<int, Session> session in sessions)
		{
			if (session.Value.characterId == characterId)
			{
				list2.Add(session.Key);
			}
		}
		foreach (int item2 in list2)
		{
			sessions.Remove(item2);
		}
	}
}
