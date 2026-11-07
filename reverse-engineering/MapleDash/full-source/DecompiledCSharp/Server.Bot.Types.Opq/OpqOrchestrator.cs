using System;
using System.Collections.Generic;
using Server.Bot.Environment;
using Server.Field;

namespace Server.Bot.Types.Opq;

public static class OpqOrchestrator
{
	private const long TickPeriodMs = 1000L;

	public static readonly OpqSharedContext Context = new OpqSharedContext();

	private static readonly List<BotCharacter> registered = new List<BotCharacter>();

	private static readonly Dictionary<int, int> boxSortedIndex = new Dictionary<int, int>();

	private static BotTasks.Handle tickHandle;

	private static long tickGeneration;

	public static Func<int, int> LeaderMapOf;

	public static void Register(BotCharacter bot)
	{
		if (bot == null)
		{
			return;
		}
		for (int i = 0; i < registered.Count; i++)
		{
			if (registered[i] != null && registered[i].id == bot.id)
			{
				EnsureTick();
				return;
			}
		}
		registered.Add(bot);
		EnsureTick();
	}

	public static void Unregister(BotCharacter bot)
	{
		if (bot != null)
		{
			int botId = bot.id;
			registered.RemoveAll((BotCharacter b) => b == null || b.id == botId || BotRegistry.Get(b.id) != b);
			Context.PutCloudAssignment(botId, null);
			Context.PutBoxAssignment(botId, null);
			Context.PutPlatformAssignment(botId, null);
			Context.ClearTaskComplete(botId);
			if (registered.Count == 0)
			{
				StopTick();
			}
		}
	}

	public static void NoteLeaderFromBot(BotCharacter bot)
	{
		if (bot != null && PartyOf(bot.id) > 0 && !Context.IsPqActive)
		{
			Context.SetPqActive(value: true);
			if (Context.CurrentPhase == OpqPhase.Inactive)
			{
				Context.SetCurrentPhase(OpqPhase.Recruitment);
			}
		}
	}

	public static int? AssignCloudReactor(BotCharacter bot)
	{
		if (bot == null)
		{
			return null;
		}
		int id = bot.id;
		int? result = Context.MyCloudAssignment(id);
		if (result.HasValue)
		{
			MapReactor mapReactor = MapReactorService.Find(bot.mapId, result.Value);
			if (mapReactor != null && mapReactor.Hittable)
			{
				return result;
			}
			Context.PutCloudAssignment(id, null);
		}
		ICollection<int> cloudAssignmentValues = Context.CloudAssignmentValues;
		MapReactor mapReactor2 = null;
		double num = double.MaxValue;
		foreach (MapReactor item in MapReactorService.Get(bot.mapId))
		{
			if (item.id == 2002001 && item.Hittable && !cloudAssignmentValues.Contains(item.objectId))
			{
				double num2 = bot.x - (float)item.x;
				double num3 = bot.y - (float)item.y;
				double num4 = num2 * num2 + num3 * num3;
				if (num4 < num)
				{
					num = num4;
					mapReactor2 = item;
				}
			}
		}
		if (mapReactor2 == null)
		{
			return null;
		}
		Context.PutCloudAssignment(id, mapReactor2.objectId);
		return mapReactor2.objectId;
	}

	public static bool HasUnclaimedLiveCloudReactor(int mapId, int askingBotId)
	{
		ICollection<int> cloudAssignmentValues = Context.CloudAssignmentValues;
		int? num = Context.MyCloudAssignment(askingBotId);
		foreach (MapReactor item in MapReactorService.Get(mapId))
		{
			if (item.id == 2002001 && item.Hittable && (!cloudAssignmentValues.Contains(item.objectId) || (num.HasValue && num.Value == item.objectId)))
			{
				return true;
			}
		}
		return false;
	}

	public static int? AssignBoxReactor(BotCharacter bot)
	{
		if (bot == null)
		{
			return null;
		}
		int id = bot.id;
		BuildBoxSortedIndex(bot.mapId);
		int? result = Context.MyBoxAssignment(id);
		if (result.HasValue)
		{
			MapReactor mapReactor = MapReactorService.Find(bot.mapId, result.Value);
			if (mapReactor != null && mapReactor.Hittable)
			{
				return result;
			}
			Context.PutBoxAssignment(id, null);
		}
		ICollection<int> boxAssignmentValues = Context.BoxAssignmentValues;
		MapReactor mapReactor2 = null;
		double num = double.MaxValue;
		foreach (MapReactor item in MapReactorService.Get(bot.mapId))
		{
			if (IsBoxReactor(item.id) && item.Hittable && (item.state <= 0 || boxAssignmentValues.Contains(item.objectId)) && !boxAssignmentValues.Contains(item.objectId))
			{
				double num2 = bot.x - (float)item.x;
				double num3 = bot.y - (float)item.y;
				double num4 = num2 * num2 + num3 * num3;
				if (num4 < num)
				{
					num = num4;
					mapReactor2 = item;
				}
			}
		}
		if (mapReactor2 == null)
		{
			return null;
		}
		Context.PutBoxAssignment(id, mapReactor2.objectId);
		return mapReactor2.objectId;
	}

	public static bool HasUnclaimedLiveBoxReactor(int mapId, int askingBotId)
	{
		ICollection<int> boxAssignmentValues = Context.BoxAssignmentValues;
		foreach (MapReactor item in MapReactorService.Get(mapId))
		{
			if (IsBoxReactor(item.id) && item.Hittable && (item.state <= 0 || boxAssignmentValues.Contains(item.objectId)) && !boxAssignmentValues.Contains(item.objectId))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsBoxReactor(int reactorId)
	{
		if (reactorId >= 2002004)
		{
			return reactorId <= 2002010;
		}
		return false;
	}

	private static void BuildBoxSortedIndex(int mapId)
	{
		if (boxSortedIndex.Count > 0)
		{
			return;
		}
		List<MapReactor> list = new List<MapReactor>();
		foreach (MapReactor item in MapReactorService.Get(mapId))
		{
			if (IsBoxReactor(item.id))
			{
				list.Add(item);
			}
		}
		list.Sort((MapReactor a, MapReactor b) => b.x.CompareTo(a.x));
		for (int num = 0; num < list.Count; num++)
		{
			boxSortedIndex[list[num].objectId] = num;
		}
	}

	public static string GetBoxOrdinal(int reactorObjectId)
	{
		if (boxSortedIndex.TryGetValue(reactorObjectId, out var value) && value >= 0 && value < OpqConstants.Stage2BoxOrdinals.Length)
		{
			return OpqConstants.Stage2BoxOrdinals[value];
		}
		return "?";
	}

	public static int GetBoxItemId(int reactorObjectId)
	{
		if (boxSortedIndex.TryGetValue(reactorObjectId, out var value) && value >= 0 && value < OpqConstants.Stage2Items.Length)
		{
			return OpqConstants.Stage2Items[value];
		}
		return OpqConstants.Stage2Items[0];
	}

	public static bool IsChamberlainSpawned()
	{
		MapRuntime mapRuntime = MapService.Get(920010000);
		if (mapRuntime == null)
		{
			return false;
		}
		foreach (MapNpcPosition npc in mapRuntime.npcs)
		{
			if (npc.id == 2013001)
			{
				return true;
			}
		}
		return false;
	}

	public static bool FollowLeaderWarp(BotCharacter bot, int x, int y)
	{
		int num = LeaderMap(bot);
		if (num <= 0)
		{
			return false;
		}
		return WarpTo(bot, num, x, y);
	}

	public static bool WarpTo(BotCharacter bot, int mapId, int x, int y)
	{
		if (bot == null || mapId <= 0)
		{
			return false;
		}
		if (bot.mapId != mapId)
		{
			Func<int, MapRuntime> getMap = BotEnvironment.GetMap;
			try
			{
				if (((getMap != null) ? getMap(mapId) : MapService.Get(mapId)) == null)
				{
					return false;
				}
			}
			catch (ArgumentException)
			{
				return false;
			}
			BotRegistry.EnterMap(bot, mapId);
		}
		BotMovementService.Stop(bot);
		bot.x = x;
		bot.y = MapService.SnapToGround(mapId, x, y);
		return true;
	}

	public static void MirrorPhase(OpqPhase phase)
	{
		if (Context.CurrentPhase != phase)
		{
			Context.SetCurrentPhase(phase);
		}
	}

	public static void ResetForNewRun()
	{
		Context.Reset();
		Context.SetPqActive(value: true);
		Context.SetCurrentPhase(OpqPhase.Recruitment);
		boxSortedIndex.Clear();
	}

	public static void ShutdownRun()
	{
		Context.Reset();
		boxSortedIndex.Clear();
	}

	public static int LeaderMap(BotCharacter bot)
	{
		if (bot == null)
		{
			return -1;
		}
		return LeaderMapOf?.Invoke(bot.id) ?? (-1);
	}

	public static int PartyOf(int botId)
	{
		if (MapItemService.PartyOf == null)
		{
			return -1;
		}
		return MapItemService.PartyOf(botId);
	}

	private static void EnsureTick()
	{
		if (tickHandle == null || tickHandle.Cancelled || tickHandle.Completed)
		{
			tickGeneration++;
			long generation = tickGeneration;
			tickHandle = BotTasks.ScheduleFixedDelay(() =>
			{
				SafeTick(generation);
			}, 1000L, 1000L, "EnsureTick", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Types/Opq/OpqOrchestrator.cs");
		}
	}

	private static void StopTick()
	{
		tickGeneration++;
		tickHandle?.Cancel();
		tickHandle = null;
	}

	private static void SafeTick(long generation)
	{
		if (generation == tickGeneration)
		{
			Tick();
		}
	}

	private static void Tick()
	{
		Prune();
		if (registered.Count == 0)
		{
			StopTick();
		}
		else
		{
			if (!Context.IsPqActive)
			{
				return;
			}
			if (AllBotsLostParty())
			{
				ResetForNewRun();
				return;
			}
			switch (Context.CurrentPhase)
			{
			case OpqPhase.Stage1:
				if (!Context.IsStage1Complete && IsChamberlainSpawned())
				{
					Context.SetStage1Complete(value: true);
					ClearCompletionFlags();
				}
				break;
			case OpqPhase.Stage2:
				if (!Context.IsStage2Complete && AllRegisteredBotsComplete())
				{
					Context.SetStage2Complete(value: true);
					ClearCompletionFlags();
				}
				break;
			}
		}
	}

	private static void Prune()
	{
		for (int num = registered.Count - 1; num >= 0; num--)
		{
			BotCharacter botCharacter = registered[num];
			if (botCharacter == null || BotRegistry.Get(botCharacter.id) != botCharacter)
			{
				registered.RemoveAt(num);
			}
		}
	}

	private static bool AllBotsLostParty()
	{
		if (registered.Count == 0)
		{
			return false;
		}
		foreach (BotCharacter item in registered)
		{
			if (PartyOf(item.id) > 0)
			{
				return false;
			}
		}
		return true;
	}

	private static bool AllRegisteredBotsComplete()
	{
		if (registered.Count == 0)
		{
			return false;
		}
		foreach (BotCharacter item in registered)
		{
			if (!Context.IsMyTaskComplete(item.id))
			{
				return false;
			}
		}
		return true;
	}

	private static void ClearCompletionFlags()
	{
		foreach (BotCharacter item in registered)
		{
			Context.ClearTaskComplete(item.id);
		}
	}
}
