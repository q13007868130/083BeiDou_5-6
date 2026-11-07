using System;
using System.Collections.Generic;
using Server.Bot.Decorator;
using Server.Bot.Environment;
using Server.Field;

namespace Server.Bot.Social;

public sealed class MarketBotAmbient
{
	private enum Phase
	{
		Rest,
		Walk,
		Sit,
		Climb
	}

	private sealed class Life
	{
		public Phase phase;

		public long until;

		public int sitBias;

		public int direction;
	}

	private readonly Dictionary<int, Life> lives = new Dictionary<int, Life>();

	private readonly Random random;

	private readonly PlatformParser platforms;

	private readonly Func<DbStore> database;

	public Func<BotCharacter, bool> KeepsShopChair;

	private static readonly int[] mounts = new int[8] { 1902000, 1902001, 1902002, 1902005, 1902008, 1902009, 1902011, 1902012 };

	public MarketBotAmbient(BotResources resources, Func<DbStore> database, Random random = null)
	{
		platforms = new PlatformParser(resources.ReadText, (string directory) => resources.ListFileStems(directory));
		this.database = database;
		this.random = random ?? new Random();
	}

	public void Forget(BotCharacter actor)
	{
		lives.Remove(actor.id);
		BotRecordedActions.Cancel(actor);
	}

	public void Clear()
	{
		lives.Clear();
	}

	public void Act(BotStateMachine bot)
	{
		BotCharacter botCharacter = bot?.chr;
		if (botCharacter == null || MapService.Get(botCharacter.mapId) == null || botCharacter.trade != null || BotRecordedActions.IsMoving(botCharacter))
		{
			return;
		}
		long num = BotScheduler.Now();
		if (!lives.TryGetValue(botCharacter.id, out var value))
		{
			Dictionary<int, Life> dictionary = lives;
			int id = botCharacter.id;
			Life life = new Life
			{
				sitBias = random.Next(8, 14),
				direction = ((random.Next(2) == 0) ? 1 : (-1)),
				until = num + random.Next(400, 1600)
			};
			value = life;
			dictionary[id] = life;
		}
		Func<BotCharacter, bool> keepsShopChair = KeepsShopChair;
		if (keepsShopChair != null && keepsShopChair(botCharacter))
		{
			Sit(bot);
			TalkSitting(bot);
		}
		else if (value.phase == Phase.Sit && botCharacter.chair > 0 && num < value.until)
		{
			TalkSitting(bot);
		}
		else if (value.phase == Phase.Sit)
		{
			Stand(bot);
			Rest(value, 2500, 7000);
			Fidget(bot);
		}
		else if (value.phase == Phase.Rest && num < value.until)
		{
			Fidget(bot);
		}
		else
		{
			if ((value.phase == Phase.Walk || value.phase == Phase.Climb) && num < value.until)
			{
				return;
			}
			if (!BotConfig.MarketWanderEnabled(database()))
			{
				TalkIdle(bot);
				return;
			}
			int num2 = random.Next(100);
			if (num2 < 72)
			{
				Stroll(bot, value, jump: false);
			}
			else if (num2 < 72 + value.sitBias)
			{
				Sit(bot);
				value.phase = Phase.Sit;
				value.until = num + random.Next(12000, 28000);
			}
			else if (num2 < 86 && botCharacter.mapId == 910000000)
			{
				Climb(bot, value);
			}
			else if (num2 < 93)
			{
				Stroll(bot, value, jump: true);
			}
			else if (num2 < 97)
			{
				botCharacter.mountItemId = mounts[random.Next(mounts.Length)];
				botCharacter.mountSkillId = 1004;
				botCharacter.mountActive = true;
				bot.output.Send(BotOutput.Kind.Mount, botCharacter, null, botCharacter.mountItemId);
				Stroll(bot, value, jump: false);
			}
			else
			{
				TalkIdle(bot);
				Rest(value, 800, 2200);
			}
		}
	}

	private void Rest(Life life, int min, int max)
	{
		life.phase = Phase.Rest;
		life.until = BotScheduler.Now() + random.Next(min, max);
	}

	private void Sit(BotStateMachine bot)
	{
		if (bot.chr.chair <= 0)
		{
			bot.chr.stance = 6 + (bot.chr.stance & 1);
			bot.output.Sit(bot.chr, BotFurniture.RandomChair(random));
		}
	}

	private void Stand(BotStateMachine bot)
	{
		if (bot.chr.chair > 0)
		{
			bot.chr.stance = 4 + (bot.chr.stance & 1);
			bot.output.CancelChair(bot.chr);
		}
	}

	private void Fidget(BotStateMachine bot)
	{
		int num = random.Next(100);
		if (num < 18)
		{
			TalkIdle(bot);
		}
		else if (num < 28)
		{
			bot.output.Emote(bot.chr, random.Next(1, 23));
		}
		else if (num < 36)
		{
			BotRecordedActions.Turn(bot.chr, bot.chr.stance != 1 && bot.chr.stance != 3 && bot.chr.stance != 5);
		}
	}

	private void TalkSitting(BotStateMachine bot)
	{
		if (random.Next(100) < 28 && bot.marketDirector.TryChat(bot.chr))
		{
			bot.output.Speak(bot.chr, bot.marketFlavor.Chat());
		}
	}

	private void TalkIdle(BotStateMachine bot)
	{
		List<BotCharacter> list = new List<BotCharacter>();
		BotCharacter chr = bot.chr;
		foreach (BotCharacter item in BotRegistry.OnMap(chr.mapId))
		{
			if (item.id != chr.id && Math.Abs(item.x - chr.x) <= 220f && Math.Abs(item.y - chr.y) <= 80f)
			{
				list.Add(item);
			}
		}
		BotCharacter botCharacter = ((list.Count == 0) ? null : list[random.Next(list.Count)]);
		if (bot.marketDirector.TryChat(chr))
		{
			bot.output.Speak(chr, bot.marketFlavor.Chat());
			if (botCharacter != null && bot.marketDirector.TryChat(botCharacter))
			{
				bot.output.Speak(botCharacter, bot.marketFlavor.Reply());
			}
		}
	}

	public string CurrentPlatform(BotCharacter actor)
	{
		string result = null;
		int num = int.MaxValue;
		foreach (string item in platforms.ListPlatformIds(actor.mapId))
		{
			Platform platform = platforms.ParsePlatform(actor.mapId, item);
			if (platform.sortedPoints.Count == 0 || actor.x < (float)(platform.minX - 20) || actor.x > (float)(platform.maxX + 20))
			{
				continue;
			}
			int num2 = Math.Abs((int)actor.y - platform.GetYAtX((int)actor.x));
			if (num2 <= 10)
			{
				int num3 = num2 * 10 + Math.Abs((int)actor.x - (platform.minX + platform.maxX) / 2) / 10;
				if (num3 < num)
				{
					num = num3;
					result = item;
				}
			}
		}
		return result;
	}

	private List<string> MainPlatforms(int mapId)
	{
		return platforms.ListPlatformIds(mapId).FindAll((string id) => id.StartsWith("m", StringComparison.Ordinal));
	}

	private Platform Current(BotCharacter actor)
	{
		string text = CurrentPlatform(actor);
		List<string> list = MainPlatforms(actor.mapId);
		try
		{
			return platforms.ParsePlatform(actor.mapId, text ?? ((list.Count > 0) ? list[0] : "m1"));
		}
		catch (Exception)
		{
			return null;
		}
	}

	public BotPoint StrollTarget(Platform platform, int x, int direction)
	{
		int num = random.Next(120, 281);
		int num2 = ((direction >= 0) ? 1 : (-1));
		if (platform == null)
		{
			return new BotPoint(x + num2 * num, 0);
		}
		int num3 = platform.minX + 24;
		int num4 = platform.maxX - 24;
		if (num4 <= num3)
		{
			return new BotPoint(x, platform.GetYAtX(x));
		}
		if (x + num2 * num > num4 || x + num2 * num < num3)
		{
			num2 = -num2;
		}
		int x2 = Math.Max(num3, Math.Min(num4, x + num2 * num));
		return new BotPoint(x2, platform.GetYAtX(x2));
	}

	private void Stroll(BotStateMachine bot, Life life, bool jump)
	{
		BotCharacter actor = bot.chr;
		Stand(bot);
		BotPoint dest = StrollTarget(Current(actor), (int)actor.x, life.direction);
		life.direction = (((float)dest.x >= actor.x) ? 1 : (-1));
		life.phase = Phase.Walk;
		life.until = BotScheduler.Now() + 8000;
		int mapId = actor.mapId;
		bot.marketDirector.RunPathfind(() =>
		{
			if (BotStore.GetBotById(actor.id) == bot && bot.running && actor.mapId == mapId)
			{
				if (jump)
				{
					BotRecordedActions.Jump(actor);
				}
				if (!BotRecordedActions.WalkAlongX(actor, dest.x, 18, () =>
				{
					Rest(life, 900, 2800);
				}))
				{
					Rest(life, 900, 2800);
				}
			}
		});
	}

	private void Climb(BotStateMachine bot, Life life)
	{
		BotCharacter actor = bot.chr;
		Stand(bot);
		List<string> list = MainPlatforms(actor.mapId);
		if (list.Count < 2)
		{
			Stroll(bot, life, jump: false);
			return;
		}
		string current = CurrentPlatform(actor);
		List<string> list2 = list.FindAll((string id) => id != current);
		if (list2.Count == 0)
		{
			list2 = list;
		}
		BotPoint dest;
		try
		{
			Platform platform = platforms.ParsePlatform(actor.mapId, list2[random.Next(list2.Count)]);
			int x = platform.minX + 40 + random.Next(Math.Max(1, platform.Width - 80));
			dest = new BotPoint(x, platform.GetYAtX(x));
		}
		catch (Exception)
		{
			Stroll(bot, life, jump: false);
			return;
		}
		life.phase = Phase.Climb;
		life.until = BotScheduler.Now() + 16000;
		int mapId = actor.mapId;
		bot.marketDirector.RunPathfind(() =>
		{
			BeginClimb(bot, life, actor, mapId, dest);
		});
	}

	private void BeginClimb(BotStateMachine bot, Life life, BotCharacter actor, int mapId, BotPoint dest)
	{
		if (BotStore.GetBotById(actor.id) != bot || !bot.running || actor.mapId != mapId || life.phase != Phase.Climb || actor.trade != null)
		{
			return;
		}
		if (!BotMovementService.PrepareNavigation(actor))
		{
			BotTasks.Schedule(() =>
			{
				BeginClimb(bot, life, actor, mapId, dest);
			}, 100L, "BeginClimb", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Social/MarketBotAmbient.cs");
			return;
		}
		BotMovementService.Enable(actor);
		BotMovementService.Move(actor, dest.x, dest.y, () =>
		{
			Rest(life, 1800, 4500);
		});
	}
}
