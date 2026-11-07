using System;
using Server.Bot.Environment;

namespace Server.Bot.Types;

public sealed class CompanionGrindBot : BotStateMachine
{
	private readonly BotPartyGrindService service;

	private readonly Random rng = new Random();

	private int leaderId;

	private int lastMoveX = int.MinValue;

	private int lastMoveY = int.MinValue;

	private long nextSwing;

	private long nextMoveAt;

	private long nextChatAt;

	private int trackedTarget;

	private int bestTargetDistance = int.MaxValue;

	private long targetProgressAt;

	private long blockedLootUntil;

	private int blockedLootX;

	private int blockedLootY;

	private bool blocksLoot;

	private static readonly string[] IdleLines = new string[3] { "这边清完了，我去旁边看看。", "还没刷怪，先转一圈。", "我看看上面还有没有怪。" };

	private static readonly string[] BossLines = new string[2] { "发现Boss，先集火！", "先打Boss，小怪等会儿清。" };

	private long nextLootAt;

	private long lootUntil;

	private int lootX;

	private int lootY;

	private bool hasLoot;

	private long nextPatrolAt;

	private long lastCombatAt;

	private int patrolX = int.MinValue;

	private int patrolY = int.MinValue;

	public CompanionGrindBot(BotCharacter bot, BotPartyGrindService service)
		: base(bot)
	{
		this.service = service ?? throw new ArgumentNullException("service");
		leaderId = service.LeaderId(bot.id);
		botType = "CompanionGrindBot";
		IsCompanion = () => true;
		nextChatAt = BotScheduler.Now() + 20000 + rng.Next(20000);
	}

	protected override long DefaultPeriodMs()
	{
		return 220L;
	}

	public override void CheckPrioritySpeed()
	{
		UpdateScheduleDelay(220L);
	}

	public override void StartScheduledTask(long initialDelayMs)
	{
		base.StartScheduledTask(Math.Min(initialDelayMs, 220L));
	}

	public override bool IsAvailableForAmbientActions()
	{
		return false;
	}

	public override void UpdateState()
	{
		base.UpdateState();
		if (CheckIfNotRunningOrPaused() || state == State.TRADING)
		{
			return;
		}
		BotActor botActor = service.Leader(chr.id);
		if (botActor == null)
		{
			service.Stop(leaderId);
			StopScheduledTask();
			return;
		}
		leaderId = botActor.id;
		if (!service.SameParty(chr, botActor))
		{
			service.Stop(botActor.id, "组队已解散，基友集合结束。");
		}
		else if (chr.mapId != botActor.mapId || chr.channel != botActor.channel || chr.world != botActor.world)
		{
			BotNavigationGraph.Point point = service.Station(chr, botActor);
			BotMovementService.Disable(chr);
			chr.world = botActor.world;
			chr.channel = botActor.channel;
			BotRegistry.EnterMap(chr, botActor.mapId);
			chr.x = point.x;
			chr.y = point.y;
			ResetMapActions();
			service.ReleaseTarget(chr);
		}
		else
		{
			if (!BotMovementService.PrepareNavigation(chr))
			{
				return;
			}
			BotMovementService.SetGrinding(chr, grinding: true);
			long num = BotScheduler.Now();
			if (lastCombatAt == 0L)
			{
				lastCombatAt = num;
			}
			BotNavigationGraph.Point destination = service.Station(chr, botActor);
			BotGrindMob botGrindMob = service.Target(chr, botActor);
			if (botGrindMob != null)
			{
				int num2 = (int)(Math.Abs(chr.x - (float)botGrindMob.x) + Math.Abs(chr.y - (float)botGrindMob.y) * 2f);
				if (trackedTarget != botGrindMob.id)
				{
					trackedTarget = botGrindMob.id;
					bestTargetDistance = num2;
					targetProgressAt = num;
				}
				else if (num2 < bestTargetDistance - 16)
				{
					bestTargetDistance = num2;
					targetProgressAt = num;
				}
				chr.equips.TryGetValue(-11, out var value);
				(int, int) tuple = BotAttack.EngageRange(chr.job, value);
				if (Math.Abs(chr.x - (float)botGrindMob.x) <= (float)tuple.Item1 && Math.Abs(chr.y - (float)botGrindMob.y) <= (float)tuple.Item2)
				{
					targetProgressAt = num;
				}
				else if (num - targetProgressAt > 10000)
				{
					service.AvoidTarget(chr, botGrindMob.id, num);
					BotMovementService.Stop(chr);
					lastMoveX = int.MinValue;
					trackedTarget = 0;
					botGrindMob = null;
				}
			}
			else
			{
				trackedTarget = 0;
			}
			if (botGrindMob != null)
			{
				lastCombatAt = num;
				if (botGrindMob.boss && num >= nextChatAt && service.TryChat(chr, BossLines[rng.Next(BossLines.Length)], num))
				{
					nextChatAt = num + 25000 + rng.Next(20000);
				}
			}
			if (hasLoot && num > lootUntil)
			{
				blocksLoot = true;
				blockedLootX = lootX;
				blockedLootY = lootY;
				blockedLootUntil = num + 10000;
				hasLoot = false;
			}
			if (num >= nextLootAt)
			{
				nextLootAt = num + 400;
				GrindLoot grindLoot = service.Loot(chr, (botGrindMob == null) ? 360f : 120f);
				if (grindLoot.found && grindLoot.distance <= 56f && service.TakeLootNearby(chr, 60f, 2) > 0)
				{
					hasLoot = false;
					lootUntil = 0L;
					grindLoot = service.Loot(chr, (botGrindMob == null) ? 360f : 120f);
				}
				if (grindLoot.found && blocksLoot && num < blockedLootUntil && Math.Abs(grindLoot.x - blockedLootX) <= 16 && Math.Abs(grindLoot.y - blockedLootY) <= 24)
				{
					grindLoot.found = false;
				}
				if (grindLoot.found && grindLoot.distance <= 380f)
				{
					if (!hasLoot || Math.Abs(grindLoot.x - lootX) > 16 || Math.Abs(grindLoot.y - lootY) > 24)
					{
						lootUntil = num + 6000;
					}
					hasLoot = true;
					lootX = grindLoot.x;
					lootY = grindLoot.y;
				}
				else
				{
					hasLoot = false;
				}
			}
			if (hasLoot && num <= lootUntil && (botGrindMob == null || (!botGrindMob.boss && NearX(lootX, 90) && NearY(lootY, 120))))
			{
				if (!NearX(lootX, 34) || !NearY(lootY, 90))
				{
					Approach(new BotNavigationGraph.Point(lootX, lootY));
					return;
				}
				if (service.TakeLootNearby(chr, 56f, 2) == 0)
				{
					blocksLoot = true;
					blockedLootX = lootX;
					blockedLootY = lootY;
					blockedLootUntil = num + 10000;
				}
				hasLoot = false;
				lootUntil = 0L;
			}
			if (botGrindMob != null)
			{
				chr.equips.TryGetValue(-11, out var value2);
				(int, int) tuple2 = BotAttack.EngageRange(chr.job, value2);
				if (Math.Abs(chr.x - (float)botGrindMob.x) <= (float)tuple2.Item1 && Math.Abs(chr.y - (float)botGrindMob.y) <= (float)tuple2.Item2)
				{
					BotMovementService.Stop(chr);
					lastMoveX = int.MinValue;
					if (num >= nextSwing)
					{
						service.Strike(chr, botGrindMob);
						nextSwing = num + 800;
					}
				}
				else
				{
					Approach(new BotNavigationGraph.Point(botGrindMob.x, botGrindMob.y));
				}
				return;
			}
			if (num >= nextChatAt && num - lastCombatAt >= 5000 && service.TryChat(chr, IdleLines[rng.Next(IdleLines.Length)], num))
			{
				nextChatAt = num + 25000 + rng.Next(20000);
			}
			if (num - lastCombatAt > 15000)
			{
				BotNavigationGraph.Point? point2 = service.LeaderSpot(chr, botActor);
				if (point2.HasValue && (!NearX(point2.Value.x, 56) || !NearY(point2.Value.y, 110)))
				{
					Approach(point2.Value);
					patrolX = int.MinValue;
					nextPatrolAt = num + 1500;
					return;
				}
				lastCombatAt = num;
			}
			if (!service.OnFloor(chr) || Math.Abs(chr.x - botActor.x) > 520f)
			{
				Approach(destination);
				patrolX = int.MinValue;
				nextPatrolAt = num + 1500;
			}
			else if (num >= nextPatrolAt)
			{
				BotNavigationGraph.Point? point3 = service.PatrolPoint(chr, botActor);
				if (point3.HasValue)
				{
					patrolX = point3.Value.x;
					patrolY = point3.Value.y;
					Approach(new BotNavigationGraph.Point(patrolX, patrolY));
					nextPatrolAt = num + 2400 + rng.Next(2600);
				}
				else
				{
					nextPatrolAt = num + 2000;
				}
			}
			else if (patrolX != int.MinValue && (!NearX(patrolX, 40) || !NearY(patrolY, 90)))
			{
				Approach(new BotNavigationGraph.Point(patrolX, patrolY));
			}
		}
	}

	private void ResetMapActions()
	{
		lastMoveX = (lastMoveY = int.MinValue);
		patrolX = (patrolY = int.MinValue);
		hasLoot = false;
		blocksLoot = false;
		lootUntil = (nextLootAt = (nextPatrolAt = 0L));
		trackedTarget = 0;
		bestTargetDistance = int.MaxValue;
		targetProgressAt = (lastCombatAt = 0L);
		nextMoveAt = (nextSwing = 0L);
	}

	private bool NearX(int x, int tolerance)
	{
		return Math.Abs(chr.x - (float)x) <= (float)tolerance;
	}

	private bool NearY(int y, int tolerance)
	{
		return Math.Abs(chr.y - (float)y) <= (float)tolerance;
	}

	private void Approach(BotNavigationGraph.Point destination)
	{
		if (Math.Abs((long)destination.x - (long)lastMoveX) >= 16 || Math.Abs((long)destination.y - (long)lastMoveY) >= 16 || !BotMovementService.IsMoving(chr))
		{
			long num = BotScheduler.Now();
			if (!BotMovementService.IsMoving(chr) || num >= nextMoveAt)
			{
				nextMoveAt = num + 700;
				lastMoveX = destination.x;
				lastMoveY = destination.y;
				BotMovementService.Move(chr, destination.x, destination.y);
			}
		}
	}
}
