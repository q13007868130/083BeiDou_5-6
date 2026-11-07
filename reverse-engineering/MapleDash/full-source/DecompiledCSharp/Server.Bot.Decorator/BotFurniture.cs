using System;

namespace Server.Bot.Decorator;

public static class BotFurniture
{
	private static readonly int[] chairs = new int[61]
	{
		3010000, 3010001, 3010002, 3010003, 3010004, 3010005, 3010006, 3010007, 3010008, 3010009,
		3010010, 3010011, 3010012, 3010013, 3010014, 3010015, 3010016, 3010017, 3010018, 3010019,
		3010022, 3010023, 3010024, 3010025, 3010026, 3010028, 3010040, 3010041, 3010043, 3010045,
		3010046, 3010047, 3010057, 3010058, 3010060, 3010061, 3010062, 3010063, 3010064, 3010065,
		3010066, 3010067, 3010069, 3010071, 3010072, 3010073, 3010080, 3010081, 3010082, 3010083,
		3010084, 3010085, 3010092, 3010098, 3010099, 3010106, 3010111, 3010116, 3011000, 3012010,
		3012011
	};

	private static readonly int[] permits = new int[6] { 5140000, 5140001, 5140002, 5140003, 5140004, 5140006 };

	public static int RandomChair(Random random)
	{
		return chairs[random.Next(chairs.Length)];
	}

	public static int RandomStorePermit(Random random)
	{
		int num = random.Next(100);
		if (num >= 85)
		{
			return permits[1 + (num - 85) / 3];
		}
		return permits[0];
	}
}
