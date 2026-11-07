using System;
using UnityEngine;

internal static class FxTex
{
	private const int N = 64;

	private static Sprite _glow;

	private static Sprite _ring;

	private static Sprite _arc;

	private static Sprite Build(Func<float, float, float> alpha)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected Obj, but got Unknown
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = new Texture2D(64, 64, (TextureFormat)4, false);
		Color[] array = new Color[4096];
		for (int i = 0; i < 64; i++)
		{
			for (int j = 0; j < 64; j++)
			{
				array[i * 64 + j] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha(((float)j + 0.5f) / 64f, ((float)i + 0.5f) / 64f)));
			}
		}
		val.SetPixels(array);
		((Texture)val).filterMode = (FilterMode)1;
		((Texture)val).wrapMode = (TextureWrapMode)1;
		val.Apply();
		return Sprite.Create(val, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 64f);
	}

	public static Sprite Glow()
	{
		if ((Object)(object)_glow != (Object)null)
		{
			return _glow;
		}
		_glow = Build((float u, float v) =>
		{
			float num = u - 0.5f;
			float num2 = v - 0.5f;
			float num3 = Mathf.Sqrt(num * num + num2 * num2) * 2f;
			return Mathf.Pow(Mathf.Clamp01(1f - num3), 2.2f);
		});
		return _glow;
	}

	public static Sprite Ring()
	{
		if ((Object)(object)_ring != (Object)null)
		{
			return _ring;
		}
		_ring = Build((float u, float v) =>
		{
			float num = u - 0.5f;
			float num2 = v - 0.5f;
			float num3 = Mathf.Sqrt(num * num + num2 * num2) * 2f;
			float num4 = Mathf.Clamp01(1f - Mathf.Abs(num3 - 0.84f) / 0.16f);
			return num4 * num4;
		});
		return _ring;
	}

	public static Sprite Arc()
	{
		if ((Object)(object)_arc != (Object)null)
		{
			return _arc;
		}
		_arc = Build((float u, float v) =>
		{
			float num = u - 0.5f;
			float num2 = v - 0.5f;
			float num3 = Mathf.Sqrt(num * num + num2 * num2) / 0.48f;
			float num4 = num + 0.2f;
			float num5 = Mathf.Sqrt(num4 * num4 + num2 * num2) / 0.44f;
			return Mathf.Clamp01((1f - num3) / 0.16f) * Mathf.Clamp01((num5 - 0.9f) / 0.16f);
		});
		return _arc;
	}
}
