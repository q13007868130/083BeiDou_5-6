using System;
using System.Collections.Generic;
using MapleDash.Js;
using Server.Field;
using UnityEngine;

public sealed class NativeEventMap
{
	public sealed class Mob
	{
		private readonly NativeEventMap map;

		public readonly int Id;

		public readonly int ObjectId;

		public readonly SpawnPoint Spawn;

		public readonly JsHostObject Host;

		public EnemyUnit Visible;

		public float X;

		public float Y;

		public long Hp;

		public long MaxHp;

		public bool Friendly;

		public long? ExpOverride;

		public int Fh;

		private long damageBlockedUntil;

		public bool Alive
		{
			get
			{
				if (!((Object)(object)Visible != (Object)null))
				{
					return Hp > 0;
				}
				return !Visible.dead;
			}
		}

		public NativeEventMap Map => map;

		public long CurrentHp
		{
			get
			{
				if (!((Object)(object)Visible != (Object)null))
				{
					return Hp;
				}
				return Visible.NativeController?.CurrentHp ?? Visible.hp;
			}
		}

		public Vector2 Position
		{
			get
			{
				//IL_002b: Unknown result type (might be due to invalid IL or missing references)
				//IL_0046: Unknown result type (might be due to invalid IL or missing references)
				//IL_0057: Unknown result type (might be due to invalid IL or missing references)
				//IL_001a: Unknown result type (might be due to invalid IL or missing references)
				if (!((Object)(object)Visible != (Object)null))
				{
					return new Vector2(X, Y);
				}
				return new Vector2(((Component)Visible).transform.position.x * 40f, (0f - ((Component)Visible).transform.position.y) * 40f);
			}
		}

		public bool Controlled => map.Instance.Property("seedControlled." + map.Id + "." + ObjectId) == "1";

		public Mob(NativeEventMap map, int id, int oid, float x, float y, long hp, SpawnPoint spawn, JsHostObject host = null)
		{
			Mob mob = this;
			this.map = map;
			Id = id;
			ObjectId = oid;
			X = x;
			Y = y;
			Hp = (MaxHp = Math.Max(1L, hp));
			Spawn = spawn;
			Friendly = Art.Num(FieldCatalog.Mobs[id].stats, "friendly") > 0f;
			Host = host ?? new JsHostObject(this);
			Host.Set("id", new JsNumber(Id));
			Host.Missing = (string key) =>
			{
				throw new JsRuntimeException("Unmigrated event monster API: " + key);
			};
			Bind(Host, "getId", (JsCall c) => new JsNumber(mob.Id));
			Bind(Host, "getObjectId", (JsCall c) => new JsNumber(mob.ObjectId));
			Bind(Host, "getMap", (JsCall c) => map.Host);
			Bind(Host, "getName", (JsCall c) => new JsString(WzNames.Mob(mob.Id)));
			Bind(Host, "isAlive", (JsCall c) => JsBoolean.Of(mob.Alive));
			Bind(Host, "getHp", (JsCall c) => new JsNumber((!((Object)(object)mob.Visible != (Object)null)) ? mob.Hp : (mob.Visible.NativeController?.CurrentHp ?? mob.Visible.hp)));
			Bind(Host, "getMaxHp", (JsCall c) => new JsNumber(mob.MaxHp));
			Bind(Host, "heal", (JsCall c) =>
			{
				long num = (long)JsConvert.ToNumber(c.Arg(0));
				if ((Object)(object)mob.Visible != (Object)null)
				{
					mob.Visible.NativeController?.Heal(num);
				}
				else
				{
					mob.Hp = Math.Max(0L, Math.Min(mob.MaxHp, mob.Hp + num));
				}
				return Done;
			});
			Bind(Host, "getPosition", (JsCall c) =>
			{
				//IL_0041: Unknown result type (might be due to invalid IL or missing references)
				//IL_0062: Unknown result type (might be due to invalid IL or missing references)
				return (!((Object)(object)mob.Visible != (Object)null)) ? NpcApi.BuildPoint(mob.X, mob.Y) : NpcApi.BuildPoint(((Component)mob.Visible).transform.position.x * 40f, (0f - ((Component)mob.Visible).transform.position.y) * 40f);
			});
			Bind(Host, "getFh", (JsCall c) => new JsNumber(mob.Fh));
			Bind(Host, "setFh", (JsCall c) =>
			{
				mob.Fh = Int(c);
				return Done;
			});
			Bind(Host, "setDamageBlockedUntil", (JsCall c) =>
			{
				mob.BlockDamageUntil((long)JsConvert.ToNumber(c.Arg(0)));
				return Done;
			});
			Bind(Host, "setHp", (JsCall c) =>
			{
				mob.SetHp((long)JsConvert.ToNumber(c.Arg(0)));
				return Done;
			});
			Bind(Host, "addHp", (JsCall c) =>
			{
				mob.SetHp(mob.CurrentHp + (long)JsConvert.ToNumber(c.Arg(0)));
				return Done;
			});
			Bind(Host, "setPosition", (JsCall c) =>
			{
				if (!(c.Arg(0) is JsHostObject jsHostObject))
				{
					return Done;
				}
				mob.X = (float)JsConvert.ToNumber(jsHostObject.Get("x"));
				mob.Y = (float)JsConvert.ToNumber(jsHostObject.Get("y"));
				mob.Host.Set("position", jsHostObject);
				if ((Object)(object)mob.Visible != (Object)null)
				{
					mob.Visible.ScriptMoveTo(mob.X, mob.Y, flying: false);
				}
				return Done;
			});
			Bind(Host, "changeDifficulty", (JsCall c) =>
			{
				mob.ChangeDifficulty(Int(c), JsConvert.ToBoolean(c.Arg(1)));
				return Done;
			});
		}

		private void ChangeDifficulty(int difficulty, bool pqMob)
		{
			(long, long)? tuple = ScriptMonster.ChangeableStats(Id, difficulty, pqMob);
			if (tuple.HasValue)
			{
				MaxHp = Math.Max(1L, tuple.Value.Item1);
				SetHp(MaxHp);
				ExpOverride = tuple.Value.Item2;
				if ((Object)(object)Visible != (Object)null)
				{
					Visible.expOverride = tuple.Value.Item2;
					Visible.NativeController?.RestoreFieldHp(MaxHp);
				}
			}
		}

		public void SetHp(long value)
		{
			value = Math.Max(0L, Math.Min(MaxHp, value));
			Hp = value;
			Visible?.NativeController?.RestoreFieldHp(value);
		}

		public void BlockDamageUntil(long timestamp)
		{
			damageBlockedUntil = timestamp;
			long num = timestamp - NativeEventScripts.Clock();
			if (num > 0)
			{
				Visible?.NativeController?.BlockFor((int)num);
			}
		}

		internal void ApplyTemplate(ScriptMonster template)
		{
			if (template.FhSet)
			{
				Fh = template.Fh;
			}
			damageBlockedUntil = template.DamageBlockedUntil;
			if (template.HpOverride >= 0)
			{
				Hp = Math.Max(0L, Math.Min(MaxHp, template.HpOverride));
			}
			if (template.DifficultyStats.HasValue)
			{
				MaxHp = Math.Max(1L, template.DifficultyStats.Value.hp);
				Hp = MaxHp;
				ExpOverride = template.DifficultyStats.Value.exp;
			}
		}

		public void ScriptMove(float x, float y, bool flying)
		{
			X = x;
			Y = y;
			if ((Object)(object)Visible != (Object)null)
			{
				Visible.ScriptMoveTo(x, y, flying);
			}
		}

		public void Render(FieldDirector field)
		{
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			if (!((Object)(object)Visible != (Object)null))
			{
				Visible = field.SpawnNativeMob(Id, new Vector3(X / 40f, (0f - Y) / 40f, 0f), ground: false, floating: false, eventOwned: true);
				if ((Object)(object)Visible == (Object)null)
				{
					throw new JsRuntimeException("Cannot render event monster: " + Id);
				}
				Visible.ScriptControlProbe = () => Controlled;
				Visible.NativeController?.RestoreFieldHp(Hp);
				if (ExpOverride.HasValue)
				{
					Visible.expOverride = ExpOverride.Value;
				}
				long num = damageBlockedUntil - NativeEventScripts.Clock();
				if (num > 0)
				{
					Visible.NativeController?.BlockFor((int)num);
				}
			}
		}

		public void Detach()
		{
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			if (!((Object)(object)Visible == (Object)null))
			{
				Hp = (Visible.dead ? 0 : (Visible.NativeController?.CurrentHp ?? Visible.hp));
				X = ((Component)Visible).transform.position.x * 40f;
				Y = (0f - ((Component)Visible).transform.position.y) * 40f;
				Visible = null;
			}
		}
	}

	private static int nextScope = -1000000;

	private static int nextMob = 1500000000;

	private static readonly List<NativeEventMap> live = new List<NativeEventMap>();

	public readonly int Id;

	public readonly int Scope;

	public readonly NativeEventScriptService.Instance Instance;

	public readonly JsHostObject Host;

	public readonly FieldMap Template;

	public readonly MapRuntime Geometry;

	private readonly List<Mob> mobs = new List<Mob>();

	private readonly Dictionary<MapReactor, JsHostObject> reactorHosts = new Dictionary<MapReactor, JsHostObject>();

	private readonly HashSet<EnemyUnit> expiring = new HashSet<EnemyUnit>();

	private MapSpawner spawner;

	private FieldDirector renderer;

	private bool disposed;

	public bool allowSummonState = true;

	private static readonly Random rng = new Random();

	private readonly List<string> effectLog = new List<string>();

	private static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

	private static JsValue Done => JsUndefined.Value;

	public int Count
	{
		get
		{
			int num = 0;
			foreach (Mob mob in mobs)
			{
				if (mob.Alive)
				{
					num++;
				}
			}
			return num;
		}
	}

	public List<Mob> AllMobs => new List<Mob>(mobs);

	public IReadOnlyList<string> EffectLog => effectLog;

	public string LastPacket { get; private set; }

	public string LastEffect { get; private set; }

	public string LastMessage { get; private set; }

	public static Mob FindByHost(JsValue host)
	{
		if (host == null)
		{
			return null;
		}
		foreach (NativeEventMap item in live)
		{
			if (item.disposed)
			{
				continue;
			}
			foreach (Mob mob in item.mobs)
			{
				if (mob.Host == host)
				{
					return mob;
				}
			}
		}
		return null;
	}

	public static Mob FindByVisible(EnemyUnit unit)
	{
		if ((Object)(object)unit == (Object)null)
		{
			return null;
		}
		foreach (NativeEventMap item in live)
		{
			if (item.disposed)
			{
				continue;
			}
			foreach (Mob mob in item.mobs)
			{
				if (mob.Visible == unit)
				{
					return mob;
				}
			}
		}
		return null;
	}

	private static int Int(JsCall c, int index = 0)
	{
		return JsConvert.ToInt32(c.Arg(index));
	}

	private static void Bind(JsHostObject host, string name, Func<JsCall, JsValue> action)
	{
		host.Set(name, JsFunction.FromNative(name, action));
	}

	public NativeEventMap(NativeEventScriptService.Instance instance, int id)
	{
		Instance = instance;
		Id = id;
		Scope = nextScope--;
		FieldMap previous = FieldCatalog.GetMap(id) ?? throw new JsRuntimeException("Missing event map: " + id);
		Template = NativeMapView.LoadMap(previous);
		MapRuntime map = GameServer.Local.Bots.GetMap(id);
		Geometry = new MapRuntime
		{
			mapId = Scope,
			templateMapId = Id,
			returnMap = map.returnMap,
			fieldLimit = map.fieldLimit,
			left = map.left,
			right = map.right,
			top = map.top,
			bottom = map.bottom,
			swim = map.swim,
			everlast = map.everlast,
			footholdSpeed = map.footholdSpeed,
			minX = map.minX,
			maxX = map.maxX,
			minY = map.minY,
			maxY = map.maxY
		};
		Geometry.footholds.AddRange(map.footholds);
		Geometry.ropes.AddRange(map.ropes);
		foreach (MapNpcPosition npc in map.npcs)
		{
			if (!npc.dynamic)
			{
				Geometry.npcs.Add(new MapNpcPosition
				{
					id = npc.id,
					x = npc.x,
					y = npc.y
				});
			}
		}
		Geometry.reactors.AddRange(map.reactors);
		foreach (MapPortal portal in map.portals)
		{
			Geometry.portals.Add(new MapPortal
			{
				id = portal.id,
				x = portal.x,
				y = portal.y,
				type = portal.type,
				targetMap = portal.targetMap,
				name = portal.name,
				targetName = portal.targetName,
				script = portal.script,
				enabled = portal.enabled
			});
		}
		MapService.Register(Geometry);
		live.Add(this);
		InitialSpawn();
		Host = new JsHostObject(this);
		Host.Missing = (string key) =>
		{
			throw new JsRuntimeException("Unmigrated event map API: " + key);
		};
		Bind(Host, "getId", (JsCall c) => new JsNumber(Id));
		Bind(Host, "getMapId", (JsCall c) => new JsNumber(Id));
		Bind(Host, "getFieldLimit", (JsCall c) => new JsNumber(Geometry.fieldLimit));
		Bind(Host, "getMapName", (JsCall c) => new JsString(WzNames.Map(Id)));
		Bind(Host, "resetPQ", (JsCall c) =>
		{
			Reset();
			return Done;
		});
		Bind(Host, "resetFully", (JsCall c) =>
		{
			Reset();
			return Done;
		});
		Bind(Host, "allowSummonState", (JsCall c) =>
		{
			allowSummonState = JsConvert.ToBoolean(c.Arg(0));
			return Done;
		});
		Bind(Host, "warpEveryone", (JsCall c) =>
		{
			int mapId = Int(c);
			NpcSession npcSession = NpcSession.MapContext();
			foreach (JsValue item2 in PlayerList().Items)
			{
				_ = item2;
				npcSession.RequestWarp(mapId, null, (c.ArgCount > 1) ? Int(c, 1) : (-1));
			}
			return Done;
		});
		Bind(Host, "getPortal", (JsCall c) => Portal(c.Arg(0)));
		Bind(Host, "countMonsters", (JsCall c) => new JsNumber(Count));
		Bind(Host, "getSpawnedMonstersOnMap", (JsCall c) => new JsNumber(Count));
		Bind(Host, "getAllMonsters", (JsCall c) => MonsterList());
		Host.Set("getMonsters", Host.Get("getAllMonsters"));
		Host.Set("getMobs", Host.Get("getAllMonsters"));
		Bind(Host, "getMonsterById", (JsCall c) => (JsValue)(((object)Find((Mob m) => m.Id == Int(c))?.Host) ?? ((object)JsNull.Value)));
		Bind(Host, "getMonsterByOid", (JsCall c) => (JsValue)(((object)Find((Mob m) => m.ObjectId == Int(c))?.Host) ?? ((object)JsNull.Value)));
		Bind(Host, "killAllMonsters", (JsCall c) =>
		{
			KillAll();
			return Done;
		});
		Bind(Host, "killMonster", (JsCall c) =>
		{
			Mob mob = mobs.Find((Mob m) => m.Host == c.Arg(0));
			if (mob == null)
			{
				return Done;
			}
			Expire(mob, !(c.Arg(1) is JsNull) && !(c.Arg(1) is JsUndefined));
			return Done;
		});
		Bind(Host, "instanceMapRespawn", (JsCall c) =>
		{
			Respawn();
			return Done;
		});
		Bind(Host, "spawnMonsterOnGroundBelow", (JsCall c) =>
		{
			JsHostObject jsHostObject = (c.Arg(0) as JsHostObject) ?? throw new JsRuntimeException("Expected event monster");
			int id2 = JsConvert.ToInt32(jsHostObject.Get("id"));
			JsHostObject jsHostObject2 = c.Arg(1) as JsHostObject;
			float x = ((jsHostObject2 != null) ? ((float)JsConvert.ToNumber(jsHostObject2.Get("x"))) : ((float)Int(c, 1)));
			float y = ((jsHostObject2 != null) ? ((float)JsConvert.ToNumber(jsHostObject2.Get("y"))) : ((float)Int(c, 2)));
			Spawn(id2, x, MapService.SnapToGround(Scope, x, y), null, jsHostObject);
			return Done;
		});
		Bind(Host, "spawnMonster", (JsCall c) =>
		{
			JsHostObject jsHostObject = (c.Arg(0) as JsHostObject) ?? throw new JsRuntimeException("Expected event monster");
			JsHostObject jsHostObject2 = jsHostObject.Get("position") as JsHostObject;
			Spawn(JsConvert.ToInt32(jsHostObject.Get("id")), (jsHostObject2 != null) ? ((float)JsConvert.ToNumber(jsHostObject2.Get("x"))) : 0f, (jsHostObject2 != null) ? ((float)JsConvert.ToNumber(jsHostObject2.Get("y"))) : 0f, null, jsHostObject);
			return Done;
		});
		Bind(Host, "broadcastMessage", (JsCall c) =>
		{
			Broadcast(c.Arg(c.ArgCount - 1));
			return Done;
		});
		Bind(Host, "dropMessage", (JsCall c) =>
		{
			DropMapMessage(Int(c), JsConvert.ToString(c.Arg(1)));
			return Done;
		});
		Bind(Host, "broadcastStringMessage", (JsCall c) =>
		{
			DropMapMessage(Int(c), JsConvert.ToString(c.Arg(1)));
			return Done;
		});
		Bind(Host, "getAllPlayers", (JsCall c) => PlayerList());
		Host.Set("getCharacters", Host.Get("getAllPlayers"));
		Host.Set("getPlayers", Host.Get("getAllPlayers"));
		Bind(Host, "spawnAllMonsterIdFromMapSpawnList", (JsCall c) =>
		{
			if (spawner == null)
			{
				return Done;
			}
			int mobId = Int(c);
			foreach (SpawnPoint item3 in spawner.SpawnPointsOf(mobId))
			{
				Spawn(item3.MobId, item3.Def.x, item3.Def.y, item3);
			}
			return Done;
		});
		Bind(Host, "movePlayer", (JsCall c) =>
		{
			if (!(c.Arg(1) is JsHostObject jsHostObject))
			{
				return Done;
			}
			MovePlayerTo((float)JsConvert.ToNumber(jsHostObject.Get("x")), (float)JsConvert.ToNumber(jsHostObject.Get("y")));
			return Done;
		});
		Bind(Host, "getEventInstance", (JsCall c) => Instance.Host);
		Bind(Host, "spawnItemDrop", (JsCall c) =>
		{
			if (!((c.Arg(2) as JsHostObject)?.Host is NativeScriptItem nativeScriptItem))
			{
				throw new JsRuntimeException("spawnItemDrop requires org.gms.client.inventory.Item");
			}
			JsHostObject jsHostObject = c.Arg(3) as JsHostObject;
			float x = ((jsHostObject != null) ? ((float)JsConvert.ToNumber(jsHostObject.Get("x"))) : 0f);
			float y = ((jsHostObject != null) ? ((float)JsConvert.ToNumber(jsHostObject.Get("y"))) : 0f);
			int dropperId = ObjectIdOf(c.Arg(0));
			int ownerId = JsConvert.ToInt32((c.Arg(1) as JsHostObject)?.Get("getId") ?? new JsNumber(0.0));
			InventoryEntry item = new InventoryEntry
			{
				itemId = nativeScriptItem.Id,
				inventoryType = nativeScriptItem.Id / 1000000,
				quantity = nativeScriptItem.Quantity,
				owner = (nativeScriptItem.Owner ?? ""),
				giftFrom = (nativeScriptItem.GiftFrom ?? ""),
				expiration = nativeScriptItem.Expiration
			};
			MapItemService.SpawnItemDrop(Scope, dropperId, ownerId, 0, item, x, y, (byte)(JsConvert.ToBoolean(c.Arg(4)) ? 2u : 0u), JsConvert.ToBoolean(c.Arg(5)));
			return Done;
		});
		Bind(Host, "shuffleReactors", (JsCall c) =>
		{
			ShuffleReactors();
			return Done;
		});
		Bind(Host, "getReactorByName", (JsCall c) =>
		{
			string name = JsConvert.ToString(c.Arg(0));
			MapReactor mapReactor = MapReactorService.Get(Scope).Find((MapReactor r) => r.name == name);
			return (mapReactor != null) ? ((JsValue)ReactorHost(mapReactor)) : ((JsValue)JsNull.Value);
		});
		Bind(Host, "getReactors", (JsCall c) =>
		{
			JsArray jsArray = new JsArray();
			foreach (MapReactor item4 in MapReactorService.Get(Scope))
			{
				jsArray.Items.Add(ReactorHost(item4));
			}
			BindArray(jsArray);
			return jsArray;
		});
		Bind(Host, "getFootholds", (JsCall c) =>
		{
			JsHostObject jsHostObject = new JsHostObject(Geometry);
			Bind(jsHostObject, "findBelow", (JsCall call) =>
			{
				if (!(call.Arg(0) is JsHostObject jsHostObject2))
				{
					return JsNull.Value;
				}
				float num = (float)JsConvert.ToNumber(jsHostObject2.Get("x"));
				float num2 = (float)JsConvert.ToNumber(jsHostObject2.Get("y"));
				FootholdSegment best = null;
				float num3 = float.MaxValue;
				foreach (FootholdSegment foothold in Geometry.footholds)
				{
					if (!foothold.Wall && !(num < Math.Min(foothold.x1, foothold.x2)) && !(num > Math.Max(foothold.x1, foothold.x2)))
					{
						float num4 = foothold.y1 + (foothold.y2 - foothold.y1) * (num - foothold.x1) / (foothold.x2 - foothold.x1);
						if (num4 >= num2 && num4 < num3)
						{
							num3 = num4;
							best = foothold;
						}
					}
				}
				if (best != null)
				{
					JsHostObject jsHostObject3 = new JsHostObject(best);
					Bind(jsHostObject3, "getId", (JsCall _) => new JsNumber(best.id));
					return jsHostObject3;
				}
				return JsNull.Value;
			});
			return jsHostObject;
		});
	}

	private Mob Find(Predicate<Mob> matches)
	{
		return mobs.Find((Mob m) => m.Alive && matches(m));
	}

	private void ShuffleReactors()
	{
		List<MapReactor> list = MapReactorService.Get(Scope);
		List<(int, int)> list2 = new List<(int, int)>();
		foreach (MapReactor item in list)
		{
			list2.Add((item.x, item.y));
		}
		for (int num = list2.Count - 1; num > 0; num--)
		{
			int num2 = rng.Next(num + 1);
			int index = num;
			List<(int, int)> list3 = list2;
			int index2 = num2;
			(int, int) tuple = list2[num2];
			(int, int) tuple2 = list2[num];
			(int, int) tuple3 = (list2[index] = tuple);
			tuple3 = (list3[index2] = tuple2);
		}
		for (int i = 0; i < list.Count; i++)
		{
			list[i].x = list2[i].Item1;
			list[i].y = list2[i].Item2;
			list[i].visualRevision++;
		}
	}

	public Mob FindAliveById(int mobId)
	{
		return Find((Mob m) => m.Id == mobId);
	}

	public Mob FindByOid(int objectId)
	{
		return Find((Mob m) => m.ObjectId == objectId);
	}

	public void Kill(Mob mob)
	{
		Expire(mob, hasKiller: false);
	}

	public void SpawnOnGround(int mobId, float x, float y)
	{
		Spawn(mobId, x, MapService.SnapToGround(Scope, x, y), null);
	}

	public Mob SpawnAt(int mobId, float x, float y, bool ground)
	{
		return Spawn(mobId, x, ground ? MapService.SnapToGround(Scope, x, y) : y, null);
	}

	public void BroadcastEffect(string path)
	{
		LastPacket = "effect:" + path;
		LastEffect = path;
		RecordEffect(path);
		if ((Object)(object)renderer != (Object)null)
		{
			NativeScriptEffect.Play(path);
		}
	}

	public void Broadcast(JsValue packet)
	{
		string text = NpcApi.DescribeScriptPacket(packet);
		if (text != null)
		{
			LastPacket = text;
			if (text.StartsWith("effect:"))
			{
				RecordEffect(text.Substring(7));
			}
		}
		if ((Object)(object)renderer != (Object)null)
		{
			NpcApi.ApplyScriptPacket(packet);
		}
	}

	private void RecordEffect(string path)
	{
		if (effectLog.Count < 256)
		{
			effectLog.Add(path);
		}
	}

	public void DropMapMessage(int type, string text)
	{
		LastMessage = text;
		if ((Object)(object)renderer != (Object)null)
		{
			NativeSystemMessage.Drop(type, text);
		}
	}

	public void DamagePlayerPacket(long damage, int mobId, int objectId)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)renderer == (Object)null))
		{
			PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
			if (!((Object)(object)playerCtrl == (Object)null))
			{
				DamageText.Show(Vector2.op_Implicit(playerCtrl.FeetPos) + Vector3.up * 0.6f, damage);
			}
		}
	}

	private void MovePlayerTo(float x, float y)
	{
		if (!((Object)(object)renderer == (Object)null))
		{
			renderer.TeleportLocalPlayer(x, y);
		}
	}

	private JsArray PlayerList()
	{
		JsArray jsArray = new JsArray();
		if ((Object)(object)renderer != (Object)null)
		{
			jsArray.Items.Add(NpcApi.BuildPlayer(NpcSession.MapContext()));
		}
		BindArray(jsArray);
		return jsArray;
	}

	private JsArray MonsterList()
	{
		JsArray jsArray = new JsArray();
		foreach (Mob mob in mobs)
		{
			if (mob.Alive)
			{
				jsArray.Items.Add(mob.Host);
			}
		}
		BindArray(jsArray);
		return jsArray;
	}

	private static void BindArray(JsArray list)
	{
		list.Set("size", JsFunction.FromNative("size", (JsCall c) => new JsNumber(list.Length)));
		list.Set("get", JsFunction.FromNative("get", (JsCall c) => list.GetIndex(Int(c))));
	}

	private static int ObjectIdOf(JsValue value)
	{
		JsHostObject jsHostObject = value as JsHostObject;
		object obj = jsHostObject?.Host;
		if (!(obj is Mob mob))
		{
			if (obj is MapReactor mapReactor)
			{
				return mapReactor.objectId;
			}
			if (jsHostObject != null && jsHostObject.Get("getObjectId") is JsFunction { Native: var native })
			{
				return JsConvert.ToInt32(native(new JsCall
				{
					This = value,
					Args = Array.Empty<JsValue>()
				}));
			}
			return JsConvert.ToInt32(value);
		}
		return mob.ObjectId;
	}

	private JsHostObject ReactorHost(MapReactor reactor)
	{
		if (reactorHosts.TryGetValue(reactor, out var value))
		{
			return value;
		}
		JsHostObject jsHostObject = new JsHostObject(reactor);
		jsHostObject.Missing = (string key) =>
		{
			throw new JsRuntimeException("Unmigrated event reactor API: " + key);
		};
		Bind(jsHostObject, "getId", (JsCall c) => new JsNumber(reactor.id));
		Bind(jsHostObject, "getObjectId", (JsCall c) => new JsNumber(reactor.objectId));
		Bind(jsHostObject, "getState", (JsCall c) => new JsNumber(reactor.state));
		Bind(jsHostObject, "getPosition", (JsCall c) => NpcApi.BuildPoint(reactor.x, reactor.y));
		Bind(jsHostObject, "getName", (JsCall c) => new JsString(reactor.name));
		Bind(jsHostObject, "resetReactorActions", (JsCall c) =>
		{
			MapReactorService.ResetActions(Scope, reactor, Int(c));
			return Done;
		});
		reactorHosts[reactor] = jsHostObject;
		return jsHostObject;
	}

	public JsHostObject ReactorFor(MapReactor reactor)
	{
		if (reactor != null)
		{
			return ReactorHost(reactor);
		}
		return null;
	}

	public JsValue Portal(JsValue key)
	{
		int index = ((key is JsString) ? Geometry.portals.FindIndex((MapPortal p) => p.name == JsConvert.ToString(key)) : JsConvert.ToInt32(key));
		if (index < 0 || index >= Geometry.portals.Count)
		{
			return JsNull.Value;
		}
		MapPortal portal = Geometry.portals[index];
		JsHostObject jsHostObject = new JsHostObject(portal);
		Bind(jsHostObject, "getId", (JsCall c) => new JsNumber(index));
		Bind(jsHostObject, "getName", (JsCall c) => new JsString(portal.name));
		Bind(jsHostObject, "getPosition", (JsCall c) => NpcApi.BuildPoint(portal.x, portal.y));
		return jsHostObject;
	}

	public void Reset()
	{
		if (disposed)
		{
			throw new JsRuntimeException("Disposed event map: " + Id);
		}
		KillAll();
		MapItemService.ClearMap(Scope);
		foreach (MapReactor item in MapReactorService.Get(Scope))
		{
			MapReactorService.Respawn(Scope, item);
		}
		spawner = new MapSpawner
		{
			Clock = () => Now,
			FullRespawn = true
		};
		spawner.SetMobInterval(Template.createMobInterval);
		spawner.SetRandomRefresh(v: true);
		foreach (FieldSpawn spawn in Template.spawns)
		{
			FieldCatalog.LoadMob(spawn.mob);
			SpawnPoint spawnPoint = spawner.AddMonsterSpawn(new SpawnPointDef
			{
				mob = spawn.mob,
				mobTime = spawn.mobTime,
				fh = spawn.fh,
				f = spawn.dir,
				team = spawn.team,
				x = spawn.x,
				y = MapService.SnapToGround(Scope, spawn.x, spawn.y),
				rx0 = spawn.rx0,
				rx1 = spawn.rx1,
				cy = (int)spawn.cy,
				isBoss = (FieldCatalog.Mobs.TryGetValue(spawn.mob, out var value) && Art.Num(value.stats, "boss") > 0f)
			});
			if (spawn.mobTime == -1)
			{
				Spawn(spawnPoint.MobId, spawnPoint.Def.x, spawnPoint.Def.y, spawnPoint);
			}
		}
	}

	private void InitialSpawn()
	{
		Reset();
		foreach (SpawnPoint allSpawnPoint in spawner.AllSpawnPoints)
		{
			if (allSpawnPoint.ShouldForceSpawn())
			{
				Spawn(allSpawnPoint.MobId, allSpawnPoint.Def.x, allSpawnPoint.Def.y, allSpawnPoint);
			}
		}
	}

	private Mob Spawn(int id, float x, float y, SpawnPoint spawn, JsHostObject host = null)
	{
		FieldCatalog.LoadMob(id);
		if (!FieldCatalog.Mobs.TryGetValue(id, out var value))
		{
			throw new JsRuntimeException("Missing event monster: " + id);
		}
		Mob mob = new Mob(this, id, nextMob++, x, y, NativeCombatData.Long(value.stats, "maxHP", 1L), spawn, host);
		if (host?.Host is ScriptMonster template)
		{
			mob.ApplyTemplate(template);
		}
		mobs.Add(mob);
		spawn?.MarkSpawned(Now);
		if (host != null)
		{
			Instance.RegisterMonster(mob.Host, mob.Friendly);
		}
		if ((Object)(object)renderer != (Object)null)
		{
			mob.Render(renderer);
		}
		return mob;
	}

	public EnemyUnit SpawnDynamic(int id, Vector3 position, bool ground)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		float x = position.x * 40f;
		float num = (0f - position.y) * 40f;
		return Spawn(id, x, ground ? MapService.SnapToGround(Scope, x, num) : num, null).Visible;
	}

	public void Respawn()
	{
		if (disposed || spawner == null)
		{
			return;
		}
		foreach (SpawnPoint item in spawner.Respawn(Instance.PlayerCount))
		{
			Spawn(item.MobId, item.Def.x, item.Def.y, item);
		}
	}

	public void Attach(FieldDirector field)
	{
		renderer = field;
		foreach (Mob mob in mobs)
		{
			if (mob.Alive)
			{
				mob.Render(field);
			}
		}
	}

	public void Detach()
	{
		foreach (Mob mob in mobs)
		{
			mob.Detach();
		}
		renderer = null;
	}

	public void Killed(EnemyUnit enemy, bool hasKiller)
	{
		if (!expiring.Contains(enemy))
		{
			Mob mob = mobs.Find((Mob m) => (Object)(object)m.Visible == (Object)(object)enemy);
			if (mob != null)
			{
				Remove(mob, hasKiller);
			}
		}
	}

	private void Expire(Mob mob, bool hasKiller)
	{
		EnemyUnit visible = mob.Visible;
		if ((Object)(object)visible != (Object)null)
		{
			expiring.Add(visible);
			try
			{
				visible.ExpireNative(1);
			}
			finally
			{
				expiring.Remove(visible);
			}
		}
		Remove(mob, hasKiller);
	}

	private void Remove(Mob mob, bool hasKiller)
	{
		if (mobs.Remove(mob))
		{
			mob.Hp = 0L;
			mob.Spawn?.OnKilled((mob.Visible?.NativeController?.ServerDeathMillis).GetValueOrDefault(), Now);
			if (hasKiller)
			{
				Instance.MonsterKilledBy(NpcApi.BuildPlayer(NpcSession.MapContext()), mob.Id, WzNames.Mob(mob.Id));
			}
			Instance.MonsterKilled(mob.Host, hasKiller, mob.Friendly);
		}
	}

	public void KillAll()
	{
		spawner?.CloseSpawnPoints();
		foreach (Mob item in new List<Mob>(mobs))
		{
			Expire(item, hasKiller: false);
		}
	}

	public void Dispose()
	{
		if (!disposed)
		{
			disposed = true;
			LucidBossCompat.Stop(this);
			Detach();
			mobs.Clear();
			live.Remove(this);
			MapItemService.ClearMap(Scope);
			MapReactorService.Clear(Scope);
			MapService.Unregister(Scope);
		}
	}
}
