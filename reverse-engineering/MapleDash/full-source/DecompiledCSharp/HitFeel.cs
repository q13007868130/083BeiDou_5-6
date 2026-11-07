using System.Runtime.CompilerServices;
using UnityEngine;

public static class HitFeel
{
	public static float StopMs = 60f;

	public static float ShakeAmp = 0.18f;

	public static float Knockback = 0.9f;

	public static float FlashDur = 0.06f;

	private static float _stopUntil;

	private static float _shakeUntil;

	private static float _shakeNow;

	private static float _shakeDur = 0.14f;

	public static Vector2 CameraOffset
	{
		[CompilerGenerated]
		get
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			return field;
		}
		[CompilerGenerated]
		private set
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			field = value;
		}
	}

	public static void Punch(float weight = 1f)
	{
		if (StopMs > 0f)
		{
			float num = Time.unscaledTime + StopMs / 1000f;
			if (num > _stopUntil)
			{
				_stopUntil = num;
			}
			Time.timeScale = 0f;
		}
		Shake(ShakeAmp * weight, 0.14f);
	}

	public static void Shake(float worldAmplitude, float seconds)
	{
		if (!(worldAmplitude <= 0f) && ClientSettings.ScreenEffects)
		{
			_shakeNow = worldAmplitude;
			_shakeDur = Mathf.Max(0.05f, seconds);
			_shakeUntil = Time.unscaledTime + _shakeDur;
		}
	}

	public static void Reset()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		Time.timeScale = 1f;
		_stopUntil = 0f;
		_shakeUntil = 0f;
		_shakeNow = 0f;
		CameraOffset = Vector2.zero;
	}

	public static void Tick()
	{
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		if (Time.timeScale == 0f && Time.unscaledTime >= _stopUntil)
		{
			Time.timeScale = 1f;
		}
		if (Time.unscaledTime < _shakeUntil && _shakeNow > 0f)
		{
			float num = Mathf.InverseLerp(_shakeUntil, _shakeUntil - _shakeDur, Time.unscaledTime);
			float num2 = _shakeNow * num;
			CameraOffset = new Vector2(Random.Range(0f - num2, num2), Random.Range(0f - num2, num2));
		}
		else
		{
			CameraOffset = Vector2.zero;
		}
	}
}
