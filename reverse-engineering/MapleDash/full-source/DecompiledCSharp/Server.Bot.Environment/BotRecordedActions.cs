using System;
using System.Collections.Generic;
using Server.Field;

namespace Server.Bot.Environment;

public static class BotRecordedActions
{
	private sealed class Walk
	{
		public BotCharacter actor;

		public int mapId;

		public int targetX;

		public int remaining;

		public bool left;

		public Action completed;

		public BotTasks.Handle task;
	}

	private static readonly Dictionary<int, Walk> walks = new Dictionary<int, Walk>();

	public static bool IsMoving(BotCharacter actor)
	{
		if (actor != null)
		{
			return walks.ContainsKey(actor.id);
		}
		return false;
	}

	public static void Cancel(BotCharacter actor)
	{
		if (actor != null && walks.TryGetValue(actor.id, out var value))
		{
			value.task?.Cancel();
			walks.Remove(actor.id);
		}
	}

	private static void Send(BotCharacter actor, params BotMovementRecording.Command[] commands)
	{
		byte[] arg = BotMovementPacket.Apply(BotMovementPacket.Reconstruct(commands), actor);
		BotMovementPlayback.Broadcast?.Invoke(actor, arg);
	}

	private static short Floor(BotCharacter actor, int x, int y)
	{
		MapRuntime mapRuntime = MapService.Get(actor.mapId);
		return (short)(((mapRuntime == null) ? null : MapGeometryCache.Get(mapRuntime).ground.Below(x, y))?.id ?? 0);
	}

	private static BotMovementRecording.Command Normal(int x, int y, int vx, int vy, int fh, int stance, int duration)
	{
		return new BotMovementRecording.Command(0, (short)x, (short)y, (short)vx, (short)vy, (short)fh, (sbyte)stance, (short)duration);
	}

	public static void Turn(BotCharacter actor, bool left)
	{
		if ((actor.stance == 1 || actor.stance == 3 || actor.stance == 5) == left)
		{
			return;
		}
		try
		{
			byte[] bytes = BotResources.Load().ReadBytes(BotMovementRecording.ResourcePath(0, left ? "turnaroundtoleft" : "turnaroundtoright", "bin"));
			BotMovementPlayback.Play(actor, BotMovementRecording.ReadBinary(bytes), offset: true);
		}
		catch (Exception)
		{
		}
	}

	public static void Jump(BotCharacter actor)
	{
		int x = (int)actor.x;
		int num = (int)actor.y;
		int num2 = actor.stance & 1;
		short fh = Floor(actor, x, num);
		Send(actor, Normal(x, num - 80, 0, 300, fh, 2 + num2, 180), Normal(x, num, 0, 0, fh, 4 + num2, 180));
	}

	public static bool WalkAlongX(BotCharacter actor, int destX, int maxSteps, Action completed)
	{
		if (actor == null || maxSteps <= 0 || MapService.Get(actor.mapId) == null || IsMoving(actor))
		{
			return false;
		}
		int num = (int)actor.x;
		int num2 = (int)actor.y;
		int num3 = (int)MapService.SnapToGround(actor.mapId, num, num2);
		if (Math.Abs(num3 - num2) > 6)
		{
			Send(actor, Normal(num, num3, 0, 0, Floor(actor, num, num2), actor.stance, 200));
		}
		Walk walk = new Walk
		{
			actor = actor,
			mapId = actor.mapId,
			targetX = destX,
			remaining = maxSteps,
			left = ((float)destX < actor.x),
			completed = completed
		};
		walks.Add(actor.id, walk);
		Step(walk);
		return true;
	}

	private static void Step(Walk walk)
	{
		BotCharacter actor = walk.actor;
		if (BotRegistry.Get(actor.id) != actor || actor.mapId != walk.mapId)
		{
			Cancel(actor);
			return;
		}
		if (actor.trade != null)
		{
			Complete(walk, stop: false);
			return;
		}
		if (walk.remaining-- <= 0 || Math.Abs(actor.x - (float)walk.targetX) <= 20f)
		{
			Complete(walk, stop: true);
			return;
		}
		int num = (int)actor.x + (((float)walk.targetX >= actor.x) ? 10 : (-10));
		MapRuntime mapRuntime = MapService.Get(actor.mapId);
		if (mapRuntime == null)
		{
			Complete(walk, stop: false);
			return;
		}
		MapGeometry mapGeometry = MapGeometryCache.Get(mapRuntime);
		FootholdSegment footholdSegment = mapGeometry.ground.Ground((int)actor.x, (int)actor.y);
		BotWalkRegions.Sample? sample = mapGeometry.regions.GroundSample(footholdSegment, num, (int)actor.y);
		if (!sample.HasValue || sample.Value.point.x != num || Math.Abs((float)BotGroundIndex.YAt(footholdSegment, (int)actor.x) - actor.y) > 26f)
		{
			Complete(walk, stop: true);
			return;
		}
		int y = sample.Value.point.y - 1;
		Send(actor, Normal(num, y, walk.left ? (-100) : 100, 0, Floor(actor, num, y), walk.left ? 3 : 2, 160));
		walk.task = BotTasks.Schedule(() =>
		{
			Step(walk);
		}, 160L, "Step", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Environment/BotRecordedActions.cs");
	}

	private static void Complete(Walk walk, bool stop)
	{
		BotCharacter actor = walk.actor;
		Cancel(actor);
		if (stop)
		{
			BotMovementRecording.Command command = Normal((int)actor.x, (int)actor.y, 0, 0, 0, 4 + (actor.stance & 1), 250);
			Send(actor, command, command);
		}
		walk.completed?.Invoke();
	}
}
