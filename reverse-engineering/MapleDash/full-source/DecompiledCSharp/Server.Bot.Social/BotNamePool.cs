using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Bot.Social;

public sealed class BotNamePool
{
	private static readonly string[] fallback = new string[32]
	{
		"小枫", "阿糖", "云鹿", "星橙", "枫眠", "南茶", "北雪", "青竹", "白舟", "洛安",
		"夏宁", "秋澈", "雨遥", "风然", "月梨", "川豆", "阿禾", "晚晴", "疏影", "清欢",
		"拾光", "半夏", "听雨", "折柳", "顾南", "沈夜", "林深", "江晚", "苏晚", "陈舟",
		"陆青", "叶安"
	};

	private readonly BotResources resources;

	private readonly Random random;

	private readonly List<string> assigned = new List<string>();

	private List<string> pool;

	private int index;

	public BotNamePool(BotResources resources, Random random = null)
	{
		this.resources = resources;
		this.random = random ?? new Random();
	}

	public string CharacterName()
	{
		string text = Next();
		assigned.Add(text);
		return text;
	}

	public string ShopOwnerName()
	{
		if (assigned.Count <= 0 || random.Next(100) >= 35)
		{
			return Next();
		}
		return assigned[random.Next(assigned.Count)];
	}

	public string Next()
	{
		if (pool == null || pool.Count == 0 || index >= pool.Count)
		{
			pool = new List<string>();
			index = 0;
			using (StringReader stringReader = new StringReader(resources.ReadText("soloMapling/FreeMarket/FMNameDesc/randomRealMaplestoryIGNs.txt") ?? ""))
			{
				string text;
				while ((text = stringReader.ReadLine()) != null)
				{
					if (text.Length > 0 && text.Length <= 12 && !HasLatin(text))
					{
						pool.Add(text);
					}
				}
			}
			if (pool.Count == 0)
			{
				pool.AddRange(fallback);
			}
			for (int num = pool.Count; num > 1; num--)
			{
				int num2 = random.Next(num);
				string value = pool[num - 1];
				pool[num - 1] = pool[num2];
				pool[num2] = value;
			}
		}
		return pool[index++];
	}

	private static bool HasLatin(string value)
	{
		foreach (char c in value)
		{
			if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
			{
				return true;
			}
		}
		return false;
	}
}
