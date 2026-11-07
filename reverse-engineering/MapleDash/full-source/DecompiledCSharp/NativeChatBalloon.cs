using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class NativeChatBalloon
{
	public sealed class Layout
	{
		public Texture2D texture;

		public Texture2D[] frames;

		public int delay;

		public Rect text;

		public Color color;

		public int anchorX;

		public int lineHeight;

		public Texture2D Frame(double now)
		{
			if (frames == null || frames.Length <= 1 || delay <= 0)
			{
				return texture;
			}
			int num = (int)(now * 1000.0 / (double)delay) % frames.Length;
			if (!((Object)(object)frames[num] != (Object)null))
			{
				return texture;
			}
			return frames[num];
		}
	}

	private sealed class Pieces
	{
		public readonly Dictionary<string, ImgNode> Node = new Dictionary<string, ImgNode>();

		public int X(string name)
		{
			return Node[name].Get("origin")?.X ?? 0;
		}

		public int Y(string name)
		{
			return Node[name].Get("origin")?.Y ?? 0;
		}
	}

	private static readonly Dictionary<string, Layout> cache = new Dictionary<string, Layout>();

	private static readonly HashSet<string> diagnosed = new HashSet<string>();

	private static readonly string[] names = new string[11]
	{
		"c", "n", "s", "w", "e", "nw", "ne", "sw", "se", "arrow",
		"head"
	};

	public static Layout Prepare(int style, int innerWidth, int lines)
	{
		return Prepare(style.ToString(), innerWidth, lines);
	}

	public static Layout Prepare(string stylePath, int innerWidth, int lines)
	{
		//IL_04a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f8: Unknown result type (might be due to invalid IL or missing references)
		string text = stylePath + ":" + innerWidth + ":" + lines;
		if (cache.TryGetValue(text, out var value))
		{
			return value;
		}
		ImgNode imgNode = WzAssets.Node("UI/ChatBalloon.img/" + stylePath);
		List<ImgNode> list = FrameRoots(imgNode);
		if (list.Count == 0)
		{
			Diagnose(stylePath, "样式节点不存在或没有九宫格子节点");
			cache[text] = null;
			return null;
		}
		Pieces pieces = Collect(list[0]);
		if (pieces == null)
		{
			Diagnose(stylePath, "九宫格子节点不全（c/n/s/w/e/nw/ne/sw/se 必须是 Canvas）");
			cache[text] = null;
			return null;
		}
		int num = Math.Max(pieces.X("nw"), Math.Max(pieces.X("w"), pieces.X("sw")));
		int num2 = Math.Max(pieces.Node["ne"].Width - pieces.X("ne"), Math.Max(pieces.Node["e"].Width - pieces.X("e"), pieces.Node["se"].Width - pieces.X("se")));
		int num3 = Math.Max(pieces.Y("nw"), Math.Max(pieces.Y("n"), pieces.Y("ne")));
		int num4 = Math.Max(pieces.Node["sw"].Height - pieces.Y("sw"), Math.Max(pieces.Node["s"].Height - pieces.Y("s"), pieces.Node["se"].Height - pieces.Y("se")));
		if (pieces.Node.ContainsKey("arrow"))
		{
			num4 = Math.Max(num4, pieces.Node["arrow"].Height - pieces.Y("arrow"));
		}
		int num5 = ((lines > 1) ? 7 : (((Math.Max(1, innerWidth) + pieces.Node["n"].Width - 1) / pieces.Node["n"].Width) | 1));
		int num6 = Math.Max(pieces.Node["ne"].Height - pieces.Y("ne") + pieces.Y("se"), pieces.Node["nw"].Height - pieces.Y("nw") + pieces.Y("sw"));
		innerWidth = num5 * pieces.Node["n"].Width;
		int num7 = Math.Max(lines, (num6 + pieces.Node["w"].Height - 1) / pieces.Node["w"].Height) * pieces.Node["w"].Height;
		int num8 = num + innerWidth + num2;
		int num9 = num3 + num7 + num4;
		if (num8 <= 0 || num9 <= 0)
		{
			Diagnose(stylePath, "合成尺寸非法 " + num8 + "x" + num9);
			cache[text] = null;
			return null;
		}
		List<Texture2D> list2 = new List<Texture2D>(list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			Pieces pieces2 = ((i == 0) ? pieces : Collect(list[i]));
			if (pieces2 != null)
			{
				Color32[] pixels = Compose(pieces2, num, num2, num3, num4, num5, innerWidth, num7, num8, num9);
				list2.Add(ToTexture(pixels, num8, num9, "ChatBalloon_" + text + "#" + list2.Count));
			}
		}
		if (list2.Count == 0)
		{
			cache[text] = null;
			return null;
		}
		uint num10 = (uint)imgNode.Int("clr", -16777216);
		int val = list[0].Int("delay");
		Layout layout = new Layout
		{
			texture = list2[0],
			frames = list2.ToArray(),
			delay = ((list2.Count > 1) ? Math.Max(30, val) : 0),
			text = new Rect((float)num, (float)(num3 - 1), (float)innerWidth, (float)num7),
			anchorX = num + innerWidth / 2 - 3,
			lineHeight = pieces.Node["w"].Height,
			color = Color32.op_Implicit(new Color32((byte)(num10 >> 16), (byte)(num10 >> 8), (byte)num10, (byte)(num10 >> 24)))
		};
		if (cache.Count >= 48)
		{
			foreach (Layout value2 in cache.Values)
			{
				Release(value2);
			}
			cache.Clear();
		}
		cache[text] = layout;
		return layout;
	}

	private static List<ImgNode> FrameRoots(ImgNode root)
	{
		List<ImgNode> list = new List<ImgNode>();
		if (root == null)
		{
			return list;
		}
		if (root.Get("nw") != null)
		{
			list.Add(root);
			return list;
		}
		for (int i = 0; i < 64; i++)
		{
			ImgNode imgNode = root.Get(i.ToString())?.TryResolve();
			if (imgNode == null)
			{
				break;
			}
			list.Add(imgNode);
		}
		return list;
	}

	private static Pieces Collect(ImgNode container)
	{
		Pieces pieces = new Pieces();
		string[] array = names;
		foreach (string text in array)
		{
			ImgNode imgNode = container?.Get(text)?.TryResolve();
			if (imgNode?.Kind != "Canvas")
			{
				if (!(text == "arrow") && !(text == "head"))
				{
					return null;
				}
			}
			else
			{
				pieces.Node[text] = imgNode;
			}
		}
		return pieces;
	}

	private static Color32[] Compose(Pieces nodes, int left, int right, int top, int bottom, int columns, int innerWidth, int innerHeight, int width, int height)
	{
		Color32[] pixels = new Color32[width * height];
		for (int i = 0; i < columns; i++)
		{
			int x = left + i * nodes.Node["n"].Width;
			Copy((i == columns / 2 && nodes.Node.ContainsKey("head")) ? "head" : "n", x, top);
			Copy((i == columns / 2 && nodes.Node.ContainsKey("arrow")) ? "arrow" : "s", x, top + innerHeight);
		}
		for (int j = 0; j < innerHeight; j += nodes.Node["w"].Height)
		{
			Copy("w", left, top + j);
			for (int k = 0; k < columns; k++)
			{
				Copy("c", left + k * nodes.Node["n"].Width, top + j);
			}
			Copy("e", left + innerWidth, top + j);
		}
		Copy("nw", left, top);
		Copy("ne", left + innerWidth, top);
		Copy("sw", left, top + innerHeight);
		Copy("se", left + innerWidth, top + innerHeight);
		return pixels;
		void Copy(string name, int num, int y)
		{
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0105: Unknown result type (might be due to invalid IL or missing references)
			//IL_0110: Unknown result type (might be due to invalid IL or missing references)
			//IL_0129: Unknown result type (might be due to invalid IL or missing references)
			//IL_0134: Unknown result type (might be due to invalid IL or missing references)
			//IL_014d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0158: Unknown result type (might be due to invalid IL or missing references)
			//IL_017a: Unknown result type (might be due to invalid IL or missing references)
			//IL_017f: Unknown result type (might be due to invalid IL or missing references)
			ImgNode imgNode = nodes.Node[name];
			Color32[] pixels2 = WzAssets.Texture(imgNode).GetPixels32();
			num -= nodes.X(name);
			y -= nodes.Y(name);
			for (int l = 0; l < imgNode.Height; l++)
			{
				for (int m = 0; m < imgNode.Width; m++)
				{
					int num2 = num + m;
					int num3 = y + l;
					if (num2 >= 0 && num2 < width && num3 >= 0 && num3 < height)
					{
						Color32 val = pixels2[(imgNode.Height - 1 - l) * imgNode.Width + m];
						if (val.a != 0)
						{
							int num4 = (height - 1 - num3) * width + num2;
							Color32 val2 = pixels[num4];
							float num5 = (float)(int)val.a / 255f;
							float num6 = (float)(int)val2.a / 255f;
							float num7 = num5 + num6 * (1f - num5);
							pixels[num4] = new Color32((byte)(((float)(int)val.r * num5 + (float)(int)val2.r * num6 * (1f - num5)) / num7), (byte)(((float)(int)val.g * num5 + (float)(int)val2.g * num6 * (1f - num5)) / num7), (byte)(((float)(int)val.b * num5 + (float)(int)val2.b * num6 * (1f - num5)) / num7), (byte)(num7 * 255f));
						}
					}
				}
			}
		}
	}

	private static Texture2D ToTexture(Color32[] pixels, int width, int height, string name)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected Obj, but got Unknown
		Texture2D val = new Texture2D(width, height, (TextureFormat)4, false)
		{
			name = name,
			filterMode = (FilterMode)0,
			wrapMode = (TextureWrapMode)1
		};
		val.SetPixels32(pixels);
		val.Apply();
		return val;
	}

	private static void Release(Layout layout)
	{
		if (layout?.frames == null)
		{
			return;
		}
		Texture2D[] frames = layout.frames;
		foreach (Texture2D val in frames)
		{
			if ((Object)(object)val != (Object)null)
			{
				Object.Destroy((Object)(object)val);
			}
		}
	}

	private static void Diagnose(string stylePath, string reason)
	{
		if (diagnosed.Add(stylePath))
		{
			Debug.LogWarning((object)("[ChatBalloon] 样式 " + stylePath + " 无法合成（" + reason + "），回退默认气泡"));
		}
	}

	public static int TextWidth(int style)
	{
		return 7 * (PiecesOf(style)?.Node["n"].Width ?? 12);
	}

	public static int FontSize(int style)
	{
		return Math.Max(1, (PiecesOf(style)?.Node["w"].Height ?? 14) - 2);
	}

	private static Pieces PiecesOf(int style)
	{
		List<ImgNode> list = FrameRoots(WzAssets.Node("UI/ChatBalloon.img/" + style));
		if (list.Count != 0)
		{
			return Collect(list[0]);
		}
		return null;
	}
}
