using System;
using System.Collections.Generic;
using System.Globalization;
using MapleDash.Wz;
using UnityEngine;

public sealed class NativeDamageNumber : MonoBehaviour
{
	private SpriteRenderer image;

	private Texture2D texture;

	private Sprite sprite;

	private Vector3 anchor;

	private double started;

	private static ImgNode selectedRoot;

	private static int loadedSkin;

	private static int failedSkin = -1;

	public static void ResetSkinFailure()
	{
		failedSkin = -1;
	}

	private static ImgNode Root(string color)
	{
		ImgNode imgNode = WzAssets.FileRoot("Effect/BasicEff.img");
		if (color == "blue" || color == "violet")
		{
			return imgNode;
		}
		if (selectedRoot == null)
		{
			selectedRoot = imgNode;
			loadedSkin = 0;
		}
		int selected = NativeDamageSkinCatalog.Selected;
		if (selected == loadedSkin || selected == failedSkin)
		{
			return selectedRoot;
		}
		try
		{
			ImgNode imgNode2 = ((selected == 0) ? imgNode : WzAssets.FileRoot("Effect/DamageSkin/" + selected + ".img"));
			string[] array = new string[4] { "NoRed0", "NoRed1", "NoCri0", "NoCri1" };
			foreach (string text in array)
			{
				if (imgNode2.Get(text)?.Resolve() == null)
				{
					throw new InvalidOperationException("Missing " + text);
				}
			}
			selectedRoot = imgNode2;
			loadedSkin = selected;
		}
		catch (Exception ex)
		{
			failedSkin = selected;
			Debug.LogWarning((object)("[DamageSkin] Retaining current skin: " + ex.Message));
		}
		return selectedRoot;
	}

	public static void Miss(Vector3 position, string color, float scale)
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected Obj, but got Unknown
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		if (!(color == "blue"))
		{
			ImgNode imgNode = Root(color).Get(((color == "violet") ? "NoViolet0" : "NoRed0") + "/Miss")?.Resolve();
			if (imgNode != null)
			{
				ImgNode imgNode2 = WzAssets.Canvas(imgNode);
				GameObject val = new GameObject("NativeDamage_Miss");
				NativeDamageNumber nativeDamageNumber = val.AddComponent<NativeDamageNumber>();
				nativeDamageNumber.texture = WzAssets.Upload(WzAssets.Pixels(imgNode), imgNode2.Width, imgNode2.Height, ((Object)val).name);
				nativeDamageNumber.sprite = Sprite.Create(nativeDamageNumber.texture, new Rect(0f, 0f, (float)imgNode2.Width, (float)imgNode2.Height), new Vector2(0f, 1f), 40f);
				nativeDamageNumber.image = val.AddComponent<SpriteRenderer>();
				nativeDamageNumber.image.sprite = nativeDamageNumber.sprite;
				((Renderer)nativeDamageNumber.image).sortingOrder = 900;
				val.transform.localScale = new Vector3(scale, scale, 1f);
				nativeDamageNumber.anchor = position + new Vector3((float)(-(imgNode2.Width / 2)) * scale / 40f, (float)imgNode2.Height * scale / 40f, 0f);
				val.transform.position = nativeDamageNumber.anchor;
				nativeDamageNumber.started = Time.realtimeSinceStartupAsDouble;
			}
		}
	}

	public static void Show(Vector3 position, long amount, string color, float scale)
	{
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Expected Obj, but got Unknown
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		string text = color switch
		{
			"violet" => "NoViolet", 
			"blue" => "NoBlue", 
			"cri" => "NoCri", 
			_ => "NoRed", 
		};
		ImgNode imgNode = Root(color);
		string text2 = amount.ToString(CultureInfo.InvariantCulture);
		List<ImgNode> list = new List<ImgNode>();
		List<int> list2 = new List<int>();
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < text2.Length; i++)
		{
			ImgNode imgNode2 = imgNode.Get(text + ((i == 0) ? "1/" : "0/") + text2[i])?.Resolve();
			if (imgNode2 == null)
			{
				throw new InvalidOperationException("Missing damage glyph: " + text + "/" + text2[i]);
			}
			ImgNode imgNode3 = WzAssets.Canvas(imgNode2);
			int num3 = (int)WzAssets.Origin(imgNode2).x;
			int num4 = imgNode3.Width - num3;
			int num5 = num + num3 - num2;
			list2.Add(num5 - num3);
			num = num5 + num4;
			num2 = num4 * 3 / 5;
			list.Add(imgNode2);
		}
		byte[] array = new byte[checked(num * 57 * 4)];
		if (color == "cri")
		{
			ImgNode imgNode4 = imgNode.Get("NoCri1/effect");
			if (imgNode4 != null)
			{
				Copy(array, num, 57, imgNode4, 0, 0);
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			int height = WzAssets.Canvas(list[j]).Height;
			int y = 57 - ((j % 2 != 0) ? 1 : 4) - Random.Range(0, 2) - height;
			Copy(array, num, 57, list[j], list2[j], y);
		}
		GameObject val = new GameObject("NativeDamage_" + text2);
		NativeDamageNumber nativeDamageNumber = val.AddComponent<NativeDamageNumber>();
		nativeDamageNumber.texture = WzAssets.Upload(array, num, 57, ((Object)val).name);
		nativeDamageNumber.sprite = Sprite.Create(nativeDamageNumber.texture, new Rect(0f, 0f, (float)num, 57f), new Vector2(0f, 1f), 40f);
		nativeDamageNumber.image = val.AddComponent<SpriteRenderer>();
		nativeDamageNumber.image.sprite = nativeDamageNumber.sprite;
		((Renderer)nativeDamageNumber.image).sortingOrder = 900;
		val.transform.localScale = new Vector3(scale, scale, 1f);
		nativeDamageNumber.anchor = position + new Vector3((float)(-(num / 2)) * scale / 40f, 47f * scale / 40f, 0f);
		val.transform.position = nativeDamageNumber.anchor;
		nativeDamageNumber.started = Time.realtimeSinceStartupAsDouble;
	}

	private static void Copy(byte[] target, int width, int height, ImgNode node, int x, int y)
	{
		ImgNode imgNode = WzAssets.Canvas(node);
		byte[] array = WzAssets.Pixels(node);
		for (int i = Math.Max(0, -y); i < imgNode.Height && i + y < height; i++)
		{
			for (int j = Math.Max(0, -x); j < imgNode.Width && j + x < width; j++)
			{
				int num = (i * imgNode.Width + j) * 4;
				int num2 = ((i + y) * width + j + x) * 4;
				int num3 = array[num + 3];
				if (num3 != 0)
				{
					int num4 = target[num2 + 3] * (255 - num3);
					int num5 = num3 * 255 + num4;
					for (int k = 0; k < 3; k++)
					{
						target[num2 + k] = (byte)((array[num + k] * num3 * 255 + target[num2 + k] * num4) / num5);
					}
					target[num2 + 3] = (byte)(num5 / 255);
				}
			}
		}
	}

	private void Update()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		double num = Time.realtimeSinceStartupAsDouble - started;
		if (num >= 1.0)
		{
			Object.Destroy((Object)(object)((Component)this).gameObject);
			return;
		}
		((Component)this).transform.position = anchor + Vector3.up * (float)(30.0 * num / 40.0);
		image.color = new Color(1f, 1f, 1f, (num <= 0.4) ? 1f : ((float)((1.0 - num) / 0.6)));
	}

	private void OnDestroy()
	{
		if ((Object)(object)sprite != (Object)null)
		{
			if (Application.isPlaying)
			{
				Object.Destroy((Object)(object)sprite);
			}
			else
			{
				Object.DestroyImmediate((Object)(object)sprite);
			}
		}
		if ((Object)(object)texture != (Object)null)
		{
			if (Application.isPlaying)
			{
				Object.Destroy((Object)(object)texture);
			}
			else
			{
				Object.DestroyImmediate((Object)(object)texture);
			}
		}
	}
}
