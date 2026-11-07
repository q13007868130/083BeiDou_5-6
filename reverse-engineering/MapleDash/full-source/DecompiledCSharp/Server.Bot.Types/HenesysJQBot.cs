using System;
using System.Collections.Generic;
using Server.Bot.Environment;
using Server.Bot.Social;
using Server.Field;

namespace Server.Bot.Types;

public sealed class HenesysJQBot : BotStateMachine
{
	public enum JQState
	{
		Reset,
		NavigateToJQ,
		AttemptJQ,
		Recover,
		Rest,
		NavigateToExit,
		Convert
	}

	private const int PetParkMap = 100000202;

	private const int HenesysParkMap = 100000200;

	private const int ExitPortalId = 5;

	private const int ExitDestPortalId = 13;

	private const int JqStartX = -1005;

	private const int JqStartY = 274;

	private const int NearStartThresholdX = 60;

	private const int NearStartThresholdY = 20;

	private static readonly string[][] TierRecordings = new string[7][]
	{
		new string[2] { "jq1fail", "jq1afail" },
		new string[2] { "jq2fail", "jq2afail" },
		new string[2] { "jq3fail", "jq3afail" },
		new string[2] { "jq4fail", "jq4afail" },
		new string[2] { "jq5fail", "jq5afail" },
		new string[6] { "jq6fail", "jq6afail", "jq6bfail", "jq6cfail", "jq6dfail", "jq6efail" },
		new string[1] { "jq7top" }
	};

	private static readonly int[] TierWeights = new int[7] { 20, 18, 16, 14, 12, 8, 4 };

	private static readonly int[] ExperiencedTierWeights = new int[7] { 0, 0, 0, 0, 15, 25, 60 };

	private const double ContinueJqChance = 0.75;

	private const string DialoguePack = "JQBotDialogue.yaml";

	private const string DialogueType = "JQBot";

	private readonly Random random;

	private readonly BotDialogue dialogue;

	private readonly List<BotTasks.Handle> delayed = new List<BotTasks.Handle>();

	private long sleepUntil;

	private long navigateStartedAt;

	private int restPhase;

	private int exitPhase;

	private bool attempting;

	private bool navigateArrived;

	private bool converted;

	private int highestCompletedTier;

	private bool hasReachedTop;

	private bool lastAttemptSuccess;

	public JQState jqState { get; private set; }

	public HenesysJQBot(BotCharacter actor, BotDialogue dialogue = null, Random random = null)
		: base(actor)
	{
		this.dialogue = dialogue ?? BotTypeManager.Dialogue;
		this.random = random ?? new Random();
		botType = "JQBot";
	}

	public override void UpdateState()
	{
		if (attempting)
		{
			return;
		}
		base.UpdateState();
		if (!CheckIfNotRunningOrPaused() && chr != null && !converted && BotScheduler.Now() >= sleepUntil)
		{
			switch (jqState)
			{
			case JQState.Reset:
				Sleep(2000 + random.Next(15000));
				jqState = JQState.NavigateToJQ;
				break;
			case JQState.NavigateToJQ:
				NavigateToJqStart();
				break;
			case JQState.AttemptJQ:
				AttemptJq();
				break;
			case JQState.Recover:
				Recover();
				break;
			case JQState.Rest:
				RestAndDecide();
				break;
			case JQState.NavigateToExit:
				NavigateToExit();
				break;
			case JQState.Convert:
				ConvertToHenesysBot();
				break;
			}
		}
	}

	private void NavigateToJqStart()
	{
		if (NearJqStart())
		{
			jqState = JQState.AttemptJQ;
		}
		else if (navigateStartedAt == 0L)
		{
			navigateStartedAt = BotScheduler.Now();
			BotMovementService.Move(chr, -1005, 274, () =>
			{
				navigateArrived = true;
			});
		}
		else if (navigateArrived || !BotMovementService.IsMoving(chr))
		{
			navigateStartedAt = 0L;
			navigateArrived = false;
			jqState = JQState.AttemptJQ;
		}
	}

	private void AttemptJq()
	{
		int tier = RollNextTier();
		string[] array = TierRecordings[tier - 1];
		string recording = array[random.Next(array.Length)];
		lastAttemptSuccess = tier == 7;
		if (OneIn(3))
		{
			Delay(500 + random.Next(2000), () =>
			{
				ChatLine("AttemptStart");
			});
		}
		if (OneIn(3))
		{
			ScheduleMidJqChat();
		}
		attempting = true;
		jqState = JQState.Recover;
		Delay(200 + random.Next(500), () =>
		{
			PlayRecording(recording, tier);
		});
	}

	private void PlayRecording(string recording, int tier)
	{
		bool flag = false;
		try
		{
			byte[] array = BotResources.Load().ReadBytes(BotMovementRecording.ResourcePath(100000202, recording, "bin"));
			if (array == null)
			{
				BotPerf.Record("JQBot.MissingRecording", 0.0);
				return;
			}
			BotMovementPlayback.Play(chr, BotMovementRecording.ReadBinary(array), offset: false, (bool stopped) =>
			{
				OnAttemptFinished(tier);
			});
			flag = true;
		}
		catch (Exception)
		{
			BotPerf.Record("JQBot.PlaybackError", 0.0);
		}
		finally
		{
			if (!flag && attempting)
			{
				OnAttemptFinished(tier);
			}
		}
	}

	private void OnAttemptFinished(int tier)
	{
		attempting = false;
		highestCompletedTier = tier;
		Sleep(500 + random.Next(1000));
	}

	private void Recover()
	{
		if (lastAttemptSuccess)
		{
			hasReachedTop = true;
			ChatLine("SuccessReaction");
			DoEmote();
			Sleep(500 + random.Next(1000));
			BotMovementService.Move(chr, -1810 + random.Next(693), 274);
			jqState = JQState.Rest;
			return;
		}
		if (!NearJqStart())
		{
			ChatLine("FailReaction");
			if (OneIn(3))
			{
				DoEmote();
			}
			Sleep(1000 + random.Next(2000));
			jqState = JQState.Rest;
			return;
		}
		if (OneIn(3))
		{
			ChatLine("FailReaction");
		}
		if (OneIn(5))
		{
			DoEmote();
		}
		jqState = JQState.AttemptJQ;
	}

	private void RestAndDecide()
	{
		if (restPhase == 0)
		{
			restPhase = 1;
			if (OneIn(3))
			{
				ChatLine("RestChat");
			}
			if (OneIn(5))
			{
				DoEmote();
			}
			Sleep(500 + random.Next(1000) + 1000 + random.Next(1500));
		}
		else
		{
			restPhase = 0;
			if (hasReachedTop)
			{
				jqState = ((random.NextDouble() < 0.75) ? JQState.NavigateToJQ : JQState.NavigateToExit);
			}
			else
			{
				jqState = JQState.NavigateToJQ;
			}
		}
	}

	private int RollNextTier()
	{
		if (hasReachedTop)
		{
			return RollWeighted(ExperiencedTierWeights);
		}
		int[] array = new int[TierWeights.Length];
		for (int i = 0; i < TierWeights.Length; i++)
		{
			array[i] = ((i + 1 > highestCompletedTier) ? TierWeights[i] : 0);
		}
		return RollWeighted(array);
	}

	private int RollWeighted(int[] weights)
	{
		int num = 0;
		foreach (int num2 in weights)
		{
			num += num2;
		}
		if (num == 0)
		{
			return 7;
		}
		int num3 = random.Next(num);
		int num4 = 0;
		for (int j = 0; j < weights.Length; j++)
		{
			num4 += weights[j];
			if (num3 < num4)
			{
				return j + 1;
			}
		}
		return 7;
	}

	private bool NearJqStart()
	{
		if (Math.Abs(chr.x - -1005f) <= 60f)
		{
			return Math.Abs(chr.y - 274f) <= 20f;
		}
		return false;
	}

	private void ScheduleMidJqChat()
	{
		int num = random.Next(3);
		for (int i = 0; i < num; i++)
		{
			Delay(20000 + random.Next(10000), () =>
			{
				ChatLine("MidJQ");
			});
		}
	}

	private void NavigateToExit()
	{
		switch (exitPhase)
		{
		case 0:
		{
			ChatLine("LeavingJQ");
			MapPortal mapPortal = FindPortal(5);
			if (mapPortal == null)
			{
				exitPhase = 0;
				jqState = JQState.Convert;
				break;
			}
			exitPhase = 1;
			navigateArrived = false;
			BotMovementService.Move(chr, mapPortal.x, mapPortal.y, () =>
			{
				navigateArrived = true;
			});
			break;
		}
		case 1:
			if (navigateArrived || !BotMovementService.IsMoving(chr))
			{
				BotMovementService.Stop(chr);
				Sleep(2000 + random.Next(1000) + random.Next(1000));
				exitPhase = 2;
			}
			break;
		case 2:
			WarpToExit();
			Sleep(1000L);
			exitPhase = 3;
			break;
		default:
			exitPhase = 0;
			jqState = JQState.Convert;
			break;
		}
	}

	private void WarpToExit()
	{
		MapPortal mapPortal = FindPortal(5);
		Func<BotCharacter, MapPortal, bool> enterPortal = BotMovementService.EnterPortal;
		if (mapPortal == null || enterPortal == null || mapPortal.targetMap != 100000200)
		{
			BotPerf.Record("JQBot.WarpUnavailable", 0.0);
			return;
		}
		try
		{
			enterPortal(chr, mapPortal);
		}
		catch (Exception)
		{
			BotPerf.Record("JQBot.WarpError", 0.0);
		}
	}

	private void ConvertToHenesysBot()
	{
		converted = true;
		BotTypeManager.ConvertBotType(chr, BotType.HENESYS_BOT);
		if (BotStore.GetBotById(chr.id) is HenesysBot henesysBot)
		{
			henesysBot.SetLastJqConversionTime(BotScheduler.Now());
		}
	}

	private MapPortal FindPortal(int id)
	{
		MapRuntime mapRuntime = MapService.Get(chr.mapId);
		if (mapRuntime == null)
		{
			return null;
		}
		foreach (MapPortal portal in mapRuntime.portals)
		{
			if (portal.id == id)
			{
				return portal;
			}
		}
		return null;
	}

	private void ChatLine(string node)
	{
		try
		{
			string text = dialogue.RandomLine("JQBotDialogue.yaml", "JQBot", node);
			if (text != null)
			{
				output.Speak(chr, text);
			}
		}
		catch (Exception)
		{
		}
	}

	private void DoEmote()
	{
		output.Emote(chr, 1 + random.Next(7));
	}

	private bool OneIn(int n)
	{
		return random.Next(n) == 0;
	}

	private void Sleep(long ms)
	{
		sleepUntil = BotScheduler.Now() + Math.Max(0L, ms);
	}

	private void Delay(long ms, Action action)
	{
		BotTasks.Handle handle = null;
		handle = BotTasks.Schedule(() =>
		{
			delayed.Remove(handle);
			if (Alive())
			{
				action();
			}
		}, ms, "Delay", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Types/HenesysJQBot.cs");
		delayed.Add(handle);
	}

	private bool Alive()
	{
		if (running && chr.online && BotRegistry.Contains(chr.id))
		{
			return BotStore.GetBotById(chr.id) == this;
		}
		return false;
	}

	public override void StopScheduledTask()
	{
		foreach (BotTasks.Handle item in delayed)
		{
			item.Cancel();
		}
		delayed.Clear();
		BotMovementPlayback.Cancel(chr);
		BotMovementService.Stop(chr);
		attempting = false;
		navigateStartedAt = 0L;
		navigateArrived = false;
		base.StopScheduledTask();
	}

	public override void DisplayCommands(BotActor player)
	{
		output.ShowOptions(player, new string[1] { chr.name });
	}
}
