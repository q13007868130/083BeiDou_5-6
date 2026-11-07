using System;
using System.Collections.Generic;
using UnityEngine;

public class BossUnit : MonoBehaviour
{
	private enum St
	{
		Enter,
		Chase,
		Telegraph,
		Air,
		Recover,
		Summon
	}

	public StageBossDef def;

	public float hp;

	public float hpMax;

	public bool dead;

	public bool enraged;

	private St st;

	private float stT;

	private float actTimer;

	private int patIdx;

	private float contactTimer;

	private float airH;

	private float targetX;

	private int summonLeft;

	private float summonT;

	private float laneVis = 2f;

	private float flashT;

	private float terrainY = -1.4f;

	private float dieT = -1f;

	private float dieDur;

	private SpriteRenderer body;

	private SpriteAnim vis;

	private string bossArtKey = "mushmom";

	private Transform ring;

	private SpriteRenderer ringSr;

	private SpriteRenderer legShade;

	private const float GroundY = -1.4f;

	private static Sprite _ring;

	public float HpRatio
	{
		get
		{
			if (!(hpMax <= 0f))
			{
				return Mathf.Clamp01(hp / hpMax);
			}
			return 0f;
		}
	}

	private string BossSoundId
	{
		get
		{
			if (!(bossArtKey == "slimeking"))
			{
				return "6130101";
			}
			return "9300003";
		}
	}

	public int Lane => 2;

	public Dictionary<string, object> CombatStats => NativeCombatData.Mob((def != null && def.type == "slime") ? "slimeking" : "mushmom");

	private float RadiusUnit => Px.ToUnit((def != null) ? def.slam.radius : 122f);

	private float DepthOffset => (laneVis - 2f) * 0.22f;

	public Vector3 DamageAnchor
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			float x = ((Component)this).transform.position.x;
			float num;
			if (!((Object)(object)body != (Object)null) || !((Object)(object)body.sprite != (Object)null))
			{
				num = ((Component)this).transform.position.y + 2.2f;
			}
			else
			{
				Bounds bounds = ((Renderer)body).bounds;
				num = bounds.max.y;
			}
			return new Vector3(x, num - 0.2f, 0f);
		}
	}

	private static Sprite RingSprite()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected Obj, but got Unknown
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)_ring != (Object)null)
		{
			return _ring;
		}
		Texture2D val = new Texture2D(128, 128, (TextureFormat)4, false);
		Color[] array = new Color[16384];
		float num = 63.5f;
		for (int i = 0; i < 128; i++)
		{
			for (int j = 0; j < 128; j++)
			{
				float num2 = ((float)j - num) / num;
				float num3 = ((float)i - num) / num;
				float num4 = Mathf.Sqrt(num2 * num2 + num3 * num3);
				Color val2 = new Color(1f, 0.22f, 0.26f, 0f);
				if (num4 <= 1f && num4 >= 0.82f)
				{
					val2.a = 0.95f;
				}
				else if (num4 < 0.82f)
				{
					val2.a = 0.16f;
				}
				array[i * 128 + j] = val2;
			}
		}
		val.SetPixels(array);
		((Texture)val).filterMode = (FilterMode)1;
		((Texture)val).wrapMode = (TextureWrapMode)1;
		val.Apply();
		_ring = Sprite.Create(val, new Rect(0f, 0f, 128f, 128f), new Vector2(0.5f, 0.5f), 128f);
		return _ring;
	}

	public void Init(StageBossDef d, float startX)
	{
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Expected Obj, but got Unknown
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		def = d;
		hpMax = NativeCombatData.Int(CombatStats, "maxHP", 1);
		hp = hpMax;
		laneVis = Lane;
		float num = 1.5f * Mathf.Max(1f, d.scale) / 3f;
		float num2 = 2.3f * Mathf.Max(1f, d.scale) / 3f;
		Color color = ((d.type == "slime") ? new Color(0.45f, 0.82f, 0.55f) : new Color(0.78f, 0.34f, 0.42f));
		if ((Object)(object)body == (Object)null)
		{
			body = SpriteFactory.Make("BossBody", ((Component)this).transform, new Vector2(num, num2), color, 300);
		}
		body.color = color;
		bossArtKey = ((d.type == "slime") ? "slimeking" : "mushmom");
		if (Art.Has("mobs", bossArtKey))
		{
			if ((Object)(object)vis == (Object)null)
			{
				vis = Art.Attach(((Component)this).transform, "mobs", bossArtKey, "stand", 0f);
			}
			if ((Object)(object)vis != (Object)null)
			{
				SpriteRenderer renderer = vis.Renderer;
				((Renderer)renderer).sortingOrder = 300;
				renderer.color = Color.white;
				if ((Object)(object)body != (Object)null && (Object)(object)body != (Object)(object)renderer)
				{
					((Renderer)body).enabled = false;
				}
				body = renderer;
				Bounds bounds = ((Renderer)renderer).bounds;
				num2 = bounds.size.y;
				bounds = ((Renderer)renderer).bounds;
				num = bounds.size.x;
			}
		}
		if ((Object)(object)legShade == (Object)null)
		{
			legShade = SpriteFactory.Make("BossShadow", ((Component)this).transform, new Vector2(num * 1.15f, 0.16f), new Color(0f, 0f, 0f, 0.42f), 120, bottomPivot: false);
			((Component)legShade).transform.localPosition = new Vector3(0f, 0.02f, 0f);
		}
		if ((Object)(object)ring == (Object)null)
		{
			GameObject val = new GameObject("TelegraphRing");
			val.transform.SetParent((Transform)null, false);
			ringSr = val.AddComponent<SpriteRenderer>();
			ringSr.sprite = RingSprite();
			((Renderer)ringSr).sortingOrder = 110;
			((Renderer)ringSr).enabled = false;
			ring = val.transform;
		}
		terrainY = PlayerCtrl.GroundAt(startX, -1.4f);
		((Component)this).transform.position = new Vector3(startX, terrainY + DepthOffset, 0f);
		st = St.Enter;
		stT = 1.2f;
		actTimer = 1.4f;
		patIdx = 0;
		if ((Object)(object)GameLoop.I != (Object)null && (Object)(object)GameLoop.I.stage != (Object)null)
		{
			GameLoop.I.stage.OnBossSpawned(this);
		}
	}

	private float CurCooldown()
	{
		float hpRatio = HpRatio;
		float num = 2.2f;
		if (def != null && def.phases != null)
		{
			foreach (PhaseDef phase in def.phases)
			{
				if (hpRatio <= phase.at + 0.0001f)
				{
					num = phase.cooldown;
				}
			}
		}
		return Mathf.Max(0.6f, num);
	}

	private void Update()
	{
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0453: Unknown result type (might be due to invalid IL or missing references)
		//IL_0479: Unknown result type (might be due to invalid IL or missing references)
		//IL_049c: Unknown result type (might be due to invalid IL or missing references)
		//IL_039d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_040d: Unknown result type (might be due to invalid IL or missing references)
		if (dead)
		{
			TickDeathFade();
		}
		else
		{
			if (def == null)
			{
				return;
			}
			float deltaTime = Time.deltaTime;
			if (deltaTime <= 0f)
			{
				return;
			}
			PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
			stT -= deltaTime;
			flashT -= deltaTime;
			contactTimer -= deltaTime;
			switch (st)
			{
			case St.Enter:
				if (stT <= 0f)
				{
					st = St.Chase;
					actTimer = 1f;
				}
				break;
			case St.Chase:
				if ((Object)(object)playerCtrl != (Object)null)
				{
					ChaseMove(deltaTime, playerCtrl);
				}
				actTimer -= deltaTime;
				if (actTimer <= 0f)
				{
					PickAction();
				}
				break;
			case St.Telegraph:
				if ((Object)(object)ringSr != (Object)null)
				{
					float num = 1f - Mathf.Clamp01(stT / Mathf.Max(0.01f, def.slam.telegraph));
					float num2 = 0.55f + 0.45f * Mathf.Sin(Time.time * (10f + 22f * num));
					Color color = ringSr.color;
					ringSr.color = new Color(color.r, color.g, color.b, 0.75f + 0.25f * num2);
					float num3 = RadiusUnit * (2.06f - 0.06f * num2);
					((Component)ring).transform.localScale = new Vector3(num3, num3 * 0.42f, 1f);
				}
				if (stT <= 0f)
				{
					st = St.Air;
					stT = def.slam.air;
				}
				break;
			case St.Air:
				airH = Mathf.Sin(Mathf.Clamp01(1f - stT / Mathf.Max(0.01f, def.slam.air)) * MathF.PI) * 1.6f;
				if (stT <= 0f)
				{
					Land();
				}
				break;
			case St.Recover:
				airH = Mathf.Lerp(airH, 0f, 1f - Mathf.Exp(-18f * deltaTime));
				if (stT <= 0f)
				{
					st = St.Chase;
					actTimer = 0.8f;
				}
				break;
			case St.Summon:
				summonT -= deltaTime;
				if (summonT <= 0f && summonLeft > 0)
				{
					summonT = Mathf.Max(0.08f, def.summon.gap);
					summonLeft--;
					if ((Object)(object)GameLoop.I != (Object)null)
					{
						GameLoop.I.SpawnMob(def.summon.type, ((Component)this).transform.position.x + Random.Range(-2.2f, 2.2f));
					}
				}
				if (summonLeft <= 0 && stT <= 0f)
				{
					st = St.Chase;
					actTimer = 0.9f;
				}
				break;
			}
			if ((Object)(object)body != (Object)null && flashT <= 0f)
			{
				body.color = BaseColor();
			}
			if ((Object)(object)vis != (Object)null)
			{
				string text = ((st == St.Chase) ? "move" : "stand");
				if (st == St.Telegraph || st == St.Air)
				{
					text = "attack1";
				}
				vis.SetBase(text);
				vis.SetFacing(((Object)(object)playerCtrl != (Object)null && playerCtrl.worldX >= ((Component)this).transform.position.x) ? 1 : (-1));
			}
			laneVis = Mathf.Lerp(laneVis, (float)Lane, 1f - Mathf.Exp(-10f * deltaTime));
			terrainY = PlayerCtrl.GroundAt(((Component)this).transform.position.x, terrainY);
			((Component)this).transform.position = new Vector3(((Component)this).transform.position.x, terrainY + DepthOffset + airH, 0f);
			if ((Object)(object)body != (Object)null)
			{
				((Renderer)body).sortingOrder = 300 - Lane * 12;
			}
		}
	}

	private Color BaseColor()
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)vis != (Object)null))
		{
			if (def == null || !(def.type == "slime"))
			{
				return new Color(0.78f, 0.34f, 0.42f);
			}
			return new Color(0.45f, 0.82f, 0.55f);
		}
		return Color.white;
	}

	private void ChaseMove(float dt, PlayerCtrl p)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		float num = p.worldX - ((Component)this).transform.position.x;
		if (Mathf.Abs(num) > 1.6f)
		{
			float num2 = Px.ToUnit(def.spd);
			Transform transform = ((Component)this).transform;
			transform.position += Vector3.right * (Mathf.Sign(num) * num2 * dt);
		}
		else if (contactTimer <= 0f && Mathf.Abs(p.lane - Lane) <= 1)
		{
			contactTimer = 0.9f;
			p.TakeDamage(NativeCombatData.Int(CombatStats, "PADamage"), (num >= 0f) ? 1 : (-1), magic: false, NativeCombatData.Int(CombatStats, "level", 1));
			AudioManager.PlayAt("Mob/" + BossSoundId + "/Attack1", ((Component)this).transform.position, 1f, 14f);
		}
	}

	private void PickAction()
	{
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		string text = "chase";
		if (def.pattern != null && def.pattern.Count > 0)
		{
			text = def.pattern[patIdx % def.pattern.Count];
			patIdx++;
		}
		if (text == "slam")
		{
			PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
			targetX = (((Object)(object)playerCtrl != (Object)null) ? playerCtrl.worldX : ((Component)this).transform.position.x);
			st = St.Telegraph;
			stT = def.slam.telegraph;
			if ((Object)(object)ringSr != (Object)null)
			{
				((Renderer)ringSr).enabled = true;
				ringSr.color = new Color(1f, 0.25f, 0.3f, 0.9f);
				float num = PlayerCtrl.GroundAt(targetX, terrainY);
				((Component)ring).transform.position = new Vector3(targetX, num + 0.03f, 0f);
			}
			if ((Object)(object)GameLoop.I != (Object)null && (Object)(object)GameLoop.I.stage != (Object)null)
			{
				GameLoop.I.stage.OnBossTelegraph(def.slam.telegraph);
			}
		}
		else if (text == "summon")
		{
			st = St.Summon;
			stT = Mathf.Max(1.2f, (float)def.summon.count * def.summon.gap + 0.4f);
			summonLeft = def.summon.count;
			summonT = 0.15f;
		}
		else
		{
			st = St.Chase;
			actTimer = CurCooldown();
		}
	}

	private void Land()
	{
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		airH = 0f;
		if ((Object)(object)ringSr != (Object)null)
		{
			((Renderer)ringSr).enabled = false;
		}
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		if ((Object)(object)playerCtrl != (Object)null && Mathf.Abs(playerCtrl.worldX - targetX) <= RadiusUnit && playerCtrl.jumpY < 0.9f)
		{
			playerCtrl.TakeDamage(def.slam.damage, (playerCtrl.worldX >= targetX) ? 1 : (-1));
		}
		HitFeel.Punch(1.6f);
		float num = PlayerCtrl.GroundAt(targetX, terrainY);
		Fx.Ring(new Vector3(targetX, num + 0.06f, 0f), RadiusUnit * 1.6f, new Color(1f, 0.4f, 0.3f, 0.9f), 0.35f);
		Fx.Burst(new Vector3(targetX, num + 0.3f, 0f), RadiusUnit * 0.8f, new Color(1f, 0.75f, 0.4f, 0.9f), 0.3f);
		st = St.Recover;
		stT = def.slam.recover;
	}

	public void TakeDamage(float dmg, int fromDir)
	{
		if (!dead)
		{
			TakeHit(CombatRules.Resolve(dmg, critical: false, 19999999L), fromDir);
		}
	}

	public void TakeHit(DamageResult hit, int fromDir, bool nativeHit = false)
	{
		ApplyHit(hit, fromDir, nativeHit, showNumber: true);
	}

	public void TakeReplayHit(DamageResult[] hits, int direction, bool indexed, int interval, bool total = false, int ordinarySkill = -1)
	{
		if (!dead)
		{
			long num = 0L;
			bool flag = false;
			for (int i = 0; i < hits.Length; i++)
			{
				DamageResult damageResult = hits[i];
				num += damageResult.Amount;
				flag |= damageResult.Critical;
			}
			NativeDamageLines.Show((Component)(object)this, hits, indexed, interval, total, ordinarySkill);
			ApplyHit(new DamageResult(num, flag), direction, nativeHit: true, showNumber: false);
		}
	}

	public void TakeReplayDot(long amount)
	{
		ApplyHit(new DamageResult(amount, critical: false), 0, nativeHit: true, showNumber: true, react: false);
	}

	private void ApplyHit(DamageResult hit, int fromDir, bool nativeHit, bool showNumber, bool react = true)
	{
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		if (dead)
		{
			return;
		}
		if (hit.Amount <= 0)
		{
			if (showNumber && hit.Amount == 0L)
			{
				DamageText.Miss(DamageAnchor + Vector3.up * 0.375f);
			}
			return;
		}
		bool critical = hit.Critical;
		float num = hit.Amount;
		hp = Mathf.Max(0f, hp - num);
		if (react)
		{
			if ((Object)(object)body != (Object)null)
			{
				body.color = Color.white;
				flashT = HitFeel.FlashDur;
			}
			if ((Object)(object)vis != (Object)null)
			{
				vis.PlayOnce("hit");
			}
			if (!nativeHit)
			{
				Fx.Burst(((Component)this).transform.position + Vector3.up * 0.9f, critical ? 0.7f : 0.5f, new Color(1f, 0.85f, 0.4f, 0.9f));
			}
			AudioManager.PlayAt("Mob/" + BossSoundId + "/Damage", ((Component)this).transform.position, 1f, 14f);
		}
		if (showNumber)
		{
			DamageText.Show(DamageAnchor + Vector3.up * 0.375f, num, critical ? "cri" : "red");
		}
		if (hp <= 0f)
		{
			Die();
		}
	}

	public void Stun(float seconds)
	{
		if (st == St.Chase)
		{
			actTimer = Mathf.Max(actTimer, seconds * 0.35f);
		}
	}

	private void Die()
	{
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		dead = true;
		if ((Object)(object)ringSr != (Object)null)
		{
			((Renderer)ringSr).enabled = false;
		}
		dieDur = Art.GetClip("mobs", bossArtKey, Art.Resolve("mobs", bossArtKey, "die", "die1"))?.Duration ?? 0.6f;
		dieT = 0f;
		if ((Object)(object)vis != (Object)null)
		{
			vis.PlayOnce("die");
		}
		AudioManager.PlayAt("Mob/" + BossSoundId + "/Die", ((Component)this).transform.position, 1f, 16f);
		Fx.Burst(((Component)this).transform.position + Vector3.up * 1f, 3.2f, new Color(1f, 0.85f, 0.4f, 1f), 0.6f);
		Fx.Ring(((Component)this).transform.position + Vector3.up * 0.8f, 4.5f, new Color(1f, 0.9f, 0.5f, 0.95f), 0.5f);
		HitFeel.Punch(2.4f);
		if ((Object)(object)GameLoop.I != (Object)null)
		{
			if ((Object)(object)GameLoop.I.stage != (Object)null)
			{
				GameLoop.I.stage.OnBossKilled(this);
			}
			GameLoop.I.OnBossKilled(this);
		}
		Object.Destroy((Object)(object)((Component)this).gameObject, dieDur + 0.05f);
	}

	private void TickDeathFade()
	{
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		if (!(dieT < 0f) && !((Object)(object)body == (Object)null))
		{
			dieT += Time.deltaTime;
			float num = dieDur * 0.65f;
			float a = ((dieT <= num) ? 1f : Mathf.Clamp01(1f - (dieT - num) / Mathf.Max(0.01f, dieDur - num)));
			Color color = body.color;
			color.a = a;
			body.color = color;
		}
	}

	private void OnDestroy()
	{
		if ((Object)(object)ring != (Object)null)
		{
			Object.Destroy((Object)(object)((Component)ring).gameObject);
		}
	}
}
