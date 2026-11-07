using System;
using System.Collections.Generic;
using System.IO;
using MapleDash.Wz;
using Server.Field;
using UnityEngine;

public sealed class NativeReactorView : MonoBehaviour
{
	private sealed class Clip
	{
		public readonly List<Sprite> frames = new List<Sprite>();

		public readonly List<int> delays = new List<int>();

		public int duration;

		public Sprite At(long elapsed)
		{
			int num = (int)(Math.Max(0L, elapsed) % duration);
			for (int i = 0; i < frames.Count; i++)
			{
				if (num < delays[i])
				{
					return frames[i];
				}
				num -= delays[i];
			}
			return frames[frames.Count - 1];
		}
	}

	private sealed class View
	{
		public MapReactor reactor;

		public SpriteRenderer renderer;

		public int revision = -1;

		public int state = -1;

		public Clip stand;

		public Clip hit;

		public long started;
	}

	private readonly Dictionary<int, View> views = new Dictionary<int, View>();

	private readonly Dictionary<string, Clip> clips = new Dictionary<string, Clip>();

	private int mapId;

	public void Bind(int id)
	{
		mapId = id;
	}

	public static MapReactor FindTarget(int map, Rect attack)
	{
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		NativeReactorView[] array = Object.FindObjectsOfType<NativeReactorView>();
		foreach (NativeReactorView nativeReactorView in array)
		{
			if (nativeReactorView.mapId != map)
			{
				continue;
			}
			foreach (MapReactor item in MapReactorService.Get(map))
			{
				if (!item.alive)
				{
					continue;
				}
				List<MapReactorTemplate.Event> list = item.template?.Events(item.state);
				if (list != null && list.Count > 0 && list[0].type != 0)
				{
					continue;
				}
				Clip clip = nativeReactorView.Load(item.id, item.state, hit: false);
				if (clip != null)
				{
					Bounds bounds = clip.At(Math.Max(0L, MapReactorService.Now() - item.lastHitAt)).bounds;
					Rect val = Rect.MinMaxRect((float)item.x / 40f + bounds.min.x, (float)(-item.y) / 40f + bounds.min.y, (float)item.x / 40f + bounds.max.x, (float)(-item.y) / 40f + bounds.max.y);
					if (attack.Overlaps(val))
					{
						return item;
					}
				}
			}
		}
		return null;
	}

	private ImgNode Root(int id)
	{
		HashSet<int> hashSet = new HashSet<int>();
		while (hashSet.Add(id))
		{
			string relative = "Reactor/" + id.ToString("D7") + ".img";
			if (!WzAssets.HasFile(relative))
			{
				return null;
			}
			ImgNode imgNode = WzAssets.FileRoot(relative);
			string text = imgNode.Text("info/link");
			if (string.IsNullOrEmpty(text))
			{
				return imgNode;
			}
			id = int.Parse(text);
		}
		throw new InvalidDataException("Reactor IMG link cycle");
	}

	private Clip Load(int id, int state, bool hit)
	{
		string text = id + "/" + state + (hit ? "/hit" : "");
		if (clips.TryGetValue(text, out var value))
		{
			return value;
		}
		ImgNode imgNode = Root(id)?.Get(state + (hit ? "/hit" : ""))?.TryResolve();
		if (imgNode == null)
		{
			clips[text] = null;
			return null;
		}
		List<ImgNode> list = new List<ImgNode>();
		foreach (ImgNode child in imgNode.Children)
		{
			if (int.TryParse(child.Name, out var _))
			{
				list.Add(child);
			}
		}
		list.Sort((ImgNode a, ImgNode b) => int.Parse(a.Name).CompareTo(int.Parse(b.Name)));
		value = new Clip();
		foreach (ImgNode item in list)
		{
			ImgNode imgNode2 = item.TryResolve();
			if (imgNode2 != null && !(imgNode2.Kind != "Canvas"))
			{
				int num = Math.Max(1, Math.Abs(imgNode2.Int("delay", 100)));
				value.frames.Add(WzAssets.Sprite(imgNode2, "reactor:" + text + "/" + item.Name));
				value.delays.Add(num);
				value.duration += num;
			}
		}
		if (value.frames.Count == 0)
		{
			value = null;
		}
		clips[text] = value;
		return value;
	}

	private void LateUpdate()
	{
		Tick(MapReactorService.Now());
	}

	public void Tick(long now)
	{
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		HashSet<int> hashSet = new HashSet<int>();
		foreach (MapReactor item in MapReactorService.Get(mapId))
		{
			hashSet.Add(item.objectId);
			if (!views.TryGetValue(item.objectId, out var value))
			{
				GameObject val = new GameObject("Reactor_" + item.objectId);
				val.transform.SetParent(((Component)this).transform, false);
				SpriteRenderer val2 = val.AddComponent<SpriteRenderer>();
				((Renderer)val2).sortingOrder = 100;
				value = new View
				{
					reactor = item,
					renderer = val2
				};
				views.Add(item.objectId, value);
			}
			((Component)value.renderer).transform.localPosition = new Vector3((float)item.x / 40f, (float)(-item.y) / 40f, 0f);
			if (value.revision != item.visualRevision || value.state != item.state)
			{
				bool flag = value.revision >= 0;
				value.revision = item.visualRevision;
				value.state = item.state;
				value.stand = Load(item.id, item.state, hit: false);
				value.hit = ((flag && item.previousState >= 0) ? Load(item.id, item.previousState, hit: true) : null);
				value.started = (flag ? item.lastHitAt : now);
			}
			long num = Math.Max(0L, now - value.started);
			if (value.hit != null && num < value.hit.duration)
			{
				value.renderer.sprite = value.hit.At(num);
			}
			else
			{
				value.renderer.sprite = ((!item.alive) ? null : value.stand?.At(num - (value.hit?.duration ?? 0)));
			}
		}
		foreach (int item2 in new List<int>(views.Keys))
		{
			if (!hashSet.Contains(item2))
			{
				Object.Destroy((Object)(object)((Component)views[item2].renderer).gameObject);
				views.Remove(item2);
			}
		}
	}
}
