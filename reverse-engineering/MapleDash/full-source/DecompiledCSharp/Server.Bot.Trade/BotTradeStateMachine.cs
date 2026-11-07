using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Server.Bot.Trade;

public sealed class BotTradeStateMachine
{
	public enum State
	{
		INITIALIZE,
		WAITING_RESPONSE,
		RESPONDING,
		AWAITING_CONFIRMATION,
		CONFIRMING,
		CONFIRMED_LOCKED,
		CLEANUP,
		COMPLETED,
		TIMED_OUT,
		DECLINE
	}

	public readonly TradeMode mode;

	private readonly BotStateMachine parent;

	private long startedAt;

	private long resumeAt;

	private Action pending;

	private TradeResult? lastResult;

	public State state { get; private set; }

	public bool OfferAccepted { get; private set; }

	public bool TradeComplete { get; private set; }

	private BotCharacter Chr => parent.chr;

	public BotTradeStateMachine(BotStateMachine parent, TradeMode mode = TradeMode.SELLING)
	{
		this.parent = parent;
		this.mode = mode;
		startedAt = BotScheduler.Now();
	}

	private void Delay(Action continuation)
	{
		resumeAt = BotScheduler.Now() + 2000;
		pending = continuation;
	}

	public void Update()
	{
		if (pending != null)
		{
			if (BotScheduler.Now() >= resumeAt)
			{
				Action action = pending;
				pending = null;
				action();
				CheckTimeout();
			}
			return;
		}
		switch (state)
		{
		case State.INITIALIZE:
			StartCallback();
			if (mode == TradeMode.SELLING)
			{
				state = ((!PostItems()) ? State.WAITING_RESPONSE : State.RESPONDING);
				break;
			}
			BotTradeCommands.WriteChat(Chr, (mode == TradeMode.BUYING) ? WantsMessage() : "我暂时没什么货。");
			state = State.WAITING_RESPONSE;
			break;
		case State.RESPONDING:
			if (mode == TradeMode.SELLING)
			{
				startedAt = BotScheduler.Now();
				BotTradeCommands.WriteChat(Chr, WantsMessage());
			}
			else if (mode == TradeMode.BUYING && CorrectItems())
			{
				int mesoOffering = parent.tradeWants.mesoOffering;
				if (mesoOffering > 0)
				{
					BotTradeCommands.SetMeso(Chr, mesoOffering);
					BotTradeCommands.WriteChat(Chr, "这件我出 " + FormatPrice(mesoOffering) + "。");
				}
				state = State.CONFIRMING;
				break;
			}
			state = State.WAITING_RESPONSE;
			break;
		case State.WAITING_RESPONSE:
			if (mode == TradeMode.SELLING)
			{
				if (!Sufficient() && BotTradeCommands.IsPartnerLocked(Chr))
				{
					state = State.DECLINE;
				}
				else if (Sufficient())
				{
					state = State.CONFIRMING;
				}
			}
			else if (mode == TradeMode.BUYING)
			{
				if (CorrectItems())
				{
					state = State.RESPONDING;
				}
				else if (BotTradeCommands.IsPartnerLocked(Chr) && !CorrectItems())
				{
					state = State.DECLINE;
				}
			}
			break;
		case State.CONFIRMING:
			startedAt = BotScheduler.Now();
			OfferAccepted = true;
			BotTradeCommands.WriteChat(Chr, parent.marketFlavor.TradeReply());
			BotTradeCommands.Confirm(Chr);
			state = State.CONFIRMED_LOCKED;
			break;
		case State.CONFIRMED_LOCKED:
			if (!parent.tradeHandler.VerifyPartner())
			{
				state = State.CLEANUP;
			}
			break;
		case State.CLEANUP:
		{
			parent.tradeInventory.Reset();
			parent.tradeWants.Reset();
			parent.tradeMode = TradeMode.NULL;
			bool successful = lastResult == TradeResult.SUCCESSFUL;
			parent.output.Emote(Chr, successful ? 2 : 4);
			parent.output.Speak(Chr, successful ? "谢谢，下次再来。" : "怎么取消了？");
			Delay(() =>
			{
				if (successful)
				{
					parent.lastTradeResult = TradeResult.SUCCESSFUL;
				}
				lastResult = null;
				state = State.COMPLETED;
			});
			return;
		}
		case State.COMPLETED:
			TradeComplete = true;
			break;
		case State.TIMED_OUT:
			BotTradeCommands.WriteChat(Chr, "等太久了，我先关了。");
			Delay(() =>
			{
				BotTradeCommands.Decline(Chr);
				TradeComplete = true;
				state = State.COMPLETED;
			});
			return;
		case State.DECLINE:
			parent.tradeLogic.blocked.AddToBlockList(Chr.id, Chr.trade.partner.actor.id, 300L);
			BotTradeCommands.WriteChat(Chr, "这个算了，下次再聊。");
			Delay(() =>
			{
				BotTradeCommands.Decline(Chr);
				state = State.COMPLETED;
			});
			return;
		default:
			throw new InvalidOperationException("Unexpected trade state: " + state);
		}
		CheckTimeout();
	}

	private void CheckTimeout()
	{
		if (BotScheduler.Now() > startedAt + 60000)
		{
			state = State.TIMED_OUT;
		}
	}

	private bool Sufficient()
	{
		return parent.tradeWants.VerifyTrade(BotTradeCommands.ReadPartnerMeso(Chr), BotTradeCommands.PartnerItems(Chr));
	}

	private bool CorrectItems()
	{
		return parent.tradeWants.VerifySufficientItems(BotTradeCommands.PartnerItems(Chr));
	}

	private bool PostItems()
	{
		InventoryEntry mainItem = parent.tradeInventory.MainItem;
		if (mainItem == null)
		{
			BotTradeCommands.WriteChat(Chr, "我现在没什么能出的，抱歉。");
			return false;
		}
		if (mainItem.isEquip)
		{
			BotTradeCommands.AddEquip(Chr, mainItem, 1);
		}
		else
		{
			BotTradeCommands.AddItem(Chr, mainItem.itemId, 1, 1);
		}
		BotTradeCommands.WriteChat(Chr, "货在这，你先看。");
		return true;
	}

	private void StartCallback()
	{
		parent.lastTradedCharacter = Chr.trade.partner.actor;
		Chr.trade.ResultCallback = (TradeResult result) =>
		{
			if (result == TradeResult.SUCCESSFUL)
			{
				lastResult = result;
			}
		};
	}

	public string WantsMessage()
	{
		long mesoWanted = parent.tradeWants.mesoWanted;
		List<ItemQuantity> itemsWanted = parent.tradeWants.itemsWanted;
		StringBuilder stringBuilder = new StringBuilder("我想要");
		if (mesoWanted > 0)
		{
			stringBuilder.Append(FormatPrice(mesoWanted));
			if (itemsWanted.Count > 0)
			{
				stringBuilder.Append("，还要");
			}
		}
		for (int i = 0; i < itemsWanted.Count; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append("、");
			}
			if (itemsWanted[i].quantity > 1)
			{
				stringBuilder.Append(itemsWanted[i].quantity).Append("个");
			}
			stringBuilder.Append(parent.ItemName(itemsWanted[i].itemId));
		}
		if (mesoWanted == 0L && itemsWanted.Count == 0)
		{
			stringBuilder.Append("先看看货");
		}
		return stringBuilder.ToString();
	}

	public static string FormatPrice(long price, int decimalPlaces = 1)
	{
		if (price < 1000)
		{
			return price.ToString(CultureInfo.InvariantCulture);
		}
		string[] array = new string[5] { "", "k", "m", "b", "t" };
		int num = 0;
		decimal num2 = price;
		while (num2 >= 1000m && num < array.Length - 1)
		{
			num2 /= 1000m;
			num++;
		}
		if (decimalPlaces <= 0)
		{
			return decimal.Floor(num2 + 0.5m).ToString(CultureInfo.InvariantCulture) + array[num];
		}
		return decimal.Round(num2, decimalPlaces, MidpointRounding.AwayFromZero).ToString("F" + decimalPlaces, CultureInfo.InvariantCulture).TrimEnd('0')
			.TrimEnd('.') + array[num];
	}
}
