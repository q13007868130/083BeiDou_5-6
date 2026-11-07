using System;
using System.Collections.Generic;
using Server.Bot.Environment;
using Server.Bot.Events;
using Server.Bot.Social;
using Server.Bot.Trade;
using Server.Bot.Types.Opq;
using Server.Bot.Types.Pq;
using Server.Field;

namespace Server.Bot;

public sealed class BotRuntime : IDisposable
{
	private readonly BotNavigationMapLoader maps;

	private readonly Dictionary<int, BotActor> players = new Dictionary<int, BotActor>();

	private readonly HashSet<int> registeredMaps = new HashSet<int>();

	public readonly BotObserverTracker observers;

	public readonly BotTownSpawner townSpawner;

	public readonly BotMapEntryResponder mapEntryResponder;

	private readonly BotDispatcher dispatcher;

	public readonly BotTradeService trades;

	public readonly BotTradeProtocol tradeProtocol;

	public readonly BotPartyService parties;

	public readonly BotPartyGrindService partyGrind;

	public readonly MarketShopService shops;

	public readonly MarketShopProtocol marketProtocol;

	private Action<BotCharacter, byte[]> movement;

	private bool disposed;

	private readonly BotTasks.Handle invitationWatch;

	private readonly NativeReactorScriptService reactorScripts;

	private readonly Dictionary<int, long> whisperReplyAt = new Dictionary<int, long>();

	private static readonly Random whisperRng = new Random();

	public IEnumerable<BotActor> Players => players.Values;

	public event Action<BotCharacter, byte[]> Movement
	{
		add
		{
			movement = (Action<BotCharacter, byte[]>)Delegate.Combine(movement, value);
			SyncMovementSink();
		}
		remove
		{
			movement = (Action<BotCharacter, byte[]>)Delegate.Remove(movement, value);
			SyncMovementSink();
		}
	}

	private void SyncMovementSink()
	{
		BotMovementPlayback.Broadcast = (BotMovementService.Broadcast = ((movement != null) ? new Action<BotCharacter, byte[]>(PublishMovement) : null));
	}

	public BotRuntime(DbStore db, BotMessenger messenger = null, BotPartyService parties = null)
	{
		BotPerf.Scope scope = BotPerf.Timer("BotRuntime.Ctor");
		BotClient.Init(db);
		BotTypeManager.Messenger = messenger ?? new BotMessenger();
		this.parties = parties ?? new BotPartyService(db);
		invitationWatch = BotTasks.ScheduleFixedRate(() =>
		{
			this.parties.TickInvitationTimeouts();
			BotTypeManager.Messenger.TickInvitationTimeouts();
			trades?.TickInvitationTimeouts();
		}, 30000L, 30000L, ".ctor", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/BotRuntime.cs");
		trades = new BotTradeService(db, BotItemCatalog.Shared);
		BotTypeManager.Parties = this.parties;
		BotTypeManager.TradeService = trades;
		tradeProtocol = new BotTradeProtocol(trades, (int id) => (!players.TryGetValue(id, out var value)) ? null : value);
		trades.ChatOutput = (BotActor actor, string text) =>
		{
			BotTypeManager.Output.Send(BotOutput.Kind.TradeChat, actor, text, 0, actor.IsBot() ? (actor.trade?.partner?.actor.id).GetValueOrDefault() : actor.id);
		};
		maps = BotNavigationMapLoader.Load();
		shops = new MarketShopService(db, trades, GetMap);
		marketProtocol = new MarketShopProtocol(shops, (int id) => (!players.TryGetValue(id, out var value)) ? null : value);
		BotEnvironment.PopulateMarketRooms = shops.PopulateRooms;
		partyGrind = new BotPartyGrindService(this.parties, GetMap, null, db);
		BotTypeManager.PartyGrind = partyGrind;
		observers = new BotObserverTracker(() => players.Values, NonBlockingMap, BotWorldGraph.Load());
		townSpawner = new BotTownSpawner(new BotTownPresenceConfig(BotResources.Load()), TryGetMap);
		BotEnvironment.GetMap = GetMap;
		BotEnvironment.TryGetMap = TryGetMap;
		BotEnvironment.TownWanderers = () =>
		{
			townSpawner.Spawn();
		};
		mapEntryResponder = new BotMapEntryResponder(BotTypeManager.Events);
		mapEntryResponder.Register();
		dispatcher = new BotDispatcher(BotTypeManager.Messages, BotTypeManager.Output, CharactersOnMap, (BotStateMachine bot, BotActor player) =>
		{
			bot.DisplayCommands(player);
		});
		dispatcher.Start();
		BotMovementService.IsObserved = observers.IsActive;
		SyncMovementSink();
		BotMovementService.EnterPortal = EnterPortal;
		BotMovementService.ResolveProfile = (BotCharacter actor, MapRuntime map) => BotMovementProfile.FromCharacter(actor, (map.fieldLimit & 2) != 0, this.parties.Members(actor.id), BotMovementService.HasSnowShoes(actor));
		BotTypeManager.RealPlayersOnMap = observers.HasRealPlayerNow;
		BotLogic.OnlinePlayers = () => players.Values;
		MapItemService.PartyOf = (int id) => this.parties.For(id)?.id ?? (-1);
		MapItemService.IsPartyMember = (int ownerId, int characterId) => this.parties.For(ownerId)?.members.Contains(characterId) ?? false;
		MapItemService.Clock = () => BotScheduler.Clock;
		MapReactorService.Clock = () => BotScheduler.Clock;
		MapReactorService.Templates = MapReactorTemplate.Parse(BotWarmup.Root("Bot/ReactorTemplates"));
		NativeReactorDrops.Load(BotWarmup.Root("Bot/ReactorDrops"));
		MapItemService.Spawned += MapReactorService.ActivateItemReactors;
		reactorScripts = new NativeReactorScriptService((string key) => MapReactorService.ScriptSource?.Invoke(key));
		reactorScripts.Message = (int map, int type, string text) =>
		{
			MapReactorService.ScriptMessage?.Invoke(map, type, text);
		};
		reactorScripts.EventInstance = (int map) => MapReactorService.EventInstance?.Invoke(map);
		reactorScripts.Player = (int owner) => MapReactorService.Player?.Invoke(owner);
		reactorScripts.Map = (int map) => MapReactorService.MapHost?.Invoke(map);
		reactorScripts.SpawnMonster = (int map, int mob, float x, float y, int qty) => MapReactorService.SpawnMonsterAt?.Invoke(map, mob, x, y, qty) ?? false;
		reactorScripts.Error = (Exception error) =>
		{
			MapReactorService.ScriptError?.Invoke(error);
		};
		MapReactorService.Act = (MapReactor reactor, int owner) =>
		{
			reactorScripts.Act(reactor, owner);
		};
		OpqOrchestrator.LeaderMapOf = (int id) =>
		{
			BotActor botActor = this.parties.Get(id);
			BotPartyService.Party party = this.parties.For(id);
			BotActor botActor2 = ((party == null) ? null : this.parties.Get(party.leader));
			return (botActor == null || botActor2 == null || !botActor2.online || botActor2.world != botActor.world || botActor2.channel != botActor.channel) ? (-1) : botActor2.mapId;
		};
		PqDirector.LoadMap = (int mapId) =>
		{
			if (MapService.Get(mapId) != null)
			{
				return true;
			}
			try
			{
				MapRuntime map;
				return TryGetMap(mapId, out map) == BotMapState.Ready;
			}
			catch (ArgumentException)
			{
				return false;
			}
		};
		HashSet<string> prefetchedShards = new HashSet<string>();
		PqDirector.PrefetchMap = (int id) =>
		{
			try
			{
				string text = maps.ShardPath(id);
				if (!string.IsNullOrEmpty(text))
				{
					maps.PinShard(text);
				}
				if (MapService.Get(id) == null && !string.IsNullOrEmpty(text) && prefetchedShards.Add(text))
				{
					BotWarmup.Start(text);
				}
			}
			catch (ArgumentException)
			{
			}
		};
		observers.Start();
		if (BotConfig.MarketAutoStartEnabled(db))
		{
			BotEnvironment.RequestMarketStartup();
		}
		scope.Dispose();
	}

	private void PublishMovement(BotCharacter actor, byte[] packet)
	{
		movement?.Invoke(actor, packet);
	}

	private IEnumerable<BotActor> CharactersOnMap(ChatMessage message)
	{
		List<BotActor> list = new List<BotActor>();
		foreach (BotActor value in players.Values)
		{
			if (value.mapId == message.mapId && value.world == message.sender.world && value.channel == message.sender.channel)
			{
				list.Add(value);
			}
		}
		foreach (BotCharacter item in BotRegistry.OnMap(message.mapId))
		{
			if (item.world == message.sender.world && item.channel == message.sender.channel)
			{
				list.Add(item);
			}
		}
		list.Sort((BotActor a, BotActor b) => a.mapEntryOrder.CompareTo(b.mapEntryOrder));
		return list;
	}

	public bool ReceiveChat(int playerId, string text)
	{
		if (disposed || string.IsNullOrWhiteSpace(text) || text[0] == '/' || !players.TryGetValue(playerId, out var value))
		{
			return false;
		}
		BotTypeManager.Messages.Add(new ChatMessage(value, text), QueueType.PRIMARY);
		BotTypeManager.Events.Publish(EventFactory.ChatGeneral(value, text));
		PqDirector.OnChat(value, text);
		return true;
	}

	public bool ReceiveWhisper(int playerId, string targetName, string text)
	{
		if (disposed || string.IsNullOrWhiteSpace(text) || string.IsNullOrEmpty(targetName) || !players.TryGetValue(playerId, out var value))
		{
			return false;
		}
		if (string.Equals(targetName, value.name, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		BotActor target = parties.FindIgnoreCase(targetName, value.world);
		if (target == null || target.id == playerId)
		{
			return false;
		}
		long clock = BotScheduler.Clock;
		if (whisperReplyAt.TryGetValue(playerId, out var value2) && clock - value2 < 1200)
		{
			return true;
		}
		whisperReplyAt[playerId] = clock;
		BotTasks.Schedule(() =>
		{
			if (!disposed)
			{
				string text2 = WhisperLine();
				if (text2 != null)
				{
					BotTypeManager.Output.Send(BotOutput.Kind.Whisper, target, text2, 0, playerId);
				}
			}
		}, 800 + whisperRng.Next(1200), "ReceiveWhisper", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/BotRuntime.cs");
		return true;
	}

	private static string WhisperLine()
	{
		string[] array = new string[4] { "Greeting", "WhatsUp", "Interesting", "Rumors" };
		try
		{
			return BotTypeManager.Dialogue.RandomLine("SocialBotDialogue.yaml", "SocialBot", array[whisperRng.Next(array.Length)]);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private MapRuntime NonBlockingMap(int id)
	{
		MapRuntime mapRuntime = MapService.Get(id);
		if (mapRuntime != null)
		{
			return mapRuntime;
		}
		if (TryGetMap(id, out var map) != BotMapState.Ready)
		{
			return null;
		}
		return map;
	}

	public MapRuntime GetMap(int id)
	{
		MapRuntime mapRuntime = MapService.Get(id);
		if (mapRuntime != null)
		{
			return mapRuntime;
		}
		mapRuntime = maps.Geometry(id);
		MapService.Register(mapRuntime);
		registeredMaps.Add(id);
		return mapRuntime;
	}

	public BotMapState TryGetMap(int id, out MapRuntime map, bool allowBlocking = false)
	{
		map = MapService.Get(id);
		if (map != null)
		{
			return BotMapState.Ready;
		}
		if (allowBlocking)
		{
			try
			{
				map = GetMap(id);
			}
			catch (ArgumentException)
			{
				map = null;
			}
			if (map == null)
			{
				return BotMapState.Missing;
			}
			return BotMapState.Ready;
		}
		bool flag;
		try
		{
			flag = maps.TryGeometry(id, out map);
		}
		catch (Exception)
		{
			map = null;
			return BotMapState.Missing;
		}
		if (!flag || map == null)
		{
			map = null;
			return BotMapState.Pending;
		}
		MapService.Register(map);
		registeredMaps.Add(id);
		return BotMapState.Ready;
	}

	public bool EnterPortal(BotCharacter actor, MapPortal portal)
	{
		if (actor == null || portal == null || !portal.enabled)
		{
			return false;
		}
		try
		{
			MapRuntime mapRuntime = NonBlockingMap(portal.targetMap);
			if (mapRuntime == null)
			{
				return false;
			}
			MapPortal mapPortal = mapRuntime.portals.Find((MapPortal p) => p.name == portal.targetName) ?? mapRuntime.portals.Find((MapPortal p) => p.id == 0);
			if (mapPortal == null)
			{
				return false;
			}
			actor.x = mapPortal.x;
			actor.y = mapPortal.y;
			BotRegistry.EnterMap(actor, mapRuntime.mapId);
			return true;
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	public BotActor SynchronizePlayer(FullCharacter character, int mapId, float mapX, float mapY, int channel = 1)
	{
		if (disposed)
		{
			throw new ObjectDisposedException("BotRuntime");
		}
		if (character?.record == null)
		{
			throw new ArgumentNullException("character");
		}
		CharRecord record = character.record;
		int num = checked((int)record.id);
		int num2;
		if (players.TryGetValue(num, out var value) && value.mapId == mapId && value.channel == channel)
		{
			num2 = ((value.world != record.world) ? 1 : 0);
			if (num2 == 0)
			{
				goto IL_0095;
			}
		}
		else
		{
			num2 = 1;
		}
		value?.trade?.Decline();
		shops.Leave(num);
		goto IL_0095;
		IL_0095:
		if (value == null)
		{
			value = new BotActor
			{
				id = num
			};
			players.Add(num, value);
		}
		int level = value.level;
		bool flag = value.character != null;
		if (value.character != null && character.tradeRevision < value.character.tradeRevision)
		{
			FullCharacter fullCharacter = CharService.Load(BotClient.GetBotClient().db, num);
			character.inventory = fullCharacter.inventory;
			record.meso = fullCharacter.record.meso;
			character.tradeRevision = value.character.tradeRevision;
		}
		value.character = character;
		value.name = record.name;
		value.world = record.world;
		value.channel = channel;
		value.level = record.level;
		value.job = record.job;
		value.gender = record.gender;
		value.skinColor = record.skincolor;
		value.hair = record.hair;
		value.face = record.face;
		value.mapId = mapId;
		value.x = mapX;
		value.y = mapY;
		value.online = true;
		parties.Bind(value);
		if (num2 != 0)
		{
			value.MarkMapEntry();
			try
			{
				TryGetMap(mapId, out var _);
				observers.MarkObservedNow(mapId);
				observers.Refresh();
			}
			catch (ArgumentException)
			{
				observers.MarkObservedNow(mapId);
			}
			BotClient.EnsureStandaloneClient(BotClient.GetBotClient()?.db);
			BotTypeManager.Events.Publish(new GameEvent(value, EventType.MAP_ENTERED));
		}
		if (flag && value.level > level)
		{
			BotTypeManager.Events.Publish(EventFactory.LevelUp(value));
		}
		return value;
	}

	public void RemovePlayer(int id)
	{
		partyGrind.Stop(id);
		shops.Leave(id);
		if (players.TryGetValue(id, out var value))
		{
			value.trade?.Decline();
			value.online = false;
		}
		parties.EndSession(id);
		players.Remove(id);
		BotTypeManager.Messenger.Leave(id);
		whisperReplyAt.Remove(id);
		observers.Refresh();
	}

	public BotActor Player(int id)
	{
		if (!players.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public void Drive(long nowMs)
	{
		if (!disposed)
		{
			BotScheduler.Clock = nowMs;
			BotScheduler.Drive();
			MapItemService.Tick(nowMs);
			MapReactorService.Tick(nowMs);
		}
	}

	public void Dispose()
	{
		if (disposed)
		{
			return;
		}
		disposed = true;
		foreach (BotActor item in new List<BotActor>(players.Values))
		{
			item.trade?.Decline();
		}
		invitationWatch.Cancel();
		shops.Dispose();
		BotEnvironment.PopulateMarketRooms = null;
		partyGrind.Dispose();
		BotTypeManager.PartyGrind = null;
		parties.Clear();
		BotTypeManager.Parties = null;
		BotTypeManager.TradeService = null;
		dispatcher.Dispose();
		mapEntryResponder.Dispose();
		townSpawner.Dispose();
		observers.Dispose();
		foreach (BotCharacter item2 in new List<BotCharacter>(BotRegistry.All().Values))
		{
			BotGeneration.RemoveBotFromServer(item2);
		}
		BotMovementService.Shutdown();
		BotTypeManager.Market.Stop();
		BotTasks.Stop();
		BotScheduler.Stop();
		BotTypeManager.RealPlayersOnMap = null;
		BotLogic.OnlinePlayers = null;
		MapItemService.PartyOf = null;
		MapItemService.IsPartyMember = null;
		MapItemService.Clock = () => 0L;
		MapItemService.ClearAll();
		MapItemService.Spawned -= MapReactorService.ActivateItemReactors;
		MapReactorService.Clock = () => 0L;
		MapReactorService.ClearAll();
		MapReactorService.Act = null;
		reactorScripts.Dispose();
		OpqOrchestrator.LeaderMapOf = null;
		OpqOrchestrator.ShutdownRun();
		PqDirector.Reset();
		PqDirector.LoadMap = null;
		PqDirector.PrefetchMap = null;
		BotMovementService.EnterPortal = null;
		BotMovementService.ResolveProfile = null;
		BotMovementService.IsObserved = null;
		BotMovementService.Broadcast = null;
		BotMovementPlayback.Broadcast = null;
		BotTypeManager.Output.Clear();
		players.Clear();
		BotTypeManager.Messages.Clear();
		BotTypeManager.Ambient.Clear();
		BotTypeManager.Messenger.Clear();
		foreach (int registeredMap in registeredMaps)
		{
			MapService.Unregister(registeredMap);
		}
		registeredMaps.Clear();
		movement = null;
		SyncMovementSink();
		BotDirectory.Reset();
		BotEnvironment.GetMap = null;
		BotEnvironment.TryGetMap = null;
		BotEnvironment.TownWanderers = null;
		BotEnvironment.Reset();
	}
}
