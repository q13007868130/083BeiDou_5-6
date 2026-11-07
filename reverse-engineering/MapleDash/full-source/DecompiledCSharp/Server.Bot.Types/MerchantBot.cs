using System;
using System.Collections.Generic;
using Server.Bot.Environment;
using Server.Bot.Social;
using Server.Bot.Trade;

namespace Server.Bot.Types;

public sealed class MerchantBot : BotStateMachine
{
	public enum Step
	{
		Reset,
		SelectItem,
		Advertise,
		CheckTrades,
		IdleActions
	}

	private readonly bool buying;

	private readonly MarketItemPool pool;

	private readonly MarketEconomy economy;

	private readonly MarketBotAmbient ambient;

	private readonly Random random;

	private List<MarketItem> items;

	private int itemIndex;

	public Step CurrentStep { get; private set; }

	public MarketItem CurrentItem
	{
		get
		{
			if (items == null || itemIndex >= items.Count)
			{
				return null;
			}
			return items[itemIndex];
		}
	}

	public override bool UsesSharedMarketTick => true;

	public MerchantBot(BotCharacter actor, bool buying, MarketItemPool pool, MarketEconomy economy, MarketBotAmbient ambient, Random random = null)
		: base(actor)
	{
		this.buying = buying;
		this.pool = pool;
		this.economy = economy;
		this.ambient = ambient;
		this.random = random ?? new Random();
		botType = "MerchantBot";
	}

	private void LoadItems()
	{
		items = random.Next(buying ? 2 : 4) switch
		{
			2 => pool.Stars("A"), 
			1 => pool.Scrolls("A", dark: true), 
			0 => pool.Scrolls("A"), 
			_ => pool.Potions("S"), 
		};
		if (!buying && items.Count == 0)
		{
			items = pool.Potions("S");
		}
		if (!buying && items.Count == 0)
		{
			items = new List<MarketItem>
			{
				new MarketItem(2000002, 1, 100),
				new MarketItem(2000001, 1, 100)
			};
		}
	}

	private void SelectItem()
	{
		if (items == null || items.Count == 0)
		{
			LoadItems();
		}
		if (!buying && items.Count == 0)
		{
			return;
		}
		itemIndex++;
		if (itemIndex >= items.Count)
		{
			itemIndex = 0;
			LoadItems();
		}
		MarketItem currentItem = CurrentItem;
		if (currentItem != null)
		{
			tradeWants.Reset();
			if (buying)
			{
				tradeMode = TradeMode.BUYING;
				tradeWants.AddItemWanted(currentItem.itemId);
				tradeWants.mesoOffering = economy.Price(MarketEconomy.JavaInt((double)currentItem.price * (0.8 + random.NextDouble() * 0.1)));
				tradeWants.mesoWanted = 0L;
			}
			else
			{
				InventoryEntry inventoryEntry = BotLogic.GenerateCleanItemWithQty(currentItem.itemId, 1);
				inventoryEntry.position = 1;
				tradeInventory.SetMain(inventoryEntry);
				int num = pool.Price(currentItem.itemId) ?? throw new InvalidOperationException("Original sale item has no market price: " + currentItem.itemId);
				tradeWants.mesoWanted = economy.Price(MarketEconomy.JavaInt((double)num * 0.9));
				tradeMode = TradeMode.SELLING;
			}
			lastTradeResult = null;
			lastTradedCharacter = null;
		}
	}

	private void Advertise()
	{
		if (CurrentItem != null)
		{
			string text = ItemName(CurrentItem.itemId);
			if (text != null && random.Next(100) < 72 && marketDirector.TrySpeak(chr))
			{
				output.Speak(chr, buying ? marketFlavor.Buying(text, MarketEconomy.Shorthand(tradeWants.mesoOffering)) : marketFlavor.Selling(text));
			}
		}
	}

	public override void UpdateState()
	{
		base.UpdateState();
		if (CheckIfNotRunningOrPaused() || state == State.TRADING)
		{
			return;
		}
		ambient.Act(this);
		if (chr.chair > 0 || IsBusy || BotRecordedActions.IsMoving(chr))
		{
			if (chr.chair > 0 && BotConfig.MarketHawkEnabled(BotClient.GetBotClient()?.db))
			{
				Advertise();
			}
			return;
		}
		switch (CurrentStep)
		{
		case Step.Reset:
			itemIndex = 0;
			LoadItems();
			CurrentStep = Step.SelectItem;
			break;
		case Step.SelectItem:
			SelectItem();
			CurrentStep = Step.Advertise;
			break;
		case Step.Advertise:
			if (BotConfig.MarketHawkEnabled(BotClient.GetBotClient()?.db))
			{
				Advertise();
			}
			CurrentStep = Step.CheckTrades;
			BusyFor(400L);
			break;
		case Step.CheckTrades:
			CheckForTrades();
			CurrentStep = Step.IdleActions;
			break;
		case Step.IdleActions:
			if (random.Next(100) == 0)
			{
				BotTypeManager.ConvertBotType(chr, BotType.NX_MERCHANT_BOT);
			}
			else
			{
				CurrentStep = Step.SelectItem;
			}
			break;
		}
	}

	public override void DisplayCommands(BotActor player)
	{
		output.ShowOptions(player, new string[1] { chr.name });
	}

	public override void StopScheduledTask()
	{
		ambient.Forget(chr);
		BotMovementService.Disable(chr);
		base.StopScheduledTask();
	}
}
