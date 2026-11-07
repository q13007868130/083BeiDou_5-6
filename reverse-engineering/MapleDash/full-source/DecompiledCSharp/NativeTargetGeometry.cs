using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class NativeTargetGeometry
{
	private static readonly Dictionary<int, Nullable> boxes = new Dictionary<int, Nullable>();

	public static Nullable Build(ImgNode root)
	{
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		Nullable result = null;
		if (root == null)
		{
			return null;
		}
		foreach (ImgNode child in root.Children)
		{
			string name = child.Name;
			if (!name.StartsWith("stand", StringComparison.Ordinal) && !name.StartsWith("move", StringComparison.Ordinal) && !name.StartsWith("fly", StringComparison.Ordinal) && !name.StartsWith("jump", StringComparison.Ordinal) && !name.StartsWith("attack", StringComparison.Ordinal) && !name.StartsWith("hit", StringComparison.Ordinal))
			{
				continue;
			}
			ImgNode imgNode = child.TryResolve();
			if (imgNode == null)
			{
				continue;
			}
			foreach (ImgNode child2 in imgNode.Children)
			{
				ImgNode imgNode2 = child2.TryResolve();
				if (imgNode2 == null || imgNode2.Kind != "Canvas")
				{
					continue;
				}
				ImgNode imgNode3 = imgNode2.Get("lt");
				ImgNode imgNode4 = imgNode2.Get("rb");
				int num;
				int num2;
				int num3;
				int num4;
				if (imgNode3 != null && imgNode4 != null)
				{
					num = imgNode3.X;
					num2 = imgNode3.Y;
					num3 = imgNode4.X;
					num4 = imgNode4.Y;
				}
				else
				{
					ImgNode imgNode5 = imgNode2.Get("origin");
					if (imgNode5 == null || imgNode2.Width <= 0 || imgNode2.Height <= 0)
					{
						continue;
					}
					num = -imgNode5.X;
					num2 = -imgNode5.Y;
					num3 = num + imgNode2.Width;
					num4 = num2 + imgNode2.Height;
				}
				Rect val = Rect.MinMaxRect((float)num, (float)num2, (float)num3, (float)num4);
				Rect value;
				if (!result.HasValue)
				{
					value = val;
				}
				else
				{
					Rect value2 = result.Value;
					float num5 = Math.Min(value2.xMin, val.xMin);
					value2 = result.Value;
					float num6 = Math.Min(value2.yMin, val.yMin);
					value2 = result.Value;
					float num7 = Math.Max(value2.xMax, val.xMax);
					value2 = result.Value;
					value = Rect.MinMaxRect(num5, num6, num7, Math.Max(value2.yMax, val.yMax));
				}
				result = value;
			}
		}
		return result;
	}

	public static Rect World(Rect pixels, Vector2 position, bool mirror)
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		float num = (mirror ? (0f - pixels.xMax) : pixels.xMin);
		float num2 = (mirror ? (0f - pixels.xMin) : pixels.xMax);
		float num3 = Math.Min(num, num2);
		float num4 = Math.Min(pixels.yMin, pixels.yMax);
		float num5 = num3 + Math.Max(1f, Math.Abs(num2 - num));
		float num6 = num4 + Math.Max(1f, Math.Abs(pixels.yMax - pixels.yMin));
		return Rect.MinMaxRect(position.x + num3 / 40f, position.y - num6 / 40f, position.x + num5 / 40f, position.y - num4 / 40f);
	}

	private static ImgNode VisualRoot(int id)
	{
		ImgNode imgNode = WzAssets.FileRoot("Mob/" + id.ToString("D7") + ".img");
		ImgNode imgNode2 = imgNode;
		HashSet<int> hashSet = new HashSet<int>();
		while (imgNode2 != null && hashSet.Add(id))
		{
			ImgNode imgNode3 = imgNode2.Get("fly/0")?.TryResolve();
			ImgNode imgNode4 = imgNode2.Get("stand/0")?.TryResolve();
			if (imgNode3?.Kind == "Canvas" || imgNode4?.Kind == "Canvas" || Build(imgNode2).HasValue)
			{
				return imgNode2;
			}
			int num = imgNode2.Int("info/link");
			if (num == 0)
			{
				break;
			}
			id = num;
			imgNode2 = WzAssets.FileRoot("Mob/" + id.ToString("D7") + ".img");
		}
		return imgNode;
	}

	public static Nullable Bounds(EnemyUnit mob, bool mirror)
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		if (!boxes.TryGetValue(mob.fieldMobId, out var value))
		{
			value = (boxes[mob.fieldMobId] = Build(VisualRoot(mob.fieldMobId)));
		}
		if (!value.HasValue)
		{
			return null;
		}
		bool flag = WzAssets.FileRoot("Mob/" + mob.fieldMobId.ToString("D7") + ".img").Int("info/noFlip") > 0;
		return World(value.Value, Vector2.op_Implicit(((Component)mob).transform.position), mirror && !flag);
	}
}
