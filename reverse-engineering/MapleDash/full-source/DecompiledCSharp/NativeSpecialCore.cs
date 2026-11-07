using System;
using System.Collections.Generic;
using UnityEngine;

public static class NativeSpecialCore
{
	private sealed class Entry
	{
		public int id;

		public string name = "";

		public string condType = "";

		public int count;

		public double cooldown;

		public double validtime;

		public float prob;

		public string effectType = "";

		public string stat = "";

		public int value;

		public double duration;
	}

	private static Dictionary<int, Entry> table;

	private static int activeId;

	private static int counter;

	private static int mobKey;

	private static double lastEvent = -99.0;

	private static double coolUntil = -99.0;

	private static double buffUntil = -99.0;

	private static string buffStat = "";

	private static int buffValue;

	private static int comboKills;

	private static double lastComboKill = -99.0;

	private static double Now => Time.realtimeSinceStartupAsDouble;

	public static int ActiveId => activeId;

	public static bool Ready
	{
		get
		{
			if (table != null)
			{
				return table.ContainsKey(activeId);
			}
			return false;
		}
	}

	private static Entry Current
	{
		get
		{
			Ensure();
			if (activeId == 0 || table == null)
			{
				return null;
			}
			if (!table.TryGetValue(activeId, out var value))
			{
				return null;
			}
			return value;
		}
	}

	public static int DamagePercent
	{
		get
		{
			if (!BuffActive("damage"))
			{
				return 0;
			}
			return buffValue;
		}
	}

	public static int BossDamagePercent
	{
		get
		{
			if (!BuffActive("bossDamage"))
			{
				return 0;
			}
			return buffValue;
		}
	}

	public static int IgnoreDefensePercent
	{
		get
		{
			if (!BuffActive("ignoreDefense"))
			{
				return 0;
			}
			return buffValue;
		}
	}

	public static int CriticalPercent
	{
		get
		{
			if (!BuffActive("critical"))
			{
				return 0;
			}
			return buffValue;
		}
	}

	public static int ExpPercent
	{
		get
		{
			if (!BuffActive("exp"))
			{
				return 0;
			}
			return buffValue;
		}
	}

	public static bool Invincible => BuffActive("invincible");

	public static float Remaining
	{
		get
		{
			if (Now >= buffUntil)
			{
				return 0f;
			}
			return (float)(buffUntil - Now);
		}
	}

	private static void Ensure()
	{
		if (table != null)
		{
			return;
		}
		table = new Dictionary<int, Entry>();
		TextAsset val = Resources.Load<TextAsset>("UI/SpecialCores");
		if ((Object)(object)val == (Object)null || !(MiniJson.Parse(val.text) is Dictionary<string, object> dictionary) || !dictionary.TryGetValue("cores", out var value) || !(value is Dictionary<string, object> dictionary2))
		{
			return;
		}
		foreach (KeyValuePair<string, object> item in dictionary2)
		{
			if (item.Value is Dictionary<string, object> dictionary3)
			{
				Entry entry = new Entry
				{
					id = Int(dictionary3, "id"),
					name = Text(dictionary3, "name"),
					effectType = Text(dictionary3, "effectType")
				};
				if (dictionary3.TryGetValue("cond", out var value2) && value2 is Dictionary<string, object> node)
				{
					entry.condType = Text(node, "type");
					entry.count = Int(node, "count");
					entry.cooldown = (double)Int(node, "cooltime") / 1000.0;
					entry.validtime = (double)Int(node, "validtime") / 1000.0;
					entry.prob = (float)Real(node, "prob");
				}
				if (dictionary3.TryGetValue("effect", out var value3) && value3 is Dictionary<string, object> node2)
				{
					entry.stat = Text(node2, "stat");
					entry.value = Int(node2, "value");
					entry.duration = (double)Int(node2, "duration") / 1000.0;
				}
				table[entry.id] = entry;
			}
		}
	}

	private static string Text(Dictionary<string, object> node, string key)
	{
		if (!node.TryGetValue(key, out var value))
		{
			return "";
		}
		return (value as string) ?? "";
	}

	private static int Int(Dictionary<string, object> node, string key)
	{
		if (!node.TryGetValue(key, out var value) || value == null)
		{
			return 0;
		}
		return Convert.ToInt32(value);
	}

	private static double Real(Dictionary<string, object> node, string key)
	{
		if (!node.TryGetValue(key, out var value) || value == null)
		{
			return 0.0;
		}
		return Convert.ToDouble(value);
	}

	public static void Observe(VMatrixResponse response)
	{
		Ensure();
		int num = activeId;
		activeId = 0;
		if (response != null && response.cores != null)
		{
			foreach (VMatrixCoreView core in response.cores)
			{
				if (core != null && core.selected && core.type == 2 && core.level > 0)
				{
					activeId = core.coreId;
					break;
				}
			}
		}
		if (activeId != num)
		{
			ResetProgress();
		}
	}

	public static void Sync()
	{
		if (GameSession.InGame && GameSession.Net != null && GameSession.CharacterId > 0)
		{
			Observe(GameSession.Net.VMatrix(new VMatrixRequest
			{
				characterId = GameSession.CharacterId,
				operation = VMatrixOperation.Snapshot
			}));
		}
	}

	private static void ResetProgress()
	{
		counter = 0;
		mobKey = 0;
		lastEvent = -99.0;
		coolUntil = -99.0;
	}

	public static void Reset()
	{
		comboKills = 0;
		lastComboKill = -99.0;
		activeId = 0;
		ResetProgress();
		buffUntil = -99.0;
		buffStat = "";
		buffValue = 0;
	}

	public static void OnAttack(PlayerCtrl player, int monsterId)
	{
		Entry current = Current;
		if (current == null)
		{
			return;
		}
		switch (current.condType)
		{
		case "attackCount":
			Accumulate(current, player, 1);
			break;
		case "attackCountMob":
			if (monsterId != mobKey)
			{
				mobKey = monsterId;
				counter = 0;
			}
			Accumulate(current, player, 1);
			break;
		case "prob":
			if (Now >= coolUntil && Random.value < current.prob)
			{
				Trigger(current, player);
			}
			break;
		}
	}

	public static void OnKill(PlayerCtrl player)
	{
		if (Now - lastComboKill >= 5.0)
		{
			comboKills = 0;
		}
		comboKills++;
		lastComboKill = Now;
		OnCombo(player, comboKills);
		Entry current = Current;
		if (current != null && current.condType == "killCount")
		{
			Accumulate(current, player, 1);
		}
	}

	public static void OnCombo(PlayerCtrl player, int combo)
	{
		Entry current = Current;
		if (current != null && !(current.condType != "combokill") && current.count > 0 && combo > 0 && combo % current.count == 0)
		{
			TryTrigger(current, player);
		}
	}

	public static void OnHit(PlayerCtrl player)
	{
		Entry current = Current;
		if (current != null && !(current.condType != "hitCount"))
		{
			Accumulate(current, player, 1);
		}
	}

	public static void OnSkillUse(PlayerCtrl player)
	{
		Entry current = Current;
		if (current != null && !(current.condType != "cooltime"))
		{
			TryTrigger(current, player);
		}
	}

	public static void OnDeath(PlayerCtrl player)
	{
		Entry current = Current;
		if (current != null && !(current.condType != "die"))
		{
			TryTrigger(current, player);
		}
	}

	public static void OnPlayerDeath(PlayerCtrl player)
	{
		OnDeath(player);
		buffUntil = -99.0;
		buffStat = "";
		buffValue = 0;
		ResetProgress();
	}

	public static void OnRune(PlayerCtrl player)
	{
		Entry current = Current;
		if (current != null && !(current.condType != "rune"))
		{
			TryTrigger(current, player);
		}
	}

	private static void Accumulate(Entry entry, PlayerCtrl player, int amount)
	{
		if (!(Now < coolUntil))
		{
			if (entry.validtime > 0.0 && Now - lastEvent > entry.validtime)
			{
				counter = 0;
			}
			counter += amount;
			lastEvent = Now;
			if (counter >= entry.count)
			{
				counter = 0;
				Trigger(entry, player);
			}
		}
	}

	private static void TryTrigger(Entry entry, PlayerCtrl player)
	{
		if (!(Now < coolUntil))
		{
			Trigger(entry, player);
		}
	}

	private static void Trigger(Entry entry, PlayerCtrl player)
	{
		coolUntil = Now + (double)Mathf.Max(0f, (float)entry.cooldown);
		lastEvent = Now;
		switch (entry.effectType)
		{
		case "selfbuff":
			buffStat = entry.stat;
			buffValue = entry.value;
			buffUntil = Now + (double)Mathf.Max(0f, (float)entry.duration);
			break;
		case "reduceCooltime":
			ReduceCooldowns(player, entry.value);
			break;
		case "heal":
			Heal(player, entry.value);
			break;
		}
	}

	private static void ReduceCooldowns(PlayerCtrl player, int percent)
	{
		if (!((Object)(object)player == (Object)null))
		{
			((Component)player).GetComponent<SkillSystem>()?.ReduceCooldowns(percent);
		}
	}

	private static void Heal(PlayerCtrl player, int percent)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)player == (Object)null) && !(player.hp <= 0f))
		{
			int num = Mathf.Max(1, Mathf.RoundToInt(player.hpMax * (float)percent / 100f));
			float hp = player.hp;
			player.hp = Mathf.Min(player.hpMax, player.hp + (float)num);
			int num2 = Mathf.RoundToInt(player.hp - hp);
			if (num2 > 0)
			{
				DamageText.Show(Vector2.op_Implicit(player.FeetPos) + Vector3.up * 1.5f, num2, "blue");
			}
		}
	}

	private static bool BuffActive(string stat)
	{
		if (Now < buffUntil)
		{
			return buffStat == stat;
		}
		return false;
	}
}
