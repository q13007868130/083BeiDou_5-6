using System.Collections.Generic;
using UnityEngine;

public sealed class NativeVfxClip
{
	public string path;

	public NativeVfxFrame[] frames;

	public Dictionary<string, object> meta;

	public float Duration
	{
		get
		{
			float num = 0f;
			NativeVfxFrame[] array = frames;
			foreach (NativeVfxFrame nativeVfxFrame in array)
			{
				num += Mathf.Max(0.001f, nativeVfxFrame.delay);
			}
			return num;
		}
	}

	public float StartDelay
	{
		get
		{
			if (frames.Length < 2)
			{
				return 0f;
			}
			float num = 0f;
			for (int i = 1; i < frames.Length; i++)
			{
				num += frames[i].delay;
			}
			num /= (float)(frames.Length - 1);
			float delay = frames[0].delay;
			if (!(delay > 0.15f) || !(delay > num * 2f))
			{
				return 0f;
			}
			return delay;
		}
	}
}
