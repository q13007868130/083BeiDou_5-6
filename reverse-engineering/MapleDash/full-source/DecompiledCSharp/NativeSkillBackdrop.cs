using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public sealed class NativeSkillBackdrop : MonoBehaviour
{
	public const int SortingOrder = -900;

	private static readonly string[] markers = new string[4] { "Map/Effect.img/customSkill/skillBackdrop/videoLayer", "Map/Effect.img/customSkill/dawnWarrior/galaxyStarBurstVideoLayer", "Map/Effect.img/customSkill/dawnWarrior/soulEclipseVideoLayer", "Map/Effect.img/customSkill/dawnWarrior/eclipseForceVideoLayer" };

	private static NativeSkillBackdrop current;

	private FieldDirector field;

	private NativeSkillBackdropState state;

	private int revision;

	private string path;

	private double expires;

	private double started;

	private bool ownsVideo;

	private SpriteRenderer image;

	private readonly List<Sprite> frames = new List<Sprite>();

	private readonly List<double> ends = new List<double>();

	private double cycle;

	public static void Enter(FieldDirector field)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		Leave();
		current = new GameObject("NativeSkillBackdrop").AddComponent<NativeSkillBackdrop>();
		current.field = field;
		current.state = GameSession.Server?.Backdrops.Map(field.Map.id);
		current.revision = -1;
		current.RefreshMap();
	}

	public static void Leave()
	{
		if (!((Object)(object)current == (Object)null))
		{
			current.Drop();
			((Component)current).gameObject.SetActive(false);
			Object.Destroy((Object)(object)((Component)current).gameObject);
			current = null;
		}
	}

	public static void ShowMap(string path, int duration)
	{
		if (current?.state != null)
		{
			current.state.Show(path, duration, Time.realtimeSinceStartupAsDouble * 1000.0);
			current.RefreshMap();
		}
	}

	public static void ShowLocal(string path, int duration)
	{
		if (!((Object)(object)current == (Object)null))
		{
			string requested = NativeSkillBackdropPacket.Normalize(path);
			current.Show(requested, Math.Max(0, Math.Min(86400000, duration)));
		}
	}

	public static bool HandlePacket(byte[] packet, int offset = 0)
	{
		if (!NativeSkillBackdropPacket.TryRead(packet, offset, out var requested, out var duration))
		{
			return false;
		}
		if ((Object)(object)current != (Object)null)
		{
			current.Show(requested, duration);
		}
		return true;
	}

	private void RefreshMap()
	{
		if (state != null)
		{
			int duration = state.Remaining(Time.realtimeSinceStartupAsDouble * 1000.0);
			if (state.Revision != revision)
			{
				revision = state.Revision;
				Show(state.Path, duration);
			}
		}
	}

	private void Show(string requested, int duration)
	{
		Drop();
		path = requested;
		expires = ((duration > 0) ? (Time.realtimeSinceStartupAsDouble + (double)duration / 1000.0) : 0.0);
		started = Time.realtimeSinceStartupAsDouble;
		if (path == null)
		{
			return;
		}
		try
		{
			bool flag = path.EndsWith(".mcv", StringComparison.Ordinal);
			ImgNode imgNode = (flag ? null : TryNode(path));
			if (imgNode == null)
			{
				string[] array = markers;
				for (int i = 0; i < array.Length; i++)
				{
					imgNode = TryNode(array[i]);
					if (imgNode != null)
					{
						break;
					}
				}
			}
			if (imgNode == null)
			{
				Debug.LogWarning((object)"[Skillback] Missing original marker");
				return;
			}
			if (flag)
			{
				string text = path.Substring(path.LastIndexOf('/') + 1);
				ownsVideo = NativeSkillVideo.Play(NativeSkillVideo.Channel.PlayerSkill, "Video/" + text, null, -900);
				return;
			}
			AddFrames(imgNode);
			if (frames.Count != 0)
			{
				image = ((Component)this).gameObject.AddComponent<SpriteRenderer>();
				((Renderer)image).sortingOrder = -900;
				image.sprite = frames[0];
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning((object)("[Skillback] " + ex.Message));
		}
	}

	private static ImgNode TryNode(string path)
	{
		int num = path.IndexOf(".img/", StringComparison.Ordinal);
		if (num < 0 || !WzAssets.HasFile(path.Substring(0, num + 4)))
		{
			return null;
		}
		return WzAssets.Node(path);
	}

	private void AddFrames(ImgNode node)
	{
		node = node.Resolve();
		if (node.Kind == "Canvas")
		{
			Add(node);
			return;
		}
		int num = 0;
		while (true)
		{
			ImgNode imgNode = node.Get(num.ToString())?.Resolve();
			if (imgNode != null)
			{
				Add(imgNode);
				num++;
				continue;
			}
			break;
		}
	}

	private void Add(ImgNode node)
	{
		if (!(node.Kind != "Canvas"))
		{
			frames.Add(WzAssets.Sprite(node));
			cycle += (double)Math.Max(1, Math.Abs(node.Int("delay", 100))) / 1000.0;
			ends.Add(cycle);
		}
	}

	private void LateUpdate()
	{
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)field == (Object)null || (Object)(object)GameLoop.I == (Object)null || GameLoop.I.state != GameLoop.GameState.Playing)
		{
			Leave();
			return;
		}
		RefreshMap();
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		if (path != null && expires > 0.0 && realtimeSinceStartupAsDouble >= expires)
		{
			Show(null, 0);
		}
		if (!((Object)(object)image == (Object)null) && !((Object)(object)Camera.main == (Object)null))
		{
			Vector3 position = ((Component)Camera.main).transform.position;
			position.z = 0f;
			((Component)this).transform.position = position;
			double num = (realtimeSinceStartupAsDouble - started) % cycle;
			int i;
			for (i = 0; i < ends.Count - 1 && num >= ends[i]; i++)
			{
			}
			image.sprite = frames[i];
		}
	}

	private void Drop()
	{
		if (ownsVideo)
		{
			NativeSkillVideo.Stop(NativeSkillVideo.Channel.PlayerSkill);
		}
		ownsVideo = false;
		if ((Object)(object)image != (Object)null)
		{
			((Renderer)image).enabled = false;
			Object.Destroy((Object)(object)image);
			image = null;
		}
		frames.Clear();
		ends.Clear();
		cycle = 0.0;
	}

	private void OnDisable()
	{
		Drop();
	}
}
