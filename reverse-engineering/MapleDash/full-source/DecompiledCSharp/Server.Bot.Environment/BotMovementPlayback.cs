using System;
using System.Collections.Generic;
using System.IO;
using Server.Field;

namespace Server.Bot.Environment;

public sealed class BotMovementPlayback
{
	private static readonly Dictionary<int, BotMovementPlayback> active = new Dictionary<int, BotMovementPlayback>();

	public static Action<BotCharacter, byte[]> Broadcast;

	private readonly BotCharacter actor;

	private readonly int mapId;

	private readonly IReadOnlyList<BotMovementRecording.Packet> frames;

	private readonly bool offset;

	private readonly Action<bool> finished;

	private readonly Func<BotCharacter, bool> midCheck;

	private readonly BotNavigationGraph.Point? stopPoint;

	private BotTasks.Handle pending;

	private int index;

	private bool done;

	private bool interrupted;

	private BotMovementPlayback(BotCharacter actor, IReadOnlyList<BotMovementRecording.Packet> frames, bool offset, Action<bool> finished, Func<BotCharacter, bool> midCheck, BotNavigationGraph.Point? stopPoint)
	{
		this.actor = actor;
		mapId = actor.mapId;
		this.frames = frames;
		this.offset = offset;
		this.finished = finished;
		this.midCheck = midCheck;
		this.stopPoint = stopPoint;
	}

	public static BotMovementPlayback Play(BotCharacter actor, IReadOnlyList<BotMovementRecording.Packet> frames, bool offset = false, Action<bool> finished = null, Func<BotCharacter, bool> midCheck = null, BotNavigationGraph.Point? stopPoint = null)
	{
		Cancel(actor);
		BotMovementPlayback botMovementPlayback = new BotMovementPlayback(actor, frames, offset, finished, midCheck, stopPoint);
		active[actor.id] = botMovementPlayback;
		botMovementPlayback.Step();
		return botMovementPlayback;
	}

	public static void Cancel(BotCharacter actor)
	{
		if (actor != null && active.TryGetValue(actor.id, out var value))
		{
			value.Complete(stopped: true, notify: false);
		}
	}

	public void Interrupt()
	{
		interrupted = true;
	}

	private void Complete(bool stopped, bool notify = true)
	{
		if (!done)
		{
			done = true;
			pending?.Cancel();
			if (active.TryGetValue(actor.id, out var value) && value == this)
			{
				active.Remove(actor.id);
			}
			if (notify)
			{
				finished?.Invoke(stopped);
			}
		}
	}

	private void Step()
	{
		if (done)
		{
			return;
		}
		if (BotRegistry.Get(actor.id) != actor || actor.mapId != mapId)
		{
			Complete(stopped: true);
			return;
		}
		if (interrupted)
		{
			StopPacket();
			Complete(stopped: true);
			return;
		}
		if (index >= frames.Count)
		{
			Complete(stopped: false);
			return;
		}
		try
		{
			BotMovementRecording.Packet packet = frames[index];
			byte[] bytes = packet.data;
			if (offset)
			{
				MapRuntime mapRuntime = MapService.Get(mapId);
				bytes = BotMovementPacket.Offset(foothold: ((mapRuntime == null) ? null : MapGeometryCache.Get(mapRuntime).ground.Below((int)actor.x, (int)actor.y))?.id ?? 0, bytes: bytes, x: (int)actor.x, y: (int)actor.y);
			}
			byte[] arg = BotMovementPacket.Apply(bytes, actor);
			Broadcast?.Invoke(actor, arg);
			if (stopPoint.HasValue)
			{
				foreach (BotMovementRecording.Command item in BotMovementPacket.Deconstruct(packet.data))
				{
					long num = item.x - stopPoint.Value.x;
					long num2 = item.y - stopPoint.Value.y;
					if (num * num + num2 * num2 <= 6400)
					{
						StopPacket();
						Complete(stopped: false);
						return;
					}
				}
			}
			if (midCheck != null && index > 0 && index % 5 == 0 && midCheck(actor))
			{
				Complete(stopped: true);
				return;
			}
			index++;
			if (index == frames.Count)
			{
				Complete(stopped: false);
				return;
			}
			long num3 = frames[index].timestamp - packet.timestamp;
			if (num3 < 0)
			{
				throw new InvalidDataException("Movement timestamps out of order");
			}
			pending = BotTasks.Schedule(Step, num3, "Step", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Environment/BotMovementPlayback.cs");
		}
		catch
		{
			Complete(stopped: true);
			throw;
		}
	}

	private void StopPacket()
	{
		BotMovementRecording.Command command = new BotMovementRecording.Command(0, (short)actor.x, (short)actor.y, 0, 0, 0, (sbyte)(((actor.stance & 1) == 0) ? 4 : 5), 250);
		byte[] arg = BotMovementPacket.Apply(BotMovementPacket.Reconstruct(new BotMovementRecording.Command[2] { command, command }), actor);
		Broadcast?.Invoke(actor, arg);
	}
}
