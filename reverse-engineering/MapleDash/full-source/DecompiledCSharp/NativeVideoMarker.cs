public static class NativeVideoMarker
{
	private static bool Pixel(byte[] p, int i, int r, int g, int b)
	{
		if (p[i * 4] == r && p[i * 4 + 1] == g && p[i * 4 + 2] == b)
		{
			return p[i * 4 + 3] == byte.MaxValue;
		}
		return false;
	}

	public static int Decode(byte[] rgba, int width, int height)
	{
		if (width < 7 || width > 8 || height < 5 || height > 8 || rgba == null || rgba.Length < width * height * 4)
		{
			return -1;
		}
		int num = rgba[16];
		int num2 = rgba[17];
		int num3 = rgba[18];
		if (Pixel(rgba, 0, 34, 17, 68) && Pixel(rgba, 1, 68, 85, 119) && Pixel(rgba, 2, 153, 170, 187) && Pixel(rgba, 3, 204, 221, 221))
		{
			if (num % 17 != 0 || num < 17 || num > 238)
			{
				return -1;
			}
			return num / 17;
		}
		if (Pixel(rgba, 0, 17, 34, 68) && Pixel(rgba, 1, 85, 102, 119) && Pixel(rgba, 2, 136, 153, 170) && Pixel(rgba, 3, 187, 204, 238))
		{
			if (num % 17 != 0 || num < 17 || num2 % 17 != 0 || num3 != 221)
			{
				return -1;
			}
			switch (num2)
			{
			case 68:
				return 14 + num / 17;
			case 85:
				if (num <= 68)
				{
					return 29 + num / 17;
				}
				break;
			}
		}
		if (Pixel(rgba, 0, 51, 85, 119) && Pixel(rgba, 1, 102, 136, 153) && Pixel(rgba, 2, 170, 187, 204) && Pixel(rgba, 3, 221, 238, 255))
		{
			if (num % 17 != 0 || num < 17 || num > 68 || num2 != 136 || num3 != 238)
			{
				return -1;
			}
			return 33 + num / 17;
		}
		return -1;
	}
}
