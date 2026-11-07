using System;
using System.Collections.Generic;
using System.IO;
using MapleDash.Wz;
using Server.Bot;
using Server.Field;
using UnityEngine;

public class PlayerCtrl : MonoBehaviour
{
	private struct PendingHit
	{
		public EnemyUnit enemy;

		public BossUnit boss;

		public MapReactor reactor;

		public DamageResult hit;

		public float timer;

		public int facing;

		public bool active;
	}

	public const float GroundY = -1.4f;

	public const int Lanes = 5;

	public const float LaneStep = 0.22f;

	public const float BaseInterval = 0.55f;

	public const float BaseReach = 2.3f;

	private WeaponDef weapon;

	private CharacterAttributes baseAttributes;

	private int weaponAttack;

	private int magicAttack;

	private float rushRemaining;

	private float rushSpeed;

	private int rushFacing;

	public static float XMin = -13f;

	public static float XMax = 13f;

	public static List<Vector4> Terrain;

	public static List<bool> TerrainNoFallThrough;

	public static List<int> TerrainForces;

	public static List<int> TerrainDrag;

	public static List<Art.ClimbLine> Climbs;

	private Art.ClimbLine climb;

	private float climbInput;

	private float grabCooldown;

	public static float FallFloor = float.NegativeInfinity;

	public static int FallRecoveries;

	public static bool InputLocked;

	public static int ScriptUiLock;

	public const float BaseWalkPx = 125f;

	public const float DownJumpRatio = 0.35355338f;

	public const int FieldLimitCannotJumpDown = 131072;

	private int dropSegment = -1;

	public const float MeleeVertical = 1.2f;

	[Header("移动")]
	public float moveSpeed = 3.125f;

	public float jumpSpeed = 13.875f;

	public float gravity = -50f;

	private NativeMovementPhysics movementPhysics;

	private double horizontalVelocity;

	private bool flashJumpUsed;

	private double soulRushUntil;

	private FieldMap teleportMap;

	private MapRuntime teleportGeometry;

	private float contactWallX;

	private int contactWallDirection;

	[Header("普攻")]
	public float attackInterval = 0.55f;

	public float reach = 2.3f;

	public float attackDamage = 11f;

	[Header("状态")]
	public float hpMax = 100f;

	public float hp = 100f;

	public float invulnUntil;

	public long lastMobAttackMs;

	private float alertUntil;

	private float contactTintStarted;

	private int contactTintPhase;

	private bool contactTintActive;

	public float mpMax = 100f;

	public float mp = 100f;

	private float attackBonus;

	private float attackBonusUntil;

	public int lane = 2;

	public int facing = 1;

	public bool grounded = true;

	[HideInInspector]
	public float worldX;

	[HideInInspector]
	public float jumpY;

	[HideInInspector]
	public float terrainY = -1.4f;

	private float laneVis;

	private float vy;

	private float fallSpeed = 16.75f;

	private float fallPeakY;

	private float fallStartY;

	private bool fallTracking;

	private float atkTimer;

	private bool attackRequested;

	private float laneCooldown;

	private SpriteRenderer sr;

	private SpriteAnim vis;

	private bool moving;

	private PendingHit pending;

	private string trailName;

	private string trailPose;

	private AfterimageFx.Frame[] trailFrames;

	private int trailCursor;

	private float swingPlayback = 1f;

	private float swingSfxAt = -1f;

	private SpriteRenderer coatSr;

	private Sprite dyedFrame;

	public const float RegenInterval = 10f;

	private bool sitting;

	private NativeBotChair chairEffect;

	private string swingAction = "atk";

	private int visibleMount;

	private int partyGrindDefense;

	private string beforeMorphKey;

	private int visibleMorph;

	public bool UsesRangedWeapon
	{
		get
		{
			if (weapon.kind != WeaponKind.bow && weapon.kind != WeaponKind.crossbow && weapon.kind != WeaponKind.gun)
			{
				return weapon.kind == WeaponKind.claw;
			}
			return true;
		}
	}

	public WeaponKind WeaponType => weapon.kind;

	public CharacterAttributes Attributes { get; private set; }

	public int WeaponAttack => weaponAttack;

	public int MagicAttack => magicAttack;

	public bool IsRushing => rushRemaining > 0f;

	public bool IsClimbing => climb != null;

	public bool IsDroppingThrough => dropSegment >= 0;

	public bool CanFlashJump
	{
		get
		{
			if (!flashJumpUsed && !grounded && !IsClimbing && vy >= 0f && ((Object)(object)vis == (Object)null || !vis.IsOneShot))
			{
				GameLoop i = GameLoop.I;
				return ((i == null) ? ((int?)null) : (i.field?.Map?.fieldLimit & 2)).GetValueOrDefault() == 0;
			}
			return false;
		}
	}

	public float SkillActionDuration { get; private set; } = 0.55f;

	public int MeleeSkillImpactMs { get; private set; }

	public float DepthOffset
	{
		get
		{
			if (Terrain == null)
			{
				return (laneVis - 2f) * 0.22f;
			}
			return 0f;
		}
	}

	public int SortingOrder => 300 - lane * 12;

	public Vector2 FeetPos
	{
		get
		{
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			return new Vector2(worldX, terrainY + jumpY + DepthOffset);
		}
	}

	public Rect ContactBounds
	{
		get
		{
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			float num = ((!Crouching) ? (-22) : ((facing <= 0) ? (-46) : 0));
			float num2 = ((!Crouching) ? 22 : ((facing > 0) ? 46 : 0));
			return Rect.MinMaxRect(FeetPos.x + num / 40f, FeetPos.y, FeetPos.x + num2 / 40f, FeetPos.y + (float)(Crouching ? 31 : 65) / 40f);
		}
	}

	public float BodyHeight
	{
		get
		{
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			float num = 1.5f;
			if ((Object)(object)sr != (Object)null && ((Renderer)sr).enabled && (Object)(object)sr.sprite != (Object)null)
			{
				Bounds bounds = ((Renderer)sr).bounds;
				num = bounds.size.y;
			}
			return Mathf.Max(0.8f, num);
		}
	}

	public float BodyHalfWidth
	{
		get
		{
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			float num = 0.41f;
			if ((Object)(object)sr != (Object)null && ((Renderer)sr).enabled && (Object)(object)sr.sprite != (Object)null)
			{
				Bounds bounds = ((Renderer)sr).bounds;
				num = bounds.size.x * 0.5f;
			}
			return Mathf.Max(0.22f, num);
		}
	}

	public float RegenTimer { get; private set; }

	public int ChairId { get; private set; }

	public bool Sitting
	{
		get
		{
			return sitting;
		}
		private set
		{
			sitting = value;
			if (!value)
			{
				ChairId = 0;
				if ((Object)(object)chairEffect != (Object)null)
				{
					chairEffect.Set(0, vis);
				}
			}
		}
	}

	public bool Crouching { get; private set; }

	private bool NativeBasicAttackActive
	{
		get
		{
			if (Terrain != null && (Object)(object)vis != (Object)null && vis.IsOneShot)
			{
				if (!(vis.Current == "atk"))
				{
					return vis.Current == "proneStab";
				}
				return true;
			}
			return false;
		}
	}

	public bool CanReceiveContact
	{
		get
		{
			if (hp > 0f)
			{
				return Time.unscaledTime >= invulnUntil;
			}
			return false;
		}
	}

	public Vector3 DamageAnchor
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			float x = FeetPos.x;
			float y = FeetPos.y;
			float num;
			if (!((Object)(object)sr != (Object)null) || !((Object)(object)sr.sprite != (Object)null))
			{
				num = BodyHeight;
			}
			else
			{
				Bounds bounds = ((Renderer)sr).bounds;
				num = bounds.size.y;
			}
			return new Vector3(x, y + num, 0f);
		}
	}

	public float EffectiveAtk
	{
		get
		{
			RefreshAttributes();
			bool flag = weapon.kind == WeaponKind.bow || weapon.kind == WeaponKind.crossbow || weapon.kind == WeaponKind.gun;
			if (weapon.kind == WeaponKind.claw)
			{
				return CombatRules.MaxPhysicalDamage(Attributes.luk, Attributes.dex + Attributes.str, weaponAttack, weapon.multiplier);
			}
			return flag ? CombatRules.MaxPhysicalDamage(Attributes.dex, Attributes.str, weaponAttack, weapon.multiplier) : CombatRules.MaxPhysicalDamage(Attributes.str, Attributes.dex, weaponAttack, weapon.multiplier);
		}
	}

	public static void SetBounds(float min, float max)
	{
		XMin = min;
		XMax = Mathf.Max(min + 0.5f, max);
	}

	public static float ClampX(float x)
	{
		return Mathf.Clamp(x, XMin, XMax);
	}

	public static void SetTerrain(List<Vector4> t, List<bool> forbidFallDown = null, List<int> forces = null, List<int> drag = null)
	{
		Terrain = t;
		TerrainDrag = ((t != null && drag != null && drag.Count == t.Count) ? drag : null);
		TerrainForces = ((t != null && forces != null && forces.Count == t.Count) ? forces : null);
		TerrainNoFallThrough = ((t != null && forbidFallDown != null && forbidFallDown.Count == t.Count) ? forbidFallDown : null);
	}

	public static float GroundAt(float x, float refY)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		if (Terrain == null || Terrain.Count == 0)
		{
			return -1.4f;
		}
		float result = refY;
		float num = float.MaxValue;
		bool flag = false;
		for (int i = 0; i < Terrain.Count; i++)
		{
			Vector4 val = Terrain[i];
			float x2 = val.x;
			float y = val.y;
			float z = val.z;
			float w = val.w;
			if (Mathf.Abs(z - x2) < 0.01f)
			{
				continue;
			}
			float num2 = Mathf.Min(x2, z);
			float num3 = Mathf.Max(x2, z);
			if (!(x < num2) && !(x > num3))
			{
				float num4 = Mathf.Lerp(y, w, (x - x2) / (z - x2));
				float num5 = Mathf.Abs(num4 - refY);
				if (num5 < num)
				{
					num = num5;
					result = num4;
					flag = true;
				}
			}
		}
		if (flag)
		{
			return result;
		}
		float atX;
		return NearestGround(x, out atX);
	}

	private static float NearestGround(float x, out float atX)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		atX = x;
		if (Terrain == null || Terrain.Count == 0)
		{
			return -1.4f;
		}
		float num = float.MaxValue;
		float num2 = -1.4f;
		for (int i = 0; i < Terrain.Count; i++)
		{
			Vector4 val = Terrain[i];
			if (!(Mathf.Abs(val.z - val.x) < 0.01f))
			{
				float num3 = Mathf.Min(val.x, val.z);
				float num4 = Mathf.Max(val.x, val.z);
				float num5 = Mathf.Clamp(x, num3, num4);
				float num6 = Mathf.Abs(num5 - x);
				float num7 = Mathf.Lerp(val.y, val.w, (num5 - val.x) / (val.z - val.x));
				if (num6 < num - 0.0001f || (Mathf.Abs(num6 - num) <= 0.0001f && num7 < num2))
				{
					num = num6;
					num2 = num7;
					atX = num5;
				}
			}
		}
		return num2;
	}

	public static bool FollowGround(float fromX, float curY, float toX, out float outY)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0308: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		//IL_0328: Unknown result type (might be due to invalid IL or missing references)
		//IL_032f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		outY = curY;
		if (Terrain == null || Terrain.Count == 0)
		{
			outY = -1.4f;
			return true;
		}
		int num = -1;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = float.MaxValue;
		for (int i = 0; i < Terrain.Count; i++)
		{
			Vector4 val = Terrain[i];
			if (Mathf.Abs(val.z - val.x) < 0.01f)
			{
				continue;
			}
			float num5 = Mathf.Min(val.x, val.z);
			float num6 = Mathf.Max(val.x, val.z);
			if (!(fromX < num5) && !(fromX > num6))
			{
				float num7 = Mathf.Abs(Mathf.Lerp(val.y, val.w, (fromX - val.x) / (val.z - val.x)) - curY);
				if (num7 < num4)
				{
					num4 = num7;
					num = i;
					num2 = num5;
					num3 = num6;
				}
			}
		}
		if (num < 0 || num4 > 0.0375f)
		{
			return false;
		}
		Vector4 val2 = Terrain[num];
		if (toX >= num2 && toX <= num3)
		{
			outY = Mathf.Lerp(val2.y, val2.w, (toX - val2.x) / (val2.z - val2.x));
			return true;
		}
		float num8 = ((toX > num3) ? num3 : num2);
		int num9 = -1;
		float num10 = float.MaxValue;
		for (int j = 0; j < Terrain.Count; j++)
		{
			if (j == num)
			{
				continue;
			}
			Vector4 val3 = Terrain[j];
			if (Mathf.Abs(val3.z - val3.x) < 0.01f)
			{
				continue;
			}
			float num11 = Mathf.Min(val3.x, val3.z);
			float num12 = Mathf.Max(val3.x, val3.z);
			bool num13 = Mathf.Abs(num11 - num8) <= 0.0375f || Mathf.Abs(num12 - num8) <= 0.0375f;
			float num14 = Mathf.Lerp(val2.y, val2.w, (num8 - val2.x) / (val2.z - val2.x));
			float num15 = Mathf.Lerp(val3.y, val3.w, (num8 - val3.x) / (val3.z - val3.x));
			if (num13 && !(Mathf.Abs(num14 - num15) > 0.0375f) && !(toX < num11) && !(toX > num12))
			{
				float num16 = Mathf.Abs(Mathf.Lerp(val3.y, val3.w, (Mathf.Clamp(toX, num11, num12) - val3.x) / (val3.z - val3.x)) - curY);
				if (num16 < num10)
				{
					num10 = num16;
					num9 = j;
				}
			}
		}
		if (num9 < 0)
		{
			return false;
		}
		Vector4 val4 = Terrain[num9];
		float num17 = Mathf.Min(val4.x, val4.z);
		float num18 = Mathf.Max(val4.x, val4.z);
		outY = Mathf.Lerp(val4.y, val4.w, (Mathf.Clamp(toX, num17, num18) - val4.x) / (val4.z - val4.x));
		return true;
	}

	public bool CanUseFlashJump(int skill)
	{
		if (skill != 11101005)
		{
			return CanFlashJump;
		}
		if (!flashJumpUsed && grounded && !IsClimbing && Time.realtimeSinceStartupAsDouble >= soulRushUntil)
		{
			NativeMobDebuffs component = ((Component)this).GetComponent<NativeMobDebuffs>();
			if ((component == null || !component.Has(122)) && ((Object)(object)vis == (Object)null || !vis.IsOneShot))
			{
				GameLoop i = GameLoop.I;
				return ((i == null) ? ((int?)null) : (i.field?.Map?.fieldLimit & 2)).GetValueOrDefault() == 0;
			}
		}
		return false;
	}

	public void ApplyFlashJump(int level, int skill = 4111006)
	{
		int num = ((skill == 11101005) ? (level * 2) : level) / 4;
		if (skill == 11101005)
		{
			grounded = false;
			soulRushUntil = Time.realtimeSinceStartupAsDouble + 1.5;
		}
		horizontalVelocity = ContactImpulse(horizontalVelocity, facing * (350 + 40 * num));
		vy = (0f - (float)ContactImpulse((0f - vy) * 40f, -250 - 20 * num)) / 40f;
		flashJumpUsed = true;
		vis?.Play("jump", once: false);
	}

	public bool TeleportDestination(NativeSkillDefinition skill, out Vector2 destination)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		destination = default;
		if (!grounded || IsClimbing || ((Object)(object)vis != (Object)null && vis.IsOneShot))
		{
			return false;
		}
		FieldMap fieldMap = GameLoop.I?.field?.Map;
		if (fieldMap == null)
		{
			return false;
		}
		if (teleportMap != fieldMap)
		{
			teleportMap = fieldMap;
			teleportGeometry = new MapRuntime
			{
				mapId = fieldMap.id,
				fieldLimit = fieldMap.fieldLimit,
				left = fieldMap.vr[0],
				right = fieldMap.vr[2],
				top = fieldMap.vr[1],
				bottom = fieldMap.vr[3]
			};
			foreach (FieldFoothold foothold in fieldMap.footholds)
			{
				teleportGeometry.AddFoothold(foothold.x1, foothold.y1, foothold.x2, foothold.y2);
			}
		}
		float horizontal = TouchInput.MoveH;
		float vertical = TouchInput.MoveV;
		bool jump = false;
		((Component)this).GetComponent<NativeMobDebuffs>()?.Input(ref horizontal, ref jump, ref vertical);
		int range = Art.Int(SkillBook.Get(skill.Id)?.Level(skill.Level), "range");
		if (!NativeTeleportMovement.Destination(teleportGeometry, Mathf.RoundToInt(FeetPos.x * 40f), Mathf.RoundToInt((0f - FeetPos.y) * 40f), (Mathf.Abs(horizontal) > 0.15f) ? ((horizontal > 0f) ? 1 : (-1)) : 0, (Mathf.Abs(vertical) > 0.15f) ? ((!(vertical > 0f)) ? 1 : (-1)) : 0, range, out var targetX, out var targetY))
		{
			return false;
		}
		destination = new Vector2(fieldMap.WorldX(targetX), fieldMap.WorldY(targetY));
		return true;
	}

	public void BeginNativeAlert()
	{
		alertUntil = Time.unscaledTime + 5f;
	}

	public void ResetTerrain()
	{
		CancelRush();
		climb = null;
		dropSegment = -1;
		grabCooldown = 0f;
		terrainY = GroundAt(worldX, -1.4f);
		jumpY = 0f;
		vy = 0f;
		horizontalVelocity = 0.0;
		contactWallDirection = 0;
		grounded = true;
		flashJumpUsed = false;
		fallTracking = false;
	}

	public void PlaySkill(string skill, float phaseDuration = 0f)
	{
		if ((Object)(object)vis == (Object)null || IsClimbing)
		{
			return;
		}
		BeginNativeAlert();
		string text = "skill_" + skill;
		if (Art.GetClip(vis.kind, vis.key, text) == null)
		{
			text = "atk";
		}
		MeleeSkillImpactMs = 0;
		if ((skill == "1001004" || skill.StartsWith("1001004@") || skill == "1001005" || skill.StartsWith("1001005@")) && text == "atk" && phaseDuration <= 0f)
		{
			int num = WzCharacter.WeaponAttackSpeed(vis.key);
			num = ((Component)this).GetComponent<NativeBuffs>()?.AttackSpeed(num) ?? num;
			Art.Clip clip = Art.GetClip(vis.kind, vis.key, text);
			int num2 = 0;
			if (clip != null)
			{
				float[] delays = clip.delays;
				foreach (float delay in delays)
				{
					num2 += Mathf.RoundToInt(WzCharacter.AttackFrameDelay(delay, num) * 1000f);
				}
			}
			SkillActionDuration = (float)num2 / 1000f;
			MeleeSkillImpactMs = NativeMeleeImpactMs(clip?.delays, num, 0);
			vis.PlayNativeAttack(text, num);
		}
		else
		{
			SkillActionDuration = GetSkillActionDuration(skill);
			float speedMul = ((phaseDuration > 0f) ? (SkillActionDuration / phaseDuration) : 1f);
			if (phaseDuration > 0f)
			{
				SkillActionDuration = phaseDuration;
			}
			vis.PlayOnce(text, speedMul);
		}
	}

	public void FinishHeldSkill()
	{
		vis?.Play("stand", once: false, force: true);
	}

	public void PlayNativeSkill(int id)
	{
		PlaySkill(id.ToString());
	}

	public static int NativeMeleeImpactMs(float[] delays, int weaponSpeed, int distancePixels)
	{
		if (delays == null || delays.Length == 0)
		{
			return 0;
		}
		int num = 0;
		int num2 = 0;
		foreach (float num3 in delays)
		{
			num += (int)Math.Round(num3 * 1000f);
			num2 += (int)Math.Round(WzCharacter.AttackFrameDelay(num3, weaponSpeed) * 1000f);
		}
		int num4 = (int)Math.Round(delays[^1] * 1000f);
		return ((num > 0) ? (num2 * (num - num4) / num) : 0) + (int)Math.Round(Math.Abs((double)distancePixels) * 1.5);
	}

	public float GetSkillActionDuration(string skill)
	{
		if ((Object)(object)vis == (Object)null)
		{
			return attackInterval;
		}
		Art.Clip clip = Art.GetClip(vis.kind, vis.key, "skill_" + skill) ?? Art.GetClip(vis.kind, vis.key, "atk");
		float num = 0f;
		if (clip != null)
		{
			float[] delays = clip.delays;
			foreach (float num2 in delays)
			{
				num += num2;
			}
		}
		if (!(num > 0f))
		{
			return attackInterval;
		}
		return num;
	}

	private void Awake()
	{
		Profile.Load();
		if ((Object)(object)((Component)this).GetComponent<NativeEquipmentEffects>() == (Object)null)
		{
			((Component)this).gameObject.AddComponent<NativeEquipmentEffects>();
		}
		if ((Object)(object)((Component)this).GetComponent<NativeRingEffect>() == (Object)null)
		{
			((Component)this).gameObject.AddComponent<NativeRingEffect>();
		}
		Profile.LeveledUp += OnLevelUp;
		movementPhysics = NativeMovementPhysics.Load();
		ImgNode imgNode = WzAssets.FileRoot("Map/Physics.img");
		moveSpeed = (float)imgNode.Int("walkSpeed", 125) / 40f;
		jumpSpeed = (float)imgNode.Int("jumpSpeed", 555) / 40f;
		gravity = (float)(-imgNode.Int("gravityAcc", 2000)) / 40f;
		fallSpeed = (float)imgNode.Int("fallSpeed", 670) / 40f;
		laneVis = lane;
		sr = ((Component)this).GetComponent<SpriteRenderer>();
		if ((Object)(object)sr != (Object)null)
		{
			((Renderer)sr).enabled = false;
		}
		BuildArt();
		ApplyLoadout();
		SyncTransform();
	}

	private void OnLevelUp(int level)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		ApplyProgression();
		NativeImgAnimation.Spawn("Effect/BasicEff.img/LevelUp", ((Component)this).transform, Vector2.op_Implicit(FeetPos), SortingOrder + 1);
	}

	public void ApplyProgression()
	{
		baseAttributes = Profile.Attributes;
		RefreshAttributes();
		hpMax = Attributes.maxHp;
		mpMax = Attributes.maxMp;
		hp = hpMax;
		mp = mpMax;
		attackDamage = EffectiveAtk;
	}

	public void RefreshProgressionCaps()
	{
		baseAttributes = Profile.Attributes;
		RefreshAttributes();
		hpMax = Attributes.maxHp;
		mpMax = Attributes.maxMp;
		hp = Mathf.Clamp(hp, 0f, hpMax);
		mp = Mathf.Clamp(mp, 0f, mpMax);
		attackDamage = EffectiveAtk;
	}

	private void OnDestroy()
	{
		Profile.LeveledUp -= OnLevelUp;
	}

	public bool TrySpendMp(int amount)
	{
		if (amount <= 0)
		{
			return true;
		}
		if (mp < (float)amount)
		{
			return false;
		}
		float previousMp = mp;
		mp -= amount;
		NativePetAutopot.OnLoss(this, hp, previousMp);
		return true;
	}

	public void TickRegen(float dt)
	{
		if (dt <= 0f || hp <= 0f)
		{
			return;
		}
		RegenTimer += dt;
		if (!(RegenTimer < 10f))
		{
			RegenTimer -= 10f;
			if (RegenTimer < 0f)
			{
				RegenTimer = 0f;
			}
			int num = Mathf.Max(3, Mathf.CeilToInt(mpMax * 0.01f));
			mp = Mathf.Min(mpMax, mp + (float)num);
			if (grounded && !moving && climb == null)
			{
				hp = Mathf.Min(hpMax, hp + 10f);
			}
		}
	}

	public bool UseChair(int itemId)
	{
		if (!grounded || IsClimbing || hp <= 0f || (Object)(object)vis == (Object)null)
		{
			return false;
		}
		Sitting = true;
		ChairId = itemId;
		vis.SetBase("sit");
		if ((Object)(object)chairEffect == (Object)null)
		{
			chairEffect = ((Component)this).gameObject.AddComponent<NativeBotChair>();
		}
		chairEffect.Set(itemId, vis);
		return true;
	}

	public void ToggleSit()
	{
		if (grounded && !IsClimbing && !(hp <= 0f))
		{
			Sitting = !Sitting;
			if ((Object)(object)vis != (Object)null)
			{
				vis.SetBase(Sitting ? "sit" : "stand");
			}
		}
	}

	public void Express(int emote)
	{
		if ((Object)(object)vis == (Object)null || hp <= 0f)
		{
			return;
		}
		switch (emote)
		{
		case 0:
			vis.ClearExpression();
			return;
		default:
			return;
		case 8:
		case 9:
		case 10:
		case 11:
		case 12:
		case 13:
		case 14:
		case 15:
		case 16:
		case 17:
		case 18:
		case 19:
		case 20:
		case 21:
		case 22:
			if (!OwnsFaceExpression(emote))
			{
				return;
			}
			break;
		case 1:
		case 2:
		case 3:
		case 4:
		case 5:
		case 6:
		case 7:
			break;
		}
		vis.Express(emote);
	}

	private static bool OwnsFaceExpression(int emote)
	{
		List<InventoryEntry> list = ((GameSession.Character != null) ? GameSession.Character.inventory : null);
		if (list == null)
		{
			return false;
		}
		int num = 5159992 + emote;
		foreach (InventoryEntry item in list)
		{
			if (item != null && item.itemId == num)
			{
				return true;
			}
		}
		return false;
	}

	private void BuildArt()
	{
		string text = WzCharacter.CurrentAppearanceKey() ?? (Profile.cls + "_" + HairStyles.KeyOf(HairStyles.IndexOf(Profile.hair)));
		visibleMount = NativeMount.Item(this);
		if (visibleMount > 0)
		{
			text = WzCharacter.MountedAppearance(text, visibleMount, NativeMount.Saddle);
		}
		if (Art.Has("chars", text))
		{
			vis = Art.Attach(((Component)this).transform, "chars", text, "stand", 0f);
			if ((Object)(object)vis != (Object)null)
			{
				SpriteRenderer renderer = vis.Renderer;
				((Renderer)renderer).sortingOrder = 300;
				sr = renderer;
				BuildCosmetics();
				return;
			}
		}
		throw new InvalidDataException("Missing original character IMG recipe: " + text);
	}

	public void RefreshFieldAppearance()
	{
		if (!((Object)(object)vis == (Object)null))
		{
			((Component)vis).gameObject.SetActive(false);
			if (Application.isPlaying)
			{
				Object.Destroy((Object)(object)((Component)vis).gameObject);
			}
			else
			{
				Object.DestroyImmediate((Object)(object)((Component)vis).gameObject);
			}
			vis = null;
			visibleMorph = 0;
			beforeMorphKey = null;
			BuildArt();
		}
	}

	private void ApplyLoadout()
	{
		weapon = WeaponDefs.Get(Profile.equipWeapon);
		baseAttributes = Profile.Attributes;
		RefreshAttributes();
		hpMax = Attributes.maxHp;
		mpMax = Attributes.maxMp;
		hp = hpMax;
		mp = mpMax;
		CancelRush();
		attackDamage = EffectiveAtk;
		attackInterval = 0.55f;
		reach = 2.3f;
		BuildCosmetics();
	}

	private void BuildCosmetics()
	{
		if ((Object)(object)coatSr != (Object)null)
		{
			((Renderer)coatSr).enabled = false;
		}
	}

	private void ApplyDye()
	{
		if ((Object)(object)coatSr != (Object)null)
		{
			((Renderer)coatSr).enabled = false;
		}
	}

	private void Update()
	{
		using (BotPerf.Timer("Player.Update"))
		{
			if ((Object)(object)GameLoop.I != (Object)null && (GameLoop.I.state != GameLoop.GameState.Playing || GameLoop.I.gameOver))
			{
				return;
			}
			if (InputLocked || ScriptUiLock > 0)
			{
				SyncTransform();
				return;
			}
			if (visibleMount != NativeMount.Item(this))
			{
				RefreshFieldAppearance();
			}
			RefreshMovementAttributes();
			float deltaTime = Time.deltaTime;
			if (deltaTime > 0f)
			{
				Step(deltaTime);
			}
			TickRegen(Time.unscaledDeltaTime);
			SyncTransform();
		}
	}

	private void Step(float dt)
	{
		//IL_03ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0451: Unknown result type (might be due to invalid IL or missing references)
		float horizontal = TouchInput.MoveH;
		bool jump = TouchInput.TakeJump();
		FieldMap fieldMap = (((Object)(object)GameLoop.I != (Object)null && (Object)(object)GameLoop.I.field != (Object)null) ? GameLoop.I.field.Map : null);
		if (fieldMap != null && (fieldMap.fieldLimit & 1) != 0)
		{
			jump = false;
		}
		climbInput = TouchInput.MoveV;
		NativeMobDebuffs component = ((Component)this).GetComponent<NativeMobDebuffs>();
		component?.Input(ref horizontal, ref jump, ref climbInput);
		if (NativeBasicAttackActive)
		{
			jump = false;
			if (grounded)
			{
				horizontal = 0f;
			}
			climbInput = ((vis.Current == "proneStab") ? (-1) : 0);
		}
		if (jump || Mathf.Abs(horizontal) > 0.15f || Mathf.Abs(climbInput) > 0.15f || !grounded)
		{
			Sitting = false;
		}
		int num = Mathf.Max(1, Mathf.CeilToInt(dt / 0.008f));
		for (int i = 0; i < num; i++)
		{
			StepMovement(horizontal, jump && i == 0, dt / (float)num);
		}
		laneCooldown -= Time.unscaledDeltaTime;
		int num2 = 0;
		if (InputHub.Pressed(GameAction.Up))
		{
			num2 = -1;
		}
		if (InputHub.Pressed(GameAction.Down))
		{
			num2 = 1;
		}
		float moveV = TouchInput.MoveV;
		if (num2 == 0 && Mathf.Abs(moveV) > 0.6f && laneCooldown <= 0f)
		{
			num2 = ((!(moveV > 0f)) ? 1 : (-1));
			laneCooldown = 0.22f;
		}
		if (num2 == 0)
		{
			num2 = TouchInput.TakeLaneStep();
		}
		if (Terrain != null)
		{
			lane = 2;
			num2 = 0;
		}
		if (num2 != 0)
		{
			lane = Mathf.Clamp(lane + num2, 0, 4);
		}
		laneVis = Mathf.Lerp(laneVis, (float)lane, 1f - Mathf.Exp(-14f * dt));
		Crouching = Terrain != null && grounded && !IsClimbing && !moving && climbInput < -0.15f;
		if ((Object)(object)vis != (Object)null)
		{
			string text;
			if (IsClimbing)
			{
				text = (climb.ladder ? "ladder" : "rope");
			}
			else if (grounded)
			{
				if (moving)
				{
					text = "walk";
				}
				else if (Crouching)
				{
					text = "prone";
				}
				else if (Sitting)
				{
					text = "sit";
				}
				else
				{
					text = ((Time.unscaledTime < alertUntil) ? "alert" : "stand");
				}
			}
			else
			{
				text = "jump";
			}
			vis.SetBase(text);
			if (IsClimbing)
			{
				vis.Play(text, once: false);
			}
			if (IsClimbing)
			{
				vis.speed = Mathf.Abs(climbInput);
			}
			else if (!vis.IsOneShot)
			{
				vis.speed = 1f;
			}
		}
		atkTimer -= dt;
		if (visibleMount == 0)
		{
			NativeBuffs component2 = ((Component)this).GetComponent<NativeBuffs>();
			if ((component2 == null || component2.Value("MORPH") <= 0 || ((Component)this).GetComponent<NativeBuffs>().Value("MORPH") >= 100) && ((Object)(object)component == (Object)null || !component.BlocksAttack) && (Terrain == null || attackRequested) && !IsClimbing && atkTimer <= 0f && ((Object)(object)vis == (Object)null || !vis.IsOneShot))
			{
				BossUnit bossUnit = FindBossTarget();
				if (attackRequested && StrikeReactor())
				{
					atkTimer = attackInterval;
				}
				else if ((Object)(object)bossUnit != (Object)null)
				{
					atkTimer = attackInterval;
					if (Terrain == null)
					{
						facing = ((((Component)bossUnit).transform.position.x >= worldX) ? 1 : (-1));
					}
					StrikeBoss(bossUnit);
				}
				else
				{
					EnemyUnit enemyUnit = FindTarget();
					if ((Object)(object)enemyUnit != (Object)null)
					{
						atkTimer = attackInterval;
						if (Terrain == null)
						{
							facing = ((((Component)enemyUnit).transform.position.x >= worldX) ? 1 : (-1));
						}
						Strike(enemyUnit);
					}
					else
					{
						if (attackRequested)
						{
							SwingEmpty();
						}
						atkTimer = 0.12f;
					}
				}
			}
		}
		attackRequested = false;
		TickPendingHit(dt);
		TickTrail();
	}

	private float BeginSwing()
	{
		if (!UsesRangedWeapon)
		{
			((Component)this).GetComponent<NativeBuffs>()?.OnCloseAttack();
		}
		BeginNativeAlert();
		pending.active = false;
		swingSfxAt = -1f;
		swingPlayback = 1f;
		trailFrames = null;
		trailPose = null;
		trailCursor = 0;
		if ((Object)(object)vis == (Object)null)
		{
			return 0f;
		}
		swingAction = (Crouching ? "proneStab" : "atk");
		Art.Clip clip = Art.GetClip(vis.kind, vis.key, swingAction);
		if (clip == null || clip.delays.Length == 0)
		{
			return 0f;
		}
		int num = WzCharacter.WeaponAttackSpeed(vis.key);
		num = ((Component)this).GetComponent<NativeBuffs>()?.AttackSpeed(num) ?? num;
		swingPlayback = 16f / (float)(Mathf.Clamp(num, 2, 10) + 10);
		vis.PlayNativeAttack(swingAction, num);
		trailName = WzCharacter.Afterimage(vis.key);
		if (trailName != null)
		{
			trailPose = ((swingAction == "proneStab") ? "proneStab" : WzCharacter.AttackPoseName(vis.key));
			AfterimageFx.Frame[] array = ((trailPose != null) ? AfterimageFx.Frames(trailName, trailPose) : null);
			if (array != null && array.Length != 0)
			{
				trailFrames = array;
			}
		}
		return WzCharacter.AttackFrameDelay(clip.delays[0], num);
	}

	private bool StrikeReactor()
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		if (Terrain == null || (Object)(object)vis == (Object)null || GameLoop.I?.field?.Map == null)
		{
			return false;
		}
		Nullable val = AfterimageFx.AttackBounds(WzCharacter.Afterimage(vis.key), Crouching ? "proneStab" : WzCharacter.AttackPoseName(vis.key));
		if (!val.HasValue)
		{
			return false;
		}
		MapReactor mapReactor = NativeReactorView.FindTarget(GameLoop.I.field.StateId, NativeTargetGeometry.World(val.Value, FeetPos, facing > 0));
		if (mapReactor == null)
		{
			return false;
		}
		float timer = BeginSwing();
		pending = new PendingHit
		{
			reactor = mapReactor,
			timer = timer,
			facing = facing,
			active = true
		};
		return true;
	}

	private void SwingEmpty()
	{
		if (SpendBasicProjectile())
		{
			SeedTowerCompat.HandleAttack(0, null);
			pending.active = false;
			float num = BeginSwing();
			swingSfxAt = ((num > 0.001f) ? num : 0f);
		}
	}

	private void PlaySwingSfx()
	{
		if (!((Object)(object)vis == (Object)null))
		{
			AudioManager.Play(WzCharacter.AttackSoundKey(vis.key) ?? "Weapon/swordL/Attack", 0.85f);
		}
	}

	private void TickPendingHit(float dt)
	{
		if (swingSfxAt >= 0f)
		{
			swingSfxAt -= dt;
			if (swingSfxAt < 0f)
			{
				PlaySwingSfx();
			}
		}
		if (!pending.active)
		{
			return;
		}
		pending.timer -= dt;
		if (pending.timer > 0f)
		{
			return;
		}
		pending.active = false;
		PendingHit pendingHit = pending;
		if (pendingHit.reactor != null)
		{
			MapReactor reactor = pendingHit.reactor;
			if (GameLoop.I?.field?.StateId == reactor.mapId && MapReactorService.Find(reactor.mapId, reactor.objectId) == reactor)
			{
				MapReactorService.HitPlayer(reactor.mapId, reactor.objectId, GameSession.CharacterId);
			}
		}
		else if ((Object)(object)pendingHit.boss != (Object)null)
		{
			if (!pendingHit.boss.dead)
			{
				HitBoss(pendingHit.boss, pendingHit.hit);
			}
		}
		else if ((Object)(object)pendingHit.enemy != (Object)null && !pendingHit.enemy.dead)
		{
			HitEnemy(pendingHit.enemy, pendingHit.hit, pendingHit.facing);
		}
		PlaySwingSfx();
	}

	private void TickTrail()
	{
		if (trailFrames != null && !((Object)(object)vis == (Object)null) && vis.IsOneShot && !(vis.Current != swingAction))
		{
			while (trailCursor < trailFrames.Length && trailFrames[trailCursor].index < vis.FrameIndex)
			{
				trailCursor++;
			}
			if (trailCursor < trailFrames.Length && trailFrames[trailCursor].index == vis.FrameIndex)
			{
				AfterimageFx.Spawn(trailName, trailPose, trailFrames[trailCursor], ((Component)vis).transform, facing, swingPlayback);
				trailCursor++;
			}
		}
	}

	private void StepMovement(float h, bool jumpReq, float dt)
	{
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_037f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0384: Unknown result type (might be due to invalid IL or missing references)
		//IL_0386: Unknown result type (might be due to invalid IL or missing references)
		//IL_038d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0395: Unknown result type (might be due to invalid IL or missing references)
		//IL_039c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_05cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fa: Unknown result type (might be due to invalid IL or missing references)
		if (rushRemaining > 0f)
		{
			h = (float)rushFacing * rushSpeed / moveSpeed;
			float num = Mathf.Min(dt, rushRemaining / rushSpeed);
			float num2 = worldX;
			rushRemaining = Mathf.Max(0f, rushRemaining - rushSpeed * num);
			float num3 = rushRemaining;
			rushRemaining = 0f;
			float num4 = climbInput;
			climbInput = 0f;
			StepMovement(h, jumpReq: false, num);
			climbInput = num4;
			rushRemaining = ((Mathf.Abs(worldX - num2) < 1E-05f) ? 0f : num3);
			return;
		}
		grabCooldown = Mathf.Max(0f, grabCooldown - dt);
		if (StepClimb(h, jumpReq, dt))
		{
			return;
		}
		if (jumpReq && grounded)
		{
			if (climbInput < -0.15f && BeginDownJump())
			{
				return;
			}
			int num5 = SupportSegment(worldX, terrainY);
			double num6 = 0.0;
			double num7 = 1.0;
			if (num5 >= 0)
			{
				Vector4 val = Terrain[num5];
				double num8 = Math.Abs(val.z - val.x);
				double num9 = (0f - (val.w - val.y)) * Mathf.Sign(val.z - val.x);
				double num10 = Math.Sqrt(num8 * num8 + num9 * num9);
				num7 = num8 / num10;
				num6 = num9 / num10;
			}
			horizontalVelocity *= num7;
			int num11 = ((h > 0.15f) ? 1 : ((h < -0.15f) ? (-1) : 0));
			if (num11 != 0)
			{
				double num12 = (double)(moveSpeed * 40f) * (1.0 + (double)Math.Sign((double)num11 * num6) * num6 * num6);
				if ((double)num11 * horizontalVelocity < 0.8 * num12)
				{
					horizontalVelocity += (double)num11 * 0.8 * num12;
				}
				if ((double)num11 * horizontalVelocity > num12)
				{
					horizontalVelocity = (double)num11 * num12;
				}
			}
			vy = jumpSpeed;
			grounded = false;
			AudioManager.Play("Game/Jump", 0.7f, pitchVary: false);
		}
		float num13 = terrainY + jumpY;
		if (grounded)
		{
			fallTracking = false;
		}
		else if (!fallTracking)
		{
			fallTracking = true;
			fallPeakY = (fallStartY = num13);
		}
		else if (num13 > fallPeakY)
		{
			fallPeakY = num13;
		}
		double num14 = horizontalVelocity;
		int input = ((h > 0.15f) ? 1 : ((h < -0.15f) ? (-1) : 0));
		float num15 = 1f;
		if (grounded)
		{
			int num16 = SupportSegment(worldX, num13);
			double force = ((num16 >= 0 && TerrainForces != null) ? ((double)TerrainForces[num16] * 0.01) : 0.0);
			double drag = ((num16 >= 0 && TerrainDrag != null && TerrainDrag[num16] != 0) ? ((double)TerrainDrag[num16] * 0.01) : 1.0);
			double slope = 0.0;
			if (num16 >= 0)
			{
				Vector4 val2 = Terrain[num16];
				Vector2 val3 = new Vector2(val2.z - val2.x, val2.w - val2.y);
				float magnitude = val3.magnitude;
				num15 = Mathf.Abs(val2.z - val2.x) / magnitude;
				slope = (0f - (val2.w - val2.y)) * Mathf.Sign(val2.z - val2.x) / magnitude;
			}
			horizontalVelocity = movementPhysics.Ground(horizontalVelocity, input, force, drag, (double)(moveSpeed * 40f) / movementPhysics.WalkSpeed, slope, dt);
		}
		else
		{
			horizontalVelocity = movementPhysics.Air(horizontalVelocity, (0f - vy) * 40f, input, dt);
		}
		float num17 = (float)((num14 + horizontalVelocity) * 0.5 * (double)dt / 40.0) * num15;
		if (Mathf.Abs(h) > 1f)
		{
			num17 = h * moveSpeed * dt;
			horizontalVelocity = 0.0;
		}
		float nextX = ClampX(worldX + num17);
		if (worldX + num17 < XMin || worldX + num17 > XMax)
		{
			horizontalVelocity = 0.0;
		}
		moving = Mathf.Abs(h) > 0.15f;
		if (moving && !NativeBasicAttackActive)
		{
			facing = ((h > 0f) ? 1 : (-1));
		}
		if (MoveHorizontal(nextX))
		{
			horizontalVelocity *= num15;
		}
		if (grounded)
		{
			return;
		}
		float num18 = vy;
		vy = Mathf.Max(vy + gravity * dt, 0f - fallSpeed);
		float num19 = num13 + (num18 + vy) * 0.5f * dt;
		if (vy <= 0f && FindLanding(worldX, num13, num19, dropSegment, out var landing))
		{
			terrainY = landing;
			int num20 = SupportSegment(worldX, landing);
			if (num20 >= 0)
			{
				Vector4 val4 = Terrain[num20];
				double num21 = Math.Abs(val4.z - val4.x);
				double num22 = (0f - (val4.w - val4.y)) * Mathf.Sign(val4.z - val4.x);
				double num23 = Math.Sqrt(num21 * num21 + num22 * num22);
				horizontalVelocity = horizontalVelocity * num21 / num23 - (double)(vy * 40f) * num22 / num23;
			}
			jumpY = 0f;
			vy = 0f;
			grounded = true;
			flashJumpUsed = false;
			dropSegment = -1;
			float num24 = (fallPeakY - landing) * 40f;
			fallTracking = false;
			int num25 = FallDamage.FromDistance(num24);
			if (num25 > 0)
			{
				TakeFallDamage(num25);
			}
		}
		else
		{
			jumpY = num19 - terrainY;
		}
		if (num19 < FallFloor)
		{
			RecoverFromFall(fallTracking ? fallStartY : num13);
		}
	}

	private bool MoveHorizontal(float nextX)
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		bool flag = grounded;
		float num = terrainY + jumpY;
		float outY = num;
		int num2 = Math.Sign(nextX - worldX);
		int num3 = ((num2 == 0) ? contactWallDirection : 0);
		float num4 = ((num2 == 0) ? contactWallX : 0f);
		bool flag2 = grounded && FollowGround(worldX, num, nextX, out outY);
		if (Terrain != null)
		{
			foreach (Vector4 item in Terrain)
			{
				if (!(Mathf.Abs(item.z - item.x) >= 0.01f) && ((worldX < item.x && nextX >= item.x) || (worldX > item.x && nextX <= item.x) || (worldX == item.x && num2 != 0 && (contactWallDirection == 0 || contactWallX != item.x || num2 == contactWallDirection))) && (!flag2 || !FollowGround(worldX, num, item.x + Mathf.Sign(nextX - worldX) * 0.001f, out var _)) && num < Mathf.Max(item.y, item.w) - 0.01f && num >= Mathf.Min(item.y, item.w) - 0.01f)
				{
					nextX = item.x;
					num4 = item.x;
					num3 = num2;
					horizontalVelocity = 0.0;
					flag2 = false;
				}
			}
		}
		contactWallX = num4;
		contactWallDirection = num3;
		if (grounded)
		{
			if (flag2 || FollowGround(worldX, num, nextX, out outY))
			{
				terrainY = outY;
				jumpY = 0f;
			}
			else
			{
				grounded = false;
				vy = 0f;
			}
		}
		worldX = nextX;
		if (flag)
		{
			return !grounded;
		}
		return false;
	}

	private void RecoverFromFall(float fellFromY)
	{
		if (!GroundBelow(worldX, fellFromY, out var atX, out var y))
		{
			y = NearestGround(worldX, out atX);
		}
		worldX = ClampX(atX);
		terrainY = y;
		jumpY = 0f;
		vy = 0f;
		horizontalVelocity = 0.0;
		contactWallDirection = 0;
		grounded = true;
		flashJumpUsed = false;
		fallTracking = false;
		climb = null;
		dropSegment = -1;
		grabCooldown = 0.3f;
		FallRecoveries++;
		Debug.Log((object)("[Player] 掉出地图，复位到 x=" + atX.ToString("F2") + " y=" + y.ToString("F2")));
	}

	private static bool GroundBelow(float x, float refY, out float atX, out float y)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		atX = x;
		y = float.NegativeInfinity;
		if (Terrain == null || Terrain.Count == 0)
		{
			return false;
		}
		float num = float.MaxValue;
		for (int i = 0; i < Terrain.Count; i++)
		{
			Vector4 val = Terrain[i];
			if (Mathf.Abs(val.z - val.x) < 0.01f)
			{
				continue;
			}
			float num2 = Mathf.Min(val.x, val.z);
			float num3 = Mathf.Max(val.x, val.z);
			float num4 = Mathf.Clamp(x, num2, num3);
			float num5 = Mathf.Lerp(val.y, val.w, (num4 - val.x) / (val.z - val.x));
			if (!(num5 >= refY))
			{
				float num6 = Mathf.Abs(num4 - x);
				if (num6 < num - 0.0001f || (Mathf.Abs(num6 - num) <= 0.0001f && num5 < y))
				{
					num = num6;
					atX = num4;
					y = num5;
				}
			}
		}
		return !float.IsNegativeInfinity(y);
	}

	private bool StepClimb(float h, bool jumpReq, float dt)
	{
		float num = terrainY + jumpY;
		if (climb == null && grabCooldown <= 0f && Mathf.Abs(climbInput) > 0.15f && Climbs != null)
		{
			float num2 = 0.25f;
			float num3 = 0.2f;
			foreach (Art.ClimbLine climb in Climbs)
			{
				float num4 = Mathf.Abs(worldX - climb.x);
				if (!(num4 > num2) && !(num < climb.bottom) && !(num > climb.top + num3) && (!grounded || !(climbInput > 0f) || !(num >= climb.top - num3)))
				{
					num2 = num4;
					this.climb = climb;
				}
			}
			if (this.climb != null)
			{
				flashJumpUsed = false;
				fallTracking = false;
				worldX = this.climb.x;
				horizontalVelocity = 0.0;
				grounded = false;
				vy = 0f;
				dropSegment = -1;
				if (num > this.climb.top)
				{
					jumpY = this.climb.top - terrainY;
				}
				num = terrainY + jumpY;
			}
		}
		if (this.climb == null)
		{
			return false;
		}
		if (jumpReq && Mathf.Abs(h) > 0.15f)
		{
			this.climb = null;
			grabCooldown = 0.25f;
			vy = jumpSpeed * 0.5f;
			facing = ((h > 0f) ? 1 : (-1));
			horizontalVelocity = (double)((float)facing * moveSpeed * 40f) * 1.3;
			return false;
		}
		float num5 = num + climbInput * moveSpeed * dt;
		if (num5 >= this.climb.top)
		{
			if (this.climb.upperExit && climbInput > 0f)
			{
				float num6 = GroundAt(worldX, this.climb.top);
				if (HasSupport(worldX, num6) && Mathf.Abs(num6 - this.climb.top) <= 0.75f)
				{
					terrainY = num6;
					jumpY = 0f;
					grounded = true;
					this.climb = null;
					grabCooldown = 0.2f;
					fallTracking = false;
					return true;
				}
			}
			num5 = this.climb.top;
		}
		if (num5 < this.climb.bottom)
		{
			this.climb = null;
			grabCooldown = 0.2f;
			vy = 0f;
			return false;
		}
		jumpY = num5 - terrainY;
		return true;
	}

	private static bool HasSupport(float x, float y)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		if (Terrain == null || Terrain.Count == 0)
		{
			return true;
		}
		foreach (Vector4 item in Terrain)
		{
			if (!(x < Mathf.Min(item.x, item.z)) && !(x > Mathf.Max(item.x, item.z)) && !(Mathf.Abs(item.z - item.x) < 0.01f) && Mathf.Abs(Mathf.Lerp(item.y, item.w, (x - item.x) / (item.z - item.x)) - y) < 0.01f)
			{
				return true;
			}
		}
		return false;
	}

	public static bool FindLanding(float x, float previousY, float nextY, int skip, out float landing)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		landing = float.NegativeInfinity;
		if (Terrain == null || Terrain.Count == 0)
		{
			landing = -1.4f;
			if (previousY >= -1.4f)
			{
				return nextY <= -1.4f;
			}
			return false;
		}
		for (int i = 0; i < Terrain.Count; i++)
		{
			if (i == skip)
			{
				continue;
			}
			Vector4 val = Terrain[i];
			if (!(x < Mathf.Min(val.x, val.z)) && !(x > Mathf.Max(val.x, val.z)) && !(Mathf.Abs(val.z - val.x) < 0.01f))
			{
				float num = Mathf.Lerp(val.y, val.w, (x - val.x) / (val.z - val.x));
				if (previousY >= num - 0.01f && nextY <= num && num > landing)
				{
					landing = num;
				}
			}
		}
		return !float.IsNegativeInfinity(landing);
	}

	private static int SupportSegment(float x, float y)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		if (Terrain == null || Terrain.Count == 0)
		{
			return -1;
		}
		int result = -1;
		float num = 0.0375f;
		for (int i = 0; i < Terrain.Count; i++)
		{
			Vector4 val = Terrain[i];
			if (!(Mathf.Abs(val.z - val.x) < 0.01f) && !(x < Mathf.Min(val.x, val.z)) && !(x > Mathf.Max(val.x, val.z)))
			{
				float num2 = Mathf.Abs(Mathf.Lerp(val.y, val.w, (x - val.x) / (val.z - val.x)) - y);
				if (num2 <= num)
				{
					num = num2;
					result = i;
				}
			}
		}
		return result;
	}

	private static bool HasLandingBelow(float x, float y, int skip)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		if (Terrain == null || Terrain.Count == 0)
		{
			return y > -1.39f;
		}
		for (int i = 0; i < Terrain.Count; i++)
		{
			if (i != skip)
			{
				Vector4 val = Terrain[i];
				if (!(Mathf.Abs(val.z - val.x) < 0.01f) && !(x < Mathf.Min(val.x, val.z)) && !(x > Mathf.Max(val.x, val.z)) && Mathf.Lerp(val.y, val.w, (x - val.x) / (val.z - val.x)) < y - 0.01f)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool BeginDownJump()
	{
		FieldMap fieldMap = (((Object)(object)GameLoop.I != (Object)null && (Object)(object)GameLoop.I.field != (Object)null) ? GameLoop.I.field.Map : null);
		if (fieldMap != null && (fieldMap.fieldLimit & 0x20000) != 0)
		{
			return false;
		}
		float y = terrainY + jumpY;
		int num = SupportSegment(worldX, y);
		if (num < 0 || !HasLandingBelow(worldX, y, num))
		{
			return false;
		}
		if (TerrainNoFallThrough != null && TerrainNoFallThrough[num])
		{
			return false;
		}
		dropSegment = num;
		grounded = false;
		horizontalVelocity = 0.0;
		vy = (0f - jumpSpeed) * 0.35355338f;
		jumpY = 0f;
		AudioManager.Play("Game/Jump", 0.7f, pitchVary: false);
		return true;
	}

	private BossUnit FindBossTarget()
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		BossUnit[] array = Object.FindObjectsOfType<BossUnit>();
		foreach (BossUnit bossUnit in array)
		{
			if (!((Object)(object)bossUnit == (Object)null) && !bossUnit.dead && Mathf.Abs(bossUnit.Lane - lane) <= 2)
			{
				float num = ((Component)bossUnit).transform.position.x - worldX;
				if ((Terrain == null || !(num * (float)facing < 0f)) && !(num < 0f - (reach + 1.3f)) && !(num > reach + 1.3f))
				{
					return bossUnit;
				}
			}
		}
		return null;
	}

	private void StrikeBoss(BossUnit b)
	{
		if (SpendBasicProjectile())
		{
			DamageResult hit = RollAttack(b.CombatStats);
			float num = BeginSwing();
			if (num <= 0.001f)
			{
				HitBoss(b, hit);
				PlaySwingSfx();
				return;
			}
			pending = new PendingHit
			{
				boss = b,
				enemy = null,
				hit = hit,
				timer = num,
				facing = facing,
				active = true
			};
		}
	}

	private EnemyUnit FindTarget()
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		EnemyUnit result = null;
		float num = float.MaxValue;
		foreach (EnemyUnit item in EnemyUnit.All)
		{
			if ((Object)(object)item == (Object)null || item.dead || Mathf.Abs(item.Lane - lane) > 1)
			{
				continue;
			}
			float num2 = ((Component)item).transform.position.x - worldX;
			if ((Terrain == null || !(num2 * (float)facing < 0f)) && InMeleeBand(FeetPos, ((Component)item).transform.position, reach))
			{
				float num3 = Mathf.Abs(num2);
				if (num3 < num)
				{
					num = num3;
					result = item;
				}
			}
		}
		return result;
	}

	public static bool InMeleeBand(Vector2 feet, Vector3 target, float reach)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if (Mathf.Abs(target.x - feet.x) > reach)
		{
			return false;
		}
		return Mathf.Abs(target.y - feet.y) <= 1.2f;
	}

	public void RequestAttack()
	{
		SkillSystem component = ((Component)this).GetComponent<SkillSystem>();
		if (!((Object)(object)component != (Object)null) || !component.TrySequenceBasicAttack())
		{
			Sitting = false;
			attackRequested = true;
		}
	}

	private void Strike(EnemyUnit e)
	{
		if (SpendBasicProjectile())
		{
			DamageResult hit = RollAttack(e.CombatStats);
			float num = BeginSwing();
			if (num <= 0.001f)
			{
				HitEnemy(e, hit, facing);
				PlaySwingSfx();
				return;
			}
			pending = new PendingHit
			{
				enemy = e,
				boss = null,
				hit = hit,
				timer = num,
				facing = facing,
				active = true
			};
		}
	}

	private bool SpendBasicProjectile()
	{
		if (!UsesRangedWeapon)
		{
			return true;
		}
		if (NativeProjectiles.Spend(NativeProjectiles.For(this, 0, 1)))
		{
			return true;
		}
		((Component)this).GetComponent<SkillSystem>()?.Report("缺少可用弹药");
		return false;
	}

	private void HitEnemy(EnemyUnit e, DamageResult hit, int dir)
	{
		if (SeedTowerCompat.HandleAttack(0, e))
		{
			return;
		}
		if ((Object)(object)e != (Object)null && !e.dead && UsesRangedWeapon)
		{
			((Component)this).GetComponent<NativeBuffs>()?.OnRangedAttack(1);
		}
		if ((Object)(object)e != (Object)null && !e.dead && (weapon.kind == WeaponKind.bow || weapon.kind == WeaponKind.crossbow))
		{
			((Component)this).GetComponent<SkillSystem>()?.NativeBasicRangedHit((Component)(object)e, hit, dir);
		}
		if (!((Object)(object)e == (Object)null) && !e.dead)
		{
			e.TakeHit(hit, dir);
			if (!e.dead)
			{
				((Component)this).GetComponent<SkillSystem>()?.NativeBasicProcHit((Component)(object)e, hit, dir);
			}
		}
	}

	private void HitBoss(BossUnit b, DamageResult hit)
	{
		if ((Object)(object)b != (Object)null && !b.dead && UsesRangedWeapon)
		{
			((Component)this).GetComponent<NativeBuffs>()?.OnRangedAttack(1);
		}
		if ((Object)(object)b != (Object)null && !b.dead && (weapon.kind == WeaponKind.bow || weapon.kind == WeaponKind.crossbow))
		{
			((Component)this).GetComponent<SkillSystem>()?.NativeBasicRangedHit((Component)(object)b, hit, facing);
		}
		if (!((Object)(object)b == (Object)null) && !b.dead)
		{
			b.TakeHit(hit, facing);
			if (!b.dead)
			{
				((Component)this).GetComponent<SkillSystem>()?.NativeBasicProcHit((Component)(object)b, hit, facing);
			}
		}
	}

	public void TakeDamage(float dmg, int dir, bool magic = false, int attackerLevel = 1, EnemyUnit attacker = null, int damageFrom = -1, int mpAttack = 0)
	{
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_065f: Unknown result type (might be due to invalid IL or missing references)
		if (hp <= 0f || Time.unscaledTime < invulnUntil)
		{
			return;
		}
		RefreshAttributes();
		NativeSpecialCore.OnHit(this);
		if (!magic && (Object)(object)attacker != (Object)null && dmg > 0f && CombatRules.PhysicalMiss(Mathf.FloorToInt((float)Attributes.avoidability + (float)Attributes.dex * 0.25f + (float)Attributes.luk * 0.5f), NativeCombatData.Int(attacker.CombatStats, "acc"), Attributes.level, attackerLevel, Profile.JobId, Random.Range(0, 10000000)))
		{
			dmg = 0f;
		}
		dmg = CombatRules.Incoming(dmg, Attributes, magic, attackerLevel, Random.value, Mathf.Max(((Component)this).GetComponent<NativeBuffs>()?.Value("INVINCIBLE") ?? 0, NativeSpecialCore.Invincible ? 100 : 0)).Amount;
		if (dmg <= 0f)
		{
			invulnUntil = Time.unscaledTime + 1.5f;
			DamageText.Miss(DamageAnchor, 1f, "violet");
			return;
		}
		float previousHp = hp;
		float previousMp = mp;
		NativeBuffs component = ((Component)this).GetComponent<NativeBuffs>();
		if ((Object)(object)attacker != (Object)null && (Object)(object)component != (Object)null)
		{
			long num = attacker.NativeController?.MaximumHp ?? attacker.hpMax;
			if (damageFrom == -1 && component.HasStat("POWERGUARD"))
			{
				int num2 = NativeBuffs.PowerGuardDamage((int)dmg, component.Value("POWERGUARD"), num, attacker.IsBoss);
				dmg -= (float)num2;
				if (num2 > 0)
				{
					attacker.TakeReplayDot(num2);
				}
			}
			else if (damageFrom >= 0 && !attacker.IsBoss && component.HasStat("MANA_REFLECTION"))
			{
				int num3 = Profile.JobId * 10000 + 1002;
				int num4 = SkillProgress.LevelOf(num3);
				if ((Profile.JobId == 212 || Profile.JobId == 222 || Profile.JobId == 232) && num4 > 0 && component.SourceOf("MANA_REFLECTION") == num3)
				{
					Dictionary<string, object> d = SkillBook.Get(num3)?.Level(num4);
					if (Random.value < (float)Art.Int(d, "prop", 100) / 100f)
					{
						attacker.TakeReplayDot((int)Math.Min(num / 5, (long)(int)dmg * (long)Art.Int(d, "x") / 100));
					}
				}
			}
		}
		int num5 = ((Profile.JobId < 200 && Profile.JobId % 10 == 2) ? (Profile.JobId * 10000 + ((Profile.JobId == 112) ? 4 : 5)) : 0);
		int num6 = ((num5 != 0) ? SkillProgress.LevelOf(num5) : 0);
		int num7 = SkillProgress.LevelOf(21120004);
		dmg = NativeBuffs.PassiveIncoming((int)dmg, ((Object)(object)attacker != (Object)null) ? (component?.Value("COMBO_BARRIER") ?? 0) : 0, (damageFrom != -3 && damageFrom != -4 && num6 > 0) ? Art.Int(SkillBook.Get(num5)?.Level(num6), "x") : 0, (damageFrom != -3 && damageFrom != -4 && num7 > 0) ? Art.Int(SkillBook.Get(21120004)?.Level(num7), "x") : 0);
		NativeShields component2 = ((Component)this).GetComponent<NativeShields>();
		float num8 = (((Object)(object)component2 != (Object)null) ? component2.Absorb(ref dmg) : 0f);
		if (dmg <= 0f && num8 > 0f)
		{
			TickContactTint();
			contactTintStarted = Time.unscaledTime;
			contactTintActive = true;
			invulnUntil = Time.unscaledTime + 1.5f;
			vis?.ShowHitExpression();
			DamageText.Show(DamageAnchor, 0L, "blue");
			return;
		}
		if ((Object)(object)component != (Object)null)
		{
			bool flag = component.HasStat("MAGIC_GUARD");
			bool flag2 = component.HasStat("MESOGUARD");
			NativeBuffs.GuardLoss((int)dmg, (int)mp, Profile.gold, component.Value("MAGIC_GUARD"), component.Value("MESOGUARD"), flag, flag2, mpAttack, out var hpLoss, out var mpLoss, out var mesoLoss, out var cancelMeso);
			hp = Mathf.Max(0f, hp - (float)hpLoss);
			mp = Mathf.Max(0f, mp - (float)mpLoss);
			if (mesoLoss > 0)
			{
				Profile.Spend(mesoLoss);
			}
			if (cancelMeso)
			{
				component.CancelStat("MESOGUARD");
			}
			if (!flag & flag2)
			{
				dmg = hpLoss;
			}
		}
		else
		{
			hp = Mathf.Max(0f, hp - dmg);
			mp = Mathf.Max(0f, mp - (float)mpAttack);
		}
		NativePetAutopot.OnLoss(this, previousHp, previousMp);
		TickContactTint();
		contactTintStarted = Time.unscaledTime;
		contactTintActive = true;
		invulnUntil = Time.unscaledTime + 1.5f;
		BeginNativeAlert();
		if (!NativeBuffs.ResistsKnockback(component?.Value("STANCE") ?? 0, (GameLoop.I?.field?.Map?.fieldLimit).GetValueOrDefault(), Random.Range(0, 100)))
		{
			if (IsClimbing)
			{
				climb = null;
				grabCooldown = 0.25f;
			}
			grounded = false;
			horizontalVelocity = ContactImpulse(horizontalVelocity, Math.Sign(dir) * 270);
			vy = (0f - (float)ContactImpulse((0f - vy) * 40f, -270.0)) / 40f;
		}
		vis?.ShowHitExpression();
		if (hp <= 0f && ConsumeHeavenDoor())
		{
			hp = 1f;
		}
		if ((Object)(object)vis != (Object)null && hp <= 0f)
		{
			vis.PlayOnce("die");
		}
		DamageText.Show(DamageAnchor, dmg, "blue");
		if (hp <= 0f && (Object)(object)GameLoop.I != (Object)null)
		{
			GameLoop.I.OnPlayerDead();
		}
	}

	public void TakeFallDamage(int dmg)
	{
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		if (hp <= 0f || dmg <= 0)
		{
			return;
		}
		RefreshAttributes();
		NativeShields component = ((Component)this).GetComponent<NativeShields>();
		if ((Object)(object)component != (Object)null)
		{
			float damage = dmg;
			component.Absorb(ref damage);
			dmg = Mathf.Max(0, Mathf.FloorToInt(damage));
			if (dmg <= 0)
			{
				return;
			}
		}
		float previousHp = hp;
		float previousMp = mp;
		NativeBuffs component2 = ((Component)this).GetComponent<NativeBuffs>();
		if ((Object)(object)component2 != (Object)null)
		{
			bool flag = component2.HasStat("MAGIC_GUARD");
			bool flag2 = component2.HasStat("MESOGUARD");
			NativeBuffs.GuardLoss(dmg, (int)mp, Profile.gold, component2.Value("MAGIC_GUARD"), component2.Value("MESOGUARD"), flag, flag2, 0, out var hpLoss, out var mpLoss, out var mesoLoss, out var cancelMeso);
			hp = Mathf.Max(0f, hp - (float)hpLoss);
			mp = Mathf.Max(0f, mp - (float)mpLoss);
			if (mesoLoss > 0)
			{
				Profile.Spend(mesoLoss);
			}
			if (cancelMeso)
			{
				component2.CancelStat("MESOGUARD");
			}
			if (!flag & flag2)
			{
				dmg = hpLoss;
			}
		}
		else
		{
			hp = Mathf.Max(0f, hp - (float)dmg);
		}
		NativePetAutopot.OnLoss(this, previousHp, previousMp);
		TickContactTint();
		contactTintStarted = Time.unscaledTime;
		contactTintActive = true;
		invulnUntil = Time.unscaledTime + 1.5f;
		BeginNativeAlert();
		vis?.ShowHitExpression();
		if (hp <= 0f && ConsumeHeavenDoor())
		{
			hp = 1f;
		}
		if ((Object)(object)vis != (Object)null && hp <= 0f)
		{
			vis.PlayOnce("die");
		}
		DamageText.Show(DamageAnchor, dmg, "blue");
		if (hp <= 0f && (Object)(object)GameLoop.I != (Object)null)
		{
			GameLoop.I.OnPlayerDead();
		}
	}

	private bool ConsumeHeavenDoor()
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		NativeSkillStates component = ((Component)this).GetComponent<NativeSkillStates>();
		if ((Object)(object)component == (Object)null)
		{
			return false;
		}
		int[] array = NativeSkillStates.AllIds();
		foreach (int num in array)
		{
			if (NativeSkillStates.IsDeathGuard(num) && component.Has(num))
			{
				component.Consume(num);
				BottomBar.Toast("天堂之门：无视一次 HP 归零");
				DamageText.Show(DamageAnchor, 0L, "blue");
				return true;
			}
		}
		return false;
	}

	internal static double ContactImpulse(double velocity, double impulse)
	{
		if (impulse < 0.0 && velocity > impulse)
		{
			velocity = Math.Max(impulse, velocity + impulse);
		}
		else if (impulse > 0.0 && velocity < impulse)
		{
			velocity = Math.Min(impulse, velocity + impulse);
		}
		return Math.Truncate(velocity);
	}

	public void TakeBossDamage(float value)
	{
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if (hp <= 0f || value <= 0f)
		{
			return;
		}
		SkillSystem component = ((Component)this).GetComponent<SkillSystem>();
		if ((Object)(object)component != (Object)null && component.TryEvadeHpRatioDamage())
		{
			DamageText.Miss(DamageAnchor, 1f, "violet");
			return;
		}
		float previousHp = hp;
		hp = Mathf.Max(0f, hp - value);
		NativePetAutopot.OnLoss(this, previousHp, mp);
		DamageText.Show(DamageAnchor, value, "blue");
		if (hp <= 0f && (Object)(object)GameLoop.I != (Object)null)
		{
			GameLoop.I.OnPlayerDead();
		}
	}

	public void DispelNativeBuffs()
	{
		((Component)this).GetComponent<NativeBuffs>()?.Dispel();
		attackBonus = 0f;
		attackBonusUntil = 0f;
		partyGrindDefense = 0;
		((Component)this).GetComponent<SkillSystem>()?.CancelActions();
	}

	public void Heal(float v, bool skill = false)
	{
		v = ((Component)this).GetComponent<NativeMobDebuffs>()?.Healing(v, skill) ?? v;
		if (v < 0f)
		{
			TakeBossDamage(0f - v);
		}
		else
		{
			hp = Mathf.Min(hpMax, hp + v);
		}
	}

	public void Revive(int hpValue, int mpValue)
	{
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		Sitting = false;
		((Component)this).GetComponent<NativeMobDebuffs>()?.Clear();
		CancelRush();
		climb = null;
		dropSegment = -1;
		grabCooldown = 0f;
		hp = Mathf.Clamp((float)hpValue, 1f, hpMax);
		mp = Mathf.Clamp((float)mpValue, 0f, mpMax);
		pending.active = false;
		((Component)this).GetComponent<NativeBuffs>()?.Clear();
		attackBonus = 0f;
		attackBonusUntil = 0f;
		invulnUntil = Time.unscaledTime + 1.5f;
		((Component)this).GetComponent<SkillSystem>()?.CancelActions();
		if ((Object)(object)vis != (Object)null)
		{
			vis.SetBase("stand");
			vis.Play("stand", once: true);
		}
		Fx.Flash(sr, new Color(1f, 1f, 1f));
	}

	public void PlayDeadPose()
	{
		Sitting = false;
		if (!((Object)(object)vis == (Object)null))
		{
			string text = ((Art.GetClip(vis.kind, vis.key, "dead") != null) ? "dead" : "die");
			vis.SetBase(text);
			vis.Play(text, once: false);
		}
	}

	public void PresentDeath(float seconds)
	{
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)vis == (Object)null))
		{
			float num = seconds * MathF.PI;
			float num2 = -10f * Mathf.Cos(num);
			float num3 = 10f * Mathf.Sin(num);
			num2 = Mathf.Sign(num2) * Mathf.Floor(Mathf.Abs(num2) + 0.5f);
			num3 = Mathf.Sign(num3) * Mathf.Floor(Mathf.Abs(num3) + 0.5f);
			((Component)vis).transform.localPosition = new Vector3(num2, 20f + num3, 0f) / 40f;
			Color color = vis.Renderer.color;
			color.a = Mathf.Clamp01(seconds / 2.5f);
			vis.Renderer.color = color;
		}
	}

	public void ClearDeathPresentation()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)vis == (Object)null))
		{
			((Component)vis).transform.localPosition = Vector3.zero;
			vis.Renderer.color = Color.white;
		}
	}

	public void AddAttackBonus(float amount, float seconds)
	{
		attackBonus = amount;
		attackBonusUntil = Time.unscaledTime + seconds;
	}

	public void SetPartyGrindEffect(bool active)
	{
		partyGrindDefense = (active ? WzAssets.Node("Item/Consume/0243.img/02431159/spec").Int("pdd") : 0);
	}

	private void RefreshAttributes()
	{
		baseAttributes = Profile.Attributes;
		NativeBuffs component = ((Component)this).GetComponent<NativeBuffs>();
		int num = (((Object)(object)component != (Object)null) ? (component.Value("WATK") + component.Value("PAD")) : 0);
		Attributes = baseAttributes.WithWeapon(weapon, num + ((Time.unscaledTime < attackBonusUntil) ? Mathf.RoundToInt(attackBonus) : 0), out weaponAttack, out magicAttack);
		Attributes.weaponDefense += partyGrindDefense;
		if ((Object)(object)component != (Object)null)
		{
			Attributes.weaponDefense += component.Value("WDEF");
			Attributes.magicDefense += component.Value("MDEF");
			Attributes.accuracy += component.Value("ACC");
			Attributes.avoidability += component.Value("AVOID");
			magicAttack += component.Value("MATK") + component.Value("MAD");
			int num2 = component.Value("MAPLE_WARRIOR");
			Attributes.str += baseAttributes.str * num2 / 100;
			Attributes.dex += baseAttributes.dex * num2 / 100;
			Attributes.intelligence += baseAttributes.intelligence * num2 / 100;
			Attributes.luk += baseAttributes.luk * num2 / 100;
			Attributes.str += component.Value("STR");
			Attributes.dex += component.Value("DEX");
			Attributes.intelligence += component.Value("INT");
			Attributes.luk += component.Value("LUK");
			int num3 = component.Value("HYPERBODYHP");
			int num4 = component.Value("HYPERBODYMP");
			if (num3 != 0)
			{
				Attributes.maxHp = (int)Math.Max(50L, Math.Min(50000L, Attributes.maxHp + (long)Attributes.maxHp * (long)num3 / 100));
			}
			if (num4 != 0)
			{
				Attributes.maxMp = (int)Math.Max(5L, Math.Min(50000L, Attributes.maxMp + (long)Attributes.maxMp * (long)num4 / 100));
			}
		}
		if ((Object)(object)component != (Object)null)
		{
			int num5 = component.Value("PADPct");
			int num6 = component.Value("MADPct");
			if (num5 != 0)
			{
				weaponAttack = weaponAttack * (100 + num5) / 100;
			}
			if (num6 != 0)
			{
				magicAttack = magicAttack * (100 + num6) / 100;
			}
		}
		ApplySpecialCore();
		float overCritBonus = 0f;
		if ((Object)(object)component != (Object)null && component.Value("CRIT_REINFORCE") > 0)
		{
			int lv = Mathf.Max(1, SkillProgress.LevelOf(400031023));
			int num7 = Art.Int(SkillBook.Get(400031023)?.Level(lv), "x");
			if (num7 > 0)
			{
				overCritBonus = Mathf.Max(0f, Attributes.criticalChance - 1f) * (float)num7 / 100f;
			}
		}
		Attributes.overCritBonus = overCritBonus;
	}

	private void ApplySpecialCore()
	{
		int num = NativeSpecialCore.DamagePercent;
		int num2 = NativeSpecialCore.BossDamagePercent;
		int num3 = NativeSpecialCore.IgnoreDefensePercent;
		int num4 = NativeSpecialCore.CriticalPercent;
		int num5 = 0;
		int num6 = 0;
		NativeBuffs component = ((Component)this).GetComponent<NativeBuffs>();
		if ((Object)(object)component != (Object)null)
		{
			num += component.Value("Damage");
			num2 += component.Value("BossDamage");
			num3 += component.Value("IgnoreDefense");
			num4 += component.Value("CriticalChance");
			num5 += component.Value("NormalDamage");
			num6 += component.Value("FinalDamage");
		}
		if (num != 0 || num2 != 0 || num3 != 0 || num4 != 0 || num5 != 0 || num6 != 0)
		{
			Dictionary<string, int> dictionary = ((Attributes.setItemBonus == null) ? new Dictionary<string, int>() : new Dictionary<string, int>(Attributes.setItemBonus));
			if (num != 0)
			{
				dictionary["Damage"] = (dictionary.TryGetValue("Damage", out var value) ? value : 0) + num;
			}
			if (num2 != 0)
			{
				dictionary["BossDamage"] = (dictionary.TryGetValue("BossDamage", out var value2) ? value2 : 0) + num2;
			}
			if (num5 != 0)
			{
				dictionary["NormalDamage"] = (dictionary.TryGetValue("NormalDamage", out var value3) ? value3 : 0) + num5;
			}
			if (num6 != 0)
			{
				dictionary["FinalDamage"] = (dictionary.TryGetValue("FinalDamage", out var value4) ? value4 : 0) + num6;
			}
			if (num3 != 0)
			{
				dictionary["IgnoreDefense"] = (dictionary.TryGetValue("IgnoreDefense", out var value5) ? value5 : 0) + num3;
			}
			Attributes.setItemBonus = dictionary;
			if (num4 > 0)
			{
				Attributes.criticalChance = Mathf.Min(1f, Attributes.criticalChance + (float)num4 / 100f);
			}
		}
	}

	public CharacterAttributes ReadCombatAttributes(out int physicalAttack, out int magicalAttack)
	{
		RefreshAttributes();
		physicalAttack = weaponAttack;
		magicalAttack = magicAttack;
		return Attributes;
	}

	public void RefreshBuffAttributes()
	{
		float num = hpMax;
		float num2 = mpMax;
		RefreshAttributes();
		hpMax = Attributes.maxHp;
		mpMax = Attributes.maxMp;
		if (hpMax < num)
		{
			hp = Mathf.Min(hp, hpMax);
		}
		if (mpMax < num2)
		{
			mp = Mathf.Min(mp, mpMax);
		}
		RefreshMovementAttributes();
	}

	public static int MovementPercent(int equipment, int passive, int temporary, int maximum, int forced = 0)
	{
		int num = Math.Max(100, Math.Min(maximum, 100 + equipment + passive));
		return Math.Max(80, Math.Min(maximum, (forced != 0) ? forced : (num + temporary)));
	}

	private static int MovementSetBuff(Dictionary<string, int> set, string key, int equipment, int buff)
	{
		if (set == null || (!set.ContainsKey(key) && !set.ContainsKey(key + "Pct")))
		{
			return buff;
		}
		return Math.Max(-32768, Math.Min(32767, AdminSetItem.ApplyStat(set, key, equipment + buff) - equipment));
	}

	public void RefreshMovementAttributes()
	{
		if (Attributes != null && movementPhysics != null)
		{
			NativeBuffs component = ((Component)this).GetComponent<NativeBuffs>();
			int temporary = MovementSetBuff(Attributes.setItemBonus, "SPD", Attributes.equipmentSpeed, component?.Value("SPEED") ?? 0);
			int temporary2 = MovementSetBuff(Attributes.setItemBonus, "JMP", Attributes.equipmentJump, component?.Value("JUMP") ?? 0);
			int num;
			switch (Profile.JobId)
			{
			default:
				num = 0;
				break;
			case 1310:
			case 1311:
			case 1312:
				num = 13100004;
				break;
			case 321:
			case 322:
				num = 3210000;
				break;
			case 311:
			case 312:
				num = 3110000;
				break;
			}
			int num2 = num;
			int num3 = ((num2 != 0) ? SkillProgress.LevelOf(num2) : 0);
			int passive = ((num3 > 0) ? Art.Int(SkillBook.Get(num2)?.Level(num3), "speed") : 0);
			moveSpeed = (float)(movementPhysics.WalkSpeed / 40.0) * (float)MovementPercent(Attributes.equipmentSpeed, passive, temporary, 140, ((Component)this).GetComponent<NativeMobDebuffs>()?.ForcedWalkSpeed ?? 0) / 100f;
			jumpSpeed = (float)(movementPhysics.JumpSpeed / 40.0) * (float)MovementPercent(Attributes.equipmentJump, 0, temporary2, 123) / 100f;
		}
	}

	public DamageResult RollAttack(Dictionary<string, object> target, NativeSkillDefinition skill = null)
	{
		RefreshAttributes();
		return CombatRules.Attack(Attributes, weapon, weaponAttack, magicAttack, target, skill, Random.value, Random.value, Random.value, GameSession.Character?.record.damageCap ?? 19999999);
	}

	public DamageResult[] ReplayFallback(NativeSkillDefinition skill, int count, bool close)
	{
		RefreshAttributes();
		long val;
		if (skill.Magic)
		{
			long num = (long)(Math.Ceiling(((double)magicAttack * Math.Ceiling((double)magicAttack / 1000.0) + (double)magicAttack) / 30.0) + Math.Ceiling((double)Attributes.intelligence / 200.0));
			int num2 = SkillProgress.LevelOf(12110001);
			Dictionary<string, object> dictionary = ((num2 <= 0) ? null : SkillBook.Get(12110001)?.Level(num2));
			if (dictionary != null)
			{
				num = num * (int)Art.Num(dictionary, "y") / 100;
			}
			val = (long)((float)num * skill.MagicAttack);
			goto IL_0286;
		}
		int jobId = Profile.JobId;
		bool flag = weapon.kind == WeaponKind.bow || weapon.kind == WeaponKind.crossbow || weapon.kind == WeaponKind.gun;
		int num3;
		int num4;
		if (weapon.kind != WeaponKind.claw)
		{
			if (weapon.kind == WeaponKind.dagger)
			{
				num3 = ((jobId / 100 == 4) ? 1 : 0);
				if (num3 != 0)
				{
					goto IL_0148;
				}
			}
			else
			{
				num3 = 0;
			}
			num4 = (flag ? Attributes.dex : Attributes.str);
			goto IL_0153;
		}
		num3 = 1;
		goto IL_0148;
		IL_0286:
		val = Math.Max(1L, val);
		int num5 = ((Profile.JobId / 100 == 11) ? Math.Min(10, (SkillProgress.LevelOf(11100000) + 1) / 2) : 0);
		long num6 = Math.Max(1L, (long)Math.Floor((double)(val * (10 + num5 * 5)) / 100.0 + 0.5));
		DamageResult[] array = new DamageResult[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = new DamageResult((long)(close ? Math.Min(val, (double)num6 + Math.Floor(Random.value * (float)(val - num6 + 1))) : ((double)val)), critical: false);
		}
		return array;
		IL_0148:
		num4 = Attributes.luk;
		goto IL_0153;
		IL_0153:
		int mainStat = num4;
		int secondaryStat;
		if (num3 != 0)
		{
			secondaryStat = Attributes.dex + Attributes.str;
		}
		else
		{
			secondaryStat = (flag ? Attributes.str : Attributes.dex);
		}
		int num7 = ((!string.IsNullOrEmpty(Profile.equipWeapon)) ? CombatRules.MaxPhysicalDamage(mainStat, secondaryStat, Mathf.Max(14, weaponAttack), weapon.multiplier) : ((jobId / 100 != 5 && jobId / 100 != 15) ? 1 : ((int)Math.Ceiling(((double)Attributes.str * ((jobId % 100 == 0) ? 3.0 : 4.2) + (double)Attributes.dex) * Math.Min(Math.Floor((2.0 * (double)Attributes.level + 31.0) / 3.0), 31.0) / 100.0))));
		val = (long)Math.Floor((double)num7 * (double)skill.DamagePercent / 100.0 + 0.5);
		goto IL_0286;
	}

	public void BeginRush(NativeSkillDefinition skill, float duration)
	{
		rushRemaining = Mathf.Max(0f, skill.Max.x - skill.Min.x);
		rushSpeed = rushRemaining / Mathf.Max(0.01f, duration);
		rushFacing = facing;
	}

	public void CancelRush()
	{
		rushRemaining = 0f;
	}

	public void PlaceAt(float x, float groundY)
	{
		Sitting = false;
		CancelRush();
		climb = null;
		dropSegment = -1;
		grabCooldown = 0f;
		worldX = ClampX(x);
		terrainY = groundY;
		jumpY = 0f;
		vy = 0f;
		horizontalVelocity = 0.0;
		contactWallDirection = 0;
		grounded = true;
		flashJumpUsed = false;
		fallTracking = false;
		laneVis = lane;
		SyncTransform();
	}

	public void ResetCombatState()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		alertUntil = 0f;
		contactTintActive = false;
		if ((Object)(object)sr != (Object)null)
		{
			sr.color = Color.white;
		}
		hp = hpMax;
		mp = mpMax;
		CancelRush();
		((Component)this).GetComponent<NativeBuffs>()?.Clear();
		attackBonus = 0f;
		attackBonusUntil = 0f;
		partyGrindDefense = 0;
		pending.active = false;
		((Component)this).GetComponent<SkillSystem>()?.CancelActions();
	}

	private void SyncMorph()
	{
		if ((Object)(object)vis == (Object)null)
		{
			return;
		}
		int num = ((Component)this).GetComponent<NativeBuffs>()?.Value("MORPH") ?? 0;
		if (num == visibleMorph)
		{
			return;
		}
		if (num > 0)
		{
			if (WzAssets.MorphClip(num.ToString(), "stand") == null)
			{
				return;
			}
			if (visibleMorph == 0)
			{
				beforeMorphKey = vis.key;
			}
			vis.Init("morph", num.ToString(), "stand", sr);
		}
		else if (beforeMorphKey != null)
		{
			vis.Init("chars", beforeMorphKey, "stand", sr);
		}
		visibleMorph = num;
	}

	private void SyncTransform()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		((Component)this).transform.position = new Vector3(worldX, terrainY + jumpY + DepthOffset, 0f);
		if (!((Object)(object)sr == (Object)null))
		{
			SyncMorph();
			TickContactTint();
			((Renderer)sr).sortingOrder = SortingOrder;
			if ((Object)(object)vis != (Object)null)
			{
				vis.SetFacing(facing);
				ApplyDye();
			}
			else
			{
				Vector3 localScale = ((Component)sr).transform.localScale;
				((Component)sr).transform.localScale = new Vector3(Mathf.Abs(localScale.x) * ((facing >= 0) ? 1f : (-1f)), localScale.y, 1f);
			}
		}
	}

	private void TickContactTint()
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		if (!contactTintActive)
		{
			return;
		}
		int num = Mathf.Clamp(Mathf.FloorToInt((Time.unscaledTime - contactTintStarted) / 0.03f), 0, 50);
		if (Time.unscaledTime >= contactTintStarted + 1.5f)
		{
			contactTintPhase = (contactTintPhase + num) & 3;
			contactTintActive = false;
			if ((Object)(object)sr != (Object)null)
			{
				sr.color = Color.white;
			}
		}
		else if ((Object)(object)sr != (Object)null)
		{
			sr.color = ((hp > 0f && ((contactTintPhase + num + 1) & 3) < 2) ? Color32.op_Implicit(new Color32((byte)128, (byte)128, (byte)128, byte.MaxValue)) : Color.white);
		}
	}
}
