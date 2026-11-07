using System;
using System.Collections.Generic;
using Server.Bot.Environment;
using Server.Field;

namespace Server.Bot.Types;

public sealed class AmbientMapBot : BotStateMachine
{
	private readonly DbStore db;

	private readonly Random random;

	private readonly Func<IEnumerable<BotActor>> players;

	private long nextActionAt;

	public AmbientMapBot(BotCharacter actor, DbStore db, Func<IEnumerable<BotActor>> players, Random random = null)
		: base(actor)
	{
		this.db = db;
		this.players = players;
		this.random = random ?? new Random();
		botType = "AmbientMapBot";
		ScheduleNext();
	}

	public override bool IsAvailableForAmbientActions()
	{
		return state != State.TRADING;
	}

	public override void CheckPrioritySpeed()
	{
		UpdateScheduleDelay(CheckMainPlayersOnMap() ? BotConfig.AmbientActionMinMs(db) : 2500);
	}

	public override void UpdateState()
	{
		base.UpdateState();
		if (!BotConfig.AmbientBehaviorEnabled(db) || !BotConfig.AmbientHasAnyActionEnabled(db))
		{
			StopScheduledTask();
		}
		else
		{
			if (CheckIfNotRunningOrPaused() || BotScheduler.Now() < nextActionAt)
			{
				return;
			}
			BotTypeManager.Flavor.MaybeExpress(this);
			int num = (BotConfig.AmbientMoveEnabled(db) ? 85 : 0);
			int num2 = (BotConfig.AmbientFacePlayerEnabled(db) ? 5 : 0);
			int num3 = (BotConfig.AmbientEmoteEnabled(db) ? 5 : 0);
			int num4 = (BotConfig.AmbientChatEnabled(db) ? 5 : 0);
			int num5 = num + num2 + num3 + num4;
			if (num5 > 0)
			{
				int num6 = random.Next(num5);
				if (num6 < num)
				{
					Stroll();
				}
				else if (num6 < num + num2)
				{
					FaceNearbyPlayerOrTurn();
				}
				else if (num6 < num + num2 + num3)
				{
					output.Emote(chr, random.Next(1, 23));
				}
				else
				{
					output.Speak(chr, marketFlavor.Chat());
				}
			}
			ScheduleNext();
		}
	}

	private void ScheduleNext()
	{
		long num = Math.Min(BotConfig.AmbientActionMinMs(db), 800L);
		long num2 = Math.Max(num, Math.Min(BotConfig.AmbientActionMaxMs(db), 1600L));
		nextActionAt = BotScheduler.Now() + num + (long)(random.NextDouble() * (double)(num2 - num + 1));
	}

	private void Stroll()
	{
		MapRuntime mapRuntime = MapService.Get(chr.mapId);
		if (mapRuntime == null || mapRuntime.footholds.Count == 0 || mapRuntime.maxX <= mapRuntime.minX)
		{
			return;
		}
		BotPoint? botPoint = null;
		for (int i = 0; i < 24; i++)
		{
			int num = random.Next((int)mapRuntime.minX, (int)mapRuntime.maxX + 1);
			float? num2 = mapRuntime.PointBelow(num, mapRuntime.minY);
			if (num2.HasValue)
			{
				botPoint = new BotPoint(num, (int)num2.Value);
				if (Math.Abs((float)num - chr.x) >= 180f)
				{
					break;
				}
			}
		}
		if (botPoint.HasValue && BotMovementService.Observed(chr.mapId))
		{
			BotMovementService.Enable(chr);
			BotMovementService.Move(chr, botPoint.Value.x, botPoint.Value.y);
		}
	}

	private void FaceNearbyPlayerOrTurn()
	{
		BotActor botActor = null;
		float num = float.MaxValue;
		if (players != null)
		{
			foreach (BotActor item in players())
			{
				if (item.mapId == chr.mapId && item.id != chr.id && item.id <= 20000 && item.id != 999)
				{
					float num2 = Math.Abs(item.x - chr.x);
					if (num2 < num)
					{
						num = num2;
						botActor = item;
					}
				}
			}
		}
		bool flag = ((botActor != null && num < 500f) ? (botActor.x < chr.x) : (chr.facing >= 0));
		if (flag != chr.facing < 0)
		{
			List<BotMovementRecording.Packet> frames = BotMovementRecording.ReadBinary(BotResources.Load().ReadBytes(BotMovementRecording.ResourcePath(0, flag ? "turnaroundtoleft" : "turnaroundtoright", "bin")));
			BotMovementPlayback.Play(chr, frames, offset: true);
		}
	}

	public override void StopScheduledTask()
	{
		BotTypeManager.Flavor.Forget(chr);
		BotMovementService.Disable(chr);
		base.StopScheduledTask();
	}
}
