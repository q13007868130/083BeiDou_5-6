using System;
using MapleDash.Wz;
using UnityEngine;

public static class NativeEquipBackdrop
{
	public struct Gap(int row, int col, int slot, string label, int glyph)
	{
		public readonly int row = row;

		public readonly int col = col;

		public readonly int slot = slot;

		public readonly int glyph = glyph;

		public readonly string label = label;
	}

	private struct LabelGlyph
	{
		public int width;

		public uint[] rows;
	}

	public const int Width = 175;

	public const int Height = 304;

	private const int GridX = 5;

	private const int GridY = 35;

	private const int Step = 33;

	private const int Cell = 32;

	public static readonly Color32 LabelColor = new Color32((byte)119, (byte)136, (byte)170, byte.MaxValue);

	public static readonly Color32 OutlineColor = new Color32((byte)221, (byte)238, (byte)238, byte.MaxValue);

	private const int BandTop = 2;

	private const int BandBottom = 17;

	private const int SeedLow = 18;

	private const int SeedHigh = 28;

	public static readonly Gap[] Gaps = new Gap[3]
	{
		new Gap(5, 3, -51, "副手", 0),
		new Gap(5, 4, -53, "图腾", 1),
		new Gap(6, 4, -56, "纹章", 2)
	};

	private static readonly LabelGlyph[] Glyphs = new LabelGlyph[3]
	{
		new LabelGlyph
		{
			width = 23,
			rows = new uint[11]
			{
				3933311u, 255232u, 132414u, 4187426u, 132414u, 132352u, 8385919u, 132425u, 132223u, 132169u,
				198527u
			}
		},
		new LabelGlyph
		{
			width = 22,
			rows = new uint[11]
			{
				1405951u, 4182537u, 152313u, 4190797u, 1135155u, 3134025u, 619415u, 4149793u, 2118161u, 3099169u,
				1076223u
			}
		},
		new LabelGlyph
		{
			width = 23,
			rows = new uint[11]
			{
				131140u, 4186244u, 559090u, 8385066u, 1065511u, 2081092u, 1065282u, 2080911u, 131408u, 8385068u,
				132115u
			}
		}
	};

	private static Texture2D patched;

	private static bool tried;

	public static string LabelOf(int slot)
	{
		Gap[] gaps = Gaps;
		for (int i = 0; i < gaps.Length; i++)
		{
			Gap gap = gaps[i];
			if (gap.slot == slot)
			{
				return gap.label;
			}
		}
		return null;
	}

	public static Texture2D Texture()
	{
		if (tried)
		{
			return patched;
		}
		tried = true;
		try
		{
			ImgNode imgNode = WzAssets.Node("UI/UIWindow.img/Equip/backgrnd");
			if (imgNode == null)
			{
				return null;
			}
			ImgNode imgNode2 = WzAssets.Canvas(imgNode);
			int width = imgNode2.Width;
			int height = imgNode2.Height;
			if (width != 175 || height != 304)
			{
				return null;
			}
			byte[] array = (byte[])WzAssets.Pixels(imgNode2).Clone();
			Gap[] gaps = Gaps;
			foreach (Gap gap in gaps)
			{
				Repair(array, width, height, gap);
			}
			int[] array2 = new int[2] { 2, 3 };
			foreach (int num in array2)
			{
				for (int j = 0; j < 32; j++)
				{
					Buffer.BlockCopy(array, ((35 + j) * width + 71) * 4, array, ((231 + j) * width + 5 + num * 33) * 4, 128);
				}
			}
			patched = WzAssets.Upload(array, width, height, "EquipBackgrnd");
		}
		catch (Exception ex)
		{
			Debug.LogWarning((object)("[EquipBackdrop] 背景修补失败，回退原图：" + ex.Message));
		}
		return patched;
	}

	private static void Repair(byte[] px, int w, int h, Gap gap)
	{
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		int num = 5 + gap.col * 33;
		int num2 = 35 + gap.row * 33;
		if (num < 1 || num2 < 1 || num + 32 > w || num2 + 32 > h)
		{
			return;
		}
		for (int i = 2; i <= 17; i++)
		{
			CopyRow(px, w, num, num2, i, SeedFor(i));
		}
		LabelGlyph labelGlyph = Glyphs[gap.glyph];
		int num3 = num + 3;
		int num4 = num2 + 3;
		for (int j = 0; j < labelGlyph.rows.Length; j++)
		{
			uint num5 = labelGlyph.rows[j];
			for (int k = 0; k < labelGlyph.width; k++)
			{
				if (((num5 >> k) & 1) != 0)
				{
					if (k > 0)
					{
						Put(px, w, num3 + k - 1, num4 + j, OutlineColor);
					}
					if (k < labelGlyph.width - 1)
					{
						Put(px, w, num3 + k + 1, num4 + j, OutlineColor);
					}
					Put(px, w, num3 + k, num4 + j - 1, OutlineColor);
					Put(px, w, num3 + k, num4 + j + 1, OutlineColor);
				}
			}
		}
		for (int l = 0; l < labelGlyph.rows.Length; l++)
		{
			uint num6 = labelGlyph.rows[l];
			for (int m = 0; m < labelGlyph.width; m++)
			{
				if (((num6 >> m) & 1) != 0)
				{
					Put(px, w, num3 + m, num4 + l, LabelColor);
				}
			}
		}
	}

	private static void Put(byte[] px, int w, int x, int y, Color32 c)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		int num = (y * w + x) * 4;
		px[num] = c.r;
		px[num + 1] = c.g;
		px[num + 2] = c.b;
		px[num + 3] = byte.MaxValue;
	}

	private static int SeedFor(int y)
	{
		return Math.Min(28, y + 18 - 2 + 2);
	}

	private static void CopyRow(byte[] px, int w, int x0, int y0, int y, int seed)
	{
		int num = (y0 + seed) * w;
		int num2 = (y0 + y) * w;
		for (int i = 1; i <= 30; i++)
		{
			int num3 = (num + x0 + i) * 4;
			int num4 = (num2 + x0 + i) * 4;
			px[num4] = px[num3];
			px[num4 + 1] = px[num3 + 1];
			px[num4 + 2] = px[num3 + 2];
			px[num4 + 3] = px[num3 + 3];
		}
	}

	static NativeEquipBackdrop()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
	}
}
