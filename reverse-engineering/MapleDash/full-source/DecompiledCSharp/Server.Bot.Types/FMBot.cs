using System;
using Server.Bot.Environment;
using Server.Bot.Social;

namespace Server.Bot.Types;

public sealed class FMBot : BotStateMachine
{
	public enum MarketState
	{
		Reset,
		Ambient,
		NavToRoom,
		ProcessRoom,
		BrowsingRoom,
		Action,
		EnterShop,
		ProcessShop,
		ExitShop,
		ExitRoom
	}

	private readonly MarketBotAmbient ambient;

	private readonly Random random;

	public MarketState marketState { get; private set; }

	public int Room { get; private set; } = 1;

	public override bool UsesSharedMarketTick => true;

	public FMBot(BotCharacter actor, MarketBotAmbient ambient, Random random = null)
		: base(actor)
	{
		this.ambient = ambient;
		this.random = random ?? new Random();
		botType = "FMBot";
	}

	public override bool IsAvailableForAmbientActions()
	{
		return state != State.TRADING;
	}

	public override void UpdateState()
	{
		base.UpdateState();
		if (CheckIfNotRunningOrPaused() || state == State.TRADING)
		{
			return;
		}
		ambient.Act(this);
		if (chr.chair <= 0 && !IsBusy)
		{
			if (marketState == MarketState.Reset)
			{
				Room = ((chr.mapId == 910000000) ? random.Next(1, 13) : (chr.mapId - 910000000));
				marketState = MarketState.Ambient;
			}
			else
			{
				marketState = MarketState.Ambient;
			}
		}
	}

	public override void DisplayCommands(BotActor player)
	{
		output.ShowOptions(player, new string[1] { chr.name });
	}

	public static bool ShouldPurchaseItem(int? askingPrice, int? marketValue, Random random = null)
	{
		if (!askingPrice.HasValue || !marketValue.HasValue || askingPrice <= 0 || marketValue <= 0)
		{
			return false;
		}
		double num = (double)askingPrice.Value / (double)marketValue.Value;
		double[] array = new double[8] { 0.7, 0.85, 0.95, 1.0, 1.05, 1.1, 1.15, 1.2 };
		double[] array2 = new double[8] { 1.0, 0.95, 0.85, 0.75, 0.55, 0.35, 0.15, 0.05 };
		if (num > 1.2)
		{
			return false;
		}
		for (int i = 0; i < array.Length; i++)
		{
			if (num <= array[i])
			{
				return (random ?? new Random()).NextDouble() < array2[i];
			}
		}
		return false;
	}

	public override void StopScheduledTask()
	{
		ambient.Forget(chr);
		BotMovementService.Disable(chr);
		base.StopScheduledTask();
	}
}
