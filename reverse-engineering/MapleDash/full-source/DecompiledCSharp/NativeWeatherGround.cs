using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class NativeWeatherGround : MonoBehaviour
{
	private sealed class Deposit
	{
		public NativeImgAnimation Art;

		public int X;

		public int Slot;

		public int Depth;

		public int Half;
	}

	private sealed class Print
	{
		public NativeImgAnimation Art;

		public int X;

		public long Born;
	}

	private sealed class Puddle
	{
		public NativeImgAnimation Art;

		public int X;

		public int Y;

		public int Tier;

		public int Variant;

		public int MaxTier;
	}

	private readonly List<Deposit> deposits = new List<Deposit>();

	private readonly List<Print> prints = new List<Print>();

	private readonly List<Puddle> puddles = new List<Puddle>();

	private readonly List<NativeImgAnimation> splashes = new List<NativeImgAnimation>();

	private readonly Dictionary<string, int> variants = new Dictionary<string, int>();

	private FieldMap map;

	private string levelKind;

	private string seededFor;

	private float snowLevel;

	private float wetLevel;

	private float snowCarry;

	private float wetCarry;

	private float splashCarry;

	private int snowCatchUp;

	private int wetCatchUp;

	private int lastPrintX = int.MinValue;

	private int lastWalkX;

	private bool wetSeeded;

	private bool wasGrounded;

	private bool wasWet;

	private long lastFrame;

	private uint snowRand = 2135587861u;

	private uint wetRand = 625341585u;

	private uint splashRand = 495397697u;

	private Color tint;

	private int groundOrder;

	public int DepositCount => deposits.Count;

	public int PuddleCount => puddles.Count;

	public int FootprintCount => prints.Count;

	public float SnowLevel
	{
		get
		{
			if (!(levelKind == "snow"))
			{
				return 0f;
			}
			return snowLevel;
		}
	}

	private static uint Next(ref uint value)
	{
		value ^= value << 13;
		value ^= value >> 17;
		value ^= value << 5;
		return value;
	}

	public void Bind(FieldMap value, int groundOrder = -399)
	{
		map = value;
		this.groundOrder = groundOrder;
	}

	private static Vector3 World(int x, int y)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return new Vector3((float)x / 40f, (float)(-y) / 40f, 0f);
	}

	private bool Ground(int x, int from, int to, out int y)
	{
		float num = float.PositiveInfinity;
		if (map != null)
		{
			foreach (FieldFoothold foothold in map.footholds)
			{
				if (foothold.x1 != foothold.x2 && !((float)x < Mathf.Min(foothold.x1, foothold.x2)) && !((float)x > Mathf.Max(foothold.x1, foothold.x2)))
				{
					float num2 = foothold.y1 + (foothold.y2 - foothold.y1) * ((float)x - foothold.x1) / (foothold.x2 - foothold.x1);
					if (num2 >= (float)from && num2 <= (float)to && num2 < num)
					{
						num = num2;
					}
				}
			}
		}
		y = ((!float.IsPositiveInfinity(num)) ? ((int)num) : 0);
		return !float.IsPositiveInfinity(num);
	}

	private bool Neighbours(int x, int from, int to, int y, int probe, int tolerance)
	{
		if (Ground(x - probe, from, to, out var y2) && Ground(x + probe, from, to, out var y3) && Math.Abs(y2 - y) <= tolerance)
		{
			return Math.Abs(y3 - y) <= tolerance;
		}
		return false;
	}

	private NativeImgAnimation Make(string path, int x, int y, int order, bool loop = false, float life = 0f)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		NativeImgAnimation nativeImgAnimation = NativeImgAnimation.Spawn(path, ((Component)this).transform, World(x, y), order, flip: false, loop, life);
		if ((Object)(object)nativeImgAnimation != (Object)null)
		{
			nativeImgAnimation.Tint = tint;
		}
		return nativeImgAnimation;
	}

	private int Variants(string kind)
	{
		if (variants.TryGetValue(kind, out var i))
		{
			return i;
		}
		for (; i < 32; i++)
		{
			if (WzAssets.Node("Effect/WeatherAccum.img/" + kind + "/" + i + "/0") == null)
			{
				break;
			}
		}
		variants[kind] = i;
		return i;
	}

	private static void Remove(NativeImgAnimation art)
	{
		if ((Object)(object)art != (Object)null)
		{
			art.Remove();
		}
	}

	private void Cull(int cx, int half)
	{
		for (int num = deposits.Count - 1; num >= 0; num--)
		{
			if ((Object)(object)deposits[num].Art == (Object)null || Math.Abs(deposits[num].X - cx) > half * 3)
			{
				Remove(deposits[num].Art);
				deposits.RemoveAt(num);
			}
		}
		for (int num2 = puddles.Count - 1; num2 >= 0; num2--)
		{
			if ((Object)(object)puddles[num2].Art == (Object)null || Math.Abs(puddles[num2].X - cx) > half * 3)
			{
				Remove(puddles[num2].Art);
				puddles.RemoveAt(num2);
			}
		}
	}

	private void RemoveFarDeposit(int cx)
	{
		int index = 0;
		for (int i = 1; i < deposits.Count; i++)
		{
			if (Math.Abs(deposits[i].X - cx) > Math.Abs(deposits[index].X - cx))
			{
				index = i;
			}
		}
		Remove(deposits[index].Art);
		deposits.RemoveAt(index);
	}

	private void RemoveFarPuddle(int cx)
	{
		int index = 0;
		for (int i = 1; i < puddles.Count; i++)
		{
			if (Math.Abs(puddles[i].X - cx) > Math.Abs(puddles[index].X - cx))
			{
				index = i;
			}
		}
		Remove(puddles[index].Art);
		puddles.RemoveAt(index);
	}

	private bool PlaceDeposit(string kind, int cx, int cy, int hw, int hh)
	{
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		int num = Variants(kind);
		if (num == 0)
		{
			return false;
		}
		hw += 80;
		hh += 80;
		for (int i = 0; i < 6; i++)
		{
			int num2 = (int)Math.Floor((double)(cx - hw) / 26.0) + (int)(Next(ref snowRand) % (uint)(2 * hw / 26 + 1));
			int num3 = 0;
			foreach (Deposit deposit2 in deposits)
			{
				if (deposit2.Slot == num2)
				{
					num3++;
				}
			}
			if (num3 >= 3)
			{
				continue;
			}
			int x = num2 * 26 + (int)(Next(ref snowRand) % 26);
			if (Ground(x, cy - hh, cy + hh, out var y) && Neighbours(x, cy - hh, cy + hh, y, 14, 10))
			{
				NativeImgAnimation nativeImgAnimation = Make("Effect/WeatherAccum.img/" + kind + "/" + (int)(Next(ref snowRand) % (uint)num), x, y + 5 - num3 * 2, groundOrder);
				if ((Object)(object)nativeImgAnimation == (Object)null)
				{
					return false;
				}
				List<Deposit> list = deposits;
				Deposit deposit = new Deposit
				{
					Art = nativeImgAnimation,
					X = x,
					Slot = num2,
					Depth = num3
				};
				Rect rect = nativeImgAnimation.Image.sprite.rect;
				deposit.Half = (int)(rect.width / 2f);
				list.Add(deposit);
				return true;
			}
		}
		return false;
	}

	private void Footprints(long now)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		for (int num = prints.Count - 1; num >= 0; num--)
		{
			Print print = prints[num];
			long num2 = now - print.Born;
			if ((Object)(object)print.Art == (Object)null || num2 >= 11000)
			{
				Remove(print.Art);
				prints.RemoveAt(num);
			}
			else
			{
				Color val = tint;
				val.a *= ((num2 > 7500) ? ((float)(11000 - num2) / 3500f) : 1f);
				print.Art.Tint = val;
			}
		}
		PlayerCtrl playerCtrl = Object.FindObjectOfType<PlayerCtrl>();
		if ((Object)(object)playerCtrl == (Object)null || deposits.Count == 0)
		{
			wasGrounded = false;
			return;
		}
		int num3 = (int)(playerCtrl.FeetPos.x * 40f);
		int num4 = (int)((0f - playerCtrl.FeetPos.y) * 40f);
		bool flag = Ground(num3, num4 - 6, num4 + 8, out var y) && Math.Abs(y - num4) <= 8;
		bool flag2 = flag && !wasGrounded;
		wasGrounded = flag;
		if (!flag || (!flag2 && lastPrintX != int.MinValue && Math.Abs(num3 - lastPrintX) < 22) || levelKind != "snow")
		{
			return;
		}
		bool flag3 = false;
		foreach (Deposit deposit in deposits)
		{
			flag3 |= Math.Abs(num3 - deposit.X) <= deposit.Half;
		}
		int num5 = Variants("print");
		if (!flag3 || num5 == 0)
		{
			return;
		}
		NativeImgAnimation nativeImgAnimation = Make("Effect/WeatherAccum.img/print/" + Next(ref snowRand) % (uint)num5, num3, y + 5, groundOrder + 1, loop: false, 11f);
		if (!((Object)(object)nativeImgAnimation == (Object)null))
		{
			if (prints.Count >= 20)
			{
				Remove(prints[0].Art);
				prints.RemoveAt(0);
			}
			prints.Add(new Print
			{
				Art = nativeImgAnimation,
				X = num3,
				Born = now
			});
			lastPrintX = num3;
		}
	}

	private void Accumulate(int sky, int age, int dt, long now, int cx, int cy, int hw, int hh)
	{
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		object obj;
		switch (sky)
		{
		default:
			obj = null;
			break;
		case 7:
			obj = "petal";
			break;
		case 6:
			obj = "leaf";
			break;
		case 2:
		case 5:
			obj = "snow";
			break;
		}
		string text = (string)obj;
		if (text != null && seededFor != text && snowLevel <= 0f)
		{
			seededFor = text;
			snowLevel = Mathf.Min(1f, (float)(age / 1000 * 1000) / 90000f);
			snowCatchUp = (int)(snowLevel * 70f);
		}
		bool flag = text != null && (levelKind == null || levelKind == text);
		if (flag)
		{
			levelKind = text;
			snowLevel = Mathf.Min(1f, snowLevel + (float)dt / 90000f);
		}
		else
		{
			snowLevel = Mathf.Max(0f, snowLevel - (float)dt / 25000f);
		}
		foreach (Deposit deposit in deposits)
		{
			if ((Object)(object)deposit.Art != (Object)null)
			{
				deposit.Art.Tint = tint;
			}
		}
		Footprints(now);
		if (snowLevel <= 0f)
		{
			foreach (Deposit deposit2 in deposits)
			{
				Remove(deposit2.Art);
			}
			deposits.Clear();
			levelKind = null;
			snowCarry = 0f;
			seededFor = null;
			snowCatchUp = 0;
			return;
		}
		int num = (int)(snowLevel * 70f);
		if (deposits.Count > num)
		{
			RemoveFarDeposit(cx);
		}
		else if (flag && deposits.Count < num)
		{
			snowCarry += (float)(14 * dt) / 1000f;
			int num2 = (int)snowCarry;
			snowCarry -= num2;
			int num3 = Math.Min(snowCatchUp, 25);
			num2 += num3;
			snowCatchUp -= num3;
			while (num2 > 0 && deposits.Count < num && PlaceDeposit(text, cx, cy, hw, hh))
			{
				num2--;
			}
			if (num3 > 0 && num2 > 0)
			{
				snowCatchUp += Math.Min(num2, num3);
			}
		}
	}

	private static int Half(int tier)
	{
		return tier switch
		{
			1 => 24, 
			0 => 15, 
			_ => 35, 
		};
	}

	private bool Holds(int x, int from, int to, int y, int tier)
	{
		return Neighbours(x, from, to, y, Half(tier) + 2, 10);
	}

	private bool PlacePuddle(int cx, int cy, int hw, int hh)
	{
		hw += 80;
		hh += 80;
		int num = cy - hh;
		int to = cy + hh;
		for (int i = 0; i < 12; i++)
		{
			int num2 = cx - hw + (int)(Next(ref wetRand) % (uint)(2 * hw));
			if (!Ground(num2, num, to, out var y) || !Neighbours(num2, num, to, y, 12, 3) || !Holds(num2, num, to, y, 0))
			{
				continue;
			}
			int j;
			for (j = 0; j + 1 < 3 && Holds(num2, num, to, y, j + 1); j++)
			{
			}
			bool flag = false;
			foreach (Puddle puddle in puddles)
			{
				if (Math.Abs(y + 6 - puddle.Y) <= 24 && Math.Abs(num2 - puddle.X) < Half(j) + Half(puddle.MaxTier) + 56)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				int variant = (int)(Next(ref wetRand) % 3);
				NativeImgAnimation nativeImgAnimation = Make("Effect/WeatherPuddle.img/0/" + variant, num2, y + 6, groundOrder, loop: true);
				if ((Object)(object)nativeImgAnimation == (Object)null)
				{
					return false;
				}
				puddles.Add(new Puddle
				{
					Art = nativeImgAnimation,
					X = num2,
					Y = y + 6,
					Variant = variant,
					MaxTier = j
				});
				return true;
			}
		}
		return false;
	}

	private bool Promote(int tier)
	{
		foreach (Puddle puddle in puddles)
		{
			if (puddle.Tier < tier && puddle.Tier + 1 < 3 && puddle.Tier + 1 <= puddle.MaxTier)
			{
				NativeImgAnimation nativeImgAnimation = Make("Effect/WeatherPuddle.img/" + (puddle.Tier + 1) + "/" + puddle.Variant, puddle.X, puddle.Y, groundOrder, loop: true);
				if ((Object)(object)nativeImgAnimation == (Object)null)
				{
					return false;
				}
				Remove(puddle.Art);
				puddle.Art = nativeImgAnimation;
				puddle.Tier++;
				return true;
			}
		}
		return false;
	}

	private void StepWater()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		PlayerCtrl playerCtrl = Object.FindObjectOfType<PlayerCtrl>();
		if ((Object)(object)playerCtrl == (Object)null)
		{
			wasWet = false;
			return;
		}
		int num = (int)(playerCtrl.FeetPos.x * 40f);
		int num2 = (int)((0f - playerCtrl.FeetPos.y) * 40f);
		Puddle puddle = null;
		foreach (Puddle puddle2 in puddles)
		{
			if (num >= puddle2.X - Half(puddle2.Tier) && num <= puddle2.X + Half(puddle2.Tier) && Math.Abs(num2 - puddle2.Y) <= 14)
			{
				puddle = puddle2;
				break;
			}
		}
		if (puddle == null)
		{
			wasWet = false;
		}
		else if (!wasWet)
		{
			wasWet = true;
			lastWalkX = num;
			Burst(num, puddle.Y - 6, 6, 11);
		}
		else if (Math.Abs(num - lastWalkX) >= 26)
		{
			lastWalkX = num;
			Burst(num, puddle.Y - 6, 2, 11);
		}
	}

	private void Puddles(int sky, int age, int dt, int cx, int cy, int hw, int hh)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		StepWater();
		foreach (Puddle puddle in puddles)
		{
			if ((Object)(object)puddle.Art != (Object)null)
			{
				puddle.Art.Tint = tint;
			}
		}
		bool flag = sky == 1 || sky == 4;
		if (flag && !wetSeeded && wetLevel <= 0f)
		{
			wetSeeded = true;
			wetLevel = Mathf.Min(1f, (float)(age / 1000 * 1000) / 75000f);
			wetCatchUp = (int)(wetLevel * 26f);
		}
		if (!flag)
		{
			wetSeeded = false;
		}
		wetLevel = Mathf.Clamp01(wetLevel + (float)dt / (flag ? 75000f : (-40000f)));
		if (wetLevel <= 0f)
		{
			foreach (Puddle puddle2 in puddles)
			{
				Remove(puddle2.Art);
			}
			puddles.Clear();
			wetCarry = 0f;
			return;
		}
		int num = (int)(wetLevel * 26f);
		if (puddles.Count > num)
		{
			RemoveFarPuddle(cx);
		}
		else if (flag)
		{
			wetCarry += (float)(3 * dt) / 1000f;
			int num2 = (int)wetCarry;
			wetCarry -= num2;
			int num3 = Math.Min(wetCatchUp, 10);
			num2 += num3;
			wetCatchUp -= num3;
			int tier = Math.Min(2, (int)(wetLevel * 3f));
			while (num2-- > 0 && ((puddles.Count < num && PlacePuddle(cx, cy, hw, hh)) || Promote(tier)))
			{
			}
		}
	}

	private void Splash(int x, int surface, Color color)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		if (splashes.Count < 40)
		{
			NativeImgAnimation nativeImgAnimation = Make("Effect/WeatherSplash.img/rain/" + Next(ref splashRand) % 3, x, surface + 4, groundOrder + 1, loop: false, 0.52f);
			if ((Object)(object)nativeImgAnimation != (Object)null)
			{
				nativeImgAnimation.Tint = color;
				splashes.Add(nativeImgAnimation);
			}
		}
	}

	private void Burst(int x, int y, int count, int spread)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < count; i++)
		{
			if (splashes.Count >= 40)
			{
				break;
			}
			Splash(x - spread + (int)(Next(ref splashRand) % (uint)(2 * spread + 1)), y, tint);
		}
	}

	private void RainSplashes(float intensity, int dt, int cx, int cy, int hw, int hh, Color rainTint)
	{
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		splashes.RemoveAll((NativeImgAnimation nativeImgAnimation) => (Object)(object)nativeImgAnimation == (Object)null);
		if (intensity <= 0f || dt == 0)
		{
			return;
		}
		splashCarry += 22.4f * intensity * (float)dt / 1000f;
		int num = (int)splashCarry;
		splashCarry -= num;
		hw += 64;
		hh += 64;
		while (num-- > 0)
		{
			if (splashes.Count >= 40)
			{
				splashCarry = 0f;
				break;
			}
			int x;
			if (Next(ref splashRand) % 3 == 0 && puddles.Count > 0)
			{
				Puddle puddle = puddles[(int)(Next(ref wetRand) % (uint)puddles.Count)];
				x = puddle.X - Half(puddle.Tier) + (int)(Next(ref wetRand) % (uint)(2 * Half(puddle.Tier) + 1));
			}
			else
			{
				x = cx - hw + (int)(Next(ref splashRand) % (uint)(2 * hw));
			}
			if (Ground(x, cy - hh, cy + hh, out var y))
			{
				Splash(x, y, rainTint);
			}
		}
	}

	public void Tick(int sky, int age, float rain, long now, Color scenery, Color rainTint)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		int dt = (int)((lastFrame != 0L) ? Math.Min(100L, Math.Max(0L, now - lastFrame)) : 0);
		lastFrame = now;
		tint = scenery;
		Camera main = Camera.main;
		if (!((Object)(object)main == (Object)null) && map != null)
		{
			int cx = (int)(((Component)main).transform.position.x * 40f);
			int cy = (int)((0f - ((Component)main).transform.position.y) * 40f);
			int num = (int)(main.orthographicSize * 40f);
			int num2 = (int)((float)num * main.aspect);
			Cull(cx, num2);
			Accumulate(sky, age, dt, now, cx, cy, num2, num);
			Puddles(sky, age, dt, cx, cy, num2, num);
			RainSplashes(rain, dt, cx, cy, num2, num, rainTint);
		}
	}

	public void Clear()
	{
		foreach (Deposit deposit in deposits)
		{
			Remove(deposit.Art);
		}
		deposits.Clear();
		foreach (Puddle puddle in puddles)
		{
			Remove(puddle.Art);
		}
		puddles.Clear();
		foreach (Print print in prints)
		{
			Remove(print.Art);
		}
		prints.Clear();
		foreach (NativeImgAnimation splash in splashes)
		{
			Remove(splash);
		}
		splashes.Clear();
		snowLevel = (wetLevel = (snowCarry = (wetCarry = (splashCarry = 0f))));
		snowCatchUp = (wetCatchUp = 0);
		levelKind = (seededFor = null);
		wetSeeded = (wasGrounded = (wasWet = false));
		lastPrintX = int.MinValue;
		lastFrame = 0L;
	}

	private void OnDisable()
	{
		Clear();
	}
}
