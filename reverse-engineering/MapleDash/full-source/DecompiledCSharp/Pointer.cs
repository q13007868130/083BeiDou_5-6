using System;
using UnityEngine;

public static class Pointer
{
	public struct InputMask : IDisposable
	{
		private bool active;

		private readonly Vector2 pos;

		private readonly Vector2 press;

		private readonly bool down;

		private readonly bool pressed;

		private readonly bool released;

		private readonly bool clicked;

		private readonly bool twice;

		private readonly bool rightDown;

		private readonly bool rightPressed;

		private readonly bool rightReleased;

		private readonly bool dragging;

		private readonly bool dragStarted;

		private readonly bool dragEnded;

		public InputMask(bool hide)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_00af: Unknown result type (might be due to invalid IL or missing references)
			active = hide;
			pos = Pos;
			press = PressPosition;
			down = Down;
			pressed = Pressed;
			released = Released;
			clicked = Clicked;
			twice = DoubleClicked;
			rightDown = RightDown;
			rightPressed = RightPressed;
			rightReleased = RightReleased;
			dragging = Dragging;
			dragStarted = DragStarted;
			dragEnded = DragEnded;
			if (hide)
			{
				Pos = (PressPosition = new Vector2(-10000f, -10000f));
				Down = (Pressed = (Released = (Clicked = (DoubleClicked = false))));
				RightDown = (RightPressed = (RightReleased = false));
				Dragging = (DragStarted = (DragEnded = false));
			}
		}

		public void Dispose()
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			if (active)
			{
				active = false;
				Pos = pos;
				PressPosition = press;
				Down = down;
				Pressed = pressed;
				Released = released;
				Clicked = clicked;
				DoubleClicked = twice;
				RightDown = rightDown;
				RightPressed = rightPressed;
				RightReleased = rightReleased;
				Dragging = dragging;
				DragStarted = dragStarted;
				DragEnded = dragEnded;
			}
		}
	}

	public static Vector2 Pos;

	public static bool Down;

	public static bool Pressed;

	public static bool Released;

	public static bool RightDown;

	public static bool RightPressed;

	public static bool RightReleased;

	public static bool Clicked;

	public static bool DoubleClicked;

	public static bool Dragging;

	public static bool DragStarted;

	public static bool DragEnded;

	public static Vector2 PressPosition;

	public static bool Blocked;

	public static bool Suppressed;

	private static float lastClickTime = -10f;

	private static Vector2 lastClickPosition;

	private static bool doubleCandidate;

	private static int frame = -1;

	private static bool touchGesture;

	private static bool touchMoved;

	private static bool touchSecondary;

	private static bool touchHold;

	private static bool touchOnControl;

	private static bool mouseOnControl;

	private static int touchId;

	private static float touchSince;

	private static Vector2 touchStart;

	private static bool initialized;

	public static bool UsesVirtualCursor { get; private set; }

	public static void ConsumeClick()
	{
		Clicked = (DoubleClicked = (Released = false));
	}

	public static bool HitsWindow(Rect rect)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		if (!rect.Contains(Pos))
		{
			if (Down || Released || DragEnded)
			{
				return rect.Contains(PressPosition);
			}
			return false;
		}
		return true;
	}

	private static void ReadDevice(out bool left, out bool right)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_032a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Invalid comparison between Unknown and I4
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Invalid comparison between Unknown and I4
		//IL_034a: Unknown result type (might be due to invalid IL or missing references)
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Invalid comparison between Unknown and I4
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Invalid comparison between Unknown and I4
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Invalid comparison between Unknown and I4
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Invalid comparison between Unknown and I4
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		left = (right = false);
		if (!initialized)
		{
			initialized = true;
			Pos = new Vector2(UiKit.W * 0.5f, UiKit.H * 0.5f);
			UsesVirtualCursor = Application.isMobilePlatform;
		}
		if (!Application.isFocused)
		{
			touchGesture = (touchHold = (touchOnControl = false));
			mouseOnControl = false;
			return;
		}
		if (Input.touchCount > 0)
		{
			UsesVirtualCursor = true;
			if (!touchGesture)
			{
				Touch touch = Input.GetTouch(0);
				if ((int)touch.phase != 0)
				{
					return;
				}
				touchGesture = true;
				touchId = touch.fingerId;
				touchStart = touch.position;
				touchSince = Time.unscaledTime;
				touchMoved = (touchSecondary = (touchHold = false));
				touchOnControl = VirtualControls.HitsAtScreen(touch.position);
			}
			if (touchOnControl)
			{
				bool flag = false;
				for (int i = 0; i < Input.touchCount; i++)
				{
					Touch touch2 = Input.GetTouch(i);
					if ((int)touch2.phase != 3 && (int)touch2.phase != 4)
					{
						flag = true;
					}
				}
				if (!flag)
				{
					touchGesture = false;
					touchOnControl = false;
				}
				return;
			}
			int num = 0;
			bool flag2 = false;
			for (int j = 0; j < Input.touchCount; j++)
			{
				Touch touch3 = Input.GetTouch(j);
				if ((int)touch3.phase == 4)
				{
					flag2 = true;
				}
				if ((int)touch3.phase != 3 && (int)touch3.phase != 4)
				{
					num++;
				}
				if (touch3.fingerId == touchId)
				{
					Vector2 val = touch3.position - touchStart;
					if (val.sqrMagnitude > 64f * UiKit.Scale * UiKit.Scale)
					{
						touchMoved = true;
					}
					if (!touchSecondary && (int)touch3.phase != 0 && (int)touch3.phase != 4)
					{
						Pos += new Vector2(touch3.deltaPosition.x, 0f - touch3.deltaPosition.y) / UiKit.Scale;
					}
				}
			}
			if (Input.touchCount >= 2 && !touchHold)
			{
				touchSecondary = true;
			}
			if (flag2)
			{
				touchGesture = (touchHold = (touchOnControl = false));
				return;
			}
			if (num == 0)
			{
				if (touchHold)
				{
					left = false;
				}
				else if (!touchMoved && Time.unscaledTime - touchSince <= 0.35f)
				{
					right = touchSecondary;
					left = !touchSecondary;
				}
				touchGesture = (touchHold = false);
			}
			else
			{
				if (!touchSecondary && !touchMoved && Time.unscaledTime - touchSince >= 0.35f)
				{
					touchHold = true;
				}
				left = touchHold;
			}
		}
		else
		{
			touchGesture = (touchHold = false);
			if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Mathf.Abs(Input.GetAxisRaw("Mouse X")) > 0f || Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > 0f)
			{
				UsesVirtualCursor = false;
			}
			if (!UsesVirtualCursor)
			{
				Pos = new Vector2(Input.mousePosition.x / UiKit.Scale, ((float)Screen.height - Input.mousePosition.y) / UiKit.Scale);
				bool mouseButton = Input.GetMouseButton(0);
				if (!mouseButton)
				{
					mouseOnControl = false;
				}
				else if (!mouseOnControl && VirtualControls.HitsAtScreen(Vector2.op_Implicit(Input.mousePosition)))
				{
					mouseOnControl = true;
				}
				left = mouseButton && !mouseOnControl;
				right = Input.GetMouseButton(1);
			}
		}
		Pos = new Vector2(Mathf.Clamp(Pos.x, 0f, UiKit.W - 1f), Mathf.Clamp(Pos.y, 0f, UiKit.H - 1f));
	}

	public static void Tick()
	{
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		if (frame == Time.frameCount)
		{
			return;
		}
		frame = Time.frameCount;
		if (Suppressed)
		{
			Pressed = (Released = (RightPressed = (RightReleased = false)));
			Clicked = (DoubleClicked = (DragStarted = (DragEnded = false)));
			Down = (RightDown = false);
			Dragging = false;
			doubleCandidate = false;
			touchGesture = (touchHold = (touchOnControl = false));
			mouseOnControl = false;
			lastClickTime = -10f;
			return;
		}
		ReadDevice(out var left, out var right);
		bool isFocused = Application.isFocused;
		Pressed = left && !Down;
		Released = !left && Down;
		RightPressed = right && !RightDown;
		RightReleased = !right && RightDown;
		Down = left;
		RightDown = right;
		Clicked = (DoubleClicked = (DragStarted = (DragEnded = false)));
		Vector2 val;
		if (Pressed)
		{
			PressPosition = Pos;
			int num;
			if (Time.unscaledTime - lastClickTime <= 0.5f)
			{
				val = Pos - lastClickPosition;
				num = ((val.sqrMagnitude <= 256f) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			doubleCandidate = (byte)num != 0;
		}
		if ((Down || Released) && !Dragging)
		{
			val = Pos - PressPosition;
			if (val.sqrMagnitude > 16f)
			{
				DragStarted = (Dragging = true);
			}
		}
		if (Released)
		{
			DragEnded = Dragging;
			Clicked = isFocused && !Dragging;
			DoubleClicked = Clicked && doubleCandidate;
			lastClickTime = ((Clicked && !DoubleClicked) ? Time.unscaledTime : (-10f));
			lastClickPosition = Pos;
			Dragging = false;
		}
		if (!isFocused)
		{
			Released = (RightReleased = false);
			Dragging = false;
			lastClickTime = -10f;
		}
	}
}
