using System;
using System.Collections.Generic;
using Server.Bot.Social;
using Server.Bot.Trade;

namespace Server.Bot.Types;

public sealed class ScrollingBot : BotStateMachine
{
	public enum Step
	{
		Reset,
		SetScrollVars,
		ScrollingItems,
		AdvertiseSales,
		WaitingAfterAd
	}

	private readonly Func<List<MarketEquipment>> generate;

	private readonly MarketScrollPricing pricing;

	private readonly MarketEconomy economy;

	private readonly BotDialogue dialogue;

	private readonly Random random;

	private List<MarketEquipment> items;

	private bool[] results = Array.Empty<bool>();

	private int position;

	private long waitUntil;

	private bool awaitingTradeCheck;

	public Step CurrentStep { get; private set; }

	public ScrollingBot(BotCharacter actor, Func<List<MarketEquipment>> generate, MarketScrollPricing pricing, MarketEconomy economy, BotDialogue dialogue, Random random = null)
		: base(actor)
	{
		this.generate = generate;
		this.pricing = pricing;
		this.economy = economy;
		this.dialogue = dialogue;
		this.random = random ?? new Random();
		botType = "ScrollingBot";
	}

	public static bool[] ExactScrollList(int successes, int slots, Random random)
	{
		bool[] array = new bool[slots];
		for (int i = 0; i < successes; i++)
		{
			array[i] = true;
		}
		for (int num = slots - 1; num > 0; num--)
		{
			int num2 = random.Next(num + 1);
			bool flag = array[num];
			array[num] = array[num2];
			array[num2] = flag;
		}
		return array;
	}

	public override void UpdateState()
	{
		base.UpdateState();
		if (CheckIfNotRunningOrPaused() || state == State.TRADING || IsBusy)
		{
			return;
		}
		if (awaitingTradeCheck)
		{
			awaitingTradeCheck = false;
			CheckForTrades();
			return;
		}
		switch (CurrentStep)
		{
		case Step.Reset:
			CurrentStep = Step.SetScrollVars;
			break;
		case Step.SetScrollVars:
			items = generate();
			if (items.Count != 0)
			{
				InventoryEntry item2 = items[0].Item;
				int successes = BotItemCatalog.Int(item2.equipmentData, "level");
				int upgradeslots = BotItemCatalog.Shared.CleanEquip(item2.itemId).upgradeslots;
				results = ExactScrollList(successes, upgradeslots, random);
				position = 0;
				CurrentStep = Step.ScrollingItems;
			}
			break;
		case Step.ScrollingItems:
		{
			if (position >= results.Length)
			{
				CheckForTrades();
				CurrentStep = Step.AdvertiseSales;
				break;
			}
			bool flag = results[position++];
			output.Send(BotOutput.Kind.Scroll, chr, null, flag ? 1 : 2);
			output.Emote(chr, flag ? 2 : 4);
			long num = 0L;
			if (random.NextDouble() < 0.2)
			{
				BotDialogue.Dialogue dialogue = this.dialogue.Get("ScrollingBotDialogue.yaml", botType, flag ? "ScrollSuccess" : "ScrollFail");
				this.dialogue.Run(chr, dialogue, output, flavor: true);
				num = dialogue?.durationMs ?? 0;
			}
			BusyFor(num + 10000 + random.Next(5001));
			awaitingTradeCheck = true;
			break;
		}
		case Step.AdvertiseSales:
			if (random.NextDouble() < 0.1)
			{
				InventoryEntry item = items[0].Item;
				tradeInventory.SetMain(item);
				tradeWants.Reset();
				tradeWants.mesoWanted = economy.Price(pricing.EquipValue(item));
				string text = this.dialogue.RandomLine("ScrollingBotDialogue.yaml", botType, "AdvertiseSale")?.Replace("%PRICE%", MarketEconomy.Shorthand(tradeWants.mesoWanted));
				output.Send(BotOutput.Kind.ItemMegaphone, chr, (text == null) ? null : (chr.name + " : " + text), 0, 0, 0, 0, item);
				tradeMode = TradeMode.SELLING;
			}
			waitUntil = BotScheduler.Now() + 300000 + random.Next(180001);
			CurrentStep = Step.WaitingAfterAd;
			break;
		case Step.WaitingAfterAd:
			CheckForTrades();
			if (BotScheduler.Now() >= waitUntil)
			{
				CurrentStep = Step.SetScrollVars;
			}
			break;
		}
	}

	public override void DisplayCommands(BotActor player)
	{
		output.ShowOptions(player, new string[1] { chr.name });
	}
}
