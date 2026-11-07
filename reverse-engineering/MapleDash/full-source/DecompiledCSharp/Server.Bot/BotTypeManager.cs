using System;
using System.Collections.Generic;
using Server.Bot.Decorator;
using Server.Bot.Environment;
using Server.Bot.Events;
using Server.Bot.Social;
using Server.Bot.Trade;
using Server.Bot.Types;
using Server.Bot.Types.Blackjack;
using Server.Bot.Types.Pq;

namespace Server.Bot;

public static class BotTypeManager
{
	public static Func<int, bool> RealPlayersOnMap;

	private static readonly Random rng = new Random();

	public static readonly BotMessages Messages = new BotMessages();

	public static readonly BotOutput Output = new BotOutput();

	public static readonly BotTradeLogic Trades = new BotTradeLogic();

	public static readonly MarketBotFlavor MarketFlavor = new MarketBotFlavor();

	public static readonly MarketBotDirector Market = new MarketBotDirector(() => BotClient.GetBotClient()?.db);

	private static BotDialogue dialogue;

	private static MarketItemPool items;

	private static MarketEconomy economy;

	public static BotPartyGrindService PartyGrind;

	public static BotPartyService Parties;

	public static BotTradeService TradeService;

	public static Func<BotActor, bool> GrantTutorialAdmin;

	public static Action<BotActor, int> ApplySkillEffect;

	public static BotMessenger Messenger = new BotMessenger();

	private static MarketBotAmbient ambient;

	public static readonly EventBus Events = new EventBus();

	public static readonly BotFlavor Flavor = new BotFlavor();

	public static BotDialogue Dialogue => dialogue ?? (dialogue = new BotDialogue(BotResources.Load()));

	public static MarketBotAmbient Ambient => ambient ?? (ambient = new MarketBotAmbient(BotResources.Load(), () => BotClient.GetBotClient()?.db));

	private static ScrollingBot CreateScrollingBot(BotCharacter actor)
	{
		BotItemCatalog shared = BotItemCatalog.Shared;
		MarketItemPool pool = items ?? (items = new MarketItemPool(BotResources.Load()));
		MarketEquipmentPool equipment = new MarketEquipmentPool(pool, shared, new EquipMetadataCache(shared, EquipMetadataCache.LoadPools()), BotClient.GetBotClient().db);
		return new ScrollingBot(actor, () => equipment.ForJob("A", 0), new MarketScrollPricing(pool, shared), economy ?? (economy = new MarketEconomy()), dialogue ?? (dialogue = new BotDialogue(BotResources.Load())));
	}

	public static BotStateMachine CreateAndSetBot(BotCharacter c, BotType t)
	{
		BotStateMachine botStateMachine = t switch
		{
			BotType.COMPANION_GRIND_BOT => (BotStateMachine)new CompanionGrindBot(c, PartyGrind), 
			BotType.SCROLL_BOT => CreateScrollingBot(c), 
			BotType.FM_BOT => new FMBot(c, Ambient), 
			BotType.SELLING_MERCHANT_BOT => new MerchantBot(c, buying: false, items ?? (items = new MarketItemPool(BotResources.Load())), economy ?? (economy = new MarketEconomy()), Ambient), 
			BotType.BUYING_MERCHANT_BOT => new MerchantBot(c, buying: true, items ?? (items = new MarketItemPool(BotResources.Load())), economy ?? (economy = new MarketEconomy()), Ambient), 
			BotType.NX_MERCHANT_BOT => new NXMerchantBot(c, Ambient, Messenger, new BotNXCodes(BotClient.GetBotClient().db), dialogue ?? (dialogue = new BotDialogue(BotResources.Load()))), 
			BotType.SOCIAL_BOT => new SocialBot(c, dialogue ?? (dialogue = new BotDialogue(BotResources.Load())), Output, Messages, () => BotFurniture.RandomChair(rng)), 
			BotType.TOWN_WANDERER_BOT => new TownWandererBot(c, dialogue ?? (dialogue = new BotDialogue(BotResources.Load())), Flavor, Events), 
			BotType.HENESYS_BOT => new HenesysBot(c, dialogue ?? (dialogue = new BotDialogue(BotResources.Load())), Flavor, BotConfig.HenesysBotsChangeMapsEnabled(BotClient.GetBotClient()?.db)), 
			BotType.BLACKJACK_DEALER => new BlackjackDealerBot(c, Messages), 
			BotType.OPQ_BOT => new OpqBot(c), 
			BotType.PQ_BOT => new PqBot(c), 
			BotType.HENESYS_JQ_BOT => new HenesysJQBot(c), 
			BotType.DROP_GAME_BOT => new DropGameBot(c, Parties), 
			BotType.TUTORIAL_BOT => new TutorialBot(c, TradeService), 
			BotType.DICE_BOT => new DiceBot(c, Messages), 
			BotType.GACHA_BOT => new GachaBot(c, Events, Dialogue), 
			BotType.GAME_ZONE_HOST_BOT => new GameZoneHostBot(c, Events, Dialogue, Messages), 
			_ => new GenericBot(c), 
		};
		if (botStateMachine is GenericBot)
		{
			botStateMachine.botType = t.ToString();
		}
		botStateMachine.IsMapObserved = () => BotMovementService.Observed(c.mapId);
		botStateMachine.HasPlayersOnMap = () => RealPlayersOnMap != null && RealPlayersOnMap(c.mapId);
		BotStore.AddActiveBot(c.id, botStateMachine);
		return botStateMachine;
	}

	public static void ManuallyStartBot(BotCharacter c)
	{
		if (c != null)
		{
			BotStateMachine botById = BotStore.GetBotById(c.id);
			if (botById != null && !botById.running)
			{
				botById.running = true;
				long initialDelayMs = (botById.SkipsSpawnChoreography ? rng.Next(250, 1100) : (7000L + (long)rng.Next(0, 3000)));
				botById.StartScheduledTask(initialDelayMs);
			}
		}
	}

	public static void ManuallyStopBot(BotCharacter c)
	{
		if (c != null)
		{
			BotStateMachine botById = BotStore.GetBotById(c.id);
			if (botById != null)
			{
				botById.running = false;
				botById.StopScheduledTask();
			}
		}
	}

	public static void ConvertBotType(BotCharacter c, BotType t)
	{
		ManuallyStopBot(c);
		CreateAndSetBot(c, t);
		ManuallyStartBot(c);
	}

	public static void SetBotTypes(List<int> botIds, BotType t)
	{
		foreach (int botId in botIds)
		{
			BotCharacter botCharacter = BotRegistry.Get(botId);
			if (botCharacter != null)
			{
				CreateAndSetBot(botCharacter, t);
			}
		}
	}

	public static void StartBots(List<int> botIds)
	{
		foreach (int botId in botIds)
		{
			BotCharacter botCharacter = BotRegistry.Get(botId);
			if (botCharacter != null)
			{
				ManuallyStartBot(botCharacter);
			}
		}
	}

	public static void StopBots(List<int> botIds)
	{
		foreach (int botId in botIds)
		{
			BotCharacter botCharacter = BotRegistry.Get(botId);
			if (botCharacter != null)
			{
				ManuallyStopBot(botCharacter);
			}
		}
	}

	public static void SetAndStartBots(List<int> botIds, BotType t)
	{
		foreach (int botId in botIds)
		{
			BotCharacter botCharacter = BotRegistry.Get(botId);
			if (botCharacter != null)
			{
				try
				{
					CreateAndSetBot(botCharacter, t);
					ManuallyStartBot(botCharacter);
				}
				catch (Exception)
				{
				}
			}
		}
	}
}
