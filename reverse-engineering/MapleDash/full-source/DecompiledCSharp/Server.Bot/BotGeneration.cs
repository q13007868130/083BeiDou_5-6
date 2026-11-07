using System;
using System.Collections.Generic;
using System.Threading;
using Server.Bot.Decorator;
using Server.Bot.Environment;
using Server.Bot.Social;
using Server.Field;

namespace Server.Bot;

public static class BotGeneration
{
	private const int BotBaseId = 20000;

	private static int currentBotCount = 100;

	private static bool environmentBotLimitActive;

	private static int environmentBotLimitStartCount;

	public const long SpawnChoreographyMaxMs = 7000L;

	private static readonly Random rng = new Random();

	private static BotDecorate decorator;

	private static BotNamePool names;

	private static DbStore Db => BotClient.GetBotClient()?.db;

	public static int GetBotsCreatedCount()
	{
		return currentBotCount - 100;
	}

	public static void EnableEnvironmentBotLimit()
	{
		Interlocked.CompareExchange(ref environmentBotLimitStartCount, GetBotsCreatedCount(), 0);
		environmentBotLimitActive = true;
	}

	public static bool EnvironmentBotLimitReached()
	{
		if (!environmentBotLimitActive)
		{
			return false;
		}
		int num = ((Db != null) ? BotConfig.EnvironmentBotMax(Db) : 30);
		return GetEnvironmentLimitedBotCount() >= num;
	}

	public static void DisableEnvironmentBotLimit()
	{
		environmentBotLimitActive = false;
		environmentBotLimitStartCount = 0;
	}

	private static int GetEnvironmentLimitedBotCount()
	{
		return Math.Max(0, GetBotsCreatedCount() - environmentBotLimitStartCount);
	}

	public static int CreateBot(int x, int y, int mapId, bool ignoreEnvironmentLimit = false, bool playArrival = true)
	{
		int num = ReserveBotId(ignoreEnvironmentLimit);
		if (num < 0)
		{
			return -1;
		}
		BotCharacter botCharacter = LoadBotTemplate();
		BotDirectory.Ensure(Db);
		BotProfile botProfile = BotDirectory.Take(mapId, rng);
		botCharacter.profileId = botProfile?.id ?? 0;
		SetBotStats(botCharacter, num, botProfile);
		if (BotConfig.NormalEquipsEnabled(Db))
		{
			botCharacter.equips.Clear();
			botCharacter.equipment.Clear();
		}
		AddBotToServer(botCharacter);
		PlaceBotOnMap(botCharacter, x, y, mapId);
		SetBotVariables(botCharacter, botProfile);
		if (playArrival)
		{
			PlaySpawnChoreography(botCharacter);
		}
		return num;
	}

	public static void PlaySpawnChoreography(BotCharacter bot, Action completed = null)
	{
		bot.arrivalTask?.Cancel();
		BotMovementPlayback.Cancel(bot);
		int mapId = bot.mapId;
		bot.arrivalTask = BotTasks.Schedule(() =>
		{
			Replay("portalenterdrop", () =>
			{
				if (rng.Next(2) == 0)
				{
					completed?.Invoke();
				}
				else
				{
					bot.arrivalTask = BotTasks.Schedule(Turn, rng.Next(1000, 1501), "PlaySpawnChoreography", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/BotGeneration.cs");
				}
			});
		}, rng.Next(500, 1201) + 1500, "PlaySpawnChoreography", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/BotGeneration.cs");
		bool Alive()
		{
			if (BotRegistry.Get(bot.id) == bot)
			{
				return bot.mapId == mapId;
			}
			return false;
		}
		void Replay(string name, Action next)
		{
			if (Alive())
			{
				List<BotMovementRecording.Packet> frames = BotMovementRecording.ReadBinary(BotResources.Load().ReadBytes(BotMovementRecording.ResourcePath(0, name, "bin")));
				BotMovementPlayback.Play(bot, frames, offset: true, (bool stopped) =>
				{
					if (!stopped && Alive())
					{
						next();
					}
				});
			}
		}
		void Turn()
		{
			if (Alive())
			{
				if (bot.stance == 3 || bot.stance == 5 || bot.stance == 7)
				{
					completed?.Invoke();
				}
				else
				{
					Replay("turnaroundtoleft", () =>
					{
						completed?.Invoke();
					});
				}
			}
		}
	}

	public static void WarpBotToLocation(BotCharacter bot, int x, int y, int mapId, Action completed = null)
	{
		PlaceBotOnMap(bot, x, y, mapId);
		PlaySpawnChoreography(bot, completed);
	}

	private static int ReserveBotId(bool ignoreLimit = false)
	{
		int num;
		do
		{
			num = currentBotCount;
			int num2 = Math.Max(0, num - 100 - environmentBotLimitStartCount);
			int num3 = ((Db != null) ? BotConfig.EnvironmentBotMax(Db) : 30);
			if (!ignoreLimit && environmentBotLimitActive && num2 >= num3)
			{
				return -1;
			}
		}
		while (Interlocked.CompareExchange(ref currentBotCount, num + 1, num) != num);
		return 20000 + num;
	}

	private static BotCharacter LoadBotTemplate()
	{
		BotClient botClient = BotClient.EnsureStandaloneClient(Db);
		BotClient botClient2 = new BotClient
		{
			db = botClient.db,
			world = botClient.world,
			channel = botClient.channel,
			accId = botClient.accId,
			gmLevel = botClient.gmLevel
		};
		return botClient2.player = new BotCharacter
		{
			client = botClient2,
			world = botClient.world,
			channel = botClient.channel,
			accId = botClient.accId,
			gmLevel = botClient.gmLevel
		};
	}

	private static void SetBotStats(BotCharacter bot, int botId, BotProfile profile)
	{
		ref string name = ref bot.name;
		string text;
		if (profile != null && !string.IsNullOrEmpty(profile.name))
		{
			text = profile.name;
		}
		else
		{
			text = (BotConfig.RandomChineseNameEnabled(Db) ? BotHelpers.GetRandomChineseBotName() : GetRandomCharacterIGN());
		}
		name = text;
		bot.id = botId;
		bot.fame = botId;
	}

	private static void PlaceBotOnMap(BotCharacter bot, int x, int y, int mapId)
	{
		bot.x = x;
		bot.y = MapService.SnapToGround(mapId, x, y);
		bot.stance = 5;
		bot.facing = -1;
		BotRegistry.EnterMap(bot, mapId);
	}

	private static void AddBotToServer(BotCharacter bot)
	{
		BotRegistry.Add(bot);
	}

	private static void SetBotVariables(BotCharacter bot, BotProfile profile = null)
	{
		BotAppearanceTemplates.Ensure(Db);
		IReadOnlyList<BotAppearanceTemplate> all = BotAppearanceTemplates.All;
		if (profile != null && all.Count > 0)
		{
			all[(profile.template % all.Count + all.Count) % all.Count].ApplyTo(bot);
			ApplyIdentity(bot, profile);
			return;
		}
		BotAppearanceTemplate botAppearanceTemplate = BotAppearanceTemplates.Take(rng);
		if (botAppearanceTemplate != null)
		{
			botAppearanceTemplate.ApplyTo(bot);
			return;
		}
		if (decorator == null)
		{
			decorator = new BotDecorate(BotResources.Load(), BotItemCatalog.Shared, BotTypeManager.Output);
		}
		decorator.ApplyAppearance(bot, Db);
	}

	private static void ApplyIdentity(BotCharacter bot, BotProfile profile)
	{
		if (profile.level > 0)
		{
			bot.level = Math.Max(1, Math.Min(200, profile.level));
			bot.job = Math.Max(0, profile.job);
			bot.tier = TierForLevel(bot.level);
			AlignWeapon(bot);
		}
	}

	private static BotTier TierForLevel(int level)
	{
		if (level < 180)
		{
			if (level < 160)
			{
				if (level < 140)
				{
					if (level < 120)
					{
						return BotTier.D;
					}
					return BotTier.C;
				}
				return BotTier.B;
			}
			return BotTier.A;
		}
		return BotTier.S;
	}

	private static void AlignWeapon(BotCharacter bot)
	{
		int num = WeaponStyleOfJob(bot.job);
		if (num == 0)
		{
			return;
		}
		bot.equips.TryGetValue(-11, out var value);
		if (StyleOfWeapon(value) == num)
		{
			return;
		}
		int num2 = DefaultWeapon(num);
		if (num2 <= 0)
		{
			return;
		}
		try
		{
			InventoryEntry inventoryEntry = BotItemCatalog.Shared.CleanItem(num2);
			inventoryEntry.position = -11;
			bot.equipment[-11] = inventoryEntry;
			bot.equips[-11] = num2;
		}
		catch (Exception)
		{
		}
	}

	private static int StyleOfWeapon(int itemId)
	{
		switch (itemId / 10000)
		{
		case 130:
		case 131:
		case 140:
		case 141:
		case 143:
		case 144:
			return 100;
		case 137:
		case 138:
			return 200;
		case 145:
		case 146:
			return 300;
		case 133:
		case 147:
			return 400;
		case 148:
		case 149:
			return 500;
		default:
			return 0;
		}
	}

	private static int WeaponStyleOfJob(int job)
	{
		return (job / 100) switch
		{
			1 => 100, 
			2 => 200, 
			3 => 300, 
			4 => 400, 
			5 => 500, 
			_ => 0, 
		};
	}

	private static int DefaultWeapon(int style)
	{
		return style switch
		{
			100 => 1302000, 
			200 => 1372000, 
			300 => 1452000, 
			400 => 1332000, 
			500 => 1482000, 
			_ => 0, 
		};
	}

	public static void RemoveBotFromServer(BotCharacter bot)
	{
		if (bot != null)
		{
			if (bot.profileId != 0)
			{
				BotDirectory.Release(bot.profileId);
				bot.profileId = 0;
			}
			BotStore.RemoveActiveBot(bot.id);
			BotWander.Stop(bot);
			BotRegistry.Remove(bot.id);
		}
	}

	public static BotCharacter CreateBotPollReadiness(int x, int y, int mapId, bool ignoreEnvironmentLimit = false, bool playArrival = true)
	{
		int num = CreateBot(x, y, mapId, ignoreEnvironmentLimit, playArrival);
		if (num >= 0)
		{
			return BotRegistry.Get(num);
		}
		return null;
	}

	private static string GetRandomCharacterIGN()
	{
		if (names == null)
		{
			names = new BotNamePool(BotResources.Load(), rng);
		}
		return names.CharacterName();
	}

	public static string GetRandomShopOwnerIGN()
	{
		if (names == null)
		{
			names = new BotNamePool(BotResources.Load(), rng);
		}
		return names.ShopOwnerName();
	}
}
