using System;
using System.Collections.Generic;
using Server.Bot.Decorator;
using Server.Bot.Environment;
using Server.Field;

namespace Server.Bot;

public sealed class BotPartyGrindService : IDisposable
{
	public const int ItemId = 2431159;

	public const int Count = 5;

	public const int PartyCap = 6;

	public const long Duration = 1800000L;

	private readonly BotPartyService parties;

	private readonly Func<int, MapRuntime> maps;

	private readonly Random random;

	private readonly DbStore db;

	private readonly BotDecorate decorator;

	private readonly Dictionary<int, int> leaders = new Dictionary<int, int>();

	private readonly Dictionary<int, List<int>> companions = new Dictionary<int, List<int>>();

	private readonly Dictionary<int, int> claims = new Dictionary<int, int>();

	private readonly Dictionary<int, (int map, int mob, long until)> avoided = new Dictionary<int, (int, int, long)>();

	private readonly Dictionary<int, long> nextChat = new Dictionary<int, long>();

	private readonly Dictionary<int, long> floorRebindAt = new Dictionary<int, long>();

	private readonly Dictionary<int, BotPartyGrindFloors.Floor> floors = new Dictionary<int, BotPartyGrindFloors.Floor>();

	private readonly Dictionary<int, int> floorMaps = new Dictionary<int, int>();

	private readonly Dictionary<int, (long expiry, BotTasks.Handle watch)> active = new Dictionary<int, (long, BotTasks.Handle)>();

	private List<BotGrindMob> cachedMobs = new List<BotGrindMob>();

	private long cachedAt = long.MinValue;

	private int cachedMap;

	private int cachedWorld;

	private int cachedChannel;

	public Func<int, int, int, List<BotGrindMob>> Monsters;

	public Action<BotCharacter, BotGrindMob, int, BotAttack.Skill, int[]> Hit;

	public Func<BotCharacter, float, GrindLoot> FindLoot;

	public Func<BotCharacter, float, int, int> TakeLoot;

	public Action<int, string> Notice;

	public Action<int, bool> Effect;

	public IReadOnlyCollection<int> ActiveLeaders => active.Keys;

	public int ConfiguredCount => BotConfig.PartyCompanionCount(db);

	public long ConfiguredDurationMs => BotConfig.PartyCompanionDurationMs(db);

	public bool Enabled => BotConfig.PartyCompanionEnabled(db);

	public BotPartyGrindService(BotPartyService parties, Func<int, MapRuntime> maps, Random random = null, DbStore db = null)
	{
		this.parties = parties;
		this.maps = maps;
		this.random = random ?? new Random();
		this.db = db;
		decorator = new BotDecorate(BotResources.Load(), BotItemCatalog.Shared, BotTypeManager.Output, this.random);
	}

	public int LeaderId(int botId)
	{
		if (!leaders.TryGetValue(botId, out var value))
		{
			return -1;
		}
		return value;
	}

	public long Expiry(int id)
	{
		if (!active.TryGetValue(id, out (long, BotTasks.Handle) value))
		{
			return 0L;
		}
		return value.Item1;
	}

	public bool IsActive(int id)
	{
		return active.ContainsKey(id);
	}

	public IReadOnlyList<int> Companions(int id)
	{
		if (!companions.TryGetValue(id, out var value))
		{
			return Array.Empty<int>();
		}
		return value;
	}

	public int CompanionCountOf(int leaderId)
	{
		if (!companions.TryGetValue(leaderId, out var value))
		{
			return 0;
		}
		return value.Count;
	}

	public BotActor Leader(int botId)
	{
		BotActor botActor = parties.Get(LeaderId(botId));
		BotCharacter botCharacter = BotRegistry.Get(botId);
		if (botActor == null || !botActor.online || botActor.IsBot() || botCharacter == null || botActor.world != botCharacter.world || botActor.channel != botCharacter.channel)
		{
			return null;
		}
		return botActor;
	}

	public static int CompanionLevel(int level, int offset, int jitter = 3)
	{
		return Math.Max(1, Math.Min(255, level + Math.Max(-jitter, Math.Min(jitter, offset))));
	}

	public bool Start(BotActor leader)
	{
		if (leader == null || leader.IsBot() || !leader.online)
		{
			return false;
		}
		if (!Enabled)
		{
			Notice?.Invoke(leader.id, "组队伴侣已被后台关闭。");
			return false;
		}
		int configuredCount = ConfiguredCount;
		bool flag = BotConfig.PartyCompanionRequireParty(db);
		BotPartyService.Party party = parties.For(leader.id);
		if (party == null && !flag)
		{
			if (!parties.Create(leader))
			{
				Notice?.Invoke(leader.id, "无法创建队伍，基友召唤失败。");
				return false;
			}
			party = parties.For(leader.id);
		}
		string text;
		if (IsActive(leader.id))
		{
			text = "基友集合仍在生效中，无法重复使用。";
		}
		else if (party == null)
		{
			text = "请先开启组队后再使用基友集合。未消耗道具。";
		}
		else
		{
			text = ((party.members.Count + configuredCount > 6) ? ("队伍空位不足，需要" + configuredCount + "个空位才能召唤基友。未消耗道具。") : null);
		}
		if (text != null)
		{
			Notice?.Invoke(leader.id, text);
			return false;
		}
		if (!Spawn(leader, configuredCount))
		{
			Notice?.Invoke(leader.id, "基友召唤失败，未消耗道具。");
			return false;
		}
		long expiry = BotScheduler.Now() + ConfiguredDurationMs;
		BotTasks.Handle item = BotTasks.ScheduleFixedRate(() =>
		{
			if (BotScheduler.Now() >= expiry)
			{
				Stop(leader.id, "搬砖时间结束，基友已离开。");
			}
			else if (parties.For(leader.id) == null)
			{
				Stop(leader.id, "组队已解散，基友集合结束。");
			}
		}, 1000L, 1000L, "Start", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/BotPartyGrindService.cs");
		active[leader.id] = (expiry, item);
		Effect?.Invoke(leader.id, arg2: true);
		Notice?.Invoke(leader.id, "基友集合生效，持续" + BotConfig.PartyCompanionMinutes(db) + "分钟。");
		return true;
	}

	private bool Spawn(BotActor leader, int count)
	{
		Stop(leader.id);
		MapRuntime map = maps(leader.mapId);
		List<BotPartyGrindFloors.Floor> list = BotPartyGrindFloors.FromMap(map, new BotNavigationGraph.Point((int)leader.x, (int)leader.y));
		List<int> list2 = new List<int>();
		try
		{
			for (int i = 0; i < count; i++)
			{
				BotPartyGrindFloors.Floor floor = list[BotPartyGrindFloors.IndexFor(i, list.Count)];
				BotNavigationGraph.Point point = BotPartyGrindFloors.Station(map, floor, (int)leader.x + (i % 3 - 1) * 48);
				BotCharacter botCharacter = BotGeneration.CreateBotPollReadiness(point.x, point.y, leader.mapId, ignoreEnvironmentLimit: true, playArrival: false);
				if (botCharacter == null)
				{
					Cleanup(list2);
					return false;
				}
				list2.Add(botCharacter.id);
				botCharacter.world = leader.world;
				botCharacter.channel = leader.channel;
				botCharacter.client.world = leader.world;
				botCharacter.client.channel = leader.channel;
				int num = BotConfig.PartyCompanionLevelJitter(db);
				botCharacter.level = CompanionLevel(leader.level, random.Next(-num, num + 1));
				botCharacter.job = decorator.SelectJob(botCharacter.level);
				int luk = Math.Min(999, 4 + Math.Max(0, botCharacter.level - 1) * 5);
				botCharacter.str = (botCharacter.dex = (botCharacter.intelligence = (botCharacter.luk = luk)));
				decorator.EquipWeapon(botCharacter);
				leaders[botCharacter.id] = leader.id;
				floors[botCharacter.id] = new BotPartyGrindFloors.Floor(point.y, floor.minX, floor.maxX);
				BotStateMachine botStateMachine = BotTypeManager.CreateAndSetBot(botCharacter, BotType.COMPANION_GRIND_BOT);
				if (!parties.Join(botCharacter, parties.For(leader.id).id))
				{
					Cleanup(list2);
					return false;
				}
				BotMovementService.SetGrinding(botCharacter, grinding: true);
				BotMovementService.Teleport(botCharacter, point);
				botStateMachine.StartScheduledTask(180 + i * 80);
			}
			companions[leader.id] = list2;
			floorMaps[leader.id] = leader.mapId;
			return list2.Count == count;
		}
		catch
		{
			Cleanup(list2);
			return false;
		}
	}

	private void BindFloors(BotActor leader)
	{
		if (!companions.TryGetValue(leader.id, out var value))
		{
			return;
		}
		bool flag = true;
		foreach (int item in value)
		{
			if (!floors.ContainsKey(item))
			{
				flag = false;
			}
		}
		if ((floorMaps.TryGetValue(leader.id, out var value2) && value2 == leader.mapId) & flag)
		{
			return;
		}
		MapRuntime map = maps(leader.mapId);
		List<BotPartyGrindFloors.Floor> list = BotPartyGrindFloors.FromMap(map, new BotNavigationGraph.Point((int)leader.x, (int)leader.y));
		foreach (int item2 in value)
		{
			claims.Remove(item2);
			avoided.Remove(item2);
		}
		for (int i = 0; i < value.Count; i++)
		{
			BotPartyGrindFloors.Floor floor = list[BotPartyGrindFloors.IndexFor(i, list.Count)];
			BotNavigationGraph.Point point = BotPartyGrindFloors.Station(map, floor, (int)leader.x);
			floors[value[i]] = new BotPartyGrindFloors.Floor(point.y, floor.minX, floor.maxX);
		}
		floorMaps[leader.id] = leader.mapId;
	}

	public BotNavigationGraph.Point Station(BotCharacter bot, BotActor leader)
	{
		BindFloors(leader);
		if (!floors.TryGetValue(bot.id, out var value))
		{
			return new BotNavigationGraph.Point((int)leader.x, (int)leader.y);
		}
		int num = (companions.TryGetValue(leader.id, out var value2) ? Math.Max(0, value2.IndexOf(bot.id)) : 0);
		return BotPartyGrindFloors.Station(maps(leader.mapId), value, (int)leader.x + (num % 3 - 1) * 48);
	}

	public bool OnFloor(BotCharacter bot)
	{
		if (floors.TryGetValue(bot.id, out var value))
		{
			return value.ContainsY((int)bot.y);
		}
		return true;
	}

	public GrindLoot Loot(BotCharacter bot, float range)
	{
		if (FindLoot == null || bot == null)
		{
			return default;
		}
		try
		{
			return FindLoot(bot, range);
		}
		catch (Exception)
		{
			return default;
		}
	}

	public int TakeLootNearby(BotCharacter bot, float range, int max)
	{
		if (TakeLoot == null || bot == null)
		{
			return 0;
		}
		try
		{
			return TakeLoot(bot, range, max);
		}
		catch (Exception)
		{
			return 0;
		}
	}

	public BotNavigationGraph.Point? PatrolPoint(BotCharacter bot, BotActor leader)
	{
		if (bot == null || leader == null || bot.mapId != leader.mapId)
		{
			return null;
		}
		BindFloors(leader);
		if (!floors.TryGetValue(bot.id, out var value))
		{
			return null;
		}
		int num = value.minX;
		int num2 = value.maxX;
		int num3 = (int)leader.x;
		int num4 = Math.Max(num, num3 - 420);
		int num5 = Math.Min(num2, num3 + 420);
		if (num5 - num4 >= 160)
		{
			num = num4;
			num2 = num5;
		}
		if (num2 - num < 80)
		{
			num = value.minX;
			num2 = value.maxX;
		}
		if (num2 - num < 8)
		{
			return null;
		}
		int num6 = (int)bot.x;
		int num7 = num + random.Next(0, num2 - num);
		if (Math.Abs(num7 - num6) < 90)
		{
			num7 = num6 + ((num6 - num <= num2 - num6) ? 1 : (-1)) * random.Next(90, 200);
		}
		num7 = Math.Max(num, Math.Min(num2, num7));
		return BotPartyGrindFloors.Station(maps(bot.mapId), value, num7);
	}

	public BotNavigationGraph.Point? LeaderSpot(BotCharacter bot, BotActor leader)
	{
		if (bot == null || leader == null || bot.mapId != leader.mapId)
		{
			return null;
		}
		MapRuntime map = maps(leader.mapId);
		int num = (companions.TryGetValue(leader.id, out var value) ? Math.Max(0, value.IndexOf(bot.id)) : 0);
		int desiredX = (int)leader.x + (num % 3 - 1) * 48;
		bool flag = floors.TryGetValue(bot.id, out var value2);
		if (flag && value2.ContainsY((int)leader.y))
		{
			return BotPartyGrindFloors.Station(map, value2, desiredX);
		}
		long num2 = BotScheduler.Now();
		if (floorRebindAt.TryGetValue(bot.id, out var value3) && num2 - value3 < 5000)
		{
			if (!flag)
			{
				return null;
			}
			return BotPartyGrindFloors.Station(map, value2, desiredX);
		}
		floorRebindAt[bot.id] = num2;
		List<BotPartyGrindFloors.Floor> list = BotPartyGrindFloors.FromMap(map, new BotNavigationGraph.Point((int)leader.x, (int)leader.y));
		if (list.Count == 0)
		{
			return null;
		}
		BotPartyGrindFloors.Floor floor = list[0];
		float num3 = float.MaxValue;
		foreach (BotPartyGrindFloors.Floor item in list)
		{
			float num4 = Math.Abs(item.centerY - (int)leader.y);
			if (num4 < num3)
			{
				num3 = num4;
				floor = item;
			}
		}
		BotNavigationGraph.Point value4 = BotPartyGrindFloors.Station(map, floor, desiredX);
		floors[bot.id] = new BotPartyGrindFloors.Floor(value4.y, floor.minX, floor.maxX);
		return value4;
	}

	private List<BotGrindMob> Mobs(BotActor bot)
	{
		long num = BotScheduler.Now();
		if (cachedAt != long.MinValue && cachedMap == bot.mapId && cachedWorld == bot.world && cachedChannel == bot.channel && num - cachedAt < 50)
		{
			return cachedMobs;
		}
		cachedMobs = Monsters?.Invoke(bot.mapId, bot.world, bot.channel) ?? new List<BotGrindMob>();
		cachedAt = num;
		cachedMap = bot.mapId;
		cachedWorld = bot.world;
		cachedChannel = bot.channel;
		return cachedMobs;
	}

	public BotGrindMob Target(BotCharacter bot, BotActor leader)
	{
		if (bot.mapId != leader.mapId || bot.world != leader.world || bot.channel != leader.channel)
		{
			return null;
		}
		BindFloors(leader);
		List<BotGrindMob> list = Mobs(bot);
		long now = BotScheduler.Now();
		BotGrindMob botGrindMob = null;
		float num = float.MaxValue;
		foreach (BotGrindMob item in list)
		{
			if (Eligible(item) && item.boss && Distance(item) < num)
			{
				botGrindMob = item;
				num = Distance(item);
			}
		}
		if (botGrindMob != null)
		{
			claims[bot.id] = botGrindMob.id;
			return botGrindMob;
		}
		HashSet<int> hashSet = new HashSet<int>();
		if (companions.TryGetValue(leader.id, out var value))
		{
			foreach (int item2 in value)
			{
				if (item2 != bot.id && BotRegistry.Get(item2)?.mapId == bot.mapId && claims.TryGetValue(item2, out var value2))
				{
					hashSet.Add(value2);
				}
			}
		}
		if (claims.TryGetValue(bot.id, out var held))
		{
			BotGrindMob botGrindMob2 = list.Find((BotGrindMob m) => m != null && m.id == held);
			if (Eligible(botGrindMob2) && !hashSet.Contains(held))
			{
				return botGrindMob2;
			}
			claims.Remove(bot.id);
		}
		BotGrindMob botGrindMob3 = null;
		float num2 = float.MaxValue;
		foreach (BotGrindMob item3 in list)
		{
			if (Eligible(item3) && !hashSet.Contains(item3.id))
			{
				float num3 = Distance(item3) + (float)((floors.TryGetValue(bot.id, out var value3) && !value3.ContainsY(item3.y)) ? 160 : 0);
				if (num3 < num2)
				{
					botGrindMob3 = item3;
					num2 = num3;
				}
			}
		}
		if (botGrindMob3 == null)
		{
			foreach (BotGrindMob item4 in list)
			{
				if (Eligible(item4) && Distance(item4) < num2)
				{
					botGrindMob3 = item4;
					num2 = Distance(item4);
				}
			}
		}
		if (botGrindMob3 != null)
		{
			claims[bot.id] = botGrindMob3.id;
		}
		return botGrindMob3;
		float Distance(BotGrindMob mob)
		{
			return Math.Abs((float)mob.x - bot.x) + Math.Abs((float)mob.y - bot.y) * 2f;
		}
		bool Eligible(BotGrindMob mob)
		{
			if (mob != null && mob.alive && !mob.fake && mob.mapId == bot.mapId && mob.world == bot.world && mob.channel == bot.channel)
			{
				if (avoided.TryGetValue(bot.id, out (int, int, long) value4) && value4.Item1 == bot.mapId && value4.Item2 == mob.id)
				{
					return now >= value4.Item3;
				}
				return true;
			}
			return false;
		}
	}

	public void AvoidTarget(BotCharacter bot, int mobId, long now)
	{
		claims.Remove(bot.id);
		avoided[bot.id] = (bot.mapId, mobId, now + 10000);
	}

	public void ReleaseTarget(BotCharacter bot)
	{
		claims.Remove(bot.id);
	}

	public bool TryChat(BotCharacter bot, string text, long now)
	{
		int key = LeaderId(bot.id);
		BotActor botActor = Leader(bot.id);
		if (botActor == null || botActor.mapId != bot.mapId || string.IsNullOrWhiteSpace(text) || (nextChat.TryGetValue(key, out var value) && now < value))
		{
			return false;
		}
		nextChat[key] = now + 15000;
		BotTypeManager.Output.Speak(bot, text);
		return true;
	}

	public bool SameParty(BotCharacter bot, BotActor leader)
	{
		if (parties.For(leader.id) != null)
		{
			return parties.For(bot.id) == parties.For(leader.id);
		}
		return false;
	}

	public void Strike(BotCharacter bot, BotGrindMob target)
	{
		if (target == null || !target.alive)
		{
			return;
		}
		bot.equips.TryGetValue(-11, out var value);
		BotAttack.Skill arg = BotAttack.GrindSkill(bot.job, value);
		(int, int) tuple = BotAttack.EngageRange(bot.job, value);
		if (!(Math.Abs((float)target.x - bot.x) > (float)tuple.Item1) && !(Math.Abs((float)target.y - bot.y) > (float)tuple.Item2))
		{
			int num = BotAttack.LineDamage(BotAttack.JobTier(bot.job), bot.level, target.maxHp, random);
			bot.stance = (((float)target.x < bot.x) ? 5 : 4);
			bot.facing = ((!((float)target.x < bot.x)) ? 1 : (-1));
			if (arg.id == 0)
			{
				BotAttack.BasicSwing(bot, random);
			}
			Hit?.Invoke(bot, target, num, arg, BotAttack.SplitDamage(num, arg.hits));
		}
	}

	public void Stop(int leaderId, string message = null)
	{
		bool flag = active.TryGetValue(leaderId, out (long, BotTasks.Handle) value);
		if (flag)
		{
			active.Remove(leaderId);
			value.Item2.Cancel();
		}
		if (companions.TryGetValue(leaderId, out var value2))
		{
			companions.Remove(leaderId);
			Cleanup(value2);
		}
		floorMaps.Remove(leaderId);
		nextChat.Remove(leaderId);
		if (flag)
		{
			Effect?.Invoke(leaderId, arg2: false);
			if (!string.IsNullOrEmpty(message))
			{
				Notice?.Invoke(leaderId, message);
			}
		}
	}

	private void Cleanup(List<int> ids)
	{
		foreach (int id in ids)
		{
			leaders.Remove(id);
			floors.Remove(id);
			claims.Remove(id);
			avoided.Remove(id);
			floorRebindAt.Remove(id);
			BotCharacter botCharacter = BotRegistry.Get(id);
			parties.Leave(id);
			if (botCharacter != null)
			{
				BotMovementService.Stop(botCharacter);
				BotMovementService.Disable(botCharacter);
				BotTypeManager.ManuallyStopBot(botCharacter);
				BotGeneration.RemoveBotFromServer(botCharacter);
			}
		}
	}

	public void Dispose()
	{
		foreach (int item in new List<int>(active.Keys))
		{
			Stop(item);
		}
		foreach (int item2 in new List<int>(companions.Keys))
		{
			Stop(item2);
		}
		Monsters = null;
		Hit = null;
		Notice = null;
		Effect = null;
	}
}
