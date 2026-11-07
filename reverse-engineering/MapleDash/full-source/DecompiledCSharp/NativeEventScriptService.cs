using System;
using System.Collections.Generic;
using System.Globalization;
using MapleDash.Js;
using Server.Field;

public sealed class NativeEventScriptService
{
	private sealed class Task
	{
		public Manager Manager;

		public Instance Owner;

		public long At;

		public Action Action;

		public bool Cancelled;

		public JsValue Host()
		{
			JsHostObject jsHostObject = new JsHostObject(this);
			jsHostObject.Set("cancel", JsFunction.FromNative("cancel", (JsCall c) =>
			{
				bool v = !Cancelled;
				Cancelled = true;
				return JsBoolean.Of(v);
			}));
			jsHostObject.Set("isCancelled", JsFunction.FromNative("isCancelled", (JsCall c) => JsBoolean.Of(Cancelled)));
			return jsHostObject;
		}
	}

	public sealed class Manager
	{
		private readonly NativeEventScriptService service;

		private readonly JsEngine engine;

		private readonly Dictionary<string, Instance> instances = new Dictionary<string, Instance>();

		private readonly Dictionary<string, string> properties = new Dictionary<string, string>();

		public readonly string Name;

		public readonly JsHostObject Host;

		public bool Cancelled { get; private set; }

		internal JsInterpreter InterpOf => engine.Interp;

		public Manager(NativeEventScriptService service, string name, string text)
		{
			Manager manager = this;
			this.service = service;
			Name = name;
			engine = new JsEngine(text, "event/" + name);
			Host = Strict(this, "event manager");
			Properties(Host, properties);
			Bind(Host, "getName", (JsCall c) => new JsString(manager.Name));
			Bind(Host, "getTransportationTime", (JsCall c) => new JsNumber((service.TransportationTime ?? throw new JsRuntimeException("Event transportation time is not configured"))(JsConvert.ToInt32(c.Arg(0)))));
			Bind(Host, "getBossTime", (JsCall c) => new JsNumber(service.BossTime(Long(c))));
			Bind(Host, "getChannelServer", (JsCall c) => service.ChannelServer?.Invoke() ?? throw new JsRuntimeException("Event channel server is not configured"));
			Bind(Host, "newInstance", (JsCall c) => manager.NewInstance(Text(c)).Host);
			Bind(Host, "getInstance", (JsCall c) => (!manager.instances.TryGetValue(Text(c), out var value)) ? ((JsValue)JsNull.Value) : ((JsValue)value.Host));
			Bind(Host, "getInstances", (JsCall c) =>
			{
				List<JsValue> list = new List<JsValue>();
				foreach (Instance value2 in manager.instances.Values)
				{
					list.Add(value2.Host);
				}
				return Players(list);
			});
			Bind(Host, "disposeInstance", (JsCall c) =>
			{
				if (manager.instances.TryGetValue(Text(c), out var value))
				{
					manager.Remove(value);
				}
				return Void;
			});
			Bind(Host, "schedule", (JsCall c) =>
			{
				Instance owner = ((c.ArgCount > 2) ? ((c.Arg(1) as JsHostObject)?.Host as Instance) : null);
				return service.Schedule(manager, owner, Long(c, (c.ArgCount <= 2) ? 1 : 2), () =>
				{
					manager.Call(Text(c), (JsValue)(((object)owner?.Host) ?? ((object)JsNull.Value)));
				}).Host();
			});
			Bind(Host, "scheduleAtTimestamp", (JsCall c) => service.Schedule(manager, null, Long(c, 1) - service.clock(), () =>
			{
				manager.Call(Text(c), JsNull.Value);
			}).Host());
			Bind(Host, "startInstance", (JsCall c) =>
			{
				if (c.ArgCount == 1)
				{
					if (c.Arg(0) is JsHostObject jsHostObject && jsHostObject.Host is Expedition)
					{
						return JsBoolean.Of(manager.StartExpedition(c.Arg(0)) != null);
					}
					if (c.Arg(0) is JsHostObject jsHostObject2 && !jsHostObject2.Members.Has("getId"))
					{
						throw new JsRuntimeException("Unmigrated event startInstance overload: " + manager.Name);
					}
					return JsBoolean.Of(manager.Start(c.Arg(0)) != null);
				}
				if (manager.IsParty(c.Arg(0)))
				{
					return JsBoolean.Of(manager.StartParty(c.Arg(0), c.Arg(1), (c.ArgCount <= 2) ? 1 : Number(c, 2)) != null);
				}
				if (c.ArgCount == 2 && c.Arg(0) is JsNumber)
				{
					return JsBoolean.Of(manager.Start(c.Arg(1), Number(c)) != null);
				}
				if (c.ArgCount >= 2 && c.Arg(0) is JsNumber && c.Arg(1) is JsHostObject jsHostObject3 && jsHostObject3.Host is Expedition)
				{
					return JsBoolean.Of(manager.StartExpedition(c.Arg(1), Number(c), (c.ArgCount > 2) ? c.Arg(2) : null) != null);
				}
				if (c.ArgCount == 4 && c.Arg(0) is JsNumber && manager.IsParty(c.Arg(1)))
				{
					return JsBoolean.Of(manager.StartParty(c.Arg(1), c.Arg(2), Number(c, 3), Number(c)) != null);
				}
				if (c.ArgCount == 4 && c.Arg(0) is JsNumber)
				{
					return JsBoolean.Of(manager.Start(c.Arg(1), Number(c), c.Arg(2), Number(c, 3)) != null);
				}
				throw new JsRuntimeException("Unmigrated event startInstance overload");
			});
			Bind(Host, "getEligibleParty", (JsCall c) =>
			{
				JsValue jsValue = c.Arg(0);
				if (jsValue is JsNull || jsValue is JsUndefined)
				{
					return new JsArray();
				}
				JsValue jsValue2;
				try
				{
					jsValue2 = manager.CallMember(jsValue, "getPartyMembersOnline");
				}
				catch (Exception)
				{
					return new JsArray();
				}
				try
				{
					JsValue jsValue3 = manager.Call("getEligibleParty", jsValue2);
					return (jsValue3 is JsArray) ? jsValue3 : new JsArray();
				}
				catch (Exception obj)
				{
					service.Error?.Invoke(obj);
					return new JsArray();
				}
			});
			Bind(Host, "getMonster", (JsCall c) =>
			{
				if (service.MonsterFactory == null)
				{
					throw new JsRuntimeException("Event monster factory is not configured");
				}
				return service.MonsterFactory(Number(c));
			});
		}

		internal void Initialize()
		{
			service.configure?.Invoke(engine);
			engine.Bind("em", Host);
			engine.Run();
			Call("init");
		}

		internal JsValue Call(string method, params JsValue[] args)
		{
			if (Cancelled || !engine.HasFunction(method))
			{
				return JsNull.Value;
			}
			return engine.Call(method, args);
		}

		public Instance NewInstance(string name)
		{
			if (Cancelled)
			{
				throw new JsRuntimeException("Disposed event manager: " + Name);
			}
			if (instances.ContainsKey(name))
			{
				throw new JsRuntimeException("Event instance already exists: " + name);
			}
			Instance instance = new Instance(this, service, name);
			instances.Add(name, instance);
			return instance;
		}

		private int PickLobby(int requestedLobby)
		{
			int val = (engine.HasFunction("getMaxLobbies") ? JsConvert.ToInt32(Call("getMaxLobbies")) : 8);
			int num = Math.Min(8, val);
			int i;
			for (i = ((requestedLobby >= 0) ? requestedLobby : 0); i < num; i++)
			{
				bool flag = false;
				foreach (Instance value in instances.Values)
				{
					if (value.Lobby == i)
					{
						flag = true;
					}
				}
				if (!flag)
				{
					break;
				}
				if (requestedLobby >= 0)
				{
					return -1;
				}
			}
			if (i < num)
			{
				return i;
			}
			return -1;
		}

		public Instance Start(JsValue player, int requestedLobby = -1, JsValue leader = null, int difficulty = 1)
		{
			if (Cancelled)
			{
				return null;
			}
			if (requestedLobby < -1)
			{
				return null;
			}
			int num = PickLobby(requestedLobby);
			if (num < 0)
			{
				return null;
			}
			HashSet<string> hashSet = new HashSet<string>(instances.Keys);
			try
			{
				if (!((Call("setup", new JsNumber(difficulty), new JsNumber(num)) as JsHostObject)?.Host is Instance instance) || instance.manager != this)
				{
					throw new JsRuntimeException("Event setup did not return its instance: " + Name);
				}
				instance.Lobby = num;
				instance.Leader = PlayerId(leader ?? player, engine);
				instance.Register(player);
				instance.Start();
				return instance;
			}
			catch
			{
				foreach (Instance item in new List<Instance>(instances.Values))
				{
					if (!hashSet.Contains(item.Name))
					{
						item.Dispose();
					}
				}
				throw;
			}
		}

		public Instance StartExpedition(JsValue expedition, int requestedLobby = -1, JsValue leader = null)
		{
			if (Cancelled || !((expedition as JsHostObject)?.Host is Expedition expedition2))
			{
				return null;
			}
			int num = PickLobby(requestedLobby);
			if (num < 0)
			{
				return null;
			}
			JsValue jsValue = leader ?? CallMember(expedition, "getLeader");
			HashSet<string> hashSet = new HashSet<string>(instances.Keys);
			try
			{
				if (!((Call("setup", new JsNumber(1.0), new JsNumber(num)) as JsHostObject)?.Host is Instance instance) || instance.manager != this)
				{
					throw new JsRuntimeException("Event setup did not return its instance: " + Name);
				}
				instance.Lobby = num;
				instance.Leader = expedition2.LeaderId;
				expedition2.Start();
				instance.Exped = expedition2;
				if (jsValue != null && !(jsValue is JsNull) && !(jsValue is JsUndefined) && JsConvert.ToInt32(CallMember(jsValue, "getMapId")) == expedition2.StartMapId)
				{
					instance.Register(jsValue);
				}
				instance.Start();
				return instance;
			}
			catch
			{
				foreach (Instance item in new List<Instance>(instances.Values))
				{
					if (!hashSet.Contains(item.Name))
					{
						item.Dispose();
					}
				}
				throw;
			}
		}

		public Instance StartParty(JsValue party, JsValue map, int difficulty, int requestedLobby = -1)
		{
			if (Cancelled || party is JsNull || party is JsUndefined || map is JsNull)
			{
				return null;
			}
			JsValue jsValue = CallMember(party, "getLeader");
			if (jsValue is JsNull || jsValue is JsUndefined)
			{
				return null;
			}
			int num = PlayerId(jsValue, engine);
			if (service.LocalPlayerId != null && service.LocalPlayerId() != num)
			{
				return null;
			}
			int num2 = JsConvert.ToInt32(CallMember(map, "getId"));
			if (JsConvert.ToInt32(CallMember(jsValue, "getMapId")) != num2)
			{
				return null;
			}
			return Start(jsValue, requestedLobby, jsValue, difficulty);
		}

		public void Cancel()
		{
			if (Cancelled)
			{
				return;
			}
			try
			{
				Call("cancelSchedule", JsNull.Value);
			}
			finally
			{
				foreach (Instance item in new List<Instance>(instances.Values))
				{
					item.Dispose();
				}
				Cancelled = true;
				properties.Clear();
			}
		}

		internal int Id(JsValue player)
		{
			return PlayerId(player, engine);
		}

		private bool IsParty(JsValue value)
		{
			if (value is JsHostObject jsHostObject)
			{
				return jsHostObject.Members.Has("getLeaderId");
			}
			return false;
		}

		internal JsValue CallMember(JsValue value, string name, params JsValue[] args)
		{
			return engine.Interp.Call(engine.Interp.GetMember(value, name), value, args);
		}

		internal void Remove(Instance instance)
		{
			service.Schedule(this, null, service.LobbyDelayMillis, () =>
			{
				if (instances.TryGetValue(instance.Name, out var value) && value == instance)
				{
					instances.Remove(instance.Name);
				}
				if (!instance.Disposed && instance.PlayerCount == 0)
				{
					instance.Dispose();
				}
			});
		}
	}

	public sealed class Instance
	{
		private sealed class Reward
		{
			public List<int> Items;

			public List<int> Quantities;

			public int Exp;
		}

		internal readonly Manager manager;

		private readonly NativeEventScriptService service;

		private readonly Dictionary<int, JsValue> players = new Dictionary<int, JsValue>();

		private readonly Dictionary<string, string> properties = new Dictionary<string, string>();

		private readonly Dictionary<int, JsValue> maps = new Dictionary<int, JsValue>();

		private readonly HashSet<JsValue> monsters = new HashSet<JsValue>();

		private readonly Dictionary<int, int> killCount = new Dictionary<int, int>();

		private readonly Dictionary<int, int> playerGrid = new Dictionary<int, int>();

		private readonly List<int> onMapClearExp = new List<int>();

		private readonly List<int> onMapClearMeso = new List<int>();

		private readonly List<int> exclusiveItems = new List<int>();

		private readonly Dictionary<int, Reward> rewards = new Dictionary<int, Reward>();

		private Task timer;

		private long timerStarted;

		private long eventTime;

		private bool disposing;

		public readonly string Name;

		public readonly JsHostObject Host;

		public bool Disposed { get; private set; }

		public bool Started { get; private set; }

		public bool Cleared { get; private set; }

		public int Leader { get; internal set; }

		public int Lobby { get; internal set; } = -1;

		public Expedition Exped { get; internal set; }

		public int PlayerCount => players.Count;

		public int MaximumPlayerCount { get; private set; }

		public string EventName => manager.Name;

		private JsInterpreter engine_interp => manager.InterpOf;

		public string Property(string key)
		{
			if (!properties.TryGetValue(key, out var value))
			{
				return null;
			}
			return value;
		}

		public int Grid(int characterId)
		{
			if (!playerGrid.TryGetValue(characterId, out var value))
			{
				return -1;
			}
			return value;
		}

		public void SetIntProperty(string key, int value)
		{
			properties[key] = value.ToString(CultureInfo.InvariantCulture);
		}

		internal Instance(Manager manager, NativeEventScriptService service, string name)
		{
			Instance instance = this;
			this.manager = manager;
			this.service = service;
			Name = name;
			Host = Strict(this, "event instance");
			Properties(Host, properties);
			Bind(Host, "getName", (JsCall c) => new JsString(instance.Name));
			Bind(Host, "getChannelServer", (JsCall c) => service.ChannelServer?.Invoke() ?? throw new JsRuntimeException("Event channel server is not configured"));
			Bind(Host, "getEm", (JsCall c) => manager.Host);
			Bind(Host, "getEventManager", (JsCall c) => manager.Host);
			Bind(Host, "spawnNpc", (JsCall c) =>
			{
				if (service.SpawnNpc == null)
				{
					throw new JsRuntimeException("Event NPC spawning is not configured");
				}
				service.SpawnNpc(instance, Number(c), c.Arg(1), c.Arg(2));
				return Void;
			});
			Bind(Host, "invokeScriptFunction", (JsCall c) =>
			{
				JsValue[] array = new JsValue[Math.Max(0, c.ArgCount - 1)];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = c.Arg(i + 1);
				}
				return instance.Invoke(Text(c), array);
			});
			Bind(Host, "setExclusiveItems", (JsCall c) =>
			{
				instance.exclusiveItems.AddRange(Integers(c.Arg(0)));
				return Void;
			});
			Bind(Host, "setEventRewards", (JsCall c) =>
			{
				bool flag = c.Arg(0) is JsNumber;
				int num = ((!flag) ? 1 : Number(c));
				int num2 = (flag ? 1 : 0);
				if (num < 1 || num > service.MaxEventLevels)
				{
					return Void;
				}
				instance.rewards[num] = new Reward
				{
					Items = Integers(c.Arg(num2)),
					Quantities = Integers(c.Arg(num2 + 1)),
					Exp = ((c.ArgCount > num2 + 2) ? Number(c, num2 + 2) : 0)
				};
				return Void;
			});
			Bind(Host, "giveEventReward", (JsCall c) => JsBoolean.Of(instance.GiveReward(c.Arg(0), (c.ArgCount <= 1) ? 1 : Number(c, 1))));
			Bind(Host, "getPlayers", (JsCall c) => Players(instance.players.Values));
			Bind(Host, "getPlayerCount", (JsCall c) => new JsNumber(instance.players.Count));
			Bind(Host, "getPlayerById", (JsCall c) => (!instance.players.TryGetValue(Number(c), out var value)) ? JsNull.Value : value);
			Bind(Host, "getLeaderId", (JsCall c) => new JsNumber(instance.Leader));
			Bind(Host, "getKillCount", (JsCall c) => new JsNumber(instance.killCount.TryGetValue(manager.Id(c.Arg(0)), out var value) ? value : 0));
			Bind(Host, "isLeader", (JsCall c) => JsBoolean.Of(manager.Id(c.Arg(0)) == instance.Leader));
			Bind(Host, "registerPlayer", (JsCall c) =>
			{
				instance.Register(c.Arg(0), c.ArgCount < 2 || JsConvert.ToBoolean(c.Arg(1)));
				return Void;
			});
			Bind(Host, "unregisterPlayer", (JsCall c) =>
			{
				instance.Unregister(c.Arg(0));
				return Void;
			});
			Bind(Host, "exitPlayer", (JsCall c) =>
			{
				instance.Unregister(c.Arg(0));
				instance.Invoke("playerExit", instance.Host, c.Arg(0));
				return Void;
			});
			Bind(Host, "dispose", (JsCall c) =>
			{
				instance.Dispose();
				return Void;
			});
			Bind(Host, "isDisposed", (JsCall c) => JsBoolean.Of(instance.Disposed));
			Bind(Host, "isEventDisposed", (JsCall c) => JsBoolean.Of(instance.Disposed));
			Bind(Host, "isEventCleared", (JsCall c) => JsBoolean.Of(instance.Cleared));
			Bind(Host, "setEventCleared", (JsCall c) =>
			{
				instance.SetCleared();
				return Void;
			});
			Bind(Host, "showClearEffect", (JsCall c) =>
			{
				bool arg = false;
				int num = int.MinValue;
				JsValue[] args = c.Args;
				foreach (JsValue jsValue in args)
				{
					if (jsValue is JsBoolean jsBoolean)
					{
						arg = jsBoolean.Value;
					}
					else if (jsValue is JsNumber jsNumber)
					{
						num = (int)jsNumber.Value;
					}
				}
				if (num == int.MinValue)
				{
					num = ((instance.Leader != 0 && instance.players.TryGetValue(instance.Leader, out var value)) ? JsConvert.ToInt32(manager.CallMember(value, "getMapId")) : int.MinValue);
				}
				service.ClearEffect?.Invoke(instance, num, arg);
				return Void;
			});
			Bind(Host, "isEventTeamLackingNow", (JsCall c) => JsBoolean.Of((!instance.Cleared) ? ((JsConvert.ToBoolean(c.Arg(0)) && manager.Id(c.Arg(2)) == instance.Leader) || instance.PlayerCount <= Number(c, 1)) : (JsConvert.ToBoolean(c.Arg(0)) && instance.PlayerCount <= 1)));
			Bind(Host, "isExpeditionTeamLackingNow", (JsCall c) => JsBoolean.Of((!instance.Cleared) ? (instance.PlayerCount <= 1) : (JsConvert.ToBoolean(c.Arg(0)) && instance.PlayerCount <= 1)));
			Bind(Host, "checkEventTeamLacking", (JsCall c) => JsBoolean.Of((!instance.Cleared || instance.PlayerCount <= 1) && ((!instance.Cleared && JsConvert.ToBoolean(c.Arg(0)) && !instance.LeaderOnline()) || instance.PlayerCount < Number(c, 1))));
			Bind(Host, "isEventTeamTogether", (JsCall c) => JsBoolean.Of(instance.TeamTogether()));
			Bind(Host, "dropMessage", (JsCall c) =>
			{
				foreach (JsValue item2 in new List<JsValue>(instance.players.Values))
				{
					manager.CallMember(item2, "dropMessage", c.Arg(0), c.Arg(1));
				}
				return Void;
			});
			Bind(Host, "startEvent", (JsCall c) =>
			{
				instance.Start();
				return Void;
			});
			Bind(Host, "schedule", (JsCall c) =>
			{
				if (!instance.Disposed)
				{
					service.Schedule(manager, instance, Long(c, 1), () =>
					{
						instance.Invoke(Text(c), instance.Host);
					});
				}
				return Void;
			});
			Bind(Host, "startEventTimer", (JsCall c) =>
			{
				instance.StartTimer(Long(c));
				return Void;
			});
			Bind(Host, "restartEventTimer", (JsCall c) =>
			{
				instance.StopTimer();
				instance.StartTimer(Long(c));
				return Void;
			});
			Bind(Host, "stopEventTimer", (JsCall c) =>
			{
				instance.StopTimer();
				return Void;
			});
			Bind(Host, "getTimeLeft", (JsCall c) => new JsNumber(instance.eventTime - (service.clock() - instance.timerStarted)));
			Bind(Host, "isTimerStarted", (JsCall c) => JsBoolean.Of(instance.eventTime > 0 && instance.timerStarted > 0));
			Bind(Host, "getInstanceMap", (JsCall c) => instance.GetMap(Number(c)));
			Bind(Host, "getMapInstance", (JsCall c) => instance.GetMap(Number(c)));
			Bind(Host, "getEm", (JsCall c) => manager.Host);
			Bind(Host, "getMapFactory", (JsCall c) =>
			{
				JsHostObject jsHostObject = new JsHostObject(null);
				jsHostObject.Set("getMap", JsFunction.FromNative("getMap", (JsCall c2) => (service.WarpMapFactory == null) ? JsNull.Value : service.WarpMapFactory(Number(c2))));
				return jsHostObject;
			});
			Bind(Host, "isEventLeader", (JsCall c) => JsBoolean.Of(instance.IsPlayer(c.Arg(0)) && manager.Id(c.Arg(0)) == instance.Leader));
			Bind(Host, "clearPQ", (JsCall c) =>
			{
				instance.Invoke("clearPQ", instance.Host);
				return Void;
			});
			Bind(Host, "warpEventTeam", (JsCall c) =>
			{
				int num = ((c.ArgCount > 1) ? Number(c) : int.MinValue);
				int to = ((c.ArgCount > 1) ? Number(c, 1) : Number(c));
				instance.WarpTeam(num, to);
				return Void;
			});
			Bind(Host, "warpEventTeamToMapSpawnPoint", (JsCall c) =>
			{
				int num = ((c.ArgCount > 2) ? Number(c) : int.MinValue);
				int to = ((c.ArgCount > 2) ? Number(c, 1) : Number(c));
				instance.WarpTeam(num, to);
				return Void;
			});
			Bind(Host, "linkToNextStage", (JsCall c) =>
			{
				instance.GiveStageReward(Number(c));
				service.LinkPortal?.Invoke(instance, Text(c, 1) + (Number(c) - 1), Number(c, 2));
				return Void;
			});
			Bind(Host, "linkPortalToScript", (JsCall c) =>
			{
				instance.GiveStageReward(Number(c));
				service.LinkPortal?.Invoke(instance, Text(c, 2), Number(c, 3));
				return Void;
			});
			Bind(Host, "gridInsert", (JsCall c) =>
			{
				instance.playerGrid[manager.Id(c.Arg(0))] = Number(c, 1);
				return Void;
			});
			Bind(Host, "gridCheck", (JsCall c) => new JsNumber(instance.playerGrid.TryGetValue(manager.Id(c.Arg(0)), out var value) ? value : (-1)));
			Bind(Host, "gridRemove", (JsCall c) =>
			{
				instance.playerGrid.Remove(manager.Id(c.Arg(0)));
				return Void;
			});
			Bind(Host, "gridSize", (JsCall c) => new JsNumber(instance.playerGrid.Count));
			Bind(Host, "gridClear", (JsCall c) =>
			{
				instance.playerGrid.Clear();
				return Void;
			});
			Bind(Host, "setEventClearStageExp", (JsCall c) =>
			{
				instance.onMapClearExp.Clear();
				instance.onMapClearExp.AddRange(Integers(c.Arg(0)));
				return Void;
			});
			Bind(Host, "setEventClearStageMeso", (JsCall c) =>
			{
				instance.onMapClearMeso.Clear();
				instance.onMapClearMeso.AddRange(Integers(c.Arg(0)));
				return Void;
			});
			Bind(Host, "getClearStageExp", (JsCall c) => new JsNumber(StageBonus(instance.onMapClearExp, Number(c))));
			Bind(Host, "getClearStageMeso", (JsCall c) => new JsNumber(StageBonus(instance.onMapClearMeso, Number(c))));
			Bind(Host, "getClearStageBonus", (JsCall c) =>
			{
				JsArray bonus = new JsArray(new JsValue[2]
				{
					new JsNumber(StageBonus(instance.onMapClearExp, Number(c))),
					new JsNumber(StageBonus(instance.onMapClearMeso, Number(c)))
				});
				bonus.Set("get", JsFunction.FromNative("get", (JsCall call) => bonus.GetIndex(Number(call))));
				bonus.Set("size", JsFunction.FromNative("size", (JsCall _) => new JsNumber(bonus.Length)));
				return bonus;
			});
			Bind(Host, "giveEventPlayersStageReward", (JsCall c) =>
			{
				instance.GiveStageReward(Number(c));
				return Void;
			});
			Bind(Host, "giveEventPlayersExp", (JsCall c) =>
			{
				instance.GiveAll("gainExp", Number(c), (c.ArgCount > 1) ? Number(c, 1) : (-1));
				return Void;
			});
			Bind(Host, "giveEventPlayersMeso", (JsCall c) =>
			{
				instance.GiveAll("gainMeso", Number(c), (c.ArgCount > 1) ? Number(c, 1) : (-1));
				return Void;
			});
			Bind(Host, "showWrongEffect", (JsCall c) =>
			{
				service.WrongEffect?.Invoke(instance, (c.ArgCount > 0) ? Number(c) : (-1));
				return Void;
			});
			Bind(Host, "dropAllExclusiveItems", (JsCall c) =>
			{
				instance.DropExclusiveItems();
				return Void;
			});
			Bind(Host, "registerMonster", (JsCall c) =>
			{
				if (c.Arg(0) is JsHostObject item)
				{
					instance.monsters.Add(item);
				}
				return Void;
			});
			Bind(Host, "activatedAllReactorsOnMap", (JsCall c) =>
			{
				int mapId = JsConvert.ToInt32(manager.CallMember(c.Arg(0), "getId"));
				return JsBoolean.Of(instance.AllReactorsActivated(mapId, Number(c, 1), Number(c, 2)));
			});
		}

		private List<JsValue> PlayerSnapshot()
		{
			List<JsValue> list = new List<JsValue>();
			foreach (KeyValuePair<int, JsValue> player in players)
			{
				if (!Disposed)
				{
					list.Add(player.Value);
				}
			}
			return list;
		}

		private bool IsPlayer(JsValue player)
		{
			if (player is JsNull || player is JsUndefined)
			{
				return false;
			}
			return engine_getId(player) != 0;
		}

		private int engine_getId(JsValue player)
		{
			try
			{
				return manager.Id(player);
			}
			catch (Exception)
			{
				return 0;
			}
		}

		private int MapOf(JsValue player)
		{
			try
			{
				return JsConvert.ToInt32(manager.CallMember(player, "getMapId"));
			}
			catch (Exception)
			{
				return -1;
			}
		}

		private void WarpTeam(int from, int to)
		{
			if (to <= 0)
			{
				return;
			}
			foreach (JsValue item in PlayerSnapshot())
			{
				if (from != int.MinValue && MapOf(item) != from)
				{
					continue;
				}
				if (service.WarpPlayer != null)
				{
					service.WarpPlayer(this, to, item);
					continue;
				}
				try
				{
					manager.CallMember(item, "changeMap", GetMap(to));
				}
				catch (Exception obj)
				{
					service.Error?.Invoke(obj);
				}
			}
		}

		private void GiveAll(string method, long amount, int mapId)
		{
			if (amount == 0L)
			{
				return;
			}
			foreach (JsValue item in PlayerSnapshot())
			{
				if (mapId != -1 && MapOf(item) != mapId)
				{
					continue;
				}
				try
				{
					double num = 1.0;
					if (method == "gainExp")
					{
						JsValue jsValue = engine_interp?.GetMember(item, "getExpRate");
						if (jsValue is JsFunction)
						{
							num = JsConvert.ToNumber(engine_interp.Call(jsValue, item, Array.Empty<JsValue>()));
						}
					}
					manager.CallMember(item, method, new JsNumber(Math.Round((double)amount * num)));
				}
				catch (Exception obj)
				{
					service.Error?.Invoke(obj);
				}
			}
		}

		private void GiveStageReward(int stage)
		{
			GiveAll("gainExp", StageBonus(onMapClearExp, stage), -1);
			GiveAll("gainMeso", StageBonus(onMapClearMeso, stage), -1);
		}

		private static int StageBonus(List<int> table, int stage)
		{
			if (stage < 1 || stage > table.Count)
			{
				return 0;
			}
			return table[stage - 1];
		}

		private void DropExclusiveItems()
		{
			if (exclusiveItems.Count == 0)
			{
				return;
			}
			foreach (JsValue item in PlayerSnapshot())
			{
				foreach (int exclusiveItem in exclusiveItems)
				{
					try
					{
						manager.CallMember(item, "removeAll", new JsNumber(exclusiveItem));
					}
					catch (Exception)
					{
					}
				}
			}
		}

		private bool AllReactorsActivated(int mapId, int min, int max)
		{
			foreach (MapReactor item in MapReactorService.Get(mapId))
			{
				if (item.id >= min && item.id <= max && item.state <= 0)
				{
					return false;
				}
			}
			return true;
		}

		private JsValue GetMap(int id)
		{
			if (Disposed)
			{
				throw new JsRuntimeException("Disposed event instance: " + Name);
			}
			if (maps.TryGetValue(id, out var value))
			{
				return value;
			}
			if (service.InstanceMap == null)
			{
				throw new JsRuntimeException("Unmigrated event instance map isolation: " + id);
			}
			value = service.InstanceMap(this, id);
			if (value == null || value is JsNull)
			{
				throw new JsRuntimeException("Missing event instance map: " + id);
			}
			maps.Add(id, value);
			return value;
		}

		public JsValue Invoke(string method, params JsValue[] args)
		{
			if (!Disposed)
			{
				return manager.Call(method, args);
			}
			return JsNull.Value;
		}

		private static List<int> Integers(JsValue value)
		{
			JsArray obj = (value as JsArray) ?? throw new JsRuntimeException("Event item list must be an array");
			List<int> list = new List<int>();
			foreach (JsValue item in obj.Items)
			{
				list.Add(JsConvert.ToInt32(item));
			}
			return list;
		}

		private bool GiveReward(JsValue player, int level)
		{
			if (!rewards.TryGetValue(level, out var value))
			{
				return true;
			}
			if (value.Items.Count > 0)
			{
				HashSet<int> hashSet = new HashSet<int>();
				foreach (int item in value.Items)
				{
					hashSet.Add(item / 1000000);
				}
				foreach (int item2 in hashSet)
				{
					if (!JsConvert.ToBoolean(manager.CallMember(player, "hasEmptySlot", new JsNumber(item2))))
					{
						return false;
					}
				}
				int num = service.RewardIndex(value.Items.Count);
				if (num >= value.Quantities.Count)
				{
					throw new JsRuntimeException("Missing event reward quantity");
				}
				JsValue value2 = manager.CallMember(player, "getAbstractPlayerInteraction");
				manager.CallMember(value2, "gainItem", new JsNumber(value.Items[num]), new JsNumber((short)value.Quantities[num]));
			}
			if (value.Exp > 0)
			{
				manager.CallMember(player, "gainExp", new JsNumber(value.Exp));
			}
			return true;
		}

		private bool LeaderOnline()
		{
			foreach (JsValue value in players.Values)
			{
				if (manager.Id(value) == Leader)
				{
					return true;
				}
			}
			return false;
		}

		private bool TeamTogether()
		{
			if (players.Count <= 1)
			{
				return true;
			}
			int num = int.MinValue;
			foreach (JsValue value in players.Values)
			{
				int num2 = JsConvert.ToInt32(manager.CallMember(value, "getMapId"));
				if (num == int.MinValue)
				{
					num = num2;
				}
				else if (num2 != num)
				{
					return false;
				}
			}
			return true;
		}

		public void SetCleared()
		{
			if (Disposed || Cleared)
			{
				return;
			}
			Cleared = true;
			service.Completed?.Invoke(this);
			foreach (JsValue item in new List<JsValue>(players.Values))
			{
				if (service.AwardClearQuestPoints == null)
				{
					throw new JsRuntimeException("Event clear quest points are not configured");
				}
				service.AwardClearQuestPoints(item);
			}
			manager.Remove(this);
		}

		public void RegisterMonster(JsValue monster, bool friendly = false)
		{
			if (!Disposed && !friendly)
			{
				monsters.Add(monster);
			}
		}

		public void MonsterKilled(JsValue monster, bool hasKiller, bool friendly = false)
		{
			if (Disposed)
			{
				return;
			}
			if (friendly)
			{
				Invoke("friendlyKilled", monster, Host, JsBoolean.Of(hasKiller));
				return;
			}
			monsters.Remove(monster);
			if (Started)
			{
				bool flag = monsters.Count == 0;
				Invoke("monsterKilled", monster, Host, JsBoolean.Of(hasKiller));
				if (flag)
				{
					Invoke("allMonstersDead", Host, JsBoolean.Of(hasKiller));
				}
			}
		}

		public void MonsterKilledBy(JsValue killer, int mobId, string mobName = null)
		{
			if (!Disposed && mobId > 0 && !(killer is JsNull) && !(killer is JsUndefined) && Invoke("monsterValue", Host, new JsNumber(mobId)) is JsNumber jsNumber)
			{
				int num = (int)jsNumber.Value;
				if (num != 0)
				{
					int key = manager.Id(killer);
					killCount.TryGetValue(key, out var value);
					killCount[key] = value + num;
					Exped?.MonsterKilled(mobId, mobName ?? ("mob " + mobId));
				}
			}
		}

		public int GetKillCount(JsValue player)
		{
			if (!killCount.TryGetValue(manager.Id(player), out var value))
			{
				return 0;
			}
			return value;
		}

		public void PlayerDisconnected(JsValue player)
		{
			if (!Disposed)
			{
				Invoke("playerDisconnected", Host, player);
			}
		}

		public void ReviveMonster(JsValue monster)
		{
			if (!Disposed && !(monster is JsNull) && !(monster is JsUndefined))
			{
				Invoke("monsterRevive", Host, monster);
			}
		}

		public void Register(JsValue player, bool entry = true)
		{
			if (Disposed || player is JsNull || player is JsUndefined)
			{
				return;
			}
			int key = manager.Id(player);
			if (!players.ContainsKey(key))
			{
				players.Add(key, player);
				MaximumPlayerCount = Math.Max(MaximumPlayerCount, players.Count);
				service.membership[key] = this;
				if (entry)
				{
					Invoke("playerEntry", Host, player);
				}
			}
		}

		public void Unregister(JsValue player)
		{
			if (Disposed)
			{
				return;
			}
			int key = manager.Id(player);
			Invoke("playerUnregistered", Host, player);
			players.Remove(key);
			service.membership.Remove(key);
			if (exclusiveItems.Count <= 0)
			{
				return;
			}
			JsValue value = manager.CallMember(player, "getAbstractPlayerInteraction");
			foreach (int exclusiveItem in exclusiveItems)
			{
				manager.CallMember(value, "removeAll", new JsNumber(exclusiveItem));
			}
		}

		public void Start()
		{
			if (!Disposed)
			{
				Started = true;
				Invoke("afterSetup", Host);
			}
		}

		public void ChangedMap(JsValue player, int map, bool after = false)
		{
			Invoke(after ? "afterChangedMap" : "changedMap", Host, player, new JsNumber(map));
		}

		public void StartTimer(long duration)
		{
			if (!Disposed)
			{
				timerStarted = service.clock();
				eventTime = duration;
				service.Clock?.Invoke(this, (int)(duration / 1000));
				timer = service.Schedule(manager, this, duration, () =>
				{
					DismissTimer();
					Invoke("scheduledTimeout", Host);
				});
			}
		}

		private void DismissTimer()
		{
			service.Clock?.Invoke(this, null);
			timer = null;
			timerStarted = 0L;
			eventTime = 0L;
		}

		public void StopTimer()
		{
			if (timer != null)
			{
				timer.Cancelled = true;
			}
			DismissTimer();
		}

		public void Dispose()
		{
			if (Disposed || disposing)
			{
				return;
			}
			disposing = true;
			try
			{
				Invoke("dispose", Host);
			}
			finally
			{
				Disposed = true;
				foreach (int key in players.Keys)
				{
					if (service.GetInstance(key) == this)
					{
						service.membership.Remove(key);
					}
				}
				players.Clear();
				if (timer != null)
				{
					timer.Cancelled = true;
				}
				timer = null;
				foreach (Task task in service.tasks)
				{
					if (task.Owner == this)
					{
						task.Cancelled = true;
					}
				}
				properties.Clear();
				maps.Clear();
				monsters.Clear();
				killCount.Clear();
				manager.Remove(this);
				service.ReleaseMaps?.Invoke(this);
				if (Exped != null)
				{
					Exped.Dispose(Cleared);
					ExpeditionRegistry.RemoveExpedition(Exped);
					Exped = null;
				}
				disposing = false;
			}
		}
	}

	private readonly Func<string, string> source;

	private readonly Func<long> clock;

	private readonly Action<JsEngine> configure;

	private readonly Dictionary<string, Manager> managers = new Dictionary<string, Manager>();

	private readonly Dictionary<int, Instance> membership = new Dictionary<int, Instance>();

	private readonly List<Task> tasks = new List<Task>();

	public Func<Instance, int, JsValue> InstanceMap;

	public Func<JsValue> ChannelServer;

	public Func<int, int> TransportationTime;

	public Func<long, long> BossTime = (long value) => value;

	public Action<Instance, int?> Clock;

	public Action<Instance> ReleaseMaps;

	public Action<Instance, int, JsValue> WarpPlayer;

	public Action<Instance, string, int> LinkPortal;

	public Action<Instance, int> WrongEffect;

	public Action<JsValue> AwardClearQuestPoints;

	public Action<Instance> Completed;

	public Action<Instance, int, JsValue, JsValue> SpawnNpc;

	public Action<Instance, int, bool> ClearEffect;

	public Action<Exception> Error;

	public Func<int> LocalPlayerId;

	public long LobbyDelayMillis = 10000L;

	public int MaxEventLevels = 8;

	public Func<int, int> RewardIndex = (int count) => new Random().Next(count);

	public Func<int, JsValue> MonsterFactory;

	public Func<int, JsValue> WarpMapFactory;

	private static JsValue Void = JsUndefined.Value;

	public NativeEventScriptService(Func<string, string> source, Func<long> clock, Action<JsEngine> configure = null)
	{
		this.source = source;
		this.clock = clock;
		this.configure = configure;
	}

	public Manager GetManager(string name)
	{
		if (managers.TryGetValue(name, out var value))
		{
			return value;
		}
		string text = source("event/" + name);
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		Manager manager = new Manager(this, name, text);
		managers.Add(name, manager);
		try
		{
			manager.Initialize();
			return manager;
		}
		catch
		{
			manager.Cancel();
			managers.Remove(name);
			throw;
		}
	}

	public Instance GetInstance(int character)
	{
		if (!membership.TryGetValue(character, out var value))
		{
			return null;
		}
		return value;
	}

	public void Tick()
	{
		long now = clock();
		List<Task> list = tasks.FindAll((Task t) => !t.Cancelled && t.At <= now);
		list.Sort((Task a, Task b) => a.At.CompareTo(b.At));
		foreach (Task item in list)
		{
			tasks.Remove(item);
			if (item.Cancelled || item.Manager.Cancelled)
			{
				continue;
			}
			Instance owner = item.Owner;
			if (owner != null && owner.Disposed)
			{
				continue;
			}
			item.Cancelled = true;
			try
			{
				item.Action();
			}
			catch (Exception obj)
			{
				if (Error == null)
				{
					throw;
				}
				Error(obj);
			}
		}
		tasks.RemoveAll((Task t) => t.Cancelled || t.Manager.Cancelled || (t.Owner?.Disposed ?? false));
	}

	public void Clear()
	{
		foreach (Manager item in new List<Manager>(managers.Values))
		{
			item.Cancel();
		}
		managers.Clear();
		membership.Clear();
		tasks.Clear();
	}

	private Task Schedule(Manager manager, Instance owner, long delay, Action action)
	{
		Task task = new Task
		{
			Manager = manager,
			Owner = owner,
			At = clock() + Math.Max(0L, delay),
			Action = action
		};
		tasks.Add(task);
		return task;
	}

	private static void Bind(JsHostObject host, string name, Func<JsCall, JsValue> call)
	{
		host.Set(name, JsFunction.FromNative(name, call));
	}

	private static string Text(JsCall c, int index = 0)
	{
		return JsConvert.ToString(c.Arg(index));
	}

	private static int Number(JsCall c, int index = 0)
	{
		return JsConvert.ToInt32(c.Arg(index));
	}

	private static long Long(JsCall c, int index = 0)
	{
		return (long)JsConvert.ToNumber(c.Arg(index));
	}

	private static JsHostObject Strict(object value, string kind)
	{
		return new JsHostObject(value)
		{
			Missing = (string key) =>
			{
				throw new JsRuntimeException("Unmigrated " + kind + " API: " + key);
			}
		};
	}

	private static void Properties(JsHostObject host, Dictionary<string, string> properties)
	{
		Bind(host, "setProperty", (JsCall c) =>
		{
			properties[Text(c)] = Text(c, 1);
			return Void;
		});
		Bind(host, "setIntProperty", (JsCall c) =>
		{
			properties[Text(c)] = Number(c, 1).ToString(CultureInfo.InvariantCulture);
			return Void;
		});
		Bind(host, "getProperty", (JsCall c) => (!properties.TryGetValue(Text(c), out var value)) ? ((JsValue)JsNull.Value) : ((JsValue)new JsString(value)));
		Bind(host, "getIntProperty", (JsCall c) => (!properties.TryGetValue(Text(c), out var value) || !int.TryParse(value, out var result)) ? new JsNumber(0.0) : new JsNumber(result));
	}

	private static int PlayerId(JsValue player, JsEngine engine)
	{
		JsValue member = engine.Interp.GetMember(player, "getId");
		return JsConvert.ToInt32(engine.Interp.Call(member, player, Array.Empty<JsValue>()));
	}

	private static JsValue Players(IEnumerable<JsValue> players)
	{
		JsArray array = new JsArray(players);
		array.Set("get", JsFunction.FromNative("get", (JsCall c) => array.GetIndex(Number(c))));
		array.Set("size", JsFunction.FromNative("size", (JsCall c) => new JsNumber(array.Length)));
		array.Set("isEmpty", JsFunction.FromNative("isEmpty", (JsCall c) => JsBoolean.Of(array.Length == 0)));
		return array;
	}
}
