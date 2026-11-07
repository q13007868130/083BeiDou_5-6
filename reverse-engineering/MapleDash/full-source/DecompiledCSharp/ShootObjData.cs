using System.Collections.Generic;
using UnityEngine;

public sealed class ShootObjData
{
	public int skillId;

	public Vector2 start;

	public Vector2 bodyWH = new Vector2(100f, 100f);

	public float startDelayMs;

	public float deleteAlphaDelayMs;

	public int deleteAlpha;

	public float infoSpeed;

	public int maxMobCount;

	public int totalHitCount;

	public bool pickDir3Way;

	public bool pickDir5Way;

	public bool multiStartPos;

	public int groupCount = 1;

	public float groupIntervalMs;

	public float groupVMin;

	public float groupVMax;

	public float randomDelayMinMs;

	public float randomDelayMaxMs;

	public string sprayType;

	public int hitLimitEachMob;

	public int hitLimitEveryMob;

	public int multiAttackCount = 1;

	public float multiAttackHitDelayMs;

	public bool noHitWhenMaxCount;

	public bool useUpWhenMaxMobCount;

	public bool rotateAttack;

	public float flipRotate;

	public float startAlphaDelayMs;

	public bool startOffset;

	public bool triggerSpecialCore;

	public bool chaseTarget;

	public readonly List<ShootObjPath> paths = new List<ShootObjPath>();

	public readonly List<string> layers = new List<string>();

	public readonly List<Vector2> angles = new List<Vector2>();

	public const float FramesPerSecond = 60f;

	public int PerSweep => Mathf.Max(1, maxMobCount);

	public int PerMobLimit => Mathf.Max(1, Mathf.Max(hitLimitEachMob, hitLimitEveryMob));

	public int HitBudget(int fallback)
	{
		if (totalHitCount <= 0)
		{
			if (!noHitWhenMaxCount)
			{
				return int.MaxValue;
			}
			return Mathf.Max(1, fallback);
		}
		return totalHitCount;
	}

	public static float Speed(float v)
	{
		if (!(v <= 0f))
		{
			return v * 60f;
		}
		return 0f;
	}

	public float Duration(ShootObjPath path)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		float num = 0f;
		Vector2 pos = start;
		foreach (ShootObjPoint point in path.points)
		{
			num += point.delayMs;
			float num2 = Speed(point.v);
			if (num2 > 0f)
			{
				float num3 = point.pos.x - pos.x;
				float num4 = point.pos.y - pos.y;
				num += Mathf.Sqrt(num3 * num3 + num4 * num4) / num2 * 1000f;
			}
			pos = point.pos;
		}
		return num;
	}

	public ShootObjData()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
	}
}
