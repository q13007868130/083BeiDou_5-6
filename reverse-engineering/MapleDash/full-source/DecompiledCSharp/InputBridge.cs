using UnityEngine;

[DefaultExecutionOrder(-150)]
public class InputBridge : MonoBehaviour
{
	public static InputBridge I;

	private void Awake()
	{
		I = this;
		KeyMap.Ensure();
		Input.simulateMouseWithTouches = false;
		if ((Object)(object)((Component)this).GetComponent<NativeCursor>() == (Object)null)
		{
			((Component)this).gameObject.AddComponent<NativeCursor>();
		}
	}

	private void Update()
	{
		//IL_0369: Unknown result type (might be due to invalid IL or missing references)
		//IL_036f: Unknown result type (might be due to invalid IL or missing references)
		GameLoop i = GameLoop.I;
		UiKit.RefreshMetrics();
		GameSideMenu.Tick();
		Pointer.Tick();
		bool flag = (Object)(object)i != (Object)null && i.state == GameLoop.GameState.Playing && (Object)(object)i.player != (Object)null && !i.gameOver && !PlayerCtrl.InputLocked && !BottomBar.ChatFocused && !GameSideMenu.Blocking && !NpcSession.Blocking;
		bool flag2 = NpcSession.Blocking && !GameSideMenu.Blocking && !BottomBar.ChatFocused;
		bool flag3 = BottomBar.ChatFocused && !GameSideMenu.Blocking && !NpcSession.Blocking;
		VirtualControls.Tick((flag | flag2 | flag3) && !VirtualControlsEditor.Visible);
		if ((Object)(object)i != (Object)null && i.state == GameLoop.GameState.Playing && (Object)(object)i.player != (Object)null && !NpcSession.Blocking && !ShopWindow.Visible && !KeyConfigWindow.Visible && !VirtualControlsEditor.Visible && !GameSideMenu.Blocking && (InputHub.KeyDown((KeyCode)13) || InputHub.KeyDown((KeyCode)271)))
		{
			BottomBar.QueueChatEnter();
			flag = false;
		}
		if (flag2 && !KeyConfigWindow.Visible && !VirtualControlsEditor.Visible)
		{
			if (Input.GetKeyDown((KeyCode)27) || InputHub.Pressed(GameAction.Menu))
			{
				if (!GameWindows.CloseTop())
				{
					NpcSession.Active?.Cancel();
				}
			}
			else if (InputHub.Pressed(GameAction.NpcTalk))
			{
				NpcDialogUi.ContinueWithTalkKey();
			}
			else if (InputHub.Pressed(GameAction.Up))
			{
				NpcDialogUi.MoveSelection(-1);
			}
			else if (InputHub.Pressed(GameAction.Down))
			{
				NpcDialogUi.MoveSelection(1);
			}
		}
		InputHub.TickRepeat(Time.unscaledTime, flag && Application.isFocused && Time.timeScale > 0f && !KeyConfigWindow.Visible && !VirtualControlsEditor.Visible);
		if (flag)
		{
			TouchInput.MoveH = InputHub.AxisX;
			TouchInput.MoveV = InputHub.AxisY;
			if (InputHub.Pressed(GameAction.Jump) || InputHub.Repeated(GameAction.Jump))
			{
				TouchInput.PushJump();
			}
		}
		else
		{
			TouchInput.MoveH = 0f;
			TouchInput.MoveV = 0f;
		}
		if ((flag | flag2) && !VirtualControlsEditor.Visible)
		{
			NativeKeyActions.Entry[] entries = NativeKeyActions.Entries;
			foreach (NativeKeyActions.Entry entry in entries)
			{
				if (entry.action != GameAction.Jump && entry.action != GameAction.Attack && entry.action != GameAction.Pickup && (flag || NativeKeyActions.AvailableDuringNpcDialog(entry.action)) && InputHub.Pressed(entry.action))
				{
					NativeKeyActions.Execute(entry.action);
				}
			}
			GameAction[] array = new GameAction[2]
			{
				GameAction.Help,
				GameAction.QuickSlot
			};
			foreach (GameAction gameAction in array)
			{
				if (InputHub.Pressed(gameAction))
				{
					NativeKeyActions.Execute(gameAction);
				}
			}
		}
		if (!((KeyConfigWindow.Visible || VirtualControlsEditor.Visible || GameSideMenu.Blocking || BottomBar.ChatFocused || BottomBar.ChannelListOpen) | flag2) && !NpcSession.Blocking && (Input.GetKeyDown((KeyCode)27) || InputHub.Pressed(GameAction.Menu)))
		{
			NativeKeyActions.Execute(GameAction.Menu);
		}
		if (KeyConfigWindow.Visible || VirtualControlsEditor.Visible || NpcSession.Blocking)
		{
			TouchInput.MoveH = (TouchInput.MoveV = 0f);
			TouchInput.TakeJump();
			return;
		}
		if (flag)
		{
			for (int k = 0; k < 26; k++)
			{
				if (QuickSlots.Id(k) != 0 && InputHub.Pressed(QuickSlots.Action(k)))
				{
					QuickSlots.Cast(k);
				}
			}
		}
		bool flag4 = false;
		for (int l = 0; l < 26; l++)
		{
			if (QuickSlots.Id(l) != 0 && KeyMap.Of(QuickSlots.Action(l)) == KeyMap.Of(GameAction.Attack))
			{
				flag4 = true;
			}
		}
		if (flag && !flag4 && (InputHub.Pressed(GameAction.Attack) || InputHub.Repeated(GameAction.Attack)))
		{
			i.player.RequestAttack();
		}
		if (flag && (InputHub.Pressed(GameAction.Pickup) || InputHub.Repeated(GameAction.Pickup)))
		{
			GroundItem.TryPickupNearest();
		}
	}

	private void LateUpdate()
	{
		InputHub.EndFrame();
	}

	private void OnGUI()
	{
		if (!((Object)(object)GameLoop.I == (Object)null))
		{
			GUI.depth = -300;
			UiKit.Begin();
			if (!GameHUD.InterfaceHidden && UiPreviewHook.Draw == null)
			{
				VirtualControls.Draw();
				VirtualControls.DrawDropTargets();
			}
			GameSideMenu.Draw();
			VirtualControlsEditor.Draw();
		}
	}
}
