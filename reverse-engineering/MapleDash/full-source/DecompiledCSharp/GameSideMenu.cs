using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameSideMenu
{
	private enum BallEdge
	{
		Left,
		Right,
		Top,
		Bottom
	}

	private enum BallState
	{
		Snapped,
		Full,
		Dragging
	}

	private sealed class MenuEntry
	{
		public string glyph;

		public string glyphOff;

		public string title;

		public string desc;

		public bool gameplayOnly;

		public bool touchOnly;

		public Action run;

		public Func<bool> state;

		public string descOn;

		public string descOff;

		public bool IsSwitch => state != null;

		public string DescNow
		{
			get
			{
				if (!IsSwitch)
				{
					return desc;
				}
				if (!state())
				{
					return descOff;
				}
				return descOn;
			}
		}

		public string GlyphNow
		{
			get
			{
				if (!IsSwitch || state() || string.IsNullOrEmpty(glyphOff))
				{
					return glyph;
				}
				return glyphOff;
			}
		}
	}

	private const float PanelMaxW = 360f;

	private const float HeaderH = 96f;

	private const float SectionH = 34f;

	private const float EntryH = 88f;

	private const float EntryGap = 12f;

	private const float Inset = 18f;

	private const float CloseDrag = 64f;

	private const float TapSlop = 10f;

	private const float OpenTime = 0.16f;

	private const float CloseTime = 0.13f;

	private const float BallR = 32f;

	private const float BallHitR = 44f;

	private const float BallFullGapDp = 20f;

	private const float BallIdleAlpha = 0.85f;

	private const float BallFadeDelay = 1f;

	private const float BallIdleReturn = 5f;

	private const float BallTapSlop = 10f;

	private static readonly Color PanelBg = new Color(0.045f, 0.05f, 0.064f, 0.975f);

	private static readonly Color PanelHead = new Color(0.07f, 0.078f, 0.1f, 0.985f);

	private static readonly Color CardBg = new Color(0.095f, 0.105f, 0.135f, 0.98f);

	private static readonly Color CardHot = new Color(0.125f, 0.165f, 0.225f, 1f);

	private static readonly Color CardOff = new Color(0.07f, 0.075f, 0.09f, 0.98f);

	private static readonly Color IconBg = new Color(0.075f, 0.245f, 0.44f, 1f);

	private static readonly Color Accent = new Color(0.2f, 0.72f, 1f, 1f);

	private static readonly Color TxtMain = new Color(0.93f, 0.95f, 0.98f, 1f);

	private static readonly Color TxtDim = new Color(0.62f, 0.66f, 0.74f, 1f);

	private static readonly Color BallBody = new Color(1f, 0.32f, 0.045f, 1f);

	private static readonly Color BallRing = new Color(1f, 0.94f, 0.78f, 1f);

	private static readonly Color BallIcon = Color.white;

	private static readonly MenuEntry[] items = new MenuEntry[5]
	{
		new MenuEntry
		{
			glyph = "显",
			glyphOff = "隐",
			title = "虚拟按键",
			descOn = "显示中 · 点击隐藏",
			descOff = "已隐藏 · 点击显示",
			gameplayOnly = false,
			touchOnly = true,
			state = () => VirtualControls.OverlayVisible,
			run = ToggleVirtualControls
		},
		new MenuEntry
		{
			glyph = "键",
			title = "按键布局",
			desc = "调整位置、大小与样式",
			gameplayOnly = false,
			touchOnly = true,
			run = OpenVirtualEditor
		},
		new MenuEntry
		{
			glyph = "记",
			glyphOff = "停",
			title = "日志记录",
			descOn = "写入日志文件 · 点击关闭",
			descOff = "已关闭 · 点击开启",
			gameplayOnly = false,
			state = () => FileLog.Enabled,
			run = ToggleFileLog
		},
		new MenuEntry
		{
			glyph = "享",
			title = "分享日志",
			desc = "选择日期日志，转发给开发者分析",
			gameplayOnly = false,
			run = LogShare.Share
		},
		new MenuEntry
		{
			glyph = "后",
			title = "后台系统",
			desc = "浏览器打开服务器管理后台",
			gameplayOnly = false,
			run = OpenAdminConsole
		}
	};

	private static float slide;

	private static float openedAt;

	private static bool fromRight;

	private static bool swallow;

	private static List<MenuEntry> entries;

	private static bool pressing;

	private static bool pressMouse;

	private static int pressFinger = -1;

	private static Vector2 pressStart;

	private static Vector2 pressPos;

	private static bool pressOutside;

	private static bool pressDragged;

	private static int hotEntry = -1;

	private static bool ballPlaced;

	private static BallEdge ballEdge = BallEdge.Right;

	private static BallState ballState = BallState.Snapped;

	private static Vector2 ballPos;

	private static Vector2 ballTarget;

	private static float ballOpacity = 1f;

	private static float ballSnapAt;

	private static float ballLastTouch;

	private static bool ballPressing;

	private static bool ballDragging;

	private static bool ballWasFull;

	private static bool ballMouse;

	private static int ballFinger = -1;

	private static Vector2 ballPressPos;

	private static Vector2 ballGrab;

	private static bool editorWasVisible;

	private static GameLoop.GameState lastState;

	private static bool stateKnown;

	public static bool Visible { get; private set; }

	public static bool Blocking
	{
		get
		{
			if (!Visible && !(slide > 0.001f) && !swallow)
			{
				return ballPressing;
			}
			return true;
		}
	}

	private static List<MenuEntry> Entries
	{
		get
		{
			if (entries == null)
			{
				entries = new List<MenuEntry>(items.Length);
				MenuEntry[] array = items;
				foreach (MenuEntry menuEntry in array)
				{
					if (!menuEntry.touchOnly || GamePlatform.TouchControls)
					{
						entries.Add(menuEntry);
					}
				}
			}
			return entries;
		}
	}

	private static float PanelW => Mathf.Min(360f, Mathf.Max(220f, UiKit.W * 0.46f));

	private static float BallFullGap
	{
		get
		{
			float num = ((Screen.dpi > 1f) ? (Screen.dpi / 160f) : 1f);
			return Mathf.Max(6f, 20f * num / Mathf.Max(0.01f, UiKit.Scale));
		}
	}

	public static void Tick()
	{
		LogShare.Tick();
		GameLoop i = GameLoop.I;
		if ((Object)(object)i == (Object)null || i.state != GameLoop.GameState.Playing)
		{
			ResetAll();
			return;
		}
		if (!stateKnown || lastState != i.state)
		{
			stateKnown = true;
			lastState = i.state;
			if (Visible)
			{
				Close(immediate: true);
			}
			ResetBallPress();
		}
		if (VirtualControlsEditor.Visible)
		{
			HideForEditor();
			return;
		}
		if (editorWasVisible)
		{
			editorWasVisible = false;
			RevealBall();
		}
		EnsureBall();
		if (!Application.isFocused)
		{
			ResetBallPress();
			Pointer.Suppressed = Blocking;
			return;
		}
		if (Visible)
		{
			if (Input.GetKeyDown((KeyCode)27) && Time.unscaledTime - openedAt > 0.4f)
			{
				Close(immediate: false);
			}
			else
			{
				PollPanel();
			}
		}
		else if (slide <= 0.001f)
		{
			PollBall();
			if (i.state == GameLoop.GameState.Playing && !Visible && Input.GetKeyDown((KeyCode)289))
			{
				OpenVirtualEditor();
			}
			if (!Visible && !ballPressing && ballState == BallState.Full && Time.unscaledTime - ballLastTouch >= 5f)
			{
				SnapBall();
			}
		}
		float num = 1f / (Visible ? 0.16f : 0.13f);
		slide = Mathf.MoveTowards(slide, Visible ? 1f : 0f, Time.unscaledDeltaTime * num);
		UpdateBall();
		if (swallow && !RawPointerDown())
		{
			swallow = false;
		}
		Pointer.Suppressed = Blocking;
	}

	private static void ResetAll()
	{
		Visible = false;
		slide = 0f;
		swallow = false;
		fromRight = false;
		openedAt = 0f;
		ResetPress();
		ballPlaced = false;
		ballPressing = false;
		ballDragging = false;
		ballWasFull = false;
		ballMouse = false;
		ballFinger = -1;
		editorWasVisible = false;
		stateKnown = false;
		AndroidBallGesture.Clear();
		Pointer.Suppressed = false;
	}

	private static void HideForEditor()
	{
		editorWasVisible = true;
		if (Visible)
		{
			Visible = false;
			slide = 0f;
			ResetPress();
		}
		ResetBallPress();
		swallow = false;
		AndroidBallGesture.Clear();
		Pointer.Suppressed = false;
	}

	private static void OpenPanel()
	{
		Open(ballEdge == BallEdge.Right || (ballEdge != BallEdge.Left && ballPos.x >= UiKit.W * 0.5f));
	}

	private static void Open(bool right)
	{
		Visible = true;
		fromRight = right;
		openedAt = Time.unscaledTime;
		slide = Mathf.Max(slide, 0.001f);
		swallow = true;
		ResetPress();
		AndroidBallGesture.Clear();
	}

	private static void Close(bool immediate)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		Visible = false;
		ResetPress();
		ResetBallPress();
		if (immediate)
		{
			slide = 0f;
		}
		swallow = RawPointerDown();
		if (ballPlaced)
		{
			ballState = BallState.Full;
			ballTarget = FullPos(ballEdge, ballPos);
			ballLastTouch = Time.unscaledTime;
		}
		AndroidBallGesture.Clear();
	}

	private static void OpenVirtualEditor()
	{
		if (!((Object)(object)GameLoop.I == (Object)null) && GameLoop.I.state == GameLoop.GameState.Playing)
		{
			Close(immediate: true);
			if (!VirtualControlsEditor.Visible)
			{
				VirtualControlsEditor.Toggle();
			}
		}
	}

	private static void OpenAdminConsole()
	{
		try
		{
			string text = AdminHost.OpenInBrowser();
			BottomBar.Toast("已打开后台系统 " + text);
		}
		catch (Exception ex)
		{
			BottomBar.Toast("后台系统启动失败：" + ex.Message);
		}
	}

	private static void ToggleVirtualControls()
	{
		VirtualControls.OverlayVisible = !VirtualControls.OverlayVisible;
		BottomBar.Toast(VirtualControls.OverlayVisible ? "虚拟按键已显示" : "虚拟按键已隐藏");
	}

	private static void ToggleFileLog()
	{
		BottomBar.Toast(FileLog.Toggle() ? ("日志记录已开启 · 写入 " + FileLog.Dir) : "日志记录已关闭");
	}

	private static bool RawPointerDown()
	{
		if (Input.touchCount <= 0)
		{
			return Input.GetMouseButton(0);
		}
		return true;
	}

	private static void EnsureBall()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if (!ballPlaced)
		{
			ballPlaced = true;
			ballEdge = BallEdge.Right;
			ballPos = (ballTarget = SnappedPos(ballEdge, new Vector2(UiKit.W, UiKit.H * 0.18f)));
			ballState = BallState.Snapped;
			ballOpacity = 1f;
			ballSnapAt = (ballLastTouch = Time.unscaledTime);
		}
	}

	private static void UpdateBall()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		if (!ballPlaced)
		{
			return;
		}
		Vector2 val;
		if (ballState != BallState.Dragging)
		{
			float num = 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime);
			ballPos = Vector2.Lerp(ballPos, ballTarget, num);
			val = ballPos - ballTarget;
			if (val.sqrMagnitude < 0.04f)
			{
				ballPos = ballTarget;
			}
		}
		float num2 = 1f;
		if (ballState == BallState.Snapped && Time.unscaledTime - ballSnapAt >= 1f)
		{
			num2 = 0.85f;
		}
		ballOpacity = Mathf.MoveTowards(ballOpacity, num2, Time.unscaledDeltaTime * 3f);
		if (!Visible && slide <= 0.001f && !ballPressing && !ballDragging)
		{
			val = ballPos - ballTarget;
			if (val.sqrMagnitude < 1f)
			{
				AndroidBallGesture.Update(ballPos, 44f);
			}
		}
	}

	private static void RevealBall()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		EnsureBall();
		ballState = BallState.Full;
		ballTarget = FullPos(ballEdge, ballPos);
		ballLastTouch = Time.unscaledTime;
	}

	private static void SnapBall()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		EnsureBall();
		ballEdge = NearestEdge(ballPos);
		ballTarget = SnappedPos(ballEdge, ballPos);
		ballState = BallState.Snapped;
		ballSnapAt = Time.unscaledTime;
		ballLastTouch = Time.unscaledTime;
	}

	private static BallEdge NearestEdge(Vector2 p)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		float x = p.x;
		float num = UiKit.W - p.x;
		float y = p.y;
		float num2 = UiKit.H - p.y;
		float num3 = Mathf.Min(Mathf.Min(x, num), Mathf.Min(y, num2));
		if (Mathf.Approximately(num3, x))
		{
			return BallEdge.Left;
		}
		if (Mathf.Approximately(num3, num))
		{
			return BallEdge.Right;
		}
		if (Mathf.Approximately(num3, y))
		{
			return BallEdge.Top;
		}
		return BallEdge.Bottom;
	}

	private static Vector2 SnappedPos(BallEdge edge, Vector2 p)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Clamp(p.x, 32f, UiKit.W - 32f);
		float num2 = Mathf.Clamp(p.y, 32f, UiKit.H - 32f);
		switch (edge)
		{
		case BallEdge.Left:
			num = 0f;
			break;
		case BallEdge.Right:
			num = UiKit.W;
			break;
		case BallEdge.Top:
			num2 = 0f;
			break;
		case BallEdge.Bottom:
			num2 = UiKit.H;
			break;
		}
		return new Vector2(num, num2);
	}

	private static Vector2 FullPos(BallEdge edge, Vector2 p)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		float ballFullGap = BallFullGap;
		float num = Mathf.Clamp(p.x, 40f, UiKit.W - 32f - 8f);
		float num2 = Mathf.Clamp(p.y, 40f, UiKit.H - 32f - 8f);
		switch (edge)
		{
		case BallEdge.Left:
			num = 32f + ballFullGap;
			break;
		case BallEdge.Right:
			num = UiKit.W - 32f - ballFullGap;
			break;
		case BallEdge.Top:
			num2 = 32f + ballFullGap;
			break;
		case BallEdge.Bottom:
			num2 = UiKit.H - 32f - ballFullGap;
			break;
		}
		return new Vector2(num, num2);
	}

	private static void PollBall()
	{
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Invalid comparison between Unknown and I4
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Invalid comparison between Unknown and I4
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		if (Input.touchCount > 0)
		{
			if (ballPressing && ballMouse)
			{
				ResetBallPress();
			}
			if (ballPressing)
			{
				for (int i = 0; i < Input.touchCount; i++)
				{
					Touch touch = Input.GetTouch(i);
					if (touch.fingerId != ballFinger)
					{
						continue;
					}
					if ((int)touch.phase == 4)
					{
						CancelBallPress();
						return;
					}
					Vector2 gui = ToGui(touch.position);
					if ((int)touch.phase == 3)
					{
						UpdateBallPress(gui);
						FinishBallPress();
					}
					else
					{
						UpdateBallPress(gui);
					}
					return;
				}
				CancelBallPress();
				return;
			}
			for (int j = 0; j < Input.touchCount; j++)
			{
				Touch touch2 = Input.GetTouch(j);
				if ((int)touch2.phase == 0)
				{
					Vector2 val = ToGui(touch2.position);
					if (BallContains(val))
					{
						BeginBallPress(val, touch2.fingerId, mouse: false);
						break;
					}
				}
			}
		}
		else if (ballPressing && ballMouse)
		{
			UpdateBallPress(MouseGui());
			if (!Input.GetMouseButton(0))
			{
				FinishBallPress();
			}
		}
		else if (Input.GetMouseButtonDown(0))
		{
			Vector2 val2 = MouseGui();
			if (BallContains(val2))
			{
				BeginBallPress(val2, -1, mouse: true);
			}
		}
	}

	private static bool BallContains(Vector2 p)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = p - ballPos;
		return val.sqrMagnitude <= 1936f;
	}

	private static void BeginBallPress(Vector2 gui, int finger, bool mouse)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		ballPressing = true;
		ballMouse = mouse;
		ballFinger = finger;
		ballPressPos = gui;
		ballDragging = false;
		ballWasFull = ballState == BallState.Full;
		ballGrab = gui - ballPos;
		ballLastTouch = Time.unscaledTime;
		if (!ballWasFull)
		{
			ballState = BallState.Full;
			ballTarget = FullPos(ballEdge, ballPos);
		}
	}

	private static void UpdateBallPress(Vector2 gui)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = gui - ballPressPos;
		if (!ballDragging && val.sqrMagnitude > 100f)
		{
			ballDragging = true;
			ballState = BallState.Dragging;
			ballGrab = gui - ballPos;
		}
		if (ballDragging)
		{
			ballPos = new Vector2(Mathf.Clamp(gui.x - ballGrab.x, 32f, UiKit.W - 32f), Mathf.Clamp(gui.y - ballGrab.y, 32f, UiKit.H - 32f));
			ballTarget = ballPos;
			ballLastTouch = Time.unscaledTime;
		}
	}

	private static void FinishBallPress()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		if (ballDragging)
		{
			SnapBall();
		}
		else if (ballWasFull)
		{
			OpenPanel();
		}
		else
		{
			ballState = BallState.Full;
			ballTarget = FullPos(ballEdge, ballPos);
			ballLastTouch = Time.unscaledTime;
		}
		ResetBallPress();
		swallow = RawPointerDown();
	}

	private static void CancelBallPress()
	{
		if (ballDragging)
		{
			SnapBall();
		}
		ResetBallPress();
		swallow = RawPointerDown();
	}

	private static void ResetBallPress()
	{
		ballPressing = false;
		ballMouse = false;
		ballFinger = -1;
		ballDragging = false;
		ballWasFull = false;
	}

	private static void PollPanel()
	{
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Invalid comparison between Unknown and I4
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Invalid comparison between Unknown and I4
		hotEntry = -1;
		if (Input.touchCount > 0)
		{
			if (pressing && pressMouse)
			{
				ResetPress();
			}
			if (pressing)
			{
				for (int i = 0; i < Input.touchCount; i++)
				{
					Touch touch = Input.GetTouch(i);
					if (touch.fingerId != pressFinger)
					{
						continue;
					}
					if ((int)touch.phase == 4)
					{
						ResetPress();
						return;
					}
					pressPos = ToGui(touch.position);
					if ((int)touch.phase == 3)
					{
						FinishPress();
					}
					else
					{
						UpdateDrag();
					}
					return;
				}
				ResetPress();
				return;
			}
			for (int j = 0; j < Input.touchCount; j++)
			{
				Touch touch2 = Input.GetTouch(j);
				if ((int)touch2.phase == 0)
				{
					BeginPress(ToGui(touch2.position), touch2.fingerId, mouse: false);
					break;
				}
			}
		}
		else if (Input.GetMouseButtonDown(0))
		{
			BeginPress(MouseGui(), -1, mouse: true);
		}
		else if (pressing && pressMouse)
		{
			if (Input.GetMouseButton(0))
			{
				pressPos = MouseGui();
				UpdateDrag();
			}
			else
			{
				FinishPress();
			}
		}
	}

	private static void BeginPress(Vector2 gui, int finger, bool mouse)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		pressing = true;
		pressMouse = mouse;
		pressFinger = finger;
		pressStart = (pressPos = gui);
		Rect val = PanelRect();
		pressOutside = !val.Contains(gui);
		pressDragged = false;
		hotEntry = (pressOutside ? (-1) : EntryAt(gui));
	}

	private static void UpdateDrag()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = pressPos - pressStart;
		if (!pressDragged && val.sqrMagnitude > 100f)
		{
			pressDragged = true;
		}
		float num = (fromRight ? val.x : (0f - val.x));
		if (!pressOutside && num >= 64f && Mathf.Abs(val.x) >= Mathf.Abs(val.y) * 1.15f)
		{
			Close(immediate: false);
		}
		else
		{
			hotEntry = ((!pressOutside && !pressDragged) ? EntryAt(pressPos) : (-1));
		}
	}

	private static void FinishPress()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		if (pressOutside)
		{
			if (!pressDragged)
			{
				Close(immediate: false);
			}
			else
			{
				ResetPress();
			}
			return;
		}
		if (!pressDragged)
		{
			Rect val = CloseRect();
			if (val.Contains(pressStart))
			{
				val = CloseRect();
				if (val.Contains(pressPos))
				{
					Close(immediate: false);
					return;
				}
			}
			int num = EntryAt(pressStart);
			if (num >= 0 && num == EntryAt(pressPos) && EntryEnabled(num))
			{
				if (Entries[num].IsSwitch)
				{
					Action run = Entries[num].run;
					ResetPress();
					run?.Invoke();
				}
				else
				{
					Action run2 = Entries[num].run;
					Close(immediate: true);
					run2?.Invoke();
				}
				return;
			}
		}
		ResetPress();
	}

	private static void ResetPress()
	{
		pressing = false;
		pressMouse = false;
		pressFinger = -1;
		pressDragged = false;
		pressOutside = false;
		hotEntry = -1;
	}

	private static Vector2 ToGui(Vector2 screen)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2(screen.x / UiKit.Scale, ((float)Screen.height - screen.y) / UiKit.Scale);
	}

	private static Vector2 MouseGui()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2(Input.mousePosition.x / UiKit.Scale, ((float)Screen.height - Input.mousePosition.y) / UiKit.Scale);
	}

	private static float Ease(float t)
	{
		t = Mathf.Clamp01(t);
		return t * t * (3f - 2f * t);
	}

	private static float PanelX()
	{
		float num = Ease(slide);
		if (!fromRight)
		{
			return (0f - (1f - num)) * PanelW;
		}
		return UiKit.W - PanelW * num;
	}

	private static Rect PanelRect()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(PanelX(), 0f, PanelW, UiKit.H);
	}

	private static Rect CloseRect()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(PanelX() + PanelW - 58f, 15f, 42f, 42f);
	}

	private static Rect EntryRect(int index)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		float num = 130f + (float)index * 100f;
		return new Rect(PanelX() + 18f, num, PanelW - 36f, 88f);
	}

	private static int EntryAt(Vector2 p)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < Entries.Count; i++)
		{
			Rect val = EntryRect(i);
			if (val.Contains(p))
			{
				return i;
			}
		}
		return -1;
	}

	private static bool EntryEnabled(int index)
	{
		if (Entries[index].gameplayOnly)
		{
			if ((Object)(object)GameLoop.I != (Object)null)
			{
				return GameLoop.I.state == GameLoop.GameState.Playing;
			}
			return false;
		}
		return true;
	}

	private static Color A(Color c, float a)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		return new Color(c.r, c.g, c.b, c.a * a);
	}

	private static void Disc(Vector2 center, float r, Color c)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		Color color = GUI.color;
		GUI.color = c;
		GUI.DrawTexture(new Rect(center.x - r, center.y - r, r * 2f, r * 2f), (Texture)(object)VirtualControls.DiscTex());
		GUI.color = color;
	}

	private static void Round(Rect r, Color c)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Color color = GUI.color;
		GUI.color = c;
		GUI.DrawTexture(r, (Texture)(object)VirtualControls.SoftTex());
		GUI.color = color;
	}

	private static void Pill(Rect r, Color c)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		float num = r.height * 0.5f;
		Color color = GUI.color;
		GUI.color = c;
		GUI.DrawTexture(new Rect(r.x + num, r.y, Mathf.Max(0f, r.width - num * 2f), r.height), (Texture)(object)Texture2D.whiteTexture);
		GUI.color = color;
		Disc(new Vector2(r.x + num, r.center.y), num, c);
		Disc(new Vector2(r.xMax - num, r.center.y), num, c);
	}

	public static void Draw()
	{
		GameLoop i = GameLoop.I;
		if (!((Object)(object)i == (Object)null) && i.state == GameLoop.GameState.Playing && !VirtualControlsEditor.Visible && ballPlaced)
		{
			if (slide > 0.0001f)
			{
				DrawPanel();
			}
			else
			{
				DrawBall();
			}
		}
	}

	private static void DrawBall()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		float num = ballOpacity;
		if (!(num <= 0.01f))
		{
			Vector2 val = ballPos;
			Disc(val + new Vector2(2f, 3f), 34f, new Color(0f, 0f, 0f, 0.28f * num));
			Disc(val, 38f, new Color(1f, 0.4f, 0.06f, 0.18f * num));
			Disc(val, 32f, A(BallBody, num));
			VirtualControls.DrawAnnulus(val, 30.6f, 34.4f, A(BallRing, num), fade: false);
			float num2 = 12.16f;
			for (int i = -1; i <= 1; i++)
			{
				VirtualControls.DrawLine(new Vector2(val.x - num2, val.y + (float)i * 9f), new Vector2(val.x + num2, val.y + (float)i * 9f), A(BallIcon, num), 3.6f);
			}
		}
	}

	private static void DrawPanel()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		//IL_029f: Unknown result type (might be due to invalid IL or missing references)
		//IL_076d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0772: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0781: Unknown result type (might be due to invalid IL or missing references)
		//IL_0786: Unknown result type (might be due to invalid IL or missing references)
		//IL_078a: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0813: Unknown result type (might be due to invalid IL or missing references)
		//IL_0818: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0807: Unknown result type (might be due to invalid IL or missing references)
		//IL_033f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0839: Unknown result type (might be due to invalid IL or missing references)
		//IL_0832: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0330: Unknown result type (might be due to invalid IL or missing references)
		//IL_083e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0840: Unknown result type (might be due to invalid IL or missing references)
		//IL_084d: Unknown result type (might be due to invalid IL or missing references)
		//IL_085a: Unknown result type (might be due to invalid IL or missing references)
		//IL_085f: Unknown result type (might be due to invalid IL or missing references)
		//IL_086c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0879: Unknown result type (might be due to invalid IL or missing references)
		//IL_087e: Unknown result type (might be due to invalid IL or missing references)
		//IL_088a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0897: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_08b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0906: Unknown result type (might be due to invalid IL or missing references)
		//IL_091f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0948: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		//IL_034c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0391: Unknown result type (might be due to invalid IL or missing references)
		//IL_0396: Unknown result type (might be due to invalid IL or missing references)
		//IL_0976: Unknown result type (might be due to invalid IL or missing references)
		//IL_041d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0427: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_044f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0402: Unknown result type (might be due to invalid IL or missing references)
		//IL_046d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0483: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0516: Unknown result type (might be due to invalid IL or missing references)
		//IL_0553: Unknown result type (might be due to invalid IL or missing references)
		//IL_054c: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0577: Unknown result type (might be due to invalid IL or missing references)
		//IL_0591: Unknown result type (might be due to invalid IL or missing references)
		//IL_0596: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0706: Unknown result type (might be due to invalid IL or missing references)
		//IL_0715: Unknown result type (might be due to invalid IL or missing references)
		//IL_071a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0730: Unknown result type (might be due to invalid IL or missing references)
		//IL_0745: Unknown result type (might be due to invalid IL or missing references)
		//IL_074a: Unknown result type (might be due to invalid IL or missing references)
		//IL_05cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_063c: Unknown result type (might be due to invalid IL or missing references)
		//IL_064c: Unknown result type (might be due to invalid IL or missing references)
		//IL_066d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0679: Unknown result type (might be due to invalid IL or missing references)
		//IL_0683: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_069d: Unknown result type (might be due to invalid IL or missing references)
		float num = Ease(slide);
		Rect r = PanelRect();
		UiKit.Fill(new Rect(0f, 0f, UiKit.W, UiKit.H), new Color(0f, 0f, 0f, 0.46f * num));
		for (int i = 1; i <= 6; i++)
		{
			float num2 = 0.1f * (1f - (float)(i - 1) / 6f) * num;
			UiKit.Fill(new Rect(fromRight ? (r.xMin - (float)i * 4f) : (r.xMax + (float)(i - 1) * 4f), 0f, 4f, UiKit.H), new Color(0f, 0f, 0f, num2));
		}
		UiKit.Fill(r, PanelBg);
		UiKit.Fill(new Rect(r.x, 0f, r.width, 96f), PanelHead);
		UiKit.Fill(new Rect(fromRight ? r.x : (r.xMax - 2f), 0f, 2f, UiKit.H), new Color(0.32f, 0.66f, 1f, 0.45f * num));
		UiKit.Fill(new Rect(r.x, 95f, r.width, 1f), new Color(1f, 1f, 1f, 0.07f));
		VirtualControls.Label(new Rect(r.x + 22f, 22f, r.width - 90f, 34f), "快捷菜单", 26, TxtMain, (TextAnchor)3, 2);
		VirtualControls.Label(new Rect(r.x + 22f, 58f, r.width - 90f, 22f), "随时调整游戏操作", 12, TxtDim, (TextAnchor)3, 0);
		VirtualControls.Label(new Rect(r.x + 18f + 2f, 102f, 120f, 22f), "功能", 12, new Color(1f, 1f, 1f, 0.38f), (TextAnchor)3, 0);
		UiKit.Fill(new Rect(r.x + 18f + 46f, 113f, PanelW - 36f - 46f, 1f), new Color(1f, 1f, 1f, 0.05f));
		for (int j = 0; j < Entries.Count; j++)
		{
			bool flag = EntryEnabled(j);
			bool isSwitch = Entries[j].IsSwitch;
			bool flag2 = isSwitch && Entries[j].state();
			Rect r2 = EntryRect(j);
			bool flag3 = flag && pressing && !pressOutside && !pressDragged && hotEntry == j;
			bool flag4 = flag && !Application.isMobilePlatform && !pressing && r2.Contains(MouseGui());
			Round(r2, (!flag) ? CardOff : ((flag3 | flag4) ? CardHot : CardBg));
			if (flag3 | flag4)
			{
				UiKit.Fill(new Rect(r2.x, r2.y + 12f, 3f, r2.height - 24f), Accent);
			}
			Rect r3 = new Rect(r2.x + 14f, r2.y + (r2.height - 52f) * 0.5f, 52f, 52f);
			Round(r3, (Color)((!flag) ? new Color(0.1f, 0.12f, 0.15f, 0.9f) : ((isSwitch && !flag2) ? new Color(0.115f, 0.135f, 0.17f, 0.95f) : IconBg)));
			VirtualControls.Label(r3, Entries[j].GlyphNow, 24, (!flag) ? new Color(0.58f, 0.62f, 0.68f) : ((isSwitch && !flag2) ? new Color(0.66f, 0.72f, 0.8f) : new Color(0.88f, 0.96f, 1f)), (TextAnchor)4);
			float num3 = r3.xMax + 14f;
			float num4 = r2.xMax - num3 - (isSwitch ? 70f : 42f);
			VirtualControls.Label(new Rect(num3, r2.y + 18f, num4, 26f), Entries[j].title, 19, flag ? TxtMain : TxtDim, (TextAnchor)3);
			VirtualControls.Label(new Rect(num3, r2.y + 46f, num4, 20f), flag ? Entries[j].DescNow : "进入游戏后可编辑", 12, (Color)(flag ? TxtDim : new Color(0.48f, 0.51f, 0.57f)), (TextAnchor)3, 0);
			if (isSwitch)
			{
				Rect r4 = new Rect(r2.xMax - 60f, r2.center.y - 13f, 46f, 26f);
				Pill(r4, (!flag) ? new Color(0.14f, 0.15f, 0.18f, 1f) : (flag2 ? new Color(0.2f, 0.6f, 1f, 1f) : new Color(0.17f, 0.19f, 0.24f, 1f)));
				float num5 = r4.height * 0.5f - 3f;
				float num6 = (flag2 ? (r4.xMax - r4.height * 0.5f) : (r4.x + r4.height * 0.5f));
				Disc(new Vector2(num6, r4.center.y + 1.4f), num5 + 1f, new Color(0f, 0f, 0f, 0.22f));
				Disc(new Vector2(num6, r4.center.y), num5, (Color)(flag ? Color.white : new Color(0.44f, 0.47f, 0.52f)));
			}
			else
			{
				float num7 = r2.xMax - 24f;
				float y = r2.center.y;
				Color col = new Color(1f, 1f, 1f, flag ? 0.36f : 0.12f);
				VirtualControls.DrawLine(new Vector2(num7 - 4f, y - 7f), new Vector2(num7 + 3f, y), col, 2f);
				VirtualControls.DrawLine(new Vector2(num7 + 3f, y), new Vector2(num7 - 4f, y + 7f), col, 2f);
			}
		}
		Rect val = CloseRect();
		int num8;
		if (pressing && !pressOutside)
		{
			Rect val2 = CloseRect();
			num8 = (val2.Contains(pressPos) ? 1 : 0);
		}
		else
		{
			num8 = 0;
		}
		bool flag5 = !Application.isMobilePlatform && !pressing && val.Contains(MouseGui());
		if (((uint)num8 | (flag5 ? 1u : 0u)) != 0)
		{
			Round(new Rect(val.x + 2f, val.y + 2f, val.width - 4f, val.height - 4f), new Color(1f, 1f, 1f, 0.1f));
		}
		Vector2 center = val.center;
		Color col2 = (Color)((((uint)num8 | (flag5 ? 1u : 0u)) != 0) ? Color.white : new Color(1f, 1f, 1f, 0.55f));
		VirtualControls.DrawLine(new Vector2(center.x - 7f, center.y - 7f), new Vector2(center.x + 7f, center.y + 7f), col2, 2.2f);
		VirtualControls.DrawLine(new Vector2(center.x + 7f, center.y - 7f), new Vector2(center.x - 7f, center.y + 7f), col2, 2.2f);
		float num9 = UiKit.H - 62f;
		UiKit.Fill(new Rect(r.x + 18f, num9 - 12f, PanelW - 36f, 1f), new Color(1f, 1f, 1f, 0.06f));
		VirtualControls.Label(new Rect(r.x + 18f, num9, PanelW - 36f, 22f), fromRight ? "点击空白处或向右滑动关闭" : "点击空白处或向左滑动关闭", 12, new Color(1f, 1f, 1f, 0.3f), (TextAnchor)4, 0);
	}

	static GameSideMenu()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
	}
}
