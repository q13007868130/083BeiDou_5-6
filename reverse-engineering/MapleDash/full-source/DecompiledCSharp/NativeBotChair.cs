using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public sealed class NativeBotChair : MonoBehaviour
{
	private sealed class Layer
	{
		public ImgNode node;

		public NativeImgAnimation animation;
	}

	private readonly List<Layer> layers = new List<Layer>();

	private SpriteAnim avatar;

	private int chair;

	private string anchorFrame;

	private Vector2Int brow;

	public void Set(int id, SpriteAnim character)
	{
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		avatar = character;
		if (chair == id)
		{
			return;
		}
		foreach (Layer layer in layers)
		{
			if ((Object)(object)layer.animation != (Object)null)
			{
				layer.animation.Remove();
			}
		}
		layers.Clear();
		chair = id;
		anchorFrame = null;
		if (id <= 0)
		{
			return;
		}
		ImgNode imgNode = WzAssets.Node("Item/Install/0301.img/" + id.ToString("D8"));
		string[] array = new string[2] { "effect", "effect2" };
		foreach (string path in array)
		{
			ImgNode imgNode2 = imgNode?.Get(path);
			if (imgNode2 != null)
			{
				NativeImgAnimation nativeImgAnimation = NativeImgAnimation.SpawnAt(imgNode2, ((Component)this).transform, ((Component)this).transform.position, ((Renderer)character.Renderer).sortingOrder + imgNode2.Int("z"), flip: false, repeat: true);
				if ((Object)(object)nativeImgAnimation != (Object)null)
				{
					layers.Add(new Layer
					{
						node = imgNode2,
						animation = nativeImgAnimation
					});
				}
			}
		}
		Refresh();
	}

	public void Refresh()
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)avatar == (Object)null || layers.Count == 0)
		{
			return;
		}
		if (anchorFrame != avatar.FrameUri)
		{
			anchorFrame = avatar.FrameUri;
			brow = WzCharacter.Attachment(anchorFrame, "brow");
		}
		foreach (Layer layer in layers)
		{
			bool flipX = layer.node.Int("fixed") == 0 && avatar.Renderer.flipX;
			Vector2Int val = ((layer.node.Int("pos") == 1) ? brow : Vector2Int.zero);
			((Component)layer.animation).transform.localPosition = new Vector3((float)(avatar.Renderer.flipX ? (-val.x) : val.x) / 40f, (float)(-val.y) / 40f, 0f);
			layer.animation.Image.flipX = flipX;
		}
	}

	private void LateUpdate()
	{
		Refresh();
	}
}
