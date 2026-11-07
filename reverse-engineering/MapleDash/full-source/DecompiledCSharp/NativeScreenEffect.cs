using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class NativeScreenEffect
{
	private sealed class Playing
	{
		public List<ImgNode> Frames;

		public float Started;

		public float Total;
	}

	private static readonly List<Playing> active = new List<Playing>();

	public static int ActiveCount => active.Count;

	public static void Play(string path)
	{
		ImgNode imgNode = WzAssets.Node(path);
		if (imgNode == null)
		{
			return;
		}
		List<ImgNode> list = WzAssets.Frames(imgNode);
		if (list.Count == 0)
		{
			return;
		}
		float num = 0f;
		foreach (ImgNode item in list)
		{
			num += (float)Mathf.Max(1, item.Int("delay", 120));
		}
		active.Add(new Playing
		{
			Frames = list,
			Started = Time.unscaledTime,
			Total = num
		});
	}

	public static void Clear()
	{
		active.Clear();
	}

	public static void Draw()
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		if (active.Count == 0)
		{
			return;
		}
		float num = 0f;
		for (int num2 = active.Count - 1; num2 >= 0; num2--)
		{
			Playing playing = active[num2];
			num = (Time.unscaledTime - playing.Started) * 1000f;
			if (num >= playing.Total)
			{
				active.RemoveAt(num2);
			}
			else
			{
				ImgNode imgNode = FrameAt(playing.Frames, num);
				if (imgNode != null)
				{
					Texture2D val = WzAssets.Texture(imgNode);
					if (!((Object)(object)val == (Object)null))
					{
						Vector2 val2 = WzAssets.Origin(imgNode);
						GUI.DrawTexture(new Rect(UiKit.W * 0.5f - val2.x, UiKit.H * 0.5f - val2.y, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
					}
				}
			}
		}
	}

	private static ImgNode FrameAt(List<ImgNode> frames, float elapsedMs)
	{
		float num = 0f;
		for (int i = 0; i < frames.Count; i++)
		{
			num += (float)Mathf.Max(1, frames[i].Int("delay", 120));
			if (elapsedMs < num)
			{
				return frames[i];
			}
		}
		return frames[frames.Count - 1];
	}
}
