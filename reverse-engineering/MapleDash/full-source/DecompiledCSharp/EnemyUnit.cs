using System;
using System.Collections.Generic;
using System.IO;
using MapleDash.Wz;
using UnityEngine;

public class EnemyUnit : MonoBehaviour
{
	public readonly Dictionary<int, long> DamageContributions = new Dictionary<int, long>();

	public long TotalHealed;

	public static readonly List<EnemyUnit> All = new List<EnemyUnit>();

	public string label = "杂兵";

	public string artKey = "mushroom";

	public long hpMax = 40L;

	public long hp;

	public float damage = 8f;

	public float speed = 1.9f;

	public float attackRange = 1.15f;

	public float attackInterval = 1.3f;

	public int lane = 2;

	public bool elite;

	public bool dead;

	public Color tint = new Color(0.62f, 0.36f, 0.72f);

	public bool fieldMode;

	public int fieldMobId;

	public bool nativeFloating;

	public bool eventOwned;

	public long? expOverride;

	public float homeX;

	public float roamMin = float.NegativeInfinity;

	public float roamMax = float.PositiveInfinity;

	public float aggroRange = 8f;

	private int patrolDir = 1;

	private static int nextVisualSequence;

	private static readonly Stack<int> freeVisualSequences = new Stack<int>();

	private readonly int visualSequence = ((freeVisualSequences.Count > 0) ? freeVisualSequences.Pop() : (++nextVisualSequence));

	private float stunUntil;

	private float knockV;

	private float laneCooldown;

	private float dieT = -1f;

	private float dieDur;

	private float terrainY = -1.4f;

	private SpriteRenderer body;

	private SpriteAnim vis;

	private SpriteRenderer hpBar;

	private SpriteRenderer hpBg;

	private Transform hpRoot;

	private float laneVis;

	private float visHeight = 1.15f;

	private const float GroundY = -1.4f;

	public Func<bool> ScriptControlProbe;

	private float controlProbeAt;

	private Vector3 vacFrom;

	private Vector3 vacTo;

	private float vacStarted;

	private bool vacMoving;

	public bool HasBotContribution { get; private set; }

	public bool VacuumLocked { get; set; }

	public int Lane => lane;

	public Dictionary<string, object> CombatStats
	{
		get
		{
			Dictionary<string, object> dictionary = ((fieldMode && FieldCatalog.Mobs.TryGetValue(fieldMobId, out var value)) ? value.stats : NativeCombatData.Mob(artKey));
			if (!((Object)(object)NativeController != (Object)null))
			{
				return dictionary;
			}
			return NativeController.EffectiveStats(dictionary);
		}
	}

	public NativeMobController NativeController { get; private set; }

	public bool IsBoss => NativeCombatData.Int(CombatStats, "boss") != 0;

	public string MobSoundId
	{
		get
		{
			if (fieldMode && fieldMobId > 0)
			{
				return fieldMobId.ToString();
			}
			if (((Dictionary<string, object>)WzAssets.Index["mobs"]).TryGetValue(artKey, out var value) && double.TryParse(Convert.ToString(value), out var result))
			{
				return ((int)result).ToString();
			}
			return null;
		}
	}

	private float DepthOffset => (laneVis - 2f) * 0.22f;

	public int BodySortingOrder => 299 - lane * 12;

	public Nullable ProjectileBounds
	{
		get
		{
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
			//IL_0105: Unknown result type (might be due to invalid IL or missing references)
			ImgNode obj = ((vis?.FrameUri == null) ? null : WzAssets.Node(vis.FrameUri));
			ImgNode imgNode = obj?.Get("lt");
			ImgNode imgNode2 = obj?.Get("rb");
			if (imgNode == null || imgNode2 == null)
			{
				return null;
			}
			float num = (body.flipX ? (-imgNode2.X) : imgNode.X);
			float num2 = (body.flipX ? (-imgNode.X) : imgNode2.X);
			return Rect.MinMaxRect(((Component)this).transform.position.x + num / 40f, ((Component)this).transform.position.y - (float)imgNode2.Y / 40f, ((Component)this).transform.position.x + num2 / 40f, ((Component)this).transform.position.y - (float)imgNode.Y / 40f);
		}
	}

	public Nullable SelectionBounds
	{
		get
		{
			if (!fieldMode)
			{
				return null;
			}
			return NativeTargetGeometry.Bounds(this, (Object)(object)body != (Object)null && body.flipX);
		}
	}

	public bool ScriptControlled { get; set; }

	public Vector3 DamageAnchor
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return HeadPos;
		}
	}

	private Vector3 HeadPos
	{
		get
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			float num;
			if (!((Object)(object)body != (Object)null) || !((Object)(object)body.sprite != (Object)null))
			{
				num = ((Component)this).transform.position.y + visHeight;
			}
			else
			{
				Bounds bounds = ((Renderer)body).bounds;
				num = bounds.max.y;
			}
			float num2 = num;
			return new Vector3(((Component)this).transform.position.x, num2 - 0.12f, 0f);
		}
	}

	public float SkillsSealedUntil { get; private set; }

	public Vector3 VacuumPosition
	{
		get
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			if (!vacMoving)
			{
				return ((Component)this).transform.position;
			}
			return vacTo;
		}
	}

	public void BindNative(FieldDirector field)
	{
		NativeController = NativeMobController.Attach(this, field);
	}

	public void PlayNativeAction(string action)
	{
		if ((Object)(object)vis != (Object)null)
		{
			vis.PlayOnce(action);
		}
	}

	public void DefeatNative()
	{
		if (!dead)
		{
			hp = 0L;
			Die();
		}
	}

	public void ExpireNative(int action)
	{
		if (!dead)
		{
			dead = true;
			All.Remove(this);
			if ((Object)(object)hpRoot != (Object)null)
			{
				((Component)hpRoot).gameObject.SetActive(false);
			}
			NativeController?.ReleaseExpired();
			string anim = "die" + action;
			dieDur = Art.GetClip("mobs", artKey, anim)?.Duration ?? 0f;
			dieT = 0f;
			if ((Object)(object)vis != (Object)null && dieDur > 0f)
			{
				vis.PlayOnce(anim);
			}
			if (Application.isPlaying)
			{
				Object.Destroy((Object)(object)((Component)this).gameObject, dieDur + 0.05f);
			}
			else
			{
				Object.DestroyImmediate((Object)(object)((Component)this).gameObject);
			}
		}
	}

	private void OnEnable()
	{
		All.Add(this);
	}

	private void OnDisable()
	{
		All.Remove(this);
	}

	private void OnDestroy()
	{
		freeVisualSequences.Push(visualSequence);
	}

	public void Init(int laneIndex, float startX)
	{
		Init(laneIndex, startX, float.NaN);
	}

	public void Init(int laneIndex, float startX, float refY)
	{
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		lane = laneIndex;
		laneVis = laneIndex;
		Dictionary<string, object> combatStats = CombatStats;
		hpMax = Math.Max(1L, NativeCombatData.Long(combatStats, "maxHP", 40L));
		damage = NativeCombatData.Int(combatStats, "PADamage", 8);
		hp = hpMax;
		terrainY = PlayerCtrl.GroundAt(startX, float.IsNaN(refY) ? (-1.4f) : refY);
		((Component)this).transform.position = new Vector3(startX, terrainY + DepthOffset, 0f);
		BuildVisual();
	}

	private void BuildVisual()
	{
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		SpriteRenderer component = ((Component)this).gameObject.GetComponent<SpriteRenderer>();
		if ((Object)(object)component != (Object)null)
		{
			((Renderer)component).enabled = false;
		}
		vis = (Art.Has("mobs", artKey) ? Art.Attach(((Component)this).transform, "mobs", artKey, "stand", 0f) : null);
		if ((Object)(object)vis == (Object)null)
		{
			throw new InvalidDataException("Missing original monster IMG: " + artKey);
		}
		body = vis.Renderer;
		((Renderer)body).sortingOrder = BodySortingOrder;
		body.color = Color.white;
		body.spriteSortPoint = (SpriteSortPoint)1;
		Vector3 localPosition = ((Component)body).transform.localPosition;
		localPosition.z = (float)(-visualSequence) * 0.0001f;
		((Component)body).transform.localPosition = localPosition;
		Bounds bounds = ((Renderer)body).bounds;
		visHeight = Mathf.Max(0.6f, bounds.size.y);
		if ((Object)(object)hpRoot == (Object)null)
		{
			hpRoot = new GameObject("HpRoot").transform;
			hpRoot.SetParent(((Component)this).transform, false);
			hpRoot.localPosition = new Vector3(0f, visHeight + 0.15f, 0f);
			hpBg = SpriteFactory.Make("HpBg", hpRoot, new Vector2(0.9f, 0.11f), new Color(0.08f, 0.08f, 0.1f, 0.9f), 410, bottomPivot: false);
			((Component)hpBg).transform.localPosition = Vector3.zero;
			hpBar = SpriteFactory.Make("HpBar", hpRoot, new Vector2(0.86f, 0.07f), new Color(0.4f, 0.95f, 0.45f), 411, bottomPivot: false);
			((Component)hpBar).transform.localPosition = Vector3.zero;
		}
		((Component)hpRoot).gameObject.SetActive(hp < hpMax);
	}

	private void Update()
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0568: Unknown result type (might be due to invalid IL or missing references)
		//IL_0596: Unknown result type (might be due to invalid IL or missing references)
		//IL_0300: Unknown result type (might be due to invalid IL or missing references)
		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
		//IL_036c: Unknown result type (might be due to invalid IL or missing references)
		if (dead)
		{
			TickDeathFade();
			return;
		}
		float deltaTime = Time.deltaTime;
		if (VacuumLocked && vacMoving)
		{
			float num = Mathf.Clamp01((Time.time - vacStarted) / 0.13f);
			((Component)this).transform.position = Vector3.Lerp(vacFrom, vacTo, num);
			terrainY = ((Component)this).transform.position.y;
			if (num >= 1f)
			{
				vacMoving = false;
			}
		}
		else if (!VacuumLocked)
		{
			vacMoving = false;
		}
		if (deltaTime <= 0f)
		{
			SyncVisual();
			return;
		}
		if (!VacuumLocked && knockV != 0f)
		{
			Transform transform = ((Component)this).transform;
			transform.position += Vector3.right * (knockV * deltaTime);
			knockV = Mathf.MoveTowards(knockV, 0f, 26f * deltaTime);
		}
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		if ((Object)(object)playerCtrl == (Object)null)
		{
			return;
		}
		if (ScriptControlProbe != null && !ScriptControlled && Time.time >= controlProbeAt)
		{
			controlProbeAt = Time.time + 0.5f;
			ScriptControlled = ScriptControlProbe();
		}
		laneCooldown -= deltaTime;
		if (!fieldMode && laneCooldown <= 0f && Mathf.Abs(lane - playerCtrl.lane) > 1)
		{
			laneCooldown = 0.35f;
			lane += ((playerCtrl.lane > lane) ? 1 : (-1));
		}
		bool flag = Time.time < stunUntil || VacuumLocked || ScriptControlled;
		bool flag2 = false;
		int direction = 1;
		float num2 = playerCtrl.worldX - ((Component)this).transform.position.x;
		bool flag3 = true;
		if (fieldMode)
		{
			flag3 = (Object)(object)NativeController != (Object)null && NativeController.UpdatePursuit(playerCtrl);
		}
		float num3 = (((Object)(object)NativeController != (Object)null) ? NativeController.GroundMoveSpeed(flag3) : speed);
		if (fieldMode)
		{
			NativeMobController nativeController = NativeController;
			if (nativeController != null && nativeController.Flying)
			{
				flag2 = NativeController.MoveFlight(playerCtrl, deltaTime, flag || NativeController.Busy, out direction);
			}
		}
		if (!flag && !nativeFloating && ((Object)(object)NativeController == (Object)null || !NativeController.Busy))
		{
			if (fieldMode && !flag3)
			{
				flag2 = Roam(deltaTime, out direction);
			}
			else if (Mathf.Abs(num2) > attackRange)
			{
				int num4 = ((num2 > 0f) ? 1 : (-1));
				direction = num4;
				float num5 = ((Component)this).transform.position.x + (float)num4 * num3 * deltaTime;
				float num6 = PlayerCtrl.GroundAt(num5, terrainY);
				if (num6 <= terrainY + 0.6f && ((Object)(object)NativeController == (Object)null || NativeController.CanMove))
				{
					((Component)this).transform.position = new Vector3(num5, ((Component)this).transform.position.y, 0f);
					terrainY = num6;
					flag2 = true;
				}
			}
			else
			{
				direction = ((num2 >= 0f) ? 1 : (-1));
			}
		}
		if (playerCtrl.CanReceiveContact && TouchesPlayer(playerCtrl))
		{
			playerCtrl.TakeDamage(((Object)(object)NativeController != (Object)null) ? ((float)NativeCombatData.Int(CombatStats, "PADamage")) : damage, (playerCtrl.worldX >= ((Component)this).transform.position.x) ? 1 : (-1), magic: false, NativeCombatData.Int(CombatStats, "level", 1), this);
		}
		laneVis = Mathf.Lerp(laneVis, (float)lane, 1f - Mathf.Exp(-12f * deltaTime));
		SpriteAnim spriteAnim;
		string text;
		if ((Object)(object)vis != (Object)null && !ScriptControlled)
		{
			spriteAnim = vis;
			NativeMobController nativeController2 = NativeController;
			if (nativeController2 == null || !nativeController2.Flying)
			{
				if (!flag2)
				{
					text = "stand";
				}
				else
				{
					if (flag3)
					{
						NativeMobController nativeController3 = NativeController;
						if (nativeController3 != null && nativeController3.HasChase)
						{
							text = "chase";
							goto IL_0496;
						}
					}
					text = "move";
				}
			}
			else
			{
				text = "fly";
			}
			goto IL_0496;
		}
		goto IL_04d0;
		IL_04d0:
		if ((Object)(object)hpBar != (Object)null)
		{
			if (((Component)hpRoot).gameObject.activeSelf != hp < hpMax)
			{
				((Component)hpRoot).gameObject.SetActive(hp < hpMax);
			}
			float num7 = ((hpMax <= 0) ? 0f : Mathf.Clamp01((float)((double)hp / (double)hpMax)));
			float num8 = 0.86f * num7;
			((Component)hpBar).transform.localScale = new Vector3(num8, 0.07f, 1f);
			((Component)hpBar).transform.localPosition = new Vector3((0f - (0.86f - num8)) * 0.5f, 0f, 0f);
		}
		SyncVisual();
		return;
		IL_0496:
		spriteAnim.SetBase(text);
		vis.SetFacing(((Object)(object)NativeController != (Object)null && NativeController.Busy) ? NativeController.Direction : direction);
		goto IL_04d0;
	}

	private bool Roam(float dt, out int faceDir)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		faceDir = patrolDir;
		if (!(roamMax - roamMin >= 0.01f))
		{
			return false;
		}
		if ((Object)(object)NativeController != (Object)null && !NativeController.CanMove)
		{
			return false;
		}
		float num = ((Component)this).transform.position.x + (float)patrolDir * (((Object)(object)NativeController != (Object)null) ? NativeController.GroundMoveSpeed(chasing: false) : speed) * dt;
		if (num < roamMin)
		{
			num = roamMin;
			patrolDir = 1;
		}
		if (num > roamMax)
		{
			num = roamMax;
			patrolDir = -1;
		}
		float num2 = PlayerCtrl.GroundAt(num, terrainY);
		if (num2 > terrainY + 0.6f || num2 < terrainY - 1.2f)
		{
			patrolDir = -patrolDir;
			return false;
		}
		faceDir = patrolDir;
		((Component)this).transform.position = new Vector3(num, ((Component)this).transform.position.y, 0f);
		terrainY = num2;
		return true;
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

	private void SyncVisual()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		if (!nativeFloating && !VacuumLocked)
		{
			terrainY = PlayerCtrl.GroundAt(((Component)this).transform.position.x, terrainY);
			((Component)this).transform.position = new Vector3(((Component)this).transform.position.x, terrainY + DepthOffset, 0f);
		}
		if ((Object)(object)body != (Object)null)
		{
			((Renderer)body).sortingOrder = BodySortingOrder;
		}
		if ((Object)(object)body != (Object)null && (Object)(object)body.sprite != (Object)null)
		{
			Bounds bounds = ((Renderer)body).bounds;
			visHeight = Mathf.Max(0.6f, bounds.size.y);
		}
	}

	private bool TouchesPlayer(PlayerCtrl p)
	{
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		NativeBuffs component = ((Component)p).GetComponent<NativeBuffs>();
		if (component != null && component.SuppressesContact(fieldMobId, (GameLoop.I?.field?.Map?.fieldLimit).GetValueOrDefault()))
		{
			return false;
		}
		if (fieldMode && WzAssets.FileRoot("Mob/" + fieldMobId.ToString("D7") + ".img").Int("info/notAttack") != 0)
		{
			return false;
		}
		if ((Object)(object)vis == (Object)null || vis.FrameUri == null)
		{
			return false;
		}
		ImgNode imgNode = WzAssets.Node(vis.FrameUri);
		ImgNode imgNode2 = imgNode?.Get("lt");
		ImgNode imgNode3 = imgNode?.Get("rb");
		if (imgNode2 == null || imgNode3 == null)
		{
			return false;
		}
		int num = imgNode2.X;
		int num2 = imgNode3.X;
		if (body.flipX)
		{
			num = -imgNode3.X;
			num2 = -imgNode2.X;
		}
		int num3 = (num + num2) / 2;
		int num4 = (imgNode2.Y + imgNode3.Y) / 2;
		num = Math.Min(num + 25, num3 - 1);
		num2 = Math.Max(num2 - 25, num3 + 1);
		int num5 = Math.Min(imgNode2.Y + 30, num4 - 1);
		int num6 = Math.Max(imgNode3.Y - 10, num4 + 1);
		Rect val = Rect.MinMaxRect(((Component)this).transform.position.x + (float)num / 40f, ((Component)this).transform.position.y - (float)num6 / 40f, ((Component)this).transform.position.x + (float)num2 / 40f, ((Component)this).transform.position.y - (float)num5 / 40f);
		return val.Overlaps(p.ContactBounds);
	}

	public void TakeDamage(float dmg, int fromDir)
	{
		if (!dead)
		{
			TakeHit(CombatRules.Resolve(dmg, critical: false, 19999999L), fromDir);
		}
	}

	public void ScriptMoveTo(float mapX, float mapY, bool flying)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		ScriptControlled = true;
		if (flying)
		{
			nativeFloating = true;
		}
		float x = ((Component)this).transform.position.x;
		((Component)this).transform.position = new Vector3(mapX / 40f, (0f - mapY) / 40f, ((Component)this).transform.position.z);
		terrainY = (0f - mapY) / 40f;
		if ((Object)(object)vis != (Object)null)
		{
			vis.SetBase(flying ? "fly" : "move");
			vis.SetFacing((mapX >= x * 40f) ? 1 : (-1));
		}
	}

	public void TakeHit(DamageResult hit, int fromDir, bool nativeHit = false, bool magic = false)
	{
		if (!SeedTowerCompat.ProtectsTarget(this))
		{
			if ((Object)(object)NativeController != (Object)null)
			{
				hit = NativeController.FilterHit(hit, magic);
			}
			ApplyHit(hit, fromDir, nativeHit, showNumber: true);
		}
	}

	public long TakeReplayHit(DamageResult[] hits, int direction, bool magic, bool indexed, int interval, bool total = false, int ordinarySkill = -1)
	{
		if (dead)
		{
			return 0L;
		}
		if (SeedTowerCompat.ProtectsTarget(this))
		{
			return 0L;
		}
		DamageResult[] array = new DamageResult[hits.Length];
		long num = 0L;
		bool flag = false;
		for (int i = 0; i < hits.Length; i++)
		{
			array[i] = (((Object)(object)NativeController != (Object)null) ? NativeController.FilterHit(hits[i], magic) : hits[i]);
			num += array[i].Amount;
			flag |= array[i].Critical;
		}
		NativeDamageLines.Show((Component)(object)this, array, indexed, interval, total, ordinarySkill);
		ApplyHit(new DamageResult(num, flag), direction, nativeHit: true, showNumber: false);
		return num;
	}

	public void TakeReplayDot(long amount)
	{
		ApplyHit(new DamageResult(amount, critical: false), 0, nativeHit: true, showNumber: true, react: false);
	}

	public void TakeBotHit(int actorId, long damage, int fromDir)
	{
		if (!dead)
		{
			HasBotContribution = true;
			ApplyHit(new DamageResult(damage, critical: false), fromDir, nativeHit: true, showNumber: false, react: true, actorId);
		}
	}

	private void ApplyHit(DamageResult hit, int fromDir, bool nativeHit, bool showNumber, bool react = true, int attackerId = 0)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
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
		long amount = hit.Amount;
		if (fieldMode && (attackerId != 0 || GameSession.InGame))
		{
			int key = ((attackerId != 0) ? attackerId : GameSession.CharacterId);
			long num = Math.Min(((Object)(object)NativeController != (Object)null) ? NativeController.CurrentHp : hp, hit.Amount);
			DamageContributions.TryGetValue(key, out var value);
			DamageContributions[key] = value + num;
		}
		if ((Object)(object)NativeController != (Object)null)
		{
			NativeController.ApplyDamage(hit.Amount);
		}
		else
		{
			hp = Math.Max(0L, hp - amount);
		}
		if (attackerId == 0)
		{
			NativeController?.OnPlayerDamage();
		}
		if (react)
		{
			int num2 = NativeCombatData.Int(CombatStats, "pushed", 1);
			knockV = (VacuumLocked ? 0f : ((float)fromDir * HitFeel.Knockback * 6.1f * Mathf.Clamp01(1f / Mathf.Max(1f, (float)num2))));
			if (!fieldMode)
			{
				Fx.Flash(body, Color.white);
			}
			if ((Object)(object)vis != (Object)null)
			{
				vis.PlayOnce("hit");
			}
			if (!fieldMode && !nativeHit)
			{
				Fx.Burst(((Component)this).transform.position + Vector3.up * 0.7f, critical ? 0.6f : 0.45f, new Color(1f, 0.9f, 0.5f, 0.9f));
			}
			string mobSoundId = MobSoundId;
			if (mobSoundId != null)
			{
				AudioManager.PlayAt("Mob/" + mobSoundId + "/Damage", ((Component)this).transform.position, 0.9f, 9f);
			}
		}
		if (showNumber)
		{
			DamageText.Show(DamageAnchor + Vector3.up * 0.375f, amount, critical ? "cri" : "red");
		}
		if (!fieldMode & critical)
		{
			HitFeel.Punch(1.35f);
		}
		if (hp <= 0)
		{
			Die();
		}
	}

	public void SealSkills(float seconds)
	{
		SkillsSealedUntil = Time.unscaledTime + Mathf.Max(0f, seconds);
	}

	public void MoveVac(Vector3 position)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		knockV = 0f;
		vacFrom = ((Component)this).transform.position;
		vacTo = position;
		vacStarted = Time.time;
		vacMoving = true;
	}

	public void PullTowards(float x)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		if (!IsBoss)
		{
			Vector3 position = ((Component)this).transform.position;
			position.x = PlayerCtrl.ClampX(x + ((position.x >= x) ? 1f : (-1f)));
			position.y = PlayerCtrl.GroundAt(position.x, position.y);
			terrainY = position.y;
			((Component)this).transform.position = position;
		}
	}

	public void Push(int direction, NativeSkillDefinition skill)
	{
		knockV = (float)direction * Mathf.Max(0f, skill.Max.x - skill.Min.x);
	}

	public void Stun(float seconds)
	{
		if (!IsBoss)
		{
			stunUntil = Mathf.Max(stunUntil, Time.time + seconds);
		}
	}

	private void Die()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		dead = true;
		All.Remove(this);
		if ((Object)(object)hpRoot != (Object)null)
		{
			((Component)hpRoot).gameObject.SetActive(false);
		}
		Fx.Burst(((Component)this).transform.position + Vector3.up * 0.6f, 1.1f, new Color(1f, 0.8f, 0.35f, 0.95f), 0.3f);
		if ((Object)(object)GameLoop.I != (Object)null)
		{
			GameLoop.I.OnEnemyKilled(this);
		}
		dieDur = DeathSeconds();
		dieT = 0f;
		if ((Object)(object)vis != (Object)null)
		{
			vis.PlayOnce(((Object)(object)NativeController != (Object)null) ? ("die" + NativeController.DeathAction) : "die");
		}
		string mobSoundId = MobSoundId;
		if (mobSoundId != null)
		{
			AudioManager.PlayAt("Mob/" + mobSoundId + "/Die", ((Component)this).transform.position, 1f, 11f);
		}
		Object.Destroy((Object)(object)((Component)this).gameObject, dieDur + 0.05f);
	}

	public static float DeathSeconds(EnemyUnit e)
	{
		return Art.GetClip("mobs", e.artKey, ((Object)(object)e.NativeController != (Object)null) ? ("die" + e.NativeController.DeathAction) : Art.Resolve("mobs", e.artKey, "die", "die1"))?.Duration ?? 0.45f;
	}

	private float DeathSeconds()
	{
		return DeathSeconds(this);
	}

	public EnemyUnit()
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
	}
}
