using System.Collections.Generic;
using UnityEngine;

public static class NativeQuiverBurst
{
	public const int Skill = 3141009;

	public const int MagicShot = 3140010;

	public const int DrainShot = 3140011;

	private const int FallbackCapacity = 50;

	private const int FallbackThreshold = 50;

	private static double until = -99.0;

	private static int loads;

	private static NativeSkillDefinition magicDef;

	private static NativeSkillDefinition drainDef;

	private static int magicPercent;

	private static int drainPercent;

	public static bool Active => Time.realtimeSinceStartupAsDouble < until;

	public static int Loads => loads;

	public static float Remaining => Mathf.Max(0f, (float)(until - Time.realtimeSinceStartupAsDouble));

	public static int Capacity
	{
		get
		{
			int level = LevelOf(3141009, 1);
			return Mathf.Max(1, Mathf.RoundToInt(SkillBook.Common(3141009, "w2", level, 50f)));
		}
	}

	public static int Threshold
	{
		get
		{
			int level = LevelOf(3141009, 1);
			return Mathf.Max(1, Mathf.RoundToInt(SkillBook.Common(3141009, "w", level, 50f)));
		}
	}

	public static bool IsSkill(int id)
	{
		return id == 3141009;
	}

	private static int LevelOf(int id, int fallback)
	{
		int num = SkillProgress.LevelOf(id);
		if (num <= 0)
		{
			return Mathf.Max(1, fallback);
		}
		return num;
	}

	public static void Reset()
	{
		until = -99.0;
		loads = 0;
	}

	public static void Activate(int level)
	{
		float num = SkillBook.Common(3141009, "time", Mathf.Max(1, level), 30f);
		until = Time.realtimeSinceStartupAsDouble + (double)Mathf.Max(1f, num);
	}

	public static bool Load(PlayerCtrl player, Component target, int direction, bool magic)
	{
		if (SkillProgress.LevelOf(3141009) <= 0)
		{
			return false;
		}
		int capacity = Capacity;
		if (loads < capacity)
		{
			loads++;
		}
		if (loads < Threshold)
		{
			return false;
		}
		return FullBurst(player, target, direction, magic);
	}

	public static bool TryFire(PlayerCtrl player, Component target, int direction, bool magic)
	{
		if (!Active || loads <= 0 || (Object)(object)player == (Object)null || (Object)(object)target == (Object)null)
		{
			return false;
		}
		loads--;
		return Shoot(player, target, direction, magic, burst: false);
	}

	private static bool FullBurst(PlayerCtrl player, Component target, int direction, bool magic)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		int num = loads;
		loads = 0;
		if (num <= 0)
		{
			return false;
		}
		NativeSkillDefinition nativeSkillDefinition = Definition(magic);
		if (nativeSkillDefinition == null)
		{
			return false;
		}
		Vector3 feet = Vector2.op_Implicit(player.FeetPos);
		int num2 = 0;
		foreach (EnemyUnit item in EnemyUnit.All)
		{
			if (!((Object)(object)item == (Object)null) && !item.dead && InBurst(feet, direction, (Component)(object)item) && Settle(player, item, null, nativeSkillDefinition, direction, magic))
			{
				num2++;
			}
		}
		BossUnit[] array = Object.FindObjectsOfType<BossUnit>();
		foreach (BossUnit bossUnit in array)
		{
			if (!((Object)(object)bossUnit == (Object)null) && !bossUnit.dead && InBurst(feet, direction, (Component)(object)bossUnit) && Settle(player, null, bossUnit, nativeSkillDefinition, direction, magic))
			{
				num2++;
			}
		}
		if (num2 == 0)
		{
			return false;
		}
		PlayBurst(player, direction, magic);
		return true;
	}

	private static bool InBurst(Vector3 feet, int direction, Component target)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = SkillBook.CommonVector(3140010, "lt", new Vector2(-700f, -350f));
		Vector2 val2 = SkillBook.CommonVector(3140010, "rb", new Vector2(100f, 350f));
		float num = feet.x + val2.x / 40f * (float)direction;
		float num2 = feet.x + val.x / 40f * (float)direction;
		Rect val3 = Rect.MinMaxRect(Mathf.Min(num, num2), feet.y - val2.y / 40f, Mathf.Max(num, num2), feet.y - val.y / 40f);
		EnemyUnit enemyUnit = target as EnemyUnit;
		Rect val4 = (Rect)(((Object)(object)enemyUnit != (Object)null && enemyUnit.SelectionBounds.HasValue) ? enemyUnit.SelectionBounds.Value : new Rect(target.transform.position.x, target.transform.position.y, 0f, 0f));
		return val3.Overlaps(val4);
	}

	private static bool Shoot(PlayerCtrl player, Component target, int direction, bool magic, bool burst)
	{
		NativeSkillDefinition nativeSkillDefinition = Definition(magic);
		if (nativeSkillDefinition == null)
		{
			return false;
		}
		EnemyUnit enemy = target as EnemyUnit;
		BossUnit boss = target as BossUnit;
		if (!Settle(player, enemy, boss, nativeSkillDefinition, direction, magic))
		{
			return false;
		}
		PlayHit(player, target, direction, magic);
		return true;
	}

	private static bool Settle(PlayerCtrl player, EnemyUnit enemy, BossUnit boss, NativeSkillDefinition definition, int direction, bool magic)
	{
		Dictionary<string, object> dictionary;
		if ((Object)(object)enemy != (Object)null)
		{
			dictionary = enemy.CombatStats;
		}
		else
		{
			dictionary = (((Object)(object)boss != (Object)null) ? boss.CombatStats : null);
		}
		if (dictionary == null)
		{
			return false;
		}
		int ordinarySkill = (magic ? 3140010 : 3140011);
		DamageResult damageResult = player.RollAttack(dictionary, definition);
		if ((Object)(object)enemy != (Object)null)
		{
			enemy.TakeReplayHit(new DamageResult[1] { damageResult }, direction, magic: false, indexed: false, 120, total: false, ordinarySkill);
		}
		else
		{
			boss.TakeReplayHit(new DamageResult[1] { damageResult }, direction, indexed: false, 120, total: false, ordinarySkill);
		}
		return true;
	}

	private static void PlayHit(PlayerCtrl player, Component target, int direction, bool magic)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		int id = (magic ? 3140010 : 3140011);
		int level = LevelOf(id, Mathf.Max(1, SkillProgress.LevelOf(3141009)));
		string key = NativeSkillVfx.Key(id, level);
		List<NativeVfxClip> list = NativeSkillVfx.Parts(key, "hit");
		if (list.Count != 0)
		{
			Vector3 anchor = (Vector3)(((Object)(object)target != (Object)null) ? new Vector3(target.transform.position.x, target.transform.position.y + 0.75f, 0f) : Vector2.op_Implicit(player.FeetPos));
			SkillFx.PlayPart(key, list[Random.Range(0, list.Count)].path, anchor, direction, 1f, player.SortingOrder + 2);
		}
	}

	private static void PlayBurst(PlayerCtrl player, int direction, bool magic)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		int id = (magic ? 3140010 : 3140011);
		int level = LevelOf(id, Mathf.Max(1, SkillProgress.LevelOf(3141009)));
		string key = NativeSkillVfx.Key(id, level);
		List<NativeVfxClip> list = NativeSkillVfx.Parts(key, "effect");
		if (list.Count != 0)
		{
			Vector3 anchor = Vector2.op_Implicit(player.FeetPos) + Vector3.right * (7.5f * (float)direction);
			SkillFx.PlayPart(key, list[0].path, anchor, direction, 1f, player.SortingOrder + 2);
			AudioManager.Play("Skill/" + 3141009 + "/Use", 0.9f);
		}
	}

	private static NativeSkillDefinition Definition(bool magic)
	{
		int num = (magic ? 3140010 : 3140011);
		int level = LevelOf(num, Mathf.Max(1, SkillProgress.LevelOf(3141009)));
		int num2 = Mathf.RoundToInt(SkillBook.Common(num, "damage", level));
		if (num2 <= 0)
		{
			return null;
		}
		if (magic && magicDef != null && magicPercent == num2)
		{
			return magicDef;
		}
		if (!magic && drainDef != null && drainPercent == num2)
		{
			return drainDef;
		}
		Dictionary<string, object> skill = new Dictionary<string, object>
		{
			{ "id", num },
			{ "kind", "Attack" },
			{ "magic", false },
			{ "summonInterval", 1f }
		};
		Dictionary<string, object> level2 = new Dictionary<string, object>
		{
			{ "level", 1 },
			{ "damage", num2 },
			{ "attackCount", 1 },
			{ "mobCount", 1 }
		};
		NativeSkillDefinition result = new NativeSkillDefinition(skill, level2);
		if (magic)
		{
			magicDef = result;
			magicPercent = num2;
		}
		else
		{
			drainDef = result;
			drainPercent = num2;
		}
		return result;
	}
}
