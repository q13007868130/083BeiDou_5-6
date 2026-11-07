using System;
using System.Collections.Generic;
using MapleDash.Js;
using UnityEngine;

public static class LucidBossCompat
{
	private sealed class Encounter
	{
		private readonly NativeEventMap map;

		private readonly NativeEventMap.Mob boss;

		private readonly int phase;

		private readonly int bossObjectId;

		private bool active = true;

		private bool furyFailed;

		private long nextButterfly;

		private long nextDust;

		private long nextSummon;

		private long nextDragon;

		private long nextLaser;

		private long nextShoot;

		private long nextRush;

		private long nextBomb;

		private long nextStainedGlass;

		private long nextHurdleDamage;

		private long nextFury;

		private int butterflyGauge;

		private int butterflyPosition;

		private int stainedGlassIndex;

		private bool butterflyWarningIssued;

		private readonly List<KeyValuePair<long, Action>> jobs = new List<KeyValuePair<long, Action>>();

		public int Phase => phase;

		private bool PlayerAvailable
		{
			get
			{
				if (!NpcApi.ScriptAlive)
				{
					return false;
				}
				return PlayerMapMatches;
			}
		}

		private bool PlayerMapMatches => NpcApi.ScriptMapId == map.Id;

		private Vector2 PlayerPosition
		{
			get
			{
				//IL_0000: Unknown result type (might be due to invalid IL or missing references)
				return NpcApi.ScriptPosition;
			}
		}

		public Encounter(NativeEventMap map, NativeEventMap.Mob boss, int phase)
		{
			this.map = map;
			this.boss = boss;
			this.phase = phase;
			bossObjectId = boss.ObjectId;
			long num = Now();
			nextButterfly = num + 5000;
			nextDust = num + 9000;
			nextSummon = num + 7000;
			nextDragon = num + ((phase == 1) ? 20000 : 28000);
			nextLaser = num + 12000;
			nextShoot = num + 25000;
			nextRush = num + 18000;
			nextBomb = num + ((phase == 1) ? 12000 : 15000);
			nextStainedGlass = num + 10000;
			nextHurdleDamage = num + 1000;
			nextFury = num + 45000;
		}

		public void Start()
		{
			if (phase == 3)
			{
				map.BroadcastEffect("customSkill/lucid/furyVideoLayer");
				map.DropMapMessage(5, "[Lucid] Lucid has become enraged!");
				map.DropMapMessage(5, "[Lucid] The nightmare fog will close in after 45 seconds.");
			}
		}

		private void Schedule(long delay, Action action)
		{
			jobs.Add(new KeyValuePair<long, Action>(Now() + delay, action));
		}

		public void Tick(long now)
		{
			//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
			if (!active)
			{
				return;
			}
			RunJobs(now);
			if (!active)
			{
				return;
			}
			if (!IsCurrentBossAlive())
			{
				if (phase == 3 && furyFailed)
				{
					return;
				}
				if (phase == 2)
				{
					NativeEventMap.Mob mob = map.FindAliveById(8880152);
					if (mob != null)
					{
						StartPhase(map, mob, 3);
						return;
					}
				}
				if (encounters.TryGetValue(map, out var value) && value == this)
				{
					encounters.Remove(map);
				}
				StopInternal(cleanup: false);
				return;
			}
			if (phase == 3)
			{
				if (now >= nextFury)
				{
					FailFury();
				}
				return;
			}
			int hpPercent = HpPercent();
			if (now >= nextButterfly)
			{
				CreateButterflies(now, hpPercent);
			}
			if (now >= nextDust)
			{
				Vector2 position = boss.Position;
				ApplyDamage(10, new Rect(position.x - 600f, position.y - 500f, 1200f, 650f), "fairy-dust", boss);
				nextDust = now + ((phase == 1) ? 12000 : 10000);
			}
			if (now >= nextSummon)
			{
				SummonNightmares();
				nextSummon = now + ((phase == 1) ? 15000 : 18000);
			}
			if (now >= nextDragon)
			{
				map.DropMapMessage(5, "[Lucid] Lucid is preparing a powerful attack!");
				map.BroadcastEffect((phase == 1) ? "customSkill/lucid/dragonP1VideoLayer" : "customSkill/lucid/dragonP2VideoLayer");
				Schedule(6300L, () =>
				{
					if (active && IsCurrentBossAlive())
					{
						ApplyDamage(100, null, "dragon-breath", boss);
					}
				});
				nextDragon = now + ((phase == 1) ? 35000 : 38000);
			}
			if (phase == 2 && now >= nextLaser)
			{
				map.DropMapMessage(5, "[Lucid] Lucid is preparing a powerful attack!");
				map.BroadcastEffect("customSkill/lucid/laserRainVideoLayer");
				ScheduleDamage(1260L, 18, null, "laser-rain-1");
				ScheduleDamage(3000L, 18, null, "laser-rain-2");
				Schedule(3000L, () =>
				{
					//IL_002f: Unknown result type (might be due to invalid IL or missing references)
					if (active && phase == 2 && IsCurrentBossAlive() && CountMobs(8880171) < 15)
					{
						SpawnGroundMob(8880171, RandomGroundPoint(2));
					}
				});
				nextLaser = now + 30000;
			}
			if (phase == 2 && now >= nextShoot)
			{
				map.DropMapMessage(5, "[Lucid] Lucid is gathering power!");
				map.BroadcastEffect("customSkill/lucid/phantomBarrageVideoLayer");
				for (int num = 0; num < 12; num++)
				{
					ScheduleDamage(2400 + (long)num * 1000L, 10, null, "phantom-barrage");
				}
				nextShoot = now + 45000;
			}
			if (phase == 2 && now >= nextRush)
			{
				CastRush();
				nextRush = now + 20000;
			}
			if (phase == 2 && now >= nextStainedGlass)
			{
				BreakStainedGlass();
				nextStainedGlass = now + 10000;
			}
			if (now >= nextHurdleDamage)
			{
				ApplyHurdleDamage();
				nextHurdleDamage = now + 1000;
			}
			if (now >= nextBomb)
			{
				CastContagiousBomb();
				nextBomb = now + 25000;
			}
		}

		private void RunJobs(long now)
		{
			if (jobs.Count == 0)
			{
				return;
			}
			for (int num = jobs.Count - 1; num >= 0; num--)
			{
				if (jobs[num].Key <= now)
				{
					Action value = jobs[num].Value;
					jobs.RemoveAt(num);
					if (active)
					{
						value();
					}
				}
			}
		}

		private bool IsCurrentBossAlive()
		{
			if (boss.Alive)
			{
				return map.FindByOid(bossObjectId) == boss;
			}
			return false;
		}

		private int HpPercent()
		{
			long num = Math.Max(1L, boss.MaxHp);
			long val = (long)Math.Ceiling((double)boss.CurrentHp * 100.0 / (double)num);
			return (int)Math.Max(1L, Math.Min(100L, val));
		}

		private void CreateButterflies(long now, int hpPercent)
		{
			//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			int num = Math.Min(ButterflyCreateCount(hpPercent), 40 - butterflyGauge);
			butterflyGauge += num;
			if (!butterflyWarningIssued && butterflyGauge >= 20)
			{
				butterflyWarningIssued = true;
				map.DropMapMessage(5, "[Lucid] The dream is growing stronger. Be careful!");
			}
			Vector2[] array = ((phase == 1) ? LucidTables.ButterflyP1 : LucidTables.ButterflyP2);
			int mobId = ((phase == 1) ? 8880165 : 8880175);
			int num2 = CountMobs(mobId);
			int num3 = Math.Min(num, 40 - num2);
			for (int i = 0; i < num3; i++)
			{
				Vector2 point = array[butterflyPosition++ % array.Length];
				SpawnAt(mobId, point, raw: true);
			}
			if (butterflyGauge >= 40)
			{
				butterflyGauge = 0;
				butterflyWarningIssued = false;
				map.DropMapMessage(5, "[Lucid] Lucid has become enraged!");
				KillMatching(map, (int id) => id == 8880165 || id == 8880175);
				map.BroadcastEffect("customSkill/lucid/butterflyBurstVideoLayer");
				ScheduleDamage(1800L, 30, null, "butterfly-burst");
			}
			nextButterfly = now + ButterflyIntervalMillis(hpPercent);
		}

		private void FailFury()
		{
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			if (!active || furyFailed || !IsCurrentBossAlive())
			{
				return;
			}
			furyFailed = true;
			Vector2 position = boss.Position;
			int fh = boss.Fh;
			map.Kill(boss);
			NativeEventMap.Mob fail = map.SpawnAt(8880154, position.x, position.y, ground: false);
			if (fail != null)
			{
				fail.Fh = fh;
				fail.BlockDamageUntil(Now() + 10000);
			}
			Schedule(3000L, () =>
			{
				if (active && furyFailed && fail != null && map.FindByOid(fail.ObjectId) == fail)
				{
					ApplyDamage(100, null, "fury-fail", fail);
				}
			});
			Schedule(10000L, () =>
			{
				ResummonPhaseTwo(fail);
			});
		}

		private void ResummonPhaseTwo(NativeEventMap.Mob fail)
		{
			if (active && furyFailed && (!encounters.TryGetValue(map, out var value) || value == this))
			{
				if (fail != null && fail.Alive)
				{
					map.Kill(fail);
				}
				NativeEventMap.Mob mob = map.SpawnAt(8880141, LucidTables.FuryResummon.x, LucidTables.FuryResummon.y, ground: false);
				if (mob != null)
				{
					long hp = Math.Max(1L, mob.MaxHp * 16 / 100);
					mob.SetHp(hp);
					map.Instance?.SetIntProperty("phase", 2);
					StartPhase(map, mob, 2);
				}
			}
		}

		private void SummonNightmares()
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			map.DropMapMessage(5, "[Lucid] Lucid has summoned a powerful nightmare!");
			if (phase == 1 && CountMobs(8880164) < 4)
			{
				SpawnGroundMob(8880164, RandomGroundPoint(1));
			}
			int mobId = ((phase == 1) ? 8880161 : 8880171);
			int num = Math.Min((phase == 1) ? 1 : 2, 15 - CountMobs(mobId));
			for (int i = 0; i < num; i++)
			{
				SpawnGroundMob(mobId, RandomGroundPoint(phase));
			}
		}

		private void CastRush()
		{
			map.DropMapMessage(5, "[Lucid] Lucid is unleashing even greater power!");
			map.BroadcastEffect("customSkill/lucid/rushVideoLayer");
			HashSet<int> hit = new HashSet<int>();
			for (long num = 0L; num <= 3000; num += 100)
			{
				long collisionTime = num;
				Schedule(num, () =>
				{
					//IL_002b: Unknown result type (might be due to invalid IL or missing references)
					//IL_0030: Unknown result type (might be due to invalid IL or missing references)
					//IL_0033: Unknown result type (might be due to invalid IL or missing references)
					//IL_0044: Unknown result type (might be due to invalid IL or missing references)
					//IL_007f: Unknown result type (might be due to invalid IL or missing references)
					//IL_0091: Unknown result type (might be due to invalid IL or missing references)
					if (active && IsCurrentBossAlive())
					{
						Vector2 val = RushPositionAt(collisionTime);
						Rect val2 = new Rect(val.x + LucidTables.RushBodyLt.x, val.y + LucidTables.RushBodyLt.y, LucidTables.RushBodyRb.x - LucidTables.RushBodyLt.x, LucidTables.RushBodyRb.y - LucidTables.RushBodyLt.y);
						if (val2.Contains(PlayerPosition) && hit.Add(0))
						{
							DamageCharacter(20, "rush", boss);
						}
					}
				});
			}
		}

		private void BreakStainedGlass()
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			int num = stainedGlassIndex++ % LucidTables.GlassAreas.Length;
			Rect area = LucidTables.GlassAreas[num];
			map.BroadcastEffect(StainedGlassEffects[num]);
			ScheduleDamage(1260L, 20, area, "stained-glass");
			Schedule(3000L, () =>
			{
				//IL_0061: Unknown result type (might be due to invalid IL or missing references)
				if (active && IsCurrentBossAlive() && CountMobs(8880171) < 15)
				{
					SpawnGroundMob(8880171, new Vector2(area.x + area.width / 2f, area.y));
				}
			});
		}

		private void ApplyHurdleDamage()
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			int num = ((phase == 1) ? 8880161 : 8880171);
			Vector2 playerPosition = PlayerPosition;
			foreach (NativeEventMap.Mob allMob in map.AllMobs)
			{
				if (allMob.Id == num && allMob.Alive)
				{
					Vector2 position = allMob.Position;
					Rect val = new Rect(position.x - 20f, position.y - 500f, 40f, 510f);
					if (val.Contains(playerPosition))
					{
						DamageCharacter(20, "hurdle-area", boss);
						break;
					}
				}
			}
		}

		private void CastContagiousBomb()
		{
			if (!PlayerAvailable)
			{
				return;
			}
			map.DropMapMessage(5, "Lucid's bomb is spreading from you. Move away from the party!");
			map.BroadcastEffect("customSkill/lucid/bombVideoLayer");
			Schedule(3000L, () =>
			{
				//IL_001a: Unknown result type (might be due to invalid IL or missing references)
				//IL_001f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0023: Unknown result type (might be due to invalid IL or missing references)
				//IL_002f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0045: Unknown result type (might be due to invalid IL or missing references)
				if (active && IsCurrentBossAlive() && PlayerAvailable)
				{
					Vector2 playerPosition = PlayerPosition;
					ApplyDamage(35, new Rect(playerPosition.x - 250f, playerPosition.y - 250f, 500f, 500f), "contagious-bomb", boss);
				}
			});
		}

		private void ScheduleDamage(long delay, int percent, Nullable range, string skill)
		{
			Schedule(delay, () =>
			{
				if (active && IsCurrentBossAlive())
				{
					ApplyDamage(percent, range, skill, boss);
				}
			});
		}

		private void ApplyDamage(int percent, Nullable range, string skill, NativeEventMap.Mob source)
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			if (!PlayerAvailable)
			{
				return;
			}
			if (range.HasValue)
			{
				Rect value = range.Value;
				if (!value.Contains(PlayerPosition))
				{
					return;
				}
			}
			DamageCharacter(percent, skill, source);
		}

		private void DamageCharacter(int percent, string skill, NativeEventMap.Mob source)
		{
			if (PlayerAvailable)
			{
				long num = NpcApi.ScriptMaxHp;
				int num2 = (int)Math.Max(1L, num * percent / 100);
				NpcApi.ScriptHp -= num2;
				map.DamagePlayerPacket(num2, source?.Id ?? boss.Id, source?.ObjectId ?? bossObjectId);
				Debug.Log((object)("[LucidSkillTrace] map=" + map.Id + " phase=" + phase + " skill=" + skill + " mob=" + (source?.Id ?? boss.Id) + " oid=" + (source?.ObjectId ?? bossObjectId) + " percent=" + percent + " damage=" + num2 + " hp=" + NpcApi.ScriptHp));
			}
		}

		private void SpawnGroundMob(int mobId, Vector2 point)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			map.SpawnOnGround(mobId, point.x, point.y);
		}

		private void SpawnAt(int mobId, Vector2 point, bool raw)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			map.SpawnAt(mobId, point.x, point.y, !raw);
		}

		private int CountMobs(int mobId)
		{
			int num = 0;
			foreach (NativeEventMap.Mob allMob in map.AllMobs)
			{
				if (allMob.Id == mobId && allMob.Alive)
				{
					num++;
				}
			}
			return num;
		}

		private static Vector2 RandomGroundPoint(int targetPhase)
		{
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			int[] array = ((targetPhase != 1) ? new int[6] { 0, 250, 500, 750, 1000, 1250 } : new int[6] { 220, 520, 820, 1120, 1420, 1720 });
			return new Vector2((float)array[Random.Range(0, array.Length)], (float)((targetPhase != 1) ? 380 : 0));
		}

		public void StopInternal(bool cleanup)
		{
			active = false;
			jobs.Clear();
			if (cleanup)
			{
				CleanupAuxiliaryMobs(map);
			}
		}
	}

	public const int PhaseOne = 1;

	public const int PhaseTwo = 2;

	public const int PhaseThree = 3;

	public const int LucidP1 = 8880140;

	public const int LucidP2 = 8880141;

	public const int LucidP3 = 8880152;

	public const int FurySuccess = 8880153;

	public const int FuryFail = 8880154;

	private const int MushroomP1 = 8880164;

	private const int GolemP1 = 8880161;

	private const int ButterflyP1 = 8880165;

	private const int GolemP2 = 8880171;

	private const int ButterflyP2 = 8880175;

	private const int ButterflyCapacity = 40;

	private const int MaxGolems = 15;

	private const int MaxMushrooms = 4;

	private const long ControllerTickMs = 250L;

	private const long FuryLimitMs = 45000L;

	private const long FuryFailImpactDelayMs = 3000L;

	private const long FuryFailResummonDelayMs = 10000L;

	private const int FuryResummonHpPercent = 16;

	private const long DragonBreathDamageMs = 6300L;

	private const long ButterflyBurstImpactMs = 1800L;

	private const long RushDurationMs = 3000L;

	private const long RushHitIntervalMs = 100L;

	private const int PhantomBarragePrepareMs = 2400;

	private const int PhantomBarrageHitIntervalMs = 1000;

	private const int PhantomBarrageHitCount = 12;

	private const string EffectRoot = "customSkill/lucid/";

	private const string DragonP1Effect = "customSkill/lucid/dragonP1VideoLayer";

	private const string DragonP2Effect = "customSkill/lucid/dragonP2VideoLayer";

	private const string LaserRainEffect = "customSkill/lucid/laserRainVideoLayer";

	private const string PhantomBarrageEffect = "customSkill/lucid/phantomBarrageVideoLayer";

	private const string RushEffect = "customSkill/lucid/rushVideoLayer";

	private const string FuryEffect = "customSkill/lucid/furyVideoLayer";

	private const string FuryStopEffect = "customSkill/lucid/furyStopVideoLayer";

	private const string ButterflyBurstEffect = "customSkill/lucid/butterflyBurstVideoLayer";

	private const string BombEffect = "customSkill/lucid/bombVideoLayer";

	private static readonly string[] StainedGlassEffects = new string[6] { "customSkill/lucid/stainedGlassVideoLayer", "customSkill/lucid/stainedGlass1VideoLayer", "customSkill/lucid/stainedGlass2VideoLayer", "customSkill/lucid/stainedGlass3VideoLayer", "customSkill/lucid/stainedGlass4VideoLayer", "customSkill/lucid/stainedGlass5VideoLayer" };

	private static readonly HashSet<int> SupportMobs = new HashSet<int> { 8880164, 8880161, 8880165, 8880171, 8880175 };

	private static readonly HashSet<int> FuryTransitionMobs = new HashSet<int> { 8880153, 8880154 };

	private static readonly Dictionary<NativeEventMap, Encounter> encounters = new Dictionary<NativeEventMap, Encounter>();

	public const string TypeName = "org.gms.server.life.LucidBossCompat";

	public static int ActiveCount => encounters.Count;

	public static bool IsLucidBoss(int mobId)
	{
		if (mobId != 8880140 && mobId != 8880141)
		{
			return mobId == 8880152;
		}
		return true;
	}

	public static int ButterflyIntervalMillis(int hpPercent)
	{
		if (hpPercent < 90)
		{
			if (hpPercent < 70)
			{
				if (hpPercent < 50)
				{
					if (hpPercent < 20)
					{
						return 2000;
					}
					return 3000;
				}
				return 4000;
			}
			return 4500;
		}
		return 5000;
	}

	public static int ButterflyCreateCount(int hpPercent)
	{
		if (hpPercent < 90)
		{
			if (hpPercent < 70)
			{
				if (hpPercent < 50)
				{
					if (hpPercent < 20)
					{
						return 20;
					}
					return 15;
				}
				return 10;
			}
			return 7;
		}
		return 5;
	}

	public static JsValue Resolve(string name)
	{
		if (!(name == "org.gms.server.life.LucidBossCompat"))
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
			throw new JsRuntimeException("Unmigrated LucidBossCompat API: " + key);
		};
		jsHostObject.Set("startPhase", JsFunction.FromNative("startPhase", (JsCall c) =>
		{
			NativeEventMap nativeEventMap = Map(c.Arg(0));
			NativeEventMap.Mob mob = Mob(c.Arg(1));
			int num = JsConvert.ToInt32(c.Arg(2));
			if (nativeEventMap == null || mob == null || !IsLucidBoss(mob.Id))
			{
				throw new JsRuntimeException("invalid Lucid encounter phase");
			}
			if (mob.Id != num switch
			{
				3 => 8880152, 
				2 => 8880141, 
				1 => 8880140, 
				_ => -1, 
			})
			{
				throw new JsRuntimeException("Lucid phase and boss ID do not match");
			}
			StartPhase(nativeEventMap, mob, num);
			return JsUndefined.Value;
		}));
		jsHostObject.Set("stop", JsFunction.FromNative("stop", (JsCall c) =>
		{
			Stop(Map(c.Arg(0)));
			return JsUndefined.Value;
		}));
		jsHostObject.Set("finishFurySuccess", JsFunction.FromNative("finishFurySuccess", (JsCall c) =>
		{
			FinishFurySuccess(Map(c.Arg(0)));
			return JsUndefined.Value;
		}));
		jsHostObject.Set("protectFurySuccessMob", JsFunction.FromNative("protectFurySuccessMob", (JsCall c) =>
		{
			ProtectFurySuccessMob(Mob(c.Arg(0)));
			return JsUndefined.Value;
		}));
		jsHostObject.Set("isLucidBoss", JsFunction.FromNative("isLucidBoss", (JsCall c) => JsBoolean.Of(IsLucidBoss(JsConvert.ToInt32(c.Arg(0))))));
		return jsHostObject;
	}

	private static NativeEventMap Map(JsValue value)
	{
		return (value as JsHostObject)?.Host as NativeEventMap;
	}

	private static NativeEventMap.Mob Mob(JsValue value)
	{
		return NativeEventMap.FindByHost(value);
	}

	public static void StartPhase(NativeEventMap map, NativeEventMap.Mob boss, int phase)
	{
		if (map != null && boss != null)
		{
			if (phase == 3)
			{
				CleanupSupportMobs(map);
			}
			Encounter encounter = new Encounter(map, boss, phase);
			if (encounters.TryGetValue(map, out var value))
			{
				encounters[map] = encounter;
				value.StopInternal(cleanup: false);
			}
			else
			{
				encounters.Add(map, encounter);
			}
			encounter.Start();
		}
	}

	public static void Stop(NativeEventMap map)
	{
		if (map != null)
		{
			if (encounters.TryGetValue(map, out var value))
			{
				encounters.Remove(map);
				value.StopInternal(cleanup: true);
			}
			else
			{
				CleanupAuxiliaryMobs(map);
			}
		}
	}

	public static void FinishFurySuccess(NativeEventMap map)
	{
		if (map != null)
		{
			if (encounters.TryGetValue(map, out var value))
			{
				encounters.Remove(map);
				value.StopInternal(cleanup: false);
			}
			map.BroadcastEffect("customSkill/lucid/furyStopVideoLayer");
		}
	}

	public static void ProtectFurySuccessMob(NativeEventMap.Mob mob)
	{
		if (mob != null && mob.Id == 8880153)
		{
			mob.BlockDamageUntil(Now() + 5000);
		}
	}

	private static long Now()
	{
		return NativeEventScripts.Clock();
	}

	public static void Tick(long now)
	{
		if (encounters.Count == 0)
		{
			return;
		}
		List<Encounter> list = null;
		foreach (Encounter value in encounters.Values)
		{
			(list ?? (list = new List<Encounter>())).Add(value);
		}
		for (int i = 0; i < list.Count; i++)
		{
			list[i].Tick(now);
		}
	}

	public static void ResetAll()
	{
		encounters.Clear();
		NativeScriptEffect.Reset();
	}

	public static int PhaseOf(NativeEventMap map)
	{
		if (!encounters.TryGetValue(map, out var value))
		{
			return 0;
		}
		return value.Phase;
	}

	private static void CleanupSupportMobs(NativeEventMap map)
	{
		KillMatching(map, (int id) => SupportMobs.Contains(id));
	}

	private static void CleanupAuxiliaryMobs(NativeEventMap map)
	{
		KillMatching(map, (int id) => SupportMobs.Contains(id) || FuryTransitionMobs.Contains(id));
	}

	private static void KillMatching(NativeEventMap map, Func<int, bool> matches)
	{
		foreach (NativeEventMap.Mob allMob in map.AllMobs)
		{
			if (matches(allMob.Id))
			{
				map.Kill(allMob);
			}
		}
	}

	public static Vector2 RushPositionAt(long elapsedMillis)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		Vector2[] rushPath = LucidTables.RushPath;
		int[] rushSpeeds = LucidTables.RushSpeeds;
		double[] array = new double[rushPath.Length - 1];
		double num = 0.0;
		for (int i = 1; i < rushPath.Length; i++)
		{
			num += (array[i - 1] = Vector2.Distance(rushPath[i - 1], rushPath[i]) / (float)Math.Max(1, rushSpeeds[i]));
		}
		double num2 = (double)Math.Max(0L, Math.Min(3000L, elapsedMillis)) * num / 3000.0;
		double num3 = 0.0;
		for (int j = 1; j < rushPath.Length; j++)
		{
			double num4 = array[j - 1];
			if (num2 <= num3 + num4 || j == rushPath.Length - 1)
			{
				double val = ((num4 == 0.0) ? 1.0 : ((num2 - num3) / num4));
				val = Math.Max(0.0, Math.Min(1.0, val));
				Vector2 val2 = rushPath[j - 1];
				Vector2 val3 = rushPath[j];
				return new Vector2((float)Math.Round((double)val2.x + (double)(val3.x - val2.x) * val), (float)Math.Round((double)val2.y + (double)(val3.y - val2.y) * val));
			}
			num3 += num4;
		}
		return rushPath[^1];
	}
}
