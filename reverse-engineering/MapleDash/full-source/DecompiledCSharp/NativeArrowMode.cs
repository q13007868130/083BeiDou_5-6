using System.Collections.Generic;
using UnityEngine;

public static class NativeArrowMode
{
	public const int Toggle = 3141005;

	public const int MagicChild = 3140007;

	public const int DrainChild = 3140008;

	public const int MagicBody = 3140006;

	public const int MagicShotFx = 3140010;

	public const int DrainShotFx = 3140011;

	public const int Off = 0;

	public const int Magic = 1;

	public const int Drain = 2;

	private static int mode;

	private static double switchedAt = -99.0;

	private static NativeSkillDefinition magicDef;

	private static int magicDefPercent;

	public static int Mode => mode;

	public static bool Active => mode != 0;

	public static bool IsMagic => mode == 1;

	public static string Label
	{
		get
		{
			if (mode != 1)
			{
				if (mode != 2)
				{
					return "普通箭镞";
				}
				return "吸血箭镞";
			}
			return "魔法箭镞";
		}
	}

	public static int ActiveFx
	{
		get
		{
			if (mode != 2)
			{
				return 3140010;
			}
			return 3140011;
		}
	}

	public static bool CooledDown => Time.realtimeSinceStartupAsDouble - switchedAt > 0.35;

	public static bool IsToggle(int id)
	{
		return id == 3141005;
	}

	public static void Reset()
	{
		mode = 0;
		switchedAt = -99.0;
	}

	public static void Sync()
	{
		int num = 0;
		FullCharacter character = GameSession.Character;
		if (character != null && character.growth != null)
		{
			num = character.growth.quiverMode;
		}
		else
		{
			DbStore dbStore = GameServer.Local?.Db;
			if (dbStore != null && GameSession.CharacterId > 0)
			{
				num = NativeArrowModeService.Get(dbStore, GameSession.CharacterId);
			}
		}
		mode = ((num >= 0 && num <= 2) ? num : 0);
		switchedAt = -99.0;
		NativeQuiverBurst.Reset();
	}

	private static void Persist()
	{
		FullCharacter character = GameSession.Character;
		if (character != null && character.growth != null)
		{
			character.growth.quiverMode = mode;
		}
		DbStore dbStore = GameServer.Local?.Db;
		if (dbStore != null && GameSession.CharacterId > 0)
		{
			NativeArrowModeService.Set(dbStore, GameSession.CharacterId, mode);
		}
	}

	public static bool Stats(out int magicChance, out int magicDamage, out int drainChance, out float drainHeal)
	{
		magicChance = 0;
		magicDamage = 0;
		drainChance = 0;
		drainHeal = 0f;
		int num = SkillProgress.LevelOf(3141005);
		if (num <= 0)
		{
			return false;
		}
		magicChance = Mathf.RoundToInt(SkillBook.Common(3141005, "u", num, 60f));
		magicDamage = Mathf.RoundToInt(SkillBook.Common(3141005, "s", num));
		drainChance = Mathf.RoundToInt(SkillBook.Common(3141005, "w", num, 60f));
		drainHeal = SkillBook.Common(3141005, "t", num, 1.8f);
		if (magicChance <= 0)
		{
			return drainChance > 0;
		}
		return true;
	}

	public static string Switch(PlayerCtrl player)
	{
		mode = ((mode != 1) ? 1 : 2);
		switchedAt = Time.realtimeSinceStartupAsDouble;
		PlaySwitch(player);
		Persist();
		return "箭镞：" + ((mode == 1) ? "魔法箭" : "吸血箭");
	}

	private static void PlaySwitch(PlayerCtrl player)
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)player == (Object)null)
		{
			return;
		}
		int level = Mathf.Max(1, SkillProgress.LevelOf(3141005));
		string key = NativeSkillVfx.Key(3141005, level);
		string branch = "mode/" + ((mode != 1) ? 1 : 0);
		foreach (NativeVfxClip item in NativeSkillVfx.Parts(key, branch))
		{
			SkillFx.PlayPart(key, item.path, Vector2.op_Implicit(player.FeetPos), player.facing, 1f, player.SortingOrder + 1);
		}
		AudioManager.Play("Skill/" + 3141005 + "/Use", 0.9f);
	}

	public static bool OnHit(PlayerCtrl player, Component target, int direction)
	{
		if (!Active || (Object)(object)player == (Object)null || (Object)(object)target == (Object)null)
		{
			return false;
		}
		if (!Stats(out var magicChance, out var magicDamage, out var drainChance, out var drainHeal))
		{
			return false;
		}
		bool flag = mode == 1;
		bool flag2;
		if (flag)
		{
			int damagePercent = ((magicDamage > 0) ? magicDamage : MagicBowPercent());
			flag2 = magicChance > 0 && Random.value * 100f < (float)magicChance && FireMagicArrow(player, target, direction, damagePercent);
		}
		else
		{
			flag2 = drainChance > 0 && Random.value * 100f < (float)drainChance && DrainLife(player, target, direction, drainHeal);
		}
		if (flag2)
		{
			NativeQuiverBurst.Load(player, target, direction, flag);
		}
		NativeQuiverBurst.TryFire(player, target, direction, flag);
		return flag2;
	}

	private static int MagicBowPercent()
	{
		int level = Mathf.Max(1, SkillProgress.LevelOf(3140006));
		return Mathf.RoundToInt(SkillBook.Common(3140006, "damage", level));
	}

	private static bool FireMagicArrow(PlayerCtrl player, Component target, int direction, int damagePercent)
	{
		NativeSkillDefinition nativeSkillDefinition = MagicDefinition(damagePercent);
		if (nativeSkillDefinition == null)
		{
			return false;
		}
		EnemyUnit enemyUnit = target as EnemyUnit;
		BossUnit bossUnit = target as BossUnit;
		Dictionary<string, object> dictionary;
		if ((Object)(object)enemyUnit != (Object)null)
		{
			dictionary = enemyUnit.CombatStats;
		}
		else
		{
			dictionary = (((Object)(object)bossUnit != (Object)null) ? bossUnit.CombatStats : null);
		}
		if (dictionary == null)
		{
			return false;
		}
		DamageResult damageResult = player.RollAttack(dictionary, nativeSkillDefinition);
		if ((Object)(object)enemyUnit != (Object)null)
		{
			enemyUnit.TakeReplayHit(new DamageResult[1] { damageResult }, direction, magic: false, indexed: false, 120, total: false, 3140010);
		}
		else if ((Object)(object)bossUnit != (Object)null)
		{
			bossUnit.TakeReplayHit(new DamageResult[1] { damageResult }, direction, indexed: false, 120, total: false, 3140010);
		}
		PlayShotFx(player, target, direction, 3140010);
		return true;
	}

	private static bool DrainLife(PlayerCtrl player, Component target, int direction, float healPercent)
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		int num = Mathf.Max(1, Mathf.RoundToInt(player.hpMax * healPercent / 100f));
		float hp = player.hp;
		player.hp = Mathf.Min(player.hpMax, player.hp + (float)num);
		int num2 = Mathf.RoundToInt(player.hp - hp);
		PlayShotFx(player, target, direction, 3140011);
		if (num2 > 0)
		{
			DamageText.Show(Vector2.op_Implicit(player.FeetPos) + Vector3.up * 1.5f, num2, "blue");
		}
		return true;
	}

	private static void PlayShotFx(PlayerCtrl player, Component target, int direction, int fxId)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		int level = Mathf.Max(1, SkillProgress.LevelOf(fxId));
		string key = NativeSkillVfx.Key(fxId, level);
		List<NativeVfxClip> list = NativeSkillVfx.Parts(key, "hit");
		if (list.Count != 0)
		{
			Vector3 anchor = (Vector3)(((Object)(object)target != (Object)null) ? new Vector3(target.transform.position.x, target.transform.position.y + 0.75f, 0f) : Vector2.op_Implicit(player.FeetPos));
			SkillFx.PlayPart(key, list[Random.Range(0, list.Count)].path, anchor, direction, 1f, player.SortingOrder + 2);
		}
	}

	private static NativeSkillDefinition MagicDefinition(int damagePercent)
	{
		if (damagePercent <= 0)
		{
			return null;
		}
		if (magicDef != null && magicDefPercent == damagePercent)
		{
			return magicDef;
		}
		Dictionary<string, object> skill = new Dictionary<string, object>
		{
			{ "id", 3140006 },
			{ "kind", "Attack" },
			{ "magic", false },
			{ "summonInterval", 1f }
		};
		Dictionary<string, object> level = new Dictionary<string, object>
		{
			{ "level", 1 },
			{ "damage", damagePercent },
			{ "attackCount", 1 },
			{ "mobCount", 1 }
		};
		magicDef = new NativeSkillDefinition(skill, level);
		magicDefPercent = damagePercent;
		return magicDef;
	}
}
