using System;

namespace Server.Bot.Social;

public static class BotDealerDice
{
	public enum BetType
	{
		Cho,
		Han,
		None
	}

	public const int DoubleDownSkillId = 5320007;

	public const int SkillLevel = 5;

	public const int DiceSides = 6;

	private static readonly Random fallback = new Random();

	public static int[] Roll(BotCharacter dealer, Random random = null, BotOutput output = null, bool announce = true)
	{
		Random random2 = random ?? fallback;
		int num = random2.Next(6) + 1;
		int num2 = random2.Next(6) + 1;
		if (announce && dealer != null)
		{
			try
			{
				(output ?? BotTypeManager.Output).Speak(dealer, AnnounceText(num, num2));
			}
			catch (Exception)
			{
			}
		}
		return new int[2] { num, num2 };
	}

	public static string AnnounceText(int dice1, int dice2)
	{
		return "test " + dice1 + " " + dice2;
	}

	public static string ResultText(int[] rolls)
	{
		if (rolls == null || rolls.Length < 2)
		{
			return "Result: -";
		}
		bool flag = Sum(rolls) % 2 != 0;
		return "Result: [" + rolls[0] + "] [" + rolls[1] + "] - " + (flag ? "Han" : "Cho");
	}

	public static int Sum(int[] rolls)
	{
		if (rolls == null || rolls.Length < 2)
		{
			return 0;
		}
		return rolls[0] + rolls[1];
	}

	public static bool IsOdd(int[] rolls)
	{
		return Sum(rolls) % 2 != 0;
	}

	public static bool PlayerWins(BetType bet, int[] rolls)
	{
		bool flag = IsOdd(rolls);
		return bet switch
		{
			BetType.Han => flag, 
			BetType.Cho => !flag, 
			_ => false, 
		};
	}

	public static string BetAnnouncement(string playerName, BetType bet)
	{
		return playerName + " bets on " + bet.ToString().ToUpperInvariant() + "!";
	}
}
