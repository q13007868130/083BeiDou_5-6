using System;
using System.Collections.Generic;
using Server.Bot.Environment;
using Server.Bot.Social;
using Server.Bot.Trade;
using Server.Field;

namespace Server.Bot.Types;

public sealed class TutorialBot : BotStateMachine
{
	public enum TutorialState
	{
		Reset,
		WaitForPlayerInRange,
		Start,
		Inquire,
		WaitForInquiryResponse,
		AskAdmin,
		WaitAdminResponse,
		GrantAdmin,
		GiftTrade,
		WaitTradeAccept,
		Tutorial1,
		Tutorial2,
		Tutorial2WaitPick,
		Tutorial3,
		Tutorial3WaitDrop,
		Tutorial3Upgrade,
		SendOff,
		Return
	}

	private const string DialoguePack = "TutorialBotDialogue.yaml";

	private const string ReturnRecording = "tutorial2";

	private static readonly int[] Hats = new int[3] { 1002419, 1002515, 1002418 };

	private const int PowerElixir = 2000005;

	private const int OnyxApple = 2022179;

	private const int TownScrollHenesys = 2030000;

	private const int IlbiStars = 2070006;

	private const int TradeMesos = 1000000000;

	private const int MapleKandayo = 1472032;

	private const int BlueSaunaRobe = 1050018;

	private const int RedSaunaRobe = 1051017;

	private const int WeaponScroll = 2043003;

	private const int NxCreditPerCode = 25000;

	private const long InquiryTimeoutMs = 20000L;

	private const long AdminTimeoutMs = 30000L;

	private const long TradeTimeoutMs = 30000L;

	private const long PickTimeoutMs = 40000L;

	private const int PortalX = 927;

	private const int PortalY = 485;

	private const int WeaponSlot = -11;

	private readonly Random random;

	private readonly BotDialogue dialogue;

	private readonly BotMessages messages;

	private readonly BotTradeService trades;

	private MarketScrollPricing pricing;

	private BotNXCodes nxCodes;

	private bool pricingTried;

	private bool? runTutorial;

	private bool? tutPicked;

	private bool? wantsAdmin;

	private string generatedNXCode;

	private string generatedNXCode2;

	private readonly List<int> noobsHelped = new List<int>();

	private List<string> hint;

	private int wepId;

	private InventoryEntry playersWep;

	private long startTime;

	private long endTime;

	private bool scripted;

	private int generation;

	private BotTasks.Handle sequenceTask;

	public TutorialState tutorialBotState { get; private set; }

	private BotActor Player => interactors.Respondant;

	private Func<bool> Guard()
	{
		int current = generation;
		return () => current == generation && Alive();
	}

	public TutorialBot(BotCharacter c, BotTradeService trades = null, BotDialogue dialogue = null, BotMessages messages = null, Random random = null)
		: base(c)
	{
		this.trades = trades;
		this.dialogue = dialogue ?? BotTypeManager.Dialogue;
		this.messages = messages ?? BotTypeManager.Messages;
		this.random = random ?? new Random();
		hint = new List<string> { c.name };
		startTime = BotScheduler.Now();
		botType = "TutorialBot";
	}

	public override void CheckPrioritySpeed()
	{
		if (interactors.Respondant != null)
		{
			UpdateScheduleDelay(1000L);
		}
		else
		{
			base.CheckPrioritySpeed();
		}
	}

	public override void UpdateState()
	{
		base.UpdateState();
		if (CheckIfNotRunningOrPaused())
		{
			return;
		}
		DebugLog(chr.name + " TutorialBotState: " + tutorialBotState);
		if (scripted)
		{
			return;
		}
		switch (tutorialBotState)
		{
		case TutorialState.Reset:
			ResetTutorialBotState();
			SetState(TutorialState.WaitForPlayerInRange);
			break;
		case TutorialState.WaitForPlayerInRange:
			WaitForNewPlayer();
			BotWaitingForPlayerFlavorText();
			if (!scripted && interactors.Respondant != null)
			{
				SetState(TutorialState.Start);
			}
			break;
		case TutorialState.Start:
			GreetPlayer();
			break;
		case TutorialState.Inquire:
			InquirePlayer();
			break;
		case TutorialState.WaitForInquiryResponse:
			WaitForResponse();
			if (runTutorial.HasValue)
			{
				Continue(runTutorial.Value ? TutorialState.AskAdmin : TutorialState.SendOff);
			}
			break;
		case TutorialState.AskAdmin:
			AskAdminRights();
			break;
		case TutorialState.WaitAdminResponse:
			WaitForResponse();
			if (wantsAdmin.HasValue)
			{
				Continue(wantsAdmin.Value ? TutorialState.GrantAdmin : TutorialState.Tutorial1);
			}
			break;
		case TutorialState.GrantAdmin:
			GrantAdminRights();
			break;
		case TutorialState.GiftTrade:
			InitiateGiftTrade();
			break;
		case TutorialState.WaitTradeAccept:
			if (WaitForTradeAccept())
			{
				PlaceTradeItems();
			}
			break;
		case TutorialState.Tutorial1:
			Tutorial1();
			break;
		case TutorialState.Tutorial2:
			Tutorial2();
			break;
		case TutorialState.Tutorial2WaitPick:
			WaitForResponse();
			if (tutPicked.HasValue && tutPicked.Value)
			{
				Continue(TutorialState.Tutorial3);
			}
			break;
		case TutorialState.Tutorial3:
			AnalyzePlayersWeapon();
			break;
		case TutorialState.Tutorial3WaitDrop:
			WaitForPlayerToDropWeapon();
			if (playersWep != null)
			{
				Continue(TutorialState.Tutorial3Upgrade);
			}
			break;
		case TutorialState.Tutorial3Upgrade:
			UpgradeAndReturnWeapon();
			break;
		case TutorialState.SendOff:
			SendOff();
			break;
		case TutorialState.Return:
			ReturnOrigin();
			break;
		default:
			DebugLog("Unexpected TutorialBot state: " + tutorialBotState);
			state = State.FINISHED;
			ResetTutorialBotState();
			break;
		}
	}

	public override void DisplayCommands(BotActor player)
	{
		if (player != null && hint != null)
		{
			output.ShowOptions(player, hint);
		}
	}

	private void SetState(TutorialState next)
	{
		tutorialBotState = next;
	}

	private void Continue(TutorialState next)
	{
		scripted = false;
		SetState(next);
	}

	private bool Alive()
	{
		if (running && chr.online)
		{
			return BotStore.GetBotById(chr.id) == this;
		}
		return false;
	}

	private void StartTimer(long durationMs)
	{
		startTime = BotScheduler.Now();
		endTime = startTime + durationMs;
	}

	private void ResetTutorialBotState()
	{
		SetState(TutorialState.Reset);
		runTutorial = null;
		tutPicked = null;
		wantsAdmin = null;
		generatedNXCode = null;
		generatedNXCode2 = null;
		hint = new List<string> { chr.name };
		wepId = 0;
		playersWep = null;
		startTime = BotScheduler.Now();
		endTime = 0L;
		scripted = false;
		sequenceTask?.Cancel();
		sequenceTask = null;
	}

	private void After(long ms, Action next)
	{
		scripted = true;
		sequenceTask?.Cancel();
		Func<bool> keepGoing = Guard();
		sequenceTask = BotTasks.Schedule(() =>
		{
			if (!keepGoing())
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
		}, ms, "After", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Types/TutorialBot.cs");
	}

	private void Schedule(long ms, Action work)
	{
		Func<bool> keepGoing = Guard();
		BotTasks.Schedule(() =>
		{
			if (!keepGoing())
			{
				return;
			}
			try
			{
				work();
			}
			catch (Exception ex)
			{
				DebugLog(chr.name + " scheduled task error: " + ex.Message);
			}
		}, ms, "Schedule", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Types/TutorialBot.cs");
	}

	private void Say(string node, IDictionary<string, string> replacements = null, Action then = null, bool flavor = false)
	{
		BotDialogue.Dialogue dialogue;
		try
		{
			dialogue = this.dialogue.Get("TutorialBotDialogue.yaml", botType, node);
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
		if (replacements != null)
		{
			try
			{
				dialogue.text = BotDialogue.ReplaceStrings(dialogue.text, replacements);
			}
			catch (Exception ex2)
			{
				DebugLog("dialogue replace failed " + node + ": " + ex2.Message);
			}
		}
		scripted = true;
		this.dialogue.Run(chr, dialogue, output, flavor, () =>
		{
			if (Alive())
			{
				scripted = false;
				then?.Invoke();
			}
		}, Guard());
	}

	private void Face(BotActor player)
	{
		if (player != null)
		{
			chr.facing = ((player.x >= chr.x) ? 1 : (-1));
		}
	}

	private string NameOf(int itemId)
	{
		string text;
		if (ItemName != null)
		{
			text = ItemName(itemId);
			if (text == null)
			{
				return "NULL";
			}
		}
		else
		{
			text = "NULL";
		}
		return text;
	}

	private void AbandonTutorial()
	{
		ExpireOptions();
		state = State.FINISHED;
		ResetTutorialBotState();
	}

	private bool RequirePlayer(out BotActor player)
	{
		player = Player;
		if (player != null)
		{
			return true;
		}
		AbandonTutorial();
		return false;
	}

	private void ExpireOptions()
	{
		BotActor player = Player;
		if (player != null)
		{
			output.ExpireOptions(player);
		}
	}

	private void WaitForNewPlayer()
	{
		if (interactors.Respondant == null)
		{
			BotActor botActor = BotLogic.WaitForPlayerInRange(chr, 500, 150);
			if (botActor != null && !BotHelpers.IsBot(botActor) && !noobsHelped.Contains(botActor.id))
			{
				interactors.SetRespondant(botActor);
			}
		}
	}

	private void BotWaitingForPlayerFlavorText()
	{
		if (random.Next(100) < 6)
		{
			Say("Flavor", null, null, flavor: true);
		}
	}

	private void GreetPlayer()
	{
		if (RequirePlayer(out var player))
		{
			Face(player);
			Say("Greeting", Replacements("%RESP_NAME", player.name), () =>
			{
				Continue(TutorialState.Inquire);
			});
		}
	}

	private void InquirePlayer()
	{
		if (RequirePlayer(out var player))
		{
			Say("Inquiry", null, () =>
			{
				SetHint(YesNoHint());
				DisplayCommands(player);
				StartTimer(20000L);
				Continue(TutorialState.WaitForInquiryResponse);
			});
		}
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
		ResetTutorialBotState();
	}

	private void AskAdminRights()
	{
		if (RequirePlayer(out var player))
		{
			Say("AskAdmin", null, () =>
			{
				SetHint(YesNoHint());
				DisplayCommands(player);
				StartTimer(30000L);
				Continue(TutorialState.WaitAdminResponse);
			});
		}
	}

	private void GrantAdminRights()
	{
		if (RequirePlayer(out var player))
		{
			Func<BotActor, bool> grantTutorialAdmin = BotTypeManager.GrantTutorialAdmin;
			if (grantTutorialAdmin == null || !grantTutorialAdmin(player))
			{
				throw new InvalidOperationException("Tutorial admin progression is not connected");
			}
			generatedNXCode = GenerateNxCode();
			generatedNXCode2 = GenerateNxCode();
			Dictionary<string, string> replacements = Replacements("%NX_CODE", generatedNXCode ?? "(兑换码生成失败)", "%NX_CODE_2", generatedNXCode2 ?? "(兑换码生成失败)");
			Say("GrantAdmin", replacements, () =>
			{
				DebugLog("[TutorialBot] Granted admin to " + player.name + " | NX code 1: " + (generatedNXCode ?? "-") + " | NX code 2: " + (generatedNXCode2 ?? "-"));
				Continue(TutorialState.GiftTrade);
			});
		}
	}

	private string GenerateNxCode()
	{
		try
		{
			DbStore dbStore = BotClient.GetBotClient()?.db;
			if (dbStore == null)
			{
				DebugLog("[TutorialBot] NX code skipped: no DbStore");
				return null;
			}
			if (nxCodes == null)
			{
				nxCodes = new BotNXCodes(dbStore, random);
			}
			string text = nxCodes.Generate();
			return (nxCodes.Create(text, 25000) > 0) ? text : null;
		}
		catch (Exception ex)
		{
			DebugLog("[TutorialBot] NX code generation failed: " + ex.Message);
			return null;
		}
	}

	private void InitiateGiftTrade()
	{
		if (!RequirePlayer(out var player))
		{
			return;
		}
		Say("GiftTradeIntro", null, () =>
		{
			if (trades == null)
			{
				DebugLog("[TutorialBot] GIFT_TRADE: BotTradeService not wired, skipping gift trade");
				After(1500L, () =>
				{
					Continue(TutorialState.Tutorial1);
				});
			}
			else
			{
				StartTimer(30000L);
				After(2000L, () =>
				{
					if (!BotTradeCommands.SendRequest(chr, player, trades))
					{
						DebugLog("[TutorialBot] trade request to " + player.name + " failed");
					}
					scripted = false;
					SetState(TutorialState.WaitTradeAccept);
				});
			}
		});
	}

	private bool WaitForTradeAccept()
	{
		if (!(chr.trade is BotTradeService.Session session))
		{
			return false;
		}
		if (session.FullTrade)
		{
			return true;
		}
		if (BotScheduler.Now() > endTime)
		{
			BotTradeCommands.Decline(chr);
			output.Speak(chr, "No worries, the gifts will be waiting for you next time.");
			Continue(TutorialState.Tutorial1);
		}
		return false;
	}

	private void PlaceTradeItems()
	{
		if (RequirePlayer(out var player))
		{
			int robeId = ((player.gender == 0) ? 1050018 : 1051017);
			scripted = true;
			BotTiming.CreateChain().StopUnless(Guard()).Pause(1000L)
				.Run(() =>
				{
					BotTradeCommands.WriteChat(chr, "Here are some goodies to get you started!");
				})
				.Pause(1500L)
				.Run(() =>
				{
					BotTradeCommands.SetMeso(chr, 1000000000L);
				})
				.Pause(500L)
				.Run(() =>
				{
					BotTradeCommands.AddItem(chr, 2000005, 1000, 1);
				})
				.Pause(300L)
				.Run(() =>
				{
					BotTradeCommands.AddItem(chr, 2022179, 100, 2);
				})
				.Pause(300L)
				.Run(() =>
				{
					BotTradeCommands.AddItem(chr, 2030000, 100, 3);
				})
				.Pause(300L)
				.Run(() =>
				{
					BotTradeCommands.AddItem(chr, 2070006, 800, 4);
				})
				.Pause(300L)
				.Run(() =>
				{
					BotTradeCommands.AddCleanEquip(chr, 1472032, 5);
				})
				.Pause(300L)
				.Run(() =>
				{
					BotTradeCommands.AddCleanEquip(chr, robeId, 6);
				})
				.Pause(500L)
				.Run(() =>
				{
					BotTradeCommands.WriteChat(chr, "1B mesos, potions, scrolls, stars, and gear. All yours!");
				})
				.Pause(1000L)
				.Run(() =>
				{
					BotTradeCommands.Confirm(chr);
				})
				.Run(() =>
				{
					Continue(TutorialState.Tutorial1);
				})
				.Start();
		}
	}

	private void Tutorial1()
	{
		Say("Tutorial_1", Replacements("%CHR_NAME", chr.name), () =>
		{
			Continue(TutorialState.Tutorial2);
		});
	}

	private void Tutorial2()
	{
		Say("Tutorial_2", null, () =>
		{
			Tutorial2Drops();
			StartTimer(40000L);
			Continue(TutorialState.Tutorial2WaitPick);
		});
	}

	private void Tutorial2Drops()
	{
		DropCommands.BotThrowEquipsInARow(chr, Hats, (int)chr.x, (int)chr.y, 75);
		DisplayTutorialDropOptions();
	}

	private void DisplayTutorialDropOptions()
	{
		List<string> list = new List<string>();
		int[] hats = Hats;
		foreach (int itemId in hats)
		{
			list.Add(NameOf(itemId));
		}
		SetHint(list);
		DisplayCommands(Player);
	}

	private void AnalyzePlayersWeapon()
	{
		InventoryEntry inventoryEntry = ReadEquippedWeapon();
		if (inventoryEntry == null)
		{
			Continue(TutorialState.SendOff);
			return;
		}
		wepId = inventoryEntry.itemId;
		Say("AnalyzePlayersWeapon", Replacements("%WPN_NAME", NameOf(inventoryEntry.itemId)), () =>
		{
			Continue(TutorialState.Tutorial3WaitDrop);
		});
	}

	private InventoryEntry ReadEquippedWeapon()
	{
		BotActor player = Player;
		if (player == null || player.IsBot())
		{
			return null;
		}
		List<InventoryEntry> list = player.character?.inventory;
		if (list == null)
		{
			return null;
		}
		foreach (InventoryEntry item in list)
		{
			if (item != null && item.position == -11 && item.inventoryType == 1)
			{
				return item;
			}
		}
		return null;
	}

	private void WaitForPlayerToDropWeapon()
	{
		if (!RequirePlayer(out var player))
		{
			return;
		}
		List<MapItem> list = BotLogic.CheckForItemOnFloor(player.mapId, (int)player.x, (int)player.y, wepId);
		InventoryEntry inventoryEntry = null;
		foreach (MapItem item in list)
		{
			if (item != null && item.item != null && item.ItemId == wepId)
			{
				inventoryEntry = item.item;
			}
		}
		if (list.Count > 0)
		{
			DropCommands.LootItemListOnFloor(chr, list);
		}
		playersWep = inventoryEntry;
	}

	private void UpgradeAndReturnWeapon()
	{
		After(2000L, () =>
		{
			Say("LootDialogue", Replacements("%WPN_NAME", NameOf(wepId)), () =>
			{
				After(6000L, ScrollAndReturn);
			});
		});
	}

	private void ScrollAndReturn()
	{
		if (ScrollPlayersDroppedWeapon())
		{
			After(3000L, () =>
			{
				Say("UpgradeDialogue", null, ReturnUpgradedWeapon);
			});
		}
		else
		{
			DebugLog("[TutorialBot] weapon upgrade skipped: MarketScrollPricing unavailable");
			output.Speak(chr, "Looks like my tools aren't cooperating today. Here's your weapon back, untouched.");
			ReturnUpgradedWeapon();
		}
	}

	private bool ScrollPlayersDroppedWeapon()
	{
		if (playersWep == null)
		{
			return false;
		}
		MarketScrollPricing marketScrollPricing = ResolvePricing();
		if (marketScrollPricing == null)
		{
			return false;
		}
		try
		{
			InventoryEntry inventoryEntry = playersWep.Copy();
			if (inventoryEntry.upgradeslots > 0)
			{
				marketScrollPricing.ImproveStats(inventoryEntry, 2043003);
				inventoryEntry.upgradeslots--;
				if (inventoryEntry.equipmentData == null)
				{
					inventoryEntry.equipmentData = new Dictionary<string, object>();
				}
				inventoryEntry.equipmentData["level"] = (sbyte)(BotItemCatalog.Int(inventoryEntry.equipmentData, "level") + 1);
			}
			inventoryEntry.owner = Player?.name ?? inventoryEntry.owner;
			playersWep = inventoryEntry;
			output.Send(BotOutput.Kind.Scroll, chr, null, 1);
			output.Emote(chr, 2);
			return true;
		}
		catch (Exception ex)
		{
			DebugLog("[TutorialBot] weapon scroll failed: " + ex.Message);
			return false;
		}
	}

	private MarketScrollPricing ResolvePricing()
	{
		if (pricing != null || pricingTried)
		{
			return pricing;
		}
		pricingTried = true;
		try
		{
			pricing = new MarketScrollPricing(new MarketItemPool(BotResources.Load()), BotItemCatalog.Shared, random);
		}
		catch (Exception ex)
		{
			DebugLog("[TutorialBot] MarketScrollPricing unavailable: " + ex.Message);
			pricing = null;
		}
		return pricing;
	}

	private void ReturnUpgradedWeapon()
	{
		if (RequirePlayer(out var player))
		{
			if (playersWep != null)
			{
				DropCommands.BotThrowEquip(chr, playersWep, (int)player.x, (int)player.y);
			}
			Say("ReturnDialogue", null, () =>
			{
				Continue(TutorialState.SendOff);
			});
		}
	}

	private void SendOff()
	{
		Say("SendoffDialogue", null, EscortPlayerToPortal);
	}

	private void EscortPlayerToPortal()
	{
		scripted = true;
		Func<bool> keepGoing = Guard();
		BotMovementService.Move(chr, 927, 485, () =>
		{
			if (keepGoing())
			{
				After(2000L, SendOffPartingGift);
			}
		});
	}

	private void SendOffPartingGift()
	{
		if (!RequirePlayer(out var player))
		{
			return;
		}
		Say("SendoffPartingGiftDialogue", null, () =>
		{
			DropCommands.BotThrowItem(chr, 2022179, (int)player.x, (int)player.y, out var _);
			if (!noobsHelped.Contains(player.id))
			{
				noobsHelped.Add(player.id);
			}
			After(1000L, () =>
			{
				Continue(TutorialState.Return);
			});
		});
	}

	private void ReturnOrigin()
	{
		ExpireOptions();
		scripted = true;
		Func<bool> keepGoing = Guard();
		List<BotMovementRecording.Packet> list = LoadMovementRecording(chr.mapId);
		Action finish = () =>
		{
			if (keepGoing())
			{
				After(1000L, () =>
				{
					interactors.ResetRespondants();
					Continue(TutorialState.Reset);
				});
			}
		};
		if (list != null && list.Count > 0)
		{
			BotMovementPlayback.Play(chr, list, offset: false, (bool stopped) =>
			{
				if (!stopped)
				{
					finish();
				}
			});
		}
		else
		{
			DebugLog("[TutorialBot] return recording missing for map " + chr.mapId);
			finish();
		}
	}

	private static List<BotMovementRecording.Packet> LoadMovementRecording(int mapId)
	{
		try
		{
			byte[] array = BotResources.Load().ReadBytes(BotMovementRecording.ResourcePath(mapId, "tutorial2", "bin"));
			return (array == null) ? null : BotMovementRecording.ReadBinary(array);
		}
		catch (Exception)
		{
			return null;
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
		if (tutorialBotState == TutorialState.WaitForInquiryResponse)
		{
			HandleInquiryResponse(content);
		}
		else if (tutorialBotState == TutorialState.WaitAdminResponse)
		{
			HandleAdminResponse(content);
		}
		else if (tutorialBotState == TutorialState.Tutorial2WaitPick)
		{
			HandleTutPickResponse(content);
		}
	}

	private void HandleInquiryResponse(string content)
	{
		if (content.Contains("yes"))
		{
			runTutorial = true;
		}
		else if (content.Contains("no"))
		{
			runTutorial = false;
		}
	}

	private void HandleAdminResponse(string content)
	{
		if (content.Contains("yes"))
		{
			wantsAdmin = true;
		}
		else if (content.Contains("no"))
		{
			wantsAdmin = false;
		}
	}

	private void HandleTutPickResponse(string content)
	{
		int[] hats = Hats;
		foreach (int num in hats)
		{
			string text = NameOf(num);
			if (!(text == "NULL") && content.Contains(text.ToLowerInvariant()))
			{
				HandleSelectedHat(text, num);
				return;
			}
		}
		output.Speak(chr, "Did you type the name in correctly?");
		output.Emote(chr, 6);
	}

	private void HandleSelectedHat(string itemName, int itemIdSelected)
	{
		output.Speak(chr, "You've selected " + itemName + "! Enjoy it!");
		tutPicked = true;
		Schedule(2500L, () =>
		{
			LootLeftoverHats(itemIdSelected);
		});
	}

	private void LootLeftoverHats(int itemIdSelected)
	{
		List<int> list = new List<int>();
		int[] hats = Hats;
		foreach (int num in hats)
		{
			if (num != itemIdSelected)
			{
				list.Add(num);
			}
		}
		DropCommands.BotLootSelectedItems(chr, list.ToArray());
	}

	private static string[] YesNoHint()
	{
		return new string[2] { "Yes", "No" };
	}

	private void SetHint(IList<string> options)
	{
		hint = new List<string>(options);
	}

	private static Dictionary<string, string> Replacements(string key, string value)
	{
		return new Dictionary<string, string> { [key] = value };
	}

	private static Dictionary<string, string> Replacements(string key1, string value1, string key2, string value2)
	{
		return new Dictionary<string, string>
		{
			[key1] = value1,
			[key2] = value2
		};
	}

	public override void StopScheduledTask()
	{
		generation++;
		sequenceTask?.Cancel();
		sequenceTask = null;
		scripted = false;
		ExpireOptions();
		BotMovementPlayback.Cancel(chr);
		BotMovementService.Stop(chr);
		chr.trade?.Decline();
		base.StopScheduledTask();
	}
}
