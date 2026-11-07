using System;
using System.Collections.Generic;
using Server.Bot.Environment;

namespace Server.Bot.Social;

public sealed class BotFlavor
{
	public enum Action
	{
		Emote,
		BuffFlex,
		SkillSwing
	}

	private readonly Random random;

	private readonly Dictionary<int, long> cooldown = new Dictionary<int, long>();

	private static readonly int[] friendly = new int[4] { 1, 2, 5, 6 };

	public double Chance = 0.12;

	public long CooldownMinMs = 15000L;

	public long CooldownMaxMs = 40000L;

	public BotFlavor(Random random = null)
	{
		this.random = random ?? new Random();
	}

	public void MaybeExpress(BotStateMachine bot)
	{
		if (bot != null && bot.chr != null && bot.state != BotStateMachine.State.TRADING && bot.CheckMainPlayersOnMap())
		{
			long num = BotScheduler.Now();
			if ((!cooldown.TryGetValue(bot.chr.id, out var value) || num >= value) && !(random.NextDouble() >= Chance))
			{
				Force(bot, Pick());
				cooldown[bot.chr.id] = num + CooldownMinMs + (long)(random.NextDouble() * (double)Math.Max(1L, CooldownMaxMs - CooldownMinMs));
			}
		}
	}

	public void Forget(BotCharacter actor)
	{
		if (actor != null)
		{
			cooldown.Remove(actor.id);
		}
	}

	public Action Pick()
	{
		int num = random.Next(11);
		if (num >= 5)
		{
			if (num >= 8)
			{
				return Action.SkillSwing;
			}
			return Action.BuffFlex;
		}
		return Action.Emote;
	}

	public void Force(BotStateMachine bot, Action action)
	{
		if (bot?.chr == null)
		{
			return;
		}
		if (action != Action.SkillSwing)
		{
			bot.output.Emote(bot.chr, friendly[random.Next(friendly.Length)]);
			return;
		}
		BotMovementRuntime botMovementRuntime = BotMovementService.Get(bot.chr);
		if (botMovementRuntime != null)
		{
			botMovementRuntime.alertedUntilMs = BotScheduler.Now() + 5000;
		}
		BotAttack.BasicSwing(bot.chr, random, bot.output);
	}
}
