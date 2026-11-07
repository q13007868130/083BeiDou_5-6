using System;
using System.Collections.Generic;

namespace Server.Bot.Types.Blackjack;

public sealed class Deck
{
	private static readonly char[] Suits = new char[4] { 'H', 'D', 'C', 'S' };

	private static readonly char[] Ranks = new char[13]
	{
		'2', '3', '4', '5', '6', '7', '8', '9', 'T', 'J',
		'Q', 'K', 'A'
	};

	private readonly Random random;

	private readonly List<string> cards;

	public int Remaining => cards.Count;

	public Deck(Random random = null)
	{
		this.random = random ?? new Random();
		cards = CreateAndShuffle();
	}

	private List<string> CreateAndShuffle()
	{
		List<string> list = new List<string>(52);
		char[] suits = Suits;
		foreach (char c in suits)
		{
			char[] ranks = Ranks;
			foreach (char c2 in ranks)
			{
				list.Add(new string(new char[2] { c2, c }));
			}
		}
		for (int num = list.Count - 1; num > 0; num--)
		{
			int num2 = random.Next(num + 1);
			List<string> list2 = list;
			int i = num;
			List<string> list3 = list;
			int j = num2;
			string text = list[num2];
			string text2 = list[num];
			string text3 = (list2[i] = text);
			text3 = (list3[j] = text2);
		}
		return list;
	}

	public string Draw()
	{
		if (cards.Count == 0)
		{
			cards.AddRange(CreateAndShuffle());
		}
		string result = cards[0];
		cards.RemoveAt(0);
		return result;
	}

	public void Reshuffle()
	{
		cards.Clear();
		cards.AddRange(CreateAndShuffle());
	}
}
