using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public sealed class NativeMapIntro : MonoBehaviour
{
	private sealed class MovingLayer
	{
		public NativeImgAnimation Image;

		public NativeIntroTimeline.Command Command;

		public long Started;
	}

	private NativeIntroTimeline scene;

	private readonly List<MovingLayer> layers = new List<MovingLayer>();

	private FieldDirector field;

	private object character;

	private bool ownsLock;

	private int previousLock;

	private static long Now => (long)(Time.realtimeSinceStartupAsDouble * 1000.0);

	public static void Enter(FieldDirector field)
	{
		((Component)field).GetComponent<NativeMapIntro>()?.Clear();
	}

	public static bool Active(FieldDirector field)
	{
		return ((Component)field).GetComponent<NativeMapIntro>()?.scene != null;
	}

	public static NativeMapIntro Show(string path, bool lockUi = false)
	{
		FieldDirector fieldDirector = GameLoop.I?.field;
		if ((Object)(object)fieldDirector == (Object)null)
		{
			throw new InvalidOperationException("Intro requires an active original field");
		}
		NativeMapIntro nativeMapIntro = ((Component)fieldDirector).GetComponent<NativeMapIntro>() ?? ((Component)fieldDirector).gameObject.AddComponent<NativeMapIntro>();
		nativeMapIntro.Begin(fieldDirector, path, lockUi, Now);
		return nativeMapIntro;
	}

	public void Begin(FieldDirector owner, string path, bool lockUi, long now)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		PlayerCtrl playerCtrl = GameLoop.I?.player;
		NativeIntroTimeline nativeIntroTimeline = new NativeIntroTimeline(WzAssets.Node(path), now, (int)((playerCtrl?.worldX ?? 0f) * 40f), (int)((0f - (playerCtrl?.FeetPos.y ?? 0f)) * 40f));
		foreach (NativeIntroTimeline.Command command in nativeIntroTimeline.Commands)
		{
			if (command.Type == 0)
			{
				List<ImgNode> list = Variants(command.Visual);
				if (list.Count == 0)
				{
					throw new InvalidOperationException("Missing intro variants: " + command.Visual);
				}
				foreach (ImgNode item in list)
				{
					if (WzAssets.Frames(item).Count == 0)
					{
						throw new InvalidOperationException("Missing intro frames: " + command.Visual + "/" + item.Name);
					}
				}
			}
			if (!string.IsNullOrEmpty(command.Sound))
			{
				if (!AudioManager.Has(command.Sound))
				{
					throw new InvalidOperationException("Unexported original intro sound: " + command.Sound);
				}
				AudioManager.Preload(command.Sound);
			}
		}
		Clear();
		field = owner;
		character = GameSession.Character;
		scene = nativeIntroTimeline;
		previousLock = PlayerCtrl.ScriptUiLock;
		ownsLock = lockUi;
		if (ownsLock)
		{
			PlayerCtrl.ScriptUiLock++;
		}
	}

	private static List<ImgNode> Variants(string path)
	{
		ImgNode imgNode = WzAssets.Node(path);
		List<ImgNode> list = new List<ImgNode>();
		if (imgNode != null)
		{
			int num = 0;
			while (true)
			{
				ImgNode imgNode2 = imgNode.Get(num.ToString());
				if (imgNode2 == null)
				{
					break;
				}
				list.Add(imgNode2);
				num++;
			}
		}
		return list;
	}

	public void Tick(long now)
	{
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		if (scene == null)
		{
			return;
		}
		if (character != GameSession.Character)
		{
			Clear();
			return;
		}
		scene.Tick(now, (NativeIntroTimeline.Command command2) =>
		{
			//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
			if (command2.Type == 2)
			{
				field.WarpTo(command2.Field, null, 0);
			}
			else if (command2.Type == 5)
			{
				AudioManager.Play(command2.Sound, 1f, pitchVary: false);
			}
			else
			{
				List<ImgNode> list = Variants(command2.Visual);
				ImgNode action = list[Random.Range(0, list.Count)];
				float num = 0f;
				foreach (ImgNode item in WzAssets.Frames(action))
				{
					num += (float)Math.Max(1, Math.Abs(item.Int("delay", 100))) / 1000f;
				}
				NativeImgAnimation nativeImgAnimation = NativeImgAnimation.SpawnAt(action, ((Component)this).transform, new Vector3((float)command2.X / 40f, (float)(-command2.Y) / 40f, 0f), 500 + command2.Z, flip: false, repeat: false, num);
				if ((Object)(object)nativeImgAnimation == (Object)null)
				{
					throw new InvalidOperationException("Cannot display original intro: " + command2.Visual);
				}
				layers.Add(new MovingLayer
				{
					Image = nativeImgAnimation,
					Command = command2,
					Started = now
				});
				if (!string.IsNullOrEmpty(command2.Sound))
				{
					AudioManager.Play(command2.Sound, 1f, pitchVary: false);
				}
			}
		});
		foreach (MovingLayer layer in layers)
		{
			if (!((Object)(object)layer.Image == (Object)null))
			{
				NativeIntroTimeline.Command command = layer.Command;
				long elapsed = now - layer.Started;
				((Component)layer.Image).transform.position = new Vector3((float)(command.X + command.Displacement(command.MoveX, elapsed)) / 40f, (float)(-(command.Y + command.Displacement(command.MoveY, elapsed))) / 40f, 0f);
			}
		}
		layers.RemoveAll((MovingLayer layer) => (Object)(object)layer.Image == (Object)null);
	}

	public void Clear()
	{
		scene = null;
		foreach (MovingLayer layer in layers)
		{
			if ((Object)(object)layer.Image != (Object)null)
			{
				layer.Image.Remove();
			}
		}
		layers.Clear();
		if (ownsLock)
		{
			PlayerCtrl.ScriptUiLock = previousLock;
		}
		ownsLock = false;
	}

	private void Update()
	{
		Tick(Now);
	}

	private void OnDestroy()
	{
		Clear();
	}
}
