using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class NativeBossEncounter : MonoBehaviour
{
	private readonly NativeBossTimeline tasks = new NativeBossTimeline();

	private FieldDirector field;

	private NativeLucidEncounter lucid;

	public void Bind(FieldDirector owner)
	{
		field = owner;
	}

	public void Schedule(EnemyUnit source, long delay, Action action, bool afterDeath = false)
	{
		tasks.Schedule(NativeMobController.Now, delay, () => afterDeath || ((Object)(object)source != (Object)null && !source.dead && ((Behaviour)source).isActiveAndEnabled), action);
	}

	private void Update()
	{
		if (!((Object)(object)field == (Object)null) && field.Map != null && !((Object)(object)GameLoop.I == (Object)null) && GameLoop.I.state == GameLoop.GameState.Playing && !PlayerCtrl.InputLocked && !(Time.deltaTime <= 0f))
		{
			tasks.Tick(NativeMobController.Now);
			lucid?.Tick();
		}
	}

	public void Clear()
	{
		tasks.Clear();
		lucid = null;
		NativeSkillVideo.Stop(NativeSkillVideo.Channel.BossScene);
	}

	public void OnSpawn(EnemyUnit mob)
	{
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		if (mob.eventOwned)
		{
			return;
		}
		if (NativeBossRules.IsLucid(mob.fieldMobId))
		{
			lucid = new NativeLucidEncounter(this, field, mob);
		}
		if (mob.fieldMobId >= 8810118 && mob.fieldMobId <= 8810122)
		{
			for (int i = 8810010; i <= 8810017; i++)
			{
				Remove(i);
			}
			int[] array = new int[8] { 8810107, 8810102, 8810103, 8810104, 8810105, 8810106, 8810108, 8810109 };
			foreach (int num in array)
			{
				if (Count(num) == 0)
				{
					field.SpawnNativeMob(num, ((Component)mob).transform.position);
				}
			}
		}
		if (mob.fieldMobId < 8810010 || mob.fieldMobId > 8810017)
		{
			return;
		}
		for (int k = 8810010; k <= 8810017; k++)
		{
			if (Count(k) == 0)
			{
				return;
			}
		}
		foreach (EnemyUnit item in new List<EnemyUnit>(EnemyUnit.All))
		{
			if (item.fieldMobId == 8810018 || (item.fieldMobId >= 8810118 && item.fieldMobId <= 8810122))
			{
				item.DefeatNative();
			}
		}
		for (int l = 8810010; l <= 8810017; l++)
		{
			Remove(l);
		}
	}

	public void OnDamaged(EnemyUnit mob, long amount)
	{
		int fieldMobId = mob.fieldMobId;
		switch (fieldMobId)
		{
		case 8810018:
		case 8810118:
		case 8810119:
		case 8810120:
		case 8810121:
		case 8810122:
			mob.NativeController?.Heal(amount);
			return;
		}
		bool flag = fieldMobId >= 8810002 && fieldMobId <= 8810009;
		bool flag2 = fieldMobId >= 8810102 && fieldMobId <= 8810109;
		if (!flag && !flag2)
		{
			return;
		}
		foreach (EnemyUnit item in EnemyUnit.All)
		{
			if (!item.fieldMode || item.dead)
			{
				continue;
			}
			bool num;
			if (!flag)
			{
				if (item.fieldMobId < 8810118)
				{
					continue;
				}
				num = item.fieldMobId <= 8810122;
			}
			else
			{
				num = item.fieldMobId == 8810018;
			}
			if (num)
			{
				item.NativeController?.FakeDamage(amount);
			}
		}
	}

	public void OnKilled(EnemyUnit mob)
	{
		lucid?.OnKilled(mob);
	}

	public bool BlocksDamage(EnemyUnit mob)
	{
		int fieldMobId = mob.fieldMobId;
		if (fieldMobId != 8800000 && fieldMobId != 8800100)
		{
			return false;
		}
		int num = fieldMobId + 3;
		foreach (EnemyUnit item in EnemyUnit.All)
		{
			if (item.fieldMode && !item.dead && item.fieldMobId >= num && item.fieldMobId <= num + 7)
			{
				return true;
			}
		}
		return false;
	}

	public void Effect(string path)
	{
		if (!string.IsNullOrEmpty(path) && !NativeBossVideoEffect.Handle(path))
		{
			Debug.Log((object)("[BossEffect] " + path));
		}
	}

	public void Damage(EnemyUnit source, int percent, Nullable absolute = null)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		PlayerCtrl playerCtrl = GameLoop.I?.player;
		if ((Object)(object)playerCtrl == (Object)null || playerCtrl.hp <= 0f)
		{
			return;
		}
		if (absolute.HasValue)
		{
			Rect value = absolute.Value;
			if (!value.Contains(NativeMobController.Pixels(Vector2.op_Implicit(playerCtrl.FeetPos))))
			{
				return;
			}
		}
		playerCtrl.TakeBossDamage(NativeBossRules.PercentDamage((int)playerCtrl.hpMax, percent));
	}

	public void Remove(int id)
	{
		foreach (EnemyUnit item in new List<EnemyUnit>(EnemyUnit.All))
		{
			if (item.fieldMode && item.fieldMobId == id)
			{
				item.dead = true;
				((Component)item).gameObject.SetActive(false);
				Object.Destroy((Object)(object)((Component)item).gameObject);
			}
		}
	}

	public int Count(int id)
	{
		int num = 0;
		foreach (EnemyUnit item in EnemyUnit.All)
		{
			if (item.fieldMode && !item.dead && item.fieldMobId == id)
			{
				num++;
			}
		}
		return num;
	}

	public EnemyUnit Spawn(int id, Vector2 pixels, bool ground = true)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		return field.SpawnNativeMob(id, new Vector3(pixels.x / 40f, (0f - pixels.y) / 40f, 0f), ground, !ground);
	}
}
