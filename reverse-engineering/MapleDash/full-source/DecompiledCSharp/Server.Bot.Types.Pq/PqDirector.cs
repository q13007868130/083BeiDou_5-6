using System;
using System.Collections.Generic;
using Server.Bot.Environment;
using Server.Field;

namespace Server.Bot.Types.Pq;

public static class PqDirector
{
	public delegate bool BotTalkHandler(BotCharacter bot, int npcId, out string say);

	private sealed class Lobby
	{
		public PqDef def;

		public readonly List<int> bots = new List<int>();

		public int want;

		public int anchorX = int.MinValue;

		public int anchorY;

		public bool spawning;

		public long nextChatAt;

		public long fillAt;

		public long enterAt;

		public long fallbackAt;

		public int hostBot;

		public bool leadershipGiven;
	}

	public static Action<int> WarpLocalPlayer;

	public static Func<int, bool> LoadMap;

	public static Action<int> PrefetchMap;

	public static Func<int> LocalPlayerId = () => 0;

	public static Func<int> LocalMapId = () => 0;

	public static Action<int, int> GrantItem;

	public static Action<int, bool, string[], Action<bool>> TalkNpc;

	public static BotTalkHandler BotTalkNpc;

	public static Action<string> Notice;

	private const int PartySize = 6;

	private const long TickMs = 500L;

	private const long FillIntervalMs = 1600L;

	private const long EnterFallbackMs = 30000L;

	private const long EnterHoldMs = 2200L;

	private const long ChatMinMs = 6000L;

	private const long ChatMaxMs = 15000L;

	private static readonly Dictionary<string, Lobby> lobbies = new Dictionary<string, Lobby>();

	private static readonly Dictionary<int, PqRun> runs = new Dictionary<int, PqRun>();

	private static readonly Random rng = new Random();

	private static BotTasks.Handle tickHandle;

	private static int localMap = int.MinValue;

	private static int pendingEnterParty;

	private static long pendingEnterAt;

	private static List<int> prefetchIds;

	private static int prefetchIndex;

	public static Func<int, int> LiveMapId;

	public static Action<int> EnterInstance;

	public static Action<int> LeaveInstance;

	public static Func<int, int> InstanceGrid;

	public static Func<string, string> InstanceProperty;

	public static Func<BotCharacter, int, int, int> PickupItem;

	public static Func<BotCharacter, int, float, bool> WalkToItem;

	public static Action<BotCharacter, int, int> BotItem;

	private static DbStore Db => BotClient.GetBotClient()?.db;

	private static BotPartyService Parties => BotTypeManager.Parties;

	public static bool Enabled => BotConfig.PqLobbyBotsEnabled(Db);

	internal static Random Random => rng;

	public static int TemplateMapId(int mapId)
	{
		MapRuntime mapRuntime = MapService.Get(mapId);
		if (mapRuntime == null || mapRuntime.templateMapId <= 0)
		{
			return mapId;
		}
		return mapRuntime.templateMapId;
	}

	public static int Live(int mapId)
	{
		if (LiveMapId == null)
		{
			return mapId;
		}
		return LiveMapId(mapId);
	}

	public static void Reset()
	{
		tickHandle?.Cancel();
		tickHandle = null;
		lobbies.Clear();
		runs.Clear();
		localMap = int.MinValue;
		pendingEnterParty = 0;
		pendingEnterAt = 0L;
		prefetchIds = null;
		prefetchIndex = 0;
	}

	public static void PrefetchStep()
	{
		if (PrefetchMap == null)
		{
			return;
		}
		HashSet<int> seen;
		if (prefetchIds == null)
		{
			prefetchIds = new List<int>();
			seen = new HashSet<int>();
			foreach (PqDef item in PqCatalog.All)
			{
				Add(item.recruitMap);
				Add(item.entryMap);
				Add(item.exitMap);
				foreach (PqStage stage in item.stages)
				{
					Add(stage.mapId);
				}
				foreach (int extraMap in item.extraMaps)
				{
					Add(extraMap);
				}
				foreach (PqStage stage2 in item.stages)
				{
					foreach (PqStep step in stage2.steps)
					{
						if (step.maps != null)
						{
							int[] maps = step.maps;
							for (int i = 0; i < maps.Length; i++)
							{
								Add(maps[i]);
							}
						}
					}
				}
			}
		}
		if (prefetchIndex >= prefetchIds.Count)
		{
			return;
		}
		int obj = prefetchIds[prefetchIndex++];
		try
		{
			PrefetchMap(obj);
		}
		catch (Exception)
		{
		}
		void Add(int id)
		{
			if (id > 0 && seen.Add(id))
			{
				prefetchIds.Add(id);
			}
		}
	}

	public static void PrefetchAll()
	{
		if (PrefetchMap == null)
		{
			return;
		}
		if (prefetchIds == null)
		{
			PrefetchStep();
		}
		while (prefetchIds != null && prefetchIndex < prefetchIds.Count)
		{
			int obj = prefetchIds[prefetchIndex++];
			try
			{
				PrefetchMap(obj);
			}
			catch (Exception)
			{
			}
		}
	}

	private static void EnsureTick()
	{
		if (tickHandle == null || tickHandle.Cancelled || tickHandle.Completed)
		{
			tickHandle = BotTasks.ScheduleFixedDelay(Tick, 500L, 500L, "EnsureTick", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Types/Pq/PqDirector.cs");
		}
	}

	public static void OnLocalMap(int mapId)
	{
		if (mapId == localMap)
		{
			return;
		}
		localMap = mapId;
		if (!Enabled)
		{
			return;
		}
		EnsureTick();
		PqDef pqDef = PqCatalog.ForMap(mapId);
		if (pqDef != null)
		{
			EnsureLobby(pqDef);
			return;
		}
		PqDef pqDef2 = PqCatalog.ByStageMap(TemplateMapId(mapId));
		foreach (Lobby item in new List<Lobby>(lobbies.Values))
		{
			if (item.def.recruitMap != mapId && item.def != pqDef2)
			{
				RetireIdle(item);
			}
		}
	}

	private static Lobby LobbyOf(PqDef def)
	{
		if (def == null)
		{
			return null;
		}
		if (!lobbies.TryGetValue(def.key, out var value))
		{
			Dictionary<string, Lobby> dictionary = lobbies;
			string key = def.key;
			Lobby lobby = new Lobby
			{
				def = def,
				want = BotConfig.PqLobbyBotCount(Db)
			};
			value = lobby;
			dictionary[key] = lobby;
		}
		return value;
	}

	private static void EnsureLobby(PqDef def, int attempt = 0)
	{
		Lobby lobby = LobbyOf(def);
		if (lobby == null || lobby.spawning || lobby.want <= 0)
		{
			return;
		}
		if (!MapService.Has(def.recruitMap))
		{
			bool flag = false;
			try
			{
				flag = LoadMap != null && LoadMap(def.recruitMap);
			}
			catch (ArgumentException)
			{
				flag = false;
			}
			if (!flag)
			{
				if (attempt < 180 && localMap == def.recruitMap)
				{
					BotEnvironment.PostNextFrame(() =>
					{
						EnsureLobby(def, attempt + 1);
					});
				}
				return;
			}
		}
		lobby.bots.RemoveAll((int id) => BotRegistry.Get(id) == null);
		if (lobby.bots.Count < lobby.want)
		{
			lobby.spawning = true;
			int index = lobby.bots.Count;
			BotEnvironment.Post(() =>
			{
				SpawnOne(lobby, index);
			}, "EnsureLobby");
		}
	}

	private static void SpawnOne(Lobby lobby, int index)
	{
		if (lobby.def == null)
		{
			return;
		}
		if (localMap != lobby.def.recruitMap || !Enabled || !lobbies.TryGetValue(lobby.def.key, out var value) || value != lobby)
		{
			lobby.spawning = false;
			return;
		}
		(int x, int y) tuple = Seat(lobby, index);
		int item = tuple.x;
		int item2 = tuple.y;
		BotCharacter botCharacter = null;
		try
		{
			botCharacter = BotGeneration.CreateBotPollReadiness(item, item2, lobby.def.recruitMap, ignoreEnvironmentLimit: true, playArrival: false);
		}
		catch (Exception)
		{
			botCharacter = null;
		}
		if (botCharacter == null)
		{
			lobby.spawning = false;
			return;
		}
		botCharacter.level = Math.Max(1, lobby.def.minLevel + rng.Next(0, 13));
		if (index == 0 && lobby.anchorX == int.MinValue)
		{
			lobby.anchorX = (int)botCharacter.x;
			lobby.anchorY = (int)botCharacter.y;
		}
		lobby.bots.Add(botCharacter.id);
		BotTypeManager.SetAndStartBots(new List<int> { botCharacter.id }, BotType.PQ_BOT);
		if (index + 1 < lobby.want)
		{
			BotEnvironment.PostNextFrame(() =>
			{
				SpawnOne(lobby, index + 1);
			});
		}
		else
		{
			lobby.spawning = false;
		}
	}

	private static (int x, int y) Seat(Lobby lobby, int index)
	{
		MapRuntime mapRuntime = MapService.Get(lobby.def.recruitMap);
		int num = lobby.anchorX;
		if (num == int.MinValue)
		{
			MapNpcPosition mapNpcPosition = mapRuntime?.npcs.Find((MapNpcPosition n) => n.id == lobby.def.recruitNpc);
			if (mapNpcPosition == null)
			{
				num = ((mapRuntime != null && mapRuntime.maxX > mapRuntime.minX) ? ((int)((mapRuntime.minX + mapRuntime.maxX) / 2f)) : 0);
			}
			else
			{
				num = mapNpcPosition.x;
				lobby.anchorY = mapNpcPosition.y;
			}
			lobby.anchorX = num;
		}
		Math.Max(1, lobby.want);
		int num2 = index / 2;
		int num3 = index % 2;
		int num4 = num + ((num3 != 0) ? 1 : (-1)) * (28 + num2 * 34) + rng.Next(-10, 11);
		int num5 = ((lobby.anchorY != 0) ? lobby.anchorY : 0);
		return (x: num4, y: (int)MapService.SnapToGround(lobby.def.recruitMap, num4, num5));
	}

	private static void RetireIdle(Lobby lobby)
	{
		BotPartyService.Party party = Parties?.For(LocalPlayerId());
		for (int num = lobby.bots.Count - 1; num >= 0; num--)
		{
			BotCharacter botCharacter = BotRegistry.Get(lobby.bots[num]);
			if (botCharacter == null)
			{
				lobby.bots.RemoveAt(num);
			}
			else if (party == null || !party.members.Contains(botCharacter.id))
			{
				if (botCharacter.mapId != lobby.def.recruitMap)
				{
					lobby.bots.RemoveAt(num);
				}
				else
				{
					BotTypeManager.ManuallyStopBot(botCharacter);
					BotGeneration.RemoveBotFromServer(botCharacter);
					lobby.bots.RemoveAt(num);
				}
			}
		}
	}

	public static void OnChat(BotActor player, string text)
	{
		if (!Enabled || player == null || player.IsBot() || string.IsNullOrEmpty(text))
		{
			return;
		}
		PqDef pqDef = PqCatalog.ForMap(player.mapId);
		if (pqDef != null && Matches(pqDef, text))
		{
			Lobby lobby = LobbyOf(pqDef);
			if (lobby != null)
			{
				EnsureTick();
				React(lobby, player);
			}
		}
	}

	public static bool Matches(PqDef def, string text)
	{
		if (def == null || string.IsNullOrEmpty(text))
		{
			return false;
		}
		string[] keywords = def.keywords;
		foreach (string value in keywords)
		{
			if (!string.IsNullOrEmpty(value) && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static void React(Lobby lobby, BotActor player)
	{
		List<BotCharacter> list = FreeBots(lobby, 3);
		if (list.Count == 0)
		{
			return;
		}
		foreach (BotCharacter item in list)
		{
			BotTypeManager.Output.Speak(item, PqChatLines.Answer(rng));
		}
		BotPartyService.Party party = Parties?.For(player.id);
		if (party == null)
		{
			BotStartsParty(lobby, player, list[0]);
			return;
		}
		if (party.leader != player.id)
		{
			BotTypeManager.Output.Speak(list[0], "你不是队长呀，让队长来开。");
			return;
		}
		foreach (BotCharacter item2 in list)
		{
			if (party.members.Count >= 6)
			{
				break;
			}
			if (TryJoin(item2, party))
			{
				lobby.enterAt = 0L;
				break;
			}
		}
	}

	private static void BotStartsParty(Lobby lobby, BotActor player, BotCharacter host)
	{
		if (Parties != null && host != null)
		{
			if (!Parties.Create(host))
			{
				BotTypeManager.Output.Speak(host, "我这边开不了队，等我一下。");
			}
			else if (Parties.For(host.id) != null)
			{
				lobby.hostBot = host.id;
				lobby.leadershipGiven = false;
				BotTypeManager.Output.Speak(host, "我开个队，缺人，来一个。" + lobby.def.name + "的");
				Parties.Invite(host, player);
			}
		}
	}

	private static bool TryJoin(BotCharacter bot, BotPartyService.Party party)
	{
		if (bot == null || party == null || Parties == null)
		{
			return false;
		}
		if (party.members.Count >= 6)
		{
			return false;
		}
		if (Parties.For(bot.id) != null)
		{
			return false;
		}
		if (!Parties.Join(bot, party.id))
		{
			return false;
		}
		BotTypeManager.Output.Speak(bot, PqChatLines.Role(KeyOf(party), rng));
		return true;
	}

	private static string KeyOf(BotPartyService.Party party)
	{
		if (party == null)
		{
			return null;
		}
		foreach (Lobby lobby in lobbies.Values)
		{
			if (lobby.bots.Contains(party.leader) || party.members.Exists((int m) => lobby.bots.Contains(m)))
			{
				return lobby.def.key;
			}
		}
		return null;
	}

	private static List<BotCharacter> FreeBots(Lobby lobby, int max)
	{
		List<BotCharacter> list = new List<BotCharacter>();
		foreach (int bot in lobby.bots)
		{
			if (list.Count >= max)
			{
				break;
			}
			BotCharacter botCharacter = BotRegistry.Get(bot);
			if (botCharacter != null && botCharacter.mapId == lobby.def.recruitMap && (Parties == null || Parties.For(bot) == null) && !partiesRunContains(bot))
			{
				list.Add(botCharacter);
			}
		}
		return list;
	}

	private static bool partiesRunContains(int botId)
	{
		foreach (PqRun value in runs.Values)
		{
			if (value.Contains(botId))
			{
				return true;
			}
		}
		return false;
	}

	private static void Tick()
	{
		if (!Enabled)
		{
			return;
		}
		long num = BotScheduler.Now();
		foreach (PqRun item in new List<PqRun>(runs.Values))
		{
			item.Tick(num);
		}
		BotActor botActor = LocalPlayer();
		if (botActor == null)
		{
			return;
		}
		PqDef pqDef = PqCatalog.ForMap(botActor.mapId);
		if (pqDef == null)
		{
			AdoptStageEntry(botActor, num);
			return;
		}
		Lobby lobby = LobbyOf(pqDef);
		if (lobby != null)
		{
			AnswerInvitations(lobby);
			HandOverLeadership(lobby, botActor, num);
			ChatAmbient(lobby, num);
			AutoFill(lobby, botActor, num);
			CheckEnter(lobby, botActor, num);
		}
	}

	private static void AdoptStageEntry(BotActor player, long ms)
	{
		PqDef pqDef = PqCatalog.ByStageMap(TemplateMapId(player.mapId));
		if (pqDef == null || Parties == null)
		{
			return;
		}
		BotPartyService.Party party = Parties.For(player.id);
		if (party == null || runs.ContainsKey(party.id))
		{
			return;
		}
		if (party.leader != player.id)
		{
			BotActor botActor = Parties.Get(party.leader);
			if (botActor == null || !botActor.IsBot() || !Parties.ChangeLeader(party.leader, player.id))
			{
				return;
			}
		}
		Lobby lobby = LobbyOf(pqDef);
		if (lobby != null)
		{
			foreach (int item in new List<int>(lobby.bots))
			{
				if (party.members.Count >= 6)
				{
					break;
				}
				BotCharacter botCharacter = BotRegistry.Get(item);
				if (botCharacter != null && botCharacter.mapId == pqDef.recruitMap)
				{
					TryJoin(botCharacter, party);
				}
			}
		}
		Start(pqDef, party.id, player.id, openViaNpc: false);
	}

	private static void AnswerInvitations(Lobby lobby)
	{
		if (Parties == null)
		{
			return;
		}
		int num = LocalPlayerId();
		foreach (int bot in lobby.bots)
		{
			BotPartyService.Invitation invitation = Parties.Pending(bot);
			if (invitation == null)
			{
				continue;
			}
			BotCharacter botCharacter = BotRegistry.Get(bot);
			if (botCharacter != null && invitation.inviter != null && invitation.inviter.id == num)
			{
				BotPartyService.Party party = Parties.ById(invitation.party);
				bool flag = party != null && party.members.Count < 6 && Parties.For(bot) == null;
				Parties.Answer(botCharacter, invitation.party, flag);
				if (flag)
				{
					BotTypeManager.Output.Speak(botCharacter, "谢了，我进队了。" + PqChatLines.Role(lobby.def.key, rng));
					lobby.hostBot = 0;
					lobby.enterAt = 0L;
					lobby.fallbackAt = 0L;
				}
				else
				{
					BotTypeManager.Output.Speak(botCharacter, PqChatLines.Decline(rng));
				}
			}
		}
	}

	private static void HandOverLeadership(Lobby lobby, BotActor player, long ms)
	{
		if (lobby.leadershipGiven || Parties == null)
		{
			return;
		}
		BotPartyService.Party party = Parties.For(player.id);
		if (party == null || party.leader == player.id)
		{
			lobby.leadershipGiven = lobby.hostBot == 0;
		}
		else if (lobby.hostBot != 0 && party.leader == lobby.hostBot && Parties.ChangeLeader(party.leader, player.id))
		{
			lobby.leadershipGiven = true;
			BotCharacter botCharacter = BotRegistry.Get(lobby.hostBot);
			if (botCharacter != null)
			{
				BotTypeManager.Output.Speak(botCharacter, "队长给你，NPC 只有队长点得动。");
			}
			lobby.hostBot = 0;
			lobby.fallbackAt = ms + 30000;
		}
	}

	private static void ChatAmbient(Lobby lobby, long ms)
	{
		if (ms < lobby.nextChatAt)
		{
			return;
		}
		lobby.nextChatAt = ms + 6000 + rng.Next(9000);
		List<int> bots = lobby.bots;
		if (bots.Count != 0)
		{
			BotCharacter botCharacter = BotRegistry.Get(bots[rng.Next(bots.Count)]);
			if (botCharacter != null && botCharacter.mapId == lobby.def.recruitMap)
			{
				bool flag = rng.Next(100) < 55;
				BotTypeManager.Output.Speak(botCharacter, flag ? PqChatLines.Recruit(lobby.def.key, rng) : PqChatLines.Idle(rng));
			}
		}
	}

	private static void AutoFill(Lobby lobby, BotActor player, long ms)
	{
		if (Parties == null)
		{
			return;
		}
		BotPartyService.Party party = Parties.For(player.id);
		if (party == null || party.leader != player.id)
		{
			lobby.fillAt = 0L;
		}
		else
		{
			if (party.members.Count >= 6)
			{
				return;
			}
			if (lobby.fallbackAt == 0L)
			{
				lobby.fallbackAt = ms + 30000;
			}
			if (ms >= lobby.fillAt)
			{
				lobby.fillAt = ms + 1600;
				List<BotCharacter> list = FreeBots(lobby, 1);
				if (list.Count != 0)
				{
					TryJoin(list[0], party);
				}
			}
		}
	}

	private static void CheckEnter(Lobby lobby, BotActor player, long ms)
	{
		if (Parties == null || lobby.def == null || pendingEnterParty != 0)
		{
			return;
		}
		BotPartyService.Party party = Parties.For(player.id);
		if (party == null || party.leader != player.id)
		{
			lobby.enterAt = 0L;
		}
		else
		{
			if (runs.ContainsKey(party.id))
			{
				return;
			}
			bool flag = party.members.Count >= 6;
			bool flag2 = lobby.fallbackAt != 0L && ms >= lobby.fallbackAt && party.members.Count >= 3;
			if (!flag && !flag2)
			{
				return;
			}
			if (lobby.enterAt == 0L)
			{
				lobby.enterAt = ms + 2200;
				{
					foreach (int bot in lobby.bots)
					{
						BotCharacter botCharacter = BotRegistry.Get(bot);
						if (botCharacter != null && party.members.Contains(bot))
						{
							BotTypeManager.Output.Speak(botCharacter, PqChatLines.beforeEnter[rng.Next(PqChatLines.beforeEnter.Length)]);
						}
					}
					return;
				}
			}
			if (ms >= lobby.enterAt)
			{
				lobby.enterAt = 0L;
				Start(lobby.def, party.id, player.id);
			}
		}
	}

	public static PqRun Start(PqDef def, int partyId, int leaderId, bool openViaNpc = true)
	{
		if (def == null || partiesRunHas(partyId))
		{
			return null;
		}
		PqRun pqRun = new PqRun(def, partyId, leaderId);
		runs[partyId] = pqRun;
		pqRun.Begin(openViaNpc);
		return pqRun;
	}

	private static bool partiesRunHas(int partyId)
	{
		return runs.ContainsKey(partyId);
	}

	public static PqRun RunOf(int partyId)
	{
		if (!runs.TryGetValue(partyId, out var value))
		{
			return null;
		}
		return value;
	}

	public static PqRun RunOfBot(int botId)
	{
		foreach (PqRun value in runs.Values)
		{
			if (value.Contains(botId))
			{
				return value;
			}
		}
		return null;
	}

	public static void Finish(PqRun run)
	{
		if (run == null)
		{
			return;
		}
		runs.Remove(run.partyId);
		Parties?.Leave(run.leaderId);
		foreach (Lobby value in lobbies.Values)
		{
			value.enterAt = 0L;
			value.fillAt = 0L;
			value.fallbackAt = 0L;
			value.hostBot = 0;
			value.leadershipGiven = false;
		}
	}

	public static BotActor LocalPlayer()
	{
		int num = LocalPlayerId();
		if (num == 0)
		{
			return null;
		}
		BotActor botActor = Parties?.Get(num);
		if (botActor == null || !botActor.online || botActor.IsBot())
		{
			return null;
		}
		return botActor;
	}

	public static bool WarpBot(BotCharacter bot, int mapId)
	{
		if (bot == null || mapId == 0 || (mapId < 0 && !MapService.Has(mapId)))
		{
			return false;
		}
		if (bot.mapId != mapId)
		{
			if (!MapService.Has(mapId))
			{
				bool flag = false;
				try
				{
					flag = LoadMap != null && LoadMap(mapId);
				}
				catch (ArgumentException)
				{
					flag = false;
				}
				if (!flag)
				{
					return false;
				}
			}
			BotRegistry.EnterMap(bot, mapId);
		}
		var (num, num2) = EntryPoint(mapId);
		BotMovementService.Stop(bot);
		bot.x = num;
		bot.y = num2;
		return true;
	}

	public static (int x, int y) EntryPoint(int mapId)
	{
		MapRuntime mapRuntime = MapService.Get(mapId);
		if (mapRuntime == null)
		{
			return (x: 0, y: 0);
		}
		foreach (MapPortal portal in mapRuntime.portals)
		{
			if (portal.id == 0)
			{
				return (x: portal.x, y: (int)MapService.SnapToGround(mapId, portal.x, portal.y));
			}
		}
		if (mapRuntime.portals.Count > 0)
		{
			MapPortal mapPortal = mapRuntime.portals[0];
			return (x: mapPortal.x, y: (int)MapService.SnapToGround(mapId, mapPortal.x, mapPortal.y));
		}
		int num = ((mapRuntime.maxX > mapRuntime.minX) ? ((int)((mapRuntime.minX + mapRuntime.maxX) / 2f)) : 0);
		return (x: num, y: (int)MapService.SnapToGround(mapId, num, mapRuntime.minY));
	}

	public static void WarpPlayer(int mapId)
	{
		if (mapId <= 0)
		{
			return;
		}
		try
		{
			WarpLocalPlayer?.Invoke(mapId);
		}
		catch (Exception)
		{
		}
	}

	public static void Talk(int npcId, bool openOnly, string[] prefer, Action<bool> done)
	{
		if (TalkNpc == null)
		{
			done?.Invoke(obj: true);
			return;
		}
		try
		{
			TalkNpc(npcId, openOnly, prefer, done);
		}
		catch (Exception)
		{
			done?.Invoke(obj: false);
		}
	}

	public static void Give(int itemId, int count)
	{
		if (itemId > 0 && count > 0)
		{
			GrantItem?.Invoke(itemId, count);
		}
	}

	public static bool BotTalk(BotCharacter bot, int npcId, out string say)
	{
		say = null;
		if (bot == null || npcId == 0 || BotTalkNpc == null)
		{
			return false;
		}
		try
		{
			return BotTalkNpc(bot, npcId, out say);
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static bool EnterEvent(int botId)
	{
		if (EnterInstance == null)
		{
			return false;
		}
		try
		{
			EnterInstance(botId);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static void LeaveEvent(int botId)
	{
		try
		{
			LeaveInstance?.Invoke(botId);
		}
		catch (Exception)
		{
		}
	}

	public static int GridOf(int botId)
	{
		try
		{
			return (InstanceGrid != null) ? InstanceGrid(botId) : (-1);
		}
		catch (Exception)
		{
			return -1;
		}
	}

	public static string PropertyOf(string key)
	{
		try
		{
			return (InstanceProperty != null) ? InstanceProperty(key) : null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static int Pickup(BotCharacter bot, int itemId, int max = int.MaxValue)
	{
		if (bot == null || itemId <= 0 || max <= 0 || PickupItem == null)
		{
			return 0;
		}
		try
		{
			return PickupItem(bot, itemId, max);
		}
		catch (Exception)
		{
			return 0;
		}
	}

	public static bool WalkToDrop(BotCharacter bot, int itemId, float searchRange = float.MaxValue)
	{
		if (bot == null || itemId <= 0 || WalkToItem == null)
		{
			return false;
		}
		try
		{
			return WalkToItem(bot, itemId, searchRange);
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static void BotGain(BotCharacter bot, int itemId, int count)
	{
		if (bot == null || itemId <= 0 || count == 0 || BotItem == null)
		{
			return;
		}
		try
		{
			BotItem(bot, itemId, count);
		}
		catch (Exception)
		{
		}
	}

	public static void Say(BotCharacter bot, string text)
	{
		if (bot != null && !string.IsNullOrEmpty(text))
		{
			BotTypeManager.Output.Speak(bot, text);
		}
	}

	public static void Notify(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			Notice?.Invoke(text);
		}
	}
}
