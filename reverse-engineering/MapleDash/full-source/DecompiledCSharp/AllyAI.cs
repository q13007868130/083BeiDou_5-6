using UnityEngine;

public class AllyAI : MonoBehaviour
{
	public int lane = 2;

	public float xOffset = -1.6f;

	public float speed = 8f;

	public float attackInterval = 0.9f;

	public float reach = 2f;

	public float damage = 7f;

	public Color tint = Color.cyan;

	private float laneVis;

	private float atkTimer;

	private SpriteRenderer body;

	private SpriteAnim vis;

	private PlayerCtrl owner;

	private string artKey = "warrior";

	private float DepthOffset => (laneVis - 2f) * 0.22f;

	public void Setup(PlayerCtrl p, Color c, int laneIndex, float offset, string heroArt = null)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		owner = p;
		tint = c;
		lane = laneIndex;
		laneVis = laneIndex;
		xOffset = offset;
		if (!string.IsNullOrEmpty(heroArt))
		{
			artKey = heroArt;
		}
		Build();
	}

	private void Build()
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		body = ((Component)this).GetComponent<SpriteRenderer>();
		if ((Object)(object)body == (Object)null)
		{
			body = SpriteFactory.Make("AllyBody", ((Component)this).transform, new Vector2(0.7f, 1.25f), tint, 280);
		}
		body.color = tint;
		string key = (Art.Has("chars", artKey) ? artKey : (artKey + "_h0"));
		if (!Art.Has("chars", key))
		{
			return;
		}
		vis = Art.Attach(((Component)this).transform, "chars", key, "stand", 0f);
		if ((Object)(object)vis != (Object)null)
		{
			SpriteRenderer renderer = vis.Renderer;
			((Renderer)renderer).sortingOrder = 280;
			renderer.color = Color.white;
			if ((Object)(object)body != (Object)null && (Object)(object)body != (Object)(object)renderer)
			{
				((Renderer)body).enabled = false;
			}
			body = renderer;
		}
	}

	private void Update()
	{
		if (!((Object)(object)owner == (Object)null))
		{
			float deltaTime = Time.deltaTime;
			if (deltaTime > 0f)
			{
				Step(deltaTime);
			}
			Sync();
		}
	}

	private void Step(float dt)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_031a: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0329: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0359: Unknown result type (might be due to invalid IL or missing references)
		//IL_0365: Unknown result type (might be due to invalid IL or missing references)
		//IL_0375: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Clamp(owner.worldX - xOffset * ((owner.facing >= 0) ? 1f : (-1f)), PlayerCtrl.XMin, PlayerCtrl.XMax) - ((Component)this).transform.position.x;
		bool flag = Mathf.Abs(num) > 0.12f;
		if (flag)
		{
			float num2 = speed * Mathf.Clamp(Mathf.Abs(num) / 2f, 0.55f, 1.6f);
			Transform transform = ((Component)this).transform;
			transform.position += Vector3.right * (Mathf.Sign(num) * num2 * dt);
		}
		laneVis = Mathf.Lerp(laneVis, (float)lane, 1f - Mathf.Exp(-11f * dt));
		if ((Object)(object)vis != (Object)null)
		{
			vis.SetBase(flag ? "walk" : "stand");
			int facing = ((!flag) ? ((!((Object)(object)owner != (Object)null)) ? 1 : owner.facing) : ((num >= 0f) ? 1 : (-1)));
			vis.SetFacing(facing);
		}
		atkTimer -= dt;
		if (!(atkTimer <= 0f))
		{
			return;
		}
		BossUnit bossUnit = FindBoss();
		if ((Object)(object)bossUnit != (Object)null)
		{
			atkTimer = attackInterval;
			bossUnit.TakeDamage(damage, (((Component)bossUnit).transform.position.x >= ((Component)this).transform.position.x) ? 1 : (-1));
			HitFeel.Punch(0.5f);
			if ((Object)(object)vis != (Object)null)
			{
				vis.PlayOnce("atk", 1.2f);
			}
			Fx.Slash(new Vector3(((Component)this).transform.position.x + ((((Component)bossUnit).transform.position.x >= ((Component)this).transform.position.x) ? 0.8f : (-0.8f)), ((Component)this).transform.position.y + 0.65f, 0f), new Color(tint.r, tint.g, tint.b, 0.8f), (((Component)bossUnit).transform.position.x >= ((Component)this).transform.position.x) ? 1 : (-1));
			return;
		}
		EnemyUnit enemyUnit = FindTarget();
		if ((Object)(object)enemyUnit != (Object)null)
		{
			atkTimer = attackInterval;
			enemyUnit.TakeDamage(damage, (((Component)enemyUnit).transform.position.x >= ((Component)this).transform.position.x) ? 1 : (-1));
			HitFeel.Punch(0.55f);
			if ((Object)(object)vis != (Object)null)
			{
				vis.PlayOnce("atk", 1.2f);
			}
			Fx.Slash(((Component)this).transform.position + Vector3.up * 0.65f, new Color(tint.r, tint.g, tint.b, 0.8f), (((Component)enemyUnit).transform.position.x >= ((Component)this).transform.position.x) ? 1 : (-1));
		}
		else
		{
			atkTimer = 0.15f;
		}
	}

	private BossUnit FindBoss()
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		BossUnit[] array = Object.FindObjectsOfType<BossUnit>();
		foreach (BossUnit bossUnit in array)
		{
			if (!((Object)(object)bossUnit == (Object)null) && !bossUnit.dead && Mathf.Abs(bossUnit.Lane - lane) <= 2 && Mathf.Abs(((Component)bossUnit).transform.position.x - ((Component)this).transform.position.x) <= reach + 2.4f)
			{
				return bossUnit;
			}
		}
		return null;
	}

	private EnemyUnit FindTarget()
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		EnemyUnit result = null;
		float num = float.MaxValue;
		foreach (EnemyUnit item in EnemyUnit.All)
		{
			if (!((Object)(object)item == (Object)null) && !item.dead && Mathf.Abs(item.Lane - lane) <= 1)
			{
				float num2 = Mathf.Abs(((Component)item).transform.position.x - ((Component)this).transform.position.x);
				if (!(num2 > reach) && num2 < num)
				{
					num = num2;
					result = item;
				}
			}
		}
		return result;
	}

	private void Sync()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		((Component)this).transform.position = new Vector3(((Component)this).transform.position.x, -1.4f + DepthOffset, 0f);
		if ((Object)(object)body != (Object)null)
		{
			((Renderer)body).sortingOrder = 300 - lane * 12 - 2;
		}
	}

	public AllyAI()
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
	}
}
