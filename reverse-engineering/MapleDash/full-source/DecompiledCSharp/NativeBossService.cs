using System;
using System.Collections.Generic;
using System.Globalization;

public sealed class NativeBossService
{
	private sealed class Growth
	{
		public string key;

		public double maximum;

		public Growth(string key, double maximum)
		{
			this.key = key;
			this.maximum = maximum;
		}
	}

	private readonly DbStore db;

	private readonly HashSet<string> recorded = new HashSet<string>();

	private readonly Dictionary<int, (long expires, double multiplier)> cache = new Dictionary<int, (long, double)>();

	public Func<DateTimeOffset> Clock = () => DateTimeOffset.Now;

	private static readonly Dictionary<int, Growth> Growths = new Dictionary<int, Growth>
	{
		{
			8500002,
			new Growth("papulatus", 1.0)
		},
		{
			9420544,
			new Growth("scarga", 1.2)
		},
		{
			9600025,
			new Growth("yaoseng", 1.4)
		},
		{
			9420522,
			new Growth("krexel", 1.6)
		},
		{
			8800002,
			new Growth("zakum", 1.8)
		},
		{
			9400300,
			new Growth("showa", 2.4)
		},
		{
			8810018,
			new Growth("horntail", 2.6)
		},
		{
			8820001,
			new Growth("pinkbean", 2.8)
		},
		{
			9400265,
			new Growth("tokyo_vergamot", 3.0)
		},
		{
			9400270,
			new Growth("tokyo_dunas", 3.2)
		},
		{
			9400273,
			new Growth("tokyo_nibergen", 3.4)
		},
		{
			9400266,
			new Growth("tokyo_nux", 3.6)
		},
		{
			9400294,
			new Growth("tokyo_dunas2", 3.8)
		},
		{
			9400289,
			new Growth("tokyo_aufheben", 4.0)
		},
		{
			8840000,
			new Growth("vonleon", 4.2)
		}
	};

	private static readonly Dictionary<int, string> GrowthNames = new Dictionary<int, string>
	{
		{ 8500002, "帕普拉图斯" },
		{ 9420544, "狮熊双王" },
		{ 9600025, "武林妖僧" },
		{ 9420522, "克雷塞尔" },
		{ 8800002, "扎昆" },
		{ 9400300, "昭和大头老板" },
		{ 8810018, "暗黑龙王" },
		{ 8820001, "品克缤" },
		{ 9400265, "贝尔加莫特" },
		{ 9400270, "都纳斯" },
		{ 9400273, "尼贝隆" },
		{ 9400266, "努克斯" },
		{ 9400294, "再生都纳斯" },
		{ 9400289, "欧碧拉" },
		{ 8840000, "狮子王" }
	};

	private static readonly Dictionary<string, int> Entries = new Dictionary<string, int>
	{
		{ "ZAKUM", 3 },
		{ "CHAOS_ZAKUM", 5 },
		{ "HORNTAIL", 3 },
		{ "CHAOS_HORNTAIL", 5 },
		{ "CYGNUS", 1 },
		{ "AKAYRUM", 1 },
		{ "LUCID", 1 },
		{ "PINKBEAN", 3 },
		{ "SCARGA", 1 },
		{ "PAPULATUS", 2 }
	};

	public NativeBossService(DbStore db)
	{
		this.db = db;
	}

	public BossResponse Handle(BossRequest r)
	{
		lock (db)
		{
			using (db.DeferSaves())
			{
				CharRecord character = CharactersDao.FindById(db, r.characterId);
				if (character == null)
				{
					return new BossResponse
					{
						ok = false,
						error = "角色不存在"
					};
				}
				BossResponse bossResponse = new BossResponse();
				DbRow r2 = db.Table("world_prop").Find((DbRow x) => DbStore.Int(x, "id") == character.world);
				bossResponse.dropRate = DbStore.Dbl(r2, "boss_drop_rate", 1.0);
				if (r.operation == BossOperation.RecordKill && Growths.TryGetValue(r.mobId, out var value))
				{
					if (string.IsNullOrEmpty(r.encounter))
					{
						return new BossResponse
						{
							ok = false,
							error = "缺少击杀会话"
						};
					}
					string item = r.characterId + ":" + r.encounter;
					if (recorded.Add(item))
					{
						int num = KillCount(r.characterId, value.key);
						new NativeScriptValues(db).Handle(new ScriptValueRequest
						{
							characterId = r.characterId,
							write = true,
							name = "boss_growth_kill_" + value.key,
							value = ((num == int.MaxValue) ? num : (num + 1)).ToString(CultureInfo.InvariantCulture)
						});
						cache.Remove(r.characterId);
						int num2 = Math.Min(10, (num == int.MaxValue) ? num : (num + 1));
						bossResponse.message = "[BOSS记录] 击败" + GrowthNames[r.mobId] + "，进度 " + num2 + "/10，BOSS伤害 +" + ((double)num2 / 10.0 * value.maximum).ToString("F1", CultureInfo.InvariantCulture) + "% / +" + value.maximum.ToString("F1", CultureInfo.InvariantCulture) + "%。";
						db.Save();
					}
				}
				if ((r.operation == BossOperation.CheckEntry || r.operation == BossOperation.Enter) && GameConfigDao.GetBool(db, "use_enable_daily_expeditions") && Entries.TryGetValue(r.boss ?? "", out var value2))
				{
					DateTimeOffset dateTimeOffset = Clock();
					long start = new DateTimeOffset(dateTimeOffset.Date, dateTimeOffset.Offset).ToUnixTimeMilliseconds();
					DbTable dbTable = db.Table("bosslog_daily");
					dbTable.Delete((DbRow x) => DbStore.Long(x, "attempttime", 0L) < start);
					int count = dbTable.Select((DbRow x) => DbStore.Int(x, "characterid") == r.characterId && DbStore.Str(x, "bosstype") == r.boss).Count;
					if (r.channel < 0 || count >= value2)
					{
						bossResponse.ok = false;
						bossResponse.error = "已达到该 Boss 的挑战次数限制";
					}
					else if (r.operation == BossOperation.Enter)
					{
						dbTable.Insert(new DbRow
						{
							["characterid"] = (long)r.characterId,
							["bosstype"] = r.boss,
							["attempttime"] = Clock().ToUnixTimeMilliseconds()
						});
						db.Save();
					}
				}
				FillGrowth(r.characterId, bossResponse);
				return bossResponse;
			}
		}
	}

	public static double Bonus(int quests, double bossPercent, int cards)
	{
		return Math.Min(100.0, Math.Min(1.0, (double)Math.Max(0, quests) / 1000.0) * 30.0 + Math.Min(40.0, Math.Max(0.0, bossPercent)) + Math.Min(1.0, (double)Math.Max(0, cards) / 300.0) * 30.0);
	}

	private int KillCount(int cid, string key)
	{
		string value = new NativeScriptValues(db).Handle(new ScriptValueRequest
		{
			characterId = cid,
			name = "boss_growth_kill_" + key
		}).value;
		if (value != null)
		{
			if (!int.TryParse(value, out var result))
			{
				return 0;
			}
			return Math.Max(0, result);
		}
		DbRow dbRow = db.Table("boss_growth").Find((DbRow x) => DbStore.Int(x, "characterid") == cid && DbStore.Str(x, "boss") == key);
		int result2 = (int)Math.Min(2147483647L, Math.Max(0L, DbStore.Long(dbRow, "kills", 0L)));
		if (dbRow != null)
		{
			new NativeScriptValues(db).Handle(new ScriptValueRequest
			{
				characterId = cid,
				write = true,
				name = "boss_growth_kill_" + key,
				value = result2.ToString(CultureInfo.InvariantCulture)
			});
		}
		return result2;
	}

	public void Invalidate(int cid)
	{
		cache.Remove(cid);
	}

	private void FillGrowth(int cid, BossResponse result)
	{
		int count = (from x in db.Table("queststatus")
			select DbStore.Int(x, "characterid") == cid && DbStore.Int(x, "status") == 2).Count;
		int count2 = (from x in db.Table("monsterbook")
			select DbStore.Int(x, "charid") == cid).Count;
		long num = Clock().ToUnixTimeMilliseconds();
		double num2;
		if (cache.TryGetValue(cid, out (long, double) value) && value.Item1 > num)
		{
			num2 = value.Item2;
		}
		else
		{
			num2 = 0.0;
			foreach (Growth value2 in Growths.Values)
			{
				num2 += Math.Min(1.0, (double)KillCount(cid, value2.key) / 10.0) * value2.maximum;
			}
			cache[cid] = (num + 300000, num2);
		}
		result.questCount = count;
		result.monsterCardCount = count2;
		result.questBonusPercent = Math.Min(1.0, (double)count / 1000.0) * 30.0;
		result.monsterCardBonusPercent = Math.Min(1.0, (double)count2 / 300.0) * 30.0;
		result.bossBonusPercent = Math.Min(40.0, Math.Max(0.0, num2));
		result.bonusPercent = Bonus(count, num2, count2);
		result.multiplier = 1.0 + result.bonusPercent / 100.0;
	}

	public void DeleteCharacter(int cid)
	{
		db.Table("boss_growth").Delete((DbRow x) => DbStore.Int(x, "characterid") == cid);
		db.Table("bosslog_daily").Delete((DbRow x) => DbStore.Int(x, "characterid") == cid);
		cache.Remove(cid);
	}
}
