using System;

public sealed class NativeWeatherService
{
	public bool Enabled = true;

	public bool InjectSky = true;

	public bool SeasonDrift = true;

	public long DayLength = 14400000L;

	public long ChangeInterval = 900000L;

	public long OverrideHold = 3600000L;

	public int RainbowDuration = 180;

	private readonly NativeWeatherRegion[] regions;

	private readonly int[] skies;

	private readonly long[] since;

	private readonly long[] rainbowUntil;

	private readonly uint[] tokens;

	private readonly Func<double> random;

	private long nextRoll;

	private long overrideUntil;

	private int overrideSky = -1;

	private int overrideMinute = -1;

	private int revision;

	public int Revision => revision;

	public long NextRollAt => nextRoll;

	public long OverrideUntil => overrideUntil;

	public int OverrideSky => overrideSky;

	public int OverrideMinute => overrideMinute;

	public int RegionCount => regions.Length;

	public void Touch()
	{
		revision++;
	}

	public NativeWeatherService(NativeWeatherRegion[] regions, long now, Func<double> random)
	{
		this.regions = regions;
		this.random = random;
		skies = new int[regions.Length];
		since = new long[regions.Length];
		rainbowUntil = new long[regions.Length];
		tokens = new uint[regions.Length];
		for (int i = 0; i < regions.Length; i++)
		{
			skies[i] = Math.Max(0, regions[i].Forced);
			since[i] = now;
			tokens[i] = NewToken();
		}
	}

	private uint NewToken()
	{
		return (uint)(random() * 4294967296.0);
	}

	private int Automatic(int r)
	{
		if (regions[r].Forced < 0)
		{
			return skies[r];
		}
		return regions[r].Forced;
	}

	private int Effective(int r)
	{
		if (overrideSky < 0)
		{
			return Automatic(r);
		}
		return overrideSky;
	}

	private void Transition(int r, int previous, int next, long now)
	{
		if (previous != next)
		{
			since[r] = now;
			tokens[r] = NewToken();
			revision++;
			rainbowUntil[r] = (((previous == 1 || previous == 4) && next != 1 && next != 4) ? (now + (long)RainbowDuration * 1000L) : 0);
		}
	}

	public void ClearOverride(long now)
	{
		if (overrideSky >= 0)
		{
			for (int i = 0; i < regions.Length; i++)
			{
				Transition(i, overrideSky, Automatic(i), now);
			}
		}
		overrideSky = (overrideMinute = -1);
		overrideUntil = 0L;
		nextRoll = 0L;
		revision++;
	}

	public NativeWeatherRegion Region(int i)
	{
		return regions[i];
	}

	public int SkyOf(int r)
	{
		return skies[r];
	}

	public int MinuteOfDay(long now)
	{
		if (overrideMinute < 0)
		{
			return (int)((now % DayLength + DayLength) % DayLength * 1440 / Math.Max(1L, DayLength));
		}
		return overrideMinute;
	}

	public float NightLevel(long now)
	{
		return NativeWeatherState.Night(MinuteOfDay(now));
	}

	public void RerollWind()
	{
		for (int i = 0; i < regions.Length; i++)
		{
			tokens[i] = NewToken();
		}
		revision++;
	}

	public void ApplyForced(int r, int previousEffective, long now)
	{
		Transition(r, previousEffective, Effective(r), now);
		revision++;
	}

	private void Expire(long now)
	{
		if (overrideUntil > 0 && overrideUntil <= now)
		{
			ClearOverride(now);
		}
	}

	public void Override(int? sky, int? minute, long duration, long now)
	{
		if (!sky.HasValue && !minute.HasValue)
		{
			throw new ArgumentException("Weather or time required");
		}
		Expire(now);
		for (int i = 0; i < regions.Length; i++)
		{
			Transition(i, Effective(i), sky ?? Automatic(i), now);
		}
		overrideSky = sky ?? (-1);
		overrideMinute = minute ?? (-1);
		overrideUntil = now + duration;
		revision++;
	}

	public bool Roll(long now, int month)
	{
		Expire(now);
		if (!Enabled || overrideSky >= 0 || now < nextRoll)
		{
			return false;
		}
		nextRoll = now + ChangeInterval;
		for (int i = 0; i < regions.Length; i++)
		{
			int num = regions[i].Forced;
			if (num < 0)
			{
				double[] array = (double[])regions[i].Weights.Clone();
				if (SeasonDrift)
				{
					ApplySeason(array, month);
				}
				double num2 = 0.0;
				double[] array2 = array;
				foreach (double val in array2)
				{
					num2 += Math.Max(0.0, val);
				}
				double num3 = random() * num2;
				num = 0;
				for (int k = 0; k < array.Length; k++)
				{
					num3 -= Math.Max(0.0, array[k]);
					if (num3 < 0.0)
					{
						num = k;
						break;
					}
				}
			}
			Transition(i, skies[i], num, now);
			skies[i] = num;
		}
		return true;
	}

	public void Sync(int map, bool snap, NativeWeatherState client, long now)
	{
		Expire(now);
		int num = RegionForMap(map);
		NativeWeatherRegion nativeWeatherRegion = regions[num];
		int num2 = (int)((overrideMinute >= 0) ? overrideMinute : ((now % DayLength + DayLength) % DayLength * 1440 / DayLength));
		int flags = (snap ? 1 : 0) | ((overrideMinute >= 0 || !Enabled) ? 2 : 0) | (InjectSky ? 4 : 0) | ((!Enabled) ? 8 : 0);
		int num3 = (int)Math.Min(2147483647L, Math.Max(0L, now - since[num]));
		client.Set(Enabled ? num2 : 720, (int)Math.Max(1L, DayLength / 1440), Enabled ? Effective(num) : 0, flags, num3 / 1000, nativeWeatherRegion.Tint, (int)Math.Min(32767L, Math.Max(0L, rainbowUntil[num] - now) / 1000), num, num3, tokens[num], now);
	}

	public static int RegionForMap(int map)
	{
		if (map < 0)
		{
			return 26;
		}
		switch (map / 1000000)
		{
		case 100:
			return 19;
		case 101:
			return 3;
		case 102:
			return 4;
		case 103:
			return 5;
		case 104:
			return 16;
		case 105:
			return 10;
		case 110:
			return 14;
		case 120:
			return 18;
		case 130:
			return 20;
		case 140:
			return 1;
		case 200:
			return 7;
		case 211:
			return 0;
		case 220:
		case 221:
		case 222:
			return 13;
		case 230:
			return 11;
		case 240:
			return 12;
		case 250:
		case 251:
			return 8;
		case 260:
			return 9;
		case 261:
			return 17;
		case 270:
			return 21;
		case 300:
			return 22;
		case 600:
		case 610:
			return 23;
		case 680:
			return 15;
		case 740:
		case 741:
		case 742:
			return 24;
		case 800:
			if (map != 800000000)
			{
				return 25;
			}
			return 2;
		case 801:
			return 6;
		default:
			return 26;
		}
	}

	public static void ApplySeason(double[] w, int month)
	{
		if (month == 12 || month <= 2)
		{
			w[2] *= 1.8;
			w[5] *= 1.5;
			w[1] *= 0.7;
			w[6] *= 0.4;
			w[7] *= 0.3;
		}
		else if (month <= 5)
		{
			w[7] *= 1.8;
			w[1] *= 1.2;
			w[2] *= 0.5;
			w[5] *= 0.4;
		}
		else if (month <= 8)
		{
			w[1] *= 1.4;
			w[4] *= 1.3;
			w[2] *= 0.3;
			w[5] *= 0.2;
			w[6] *= 0.6;
		}
		else
		{
			w[6] *= 1.8;
			w[3] *= 1.2;
			w[7] *= 0.4;
			w[2] *= 0.7;
		}
	}
}
