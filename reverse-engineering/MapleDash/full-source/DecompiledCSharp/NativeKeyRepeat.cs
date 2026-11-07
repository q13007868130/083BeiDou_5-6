using System;

public sealed class NativeKeyRepeat
{
	private float since;

	private bool repeating;

	public float Delay { get; private set; }

	public float Interval { get; private set; }

	public int Key { get; private set; }

	public NativeKeyRepeat()
	{
		Configure(3, 0);
	}

	public void Configure(int keyboardDelay, int keyboardSpeed)
	{
		Delay = (float)Math.Clamp((keyboardDelay + 1) * 250, 250, 1000) / 1000f;
		Interval = (float)Math.Clamp(400 - keyboardSpeed * 12, 33, 400) / 1000f;
		Reset();
	}

	public void Reset()
	{
		Key = 0;
		repeating = false;
		since = 0f;
	}

	public void Press(int key, float now)
	{
		Key = key;
		since = now;
		repeating = false;
	}

	public bool Poll(bool held, float now)
	{
		if (!held)
		{
			Reset();
			return false;
		}
		if (Key == 0 || now - since + 1E-06f < (repeating ? Interval : Delay))
		{
			return false;
		}
		since = now;
		repeating = true;
		return true;
	}
}
