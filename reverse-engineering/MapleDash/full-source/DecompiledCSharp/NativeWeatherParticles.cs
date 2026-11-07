using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public sealed class NativeWeatherParticles : MonoBehaviour
{
	private sealed class Particle
	{
		public SpriteRenderer Image;

		public int X;

		public int Y;

		public int Amplitude;

		public int Angle;

		public int Fade;

		public int Alpha;

		public int Drift;

		public int Duration;
	}

	private readonly List<Particle> particles = new List<Particle>();

	private int item = -1;

	private int sky = -1;

	private int width;

	private int height;

	private uint token;

	private uint random = 1u;

	private uint windRandom = 2654435769u;

	private double started;

	public int Count => particles.Count;

	private int Rand()
	{
		random = random * 214013 + 2531011;
		return (int)((random >> 16) & 0x7FFF);
	}

	private static uint Mix(uint v)
	{
		v ^= v >> 16;
		v *= 2146121005;
		v ^= v >> 15;
		v *= 2221713035u;
		return v ^ (v >> 16);
	}

	private uint WindRand()
	{
		windRandom ^= windRandom << 13;
		windRandom ^= windRandom >> 17;
		windRandom ^= windRandom << 5;
		return windRandom;
	}

	public static int Prevailing(int sky, uint token)
	{
		uint num = Mix(token ^ (uint)(sky * -1640531527));
		return sky switch
		{
			8 => (((num & 1) != 0) ? 1 : (-1)) * (int)(1200 + Mix(num + 2769414579u) % 601), 
			5 => (((num & 1) != 0) ? 1 : (-1)) * (int)(150 + Mix(num + 625341585) % 151), 
			_ => (int)(num % 601 - 300), 
		};
	}

	private int Drift(int sky, uint token, int fall)
	{
		int num = Prevailing(sky, token);
		int num2;
		switch (sky)
		{
		case 2:
		case 5:
			return Mathf.Clamp(Math.Abs(num), fall * 45 / 100, fall * 75 / 100) + (int)(WindRand() % 181) - 90;
		default:
			num2 = 150;
			break;
		case 8:
			num2 = 260;
			break;
		}
		int num3 = num2;
		return num + (int)(WindRand() % (uint)(num3 * 2 + 1)) - num3;
	}

	public void Clear()
	{
		foreach (Particle particle in particles)
		{
			if ((Object)(object)particle.Image != (Object)null)
			{
				if (Application.isPlaying)
				{
					Object.Destroy((Object)(object)((Component)particle.Image).gameObject);
				}
				else
				{
					Object.DestroyImmediate((Object)(object)((Component)particle.Image).gameObject);
				}
			}
		}
		particles.Clear();
		item = -1;
	}

	private void Build(int wanted, int weather, uint skyToken, int w, int h, double now)
	{
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		Clear();
		item = wanted;
		sky = weather;
		token = skyToken;
		width = w;
		height = h;
		started = now;
		if (wanted == 0)
		{
			return;
		}
		ImgNode imgNode = WzAssets.Node("Item/Cash/0512.img/" + wanted.ToString("D8") + "/info");
		if (imgNode == null)
		{
			return;
		}
		ImgNode imgNode2 = WzAssets.Node(imgNode.Text("path"));
		if (imgNode2 == null)
		{
			return;
		}
		List<Sprite> list = new List<Sprite>();
		int num = 0;
		while (imgNode2.Get(num.ToString()) != null)
		{
			list.Add(WzAssets.Sprite(imgNode2.Get(num.ToString())));
			num++;
		}
		if (list.Count == 0)
		{
			return;
		}
		int num2 = imgNode2.Int("count", (imgNode2.Int("random") != 0) ? 100 : 0);
		if (imgNode2.Int("random") != 0)
		{
			num2 = num2 * (Rand() % 25 + 30) / 100;
		}
		for (int i = 0; i < num2; i++)
		{
			int num3 = 15 + Rand() % 4 * (Rand() % 4);
			int angle = Rand() % 360;
			int fade = Rand() % 3000 + 3000;
			int index = Rand() % list.Count;
			int x = Rand() % w - w / 2;
			int y = Rand() % h - h / 2;
			Rand();
			GameObject val = new GameObject("NativeWeatherParticle");
			val.transform.SetParent(((Component)this).transform, false);
			SpriteRenderer val2 = val.AddComponent<SpriteRenderer>();
			val2.sprite = list[index];
			((Renderer)val2).sortingOrder = -700;
			particles.Add(new Particle
			{
				Image = val2,
				X = x,
				Y = y,
				Amplitude = num3,
				Angle = angle,
				Fade = fade,
				Alpha = imgNode2.Get(index.ToString()).Int("a0", 255),
				Drift = Drift(weather, skyToken, h / 2),
				Duration = (50000 - imgNode.Int("speed") * 8000) / num3
			});
			if (imgNode.Int("type") == 2)
			{
				Rand();
			}
		}
	}

	private static int Wrap(int value, int start, int size)
	{
		return start + ((value - start) % size + size) % size;
	}

	private static int RotatedPixel(double value)
	{
		return (int)(value + ((value >= 0.0) ? 0.5 : (-0.5)));
	}

	public void Tick(int wanted, int weather, uint skyToken, double now, Color tint)
	{
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		Camera main = Camera.main;
		if ((Object)(object)main == (Object)null)
		{
			return;
		}
		int num = Mathf.Max(2, (int)(main.orthographicSize * 40f * 2f));
		int num2 = Mathf.Max(2, (int)((float)num * main.aspect));
		if (wanted != item || num2 != width || num != height)
		{
			Build(wanted, weather, skyToken, num2, num, now);
		}
		int num3 = (int)Math.Max(0.0, (now - started) * 1000.0);
		foreach (Particle particle in particles)
		{
			double num4 = (double)num3 / (double)particle.Duration;
			double num5 = (double)particle.Angle * Math.PI / 180.0;
			double num6 = num5 + (double)(num3 % 2000) * Math.PI / 1000.0;
			int num7 = particle.Amplitude / 2;
			int num8 = RotatedPixel((double)num7 * Math.Cos(num6)) - RotatedPixel((double)num7 * Math.Cos(num5));
			int num9 = (RotatedPixel((double)num7 * Math.Sin(num6)) - RotatedPixel((double)num7 * Math.Sin(num5))) * 5 / 100;
			int num10 = Wrap(particle.X + (int)((double)particle.Drift * num4), -num2 / 2 - particle.Amplitude, num2 + particle.Amplitude) + num8;
			int num11 = Wrap(particle.Y + (int)((double)(num / 2) * num4), -num / 2 - particle.Amplitude, num + particle.Amplitude) + num9;
			((Component)particle.Image).transform.position = new Vector3(((Component)main).transform.position.x + (float)num10 / 40f, ((Component)main).transform.position.y - (float)num11 / 40f, 0f);
			Color white = Color.white;
			white.a = Mathf.Lerp(1f, (float)(particle.Alpha - 2 * particle.Amplitude), Mathf.Clamp01((float)num3 / (float)particle.Fade)) / 255f;
			particle.Image.color = white;
		}
	}

	private void OnDisable()
	{
		Clear();
		item = -1;
	}
}
