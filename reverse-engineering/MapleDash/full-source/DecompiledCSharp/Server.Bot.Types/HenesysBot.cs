using System;
using Server.Bot.Environment;
using Server.Bot.Social;
using Server.Field;

namespace Server.Bot.Types;

public sealed class HenesysBot : BotStateMachine
{
	public enum HenesysState
	{
		Reset,
		Idle,
		Wander,
		Emote,
		ChangeMap
	}

	private const int HenesysMain = 100000000;

	private const int HenesysMarket = 100000100;

	private const int HenesysPark = 100000200;

	private const int PetPark = 100000202;

	private static readonly int[,] Links = new int[4, 4]
	{
		{ 100000000, 23, 100000100, 14 },
		{ 100000000, 24, 100000200, 18 },
		{ 100000200, 19, 100000100, 15 },
		{ 100000200, 13, 100000202, 5 }
	};

	private const long MapChangeCooldownMs = 180000L;

	private const long JqConversionCooldownMs = 600000L;

	private readonly Random random;

	private readonly BotDialogue dialogue;

	private readonly BotFlavor flavor;

	private readonly bool mapRoaming;

	private long lastMapChangeTime;

	private long lastJqConversionTime;

	private int lifetime;

	public HenesysState henesysState { get; private set; }

	public void SetLastJqConversionTime(long value)
	{
		lastJqConversionTime = value;
	}

	public HenesysBot(BotCharacter actor, BotDialogue dialogue, BotFlavor flavor, bool mapRoaming, Random random = null)
		: base(actor)
	{
		this.dialogue = dialogue;
		this.flavor = flavor;
		this.mapRoaming = mapRoaming;
		this.random = random ?? new Random();
		botType = "HenesysBot";
	}

	public override bool IsAvailableForAmbientActions()
	{
		return state != State.TRADING;
	}

	public override void UpdateState()
	{
		base.UpdateState();
		if (!CheckIfNotRunningOrPaused() && chr != null && MapService.Get(chr.mapId) != null)
		{
			switch (henesysState)
			{
			case HenesysState.Reset:
				henesysState = HenesysState.Idle;
				break;
			case HenesysState.Idle:
				DecideNextAction();
				break;
			case HenesysState.Wander:
				WanderPlatforms();
				RandomEmote();
				RandomChat("WanderChat");
				henesysState = HenesysState.Idle;
				break;
			case HenesysState.Emote:
				RandomEmote();
				RandomChat("EmoteReaction");
				henesysState = HenesysState.Idle;
				break;
			case HenesysState.ChangeMap:
				RandomChat("MapTransition");
				ChangeMap();
				lastMapChangeTime = BotScheduler.Now();
				RandomEmote();
				EnsureRoaming();
				henesysState = HenesysState.Idle;
				break;
			}
		}
	}

	private void DecideNextAction()
	{
		long num = BotScheduler.Now();
		if (chr.mapId == 100000202 && num - lastJqConversionTime > 600000 && Chance(4))
		{
			random.Next();
			lastJqConversionTime = num;
			return;
		}
		flavor.MaybeExpress(this);
		if (mapRoaming && num - lastMapChangeTime > 180000 && Chance(10))
		{
			henesysState = HenesysState.ChangeMap;
		}
		else if (Chance(3))
		{
			henesysState = HenesysState.Wander;
		}
		else if (Chance(5))
		{
			henesysState = HenesysState.Emote;
		}
		else
		{
			EnsureRoaming();
		}
	}

	private bool Chance(int n)
	{
		return random.Next(n) == 0;
	}

	private void EnsureRoaming()
	{
		if (!BotMovementService.Observed(chr.mapId))
		{
			BotWander.Stop(chr);
		}
		else if (!BotWander.IsWandering(chr))
		{
			BotWander.Start(chr, banded: false, 0f, 0f);
		}
	}

	private void WanderPlatforms()
	{
		EnsureRoaming();
	}

	private void RandomEmote()
	{
		if (Chance(10))
		{
			output.Emote(chr, Chance(50) ? 2 : 3);
		}
	}

	private void RandomChat(string node)
	{
		if (Chance(20))
		{
			string text = Line(node);
			if (text != null)
			{
				output.Speak(chr, text);
			}
		}
	}

	private string Line(string node)
	{
		try
		{
			return dialogue.RandomLine("HenesysBotDialogue.yaml", "HenesysBot", node);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private void ChangeMap()
	{
		int mapId = chr.mapId;
		int[] array = new int[4];
		int num = 0;
		for (int i = 0; i < Links.GetLength(0); i++)
		{
			int num2 = Links[i, 0];
			int num3 = Links[i, 2];
			if (num2 == mapId)
			{
				array[num++] = num3;
			}
			else if (num3 == mapId)
			{
				array[num++] = num2;
			}
		}
		if (num == 0)
		{
			return;
		}
		double num4 = 0.0;
		double[] array2 = new double[num];
		for (int j = 0; j < num; j++)
		{
			array2[j] = 1.0 / (double)(1 + BotRegistry.OnMap(array[j]).Count);
			num4 += array2[j];
		}
		double num5 = random.NextDouble() * num4;
		double num6 = 0.0;
		int to = array[num - 1];
		for (int k = 0; k < num; k++)
		{
			num6 += array2[k];
			if (num5 < num6)
			{
				to = array[k];
				break;
			}
		}
		int num7 = PortalForRoute(mapId, to);
		if (num7 < 0)
		{
			return;
		}
		MapRuntime mapRuntime = MapService.Get(mapId);
		if (mapRuntime == null)
		{
			return;
		}
		MapPortal mapPortal = null;
		foreach (MapPortal portal in mapRuntime.portals)
		{
			if (portal.id == num7)
			{
				mapPortal = portal;
				break;
			}
		}
		if (mapPortal != null)
		{
			BotWander.Stop(chr);
			BotMovementService.Move(chr, mapPortal.x, mapPortal.y);
		}
	}

	private static int PortalForRoute(int from, int to)
	{
		for (int i = 0; i < Links.GetLength(0); i++)
		{
			if (Links[i, 0] == from && Links[i, 2] == to)
			{
				return Links[i, 1];
			}
			if (Links[i, 2] == from && Links[i, 0] == to)
			{
				return Links[i, 3];
			}
		}
		return -1;
	}

	public override void StopScheduledTask()
	{
		lifetime++;
		flavor.Forget(chr);
		BotWander.Stop(chr);
		BotMovementService.Disable(chr);
		base.StopScheduledTask();
	}
}
