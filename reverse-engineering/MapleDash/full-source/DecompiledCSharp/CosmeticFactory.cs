using System.Collections.Generic;
using UnityEngine;

public static class CosmeticFactory
{
	private const int N = 96;

	private static readonly Dictionary<string, Sprite> sprCache = new Dictionary<string, Sprite>();

	private static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();

	public static readonly Dictionary<string, Color> HairColors = new Dictionary<string, Color>
	{
		{
			"black",
			new Color(0.13f, 0.13f, 0.16f)
		},
		{
			"brown",
			new Color(0.47f, 0.29f, 0.15f)
		},
		{
			"blonde",
			new Color(0.95f, 0.82f, 0.42f)
		},
		{
			"blue",
			new Color(0.32f, 0.52f, 0.95f)
		},
		{
			"pink",
			new Color(0.96f, 0.58f, 0.8f)
		},
		{
			"green",
			new Color(0.42f, 0.82f, 0.47f)
		}
	};

	public static readonly Dictionary<string, Color> OutfitColors = new Dictionary<string, Color>
	{
		{
			"none",
			new Color(0.85f, 0.86f, 0.9f)
		},
		{
			"red",
			new Color(0.86f, 0.28f, 0.3f)
		},
		{
			"blue",
			new Color(0.28f, 0.48f, 0.9f)
		},
		{
			"purple",
			new Color(0.64f, 0.36f, 0.88f)
		},
		{
			"white",
			new Color(0.94f, 0.95f, 0.99f)
		},
		{
			"gold",
			new Color(0.93f, 0.76f, 0.3f)
		}
	};

	public static Sprite DyeHair(Sprite src, Color hair)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_029f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0307: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_031a: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)src == (Object)null || (Object)(object)src.texture == (Object)null)
		{
			return null;
		}
		string key = "h" + ((Object)src.texture).GetInstanceID() + "_" + CKey(hair);
		if (sprCache.TryGetValue(key, out var value) && (Object)(object)value != (Object)null)
		{
			return value;
		}
		int width = ((Texture)src.texture).width;
		int height = ((Texture)src.texture).height;
		Color32[] pixels;
		try
		{
			pixels = src.texture.GetPixels32();
		}
		catch
		{
			return null;
		}
		if (!Bounds(pixels, width, height, out var x, out var x2, out var y, out var y2))
		{
			return null;
		}
		int num = y2 - y + 1;
		int num2 = Mathf.Max(1, Mathf.RoundToInt((float)num * 0.12f));
		if (!Dominant(pixels, width, height, y2 - num2, y2, out var c))
		{
			return null;
		}
		bool[] array = new bool[width * height];
		Queue<int> queue = new Queue<int>();
		int num3 = y2 - Mathf.RoundToInt((float)num * 0.62f);
		for (int i = Mathf.Max(y2 - num2 * 2, num3); i <= y2; i++)
		{
			for (int j = x; j <= x2; j++)
			{
				int num4 = i * width + j;
				if (pixels[num4].a > 60 && !array[num4] && Near(pixels[num4], c, 0.34f))
				{
					array[num4] = true;
					queue.Enqueue(num4);
				}
			}
		}
		while (queue.Count > 0)
		{
			int num5 = queue.Dequeue();
			int num6 = num5 % width;
			int num7 = num5 / width;
			for (int k = 0; k < 4; k++)
			{
				int num8 = num6 + k switch
				{
					1 => -1, 
					0 => 1, 
					_ => 0, 
				};
				int num9 = num7 + k switch
				{
					3 => -1, 
					2 => 1, 
					_ => 0, 
				};
				if (num8 >= x && num8 <= x2 && num9 >= num3 && num9 <= y2)
				{
					int num10 = num9 * width + num8;
					if (!array[num10] && pixels[num10].a > 60 && Near(pixels[num10], c, 0.34f))
					{
						array[num10] = true;
						queue.Enqueue(num10);
					}
				}
			}
		}
		Color32[] array2 = new Color32[width * height];
		float num11 = Mathf.Max(0.05f, Lum(c));
		for (int l = 0; l < width * height; l++)
		{
			if (array[l])
			{
				Color32 val = pixels[l];
				float num12 = Mathf.Clamp(Lum(val) / num11, 0.32f, 1.45f);
				Color val2 = new Color(hair.r * num12, hair.g * num12, hair.b * num12, (float)(int)val.a / 255f);
				if (Lum(val) < 0.1f)
				{
					val2 = Color.Lerp(val2, new Color(0.07f, 0.06f, 0.1f), 0.55f);
				}
				array2[l] = new Color32(Byte(val2.r), Byte(val2.g), Byte(val2.b), val.a);
			}
		}
		Texture2D tex = MakeTex(array2, width, height, "dye_hair");
		value = MakeSprite(tex, src);
		Cache(key, value, tex);
		return value;
	}

	public static Sprite DyeCoat(Sprite src, Color outfit)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)src == (Object)null || (Object)(object)src.texture == (Object)null)
		{
			return null;
		}
		string key = "c" + ((Object)src.texture).GetInstanceID() + "_" + CKey(outfit);
		if (sprCache.TryGetValue(key, out var value) && (Object)(object)value != (Object)null)
		{
			return value;
		}
		int width = ((Texture)src.texture).width;
		int height = ((Texture)src.texture).height;
		Color32[] pixels;
		try
		{
			pixels = src.texture.GetPixels32();
		}
		catch
		{
			return null;
		}
		if (!Bounds(pixels, width, height, out var x, out var x2, out var y, out var y2))
		{
			return null;
		}
		int num = y2 - y + 1;
		int num2 = y2 - Mathf.RoundToInt((float)num * 0.46f);
		int num3 = y2 - Mathf.RoundToInt((float)num * 0.96f);
		Color32[] array = new Color32[width * height];
		for (int i = num3; i <= num2; i++)
		{
			for (int j = x; j <= x2; j++)
			{
				int num4 = i * width + j;
				Color32 val = pixels[num4];
				if (val.a > 60 && !Skin(val))
				{
					float num5 = Mathf.Clamp(Lum(val) / 0.34f, 0.3f, 1.5f);
					Color val2 = new Color(outfit.r * num5, outfit.g * num5, outfit.b * num5, (float)(int)val.a / 255f);
					val2 = Color.Lerp(new Color((float)(int)val.r / 255f, (float)(int)val.g / 255f, (float)(int)val.b / 255f, 1f), val2, 0.88f);
					array[num4] = new Color32(Byte(val2.r), Byte(val2.g), Byte(val2.b), val.a);
				}
			}
		}
		Texture2D tex = MakeTex(array, width, height, "dye_coat");
		value = MakeSprite(tex, src);
		Cache(key, value, tex);
		return value;
	}

	public static Sprite WeaponSprite(WeaponKind kind, Color accent)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		int num = (int)kind;
		string key = "w" + num + "_" + CKey(accent);
		if (sprCache.TryGetValue(key, out var value) && (Object)(object)value != (Object)null)
		{
			return value;
		}
		Color[] array = new Color[9216];
		Color val = Color.Lerp(accent, Color.white, 0.55f);
		Color val2 = new Color(accent.r * 0.45f, accent.g * 0.45f, accent.b * 0.45f, 1f);
		Color val3 = new Color(1f, 0.82f, 0.36f);
		Color val4 = new Color(0.46f, 0.29f, 0.17f);
		switch (kind)
		{
		case WeaponKind.sword:
			PaintBlade(array, accent, val, val2, val3, val4, 0.052f, 0.03f, 0.16f, 0.3f);
			break;
		case WeaponKind.dagger:
			PaintBlade(array, accent, val, val2, val3, val4, 0.04f, 0.022f, 0.1f, 0.36f);
			break;
		case WeaponKind.greatsword:
			PaintBlade(array, accent, val, val2, val3, val4, 0.088f, 0.052f, 0.21f, 0.34f);
			break;
		case WeaponKind.staff:
			Rect(array, 0.478f, 0.04f, 0.522f, 0.86f, val4);
			Rect(array, 0.451f, 0.1f, 0.549f, 0.135f, val3);
			Rect(array, 0.451f, 0.165f, 0.549f, 0.2f, val3);
			Disc(array, 0.5f, 0.885f, 0.082f, accent);
			Disc(array, 0.5f, 0.885f, 0.052f, val);
			Ring(array, 0.5f, 0.885f, 0.104f, 0.014f, val3);
			Rect(array, 0.468f, 0.8f, 0.532f, 0.82f, val3);
			break;
		case WeaponKind.mace:
			Rect(array, 0.478f, 0.03f, 0.522f, 0.7f, val4);
			Disc(array, 0.5f, 0.79f, 0.105f, val2);
			Disc(array, 0.5f, 0.82f, 0.075f, accent);
			Ring(array, 0.5f, 0.79f, 0.128f, 0.012f, val3);
			Rect(array, 0.492f, 0.88f, 0.508f, 0.955f, val3);
			Rect(array, 0.492f, 0.625f, 0.508f, 0.7f, val3);
			Rect(array, 0.345f, 0.782f, 0.4f, 0.798f, val3);
			Rect(array, 0.6f, 0.782f, 0.655f, 0.798f, val3);
			break;
		case WeaponKind.bow:
			RingPart(array, 0.38f, 0.5f, 0.335f, 0.028f, val4, rightHalf: true);
			Rect(array, 0.712f, 0.165f, 0.727f, 0.835f, new Color(0.88f, 0.9f, 0.95f));
			Rect(array, 0.345f, 0.44f, 0.42f, 0.56f, val3);
			Rect(array, 0.352f, 0.4f, 0.365f, 0.62f, val2);
			break;
		}
		Texture2D tex = MakeTex(ToColor32(array), 96, 96, "wpn");
		value = MakeSprite(tex, null);
		Cache(key, value, tex);
		return value;
	}

	private static void PaintBlade(Color[] buf, Color accent, Color bright, Color dark, Color gold, Color wood, float hwBottom, float hwTop, float guardHalf, float bladeFrom)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		Rect(buf, 0.466f, 0.04f, 0.534f, guardHalf - 0.055f, wood);
		Disc(buf, 0.5f, 0.045f, 0.042f, gold);
		Rect(buf, 0.5f - guardHalf, guardHalf - 0.055f, 0.5f + guardHalf, guardHalf, gold);
		Disc(buf, 0.5f - guardHalf, guardHalf - 0.028f, 0.026f, gold);
		Disc(buf, 0.5f + guardHalf, guardHalf - 0.028f, 0.026f, gold);
		int num = 220;
		for (int i = 0; i < num; i++)
		{
			float num2 = (float)i / (float)(num - 1);
			float num3 = Mathf.Lerp(bladeFrom, 0.985f, num2);
			float num4 = Mathf.Lerp(hwBottom, hwTop, num2);
			if (num2 > 0.88f)
			{
				num4 *= Mathf.Lerp(1f, 0.05f, (num2 - 0.88f) / 0.12f);
			}
			Color c = accent;
			if (num2 > 0.55f)
			{
				c = Color.Lerp(accent, bright, (num2 - 0.55f) / 0.45f);
			}
			Rect(buf, 0.5f - num4, num3, 0.5f + num4, num3 + 0.006f, c);
			Rect(buf, 0.5f - num4 * 0.18f, num3, 0.5f + num4 * 0.18f, num3 + 0.006f, bright);
			Rect(buf, 0.5f - num4, num3, 0.5f - num4 + 0.01f, num3 + 0.006f, dark);
			Rect(buf, 0.5f + num4 - 0.01f, num3, 0.5f + num4, num3 + 0.006f, dark);
		}
	}

	private static void Rect(Color[] buf, float u0, float v0, float u1, float v1, Color c)
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		int num = Mathf.Clamp(Mathf.FloorToInt(u0 * 96f), 0, 95);
		int num2 = Mathf.Clamp(Mathf.CeilToInt(u1 * 96f), 0, 95);
		int num3 = Mathf.Clamp(Mathf.FloorToInt(v0 * 96f), 0, 95);
		int num4 = Mathf.Clamp(Mathf.CeilToInt(v1 * 96f), 0, 95);
		for (int i = num3; i <= num4; i++)
		{
			for (int j = num; j <= num2; j++)
			{
				buf[i * 96 + j] = c;
			}
		}
	}

	private static void Disc(Color[] buf, float cu, float cv, float r, Color c)
	{
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		int num = Mathf.Clamp(Mathf.FloorToInt((cu - r) * 96f), 0, 95);
		int num2 = Mathf.Clamp(Mathf.CeilToInt((cu + r) * 96f), 0, 95);
		int num3 = Mathf.Clamp(Mathf.FloorToInt((cv - r) * 96f), 0, 95);
		int num4 = Mathf.Clamp(Mathf.CeilToInt((cv + r) * 96f), 0, 95);
		for (int i = num3; i <= num4; i++)
		{
			for (int j = num; j <= num2; j++)
			{
				float num5 = ((float)j + 0.5f) / 96f - cu;
				float num6 = ((float)i + 0.5f) / 96f - cv;
				if (num5 * num5 + num6 * num6 <= r * r)
				{
					buf[i * 96 + j] = c;
				}
			}
		}
	}

	private static void Ring(Color[] buf, float cu, float cv, float r, float thick, Color c)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		RingPart(buf, cu, cv, r, thick, c, rightHalf: false);
	}

	private static void RingPart(Color[] buf, float cu, float cv, float r, float thick, Color c, bool rightHalf)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < 96; i++)
		{
			for (int j = 0; j < 96; j++)
			{
				float num = ((float)j + 0.5f) / 96f - cu;
				float num2 = ((float)i + 0.5f) / 96f - cv;
				if ((!rightHalf || !(num < 0f)) && Mathf.Abs(Mathf.Sqrt(num * num + num2 * num2) - r) <= thick)
				{
					buf[i * 96 + j] = c;
				}
			}
		}
	}

	private static bool Bounds(Color32[] px, int w, int h, out int x0, out int x1, out int y0, out int y1)
	{
		x0 = w;
		x1 = -1;
		y0 = h;
		y1 = -1;
		for (int i = 0; i < h; i++)
		{
			for (int j = 0; j < w; j++)
			{
				if (px[i * w + j].a > 60)
				{
					if (j < x0)
					{
						x0 = j;
					}
					if (j > x1)
					{
						x1 = j;
					}
					if (i < y0)
					{
						y0 = i;
					}
					if (i > y1)
					{
						y1 = i;
					}
				}
			}
		}
		if (x1 >= x0)
		{
			return y1 >= y0;
		}
		return false;
	}

	private static bool Dominant(Color32[] px, int w, int h, int yb, int yt, out Color32 c)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		c = new Color32((byte)0, (byte)0, (byte)0, byte.MaxValue);
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		Dictionary<int, Color32> dictionary2 = new Dictionary<int, Color32>();
		for (int i = Mathf.Max(0, yb); i <= Mathf.Min(h - 1, yt); i++)
		{
			for (int j = 0; j < w; j++)
			{
				Color32 val = px[i * w + j];
				if (val.a >= 200)
				{
					float num = Lum(val);
					if (!(num < 0.09f) && !(num > 0.93f))
					{
						int key = (val.r << 16) | (val.g << 8) | val.b;
						dictionary.TryGetValue(key, out var value);
						dictionary[key] = value + 1;
						dictionary2[key] = val;
					}
				}
			}
		}
		int num2 = -1;
		int num3 = -1;
		foreach (KeyValuePair<int, int> item in dictionary)
		{
			if (item.Value > num3)
			{
				num3 = item.Value;
				num2 = item.Key;
			}
		}
		if (num2 < 0)
		{
			return false;
		}
		c = dictionary2[num2];
		return true;
	}

	private static bool Near(Color32 a, Color32 b, float tol)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		float num = (float)(a.r - b.r) / 255f;
		float num2 = (float)(a.g - b.g) / 255f;
		float num3 = (float)(a.b - b.b) / 255f;
		return num * num + num2 * num2 + num3 * num3 <= tol * tol;
	}

	private static float Lum(Color32 c)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		return (0.299f * (float)(int)c.r + 0.587f * (float)(int)c.g + 0.114f * (float)(int)c.b) / 255f;
	}

	private static bool Skin(Color32 c)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		if (c.r > 185 && c.g > 155 && c.b > 115)
		{
			return c.r >= c.b - 8;
		}
		return false;
	}

	private static byte Byte(float v)
	{
		return (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);
	}

	private static Color32[] ToColor32(Color[] src)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		Color32[] array = new Color32[src.Length];
		for (int i = 0; i < src.Length; i++)
		{
			array[i] = new Color32(Byte(src[i].r), Byte(src[i].g), Byte(src[i].b), Byte(src[i].a));
		}
		return array;
	}

	private static Texture2D MakeTex(Color32[] px, int w, int h, string name)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected Obj, but got Unknown
		Texture2D val = new Texture2D(w, h, (TextureFormat)4, false);
		val.SetPixels32(px);
		((Texture)val).filterMode = (FilterMode)0;
		((Texture)val).wrapMode = (TextureWrapMode)1;
		((Object)val).name = name;
		val.Apply();
		return val;
	}

	private static Sprite MakeSprite(Texture2D tex, Sprite src)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = (Vector2)(((Object)(object)src != (Object)null) ? src.pivot : new Vector2(0.5f, 0.5f));
		Sprite val2 = Sprite.Create(tex, new Rect(0f, 0f, (float)((Texture)tex).width, (float)((Texture)tex).height), val, 40f, 0u, (SpriteMeshType)0);
		((Object)val2).name = ((Object)tex).name;
		return val2;
	}

	private static void Cache(string key, Sprite sp, Texture2D tex)
	{
		if (sprCache.Count > 400)
		{
			sprCache.Clear();
			texCache.Clear();
		}
		sprCache[key] = sp;
		texCache[key] = tex;
	}

	private static string CKey(Color c)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		return Mathf.RoundToInt(c.r * 255f) + "-" + Mathf.RoundToInt(c.g * 255f) + "-" + Mathf.RoundToInt(c.b * 255f);
	}

	public static Color HairColor(string id)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if (!HairColors.TryGetValue(id, out var value))
		{
			return HairColors["black"];
		}
		return value;
	}

	public static Color OutfitColor(string id)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if (!OutfitColors.TryGetValue(id, out var value))
		{
			return OutfitColors["none"];
		}
		return value;
	}

	static CosmeticFactory()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
	}
}
