using System;
using System.Collections.Generic;
using MapleDash.Js;

public sealed class ScriptMonster
{
	public readonly int Id;

	public double X;

	public double Y;

	public int Fh;

	public long DamageBlockedUntil;

	public long HpOverride = -1L;

	public (long hp, long exp)? DifficultyStats;

	public bool PositionSet;

	public const string TypeName = "org.gms.server.life.LifeFactory";

	public bool FhSet;

	private static readonly int[] MobHpTable = new int[199]
	{
		0, 15, 20, 25, 35, 50, 65, 80, 95, 110,
		125, 150, 175, 200, 225, 250, 275, 300, 325, 350,
		375, 405, 435, 465, 495, 525, 580, 650, 720, 790,
		900, 990, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800,
		1900, 2000, 2100, 2200, 2300, 2400, 2520, 2640, 2760, 2880,
		3000, 3200, 3400, 3600, 3800, 4000, 4300, 4600, 4900, 5200,
		5500, 5900, 6300, 6700, 7100, 7500, 8000, 8500, 9000, 9500,
		10000, 11000, 12000, 13000, 14000, 15000, 17000, 19000, 21000, 23000,
		25000, 27000, 29000, 31000, 33000, 35000, 37000, 39000, 41000, 43000,
		45000, 47000, 49000, 51000, 53000, 55000, 57000, 59000, 61000, 63000,
		65000, 67000, 69000, 71000, 73000, 75000, 77000, 79000, 81000, 83000,
		85000, 89000, 91000, 93000, 95000, 97000, 99000, 101000, 103000, 105000,
		107000, 109000, 111000, 113000, 115000, 118000, 120000, 125000, 130000, 135000,
		140000, 145000, 150000, 155000, 160000, 165000, 170000, 175000, 180000, 185000,
		190000, 195000, 200000, 205000, 210000, 215000, 220000, 225000, 230000, 235000,
		240000, 250000, 260000, 270000, 280000, 290000, 300000, 310000, 320000, 330000,
		340000, 350000, 360000, 370000, 380000, 390000, 400000, 410000, 420000, 430000,
		440000, 450000, 460000, 470000, 480000, 490000, 500000, 510000, 520000, 530000,
		550000, 570000, 590000, 610000, 630000, 650000, 670000, 690000, 710000, 730000,
		750000, 770000, 790000, 810000, 830000, 850000, 870000, 890000, 910000
	};

	private static JsValue Done => JsUndefined.Value;

	public ScriptMonster(int id)
	{
		Id = id;
	}

	public JsHostObject Host()
	{
		JsHostObject host = new JsHostObject(this);
		host.Set("id", new JsNumber(Id));
		host.Missing = (string key) =>
		{
			throw new JsRuntimeException("Unmigrated monster API: " + key);
		};
		Bind(host, "getId", (JsCall _) => new JsNumber(Id));
		host.Set("setId", JsFunction.FromNative("setId", (JsCall _) => Done));
		Bind(host, "getName", (JsCall _) => new JsString(WzNames.Mob(Id)));
		host.Set("position", NpcApi.BuildPoint(0.0, 0.0));
		Bind(host, "getPosition", (JsCall _) => NpcApi.BuildPoint(X, Y));
		host.Set("setPosition", JsFunction.FromNative("setPosition", (JsCall c) =>
		{
			if (!(c.Arg(0) is JsHostObject jsHostObject))
			{
				return Done;
			}
			X = JsConvert.ToNumber(jsHostObject.Get("x"));
			Y = JsConvert.ToNumber(jsHostObject.Get("y"));
			PositionSet = true;
			host.Set("position", jsHostObject);
			return Done;
		}));
		Bind(host, "getFh", (JsCall _) => new JsNumber(Fh));
		host.Set("setFh", JsFunction.FromNative("setFh", (JsCall c) =>
		{
			Fh = JsConvert.ToInt32(c.Arg(0));
			FhSet = true;
			return Done;
		}));
		Bind(host, "isAlive", (JsCall _) => JsBoolean.Of(v: true));
		Bind(host, "getObjectId", (JsCall _) => new JsNumber(0.0));
		Bind(host, "getHp", (JsCall _) => new JsNumber((HpOverride >= 0) ? HpOverride : MaxHp()));
		Bind(host, "getMaxHp", (JsCall _) => new JsNumber(MaxHp()));
		Bind(host, "getMap", (JsCall _) => JsNull.Value);
		host.Set("setHp", JsFunction.FromNative("setHp", (JsCall c) =>
		{
			HpOverride = (long)JsConvert.ToNumber(c.Arg(0));
			return Done;
		}));
		host.Set("addHp", JsFunction.FromNative("addHp", (JsCall c) =>
		{
			long num = (long)JsConvert.ToNumber(c.Arg(0));
			HpOverride = Math.Max(0L, ((HpOverride >= 0) ? HpOverride : MaxHp()) + num);
			return Done;
		}));
		host.Set("setDamageBlockedUntil", JsFunction.FromNative("setDamageBlockedUntil", (JsCall c) =>
		{
			DamageBlockedUntil = (long)JsConvert.ToNumber(c.Arg(0));
			return Done;
		}));
		host.Set("changeDifficulty", JsFunction.FromNative("changeDifficulty", (JsCall c) =>
		{
			(long, long)? difficultyStats = ChangeableStats(Id, JsConvert.ToInt32(c.Arg(0)), JsConvert.ToBoolean(c.Arg(1)));
			if (difficultyStats.HasValue)
			{
				DifficultyStats = difficultyStats;
			}
			return Done;
		}));
		return host;
	}

	private long MaxHp()
	{
		FieldCatalog.LoadMob(Id);
		if (!FieldCatalog.Mobs.TryGetValue(Id, out var value))
		{
			return 1L;
		}
		return Math.Max(1L, NativeCombatData.Long(value.stats, "maxHP", 1L));
	}

	private static long MobHpVal(int level)
	{
		return (level < 0 || level >= MobHpTable.Length) ? int.MaxValue : MobHpTable[level];
	}

	private static long JRound(double v)
	{
		return (long)Math.Floor(v + 0.5);
	}

	public static (long hp, long exp)? ChangeableStats(int mobId, int difficulty, bool pqMob)
	{
		FieldCatalog.LoadMob(mobId);
		if (!FieldCatalog.Mobs.TryGetValue(mobId, out var value))
		{
			return null;
		}
		Dictionary<string, object> stats = value.stats;
		if (NativeCombatData.Long(stats, "changeable", 0L) <= 0)
		{
			return null;
		}
		float num = difficulty switch
		{
			6 => 7.7f, 
			5 => 5.6f, 
			4 => 3.2f, 
			3 => 2.1f, 
			2 => 1.4f, 
			_ => 1f, 
		};
		long num2 = NativeCombatData.Long(stats, "maxHP", 1L);
		long num3 = NativeCombatData.Long(stats, "exp", 0L);
		int num4 = (int)NativeCombatData.Long(stats, "level", 1L);
		int num5 = Math.Max(1, (int)((float)num4 * num));
		bool flag = NativeCombatData.Long(stats, "boss", 0L) > 0;
		double num6 = (double)num5 / (double)Math.Max(1, num4);
		double num7 = ((num3 > 0) ? ((double)num2 / (double)num3) : double.PositiveInfinity);
		double num8 = (pqMob ? 1.5 : 1.0);
		long item = Math.Max(1L, JRound(((!flag) ? ((double)MobHpVal(num5)) : ((double)num2 * num6)) * num8));
		long item2 = Math.Min(JRound(((!flag) ? ((double)MobHpVal(num5) / num7) : ((double)num3)) * num8), 2147483647L);
		return (item, item2);
	}

	private static void Bind(JsHostObject host, string name, Func<JsCall, JsValue> call)
	{
		host.Set(name, JsFunction.FromNative(name, call));
	}

	public static JsValue Resolve(string name)
	{
		if (!(name == "org.gms.server.life.LifeFactory"))
		{
			return null;
		}
		return Type();
	}

	private static JsValue Type()
	{
		JsHostObject jsHostObject = new JsHostObject(null);
		jsHostObject.Missing = (string key) =>
		{
			throw new JsRuntimeException("Unmigrated LifeFactory API: " + key);
		};
		jsHostObject.Set("getMonster", JsFunction.FromNative("getMonster", (JsCall c) => new ScriptMonster(JsConvert.ToInt32(c.Arg(0))).Host()));
		return jsHostObject;
	}
}
