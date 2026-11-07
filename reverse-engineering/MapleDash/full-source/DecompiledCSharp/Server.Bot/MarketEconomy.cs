using System;
using System.Collections.Generic;
using System.Globalization;

namespace Server.Bot;

public sealed class MarketEconomy
{
	private readonly Random random;

	private readonly Func<DateTime> localTime;

	private List<double> indices;

	public IReadOnlyList<double> MarketIndices
	{
		get
		{
			if (indices == null)
			{
				indices = new List<double>(12);
				for (int i = 0; i < 12; i++)
				{
					indices.Add(Math.Max(0.85, Math.Min(1.15, 1.0 + 0.14999999999999997 * Math.Sin(Math.PI / 6.0 * (double)i) + (random.NextDouble() - 0.5) * 0.05)));
				}
			}
			return indices;
		}
	}

	public double CurrentIndex => MarketIndices[localTime().Hour / 2 % 12];

	public MarketEconomy(Random random = null, Func<DateTime> localTime = null)
	{
		this.random = random ?? new Random();
		this.localTime = localTime ?? ((Func<DateTime>)(() => DateTime.Now));
	}

	public static int JavaInt(double value)
	{
		if (!double.IsNaN(value))
		{
			if (!(value >= 2147483647.0))
			{
				if (!(value <= -2147483648.0))
				{
					return (int)value;
				}
				return int.MinValue;
			}
			return int.MaxValue;
		}
		return 0;
	}

	public static bool IsHotRoom(int mapId)
	{
		if (mapId != 910000001 && mapId != 910000002)
		{
			return mapId == 910000007;
		}
		return true;
	}

	public static int RoomPremium(int mapId, int price)
	{
		return JavaInt((double)price * mapId switch
		{
			910000001 => 1.25, 
			910000002 => 1.2, 
			910000003 => 1.1, 
			910000007 => 1.15, 
			910000014 => 0.97, 
			910000015 => 0.95, 
			910000016 => 0.93, 
			910000017 => 0.91, 
			910000018 => 0.9, 
			910000019 => 0.88, 
			910000020 => 0.86, 
			910000021 => 0.84, 
			910000022 => 0.82, 
			_ => 1.0, 
		});
	}

	public static int DoorPremium(int mapId, int x, int y, int price)
	{
		if ((mapId >= 910000001 && mapId <= 910000006 && x >= 300 && x <= 640 && y >= -80 && y <= 34) || (mapId >= 910000007 && mapId <= 910000012 && x >= -709 && x <= -109 && y >= -100 && y <= 102))
		{
			return JavaInt((double)price * 1.2);
		}
		return price;
	}

	public static int DayPrice(int price, DayOfWeek day)
	{
		double num = price;
		double num2;
		switch (day)
		{
		default:
			num2 = 1.0;
			break;
		case DayOfWeek.Monday:
		case DayOfWeek.Tuesday:
			num2 = 0.95;
			break;
		case DayOfWeek.Sunday:
		case DayOfWeek.Friday:
		case DayOfWeek.Saturday:
			num2 = 1.1;
			break;
		}
		return JavaInt(num * num2);
	}

	public int RandomAdjustment(int price)
	{
		int num = random.Next(100) + 1;
		double num2;
		if (num <= 10)
		{
			num2 = 0.9;
		}
		else if (num <= 30)
		{
			num2 = 0.95;
		}
		else if (num <= 70)
		{
			num2 = 1.0;
		}
		else
		{
			num2 = ((num <= 90) ? 1.05 : 1.1);
		}
		return JavaInt((double)price * num2);
	}

	public int Price(int price, int? mapId = null, int? x = null, int? y = null)
	{
		price = JavaInt((double)price * CurrentIndex);
		price = DayPrice(price, localTime().DayOfWeek);
		price = RandomAdjustment(price);
		if (mapId.HasValue)
		{
			price = RoomPremium(mapId.Value, price);
		}
		if (mapId.HasValue && x.HasValue && y.HasValue)
		{
			price = DoorPremium(mapId.Value, x.Value, y.Value, price);
		}
		return Style(price);
	}

	public static int Quantity(int quantity, int mapId, int x, int y)
	{
		if (quantity != 1)
		{
			return DoorPremium(mapId, x, y, RoomPremium(mapId, quantity));
		}
		return 1;
	}

	public static int CleanNumber(int number)
	{
		int length = ((number == int.MinValue) ? int.MinValue : Math.Abs(number)).ToString(CultureInfo.InvariantCulture).Length;
		int num = ((length >= 6 && length <= 9) ? 2 : ((length == 10) ? 3 : 0));
		if (num == 0)
		{
			return number;
		}
		int.Parse(number.ToString(CultureInfo.InvariantCulture).Substring(0, num), CultureInfo.InvariantCulture);
		int num2 = (int)Math.Pow(10.0, length - num);
		return number / num2 * num2;
	}

	public static int TrailingDigit(int number)
	{
		char[] array = number.ToString(CultureInfo.InvariantCulture).ToCharArray();
		int num = array.Length - 1;
		while (num >= 0 && array[num] == '0')
		{
			num--;
		}
		if (num < 0)
		{
			return number;
		}
		for (int i = num + 1; i < array.Length; i++)
		{
			array[i] = array[num];
		}
		long num2 = long.Parse(new string(array), CultureInfo.InvariantCulture);
		if (num2 <= int.MaxValue)
		{
			if (num2 >= int.MinValue)
			{
				return (int)num2;
			}
			return int.MinValue;
		}
		return int.MaxValue;
	}

	public int Style(int number)
	{
		int num = random.Next(3);
		number = CleanNumber(number);
		return num switch
		{
			2 => TrailingDigit(number), 
			1 => number - 1, 
			_ => number, 
		};
	}

	public static string Shorthand(long price, int decimals = 1)
	{
		if (price < 1000)
		{
			return price.ToString(CultureInfo.InvariantCulture);
		}
		string[] array = new string[5] { "", "k", "m", "b", "t" };
		int num = 0;
		double num2 = price;
		while (num2 >= 1000.0 && num < array.Length - 1)
		{
			num2 /= 1000.0;
			num++;
		}
		if (decimals <= 0)
		{
			return Math.Floor(num2 + 0.5).ToString(CultureInfo.InvariantCulture) + array[num];
		}
		return decimal.Round((decimal)num2, decimals, MidpointRounding.AwayFromZero).ToString("F" + decimals, CultureInfo.InvariantCulture).TrimEnd('0')
			.TrimEnd('.') + array[num];
	}

	public string TierForRoom(int mapId)
	{
		int num;
		int num2;
		if (IsHotRoom(mapId))
		{
			num = 85;
			num2 = 13;
		}
		else if (mapId == 910000003)
		{
			num = 60;
			num2 = 35;
		}
		else if ((mapId >= 910000004 && mapId <= 910000006) || (mapId >= 910000008 && mapId <= 910000012))
		{
			num = 25;
			num2 = 60;
		}
		else
		{
			switch (mapId)
			{
			case 910000013:
				num = 33;
				num2 = 33;
				break;
			case 910000014:
			case 910000015:
			case 910000016:
			case 910000017:
				num = 15;
				num2 = 30;
				break;
			default:
				if (mapId >= 910000018 && mapId <= 910000022)
				{
					num = 20;
					num2 = 40;
				}
				else
				{
					num = 25;
					num2 = 55;
				}
				break;
			}
		}
		int num3 = random.Next(100);
		if (num3 >= num)
		{
			if (num3 >= num + num2)
			{
				return "B";
			}
			return "A";
		}
		return "S";
	}

	public static int WzPriceByJob(int price, int jobStyle)
	{
		return JavaInt((double)JavaInt((double)price * 1.5) * jobStyle switch
		{
			500 => 1.25, 
			400 => 1.5, 
			300 => 1.3, 
			200 => 1.35, 
			100 => 1.4, 
			_ => 1.3, 
		});
	}
}
