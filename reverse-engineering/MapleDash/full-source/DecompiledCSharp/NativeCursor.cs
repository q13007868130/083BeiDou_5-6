using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public sealed class NativeCursor : MonoBehaviour
{
	[Serializable]
	private class Frame
	{
		public string file;

		public int x;

		public int y;

		public int delay;
	}

	[Serializable]
	private class State
	{
		public int id;

		public Frame[] frames;
	}

	[Serializable]
	private class Manifest
	{
		public State[] states;
	}

	private Manifest manifest;

	private int state;

	private float since;

	private bool wasVisible;

	private string activeFrame;

	public const int StateDefault = 0;

	public const int StateGrab = 11;

	public const int StateHold = 12;

	private static int request = -1;

	public static void Request(int id)
	{
		request = id;
	}

	private void Awake()
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		List<State> list = new List<State>();
		foreach (ImgNode child in WzAssets.Node("UI/Basic.img/Cursor").Children)
		{
			if (!int.TryParse(child.Name, out var result))
			{
				continue;
			}
			List<Frame> list2 = new List<Frame>();
			foreach (ImgNode item in WzAssets.Frames(child))
			{
				ImgNode imgNode = item.Resolve();
				Vector2 val = WzAssets.Origin(imgNode);
				list2.Add(new Frame
				{
					file = "wz://UI/Basic.img/Cursor/" + result + "/" + item.Name,
					x = (int)val.x,
					y = (int)val.y,
					delay = Math.Abs(imgNode.Int("delay", 100))
				});
			}
			list.Add(new State
			{
				id = result,
				frames = list2.ToArray()
			});
		}
		manifest = new Manifest
		{
			states = list.ToArray()
		};
		wasVisible = Cursor.visible;
	}

	private void Update()
	{
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		if (manifest == null)
		{
			return;
		}
		Cursor.lockState = (CursorLockMode)0;
		Cursor.visible = !Application.isMobilePlatform;
		int num = request;
		request = -1;
		int num2 = ((num >= 0) ? num : ((!GameSideMenu.Blocking) ? ((Pointer.Down || Pointer.RightDown) ? 12 : ((Time.unscaledTime - since < 1f && this.state == 1) ? 1 : 0)) : 0));
		if (!GameSideMenu.Blocking && (Pointer.Released || Pointer.RightReleased))
		{
			num2 = 1;
		}
		if (num2 != this.state)
		{
			this.state = num2;
			since = Time.unscaledTime;
		}
		if (Application.isMobilePlatform || !Application.isFocused)
		{
			return;
		}
		State state = SelectState(this.state);
		if (state != null)
		{
			Frame frame = CurrentFrame(state);
			Texture2D val = Art.Tex(frame.file);
			if ((Object)(object)val != (Object)null && activeFrame != frame.file)
			{
				Cursor.SetCursor(val, new Vector2((float)Mathf.Clamp(frame.x, 0, ((Texture)val).width - 1), (float)Mathf.Clamp(frame.y, 0, ((Texture)val).height - 1)), (CursorMode)1);
				activeFrame = frame.file;
			}
		}
	}

	private State SelectState(int id)
	{
		return Array.Find(manifest.states, (State s) => s.id == id && s.frames.Length != 0) ?? Array.Find(manifest.states, (State s) => s.id == 0 && s.frames.Length != 0);
	}

	private Frame CurrentFrame(State selected)
	{
		float num = (Time.unscaledTime - since) * 1000f;
		int num2 = 0;
		Frame[] frames = selected.frames;
		foreach (Frame frame in frames)
		{
			num2 += Mathf.Max(1, frame.delay);
		}
		num %= (float)num2;
		Frame result = selected.frames[0];
		frames = selected.frames;
		foreach (Frame frame2 in frames)
		{
			result = frame2;
			num -= (float)Mathf.Max(1, frame2.delay);
			if (num < 0f)
			{
				break;
			}
		}
		return result;
	}

	private void OnGUI()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Invalid comparison between Unknown and I4
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		GUI.depth = -10000;
		if (!Application.isMobilePlatform || manifest == null || !Application.isFocused || (int)Event.current.type != 7 || VirtualControlsEditor.Visible)
		{
			return;
		}
		State state = SelectState(this.state);
		if (state != null && state.frames.Length != 0)
		{
			Frame frame = CurrentFrame(state);
			Texture2D val = Art.Tex(frame.file);
			if ((Object)(object)val == (Object)null)
			{
				Cursor.visible = true;
				return;
			}
			Matrix4x4 matrix = GUI.matrix;
			Color color = GUI.color;
			GUI.color = Color.white;
			GUI.matrix = Matrix4x4.Scale(new Vector3(UiKit.Scale, UiKit.Scale, 1f));
			GUI.DrawTexture(new Rect(Pointer.Pos.x - (float)frame.x, Pointer.Pos.y - (float)frame.y, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
			GUI.matrix = matrix;
			GUI.color = color;
		}
	}

	private void OnDisable()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		Cursor.SetCursor((Texture2D)null, Vector2.zero, (CursorMode)0);
		Cursor.visible = wasVisible;
	}
}
