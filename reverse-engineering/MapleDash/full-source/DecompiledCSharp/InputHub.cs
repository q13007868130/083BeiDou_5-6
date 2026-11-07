using System.Collections.Generic;
using UnityEngine;

public static class InputHub
{
	private static readonly NativeKeyRepeat repeat = new NativeKeyRepeat();

	private static readonly SortedDictionary<int, KeyCode> repeatKeys = BuildRepeatKeys();

	private static int repeatedKey;

	private static readonly HashSet<KeyCode> virtualHeld = new HashSet<KeyCode>();

	private static readonly HashSet<KeyCode> virtualPressed = new HashSet<KeyCode>();

	private static readonly HashSet<KeyCode> virtualReleaseNext = new HashSet<KeyCode>();

	private static readonly HashSet<GameAction> virtualActionsHeld = new HashSet<GameAction>();

	private static readonly HashSet<GameAction> virtualActionsPressed = new HashSet<GameAction>();

	public static bool Enabled = true;

	private static Vector2 virtualAxis;

	private static bool virtualAxisActive;

	private static int previousAxisKeys;

	public static float AxisX
	{
		get
		{
			if (virtualAxisActive)
			{
				return virtualAxis.x;
			}
			float num = 0f;
			if (Held(GameAction.Left))
			{
				num--;
			}
			if (Held(GameAction.Right))
			{
				num++;
			}
			return num;
		}
	}

	public static float AxisY
	{
		get
		{
			if (virtualAxisActive)
			{
				return virtualAxis.y;
			}
			float num = 0f;
			if (Held(GameAction.Down))
			{
				num--;
			}
			if (Held(GameAction.Up))
			{
				num++;
			}
			return num;
		}
	}

	private static SortedDictionary<int, KeyCode> BuildRepeatKeys()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		SortedDictionary<int, KeyCode> sortedDictionary = new SortedDictionary<int, KeyCode>();
		foreach (KeyCode physicalKey in NativeKeyLayout.PhysicalKeys)
		{
			int num = NativeKeyLayout.VirtualKey(physicalKey);
			if (num != 0)
			{
				sortedDictionary[num] = NativeKeyLayout.FunctionKey(physicalKey);
			}
		}
		return sortedDictionary;
	}

	private static bool KeyHeld(KeyCode key)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		KeyCode val = NativeKeyLayout.OtherModifier(key);
		if (!Input.GetKey(key) && ((int)val == 0 || !Input.GetKey(val)))
		{
			return virtualHeld.Contains(key);
		}
		return true;
	}

	private static bool KeyPressed(KeyCode key)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		KeyCode val = NativeKeyLayout.OtherModifier(key);
		if (!Input.GetKeyDown(key) && ((int)val == 0 || !Input.GetKeyDown(val)))
		{
			return virtualPressed.Contains(key);
		}
		return true;
	}

	private static int RepeatId(GameAction action)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		KeyCode val = KeyMap.Of(action);
		if ((int)val != 0)
		{
			return NativeKeyLayout.VirtualKey(val);
		}
		return (int)(256 + action);
	}

	public static void TickRepeat(float now, bool active)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		repeatedKey = 0;
		if (!active || !Enabled)
		{
			repeat.Reset();
			previousAxisKeys = 0;
			return;
		}
		int num = 0;
		foreach (KeyValuePair<int, KeyCode> repeatKey in repeatKeys)
		{
			if (KeyPressed(repeatKey.Value))
			{
				num = repeatKey.Key;
			}
		}
		int num2 = (int)(((virtualAxis.x < -0.15f) ? 1u : 0u) | (uint)((virtualAxis.y > 0.15f) ? 2 : 0) | (uint)((virtualAxis.x > 0.15f) ? 4 : 0)) | ((virtualAxis.y < -0.15f) ? 8 : 0);
		for (int i = 0; i < 4; i++)
		{
			if ((num2 & ~previousAxisKeys & (1 << i)) != 0)
			{
				num = Mathf.Max(num, 37 + i);
			}
		}
		previousAxisKeys = num2;
		foreach (GameAction item in virtualActionsPressed)
		{
			num = Mathf.Max(num, RepeatId(item));
		}
		if (num != 0)
		{
			repeat.Press(num, now);
		}
		bool held = repeatKeys.TryGetValue(repeat.Key, out var value) && KeyHeld(value);
		if (repeat.Key >= 37 && repeat.Key <= 40 && (num2 & (1 << repeat.Key - 37)) != 0)
		{
			held = true;
		}
		foreach (GameAction item2 in virtualActionsHeld)
		{
			if (RepeatId(item2) == repeat.Key)
			{
				held = true;
			}
		}
		if (repeat.Poll(held, now))
		{
			repeatedKey = repeat.Key;
		}
	}

	public static bool Repeated(GameAction action)
	{
		if (Enabled && repeatedKey != 0)
		{
			return repeatedKey == RepeatId(action);
		}
		return false;
	}

	public static void SetVirtualAxis(float x, float y)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		virtualAxis = new Vector2(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f));
		virtualAxisActive = virtualAxis.sqrMagnitude > 0.0001f;
	}

	public static void ClearVirtualAxis()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		virtualAxis = Vector2.zero;
		virtualAxisActive = false;
	}

	public static void SetVirtual(KeyCode k, bool down)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		k = NativeKeyLayout.FunctionKey(k);
		if ((int)k == 0)
		{
			return;
		}
		if (down)
		{
			if (virtualHeld.Add(k))
			{
				virtualPressed.Add(k);
			}
			virtualReleaseNext.Remove(k);
		}
		else
		{
			virtualHeld.Remove(k);
		}
	}

	public static bool KeyDown(KeyCode key)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		if (Enabled)
		{
			return KeyPressed(key);
		}
		return false;
	}

	public static bool Held(GameAction a)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		if (!Enabled)
		{
			return false;
		}
		if (virtualActionsHeld.Contains(a))
		{
			return true;
		}
		KeyCode val = KeyMap.Of(a);
		if ((int)val == 0)
		{
			return false;
		}
		KeyCode val2 = NativeKeyLayout.OtherModifier(val);
		if (!Input.GetKey(val) && ((int)val2 == 0 || !Input.GetKey(val2)))
		{
			return virtualHeld.Contains(val);
		}
		return true;
	}

	public static bool Pressed(GameAction a)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		if (!Enabled)
		{
			return false;
		}
		if (virtualActionsPressed.Contains(a))
		{
			return true;
		}
		KeyCode val = KeyMap.Of(a);
		if ((int)val == 0)
		{
			return false;
		}
		KeyCode val2 = NativeKeyLayout.OtherModifier(val);
		if (!Input.GetKeyDown(val) && ((int)val2 == 0 || !Input.GetKeyDown(val2)))
		{
			return virtualPressed.Contains(val);
		}
		return true;
	}

	public static void SetVirtualAction(GameAction a, bool down)
	{
		if (down)
		{
			if (virtualActionsHeld.Add(a))
			{
				virtualActionsPressed.Add(a);
			}
		}
		else
		{
			virtualActionsHeld.Remove(a);
		}
	}

	public static void EndFrame()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		virtualPressed.Clear();
		virtualActionsPressed.Clear();
		foreach (KeyCode item in virtualReleaseNext)
		{
			virtualHeld.Remove(item);
		}
		virtualReleaseNext.Clear();
	}

	public static void ReleaseAllVirtual()
	{
		repeat.Reset();
		repeatedKey = 0;
		previousAxisKeys = 0;
		virtualHeld.Clear();
		virtualPressed.Clear();
		virtualReleaseNext.Clear();
		virtualActionsHeld.Clear();
		virtualActionsPressed.Clear();
		ClearVirtualAxis();
	}
}
