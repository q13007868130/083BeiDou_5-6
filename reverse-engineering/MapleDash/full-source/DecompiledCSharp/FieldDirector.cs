using System;
using System.Collections.Generic;
using System.Diagnostics;
using Server.Bot;
using Server.Bot.Environment;
using Server.Bot.Types.Pq;
using Server.Field;
using UnityEngine;

public sealed class FieldDirector : MonoBehaviour
{
	private enum WarpState
	{
		None,
		Out,
		Hold,
		In
	}

	private sealed class CachedField
	{
		public MapSpawner spawner;

		public readonly List<Point> points = new List<Point>();
	}

	private sealed class WildBoss
	{
		public int mapId;

		public int mobId;

		public float x;

		public float y;

		public bool alive = true;
	}

	private sealed class Point
	{
		public SpawnPoint sp;

		public EnemyUnit mob;

		public bool restoreAlive;

		public long restoreHp;
	}

	private FieldView view;

	private static Action<Exception> hookedStartupError;

	private bool grindHooked;

	private bool pendingMapLogged;

	private MapSpawner spawner;

	private readonly List<Point> points = new List<Point>();

	private readonly Dictionary<SpawnPoint, Point> bySpawn = new Dictionary<SpawnPoint, Point>();

	private readonly Dictionary<int, Point> byMob = new Dictionary<int, Point>();

	private readonly List<EnemyUnit> dynamicMobs = new List<EnemyUnit>();

	private readonly Random rng = new Random();

	private float respawnTimer;

	private readonly List<Point> frenzyPoints = new List<Point>();

	private long frenzyExpires;

	private NativeImgAnimation frenzyImage;

	private Vector3 frenzyPosition;

	private int frenzyFacing;

	private int frenzyOrder;

	private FieldPortal nearPortal;

	private float portalLock;

	private bool portalArmed;

	private float saveTimer;

	private bool logKills;

	private BotCombatField botCombat;

	private GroundItemBridge groundLoot;

	private readonly BotGroundPickup botPickup = new BotGroundPickup();

	private BotField botField;

	private bool logFrames;

	private bool logPerf;

	private float logClock;

	private int hitchFrames;

	private const float RespawnInterval = 10f;

	private const int AggroRangePx = 320;

	private WarpState warpState;

	private float warpClock;

	private FieldPortal warpTarget;

	public static readonly HashSet<string> BlockedPortalScripts = new HashSet<string>();

	private int pendingWarpMap = -1;

	private string pendingWarpPortal;

	private int pendingWarpPortalIndex = -1;

	private int deferredWarpMap = -1;

	private readonly Dictionary<int, CachedField> cachedFields = new Dictionary<int, CachedField>();

	private object cachedCharacter;

	private static readonly List<WildBoss> wildBosses = new List<WildBoss>();

	private static readonly HashSet<int> wildDirty = new HashSet<int>();

	private double nextEnvironmentDamage;

	public FieldMap Map { get; private set; }

	public NativeEventMap EventMap { get; private set; }

	public int StateId => EventMap?.Scope ?? Map?.id ?? (-1);

	public int BotMapId => Map?.id ?? (-1);

	public int AliveCount { get; private set; }

	public bool FrenzyActive => frenzyExpires > NowMs();

	public FieldPortal NearPortal => nearPortal;

	public bool Warping => warpState != WarpState.None;

	public string ActivateFrenzy(PlayerCtrl owner)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		if (Map == null || spawner == null || (Object)(object)owner == (Object)null)
		{
			return "当前无法放置轮回碑石。";
		}
		if (frenzyExpires != 0L)
		{
			return "该地图已经存在轮回碑石。";
		}
		float num = owner.FeetPos.x * 40f;
		float num2 = (0f - owner.FeetPos.y) * 40f - 1f;
		bool flag = false;
		foreach (FieldFoothold foothold in Map.footholds)
		{
			if (foothold.x1 != foothold.x2 && num >= Mathf.Min(foothold.x1, foothold.x2) && num <= Mathf.Max(foothold.x1, foothold.x2) && foothold.y1 + (foothold.y2 - foothold.y1) * (num - foothold.x1) / (foothold.x2 - foothold.x1) >= num2)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return "当前位置无法放置轮回碑石。";
		}
		frenzyPosition = new Vector3(num / 40f, (0f - (DropToFoothold(num, num2) - 1f)) / 40f, 0f);
		frenzyFacing = owner.facing;
		frenzyOrder = owner.SortingOrder - 1;
		foreach (SpawnPoint item in spawner.StartFrenzy())
		{
			Point point = new Point
			{
				sp = item
			};
			points.Add(point);
			bySpawn[item] = point;
			frenzyPoints.Add(point);
		}
		frenzyExpires = NowMs() + 600000;
		frenzyImage = NativeImgAnimation.Spawn("Skill/000.img/skill/0001016/summon/summoned", ((Component)this).transform, frenzyPosition, frenzyOrder, owner.facing > 0);
		NativeImgAnimation.Spawn("Skill/000.img/skill/0001016/effect", ((Component)owner).transform, Vector2.op_Implicit(owner.FeetPos), owner.SortingOrder + 1, owner.facing > 0);
		respawnTimer = 0f;
		TickRespawn(0f);
		return null;
	}

	public void StopFrenzy()
	{
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		if (frenzyExpires == 0L)
		{
			return;
		}
		frenzyExpires = 0L;
		spawner?.StopFrenzy();
		foreach (Point frenzyPoint in frenzyPoints)
		{
			if ((Object)(object)frenzyPoint.mob != (Object)null)
			{
				frenzyPoint.mob.ExpireNative(1);
			}
			points.Remove(frenzyPoint);
			bySpawn.Remove(frenzyPoint.sp);
		}
		frenzyPoints.Clear();
		frenzyImage?.Remove();
		frenzyImage = null;
		NativeImgAnimation.Spawn("Skill/000.img/skill/0001016/summon/die", ((Component)this).transform, frenzyPosition, frenzyOrder, frenzyFacing > 0);
	}

	public static void ResetWildBosses()
	{
		wildBosses.Clear();
		wildDirty.Clear();
	}

	public static void SpawnWildBoss(int mapId, int mobId, float x, float y)
	{
		WildBoss wildBoss = wildBosses.Find((WildBoss b) => b.mapId == mapId && b.mobId == mobId);
		if (wildBoss == null)
		{
			wildBoss = new WildBoss
			{
				mapId = mapId,
				mobId = mobId
			};
			wildBosses.Add(wildBoss);
		}
		wildBoss.x = x;
		wildBoss.y = y;
		wildBoss.alive = true;
		wildDirty.Add(mapId);
	}

	public static bool HasWildBoss(int mapId, int mobId)
	{
		return wildBosses.Exists((WildBoss b) => b.mapId == mapId && b.mobId == mobId && b.alive);
	}

	private static void MarkWildBossKilled(int mapId, int mobId)
	{
		WildBoss wildBoss = wildBosses.Find((WildBoss b) => b.mapId == mapId && b.mobId == mobId);
		if (wildBoss != null)
		{
			wildBoss.alive = false;
		}
	}

	private void MaterializeWildBosses(FieldMap map)
	{
		foreach (WildBoss wildBoss in wildBosses)
		{
			if (wildBoss.alive && wildBoss.mapId == map.id)
			{
				SpawnWildBossHere(wildBoss.mobId, wildBoss.x, wildBoss.y);
			}
		}
	}

	private void SpawnWildBossHere(int mobId, float x, float y)
	{
		if (Map == null)
		{
			return;
		}
		Point point = null;
		foreach (Point point2 in points)
		{
			if (point2.sp != null && point2.sp.MobId == mobId)
			{
				if ((Object)(object)point2.mob != (Object)null && !point2.mob.dead)
				{
					return;
				}
				point = point2;
			}
		}
		if (point != null)
		{
			SpawnAt(point);
			return;
		}
		FieldCatalog.LoadMob(mobId);
		if (FieldCatalog.Mobs.ContainsKey(mobId))
		{
			AddScriptMonsterSpawn(mobId, x, y, -1, -1);
		}
	}

	private void PreserveSpawnState()
	{
		if (EventMap != null)
		{
			EventMap.Detach();
		}
		else
		{
			if (Map == null || spawner == null)
			{
				return;
			}
			StopFrenzy();
			CachedField cachedField = new CachedField
			{
				spawner = spawner
			};
			foreach (Point point in points)
			{
				point.restoreAlive = (Object)(object)point.mob != (Object)null && !point.mob.dead;
				point.restoreHp = (point.restoreAlive ? (point.mob.NativeController?.CurrentHp ?? point.mob.hp) : 0);
				cachedField.points.Add(point);
			}
			cachedFields[Map.id] = cachedField;
		}
	}

	public void Begin(int startMapId)
	{
		if (cachedCharacter != GameSession.Character)
		{
			ClearMobs();
			Map = null;
			cachedFields.Clear();
			ResetWildBosses();
			cachedCharacter = GameSession.Character;
		}
		FieldCatalog.Ensure();
		PlayerCtrl.InputLocked = false;
		warpState = WarpState.None;
		warpTarget = null;
		ScreenFade.Dismiss();
		if ((Object)(object)view == (Object)null)
		{
			view = ((Component)this).gameObject.AddComponent<FieldView>();
		}
		logKills = PlayerPrefs.GetInt("debug_field_log", 0) != 0;
		if (logKills)
		{
			PlayerPrefs.DeleteKey("debug_field_log");
		}
		logFrames = PlayerPrefs.GetInt("debug_frame_log", 0) != 0;
		if (logFrames)
		{
			PlayerPrefs.DeleteKey("debug_frame_log");
		}
		logPerf = PlayerPrefs.GetInt("debug_bot_perf", 0) != 0;
		if (logPerf)
		{
			PlayerPrefs.DeleteKey("debug_bot_perf");
		}
		Backdrop backdrop = Object.FindObjectOfType<Backdrop>();
		if ((Object)(object)backdrop != (Object)null)
		{
			((Component)backdrop).gameObject.SetActive(false);
		}
		Enter(startMapId, null);
		NativeEventScripts.QueueWildBossEvents();
	}

	public void Enter(int mapId, string spawnPortalName, int spawnPortalIndex = -1)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		FieldCatalog.Ensure();
		BlockedPortalScripts.Clear();
		NativeEventMap nativeEventMap = NativeEventScripts.ResolveMap(mapId);
		FieldMap obj = nativeEventMap?.Template ?? FieldCatalog.GetMap(mapId);
		FieldMap fieldMap = obj;
		if (obj == null)
		{
			Debug.LogWarning((object)("[Field] 没有地图 " + mapId));
			return;
		}
		long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
		try
		{
			view.Show(fieldMap);
		}
		catch (Exception ex)
		{
			Debug.LogError((object)("[Field] 无法进入地图 " + mapId + ": " + ex.Message));
			return;
		}
		long elapsedMilliseconds2 = stopwatch.ElapsedMilliseconds;
		deferredWarpMap = ((fieldMap.id == 0 && fieldMap.returnMap > 0 && fieldMap.returnMap != fieldMap.id) ? fieldMap.returnMap : (-1));
		FileLog.Note("进入地图 " + mapId);
		PreserveSpawnState();
		ClearMobs();
		EnemyUnit[] array = Object.FindObjectsOfType<EnemyUnit>();
		foreach (EnemyUnit enemyUnit in array)
		{
			if ((Object)(object)enemyUnit != (Object)null && !enemyUnit.fieldMode)
			{
				((Component)enemyUnit).gameObject.SetActive(false);
				Object.Destroy((Object)(object)((Component)enemyUnit).gameObject);
			}
		}
		GroundItem.Clear();
		NativeSummon[] array2 = Object.FindObjectsOfType<NativeSummon>();
		foreach (NativeSummon nativeSummon in array2)
		{
			if ((Object)(object)nativeSummon != (Object)null)
			{
				((Component)nativeSummon).gameObject.SetActive(false);
				Object.Destroy((Object)(object)((Component)nativeSummon).gameObject);
			}
		}
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		if (playerCtrl != null)
		{
			((Component)playerCtrl).GetComponent<SkillSystem>()?.CancelActions();
		}
		bool flag = (Map != null && Map.fieldType == 82) != (fieldMap.fieldType == 82);
		Map = fieldMap;
		EventMap = nativeEventMap;
		NativeEventScripts.ChangedMap(fieldMap.id, after: false);
		nextEnvironmentDamage = Time.realtimeSinceStartupAsDouble + EnvironmentInterval();
		NpcField.Bind(fieldMap);
		if (flag)
		{
			playerCtrl?.RefreshFieldAppearance();
		}
		ApplyGeometry(fieldMap);
		PlacePlayer(fieldMap, spawnPortalName, spawnPortalIndex);
		if (playerCtrl != null)
		{
			((Component)playerCtrl).GetComponent<SkillSystem>()?.RestoreBuffSummons();
		}
		AudioManager.PlayBgm(fieldMap.bgm);
		long elapsedMilliseconds3 = stopwatch.ElapsedMilliseconds;
		if (EventMap != null)
		{
			points.Clear();
			byMob.Clear();
			bySpawn.Clear();
			spawner = null;
			EventMap.Attach(this);
		}
		else
		{
			BuildPoints(fieldMap);
			wildDirty.Remove(fieldMap.id);
			MaterializeWildBosses(fieldMap);
		}
		long elapsedMilliseconds4 = stopwatch.ElapsedMilliseconds;
		respawnTimer = 10f;
		portalLock = 0.4f;
		portalArmed = false;
		nearPortal = null;
		Profile.LastMapId = fieldMap.id;
		NativeSkillBackdrop.Enter(this);
		NativeMapScripts.Enter(this);
		if (NativeMapIntro.Active(this))
		{
			deferredWarpMap = -1;
		}
		NativeEventScripts.ChangedMap(fieldMap.id, after: true);
		FileLog.Note("[Field/Enter] map=" + fieldMap.id + " data_ms=" + elapsedMilliseconds + " art_ms=" + (elapsedMilliseconds2 - elapsedMilliseconds) + " geometry_ms=" + (elapsedMilliseconds3 - elapsedMilliseconds2) + " mobs_ms=" + (elapsedMilliseconds4 - elapsedMilliseconds3) + " scripts_ms=" + (stopwatch.ElapsedMilliseconds - elapsedMilliseconds4));
		Debug.Log((object)("[Field] 进入 " + fieldMap.id + " " + fieldMap.name + "：刷怪点 " + points.Count + "，传送门 " + fieldMap.portals.Count));
	}

	private void ApplyGeometry(FieldMap map)
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		List<Vector4> list = new List<Vector4>();
		List<int> list2 = new List<int>();
		List<int> list3 = new List<int>();
		List<bool> list4 = new List<bool>();
		float num = float.MaxValue;
		float num2 = float.MinValue;
		foreach (FieldFoothold foothold in map.footholds)
		{
			float num3 = map.WorldX(foothold.x1);
			float num4 = map.WorldY(foothold.y1);
			float num5 = map.WorldX(foothold.x2);
			float num6 = map.WorldY(foothold.y2);
			list.Add(new Vector4(num3, num4, num5, num6));
			list2.Add(foothold.force);
			list3.Add(foothold.drag);
			list4.Add(foothold.forbidFallDown != 0);
			num = Mathf.Min(num, num3);
			num2 = Mathf.Max(num2, num5);
		}
		PlayerCtrl.SetTerrain((list.Count > 0) ? list : null, list4, list2, list3);
		List<Art.ClimbLine> list5 = new List<Art.ClimbLine>();
		foreach (FieldRope rope in map.ropes)
		{
			list5.Add(new Art.ClimbLine
			{
				x = map.WorldX(rope.x),
				top = map.WorldY(rope.y1),
				bottom = map.WorldY(rope.y2),
				ladder = rope.ladder,
				upperExit = rope.upperExit
			});
		}
		PlayerCtrl.Climbs = ((list5.Count > 0) ? list5 : null);
		if (list.Count > 0)
		{
			PlayerCtrl.SetBounds(num, num2);
		}
		else
		{
			PlayerCtrl.SetBounds(map.ViewMinX, map.ViewMaxX);
		}
		PlayerCtrl.FallFloor = map.ViewMinY - 4f;
		CameraRig cameraRig = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.camRig : null);
		if ((Object)(object)cameraRig != (Object)null)
		{
			cameraRig.SetBounds(map.ViewMinX, map.ViewMaxX, map.ViewMinY, map.ViewMaxY);
		}
	}

	private void PlacePlayer(FieldMap map, string portalName, int portalIndex = -1)
	{
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		if (!((Object)(object)playerCtrl == (Object)null))
		{
			FieldPortal fieldPortal = map.Portal(portalName) ?? ((portalIndex >= 0 && portalIndex < map.portals.Count) ? map.portals[portalIndex] : null) ?? map.FirstPortal ?? map.SpawnPortal;
			float num = ((fieldPortal != null) ? map.WorldX(fieldPortal.x) : 0f);
			float refY = ((fieldPortal != null) ? map.WorldY(fieldPortal.y) : 0f);
			float groundY = PlayerCtrl.GroundAt(Mathf.Clamp(num, PlayerCtrl.XMin, PlayerCtrl.XMax), refY);
			playerCtrl.PlaceAt(num, groundY);
			if (fieldPortal != null)
			{
				playerCtrl.facing = 1;
			}
			if ((Object)(object)GameLoop.I.camRig != (Object)null)
			{
				GameLoop.I.camRig.SnapTo(num);
			}
		}
	}

	private void BuildPoints(FieldMap map)
	{
		points.Clear();
		byMob.Clear();
		bySpawn.Clear();
		if (cachedFields.TryGetValue(map.id, out var value))
		{
			spawner = value.spawner;
			{
				foreach (Point point2 in value.points)
				{
					points.Add(point2);
					bySpawn[point2.sp] = point2;
					if (point2.restoreAlive)
					{
						SpawnAt(point2, markSpawned: false);
						point2.mob?.NativeController?.RestoreFieldHp(point2.restoreHp);
					}
					else if (point2.sp.ShouldSpawn(NowMs()))
					{
						SpawnAt(point2);
					}
				}
				return;
			}
		}
		spawner = new MapSpawner();
		spawner.Clock = NowMs;
		spawner.SetMobInterval(Map.createMobInterval);
		if (GameServer.Local != null)
		{
			DbStore db = GameServer.Local.Db;
			spawner.BossRespawnMobTimeRate = GameConfigDao.GetFloat(db, "boss_respawn_mob_time_rate", 1.0);
			spawner.FullRespawn = GameConfigDao.GetBool(db, "use_enable_full_respawn");
		}
		spawner.SetRandomRefresh(GameServer.Local == null || GameConfigDao.GetBool(GameServer.Local.Db, "use_monster_random_refresh_position", fallback: true));
		foreach (FieldSpawn spawn in map.spawns)
		{
			float y = DropToFoothold(spawn.x, spawn.y) - 1f;
			SpawnPointDef spawnPointDef = new SpawnPointDef
			{
				mob = spawn.mob,
				mobTime = spawn.mobTime,
				fh = spawn.fh,
				f = spawn.dir,
				team = spawn.team,
				cy = (int)spawn.cy,
				x = spawn.x,
				y = y,
				rx0 = spawn.rx0,
				rx1 = spawn.rx1,
				immobile = false,
				isBoss = IsBossMob(spawn.mob)
			};
			SpawnPoint spawnPoint = spawner.AddMonsterSpawn(spawnPointDef);
			Point point = new Point
			{
				sp = spawnPoint,
				mob = null
			};
			points.Add(point);
			bySpawn[spawnPoint] = point;
			if (spawnPoint.ShouldSpawn(NowMs()) || spawnPointDef.mobTime == -1)
			{
				SpawnAt(point);
			}
		}
	}

	public void AddScriptMonsterSpawn(int id, float x, float y, int mobTime, int team)
	{
		if (Map != null && spawner != null)
		{
			FieldCatalog.LoadMob(id);
			SpawnPoint spawnPoint = spawner.AddMonsterSpawn(new SpawnPointDef
			{
				mob = id,
				x = x,
				y = DropToFoothold(x, y) - 1f,
				mobTime = mobTime,
				team = team,
				isBoss = IsBossMob(id)
			});
			Point point = new Point
			{
				sp = spawnPoint
			};
			points.Add(point);
			bySpawn[spawnPoint] = point;
			if (spawnPoint.ShouldSpawn(NowMs()) || spawnPoint.MobTime == -1)
			{
				SpawnAt(point);
			}
		}
	}

	private float DropToFoothold(float mapX, float mapY)
	{
		if (Map == null)
		{
			return mapY;
		}
		float num = mapY;
		bool flag = false;
		foreach (FieldFoothold foothold in Map.footholds)
		{
			float num2 = Mathf.Min(foothold.x1, foothold.x2);
			float num3 = Mathf.Max(foothold.x1, foothold.x2);
			if (!(mapX < num2) && !(mapX > num3))
			{
				float num4 = ((foothold.x2 == foothold.x1) ? 0f : ((mapX - foothold.x1) / (foothold.x2 - foothold.x1)));
				float num5 = foothold.y1 + (foothold.y2 - foothold.y1) * num4;
				if (num5 >= mapY && (!flag || num5 < num))
				{
					num = num5;
					flag = true;
				}
			}
		}
		return num;
	}

	private static bool IsBossMob(int mobId)
	{
		if (FieldCatalog.Mobs.TryGetValue(mobId, out var value))
		{
			return Art.Num(value.stats, "boss") > 0f;
		}
		return false;
	}

	private static long NowMs()
	{
		return (long)(Time.realtimeSinceStartupAsDouble * 1000.0);
	}

	private void SpawnAt(Point p, bool markSpawned = true)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		SpawnPointDef def = p.sp.Def;
		if (FieldCatalog.Mobs.TryGetValue(def.mob, out var value))
		{
			GameObject val = new GameObject("Mob_" + def.mob);
			((Renderer)val.AddComponent<SpriteRenderer>()).enabled = false;
			EnemyUnit enemyUnit = val.AddComponent<EnemyUnit>();
			enemyUnit.artKey = (string.IsNullOrEmpty(value.artKey) ? value.id.ToString() : value.artKey);
			enemyUnit.fieldMode = true;
			enemyUnit.fieldMobId = def.mob;
			enemyUnit.label = value.name;
			float num = Map.WorldX(def.x);
			float refY = Map.WorldY(def.y);
			enemyUnit.homeX = num;
			float rx = def.rx0;
			float rx2 = def.rx1;
			if (rx2 > rx)
			{
				enemyUnit.roamMin = Map.WorldX(rx);
				enemyUnit.roamMax = Map.WorldX(rx2);
			}
			else
			{
				enemyUnit.roamMin = num - 3f;
				enemyUnit.roamMax = num + 3f;
			}
			enemyUnit.aggroRange = 8f;
			enemyUnit.Init(2, num, refY);
			ApplyStats(enemyUnit, value);
			enemyUnit.BindNative(this);
			p.mob = enemyUnit;
			if (markSpawned)
			{
				p.sp.MarkSpawned(NowMs());
			}
			byMob[((Object)enemyUnit).GetInstanceID()] = p;
		}
	}

	public EnemyUnit SpawnNativeMob(int mobId, Vector3 position, bool ground = true, bool floating = false, bool eventOwned = false)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		if (Map == null)
		{
			return null;
		}
		if (EventMap != null && !eventOwned)
		{
			EnemyUnit enemyUnit = EventMap.SpawnDynamic(mobId, position, ground);
			if ((Object)(object)enemyUnit != (Object)null)
			{
				enemyUnit.nativeFloating = floating;
			}
			return enemyUnit;
		}
		FieldCatalog.LoadMob(mobId);
		if (!FieldCatalog.Mobs.TryGetValue(mobId, out var value))
		{
			return null;
		}
		EnemyUnit enemyUnit2 = new GameObject("Mob_" + mobId).AddComponent<EnemyUnit>();
		enemyUnit2.artKey = value.artKey;
		enemyUnit2.fieldMode = true;
		enemyUnit2.fieldMobId = mobId;
		enemyUnit2.label = value.name;
		enemyUnit2.eventOwned = eventOwned;
		enemyUnit2.homeX = position.x;
		enemyUnit2.roamMin = position.x - 3f;
		enemyUnit2.roamMax = position.x + 3f;
		enemyUnit2.aggroRange = 8f;
		enemyUnit2.Init(2, position.x, position.y);
		enemyUnit2.nativeFloating = floating;
		if (!ground)
		{
			((Component)enemyUnit2).transform.position = position;
		}
		ApplyStats(enemyUnit2, value);
		dynamicMobs.Add(enemyUnit2);
		enemyUnit2.BindNative(this);
		return enemyUnit2;
	}

	private void ApplyStats(EnemyUnit e, FieldMob def)
	{
		Dictionary<string, object> stats = def.stats;
		e.hpMax = Math.Max(1L, (long)Art.Num(stats, "maxHP", 1f));
		e.hp = e.hpMax;
		e.damage = Mathf.Max(0f, Art.Num(stats, "PADamage"));
		float wzSpeed = Art.Num(stats, "speed");
		e.speed = MobSpeed(wzSpeed);
	}

	public static float MobSpeed(float wzSpeed)
	{
		return (float)(125.0 * NativeMobMotion.Multiplier((int)wzSpeed) / 40.0);
	}

	private void Update()
	{
		using (BotPerf.Timer("Field.Update"))
		{
			BotWorkPump.BeginFrame();
			TickBots();
			TickFrameLog();
			if (Warping)
			{
				TickWarpFade();
			}
			else if (deferredWarpMap >= 0)
			{
				int mapId = deferredWarpMap;
				deferredWarpMap = -1;
				WarpTo(mapId);
			}
			else
			{
				if (Map == null || (Object)(object)GameLoop.I == (Object)null || GameLoop.I.state != GameLoop.GameState.Playing)
				{
					return;
				}
				PlayerCtrl player = GameLoop.I.player;
				if ((Object)(object)player == (Object)null)
				{
					return;
				}
				float deltaTime = Time.deltaTime;
				if (deltaTime <= 0f)
				{
					return;
				}
				(((Component)this).GetComponent<NativePetField>() ?? ((Component)this).gameObject.AddComponent<NativePetField>()).Tick(player, deltaTime);
				(((Component)this).GetComponent<NativeAndroid>() ?? ((Component)this).gameObject.AddComponent<NativeAndroid>()).Tick(player, deltaTime);
				portalLock = Mathf.Max(0f, portalLock - deltaTime);
				CheckPortals(player, deltaTime);
				TickRespawn(deltaTime);
				if (EventMap == null && wildDirty.Count > 0 && wildDirty.Remove(Map.id))
				{
					MaterializeWildBosses(Map);
				}
				TickEnvironment(player, Time.realtimeSinceStartupAsDouble);
				AliveCount = 0;
				foreach (EnemyUnit item in EnemyUnit.All)
				{
					if (item.fieldMode && !item.dead)
					{
						AliveCount++;
					}
				}
				saveTimer += deltaTime;
				if (saveTimer > 20f)
				{
					saveTimer = 0f;
					Profile.Save();
				}
			}
		}
	}

	private static double EnvironmentInterval()
	{
		int val = ((GameServer.Local == null) ? 2500 : GameConfigDao.GetInt(GameServer.Local.Db, "map_damage_overtime_interval", 2500));
		int val2 = ((GameServer.Local == null) ? 2 : GameConfigDao.GetInt(GameServer.Local.Db, "map_damage_overtime_count", 2));
		return (double)(Math.Max(1, val) * Math.Max(1, val2)) / 1000.0;
	}

	public static bool FieldProtection(int mapId, int returnMap, int protection, bool coldEquipment)
	{
		bool flag = (returnMap == 211000000 && mapId != 200082300) || returnMap == 193000000;
		switch (protection)
		{
		case 2:
			if (returnMap != 230000000)
			{
				return mapId == 200082300;
			}
			return true;
		default:
			return false;
		case 1:
			return flag;
		case 0:
			return coldEquipment & flag;
		}
	}

	public static bool IsEnvironmentalEquipment(InventoryEntry item)
	{
		if (item != null && item.inventoryType == 1)
		{
			return item.position < 0;
		}
		return false;
	}

	public void TickEnvironment(PlayerCtrl player, double now)
	{
		if (Map == null || Map.decHp <= 0 || (Object)(object)player == (Object)null || player.hp <= 0f || now < nextEnvironmentDamage)
		{
			return;
		}
		nextEnvironmentDamage = now + EnvironmentInterval();
		bool flag = false;
		bool flag2 = false;
		if (GameSession.InGame)
		{
			foreach (InventoryEntry item in GameSession.Character.inventory)
			{
				if (IsEnvironmentalEquipment(item))
				{
					flag |= Map.protectItem > 0 && item.itemId == Map.protectItem;
					flag2 |= (item.flag & 4) != 0;
					continue;
				}
				for (int num = -1; num >= -59; num--)
				{
					flag |= Map.protectItem > 0 && Profile.EquipOfSlot(num) == Map.protectItem;
				}
			}
		}
		int protection = ((Component)player).GetComponent<NativeBuffs>()?.Value("MAP_PROTECTION") ?? 0;
		if (!flag && !FieldProtection(Map.id, Map.returnMap, protection, flag2))
		{
			player.hp = Mathf.Max(0f, player.hp - (float)Map.decHp);
			if (player.hp <= 0f)
			{
				GameLoop.I?.OnPlayerDead();
			}
		}
	}

	private void TickBots()
	{
		//IL_030c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		if (Map == null || GameServer.Local == null || !GameSession.InGame || (Object)(object)GameLoop.I == (Object)null || (Object)(object)GameLoop.I.player == (Object)null || !BotWarmup.Ready("Bot/Items") || !BotWarmup.Ready("Bot/SoloMapling") || !BotWarmup.Ready("Bot/Maps") || !BotWarmup.Ready("Bot/ReactorTemplates") || !BotWarmup.Ready("Bot/ReactorDrops"))
		{
			return;
		}
		PlayerCtrl player = GameLoop.I.player;
		BotRuntime bots = GameServer.Local.Bots;
		if (hookedStartupError == null)
		{
			hookedStartupError = (Exception e) =>
			{
				FileLog.Note("[Bot/Environment] 启动步骤异常（已跳过）：\n" + e);
			};
			BotEnvironment.StartupError = hookedStartupError;
			PqDirector.LocalPlayerId = () => GameSession.CharacterId;
			PqDirector.LocalMapId = () => BotMapId;
			PqDirector.LiveMapId = (int num) => NativeEventScripts.ResolveMap(num)?.Scope ?? num;
			PqDirector.WarpLocalPlayer = (int target) =>
			{
				if (target > 0)
				{
					pendingWarpMap = -1;
					WarpTo(target);
				}
			};
			PqDirector.GrantItem = (int itemId, int count) =>
			{
				Profile.AddItem(itemId, count);
			};
			PqDirector.TalkNpc = (int npcId, bool openOnly, string[] prefer, Action<bool> done) =>
			{
				PqNpcDriver.Begin(npcId, openOnly ? PqNpcPolicy.OpenOnly : PqNpcPolicy.Complete, prefer, done);
			};
			PqDirector.BotTalkNpc = delegate(BotCharacter bot, int npcId, out string say)
			{
				return PqBotNpcDriver.Talk(bot, npcId, out say);
			};
			PqDirector.EnterInstance = (int botId) =>
			{
				NativeEventScripts.RegisterBotPlayer(botId);
			};
			PqDirector.LeaveInstance = (int botId) =>
			{
				NativeEventScripts.UnregisterBotPlayer(botId);
			};
			PqDirector.InstanceGrid = NativeEventScripts.GridOf;
			PqDirector.InstanceProperty = NativeEventScripts.InstanceProperty;
			PqDirector.PickupItem = (BotCharacter bot, int itemId, int max) => botPickup.PickNearby(bot, itemId, 120f, max);
			PqDirector.WalkToItem = (BotCharacter bot, int itemId, float searchRange) => botPickup.WalkTo(bot, itemId, 60f, searchRange);
			PqDirector.BotItem = (BotCharacter bot, int itemId, int count) =>
			{
				BotNpcApi.Gain(bot, itemId, count);
			};
			PqDirector.Notice = (string text) =>
			{
				NativeSystemMessage.Drop(5, text);
			};
		}
		if (bots.TryGetMap(BotMapId, out var _) == BotMapState.Pending)
		{
			if (!pendingMapLogged)
			{
				pendingMapLogged = true;
				FileLog.Note("[Bot/PQ] 地图 " + StateId + " 的几何分片后台解析中，本帧不铺新假人（不阻塞主线程）");
			}
			if ((Object)(object)botField == (Object)null)
			{
				botField = ((Component)this).GetComponent<BotField>() ?? ((Component)this).gameObject.AddComponent<BotField>();
			}
			botField.Synchronize(BotMapId, GameSession.World, GameSession.Channel);
			return;
		}
		pendingMapLogged = false;
		bots.SynchronizePlayer(GameSession.Character, BotMapId, ((Component)player).transform.position.x * 40f, (0f - ((Component)player).transform.position.y) * 40f, GameSession.Channel);
		PqDirector.OnLocalMap(BotMapId);
		if ((Object)(object)botCombat == (Object)null)
		{
			botCombat = ((Component)this).GetComponent<BotCombatField>() ?? ((Component)this).gameObject.AddComponent<BotCombatField>();
		}
		botCombat.Bind(this, bots);
		if ((Object)(object)groundLoot == (Object)null)
		{
			groundLoot = ((Component)this).GetComponent<GroundItemBridge>() ?? ((Component)this).gameObject.AddComponent<GroundItemBridge>();
		}
		groundLoot.Bind(this, bots);
		botPickup.Bind(bots);
		if (!grindHooked && BotTypeManager.PartyGrind != null)
		{
			grindHooked = true;
			BotPartyGrindService partyGrind = BotTypeManager.PartyGrind;
			partyGrind.FindLoot = (BotCharacter bot, float range) =>
			{
				BotGroundPickup.LootSpot lootSpot = botPickup.Nearest(bot, range);
				return new GrindLoot
				{
					found = lootSpot.found,
					x = lootSpot.x,
					y = lootSpot.y,
					distance = lootSpot.distance
				};
			};
			partyGrind.TakeLoot = (BotCharacter bot, float range, int max) => botPickup.PickAny(bot, range, max);
		}
		bots.Drive((long)(Time.realtimeSinceStartup * 1000f));
		view.SyncDynamicNpcs();
		if ((Object)(object)botField == (Object)null)
		{
			botField = ((Component)this).GetComponent<BotField>() ?? ((Component)this).gameObject.AddComponent<BotField>();
		}
		botField.Synchronize(BotMapId, GameSession.World, GameSession.Channel);
	}

	private void TickFrameLog()
	{
		bool flag = logPerf || FileLog.Enabled;
		if (BotPerf.Enabled != flag)
		{
			BotPerf.Enabled = flag;
			BotPerf.Sink = (flag ? new Action<string>(Debug.Log) : null);
		}
		bool flag2 = logFrames || FileLog.Enabled;
		if (!flag2 && !flag)
		{
			return;
		}
		float unscaledDeltaTime = Time.unscaledDeltaTime;
		if (unscaledDeltaTime > 0.05f)
		{
			hitchFrames++;
		}
		logClock += unscaledDeltaTime;
		if (!(logClock < 2f))
		{
			if (flag2)
			{
				Debug.Log((object)("[Bot/Frame] 2 秒内 " + hitchFrames + " 帧超 50ms，假人 " + BotRegistry.Count() + " 个，本帧 " + (unscaledDeltaTime * 1000f).ToString("F1") + "ms"));
			}
			if (flag)
			{
				BotPerf.Flush();
			}
			logClock = 0f;
			hitchFrames = 0;
		}
	}

	private void TickRespawn(float dt)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		if (frenzyExpires != 0L)
		{
			if (NowMs() >= frenzyExpires)
			{
				StopFrenzy();
			}
			else if ((Object)(object)frenzyImage == (Object)null)
			{
				frenzyImage = NativeImgAnimation.Spawn("Skill/000.img/skill/0001016/summon/stand", ((Component)this).transform, frenzyPosition, frenzyOrder, frenzyFacing > 0, repeat: true);
			}
		}
		respawnTimer -= dt;
		if (respawnTimer > 0f)
		{
			return;
		}
		respawnTimer = ((frenzyExpires != 0L) ? Mathf.Max(0.001f, 5f) : 10f);
		if (spawner == null)
		{
			return;
		}
		foreach (SpawnPoint item in spawner.Respawn(1))
		{
			if (bySpawn.TryGetValue(item, out var value) && ((Object)(object)value.mob == (Object)null || value.mob.dead))
			{
				SpawnAt(value);
			}
		}
	}

	private void CheckPortals(PlayerCtrl player, float dt)
	{
		if (Warping)
		{
			nearPortal = null;
			return;
		}
		if (player.IsClimbing)
		{
			nearPortal = null;
			portalArmed = false;
			return;
		}
		FieldPortal fieldPortal = (nearPortal = FindPortal(player, out var gated));
		if (fieldPortal != null && !fieldPortal.TouchTrigger && !PressUp())
		{
			portalArmed = true;
		}
		if (fieldPortal == null)
		{
			portalArmed = true;
			if (gated != null && portalLock <= 0f && PressUpEdge())
			{
				UsePortal(gated);
			}
		}
		else if (!(portalLock > 0f) && portalArmed && (fieldPortal.TouchTrigger || PressUp()))
		{
			portalArmed = false;
			UsePortal(fieldPortal);
		}
	}

	private FieldPortal FindPortal(PlayerCtrl player, out FieldPortal gated)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		gated = null;
		Vector2 feetPos = player.FeetPos;
		FieldPortal result = null;
		float num = float.MaxValue;
		foreach (FieldPortal portal in Map.portals)
		{
			if (!portal.Usable || (!portal.TouchTrigger && !portal.PressUpTrigger) || (portal.HasScript && BlockedPortalScripts.Contains(portal.script)))
			{
				continue;
			}
			float num2 = Map.WorldX(portal.x);
			float num3 = Map.WorldY(portal.y);
			float num4 = Mathf.Abs(feetPos.x - num2);
			if (num4 > 0.75f || Mathf.Abs(feetPos.y - num3) > 2f)
			{
				continue;
			}
			if (!PortalGate.Open(portal.script))
			{
				if (gated == null)
				{
					gated = portal;
				}
			}
			else if (num4 < num)
			{
				num = num4;
				result = portal;
			}
		}
		return result;
	}

	private static bool PressUp()
	{
		if (Input.GetKey((KeyCode)273) || Input.GetKey((KeyCode)119))
		{
			return true;
		}
		if (InputHub.Held(GameAction.Up))
		{
			return true;
		}
		if (Input.GetAxisRaw("Vertical") > 0.6f)
		{
			return true;
		}
		return TouchInput.MoveV > 0.6f;
	}

	private static bool PressUpEdge()
	{
		if (Input.GetKeyDown((KeyCode)273) || Input.GetKeyDown((KeyCode)119))
		{
			return true;
		}
		return InputHub.Pressed(GameAction.Up);
	}

	private void UsePortal(FieldPortal portal)
	{
		if (!Warping && (!portal.HasScript || !NpcSession.RunPortal(portal)))
		{
			StartWarp(portal);
		}
	}

	private void StartWarp(FieldPortal portal)
	{
		if (portal != null && Map != null && portal.tm == Map.id)
		{
			WarpInsideMap(portal);
			AudioManager.Play("Game/Portal", 0.7f, pitchVary: false);
			return;
		}
		warpTarget = portal;
		warpState = WarpState.Out;
		warpClock = 0f;
		PlayerCtrl.InputLocked = true;
		ScreenFade.Attach((MonoBehaviour)(object)this);
		ScreenFade.Set(0f);
		AudioManager.Play("Game/Portal", 0.7f, pitchVary: false);
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		if ((Object)(object)playerCtrl != (Object)null)
		{
			playerCtrl.invulnUntil = Time.unscaledTime + 0.7f + 0.2f;
		}
	}

	private static bool Advance(float from, float to, float duration, float elapsed)
	{
		float num = ((duration <= 0f) ? 1f : Mathf.Clamp01(elapsed / duration));
		ScreenFade.Set(Mathf.Lerp(from, to, num));
		return num >= 1f;
	}

	private void TickWarpFade()
	{
		warpClock += Time.unscaledDeltaTime;
		switch (warpState)
		{
		case WarpState.Out:
			if (!Advance(0f, 1f, 0.32f, warpClock))
			{
				break;
			}
			try
			{
				if (warpTarget == null)
				{
					int num = pendingWarpMap;
					pendingWarpMap = -1;
					string spawnPortalName = pendingWarpPortal;
					pendingWarpPortal = null;
					int spawnPortalIndex = pendingWarpPortalIndex;
					pendingWarpPortalIndex = -1;
					if (num < 0)
					{
						num = ((Map != null) ? Map.returnMap : 1000000);
					}
					if (num == 0)
					{
						try
						{
							FieldMap map = FieldCatalog.GetMap(0);
							if (map != null && map.returnMap > 0 && map.returnMap != 0)
							{
								num = map.returnMap;
							}
						}
						catch (Exception ex)
						{
							Debug.LogWarning((object)("[Field] 0 号过场图解析失败，仍按原目标进入：" + ex.Message));
						}
					}
					Enter(num, spawnPortalName, spawnPortalIndex);
				}
				else if (warpTarget.tm == Map.id)
				{
					WarpInsideMap(warpTarget);
				}
				else
				{
					Enter(warpTarget.tm, string.IsNullOrEmpty(warpTarget.targetName) ? null : warpTarget.targetName);
				}
				break;
			}
			catch (Exception ex2)
			{
				Debug.LogException(ex2);
				FileLog.Note("[Field/Warp] 换图 Enter 抛异常（已强制结束黑幕）：map=" + ((Map != null) ? Map.id.ToString() : "null") + " err=" + ex2.Message);
				break;
			}
			finally
			{
				warpState = WarpState.Hold;
				warpClock = 0f;
			}
		case WarpState.Hold:
			if (!(warpClock < 0.06f))
			{
				warpState = WarpState.In;
				warpClock = 0f;
			}
			break;
		case WarpState.In:
			if (Advance(1f, 0f, 0.32f, warpClock))
			{
				ScreenFade.Set(0f);
				warpState = WarpState.None;
				warpTarget = null;
				PlayerCtrl.InputLocked = false;
			}
			break;
		}
	}

	private void WarpInsideMap(FieldPortal portal)
	{
		FieldPortal target = Map.Portal(portal.targetName) ?? ((Map.portals.Count > 0) ? Map.portals[0] : null);
		PlaceInsideMap(target);
	}

	private void PlaceInsideMap(FieldPortal target)
	{
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		if (target != null && !((Object)(object)playerCtrl == (Object)null))
		{
			float num = Map.WorldX(target.x);
			playerCtrl.PlaceAt(num, PlayerCtrl.GroundAt(num, Map.WorldY(target.y)));
			if ((Object)(object)GameLoop.I.camRig != (Object)null)
			{
				GameLoop.I.camRig.SnapTo(num);
			}
			portalLock = 0.4f;
			portalArmed = false;
		}
	}

	public void OnEnemyKilled(EnemyUnit e)
	{
		if ((Object)(object)e == (Object)null)
		{
			return;
		}
		e.NativeController?.OnKilled();
		dynamicMobs.Remove(e);
		if (byMob.TryGetValue(((Object)e).GetInstanceID(), out var value))
		{
			byMob.Remove(((Object)e).GetInstanceID());
			value.mob = null;
			value.sp.OnKilled((long)((double)DeathAnimSeconds(e) * 1000.0), NowMs());
		}
		if (Map != null)
		{
			MarkWildBossKilled(Map.id, e.fieldMobId);
		}
		if (!FieldCatalog.Mobs.TryGetValue(e.fieldMobId, out var value2))
		{
			return;
		}
		int num = (int)Art.Num(value2.stats, "exp");
		num = (int)(e.expOverride ?? num);
		CommunityResponse communityResponse = (GameSession.InGame ? GameSession.Net.Community(new CommunityRequest
		{
			characterId = GameSession.CharacterId,
			operation = CommunityOperation.Read
		}) : new CommunityResponse());
		bool flag = true;
		int num2 = 1;
		if (e.HasBotContribution && GameSession.InGame && GameServer.Local != null)
		{
			BotRuntime bots = GameServer.Local.Bots;
			GameLoop i = GameLoop.I;
			object obj;
			if (i == null)
			{
				obj = null;
			}
			else
			{
				PlayerCtrl player = i.player;
				obj = ((player != null) ? ((Component)player).GetComponent<NativeBuffs>() : null);
			}
			NativeBuffs nativeBuffs = (NativeBuffs)obj;
			BotPartyExperience.Award award = BotPartyExperience.Calculate(GameServer.Local.Db, bots.parties, bots.Player(GameSession.CharacterId), (int)Art.Num(value2.stats, "level", 1f), num, (((Object)(object)e.NativeController != (Object)null) ? e.NativeController.MaximumHp : e.hpMax) + e.TotalHealed, e.DamageContributions, (float)(AdminSetItem.ApplyRate(EquipLoadout.CurrentSetBonus(), "ExpRate", communityResponse.expRate) * (double)(100 + (nativeBuffs?.Value("ExpRate") ?? 0)) / 100.0), (float)communityResponse.expRate, nativeBuffs?.Value("HOLY_SYMBOL") ?? 0, GameConfigDao.GetBool(GameServer.Local.Db, "use_full_holy_symbol"), nativeBuffs?.Value("EXP_INCREASE") ?? 0, nativeBuffs?.HasStat("EXP_BUFF") ?? false, e.NativeController?.Showdown ?? 0);
			num2 = award.reputationCalls;
			flag = award.eligible && (Object)(object)GameLoop.I?.player != (Object)null && GameLoop.I.player.hp > 0f;
			if ((Object)(object)GameLoop.I?.player != (Object)null && GameLoop.I.player.hp > 0f && award.eligible)
			{
				Profile.GrantExperience(award.personal + award.party);
			}
		}
		else if (num > 0)
		{
			Dictionary<string, int> bonus = EquipLoadout.CurrentSetBonus();
			GameLoop i2 = GameLoop.I;
			object obj2;
			if (i2 == null)
			{
				obj2 = null;
			}
			else
			{
				PlayerCtrl player2 = i2.player;
				obj2 = ((player2 != null) ? ((Component)player2).GetComponent<NativeBuffs>() : null);
			}
			NativeBuffs nativeBuffs2 = (NativeBuffs)obj2;
			bool full = GameServer.Local != null && GameConfigDao.GetBool(GameServer.Local.Db, "use_full_holy_symbol");
			float num3 = (float)num * BotPartyExperience.StatusMultiplier(nativeBuffs2?.Value("HOLY_SYMBOL") ?? 0, sharers: false, full, e.NativeController?.Showdown ?? 0);
			num3 *= (float)AdminSetItem.ApplyRate(bonus, "ExpRate", communityResponse.expRate);
			int num4 = nativeBuffs2?.Value("ExpRate") ?? 0;
			if (num4 != 0)
			{
				num3 *= (float)(100 + num4) / 100f;
			}
			num3 += (float)(nativeBuffs2?.Value("EXP_INCREASE") ?? 0);
			if (nativeBuffs2 != null && nativeBuffs2.HasStat("EXP_BUFF"))
			{
				num3 *= 2f;
			}
			Profile.GrantExperience((long)Math.Floor((double)num3 + 0.5));
		}
		int num5 = 0;
		while (GameSession.InGame && num5 < num2)
		{
			GameSession.Net.Community(new CommunityRequest
			{
				characterId = GameSession.CharacterId,
				operation = CommunityOperation.RecordKill,
				value = (int)Art.Num(value2.stats, "maxHP"),
				targetId = (int)Art.Num(value2.stats, "boss")
			});
			num5++;
		}
		if (flag)
		{
			QuestLogic.OnMobKilled(value2.id);
		}
		int num6 = (int)Art.Num(value2.stats, "getCP");
		if (num6 > 0 && NativeCarnivalHud.OnCarnivalMap(Profile.LastMapId))
		{
			NativeCarnivalHud.ReportKill(num6);
		}
		int dropped = 0;
		int mesosTotal = 0;
		DropLoot(e, value2, ref dropped, ref mesosTotal);
		if (logKills)
		{
			Debug.Log((object)("[Field] 击杀 " + value2.name + "(" + value2.id + ") 经验+" + num + " 掉落 " + dropped + " 件 金币 " + mesosTotal));
		}
		EventMap?.Killed(e, hasKiller: true);
	}

	public void OnEnemyExpired(EnemyUnit e)
	{
		if (!((Object)(object)e == (Object)null))
		{
			EventMap?.Killed(e, hasKiller: false);
			dynamicMobs.Remove(e);
			if (byMob.TryGetValue(((Object)e).GetInstanceID(), out var value))
			{
				byMob.Remove(((Object)e).GetInstanceID());
				value.mob = null;
				value.sp.OnKilled((long)(DeathAnimSeconds(e) * 1000f), NowMs());
			}
		}
	}

	private static float DeathAnimSeconds(EnemyUnit e)
	{
		if ((Object)(object)e.NativeController != (Object)null)
		{
			return (float)e.NativeController.ServerDeathMillis / 1000f;
		}
		return Art.GetClip("mobs", e.artKey, ((Object)(object)e.NativeController != (Object)null) ? ("die" + e.NativeController.DeathAction) : Art.Resolve("mobs", e.artKey, "die", "die1"))?.Duration ?? 0.6f;
	}

	private void DropLoot(EnemyUnit e, FieldMob mob, ref int dropped, ref int mesosTotal)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		Vector3 pos = ((Component)e).transform.position;
		int n = 0;
		int mesos = 0;
		CommunityResponse communityResponse = (GameSession.InGame ? GameSession.Net.Community(new CommunityRequest
		{
			characterId = GameSession.CharacterId,
			operation = CommunityOperation.Read
		}) : new CommunityResponse());
		double rate = communityResponse.dropRate;
		double mesoRate = communityResponse.mesoRate;
		if (e.IsBoss && GameSession.InGame)
		{
			rate = communityResponse.familyDropRate * GameSession.Net.Boss(new BossRequest
			{
				characterId = GameSession.CharacterId
			}).dropRate;
		}
		Dictionary<string, int> bonus = EquipLoadout.CurrentSetBonus();
		GameLoop i = GameLoop.I;
		object obj;
		if (i == null)
		{
			obj = null;
		}
		else
		{
			PlayerCtrl player = i.player;
			obj = ((player != null) ? ((Component)player).GetComponent<NativeBuffs>() : null);
		}
		NativeBuffs nativeBuffs = (NativeBuffs)obj;
		rate = AdminSetItem.ApplyRate(bonus, "DropRate", rate);
		int num = nativeBuffs?.Value("DropRate") ?? 0;
		if (num != 0)
		{
			rate = (float)(rate * (double)(100 + num) / 100.0);
		}
		int num2 = e.NativeController?.Showdown ?? 0;
		if (num2 != 0)
		{
			rate = (float)(rate * (1.0 + (double)num2 / 100.0));
		}
		mesoRate = AdminSetItem.ApplyRate(bonus, "MesoRate", mesoRate);
		int? mesoUp = ((nativeBuffs != null && nativeBuffs.HasStat("MESOUP")) ? new int?(nativeBuffs.Value("MESOUP")) : ((int?)null));
		FieldLoot.Roll(mob.id, (int itemId, int qty, int d) =>
		{
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			if (itemId == 0)
			{
				qty = RewardRules.MesoAmount(qty, mesoUp, mesoRate);
				mesos += qty;
			}
			n++;
			Vector3 val = pos + Offset(d);
			MapItemService.CalcDropPos(StateId, val.x * 40f, (0f - val.y) * 40f, pos.x * 40f, (0f - pos.y) * 40f, out var outX, out var outY);
			GroundItem.Spawn(itemId, qty, new Vector3(outX / 40f, (0f - outY) / 40f, 0f), null, pos);
		}, rng, rate, QuestLogic.NeedQuestItem);
		dropped += n;
		mesosTotal += mesos;
	}

	private static Vector3 Offset(int d)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return new Vector3(FieldLoot.OffsetPx(d) / 40f, 0f, 0f);
	}

	public EnemyUnit NearestAliveBoss(float playerX)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		EnemyUnit result = null;
		float num = float.MaxValue;
		foreach (Point point in points)
		{
			if (!((Object)(object)point.mob == (Object)null) && !point.mob.dead && point.sp != null && point.sp.Def.isBoss)
			{
				float num2 = Mathf.Abs(((Component)point.mob).transform.position.x * 40f - playerX);
				if (num2 < num)
				{
					num = num2;
					result = point.mob;
				}
			}
		}
		return result;
	}

	public void ClearMobs()
	{
		StopFrenzy();
		NativeBotBossCombat.Stop();
		foreach (EnemyUnit dynamicMob in dynamicMobs)
		{
			if ((Object)(object)dynamicMob != (Object)null)
			{
				((Component)dynamicMob).gameObject.SetActive(false);
				Object.Destroy((Object)(object)((Component)dynamicMob).gameObject);
			}
		}
		dynamicMobs.Clear();
		((Component)this).GetComponent<NativeBossEncounter>()?.Clear();
		foreach (Point point in points)
		{
			if ((Object)(object)point.mob != (Object)null)
			{
				((Component)point.mob).gameObject.SetActive(false);
				Object.Destroy((Object)(object)((Component)point.mob).gameObject);
				point.mob = null;
			}
		}
		points.Clear();
		byMob.Clear();
		bySpawn.Clear();
		spawner = null;
		AliveCount = 0;
	}

	public void ReloadCurrentMap()
	{
		if (Map == null || Warping)
		{
			throw new ArgumentException("地图正在切换");
		}
		int id = Map.id;
		ClearMobs();
		cachedFields.Remove(id);
		Map = null;
		FieldCatalog.ResetMap(id);
		NativeMapScripts component = ((Component)this).GetComponent<NativeMapScripts>();
		if ((Object)(object)component != (Object)null)
		{
			component.Reload();
		}
		Enter(id, null);
	}

	public void RemoveNpcView(int npcId)
	{
		view?.RemoveNpc(npcId);
	}

	public void WarpTo(int mapId, string portalName = null, int portalIndex = -1)
	{
		if (Warping || mapId < 0)
		{
			return;
		}
		if (Map != null && Map.id == mapId)
		{
			if (!string.IsNullOrEmpty(portalName) || portalIndex >= 0)
			{
				FieldPortal fieldPortal;
				if (string.IsNullOrEmpty(portalName))
				{
					fieldPortal = ((portalIndex >= 0 && portalIndex < Map.portals.Count) ? Map.portals[portalIndex] : null);
				}
				else
				{
					fieldPortal = Map.Portal(portalName);
				}
				PlaceInsideMap(fieldPortal ?? ((Map.portals.Count > 0) ? Map.portals[0] : null));
			}
		}
		else
		{
			pendingWarpMap = mapId;
			pendingWarpPortal = portalName;
			pendingWarpPortalIndex = portalIndex;
			StartWarp(null);
		}
	}

	public void TeleportLocalPlayer(float x, float y)
	{
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		if (!((Object)(object)playerCtrl == (Object)null) && Map != null)
		{
			float x2 = x / 40f;
			float groundY = (0f - y) / 40f;
			playerCtrl.PlaceAt(x2, groundY);
		}
	}

	public void RespawnPlayer()
	{
		if (!Warping)
		{
			StartWarp(null);
		}
	}

	public void Shutdown()
	{
		warpState = WarpState.None;
		warpTarget = null;
		deferredWarpMap = -1;
		warpClock = 0f;
		PlayerCtrl.InputLocked = false;
		ScreenFade.Dismiss();
		ClearMobs();
		GroundItem.Clear();
		NpcField.Clear();
		if ((Object)(object)botField == (Object)null)
		{
			botField = ((Component)this).GetComponent<BotField>();
		}
		botField?.Clear();
		NpcSession.CloseActive();
		NativeNpcShopWindow.Close();
		if ((Object)(object)view != (Object)null)
		{
			view.Clear();
		}
		Map = null;
		PlayerCtrl.SetTerrain(null);
		PlayerCtrl.Climbs = null;
		PlayerCtrl.FallFloor = float.NegativeInfinity;
		CameraRig cameraRig = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.camRig : null);
		if ((Object)(object)cameraRig != (Object)null)
		{
			cameraRig.ClearStage();
		}
		Backdrop backdrop = Object.FindObjectOfType<Backdrop>();
		if ((Object)(object)backdrop != (Object)null)
		{
			((Component)backdrop).gameObject.SetActive(true);
		}
	}
}
