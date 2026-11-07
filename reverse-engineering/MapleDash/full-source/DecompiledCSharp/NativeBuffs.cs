using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public sealed class NativeBuffs : MonoBehaviour
{
	public sealed class Effect
	{
		public int Id;

		public int Level;

		public double Until;

		public double Started;

		public double NextRecovery;

		public double NextDragonBlood;

		public double NextBeholderHeal;

		public double NextBeholderBuff;

		public double NextArcaneDecay;

		public int DurationMs;

		public bool NoShadow;

		public readonly Dictionary<string, int> Stats = new Dictionary<string, int>();

		public int RemainingMs(double now)
		{
			return (int)Math.Min(2147483647.0, Math.Max(0.0, Math.Ceiling((Until - now) * 1000.0)));
		}

		public int Shadow(double now)
		{
			if (DurationMs >= 16)
			{
				return Mathf.Clamp(RemainingMs(now) / (DurationMs / 16), 0, 15);
			}
			return 0;
		}
	}

	private static Dictionary<string, object> contract;

	private readonly List<Effect> active = new List<Effect>();

	private readonly Dictionary<string, Effect> winners = new Dictionary<string, Effect>();

	private readonly List<Effect> visible = new List<Effect>();

	private static readonly HashSet<string> shared = new HashSet<string>
	{
		"COUPON_EXP1", "COUPON_EXP2", "COUPON_EXP3", "COUPON_EXP4", "COUPON_DRP1", "COUPON_DRP2", "COUPON_DRP3", "MESO_UP_BY_ITEM", "ITEM_UP_BY_ITEM", "RESPECT_PIMMUNE",
		"RESPECT_MIMMUNE", "DEFENSE_ATT", "DEFENSE_STATE", "WATK", "WDEF", "MATK", "MDEF", "ACC", "AVOID", "SPEED",
		"JUMP", "Damage", "NormalDamage", "FinalDamage", "IgnoreDefense", "CriticalChance", "ExpRate", "DropRate", "HP", "HPPct",
		"PAD", "MAD", "PADPct", "MADPct", "PDDPct", "STRPct", "DEXPct", "INTPct", "LUKPct", "CRIT_REINFORCE",
		"SUMMON_DURATION", "SKILL_ORDER", "ASR", "STR", "DEX", "INT", "LUK", "BossDamage", "INVINCIBLE", "STACK_BLESSING"
	};

	public static double Now => Time.realtimeSinceStartupAsDouble;

	public IReadOnlyList<Effect> Visible
	{
		get
		{
			Tick(Now);
			return visible;
		}
	}

	private static Dictionary<string, object> Contract
	{
		get
		{
			if (contract == null)
			{
				contract = (Dictionary<string, object>)MiniJson.Parse(Resources.Load<TextAsset>("NativeBuffContract").text);
			}
			return contract;
		}
	}

	public int SkillLevelBoost
	{
		get
		{
			if (!winners.TryGetValue("SKILL_ORDER", out var value) || value.Until <= Now)
			{
				return 0;
			}
			if (!value.Stats.TryGetValue("SKILL_ORDER", out var value2))
			{
				return 0;
			}
			return value2;
		}
	}

	public event Action<int> Removed;

	private static bool ConfigBool(string key, bool fallback = false)
	{
		if (GameServer.Local != null)
		{
			return GameConfigDao.GetBool(GameServer.Local.Db, key, fallback);
		}
		return fallback;
	}

	private static string Config(string key, string fallback)
	{
		if (GameServer.Local != null)
		{
			return GameConfigDao.Get(GameServer.Local.Db, key, fallback);
		}
		return fallback;
	}

	public static int SkillDuration(int id, int level)
	{
		int result = NativeSkillOverrides.DurationSeconds(id, Art.Int(SkillBook.Get(id)?.Level(level), "time")) * 1000;
		if (!ConfigBool("use_buff_everlasting"))
		{
			return result;
		}
		if (MiniJson.Parse(Config("job_buff_custom_time_list", "{\"4121006\":1800000,\"2121004\":60000,\"2221004\":60000,\"2321004\":60000}")) is Dictionary<string, object> dictionary && dictionary.TryGetValue(id.ToString(), out var value) && Convert.ToInt32(value) > 0)
		{
			return Convert.ToInt32(value);
		}
		bool flag = SkillProgress.LevelOf(id) > 0;
		if (Array.Exists(Config("job_special_buff_time_list", "3121002,3221002").Split(','), (string v) => v.Trim() == id.ToString()))
		{
			if (!flag)
			{
				return result;
			}
			return int.MaxValue;
		}
		if (Array.Exists(Config("job_buff_original_time_list", "2321005,3121002,3221002,4221006,4211005").Split(','), (string v) => v.Trim() == id.ToString()))
		{
			return result;
		}
		if (!flag)
		{
			return 1800000;
		}
		return int.MaxValue;
	}

	public static NativeBuffs For(PlayerCtrl player)
	{
		return ((Component)player).GetComponent<NativeBuffs>() ?? ((Component)player).gameObject.AddComponent<NativeBuffs>();
	}

	public int Value(string stat)
	{
		Tick(Now);
		if (!winners.TryGetValue(stat, out var value))
		{
			return 0;
		}
		return value.Stats[stat];
	}

	public static bool IsHerosWill(int id)
	{
		if (id != 400001009)
		{
			return ((List<object>)Contract["herosWill"]).Exists((object v) => Convert.ToInt32(v) == id);
		}
		return true;
	}

	private static bool OverTime(int id, ImgNode root)
	{
		bool result = ((root.Get("skillType") != null) ? (root.Int("skillType") == 2) : ((root.Get("effect") != null && root.Get("hit") == null && root.Get("ball") == null) || root.Get("action/0")?.Value?.ToString() == "alert2"));
		if (root.Get("skillType") == null && ((Dictionary<string, object>)Contract["forcedOverTime"]).TryGetValue(id.ToString(), out var value))
		{
			result = (bool)value;
		}
		return result;
	}

	public static Dictionary<string, int> SkillStats(int id, int level)
	{
		ImgNode imgNode = WzAssets.Node("Skill/" + (id / 10000).ToString("D3") + ".img/skill/" + id.ToString("D7"));
		Dictionary<string, object> dictionary = SkillBook.Get(id)?.Level(level);
		Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
		if (NativeSkillOverrides.IsDurationTarget(id))
		{
			dictionary2["SUMMON_DURATION"] = 1;
		}
		if (NativeSkillStates.IsTimedEffect(id))
		{
			if (dictionary != null)
			{
				NativeSkillEffects.AddBuff(dictionary2, id, dictionary);
			}
			NativeSkillMechanics.AddTimedEffect(dictionary2, id);
			return dictionary2;
		}
		if (imgNode == null || dictionary == null)
		{
			return dictionary2;
		}
		bool flag = OverTime(id, imgNode);
		bool flag2 = ((List<object>)Contract["summons"]).Exists((object v) => Convert.ToInt32(v) == id);
		string[] array = new string[9] { "pad", "pdd", "mad", "mdd", "acc", "eva", "speed", "jump", "expBuff" };
		string[] array2 = new string[9] { "WATK", "WDEF", "MATK", "MDEF", "ACC", "AVOID", "SPEED", "JUMP", "EXP_BUFF" };
		if (flag && !flag2)
		{
			for (int num = 0; num < array.Length; num++)
			{
				int num2 = ((num < 8) ? ((short)Art.Int(dictionary, array[num])) : Art.Int(dictionary, array[num]));
				if (num2 != 0)
				{
					dictionary2[array2[num]] = num2;
				}
			}
		}
		if (flag && !flag2 && ConfigBool("use_ultra_nimble_feet") && ((List<object>)Contract["nimbleFeet"]).Exists((object v) => Convert.ToInt32(v) == id))
		{
			int num3 = Art.Int(dictionary, "speed");
			dictionary2["SPEED"] = num3 * 15;
			dictionary2["JUMP"] = num3 * 4;
		}
		int num4 = Art.Int(dictionary, "barrier");
		if (num4 != 0)
		{
			dictionary2["AURA"] = num4;
		}
		if (((Dictionary<string, object>)Contract["statups"]).TryGetValue(id.ToString(), out var value))
		{
			foreach (List<object> item in (List<object>)value)
			{
				string text = (string)item[1];
				int value2 = text switch
				{
					"x << 8 | y" => (Art.Int(dictionary, "x") << 8) | Art.Int(dictionary, "y"), 
					"iprop" => Art.Int(dictionary, "prop", 100), 
					"duration" => Art.Int(dictionary, "time") * 1000, 
					"sourceid" => id, 
					_ => int.TryParse(text, out var result) ? result : Art.Int(dictionary, text), 
				};
				dictionary2[(string)item[0]] = value2;
			}
		}
		int num5 = Art.Int(dictionary, "morph");
		if (num5 > 0)
		{
			dictionary2["MORPH"] = num5;
		}
		if (id == 400001021)
		{
			int num6 = Art.Int(dictionary, "x");
			int num7 = Art.Int(dictionary, "z");
			if (num6 > 0 && num7 > 0)
			{
				dictionary2["FinalDamage"] = num6 * num7;
			}
		}
		if (id == 400031023)
		{
			dictionary2["CRIT_REINFORCE"] = 1;
		}
		if (NativeSkillMechanics.Str(id, "kind") == "levelBoost")
		{
			int num8 = Art.Int(dictionary, NativeSkillMechanics.Str(id, "bonusField"));
			if (num8 > 0)
			{
				dictionary2["SKILL_ORDER"] = num8;
			}
		}
		if (((List<object>)Contract["mapChairs"]).Exists((object v) => Convert.ToInt32(v) == id))
		{
			dictionary2["MAP_CHAIR"] = 1;
		}
		if (dictionary2.ContainsKey("RECOVERY") && ConfigBool("use_ultra_recovery"))
		{
			dictionary2["RECOVERY"] *= 10;
		}
		NativeSkillEffects.AddBuff(dictionary2, id, dictionary);
		NativeShields.BuffInvincible(dictionary2, id);
		return dictionary2;
	}

	public bool ApplySkill(int id, int level, double now)
	{
		ImgNode imgNode = WzAssets.Node("Skill/" + (id / 10000).ToString("D3") + ".img/skill/" + id.ToString("D7"));
		bool flag = NativeSkillStates.IsTimedEffect(id);
		bool flag2 = NativeSkillOverrides.IsDurationTarget(id);
		bool flag3 = ((List<object>)Contract["summons"]).Exists((object v) => Convert.ToInt32(v) == id) | flag2;
		if (!flag && !flag2 && (imgNode == null || (!OverTime(id, imgNode) && !flag3)))
		{
			return false;
		}
		Dictionary<string, int> dictionary = SkillStats(id, level);
		if (dictionary.Count == 0)
		{
			return false;
		}
		int num = SkillDuration(id, level);
		if (num <= 0)
		{
			return false;
		}
		Effect effect = new Effect
		{
			Id = id,
			Level = level,
			DurationMs = num,
			Started = now,
			Until = now + (double)num / 1000.0,
			NextRecovery = now + (double)(ConfigBool("use_ultra_recovery") ? 2 : 5)
		};
		effect.NextDragonBlood = now + 4.0;
		effect.NextBeholderHeal = now + (double)Art.Int(LearnedRow(1320008), "x");
		effect.NextBeholderBuff = now + (double)Art.Int(LearnedRow(1320009), "x");
		effect.NextArcaneDecay = now + (double)Mathf.Max(1, Art.Int(SkillBook.Get(id)?.Level(level), "y", 10));
		foreach (KeyValuePair<string, int> item in dictionary)
		{
			effect.Stats.Add(item.Key, item.Value);
		}
		Register(effect);
		return true;
	}

	private void Register(Effect effect)
	{
		Dictionary<string, int> stats = effect.Stats;
		int id = effect.Id;
		List<int> list = new List<int>();
		active.RemoveAll((Effect e) => e.Id == id);
		foreach (KeyValuePair<string, int> item in stats)
		{
			if (shared.Contains(item.Key))
			{
				continue;
			}
			for (int num = active.Count - 1; num >= 0; num--)
			{
				if (active[num].Stats.ContainsKey(item.Key))
				{
					list.Add(active[num].Id);
					active.RemoveAt(num);
				}
			}
			break;
		}
		int val = ((GameServer.Local == null) ? 5 : GameConfigDao.GetInt(GameServer.Local.Db, "max_monitored_buff_stats", 5));
		foreach (KeyValuePair<string, int> item2 in stats)
		{
			int num2 = 0;
			Effect effect2 = null;
			foreach (Effect item3 in active)
			{
				if (item3.Stats.TryGetValue(item2.Key, out var value))
				{
					num2++;
					if (effect2 == null || value < effect2.Stats[item2.Key])
					{
						effect2 = item3;
					}
				}
			}
			if (num2 >= Math.Max(1, val) && effect2 != null)
			{
				effect2.Stats.Remove(item2.Key);
				if (effect2.Stats.Count == 0)
				{
					list.Add(effect2.Id);
					active.Remove(effect2);
				}
			}
		}
		active.Add(effect);
		Rebuild();
		foreach (int item4 in list)
		{
			Removed?.Invoke(item4);
		}
		((Component)this).GetComponent<PlayerCtrl>()?.RefreshBuffAttributes();
	}

	public void ApplySoulWeapon(int id, int level, int durationMs)
	{
		double now = Now;
		Effect effect = new Effect
		{
			Id = id,
			Level = level,
			DurationMs = durationMs,
			Started = now,
			Until = now + (double)durationMs / 1000.0
		};
		effect.Stats["SOUL_WEAPON_ACTIVE"] = 1;
		Register(effect);
	}

	public void ApplyItem(int itemId, Dictionary<string, int> stats, int durationMs, double now)
	{
		Effect effect = new Effect
		{
			Id = -itemId,
			DurationMs = durationMs,
			Started = now,
			Until = now + (double)durationMs / 1000.0
		};
		ImgNode imgNode = NativeItemSource.Node(itemId);
		effect.NoShadow = imgNode == null || imgNode.Int("info/noShadow") != 0;
		Effect effect2 = effect;
		foreach (KeyValuePair<string, int> stat in stats)
		{
			effect2.Stats.Add(stat.Key, stat.Value);
		}
		ImgNode imgNode2 = NativeItemSource.Node(itemId)?.Get("spec");
		int num = ((imgNode2 != null && imgNode2.Int("mmpRRate") > 0) ? imgNode2.Int("mmpRRate") : (imgNode2?.Int("mhpRRate") ?? 0));
		effect2.NextRecovery = now + (double)(short)(num * 100) / 1000.0;
		Register(effect2);
	}

	public void ApplyStack(int skillId, int level, Dictionary<string, int> stats, int durationMs, double now)
	{
		Effect effect = new Effect
		{
			Id = skillId,
			Level = level,
			DurationMs = durationMs,
			Started = now,
			Until = now + (double)durationMs / 1000.0
		};
		foreach (KeyValuePair<string, int> stat in stats)
		{
			effect.Stats.Add(stat.Key, stat.Value);
		}
		Register(effect);
	}

	public void Tick(double now)
	{
		bool flag = false;
		for (int num = active.Count - 1; num >= 0; num--)
		{
			if (active[num].Until <= now)
			{
				int id = active[num].Id;
				active.RemoveAt(num);
				flag = true;
				Removed?.Invoke(id);
			}
		}
		if (flag)
		{
			Rebuild();
			((Component)this).GetComponent<PlayerCtrl>()?.RefreshBuffAttributes();
		}
	}

	private void Rebuild()
	{
		winners.Clear();
		visible.Clear();
		bool flag = ConfigBool("use_buff_most_significant", fallback: true);
		foreach (Effect item in active)
		{
			foreach (KeyValuePair<string, int> stat in item.Stats)
			{
				if (!flag || !winners.TryGetValue(stat.Key, out var value) || stat.Value > value.Stats[stat.Key] || (stat.Value == value.Stats[stat.Key] && item.Stats.Count >= value.Stats.Count))
				{
					winners[stat.Key] = item;
				}
			}
		}
		foreach (Effect item2 in active)
		{
			foreach (Effect value2 in winners.Values)
			{
				if (value2 == item2)
				{
					if (item2.Id != 5211006 && item2.Id != 5220011 && item2.Id != 22151002)
					{
						visible.Add(item2);
					}
					break;
				}
			}
		}
	}

	public bool Cancel(int id)
	{
		if (active.RemoveAll((Effect e) => e.Id == id) == 0)
		{
			return false;
		}
		Rebuild();
		Removed?.Invoke(id);
		((Component)this).GetComponent<PlayerCtrl>()?.RefreshBuffAttributes();
		return true;
	}

	public bool HasSource(int id)
	{
		Tick(Now);
		return active.Exists((Effect e) => e.Id == id);
	}

	private static Dictionary<string, object> LearnedRow(int id)
	{
		int num = SkillProgress.LevelOf(id);
		if (num <= 0)
		{
			return null;
		}
		return SkillBook.Get(id)?.Level(num);
	}

	public static int AttackSpeed(int weaponSpeed, int booster, int infusion, int skill = 0)
	{
		return Math.Max(2, Math.Min(10, weaponSpeed + booster + infusion - ((skill == 4001334) ? 2 : 0)));
	}

	public int AttackSpeed(int weaponSpeed, int skill = 0)
	{
		return AttackSpeed(weaponSpeed, Value("BOOSTER"), Value("SPEED_INFUSION"), skill);
	}

	public void Dispel()
	{
		if (ConfigBool("use_undispel_holy_shield") && HasSource(2321005))
		{
			return;
		}
		List<int> list = new List<int>();
		foreach (Effect item in active)
		{
			if (item.Id > 0 && item.Id != 21000000)
			{
				list.Add(item.Id);
			}
		}
		foreach (int item2 in list)
		{
			Cancel(item2);
		}
	}

	public bool HasStat(string stat)
	{
		Tick(Now);
		return winners.ContainsKey(stat);
	}

	public bool SuppressesContact(int mobId, int fieldLimit)
	{
		if ((fieldLimit & 2) == 0 && mobId != 9400594)
		{
			if (Value("DARKSIGHT") == 0)
			{
				return Value("WIND_WALK") != 0;
			}
			return true;
		}
		return false;
	}

	public static bool ResistsKnockback(int stance, int fieldLimit, int roll)
	{
		if ((fieldLimit & 2) == 0)
		{
			return roll < Math.Max(0, Math.Min(100, stance));
		}
		return false;
	}

	public int SourceOf(string stat)
	{
		Tick(Now);
		if (!winners.TryGetValue(stat, out var value))
		{
			return 0;
		}
		return value.Id;
	}

	public void OnRangedAttack(int targets)
	{
		if (targets > 0)
		{
			string text;
			if (SkillProgress.LevelOf(14100005) > 0 && HasStat("DARKSIGHT") && SourceOf("DARKSIGHT") != 9101004)
			{
				text = "DARKSIGHT";
			}
			else
			{
				text = ((SkillProgress.LevelOf(13101006) > 0 && HasStat("WIND_WALK")) ? "WIND_WALK" : null);
			}
			if (text != null)
			{
				RemoveAttackHidden(text);
			}
		}
	}

	public void OnCloseAttack()
	{
		string text;
		if ((SkillProgress.LevelOf(14100005) > 0 || SkillProgress.LevelOf(4001003) > 0) && HasStat("DARKSIGHT"))
		{
			text = "DARKSIGHT";
		}
		else
		{
			text = ((SkillProgress.LevelOf(13101006) > 0 && HasStat("WIND_WALK")) ? "WIND_WALK" : null);
		}
		if (text != null)
		{
			RemoveAttackHidden(text);
		}
	}

	private void RemoveAttackHidden(string stat)
	{
		CancelStat(stat);
		List<int> list = new List<int>();
		foreach (Effect item in active)
		{
			item.Stats.Remove(stat);
			if (item.Stats.Count == 0)
			{
				list.Add(item.Id);
			}
		}
		foreach (int item2 in list)
		{
			Cancel(item2);
		}
		Rebuild();
		((Component)this).GetComponent<PlayerCtrl>()?.RefreshBuffAttributes();
	}

	public static int PowerGuardDamage(int incoming, int percent, long monsterHp, bool boss)
	{
		return (int)Math.Max(0L, Math.Min(monsterHp / 10, (int)((double)incoming * ((double)percent / (boss ? 200.0 : 100.0)))));
	}

	public static int PassiveIncoming(int incoming, int barrier, int achilles, int highDefense)
	{
		if (barrier != 0)
		{
			incoming = (int)((double)incoming * ((double)barrier / 1000.0));
		}
		if (achilles != 0)
		{
			incoming = (int)((double)incoming * ((double)achilles / 1000.0));
		}
		if (highDefense != 0)
		{
			incoming = (int)((double)incoming * Math.Ceiling((double)highDefense / 1000.0));
		}
		return Math.Max(0, incoming);
	}

	public void CancelStat(string stat)
	{
		Tick(Now);
		if (winners.TryGetValue(stat, out var value))
		{
			Cancel(value.Id);
		}
	}

	public static void GuardLoss(int damage, int availableMp, long availableMeso, int magicGuard, int mesoGuard, bool hasMagicGuard, bool hasMesoGuard, int mpAttack, out int hpLoss, out int mpLoss, out long mesoLoss, out bool cancelMeso)
	{
		hpLoss = damage;
		mpLoss = mpAttack;
		mesoLoss = 0L;
		cancelMeso = false;
		if (hasMagicGuard && mpAttack == 0)
		{
			mpLoss = Math.Min(availableMp, (int)((double)damage * ((double)magicGuard / 100.0)));
			hpLoss = damage - mpLoss;
		}
		else if (hasMesoGuard)
		{
			hpLoss = damage / 2;
			long num = (long)((double)hpLoss * ((double)mesoGuard / 100.0));
			cancelMeso = availableMeso < num;
			mesoLoss = Math.Min(availableMeso, num);
		}
	}

	public bool CancelByUser(int id)
	{
		if (id == 1311006 || (id == 5101007 && Value("ENERGY_CHARGE") > 0))
		{
			return false;
		}
		return Cancel(id);
	}

	public void Clear()
	{
		List<int> list = new List<int>();
		foreach (Effect item in active)
		{
			list.Add(item.Id);
		}
		active.Clear();
		Rebuild();
		foreach (int item2 in list)
		{
			Removed?.Invoke(item2);
		}
		((Component)this).GetComponent<PlayerCtrl>()?.RefreshBuffAttributes();
	}

	private void Update()
	{
		PlayerCtrl component = ((Component)this).GetComponent<PlayerCtrl>();
		if ((Object)(object)component != (Object)null && component.hp <= 0f)
		{
			if (active.Count > 0)
			{
				Clear();
			}
		}
		else
		{
			TickPeriodic(component, Now);
		}
	}

	public void TickPeriodic(PlayerCtrl player, double now)
	{
		Tick(now);
		if ((Object)(object)player == (Object)null || player.hp <= 0f)
		{
			return;
		}
		if (winners.TryGetValue("RECOVERY", out var value) && now >= value.NextRecovery)
		{
			value.NextRecovery = now + (double)(ConfigBool("use_ultra_recovery") ? 2 : 5);
			player.hp = Mathf.Min(player.hpMax, player.hp + (float)(sbyte)value.Stats["RECOVERY"]);
		}
		Effect effect = null;
		if (winners.TryGetValue("HPREC", out var value2))
		{
			effect = value2;
		}
		if (winners.TryGetValue("MPREC", out var value3) && (effect == null || value3.Started > effect.Started))
		{
			effect = value3;
		}
		if (effect != null && now >= effect.NextRecovery)
		{
			ImgNode imgNode = NativeItemSource.Node(-effect.Id)?.Get("spec");
			int num = ((imgNode != null && imgNode.Int("mmpRRate") > 0) ? imgNode.Int("mmpRRate") : (imgNode?.Int("mhpRRate") ?? 0));
			if (num > 0)
			{
				effect.NextRecovery = now + (double)(short)(num * 100) / 1000.0;
				player.hp = Mathf.Clamp(player.hp + (float)Value("HPREC"), 0f, player.hpMax);
				player.mp = Mathf.Clamp(player.mp + (float)Value("MPREC"), 0f, player.mpMax);
			}
		}
		if (winners.TryGetValue("DRAGONBLOOD", out var value4) && now >= value4.NextDragonBlood)
		{
			value4.NextDragonBlood = now + 4.0;
			player.hp = Mathf.Clamp(player.hp - (float)value4.Stats["DRAGONBLOOD"], 0f, player.hpMax);
			if (player.hp <= 0f)
			{
				Clear();
				GameLoop.I?.OnPlayerDead();
				return;
			}
		}
		Effect effect2 = active.Find((Effect e) => e.Id == 400001021);
		if (effect2 != null && now >= effect2.NextArcaneDecay)
		{
			Dictionary<string, object> d = LearnedRow(400001021);
			int num2 = Mathf.Max(1, Art.Int(d, "z", 1));
			effect2.NextArcaneDecay = now + (double)Mathf.Max(1, Art.Int(d, "y", 10));
			if (effect2.Stats.TryGetValue("FinalDamage", out var value5) && value5 > 0)
			{
				effect2.Stats["FinalDamage"] = Mathf.Max(0, value5 - num2);
				Rebuild();
				player.RefreshBuffAttributes();
			}
		}
		Effect effect3 = active.Find((Effect e) => e.Id == 1321007);
		if (effect3 != null)
		{
			Dictionary<string, object> dictionary = LearnedRow(1320008);
			if (dictionary != null && now >= effect3.NextBeholderHeal)
			{
				effect3.NextBeholderHeal = now + (double)Art.Int(dictionary, "x");
				player.hp = Mathf.Clamp(player.hp + (float)Art.Int(dictionary, "hp"), 0f, player.hpMax);
			}
			Dictionary<string, object> dictionary2 = LearnedRow(1320009);
			if (dictionary2 != null && now >= effect3.NextBeholderBuff)
			{
				effect3.NextBeholderBuff = now + (double)Art.Int(dictionary2, "x");
				ApplySkill(1320009, SkillProgress.LevelOf(1320009), now);
			}
		}
	}
}
