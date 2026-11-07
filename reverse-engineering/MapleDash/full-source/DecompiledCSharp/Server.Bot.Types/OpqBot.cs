using System;
using System.Collections.Generic;
using Server.Bot.Environment;
using Server.Bot.Social;
using Server.Bot.Types.Opq;
using Server.Field;

namespace Server.Bot.Types;

public sealed class OpqBot : BotStateMachine
{
	public enum OpqState
	{
		Reset,
		Recruitment,
		InPartyIdle,
		Stage1Navigate,
		Stage1HitReactor,
		Stage1Loot,
		Stage1Return,
		Stage1DropItems,
		Stage1Wait,
		Stage1Transition,
		Stage1TransitionPt2,
		Stage2Navigate,
		Stage2HitBox,
		Stage2Loot,
		Stage2Return,
		Stage2DropItems,
		Stage2Wait,
		ExitDetect,
		ExitLobby,
		LoopCheck
	}

	private const long NavigateSettleMs = 1500L;

	private const int HitRange = 200;

	private readonly Random random;

	private long stageWaitStartTime;

	private long lastRecruitAt;

	private int reactorHitsThisTarget;

	private int cloudPiecesLooted;

	private int lootedRecordItemId = -1;

	private int announcedBoxOid;

	private long sleepUntil;

	private int phaseStep;

	private long navigateStartedAt;

	private BotTasks.Handle swingTask;

	private int swingToken;

	public OpqState opqState { get; private set; }

	public string lastTransitionReason { get; private set; } = "";

	public OpqBot(BotCharacter chr, Random random = null)
		: base(chr)
	{
		this.random = random ?? new Random();
		botType = "OPQBot";
		OpqOrchestrator.Register(chr);
	}

	public static bool IsInsidePq(int mapId)
	{
		if (mapId != 920010000 && mapId != 920010100 && mapId != 920010400)
		{
			return mapId == 920011200;
		}
		return true;
	}

	public override bool IsAvailableForAmbientActions()
	{
		return opqState == OpqState.Recruitment;
	}

	protected override long DefaultPeriodMs()
	{
		return 1000L;
	}

	public override void CheckPrioritySpeed()
	{
	}

	public override void StartScheduledTask(long initialDelayMs)
	{
		OpqOrchestrator.Register(chr);
		base.StartScheduledTask(initialDelayMs);
	}

	public override void StopScheduledTask()
	{
		CancelSwing();
		BotMovementService.Stop(chr);
		BotMovementPlayback.Cancel(chr);
		OpqOrchestrator.Unregister(chr);
		base.StopScheduledTask();
	}

	public void SetStateForDebug(OpqState state)
	{
		TransitionTo(state, "debug forced");
	}

	public override void UpdateState()
	{
		base.UpdateState();
		if (CheckIfNotRunningOrPaused() || chr == null)
		{
			return;
		}
		if (IsInsidePqState() && !IsInParty())
		{
			HandlePqAbandoned();
			return;
		}
		OpqState? opqState = DetectPhaseFromMap();
		if (opqState.HasValue && opqState.Value != this.opqState && !InSameStageFamily(opqState.Value, this.opqState))
		{
			TransitionTo(opqState.Value, "map-derived rehome (mapId=" + chr.mapId + ")");
		}
		switch (this.opqState)
		{
		case OpqState.Reset:
			ResetOpqState();
			TransitionTo(OpqState.Recruitment, "RESET 完成，开始招募");
			break;
		case OpqState.Recruitment:
			HandleRecruitment();
			break;
		case OpqState.InPartyIdle:
			HandleInPartyIdle();
			break;
		case OpqState.Stage1Navigate:
			HandleStage1Navigate();
			break;
		case OpqState.Stage1HitReactor:
			HandleStage1HitReactor();
			break;
		case OpqState.Stage1Loot:
			HandleStage1Loot();
			break;
		case OpqState.Stage1Return:
			HandleStage1Return();
			break;
		case OpqState.Stage1DropItems:
			HandleStage1DropItems();
			break;
		case OpqState.Stage1Wait:
			HandleStage1Wait();
			break;
		case OpqState.Stage1Transition:
			HandleStage1Transition();
			break;
		case OpqState.Stage1TransitionPt2:
			HandleStage1TransitionPart2();
			break;
		case OpqState.Stage2Navigate:
			HandleStage2Navigate();
			break;
		case OpqState.Stage2HitBox:
			HandleStage2HitBox();
			break;
		case OpqState.Stage2Loot:
			HandleStage2Loot();
			break;
		case OpqState.Stage2Return:
			HandleStage2Return();
			break;
		case OpqState.Stage2DropItems:
			HandleStage2DropItems();
			break;
		case OpqState.Stage2Wait:
			HandleStage2Wait();
			break;
		case OpqState.ExitDetect:
			HandleExitDetect();
			break;
		case OpqState.ExitLobby:
			HandleExitLobby();
			break;
		case OpqState.LoopCheck:
			HandleLoopCheck();
			break;
		default:
			CancelSwing();
			state = State.FINISHED;
			ResetOpqState();
			break;
		}
	}

	private void TransitionTo(OpqState next, string reason)
	{
		if (opqState != next)
		{
			opqState = next;
			lastTransitionReason = reason;
			phaseStep = 0;
			navigateStartedAt = 0L;
			CancelSwing();
		}
	}

	private void ResetOpqState()
	{
		opqState = OpqState.Reset;
		lastRecruitAt = 0L;
		ResetScratch();
	}

	private void ResetScratch()
	{
		stageWaitStartTime = 0L;
		reactorHitsThisTarget = 0;
		cloudPiecesLooted = 0;
		lootedRecordItemId = -1;
		announcedBoxOid = 0;
		navigateStartedAt = 0L;
		phaseStep = 0;
		sleepUntil = 0L;
	}

	private void HandleRecruitment()
	{
		BotPartyService parties = BotTypeManager.Parties;
		BotPartyService.Invitation invitation = parties?.Pending(chr.id);
		if (invitation != null)
		{
			parties.Answer(chr, invitation.party, accepted: true);
		}
		if (IsInParty())
		{
			OpqOrchestrator.NoteLeaderFromBot(chr);
			OpqOrchestrator.MirrorPhase(OpqPhase.InPartyIdle);
			TransitionTo(OpqState.InPartyIdle, "已经进队");
			return;
		}
		long num = BotScheduler.Now();
		if (num - lastRecruitAt >= 12000)
		{
			lastRecruitAt = num;
			output.Speak(chr, OpqRecruitMessages.Generate(chr, random));
			WanderToRandomSpot();
		}
	}

	private void HandleInPartyIdle()
	{
		if (!IsInParty())
		{
			TransitionTo(OpqState.Recruitment, "开副本前队伍就散了");
			return;
		}
		OpqOrchestrator.NoteLeaderFromBot(chr);
		if (LeaderMap() == 920010000)
		{
			if (phaseStep == 0)
			{
				ResetStageScratch();
				Sleep(3000L);
				phaseStep = 1;
			}
			else if (!StillSleeping() && OpqOrchestrator.FollowLeaderWarp(chr, 266, 143))
			{
				TransitionTo(OpqState.Stage1Navigate, "跟着队长进关卡 1");
			}
		}
	}

	private void HandleStage1Navigate()
	{
		OpqOrchestrator.MirrorPhase(OpqPhase.Stage1);
		if (StillSleeping())
		{
			return;
		}
		int? num = OpqOrchestrator.AssignCloudReactor(chr);
		if (!OpqOrchestrator.HasUnclaimedLiveCloudReactor(chr.mapId, chr.id))
		{
			OpqOrchestrator.Context.PutCloudAssignment(chr.id, null);
			TransitionTo(OpqState.Stage1Return, "没有剩余云朵 → 回交");
		}
		else
		{
			if (!num.HasValue)
			{
				return;
			}
			MapReactor mapReactor = MapReactorService.Find(chr.mapId, num.Value);
			if (mapReactor == null || !mapReactor.Hittable)
			{
				OpqOrchestrator.Context.PutCloudAssignment(chr.id, null);
			}
			else if (navigateStartedAt == 0L)
			{
				MoveTo(mapReactor.x, mapReactor.y);
				navigateStartedAt = BotScheduler.Now();
			}
			else if (BotScheduler.Now() - navigateStartedAt >= 1500)
			{
				if (Math.Abs(chr.x - (float)mapReactor.x) <= 200f)
				{
					reactorHitsThisTarget = 0;
					TransitionTo(OpqState.Stage1HitReactor, "已到云朵射程内 (oid=" + num.Value + ")");
				}
				else
				{
					MoveTo(mapReactor.x, mapReactor.y);
					navigateStartedAt = BotScheduler.Now();
				}
			}
		}
	}

	private void HandleStage1HitReactor()
	{
		if (swingTask == null)
		{
			reactorHitsThisTarget = 0;
			StartSwingChain(stage2: false);
		}
	}

	private void HandleStage1Loot()
	{
		if (StillSleeping())
		{
			return;
		}
		int? num = OpqOrchestrator.Context.MyCloudAssignment(chr.id);
		MapReactor mapReactor = (num.HasValue ? MapReactorService.Find(chr.mapId, num.Value) : null);
		List<MapItem> list = BotLogic.CheckForItemsOnFloor(chr.mapId, (int)chr.x, (int)chr.y, 8000.0, OpqConstants.Stage1Items);
		if (list.Count == 0 && mapReactor != null)
		{
			for (int i = 1; i <= 20; i++)
			{
				int y = mapReactor.y + i * 2500;
				List<MapItem> list2 = BotLogic.CheckForItemsOnFloor(chr.mapId, mapReactor.x, y, 8000.0, OpqConstants.Stage1Items);
				if (list2.Count > 0)
				{
					list = list2;
					break;
				}
			}
		}
		if (list.Count > 0)
		{
			DropCommands.LootItemListOnFloor(chr, list);
			cloudPiecesLooted++;
			output.Send(BotOutput.Kind.Bubble, chr, "Cloud Pieces: " + cloudPiecesLooted);
		}
		Sleep(800L);
		TransitionTo(OpqState.Stage1Navigate, "捡完一轮，找下一个云朵");
	}

	private void HandleStage1Return()
	{
		if (!StillSleeping())
		{
			if (phaseStep == 0)
			{
				MoveTo(497, 143);
				Sleep(1500L);
				phaseStep = 1;
			}
			else if (Math.Abs(chr.x - 497f) > 20f)
			{
				MoveTo(497, 143);
				Sleep(1500L);
			}
			else
			{
				TransitionTo(OpqState.Stage1DropItems, "到达回交点");
			}
		}
	}

	private void HandleStage1DropItems()
	{
		if (StillSleeping())
		{
			return;
		}
		if (cloudPiecesLooted > 0)
		{
			if (phaseStep == 0)
			{
				OutputSpeak("Dropping " + cloudPiecesLooted + " cloud" + ((cloudPiecesLooted == 1) ? "" : "s") + "!");
				Sleep(400L);
				phaseStep = 1;
				return;
			}
			DropCommands.BotThrowItemQty(chr, 4001063, cloudPiecesLooted, (int)chr.x, (int)chr.y);
		}
		OpqOrchestrator.Context.MarkTaskComplete(chr.id);
		StartStageWaitTimer();
		TransitionTo(OpqState.Stage1Wait, "云朵已投放，等关卡 1 通关");
	}

	private void HandleStage1Wait()
	{
		if (WaitTimedOut())
		{
			TransitionTo(OpqState.LoopCheck, "关卡 1 等待超时");
		}
		else if (OpqOrchestrator.Context.IsStage1Complete && OpqOrchestrator.IsChamberlainSpawned())
		{
			if (phaseStep == 0)
			{
				OpqOrchestrator.FollowLeaderWarp(chr, 165, -1270);
				Sleep(1000L);
				phaseStep = 1;
			}
			else if (!StillSleeping())
			{
				MoveToPortal(4);
				TransitionTo(OpqState.Stage1Transition, "关卡 1 通关标志已置位");
			}
		}
	}

	private void HandleStage1Transition()
	{
		if (LeaderMap() == 920010100)
		{
			if (phaseStep == 0)
			{
				OpqOrchestrator.FollowLeaderWarp(chr, -260, -32);
				Sleep(1000L);
				phaseStep = 1;
			}
			else if (!StillSleeping())
			{
				MoveTo(159, -32);
				TransitionTo(OpqState.Stage1TransitionPt2, "在中转塔等队长进关卡 2");
			}
		}
	}

	private void HandleStage1TransitionPart2()
	{
		if (LeaderMap() == 920010400)
		{
			if (phaseStep == 0)
			{
				OpqOrchestrator.FollowLeaderWarp(chr, -113, -321);
				Sleep(1000L);
				phaseStep = 1;
			}
			else if (!StillSleeping())
			{
				TransitionTo(OpqState.Stage2Navigate, "已进关卡 2");
			}
		}
	}

	private void HandleStage2Navigate()
	{
		OpqOrchestrator.MirrorPhase(OpqPhase.Stage2);
		if (StillSleeping())
		{
			return;
		}
		if (LeaderLeftStage2())
		{
			OpqOrchestrator.Context.PutBoxAssignment(chr.id, null);
			FollowLeaderOut();
			return;
		}
		int? num = OpqOrchestrator.AssignBoxReactor(chr);
		if (!OpqOrchestrator.HasUnclaimedLiveBoxReactor(chr.mapId, chr.id) && !num.HasValue)
		{
			if (lootedRecordItemId > 0)
			{
				TransitionTo(OpqState.Stage2Return, "没箱子了，先回去把唱片扔了");
				return;
			}
			OpqOrchestrator.Context.MarkTaskComplete(chr.id);
			StartStageWaitTimer();
			TransitionTo(OpqState.Stage2Wait, "箱子清完，等关卡 2 通关");
		}
		else
		{
			if (!num.HasValue)
			{
				return;
			}
			MapReactor mapReactor = MapReactorService.Find(chr.mapId, num.Value);
			if (mapReactor == null || !mapReactor.Hittable)
			{
				OpqOrchestrator.Context.PutBoxAssignment(chr.id, null);
				return;
			}
			if (announcedBoxOid != mapReactor.objectId)
			{
				announcedBoxOid = mapReactor.objectId;
				OutputSpeak("I'll get the " + OpqOrchestrator.GetBoxOrdinal(mapReactor.objectId) + " box!");
			}
			if (navigateStartedAt == 0L)
			{
				MoveTo(mapReactor.x, mapReactor.y);
				navigateStartedAt = BotScheduler.Now();
			}
			else if (BotScheduler.Now() - navigateStartedAt >= 1500)
			{
				if (Math.Abs(chr.x - (float)mapReactor.x) <= 200f)
				{
					reactorHitsThisTarget = 0;
					TransitionTo(OpqState.Stage2HitBox, "已到箱子射程内 (oid=" + num.Value + ")");
				}
				else
				{
					MoveTo(mapReactor.x, mapReactor.y);
					navigateStartedAt = BotScheduler.Now();
				}
			}
		}
	}

	private void HandleStage2HitBox()
	{
		if (swingTask == null)
		{
			int? num = OpqOrchestrator.Context.MyBoxAssignment(chr.id);
			MapReactor mapReactor = (num.HasValue ? MapReactorService.Find(chr.mapId, num.Value) : null);
			if (mapReactor != null)
			{
				chr.facing = (((float)mapReactor.x >= chr.x) ? 1 : (-1));
			}
			reactorHitsThisTarget = 0;
			StartSwingChain(stage2: true);
		}
	}

	private void HandleStage2Loot()
	{
		if (!StillSleeping())
		{
			int? num = OpqOrchestrator.Context.MyBoxAssignment(chr.id);
			MapReactor mapReactor = (num.HasValue ? MapReactorService.Find(chr.mapId, num.Value) : null);
			int x = mapReactor?.x ?? ((int)chr.x);
			int y = mapReactor?.y ?? ((int)chr.y);
			List<MapItem> list = BotLogic.CheckForItemsOnFloor(chr.mapId, x, y, 8000.0, OpqConstants.Stage2Items);
			if (list.Count > 0)
			{
				DropCommands.LootItemListOnFloor(chr, list);
			}
			if (lootedRecordItemId > 0)
			{
				output.Send(BotOutput.Kind.Bubble, chr, "Got a record!");
			}
			Sleep(800L);
			OpqOrchestrator.Context.PutBoxAssignment(chr.id, null);
			TransitionTo(OpqState.Stage2Return, "捡完唱片，回音乐盒");
		}
	}

	private void HandleStage2Return()
	{
		if (!StillSleeping())
		{
			if (phaseStep == 0)
			{
				MoveTo(-1588, -127);
				Sleep(1500L);
				phaseStep = 1;
			}
			else if (Math.Abs(chr.x - -1588f) > 20f)
			{
				MoveTo(-1588, -127);
				Sleep(1500L);
			}
			else
			{
				TransitionTo(OpqState.Stage2DropItems, "到达音乐盒投放点");
			}
		}
	}

	private void HandleStage2DropItems()
	{
		if (!StillSleeping())
		{
			if (lootedRecordItemId > 0 && phaseStep == 0)
			{
				OutputSpeak("Dropping my record!");
				Sleep(400L);
				phaseStep = 1;
			}
			else if (lootedRecordItemId > 0)
			{
				DropCommands.BotThrowItem(chr, lootedRecordItemId, (int)chr.x, (int)chr.y, out var _);
				lootedRecordItemId = -1;
				Sleep(600L);
				phaseStep = 2;
			}
			else if (phaseStep < 2)
			{
				Sleep(600L);
				phaseStep = 2;
			}
			else if (LeaderLeftStage2())
			{
				FollowLeaderOut();
			}
			else if (OpqOrchestrator.HasUnclaimedLiveBoxReactor(chr.mapId, chr.id))
			{
				TransitionTo(OpqState.Stage2Navigate, "还有箱子，再来一个");
			}
			else
			{
				OpqOrchestrator.Context.MarkTaskComplete(chr.id);
				StartStageWaitTimer();
				TransitionTo(OpqState.Stage2Wait, "箱子全破，等关卡 2 通关");
			}
		}
	}

	private void HandleStage2Wait()
	{
		int num = LeaderMap();
		if (num == 920011200 || num == 200080101)
		{
			FollowLeaderOut();
		}
		else if (OpqOrchestrator.Context.IsStage2Complete)
		{
			TransitionTo(OpqState.ExitDetect, "关卡 2 通关标志已置位");
		}
		else if (WaitTimedOut())
		{
			TransitionTo(OpqState.LoopCheck, "关卡 2 等待超时");
		}
	}

	private void HandleExitDetect()
	{
		OpqOrchestrator.MirrorPhase(OpqPhase.Exit);
		if (chr.mapId == 920011200)
		{
			TransitionTo(OpqState.ExitLobby, "已到出口大厅");
			return;
		}
		switch (LeaderMap())
		{
		case 920011200:
			if (phaseStep == 0)
			{
				OpqOrchestrator.FollowLeaderWarp(chr, -161, 323);
				Sleep(1000L);
				phaseStep = 1;
			}
			else if (!StillSleeping())
			{
				TransitionTo(OpqState.ExitLobby, "跟着队长去出口大厅");
			}
			break;
		case 200080101:
			if (phaseStep == 0)
			{
				OpqOrchestrator.FollowLeaderWarp(chr, -233, 174);
				Sleep(1000L);
				phaseStep = 1;
			}
			else if (!StillSleeping())
			{
				TransitionTo(OpqState.LoopCheck, "跟着队长直接回招募大厅");
			}
			break;
		}
	}

	private void HandleExitLobby()
	{
		if (!StillSleeping())
		{
			int num = LeaderMap();
			if (phaseStep == 0)
			{
				Sleep((num == 200080101) ? 300 : 2000);
				phaseStep = 1;
			}
			else if (!OpqOrchestrator.WarpTo(chr, 200080101, -233, 174))
			{
				phaseStep = 0;
			}
			else
			{
				WanderToRandomSpot();
				TransitionTo(OpqState.LoopCheck, "已回到招募大厅");
			}
		}
	}

	private void HandleLoopCheck()
	{
		CancelSwing();
		OpqOrchestrator.Context.ClearTaskComplete(chr.id);
		OpqOrchestrator.Context.PutCloudAssignment(chr.id, null);
		OpqOrchestrator.Context.PutBoxAssignment(chr.id, null);
		OpqOrchestrator.Context.ClearPlatformAssignment(chr.id);
		ResetScratch();
		if (IsInParty())
		{
			TransitionTo(OpqState.InPartyIdle, "还在队伍里，准备下一轮");
		}
		else
		{
			TransitionTo(OpqState.Recruitment, "没队伍了，回大厅喊话");
		}
	}

	private void HandlePqAbandoned()
	{
		if (!StillSleeping())
		{
			int id = chr.id;
			OpqOrchestrator.Context.PutCloudAssignment(id, null);
			OpqOrchestrator.Context.PutBoxAssignment(id, null);
			OpqOrchestrator.Context.ClearPlatformAssignment(id);
			OpqOrchestrator.Context.ClearTaskComplete(id);
			if (phaseStep == 0)
			{
				OpqOrchestrator.WarpTo(chr, 920011200, -161, 323);
				Sleep(1500L);
				phaseStep = 1;
			}
			else
			{
				OpqOrchestrator.WarpTo(chr, 200080101, -233, 174);
				ResetOpqState();
				TransitionTo(OpqState.Recruitment, "队伍丢了，PQ 作废，已回大厅");
			}
		}
	}

	private OpqState? DetectPhaseFromMap()
	{
		return chr.mapId switch
		{
			200080101 => (OpqState?)((!IsInParty()) ? OpqState.Recruitment : OpqState.InPartyIdle), 
			920010000 => OpqState.Stage1Navigate, 
			920010100 => OpqState.Stage1Transition, 
			920010400 => OpqState.Stage2Navigate, 
			920011200 => OpqState.ExitDetect, 
			_ => null, 
		};
	}

	private static bool InSameStageFamily(OpqState a, OpqState b)
	{
		string text = Family(a);
		string text2 = Family(b);
		if (text != null)
		{
			return text == text2;
		}
		return false;
	}

	private static string Family(OpqState s)
	{
		switch (s)
		{
		case OpqState.Stage1Navigate:
		case OpqState.Stage1HitReactor:
		case OpqState.Stage1Loot:
		case OpqState.Stage1Return:
		case OpqState.Stage1DropItems:
		case OpqState.Stage1Wait:
		case OpqState.Stage1Transition:
		case OpqState.Stage1TransitionPt2:
			return "S1";
		case OpqState.Stage2Navigate:
		case OpqState.Stage2HitBox:
		case OpqState.Stage2Loot:
		case OpqState.Stage2Return:
		case OpqState.Stage2DropItems:
		case OpqState.Stage2Wait:
			return "S2";
		case OpqState.ExitDetect:
		case OpqState.ExitLobby:
			return "EX";
		case OpqState.Recruitment:
		case OpqState.InPartyIdle:
			return "LOBBY";
		default:
			return null;
		}
	}

	private bool IsInsidePqState()
	{
		OpqState opqState = this.opqState;
		if ((uint)opqState <= 2u || opqState == OpqState.LoopCheck)
		{
			return false;
		}
		return true;
	}

	private bool IsInParty()
	{
		return OpqOrchestrator.PartyOf(chr.id) > 0;
	}

	private int LeaderMap()
	{
		return OpqOrchestrator.LeaderMap(chr);
	}

	private bool LeaderLeftStage2()
	{
		int num = LeaderMap();
		if (num != 920011200)
		{
			return num == 200080101;
		}
		return true;
	}

	private void FollowLeaderOut()
	{
		if (phaseStep == 0)
		{
			Sleep(1000L);
			phaseStep = 1;
		}
		else if (!StillSleeping())
		{
			if (LeaderMap() == 920011200)
			{
				OpqOrchestrator.FollowLeaderWarp(chr, -161, 323);
				TransitionTo(OpqState.ExitLobby, "队长已经离开关卡 2，跟去出口大厅");
			}
			else
			{
				OpqOrchestrator.FollowLeaderWarp(chr, -233, 174);
				TransitionTo(OpqState.LoopCheck, "队长已经在招募大厅，跳过出口");
			}
		}
	}

	private void ResetStageScratch()
	{
		cloudPiecesLooted = 0;
		lootedRecordItemId = -1;
		reactorHitsThisTarget = 0;
	}

	private void StartStageWaitTimer()
	{
		stageWaitStartTime = BotScheduler.Now();
	}

	private bool WaitTimedOut()
	{
		if (stageWaitStartTime > 0)
		{
			return BotScheduler.Now() - stageWaitStartTime > 120000;
		}
		return false;
	}

	private void CancelSwing()
	{
		swingToken++;
		swingTask?.Cancel();
		swingTask = null;
	}

	private void StartSwingChain(bool stage2)
	{
		int token = ++swingToken;
		swingTask = BotTasks.Schedule(() =>
		{
			SwingStep(token, stage2);
		}, 0L, "StartSwingChain", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Types/OpqBot.cs");
	}

	private void SwingStep(int token, bool stage2)
	{
		if (token != swingToken)
		{
			return;
		}
		if (chr == null || CheckIfNotRunningOrPaused())
		{
			swingTask = null;
			return;
		}
		OpqSharedContext context = OpqOrchestrator.Context;
		int? num = (stage2 ? context.MyBoxAssignment(chr.id) : context.MyCloudAssignment(chr.id));
		if (!num.HasValue)
		{
			swingTask = null;
			TransitionTo(stage2 ? OpqState.Stage2Navigate : OpqState.Stage1Navigate, "HIT 状态下丢了分配");
			return;
		}
		MapReactor mapReactor = MapReactorService.Find(chr.mapId, num.Value);
		if (mapReactor == null || !mapReactor.Hittable)
		{
			swingTask = null;
			TransitionTo(stage2 ? OpqState.Stage2Loot : OpqState.Stage1Loot, "到场时反应堆已被打破");
			return;
		}
		BotAttack.BasicSwing(chr, random, output);
		int num2 = -1;
		List<InventoryEntry> list = new List<InventoryEntry>();
		if (stage2)
		{
			num2 = OpqOrchestrator.GetBoxItemId(num.Value);
			list.Add(BotLogic.GenerateCleanItem(num2));
		}
		else
		{
			list.Add(BotLogic.GenerateCleanItem(4001063));
		}
		MapReactor mapReactor2 = MapReactorService.Hit(chr.mapId, num.Value, list, chr.id, OpqOrchestrator.PartyOf(chr.id));
		reactorHitsThisTarget++;
		int num3;
		if (mapReactor2 != null)
		{
			num3 = ((!mapReactor2.Hittable) ? 1 : 0);
			if (num3 == 0)
			{
				goto IL_019f;
			}
		}
		else
		{
			num3 = 1;
		}
		if (num2 > 0)
		{
			lootedRecordItemId = num2;
		}
		goto IL_019f;
		IL_019f:
		if (num3 != 0 || reactorHitsThisTarget >= 4)
		{
			swingTask = null;
			TransitionTo(stage2 ? OpqState.Stage2Loot : OpqState.Stage1Loot, "反应堆已破 (hits=" + reactorHitsThisTarget + ")");
		}
		else
		{
			swingTask = BotTasks.Schedule(() =>
			{
				SwingStep(token, stage2);
			}, 700L, "SwingStep", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Types/OpqBot.cs");
		}
	}

	private void MoveTo(int x, int y)
	{
		int y2 = (int)MapService.SnapToGround(chr.mapId, x, y);
		BotMovementService.Move(chr, x, y2);
	}

	private bool MoveToPortal(int portalId)
	{
		MapRuntime mapRuntime = MapService.Get(chr.mapId);
		if (mapRuntime == null)
		{
			return false;
		}
		MapPortal mapPortal = null;
		foreach (MapPortal portal in mapRuntime.portals)
		{
			if (portal.id == portalId)
			{
				mapPortal = portal;
				break;
			}
		}
		if (mapPortal == null)
		{
			return false;
		}
		MoveTo(mapPortal.x, mapPortal.y);
		return true;
	}

	private void OutputSpeak(string text)
	{
		output.Speak(chr, text);
	}

	private void Sleep(long ms)
	{
		sleepUntil = BotScheduler.Now() + ms;
	}

	private bool StillSleeping()
	{
		return BotScheduler.Now() < sleepUntil;
	}

	private void WanderToRandomSpot()
	{
		MapRuntime mapRuntime = MapService.Get(chr.mapId);
		if (mapRuntime != null)
		{
			int num = mapRuntime.left + 80;
			int num2 = mapRuntime.right - 80;
			if (num2 > num)
			{
				int num3 = num + random.Next(num2 - num);
				int y = (int)MapService.SnapToGround(chr.mapId, num3, chr.y);
				BotMovementService.Move(chr, num3, y);
			}
		}
	}

	public override void DisplayCommands(BotActor player)
	{
		output.ShowOptions(player, new string[1] { chr.name });
	}
}
