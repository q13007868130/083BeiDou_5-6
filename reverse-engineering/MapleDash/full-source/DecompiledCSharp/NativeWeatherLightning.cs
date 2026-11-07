public static class NativeWeatherLightning
{
	private readonly struct LightningPattern(uint w, uint e, uint p, uint q, uint c, float b, int k)
	{
		public readonly uint windowMs = w;

		public readonly uint earliestMs = e;

		public readonly uint pulseMs = p;

		public readonly uint quietEvery = q;

		public readonly uint clusterEvery = c;

		public readonly float minBrightness = b;

		public readonly int kind = k;
	}

	private static uint LightningHash(uint x)
	{
		x ^= x >> 16;
		x *= 2146121005;
		x ^= x >> 15;
		x *= 2221713035u;
		x ^= x >> 16;
		return x;
	}

	private static LightningPattern LightningPatternForRegion(int palette)
	{
		return palette switch
		{
			0 => new LightningPattern(19000u, 2000u, 1150u, 5u, 0u, 0.84f, 6), 
			1 => new LightningPattern(22500u, 1600u, 1150u, 4u, 2u, 0.76f, 6), 
			2 => new LightningPattern(17500u, 1000u, 1100u, 6u, 5u, 0.72f, 0), 
			3 => new LightningPattern(18000u, 1500u, 1200u, 5u, 0u, 0.76f, 3), 
			4 => new LightningPattern(25000u, 3000u, 380u, 3u, 0u, 0.88f, 7), 
			5 => new LightningPattern(11000u, 800u, 640u, 9u, 2u, 0.84f, 4), 
			6 => new LightningPattern(9500u, 500u, 640u, 8u, 2u, 0.8f, 4), 
			7 => new LightningPattern(21000u, 2000u, 1500u, 4u, 3u, 0.72f, 5), 
			8 => new LightningPattern(20000u, 1400u, 1200u, 6u, 0u, 0.72f, 3), 
			9 => new LightningPattern(28000u, 2500u, 380u, 4u, 0u, 0.78f, 7), 
			10 => new LightningPattern(17000u, 1500u, 1200u, 5u, 2u, 0.82f, 3), 
			11 => new LightningPattern(16000u, 1000u, 780u, 6u, 2u, 0.7f, 2), 
			12 => new LightningPattern(19500u, 2200u, 1200u, 4u, 0u, 0.78f, 3), 
			13 => new LightningPattern(19000u, 1800u, 1500u, 5u, 3u, 0.76f, 5), 
			14 => new LightningPattern(12000u, 700u, 780u, 6u, 2u, 0.85f, 2), 
			15 => new LightningPattern(18000u, 1200u, 1100u, 5u, 4u, 0.7f, 0), 
			16 => new LightningPattern(14000u, 1000u, 780u, 7u, 3u, 0.78f, 2), 
			17 => new LightningPattern(23000u, 2000u, 380u, 3u, 0u, 0.8f, 7), 
			18 => new LightningPattern(13000u, 750u, 780u, 7u, 2u, 0.82f, 2), 
			19 => new LightningPattern(15000u, 500u, 1250u, 5u, 2u, 0.8f, 1), 
			20 => new LightningPattern(23000u, 2500u, 1500u, 3u, 2u, 0.72f, 5), 
			21 => new LightningPattern(26000u, 3000u, 1500u, 3u, 0u, 0.68f, 5), 
			22 => new LightningPattern(17500u, 1200u, 1200u, 5u, 2u, 0.8f, 3), 
			23 => new LightningPattern(10500u, 700u, 640u, 8u, 2u, 0.86f, 4), 
			24 => new LightningPattern(15500u, 1000u, 780u, 5u, 3u, 0.74f, 2), 
			25 => new LightningPattern(16500u, 900u, 1100u, 6u, 4u, 0.76f, 0), 
			_ => new LightningPattern(12000u, 700u, 1100u, 7u, 4u, 0.75f, 0), 
		};
	}

	private static float LightningEnvelope(int kind, uint t, uint variation)
	{
		switch (kind)
		{
		case 1:
			switch (variation & 3)
			{
			case 0u:
				if (t < 95)
				{
					return 1f - 0.3f * (float)t / 95f;
				}
				if (t < 590)
				{
					return 0.42f * (1f - (float)(t - 95) / 495f);
				}
				return 0f;
			case 1u:
				if (t < 45)
				{
					return 1f - 0.3f * (float)t / 45f;
				}
				if (t < 145)
				{
					return 0f;
				}
				if (t < 210)
				{
					return 0.72f - 0.24f * (float)(t - 145) / 65f;
				}
				if (t < 320)
				{
					return 0f;
				}
				if (t < 410)
				{
					return 0.9f - 0.36f * (float)(t - 320) / 90f;
				}
				if (t < 700)
				{
					return 0.28f * (1f - (float)(t - 410) / 290f);
				}
				return 0f;
			case 2u:
				if (t < 170)
				{
					return 0.88f - 0.2f * (float)t / 170f;
				}
				if (t < 700)
				{
					return 0.58f - 0.17f * (float)(t - 170) / 530f;
				}
				if (t < 840)
				{
					return 0.8f - 0.26f * (float)(t - 700) / 140f;
				}
				return 0.3f * (1f - (float)(t - 840) / 410f);
			default:
				if (t < 65)
				{
					return 0.96f - 0.24f * (float)t / 65f;
				}
				if (t < 245)
				{
					return 0.34f - 0.14f * (float)(t - 65) / 180f;
				}
				if (t < 360)
				{
					return 0.84f - 0.32f * (float)(t - 245) / 115f;
				}
				if (t < 880)
				{
					return 0.3f * (1f - (float)(t - 360) / 520f);
				}
				return 0f;
			}
		case 2:
			if (t < 55)
			{
				return 1f - 0.22f * (float)t / 55f;
			}
			if (t < 145)
			{
				return 0f;
			}
			if (t < 205)
			{
				return 0.82f - 0.2f * (float)(t - 145) / 60f;
			}
			if (t < 315)
			{
				return 0f;
			}
			if (t < 390)
			{
				return 0.94f - 0.32f * (float)(t - 315) / 75f;
			}
			return 0.3f * (1f - (float)(t - 390) / 390f);
		case 3:
			if (t < 150)
			{
				return 0.88f - 0.2f * (float)t / 150f;
			}
			if (t < 680)
			{
				return 0.46f - 0.16f * (float)(t - 150) / 530f;
			}
			if (t < 800)
			{
				return 0.62f - 0.24f * (float)(t - 680) / 120f;
			}
			return 0.3f * (1f - (float)(t - 800) / 400f);
		case 4:
			switch (t)
			{
			case 0u:
			case 1u:
			case 2u:
			case 3u:
			case 4u:
			case 5u:
			case 6u:
			case 7u:
			case 8u:
			case 9u:
			case 10u:
			case 11u:
			case 12u:
			case 13u:
			case 14u:
			case 15u:
			case 16u:
			case 17u:
			case 18u:
			case 19u:
			case 20u:
			case 21u:
			case 22u:
			case 23u:
			case 24u:
			case 25u:
			case 26u:
			case 27u:
			case 28u:
			case 29u:
			case 30u:
			case 31u:
			case 32u:
			case 33u:
			case 34u:
			case 35u:
			case 36u:
			case 37u:
			case 38u:
			case 39u:
			case 40u:
			case 41u:
			case 42u:
			case 43u:
			case 44u:
			case 45u:
			case 46u:
			case 47u:
				return 1f - 0.24f * (float)t / 48f;
			case 48u:
			case 49u:
			case 50u:
			case 51u:
			case 52u:
			case 53u:
			case 54u:
			case 55u:
			case 56u:
			case 57u:
			case 58u:
			case 59u:
			case 60u:
			case 61u:
			case 62u:
			case 63u:
			case 64u:
			case 65u:
			case 66u:
			case 67u:
			case 68u:
			case 69u:
			case 70u:
			case 71u:
			case 72u:
			case 73u:
			case 74u:
			case 75u:
			case 76u:
			case 77u:
			case 78u:
			case 79u:
			case 80u:
			case 81u:
			case 82u:
			case 83u:
			case 84u:
			case 85u:
			case 86u:
			case 87u:
			case 88u:
			case 89u:
			case 90u:
			case 91u:
			case 92u:
			case 93u:
			case 94u:
			case 95u:
			case 96u:
			case 97u:
			case 98u:
			case 99u:
			case 100u:
			case 101u:
			case 102u:
			case 103u:
			case 104u:
				return 0f;
			case 105u:
			case 106u:
			case 107u:
			case 108u:
			case 109u:
			case 110u:
			case 111u:
			case 112u:
			case 113u:
			case 114u:
			case 115u:
			case 116u:
			case 117u:
			case 118u:
			case 119u:
			case 120u:
			case 121u:
			case 122u:
			case 123u:
			case 124u:
			case 125u:
			case 126u:
			case 127u:
			case 128u:
			case 129u:
			case 130u:
			case 131u:
			case 132u:
			case 133u:
			case 134u:
			case 135u:
			case 136u:
			case 137u:
			case 138u:
			case 139u:
			case 140u:
			case 141u:
			case 142u:
			case 143u:
			case 144u:
			case 145u:
			case 146u:
			case 147u:
			case 148u:
			case 149u:
			case 150u:
			case 151u:
			case 152u:
			case 153u:
			case 154u:
			case 155u:
			case 156u:
			case 157u:
			case 158u:
			case 159u:
				return 0.78f - 0.2f * (float)(t - 105) / 55f;
			case 160u:
			case 161u:
			case 162u:
			case 163u:
			case 164u:
			case 165u:
			case 166u:
			case 167u:
			case 168u:
			case 169u:
			case 170u:
			case 171u:
			case 172u:
			case 173u:
			case 174u:
			case 175u:
			case 176u:
			case 177u:
			case 178u:
			case 179u:
			case 180u:
			case 181u:
			case 182u:
			case 183u:
			case 184u:
			case 185u:
			case 186u:
			case 187u:
			case 188u:
			case 189u:
			case 190u:
			case 191u:
			case 192u:
			case 193u:
			case 194u:
			case 195u:
			case 196u:
			case 197u:
			case 198u:
			case 199u:
			case 200u:
			case 201u:
			case 202u:
			case 203u:
			case 204u:
			case 205u:
			case 206u:
			case 207u:
			case 208u:
			case 209u:
			case 210u:
			case 211u:
			case 212u:
			case 213u:
			case 214u:
			case 215u:
			case 216u:
			case 217u:
			case 218u:
			case 219u:
			case 220u:
			case 221u:
			case 222u:
			case 223u:
			case 224u:
				return 0f;
			case 225u:
			case 226u:
			case 227u:
			case 228u:
			case 229u:
			case 230u:
			case 231u:
			case 232u:
			case 233u:
			case 234u:
			case 235u:
			case 236u:
			case 237u:
			case 238u:
			case 239u:
			case 240u:
			case 241u:
			case 242u:
			case 243u:
			case 244u:
			case 245u:
			case 246u:
			case 247u:
			case 248u:
			case 249u:
			case 250u:
			case 251u:
			case 252u:
			case 253u:
			case 254u:
			case 255u:
			case 256u:
			case 257u:
			case 258u:
			case 259u:
			case 260u:
			case 261u:
			case 262u:
			case 263u:
			case 264u:
			case 265u:
			case 266u:
			case 267u:
			case 268u:
			case 269u:
			case 270u:
			case 271u:
			case 272u:
			case 273u:
			case 274u:
			case 275u:
			case 276u:
			case 277u:
			case 278u:
			case 279u:
			case 280u:
			case 281u:
			case 282u:
			case 283u:
			case 284u:
			case 285u:
			case 286u:
			case 287u:
			case 288u:
			case 289u:
			case 290u:
			case 291u:
			case 292u:
			case 293u:
			case 294u:
				return 0.92f - 0.34f * (float)(t - 225) / 70f;
			default:
				return 0.34f * (1f - (float)(t - 295) / 345f);
			}
		case 5:
			if (t < 160)
			{
				return 0.92f - 0.2f * (float)t / 160f;
			}
			if (t < 760)
			{
				return 0.46f - 0.13f * (float)(t - 160) / 600f;
			}
			if (t < 940)
			{
				return 0.76f - 0.22f * (float)(t - 760) / 180f;
			}
			return 0.42f * (1f - (float)(t - 940) / 560f);
		case 6:
			if (t < 220)
			{
				return 0.96f - 0.18f * (float)t / 220f;
			}
			if (t < 760)
			{
				return 0.58f - 0.22f * (float)(t - 220) / 540f;
			}
			return 0.34f * (1f - (float)(t - 760) / 390f);
		case 7:
			switch (t)
			{
			case 0u:
			case 1u:
			case 2u:
			case 3u:
			case 4u:
			case 5u:
			case 6u:
			case 7u:
			case 8u:
			case 9u:
			case 10u:
			case 11u:
			case 12u:
			case 13u:
			case 14u:
			case 15u:
			case 16u:
			case 17u:
			case 18u:
			case 19u:
			case 20u:
			case 21u:
			case 22u:
			case 23u:
			case 24u:
			case 25u:
			case 26u:
			case 27u:
			case 28u:
			case 29u:
			case 30u:
			case 31u:
			case 32u:
			case 33u:
			case 34u:
			case 35u:
			case 36u:
			case 37u:
			case 38u:
			case 39u:
			case 40u:
			case 41u:
			case 42u:
			case 43u:
			case 44u:
			case 45u:
			case 46u:
			case 47u:
			case 48u:
			case 49u:
			case 50u:
			case 51u:
			case 52u:
			case 53u:
			case 54u:
			case 55u:
			case 56u:
			case 57u:
			case 58u:
			case 59u:
			case 60u:
			case 61u:
			case 62u:
			case 63u:
			case 64u:
				return 1f - 0.38f * (float)t / 65f;
			case 65u:
			case 66u:
			case 67u:
			case 68u:
			case 69u:
			case 70u:
			case 71u:
			case 72u:
			case 73u:
			case 74u:
			case 75u:
			case 76u:
			case 77u:
			case 78u:
			case 79u:
			case 80u:
			case 81u:
			case 82u:
			case 83u:
			case 84u:
			case 85u:
			case 86u:
			case 87u:
			case 88u:
			case 89u:
			case 90u:
			case 91u:
			case 92u:
			case 93u:
			case 94u:
			case 95u:
			case 96u:
			case 97u:
			case 98u:
			case 99u:
			case 100u:
			case 101u:
			case 102u:
			case 103u:
			case 104u:
			case 105u:
			case 106u:
			case 107u:
			case 108u:
			case 109u:
			case 110u:
			case 111u:
			case 112u:
			case 113u:
			case 114u:
			case 115u:
			case 116u:
			case 117u:
			case 118u:
			case 119u:
			case 120u:
			case 121u:
			case 122u:
			case 123u:
			case 124u:
			case 125u:
			case 126u:
			case 127u:
			case 128u:
			case 129u:
			case 130u:
			case 131u:
			case 132u:
			case 133u:
			case 134u:
			case 135u:
			case 136u:
			case 137u:
			case 138u:
			case 139u:
			case 140u:
			case 141u:
			case 142u:
			case 143u:
			case 144u:
			case 145u:
			case 146u:
			case 147u:
			case 148u:
			case 149u:
				return 0.18f * (1f - (float)(t - 65) / 85f);
			case 150u:
			case 151u:
			case 152u:
			case 153u:
			case 154u:
			case 155u:
			case 156u:
			case 157u:
			case 158u:
			case 159u:
			case 160u:
			case 161u:
			case 162u:
			case 163u:
			case 164u:
			case 165u:
			case 166u:
			case 167u:
			case 168u:
			case 169u:
			case 170u:
			case 171u:
			case 172u:
			case 173u:
			case 174u:
			case 175u:
			case 176u:
			case 177u:
			case 178u:
			case 179u:
			case 180u:
			case 181u:
			case 182u:
			case 183u:
			case 184u:
			case 185u:
			case 186u:
			case 187u:
			case 188u:
			case 189u:
			case 190u:
			case 191u:
			case 192u:
			case 193u:
			case 194u:
			case 195u:
			case 196u:
			case 197u:
			case 198u:
			case 199u:
			case 200u:
			case 201u:
			case 202u:
			case 203u:
			case 204u:
			case 205u:
			case 206u:
			case 207u:
			case 208u:
			case 209u:
			case 210u:
			case 211u:
			case 212u:
			case 213u:
			case 214u:
			case 215u:
			case 216u:
			case 217u:
			case 218u:
			case 219u:
				return 0.7f * (1f - (float)(t - 150) / 70f);
			default:
				return 0.1f * (1f - (float)(t - 220) / 160f);
			}
		default:
			if (variation % 3 == 0)
			{
				if (t < 85)
				{
					return 1f - 0.32f * (float)t / 85f;
				}
				if (t < 540)
				{
					return 0.4f * (1f - (float)(t - 85) / 455f);
				}
				return 0f;
			}
			if (variation % 3 == 1)
			{
				if (t < 70)
				{
					return 1f - 0.26f * (float)t / 70f;
				}
				if (t < 170)
				{
					return 0.38f - 0.13f * (float)(t - 70) / 100f;
				}
				if (t < 270)
				{
					return 0.88f - 0.28f * (float)(t - 170) / 100f;
				}
				if (t < 520)
				{
					return 0.44f * (1f - (float)(t - 270) / 250f);
				}
				return 0f;
			}
			if (t < 150)
			{
				return 0.9f - 0.22f * (float)t / 150f;
			}
			if (t < 620)
			{
				return 0.52f - 0.18f * (float)(t - 150) / 470f;
			}
			if (t < 770)
			{
				return 0.82f - 0.3f * (float)(t - 620) / 150f;
			}
			return 0.3f * (1f - (float)(t - 770) / 330f);
		}
	}

	private static float LightningEvent(LightningPattern p, uint within, uint h)
	{
		uint num = p.windowMs - p.earliestMs - p.pulseMs;
		uint num2 = p.earliestMs + (h >> 4) % num;
		if (within < num2 || within - num2 >= p.pulseMs)
		{
			return 0f;
		}
		float num3 = p.minBrightness + (1f - p.minBrightness) * (float)((h >> 24) & 0xFF) / 255f;
		return LightningEnvelope(p.kind, within - num2, h >> 12) * num3;
	}

	public static float Evaluate(uint ageMs, uint seed, int palette)
	{
		LightningPattern p = LightningPatternForRegion(palette);
		uint num = ageMs / p.windowMs;
		uint num2 = LightningHash(seed ^ (uint)((int)num * -1640531527));
		if (p.quietEvery != 0 && num2 % p.quietEvery == 0)
		{
			return 0f;
		}
		uint within = ageMs % p.windowMs;
		float num3 = LightningEvent(p, within, num2);
		if (p.clusterEvery != 0 && (num2 >> 8) % p.clusterEvery == 0)
		{
			float num4 = LightningEvent(p, within, LightningHash(num2 ^ 0xA511E9B3u));
			if (num4 > num3)
			{
				num3 = num4;
			}
		}
		return num3;
	}
}
