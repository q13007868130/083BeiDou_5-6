using System;
using System.Collections.Generic;
using Server.Bot.Social;

namespace Server.Bot.Types;

public sealed class SocialBot : BotStateMachine
{
	public enum Variant
	{
		SINGLE_RESPONSE,
		INTERACTIVE
	}

	private enum InteractionLevel
	{
		NORMAL,
		REDUCED,
		NONVERBAL,
		IGNORE
	}

	private enum SocialState
	{
		IDLE_AMBIENT,
		GREETING,
		AWAITING_CHOICE,
		RESPONDING
	}

	private sealed class Tracker
	{
		public int count;

		public long last = BotScheduler.Now();

		public bool Expired => BotScheduler.Now() - last > 300000;

		public InteractionLevel Level
		{
			get
			{
				if (Expired)
				{
					count = 0;
					last = BotScheduler.Now();
					return InteractionLevel.NORMAL;
				}
				if (count > 3)
				{
					if (count > 4)
					{
						if (count != 5)
						{
							return InteractionLevel.IGNORE;
						}
						return InteractionLevel.NONVERBAL;
					}
					return InteractionLevel.REDUCED;
				}
				return InteractionLevel.NORMAL;
			}
		}

		public void Increment()
		{
			count++;
			last = BotScheduler.Now();
		}
	}

	private static readonly string[] options = new string[4] { "最近在忙什么？", "有什么新鲜事？", "听到什么传闻？", "先聊到这里" };

	private readonly Random random;

	private readonly BotDialogue dialogue;

	private readonly BotMessages messages;

	private readonly Func<int> randomChair;

	private readonly Dictionary<int, Tracker> trackers = new Dictionary<int, Tracker>();

	public readonly Variant variant;

	private SocialState socialState;

	private long lastMessageTime;

	private long lastCleanupTime;

	private bool wasSitting;

	private int originalChair;

	private int conversationVersion;

	public SocialBot(BotCharacter chr, BotDialogue dialogue, BotOutput output, BotMessages messages, Func<int> randomChair, Random random = null)
		: base(chr)
	{
		this.dialogue = dialogue;
		base.output = output;
		this.messages = messages;
		this.randomChair = randomChair;
		this.random = random ?? new Random();
		variant = ((this.random.NextDouble() < 0.3) ? Variant.INTERACTIVE : Variant.SINGLE_RESPONSE);
		lastCleanupTime = BotScheduler.Now();
		botType = "SocialBot";
	}

	public override bool IsAvailableForAmbientActions()
	{
		return !hasActiveRespondant;
	}

	public override void CheckPrioritySpeed()
	{
		if (hasActiveRespondant)
		{
			SetPriorityHigh();
		}
		else if (CheckMainPlayersOnMap())
		{
			SetPriorityNormal();
		}
		else
		{
			UpdateScheduleDelay(30000L);
		}
	}

	public override void UpdateState()
	{
		base.UpdateState();
		if (CheckIfNotRunningOrPaused())
		{
			return;
		}
		CheckPrioritySpeed();
		if (hasActiveRespondant)
		{
			if (lastMessageTime != 0L && BotScheduler.Now() - lastMessageTime > 35000)
			{
				SpeakCategory("Timeout");
				ResetConversation();
			}
			ChatMessage chatMessage = messages.Poll();
			if (chatMessage != null && !BotHelpers.IsBot(chatMessage.sender) && interactors.Respondant != null && chatMessage.sender.id == interactors.Respondant.id)
			{
				lastMessageTime = BotScheduler.Now();
				HandleMessage(chatMessage);
			}
		}
		if (BotScheduler.Now() - lastCleanupTime < 120000)
		{
			return;
		}
		lastCleanupTime = BotScheduler.Now();
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, Tracker> tracker in trackers)
		{
			if (tracker.Value.Expired)
			{
				list.Add(tracker.Key);
			}
		}
		foreach (int item in list)
		{
			trackers.Remove(item);
		}
	}

	private Tracker GetTracker(int id)
	{
		if (!trackers.TryGetValue(id, out var value))
		{
			value = (trackers[id] = new Tracker());
		}
		return value;
	}

	private BotTiming.Chain Sequence()
	{
		int version = conversationVersion;
		return BotTiming.CreateChain().StopUnless(() => running && version == conversationVersion && BotRegistry.Contains(chr.id));
	}

	public void OnFirstInteraction(BotActor player)
	{
		lastMessageTime = BotScheduler.Now();
		Tracker tracker = GetTracker(player.id);
		InteractionLevel level = tracker.Level;
		if (level == InteractionLevel.IGNORE)
		{
			BusyHint(player);
			ResetConversation();
			return;
		}
		wasSitting = chr.chair > 0;
		BotTiming.Chain chain = Sequence();
		if (wasSitting)
		{
			originalChair = chr.chair;
			chain.Run(() =>
			{
				output.CancelChair(chr);
			}).Pause(600L);
		}
		chain.Run(() =>
		{
			Face(player);
		}).Pause(2000 + random.Next(2000)).Run(() =>
		{
			switch (level)
			{
			case InteractionLevel.NORMAL:
				if (variant == Variant.SINGLE_RESPONSE)
				{
					string category = ((random.NextDouble() < 0.01) ? "Rare" : "SingleResponse");
					BotTiming.Chain chain2 = Sequence();
					string line = Line(category);
					if (line != null)
					{
						chain2.Run(() =>
						{
							output.Speak(chr, line);
						}).Pause(500 + random.Next(500)).Run(() =>
						{
							int num = Emote(category);
							if (num > 0)
							{
								output.Emote(chr, num);
							}
						});
					}
					chain2.Run(() =>
					{
						tracker.Increment();
						Face(player);
					}).Run(() =>
					{
						Resit(ResetConversation);
					}).Start();
				}
				else
				{
					ReplyWithEmote("Greeting", () =>
					{
						output.ShowOptions(player, options);
						socialState = SocialState.GREETING;
						tracker.Increment();
						socialState = SocialState.AWAITING_CHOICE;
					});
				}
				break;
			case InteractionLevel.REDUCED:
				SpeakCategory("Reduced");
				tracker.Increment();
				ResetConversation();
				break;
			case InteractionLevel.NONVERBAL:
				SpeakCategory("Nonverbal");
				tracker.Increment();
				ResetConversation();
				break;
			}
		})
			.Start();
	}

	private void HandleMessage(ChatMessage message)
	{
		Tracker tracker = GetTracker(message.sender.id);
		switch (tracker.Level)
		{
		case InteractionLevel.IGNORE:
			BusyHint(message.sender);
			ResetConversation();
			return;
		case InteractionLevel.NONVERBAL:
			SpeakCategory("Nonverbal");
			tracker.Increment();
			ResetConversation();
			return;
		case InteractionLevel.REDUCED:
			SpeakCategory("Reduced");
			tracker.Increment();
			ResetConversation();
			return;
		}
		if (socialState != SocialState.AWAITING_CHOICE)
		{
			return;
		}
		string text = message.content.Trim().ToLowerInvariant();
		string category;
		if (text == "1" || text.Contains("忙什么"))
		{
			category = "WhatsUp";
		}
		else if (text == "2" || text.Contains("新鲜事"))
		{
			category = "Interesting";
		}
		else if (text == "3" || text.Contains("传闻"))
		{
			category = "Rumors";
		}
		else
		{
			if (text == "4" || text.Contains("先聊到这里") || text.Contains("再见"))
			{
				tracker.Increment();
				ReplyWithEmote("Goodbye", () =>
				{
					Resit(ResetConversation);
				});
				return;
			}
			category = "WhatsUp";
		}
		if (random.NextDouble() < 0.01)
		{
			category = "Rare";
		}
		Sequence().Pause(2000 + random.Next(2000)).Run(() =>
		{
			Face(message.sender);
			ReplyWithEmote(category, () =>
			{
				output.ShowOptions(message.sender, options);
			});
		}).Start();
		tracker.Increment();
	}

	private void ReplyWithEmote(string category, Action then)
	{
		string text = Line(category);
		if (text == null)
		{
			then();
			return;
		}
		output.Speak(chr, text);
		int emote = Emote(category);
		if (emote > 0)
		{
			Sequence().Pause(400L).Run(() =>
			{
				output.Emote(chr, emote);
			}).Run(then)
				.Start();
		}
		else
		{
			then();
		}
	}

	private void Resit(Action then)
	{
		bool num = wasSitting || random.NextDouble() < 0.4;
		int chair = originalChair;
		if (num)
		{
			Sequence().Pause(1000 + random.Next(1500)).Run(() =>
			{
				output.Sit(chr, (chair > 0) ? chair : randomChair());
				wasSitting = false;
				originalChair = 0;
				then();
			}).Start();
		}
		else
		{
			wasSitting = false;
			originalChair = 0;
			then();
		}
	}

	private void BusyHint(BotActor player)
	{
		output.Send(BotOutput.Kind.YellowMessage, player, "对方好像正在忙...", 0, player.id);
		output.Send(BotOutput.Kind.Hint, player, "对方好像正在忙...", 0, player.id, 150, 5);
		output.Send(BotOutput.Kind.EnableActions, player, null, 0, player.id);
		BotTasks.Schedule(() =>
		{
			output.ExpireOptions(player);
		}, 5000L, "BusyHint", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Types/SocialBot.cs");
	}

	private void ResetConversation()
	{
		if (interactors.Respondant != null)
		{
			output.ExpireOptions(interactors.Respondant);
		}
		interactors.ResetRespondants();
		socialState = SocialState.IDLE_AMBIENT;
		lastMessageTime = 0L;
		conversationVersion++;
	}

	private void Face(BotActor player)
	{
		chr.facing = ((player.x >= chr.x) ? 1 : (-1));
	}

	private string Line(string category)
	{
		try
		{
			return dialogue.RandomLine("SocialBotDialogue.yaml", "SocialBot", category);
		}
		catch
		{
			return null;
		}
	}

	private int Emote(string category)
	{
		try
		{
			return dialogue.RandomEmote("SocialBotDialogue.yaml", "SocialBot", category);
		}
		catch
		{
			return -1;
		}
	}

	private void SpeakCategory(string category)
	{
		string text = Line(category);
		if (text != null)
		{
			output.Speak(chr, text);
		}
	}

	public override void StopScheduledTask()
	{
		conversationVersion++;
		base.StopScheduledTask();
	}
}
