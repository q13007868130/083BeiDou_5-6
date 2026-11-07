using System;
using System.Collections.Generic;
using Server.Bot.Environment;
using Server.Field;

namespace Server.Bot;

public sealed class BotWander
{
	public enum State
	{
		Dwell,
		Walk
	}

	private const float ArriveX = 25f;

	private const float ArriveY = 60f;

	private const float ProgressEps = 12f;

	private const long StuckMs = 6000L;

	private const long DwellMinMs = 2500L;

	private const long DwellMaxMs = 6000L;

	private const float MinStroll = 60f;

	public State state;

	public float targetX;

	public float targetY;

	public long dwellUntilMs;

	public float bestDist = float.MaxValue;

	public long progressAtMs;

	public int mapId;

	public bool banded;

	private long nextPollMs;

	private bool navigationReady;

	public float bandLo;

	public float bandHi;

	private static readonly Random rng = new Random();

	private static readonly List<BotCharacter> active = new List<BotCharacter>();

	private static readonly HashSet<int> activeIds = new HashSet<int>();

	private static readonly BotGroundSpotPicker picker = new BotGroundSpotPicker(rng);

	public static void Start(BotCharacter bot, bool banded, float lo, float hi)
	{
		if (bot != null && MapService.Get(bot.mapId) != null)
		{
			bot.wander = new BotWander
			{
				mapId = bot.mapId,
				banded = banded,
				bandLo = lo,
				bandHi = hi,
				state = State.Dwell,
				dwellUntilMs = 0L,
				progressAtMs = BotScheduler.Now(),
				nextPollMs = BotScheduler.Now() + rng.Next(1500)
			};
			if (activeIds.Add(bot.id))
			{
				active.Add(bot);
			}
		}
	}

	public static void Stop(BotCharacter bot)
	{
		bot.wander = null;
		bot.moving = false;
	}

	public static bool IsWandering(BotCharacter bot)
	{
		return bot.wander != null;
	}

	public static void Tick(BotCharacter bot, long nowMs)
	{
		BotWander wander = bot.wander;
		if (wander == null)
		{
			return;
		}
		if (bot.mapId != wander.mapId)
		{
			Stop(bot);
		}
		else
		{
			if (nowMs < wander.nextPollMs)
			{
				return;
			}
			wander.nextPollMs += 400L;
			if (!BotMovementService.Observed(bot.mapId))
			{
				return;
			}
			MapRuntime mapRuntime = MapService.Get(bot.mapId);
			if (mapRuntime == null || mapRuntime.footholds.Count == 0)
			{
				bot.moving = false;
			}
			else
			{
				if (!BotMovementService.PrepareNavigation(bot))
				{
					return;
				}
				if (!wander.navigationReady)
				{
					BotMovementService.Enable(bot);
					if (!wander.banded)
					{
						BotNavigationGraph botNavigationGraph = BotMovementService.Graphs.Get(mapRuntime);
						HashSet<int> hashSet = BotMovementService.Reachable(botNavigationGraph, new BotGroundIndex(mapRuntime), new BotNavigationGraph.Point((int)bot.x, (int)bot.y));
						int num = int.MaxValue;
						int num2 = int.MinValue;
						foreach (BotNavigationGraph.Region region in botNavigationGraph.regions)
						{
							if (!region.isRopeRegion && (hashSet.Count == 0 || hashSet.Contains(region.id)))
							{
								num = Math.Min(num, region.minX);
								num2 = Math.Max(num2, region.maxX);
							}
						}
						if (num2 > num)
						{
							int num3 = Math.Max(30, (int)(0.075 * (double)(num2 - num)));
							if (num2 - num3 > num + num3)
							{
								wander.banded = true;
								wander.bandLo = num + num3;
								wander.bandHi = num2 - num3;
							}
						}
					}
					wander.navigationReady = true;
				}
				if (wander.state == State.Dwell)
				{
					if (nowMs < wander.dwellUntilMs)
					{
						bot.moving = false;
						return;
					}
					(float, float)? tuple = PickStroll(bot, mapRuntime, wander);
					if (!tuple.HasValue)
					{
						wander.dwellUntilMs = nowMs + 2500;
						bot.moving = false;
						return;
					}
					wander.targetX = tuple.Value.Item1;
					wander.targetY = tuple.Value.Item2;
					wander.state = State.Walk;
					wander.bestDist = float.MaxValue;
					wander.progressAtMs = nowMs;
					BotMovementService.Move(bot, (int)wander.targetX, (int)wander.targetY);
					return;
				}
				float value = wander.targetX - bot.x;
				float value2 = wander.targetY - bot.y;
				if (Math.Abs(value) <= 25f && Math.Abs(value2) <= 60f && !BotMovementService.IsMoving(bot))
				{
					wander.state = State.Dwell;
					wander.dwellUntilMs = nowMs + 2500 + (long)(rng.NextDouble() * 3500.0);
					bot.moving = false;
					return;
				}
				float num4 = Math.Abs(value) + Math.Abs(value2);
				if (num4 < wander.bestDist - 12f)
				{
					wander.bestDist = num4;
					wander.progressAtMs = nowMs;
				}
				else if (nowMs - wander.progressAtMs > 6000)
				{
					(float, float)? tuple2 = PickStroll(bot, mapRuntime, wander);
					if (tuple2.HasValue)
					{
						wander.targetX = tuple2.Value.Item1;
						wander.targetY = tuple2.Value.Item2;
						wander.bestDist = float.MaxValue;
						wander.progressAtMs = nowMs;
						BotMovementService.Move(bot, (int)wander.targetX, (int)wander.targetY);
					}
					else
					{
						wander.state = State.Dwell;
						wander.dwellUntilMs = nowMs + 2500;
						bot.moving = false;
					}
					return;
				}
				if (!BotMovementService.IsMoving(bot))
				{
					BotMovementService.Move(bot, (int)wander.targetX, (int)wander.targetY);
				}
			}
		}
	}

	public static void AdvanceAll(long nowMs)
	{
		using (BotPerf.Timer("BotWander.AdvanceAll"))
		{
			for (int num = active.Count - 1; num >= 0; num--)
			{
				BotCharacter botCharacter = active[num];
				if (botCharacter == null || botCharacter.wander == null || !BotRegistry.Contains(botCharacter.id))
				{
					if (botCharacter != null)
					{
						activeIds.Remove(botCharacter.id);
					}
					active[num] = active[active.Count - 1];
					active.RemoveAt(active.Count - 1);
				}
				else
				{
					Tick(botCharacter, nowMs);
				}
			}
		}
	}

	private static (float x, float y)? PickStroll(BotCharacter bot, MapRuntime rt, BotWander w)
	{
		BotNavigationGraph.Point? point = null;
		BotNavigationGraph.Point point2 = new BotNavigationGraph.Point((int)bot.x, (int)bot.y);
		for (int i = 0; i < 6; i++)
		{
			BotNavigationGraph.Point? point3 = (w.banded ? picker.Pick(rt, point2, (int)w.bandLo, (int)w.bandHi) : picker.Pick(rt, point2));
			if (!point3.HasValue)
			{
				break;
			}
			point = point3;
			if ((float)Math.Abs(point3.Value.x - point2.x) >= 60f)
			{
				break;
			}
		}
		if (!point.HasValue)
		{
			return null;
		}
		return (point.Value.x, point.Value.y);
	}
}
