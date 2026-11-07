using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Server.Bot.Types.Blackjack;
using Server.Field;

namespace Server.Bot.Environment;

public static class BotEnvironment
{
	public static Action PopulateMarketRooms;

	public static Action<Exception> StartupError;

	private static int startupErrorCount;

	private const int FMEntrance = 910000000;

	private const int Henesys = 100000000;

	private const int HenesysMarket = 100000100;

	private const int HenesysPark = 100000200;

	public const int MapWaitFrames = 180;

	private static readonly object gate = new object();

	private static bool startupStarted;

	private static bool startupComplete;

	private static bool limitDisableScheduled;

	private static int marketEntranceBotCount;

	private static int fmEntranceCapacity = -1;

	private static readonly Random rng = new Random();

	private static PlatformParser platforms;

	public static Func<int, MapRuntime> GetMap;

	public static BotMapLoad TryGetMap;

	public static Action TownWanderers;

	private static BotResources res;

	private static readonly Queue<Action> steps = new Queue<Action>();

	private static BotTasks.Handle pumpTask;

	private static bool yieldRequested;

	public static int StartupErrorCount => startupErrorCount;

	private static DbStore Db => BotClient.GetBotClient()?.db;

	private static BotResources Res => res ?? (res = BotResources.Load());

	private static PlatformParser Platforms => platforms ?? (platforms = new PlatformParser((string path) => Res.ReadText(path), (string dir) => Res.ListFileStems(dir)));

	public static bool IsMarketStartupComplete => startupComplete;

	public static bool IsMarketStartupRunning
	{
		get
		{
			if (startupStarted)
			{
				return !startupComplete;
			}
			return false;
		}
	}

	public static void Post(Action step, [CallerMemberName] string caller = "")
	{
		if (step == null)
		{
			return;
		}
		lock (steps)
		{
			steps.Enqueue(() =>
			{
				using (BotPerf.Timer("BotEnvironment.Step." + caller))
				{
					step();
				}
			});
		}
		EnsurePump();
	}

	public static void PostNextFrame(Action step)
	{
		if (step != null)
		{
			lock (steps)
			{
				steps.Enqueue(step);
			}
			yieldRequested = true;
			EnsurePump();
		}
	}

	private static void EnsurePump()
	{
		if (pumpTask == null || pumpTask.Cancelled || pumpTask.Completed)
		{
			pumpTask = BotTasks.Schedule(PumpStep, 0L, "EnsurePump", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Environment/BotEnvironment.cs");
		}
	}

	private static void PumpStep()
	{
		pumpTask = null;
		yieldRequested = false;
		int num = 0;
		do
		{
			Action action;
			lock (steps)
			{
				action = ((steps.Count > 0) ? steps.Dequeue() : null);
			}
			if (action == null)
			{
				FinishStartup();
				return;
			}
			try
			{
				using (BotPerf.Timer("BotEnvironment.Step"))
				{
					action();
				}
			}
			catch (Exception obj)
			{
				startupErrorCount++;
				StartupError?.Invoke(obj);
			}
			if (yieldRequested)
			{
				yieldRequested = false;
				EnsurePump();
				return;
			}
		}
		while (BotWorkPump.Has(0.75f) && ++num <= 512);
		EnsurePump();
	}

	private static void FinishStartup()
	{
		if (startupStarted && !startupComplete)
		{
			startupComplete = true;
			if (!limitDisableScheduled)
			{
				limitDisableScheduled = true;
				BotTasks.Schedule(BotGeneration.DisableEnvironmentBotLimit, 120000L, "FinishStartup", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Environment/BotEnvironment.cs");
			}
		}
	}

	private static void AbortStartup()
	{
		lock (gate)
		{
			startupStarted = false;
			startupComplete = false;
		}
		lock (steps)
		{
			steps.Clear();
		}
	}

	public static void Reset()
	{
		lock (gate)
		{
			startupStarted = false;
			startupComplete = false;
			limitDisableScheduled = false;
			marketEntranceBotCount = 0;
			startupErrorCount = 0;
		}
		lock (steps)
		{
			steps.Clear();
		}
		pumpTask = null;
		yieldRequested = false;
	}

	public static void RequestMarketStartup()
	{
		if (!startupStarted)
		{
			BotTasks.Schedule(MarketEnvironmentStartup, 0L, "RequestMarketStartup", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Environment/BotEnvironment.cs");
		}
	}

	public static void MarketEnvironmentStartup()
	{
		lock (gate)
		{
			if (startupStarted)
			{
				return;
			}
			startupStarted = true;
		}
		BotGeneration.EnableEnvironmentBotLimit();
		lock (steps)
		{
			steps.Clear();
		}
		Post(() =>
		{
			PopulateMarketRooms?.Invoke();
		}, "MarketEnvironmentStartup");
		Post(PlanWave1, "MarketEnvironmentStartup");
	}

	private static void PlanWave1()
	{
		EnsureMapThen(910000000, () =>
		{
			PlanScaledFMEntranceSeats();
			PlanMerchantSeats();
			PlanSocialSeats();
			Post(PlanWave2, "PlanWave1");
		});
	}

	private static void PlanScaledFMEntranceSeats()
	{
		if (BotConfig.FmBotsEnabled(Db))
		{
			int[] array = DistributeMarketEntranceWanderers(BotConfig.MarketBotMax(Db));
			PlanPlatformSeats(910000000, "m1", array[0], BotType.FM_BOT);
			PlanPlatformSeats(910000000, "m2", array[1], BotType.FM_BOT);
			PlanPlatformSeats(910000000, "m5", array[2], BotType.FM_BOT);
		}
	}

	public static int[] DistributeMarketEntranceWanderers(int cap)
	{
		int num = Math.Max(0, cap);
		int num2 = num * 2 / 3;
		if (num > 0 && num2 < 3)
		{
			num2 = Math.Min(num, 3);
		}
		int num3 = num2 / 3;
		int num4 = num2 / 3;
		int num5 = num2 - num3 - num4;
		return new int[3] { num3, num4, num5 };
	}

	private static void PlanMerchantSeats()
	{
		if (BotConfig.FmMerchantsEnabled(Db))
		{
			int num = BotConfig.MarketBotMax(Db);
			int num2 = Math.Max(2, num / 6);
			int num3 = Math.Max(1, num / 8);
			int nx = Math.Max(1, num / 15);
			PlanMerchantBatch("m1", Math.Max(1, num2 / 2), Math.Max(1, num3 / 2), 0);
			PlanMerchantBatch("m2", num2 - num2 / 2, num3 - num3 / 2, 0);
			PlanMerchantBatch("m5", 0, 0, nx);
		}
	}

	private static void PlanMerchantBatch(string platform, int selling, int buying, int nx)
	{
		PlanPlatformSeats(910000000, platform, selling, BotType.SELLING_MERCHANT_BOT);
		PlanPlatformSeats(910000000, platform, buying, BotType.BUYING_MERCHANT_BOT);
		PlanPlatformSeats(910000000, platform, nx, BotType.NX_MERCHANT_BOT);
	}

	private static void PlanSocialSeats()
	{
		EnqueueBotSpawns(FillerSeats(3, 910000000, new BotPoint(-260, 34), new BotPoint(330, 34)), 910000000, BotType.SOCIAL_BOT);
	}

	private static void PlanWave2()
	{
		EnsureMapThen(910000000, () =>
		{
			if (BotConfig.GachaBotsEnabled(Db))
			{
				EnqueueBotSpawns(RadiusSeats(2, 910000000, "m5", new BotPoint(40, 34), 250), 910000000, BotType.GACHA_BOT);
			}
			PlanBlackjackTable();
			PlanOpqLobbyBots();
			Post(PlanHenesys, "PlanWave2");
		});
	}

	private static void PlanBlackjackTable()
	{
		if (BotConfig.BlackjackTablesEnabled(Db))
		{
			List<BotPoint> seats = TableSeats(910000000, new BotPoint(-320, -266), new BotPoint(260, -266), new BotPoint(-320, 4), new BotPoint(260, 4));
			Post(() =>
			{
				SpawnBlackjackTable(seats);
			}, "PlanBlackjackTable");
		}
	}

	private static List<BotPoint> TableSeats(int mapId, BotPoint topP1, BotPoint topP2, BotPoint botP1, BotPoint botP2)
	{
		int num = Math.Min(topP1.x, topP2.x);
		int num2 = Math.Max(topP1.x, topP2.x);
		int y = topP1.y;
		int num3 = (num2 - num) / 3;
		int num4 = Math.Min(botP1.x, botP2.x);
		int num5 = Math.Max(botP1.x, botP2.x);
		int y2 = botP1.y;
		int num6 = (num5 - num4) / 3;
		List<BotPoint> list = new List<BotPoint>
		{
			new BotPoint(num + num3 + num3 / 2, y),
			new BotPoint(num + num3 / 2 + Jitter(), y),
			new BotPoint(num + num3 * 2 + num3 / 2 + Jitter(), y),
			new BotPoint(num4 + num6 / 2 + Jitter(), y2),
			new BotPoint(num4 + num6 + num6 / 2 + Jitter(), y2),
			new BotPoint(num4 + num6 * 2 + num6 / 2 + Jitter(), y2)
		};
		for (int i = 0; i < list.Count; i++)
		{
			list[i] = PlatformSpawner.SnapToGround(mapId, list[i]);
		}
		return list;
		static int Jitter()
		{
			return rng.Next(125) - 50;
		}
	}

	private static void SpawnBlackjackTable(List<BotPoint> seats)
	{
		List<int> playerSeatOrder;
		BotCharacter dealer;
		List<BotCharacter> players;
		int stage;
		if (seats != null && seats.Count >= 6)
		{
			playerSeatOrder = new List<int> { 1, 2, 3, 4, 5 };
			Shuffle(playerSeatOrder);
			dealer = null;
			players = new List<BotCharacter>();
			stage = 0;
			Post(Step, "SpawnBlackjackTable");
		}
		void Step()
		{
			switch (stage++)
			{
			case 0:
				dealer = CreateBotWithRetry(seats[0], 910000000, 5);
				if (dealer != null)
				{
					Post(Step, "SpawnBlackjackTable");
				}
				break;
			case 1:
			case 2:
			{
				BotCharacter botCharacter = CreateBotWithRetry(seats[playerSeatOrder[stage - 2]], 910000000, 5);
				if (botCharacter != null)
				{
					players.Add(botCharacter);
				}
				Post(Step, "SpawnBlackjackTable");
				break;
			}
			default:
				WireTable(dealer, players, seats[0]);
				break;
			}
		}
	}

	private static void WireTable(BotCharacter dealer, List<BotCharacter> players, BotPoint dealerSeat)
	{
		if (dealer == null)
		{
			return;
		}
		BotTypeManager.SetAndStartBots(new List<int> { dealer.id }, BotType.BLACKJACK_DEALER);
		if (!(BotStore.GetBotById(dealer.id) is BlackjackDealerBot blackjackDealerBot))
		{
			return;
		}
		foreach (BotCharacter player in players)
		{
			blackjackDealerBot.Table.AddPlayer(player);
			blackjackDealerBot.interactors.SetRespondant(player);
		}
		BotTasks.Schedule(() =>
		{
			foreach (BotCharacter player2 in players)
			{
				player2.facing = (((float)dealerSeat.x >= player2.x) ? 1 : (-1));
			}
		}, 7500L, "WireTable", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Environment/BotEnvironment.cs");
	}

	private static void PlanOpqLobbyBots()
	{
		if (!BotConfig.OpqLobbyBotsEnabled(Db))
		{
			return;
		}
		string[] array = new string[2] { "m3", "m4" };
		int num = 3 / array.Length;
		int num2 = 3 % array.Length;
		for (int i = 0; i < array.Length; i++)
		{
			int count = num + ((i < num2) ? 1 : 0);
			if (count > 0)
			{
				string platformId = array[i];
				Post(() =>
				{
					EnqueueBotSpawns(PlatformSeats(910000000, platformId, count), 910000000, BotType.OPQ_BOT, 50, 70);
				}, "PlanOpqLobbyBots");
			}
		}
	}

	private static void Shuffle(List<int> list)
	{
		for (int num = list.Count - 1; num > 0; num--)
		{
			int num2 = rng.Next(num + 1);
			int index = num;
			int index2 = num2;
			int num3 = list[num2];
			int num4 = list[num];
			int num5 = (list[index] = num3);
			num5 = (list[index2] = num4);
		}
	}

	private static void PlanHenesys()
	{
		if (!BotConfig.HenesysCrowdEnabled(Db) && !BotConfig.HenesysMarketCrowdEnabled(Db) && !BotConfig.HenesysParkCrowdEnabled(Db))
		{
			Post(PlanWave3, "PlanHenesys");
			return;
		}
		EnsureMapThen(100000000, () =>
		{
			if (BotConfig.HenesysCrowdEnabled(Db))
			{
				PlanPlatformSeats(100000000, "m1", 30, BotType.HENESYS_BOT);
				PlanPlatformSeats(100000000, "m4_social", 3, BotType.HENESYS_BOT);
				PlanPlatformSeats(100000000, "m5_social", 3, BotType.HENESYS_BOT);
				PlanPlatformSeats(100000000, "m6_social", 3, BotType.HENESYS_BOT);
			}
			PlanHenesysMarket();
		});
	}

	private static void PlanHenesysMarket()
	{
		if (!BotConfig.HenesysMarketCrowdEnabled(Db) && !BotConfig.HenesysParkCrowdEnabled(Db))
		{
			Post(PlanWave3, "PlanHenesysMarket");
			return;
		}
		EnsureMapThen(100000100, () =>
		{
			if (BotConfig.HenesysMarketCrowdEnabled(Db))
			{
				PlanPlatformSeats(100000100, "m1", 10, BotType.HENESYS_BOT);
				PlanPlatformSeats(100000100, "m2", 10, BotType.HENESYS_BOT);
			}
			PlanHenesysPark();
		});
	}

	private static void PlanHenesysPark()
	{
		if (!BotConfig.HenesysParkCrowdEnabled(Db))
		{
			Post(PlanWave3, "PlanHenesysPark");
			return;
		}
		EnsureMapThen(100000200, () =>
		{
			PlanPlatformSeats(100000200, "m1", 10, BotType.HENESYS_BOT);
			Post(PlanWave3, "PlanHenesysPark");
		});
	}

	private static void PlanWave3()
	{
		TownWanderers?.Invoke();
	}

	private static void PlanPlatformSeats(int mapId, string platformId, int count, BotType type)
	{
		if (count > 0)
		{
			Post(() =>
			{
				EnqueueBotSpawns(PlatformSeats(mapId, platformId, count), mapId, type);
			}, "PlanPlatformSeats");
		}
	}

	private static void EnqueueBotSpawns(List<BotPoint> seats, int mapId, BotType type, int minLevel = 0, int maxLevel = 0)
	{
		List<int> ids = new List<int>();
		int index = 0;
		Post(NextBot, "EnqueueBotSpawns");
		void NextBot()
		{
			if (index >= seats.Count)
			{
				BotTypeManager.SetAndStartBots(ids, type);
			}
			else
			{
				BotCharacter botCharacter = CreateBotWithRetry(seats[index++], mapId, 5);
				if (botCharacter != null)
				{
					if (maxLevel >= minLevel && minLevel > 0)
					{
						botCharacter.level = minLevel + rng.Next(maxLevel - minLevel + 1);
					}
					ids.Add(botCharacter.id);
				}
				Post(NextBot, "EnqueueBotSpawns");
			}
		}
	}

	private static List<BotPoint> PlatformSeats(int mapId, string platformId, int count)
	{
		Platform platform = Platforms.ParsePlatform(mapId, platformId);
		List<BotPoint> list = new List<BotPoint>();
		List<BotPoint> list2 = new List<BotPoint>();
		PlatformSpawner platformSpawner = new PlatformSpawner();
		for (int i = 0; i < count; i++)
		{
			BotPoint item = PlatformSpawner.SnapToGround(mapId, platformSpawner.FindUnoccupiedPoint(platform, list));
			list.Add(item);
			list2.Add(item);
		}
		return list2;
	}

	private static List<BotPoint> RadiusSeats(int numBots, int mapId, string platformId, BotPoint center, int radius)
	{
		Platform platform = Platforms.ParsePlatform(mapId, platformId);
		List<BotPoint> list = new List<BotPoint>();
		List<BotPoint> list2 = new List<BotPoint>();
		PlatformSpawner spawner = new PlatformSpawner();
		for (int i = 0; i < numBots; i++)
		{
			BotPoint? botPoint = FindUnoccupiedPointInRadius(spawner, platform, list, center, radius);
			if (botPoint.HasValue)
			{
				BotPoint item = PlatformSpawner.SnapToGround(mapId, botPoint.Value);
				list.Add(item);
				list2.Add(item);
			}
		}
		return list2;
	}

	private static List<BotPoint> FillerSeats(int numBots, int mapId, BotPoint p1, BotPoint p2)
	{
		int minX = Math.Min(p1.x, p2.x);
		int maxX = Math.Max(p1.x, p2.x);
		Platform platform = new Platform(minX, maxX, p1.y, new List<BotPoint> { p1, p2 }, Platform.Type.FLAT);
		List<BotPoint> list = new List<BotPoint>();
		List<BotPoint> list2 = new List<BotPoint>();
		foreach (BotPoint item2 in new PlatformSpawner().FindUnoccupiedPoints(platform, list, numBots))
		{
			BotPoint item = PlatformSpawner.SnapToGround(mapId, item2);
			list.Add(item);
			list2.Add(item);
		}
		return list2;
	}

	private static void EnsureMapThen(int mapId, Action next, int attempt = 0)
	{
		if (MapService.Has(mapId))
		{
			next();
			return;
		}
		MapRuntime map;
		switch ((TryGetMap != null) ? TryGetMap(mapId, out map, allowBlocking: false) : BotMapState.Missing)
		{
		case BotMapState.Ready:
			next();
			return;
		case BotMapState.Missing:
			next();
			return;
		}
		if (attempt >= 180)
		{
			try
			{
				GetMap?.Invoke(mapId);
			}
			catch (ArgumentException)
			{
			}
			next();
		}
		else
		{
			PostNextFrame(() =>
			{
				EnsureMapThen(mapId, next, attempt + 1);
			});
		}
	}

	private static BotCharacter CreateBotWithRetry(BotPoint spawn, int mapId, int maxRetries)
	{
		for (int i = 1; i <= maxRetries; i++)
		{
			if (BotGeneration.EnvironmentBotLimitReached())
			{
				return null;
			}
			bool flag = ReserveMarketEntranceBotSlot(mapId);
			if (mapId == 910000000 && !flag)
			{
				return null;
			}
			try
			{
				BotCharacter botCharacter = BotGeneration.CreateBotPollReadiness(spawn.x, spawn.y, mapId);
				if (botCharacter != null)
				{
					return botCharacter;
				}
			}
			catch (Exception)
			{
			}
			ReleaseMarketEntranceBotSlot(mapId, flag);
			if (BotGeneration.EnvironmentBotLimitReached())
			{
				return null;
			}
		}
		return null;
	}

	private static bool ReserveMarketEntranceBotSlot(int mapId)
	{
		if (mapId != 910000000)
		{
			return false;
		}
		int num = FmEntranceCapacity();
		int num2;
		do
		{
			num2 = marketEntranceBotCount;
			if (num2 >= num)
			{
				return false;
			}
		}
		while (Interlocked.CompareExchange(ref marketEntranceBotCount, num2 + 1, num2) != num2);
		return true;
	}

	public static int FmEntranceCapacity()
	{
		if (fmEntranceCapacity > 0)
		{
			return fmEntranceCapacity;
		}
		int num = BotConfig.MarketBotMax(Db);
		int[] array = DistributeMarketEntranceWanderers(num);
		int num2 = array[0] + array[1] + array[2];
		int num3 = Math.Max(2, num / 6);
		int num4 = Math.Max(1, num / 8);
		int num5 = Math.Max(1, num / 15);
		int num6 = Math.Max(1, num3 / 2) + (num3 - num3 / 2) + Math.Max(1, num4 / 2) + (num4 - num4 / 2) + num5;
		int num7 = 3;
		int num8 = (BotConfig.GachaBotsEnabled(Db) ? 2 : 0);
		int num9 = (BotConfig.BlackjackTablesEnabled(Db) ? 3 : 0);
		int num10 = (BotConfig.OpqLobbyBotsEnabled(Db) ? 3 : 0);
		fmEntranceCapacity = num2 + num6 + num7 + num8 + num9 + num10;
		return fmEntranceCapacity;
	}

	private static void ReleaseMarketEntranceBotSlot(int mapId, bool reserved)
	{
		if ((mapId == 910000000) & reserved)
		{
			Math.Max(0, Interlocked.Decrement(ref marketEntranceBotCount));
		}
	}

	public static List<int> SpawnBotsOnMapOnPlatform(int numBots, int mapId, string platformId)
	{
		EnsureMap(mapId);
		List<int> list = new List<int>();
		foreach (BotPoint item in PlatformSeats(mapId, platformId, numBots))
		{
			BotCharacter botCharacter = CreateBotWithRetry(item, mapId, 5);
			if (botCharacter != null)
			{
				list.Add(botCharacter.id);
			}
		}
		return list;
	}

	public static List<int> SpawnBotsOnMapOnPlatformInRadius(int numBots, int mapId, string platformId, BotPoint center, int radius)
	{
		EnsureMap(mapId);
		List<int> list = new List<int>();
		foreach (BotPoint item in RadiusSeats(numBots, mapId, platformId, center, radius))
		{
			BotCharacter botCharacter = CreateBotWithRetry(item, mapId, 5);
			if (botCharacter != null)
			{
				list.Add(botCharacter.id);
			}
		}
		return list;
	}

	public static List<int> SpawnFillerBots(int numBots, int mapId, BotPoint p1, BotPoint p2)
	{
		EnsureMap(mapId);
		List<int> list = new List<int>();
		foreach (BotPoint item in FillerSeats(numBots, mapId, p1, p2))
		{
			BotCharacter botCharacter = CreateBotWithRetry(item, mapId, 5);
			if (botCharacter != null)
			{
				list.Add(botCharacter.id);
			}
		}
		return list;
	}

	private static void EnsureMap(int mapId)
	{
		if (!MapService.Has(mapId))
		{
			GetMap?.Invoke(mapId);
		}
	}

	private static BotPoint? FindUnoccupiedPointInRadius(PlatformSpawner spawner, Platform platform, List<BotPoint> occupied, BotPoint center, int radius)
	{
		for (int i = 0; i < 100; i++)
		{
			BotPoint value = spawner.FindUnoccupiedPoint(platform, occupied);
			if (Math.Abs(value.x - center.x) <= radius && Math.Abs(value.y - center.y) <= radius)
			{
				return value;
			}
		}
		return null;
	}
}
