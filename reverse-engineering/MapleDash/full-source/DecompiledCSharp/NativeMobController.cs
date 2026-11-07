using System;
using System.Collections.Generic;
using System.Globalization;
using MapleDash.Wz;
using UnityEngine;

public sealed class NativeMobController : MonoBehaviour
{
	private sealed class Cast
	{
		public int id;

		public int level;

		public int action;

		public int mp;

		public int hp;

		public int x;

		public int y;

		public int count;

		public int limit;

		public int probability;

		public long duration;

		public long cooldown;

		public int serverDelay;

		public int effectAfter;

		public bool cast;

		public Nullable range;

		public readonly List<int> summons = new List<int>();

		public string Key => id + ":" + level;
	}

	private sealed class BuffValue
	{
		public int value;

		public long until;
	}

	private readonly List<Cast> skills = new List<Cast>();

	private readonly Dictionary<int, BuffValue> buffs = new Dictionary<int, BuffValue>();

	private readonly List<EnemyUnit> summoned = new List<EnemyUnit>();

	private EnemyUnit mob;

	private FieldDirector field;

	private NativeBossEncounter encounter;

	private NativeMobCastState state;

	private NativeMobHealth health;

	private ImgNode root;

	private ImgNode artRoot;

	private NativeMovementPhysics motionPhysics;

	private readonly NativeMobPursuit pursuit = new NativeMobPursuit();

	private long pursuitTick;

	private bool flightWaypoint;

	private Vector2 waypoint;

	private int flightPoints;

	private double flightVx;

	private double flightVy;

	private long blockedUntil;

	private long busyUntil;

	private long nextDecision;

	private long attackGapUntil;

	private static readonly float TrembleAmplitude = 0.75f;

	private long removeAt = long.MaxValue;

	private long nextMpRecovery;

	private int destructionHp = -1;

	private int deathAction = 1;

	private int destructionAction = 1;

	private readonly List<int> revives = new List<int>();

	private readonly string killId = Guid.NewGuid().ToString("N");

	private int showdown;

	private long showdownUntil;

	private const float PlayerHitHalfWidth = 14f;

	private const float PlayerHitHeight = 48f;

	public bool Flying => artRoot?.Get("fly") != null;

	public bool Pursuing => pursuit.Active;

	public int DeathAction => deathAction;

	public int ServerDeathMillis => ServerMillis(root?.Get("die1"));

	public static long Now => (long)(Time.timeAsDouble * 1000.0);

	public long CurrentHp => health?.Current ?? 0;

	public long MaximumHp => health?.Maximum ?? 1;

	public bool Busy => Now < busyUntil;

	public bool CanMove
	{
		get
		{
			if (artRoot != null)
			{
				return artRoot.Get("move") != null;
			}
			return false;
		}
	}

	public bool HasChase => artRoot?.Get("chase") != null;

	public int Direction { get; private set; } = 1;

	public int Showdown
	{
		get
		{
			if (Now >= showdownUntil)
			{
				return 0;
			}
			return showdown;
		}
	}

	public void OnPlayerDamage()
	{
		if (!pursuit.Active)
		{
			pursuit.Acquire(Random.Range(0, 60000));
			flightWaypoint = false;
			flightPoints = 0;
		}
	}

	public bool UpdatePursuit(PlayerCtrl player)
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		long now = Now;
		pursuit.Tick(Math.Max(0L, now - pursuitTick), (Object)(object)player != (Object)null && player.hp > 0f);
		pursuitTick = now;
		if (!pursuit.Active && (Object)(object)player != (Object)null && player.hp > 0f && root.Int("info/firstAttack") != 0 && root.Int("info/notAttack") == 0)
		{
			Vector2 val = Pixels(Vector2.op_Implicit(player.FeetPos)) - Pixels(((Component)mob).transform.position);
			if (NativeMobMotion.AcquisitionRange((int)val.x, (int)val.y))
			{
				OnPlayerDamage();
			}
		}
		return pursuit.Active;
	}

	public bool MoveFlight(PlayerCtrl player, float dt, bool blocked, out int direction)
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		direction = Direction;
		if (!pursuit.Active | blocked)
		{
			return false;
		}
		NativeMovementPhysics nativeMovementPhysics = motionPhysics ?? (motionPhysics = NativeMovementPhysics.Load());
		double fly = NativeMobMotion.Multiplier(root.Int("info/flySpeed"));
		Vector2 val = Pixels(((Component)mob).transform.position);
		Vector2 val2 = Pixels(Vector2.op_Implicit(player.FeetPos));
		if (!flightWaypoint)
		{
			if (flightPoints == 0)
			{
				flightPoints = Random.Range(60, 120);
			}
			waypoint = new Vector2((float)((int)val2.x + Random.Range(-120, 120)), (float)((int)val2.y + Random.Range(-40, 40)));
			flightWaypoint = true;
			if (--flightPoints <= 0)
			{
				pursuit.Clear();
			}
		}
		float num = waypoint.x - val.x;
		float num2 = waypoint.y - val.y;
		int num3 = ((Math.Abs((int)num) >= 10) ? ((!(num < 0f)) ? 1 : (-1)) : 0);
		int vertical = ((Math.Abs((int)num2) < 10) ? (-1) : 0);
		if (Math.Abs((int)num2) >= 10 && num2 < 0f)
		{
			flightVy = nativeMovementPhysics.Flap(fly);
		}
		double num4 = flightVx;
		double num5 = flightVy;
		nativeMovementPhysics.Flight(ref flightVx, ref flightVy, num3, vertical, fly, dt);
		val.x += (float)((num4 + flightVx) * 0.5 * (double)dt);
		val.y += (float)((num5 + flightVy) * 0.5 * (double)dt);
		((Component)mob).transform.position = new Vector3(val.x / 40f, (0f - val.y) / 40f, ((Component)mob).transform.position.z);
		if (Inside(val, 11) || !Inside(val2, 150))
		{
			flightWaypoint = false;
		}
		if (num3 != 0)
		{
			direction = num3;
		}
		if (flightVx == 0.0)
		{
			return flightVy != 0.0;
		}
		return true;
		bool Inside(Vector2 p, int radius)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			if ((float)(int)p.x >= waypoint.x - (float)radius && (float)(int)p.x < waypoint.x + (float)radius && (float)(int)p.y >= waypoint.y - (float)radius)
			{
				return (float)(int)p.y < waypoint.y + (float)radius;
			}
			return false;
		}
	}

	public void Expire(int action)
	{
		deathAction = action;
		mob.ExpireNative(action);
	}

	public void ReleaseExpired()
	{
		field?.OnEnemyExpired(mob);
	}

	public float GroundMoveSpeed(bool chasing)
	{
		int raw = root.Int("info/" + ((chasing && HasChase) ? "chaseSpeed" : "speed"));
		return (float)((motionPhysics ?? (motionPhysics = NativeMovementPhysics.Load())).WalkSpeed * NativeMobMotion.Multiplier(raw) / 40.0);
	}

	public static NativeMobController Attach(EnemyUnit mob, FieldDirector field)
	{
		NativeMobController nativeMobController = ((Component)mob).gameObject.AddComponent<NativeMobController>();
		nativeMobController.mob = mob;
		nativeMobController.field = field;
		nativeMobController.encounter = ((Component)field).GetComponent<NativeBossEncounter>() ?? ((Component)field).gameObject.AddComponent<NativeBossEncounter>();
		nativeMobController.encounter.Bind(field);
		nativeMobController.Load();
		return nativeMobController;
	}

	private void Load()
	{
		root = WzAssets.FileRoot("Mob/" + mob.fieldMobId.ToString("D7") + ".img");
		artRoot = WzAssets.MobArtRoot(mob.fieldMobId);
		ImgNode imgNode = root.Get("info");
		pursuitTick = Now;
		mob.nativeFloating = mob.nativeFloating || Flying;
		state = new NativeMobCastState(imgNode.Int("maxMP"));
		health = new NativeMobHealth(Convert.ToInt64(imgNode.Get("maxHP")?.Value ?? ((object)1)));
		ImgNode imgNode2 = imgNode.Get("selfDestruction");
		destructionHp = imgNode2?.Int("hp", -1) ?? (-1);
		if (imgNode2 != null)
		{
			destructionAction = (sbyte)imgNode2.Int("action", 1);
		}
		int num = imgNode2?.Int("removeAfter", -1) ?? imgNode.Int("removeAfter", -1);
		if (imgNode.Int("removeAfter") > 0 || (imgNode2 != null && destructionHp < 0))
		{
			removeAt = Now + (long)num * 1000L;
		}
		nextMpRecovery = Now + RecoveryInterval();
		blockedUntil = Now + NativeBossRules.RegenMillis(mob.fieldMobId);
		busyUntil = blockedUntil;
		ImgNode imgNode3 = imgNode.Get("revive");
		if (imgNode3 != null)
		{
			foreach (ImgNode child in imgNode3.Children)
			{
				if (child.Value != null)
				{
					revives.Add(Convert.ToInt32(child.Value));
				}
			}
		}
		ImgNode imgNode4 = imgNode.Get("skill");
		if (imgNode4 != null)
		{
			foreach (ImgNode child2 in imgNode4.Children)
			{
				int id = child2.Int("skill");
				int level = child2.Int("level");
				ImgNode imgNode5 = MobSkillNode(id, level);
				if (imgNode5 == null)
				{
					Debug.LogWarning((object)("[MobSkill] Missing " + id + "/" + level));
					continue;
				}
				Cast cast = new Cast
				{
					id = id,
					level = level,
					action = child2.Int("action", skills.Count + 1),
					mp = imgNode5.Int("mpCon"),
					hp = imgNode5.Int("hp", 100),
					x = imgNode5.Int("x", 1),
					y = imgNode5.Int("y", 1),
					count = imgNode5.Int("count", 1),
					limit = imgNode5.Int("limit"),
					probability = imgNode5.Int("prop", 100),
					duration = (long)imgNode5.Int("time") * 1000L,
					cooldown = (long)imgNode5.Int("interval") * 1000L,
					range = Range(imgNode5),
					effectAfter = child2.Int("effectAfter"),
					cast = true
				};
				if (cast.hp <= 0)
				{
					cast.hp = 100;
				}
				cast.serverDelay = ServerMillis(root.Get("skill" + cast.action));
				foreach (ImgNode child3 in imgNode5.Children)
				{
					if (int.TryParse(child3.Name, out var _) && child3.Value != null)
					{
						cast.summons.Add(Convert.ToInt32(child3.Value));
					}
				}
				skills.Add(cast);
			}
		}
		encounter.OnSpawn(mob);
	}

	private static ImgNode MobSkillNode(int id, int level)
	{
		string path = id + "/level/" + level;
		string relative = "Skill/MobSkill/" + id + ".img";
		if (WzAssets.HasFile(relative))
		{
			return WzAssets.FileRoot(relative).Get(path);
		}
		string relative2 = "Skill/MobSkill/" + id + "/" + level + ".img";
		if (WzAssets.HasFile(relative2))
		{
			return WzAssets.FileRoot(relative2).Get(path);
		}
		if (!WzAssets.HasFile("Skill/MobSkill.img"))
		{
			return null;
		}
		return WzAssets.FileRoot("Skill/MobSkill.img").Get(path);
	}

	public static int AnimationMillis(ImgNode frames)
	{
		int num = 0;
		foreach (ImgNode item in WzAssets.Frames(frames))
		{
			num += Math.Max(1, Math.Abs(item.Resolve().Int("delay", 100)));
		}
		return num;
	}

	public static int ServerMillis(ImgNode frames)
	{
		int num = 0;
		if (frames == null)
		{
			return 0;
		}
		foreach (ImgNode child in frames.Children)
		{
			ImgNode imgNode = child.TryResolve();
			if (imgNode != null)
			{
				num += imgNode.Int("delay");
			}
		}
		return num;
	}

	public static Nullable Range(ImgNode node)
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		node = node?.Get("range") ?? node;
		ImgNode imgNode = node?.Get("lt");
		ImgNode imgNode2 = node?.Get("rb");
		if (imgNode != null && imgNode2 != null)
		{
			return new Rect((float)imgNode.X, (float)imgNode.Y, (float)(imgNode2.X - imgNode.X), (float)(imgNode2.Y - imgNode.Y));
		}
		return null;
	}

	public static MobAttackArea? Area(ImgNode node)
	{
		if (!MobAttackArea.TryParse(node, out var area))
		{
			return null;
		}
		return area;
	}

	public static Vector2 Pixels(Vector3 world)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2(world.x * 40f, (0f - world.y) * 40f);
	}

	public bool InRange(Nullable relative, PlayerCtrl player)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		if (!relative.HasValue)
		{
			return true;
		}
		Rect value = relative.Value;
		return value.Contains(Pixels(Vector2.op_Implicit(player.FeetPos)) - Pixels(((Component)mob).transform.position));
	}

	private bool Buffed(int id)
	{
		if (buffs.TryGetValue(id, out var value))
		{
			return Now < value.until;
		}
		return false;
	}

	private void Buff(int id, int value, long duration)
	{
		buffs[id] = new BuffValue
		{
			value = value,
			until = Now + duration
		};
	}

	public void ScriptReflect(long millis)
	{
		if (!skills.Exists((Cast c) => c.id == 145))
		{
			ImgNode imgNode = WzAssets.FileRoot("Skill/MobSkill.img").Get("145/level/1");
			if (imgNode == null)
			{
				Debug.LogWarning((object)"[MobSkill] Missing 145/1 for spider web");
				return;
			}
			skills.Add(new Cast
			{
				id = 145,
				level = 1,
				x = imgNode.Int("x", 1),
				y = imgNode.Int("y", 1),
				count = imgNode.Int("count", 1),
				duration = (long)imgNode.Int("time") * 1000L,
				cooldown = (long)imgNode.Int("interval") * 1000L,
				cast = true
			});
		}
		Buff(143, 1, millis);
		Buff(144, 1, millis);
		Buff(140, 1, millis);
		Buff(141, 1, millis);
	}

	public bool ApplyShowdown(int skill, int value, long duration, bool boss)
	{
		if ((skill != 4121003 && skill != 4221003) | boss)
		{
			return false;
		}
		int num = ServerMillis(WzAssets.FileRoot("Skill/" + (skill / 10000).ToString("D3") + ".img").Get("skill/" + skill.ToString("D7"))?.Get("effect"));
		showdown = value;
		showdownUntil = Now + duration + num - 100;
		return true;
	}

	public void Dispel(int skill)
	{
		switch (skill)
		{
		case 4111004:
			Remove(new int[3] { 102, 112, 152 });
			Remove(new int[3] { 103, 113, 153 });
			break;
		case 2311001:
			Remove(new int[3] { 100, 110, 150 });
			Remove(new int[3] { 101, 111, 151 });
			Remove(new int[3] { 102, 112, 152 });
			Remove(new int[3] { 103, 113, 153 });
			break;
		case 1111007:
		case 1211009:
		case 1311007:
			switch (skill)
			{
			case 1111007:
				Remove(new int[3] { 102, 112, 152 });
				break;
			case 1211009:
				Remove(new int[3] { 101, 111, 151 });
				break;
			default:
				Remove(new int[3] { 100, 110, 150 });
				break;
			}
			if (GameServer.Local != null && GameConfigDao.GetBool(GameServer.Local.Db, "use_anti_immunity_crash"))
			{
				if (skill != 1211009 && !Buffed(143) && !Buffed(145))
				{
					Remove(new int[1] { 140 });
				}
				if (skill != 1311007 && !Buffed(144) && !Buffed(145))
				{
					Remove(new int[1] { 141 });
				}
			}
			break;
		}
		void Remove(int[] ids)
		{
			foreach (int key in ids)
			{
				buffs.Remove(key);
			}
		}
	}

	private void Update()
	{
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)mob == (Object)null || mob.dead || field.Map == null || (Object)(object)GameLoop.I == (Object)null || GameLoop.I.gameOver || GameLoop.I.state != GameLoop.GameState.Playing || PlayerCtrl.InputLocked || Time.deltaTime <= 0f)
		{
			return;
		}
		if (Now >= removeAt)
		{
			Expire(destructionAction);
			return;
		}
		if (Now >= nextMpRecovery)
		{
			nextMpRecovery = Now + RecoveryInterval();
			state.HealMp(NativeCombatData.Int(mob.CombatStats, "level", 1), NativeCombatData.Int(mob.CombatStats, "maxMP"));
		}
		if (mob.VacuumLocked || Now < blockedUntil || Busy || Now < nextDecision)
		{
			return;
		}
		PlayerCtrl player = GameLoop.I.player;
		if ((Object)(object)player == (Object)null || player.hp <= 0f)
		{
			return;
		}
		nextDecision = Now + 500;
		Direction = ((player.worldX >= ((Component)mob).transform.position.x) ? 1 : (-1));
		if (Mathf.Abs(player.worldX - ((Component)mob).transform.position.x) > mob.aggroRange && !mob.IsBoss)
		{
			return;
		}
		List<Cast> list = new List<Cast>(skills);
		for (int num = list.Count - 1; num > 0; num--)
		{
			int index = Random.Range(0, num + 1);
			Cast value = list[num];
			list[num] = list[index];
			list[index] = value;
		}
		list.Sort((Cast a, Cast b) => NativeBossRules.Priority(mob.fieldMobId, b.id).CompareTo(NativeBossRules.Priority(mob.fieldMobId, a.id)));
		foreach (Cast c in list)
		{
			if (!NativeMobCastState.HpAllows(health.Current, health.Maximum, c.hp) || (c.id >= 143 && c.id <= 145 && (Buffed(143) || Buffed(144) || Buffed(145))) || !state.TryUse(c.Key, Now, c.mp, NativeBossRules.SkillCooldown(mob.fieldMobId, c.id, c.level, c.cooldown), Time.unscaledTime < mob.SkillsSealedUntil || Buffed(157)))
			{
				continue;
			}
			busyUntil = Now + Math.Max(1, Math.Max(0, c.serverDelay));
			mob.PlayNativeAction("skill" + c.action);
			if (!Projected(c))
			{
				encounter.Schedule(mob, (c.id != 129) ? Math.Max(0, c.serverDelay) : 0, () =>
				{
					Apply(c);
				});
			}
			return;
		}
		TryAttack(player);
	}

	private void TryAttack(PlayerCtrl player)
	{
		if (Now < attackGapUntil)
		{
			return;
		}
		int i = 0;
		ImgNode imgNode;
		ImgNode data;
		while (true)
		{
			imgNode = root.Get("attack" + (i + 1)) ?? artRoot.Get("attack" + (i + 1));
			if (imgNode == null)
			{
				return;
			}
			data = imgNode.Get("info");
			if (data != null && AttackInArea(Area(data), player))
			{
				int num = NativeBossRules.AttackCooldown(mob.fieldMobId, i, data.Int("attackAfter"));
				if (state.TryUse("attack:" + i, Now, data.Int("conMP"), num, sealedSkills: false))
				{
					break;
				}
			}
			i++;
		}
		int num2 = Math.Max(0, data.Int("attackAfter"));
		if (data.Int("specialAttack") != 0)
		{
			attackGapUntil = Now + num2;
		}
		int num3 = num2;
		busyUntil = Now + Math.Max(1, Math.Max(0, ServerMillis(imgNode)));
		mob.PlayNativeAction("attack" + (i + 1));
		encounter.Schedule(mob, num3, () =>
		{
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
			if (data.Int("tremble") != 0)
			{
				HitFeel.Shake(TrembleAmplitude, 0.25f);
			}
			PlayCanvas(data, "effect", ((Component)mob).transform, ((Component)mob).transform.position, mob.BodySortingOrder + 1);
			PlayerCtrl playerCtrl = GameLoop.I?.player;
			if (!((Object)(object)playerCtrl == (Object)null) && AttackInArea(Area(data), playerCtrl))
			{
				long now = Now;
				if (NativeBossRules.MobAttackAllowed(now, playerCtrl.lastMobAttackMs))
				{
					playerCtrl.lastMobAttackMs = now;
					if (!(playerCtrl.hp <= 0f) && !(Time.unscaledTime < playerCtrl.invulnUntil))
					{
						PlayCanvas(data, "hit", null, Vector2.op_Implicit(playerCtrl.FeetPos), playerCtrl.SortingOrder + 1);
						float hp = playerCtrl.hp;
						int num4 = data.Int("fixDamR");
						if (data.Get("deadlyAttack") != null)
						{
							playerCtrl.TakeBossDamage(Mathf.Max(0f, playerCtrl.hp - 1f));
							playerCtrl.mp = Mathf.Min(playerCtrl.mp, 1f);
						}
						else if (num4 > 0)
						{
							playerCtrl.TakeBossDamage(NativeBossRules.PercentDamage((int)playerCtrl.hpMax, num4));
						}
						else
						{
							playerCtrl.TakeDamage(data.Int("PADamage", NativeCombatData.Int(mob.CombatStats, "PADamage")), Direction, data.Int("magic") != 0, NativeCombatData.Int(mob.CombatStats, "level", 1), mob, i, data.Int("mpBurn"));
						}
						if (data.Get("deadlyAttack") != null || num4 > 0)
						{
							playerCtrl.mp = Mathf.Max(0f, playerCtrl.mp - (float)data.Int("mpBurn"));
						}
						int num5 = data.Int("disease");
						int level = data.Int("level");
						if (playerCtrl.hp < hp && num5 > 0)
						{
							ImgNode imgNode2 = WzAssets.FileRoot("Skill/MobSkill.img").Get(num5 + "/level/" + level);
							if (imgNode2 != null)
							{
								Apply(new Cast
								{
									id = num5,
									level = level,
									x = imgNode2.Int("x", 1),
									y = imgNode2.Int("y", 1),
									probability = imgNode2.Int("prop", 100),
									count = imgNode2.Int("count", 1),
									duration = (long)imgNode2.Int("time") * 1000L
								});
							}
						}
					}
				}
			}
		});
	}

	private bool AttackInArea(MobAttackArea? area, PlayerCtrl player)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		if (!area.HasValue)
		{
			return false;
		}
		Vector2 val = Pixels(Vector2.op_Implicit(player.FeetPos)) - Pixels(((Component)mob).transform.position);
		if (Direction > 0 && root.Int("info/noFlip") == 0)
		{
			val.x = 0f - val.x;
		}
		return area.Value.Overlaps(val.x - 14f, val.y - 48f, val.x + 14f, val.y);
	}

	private static void PlayCanvas(ImgNode info, string node, Transform parent, Vector3 at, int order)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = info?.Get(node);
		if (imgNode != null)
		{
			NativeImgAnimation.SpawnAt(imgNode, parent, at, order);
		}
	}

	private void PlaySkillCanvas(Cast c, string node, bool follow, bool repeat, float lifetime)
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		if (c.cast && c.level > 0)
		{
			ImgNode imgNode = WzAssets.FileRoot("Skill/MobSkill.img").Get(c.id + "/level/" + c.level + "/" + node);
			if (imgNode != null)
			{
				NativeImgAnimation.SpawnAt(imgNode, follow ? ((Component)mob).transform : null, ((Component)mob).transform.position, mob.BodySortingOrder + 1, flip: false, repeat, lifetime);
			}
		}
	}

	private bool Projected(Cast c)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		int fieldMobId = mob.fieldMobId;
		if (fieldMobId == 8880830 && c.id == 128 && c.level == 1)
		{
			Fixed(1560, 100, new Rect(-600f, -1000f, 1201f, 1001f), "customSkill/karing/goongiScreenVideoLayer");
			return true;
		}
		if (fieldMobId == 8880830 && c.id == 126 && c.level == 7)
		{
			Fixed(780, 30, new Rect(-215f, -325f, 554f, 333f), null);
			return true;
		}
		if (fieldMobId == 8880837 && c.id == 128 && c.level == 2)
		{
			Fixed(2370, 25, new Rect(-800f, -800f, 1601f, 1601f), "customSkill/karing/darkPulseVideoLayer");
			return true;
		}
		if (fieldMobId == 8880831)
		{
			if (c.id != 123 || c.level != 3)
			{
				if (c.id == 120)
				{
					return c.level == 5;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private void Fixed(int delay, int percent, Nullable range, string effect)
	{
		encounter.Effect(effect);
		encounter.Schedule(mob, delay, () =>
		{
			PlayerCtrl playerCtrl = GameLoop.I?.player;
			if ((Object)(object)playerCtrl != (Object)null && InRange(range, playerCtrl))
			{
				playerCtrl.TakeBossDamage(NativeBossRules.PercentDamage((int)playerCtrl.hpMax, percent));
			}
		});
	}

	private void Apply(Cast c)
	{
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0307: Unknown result type (might be due to invalid IL or missing references)
		//IL_030c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		if (Random.value * 100f >= (float)c.probability)
		{
			return;
		}
		PlaySkillCanvas(c, "effect", follow: true, repeat: false, 0f);
		PlayerCtrl playerCtrl = GameLoop.I?.player;
		switch (c.id)
		{
		case 100:
		case 101:
		case 102:
		case 103:
		case 110:
		case 111:
		case 112:
		case 113:
		case 150:
		case 151:
		case 152:
		case 153:
		case 154:
		case 155:
		case 156:
		case 157:
			ApplyBuff(c);
			break;
		case 140:
			if (!Buffed(141))
			{
				ApplyBuff(c);
			}
			break;
		case 141:
			if (!Buffed(140))
			{
				ApplyBuff(c);
			}
			break;
		case 143:
		case 144:
		case 145:
			ApplyBuff(c);
			break;
		case 114:
			if (c.range.HasValue)
			{
				foreach (EnemyUnit item in new List<EnemyUnit>(EnemyUnit.All))
				{
					if (item.fieldMode && !item.dead)
					{
						Rect value = c.range.Value;
						if (value.Contains(Pixels(((Component)item).transform.position) - Pixels(((Component)mob).transform.position)))
						{
							item.NativeController?.Heal(c.x / 1000 * (int)(950f + 1050f * Random.value));
							item.NativeController?.state.HealMp(c.y, NativeCombatData.Int(item.CombatStats, "maxMP"));
						}
					}
				}
				break;
			}
			Heal(c.x);
			state.HealMp(c.y, NativeCombatData.Int(mob.CombatStats, "maxMP"));
			break;
		case 170:
		case 186:
		case 188:
		case 189:
		case 190:
		case 191:
		case 200:
		case 201:
		case 202:
		case 203:
			Summon(c);
			break;
		case 183:
		case 184:
		case 185:
		case 187:
		{
			string effect;
			if (c.id == 183)
			{
				effect = "customBossWill/webBurst";
			}
			else if (c.id == 184)
			{
				effect = "customBossMagnus/meteorStorm";
			}
			else
			{
				effect = ((c.id == 185) ? "customBossLucid/dreamBurst" : "customBossSeren/sacredBurst");
			}
			Fixed(1400, Math.Max(1, Math.Min(100, (c.id == 184) ? 35 : c.x)), null, effect);
			break;
		}
		case 176:
			if (mob.fieldMobId == 9421583)
			{
				Fixed(1400, 35, null, null);
				break;
			}
			encounter.Effect("customBoss/akayrum/screenCrack");
			encounter.Schedule(mob, 1100L, () =>
			{
				//IL_001d: Unknown result type (might be due to invalid IL or missing references)
				//IL_0022: Unknown result type (might be due to invalid IL or missing references)
				//IL_0027: Unknown result type (might be due to invalid IL or missing references)
				//IL_002c: Unknown result type (might be due to invalid IL or missing references)
				//IL_005c: Unknown result type (might be due to invalid IL or missing references)
				//IL_006f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0074: Unknown result type (might be due to invalid IL or missing references)
				//IL_0079: Unknown result type (might be due to invalid IL or missing references)
				PlayerCtrl playerCtrl2 = GameLoop.I?.player;
				if (!((Object)(object)playerCtrl2 == (Object)null))
				{
					Vector2 val = Pixels(Vector2.op_Implicit(playerCtrl2.FeetPos));
					int[,] array = new int[5, 3]
					{
						{ -610, -181, 115 },
						{ -305, -181, 110 },
						{ 0, -181, 150 },
						{ 315, -181, 110 },
						{ 610, -181, 115 }
					};
					for (int i = 0; i < 5; i++)
					{
						int num2 = array[i, 2] - (InputHub.Held(GameAction.Down) ? 55 : 0);
						Vector2 val2 = val - new Vector2((float)array[i, 0], (float)array[i, 1]);
						if (val2.sqrMagnitude <= (float)(num2 * num2))
						{
							playerCtrl2.TakeBossDamage(Math.Max(1, c.x));
							break;
						}
					}
				}
			});
			break;
		case 174:
		case 177:
		case 214:
		case 215:
			break;
		case 105:
		case 115:
		case 134:
		case 135:
		case 136:
		case 142:
		case 169:
			break;
		case 138:
		case 146:
		case 171:
		case 172:
			break;
		case 127:
			if ((Object)(object)playerCtrl != (Object)null && InRange(c.range, playerCtrl))
			{
				playerCtrl.DispelNativeBuffs();
			}
			break;
		case 129:
			if ((Object)(object)playerCtrl != (Object)null && InRange(c.range, playerCtrl))
			{
				int num = root.Int("info/ban/banMap/0/field", -1);
				string text = root.Text("info/ban/banMsg");
				if (num >= 0 && !string.IsNullOrEmpty(text))
				{
					NativeSystemMessage.Drop(5, text);
				}
				if (num >= 0)
				{
					NativeItemUseEffects.RememberBanish();
					field.WarpTo(num, root.Text("info/ban/banMap/0/portal", "sp"));
				}
			}
			break;
		case 131:
			PlaySkillCanvas(c, "tile", follow: false, repeat: true, (float)Math.Max(0, c.x) * 0.1f);
			break;
		case 120:
		case 121:
		case 122:
		case 123:
		case 124:
		case 125:
		case 126:
		case 128:
		case 132:
		case 133:
			if (!((Object)(object)playerCtrl == (Object)null) && InRange(c.range, playerCtrl))
			{
				bool flag = c.cast && c.range.HasValue;
				if (!flag || c.id != 128 || c.count > 0)
				{
					NativeMobDebuffs.For(playerCtrl).Apply(c.id, c.x, c.y, c.duration, flag);
				}
			}
			break;
		default:
			Debug.LogWarning((object)("[MobSkill] Source-unsupported skill " + c.Key));
			break;
		}
	}

	private void ApplyBuff(Cast c)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		if (c.range.HasValue)
		{
			foreach (EnemyUnit item in new List<EnemyUnit>(EnemyUnit.All))
			{
				if (item.fieldMode && !item.dead)
				{
					Rect value = c.range.Value;
					if (value.Contains(Pixels(((Component)item).transform.position) - Pixels(((Component)mob).transform.position)))
					{
						item.NativeController?.Buff(c.id, c.x, c.duration);
					}
				}
			}
			return;
		}
		Buff(c.id, c.x, c.duration);
	}

	private void Summon(Cast c)
	{
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		summoned.RemoveAll((EnemyUnit e) => (Object)(object)e == (Object)null || e.dead);
		if (EnemyUnit.All.Count >= 80)
		{
			return;
		}
		int num = ((field.Map.id >= 925020000 && field.Map.id <= 925033804) ? int.MaxValue : c.limit);
		int num2 = Math.Min(c.summons.Count, Math.Max(0, num - summoned.Count));
		List<int> list = new List<int>(c.summons);
		for (int num3 = list.Count - 1; num3 > 0; num3--)
		{
			int index = Random.Range(0, num3 + 1);
			int value = list[num3];
			list[num3] = list[index];
			list[index] = value;
		}
		for (int num4 = 0; num4 < num2; num4++)
		{
			Vector3 val = ((Component)mob).transform.position;
			if (list[num4] == 8500003)
			{
				val.y = 14.75f;
			}
			if (list[num4] == 8500004)
			{
				val.x += (float)Random.Range(-500, 500) / 40f;
			}
			if (list[num4] == 8510100)
			{
				if (Random.Range(0, 5) == 0)
				{
					val = new Vector3((float)(Random.Range(0, 5) + ((Random.Range(0, 2) == 1) ? 180 : 0)) / 40f, -1.95f, val.z);
				}
				else
				{
					val.x += (float)Random.Range(-500, 500) / 40f;
				}
			}
			if (field.Map.id == 220080001)
			{
				if (val.x < -22.25f)
				{
					val.x = (Mathf.Ceil(Random.value * 150f) - 890f) / 40f;
				}
				else if (val.x > 5.75f)
				{
					val.x = (230f - Mathf.Ceil(Random.value * 150f)) / 40f;
				}
			}
			if (field.Map.id == 230040420)
			{
				if (val.x < -5.975f)
				{
					val.x = (Mathf.Ceil(Random.value * 150f) - 239f) / 40f;
				}
				else if (val.x > 9.275f)
				{
					val.x = (371f - Mathf.Ceil(Random.value * 150f)) / 40f;
				}
			}
			EnemyUnit enemyUnit = field.SpawnNativeMob(list[num4], val);
			if ((Object)(object)enemyUnit != (Object)null)
			{
				summoned.Add(enemyUnit);
			}
		}
	}

	public DamageResult FilterHit(DamageResult hit, bool magic)
	{
		if (mob.dead || hit.Amount <= 0)
		{
			return hit;
		}
		if (Now < blockedUntil || root.Int("info/invincible") != 0 || encounter.BlocksDamage(mob))
		{
			return new DamageResult(0L, critical: false);
		}
		int id;
		if (magic)
		{
			id = (Buffed(144) ? 144 : 145);
		}
		else
		{
			id = (Buffed(143) ? 143 : 145);
		}
		if (Buffed(id))
		{
			foreach (Cast skill in skills)
			{
				if (skill.id == 145)
				{
					GameLoop.I?.player?.TakeBossDamage(Math.Max(0, magic ? skill.y : skill.x));
				}
			}
		}
		if (Buffed(id) || Buffed(magic ? 141 : 140))
		{
			return new DamageResult(1L, critical: false);
		}
		if (mob.IsBoss && GameSession.InGame && hit.Amount != long.MaxValue)
		{
			double multiplier = GameSession.Net.Boss(new BossRequest
			{
				characterId = GameSession.CharacterId
			}).multiplier;
			double num = Math.Floor((double)hit.Amount * multiplier + 0.5);
			long num2 = ((num >= 9.223372036854776E+18) ? long.MaxValue : ((long)num));
			if (num2 > hit.Amount)
			{
				NativeSystemMessage.Drop(5, "[BOSS伤害] 本次额外造成 +" + (num2 - hit.Amount).ToString("N0", CultureInfo.InvariantCulture) + " 伤害。");
			}
			return new DamageResult(num2, hit.Critical);
		}
		return hit;
	}

	public void BlockFor(int millis)
	{
		blockedUntil = Math.Max(blockedUntil, Now + millis);
	}

	public Dictionary<string, object> EffectiveStats(Dictionary<string, object> original)
	{
		if (original == null || buffs.Count == 0)
		{
			return original;
		}
		Dictionary<string, object> dictionary = new Dictionary<string, object>(original);
		Scale(dictionary, "PADamage", 100, 110, 150);
		Scale(dictionary, "MADamage", 101, 111, 151);
		Scale(dictionary, "PDDamage", 102, 112, 152);
		Scale(dictionary, "MDDamage", 103, 113, 153);
		if (Buffed(154))
		{
			dictionary["acc"] = buffs[154].value;
		}
		if (Buffed(155))
		{
			dictionary["eva"] = buffs[155].value;
		}
		return dictionary;
	}

	private void Scale(Dictionary<string, object> result, string key, params int[] ids)
	{
		foreach (int num in ids)
		{
			if (Buffed(num))
			{
				result[key] = (int)Math.Min(2147483647L, (long)NativeCombatData.Int(result, key) * (long)buffs[num].value / 100);
				break;
			}
		}
	}

	public void ApplyDamage(long damage)
	{
		long amount = health.Damage(damage);
		mob.hp = health.Current;
		encounter.OnDamaged(mob, amount);
		if (destructionHp >= 0 && health.Current <= destructionHp)
		{
			deathAction = destructionAction;
			health.Damage(health.Current);
			mob.hp = 0L;
		}
	}

	private static int RecoveryInterval()
	{
		if (GameServer.Local != null)
		{
			return Math.Max(1, GameConfigDao.GetInt(GameServer.Local.Db, "respawn_interval", 10000));
		}
		return 10000;
	}

	public void Heal(long amount)
	{
		long current = health.Current;
		health.Heal(amount);
		mob.TotalHealed += Math.Max(0L, health.Current - current);
		mob.hp = health.Current;
	}

	public int AbsorbMp(int percent)
	{
		if (mob.dead || mob.IsBoss)
		{
			return 0;
		}
		int num = Math.Min(state.Mp, (int)((double)NativeCombatData.Int(mob.CombatStats, "maxMP") * ((double)percent / 100.0)));
		if (num > 0)
		{
			state.TryUse("mp-eater", Now, num, 0L, sealedSkills: false);
		}
		return num;
	}

	public void RestoreFieldHp(long current)
	{
		health.Damage(Math.Max(0L, health.Current - Math.Max(1L, current)));
		mob.hp = health.Current;
	}

	public void SetHpPercent(int percent)
	{
		health.SetPercent(percent);
		mob.hp = health.Current;
	}

	public void FakeDamage(long damage)
	{
		health.Damage(Math.Min(damage, Math.Max(0L, health.Current - 1)));
		mob.hp = health.Current;
	}

	public void OnKilled()
	{
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		if (GameSession.InGame && (Object)(object)GameLoop.I?.player != (Object)null && GameLoop.I.player.hp > 0f)
		{
			BossResponse bossResponse = GameSession.Net.Boss(new BossRequest
			{
				characterId = GameSession.CharacterId,
				mobId = mob.fieldMobId,
				operation = BossOperation.RecordKill,
				encounter = killId
			});
			if (bossResponse.ok && !string.IsNullOrEmpty(bossResponse.message))
			{
				NativeSystemMessage.Drop(5, bossResponse.message);
			}
			NativeHuntRuntime.MonsterKilled(mob.fieldMobId);
			NativeTitleTrials.CountKill();
		}
		encounter.OnKilled(mob);
		if (revives.Count == 0)
		{
			return;
		}
		if (field.Map != null && revives.Contains(field.Map.timeMobId))
		{
			NativeSystemMessage.DropMap(6, field.Map.timeMobMessage);
		}
		Vector3 pos = ((Component)mob).transform.position;
		encounter.Schedule(mob, Math.Max(0, ServerDeathMillis), () =>
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			List<EnemyUnit> list = new List<EnemyUnit>();
			foreach (int revife in revives)
			{
				list.Add(field.SpawnNativeMob(revife, pos));
			}
			if (field.EventMap == null)
			{
				return;
			}
			foreach (EnemyUnit item in list)
			{
				NativeEventMap.Mob mob = NativeEventMap.FindByVisible(item);
				if (mob != null)
				{
					field.EventMap.Instance.ReviveMonster(mob.Host);
				}
			}
		}, afterDeath: true);
	}
}
