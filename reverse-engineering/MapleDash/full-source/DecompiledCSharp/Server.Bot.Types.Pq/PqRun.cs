using System;
using System.Collections.Generic;
using Server.Bot.Environment;
using Server.Field;

namespace Server.Bot.Types.Pq;

public sealed class PqRun
{
	private const long ActionIntervalMs = 420L;

	private const long StepSayGapMs = 4500L;

	private const long StepTimeoutMs = 70000L;

	private const long StageIdleMs = 1500L;

	private const long TalkRetryMs = 6000L;

	private const long StartTimeoutMs = 20000L;

	private const long RewardWaitMs = 6000L;

	private const long OffMapGraceMs = 8000L;

	private const int TalkRetries = 3;

	public readonly PqDef def;

	public readonly int partyId;

	public readonly int leaderId;

	private readonly List<int> bots = new List<int>();

	private readonly Random rng = new Random();

	private readonly Dictionary<int, long> nextSwing = new Dictionary<int, long>();

	private readonly Dictionary<int, int> combatClaims = new Dictionary<int, int>();

	public int stageIndex;

	public int stepIndex;

	public bool finished;

	public string lastNote = "";

	private long stepStartedAt;

	private long stepDeadline;

	private long nextActionAt;

	private long nextSayAt;

	private long nextChatterAt;

	private long offMapAt;

	private long stageClearedAt;

	private long startDeadline;

	private long rewardUntil;

	private long talkRetryAt;

	private readonly Dictionary<int, long> nextSweepAt = new Dictionary<int, long>();

	private long noTargetSince;

	private bool noTargetNoted;

	private bool started;

	private bool stageWorkDone;

	private bool talking;

	private bool talkSent;

	private bool talkDone;

	private int talkStage = -1;

	private int talkRetries;

	private readonly Dictionary<int, int> couponNeed = new Dictionary<int, int>();

	private readonly HashSet<int> passReady = new HashSet<int>();

	private readonly HashSet<int> asked = new HashSet<int>();

	private bool questScriptFailed;

	private int checkAttempts;

	private bool botsRegistered;

	private const float NpcTalkRange = 110f;

	public IReadOnlyList<int> Bots => bots;

	private int CurrentMap => CurrentStage?.mapId ?? 0;

	public PqStage CurrentStage
	{
		get
		{
			if (stageIndex < 0 || stageIndex >= def.stages.Count)
			{
				return null;
			}
			return def.stages[stageIndex];
		}
	}

	public PqRun(PqDef def, int partyId, int leaderId)
	{
		this.def = def;
		this.partyId = partyId;
		this.leaderId = leaderId;
	}

	public bool Contains(int botId)
	{
		return bots.Contains(botId);
	}

	public void Begin(bool openViaNpc = true)
	{
		bots.Clear();
		BotPartyService parties = BotTypeManager.Parties;
		BotPartyService.Party party = parties?.ById(partyId) ?? parties?.For(leaderId);
		if (party != null)
		{
			foreach (int member in party.members)
			{
				if (BotRegistry.Get(member) != null)
				{
					bots.Add(member);
				}
			}
		}
		long num = BotScheduler.Now();
		started = true;
		stageIndex = -1;
		stepIndex = 0;
		startDeadline = num + 20000;
		if (!openViaNpc)
		{
			startDeadline = long.MaxValue;
			return;
		}
		PqDirector.Talk(def.recruitNpc, openOnly: false, new string[2] { "我想参加", "参加组队任务" }, (bool ok) =>
		{
			if (!ok)
			{
				PqDirector.Notify(def.name + "：招募 NPC 没有正常回应，尝试直接进本。");
			}
		});
		PqDirector.Notify(def.name + " 组队任务开始，假人正在进本。");
	}

	public void Tick(long now)
	{
		if (finished)
		{
			return;
		}
		PruneBots();
		BotActor botActor = PqDirector.LocalPlayer();
		if (botActor == null || playersPartyGone())
		{
			End(cleared: false);
			return;
		}
		int map = PqDirector.TemplateMapId(botActor.mapId);
		if (stageIndex < 0)
		{
			int num = def.stages.FindIndex((PqStage s) => s.mapId == map);
			if (num >= 0)
			{
				EnterStage(num, now);
			}
			else if (now >= startDeadline)
			{
				PqDirector.Notify(def.name + "：招募未成功，请重新确认 NPC 的入场条件。");
				End(cleared: false);
			}
			return;
		}
		int num2 = def.stages.FindIndex((PqStage s) => s.mapId == map);
		if (map == def.exitMap || map == def.recruitMap)
		{
			End(cleared: true);
			return;
		}
		if (num2 >= 0 && num2 != stageIndex)
		{
			EnterStage(num2, now);
			return;
		}
		if (num2 < 0 && map != PqDirector.TemplateMapId(CurrentMap))
		{
			if (offMapAt == 0L)
			{
				offMapAt = now + 8000;
			}
			else if (now >= offMapAt)
			{
				End(cleared: true);
			}
			return;
		}
		offMapAt = 0L;
		PqStage currentStage = CurrentStage;
		if (currentStage == null)
		{
			End(cleared: true);
			return;
		}
		if (!stageWorkDone && now >= nextChatterAt)
		{
			nextChatterAt = now + 8000 + rng.Next(7000);
			PqDirector.Say(RandomBot(), (rng.Next(100) < 65) ? PqChatLines.Role(def.key, rng) : PqChatLines.Idle(rng));
		}
		if (stageWorkDone)
		{
			long num3 = ((currentStage.gateMs > 0) ? currentStage.gateMs : 1500);
			if (now - stageClearedAt >= num3)
			{
				AdvanceStage(now);
			}
		}
		else
		{
			DriveWork(currentStage, now);
		}
	}

	private static int Live(int mapId)
	{
		return PqDirector.Live(mapId);
	}

	private void PruneBots()
	{
		for (int num = bots.Count - 1; num >= 0; num--)
		{
			if (BotRegistry.Get(bots[num]) == null)
			{
				bots.RemoveAt(num);
			}
			else
			{
				BotPartyService.Party party = BotTypeManager.Parties?.For(bots[num]);
				if (party == null || party.id != partyId)
				{
					bots.RemoveAt(num);
				}
			}
		}
	}

	private bool playersPartyGone()
	{
		BotPartyService.Party party = BotTypeManager.Parties?.For(leaderId);
		if (party != null && party.id == partyId)
		{
			return !party.members.Contains(leaderId);
		}
		return true;
	}

	private void EnterStage(int index, long now)
	{
		stageIndex = index;
		stepIndex = 0;
		stageWorkDone = false;
		stageClearedAt = 0L;
		talking = false;
		talkSent = false;
		talkDone = false;
		talkStage = -1;
		talkRetries = 0;
		talkRetryAt = 0L;
		couponNeed.Clear();
		passReady.Clear();
		asked.Clear();
		questScriptFailed = false;
		checkAttempts = 0;
		stepStartedAt = 0L;
		stepDeadline = 0L;
		nextSweepAt.Clear();
		noTargetSince = 0L;
		noTargetNoted = false;
		combatClaims.Clear();
		nextActionAt = now;
		nextSayAt = now;
		nextChatterAt = now + 4000;
		lastNote = CurrentStage?.title ?? "";
		PqStage currentStage = CurrentStage;
		if (currentStage != null)
		{
			MapReactorService.RespawnAll(Live(currentStage.mapId));
			foreach (PqStep step in currentStage.steps)
			{
				int[] maps = step.maps;
				foreach (int num in maps)
				{
					if (num != currentStage.mapId)
					{
						MapReactorService.RespawnAll(Live(num));
					}
				}
			}
		}
		foreach (int bot in bots)
		{
			BotCharacter botCharacter = BotRegistry.Get(bot);
			if (botCharacter != null && botCharacter.mapId != CurrentMap)
			{
				PqDirector.WarpBot(botCharacter, CurrentMap);
			}
		}
		RegisterBots();
		PqDirector.Notify(def.name + " · " + lastNote);
	}

	private void RegisterBots()
	{
		botsRegistered = false;
		foreach (int bot in bots)
		{
			if (PqDirector.EnterEvent(bot))
			{
				botsRegistered = true;
			}
		}
	}

	private void ReleaseBots()
	{
		if (!botsRegistered)
		{
			return;
		}
		botsRegistered = false;
		foreach (int bot in bots)
		{
			PqDirector.LeaveEvent(bot);
		}
	}

	private void DriveWork(PqStage stage, long now)
	{
		if (stage.steps.Count == 0)
		{
			stageWorkDone = true;
			stageClearedAt = now;
		}
		else if (stepIndex >= stage.steps.Count)
		{
			if (!talking && stage.npc != 0 && talkStage != stageIndex)
			{
				talking = true;
				talkStage = stageIndex;
				stepStartedAt = now;
				GrantRequirement(stage);
				string[] prefer = new string[5] { "确定", "是", "下一步", "继续", "好" };
				PqDirector.Talk(stage.npc, openOnly: false, prefer, (bool ok) =>
				{
					talking = false;
					if (!ok)
					{
						PqDirector.Notify(def.name + "：" + stage.title + " 的 NPC 没有回应，准备直接放行。");
					}
				});
			}
			else if (!talking)
			{
				stageWorkDone = true;
				stageClearedAt = now;
			}
		}
		else
		{
			PqStep pqStep = stage.steps[stepIndex];
			if (stepStartedAt == 0L)
			{
				stepStartedAt = now;
				stepDeadline = now + ((pqStep.timeoutMs > 0) ? pqStep.timeoutMs : 70000);
				Say(pqStep, now);
			}
			if (now >= stepDeadline)
			{
				NextStep(now, "超时跳过");
			}
			else if (pqStep.action switch
			{
				PqAction.ClearMobs => WorkMobs(pqStep, now, boss: false), 
				PqAction.KillMobs => WorkMobs(pqStep, now, boss: false), 
				PqAction.KillBoss => WorkMobs(pqStep, now, boss: true), 
				PqAction.BreakReactors => WorkReactors(pqStep, now), 
				PqAction.Stand => WorkStands(pqStep, now), 
				PqAction.GrantItem => WorkGrant(pqStep), 
				PqAction.Talk => WorkTalk(pqStep, now), 
				PqAction.BotAsk => WorkBotAsk(pqStep, now), 
				PqAction.BotCollect => WorkBotCollect(pqStep, now), 
				PqAction.BotExchange => WorkBotExchange(pqStep, now), 
				PqAction.BotWait => WorkBotWait(pqStep, now), 
				PqAction.HandOver => WorkHandOver(pqStep), 
				PqAction.StageCheck => WorkStageCheck(pqStep, now), 
				_ => true, 
			})
			{
				NextStep(now, null);
			}
		}
	}

	private void GrantRequirement(PqStage stage)
	{
		if (stage.requireItem <= 0 || stage.requireCount <= 0)
		{
			return;
		}
		foreach (PqStep step in stage.steps)
		{
			if (step.action == PqAction.GrantItem && step.itemId == stage.requireItem)
			{
				return;
			}
		}
		PqDirector.Give(stage.requireItem, stage.requireCount);
	}

	private void NextStep(long now, string reason)
	{
		if (!string.IsNullOrEmpty(reason))
		{
			lastNote = CurrentStage?.title + "（" + reason + "）";
		}
		stepIndex++;
		stepStartedAt = 0L;
		stepDeadline = 0L;
		nextSweepAt.Clear();
		noTargetSince = 0L;
		noTargetNoted = false;
		combatClaims.Clear();
		talkSent = false;
		talkDone = false;
		talkRetries = 0;
		talkRetryAt = 0L;
		nextActionAt = now;
		nextSayAt = now;
	}

	private void Say(PqStep step, long now)
	{
		if (!string.IsNullOrEmpty(step.say) && now >= nextSayAt)
		{
			nextSayAt = now + 4500;
			PqDirector.Say(RandomBot(), step.say);
		}
	}

	private List<int> StepMaps(PqStep step)
	{
		List<int> list = new List<int>();
		if (step.maps != null)
		{
			int[] maps = step.maps;
			foreach (int num in maps)
			{
				if (num > 0)
				{
					list.Add(num);
				}
			}
		}
		if (list.Count == 0)
		{
			list.Add(CurrentMap);
		}
		return list;
	}

	private void AssignMaps(List<int> maps, Func<int, int> remainingOf)
	{
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		Dictionary<int, int> dictionary2 = new Dictionary<int, int>();
		foreach (int map in maps)
		{
			dictionary[map] = remainingOf(map);
			dictionary2[map] = 0;
		}
		foreach (int bot in bots)
		{
			BotCharacter botCharacter = BotRegistry.Get(bot);
			if (botCharacter != null && dictionary.TryGetValue(botCharacter.mapId, out var value) && value > 0)
			{
				dictionary2[botCharacter.mapId]++;
			}
		}
		foreach (int bot2 in bots)
		{
			BotCharacter botCharacter2 = BotRegistry.Get(bot2);
			if (botCharacter2 == null || (dictionary.TryGetValue(botCharacter2.mapId, out var value2) && value2 > 0))
			{
				continue;
			}
			int num = 0;
			foreach (int map2 in maps)
			{
				if (dictionary[map2] > 0)
				{
					if (num == 0)
					{
						num = map2;
					}
					else if ((long)dictionary2[map2] * (long)dictionary[num] < (long)dictionary2[num] * (long)dictionary[map2])
					{
						num = map2;
					}
				}
			}
			if (num != 0 && (botCharacter2.mapId == num || PqDirector.WarpBot(botCharacter2, num)))
			{
				dictionary2[num]++;
			}
		}
	}

	private bool WorkMobs(PqStep step, long now, bool boss)
	{
		BotPartyGrindService partyGrind = BotTypeManager.PartyGrind;
		if (partyGrind == null)
		{
			return true;
		}
		BotActor botActor = PqDirector.LocalPlayer();
		if (botActor == null)
		{
			return false;
		}
		List<int> list = StepMaps(step);
		Dictionary<int, List<BotGrindMob>> live = new Dictionary<int, List<BotGrindMob>>();
		int num = 0;
		foreach (int item in list)
		{
			List<BotGrindMob> list2;
			try
			{
				list2 = partyGrind.Monsters?.Invoke(Live(item), botActor.world, botActor.channel);
			}
			catch (Exception)
			{
				list2 = null;
			}
			List<BotGrindMob> list3 = new List<BotGrindMob>();
			if (list2 != null)
			{
				foreach (BotGrindMob item2 in list2)
				{
					if (item2 != null && item2.alive && !item2.fake && boss == item2.boss && IsWanted(step, item2))
					{
						list3.Add(item2);
					}
				}
			}
			live[item] = list3;
			num += list3.Count;
		}
		if (num == 0)
		{
			return true;
		}
		if (now < nextActionAt)
		{
			return false;
		}
		nextActionAt = now + 420;
		AssignMaps(list, (int map) => live[map].Count);
		foreach (int bot in bots)
		{
			BotCharacter botCharacter = BotRegistry.Get(bot);
			if (botCharacter == null || !live.TryGetValue(botCharacter.mapId, out var value) || value.Count == 0)
			{
				continue;
			}
			BotGrindMob botGrindMob = Nearest(botCharacter, value);
			if (botGrindMob == null)
			{
				continue;
			}
			botCharacter.equips.TryGetValue(-11, out var value2);
			(int, int) tuple = BotAttack.EngageRange(botCharacter.job, value2);
			if (Math.Abs((float)botGrindMob.x - botCharacter.x) > (float)tuple.Item1 || Math.Abs((float)botGrindMob.y - botCharacter.y) > (float)tuple.Item2)
			{
				MoveToward(botCharacter, botGrindMob.x, botGrindMob.y);
				continue;
			}
			BotMovementService.Stop(botCharacter);
			if (!nextSwing.TryGetValue(bot, out var value3) || now >= value3)
			{
				botCharacter.stance = (((float)botGrindMob.x < botCharacter.x) ? 5 : 4);
				botCharacter.facing = ((!((float)botGrindMob.x < botCharacter.x)) ? 1 : (-1));
				try
				{
					partyGrind.Strike(botCharacter, botGrindMob);
					nextSwing[bot] = now + 800;
				}
				catch (Exception)
				{
				}
			}
		}
		return false;
	}

	private BotGrindMob Nearest(BotCharacter bot, List<BotGrindMob> mobs)
	{
		HashSet<int> hashSet = new HashSet<int>();
		foreach (int bot2 in bots)
		{
			if (bot2 != bot.id && BotRegistry.Get(bot2)?.mapId == bot.mapId && combatClaims.TryGetValue(bot2, out var value))
			{
				hashSet.Add(value);
			}
		}
		combatClaims.TryGetValue(bot.id, out var value2);
		BotGrindMob botGrindMob = null;
		float num = float.MaxValue;
		foreach (BotGrindMob mob in mobs)
		{
			if (mob == null || !mob.alive || hashSet.Contains(mob.id))
			{
				continue;
			}
			if (mob.id == value2)
			{
				return mob;
			}
			float num2 = Math.Abs((float)mob.x - bot.x) + Math.Abs((float)mob.y - bot.y) * 2f;
			foreach (BotGrindMob mob2 in mobs)
			{
				if (mob2 != null && mob2.alive && hashSet.Contains(mob2.id))
				{
					num2 += (float)Math.Max(0, 1400 - Math.Abs(mob.x - mob2.x) - Math.Abs(mob.y - mob2.y) * 2);
				}
			}
			if (num2 < num)
			{
				num = num2;
				botGrindMob = mob;
			}
		}
		if (botGrindMob == null)
		{
			foreach (BotGrindMob mob3 in mobs)
			{
				if (mob3 != null && mob3.alive)
				{
					float num3 = Math.Abs((float)mob3.x - bot.x) + Math.Abs((float)mob3.y - bot.y) * 2f;
					if (num3 < num)
					{
						num = num3;
						botGrindMob = mob3;
					}
				}
			}
		}
		if (botGrindMob != null)
		{
			combatClaims[bot.id] = botGrindMob.id;
		}
		else
		{
			combatClaims.Remove(bot.id);
		}
		return botGrindMob;
	}

	public static bool IsWanted(PqStep step, BotGrindMob mob)
	{
		if (step.ids.Length == 0)
		{
			return true;
		}
		int value = ((mob.mobId > 0) ? mob.mobId : mob.id);
		return Array.IndexOf(step.ids, value) >= 0;
	}

	private bool WorkReactors(PqStep step, long now)
	{
		List<int> list = StepMaps(step);
		Dictionary<int, List<MapReactor>> live = new Dictionary<int, List<MapReactor>>();
		int num = 0;
		foreach (int item in list)
		{
			List<MapReactor> list2 = new List<MapReactor>();
			foreach (MapReactor item2 in MapReactorService.Get(Live(item)))
			{
				if (InStep(step, item2.id) && item2.Hittable)
				{
					list2.Add(item2);
				}
			}
			live[item] = list2;
			num += list2.Count;
		}
		if (num == 0)
		{
			return true;
		}
		if (now < nextActionAt)
		{
			return false;
		}
		nextActionAt = now + 420;
		AssignMaps(list, (int map) => live[map].Count);
		foreach (int bot in bots)
		{
			BotCharacter botCharacter = BotRegistry.Get(bot);
			if (botCharacter == null || !live.TryGetValue(botCharacter.mapId, out var value) || value.Count == 0)
			{
				continue;
			}
			MapReactor mapReactor = NearestReactor(botCharacter, value);
			if (mapReactor == null)
			{
				continue;
			}
			if (Math.Abs((float)mapReactor.x - botCharacter.x) > 60f)
			{
				MoveToward(botCharacter, mapReactor.x, mapReactor.y);
				continue;
			}
			botCharacter.stance = (((float)mapReactor.x < botCharacter.x) ? 5 : 4);
			botCharacter.facing = ((!((float)mapReactor.x < botCharacter.x)) ? 1 : (-1));
			BotAttack.BasicSwing(botCharacter, rng);
			try
			{
				MapReactorService.Hit(Live(botCharacter.mapId), mapReactor.objectId, null, botCharacter.id, partyId);
			}
			catch (Exception)
			{
			}
		}
		return false;
	}

	private bool InStep(PqStep step, int reactorId)
	{
		if (step.ids.Length != 0)
		{
			int[] ids = step.ids;
			for (int i = 0; i < ids.Length; i++)
			{
				if (ids[i] == reactorId)
				{
					return true;
				}
			}
			if (step.minId <= 0 && step.maxId <= 0)
			{
				return false;
			}
		}
		if (step.minId > 0 || step.maxId > 0)
		{
			if (reactorId >= Math.Min(step.minId, step.maxId))
			{
				return reactorId <= Math.Max(step.minId, step.maxId);
			}
			return false;
		}
		return step.ids.Length == 0;
	}

	private MapReactor NearestReactor(BotCharacter bot, List<MapReactor> list)
	{
		MapReactor result = null;
		float num = float.MaxValue;
		foreach (MapReactor item in list)
		{
			float num2 = Math.Abs((float)item.x - bot.x) + Math.Abs((float)item.y - bot.y) * 0.4f;
			if (num2 < num)
			{
				num = num2;
				result = item;
			}
		}
		return result;
	}

	private bool WorkStands(PqStep step, long now)
	{
		if (step.stands == null || step.stands.Count == 0)
		{
			return true;
		}
		if (now < nextActionAt)
		{
			return false;
		}
		nextActionAt = now + 420;
		bool flag = true;
		int num = 0;
		foreach (int bot in bots)
		{
			BotCharacter botCharacter = BotRegistry.Get(bot);
			if (botCharacter != null && botCharacter.mapId == CurrentMap)
			{
				PqRect pqRect = step.stands[num % step.stands.Count];
				num++;
				int centerX = pqRect.CenterX;
				int centerY = pqRect.CenterY;
				if (!(Math.Abs(botCharacter.x - (float)centerX) <= 26f) || !(Math.Abs(botCharacter.y - (float)centerY) <= 40f))
				{
					flag = false;
					MoveToward(botCharacter, centerX, centerY);
				}
			}
		}
		return (num > 0) & flag;
	}

	private bool WorkGrant(PqStep step)
	{
		PqDirector.Give(step.itemId, step.itemCount);
		return true;
	}

	private bool WorkTalk(PqStep step, long now)
	{
		if (step.npc == 0)
		{
			return true;
		}
		if (talkDone)
		{
			if (string.IsNullOrEmpty(step.awaitProperty))
			{
				return true;
			}
			string text = PqDirector.PropertyOf(step.awaitProperty);
			if ((step.awaitValue != null) ? (text == step.awaitValue) : (text != null))
			{
				return true;
			}
			if (talkRetries >= 3)
			{
				lastNote = CurrentStage?.title + "（NPC 没认账，先往下走）";
				PqDirector.Notify(def.name + "：" + step.awaitProperty + " 还是 " + (text ?? "空") + "，NPC 没有放行。");
				return true;
			}
			talkRetries++;
			talkSent = false;
			talkDone = false;
			talkRetryAt = now + 2500;
			return false;
		}
		if (now < talkRetryAt)
		{
			return false;
		}
		if (!talkSent)
		{
			talkSent = true;
			string[] prefer = step.prefer ?? new string[5] { "确定", "是", "下一步", "继续", "好" };
			PqDirector.Talk(step.npc, step.openOnly, prefer, (bool ok) =>
			{
				talkDone = true;
				if (!ok)
				{
					PqDirector.Notify(def.name + "：NPC " + step.npc + " 没有正常回应。");
				}
			});
		}
		return false;
	}

	private static int Held(BotCharacter bot, int itemId)
	{
		if (bot == null || itemId <= 0)
		{
			return 0;
		}
		int num = 0;
		foreach (InventoryEntry item in bot.inventory)
		{
			if (item != null && item.itemId == itemId)
			{
				num += item.quantity;
			}
		}
		return num;
	}

	private MapNpcPosition NpcOf(int npcId)
	{
		return MapService.Get(CurrentMap)?.npcs.Find((MapNpcPosition n) => n.id == npcId);
	}

	private bool NearNpc(BotCharacter bot, int npcId, float range = 110f)
	{
		MapNpcPosition mapNpcPosition = NpcOf(npcId);
		if (mapNpcPosition == null)
		{
			return true;
		}
		if (Math.Abs(bot.x - (float)mapNpcPosition.x) <= range)
		{
			return Math.Abs(bot.y - (float)mapNpcPosition.y) <= range * 2.2f;
		}
		return false;
	}

	private void WalkToNpc(BotCharacter bot, int npcId)
	{
		MapNpcPosition mapNpcPosition = NpcOf(npcId);
		if (mapNpcPosition != null)
		{
			int x = mapNpcPosition.x + ((bot.x < (float)mapNpcPosition.x) ? (-36) : 36);
			MoveToward(bot, x, mapNpcPosition.y);
		}
	}

	private bool WorkBotAsk(PqStep step, long now)
	{
		if (step.npc == 0 || bots.Count == 0)
		{
			return true;
		}
		if (now < nextActionAt)
		{
			return false;
		}
		nextActionAt = now + 420;
		int num = 0;
		foreach (int bot in bots)
		{
			BotCharacter botCharacter = BotRegistry.Get(bot);
			if (botCharacter == null || botCharacter.mapId != CurrentMap)
			{
				continue;
			}
			num++;
			if (asked.Contains(bot))
			{
				continue;
			}
			if (!NearNpc(botCharacter, step.npc))
			{
				WalkToNpc(botCharacter, step.npc);
				continue;
			}
			BotMovementService.Stop(botCharacter);
			if (!PqDirector.BotTalk(botCharacter, step.npc, out var say))
			{
				questScriptFailed = true;
				asked.Add(bot);
				continue;
			}
			asked.Add(bot);
			int num2 = PqCatalog.Stage1Answer(PqDirector.GridOf(bot));
			if (num2 > 0)
			{
				int num3 = Held(botCharacter, 4001007) - num2;
				if (num3 > 0)
				{
					PqDirector.BotGain(botCharacter, 4001007, -num3);
				}
				couponNeed[bot] = num2;
				if (!string.IsNullOrEmpty(say))
				{
					PqDirector.Say(botCharacter, say);
				}
				PqDirector.Say(botCharacter, "我这题要 " + num2 + " 张优惠券，收够了回 Cloto 换证");
			}
			else if (!string.IsNullOrEmpty(say))
			{
				PqDirector.Say(botCharacter, say);
			}
		}
		if (num != 0)
		{
			return asked.Count >= num;
		}
		return true;
	}

	private bool WorkBotCollect(PqStep step, long now)
	{
		int num = ((step.itemId > 0) ? step.itemId : 4001007);
		if (bots.Count == 0 || num <= 0)
		{
			return true;
		}
		if (CollectDone(step, num))
		{
			return true;
		}
		if (now < nextActionAt)
		{
			return false;
		}
		nextActionAt = now + 420;
		List<int> list = StepMaps(step);
		Dictionary<int, int> work = new Dictionary<int, int>();
		foreach (int item in list)
		{
			work[item] = GroundCount(item, num) + TargetCount(step, item);
		}
		AssignMaps(list, (int map) => work[map]);
		foreach (int bot in bots)
		{
			BotCharacter botCharacter = BotRegistry.Get(bot);
			if (botCharacter == null)
			{
				continue;
			}
			int num2 = CollectShortfall(step, num, botCharacter);
			if (num2 <= 0)
			{
				combatClaims.Remove(bot);
			}
			else if (PqDirector.Pickup(botCharacter, num, num2) <= 0 && (step.reactors.Length != 0 || !StrikeNearest(botCharacter, step, now, inRangeOnly: true)) && !WalkToDrop(botCharacter, num))
			{
				if (step.reactors.Length != 0)
				{
					BreakNearest(botCharacter, step);
				}
				else if (!StrikeNearest(botCharacter, step, now) && !PqDirector.WalkToDrop(botCharacter, num))
				{
					Sweep(botCharacter, now);
				}
			}
		}
		return false;
	}

	private bool CollectDone(PqStep step, int item)
	{
		if (step.need > 0)
		{
			return HeldAll(item) >= step.need;
		}
		if (couponNeed.Count == 0)
		{
			return HeldAll(item) >= 1;
		}
		int num = 0;
		foreach (int bot in bots)
		{
			BotCharacter botCharacter = BotRegistry.Get(bot);
			if (botCharacter != null && couponNeed.TryGetValue(bot, out var value) && value > 0)
			{
				num++;
				if (Held(botCharacter, item) < value)
				{
					return false;
				}
			}
		}
		if (num <= 0)
		{
			return HeldAll(item) >= 1;
		}
		return true;
	}

	private int CollectShortfall(PqStep step, int item, BotCharacter bot)
	{
		if (step.need > 0)
		{
			return step.need - HeldAll(item);
		}
		if (couponNeed.TryGetValue(bot.id, out var value) && value > 0)
		{
			return value - Held(bot, item);
		}
		if (Held(bot, item) <= 0)
		{
			return 1;
		}
		return 0;
	}

	private bool WalkToDrop(BotCharacter bot, int item)
	{
		return PqDirector.WalkToDrop(bot, item, 340f);
	}

	private int HeldAll(int itemId)
	{
		int num = 0;
		foreach (int bot in bots)
		{
			BotCharacter botCharacter = BotRegistry.Get(bot);
			if (botCharacter != null)
			{
				num += Held(botCharacter, itemId);
			}
		}
		return num;
	}

	private static int GroundCount(int mapId, int itemId)
	{
		int num = 0;
		try
		{
			foreach (MapItem item in MapItemService.DroppedItems(Live(mapId)))
			{
				if (item != null && !item.pickedUp && !item.IsMeso && item.ItemId == itemId)
				{
					num++;
				}
			}
			return num;
		}
		catch (Exception)
		{
			return 0;
		}
	}

	private int TargetCount(PqStep step, int mapId)
	{
		int num = 0;
		if (step.reactors.Length != 0)
		{
			foreach (MapReactor item in MapReactorService.Get(Live(mapId)))
			{
				if (item.Hittable && InStep(step, item.id))
				{
					num++;
				}
			}
		}
		if (step.ids.Length != 0 || step.reactors.Length == 0)
		{
			BotPartyGrindService partyGrind = BotTypeManager.PartyGrind;
			BotActor botActor = PqDirector.LocalPlayer();
			if (partyGrind != null && botActor != null)
			{
				List<BotGrindMob> list = null;
				try
				{
					list = partyGrind.Monsters?.Invoke(Live(mapId), botActor.world, botActor.channel);
				}
				catch (Exception)
				{
					list = null;
				}
				if (list != null)
				{
					foreach (BotGrindMob item2 in list)
					{
						if (item2 != null && item2.alive && !item2.fake && IsWanted(step, item2))
						{
							num++;
						}
					}
				}
			}
		}
		return num;
	}

	private void BreakNearest(BotCharacter bot, PqStep step)
	{
		if (bot == null)
		{
			return;
		}
		MapReactor mapReactor = null;
		float num = float.MaxValue;
		foreach (MapReactor item in MapReactorService.Get(Live(bot.mapId)))
		{
			if (item.Hittable && InStep(step, item.id))
			{
				float num2 = Math.Abs((float)item.x - bot.x) + Math.Abs((float)item.y - bot.y) * 0.4f;
				if (num2 < num)
				{
					num = num2;
					mapReactor = item;
				}
			}
		}
		if (mapReactor == null)
		{
			return;
		}
		if (Math.Abs((float)mapReactor.x - bot.x) > 60f)
		{
			MoveToward(bot, mapReactor.x, mapReactor.y);
			return;
		}
		bot.stance = (((float)mapReactor.x < bot.x) ? 5 : 4);
		bot.facing = ((!((float)mapReactor.x < bot.x)) ? 1 : (-1));
		BotAttack.BasicSwing(bot, rng);
		try
		{
			MapReactorService.Hit(Live(bot.mapId), mapReactor.objectId, null, bot.id, partyId);
		}
		catch (Exception)
		{
		}
	}

	private bool StrikeNearest(BotCharacter bot, PqStep step, long now, bool inRangeOnly = false)
	{
		BotPartyGrindService partyGrind = BotTypeManager.PartyGrind;
		BotActor botActor = PqDirector.LocalPlayer();
		if (partyGrind == null || botActor == null || bot == null)
		{
			return true;
		}
		List<BotGrindMob> list = null;
		try
		{
			list = partyGrind.Monsters?.Invoke(Live(bot.mapId), botActor.world, botActor.channel);
		}
		catch (Exception)
		{
			list = null;
		}
		if (list == null)
		{
			NoteNoTarget(now, bot, 0);
			return false;
		}
		List<BotGrindMob> list2 = new List<BotGrindMob>();
		int num = 0;
		foreach (BotGrindMob item in list)
		{
			if (item != null && item.alive && !item.fake && !item.boss)
			{
				num++;
				if (IsWanted(step, item))
				{
					list2.Add(item);
				}
			}
		}
		BotGrindMob botGrindMob = Nearest(bot, list2);
		if (botGrindMob == null)
		{
			NoteNoTarget(now, bot, num);
			return false;
		}
		bot.equips.TryGetValue(-11, out var value);
		(int, int) tuple = BotAttack.EngageRange(bot.job, value);
		if (Math.Abs((float)botGrindMob.x - bot.x) > (float)tuple.Item1 || Math.Abs((float)botGrindMob.y - bot.y) > (float)tuple.Item2)
		{
			if (inRangeOnly)
			{
				return false;
			}
			MoveToward(bot, botGrindMob.x, botGrindMob.y);
			return true;
		}
		BotMovementService.Stop(bot);
		bot.stance = (((float)botGrindMob.x < bot.x) ? 5 : 4);
		bot.facing = ((!((float)botGrindMob.x < bot.x)) ? 1 : (-1));
		if (nextSwing.TryGetValue(bot.id, out var value2) && now < value2)
		{
			return true;
		}
		try
		{
			partyGrind.Strike(bot, botGrindMob);
			nextSwing[bot.id] = now + 800;
		}
		catch (Exception)
		{
		}
		return true;
	}

	private void Sweep(BotCharacter bot, long now)
	{
		if (nextSweepAt.TryGetValue(bot.id, out var value) && now < value)
		{
			return;
		}
		nextSweepAt[bot.id] = now + 2600;
		MapRuntime mapRuntime = MapService.Get(bot.mapId);
		int num = ((mapRuntime != null && mapRuntime.maxX > mapRuntime.minX) ? ((int)mapRuntime.minX + 60) : ((int)bot.x - 400));
		int num2 = ((mapRuntime != null && mapRuntime.maxX > mapRuntime.minX) ? ((int)mapRuntime.maxX - 60) : ((int)bot.x + 400));
		if (num2 > num)
		{
			int num3 = (num + num2) / 2;
			int num4 = Math.Max(120, (num2 - num) / 2);
			int num5 = ((!(bot.x >= (float)num3)) ? 1 : (-1));
			int num6 = (int)bot.x + num5 * num4;
			if (num6 < num || num6 > num2)
			{
				num5 = -num5;
				num6 = (int)bot.x + num5 * num4;
			}
			num6 = Math.Max(num, Math.Min(num2, num6));
			MoveToward(bot, num6, (int)bot.y);
		}
	}

	private void NoteNoTarget(long now, BotCharacter bot, int visible)
	{
		if (noTargetSince == 0L)
		{
			noTargetSince = now;
		}
		if (!noTargetNoted && now - noTargetSince >= 8000)
		{
			noTargetNoted = true;
			PqDirector.Notify(def.name + "：这一步在图 " + (bot?.mapId ?? 0) + "（实例 " + ((bot != null) ? Live(bot.mapId) : 0) + "）里看不到可打的怪" + ((visible > 0) ? ("（场上有 " + visible + " 只，但都不是这一步要的）") : "") + "，假人先自己扫图找。");
		}
	}

	private bool WorkBotExchange(PqStep step, long now)
	{
		if (step.npc == 0 || bots.Count == 0)
		{
			return true;
		}
		bool flag = false;
		foreach (int bot in bots)
		{
			BotCharacter botCharacter = BotRegistry.Get(bot);
			if (botCharacter == null || botCharacter.mapId != CurrentMap)
			{
				continue;
			}
			int num = (couponNeed.TryGetValue(bot, out var value) ? value : 0);
			if (num > 0)
			{
				int num2 = Held(botCharacter, 4001007);
				if (num2 > num)
				{
					PqDirector.BotGain(botCharacter, 4001007, -(num2 - num));
				}
				else if (num2 < num)
				{
					flag = true;
					PqDirector.Pickup(botCharacter, 4001007, num - num2);
				}
			}
		}
		if (flag)
		{
			if (WorkMobs(step, now, boss: false))
			{
				foreach (int bot2 in bots)
				{
					BotCharacter botCharacter2 = BotRegistry.Get(bot2);
					if (botCharacter2 != null && botCharacter2.mapId == CurrentMap)
					{
						int num3 = (couponNeed.TryGetValue(bot2, out var value2) ? value2 : 0);
						if (num3 <= 0 || Held(botCharacter2, 4001007) < num3)
						{
							Sweep(botCharacter2, now);
						}
					}
				}
			}
			return false;
		}
		if (now < nextActionAt)
		{
			return false;
		}
		nextActionAt = now + 420;
		int num4 = 0;
		int num5 = 0;
		foreach (int bot3 in bots)
		{
			BotCharacter botCharacter3 = BotRegistry.Get(bot3);
			if (botCharacter3 == null || botCharacter3.mapId != CurrentMap)
			{
				continue;
			}
			num4++;
			if (passReady.Contains(bot3))
			{
				num5++;
				continue;
			}
			int num6 = (couponNeed.TryGetValue(bot3, out var value3) ? value3 : 0);
			if (!NearNpc(botCharacter3, step.npc))
			{
				WalkToNpc(botCharacter3, step.npc);
				continue;
			}
			BotMovementService.Stop(botCharacter3);
			if (!PqDirector.BotTalk(botCharacter3, step.npc, out var say))
			{
				questScriptFailed = true;
				continue;
			}
			if (Held(botCharacter3, 4001008) > 0)
			{
				passReady.Add(bot3);
				num5++;
				PqDirector.Say(botCharacter3, "换到通行证了，我去 Cloto 旁边等队长");
				continue;
			}
			if (num6 <= 0)
			{
				int num7 = PqCatalog.Stage1Answer(PqDirector.GridOf(bot3));
				if (num7 > 0)
				{
					couponNeed[bot3] = num7;
				}
			}
			if (!string.IsNullOrEmpty(say))
			{
				PqDirector.Say(botCharacter3, say);
			}
		}
		if (questScriptFailed)
		{
			return true;
		}
		if (num4 != 0)
		{
			return num5 >= num4;
		}
		return true;
	}

	private bool WorkBotWait(PqStep step, long now)
	{
		if (bots.Count == 0)
		{
			return true;
		}
		if (now < nextActionAt)
		{
			return false;
		}
		nextActionAt = now + 420;
		int num = 0;
		bool flag = true;
		foreach (int bot in bots)
		{
			BotCharacter botCharacter = BotRegistry.Get(bot);
			if (botCharacter != null && botCharacter.mapId == CurrentMap)
			{
				num++;
				if (NearNpc(botCharacter, step.npc, 70f))
				{
					BotMovementService.Stop(botCharacter);
					continue;
				}
				flag = false;
				WalkToNpc(botCharacter, step.npc);
			}
		}
		return (num > 0) & flag;
	}

	private bool WorkHandOver(PqStep step)
	{
		int itemId = step.itemId;
		if (itemId <= 0)
		{
			return true;
		}
		int num = ((step.itemCount > 0) ? step.itemCount : Math.Max(1, bots.Count));
		if (questScriptFailed)
		{
			PqDirector.Give(itemId, num);
			return true;
		}
		int num2 = 0;
		foreach (int bot in bots)
		{
			if (num2 >= num)
			{
				break;
			}
			BotCharacter botCharacter = BotRegistry.Get(bot);
			if (botCharacter != null)
			{
				int num3 = Held(botCharacter, itemId);
				if (num3 > 0)
				{
					int num4 = Math.Min(num3, num - num2);
					PqDirector.BotGain(botCharacter, itemId, -num4);
					PqDirector.Give(itemId, num4);
					num2 += num4;
					PqDirector.Say(botCharacter, "通行证给你，去点 Cloto 吧");
				}
			}
		}
		if (num2 < num)
		{
			PqDirector.Give(itemId, num - num2);
			PqDirector.Notify(def.name + "：还差 " + (num - num2) + " 张证，先由队长补上");
		}
		return true;
	}

	private bool WorkStageCheck(PqStep step, long now)
	{
		if (PqDirector.PropertyOf((CurrentStage?.stageNo ?? 0) + "stageclear") != null)
		{
			return true;
		}
		if (now < nextActionAt)
		{
			return false;
		}
		nextActionAt = now + 2600;
		if (checkAttempts < 2)
		{
			PlaceByCombo(step);
		}
		else
		{
			ReleaseBots();
		}
		checkAttempts++;
		PqDirector.Talk(step.npc, openOnly: false, new string[4] { "确定", "是", "下一步", "继续" }, (bool _) =>
		{
		});
		return checkAttempts >= 5;
	}

	private void PlaceByCombo(PqStep step)
	{
		int.TryParse(PqDirector.PropertyOf(step.comboProperty), out var result);
		int[] array = PqCatalog.Combo(step.comboProperty, result);
		(int x, int y) tuple = PqDirector.EntryPoint(CurrentMap);
		int item = tuple.x;
		int item2 = tuple.y;
		List<int> list = new List<int>();
		if (array != null && step.stands != null)
		{
			for (int i = 0; i < array.Length && i < step.stands.Count; i++)
			{
				if (array[i] == 1)
				{
					list.Add(i);
				}
			}
		}
		int num = 0;
		foreach (int bot in bots)
		{
			BotCharacter botCharacter = BotRegistry.Get(bot);
			if (botCharacter != null && botCharacter.mapId == CurrentMap)
			{
				if (num < list.Count)
				{
					PqRect pqRect = step.stands[list[num++]];
					MoveToward(botCharacter, pqRect.CenterX, pqRect.CenterY);
				}
				else if (Math.Abs(botCharacter.x - (float)item) > 40f)
				{
					MoveToward(botCharacter, item, item2);
				}
			}
		}
	}

	private void AdvanceStage(long now)
	{
		int num = stageIndex + 1;
		if (num >= def.stages.Count)
		{
			Clear(now);
			return;
		}
		PqStage pqStage = def.stages[num];
		if (PqDirector.TemplateMapId(PqDirector.LocalMapId()) != pqStage.mapId)
		{
			PqDirector.WarpPlayer(pqStage.mapId);
		}
		EnterStage(num, now);
	}

	private void Clear(long now)
	{
		if (rewardUntil == 0L)
		{
			rewardUntil = now + 6000;
			foreach (int bot in bots)
			{
				BotCharacter botCharacter = BotRegistry.Get(bot);
				if (botCharacter != null)
				{
					PqDirector.Say(botCharacter, PqChatLines.afterClear[rng.Next(PqChatLines.afterClear.Length)]);
				}
			}
			PqDirector.Notify(def.name + " 组队任务完成，队伍即将解散。");
			int num = ((def.exitMap > 0) ? def.exitMap : def.recruitMap);
			if (PqDirector.LocalMapId() != num)
			{
				PqDirector.WarpPlayer(num);
			}
			{
				foreach (int bot2 in bots)
				{
					BotCharacter botCharacter2 = BotRegistry.Get(bot2);
					if (botCharacter2 != null)
					{
						PqDirector.WarpBot(botCharacter2, num);
					}
				}
				return;
			}
		}
		if (now >= rewardUntil)
		{
			End(cleared: true);
		}
	}

	public void End(bool cleared)
	{
		if (finished)
		{
			return;
		}
		finished = true;
		ReleaseBots();
		int recruitMap = def.recruitMap;
		foreach (int bot in bots)
		{
			BotCharacter botCharacter = BotRegistry.Get(bot);
			if (botCharacter != null)
			{
				if (!cleared)
				{
					PqDirector.Say(botCharacter, "这次没过，下次再来。");
				}
				if (botCharacter.mapId != recruitMap)
				{
					PqDirector.WarpBot(botCharacter, recruitMap);
				}
			}
		}
		if (def.exitMap > 0 && PqDirector.LocalMapId() != def.exitMap && PqDirector.LocalMapId() != recruitMap)
		{
			PqDirector.WarpPlayer(def.exitMap);
		}
		PqDirector.Finish(this);
	}

	public void DriveBot(BotCharacter bot, long now)
	{
		if (bot == null || finished)
		{
			return;
		}
		if (!BotAllowedHere(bot))
		{
			if (CurrentMap != 0)
			{
				PqDirector.WarpBot(bot, CurrentMap);
			}
		}
		else if (!stageWorkDone && now >= nextActionAt && rng.Next(100) < 12)
		{
			BotAttack.BasicSwing(bot, rng);
		}
	}

	private bool BotAllowedHere(BotCharacter bot)
	{
		if (bot == null)
		{
			return false;
		}
		if (bot.mapId == CurrentMap)
		{
			return true;
		}
		PqStage currentStage = CurrentStage;
		if (currentStage == null || stepIndex < 0 || stepIndex >= currentStage.steps.Count)
		{
			return false;
		}
		PqStep pqStep = currentStage.steps[stepIndex];
		if (pqStep.maps == null || pqStep.maps.Length == 0)
		{
			return false;
		}
		return Array.IndexOf(pqStep.maps, bot.mapId) >= 0;
	}

	private BotCharacter RandomBot()
	{
		if (bots.Count == 0)
		{
			return null;
		}
		return BotRegistry.Get(bots[rng.Next(bots.Count)]);
	}

	private static void MoveToward(BotCharacter bot, int x, int y)
	{
		if (bot == null)
		{
			return;
		}
		if (!BotMovementService.Observed(bot.mapId))
		{
			bot.x = x;
			bot.y = y;
		}
		else if (BotMovementService.PrepareNavigation(bot))
		{
			int y2 = (int)MapService.SnapToGround(bot.mapId, x, y);
			if (!BotMovementService.IsMovingToward(bot, x, y2))
			{
				BotMovementService.Move(bot, x, y2);
			}
		}
	}
}
