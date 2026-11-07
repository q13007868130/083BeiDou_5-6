using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class NativeMonsterVac : MonoBehaviour
{
	private double expires;

	private double nextPull;

	private int mapId;

	private Vector3 target;

	private readonly List<EnemyUnit> released = new List<EnemyUnit>();

	private readonly Dictionary<EnemyUnit, NativeImgAnimation> locked = new Dictionary<EnemyUnit, NativeImgAnimation>();

	public bool Active => expires > NativeBuffs.Now;

	public static NativeMonsterVac For(PlayerCtrl player)
	{
		return ((Component)player).GetComponent<NativeMonsterVac>() ?? ((Component)player).gameObject.AddComponent<NativeMonsterVac>();
	}

	public bool StartVac()
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		if (Active)
		{
			BottomBar.Toast("怪物吸星大法仍在生效中，无法叠加使用。");
			return false;
		}
		FieldDirector fieldDirector = GameLoop.I?.field;
		PlayerCtrl component = ((Component)this).GetComponent<PlayerCtrl>();
		if (fieldDirector?.Map == null || (Object)(object)component == (Object)null)
		{
			return false;
		}
		target = Vector2.op_Implicit(component.FeetPos);
		target.y = PlayerCtrl.GroundAt(target.x, target.y) + 0.025f;
		mapId = fieldDirector.Map.id;
		int num = ((GameServer.Local == null) ? 10 : GameConfigDao.GetInt(GameServer.Local.Db, "monster_vac_duration_minutes", 10));
		if (num <= 0)
		{
			num = 10;
		}
		int num2 = num * 60000;
		expires = NativeBuffs.Now + (double)num2 / 1000.0;
		nextPull = NativeBuffs.Now + 0.1;
		NativeBuffs.For(component).ApplyItem(2431158, new Dictionary<string, int> { 
		{
			"WDEF",
			NativeItemSource.Node(2431158).Get("spec").Int("pdd")
		} }, num2, NativeBuffs.Now);
		BottomBar.Toast("怪物吸星大法生效，持续" + num + "分钟。吸怪点已固定，不会跟随移动。");
		return true;
	}

	public void StopVac()
	{
		expires = 0.0;
		foreach (KeyValuePair<EnemyUnit, NativeImgAnimation> item in locked)
		{
			if ((Object)(object)item.Key != (Object)null)
			{
				item.Key.VacuumLocked = false;
			}
			if ((Object)(object)item.Value != (Object)null)
			{
				item.Value.Remove();
			}
		}
		locked.Clear();
		((Component)this).GetComponent<NativeBuffs>()?.Cancel(-2431158);
	}

	internal static Vector3 StepToward(Vector3 from, Vector3 to)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		double num = (to.x - from.x) * 40f;
		double num2 = (0f - (to.y - from.y)) * 40f;
		double num3 = Math.Sqrt(num * num + num2 * num2);
		if (num3 <= 150.0)
		{
			return to;
		}
		return from + new Vector3((float)Math.Floor(num * 150.0 / num3 + 0.5) / 40f, (0f - (float)Math.Floor(num2 * 150.0 / num3 + 0.5)) / 40f, 0f);
	}

	private void OnDestroy()
	{
		StopVac();
	}

	private void Update()
	{
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		if (expires == 0.0)
		{
			return;
		}
		if (!Active || GameLoop.I?.field?.Map?.id != mapId)
		{
			StopVac();
		}
		else
		{
			if (NativeBuffs.Now < nextPull)
			{
				return;
			}
			nextPull = NativeBuffs.Now + 0.1;
			bool flag = GameServer.Local == null || GameConfigDao.GetBool(GameServer.Local.Db, "monster_vac_stun_enabled", fallback: true);
			released.Clear();
			foreach (KeyValuePair<EnemyUnit, NativeImgAnimation> item in locked)
			{
				if ((Object)(object)item.Key == (Object)null || item.Key.dead)
				{
					released.Add(item.Key);
				}
			}
			foreach (EnemyUnit item2 in released)
			{
				if ((Object)(object)locked[item2] != (Object)null)
				{
					locked[item2].Remove();
				}
				if ((Object)(object)item2 != (Object)null)
				{
					item2.VacuumLocked = false;
				}
				locked.Remove(item2);
			}
			foreach (EnemyUnit item3 in EnemyUnit.All)
			{
				if (!((Object)(object)item3 == (Object)null) && item3.fieldMode && !item3.dead && !item3.IsBoss)
				{
					Vector3 vacuumPosition = item3.VacuumPosition;
					Vector3 val = vacuumPosition - target;
					if (val.sqrMagnitude > 0.04f)
					{
						item3.MoveVac(StepToward(vacuumPosition, target));
					}
					item3.VacuumLocked = true;
					if (!locked.ContainsKey(item3))
					{
						locked[item3] = null;
					}
					if (flag && (Object)(object)locked[item3] == (Object)null)
					{
						locked[item3] = NativeImgAnimation.Spawn("Skill/111.img/skill/1111008/mob", ((Component)item3).transform, item3.DamageAnchor, item3.BodySortingOrder + 1, flip: false, repeat: true, 2.5f);
					}
					else if (!flag && (Object)(object)locked[item3] != (Object)null)
					{
						locked[item3].Remove();
						locked[item3] = null;
					}
				}
			}
		}
	}
}
