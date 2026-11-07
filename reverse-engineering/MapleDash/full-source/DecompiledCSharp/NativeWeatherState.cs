using System;

public sealed class NativeWeatherState
{
	private long stamped;

	private long age;

	private long rainbow;

	private long last;

	private bool hasState;

	public double Minute { get; private set; } = 720.0;

	public int MsPerMinute { get; private set; } = 10000;

	public int Sky { get; private set; }

	public bool Frozen { get; private set; }

	public bool BareSky { get; private set; }

	public bool Disabled { get; private set; }

	public int Tint { get; private set; } = 4872844;

	public int Palette { get; private set; } = 26;

	public bool HasPalette { get; private set; }

	public uint Token { get; private set; }

	public int Revision { get; private set; }

	public bool Snap { get; private set; }

	public void LeaveField()
	{
		Sky = 0;
		Revision++;
	}

	private static int Int(byte[] b, ref int p)
	{
		int result = b[p] | (b[p + 1] << 8) | (b[p + 2] << 16) | (b[p + 3] << 24);
		p += 4;
		return result;
	}

	public bool Read(byte[] packet, int offset, long now)
	{
		if (packet == null || offset < 0 || offset > packet.Length - 10)
		{
			return false;
		}
		int num = offset + 2;
		int minute = packet[num] | (packet[num + 1] << 8);
		num += 2;
		int ms = Int(packet, ref num);
		int sky = packet[num++];
		int flags = packet[num++];
		int seconds = ((packet.Length - num >= 4) ? Math.Max(0, Int(packet, ref num)) : 0);
		int tint = 0;
		if (packet.Length - num >= 3)
		{
			tint = (packet[num] << 16) | (packet[num + 1] << 8) | packet[num + 2];
			num += 3;
		}
		int rainbowSeconds = 0;
		if (packet.Length - num >= 2)
		{
			rainbowSeconds = Math.Max(0, (int)(short)(packet[num] | (packet[num + 1] << 8)));
			num += 2;
		}
		int palette = ((packet.Length - num >= 1) ? packet[num++] : (-1));
		int elapsedMs = ((packet.Length - num >= 4) ? Int(packet, ref num) : (-1));
		uint token = ((packet.Length - num >= 4) ? ((uint)Int(packet, ref num)) : 0u);
		Set(minute, ms, sky, flags, seconds, tint, rainbowSeconds, palette, elapsedMs, token, now);
		return true;
	}

	public void Set(int minute, int ms, int sky, int flags, int seconds, int tint, int rainbowSeconds, int palette, int elapsedMs, uint token, long now)
	{
		Disabled = (flags & 8) != 0;
		if (Disabled)
		{
			minute = 720;
			sky = 0;
			flags |= 2;
			rainbowSeconds = 0;
		}
		Minute = ((minute >= 0 && minute < 1440) ? minute : 0);
		MsPerMinute = Math.Max(100, ms);
		Sky = ((sky >= 0 && sky < 9) ? sky : 0);
		Frozen = (flags & 2) != 0;
		BareSky = (flags & 4) != 0;
		Snap = (flags & 1) != 0;
		age = ((elapsedMs >= 0) ? elapsedMs : Math.Min(2147483647L, (long)Math.Max(0, seconds) * 1000L));
		if (elapsedMs >= 0)
		{
			Token = token;
		}
		if (tint != 0)
		{
			Tint = tint;
		}
		if (palette >= 0 && palette < 27)
		{
			Palette = palette;
			HasPalette = true;
		}
		rainbow = Math.Max(0, rainbowSeconds);
		stamped = now;
		hasState = true;
		Revision++;
	}

	public void Tick(long now)
	{
		long num = now - last;
		if (last != 0L && num >= 0 && num < 60000 && !Frozen)
		{
			Minute = (Minute + (double)num / (double)MsPerMinute) % 1440.0;
		}
		last = now;
	}

	public int Age(long now)
	{
		if (hasState)
		{
			return (int)Math.Min(2147483647L, age + Math.Max(0L, now - stamped));
		}
		return 0;
	}

	public int Rainbow(long now)
	{
		if (hasState)
		{
			return (int)Math.Max(0L, rainbow - Math.Max(0L, now - stamped) / 1000);
		}
		return 0;
	}

	public static float Night(double minute)
	{
		if (!(minute < 300.0) && !(minute >= 1140.0))
		{
			if (!(minute < 420.0))
			{
				if (!(minute < 1020.0))
				{
					return (float)(minute - 1020.0) / 120f;
				}
				return 0f;
			}
			return 1f - (float)(minute - 300.0) / 120f;
		}
		return 1f;
	}
}
