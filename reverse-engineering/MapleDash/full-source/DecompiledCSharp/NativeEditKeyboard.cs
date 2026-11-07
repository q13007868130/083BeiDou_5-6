using UnityEngine;

public static class NativeEditKeyboard
{
	public enum Signal
	{
		None,
		Done,
		Canceled
	}

	private static TouchScreenKeyboard keyboard;

	private static string owner;

	private static int selectAllRemaining;

	private static int selectAllLen;

	public static bool Active
	{
		get
		{
			if (keyboard != null)
			{
				return keyboard.active;
			}
			return false;
		}
	}

	public static bool Owns(string key)
	{
		return owner == key;
	}

	public static Signal Sync(string key, ref string value)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		if (keyboard == null || owner != key)
		{
			return Signal.None;
		}
		Signal signal = Apply(ref value, keyboard.text, keyboard.status);
		if (selectAllRemaining > 0)
		{
			try
			{
				keyboard.selection = new RangeInt(0, selectAllLen);
			}
			catch
			{
			}
			selectAllRemaining--;
		}
		if (signal != Signal.None)
		{
			Close();
		}
		return signal;
	}

	public static Signal Apply(ref string value, string text, Status status)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		if (text != null)
		{
			value = text;
		}
		if ((int)status == 1)
		{
			return Signal.Done;
		}
		if ((int)status == 2)
		{
			return Signal.Canceled;
		}
		return Signal.None;
	}

	public static bool ShouldReopen(string currentOwner, bool currentActive, string key)
	{
		if (currentActive)
		{
			return !(currentOwner == key);
		}
		return true;
	}

	public static void Open(string key, string value, TouchScreenKeyboardType type, int characterLimit, bool selectAll = false)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		if (Application.isMobilePlatform && ShouldReopen(owner, keyboard != null && keyboard.active, key))
		{
			if (keyboard != null)
			{
				keyboard.active = false;
			}
			keyboard = TouchScreenKeyboard.Open(value ?? "", type, false, false, false, false, "", characterLimit);
			owner = key;
			if (selectAll)
			{
				selectAllRemaining = 12;
				selectAllLen = (value ?? "").Length;
			}
		}
	}

	public static void Close()
	{
		if (keyboard != null)
		{
			keyboard.active = false;
		}
		keyboard = null;
		owner = null;
		selectAllRemaining = 0;
	}
}
