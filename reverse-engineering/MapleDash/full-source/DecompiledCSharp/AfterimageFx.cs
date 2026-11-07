using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class AfterimageFx
{
	public struct Canvas2
	{
		public ImgNode node;

		public float delay;
	}

	public struct Frame
	{
		public int index;

		public Canvas2[] canvases;
	}

	private static readonly Dictionary<string, Frame[]> cache = new Dictionary<string, Frame[]>();

	public static Frame[] Frames(string afterName, string action)
	{
		if (string.IsNullOrEmpty(afterName) || string.IsNullOrEmpty(action))
		{
			return null;
		}
		string key = afterName + "/" + action;
		if (cache.TryGetValue(key, out var value))
		{
			return value;
		}
		string[] array = Aliases(action);
		foreach (string action2 in array)
		{
			value = Load(afterName, action2);
			if (value != null)
			{
				break;
			}
		}
		cache[key] = value;
		return value;
	}

	private static string[] Aliases(string action)
	{
		switch (action)
		{
		case "shoot1":
		case "shoot2":
		case "shootF":
			return new string[2] { action, "shot" };
		case "swingO1":
			return new string[2] { "swingO1", "stabO1" };
		case "stabT1":
			return new string[2] { "stabT1", "swingP1" };
		default:
			return new string[1] { action };
		}
	}

	public static Nullable AttackBounds(string afterName, string action)
	{
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrEmpty(afterName) || string.IsNullOrEmpty(action))
		{
			return null;
		}
		string text = "Character/Afterimage/" + afterName + ".img";
		if (!WzAssets.HasFile(text))
		{
			return null;
		}
		string[] array = Aliases(action);
		foreach (string text2 in array)
		{
			ImgNode imgNode = WzAssets.Node(text + "/0/" + text2)?.TryResolve();
			ImgNode imgNode2 = imgNode?.Get("lt");
			ImgNode imgNode3 = imgNode?.Get("rb");
			if (imgNode2 != null && imgNode3 != null)
			{
				return Rect.MinMaxRect((float)imgNode2.X, (float)imgNode2.Y, (float)imgNode3.X, (float)imgNode3.Y);
			}
		}
		return null;
	}

	private static Frame[] Load(string afterName, string action)
	{
		string text = "Character/Afterimage/" + afterName + ".img";
		if (!WzAssets.HasFile(text))
		{
			return null;
		}
		ImgNode imgNode = WzAssets.Node(text + "/0/" + action);
		if (imgNode == null)
		{
			return null;
		}
		List<ImgNode> list = WzAssets.Frames(imgNode);
		if (list.Count == 0)
		{
			return null;
		}
		List<Frame> list2 = new List<Frame>();
		foreach (ImgNode item in list)
		{
			if (!int.TryParse(item.Name, out var result))
			{
				continue;
			}
			List<Canvas2> list3 = new List<Canvas2>();
			foreach (ImgNode child in item.Children)
			{
				if (int.TryParse(child.Name, out var _))
				{
					ImgNode imgNode2 = child.Resolve();
					if (imgNode2 != null && !(imgNode2.Kind != "Canvas"))
					{
						list3.Add(new Canvas2
						{
							node = child,
							delay = Mathf.Max(0.03f, (float)child.Int("delay", 100) / 1000f)
						});
					}
				}
			}
			if (list3.Count > 0)
			{
				list2.Add(new Frame
				{
					index = result,
					canvases = list3.ToArray()
				});
			}
		}
		if (list2.Count <= 0)
		{
			return null;
		}
		return list2.ToArray();
	}

	public static void Spawn(string afterName, string action, Frame frame, Transform anchor, int facing, float playback)
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)anchor == (Object)null)
		{
			return;
		}
		Canvas2[] canvases = frame.canvases;
		for (int i = 0; i < canvases.Length; i++)
		{
			Canvas2 canvas = canvases[i];
			Sprite val = WzAssets.Sprite(canvas.node, "ai:" + afterName + "/0/" + action + "/" + frame.index + "/" + canvas.node.Name);
			if (!((Object)(object)val == (Object)null))
			{
				GameObject val2 = new GameObject("Afterimage");
				val2.transform.SetParent(anchor, false);
				val2.transform.localPosition = Vector3.zero;
				val2.transform.localRotation = Quaternion.identity;
				SpriteRenderer val3 = val2.AddComponent<SpriteRenderer>();
				val3.sprite = val;
				val3.flipX = facing > 0;
				((Renderer)val3).sortingOrder = Mathf.Max(0, ((Object)(object)((Component)anchor).GetComponent<SpriteRenderer>() != (Object)null) ? (((Renderer)((Component)anchor).GetComponent<SpriteRenderer>()).sortingOrder - 1) : 299);
				val2.AddComponent<AfterimageGhost>().life = canvas.delay / Mathf.Max(0.1f, playback);
			}
		}
	}
}
