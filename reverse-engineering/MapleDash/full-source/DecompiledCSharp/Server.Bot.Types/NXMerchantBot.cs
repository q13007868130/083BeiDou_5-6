using System;
using Server.Bot.Environment;
using Server.Bot.Social;
using Server.Bot.Trade;

namespace Server.Bot.Types;

public sealed class NXMerchantBot : BotStateMachine
{
	public enum Step
	{
		Setup,
		Advertise,
		CheckTrades,
		DeliverCode,
		ConvertBack
	}

	private readonly MarketBotAmbient ambient;

	private readonly BotMessenger messenger;

	private readonly BotNXCodes codes;

	private readonly BotDialogue dialogue;

	private readonly Random random;

	private int cycles;

	private int attempts;

	private bool delivering;

	private BotTasks.Handle pending;

	private BotActor recipient;

	private static readonly string[] flavor = new string[4] { "ScamMessages", "BeggingMessages", "RWTMessages", "FunnyMessages" };

	private static readonly string[] advertisements = new string[7] { "出1万点券兑换码，五千万金币，交易我！", "出点券码一万点，五千万，别刀太狠。", "点券码一万点换五千万，只接受交易。", "正经出点券码，一万点五千万，要的密。", "出点券一万点，五千万，先到先得。", "卖点券码，一万点五千万，认真的来。", "有点券码，一万点换五千万，交易窗见。" };

	public Step CurrentStep { get; private set; }

	public override bool UsesSharedMarketTick => true;

	public NXMerchantBot(BotCharacter actor, MarketBotAmbient ambient, BotMessenger messenger, BotNXCodes codes, BotDialogue dialogue, Random random = null)
		: base(actor)
	{
		this.ambient = ambient;
		this.messenger = messenger;
		this.codes = codes;
		this.dialogue = dialogue;
		this.random = random ?? new Random();
		botType = "MerchantBot";
	}

	private void Advertise()
	{
		if (marketDirector.TrySpeak(chr))
		{
			output.Speak(chr, advertisements[random.Next(advertisements.Length)]);
		}
	}

	private void ConvertBack()
	{
		BotTypeManager.ConvertBotType(chr, (random.Next(2) == 0) ? BotType.SELLING_MERCHANT_BOT : BotType.BUYING_MERCHANT_BOT);
	}

	private void Later(long milliseconds, Action action)
	{
		pending = BotTasks.Schedule(() =>
		{
			if (running && chr.online && BotStore.GetBotById(chr.id) == this)
			{
				action();
			}
		}, milliseconds, "Later", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Types/NXMerchantBot.cs");
	}

	private void FinishDelivery()
	{
		lastTradeResult = null;
		lastTradedCharacter = null;
		recipient = null;
		delivering = false;
		CurrentStep = Step.ConvertBack;
	}

	private void BeginDelivery()
	{
		if (lastTradeResult != TradeResult.SUCCESSFUL)
		{
			ConvertBack();
			return;
		}
		delivering = true;
		recipient = lastTradedCharacter;
		attempts = 0;
		output.Speak(chr, "我密你了。");
		messenger.Invite(chr, recipient);
		CheckInvitation();
	}

	private void CheckInvitation()
	{
		attempts++;
		if (messenger.Accepted(chr, recipient))
		{
			string code = codes.Generate();
			codes.Create(code);
			messenger.Send(chr, chr.name + "1", typing: true);
			Later(3000L, () =>
			{
				messenger.Send(chr, "一万点的兑换码给你，记下来，不要带横杠。");
				messenger.Send(chr, chr.name + "1", typing: true);
				Later(7000L, () =>
				{
					messenger.Send(chr, code);
					messenger.Send(chr, chr.name + "1", typing: true);
					Later(2000L, () =>
					{
						messenger.Send(chr, "用得开心！");
						Later(2000L, () =>
						{
							messenger.Leave(chr.id);
							FinishDelivery();
						});
					});
				});
			});
		}
		else if (attempts < 15)
		{
			Later(2000L, CheckInvitation);
		}
		else
		{
			output.Speak(chr, "你没接聊天邀请，那就算了。");
			FinishDelivery();
		}
	}

	public override void UpdateState()
	{
		if (delivering)
		{
			return;
		}
		base.UpdateState();
		if (CheckIfNotRunningOrPaused() || state == State.TRADING)
		{
			return;
		}
		ambient.Act(this);
		if (chr.chair > 0 || IsBusy)
		{
			if (chr.chair > 0)
			{
				Advertise();
			}
			return;
		}
		if (lastTradeResult == TradeResult.SUCCESSFUL && CurrentStep != Step.DeliverCode && CurrentStep != Step.ConvertBack)
		{
			CurrentStep = Step.DeliverCode;
		}
		switch (CurrentStep)
		{
		case Step.Setup:
		{
			InventoryEntry inventoryEntry = BotLogic.GenerateCleanItemWithQty(4031865, 100);
			inventoryEntry.position = 1;
			tradeInventory.SetMain(inventoryEntry);
			tradeWants.Reset();
			tradeWants.mesoWanted = 50000000L;
			tradeMode = TradeMode.SELLING;
			lastTradeResult = null;
			lastTradedCharacter = null;
			CurrentStep = Step.Advertise;
			break;
		}
		case Step.Advertise:
			if (random.Next(25) == 0)
			{
				BotDialogue.Dialogue dialogue = this.dialogue.Get("MerchantBotDialogue.yaml", "MerchantBot", flavor[random.Next(flavor.Length)]);
				this.dialogue.Run(chr, dialogue, output, flavor: true);
				if (dialogue != null)
				{
					BusyFor(dialogue.durationMs);
				}
			}
			else
			{
				Advertise();
			}
			CurrentStep = Step.CheckTrades;
			break;
		case Step.CheckTrades:
			CheckForTrades();
			cycles++;
			CurrentStep = ((lastTradeResult == TradeResult.SUCCESSFUL) ? Step.DeliverCode : ((cycles < 15) ? Step.Advertise : Step.ConvertBack));
			break;
		case Step.DeliverCode:
			BeginDelivery();
			break;
		case Step.ConvertBack:
			ConvertBack();
			break;
		}
	}

	public override void DisplayCommands(BotActor player)
	{
		output.ShowOptions(player, new string[1] { chr.name });
	}

	public override void StopScheduledTask()
	{
		pending?.Cancel();
		delivering = false;
		messenger.Leave(chr.id);
		ambient.Forget(chr);
		BotMovementService.Disable(chr);
		base.StopScheduledTask();
	}
}
