using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public sealed class NativeImgAnimation : MonoBehaviour
{
	private List<ImgNode> frames;

	private SpriteRenderer image;

	private double started;

	private double expires;

	private bool repeat;

	public Color Tint = Color.white;

	public SpriteRenderer Image => image;

	public static NativeImgAnimation ShowInfo(string path)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		if ((Object)(object)playerCtrl == (Object)null || string.IsNullOrEmpty(path))
		{
			return null;
		}
		return Spawn(path, ((Component)playerCtrl).transform, Vector2.op_Implicit(playerCtrl.FeetPos), playerCtrl.SortingOrder + 1);
	}

	public static NativeImgAnimation Spawn(string path, Transform parent, Vector3 position, int order, bool flip = false, bool repeat = false, float lifetime = 0f)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return SpawnAt(WzAssets.Node(path), parent, position, order, flip, repeat, lifetime);
	}

	public static NativeImgAnimation SpawnAt(ImgNode action, Transform parent, Vector3 position, int order, bool flip = false, bool repeat = false, float lifetime = 0f)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected Obj, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (action == null)
		{
			return null;
		}
		List<ImgNode> list = WzAssets.Frames(action);
		if (list.Count == 0)
		{
			return null;
		}
		GameObject val = new GameObject("imgfx");
		val.transform.SetParent(parent, false);
		val.transform.position = position;
		NativeImgAnimation nativeImgAnimation = val.AddComponent<NativeImgAnimation>();
		nativeImgAnimation.frames = list;
		nativeImgAnimation.repeat = repeat;
		nativeImgAnimation.started = Time.realtimeSinceStartupAsDouble;
		nativeImgAnimation.expires = ((lifetime > 0f) ? (nativeImgAnimation.started + (double)lifetime) : double.PositiveInfinity);
		if (lifetime <= 0f && !repeat && list.Count > 1)
		{
			double num = 0.0;
			foreach (ImgNode item in list)
			{
				num += (double)Mathf.Max(1, Mathf.Abs(item.Int("delay", 100))) / 1000.0;
			}
			nativeImgAnimation.expires = nativeImgAnimation.started + num;
		}
		nativeImgAnimation.image = val.AddComponent<SpriteRenderer>();
		((Renderer)nativeImgAnimation.image).sortingOrder = order;
		nativeImgAnimation.image.flipX = flip;
		nativeImgAnimation.Tick(nativeImgAnimation.started);
		return nativeImgAnimation;
	}

	public void Tick(double now)
	{
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		if (now >= expires)
		{
			Remove();
			return;
		}
		double num = 0.0;
		foreach (ImgNode frame in frames)
		{
			num += (double)Mathf.Max(1, Mathf.Abs(frame.Int("delay", 100))) / 1000.0;
		}
		double num2 = now - started;
		if (num2 >= num && !repeat)
		{
			if (frames.Count != 1)
			{
				((Renderer)image).enabled = false;
				return;
			}
			num2 = 0.0;
		}
		if (repeat)
		{
			num2 %= num;
		}
		foreach (ImgNode frame2 in frames)
		{
			double num3 = (double)Mathf.Max(1, Mathf.Abs(frame2.Int("delay", 100))) / 1000.0;
			if (num2 < num3)
			{
				image.sprite = WzAssets.Sprite(frame2);
				Color tint = Tint;
				tint.a *= Mathf.Lerp((float)frame2.Int("a0", 255), (float)frame2.Int("a1", 255), (float)(num2 / num3)) / 255f;
				image.color = tint;
				break;
			}
			num2 -= num3;
		}
	}

	private void Update()
	{
		Tick(Time.realtimeSinceStartupAsDouble);
	}

	public void Remove()
	{
		if (Application.isPlaying)
		{
			Object.Destroy((Object)(object)((Component)this).gameObject);
		}
		else
		{
			Object.DestroyImmediate((Object)(object)((Component)this).gameObject);
		}
	}

	public NativeImgAnimation()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
	}
}
