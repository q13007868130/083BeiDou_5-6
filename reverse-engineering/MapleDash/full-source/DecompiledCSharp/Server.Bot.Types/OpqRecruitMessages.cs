using System;
using System.Text;

namespace Server.Bot.Types;

public static class OpqRecruitMessages
{
	private static readonly string[] Prefixes = new string[6] { "J>", "Joining>", "J>>", "Join>", "LFP>", "lf>" };

	private static readonly string[] PqNames = new string[6] { "OPQ", "Orbis PQ", "Orbis Party Quest", "pq", "Orbis", "OPQ please" };

	private static readonly string[] Fillers = new string[4] { "@@@@@@@@", "!!!", "plz", "asap" };

	public static string Generate(BotActor chr, Random random = null)
	{
		if (random == null)
		{
			random = new Random();
		}
		string value = Prefixes[random.Next(Prefixes.Length)];
		string value2 = PqNames[random.Next(PqNames.Length)];
		StringBuilder stringBuilder = new StringBuilder();
		if (chr != null && random.NextDouble() < 0.35)
		{
			stringBuilder.Append("Lvl ").Append(chr.level).Append(' ')
				.Append(JobName(chr.job))
				.Append(' ');
		}
		stringBuilder.Append(value).Append(' ').Append(value2);
		int num = random.Next(3);
		for (int i = 0; i < num; i++)
		{
			stringBuilder.Append(' ').Append(Fillers[random.Next(Fillers.Length)]);
		}
		string text = stringBuilder.ToString().Replace("[", "").Replace("]", "");
		if (!(random.NextDouble() < 0.15))
		{
			return text;
		}
		return text.ToUpperInvariant();
	}

	private static string JobName(int job)
	{
		if (job <= 0)
		{
			return "Beginner";
		}
		return (job / 100 % 10) switch
		{
			1 => "Warrior", 
			2 => "Magician", 
			3 => "Bowman", 
			4 => "Thief", 
			5 => "Pirate", 
			_ => "Beginner", 
		};
	}
}
