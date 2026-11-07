using System;
using UnityEngine;

public static class DamageText
{
	public static void Show(Vector3 worldPos, long amount, string skin = "red", float scale = 1f)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		NativeDamageNumber.Show(worldPos, Math.Max(0L, amount), skin, scale);
	}

	public static void Show(Vector3 worldPos, float amount, string skin = "red", float scale = 1f)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		long amount2 = (long)Math.Max(0.0, Math.Round(amount));
		NativeDamageNumber.Show(worldPos, amount2, skin, scale);
	}

	public static void Miss(Vector3 worldPos, float scale = 1f, string skin = "red")
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		NativeDamageNumber.Miss(worldPos, skin, scale);
	}
}
