using System;
using System.Collections.Generic;
using Server.Bot;
using UnityEngine;

public sealed class BotCombatField : MonoBehaviour
{
	private sealed class Number
	{
		public EnemyUnit target;

		public int amount;

		public double due;
	}

	private readonly Dictionary<int, EnemyUnit> targets = new Dictionary<int, EnemyUnit>();

	private readonly List<Number> pending = new List<Number>();

	private FieldDirector field;

	private BotPartyGrindService service;

	public void Bind(FieldDirector owner, BotRuntime runtime)
	{
		if (service != runtime.partyGrind)
		{
			Unbind();
			field = owner;
			service = runtime.partyGrind;
			service.Monsters = Snapshot;
			service.Hit = Hit;
			service.Notice = Notice;
			service.Effect = Effect;
		}
	}

	private List<BotGrindMob> Snapshot(int mapId, int world, int channel)
	{
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		List<BotGrindMob> list = new List<BotGrindMob>();
		targets.Clear();
		if ((Object)(object)field == (Object)null || field.Map == null || field.StateId != mapId || world != GameSession.World || channel != GameSession.Channel)
		{
			return list;
		}
		foreach (EnemyUnit item in EnemyUnit.All)
		{
			if (!((Object)(object)item == (Object)null) && item.fieldMode && !item.dead)
			{
				int instanceID = ((Object)item).GetInstanceID();
				targets[instanceID] = item;
				list.Add(new BotGrindMob
				{
					id = instanceID,
					mobId = item.fieldMobId,
					mapId = mapId,
					world = world,
					channel = channel,
					x = Mathf.RoundToInt(((Component)item).transform.position.x * 40f),
					y = Mathf.RoundToInt((0f - ((Component)item).transform.position.y) * 40f),
					maxHp = (((Object)(object)item.NativeController != (Object)null) ? item.NativeController.MaximumHp : item.hpMax),
					alive = true,
					boss = item.IsBoss
				});
			}
		}
		return list;
	}

	private void Hit(BotCharacter bot, BotGrindMob mob, int damage, BotAttack.Skill skill, int[] lines)
	{
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		if (targets.TryGetValue(mob.id, out var value) && !((Object)(object)value == (Object)null) && !value.dead && field.Map != null && mob.mapId == field.StateId && bot.world == GameSession.World && bot.channel == GameSession.Channel)
		{
			if (skill.id > 0)
			{
				((Component)this).GetComponent<BotField>()?.PlaySkillCast(bot, skill.id, skill.level, (Component)(object)value);
			}
			if (lines.Length != 0)
			{
				DamageText.Show(value.DamageAnchor, lines[0]);
			}
			for (int i = 1; i < lines.Length; i++)
			{
				pending.Add(new Number
				{
					target = value,
					amount = lines[i],
					due = Time.realtimeSinceStartupAsDouble + (double)i * 0.08
				});
			}
			value.TakeBotHit(bot.id, damage, bot.facing);
			mob.alive = !value.dead;
		}
	}

	private static void Effect(int playerId, bool active)
	{
		if (GameSession.InGame && GameSession.CharacterId == playerId)
		{
			GameLoop.I?.player?.SetPartyGrindEffect(active);
		}
	}

	private static void Notice(int playerId, string text)
	{
		if (GameSession.InGame && GameSession.CharacterId == playerId)
		{
			NativeSystemMessage.Drop(5, text);
		}
	}

	private void Update()
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		int num = 0;
		while (num < pending.Count)
		{
			Number number = pending[num];
			if ((Object)(object)number.target == (Object)null || number.target.dead)
			{
				pending.RemoveAt(num);
				continue;
			}
			if (number.due > realtimeSinceStartupAsDouble)
			{
				num++;
				continue;
			}
			DamageText.Show(number.target.DamageAnchor, number.amount);
			pending.RemoveAt(num);
		}
	}

	private void Unbind()
	{
		if (service != null)
		{
			if (service.Monsters == new Func<int, int, int, List<BotGrindMob>>(Snapshot))
			{
				service.Monsters = null;
			}
			if (service.Hit == new Action<BotCharacter, BotGrindMob, int, BotAttack.Skill, int[]>(Hit))
			{
				service.Hit = null;
			}
			if (service.Notice == new Action<int, string>(Notice))
			{
				service.Notice = null;
			}
			if (service.Effect == new Action<int, bool>(Effect))
			{
				service.Effect = null;
			}
		}
		service = null;
		pending.Clear();
		targets.Clear();
	}

	private void OnDisable()
	{
		Unbind();
	}
}
