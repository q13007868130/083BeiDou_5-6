using System;
using System.Collections.Generic;
using Server.Bot.Events;
using Server.Bot.Social;

namespace Server.Bot.Types;

public sealed class GameZoneHostBot : BotStateMachine
{
	public enum HostBotState
	{
		Reset,
		IdleWaiting,
		DrinkOffer,
		WaitDrinkResponse,
		DrinkSelection,
		WaitDrinkPick,
		ServeDrink,
		Farewell
	}

	private const int DrinkCoke = 2020031;

	private const int DrinkWater = 2022000;

	private const int DrinkElixir = 2000012;

	private static readonly string[] DrinkNames = new string[3] { "Coke", "Water", "Elixir" };

	private static readonly int[] DrinkIds = new int[3] { 2020031, 2022000, 2000012 };

	private const string DialoguePack = "GameZoneHostBotDialogue.yaml";

	private const string PlayerNameToken = "%PLAYER_NAME%";

	private const long DrinkOfferTimeoutMs = 20000L;

	private const long DrinkPickTimeoutMs = 30000L;

	private const int IdleFlavorChancePer100 = 6;

	private readonly Random random;

	private readonly BotDialogue dialogue;

	private readonly BotMessages messages;

	private bool? drinkAccepted;

	private int selectedDrink;

	private List<string> hint;

	private long startTime;

	private long endTime;

	private bool scripted;

	private int generation;

	private BotTasks.Handle sequenceTask;

	public HostBotState hostBotState { get; private set; }

	public GameZoneHostBot(BotCharacter c, EventBus events = null, BotDialogue dialogue = null, BotMessages messages = null, Random random = null)
		: base(c)
	{
		this.dialogue = dialogue ?? BotTypeManager.Dialogue;
		this.messages = messages ?? BotTypeManager.Messages;
		this.random = random ?? new Random();
		hint = new List<string> { c.name };
		botType = "GameZoneHostBot";
		Subscribe(events ?? BotTypeManager.Events, EventType.MAP_ENTERED);
	}

	public override void UpdateState()
	{
		base.UpdateState();
		if (CheckIfNotRunningOrPaused())
		{
			return;
		}
		ProcessQueuedEvents();
		DebugLog(chr.name + " HostBotState: " + hostBotState);
		if (scripted)
		{
			return;
		}
		switch (hostBotState)
		{
		case HostBotState.Reset:
			ResetHostBotState();
			SetState(HostBotState.IdleWaiting);
			break;
		case HostBotState.IdleWaiting:
			IdleFlavorText();
			CheckForInquirer();
			break;
		case HostBotState.DrinkOffer:
			OfferDrink();
			break;
		case HostBotState.WaitDrinkResponse:
			WaitForResponse();
			if (!drinkAccepted.HasValue)
			{
				break;
			}
			if (drinkAccepted.Value)
			{
				SetState(HostBotState.DrinkSelection);
				break;
			}
			Say("DeclineResponse", flavor: false, () =>
			{
				SetState(HostBotState.Farewell);
			});
			break;
		case HostBotState.DrinkSelection:
			PresentDrinkOptions();
			break;
		case HostBotState.WaitDrinkPick:
			WaitForResponse();
			if (selectedDrink != 0)
			{
				SetState(HostBotState.ServeDrink);
			}
			break;
		case HostBotState.ServeDrink:
			ServeDrink();
			break;
		case HostBotState.Farewell:
			Farewell();
			break;
		default:
			DebugLog("Unexpected GameZoneHostBot state: " + hostBotState);
			state = State.FINISHED;
			ResetHostBotState();
			break;
		}
	}

	private void SetState(HostBotState next)
	{
		hostBotState = next;
	}

	private bool Alive()
	{
		if (running && chr.online)
		{
			return BotStore.GetBotById(chr.id) == this;
		}
		return false;
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
		}, ms, "After", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Types/GameZoneHostBot.cs");
	}

	private void Say(string node, bool flavor = false, Action then = null)
	{
		BotDialogue.Dialogue dialogue;
		try
		{
			dialogue = this.dialogue.Get("GameZoneHostBotDialogue.yaml", botType, node);
		}
		catch (Exception ex)
		{
			DebugLog("dialogue read failed " + node + ": " + ex.Message);
			dialogue = null;
		}
		if (dialogue == null || dialogue.text == null || dialogue.text.Count == 0)
		{
			then?.Invoke();
			return;
		}
		scripted = true;
		int token = generation;
		this.dialogue.Run(chr, dialogue, output, flavor, () =>
		{
			scripted = false;
			then?.Invoke();
		}, () => Alive() && token == generation);
	}

	private void ResetHostBotState()
	{
		SetState(HostBotState.Reset);
		drinkAccepted = null;
		selectedDrink = 0;
		hint = new List<string> { chr.name };
		startTime = 0L;
		endTime = 0L;
		scripted = false;
		sequenceTask?.Cancel();
		sequenceTask = null;
	}

	private void IdleFlavorText()
	{
		if (random.Next(100) < 6)
		{
			Say("Flavor", flavor: true);
		}
	}

	private void CheckForInquirer()
	{
		if (interactors.inquirers.Count != 0)
		{
			BotActor botActor = interactors.inquirers[0];
			interactors.RemoveInquirer(botActor);
			interactors.SetRespondant(botActor);
			SetState(HostBotState.DrinkOffer);
		}
	}

	private void OfferDrink()
	{
		Say("DrinkOffer", flavor: false, () =>
		{
			hint = new List<string> { "Yes", "No" };
			DisplayCommands(interactors.Respondant);
			StartTimer(20000L);
			SetState(HostBotState.WaitDrinkResponse);
		});
	}

	private void PresentDrinkOptions()
	{
		Say("DrinkSelection", flavor: false, () =>
		{
			hint = new List<string>(DrinkNames);
			DisplayCommands(interactors.Respondant);
			StartTimer(30000L);
			SetState(HostBotState.WaitDrinkPick);
		});
	}

	private void ServeDrink()
	{
		Say("ServeDrink", flavor: false, () =>
		{
			BotActor respondant = interactors.Respondant;
			if (respondant != null)
			{
				DropCommands.BotThrowItem(chr, selectedDrink, (int)respondant.x, (int)respondant.y, out var _);
			}
			SetState(HostBotState.Farewell);
		});
	}

	private void Farewell()
	{
		Say("Farewell", flavor: true, () =>
		{
			interactors.ResetRespondants();
			SetState(HostBotState.Reset);
		});
	}

	private void StartTimer(long durationMs)
	{
		startTime = BotScheduler.Now();
		endTime = startTime + durationMs;
	}

	private void WaitForResponse()
	{
		if (BotScheduler.Now() < endTime)
		{
			ProcessMessages();
			return;
		}
		ExpireOptions();
		output.Speak(chr, "Talk to me again if you're ready.");
		state = State.FINISHED;
		ResetHostBotState();
	}

	public override void DisplayCommands(BotActor player)
	{
		if (player != null && hint != null)
		{
			output.ShowOptions(player, hint);
		}
	}

	public override void HandleEvent(GameEvent ev)
	{
		if (ev != null && ev.type == EventType.MAP_ENTERED)
		{
			string text;
			try
			{
				text = dialogue.RandomLine("GameZoneHostBotDialogue.yaml", botType, "PassiveGreeting");
			}
			catch (Exception)
			{
				text = null;
			}
			if (text != null)
			{
				text = text.Replace("%PLAYER_NAME%", ev.playerName);
			}
			if (text != null)
			{
				output.Send(BotOutput.Kind.Bubble, chr, text);
			}
			output.Emote(chr, 2);
		}
	}

	private void ProcessMessages()
	{
		Consume(QueueType.SECONDARY);
		Consume(QueueType.PRIMARY);
	}

	private void Consume(QueueType type)
	{
		ChatMessage chatMessage = messages.Poll(type);
		if (chatMessage != null)
		{
			if (!interactors.IsMessageFromRespondant(chatMessage))
			{
				messages.Add(chatMessage, type);
			}
			else
			{
				HandleMessage(chatMessage);
			}
		}
	}

	private void HandleMessage(ChatMessage message)
	{
		string content = ((message.content == null) ? "" : message.content.ToLowerInvariant());
		if (hostBotState == HostBotState.WaitDrinkResponse)
		{
			HandleDrinkOfferResponse(content);
		}
		else if (hostBotState == HostBotState.WaitDrinkPick)
		{
			HandleDrinkPickResponse(content);
		}
	}

	private void HandleDrinkOfferResponse(string content)
	{
		if (content.Contains("yes"))
		{
			drinkAccepted = true;
		}
		else if (content.Contains("no"))
		{
			drinkAccepted = false;
		}
	}

	private void HandleDrinkPickResponse(string content)
	{
		if (content.Contains("yes") || content.Contains("no"))
		{
			return;
		}
		for (int i = 0; i < DrinkNames.Length; i++)
		{
			if (content.Contains(DrinkNames[i].ToLowerInvariant()))
			{
				output.Speak(chr, "One " + DrinkNames[i] + ", coming right up!");
				int drinkId = DrinkIds[i];
				After(2000L, () =>
				{
					selectedDrink = drinkId;
					scripted = false;
				});
				return;
			}
		}
		output.Speak(chr, "Hmm, I don't have that. Try again!");
		output.Emote(chr, 6);
	}

	private void ExpireOptions()
	{
		BotActor respondant = interactors.Respondant;
		if (respondant != null)
		{
			output.ExpireOptions(respondant);
		}
	}

	public override void StopScheduledTask()
	{
		generation++;
		sequenceTask?.Cancel();
		sequenceTask = null;
		scripted = false;
		base.StopScheduledTask();
	}
}
