using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class NativeNameTag
{
	public sealed class Layout
	{
		public Texture2D texture;

		public int width;

		public int height;

		public int textX;

		public int textY;

		public Color color;
	}

	private static readonly Dictionary<string, Layout> cache = new Dictionary<string, Layout>();

	private static readonly int[] ringSlots = new int[4] { 12, 13, 15, 16 };

	public static int RingStyle(string field)
	{
		int result = 0;
		int[] array = ringSlots;
		foreach (int num in array)
		{
			int num2 = Profile.EquipOfSlot(-num - 100);
			if (num2 == 0)
			{
				num2 = Profile.EquipOfSlot(-num);
			}
			int num3 = EquipCatalog.Info(num2)?.Int(field) ?? 0;
			if (num3 != 0)
			{
				result = num3;
			}
		}
		return result;
	}

	public static bool WearsField(string field)
	{
		int[] array = ringSlots;
		foreach (int num in array)
		{
			int num2 = Profile.EquipOfSlot(-num - 100);
			if (num2 == 0)
			{
				num2 = Profile.EquipOfSlot(-num);
			}
			if (num2 != 0 && EquipCatalog.Info(num2)?.Get(field) != null)
			{
				return true;
			}
		}
		return false;
	}

	public static Layout Prepare(string path, int textWidth)
	{
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Expected Obj, but got Unknown
		//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		string text = path + ":" + textWidth;
		if (cache.TryGetValue(text, out var value))
		{
			return value;
		}
		ImgNode imgNode = WzAssets.Node(path);
		ImgNode imgNode2 = imgNode?.Get("w")?.TryResolve();
		ImgNode imgNode3 = imgNode?.Get("c")?.TryResolve();
		ImgNode imgNode4 = imgNode?.Get("e")?.TryResolve();
		if (imgNode2?.Kind != "Canvas" || imgNode3?.Kind != "Canvas" || imgNode4?.Kind != "Canvas" || imgNode3.Width <= 0)
		{
			cache[text] = null;
			return null;
		}
		int num = (Math.Max(0, textWidth) + imgNode3.Width - 1) / imgNode3.Width * imgNode3.Width;
		int num2 = Math.Max(OriginY(imgNode2), Math.Max(OriginY(imgNode3), OriginY(imgNode4)));
		int width = OriginX(imgNode2) + num + imgNode4.Width - OriginX(imgNode4);
		int height = num2 + Math.Max(imgNode2.Height - OriginY(imgNode2), Math.Max(imgNode3.Height - OriginY(imgNode3), imgNode4.Height - OriginY(imgNode4)));
		if (width <= 0 || height <= 0)
		{
			return null;
		}
		Color32[] pixels = new Color32[width * height];
		for (int i = 0; i < num; i += imgNode3.Width)
		{
			Copy(imgNode3, OriginX(imgNode2) + i, num2 - OriginY(imgNode3));
		}
		Copy(imgNode2, 0, num2 - OriginY(imgNode2));
		Copy(imgNode4, width - imgNode4.Width, 0);
		Texture2D val = new Texture2D(width, height, (TextureFormat)4, false)
		{
			name = "NameTag_" + text,
			filterMode = (FilterMode)0,
			wrapMode = (TextureWrapMode)1
		};
		val.SetPixels32(pixels);
		val.Apply();
		uint num3 = (uint)imgNode.Int("clr", -1);
		Layout layout = new Layout
		{
			texture = val,
			width = width,
			height = height,
			textX = OriginX(imgNode2) + (num - textWidth) / 2,
			textY = num2 - 2,
			color = Color32.op_Implicit(new Color32((byte)(num3 >> 16), (byte)(num3 >> 8), (byte)num3, (byte)(num3 >> 24)))
		};
		cache[text] = layout;
		return layout;
		void Copy(ImgNode node, int x, int y)
		{
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00da: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0117: Unknown result type (might be due to invalid IL or missing references)
			//IL_0122: Unknown result type (might be due to invalid IL or missing references)
			//IL_0144: Unknown result type (might be due to invalid IL or missing references)
			//IL_0149: Unknown result type (might be due to invalid IL or missing references)
			Color32[] pixels2 = WzAssets.Texture(node).GetPixels32();
			for (int j = 0; j < node.Height; j++)
			{
				for (int k = 0; k < node.Width; k++)
				{
					int num4 = x + k;
					int num5 = y + j;
					if (num4 >= 0 && num4 < width && num5 >= 0 && num5 < height)
					{
						Color32 val2 = pixels2[(node.Height - 1 - j) * node.Width + k];
						if (val2.a != 0)
						{
							int num6 = (height - 1 - num5) * width + num4;
							Color32 val3 = pixels[num6];
							float num7 = (float)(int)val2.a / 255f;
							float num8 = (float)(int)val3.a / 255f;
							float num9 = num7 + num8 * (1f - num7);
							pixels[num6] = new Color32((byte)(((float)(int)val2.r * num7 + (float)(int)val3.r * num8 * (1f - num7)) / num9), (byte)(((float)(int)val2.g * num7 + (float)(int)val3.g * num8 * (1f - num7)) / num9), (byte)(((float)(int)val2.b * num7 + (float)(int)val3.b * num8 * (1f - num7)) / num9), (byte)(num9 * 255f));
						}
					}
				}
			}
		}
		static int OriginX(ImgNode n)
		{
			return n.Get("origin")?.X ?? 0;
		}
		static int OriginY(ImgNode n)
		{
			return n.Get("origin")?.Y ?? 0;
		}
	}

	public static bool Draw(string path, string text, Vector2 anchor, GUIStyle style, out Rect bounds)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected Obj, but got Unknown
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		int num = Mathf.CeilToInt(style.CalcSize(new GUIContent(text)).x);
		Layout layout = Prepare(path, num);
		bounds = default;
		if (layout == null)
		{
			return false;
		}
		bounds = new Rect(anchor.x - (float)(layout.width / 2), anchor.y, (float)layout.width, (float)layout.height);
		GUI.DrawTexture(bounds, (Texture)(object)layout.texture);
		Color textColor = style.normal.textColor;
		style.normal.textColor = layout.color;
		GUI.Label(new Rect(bounds.x + (float)layout.textX, bounds.y + (float)layout.textY, (float)(num + 1), (float)layout.height), text, style);
		style.normal.textColor = textColor;
		return true;
	}
}
