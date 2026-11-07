using System;
using System.Collections.Generic;
using Server.Bot.Environment;
using Server.Bot.Events;
using Server.Bot.Social;
using Server.Field;

namespace Server.Bot.Types;

public sealed class GachaBot : BotStateMachine
{
	public enum GachaBotState
	{
		Reset,
		SetPosition,
		RunRoulette,
		StandBy1,
		StandBy2,
		PickupItem,
		StandBy3,
		StandBy4,
		ProcessReward
	}

	private const int DropSprayLength = 2;

	private const int DropSprayFullWidth = 4;

	private const int ItemDropOffset = 30;

	internal const int GachaListSize = 10;

	internal const double ItemReplaceChance = 0.3;

	private const int PrizeItemId = 1082223;

	private const string DialoguePack = "GachaBotDialogue.yaml";

	private const string LevelUpRecordingFirst = "rightleft45";

	private const string LevelUpRecordingSecond = "leftright70";

	private const long LevelUpPauseMs = 1500L;

	private static readonly int[] MesoFillerAmounts = new int[13]
	{
		10, 10, 10, 10, 10, 69, 69, 69, 69, 420,
		420, 420, 1337
	};

	private readonly Random random;

	private readonly BotDialogue dialogue;

	private readonly GachaFillerPool pool;

	private int baseX;

	private int baseY;

	private bool scripted;

	private int generation;

	private BotTasks.Handle sequenceTask;

	public GachaBotState gachaBotState { get; private set; }

	public GachaBot(BotCharacter c, EventBus events = null, BotDialogue dialogue = null, Random random = null)
		: base(c)
	{
		this.dialogue = dialogue ?? BotTypeManager.Dialogue;
		this.random = random ?? new Random();
		pool = GachaFillerPool.Shared;
		botType = "GachaBot";
		Subscribe(events ?? BotTypeManager.Events, EventType.LEVEL_UP, EventType.SCROLLING);
	}

	public override void UpdateState()
	{
		base.UpdateState();
		if (CheckIfNotRunningOrPaused())
		{
			return;
		}
		DebugLog(chr.name + " GachaBotState: " + gachaBotState);
		if (!scripted)
		{
			switch (gachaBotState)
			{
			case GachaBotState.Reset:
				ResetGachaBotState();
				SetState(GachaBotState.SetPosition);
				break;
			case GachaBotState.SetPosition:
				SetPosition();
				SetState(GachaBotState.RunRoulette);
				break;
			case GachaBotState.RunRoulette:
				RunRoulette();
				SetState(GachaBotState.StandBy1);
				break;
			case GachaBotState.StandBy1:
				SetState(GachaBotState.StandBy2);
				break;
			case GachaBotState.StandBy2:
				SetState(GachaBotState.PickupItem);
				break;
			case GachaBotState.PickupItem:
				PickupItem();
				SetState(GachaBotState.StandBy3);
				break;
			case GachaBotState.StandBy3:
				ExecuteEventQueueGacha();
				break;
			case GachaBotState.StandBy4:
				SetState(GachaBotState.ProcessReward);
				break;
			case GachaBotState.ProcessReward:
				ProcessReward();
				SetState(GachaBotState.RunRoulette);
				break;
			default:
				DebugLog("Unexpected GachaBot state: " + gachaBotState);
				state = State.FINISHED;
				ResetGachaBotState();
				break;
			}
		}
	}

	private void SetState(GachaBotState next)
	{
		gachaBotState = next;
	}

	private bool Alive()
	{
		if (running && chr.online)
		{
			return BotStore.GetBotById(chr.id) == this;
		}
		return false;
	}

	private void ResetGachaBotState()
	{
		SetState(GachaBotState.Reset);
		scripted = false;
		sequenceTask?.Cancel();
		sequenceTask = null;
	}

	private void After(long ms, Action next)
	{
		scripted = true;
		sequenceTask?.Cancel();
		sequenceTask = BotTasks.Schedule(() =>
		{
			if (!Alive())
			{
				return;
			}
			try
			{
				next();
			}
			catch (Exception ex)
			{
				DebugLog(chr.name + " sequence error: " + ex.Message);
				scripted = false;
			}
		}, ms, "After", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Types/GachaBot.cs");
	}

	private void SetPosition()
	{
		baseX = (int)chr.x;
		baseY = (int)chr.y;
		output.Send(BotOutput.Kind.Bubble, chr, "Position set!");
	}

	private void RunRoulette()
	{
		if (CheckMainPlayersOnMap())
		{
			GachaPop(pool.CreateGachaListWithPrize(1082223, random));
		}
	}

	private void GachaPop(List<int> itemIds)
	{
		if (itemIds == null || itemIds.Count == 0)
		{
			return;
		}
		int mapId = chr.mapId;
		MapReactorService.Spawn(mapId, 2202004, (int)chr.x, (int)chr.y);
		MapReactor mapReactor = null;
		foreach (MapReactor item in MapReactorService.Get(mapId))
		{
			if (item.Hittable && ((float)item.x - chr.x) * ((float)item.x - chr.x) + ((float)item.y - chr.y) * ((float)item.y - chr.y) <= 625f)
			{
				mapReactor = item;
				break;
			}
		}
		if (mapReactor == null)
		{
			return;
		}
		for (int i = 0; i < 3; i++)
		{
			MapReactorService.ForceHit(mapId, mapReactor.objectId);
		}
		int centerX = mapReactor.x - ((itemIds.Count % 2 == 0) ? 12 : 0);
		int y = mapReactor.y;
		for (int j = 0; j < itemIds.Count; j++)
		{
			int num = itemIds[j];
			(int x, int y) tuple = BotHelpers.AdjustCenterPositionXAxis(centerX, y, j, 2, 4, 30);
			(centerX, _) = tuple;
			var (num2, num3) = tuple;
			if (num == 0)
			{
				MapItemService.CalcDropPos(mapId, num2, num3, mapReactor.x, mapReactor.y, out var outX, out var outY);
				MapItemService.SpawnMesoDrop(mapId, MesoFillerAmounts[random.Next(MesoFillerAmounts.Length)], outX, outY, mapReactor.objectId, chr.id, MapItemService.PartyOf?.Invoke(chr.id) ?? (-1), playerDrop: false, 2);
			}
			else
			{
				MapItemService.DropFromReactor(mapId, mapReactor.objectId, chr.id, BotLogic.GenerateCleanItem(num), num2, num3, mapReactor.x, mapReactor.y);
			}
		}
		MapReactorService.ForceHit(mapId, mapReactor.objectId);
	}

	private void PickupItem()
	{
		if (CheckMainPlayersOnMap())
		{
			output.Emote(chr, 3);
			DropCommands.BotLootOwnerItems(chr, (int)chr.x, (int)chr.y, 12000.0);
		}
	}

	private void ExecuteEventQueueGacha()
	{
		ProcessQueuedEvents();
		if (!HasQueuedEvents)
		{
			SetState(GachaBotState.StandBy4);
		}
	}

	private void ProcessReward()
	{
	}

	public override void HandleEvent(GameEvent ev)
	{
		if (ev != null)
		{
			switch (ev.type)
			{
			case EventType.GACHAPON_REWARD:
				HandleGachaponEvent(ev);
				break;
			case EventType.SCROLLING:
				HandleScrollingEvent(ev);
				break;
			case EventType.LEVEL_UP:
				HandleLevelUpEvent(ev);
				break;
			default:
				throw new InvalidOperationException("Unknown Event Type: " + ev.type);
			}
		}
	}

	private void HandleGachaponEvent(GameEvent ev)
	{
		output.Emote(chr, 2);
		output.Send(BotOutput.Kind.Bubble, chr, "Lucky " + ev.playerName + "!");
	}

	private void HandleScrollingEvent(GameEvent ev)
	{
		if (ev.pass == true)
		{
			output.Emote(chr, 3);
			output.Send(BotOutput.Kind.Bubble, chr, "Nice scroll " + ev.playerName + "!");
		}
		else
		{
			output.Emote(chr, 4);
			output.Send(BotOutput.Kind.Bubble, chr, "Ooof... unlucky D:");
		}
	}

	private void HandleLevelUpEvent(GameEvent ev)
	{
		int token = generation;
		BotTiming.CreateChain().StopUnless(() => Alive() && token == generation).Run(() =>
		{
			PlayRecording("rightleft45");
		})
			.Pause(1500L)
			.Run(() =>
			{
				output.Emote(chr, 2);
				output.Send(BotOutput.Kind.Bubble, chr, "Ayy Congrats " + ev.playerName + "!");
			})
			.Pause(1500L)
			.Run(() =>
			{
				PlayRecording("leftright70");
			})
			.Run(() =>
			{
				scripted = false;
			})
			.Start();
		scripted = true;
	}

	private void PlayRecording(string name)
	{
		List<BotMovementRecording.Packet> list = LoadMovementRecording(name);
		if (list == null || list.Count == 0)
		{
			DebugLog("[GachaBot] movement recording " + name + " missing for map " + chr.mapId + ", skipping");
		}
		else
		{
			BotMovementPlayback.Play(chr, list);
		}
	}

	private List<BotMovementRecording.Packet> LoadMovementRecording(string name)
	{
		try
		{
			byte[] array = BotResources.Load().ReadBytes(BotMovementRecording.ResourcePath(chr.mapId, name, "bin"));
			return (array == null) ? null : BotMovementRecording.ReadBinary(array);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public override void DisplayCommands(BotActor player)
	{
		if (player != null)
		{
			output.ShowOptions(player, new string[1] { chr.name });
		}
	}

	public override void StopScheduledTask()
	{
		generation++;
		BotMovementPlayback.Cancel(chr);
		sequenceTask?.Cancel();
		sequenceTask = null;
		scripted = false;
		base.StopScheduledTask();
	}
}
