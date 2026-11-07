using System;
using System.Collections.Generic;
using MapleDash.Js;
using Server.Bot;
using Server.Field;
using UnityEngine;

public sealed class NativeEventScripts : MonoBehaviour
{
	private static NativeEventScripts active;

	private NativeEventScriptService service;

	private object character;

	internal static Func<long> Clock = () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

	private readonly Dictionary<NativeEventScriptService.Instance, Dictionary<int, NativeEventMap>> instanceMaps = new Dictionary<NativeEventScriptService.Instance, Dictionary<int, NativeEventMap>>();

	private readonly List<string> pendingBossEvents = new List<string>();

	private bool bossEventsQueued;

	private static NativeEventScripts Current()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected Obj, but got Unknown
		NativeEventScripts host = active;
		if ((Object)(object)host == (Object)null)
		{
			GameObject val = new GameObject("[NativeEventScripts]");
			host = val.AddComponent<NativeEventScripts>();
			if (Application.isPlaying)
			{
				Object.DontDestroyOnLoad((Object)(object)val);
			}
			active = host;
		}
		if (host.service == null || host.character != GameSession.Character)
		{
			host.service?.Clear();
			host.ReleaseAllMaps();
			host.character = GameSession.Character;
			host.bossEventsQueued = false;
			host.pendingBossEvents.Clear();
			host.service = new NativeEventScriptService(ScriptSource.LoadKey, () => Clock(), (JsEngine engine) =>
			{
				JsHostObject java = NpcApi.BuildJava();
				engine.Interp.ResolveHostType = (string type) => GameServer.Local?.Scripts.ResolveHostType(type) ?? SeedTowerCompat.Resolve(type) ?? engine.Interp.Call(engine.Interp.GetMember(java, "type"), java, new JsValue[1]
				{
					new JsString(type)
				});
			});
			host.service.TransportationTime = (int time) => GameServer.Local.Scripts.TransportationTime(GameSession.World, time);
			MapReactorService.AllowHit = SeedTowerCompat.AllowReactorHit;
			host.service.LocalPlayerId = () => GameSession.CharacterId;
			host.service.BossTime = (long value) =>
			{
				float num = ((GameServer.Local == null) ? 1f : ((float)GameConfigDao.GetFloat(GameServer.Local.Db, "boss_respawn_mob_time_rate", 1.0)));
				if (num <= 0f || num > 1f)
				{
					num = 1f;
				}
				return (long)Math.Round((double)value * (double)num);
			};
			host.service.ChannelServer = () => NpcApi.BuildChannelServer(NpcApi.BuildPlayer(NpcSession.MapContext()));
			host.service.ClearEffect = (NativeEventScriptService.Instance instance, int mapId, bool gate) =>
			{
				NativeScreenEffect.Play("Map/Effect.img/quest/party/clear");
			};
			host.service.Error = (Exception error) =>
			{
				Debug.LogError((object)("[EventScript] " + error.Message));
			};
			host.service.MonsterFactory = (int id) => new ScriptMonster(id).Host();
			host.service.WarpMapFactory = (int id) => NpcApi.BuildWarpMap(id);
			host.service.WarpPlayer = (NativeEventScriptService.Instance instance, int mapId, JsValue player) =>
			{
				FieldDirector fieldDirector = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.field : null);
				if ((Object)(object)fieldDirector != (Object)null)
				{
					fieldDirector.WarpTo(mapId);
				}
				else
				{
					Profile.LastMapId = mapId;
				}
			};
			host.service.WrongEffect = (NativeEventScriptService.Instance instance, int mapId) =>
			{
				NativeScreenEffect.Play("Map/Effect.img/quest/party/wrong_kor");
			};
			host.service.InstanceMap = (NativeEventScriptService.Instance instance, int id) =>
			{
				if (!host.instanceMaps.TryGetValue(instance, out var value))
				{
					host.instanceMaps.Add(instance, value = new Dictionary<int, NativeEventMap>());
				}
				if (!value.TryGetValue(id, out var value2))
				{
					value.Add(id, value2 = new NativeEventMap(instance, id));
				}
				return value2.Host;
			};
			host.service.ReleaseMaps = (NativeEventScriptService.Instance instance) =>
			{
				if (host.instanceMaps.TryGetValue(instance, out var value))
				{
					foreach (NativeEventMap value3 in value.Values)
					{
						value3.Dispose();
					}
					host.instanceMaps.Remove(instance);
				}
			};
			host.service.SpawnNpc = (NativeEventScriptService.Instance instance, int id, JsValue position, JsValue target) =>
			{
				NativeEventMap nativeEventMap = (target as JsHostObject)?.Host as NativeEventMap;
				JsHostObject jsHostObject = position as JsHostObject;
				if (nativeEventMap == null || nativeEventMap.Instance != instance || jsHostObject == null)
				{
					throw new JsRuntimeException("Invalid event NPC map/position");
				}
				int num = JsConvert.ToInt32(jsHostObject.Get("x"));
				int num2 = JsConvert.ToInt32(jsHostObject.Get("y"));
				nativeEventMap.Geometry.npcs.Add(new MapNpcPosition
				{
					id = id,
					x = num,
					y = (int)MapService.SnapToGround(nativeEventMap.Scope, num, num2),
					dynamic = true
				});
			};
			host.service.Completed = (NativeEventScriptService.Instance instance) =>
			{
				if (GameServer.Local != null && GameSession.InGame && instance.MaximumPlayerCount == 1 && host.service.GetInstance(GameSession.CharacterId) == instance)
				{
					string fieldSetName = ((instance.EventName == "CygnusBattle") ? "cygnus01_" : instance.EventName);
					int num = NativeSoulWeaponWz2Service.OnBossClear(GameServer.Local.Db, GameSession.CharacterId, fieldSetName, solo: true);
					GameSession.Character.soulWeapon = NativeSoulWeaponWz2Service.Read(GameServer.Local.Db, GameSession.CharacterId);
					if (num > 0)
					{
						NativeSystemMessage.Drop(5, "[灵魂武器] 获得灵魂碎片 " + num + " 个。");
					}
				}
			};
			host.service.AwardClearQuestPoints = (JsValue player) =>
			{
				if (!((player as JsHostObject)?.Get("getId") is JsFunction { Native: var native }) || JsConvert.ToInt32(native(new JsCall
				{
					This = player,
					Args = Array.Empty<JsValue>()
				})) != GameSession.CharacterId)
				{
					throw new JsRuntimeException("Event clear player is not the local character");
				}
				NpcApi.ChangeFame(ScriptOperation.AwardEventQuestPoints);
			};
			if (GameServer.Local != null)
			{
				host.service.LobbyDelayMillis = (long)GameConfigDao.GetInt(GameServer.Local.Db, "event_lobby_delay", 10) * 1000L;
			}
			if (GameServer.Local != null)
			{
				host.service.MaxEventLevels = GameConfigDao.GetInt(GameServer.Local.Db, "max_event_levels", 8);
			}
			host.service.Clock = (NativeEventScriptService.Instance instance, int? seconds) =>
			{
				if (seconds.HasValue)
				{
					NativeEventClock.Show(seconds.Value);
				}
				else
				{
					NativeEventClock.Hide();
				}
			};
		}
		return host;
	}

	public static JsValue Manager(string name)
	{
		return (JsValue)(((object)Current().service.GetManager(name)?.Host) ?? ((object)JsNull.Value));
	}

	public static void QueueWildBossEvents()
	{
		if (!GameSession.InGame)
		{
			return;
		}
		NativeEventScripts nativeEventScripts = Current();
		if (nativeEventScripts.bossEventsQueued)
		{
			return;
		}
		nativeEventScripts.bossEventsQueued = true;
		foreach (string item in ScriptSource.Keys("event"))
		{
			if (item.StartsWith("AreaBoss", StringComparison.Ordinal))
			{
				nativeEventScripts.pendingBossEvents.Add(item);
			}
		}
	}

	private void TickBossEvents()
	{
		if (pendingBossEvents.Count == 0)
		{
			return;
		}
		string text = pendingBossEvents[0];
		pendingBossEvents.RemoveAt(0);
		try
		{
			service.GetManager(text);
		}
		catch (Exception ex)
		{
			Debug.LogError((object)("[EventScript] 启动事件脚本失败 " + text + "：" + ex.Message));
		}
	}

	public static JsValue Instance()
	{
		if (!((Object)(object)active != (Object)null) || active.character != GameSession.Character)
		{
			return JsNull.Value;
		}
		return (JsValue)(((object)active.service?.GetInstance(GameSession.CharacterId)?.Host) ?? ((object)JsNull.Value));
	}

	public static bool IsEventLeader(int characterId)
	{
		NativeEventScriptService.Instance instance = CurrentInstance();
		if (instance != null)
		{
			return instance.Leader == characterId;
		}
		return false;
	}

	public static NativeEventScriptService.Instance CurrentInstance()
	{
		if ((Object)(object)active == (Object)null || active.character != GameSession.Character)
		{
			return null;
		}
		return active.service?.GetInstance(GameSession.CharacterId);
	}

	public static NativeEventScriptService.Instance InstanceById(int characterId)
	{
		return active?.service?.GetInstance(characterId);
	}

	public static bool RegisterBotPlayer(int botId)
	{
		NativeEventScriptService.Instance instance = CurrentInstance();
		BotCharacter botCharacter = BotRegistry.Get(botId);
		if (instance == null || botCharacter == null)
		{
			return false;
		}
		instance.Register(BotNpcApi.Player(null, botCharacter), entry: false);
		return true;
	}

	public static bool UnregisterBotPlayer(int botId)
	{
		BotCharacter botCharacter = BotRegistry.Get(botId);
		NativeEventScriptService.Instance instance = active?.service?.GetInstance(botId) ?? CurrentInstance();
		if (instance == null || botCharacter == null)
		{
			return false;
		}
		instance.Unregister(BotNpcApi.Player(null, botCharacter));
		return true;
	}

	public static int GridOf(int characterId)
	{
		return InstanceById(characterId)?.Grid(characterId) ?? (-1);
	}

	public static string InstanceProperty(string key)
	{
		return CurrentInstance()?.Property(key);
	}

	public static NativeEventMap ResolveMap(int id)
	{
		if ((Object)(object)active == (Object)null || active.character != GameSession.Character)
		{
			return null;
		}
		NativeEventScriptService.Instance instance = active.service?.GetInstance(GameSession.CharacterId);
		if (instance == null || !active.instanceMaps.TryGetValue(instance, out var value) || !value.TryGetValue(id, out var value2))
		{
			return null;
		}
		return value2;
	}

	public static void ChangedMap(int map, bool after)
	{
		if ((Object)(object)active == (Object)null || active.character != GameSession.Character)
		{
			return;
		}
		NativeEventScriptService.Instance instance = active?.service?.GetInstance(GameSession.CharacterId);
		if (instance == null)
		{
			return;
		}
		try
		{
			instance.ChangedMap(NpcApi.BuildPlayer(NpcSession.MapContext()), map, after);
		}
		catch (Exception ex)
		{
			Debug.LogError((object)("[EventScript] map " + map + ": " + ex.Message));
		}
	}

	public static void PlayerDisconnected()
	{
		if ((Object)(object)active == (Object)null || active.character != GameSession.Character)
		{
			return;
		}
		NativeEventScriptService.Instance instance = active.service?.GetInstance(GameSession.CharacterId);
		if (instance == null)
		{
			return;
		}
		try
		{
			instance.PlayerDisconnected(NpcApi.BuildPlayer(NpcSession.MapContext()));
		}
		catch (Exception ex)
		{
			Debug.LogError((object)("[EventScript] playerDisconnected: " + ex.Message));
		}
	}

	private void Update()
	{
		if (service != null)
		{
			if (character != GameSession.Character)
			{
				service.Clear();
				ReleaseAllMaps();
				service = null;
			}
			else
			{
				TickBossEvents();
				service.Tick();
				ExpeditionRegistry.Tick(Clock());
				LucidBossCompat.Tick(Clock());
			}
		}
	}

	private void OnDestroy()
	{
		service?.Clear();
		ReleaseAllMaps();
		if ((Object)(object)active == (Object)(object)this)
		{
			active = null;
		}
	}

	private void ReleaseAllMaps()
	{
		foreach (Dictionary<int, NativeEventMap> value in instanceMaps.Values)
		{
			foreach (NativeEventMap value2 in value.Values)
			{
				value2.Dispose();
			}
		}
		instanceMaps.Clear();
	}

	internal void TickNow()
	{
		service?.Tick();
		LucidBossCompat.Tick(Clock());
	}
}
