using System;
using Server.Bot.Environment;
using Server.Bot.Events;
using Server.Bot.Social;
using Server.Field;

namespace Server.Bot.Types;

public sealed class TownWandererBot : BotStateMachine
{
	public enum WanderState
	{
		Reset,
		Idle,
		Wander,
		Emote,
		ChangeMap
	}

	private readonly Random random;

	private readonly BotDialogue dialogue;

	private readonly BotFlavor flavor;

	private int homeMapId = -1;

	private int lifetime;

	public WanderState wanderState { get; private set; }

	public TownWandererBot(BotCharacter actor, BotDialogue dialogue, BotFlavor flavor, EventBus events, Random random = null)
		: base(actor)
	{
		this.dialogue = dialogue;
		this.flavor = flavor;
		this.random = random ?? new Random();
		botType = "TownWandererBot";
		Subscribe(events, EventType.LEVEL_UP);
	}

	public override bool IsAvailableForAmbientActions()
	{
		return true;
	}

	public override void UpdateState()
	{
		base.UpdateState();
		if (CheckIfNotRunningOrPaused() || chr == null || MapService.Get(chr.mapId) == null)
		{
			return;
		}
		ProcessQueuedEvents();
		switch (wanderState)
		{
		case WanderState.Reset:
			if (homeMapId < 0)
			{
				homeMapId = chr.mapId;
			}
			EnsureRoaming();
			wanderState = WanderState.Idle;
			break;
		case WanderState.Idle:
			flavor.MaybeExpress(this);
			EnsureRoaming();
			if (random.Next(3) == 0)
			{
				wanderState = WanderState.Wander;
			}
			else if (random.Next(5) == 0)
			{
				wanderState = WanderState.Emote;
			}
			break;
		case WanderState.Wander:
			EnsureRoaming();
			RandomEmote();
			RandomChat("WanderChat");
			wanderState = WanderState.Idle;
			break;
		case WanderState.Emote:
			RandomEmote();
			RandomChat("EmoteReaction");
			wanderState = WanderState.Idle;
			break;
		case WanderState.ChangeMap:
			wanderState = WanderState.Idle;
			break;
		}
	}

	private void EnsureRoaming()
	{
		if (!BotMovementService.Observed(chr.mapId))
		{
			BotWander.Stop(chr);
		}
		else if (!BotWander.IsWandering(chr))
		{
			BotWander.Start(chr, banded: false, 0f, 0f);
		}
	}

	private void RandomEmote()
	{
		if (random.Next(10) == 0)
		{
			output.Emote(chr, (random.Next(2) == 0) ? 2 : 3);
		}
	}

	private string Line(string node)
	{
		try
		{
			return dialogue.RandomLine("TownWandererDialogue.yaml", "TownWandererBot", node);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private void RandomChat(string node)
	{
		if (random.Next(20) == 0)
		{
			string text = Line(node);
			if (text != null)
			{
				output.Speak(chr, text);
			}
		}
	}

	public override void HandleEvent(GameEvent ev)
	{
		BotActor actor = ev?.actor;
		if (ev == null || ev.type != EventType.LEVEL_UP || actor == null || actor.id == chr.id || actor.mapId != chr.mapId || !IsAvailableForAmbientActions())
		{
			return;
		}
		double num = actor.x - chr.x;
		double num2 = actor.y - chr.y;
		if (num * num + num2 * num2 > 122500.0 || random.NextDouble() >= 0.3)
		{
			return;
		}
		int generation = lifetime;
		BotTiming.AfterRandom(5000L, 9000L, () =>
		{
			if (Valid())
			{
				string spoken = Line("CongratsLevelUp");
				int emote = ((random.Next(2) == 0) ? 2 : 6);
				BotTiming.CreateChain().StopUnless(Valid).Run(() =>
				{
					chr.facing = ((!(actor.x < chr.x)) ? 1 : (-1));
					chr.stance = ((chr.facing < 0) ? 5 : 4);
					output.IdleStanding(chr);
				})
					.PauseRandom(300L, 700L)
					.Run(() =>
					{
						if (spoken != null)
						{
							output.Speak(chr, spoken);
						}
					})
					.Pause(400L)
					.Run(() =>
					{
						output.Emote(chr, emote);
					})
					.Start();
			}
		});
		bool Valid()
		{
			if (generation == lifetime && BotStore.GetBotById(chr.id) == this && actor.mapId == chr.mapId)
			{
				return IsAvailableForAmbientActions();
			}
			return false;
		}
	}

	public override void StopScheduledTask()
	{
		lifetime++;
		flavor.Forget(chr);
		BotWander.Stop(chr);
		BotMovementService.Disable(chr);
		base.StopScheduledTask();
	}
}
