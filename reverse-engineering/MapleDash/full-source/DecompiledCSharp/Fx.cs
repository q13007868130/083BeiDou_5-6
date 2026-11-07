using UnityEngine;

public static class Fx
{
	public static void Slash(Vector3 pos, Color c, int facing)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		SpriteRenderer val = Quad("FxSlash", pos, FxTex.Arc(), new Vector2(1.9f, 1.7f), c, 400);
		((Component)val).transform.localScale = new Vector3(1.9f * ((facing >= 0) ? 1f : (-1f)), 1.7f, 1f);
		FxLife fxLife = ((Component)val).gameObject.AddComponent<FxLife>();
		fxLife.life = 0.13f;
		fxLife.grow = new Vector3(1.45f, 1.25f, 1f);
		fxLife.drift = new Vector3((float)facing * 1.2f, 0f, 0f);
	}

	public static void Burst(Vector3 pos, float radius, Color c, float life = 0.22f)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		FxLife fxLife = ((Component)Quad("FxBurst", pos, FxTex.Glow(), new Vector2(radius * 2.4f, radius * 2.4f), c, 350)).gameObject.AddComponent<FxLife>();
		fxLife.life = life;
		fxLife.grow = new Vector3(0.15f, 0.15f, 1f);
	}

	public static void Ring(Vector3 pos, float radius, Color c, float life = 0.28f)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		FxLife fxLife = ((Component)Quad("FxRing", pos, FxTex.Ring(), new Vector2(radius * 2f, radius * 2f), c, 350)).gameObject.AddComponent<FxLife>();
		fxLife.life = life;
		fxLife.grow = new Vector3(2.2f, 2.2f, 1f);
	}

	public static void Sparkle(Vector3 pos, Color c, float size = 0.4f, float life = 0.18f)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		FxLife fxLife = ((Component)Quad("FxSparkle", pos, FxTex.Glow(), new Vector2(size, size), c, 380)).gameObject.AddComponent<FxLife>();
		fxLife.life = life;
		fxLife.grow = new Vector3(1.7f, 1.7f, 1f);
	}

	public static SpriteRenderer Quad(string name, Vector3 pos, Sprite sp, Vector2 size, Color c, int order)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected Obj, but got Unknown
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name);
		val.transform.position = pos;
		SpriteRenderer val2 = val.AddComponent<SpriteRenderer>();
		val2.sprite = sp;
		val2.color = c;
		((Renderer)val2).sortingOrder = order;
		val.transform.localScale = new Vector3(size.x, size.y, 1f);
		return val2;
	}

	public static void Flash(SpriteRenderer sr, Color flash)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)sr == (Object)null))
		{
			FxFlash fxFlash = ((Component)sr).gameObject.GetComponent<FxFlash>();
			if ((Object)(object)fxFlash == (Object)null)
			{
				fxFlash = ((Component)sr).gameObject.AddComponent<FxFlash>();
			}
			fxFlash.Play(flash, HitFeel.FlashDur);
		}
	}
}
