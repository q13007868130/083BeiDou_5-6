using System;
using System.Collections.Generic;
using Server.Bot.Types;

namespace Server.Bot.Social;

public sealed class BotDispatcher : IDisposable
{
	private readonly BotMessages messages;

	private readonly BotOutput output;

	private readonly Func<ChatMessage, IEnumerable<BotActor>> charactersOnMap;

	private readonly Action<BotStateMachine, BotActor> listOptions;

	private BotTasks.Handle dispatchTask;

	private BotTasks.Handle cleanTask;

	public Action<Exception> OnError;

	public BotDispatcher(BotMessages messages, BotOutput output, Func<ChatMessage, IEnumerable<BotActor>> charactersOnMap, Action<BotStateMachine, BotActor> listOptions)
	{
		this.messages = messages ?? throw new ArgumentNullException("messages");
		this.output = output ?? throw new ArgumentNullException("output");
		this.charactersOnMap = charactersOnMap ?? throw new ArgumentNullException("charactersOnMap");
		this.listOptions = listOptions ?? throw new ArgumentNullException("listOptions");
	}

	public void Start()
	{
		if (dispatchTask == null || dispatchTask.Cancelled)
		{
			dispatchTask = BotTasks.ScheduleFixedRate(ProcessOne, 0L, 2000L, "Start", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Social/BotDispatcher.cs");
			cleanTask = BotTasks.ScheduleFixedRate(() =>
			{
				messages.Clean(BotScheduler.Now(), 10000L);
			}, 0L, 2000L, "Start", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Social/BotDispatcher.cs");
		}
	}

	public void ProcessOne()
	{
		try
		{
			ChatMessage chatMessage = messages.Poll(QueueType.PRIMARY);
			if (chatMessage == null)
			{
				return;
			}
			foreach (BotActor item in charactersOnMap(chatMessage))
			{
				if (!chatMessage.content.Contains(item.name) || BotStore.IsInvisibleBot(item.id))
				{
					continue;
				}
				BotStateMachine botById = BotStore.GetBotById(item.id);
				if (botById == null)
				{
					return;
				}
				if (!botById.running)
				{
					botById.interactors.SetRespondant(chatMessage.sender);
					botById.StartScheduledTask(0L);
					if (botById is SocialBot socialBot)
					{
						socialBot.OnFirstInteraction(chatMessage.sender);
					}
				}
				else if (botById is SocialBot socialBot2)
				{
					if (!socialBot2.hasActiveRespondant)
					{
						socialBot2.interactors.SetRespondant(chatMessage.sender);
						socialBot2.OnFirstInteraction(chatMessage.sender);
					}
				}
				else
				{
					botById.interactors.SetInquirer(chatMessage.sender);
					listOptions(botById, chatMessage.sender);
				}
				return;
			}
			if (BotStore.IsRespondant(chatMessage.sender))
			{
				output.ExpireOptions(chatMessage.sender);
				messages.Add(chatMessage);
			}
			else if (BotStore.IsInquirer(chatMessage.sender))
			{
				messages.Add(chatMessage, QueueType.TERTIARY);
			}
		}
		catch (Exception obj)
		{
			OnError?.Invoke(obj);
		}
	}

	public void Dispose()
	{
		dispatchTask?.Cancel();
		cleanTask?.Cancel();
		dispatchTask = null;
		cleanTask = null;
	}
}
