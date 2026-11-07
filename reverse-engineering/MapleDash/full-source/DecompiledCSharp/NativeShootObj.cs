using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class NativeShootObj
{
	private static readonly Dictionary<int, ShootObjData> cache = new Dictionary<int, ShootObjData>();

	public static int Count => cache.Count;

	public static ShootObjData Get(int id)
	{
		if (cache.TryGetValue(id, out var value))
		{
			return value;
		}
		ShootObjData shootObjData = Read(id);
		cache.Add(id, shootObjData);
		return shootObjData;
	}

	public static bool Has(int id)
	{
		return Get(id) != null;
	}

	private static ShootObjData Read(int id)
	{
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_036d: Unknown result type (might be due to invalid IL or missing references)
		//IL_043e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0443: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = WzAssets.Node("Skill/" + (id / 10000).ToString("D3") + ".img/skill/" + id.ToString("D7") + "/shootobj")?.TryResolve();
		if (imgNode == null)
		{
			return null;
		}
		ShootObjData shootObjData = new ShootObjData
		{
			skillId = id
		};
		ImgNode imgNode2 = imgNode.Get("info")?.TryResolve();
		if (imgNode2 != null)
		{
			shootObjData.start = Vec(imgNode2.Get("start"));
			Vector2 val = Vec(imgNode2.Get("bodyWH"));
			if (val.x > 0f && val.y > 0f)
			{
				shootObjData.bodyWH = val;
			}
			shootObjData.startDelayMs = imgNode2.Int("startDelay");
			shootObjData.infoSpeed = Num(imgNode2.Get("v"));
			shootObjData.maxMobCount = imgNode2.Int("maxMobCount", 1);
			shootObjData.totalHitCount = imgNode2.Int("totalHitCount", 1);
			shootObjData.deleteAlphaDelayMs = imgNode2.Int("deleteAlphaDelay");
			shootObjData.deleteAlpha = imgNode2.Int("deleteAlpha");
			shootObjData.pickDir3Way = imgNode2.Int("pickDir3Way") != 0;
			shootObjData.pickDir5Way = imgNode2.Int("pickDir5Way") != 0;
			shootObjData.multiStartPos = imgNode2.Int("multiStartPos") != 0;
			shootObjData.groupCount = Mathf.Max(1, imgNode2.Int("groupbuild/count", 1));
			shootObjData.groupIntervalMs = imgNode2.Int("groupbuild/startInterval", imgNode2.Int("groupbuild/startinterval"));
			shootObjData.groupVMin = imgNode2.Int("groupbuild/vmin");
			shootObjData.groupVMax = imgNode2.Int("groupbuild/vmax");
			shootObjData.randomDelayMinMs = imgNode2.Int("randomCreateDelayMin");
			shootObjData.randomDelayMaxMs = imgNode2.Int("randomCreateDelayMax");
			shootObjData.sprayType = imgNode2.Text("sprayType");
			shootObjData.hitLimitEachMob = imgNode2.Int("hitLimitEachMob");
			shootObjData.hitLimitEveryMob = imgNode2.Int("hitLimitEveryMob");
			shootObjData.multiAttackCount = Mathf.Max(1, imgNode2.Int("multiAttackCount", 1));
			shootObjData.multiAttackHitDelayMs = imgNode2.Int("multiAttackHitDelay");
			shootObjData.noHitWhenMaxCount = imgNode2.Int("noHitWhenMaxCount") != 0;
			shootObjData.useUpWhenMaxMobCount = imgNode2.Int("useUpWhenMaxMobCount") != 0;
			shootObjData.rotateAttack = imgNode2.Int("rotateAttack") != 0;
			shootObjData.flipRotate = Num(imgNode2.Get("rorateWhenFlip"));
			shootObjData.startAlphaDelayMs = imgNode2.Int("startAlphaDelay");
			shootObjData.startOffset = imgNode2.Int("startOffset") != 0;
			shootObjData.triggerSpecialCore = imgNode2.Int("triggerSpecialCore") != 0;
			shootObjData.chaseTarget = shootObjData.sprayType == "AngelRay";
			ImgNode imgNode3 = imgNode2.Get("angleList");
			if (imgNode3 != null)
			{
				foreach (ImgNode child in imgNode3.Children)
				{
					ImgNode imgNode4 = child.TryResolve();
					if (imgNode4 != null)
					{
						shootObjData.angles.Add(new Vector2((float)imgNode4.Int("min"), (float)imgNode4.Int("max")));
					}
				}
			}
		}
		ImgNode imgNode5 = imgNode.Get("moveList")?.TryResolve();
		if (imgNode5 != null)
		{
			foreach (ImgNode child2 in imgNode5.Children)
			{
				ImgNode imgNode6 = child2.TryResolve();
				if (imgNode6 == null)
				{
					continue;
				}
				ShootObjPath shootObjPath = new ShootObjPath
				{
					name = child2.Name
				};
				foreach (ImgNode child3 in imgNode6.Children)
				{
					ImgNode imgNode7 = child3.TryResolve();
					if (imgNode7 != null)
					{
						ImgNode imgNode8 = imgNode7.Get("pos")?.TryResolve();
						shootObjPath.points.Add(new ShootObjPoint
						{
							pos = Vec(imgNode8),
							hasPos = (imgNode8 != null && imgNode8.Kind == "Shape2D#Vector2D"),
							v = Num(imgNode7.Get("v")),
							delayMs = imgNode7.Int("delay")
						});
					}
				}
				if (shootObjPath.points.Count > 0)
				{
					shootObjData.paths.Add(shootObjPath);
				}
			}
		}
		ImgNode imgNode9 = imgNode.Get("layerList")?.TryResolve();
		if (imgNode9 != null)
		{
			foreach (ImgNode child4 in imgNode9.Children)
			{
				shootObjData.layers.Add(child4.Name);
			}
		}
		if (shootObjData.paths.Count == 0 && shootObjData.infoSpeed <= 0f)
		{
			return null;
		}
		return shootObjData;
	}

	private static Vector2 Vec(ImgNode node)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = node?.TryResolve();
		if (imgNode == null)
		{
			return Vector2.zero;
		}
		if (imgNode.Kind == "Shape2D#Vector2D")
		{
			return new Vector2((float)imgNode.X, (float)imgNode.Y);
		}
		if (imgNode.Value is int num)
		{
			return new Vector2((float)num, 0f);
		}
		return Vector2.zero;
	}

	private static float Num(ImgNode node)
	{
		ImgNode imgNode = node?.TryResolve();
		if (imgNode != null && imgNode.Value != null)
		{
			return Convert.ToSingle(imgNode.Value);
		}
		return 0f;
	}
}
