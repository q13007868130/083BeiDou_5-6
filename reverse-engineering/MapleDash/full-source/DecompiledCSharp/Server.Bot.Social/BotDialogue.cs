using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Server.Bot.Social;

public sealed class BotDialogue
{
	public sealed class Dialogue
	{
		public List<string> text;

		public List<int> emotes;

		public long durationMs;
	}

	private readonly BotResources resources;

	private readonly Random random;

	public BotDialogue(BotResources resources, Random random = null)
	{
		this.resources = resources;
		this.random = random ?? new Random();
	}

	public Dialogue Get(string pack, string type, string node)
	{
		if (!(resources.ReadData("soloMapling/ArtificialPlayer/BotDialoguePack/" + pack) is Dictionary<string, object> dictionary) || !dictionary.TryGetValue(type, out var value) || !(value is Dictionary<string, object> dictionary2) || !dictionary2.TryGetValue(node, out var value2) || !(value2 is Dictionary<string, object> dictionary3))
		{
			return null;
		}
		dictionary3.TryGetValue("text", out var value3);
		dictionary3.TryGetValue("emote", out var value4);
		dictionary3.TryGetValue("wait", out var value5);
		Dialogue dialogue = new Dialogue();
		List<object> obj = value3 as List<object>;
		dialogue.text = ((obj != null) ? obj.Select((object x) => x as string).ToList() : null);
		dialogue.emotes = ((value4 is List<object> source) ? source.Select(ToInt).ToList() : new List<int> { ToInt(value4) });
		dialogue.durationMs = ToInt(value5) * 1000;
		return dialogue;
	}

	private static int ToInt(object value)
	{
		if (!(value is long num))
		{
			if (value is int)
			{
				return (int)value;
			}
			if (!(value is string s) || !int.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result))
			{
				return 0;
			}
			return result;
		}
		return (int)num;
	}

	public static List<string> ReplaceStrings(List<string> input, IDictionary<string, string> replacements)
	{
		if (input == null || replacements == null)
		{
			throw new ArgumentException("Input list and replacements map cannot be null");
		}
		List<string> list = new List<string>();
		foreach (string item in input)
		{
			string text = item;
			foreach (KeyValuePair<string, string> replacement in replacements)
			{
				text = text.Replace(replacement.Key, replacement.Value);
			}
			list.Add(text);
		}
		return list;
	}

	public string RandomLine(string pack, string type, string node)
	{
		Dialogue dialogue = Get(pack, type, node);
		if (dialogue?.text != null && dialogue.text.Count != 0)
		{
			return dialogue.text[random.Next(dialogue.text.Count)];
		}
		return null;
	}

	public int RandomEmote(string pack, string type, string node)
	{
		Dialogue dialogue = Get(pack, type, node);
		return dialogue?.emotes[random.Next(dialogue.emotes.Count)] ?? (-1);
	}

	public void Run(BotCharacter bot, Dialogue dialogue, BotOutput output, bool flavor, Action then = null, Func<bool> keepGoing = null)
	{
		if (dialogue == null)
		{
			then?.Invoke();
			return;
		}
		List<string> list = (flavor ? new List<string> { dialogue.text[random.Next(dialogue.text.Count)] } : dialogue.text);
		BotTiming.Chain chain = BotTiming.CreateChain().StopUnless(() => BotRegistry.Contains(bot.id) && (keepGoing == null || keepGoing()));
		for (int num = 0; num < list.Count; num++)
		{
			string text = list[num];
			chain.Run(() =>
			{
				output.Speak(bot, text);
			});
			if (num < list.Count - 1)
			{
				chain.Pause(5000L);
			}
		}
		chain.Run(() =>
		{
			output.Emote(bot, dialogue.emotes[random.Next(dialogue.emotes.Count)]);
		}).Pause(dialogue.durationMs).Run(then)
			.Start();
	}
}
